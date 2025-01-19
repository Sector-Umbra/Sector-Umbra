namespace Content.Shared._RMC.Medical.CPR;

// ReSharper disable InconsistentNaming
// Disables Rider's InconsistentNaming error for this file to allow for capitalization of "CPR".
[ByRefEvent]
public record struct PerformCPRAttemptEvent(EntityUid Target, bool Cancelled = false);
