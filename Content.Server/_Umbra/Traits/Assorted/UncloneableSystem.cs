using Content.Server.Traits.Assorted;
using Content.Shared.Cloning.Events;

namespace Content.Server._Umbra.Traits.Assorted;

public sealed class UncloneableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UncloneableComponent, CloningAttemptEvent>(OnCloningAttempt);
    }

    private void OnCloningAttempt(Entity<UncloneableComponent> ent, ref CloningAttemptEvent args)
    {
        // Umbra: Prevent cloning if Uncloneable
        if (!ent.Comp.Cloneable)
            args.Cancelled = true;
    }
}
