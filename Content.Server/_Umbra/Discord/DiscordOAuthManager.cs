using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Umbra.Discord;
using Robust.Server.Player;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server._Umbra.Discord;

public sealed class DiscordOAuthManager : IPostInjectInit
{
    // https://discord.com/developers/docs/topics/oauth2#oauth2

    [Dependency] private readonly ILogManager _logManager = default!;
    // ReSharper disable once InconsistentNaming
    private ISawmill Log = default!;
    [Dependency] private readonly IStatusHost _statusHost = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _serverNetManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public static readonly IReadOnlyList<string> Scopes = new List<string>()
    {
        "identify",
        "guilds",
        "guilds.members.read",
    };

    private Uri _discordApi = null!;
    private string _userAgentUrl = null!;
    [ViewVariables]
    private readonly ConcurrentDictionary<string, (DateTime when, NetUserId who)> _pendingStates = new();
    [ViewVariables(VVAccess.ReadOnly)]
    private DateTime _nextUpdate = DateTime.MinValue;
    private readonly TimeSpan _cooldown = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Shared HTTP Client to be only used for turning codes into tokens.
    /// </summary>
    private readonly HttpClient _sharedHttpClient = new();

    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    };

    void IPostInjectInit.PostInject()
    {
        Log = _logManager.GetSawmill("oauth");
        _statusHost.AddHandler(OAuthHandler);
    }

    public void Initialize()
    {
        _cfg.OnValueChanged(DiscordCCVars.DiscordApiUrl, DiscordApiUrlChanged, true);
        _cfg.OnValueChanged(DiscordCCVars.DiscordOAuthUserAgentUrl, DiscordUrlChanged, true);

        _serverNetManager.RegisterNetMessage<DiscordOAuthAskStatusMessage>(ClientAskStatus);
        _serverNetManager.RegisterNetMessage<DiscordOAuthStatusMessage>();
        _serverNetManager.RegisterNetMessage<DiscordOAuthUnlinkMessage>(ClientUnlink);
        _serverNetManager.RegisterNetMessage<DiscordOAuthStartLinkMessage>(ClientLink);
        _serverNetManager.RegisterNetMessage<DiscordOAuthOpenUrlMessage>();
    }

    private void ClientLink(DiscordOAuthStartLinkMessage message)
    {
        var url = GetAuthUrl(message.MsgChannel.UserId);
        message.MsgChannel.SendMessage(new DiscordOAuthOpenUrlMessage()
        {
            Url = url,
        });
    }

    public void Shutdown()
    {
        _cfg.UnsubValueChanged(DiscordCCVars.DiscordApiUrl, DiscordApiUrlChanged);
        _cfg.UnsubValueChanged(DiscordCCVars.DiscordOAuthUserAgentUrl, DiscordUrlChanged);
    }

    private async void ClientUnlink(DiscordOAuthUnlinkMessage message)
    {
        try
        {
            var status = await GetStatus(message.MsgChannel.UserId);
            if (!status)
                return; // we dont have a link anyways

            var token = await _db.GetToken(message.MsgChannel.UserId);
            if (token == null)
                return; // how?

            await InvalidateToken(token);
            await _db.DeleteToken(token);

            message.MsgChannel.SendMessage(new DiscordOAuthStatusMessage()
            {
                Username = string.Empty,
                IsLinked = false,
            });
        }
        catch (Exception e)
        {
            Log.Error($"Encountered an error when unlinking status for player {message.MsgChannel.UserId}!\n{e}");
        }
    }

    private async void ClientAskStatus(DiscordOAuthAskStatusMessage message)
    {
        try
        {
            var status = await GetStatus(message.MsgChannel.UserId);
            var username = string.Empty;
            if (status)
            {
                var info = await GetTokenInfo(message.MsgChannel.UserId);
                username = info?.User.Username ?? string.Empty;
            }

            message.MsgChannel.SendMessage(new DiscordOAuthStatusMessage()
            {
                Username = username,
                IsLinked = status,
            });
        }
        catch (Exception e)
        {
            Log.Error($"Encountered an error when sending status for player {message.MsgChannel.UserId}!\n{e}");
        }
    }

    public async void Update()
    {
        // Try catch because async void,
        // any error that happens outside the try catch would be uncaught (unobserved task) and we do NOT want to lose a task
        try
        {
            if (DateTime.UtcNow < _nextUpdate)
                return;

            Log.Debug("Refreshing tokens...");

            _nextUpdate = DateTime.UtcNow.Add(_cooldown);

            await InvalidateLostTokens();
            await RefreshTokens();
        }
        catch (Exception e)
        {
            Log.Error("Error in update:", e);
        }
    }

    private async Task RefreshTokens()
    {
        var tokens = await _db.GetNearlyExpiredTokens();

        foreach (var token in tokens)
        {
            await RefreshToken(token);
        }
    }

    private async Task RefreshToken(DiscordOAuthToken token)
    {
        Log.Debug($"Refreshing token for {token.PlayerUserId}");

        var data = new Dictionary<string, string>
        {
            ["client_id"] = _cfg.GetCVar(DiscordCCVars.DiscordOAuthClientId),
            ["client_secret"] = _cfg.GetCVar(DiscordCCVars.DiscordOAuthClientSecret),
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = token.RefreshToken,
        };

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{_cfg.GetCVar(DiscordCCVars.DiscordOAuthApiUrl)}token");
        tokenRequest.Content = new FormUrlEncodedContent(data);

        var res = await _sharedHttpClient.SendAsync(tokenRequest);
        if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            // Our OAuth app was revoked via Discord UI.
            await _db.DeleteToken(token);
            Log.Info($"OAuth app access was removed for {token.PlayerUserId}, deleting DB entry.");
            return;
        }

        if (!res.IsSuccessStatusCode)
        {
            Log.Error($"Failed refreshing token for {token.PlayerUserId}!");
            return;
        }

        Models.TokenResponse? tokenResponse = null;
        var tokenJson = await res.Content.ReadAsStringAsync();
        try
        {
            tokenResponse = JsonSerializer.Deserialize<Models.TokenResponse>(tokenJson);
        }
        catch (Exception e)
        {
            Log.Error($"Failed deserializing Token from JSON. {tokenJson}", e);
            return;
        }

        if (tokenResponse == null)
        {
            Log.Error("Failed deserializing Token from JSON.");
            return;
        }

        var expires = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        await _db.SetToken(token.PlayerUserId!.Value, tokenResponse.AccessToken, tokenResponse.RefreshToken, expires, token.UserId);
        Log.Debug($"Got token for {token.PlayerUserId}, expires at {expires}!");
    }

    private async Task InvalidateLostTokens()
    {
        var lostTokens = await _db.GetLostTokens();

        foreach (var token in lostTokens)
        {
            await InvalidateToken(token);
            await _db.DeleteToken(token);
        }

        if (lostTokens.Count != 0)
            Log.Info($"Removed {lostTokens.Count} lost tokens!");
    }

    private async Task InvalidateToken(DiscordOAuthToken token)
    {
        var revokeParams = new Dictionary<string, string>
        {
            ["client_id"] = _cfg.GetCVar(DiscordCCVars.DiscordOAuthClientId),
            ["client_secret"] = _cfg.GetCVar(DiscordCCVars.DiscordOAuthClientSecret),
            ["token"] = token.AccessToken,
        };

        using var revokeRequest = new HttpRequestMessage(HttpMethod.Post, $"{_cfg.GetCVar(DiscordCCVars.DiscordOAuthApiUrl)}token/revoke");
        revokeRequest.Content = new FormUrlEncodedContent(revokeParams);

        await _sharedHttpClient.SendAsync(revokeRequest);
    }

    private void DiscordApiUrlChanged(string obj)
    {
        _discordApi = new Uri(obj);
    }

    private void DiscordUrlChanged(string obj)
    {
        _userAgentUrl = obj;
        if (string.IsNullOrEmpty(obj))
        {
            Log.Warning("URL for OAuth HTTP Client is empty! This may cause issues when interacting with Discord.");
        }
    }

    private async Task<bool> OAuthHandler(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Get || context.Url.AbsolutePath != "/discord/oauth")
            return false;

        var query = ParseQueryString(context.Url.Query);
        if (query.TryGetValue("error", out var error))
        {
            await context.RespondAsync($"OAuth error: {WebUtility.HtmlEncode(error)}", HttpStatusCode.OK);
            return true;
        }

        query.TryGetValue("code", out var code);
        query.TryGetValue("state", out var stateId);

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(stateId))
        {
            await context.RespondAsync("Missing code or state.", HttpStatusCode.BadRequest);
            return true;
        }

        // This also gets our netuserid
        if (!_pendingStates.TryRemove(stateId, out var state) || (DateTime.UtcNow - state.when) > TimeSpan.FromMinutes(10))
        {
            await context.RespondAsync("Invalid or expired state parameter.", HttpStatusCode.BadRequest);
            return true;
        }

        var postParams = new Dictionary<string, string>
        {
            ["client_id"] = _cfg.GetCVar(DiscordCCVars.DiscordOAuthClientId),
            ["client_secret"] = _cfg.GetCVar(DiscordCCVars.DiscordOAuthClientSecret),
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _cfg.GetCVar(DiscordCCVars.DiscordOAuthRedirectUrl),
        };

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{_cfg.GetCVar(DiscordCCVars.DiscordOAuthApiUrl)}token");
        tokenRequest.Content = new FormUrlEncodedContent(postParams);

        var tokenRes = await _sharedHttpClient.SendAsync(tokenRequest);
        if (!tokenRes.IsSuccessStatusCode)
        {
            var stringContent = await tokenRes.Content.ReadAsStringAsync();
            await context.RespondAsync("Error during token handshake.", HttpStatusCode.InternalServerError);
            Log.Error($"Failed getting Token from code: {tokenRes.StatusCode}\n{stringContent}");
            return true;
        }

        Models.TokenResponse? token = null;
        var tokenJson = await tokenRes.Content.ReadAsStringAsync();
        try
        {
            token = JsonSerializer.Deserialize<Models.TokenResponse>(tokenJson);
        }
        catch (Exception e)
        {
            Log.Error($"Failed deserializing Token from JSON. {tokenJson}", e);
            await context.RespondAsync("Error during token handshake.", HttpStatusCode.InternalServerError);
            return true;
        }

        if (token == null)
        {
            Log.Error("Failed deserializing Token from JSON.");
            await context.RespondAsync("Error during token handshake.", HttpStatusCode.InternalServerError);
            return true;
        }

        using var tempClient = GetHttpClient(token.AccessToken);
        var res = await tempClient.GetAsync("v10/oauth2/@me");
        var atMeContent = await res.Content.ReadAsStringAsync();
        var deserialized = JsonSerializer.Deserialize<Models.OAuthAtMe>(atMeContent, JsonSerializerOptions);
        if (deserialized == null)
        {
            Log.Error("Failed deserializing Token from JSON.");
            await context.RespondAsync("Error during token handshake.", HttpStatusCode.InternalServerError);
            return true;
        }

        if (!_cfg.GetCVar(DiscordCCVars.DiscordOAuthAllowMultikey))
        {
            var existingTokens = await _db.GetTokensByDiscordId(deserialized.User.Id);
            foreach (var existingToken in existingTokens)
            {
                if (existingToken.PlayerUserId == state.who)
                    continue;

                // multi-key !!
                Log.Warning($"{state.who} attempted to log in with Discord, however previous account {existingToken.PlayerUserId} is already linked to this Discord account.");
                await context.RespondAsync("Multi-Accounts are not permitted. Unlink your other account first.", HttpStatusCode.OK);
                return true;
            }
        }

        var expires = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
        await _db.SetToken(state.who, token.AccessToken, token.RefreshToken, expires, deserialized.User.Id);

        Log.Debug($"Got token for {state.who}, expires at {expires}!");
        await context.RespondAsync("Account linked! You may now return to the game.", HttpStatusCode.OK);
        // In case the person is actually on the server IN the options menu, send them their status.
        if (_playerManager.TryGetSessionById(state.who, out var session))
        {
            session.Channel.SendMessage(new DiscordOAuthStatusMessage()
            {
                Username = deserialized.User.Username,
                IsLinked = true,
            });
        }
        return true;
    }

    private async Task<HttpClient?> GetHttpClientFor(NetUserId id)
    {
        var token = await _db.GetToken(id);

        if (token == null)
            return null;

        if (DateTime.UtcNow >= token.ExpiresAt)
            return null; // our token is expired.

        var client = new HttpClient();
        client.BaseAddress = _discordApi;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", $"Space Station 14 ({_userAgentUrl}, 1.0)");

        return client;
    }

    private HttpClient GetHttpClient(string token)
    {
        var client = new HttpClient();
        client.BaseAddress = _discordApi;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", $"Space Station 14 ({_userAgentUrl}, 1.0)");

        return client;
    }

    /// <summary>
    /// Fetches the status of the OAuth integration for a given user. Returns true if we have a valid and working token. False if we do not.
    /// </summary>
    public async Task<bool> GetStatus(NetUserId id)
    {
        using var client = await GetHttpClientFor(id);

        if (client == null)
            return false;

        var deserialized = await GetTokenInfo(client);
        if (deserialized == null)
            return false;

        // We need to get a new token if our current token doesn't have the scopes we want.
        var hasScopes = Scopes.Any(x => deserialized.Scopes.Any(y => y == x));
        if (hasScopes)
            return true;

        return false;
    }

    public async Task<Models.OAuthAtMe?> GetTokenInfo(NetUserId id)
    {
        using var client = await GetHttpClientFor(id);
        if (client == null)
            throw new InvalidOperationException($"No OAuth token for user {id}");

        return await GetTokenInfo(client);
    }

    private async Task<Models.OAuthAtMe?> GetTokenInfo(HttpClient client)
    {
        var res = await client.GetAsync("v10/oauth2/@me");
        if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return null;

        var stringContent = await res.Content.ReadAsStringAsync();
        try
        {
            var deserialized = JsonSerializer.Deserialize<Models.OAuthAtMe>(stringContent, JsonSerializerOptions);
            return deserialized;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public async Task<List<string>> GetGuildList(NetUserId id)
    {
        using var client = await GetHttpClientFor(id);
        if (client == null)
            throw new InvalidOperationException($"No OAuth token for user {id}");

        var res = await client.GetAsync("v10/users/@me/guilds");
        res.EnsureSuccessStatusCode();

        var stringContent = await res.Content.ReadAsStringAsync();
        var deserialized = JsonSerializer.Deserialize<List<Models.PartialGuild>>(stringContent, JsonSerializerOptions);
        if (deserialized == null)
            return [];

        return deserialized.Select(x => x.Id).ToList();
    }

    public string GetAuthUrl(NetUserId id)
    {
        var state = GenerateState();
        _pendingStates[state] = (DateTime.UtcNow, id);
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _cfg.GetCVar(DiscordCCVars.DiscordOAuthClientId),
            ["redirect_uri"] = _cfg.GetCVar(DiscordCCVars.DiscordOAuthRedirectUrl),
            ["response_type"] = "code",
            ["scope"] = string.Join(" ", Scopes),
            ["state"] = state,
        };

        // me when no QueryString (i am not adding asp.net packages for this one usecase :godo:)
        var queryString = string.Join("&",
            query
                .Where(kvp => kvp.Value != null)
                .Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));

        var discordAuthEndpoint = $"{_cfg.GetCVar(DiscordCCVars.DiscordOAuthUrl)}authorize?{queryString}";
        return discordAuthEndpoint;
    }

    private static string GenerateState()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(query))
            return dict;

        if (query.StartsWith('?'))
            query = query[1..];

        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var kvp = pair.Split('=', 2);
            var key = WebUtility.UrlDecode(kvp[0]);
            var value = kvp.Length > 1 ? WebUtility.UrlDecode(kvp[1]) : "";
            dict[key] = value;
        }

        return dict;
    }
}
