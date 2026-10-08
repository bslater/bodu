// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.QueueingSynchronizationContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Text.Delimited;

public partial class DelimitedSerializerTests
{
    /// <summary>
    /// A single-threaded <see cref="SynchronizationContext" />, like a UI thread's, whose posted callbacks run only
    /// when its owner runs them.
    /// </summary>
    /// <remarks>
    /// While the owning thread blocks on a task, it runs nothing, so a continuation that an <see langword="await" />
    /// posts here never runs and the task never completes: the deadlock a library avoids by not resuming on the
    /// caller's context. <see cref="RunPosted" /> runs the queued callbacks afterwards, so that a test which found the
    /// deadlock leaves nothing waiting.
    /// </remarks>
    private sealed class QueueingSynchronizationContext
        : SynchronizationContext
    {
        /// <summary>The callbacks posted to the context, in order.</summary>
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _posted = new();

        /// <summary>
        /// Queues a callback, to run when the owner calls <see cref="RunPosted" />.
        /// </summary>
        /// <param name="d">The callback.</param>
        /// <param name="state">The state passed to the callback.</param>
        public override void Post(SendOrPostCallback d, object? state) =>
            _posted.Enqueue((d, state));

        /// <summary>
        /// Refuses to run a callback synchronously from another thread, which a single-threaded context cannot do while
        /// its thread is blocked.
        /// </summary>
        /// <param name="d">The callback.</param>
        /// <param name="state">The state passed to the callback.</param>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override void Send(SendOrPostCallback d, object? state) =>
            throw new NotSupportedException("A single-threaded context cannot run a callback for another thread.");

        /// <summary>
        /// Returns this context, which has no state to copy.
        /// </summary>
        /// <returns>This instance.</returns>
        public override SynchronizationContext CreateCopy() =>
            this;

        /// <summary>
        /// Runs the queued callbacks on the calling thread, including any they post in turn.
        /// </summary>
        public void RunPosted()
        {
            while (_posted.TryDequeue(out (SendOrPostCallback Callback, object? State) posted))
                posted.Callback(posted.State);
        }
    }
}
