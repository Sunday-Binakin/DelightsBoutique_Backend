namespace DelightsBoutique.Application.Auth.Dtos;

public abstract record ResendOtpResult;
public record ResendOtpSucceeded : ResendOtpResult;
public record ResendOtpFailed : ResendOtpResult;
