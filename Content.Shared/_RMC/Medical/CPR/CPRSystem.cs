using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC.Medical.CPR;

// ReSharper disable InconsistentNaming
// Disables Rider's InconsistentNaming error for this file to allow for capitalization of "CPR".
public sealed class CPRSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SharedRottingSystem _rotting = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [ValidatePrototypeId<DamageTypePrototype>]
    public const string HealType = "Asphyxiation";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CPRPerformerComponent, InteractHandEvent>(OnInteractHand,
            before: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<CPRPerformerComponent, CPRDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<ReceivingCPRComponent, ReceiveCPRAttemptEvent>(OnReceivingCPRAttempt);
        SubscribeLocalEvent<CPRReceivedComponent, ReceiveCPRAttemptEvent>(OnReceivedCPRAttempt);
        SubscribeLocalEvent<MobStateComponent, ReceiveCPRAttemptEvent>(OnMobStateCPRAttempt);
    }

    private void OnInteractHand(Entity<CPRPerformerComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = StartCPR(args.User, args.Target);
    }

    private void OnDoAfter(Entity<CPRPerformerComponent> ent, ref CPRDoAfterEvent args)
    {
        var performer = args.User;

        if (args.Target != null)
            RemComp<ReceivingCPRComponent>(args.Target.Value);

        if (args.Cancelled ||
            args.Handled ||
            args.Target is not { } target ||
            !CanCPRPopup(performer, target, false, out var damage))
        {
            return;
        }

        args.Handled = true;

        // TODO Make this reverse something else that isn't rotting.
        if (_net.IsServer)
            _rotting.ReduceAccumulator(target, TimeSpan.FromSeconds(CPRPerformerComponent.CPRDelay));

        if (!TryComp(target, out DamageableComponent? damageable) ||
            !damageable.Damage.DamageDict.TryGetValue(HealType, out damage))
        {
            return;
        }

        var heal = -FixedPoint2.Min(damage, CPRPerformerComponent.HealAmount);
        var healSpecifier = new DamageSpecifier();
        healSpecifier.DamageDict.Add(HealType, heal);
        _damageable.TryChangeDamage(target, healSpecifier, true);
        EnsureComp<CPRReceivedComponent>(target).Last = _timing.CurTime;

        if (_net.IsClient)
            return;

        var selfPopup = Loc.GetString("cm-cpr-self-perform", ("target", target), ("seconds", CPRPerformerComponent.CPRDelay));
        _popups.PopupEntity(selfPopup, target, performer);

        var othersPopup = Loc.GetString("cm-cpr-other-perform", ("performer", performer), ("target", target));
        var othersFilter = Filter.Pvs(performer).RemoveWhereAttachedEntity(e => e == performer);
        _popups.PopupEntity(othersPopup, performer, othersFilter, true, PopupType.Medium);
    }

    private void OnReceivingCPRAttempt(Entity<ReceivingCPRComponent> ent, ref ReceiveCPRAttemptEvent args)
    {
        args.Cancelled = true;

        if (_net.IsClient)
            return;

        var popup = Loc.GetString("cm-cpr-already-being-performed", ("target", ent.Owner));
        _popups.PopupEntity(popup, ent, args.Performer, PopupType.Medium);
    }

    private void OnReceivedCPRAttempt(Entity<CPRReceivedComponent> ent, ref ReceiveCPRAttemptEvent args)
    {
        if (args.Start)
            return;

        var target = ent.Owner;
        var performer = args.Performer;

        if (ent.Comp.Last > _timing.CurTime - TimeSpan.FromSeconds(CPRPerformerComponent.CPRDelay))
        {
            args.Cancelled = true;

            if (_net.IsClient)
                return;

            var selfPopup = Loc.GetString("cm-cpr-self-perform-fail-received-too-recently", ("target", target));
            _popups.PopupEntity(selfPopup, target, performer, PopupType.SmallCaution);

            var othersPopup = Loc.GetString("cm-cpr-other-perform-fail", ("performer", performer), ("target", target));
            var othersFilter = Filter.Pvs(performer).RemoveWhereAttachedEntity(e => e == performer);
            _popups.PopupEntity(othersPopup, performer, othersFilter, true, PopupType.SmallCaution);
        }
    }

    private void OnMobStateCPRAttempt(Entity<MobStateComponent> ent, ref ReceiveCPRAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_mobState.IsAlive(ent) || _rotting.IsRotten(ent))
            args.Cancelled = true;
    }

    private bool CanCPRPopup(EntityUid performer, EntityUid target, bool start, out FixedPoint2 damage)
    {
        damage = default;

        if (!HasComp<CPRPerformerComponent>(target) || !HasComp<CPRPerformerComponent>(performer))
            return false;

        var performAttempt = new PerformCPRAttemptEvent(target);
        RaiseLocalEvent(performer, ref performAttempt);

        if (performAttempt.Cancelled)
            return false;

        var receiveAttempt = new ReceiveCPRAttemptEvent(performer, target, start);
        RaiseLocalEvent(target, ref receiveAttempt);

        if (receiveAttempt.Cancelled)
            return false;

        if (!_hands.TryGetEmptyHand(performer, out _))
            return false;

        return true;
    }

    private bool StartCPR(EntityUid performer, EntityUid target)
    {
        if (!CanCPRPopup(performer, target, true, out _))
            return false;

        EnsureComp<ReceivingCPRComponent>(target);

        var doAfter = new DoAfterArgs(EntityManager, performer, TimeSpan.FromSeconds(4), new CPRDoAfterEvent(), performer, target)
        {
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };
        _doAfter.TryStartDoAfter(doAfter);

        if (_net.IsClient)
            return true;

        var selfPopup = Loc.GetString("cm-cpr-self-start-perform", ("target", target));
        _popups.PopupEntity(selfPopup, target, performer);

        var othersPopup = Loc.GetString("cm-cpr-other-start-perform", ("performer", performer), ("target", target));
        var othersFilter = Filter.Pvs(performer).RemoveWhereAttachedEntity(e => e == performer);
        _popups.PopupEntity(othersPopup, performer, othersFilter, true);

        return true;
    }
}
