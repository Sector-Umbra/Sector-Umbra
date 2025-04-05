using Robust.Shared.GameStates;

namespace Content.Shared._Umbra.Container;

[RegisterComponent, NetworkedComponent]
public sealed partial class ClaimableContainerComponent : Component
{
    /// <summary>
    /// TODO: Summaries
    /// </summary>
    [DataField]
    public bool Claimable = true;

    /// <summary>
    /// TODO: Summaries
    /// </summary>
    [DataField]
    public EntityUid IDClaim;
}

/// <summary>
/// Event raised on a lock after it has been toggled.
/// </summary>
[ByRefEvent]
public record struct AttemptClaimContainer(EntityUid IdNumber);
