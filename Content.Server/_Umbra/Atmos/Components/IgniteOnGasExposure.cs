
namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed partial class IgniteOnGasExposureComponent : Component
{
    [DataField]
    public float FireStacks = 1f;

    [DataField]
    public float MoleMinimum = 0.5f;

    [DataField]
    public int gas = 1;
}