﻿namespace Content.Shared._RMC.Medical.CPR;

// ReSharper disable InconsistentNaming
// Disables Rider's InconsistentNaming error for this file to allow for capitalization of "CPR".

/// <summary>
/// Event used to confirm if an entity is allowed to receive CPR.
/// </summary>
[ByRefEvent]
public record struct ReceiveCPRAttemptEvent(EntityUid Performer, EntityUid Target, bool Start, bool Cancelled = false);
