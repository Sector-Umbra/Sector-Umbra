using Robust.Shared.GameStates;

namespace Content.Shared._Umbra.Container;

[RegisterComponent, NetworkedComponent]
public sealed partial class ContainerClaimHolderComponent : Component
{
    /// <summary>
    /// TODO: Summaries
    /// </summary>
    [DataField]
    public EntityUid ClaimIDNumber;
}
