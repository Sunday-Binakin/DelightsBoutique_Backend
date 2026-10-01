namespace DelightsBoutique.Application.Auth.Dtos;

public abstract record LoginResult;
public record LoginSucceeded(string LoginToken) : LoginResult;
public record LoginFailed : LoginResult;
public record LoginStaffDeactivated : LoginResult;
