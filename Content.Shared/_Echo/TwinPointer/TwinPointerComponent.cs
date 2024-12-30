using Robust.Shared.GameStates;

namespace Content.Shared._Echo.TwinPointer;

/// <summary>
/// Echo: Used to identify TwinPointers and link them on spawn.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TwinPointerComponent : Component
{
}
