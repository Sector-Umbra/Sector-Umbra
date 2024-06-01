using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// An empty game rule that is only used for the StationEvent announcement.
/// </summary>
/// <remarks>
/// Umbra
/// </remarks>
[RegisterComponent, Access(typeof(AnnouncementOnlyRule))]
public sealed partial class AnnouncementOnlyRuleComponent : Component
{
}
