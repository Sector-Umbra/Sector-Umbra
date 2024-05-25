using System.Threading;

namespace Content.Server.Mind;

[RegisterComponent]
public sealed partial class RecentlySwappedComponent : Component
{
    /// <summary>
    /// List of entities that this person recently hugged.
    /// </summary>
    public List<EntityUid> RecentlyHugged = new();

    /// <summary>
    /// The token used to cancel the timed removal of this component.
    /// </summary>
    public CancellationTokenSource TimerCancelToken;
}
