// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Threading;

/// <summary>
/// Coalesces a rapid burst of triggers into a single asynchronous invocation that runs once a quiet period has elapsed
/// since the most recent trigger.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="Invoke" /> (re)starts a quiet timer of the configured delay. The callback runs only when the
/// timer elapses with no intervening <see cref="Invoke" />, so a flurry of triggers results in a single execution.
/// <see cref="FlushAsync" /> runs a pending invocation immediately and awaits any work in flight, and
/// <see cref="Cancel" /> discards a pending invocation and cancels any in-flight callback.
/// </para>
/// <para>
/// <b>Execution policy.</b> When a run becomes due while a previous callback is still running, behavior is governed by
/// the configured <see cref="AsyncDebouncerExecutionPolicy" /> (defaulting to
/// <see cref="AsyncDebouncerExecutionPolicy.QueueOneTrailingRun" />). The default and
/// <see cref="AsyncDebouncerExecutionPolicy.DropWhileRunning" /> never overlap callbacks;
/// <see cref="AsyncDebouncerExecutionPolicy.CancelAndRestart" /> cancels and replaces the in-flight callback; and
/// <see cref="AsyncDebouncerExecutionPolicy.AllowOverlap" /> permits concurrent callbacks.
/// </para>
/// <para>
/// Timing uses the supplied <see cref="TimeProvider" /> (defaulting to <see cref="TimeProvider.System" />), which
/// allows deterministic testing with a controllable provider. Exceptions thrown by the callback (other than the
/// cancellation triggered by <see cref="Cancel" />, <see cref="Dispose" />, or a restart) are surfaced through the
/// <see cref="CallbackFailed" /> event rather than being silently lost; <see cref="CurrentExecution" /> exposes the
/// most recently started callback task.
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
[DebuggerDisplay("Delay = {_delay}, Policy = {ExecutionPolicy}, Pending = {_pending}, Active = {ActiveCount}")]
public sealed partial class AsyncDebouncer : IDisposable
{
    private readonly object _gate = new();
    private readonly TimeSpan _delay;
    private readonly Func<CancellationToken, ValueTask> _callback;
    private readonly TimeProvider _timeProvider;
    private readonly AsyncDebouncerExecutionPolicy _policy;
    private readonly List<Run> _active = new();
    private ITimer? _timer;
    private bool _pending;
    private bool _trailingPending;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDebouncer" /> class.
    /// </summary>
    /// <param name="delay">The quiet period that must elapse after the last trigger before the callback runs.</param>
    /// <param name="callback">The asynchronous callback invoked when the quiet period elapses.</param>
    /// <param name="executionPolicy">
    /// The policy governing behavior when a run becomes due while a callback is in flight.
    /// </param>
    /// <param name="timeProvider">
    /// The time provider used to schedule the delay, or <see langword="null" /> to use
    /// <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="callback" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="delay" /> is negative, or <paramref name="executionPolicy" /> is not a defined value.
    /// </exception>
    public AsyncDebouncer(
        TimeSpan delay,
        Func<CancellationToken, ValueTask> callback,
        AsyncDebouncerExecutionPolicy executionPolicy = AsyncDebouncerExecutionPolicy.QueueOneTrailingRun,
        TimeProvider? timeProvider = null)
    {
        ThrowHelper.ThrowIfNull(callback);
        ThrowHelper.ThrowIfNegative(delay.Ticks, nameof(delay));
        ThrowHelper.ThrowIfEnumValueIsUndefined(executionPolicy);

        _delay = delay;
        _callback = callback;
        _policy = executionPolicy;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Occurs when a callback invocation faults with an exception other than its own cancellation.
    /// </summary>
    /// <remarks>
    /// The handler runs outside the debouncer's internal gate on the thread that observed the failure.
    /// </remarks>
    public event EventHandler<Exception>? CallbackFailed;

    /// <summary>
    /// Gets the execution policy that governs overlapping runs.
    /// </summary>
    /// <value>The configured <see cref="AsyncDebouncerExecutionPolicy" />.</value>
    /// <returns>The policy applied when a run becomes due while a callback is in flight.</returns>
    public AsyncDebouncerExecutionPolicy ExecutionPolicy =>
        _policy;

    /// <summary>
    /// Gets the most recently started callback task, if one has been started.
    /// </summary>
    /// <value>The latest callback <see cref="Task" />, or <see langword="null" /> when none has started.</value>
    /// <returns>The latest callback task, or <see langword="null" /> when no callback has been started.</returns>
    public Task? CurrentExecution
    {
        get
        {
            lock (_gate)
            {
                return _active.Count > 0 ? _active[^1].Task : null;
            }
        }
    }

    /// <summary>
    /// Gets the number of callback invocations currently in flight.
    /// </summary>
    /// <value>The number of running callbacks.</value>
    /// <returns>The number of in-flight callbacks. Intended for diagnostics.</returns>
    internal int ActiveCount
    {
        get
        {
            lock (_gate)
            {
                return _active.Count;
            }
        }
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
    /// Runs a pending invocation immediately, bypassing the remaining quiet period, and awaits any callback work in
    /// flight.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel only this caller's wait for the in-flight work to drain.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask" /> that completes when the triggered and in-flight callbacks have completed.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The debouncer has been disposed.</exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was canceled before the work drained.
    /// </exception>
    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_pending)
            {
                _pending = false;
                _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                StartDueRun();
            }
        }

