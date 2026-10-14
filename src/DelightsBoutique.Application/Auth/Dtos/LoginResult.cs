namespace DelightsBoutique.Application.Auth.Dtos;

public abstract record LoginResult;
// DevCode is only ever populated in the Development environment — see LoginCommandHandler.
public record LoginSucceeded(string LoginToken, string? DevCode = null) : LoginResult;
public record LoginFailed : LoginResult;
public record LoginStaffDeactivated : LoginResult;
