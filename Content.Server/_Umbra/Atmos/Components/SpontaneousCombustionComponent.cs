
namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed partial class SpontaneousCombustionComponent : Component
{
    [DataField]
    public float FireStacks = 1f;

    [DataField]
    public float MoleMinimum = 0.5f;

    [DataField]
    public int gas = 1;

    [DataField("protectionSlots")]
    public List<string> ProtectionSlots = new() { "head", "jumpsuit", "gloves" };

    [DataField]
    public float CachedResistance = 0f;
}
