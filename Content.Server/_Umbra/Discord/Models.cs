using System.Text.Json.Serialization;

namespace Content.Server._Umbra.Discord;

public static class Models
{
    public record OAuthAtMe(
        [property: JsonPropertyName("scopes")]
        string[] Scopes,

        [property: JsonPropertyName("expires")]
        DateTimeOffset Expires,

        [property: JsonPropertyName("user")]
        PartialUser User
    );

    public record TokenResponse(
        [property: JsonPropertyName("access_token")]
        string AccessToken,
        [property: JsonPropertyName("token_type")]
        string TokenType,
        [property: JsonPropertyName("expires_in")]
        int ExpiresIn,
        [property: JsonPropertyName("refresh_token")]
        string RefreshToken,
        [property: JsonPropertyName("scope")]
        string Scope
    );

    public record PartialGuild(
        [property: JsonPropertyName("id")]
        string Id,
        [property: JsonPropertyName("name")]
        string Name
    );

    public record PartialUser(
        [property: JsonPropertyName("id")]
        string Id,
        [property: JsonPropertyName("username")]
        string Username
    );
}
