using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Medical.CPR;

// ReSharper disable InconsistentNaming
// Disables Rider's InconsistentNaming error for this file to allow for capitalization of "CPR".

/// <summary>
/// Used to track what entities are currently receiving CPR.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(CPRSystem))]
public sealed partial class ReceivingCPRComponent : Component
{

}
