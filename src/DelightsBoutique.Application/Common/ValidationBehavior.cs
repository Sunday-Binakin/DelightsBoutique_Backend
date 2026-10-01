using FluentValidation;
using MediatR;

namespace DelightsBoutique.Application.Common;

/// <summary>
/// Cross-cutting validation as a MediatR pipeline behavior — BUILD_SPEC §13 names this
/// exact pattern ("One MediatR pipeline behavior per concern"). Without this, FluentValidation
/// validators are registered in DI but nothing ever invokes them.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count > 0)
                throw new ValidationException(failures);
        }

        return await next();
    }
}
