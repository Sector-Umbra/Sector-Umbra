using Content.Shared.Lock;


namespace Content.Shared._Umbra.Container;


public sealed class ClaimableContainerSystem : EntitySystem
{
    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClaimableContainerComponent, LockToggledEvent>(OnActivated);

    }

    private void OnActivated(EntityUid uid, ClaimableContainerComponent component, ref LockToggledEvent args)
    {
        // Locker has been either Locked, or Unlocked. Attempt to claim the locker with the persons ID.
        Log.Debug("among us trap beat 100 hour edition (real)");

        var ev = new AttemptClaimContainer();
        RaiseLocalEvent(uid, ref ev, true);

        ev.IdNumber = component.IDClaim;
    }
}

