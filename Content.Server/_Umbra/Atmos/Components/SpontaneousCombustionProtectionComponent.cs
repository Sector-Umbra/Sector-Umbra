
using Content.Server.Atmos.EntitySystems;

namespace Content.Server.Atmos.Components;

[RegisterComponent]
[Access(typeof(SpontaneousCombustionSystem))]
public sealed partial class SpontaneousCombustionProtectionComponent : Component
{
    /// <summary>
    /// The reduction of fire added by spontanious combustion as a percent in an int (100 = 100 percent prevented)
    /// </summary>
    [DataField]
    public int ProtectionPercent = 75;

    public record struct GetSpontaneousCombustionProtectionValuesEvent
    {
        public int ProtectionPercent;
    }
}
