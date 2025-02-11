
namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed partial class SpontaneousCombustionComponent : Component
{
    /// <summary>
    /// A coeffecient of firestacks added every second
    /// </summary>
    [DataField]
    public float FireStacks = 1f;

    /// <summary>
    /// Minimum mole count required to add fire
    /// </summary>
    [DataField]
    public float MoleMinimum = 0.5f;

    /// <summary>
    /// Which gas triggers adding fire (1 is oxygen)
    /// </summary>
    [DataField]
    public int Gas = 1;

    /// <summary>
    /// Which slots will be checked for spontaneous combustion resistance
    /// </summary>
    [DataField("protectionSlots")]
    public List<string> ProtectionSlots = new() { "head", "jumpsuit", "gloves" };

    [DataField]
    public float CachedResistance = 1;
}
