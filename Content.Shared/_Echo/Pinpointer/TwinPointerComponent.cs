using Robust.Shared.GameStates;

namespace Content.Shared._Echo.Pinpointer;

/// <summary>
/// Echo: Used to identify TwinPointers and link them on spawn.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTwinPointerSystem))]
public sealed partial class TwinPointerComponent : Component
{
}
