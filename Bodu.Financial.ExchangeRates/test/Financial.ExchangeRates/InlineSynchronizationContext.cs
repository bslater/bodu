// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InlineSynchronizationContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides a <see cref="SynchronizationContext" /> that runs every posted callback inline on the posting thread.
/// </summary>
/// <remarks>
/// An <see langword="await" /> that captures this context resumes on the thread that completes the awaited task, inside
/// the completion call, even when the task's source was created with
/// <see cref="TaskCreationOptions.RunContinuationsAsynchronously" />. Tests use it to run a caller's continuation at a
/// precise point in a component's completion sequence.
/// </remarks>
internal sealed class InlineSynchronizationContext : SynchronizationContext
{
    /// <summary>
    /// Runs the callback immediately on the calling thread.
    /// </summary>
    /// <param name="d">The callback to run.</param>
    /// <param name="state">The state passed to the callback.</param>
    public override void Post(SendOrPostCallback d, object? state) =>
        d(state);

    /// <summary>
    /// Runs the callback immediately on the calling thread.
    /// </summary>
    /// <param name="d">The callback to run.</param>
    /// <param name="state">The state passed to the callback.</param>
    public override void Send(SendOrPostCallback d, object? state) =>
        d(state);

    /// <summary>
    /// Returns this context, which holds no state to copy.
    /// </summary>
    /// <returns>This instance.</returns>
    public override SynchronizationContext CreateCopy() =>
        this;
}
