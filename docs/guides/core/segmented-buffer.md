---
title: Segmented buffer
---

# Segmented buffer

<xref:Bodu.Collections.Generic.SegmentedBuffer`1> is an append-only collection whose backing store grows in fixed-size **segments** instead of one contiguous array. When a `List<T>` fills up it allocates a new array of double the size and copies every existing element across; `SegmentedBuffer<T>` instead allocates one more segment and leaves the existing elements where they are. That avoids the array-doubling copy and the large-object-heap pressure that comes with very large contiguous buffers, while still offering O(1) indexed access.

Reach for it when you are accumulating a stream of unknown length — caching enumerator results, buffering streamed data, or logging events without knowing the upper bound — and the per-doubling copy of `List<T>` would be costly.

## Pattern 1 — append a stream of unknown length

```csharp
using Bodu.Collections.Generic;

var buffer = new SegmentedBuffer<int>();   // default segment size

foreach (int value in ReadFromNetwork())
    buffer.Add(value);

Console.WriteLine(buffer.Count);   // total elements appended
```

## Pattern 2 — choose a segment size

The segment size is the number of elements per chunk. Pick a larger size for fewer, bigger allocations or a smaller size to bound the per-segment footprint:

```csharp
// 512 elements per segment.
var buffer = new SegmentedBuffer<byte>(segmentSize: 512);
```

The constructor throws <xref:System.ArgumentOutOfRangeException> when `segmentSize` is less than 1.

## Pattern 3 — O(1) indexed access

Even though storage is segmented, element access is O(1): the buffer maps a flat index onto `(segment, offset)` arithmetically. The indexer supports both read and write of any already-populated position:

```csharp
var buffer = new SegmentedBuffer<string>(segmentSize: 4);
buffer.Add("a");
buffer.Add("b");

string first = buffer[0];   // "a"
buffer[1] = "B";            // overwrite in place
```

Indexing outside `[0, Count)` throws <xref:System.ArgumentOutOfRangeException> — the indexer never extends the buffer; use `Add` to append.

## Pattern 4 — enumerate in order

```csharp
var buffer = new SegmentedBuffer<int> { };
for (int i = 0; i < 1000; i++) buffer.Add(i);

foreach (int value in buffer)
    Process(value);   // yields elements in insertion order
```

## When *not* to use it

- If you know the final length up front, a pre-sized `List<T>` or array is simpler and equally fast.
- If you need removal, insertion at arbitrary positions, or set semantics, choose a different type — `SegmentedBuffer<T>` only appends and overwrites by index.
- For `ArrayPool<T>`-backed building of a single contiguous result (for example, to hand a `byte[]` to an API), prefer <xref:Bodu.Buffers.PooledBufferBuilder`1>.

## API summary

| Member | Description |
|---|---|
| `SegmentedBuffer()` | Creates a buffer with the default segment size. |
| `SegmentedBuffer(int segmentSize)` | Creates a buffer with the given elements-per-segment. |
| `Add(T)` | Appends an element, allocating a new segment when the current one fills. |
| `this[int]` | O(1) read / write of an element at an existing index. |
| `Count` | The number of elements appended. |
| `GetEnumerator()` | Enumerates elements in insertion order. |

## Where to go next

- [Choosing a collection](choosing-a-collection.md) — the full decision guide.
- [Pooled buffer builder](pooled-buffer-builder.md) — `ArrayPool<T>`-backed building of a single contiguous result.
- [Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic) — full namespace overview.
