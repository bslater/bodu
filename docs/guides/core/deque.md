---
title: Deque
---

# Deque

`Deque<T>` is a double-ended queue (deque) backed by a contiguous circular array. Elements may be added or removed from either end in amortised O(1) time. The `AllowGrow` property controls whether the backing array expands automatically when full or whether the deque rejects further inserts — letting one type cover both growable and fixed-capacity scenarios.

For a single-ended FIFO buffer with eviction-on-full semantics, see [Circular buffer](circular-buffer.md). For thread-safe concurrent FIFO access, use `ConcurrentCircularBuffer<T>` (a separate, lock-free implementation).

![Deque backing array with Head, Tail, wrap-around, and AllowGrow behaviour](../../images/diagrams/deque.svg)

`Head` and `Tail` index into a contiguous backing array but advance modulo `Capacity`, so the logical sequence may wrap past the end of the array. `AddFirst` / `RemoveFirst` operate at the head end; `AddLast` / `RemoveLast` operate at the tail end. The indexer `this[i]` reads in logical order, hiding the wrap from callers.

## Pattern 1 — growable double-ended queue (default)

`new Deque<T>()` and `new Deque<T>(capacity)` both produce a growable deque. The capacity argument is a hint — the backing array doubles automatically when filled.

```csharp
using Bodu.Collections.Generic;

var deque = new Deque<int>();
deque.AddLast(2);
deque.AddFirst(1);
deque.AddLast(3);          // contents: 1, 2, 3

int first = deque.RemoveFirst();   // 1
int last  = deque.RemoveLast();    // 3
```

## Pattern 2 — fixed-capacity deque that throws when full

Pass `allowGrow: false` to switch off automatic growth. Adds to a full deque throw `InvalidOperationException`; the `Try*` overloads return `false` without modifying state:

```csharp
using Bodu.Collections.Generic;

var bounded = new Deque<int>(capacity: 8, allowGrow: false);

for (int i = 0; i < 8; i++)
    bounded.AddLast(i);

bool added = bounded.TryAddLast(8);   // false — bounded is full
bool full  = bounded.IsFull;          // true

// Throwing variant:
// bounded.AddLast(8);                // InvalidOperationException
```

## Pattern 3 — sliding window from both ends

Use `AddFirst` / `AddLast` together with `RemoveFirst` / `RemoveLast` to build a sliding window or undo/redo buffer:

```csharp
using Bodu.Collections.Generic;

var recent = new Deque<string>(capacity: 5, allowGrow: false);

void Record(string action)
{
    if (recent.IsFull)
        recent.RemoveFirst();   // drop the oldest
    recent.AddLast(action);
}

Record("open");
Record("edit");
Record("save");

string mostRecent = recent.PeekLast();   // "save"
```

## Pattern 4 — peek without removing

```csharp
using Bodu.Collections.Generic;

var deque = new Deque<int>();
deque.AddLast(10);
deque.AddLast(20);

if (deque.TryPeekFirst(out int head)) Console.WriteLine(head);   // 10
if (deque.TryPeekLast(out int tail))  Console.WriteLine(tail);   // 20

// Throwing variants:
int h = deque.PeekFirst();   // 10
int t = deque.PeekLast();    // 20
```

## Pattern 5 — toggling between growable and fixed at runtime

`AllowGrow` is a settable property. Toggling from `true` to `false` does not shrink the existing capacity — call `TrimExcess` afterwards if a smaller footprint is wanted.

```csharp
using Bodu.Collections.Generic;

var deque = new Deque<int>(capacity: 4);   // growable

for (int i = 0; i < 100; i++)
    deque.AddLast(i);                       // backing array grows past 4

// Lock the deque to its current size so subsequent adds are rejected:
deque.AllowGrow = false;
bool added = deque.TryAddLast(101);          // false — at capacity

// Switch back to growable later if needed:
deque.AllowGrow = true;
deque.AddLast(101);                          // OK; grows again
```

## Pattern 6 — pre-grow with EnsureCapacity

`EnsureCapacity` works regardless of `AllowGrow` — it is the explicit pre-grow hatch even on fixed-capacity deques. Use it to reserve space ahead of a known burst of inserts:

```csharp
using Bodu.Collections.Generic;

var deque = new Deque<int>(capacity: 4, allowGrow: false);
deque.EnsureCapacity(10_000);   // pre-allocate; AllowGrow stays false

for (int i = 0; i < 10_000; i++)
    deque.AddLast(i);            // no throws — capacity already covers it
```

## API summary

| Member | Description |
|---|---|
| `AddFirst(T)` | Adds an element at the head. Throws if `AllowGrow` is `false` and the deque is full. |
| `AddLast(T)` | Adds an element at the tail. Throws if `AllowGrow` is `false` and the deque is full. |
| `TryAddFirst(T)` / `TryAddLast(T)` | Non-throwing variants. Always return `true` when `AllowGrow` is `true`; return `false` on full when `AllowGrow` is `false`. |
| `RemoveFirst()` / `RemoveLast()` | Removes and returns the head or tail element. Throws if empty. |
| `TryRemoveFirst(out T)` / `TryRemoveLast(out T)` | Non-throwing remove variants; return `false` if empty. |
| `PeekFirst()` / `PeekLast()` | Reads the head or tail element without removing it. Throws if empty. |
| `TryPeekFirst(out T)` / `TryPeekLast(out T)` | Non-throwing peek variants; return `false` if empty. |
| `Count` | The number of elements currently in the deque. |
| `Capacity` | The current backing-array length. Mutable in growable mode. |
| `IsEmpty` | `true` when `Count == 0`. |
| `IsFull` | `true` when `Count == Capacity`. |
| `AllowGrow` | Whether the backing array expands automatically when full. Settable at runtime. |
| `EnsureCapacity(int)` | Expands the backing array to hold at least the requested capacity. Ignores `AllowGrow`. |
| `Clear()` | Removes all elements; capacity unchanged. |
| `TrimExcess()` | Shrinks the backing array to `Count` (or 1 when empty). |
| `ToArray()` | Returns a snapshot in head-to-tail logical order. |
| `this[int]` | Read-only indexer in head-to-tail logical order. |

## Where to go next

- [Circular buffer](circular-buffer.md) — fixed-capacity FIFO with eviction-on-full semantics.
- [Evicting dictionary](evicting-dictionary.md) — fixed-capacity key-value cache with LRU / LFU / FIFO eviction.
- [Bodu.Core overview](index.md) — all key types at a glance.
- [Bodu.Collections.Generic API reference](../../apidoc/Bodu.Collections.Generic.md) — full namespace overview.
