namespace PikaNoteAPI.Infrastructure.Services.Security;

public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? NewAccessToken { get; set; }
    public string? NewRefreshToken { get; set; }

    public static TokenValidationResult Success() => new() { IsValid = true };
    public static TokenValidationResult Failure() => new() { IsValid = false };
    public static TokenValidationResult Refreshed(string accessToken, string? refreshToken) => new()
    {
        IsValid = true,
        NewAccessToken = accessToken,
        NewRefreshToken = refreshToken
    };
}
