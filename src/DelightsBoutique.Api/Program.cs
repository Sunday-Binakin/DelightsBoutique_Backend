using DelightsBoutique.Api.Auth;
using DelightsBoutique.Api.Middleware;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Auth.Commands;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Orders.Services;
using DelightsBoutique.Domain.Interfaces;
using DelightsBoutique.Infrastructure;
using DelightsBoutique.Infrastructure.Auth;
using DelightsBoutique.Infrastructure.Idempotency;
using DelightsBoutique.Infrastructure.Notifications;
using DelightsBoutique.Infrastructure.Persistence;
using DelightsBoutique.Infrastructure.Persistence.Repositories;
using DelightsBoutique.Infrastructure.Products;
using DelightsBoutique.Infrastructure.Storage;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Log = Serilog.Log;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Storefront", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --- Persistence ---
builder.Services.AddDbContext<AppDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IOwnerProfileRepository, EfOwnerProfileRepository>();
builder.Services.AddScoped<IStaffRepository, EfStaffRepository>();
builder.Services.AddScoped<IProductRepository, EfProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EfCategoryRepository>();
builder.Services.AddScoped<ISiteContentRepository, EfSiteContentRepository>();
builder.Services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<ICustomerFlagRepository, EfCustomerFlagRepository>();
builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
builder.Services.AddScoped<IReportsRepository, EfReportsRepository>();
builder.Services.AddScoped<IOrderNotificationRepository, EfOrderNotificationRepository>();
builder.Services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IProductCsvParser, ProductCsvParser>();
builder.Services.AddScoped<INewsletterRepository, EfNewsletterRepository>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// --- Order-domain services ---
builder.Services.AddSingleton<IPhoneNormalizer, PhoneNormalizer>();
builder.Services.AddSingleton<IOrderIdGenerator, OrderIdGenerator>();
builder.Services.AddSingleton<IOrderPricingService, OrderPricingService>(); // stateless, safe as singleton
builder.Services.AddScoped<IImageValidator, ImageSharpImageValidator>();
builder.Services.AddScoped<IImageProcessor, ImageSharpImageProcessor>();
// "Spaces" (DigitalOcean Spaces, durable) in Staging/Production; defaults to local disk for
// day-to-day development, where nobody wants to configure real object storage credentials.
if (string.Equals(builder.Configuration["FileStorage:Provider"], "Spaces", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IFileStorage, SpacesFileStorage>();
else
    builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

// --- MediatR + idempotency/validation/audit pipeline ---
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(LoginCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(LoginCommand).Assembly);
// Field names appear both as the "fields" key and inside messages ("'email' is not a valid
// email address."), so both must use the same camelCase form the API uses everywhere else.
ValidatorOptions.Global.PropertyNameResolver = (_, member, _) => member is null ? null : ExceptionHandlingMiddleware.ToCamelCase(member.Name);
ValidatorOptions.Global.DisplayNameResolver = (_, member, _) => member is null ? null : ExceptionHandlingMiddleware.ToCamelCase(member.Name);
// IdempotencyBehavior registered FIRST (outermost) — same key + different body must 409
// regardless of whether the body would even pass validation; same key + same body must
// replay the cached response without re-running validation or the handler at all.
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLoggingBehavior<,>));

// --- Auth infrastructure ---
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IPasswordGenerator, PasswordGenerator>();
builder.Services.AddScoped<IOtpChallengeStore, EfOtpChallengeStore>();
builder.Services.AddScoped<IOtpEmailSender, GmailSmtpOtpEmailSender>();
builder.Services.AddSingleton<IRuntimeEnvironment>(new RuntimeEnvironmentInfo(builder.Environment.IsDevelopment()));

// --- Notifications: composite email+WhatsApp, delivered via a background queue so a slow
// provider never adds latency to the request that triggered the notification. ---
builder.Services.AddScoped<INotificationChannel, EmailNotificationSender>();
builder.Services.AddScoped<INotificationChannel, NoopWhatsAppNotificationSender>(); // swap for a real WhatsApp provider later — no other code changes needed
builder.Services.AddScoped<INotificationSender, CompositeNotificationSender>();
builder.Services.AddSingleton<NotificationQueue>();
builder.Services.AddSingleton<INotificationQueue>(sp => sp.GetRequiredService<NotificationQueue>());
builder.Services.AddHostedService<NotificationQueueProcessor>();

