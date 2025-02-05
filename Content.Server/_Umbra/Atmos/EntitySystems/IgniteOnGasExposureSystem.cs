using Content.Server.Atmos.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed class IgniteOnGasExposureSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IgniteOnGasExposureComponent, ComponentAdd>(IgniteOnGasExpose);
    }

    private void IgniteOnGasExpose(EntityUid uid, IgniteOnGasExposureComponent component,  ref ComponentAdd args)
    {
        var air = _atmosphereSystem.GetContainingMixture(uid);

        if (!TryComp<FlammableComponent>(uid, out var flammable))
            return;

        if (air == null || air.GetMoles(component.gas) < component.MoleMinimum)
            return;

        _flammableSystem.AdjustFireStacks(uid, component.FireStacks, flammable, true);

    }

}