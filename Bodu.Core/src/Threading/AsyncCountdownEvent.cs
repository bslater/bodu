// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncCountdownEvent.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Threading;

/// <summary>
/// Provides an asynchronous countdown synchronization primitive that becomes signaled once its count reaches zero,
/// releasing all waiters.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AsyncCountdownEvent" /> is the asynchronous analogue of <see cref="CountdownEvent" />. It starts with a
/// positive count; each <see cref="Signal()" /> decrements the count, and when the count reaches zero every caller
/// awaiting <see cref="WaitAsync()" /> is released. The count may be raised again with <see cref="AddCount()" /> while
/// it is still above zero.
/// </para>
/// <para>
/// The releasing gate is an inner <see cref="AsyncManualResetEvent" />, so continuations never run inline on the thread
/// that drives the count to zero. The type owns no operating-system handle and does not implement
/// <see cref="IDisposable" />.
/// </para>
/// <para>
/// Unlike <see cref="CountdownEvent" />, this type is <b>not resettable</b>: once the count reaches zero the event
/// stays signaled and the count cannot be raised again. Create a new instance to count down a second time.
/// </para>
/// </remarks>
[DebuggerDisplay("CurrentCount = {CurrentCount}")]
public sealed class AsyncCountdownEvent
{
    private readonly object _gate = new();
    private readonly AsyncManualResetEvent _gateEvent;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncCountdownEvent" /> class with the specified initial count.
    /// </summary>
    /// <param name="initialCount">
    /// The number of signals required to set the event. A value of zero creates an already-signaled event.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCount" /> is negative.</exception>
    public AsyncCountdownEvent(int initialCount)
    {
        ThrowHelper.ThrowIfNegative(initialCount);

        _count = initialCount;
        _gateEvent = new AsyncManualResetEvent(initialCount == 0);
    }

    /// <summary>
    /// Gets the number of remaining signals required to set the event.
    /// </summary>
    /// <value>The current count.</value>
    public int CurrentCount
    {
        get
        {
            lock (_gate)
            {
                return _count;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the event is signaled (its count has reached zero).
    /// </summary>
    /// <value><see langword="true" /> if the count is zero; otherwise, <see langword="false" />.</value>
    public bool IsSet
    {
        get
        {
            lock (_gate)
            {
                return _count == 0;
            }
        }
    }

    /// <summary>
    /// Asynchronously waits until the count reaches zero.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> that completes when the event is signaled.</returns>
    public ValueTask WaitAsync() =>
        _gateEvent.WaitAsync();

    /// <summary>
    /// Asynchronously waits until the count reaches zero, observing a cancellation request while waiting.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the wait.</param>
    /// <returns>A <see cref="ValueTask" /> that completes when the event is signaled.</returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was canceled before the event was signaled.
    /// </exception>
    public ValueTask WaitAsync(CancellationToken cancellationToken) =>
        _gateEvent.WaitAsync(cancellationToken);

    /// <summary>
    /// Registers a single signal, decrementing the count.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the signal caused the count to reach zero; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">The event is already signaled (its count is zero).</exception>
    public bool Signal() =>
        Signal(1);

    /// <summary>
    /// Registers the specified number of signals, decrementing the count.
    /// </summary>
    /// <param name="signalCount">The number of signals to register.</param>
    /// <returns>
    /// <see langword="true" /> if the signals caused the count to reach zero; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="signalCount" /> is less than one.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="signalCount" /> is greater than the remaining count.
    /// </exception>
    public bool Signal(int signalCount)
    {
        ThrowHelper.ThrowIfZeroOrNegative(signalCount);

        lock (_gate)
        {
            if (signalCount > _count)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CountdownSignalBelowZero);

            _count -= signalCount;
            if (_count != 0)
                return false;

            _gateEvent.Set();
            return true;
        }
    }

    /// <summary>
    /// Increments the count by one.
    /// </summary>
    /// <exception cref="InvalidOperationException">The event is already signaled (its count is zero).</exception>
    public void AddCount() =>
        AddCount(1);

    /// <summary>
    /// Increments the count by the specified amount.
    /// </summary>
    /// <param name="count">The amount by which to increase the count.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is less than one.</exception>
    /// <exception cref="InvalidOperationException">
    /// The event is already signaled (its count is zero), or increasing the count by <paramref name="count" /> would
    /// overflow <see cref="int.MaxValue" />.
    /// </exception>
    public void AddCount(int count)
    {
        if (!TryAddCount(count))
            throw new InvalidOperationException(ResourceStrings.Op_Invalid_CountdownAlreadyZero);
    }

    /// <summary>
    /// Attempts to increment the count by one.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the count was incremented; <see langword="false" /> if the event is already signaled.
    /// </returns>
    public bool TryAddCount() =>
        TryAddCount(1);

    /// <summary>
    /// Attempts to increment the count by the specified amount.
    /// </summary>
    /// <param name="count">The amount by which to increase the count.</param>
    /// <returns>
    /// <see langword="true" /> if the count was incremented; <see langword="false" /> if the event is already signaled.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is less than one.</exception>
    /// <exception cref="InvalidOperationException">
    /// Increasing the count by <paramref name="count" /> would overflow <see cref="int.MaxValue" />.
    /// </exception>
    public bool TryAddCount(int count)
    {
        ThrowHelper.ThrowIfZeroOrNegative(count);

        lock (_gate)
        {
            if (_count == 0)
                return false;

            // Reject before mutating so an overflowing request leaves the count unchanged.
            if (count > int.MaxValue - _count)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CountdownCountOverflow);

            _count += count;
            return true;
        }
    }
}
