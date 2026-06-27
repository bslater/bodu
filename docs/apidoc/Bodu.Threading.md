---
uid: Bodu.Threading
---

![Bodu.Threading](~/images/hero-core.svg)

## Purpose

**Bodu.Threading** is a set of asynchronous coordination primitives for `async`/`await` code — the awaitable counterparts of the BCL synchronous gates in <xref:System.Threading>. Each type lets a caller wait on a condition without blocking a thread: a held lock can span an `await`, a signal can be awaited, and a burst of triggers can be coalesced or rate-limited. The primitives own no operating-system handles (those that need explicit teardown implement <xref:System.IDisposable> only to fault still-queued waiters at shutdown).

The whole namespace follows two shared conventions. First, acquisition is exposed through a `ValueTask` that **must be awaited exactly once**, and a contention-free acquisition completes synchronously with no allocation. Second, **success wins over cancellation**: an acquisition that can be granted immediately is granted even when the supplied <xref:System.Threading.CancellationToken> is already canceled — the token only cancels an acquisition that must queue, and cancellation removes only the calling waiter. Waiters that do queue are released in strict FIFO order, with continuations scheduled asynchronously so a releasing thread is never hijacked to run a waiter inline.

## Static documentation

- **[Introduction](~/docs/core/index.md)** — the Bodu.Core headline types and the scenarios the library covers.
- **[Async coordination primitives](~/guides/core/async-primitives.md)** — each primitive mapped to its synchronous BCL analogue, with a compiling pattern per type.

## Key types

**Locks and gates**

- <xref:Bodu.Threading.AsyncLock> — a non-reentrant async mutex. `LockAsync()` returns a <xref:Bodu.Threading.AsyncLock.Releaser> whose disposal releases the lock; scope it with `using (await gate.LockAsync())`. Implements <xref:System.IDisposable>.
- <xref:Bodu.Threading.AsyncSemaphore> — an async counting semaphore for bounded concurrency. `WaitAsync()` / `Release()` for manual permit management, or `LockAsync()` for a disposable <xref:Bodu.Threading.AsyncSemaphore.Releaser>; ctors take `(initialCount)` or `(initialCount, maxCount)`.
- <xref:Bodu.Threading.AsyncReaderWriterLock> — a writer-preferring reader/writer lock. `ReaderAsync()` admits many concurrent readers; `WriterAsync()` grants one exclusive writer. Both yield an idempotent <xref:Bodu.Threading.AsyncReaderWriterLock.Releaser>. Implements <xref:System.IDisposable>.
- <xref:Bodu.Threading.RateGate> — a synchronous leading-edge admission gate. `TryInvoke()` returns whether a call is admitted this interval; `TimeUntilNext` reports the remaining cool-down.

**Events**

- <xref:Bodu.Threading.AsyncAutoResetEvent> — auto-reset signal: each `Set()` releases exactly one waiter and reverts to unsignaled (latching at most one pending signal). Wait with `WaitAsync()`.
- <xref:Bodu.Threading.AsyncManualResetEvent> — manual-reset gate: `Set()` releases all current and future waiters, `Reset()` closes the gate again, `IsSet` reports state.
- <xref:Bodu.Threading.AsyncCountdownEvent> — fan-in countdown: `Signal()` decrements the count, `AddCount()` / `TryAddCount()` raise it, and `WaitAsync()` completes once the count reaches zero. Not resettable.

**Lazy initialization**

- <xref:Bodu.Threading.AsyncLazy`1> — runs an initializer at most once and caches the resulting <xref:System.Threading.Tasks.Task`1>, shared by every awaiter. Awaitable directly (`await lazy`) or via `Value` / `GetValueAsync(token)`; constructed from a `Func<T>` (offloaded to the thread pool) or a `Func<Task<T>>`.

**Coalescing and rate limiting**

- <xref:Bodu.Threading.AsyncDebouncer> — coalesces a burst of `Invoke()` triggers into a single callback that runs once a quiet period elapses. `FlushAsync()` runs a pending invocation now, `Cancel()` discards it, `DrainAsync()` awaits in-flight work; overlap behavior is set by <xref:Bodu.Threading.AsyncDebouncerExecutionPolicy>. Implements <xref:System.IDisposable>.
- <xref:Bodu.Threading.AsyncDebouncerExecutionPolicy> — the overlap policy: `QueueOneTrailingRun` (default), `DropWhileRunning`, `CancelAndRestart`, `AllowOverlap`.

## Example

```csharp
using Bodu.Threading;

// Mutual exclusion: a held lock may span an await.
private readonly AsyncLock _mutex = new();

public async Task UpdateAsync()
{
    using (await _mutex.LockAsync())
    {
        // Exclusive section; safe to await here.
        await SomeOperationAsync();
    }
}
```

```csharp
using Bodu.Threading;

// Bounded concurrency: at most four operations run at once.
private readonly AsyncSemaphore _throttle = new(initialCount: 4);

public async Task DownloadAsync(Uri uri)
{
    using (await _throttle.LockAsync())
    {
        await HttpGetAsync(uri);
    }
}
```

## Notes

- **Await exactly once.** Every `WaitAsync` / `LockAsync` / `ReaderAsync` / `WriterAsync` returns a `ValueTask`; await it a single time. An uncontended acquisition completes synchronously and allocates nothing.
- **Success wins over cancellation.** A free lock, an available permit, or a latched signal is taken even when the token passed to `…Async(token)` is already canceled. The token only cancels an acquisition that must queue.
- **FIFO ordering.** <xref:Bodu.Threading.AsyncLock>, <xref:Bodu.Threading.AsyncSemaphore>, and <xref:Bodu.Threading.AsyncAutoResetEvent> release queued waiters in strict first-in, first-out order. <xref:Bodu.Threading.AsyncReaderWriterLock> is writer-preferring: queued writers go FIFO and readers are admitted in a batch when no writer is queued.
- **Not reentrant.** <xref:Bodu.Threading.AsyncLock> and <xref:Bodu.Threading.AsyncReaderWriterLock> deadlock if the same flow re-acquires access it already holds; the reader/writer lock also has no upgradeable mode.
- **Releasers dispose once.** A <xref:Bodu.Threading.AsyncLock.Releaser> or <xref:Bodu.Threading.AsyncSemaphore.Releaser> should be disposed exactly once; the <xref:Bodu.Threading.AsyncReaderWriterLock.Releaser> is idempotent and tolerates repeat disposal.
- **Disposal is for shutdown.** Disposing <xref:Bodu.Threading.AsyncLock>, <xref:Bodu.Threading.AsyncReaderWriterLock>, or <xref:Bodu.Threading.AsyncDebouncer> faults any still-queued waiter with <xref:System.ObjectDisposedException>; dispose only when no further acquisitions are expected. The event types and <xref:Bodu.Threading.AsyncSemaphore> own no handle and are not <xref:System.IDisposable>.
- **Testable timing.** <xref:Bodu.Threading.AsyncDebouncer> and <xref:Bodu.Threading.RateGate> schedule through a <xref:System.TimeProvider> (defaulting to <xref:System.TimeProvider.System>), so timing-dependent behavior can be driven deterministically in tests.
- **See also:** the [async coordination primitives guide](~/guides/core/async-primitives.md) and the [Bodu.Core introduction](~/docs/core/index.md).
