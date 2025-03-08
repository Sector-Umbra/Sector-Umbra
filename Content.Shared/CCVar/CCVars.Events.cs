using Content.Shared.Administration;
using Content.Shared.CCVar.CVarAccess;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Controls if the game should run station events
    /// </summary>
    [CVarControl(AdminFlags.Server | AdminFlags.Mapping)]
    public static readonly CVarDef<bool>
        EventsEnabled = CVarDef.Create("events.enabled", true, CVar.ARCHIVE | CVar.SERVERONLY);

    /// Umbra
    /// <summary>
    /// Whether or not station events that pose a potential round ending threat can be run.
    /// </summary>
    public static readonly CVarDef<bool> RoundEndingThreatsEnabled =
        CVarDef.Create("events.allow_round_ending_threats", false, CVar.SERVERONLY);
}
