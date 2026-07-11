# WS-3 — Concurrency & Threading

**Scope:** `Bodu.Collections.Concurrent/src/` (`ConcurrentCircularBuffer<T>` Vyukov MPMC ring, `ConcurrentHashSet<T>`), `Bodu.Core/src/Threading/` (the async primitive family), and `SingleFlightCoordinator<T>` (in `Bodu.Financial.ExchangeRates`).

**Overall assessment: strong.** The lock-free ring's memory ordering, ABA safety, and seqlock generations are correct; the `ConcurrentHashSet` is a faithful `ConcurrentDictionary` port; the shared-work primitives (`SingleFlightCoordinator`, `AsyncLazy`) decouple cancellation cleanly. One confirmed High-severity race and one cross-family inconsistency are the actionable items.

## Findings

| # | file:line | category | severity | status | finding | recommendation |
|---|---|---|---|---|---|---|
| 1 | `AsyncDebouncer.cs:278-279`, `:304-305` vs `:398` | Concurrency | **High** | CONFIRMED | `RunCallbackAsync`'s `finally` removes the run under the gate then calls `run.Cts.Dispose()` **outside** the gate (`:398`). `Cancel()`/`Dispose()` snapshot `_active` under the gate then call `run.Cts.Cancel()` outside it (`:279`,`:305`). A callback that completes (normally or via cancellation) can dispose its CTS between the snapshot and the `Cancel()` call; `CancellationTokenSource.Cancel()` throws `ObjectDisposedException` on a disposed source. This surfaces `ObjectDisposedException` out of public `Cancel()` and — worse — out of `Dispose()`. | Guard the `Cancel()` calls (`try/catch ObjectDisposedException`), or don't dispose the per-run CTS, or move the CTS `Cancel` inside the gate / gate the dispose against a "cancelling" flag. |
| 2 | `AsyncLock.cs:212-216` vs `AsyncSemaphore.cs:275-289` / `AsyncReaderWriterLock.cs:334-349,357-372` | Architecture | Medium | CONFIRMED | Inconsistent grant-vs-cancel policy. `AsyncLock.AwaitAcquireAsync` re-checks `IsCancellationRequested` after the grant and *releases + throws* `OperationCanceledException`; the semaphore and RW-lock awaiters do **not** re-check — if `TrySetResult` wins the race the caller silently acquires despite a cancelled token. No leak (acquisition stays balanced), but the three sibling primitives disagree on identical semantics. | Pick one policy across the family and document it (the "success wins" model argues for dropping the re-check in `AsyncLock`; alternatively add the re-check to the other two). |
| 3 | `ConcurrentCircularBuffer{T}.cs:848` (`SlotIndex`) | Correctness | Low | PLAUSIBLE (theoretical) → **resolved by documenting** | `SlotIndex` uses `(uint)position % (uint)_capacity`. The Vyukov sequence math is modular-safe across `int` overflow, but the physical slot mapping is a clean permutation across the 2³² `uint` wrap only when `2³² % capacity == 0`, i.e. capacity is a power of two. The type accepts any `capacity >= 2`, so once per ~4 billion operations a non-power-of-two ring can misalign one slot (lost/duplicated slot → livelock). Standard Vyukov mandates power-of-two for exactly this reason. | Require power-of-two capacity (round up + mask) or document the 2³²-operation caveat. Extremely low real-world probability. |
| 4 | `SingleFlightCoordinator{T}.cs:157-158,189-190` | Performance | Low | CONFIRMED | Every caller allocates `new TaskCompletionSource(...)` before `GetOrAdd`, even the majority that only *join* an in-flight entry; the promise is discarded on join. | Minor; acceptable given `GetOrAdd`'s value-arg shape. Could use the factory overload if join-heavy contention matters. |
| 5 | `AsyncReaderWriterLock.Releaser.cs:63-81` | Architecture | Low | CONFIRMED | The RW-lock allocates a heap `ReleaseGuard` per acquisition (even uncontended) for idempotent disposal, whereas `AsyncLock`/`AsyncSemaphore` releasers are allocation-free structs. Inconsistent hot-path cost across the family. | Intentional; note the divergence. No change required. |

