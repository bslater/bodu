---
title: Extending RingBackedCollection<T>
---

# Extending `RingBackedCollection<T>`

<xref:Bodu.Collections.Generic.RingBackedCollection`1> is the abstract base that [`CircularBuffer<T>`](circular-buffer.md) and [`Deque<T>`](deque.md) share. It owns the mechanics every ring-buffer-backed collection needs — a contiguous backing array, head and tail indices that wrap modulo `Capacity`, a live `Count`, a structural-version counter, and a fail-fast `struct` enumerator — and leaves the *policy* (what happens when the ring is full or empty, which end is written) to the derived type. If you need a bounded ring with semantics neither built-in type offers — a sliding-window sampler, a fixed-size undo stack with peek-from-either-end, a most-recent-N log — derive from it rather than re-implementing the wrap arithmetic.

## What the base owns

**Public, read-only surface (inherited as-is):** `Capacity`, `Count`, `IsEmpty`, `IsFull`, the head-relative indexer `this[int]`, `Contains(T)`, `CopyTo(T[], int)`, `ToArray()`, `Clear()`, `TrimExcess()`, and `GetEnumerator()` returning the shared `Enumerator` struct. The base also implements `IEnumerable<T>`, `IReadOnlyCollection<T>`, and the non-generic `ICollection` (`IsSynchronized` is always `false`; `SyncRoot` is provided for callers that lock externally).

**Protected primitives (for the derived type):**

| Primitive | Effect | Precondition the caller must guarantee |
|---|---|---|
| `AddTail(T item)` | Writes at the tail and advances it. | `Count < Capacity` |
| `AddHead(T item)` | Writes at the slot before the head and retreats the head. | `Count < Capacity` |
| `RemoveHead()` | Returns and clears the head element, advances the head. | `Count > 0` |
| `RemoveTail()` | Returns and clears the tail element, retreats the tail. | `Count > 0` |
| `PeekHead()` / `PeekTail()` | Reads without removing. | `Count > 0` |
| `OverwriteTail(T item)` | Writes at the slot the tail points to (which is the head when full) and advances **both** head and tail; `Count` is unchanged. The eviction primitive. | `Count == Capacity` |
| `Resize(int newCapacity)` | Replaces the backing array, copying the live region to index 0. | `newCapacity >= Count` and `>= 1` |
| `RingBackedCollection(int capacity)` / `RingBackedCollection(IEnumerable<T>, int capacity)` | Constructors; `capacity < 1` throws `ArgumentOutOfRangeException`. | — |

> [!IMPORTANT]
> The protected mutators perform **no** capacity or emptiness validation — that is what keeps the built-in types' hot paths branch-free. The derived type owns the contract: test `IsFull` / `IsEmpty` (or `Count`) before calling, and throw or evict according to its own policy. Calling a primitive outside its precondition corrupts the ring.

## The version counter and the fail-fast contract

Every primitive that changes the structure — `AddTail`, `AddHead`, `RemoveHead`, `RemoveTail`, `OverwriteTail`, `Resize`, and the public `Clear()` / `TrimExcess()` — bumps a private version counter. The base `Enumerator` captures the version when it is created and compares it on every `MoveNext`; a mismatch throws <xref:System.InvalidOperationException>. Because the counter lives in the base and is bumped *inside* the primitives, a derived type gets fail-fast enumeration for free — there is nothing to call and nothing to forget. `PeekHead` / `PeekTail` and the read-only public members do not bump it.

The base additionally latches an *evicting* flag around eviction-event dispatch in `CircularBuffer<T>` and `Deque<T>`, so a handler that tries to mutate the collection mid-eviction gets a deterministic `InvalidOperationException` instead of a corrupted ring; that machinery is internal to those two types and is not part of the protected surface.

## Worked example — a fixed-capacity sliding-window sampler

The type below keeps the most recent `size` samples: a push into a full window overwrites the oldest sample, and the statistics run over whatever is held. Everything policy-related — what "full" means, which end evicts — is in the derived type; everything mechanical is inherited.

```csharp
using Bodu.Collections.Generic;

/// <summary>
/// A fixed-capacity sliding window over the most recent samples: pushing into a full window
/// overwrites the oldest sample, and the statistics are computed over whatever is held.
/// </summary>
public sealed class SlidingWindow : RingBackedCollection<double>
{
    public SlidingWindow(int size)
        : base(size)
    {
    }

