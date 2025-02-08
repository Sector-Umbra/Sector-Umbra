using Content.Server.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Atmos.EntitySystems;

public sealed class SpontaneousCombustionSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    private const float UpdateTimer = 1f;
    private float _timer;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpontaneousCombustionProtectionComponent, GotEquippedEvent>(OnSpontaneousCombustionProtectionEquipped);
        SubscribeLocalEvent<SpontaneousCombustionProtectionComponent, GotUnequippedEvent>(OnSpontaneousCombustionProtectionUnequipped);
    }
    private void OnSpontaneousCombustionProtectionEquipped(EntityUid uid, SpontaneousCombustionProtectionComponent spontaneousCombustionProtection, GotEquippedEvent args)
    {
        if (TryComp<SpontaneousCombustionComponent>(args.Equipee, out var spontaneousCombustion) && spontaneousCombustion.ProtectionSlots.Contains(args.Slot))
        {
            UpdateResistance(args.Equipee);
        }
    }

    private void OnSpontaneousCombustionProtectionUnequipped(EntityUid uid, SpontaneousCombustionProtectionComponent spontaneousCombustionProtection, GotUnequippedEvent args)
    {
        if (TryComp<SpontaneousCombustionComponent>(args.Equipee, out var spontaneousCombustion) && spontaneousCombustion.ProtectionSlots.Contains(args.Slot))
        {
            UpdateResistance(args.Equipee);
        }
    }


    private void UpdateResistance(EntityUid uid)
    {
        if (!TryComp<SpontaneousCombustionComponent>(uid, out var combustion))
            return;

        if (!TryComp(uid, out InventoryComponent? inv) || !TryComp(uid, out ContainerManagerComponent? contMan))
            return;

        float resistance = 1f;

        foreach (var slot in combustion.ProtectionSlots)
        {
            if (!_inventorySystem.TryGetSlotEntity(uid, slot, out var equipment, inv, contMan) ||
                !TryGetCombustionProtection(equipment.Value, out var itemResistance))
            {
                continue;
            }

            if (itemResistance.HasValue)
            {
                resistance = resistance - itemResistance.Value;
            }
        }

        combustion.CachedResistance = resistance;
    }

    private bool TryGetCombustionProtection(EntityUid uid, [NotNullWhen(true)] out float? resistance)
    {
        resistance = null;
        if (!TryComp<SpontaneousCombustionProtectionComponent>(uid, out var component))
            return false;

        resistance = component.ProtectionPercent;
        return true;
    }

    public override void Update(float frameTime)
    {
        _timer += frameTime;

        if (_timer < UpdateTimer)
            return;

        _timer -= UpdateTimer;

        var enumerator = EntityQueryEnumerator<SpontaneousCombustionComponent, FlammableComponent>();
        while (enumerator.MoveNext(out var uid, out var spontaneousCombustion, out var flammable))
        {
            var air = _atmosphereSystem.GetContainingMixture(uid);

            if (air != null && air.GetMoles(spontaneousCombustion.gas) >= spontaneousCombustion.MoleMinimum) ;
            {
                var stacks = spontaneousCombustion.FireStacks * spontaneousCombustion.CachedResistance;

                _flammableSystem.AdjustFireStacks(uid, stacks, flammable, true);
            }
        }
    }
}
