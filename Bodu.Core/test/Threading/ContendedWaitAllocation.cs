// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ContendedWaitAllocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Measures what the asynchronous primitives allocate on the calling thread for one contended wait, so the threading
/// tests can hold those costs without timing anything.
/// </summary>
internal static class ContendedWaitAllocation
{
    /// <summary>
    /// The most a wait that must queue, with a token that cannot be canceled, may allocate: its completion source, the
    /// source's task and the queue node (144 bytes, or 160 when the result is a reader-writer releaser), with room for a
    /// runtime's layout to differ. A registration or a state machine would exceed it.
    /// </summary>
    public const long QueuedWaiterBytes = 192;

    /// <summary>
    /// The most a wait that must queue with a cancelable token may allocate: the waiter, its registration, the boxed
    /// registration state and one state machine (456 bytes), with room for a runtime's layout to differ. A second state
    /// machine wrapped around the first would exceed it.
    /// </summary>
    public const long CancellableQueuedWaiterBytes = 512;

    /// <summary>
    /// The most a write acquisition that must queue with a cancelable token may allocate (528 bytes, with room for a
    /// runtime's layout to differ). A 64-byte delegate created per call for the after-cancellation callback would
    /// exceed it.
    /// </summary>
    public const long CancellableQueuedWriterBytes = 560;

    /// <summary>
    /// The most a hand-off of <see cref="AsyncLock" /> to a queued waiter may allocate on average: nothing is needed,
    /// so a 64-byte delegate built to produce the releaser would exceed it.
    /// </summary>
    public const long HandOffBytes = 16;

    /// <summary>
    /// The most a hand-off of write access to a queued writer may allocate on average: the new releaser's 24-byte
    /// idempotency guard, with room for a runtime's layout to differ, and no delegate built to produce the releaser.
    /// </summary>
    public const long ReleaseGuardHandOffBytes = 48;

    /// <summary>The number of untimed calls made before measuring, so one-time costs fall outside the measurement.</summary>
    private const int WarmUpCalls = 200;

    /// <summary>The number of measured calls the average is taken over.</summary>
    private const int MeasuredCalls = 2000;

    /// <summary>
    /// Returns the average number of bytes the calling thread allocates per call of <paramref name="enqueue" />, each
    /// call queuing one more waiter behind a holder that never lets go, so nothing but the queued call is measured.
    /// </summary>
    /// <typeparam name="TWait">The type of the pending wait each call returns.</typeparam>
    /// <param name="enqueue">Queues one waiter and returns its pending wait.</param>
    /// <returns>The average bytes allocated per call, rounded down, and the waits that are still pending.</returns>
    public static (long BytesPerCall, TWait[] Pending) BytesPerQueuedCall<TWait>(Func<TWait> enqueue)
    {
        var pending = new TWait[WarmUpCalls + MeasuredCalls];
        for (int i = 0; i < WarmUpCalls; i++)
            pending[i] = enqueue();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = WarmUpCalls; i < pending.Length; i++)
            pending[i] = enqueue();

        return ((GC.GetAllocatedBytesForCurrentThread() - before) / MeasuredCalls, pending);
    }

    /// <summary>
    /// Returns the average number of bytes the calling thread allocates per hand-off, measuring only
    /// <paramref name="handOff" />, which passes the primitive to the waiter <paramref name="enqueue" /> queued.
    /// </summary>
    /// <param name="enqueue">Queues one waiter behind the current holder and returns its pending wait.</param>
    /// <param name="handOff">Releases the current holder, granting the queued waiter.</param>
    /// <param name="adopt">Takes the granted wait's result as the new holder once it has finished.</param>
    /// <typeparam name="TResult">The result a granted wait yields.</typeparam>
    /// <returns>The average bytes allocated per hand-off, rounded down.</returns>
    public static long BytesPerHandOff<TResult>(Func<Task<TResult>> enqueue, Action handOff, Action<TResult> adopt)
    {
        long total = 0;
        for (int i = 0; i < WarmUpCalls + MeasuredCalls; i++)
        {
            Task<TResult> next = enqueue();

            long before = GC.GetAllocatedBytesForCurrentThread();
            handOff();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            if (i >= WarmUpCalls)
                total += allocated;

            adopt(SpinUntilCompleted(next));
        }

        return total / MeasuredCalls;
    }

    /// <summary>
    /// Waits, without allocating on the calling thread, until a granted wait has finished on the thread pool.
    /// </summary>
    /// <typeparam name="TResult">The result the wait yields.</typeparam>
    /// <param name="task">The task of the granted wait.</param>
    /// <returns>The result of the wait.</returns>
    public static TResult SpinUntilCompleted<TResult>(Task<TResult> task)
    {
        var spinner = default(SpinWait);
        while (!task.IsCompleted)
            spinner.SpinOnce();

        return task.GetAwaiter().GetResult();
    }
}
