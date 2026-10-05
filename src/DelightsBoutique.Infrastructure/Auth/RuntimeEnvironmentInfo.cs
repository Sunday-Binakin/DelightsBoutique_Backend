using DelightsBoutique.Application.Abstractions;

namespace DelightsBoutique.Infrastructure.Auth;

public record RuntimeEnvironmentInfo(bool IsDevelopment) : IRuntimeEnvironment;