// --- Cookie authentication ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "db_admin_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        // This is an API, not a page app — never redirect, just return the status code.
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// --- Rate limiting ---
builder.Services.AddRateLimiter(options =>
{
    // Production keeps these as one global bucket (BUILD_SPEC's behavior). In Development they
    // partition by client IP, so one developer's test run doesn't lock every other
    // developer on the same local server out of logging in.
    // Integration tests share one host per class and would otherwise exhaust these in a single
    // run; no rate-limit test exists, so lifting them under "Testing" only changes test speed.
    int permitLimitForEnvironment(int productionLimit) =>
        builder.Environment.IsEnvironment("Testing") ? int.MaxValue : productionLimit;

    void AddAuthLimit(string policyName, int permitLimit) => options.AddPolicy(policyName, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: builder.Environment.IsDevelopment()
                ? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                : "global",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = permitLimitForEnvironment(permitLimit)
            }));

    AddAuthLimit("auth-login", 5);
    AddAuthLimit("auth-resend-otp", 3);

    // Orders endpoints — BUILD_SPEC explicitly says "rate-limited by IP," unlike the
    // global, non-partitioned auth-* policies above, so these use real per-IP partitioning.
    options.AddPolicy("order-create", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = permitLimitForEnvironment(10) }));
    options.AddPolicy("order-track", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = permitLimitForEnvironment(20) }));
    options.AddPolicy("order-payment-proof", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = permitLimitForEnvironment(10) }));

    // Not in BUILD_SPEC's original four — added per-IP to stop a spam bot from hammering
    // the public subscribe endpoint, same partitioning style and limit as order-create.
    options.AddPolicy("newsletter-subscribe", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = permitLimitForEnvironment(10) }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// Runs in every real environment — the storefront needs non-null content to function at
// all. Skipped under "Testing" because CustomWebApplicationFactory builds the host (running
// this) BEFORE it runs migrations, so the site_content table wouldn't exist yet; that test
// harness seeds content itself, after migrating, in CustomWebApplicationFactory.InitializeAsync().
if (!app.Environment.IsEnvironment("Testing"))
{
    using var contentSeedScope = app.Services.CreateScope();
    await ContentSeeder.SeedAsync(contentSeedScope.ServiceProvider);

    // Runs in every real environment, not just Development — a fresh Staging/Production
    // database needs its first owner account the same way. Idempotent (no-op once an
    // owner exists) and self-skips if Owner:Email isn't configured yet, so it's safe to
    // leave running unconditionally rather than re-gating per environment.
    using var ownerSeedScope = app.Services.CreateScope();
    await OwnerSeeder.SeedAsync(ownerSeedScope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Strictly Development-only, never Staging/Production — gives local frontend
    // integration a known staff login and a non-empty catalog out of the box.
    using var devSeedScope = app.Services.CreateScope();
    await DevSeeder.SeedAsync(devSeedScope.ServiceProvider);
}

var configuredFileStorageRoot = app.Configuration["FileStorage:RootPath"] ?? Path.Combine("App_Data", "uploads");
var fileStorageRoot = Path.IsPathRooted(configuredFileStorageRoot)
    ? configuredFileStorageRoot
    : Path.Combine(app.Environment.ContentRootPath, configuredFileStorageRoot); // PhysicalFileProvider below requires an absolute path
Directory.CreateDirectory(fileStorageRoot); // first-boot safety, dev/CI included

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("Storefront");
// Served files are public-by-design artifacts (same trust model as the opaque-id-as-
// access-token GET /api/orders/:id) — this only short-circuits on an actual /files/...
// match, so it can't swallow any existing API route; placed before auth/CSRF/rate
// limiting deliberately, not by oversight.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(fileStorageRoot),
    RequestPath = "/files",
    // Stored names are GUIDs and never reused, so a served file can't change under its URL.
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable"
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<UploadSizeGuardMiddleware>();
app.UseRateLimiter();
app.UseMiddleware<CsrfHeaderMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
