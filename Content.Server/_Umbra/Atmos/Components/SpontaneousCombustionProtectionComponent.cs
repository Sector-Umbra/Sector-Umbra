
using Content.Server.Atmos.EntitySystems;

namespace Content.Server.Atmos.Components;

[RegisterComponent]
[Access(typeof(SpontaneousCombustionSystem))]
public sealed partial class SpontaneousCombustionProtectionComponent : Component
{
    [DataField]
    public float ProtectionPercent = 0.75f;

    public record struct GetSpontaneousCombustionProtectionValuesEvent
    {
        public float ProtectionPercent;
    }

}