        return DrainAsync(cancellationToken);
    }

    /// <summary>
    /// Awaits all callback work currently in flight, including the trailing run queued under
    /// <see cref="AsyncDebouncerExecutionPolicy.QueueOneTrailingRun" />.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel only this caller's wait.</param>
    /// <returns>A <see cref="ValueTask" /> that completes when no callback work remains in flight.</returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was canceled before the work drained.
    /// </exception>
    public async ValueTask DrainAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            Task[] tasks;
            lock (_gate)
            {
                if (_disposed)
                    return;

                tasks = _active.Where(r => r.Task is not null).Select(r => r.Task!).ToArray();
                if (tasks.Length == 0)
                    return;
            }

            // RunCallbackAsync never faults, so Task.WhenAll cannot throw; a trailing run is registered before the
            // current run's task completes, so the next iteration observes it.
            await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Discards a pending invocation and cancels the token passed to any in-flight callback.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The debouncer has been disposed.</exception>
    public void Cancel()
    {
        List<Run> active;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _pending = false;
            _trailingPending = false;
            _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            active = new List<Run>(_active);
        }

        foreach (Run run in active)
            run.Cts.Cancel();
    }

    /// <summary>
    /// Releases the resources used by the debouncer, discarding any pending invocation and canceling any in-flight
    /// callbacks.
    /// </summary>
    public void Dispose()
    {
        ITimer? timer;
        List<Run> active;
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            _pending = false;
            _trailingPending = false;
            timer = _timer;
            _timer = null;
            active = new List<Run>(_active);
        }

        timer?.Dispose();
        foreach (Run run in active)
            run.Cts.Cancel();
    }

    /// <summary>
    /// Handles a quiet-period expiry by starting the callback for the pending trigger, subject to the execution policy.
    /// </summary>
    private void OnTimerElapsed()
    {
        lock (_gate)
        {
            if (_disposed || !_pending)
                return;

            _pending = false;
            StartDueRun();
        }
    }

    /// <summary>
    /// Decides how a newly due run is handled given the execution policy and starts it if appropriate. Must be called
    /// while holding <see cref="_gate" />.
    /// </summary>
    private void StartDueRun()
    {
        if (_active.Count == 0 || _policy == AsyncDebouncerExecutionPolicy.AllowOverlap)
        {
            StartRunCore();
            return;
        }

        switch (_policy)
        {
            case AsyncDebouncerExecutionPolicy.DropWhileRunning:
                break;

            case AsyncDebouncerExecutionPolicy.QueueOneTrailingRun:
                _trailingPending = true;
                break;

            case AsyncDebouncerExecutionPolicy.CancelAndRestart:
                foreach (Run run in _active)
                    run.Cts.Cancel();
                StartRunCore();
                break;
        }
    }

    /// <summary>
    /// Creates, registers, and starts a callback invocation. Must be called while holding <see cref="_gate" />.
    /// </summary>
    private void StartRunCore()
    {
        var run = new Run(new CancellationTokenSource());
        _active.Add(run);

        // Assigned under the gate so that a draining caller never observes the run before its task exists.
        run.Task = RunCallbackAsync(run);
    }

    /// <summary>
    /// Runs the callback, swallowing the cancellation produced by <see cref="Cancel" />, <see cref="Dispose" />, or a
    /// restart, surfacing any other failure through <see cref="CallbackFailed" />, and starting a queued trailing run.
    /// </summary>
    /// <param name="run">The invocation being executed.</param>
    /// <returns>A task representing the callback invocation.</returns>
    private async Task RunCallbackAsync(Run run)
    {
        try
        {
            await _callback(run.Cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (run.Cts.IsCancellationRequested)
        {
            // Expected when the invocation is canceled.
        }
        catch (Exception ex)
        {
            CallbackFailed?.Invoke(this, ex);
        }
        finally
        {
            lock (_gate)
            {
                _active.Remove(run);

                // Run a single coalesced trailing invocation once the last in-flight callback completes.
                if (!_disposed && _trailingPending && _active.Count == 0)
                {
                    _trailingPending = false;
                    StartRunCore();
                }
            }

            run.Cts.Dispose();
        }
    }
}
