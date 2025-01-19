using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Medical.CPR;

// ReSharper disable InconsistentNaming
// Disables Rider's InconsistentNaming error for this file to allow for capitalization of "CPR".
[RegisterComponent, NetworkedComponent]
[Access(typeof(CPRSystem))]
public sealed partial class ReceivingCPRComponent : Component
{

}
