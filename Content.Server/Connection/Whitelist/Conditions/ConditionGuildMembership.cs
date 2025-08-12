using Content.Shared.CCVar;

namespace Content.Server.Connection.Whitelist.Conditions;

/// <summary>
/// Condition that matches if a person is NOT a member of a guild using the <see cref="Content.Server._Umbra.Discord.DiscordOAuthManager"/>
/// </summary>
/// <remarks>
/// The Guild ID that is checked for is the one set in <see cref="CCVars.DiscordGuildId"/>
/// </remarks>
public sealed partial class ConditionGuildMembership : WhitelistCondition
{

}
