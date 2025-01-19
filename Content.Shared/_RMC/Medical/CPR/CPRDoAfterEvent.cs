using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC.Medical.CPR;

// ReSharper disable InconsistentNaming
// Disables Rider's InconsistentNaming error for this file to allow for capitalization of "CPR".
[Serializable, NetSerializable]
public sealed partial class CPRDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
