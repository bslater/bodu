// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Threading;

/// <summary>
/// Coalesces a rapid burst of triggers into a single asynchronous invocation that runs once a quiet period has
/// elapsed since the most recent trigger.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="Invoke" /> (re)starts a quiet timer of the configured delay. The callback runs only when
/// the timer elapses with no intervening <see cref="Invoke" />, so a flurry of triggers results in a single
/// execution. <see cref="FlushAsync" /> runs a pending invocation immediately, and <see cref="Cancel" /> discards a
/// pending invocation without running it.
/// </para>
/// <para>
/// Timing uses the supplied <see cref="TimeProvider" /> (defaulting to <see cref="TimeProvider.System" />), which
/// allows deterministic testing with a controllable provider. Exceptions thrown by the callback (other than
/// cancellation) are not observed by the debouncer; the callback is responsible for handling its own failures.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// var debouncer = new AsyncDebouncer(
///     TimeSpan.FromMilliseconds(300),
///     async ct => await SaveAsync(ct));
///
/// // Called on every keystroke; SaveAsync runs once, 300 ms after typing stops.
/// textBox.TextChanged += (_, _) => debouncer.Invoke();
///]]>
/// </example>
[DebuggerDisplay("Delay = {_delay}")]
public sealed class AsyncDebouncer : IDisposable
{
    private readonly object _gate = new();
    private readonly TimeSpan _delay;
    private readonly Func<CancellationToken, ValueTask> _callback;
    private readonly TimeProvider _timeProvider;
    private ITimer? _timer;
    private CancellationTokenSource? _cts;
    private bool _pending;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDebouncer" /> class.
    /// </summary>
    /// <param name="delay">The quiet period that must elapse after the last trigger before the callback runs.</param>
    /// <param name="callback">The asynchronous callback invoked when the quiet period elapses.</param>
    /// <param name="timeProvider">The time provider used to schedule the delay, or <see langword="null" /> to use <see cref="TimeProvider.System" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="callback" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delay" /> is negative.</exception>
    public AsyncDebouncer(TimeSpan delay, Func<CancellationToken, ValueTask> callback, TimeProvider? timeProvider = null)
    {
        ThrowHelper.ThrowIfNull(callback);
        ThrowHelper.ThrowIfNegative(delay.Ticks, nameof(delay));

        _delay = delay;
        _callback = callback;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Registers a trigger, (re)starting the quiet period after which the callback runs.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The debouncer has been disposed.</exception>
    public void Invoke()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _pending = true;
            if (_timer is null)
                _timer = _timeProvider.CreateTimer(static state => ((AsyncDebouncer)state!).OnTimerElapsed(), this, _delay, Timeout.InfiniteTimeSpan);
            else
                _timer.Change(_delay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Runs a pending invocation immediately, bypassing the remaining quiet period.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> that completes when the callback completes. Completes immediately if nothing is pending.</returns>
    /// <exception cref="ObjectDisposedException">The debouncer has been disposed.</exception>
    public ValueTask FlushAsync()
    {
        CancellationToken token;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_pending)
                return ValueTask.CompletedTask;

            _pending = false;
            _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _cts = new CancellationTokenSource();
            token = _cts.Token;
        }

        return _callback(token);
    }

    /// <summary>
    /// Discards a pending invocation and cancels the token passed to any in-flight callback.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The debouncer has been disposed.</exception>
    public void Cancel()
    {
        CancellationTokenSource? cts;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _pending = false;
            _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            cts = _cts;
            _cts = null;
        }

        if (cts is null)
            return;

        cts.Cancel();
        cts.Dispose();
    }

    /// <summary>
    /// Releases the resources used by the debouncer, discarding any pending invocation.
    /// </summary>
    public void Dispose()
    {
        ITimer? timer;
        CancellationTokenSource? cts;
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            _pending = false;
            timer = _timer;
            _timer = null;
            cts = _cts;
            _cts = null;
        }

        timer?.Dispose();
        if (cts is not null)
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    /// <summary>
    /// Handles a quiet-period expiry by starting the callback for the pending trigger.
    /// </summary>
    private void OnTimerElapsed()
    {
        CancellationToken token;
        lock (_gate)
        {
            if (_disposed || !_pending)
                return;

            _pending = false;
            _cts = new CancellationTokenSource();
            token = _cts.Token;
        }

        _ = RunCallbackAsync(token);
    }

    /// <summary>
    /// Runs the callback, swallowing the cancellation that results from <see cref="Cancel" /> or <see cref="Dispose" />.
    /// </summary>
    /// <param name="token">The token observed by the callback.</param>
    /// <returns>A task representing the callback invocation.</returns>
    private async Task RunCallbackAsync(CancellationToken token)
    {
        try
        {
            await _callback(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Expected when the pending invocation is canceled.
        }
    }
}
