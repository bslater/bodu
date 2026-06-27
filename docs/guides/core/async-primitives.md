---
title: Async coordination primitives
---

# Async coordination primitives

The `Bodu.Threading` namespace provides the `async`/`await` counterparts of the synchronous gates in `System.Threading`. Each primitive lets a caller wait on a condition without blocking a thread, so a held lock or a pending signal can span an `await`. Acquisition is exposed through a `ValueTask` that must be awaited exactly once; an uncontended acquisition completes synchronously with no allocation, queued waiters are released in strict FIFO order, and *success wins over cancellation* — an acquisition that can be granted immediately is granted even when the supplied token is already canceled.

| Primitive | Synchronous / BCL analogue | Use it for |
|---|---|---|
| `AsyncLock` | `lock` statement / `SemaphoreSlim(1, 1)` | Async mutual exclusion that spans `await`. |
| `AsyncSemaphore` | `SemaphoreSlim` | Bounded concurrency — N permits. |
| `AsyncReaderWriterLock` | `ReaderWriterLockSlim` | Many concurrent readers or one exclusive writer. |
| `AsyncAutoResetEvent` | `AutoResetEvent` | Wake one waiter per signal. |
| `AsyncManualResetEvent` | `ManualResetEventSlim` | Open a gate for all current and future waiters. |
| `AsyncCountdownEvent` | `CountdownEvent` | Fan-in: release once N signals arrive. |
| `AsyncLazy<T>` | `Lazy<T>` | One-time async initialization, result cached and shared. |
| `AsyncDebouncer` | (no BCL equivalent) | Coalesce a burst of triggers into one trailing call. |
| `RateGate` | rate limiter / leading-edge throttle | Admit at most one call per fixed interval. |

## Pattern 1 — mutual exclusion with `AsyncLock`

`AsyncLock` is a non-reentrant async mutex. `LockAsync()` returns a `ValueTask<AsyncLock.Releaser>`; await it, then scope the held lock with a `using` statement so disposal releases it. Because the lock is held by a disposable releaser rather than a thread, the critical section may freely `await`.

```csharp
using Bodu.Threading;

private readonly AsyncLock _mutex = new();

public async Task UpdateAsync(CancellationToken cancellationToken)
{
    using (await _mutex.LockAsync(cancellationToken))
    {
        // Exclusive section; safe to await here.
        await SomeOperationAsync(cancellationToken);
    }
}
```

The lock is **not reentrant**: a flow that already holds it and calls `LockAsync()` again deadlocks. Dispose the `AsyncLock` only at shutdown — disposal faults any still-queued waiter with `ObjectDisposedException`.

## Pattern 2 — bounded concurrency with `AsyncSemaphore`

`AsyncSemaphore` admits a bounded number of concurrent holders. The disposable form, `LockAsync()`, pairs a permit with a `using` scope; the manual form pairs `WaitAsync()` with a `Release()` in a `finally` block. The constructor takes `(initialCount)`, or `(initialCount, maxCount)` to cap the permit count.

```csharp
using Bodu.Threading;

private readonly AsyncSemaphore _throttle = new(initialCount: 4);

// Disposable form: at most four downloads run concurrently.
public async Task DownloadAsync(Uri uri)
{
    using (await _throttle.LockAsync())
    {
        await HttpGetAsync(uri);
    }
}

// Manual form: pair every WaitAsync with a Release.
public async Task ProcessAsync()
{
    await _throttle.WaitAsync();
    try
    {
        await DoWorkAsync();
    }
    finally
    {
        _throttle.Release();
    }
}
```

`CurrentCount` reports the permits available right now. `Release(int)` returns several permits at once, handing them to queued waiters first.

## Pattern 3 — many concurrent readers, one writer with `AsyncReaderWriterLock`

`AsyncReaderWriterLock` admits any number of concurrent readers or a single exclusive writer. `ReaderAsync()` and `WriterAsync()` each return a `ValueTask<AsyncReaderWriterLock.Releaser>` scoped with `using`. The lock is writer-preferring, so a steady stream of writers can starve readers — and it has no upgradeable mode, so never request write access while holding read access on the same flow.

```csharp
using Bodu.Threading;

private readonly AsyncReaderWriterLock _lock = new();
private readonly Dictionary<string, int> _cache = new();

// Readers run concurrently with one another.
public async Task<int> CountAsync()
{
    using (await _lock.ReaderAsync())
        return _cache.Count;
}

// A writer runs exclusively, excluding all readers for its duration.
public async Task SetAsync(string key, int value)
{
    using (await _lock.WriterAsync())
        _cache[key] = value;
}
```

The releaser is idempotent: disposing it more than once releases the access exactly once.

## Pattern 4 — signalling with the event types

Three event primitives cover the common signalling shapes. Pick by how many waiters one signal should release.

