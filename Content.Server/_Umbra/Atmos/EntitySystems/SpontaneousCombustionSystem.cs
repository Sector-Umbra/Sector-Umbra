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
        SubscribeLocalEvent<SpontaneousCombustionComponent, ComponentInit>(OnSpontaneousCombustionInitialize);
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

    private void OnSpontaneousCombustionInitialize(EntityUid uid, SpontaneousCombustionComponent spontaneousCombustion, ComponentInit args)
    {
        UpdateResistance(uid);
    }


    private void UpdateResistance(EntityUid uid)
    {
        // Checks if the entity equiping the item has the spontaneous combustion component.
        if (!TryComp<SpontaneousCombustionComponent>(uid, out var combustion))
            return;

        // Checks if the entity equiping the item has inventory components.
        if (!TryComp(uid, out InventoryComponent? inv) || !TryComp(uid, out ContainerManagerComponent? contMan))
            return;

        // 100 resistance means the entity takes full fire stacks.
        int resistance = 100;

        foreach (var slot in combustion.ProtectionSlots)
        {
            if (_inventorySystem.TryGetSlotEntity(uid, slot, out var equipment, inv, contMan) &&
                TryGetCombustionProtection(equipment.Value, out var itemResistance))
            {
                resistance -= itemResistance.Value;
            }
        }
        // converts to a float and divides by 100.
        combustion.CachedResistance = (resistance / 100f);
    }

    private bool TryGetCombustionProtection(EntityUid uid, [NotNullWhen(true)] out int? resistance)
    {
        if (TryComp<SpontaneousCombustionProtectionComponent>(uid, out var component))
        {
            resistance = component.ProtectionPercent;
            return true;
        }

        resistance = null;
        return false;
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
            // Returns if the resistance is 0 (fully immune)
            if (spontaneousCombustion.CachedResistance <= 0)
            {
                return;
            }

            var air = _atmosphereSystem.GetContainingMixture(uid);

            // Checks if air is not null and if the gas on the tile of the entity is above the mole minimum.
            if (air != null && air.GetMoles(spontaneousCombustion.Gas) >= spontaneousCombustion.MoleMinimum)
            {
                var stacks = spontaneousCombustion.FireStacks * spontaneousCombustion.CachedResistance;

                _flammableSystem.AdjustFireStacks(uid, stacks, flammable, true);
            }
        }
    }
}
