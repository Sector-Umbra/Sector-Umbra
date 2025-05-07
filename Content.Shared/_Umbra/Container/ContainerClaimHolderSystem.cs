namespace Content.Shared._Umbra.Container;

public sealed partial class ContainerClaimHolderSystem : EntitySystem
{
    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClaimableContainerComponent, AttemptClaimContainer>(OnAttemptClaimContainer);


    }

    private void OnAttemptClaimContainer(EntityUid uid, ClaimableContainerComponent component, ref AttemptClaimContainer args)
    {
        Log.Debug(uid + " IM FUCKING SCREAMING " + component.Owner);
//        args.IdNumber = uid.
    }
}