    /// <summary>Records a sample, discarding the oldest when the window is full.</summary>
    public void Push(double sample)
    {
        // The protected primitives do no bounds checks: the derived type owns the contract.
        if (IsFull)
            OverwriteTail(sample);      // overwrite the oldest slot and slide head+tail forward; Count unchanged
        else
            AddTail(sample);            // append; requires Count < Capacity
    }

    /// <summary>Gets the most recently pushed sample.</summary>
    public double Latest => IsEmpty ? throw new InvalidOperationException("The window is empty.") : PeekTail();

    /// <summary>Gets the oldest sample still inside the window.</summary>
    public double Oldest => IsEmpty ? throw new InvalidOperationException("The window is empty.") : PeekHead();

    /// <summary>Removes and returns the oldest sample.</summary>
    public bool TryDropOldest(out double sample)
    {
        if (IsEmpty)
        {
            sample = default;
            return false;
        }

        sample = RemoveHead();
        return true;
    }

    /// <summary>Gets the arithmetic mean of the held samples.</summary>
    public double Average()
    {
        if (IsEmpty) return double.NaN;

        double sum = 0;
        foreach (double sample in this)      // the base struct enumerator, head-to-tail
            sum += sample;
        return sum / Count;
    }

    /// <summary>Widens the window, keeping every held sample.</summary>
    public void Grow(int newSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newSize, Count);
        if (newSize != Capacity) Resize(newSize);
    }
}
```

Using it:

```csharp
using Bodu.Collections.Generic;

var window = new SlidingWindow(3);
window.Push(10);
window.Push(20);
window.Push(30);
window.Push(40);                                       // full: overwrites 10

double[] held = window.ToArray();                      // 20, 30, 40 — head-to-tail logical order
double mean   = window.Average();                      // 30
double oldest = window.Oldest, latest = window.Latest; // 20, 40
double first  = window[0], last = window[2];           // 20, 40 — the indexer is head-relative
bool full     = window.IsFull;                         // true (3/3)
bool has30    = window.Contains(30);                   // true

window.Grow(5);
window.Push(50);                                       // 20, 30, 40, 50; Capacity == 5

bool dropped  = window.TryDropOldest(out double gone); // true, gone == 20

// Fail-fast: the base enumerator carries the structural version.
try
{
    foreach (double sample in window)
        window.Push(sample + 1);
}
catch (InvalidOperationException)
{
    Console.WriteLine("enumerator invalidated by Push");
}

window.TrimExcess();                                   // Capacity shrinks to Count (4)
window.Clear();                                        // IsEmpty == true; Average() == NaN

System.Collections.ICollection asCollection = window;
bool synchronized = asCollection.IsSynchronized;       // false — never thread-safe
```

Points worth noticing in the example:

- `Push` is the *only* place that decides between `AddTail` and `OverwriteTail`; the base never evicts on its own.
- `Average()` enumerates through `this` — the inherited struct enumerator — and therefore participates in the fail-fast contract automatically.
- `Grow` guards `Resize`'s precondition (`newCapacity >= Count`) itself, because `Resize` will not.
- `TrimExcess()` and `Clear()` come from the base and already bump the version.

## Design guidance

- **Validate at the policy boundary, not in the primitives.** Put the `IsFull` / `IsEmpty` test in your public method and let the primitive assume it; that mirrors how `CircularBuffer<T>.Enqueue` and `Deque<T>.AddLast` are written.
- **Expose `Try*` alongside throwing members** when a full or empty ring is an expected state rather than a bug (the non-throwing drop-oldest method in the example above).
- **Do not cache `Capacity`** across a `Resize` or `TrimExcess`; read the property.
- **Thread safety is yours to add.** The base is single-threaded by design (`IsSynchronized == false`). For a lock-free multi-producer ring use <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> instead of deriving — see [Concurrent collections](concurrent-collections.md).
- **Prefer the built-in types when they fit.** `CircularBuffer<T>` with `AllowOverwrite = true` already *is* a sliding window over the most recent N items, and `Deque<T>` with `DequeOverflowPolicy.EvictOpposite` covers eviction from either end; derive only when the surface or the policy genuinely differs.

## Where to go next

- [Circular buffer](circular-buffer.md) — the single-ended FIFO built on this base.
- [Deque](deque.md) — the double-ended queue built on this base.
- [Concurrent collections](concurrent-collections.md) — the lock-free ring that does *not* share this base.
- [Thread-safety contracts across Core Foundations](thread-safety.md) — the one-table view of what is and is not safe.
- [`Bodu.Collections.Generic` API reference](xref:Bodu.Collections.Generic) — full namespace overview.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
