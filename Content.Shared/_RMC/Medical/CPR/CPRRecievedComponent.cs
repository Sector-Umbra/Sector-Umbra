using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Medical.CPR;

// ReSharper disable InconsistentNaming
// Disables Rider's InconsistentNaming error for this file to allow for capitalization of "CPR".

/// <summary>
/// Event used to track once an entity has recieved successful CPR.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CPRSystem))]
public sealed partial class CPRReceivedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Last;
}
