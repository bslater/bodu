---
title: Segmented buffer
---

# Segmented buffer

<xref:Bodu.Collections.Generic.SegmentedBuffer`1> is an append-only collection whose backing store grows in fixed-size **segments** instead of one contiguous array. When a `List<T>` fills up it allocates a new array of double the size and copies every existing element across; `SegmentedBuffer<T>` instead allocates one more segment and leaves the existing elements where they are. That avoids the array-doubling copy and the large-object-heap pressure that comes with very large contiguous buffers, while still offering O(1) indexed access.

Reach for it when you are accumulating a stream of unknown length — caching enumerator results, buffering streamed data, or logging events without knowing the upper bound — and the per-doubling copy of `List<T>` would be costly.

## Pattern 1 — append a stream of unknown length

```csharp
using Bodu.Collections.Generic;

var buffer = new SegmentedBuffer<int>();   // default segment size (512 elements)

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

string first = buffer[0];   // → "a"
buffer[1] = "B";            // overwrite in place
```

Indexing outside `[0, Count)` throws <xref:System.ArgumentOutOfRangeException> — the indexer never extends the buffer; use `Add` to append.

## Pattern 4 — enumerate in order

```csharp
var buffer = new SegmentedBuffer<int>();
for (int i = 0; i < 1000; i++) buffer.Add(i);

foreach (int value in buffer)
    Process(value);   // yields elements in insertion order
```

The enumerator is fail-fast: modifying the buffer after enumeration begins — by `Add` or by assignment through the indexer — throws `InvalidOperationException` on the next iteration step.

## Worked example — buffer a stream once, read it many times

A common shape: data arrives in chunks of unpredictable size, and the consumer needs both random access and a second full pass — without re-reading the source. Buffering into a `SegmentedBuffer<T>` pays no resize-copy as the total grows:

```csharp
using Bodu.Collections.Generic;

// Chunks arriving from a reader; total length unknown up front.
int[][] chunks =
{
    new[] { 3, 1, 4 },
    new[] { 1, 5, 9, 2 },
    new[] { 6, 5, 3 },
};

var samples = new SegmentedBuffer<int>(segmentSize: 4);

foreach (int[] chunk in chunks)
    foreach (int value in chunk)
        samples.Add(value);

// Storage is now three segments: [3,1,4,1] [5,9,2,6] [5,3,_,_]
Console.WriteLine(samples.Count);    // → 10

// Random access without a copy.
Console.WriteLine(samples[0]);       // → 3
Console.WriteLine(samples[4]);       // → 5  — first element of the second segment
Console.WriteLine(samples[9]);       // → 3

// Second full pass over the buffered data.
int sum = 0;
foreach (int value in samples) sum += value;
Console.WriteLine(sum);              // → 39
```

Ten appends into a `segmentSize: 4` buffer triggered exactly three segment allocations and zero element copies; the same sequence in an unsized `List<int>` would have re-copied the contents at each capacity doubling.

## Complexity

| Operation | Cost | Notes |
|---|---|---|
| `Add(T)` | Amortized O(1) | Allocates one new segment every `segmentSize` appends; never copies existing elements. |
| `this[int]` get / set | O(1) | One division and one modulo map the flat index to `(segment, offset)`. |
| `Count` | O(1) | Maintained incrementally. |
| Enumeration | O(n) | Walks segments sequentially, in insertion order. |

## How it compares

**Versus `List<T>`.** A `List<T>` keeps one contiguous array, so growth past capacity costs an O(n) copy, and element arrays beyond ~85 KB land on the large object heap. `SegmentedBuffer<T>` allocates fixed-size segments and copies nothing on growth. `List<T>` remains the better choice when the final length is known up front (a pre-sized list never re-copies either), or when you need search, removal, insertion, sorting, or a contiguous `Span<T>` view — `SegmentedBuffer<T>` deliberately offers none of those.

**Versus <xref:Bodu.Buffers.PooledBufferBuilder`1>.** The [pooled buffer builder](pooled-buffer-builder.md) rents its storage from `ArrayPool<T>.Shared`, implements `IBufferWriter<T>`, and ends with a single contiguous result (`WrittenSpan`, `ToArrayAndDispose()`) — the right tool when a downstream API needs a `ReadOnlySpan<T>` or a `byte[]`, at the cost of mandatory disposal. `SegmentedBuffer<T>` never materializes a contiguous view and needs no disposal; prefer it when consumers only index into and enumerate the buffered data.

**Versus `MemoryStream`.** For bytes that downstream code consumes as a `Stream`, `MemoryStream` is the natural fit. It is, however, a single doubling array underneath, with the same copy and large-object-heap characteristics as `List<byte>` — buffering large byte streams that are only indexed or enumerated is cheaper in a `SegmentedBuffer<byte>`.

## When *not* to use it

- If you know the final length up front, a pre-sized `List<T>` or array is simpler and equally fast.
- If you need removal, insertion at arbitrary positions, search, or set semantics, choose a different type — `SegmentedBuffer<T>` only appends and overwrites by index.
- For `ArrayPool<T>`-backed building of a single contiguous result (for example, to hand a `byte[]` to an API), prefer <xref:Bodu.Buffers.PooledBufferBuilder`1>.
- For concurrent producers, add external synchronization — the type is not thread-safe, and the fail-fast enumerator throws if elements are added while it is iterating.

## API summary

| Member | Description |
|---|---|
| `SegmentedBuffer()` | Creates a buffer with the default segment size (512). |
| `SegmentedBuffer(int segmentSize)` | Creates a buffer with the given elements-per-segment. |
| `Add(T)` | Appends an element, allocating a new segment when the current one fills. |
| `this[int]` | O(1) read / write of an element at an existing index. |
| `Count` | The number of elements appended. |
| `GetEnumerator()` | Enumerates elements in insertion order. Fail-fast: throws `InvalidOperationException` if the buffer is modified during enumeration. |

## See also

- [Pooled buffer builder](pooled-buffer-builder.md) — `ArrayPool<T>`-backed building of a single contiguous result.
- [Choosing a collection](choosing-a-collection.md) — the full decision guide.
- [Core Foundations guides](../topics/core-foundations.md) — every guide in this topic.
- [Core Foundations topic overview](../../docs/topics/core-foundations.md) — package map and install command.
- [Bodu.Core introduction](../../docs/core/index.md) — namespaces, headline types, scenarios.
- [`SegmentedBuffer<T>` API reference](xref:Bodu.Collections.Generic.SegmentedBuffer`1)
- [`PooledBufferBuilder<T>` API reference](xref:Bodu.Buffers.PooledBufferBuilder`1)
- [`Bodu.Collections.Generic` namespace landing](xref:Bodu.Collections.Generic)
