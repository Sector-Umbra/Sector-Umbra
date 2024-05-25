using System.Linq;
using System.Numerics;
using Content.Server.Anomaly.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Administration.Logs;
using Content.Shared.Anomaly.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class BluespaceAnomalySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MindSwapSystem _mindSwapSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BluespaceAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<BluespaceAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<BluespaceAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
    }

    private void OnPulse(EntityUid uid, BluespaceAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var range = component.MaxShuffleRadius * args.Severity * args.PowerModifier;
        var mobs = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(xform.Coordinates, range, mobs);
        var allEnts = new ValueList<EntityUid>(mobs.Select(m => m.Owner)) { uid };
        var coords = new ValueList<Vector2>();
        foreach (var ent in allEnts)
        {
            if (xformQuery.TryGetComponent(ent, out var allXform))
                coords.Add(_xform.GetWorldPosition(allXform));
        }

        _random.Shuffle(coords);
        for (var i = 0; i < allEnts.Count; i++)
        {
            _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(allEnts[i])} has been shuffled to {coords[i]} by the {ToPrettyString(uid)} at {xform.Coordinates}");
            _xform.SetWorldPosition(allEnts[i], coords[i]);
        }

        if (_random.Prob(component.MindSwapChance))
        {
            var mindSwapMobs = new HashSet<Entity<MindContainerComponent>>();
            _lookup.GetEntitiesInRange(xform.Coordinates, range, mindSwapMobs);
            var mindSwapEnts = new ValueList<EntityUid>(mindSwapMobs.Where(m => m.Comp.HasMind).Select(m => m.Owner));
            if (mindSwapEnts.Count <= 1)
                return;

            var filteredMindSwapEnts = mindSwapEnts.Where(m => TryComp<MindShieldComponent>(m, out _) == false).ToList();

            while (filteredMindSwapEnts.Count > 1)
            {
                var shuffled = _random.GetItems(filteredMindSwapEnts, 2,false);
                // Remove the mind entities from the list so we don't swap them again
                filteredMindSwapEnts.Remove(shuffled[0]);
                filteredMindSwapEnts.Remove(shuffled[1]);

                var mind1 = _mindSystem.GetMind(shuffled[0]);
                var mind2 = _mindSystem.GetMind(shuffled[1]);
                if (mind1 == null || mind2 == null)
                    return;

                _mindSwapSystem.SwapMinds(shuffled[0], shuffled[1]);
            }
        }
    }

    private void OnSupercritical(EntityUid uid, BluespaceAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        var xform = Transform(uid);
        var mapPos = _xform.GetWorldPosition(xform);
        var radius = component.SupercriticalTeleportRadius * args.PowerModifier;
        var gridBounds = new Box2(mapPos - new Vector2(radius, radius), mapPos + new Vector2(radius, radius));
        var mobs = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(xform.Coordinates, component.MaxShuffleRadius, mobs);
        foreach (var comp in mobs)
        {
            var ent = comp.Owner;
            var randomX = _random.NextFloat(gridBounds.Left, gridBounds.Right);
            var randomY = _random.NextFloat(gridBounds.Bottom, gridBounds.Top);

            var pos = new Vector2(randomX, randomY);

            _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(ent)} has been teleported to {pos} by the supercritical {ToPrettyString(uid)} at {mapPos}");

            _xform.SetWorldPosition(ent, pos);
            _audio.PlayPvs(component.TeleportSound, ent);
        }

        // On super critical, swap minds of all entities in the supercritical area, while ignoring mindshields, and not allowing to swap back
        var mindSwapMobs = new HashSet<Entity<MindContainerComponent>>();
        _lookup.GetEntitiesInRange(xform.Coordinates, component.MaxShuffleRadius, mindSwapMobs);
        var mindSwapEnts = new ValueList<EntityUid>(mindSwapMobs.Where(m => m.Comp.HasMind).Select(m => m.Owner));
        if (mindSwapEnts.Count <= 1)
            return;

        var filteredMindSwapEnts = mindSwapEnts.Where(m => TryComp<MindShieldComponent>(m, out _) == false).ToList();

        while (filteredMindSwapEnts.Count > 1)
        {
            var shuffled = _random.GetItems(filteredMindSwapEnts, 2,false);
            // Remove the mind entities from the list so we don't swap them again
            filteredMindSwapEnts.Remove(shuffled[0]);
            filteredMindSwapEnts.Remove(shuffled[1]);

            var mind1 = _mindSystem.GetMind(shuffled[0]);
            var mind2 = _mindSystem.GetMind(shuffled[1]);
            if (mind1 == null || mind2 == null)
                return;

            _mindSwapSystem.SwapMinds(shuffled[0], shuffled[1], false);
        }
    }

    private void OnSeverityChanged(EntityUid uid, BluespaceAnomalyComponent component, ref AnomalySeverityChangedEvent args)
    {
        if (!TryComp<PortalComponent>(uid, out var portal))
            return;
        portal.MaxRandomRadius = (component.MaxPortalRadius - component.MinPortalRadius) * args.Severity + component.MinPortalRadius;
    }
}