## Cleared hypotheses (no defect found)

- **Torn 64-bit reads on the ring:** `_head`/`_tail`/`Slot.Sequence` are all `int`; `Slot.Value` is a reference — all naturally atomic. `Volatile` acquire/release fencing on the producer publish (`Value` then `Sequence`, `:806-807`) and consumer/seqlock reads (`TryReadStableSlot`, `:874-899`) is correct.
- **ABA on head/tail CAS:** counters are monotonic (never decremented), so `CompareExchange` ABA cannot occur within a 2³² window; sequence generations differ by `capacity` each reuse, so the seqlock cannot commit a stale generation.
- **`ConcurrentHashSet` lock-striping:** faithful `ConcurrentDictionary` port — volatile `_tables`, volatile bucket-head publish, volatile `Node._next`, removed-node `_next` left intact for in-flight readers, `_budget` mutated only under all-locks. No defect.
- **`SingleFlightCoordinator` decoupled cancellation:** operation always runs with `CancellationToken.None`; per-caller `WaitAsync(token)` isolates cancellation; `TryRemove(KeyValuePair)` removes only the matching promise so a fresh attempt is never clobbered; exceptions propagate to all joiners. Matches the documented "leader cancel does not cancel followers" contract.
- **`RateGate` timer/drift:** contains **no** `Timer` — a pure synchronous timestamp gate over `TimeProvider`. No leaked-timer or drift surface.
- **`AsyncDebouncer` timer lifetime:** single reused `ITimer` (lazily created, `Change`d, disposed once) — no per-invoke timer leak. Only defect is finding #1's CTS race.
- **`AsyncManualResetEvent` Set/Reset:** `Interlocked.CompareExchange` swap of the TCS on reset with `IsCompleted` recheck is correct; captured-task waiters complete across a reset as documented.
- **`AsyncLazy` reentrancy:** `AsyncLocal<bool>` set after `await Task.Yield()` flows into the factory (incl. `Task.Run`) but not back to the trigger; reentrant `.Value` fails fast.
- **All queue-based primitives:** waiter TCS use `RunContinuationsAsynchronously`, so no releasing thread runs a continuation inline under `_gate` (no inline-reentrancy / thread-pool-starvation hazard). Cancellation removes only the calling node under the gate with `node.List` null-checks; `TrySetResult`/`TrySetCanceled` races resolve to a single winner with no lost permit/lock/signal.

## Hot-path notes

- Ring fast paths (`Enqueue`/`Dequeue`/`TryPeek`) are allocation-free; `Slot` is cache-line padded (trailing 7×`long`) after the hot fields to avoid false sharing.
- Uncontended `AsyncLock`/`AsyncSemaphore` acquisition allocates nothing (struct releaser, synchronous `ValueTask`). The RW-lock pays one `ReleaseGuard` alloc always (finding #5).
- Contended async waits allocate TCS + `LinkedListNode` + `CancellationTokenRegistration` — standard and unavoidable for FIFO fairness.

## Architecture / alignment notes

- The six `Bodu.Threading` primitives share a consistent template: `_gate` monitor, FIFO `LinkedList<TCS>`, cancellation-at-entry, `RunContinuationsAsynchronously`, dispose-faults-waiters. The two divergences worth reconciling are the post-grant cancel re-check (#2) and the releaser allocation model (#5).
- Cancellation-race handling for the shared-work primitives is uniformly "decouple shared work from per-caller wait via `Task.WaitAsync(token)`" — clean and consistent.

## Duplication notes

`CancelWaiter` (node-removal + `TrySetCanceled`) and the `while (_waiters.First is {} first) { RemoveFirst(); if (TrySetResult) … }` grant loop are near-identical across `AsyncLock`, `AsyncSemaphore`, `AsyncAutoResetEvent`, and `AsyncReaderWriterLock` (writers). Correct in each, but a candidate for a shared internal FIFO-waiter-queue helper. Low priority.

## Convention notes

All exception text routes through `ResourceStrings`/`ConcurrentCollectionsResourceStrings`; validation uses `ThrowHelper.ThrowIf*`; file headers, file-scoped namespaces, and `{T}` file suffixes conform.
