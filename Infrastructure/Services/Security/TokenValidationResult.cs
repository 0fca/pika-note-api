namespace PikaNoteAPI.Infrastructure.Services.Security;

public class TokenValidationResult
{
    public bool IsValid { get; init; }
    public string? NewAccessToken { get; init; }
    public string? NewRefreshToken { get; init; }

    public static TokenValidationResult Success() => new() { IsValid = true };
    public static TokenValidationResult Failure() => new() { IsValid = false };
    public static TokenValidationResult Refreshed(string accessToken, string? refreshToken) => new()
    {
        IsValid = true,
        NewAccessToken = accessToken,
        NewRefreshToken = refreshToken
    };
}
