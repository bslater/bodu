// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncerExecutionPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Specifies how an <see cref="AsyncDebouncer" /> behaves when a debounced run becomes due while a previous callback
/// invocation is still in flight.
/// </summary>
public enum AsyncDebouncerExecutionPolicy
{
    /// <summary>
    /// Coalesces overlapping runs into exactly one trailing run: while a callback is in flight, at most one additional
    /// run is queued and executed once the in-flight callback completes. This is the default and never overlaps
    /// callbacks.
    /// </summary>
    QueueOneTrailingRun = 0,

    /// <summary>
    /// Drops the due run: while a callback is in flight, a newly due run is discarded. A subsequent run requires a
    /// fresh trigger that becomes due after the in-flight callback completes. Never overlaps callbacks.
    /// </summary>
    DropWhileRunning = 1,

    /// <summary>
    /// Cancels the in-flight callback through its token and immediately starts a new callback. Briefly, the canceled
    /// callback may still be winding down while the replacement runs.
    /// </summary>
    CancelAndRestart = 2,

    /// <summary>
    /// Starts the due run concurrently with any in-flight callbacks, allowing callbacks to overlap.
    /// </summary>
    AllowOverlap = 3,
}
