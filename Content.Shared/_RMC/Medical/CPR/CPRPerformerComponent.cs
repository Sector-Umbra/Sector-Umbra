namespace Content.Shared._RMC.Medical.CPR;

// ReSharper disable InconsistentNaming
// Disables Rider's InconsistentNaming error for this file to allow for capitalization of "CPR".
[RegisterComponent]
[Access(typeof(CPRSystem))]
public sealed partial class CPRPerformerComponent : Component
{
    /// <summary>
    /// Required time between CPR "cycles".
    /// </summary>
    [DataField]
    public static int CPRDelay = 7;

}
