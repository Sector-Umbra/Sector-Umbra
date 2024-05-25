using System.Threading;
using Content.Server.Interaction.Components;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Mind;

public sealed class MindSwapSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteractHandEvent>(OnTouch);
        SubscribeLocalEvent<MindSwappedEvent>(OnMindSwap);
    }

    private void OnMindSwap(MindSwappedEvent ev)
    {
        if (!ev.AllowSwapBack)
            return; // No need to do anything if we can't swap back.

        Timer.Spawn(5000,
            () => _popupSystem.PopupEntity(
            message: Loc.GetString("mind-swap-minds-message-hug"),
            uid: ev.Person,
            recipient: ev.Person,
            PopupType.Large
        ));
    }

    /// <summary>
    /// Swaps minds back if both parties hugged each other.
    /// </summary>
    private void OnTouch(InteractHandEvent args)
    {
        if (args.User == args.Target)
            return; // Can't swap minds with yourself. How would that even work?

        // Get both recently swapped components.
        var personHuggedComp = CompOrNull<RecentlySwappedComponent>(args.User);
        var targetHuggedComp = CompOrNull<RecentlySwappedComponent>(args.Target);

        // If either component is null, return.
        if (personHuggedComp == null || targetHuggedComp == null)
        {
            return;
        }

        personHuggedComp.RecentlyHugged.Add(args.Target);

        // If the person hugged the target and the target hugged the person, swap minds.
        if (personHuggedComp.RecentlyHugged.Contains(args.Target) &&
            targetHuggedComp.RecentlyHugged.Contains(args.User))
        {
            SwapMinds(args.User, args.Target);
        }
    }

    public void SwapMinds(EntityUid a, EntityUid b, bool allowSwapBack = true)
    {
        if (a == b)
        {
            DebugTools.AssertNotEqual(a, b, "Swap mind with identical entities.");
            return;
        }

        var mindA = _mindSystem.GetMind(a);
        var mindB = _mindSystem.GetMind(b);
        if (!mindA.HasValue || !mindB.HasValue)
        {
            return;
        }

        _mindSystem.SwapMinds(mindA.Value, mindB.Value);
        _adminLogger.Add(LogType.Mind, $"{ToPrettyString(a)} and {ToPrettyString(b)} have swapped minds.");
        _stunSystem.TryParalyze(a, TimeSpan.FromSeconds(5), true);
        _popupSystem.PopupEntity(
            message: Loc.GetString("mind-swap-minds"),
            uid: a,
            recipient: a,
            PopupType.Large
        );

        _stunSystem.TryParalyze(b, TimeSpan.FromSeconds(5), true);
        _popupSystem.PopupEntity(
            message: Loc.GetString("mind-swap-minds"),
            uid: b,
            recipient: b,
            PopupType.Large
        );
        RaiseLocalEvent(new MindSwappedEvent(a, b, allowSwapBack));
        RaiseLocalEvent(new MindSwappedEvent(a, b, allowSwapBack));

        if (!allowSwapBack)
        {
            return;
        }

        var targetHuggedA = EnsureComp<RecentlySwappedComponent>(a);
        targetHuggedA.RecentlyHugged.Clear();
        targetHuggedA.TimerCancelToken?.Cancel();
        targetHuggedA.TimerCancelToken = new CancellationTokenSource();

        var targetHuggedB = EnsureComp<RecentlySwappedComponent>(b);
        targetHuggedB.RecentlyHugged.Clear();
        targetHuggedB.TimerCancelToken?.Cancel();
        targetHuggedB.TimerCancelToken = new CancellationTokenSource();

        Timer.Spawn(60000, () => RemComp(b, targetHuggedB), targetHuggedB.TimerCancelToken.Token);
        Timer.Spawn(60000, () => RemComp(a, targetHuggedA), targetHuggedA.TimerCancelToken.Token);
    }
}

public readonly record struct MindSwappedEvent(EntityUid Person, EntityUid Target, bool AllowSwapBack = true);
