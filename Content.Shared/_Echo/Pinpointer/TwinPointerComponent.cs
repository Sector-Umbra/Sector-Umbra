using Robust.Shared.GameStates;

namespace Content.Shared._Echo.Pinpointer;

/// <summary>
/// Displays a sprite on the item that points towards the target component.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTwinPointerSystem))]
public sealed partial class TwinPointerComponent : Component
{
}