**`AsyncAutoResetEvent`** — each `Set()` releases exactly one waiter, then reverts to unsignaled. It latches at most one pending signal. Ideal for a single-consumer "item ready" loop.

```csharp
using Bodu.Threading;

private readonly AsyncAutoResetEvent _itemReady = new();

public void OnItemEnqueued() => _itemReady.Set();   // releases one waiter

public async Task ConsumeAsync(CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        await _itemReady.WaitAsync(token);
        ProcessNextItem();
    }
}
```

**`AsyncManualResetEvent`** — `Set()` opens the gate for every current and future waiter; `Reset()` closes it again; `IsSet` reports the state. Use it as a one-to-many "ready" gate.

```csharp
using Bodu.Threading;

private readonly AsyncManualResetEvent _ready = new();

public async Task UseServiceAsync()
{
    await _ready.WaitAsync();   // all callers proceed once Set is called
    CallService();
}

public void OnInitialized() => _ready.Set();      // open the gate for everyone
public void OnConnectionLost() => _ready.Reset(); // later waiters block again
```

**`AsyncCountdownEvent`** — fan-in: `WaitAsync()` completes once `Signal()` has been called the configured number of times. Raise the count while it is still above zero with `AddCount()`; the event is not resettable.

```csharp
using Bodu.Threading;

var remaining = new AsyncCountdownEvent(initialCount: 3);

foreach (var job in jobs)
    _ = Task.Run(async () =>
    {
        await job.RunAsync();
        remaining.Signal();   // last Signal drives the count to zero
    });

await remaining.WaitAsync();  // completes once all three have signaled
```

## Pattern 5 — one-time async initialization with `AsyncLazy<T>`

`AsyncLazy<T>` runs an initializer at most once and caches the resulting `Task<T>`, shared by every awaiter. Construct it from a `Func<Task<T>>` (an async factory) or a `Func<T>` (a synchronous factory offloaded to the thread pool). The instance is awaitable directly, or use `GetValueAsync(token)` to abandon a single caller's wait without cancelling the shared work.

```csharp
using Bodu.Threading;

private readonly AsyncLazy<Config> _config =
    new(async () => await LoadConfigAsync());

public async Task<int> GetTimeoutAsync()
{
    Config config = await _config;   // factory runs once; result is cached and shared
    return config.TimeoutSeconds;
}

// Abandon only this caller's wait on timeout; the shared factory keeps running for others.
public async Task<Config> GetWithTimeoutAsync()
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    return await _config.GetValueAsync(cts.Token);
}
```

## Pattern 6 — debouncing with `AsyncDebouncer`

`AsyncDebouncer` coalesces a burst of `Invoke()` triggers into a single callback that runs once a quiet period has elapsed since the last trigger. The callback takes a `CancellationToken`. When a run becomes due while a previous callback is still running, behavior follows the `AsyncDebouncerExecutionPolicy` (default `QueueOneTrailingRun`). `FlushAsync()` runs a pending invocation immediately, `Cancel()` discards it, and `Dispose()` tears the debouncer down.

```csharp
using Bodu.Threading;

var debouncer = new AsyncDebouncer(
    TimeSpan.FromMilliseconds(300),
    async ct => await SaveAsync(ct));

// Called on every keystroke; SaveAsync runs once, 300 ms after typing stops.
textBox.TextChanged += (_, _) => debouncer.Invoke();

// On shutdown, run the pending callback now instead of waiting out the quiet period.
await debouncer.FlushAsync();
```

A callback that faults (other than its own cancellation) surfaces through the `CallbackFailed` event rather than being lost. Timing flows through a `TimeProvider`, so the quiet period can be driven deterministically in tests.

## Pattern 7 — rate limiting with `RateGate`

`RateGate` is a **synchronous** leading-edge gate: `TryInvoke()` returns `true` when a call is admitted and `false` when it falls inside the current cool-down window. The first call after construction is always admitted. It does no queuing, scheduling, or invoking of its own — it is a cheap, thread-safe decision you place in front of an async workflow.

```csharp
using Bodu.Threading;

private readonly RateGate _gate = new(TimeSpan.FromSeconds(1));

public void OnSignalChanged()
{
    // Refreshes at most once per second regardless of call frequency.
    if (_gate.TryInvoke())
        _ = RefreshAsync();
}
```

`TimeUntilNext` reports how long remains before the next call would be admitted (`TimeSpan.Zero` when a call can proceed now). Like the debouncer, `RateGate` schedules through a `TimeProvider` for deterministic testing.

## Where to go next

- [Circular buffer](circular-buffer.md) — a fixed-capacity FIFO ring buffer, another Bodu.Core foundation type.
- [Concurrent collections](concurrent-collections.md) — thread-safe collections that pair with these gates.
- [Bodu.Core introduction](../../docs/core/index.md) — install notes and the headline types at a glance.
- [Bodu.Threading API reference](xref:Bodu.Threading) — full namespace overview.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
