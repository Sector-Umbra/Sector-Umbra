using Robust.Shared.Configuration;

namespace Content.Shared._Umbra.Discord;

[CVarDefs]
public sealed class DiscordCCVars
{
    public static readonly CVarDef<bool> DiscordOAuthEnabled =
        CVarDef.Create("umbra.discord.oauth.enabled", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<string> DiscordOAuthClientId =
        CVarDef.Create("umbra.discord.oauth.client_id", "", CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordOAuthClientSecret =
        CVarDef.Create("umbra.discord.oauth.client_secret", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> DiscordApiUrl =
        CVarDef.Create("umbra.discord.api.url", "https://discord.com/api/v10", CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordOAuthUserAgentUrl =
        CVarDef.Create("umbra.discord.oauth.user_agent_url", "", CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordOAuthRedirectUrl =
        CVarDef.Create("umbra.discord.oauth.redirect_url", "", CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordOAuthUrl =
        CVarDef.Create("umbra.discord.oauth.url", "https://discord.com/oauth2/", CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordOAuthApiUrl =
        CVarDef.Create("umbra.discord.oauth.api_url", "https://discord.com/api/oauth2/", CVar.SERVERONLY);

    /// <summary>
    /// Defines if multi-accounts should be allowed.
    /// If false, a person attempting to authenticate with Discord on another ss14 account will be denied.
    /// </summary>
    public static readonly CVarDef<bool> DiscordOAuthAllowMultikey =
        CVarDef.Create("umbra.discord.oauth.allow_multi_key", false, CVar.SERVERONLY);
}
