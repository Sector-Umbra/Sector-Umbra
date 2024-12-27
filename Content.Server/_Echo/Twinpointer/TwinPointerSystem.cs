using Content.Shared._Echo.Pinpointer;
using Content.Shared.Pinpointer;

namespace Content.Server._Echo.Twinpointer;

public sealed class TwinPointerSystem : SharedTwinPointerSystem
{
    [Dependency] private readonly SharedPinpointerSystem _pinpointerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TwinPointerComponent, AfterItemsSpawnOnUseEvent>(Handler);
    }

    private void Handler(EntityUid uid, TwinPointerComponent component, AfterItemsSpawnOnUseEvent args)
    {
        var left = args.EntityUids[0];
        var right = args.EntityUids[1];
        _pinpointerSystem.SetTarget(left, right);
        _pinpointerSystem.SetTarget(right, left);
    }
}
