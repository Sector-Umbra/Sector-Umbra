using Content.Server.Silicons.Laws;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station.Components;

// Used in CD's System
using Content.Server._CD.Traits;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Robust.Shared.Player;

namespace Content.Server.StationEvents.Events;

public sealed class IonStormRule : StationEventSystem<IonStormRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;
    [Dependency] private readonly IChatManager _chatManager = default!; // Used for CD's System

    // funny
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Threats = "IonStormThreats";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Objects = "IonStormObjects";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Crew = "IonStormCrew";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Adjectives = "IonStormAdjectives";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Verbs = "IonStormVerbs";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string NumberBase = "IonStormNumberBase";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string NumberMod = "IonStormNumberMod";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Areas = "IonStormAreas";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Feelings = "IonStormFeelings";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string FeelingsPlural = "IonStormFeelingsPlural";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Musts = "IonStormMusts";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Requires = "IonStormRequires";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Actions = "IonStormActions";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Allergies = "IonStormAllergies";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string AllergySeverities = "IonStormAllergySeverities";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Concepts = "IonStormConcepts";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Drinks = "IonStormDrinks";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Foods = "IonStormFoods";
    [Dependency] private readonly IonStormSystem _ionStorm = default!;

    protected override void Started(EntityUid uid, IonStormRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        // CD Change - Go through everyone with the SynthComponent and inform them a storm is happening.
        var synthQuery = EntityQueryEnumerator<SynthComponent>();
        while (synthQuery.MoveNext(out var ent, out var synthComp))
        {
            if (RobustRandom.Prob(synthComp.AlertChance))
                continue;

            if (!TryComp<ActorComponent>(ent, out var actor))
                continue;

            var msg = Loc.GetString("station-event-ion-storm-synth");
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
            _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Yellow);
        }
        // End of CD change

        var query = EntityQueryEnumerator<SiliconLawBoundComponent, TransformComponent, IonStormTargetComponent>();
        while (query.MoveNext(out var ent, out var lawBound, out var xform, out var target))
        {
            // only affect law holders on the station
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
                continue;

            _ionStorm.IonStormTarget((ent, lawBound, target));
        }
    }
}
