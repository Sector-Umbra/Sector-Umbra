using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._RMC.CCVar;

[CVarDefs]
public sealed partial class RMCCVars : CVars
{
    /// <summary>
    /// Used to allow the user to set their own volume for Cassette Tapes
    /// </summary>
    public static readonly CVarDef<float> VolumeGainCassettes =
        CVarDef.Create("rmc.volume_gain_cassettes", 0.5f, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);
}
