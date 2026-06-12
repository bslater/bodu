---
title: Pooled buffer builder
---

# Pooled buffer builder

`PooledBufferBuilder<T>` is the zero-allocation way to assemble a `byte` or `char` span (or any other element type) without paying for repeated array resizing or for `List<T>`'s amortised-double growth. It rents arrays from `ArrayPool<T>.Shared`, accumulates elements with automatic growth, and returns the rented array to the pool on disposal.

The type implements `IBufferWriter<T>` and `IMemoryOwner<T>`, so it composes with span-based APIs that already speak those interfaces — `Utf8Json` writers, `System.Text.Json.Utf8JsonWriter`, `MessagePackWriter`, the BCL `JsonWriter` API, and consumer code that targets `IBufferWriter<T>` directly.

## Construction

```csharp
using Bodu.Buffers;

using var builder = new PooledBufferBuilder<byte>();                     // default 256-element initial capacity
using var sized   = new PooledBufferBuilder<byte>(initialCapacity: 4096); // pre-sized
```

The initial capacity is a hint — `ArrayPool<T>.Shared` rounds up to a pool-bucket size, so `Capacity` may exceed the value you passed. The constructor rejects non-positive capacities.

Always use the `using` pattern. `Dispose()` returns the rented array to the pool; failing to dispose leaks the rental and (worse) prevents the pool from reclaiming the buffer, which can lead to unbounded pool growth in long-running services.

## Appending elements

```csharp
builder.Append((byte)0xDE);                                  // single element
builder.AppendRange(stackalloc byte[] { 0xAD, 0xBE, 0xEF }); // span
builder.AppendRange(new byte[] { 0xCA, 0xFE });              // array
builder.AppendRange(ReadOnlyMemory<byte>.Empty);             // memory
builder.AppendRange(someEnumerable);                          // fast-path if ICollection<T>
builder.AddMany((byte)0x00, count: 16);                      // fill region with repeated value
```

`AppendRange(IEnumerable<T>)` checks for `ICollection<T>` and takes the fast `CopyTo` path when available; otherwise it iterates and appends element-by-element. `AddMany` is the right primitive for filling a region with a known fill byte (zero-padding before encryption, padding before a hash final block).

## Direct span access

For consumers that want to write into the buffer directly — and avoid the second copy that `AppendRange` would impose — the `IBufferWriter<T>` surface is available:

```csharp
Span<byte> destination = builder.GetSpan(sizeHint: 64);     // grows if needed
int actuallyWritten = WriteIntoSpan(destination);
builder.Advance(actuallyWritten);                            // bump the written count
```

`GetMemory` returns a `Memory<T>`, `GetSpan` a `Span<T>`. Both grow the rented array if the requested hint exceeds available capacity. `Advance(count)` validates `count ≤ free capacity` and throws on overshoot.

This is the right pattern when composing with `Utf8JsonWriter` or any other writer that accepts an `IBufferWriter<T>`:

```csharp
using var builder = new PooledBufferBuilder<byte>();
using (Utf8JsonWriter w = new(builder))
{
    w.WriteStartObject();
    w.WriteString("name", "value");
    w.WriteEndObject();
}

ReadOnlySpan<byte> json = builder.WrittenSpan;
```

## Reading the result

```csharp
builder.WrittenCount;             // current accumulated count
builder.WrittenSpan;              // ReadOnlySpan<T> over [0, WrittenCount)
builder.WrittenMemory;            // ReadOnlyMemory<T> over [0, WrittenCount)
builder.Capacity;                 // rented array length
builder.FreeCapacity;             // Capacity − WrittenCount
builder.IsEmpty;                  // WrittenCount == 0

// Copy to caller buffer.
byte[] hash = new byte[builder.WrittenCount];
builder.CopyTo(hash);
if (builder.TryCopyTo(otherSpan)) { … }

// Convert to a stable array and release the rental in one call.
byte[] result = builder.ToArrayAndDispose();
```

`ToArrayAndDispose()` is the right primitive for the common "build, snapshot, free" pattern — it copies the written region into a fresh array and disposes the builder in one method, eliminating a second `using` block.

## Sorting in place

```csharp
builder.Sort();                                              // default comparer
builder.Sort(Comparer<byte>.Default);                        // explicit comparer
builder.Sort((a, b) => b.CompareTo(a));                      // delegate
```

Sort operates over the written region in place.

## Disposal and Reset

```csharp
builder.Reset();                                              // clear the accumulated data; keep the rented buffer
builder.Dispose();                                             // return the rented buffer to the pool
```

`Reset()` zeroes the reference slots (when `T` contains references) so the old contents do not pin objects; the rented array is **not** returned, so a subsequent `AppendRange` can fill it without a fresh rental. Use `Reset` when the builder is the body of a per-request loop and the same instance is reused across requests.

`Dispose()` releases the rental. After disposal, every property and method throws `ObjectDisposedException`. Builders are not reusable after disposal — construct a new instance instead.

## Pre-sizing

```csharp
builder.EnsureCapacity(64 * 1024);   // pre-allocate ahead of a known-size payload
```

When the final size is known up front, `EnsureCapacity` avoids the amortised growth steps. The pool may still round up to a larger bucket.

## Dangerous direct access

```csharp
ArraySegment<byte> alias = builder.DangerousGetArray();
ReadOnlySpan<byte> aliasSpan = alias.AsSpan();
```

`DangerousGetArray` exposes the underlying rented array as an `ArraySegment<T>`. The returned segment must not be retained beyond the builder's lifetime — once `Dispose` runs, the array is returned to the pool and may be handed to another tenant, who can mutate it under you.

The pattern is useful when handing the buffer to a low-level API that already accepts `ArraySegment<T>` and that finishes its work synchronously inside the `using` block. Outside that pattern, prefer `WrittenSpan` / `WrittenMemory`.

## Worked example — pooled CSV encoding

```csharp
using Bodu.Buffers;

byte[] EncodeRecord(IEnumerable<string> fields)
{
    using var builder = new PooledBufferBuilder<byte>(initialCapacity: 256);

    bool first = true;
    foreach (string field in fields)
    {
        if (!first) builder.Append((byte)',');
        first = false;

        ReadOnlySpan<byte> utf8 = Encoding.UTF8.GetBytes(field);
        builder.AppendRange(utf8);
    }

    builder.Append((byte)'\r');
    builder.Append((byte)'\n');
    return builder.ToArrayAndDispose();
}
```

The function allocates exactly one array — the final result. Every intermediate step uses the pool.

## When *not* to use `PooledBufferBuilder`

- **One-shot allocations of known size.** Just `new byte[n]` is simpler and clearer; the pool overhead is not worth it for a single call.
- **Long-lived buffers that escape the calling scope.** The pool is a per-process resource — holding a rental open for the lifetime of the process defeats the point. Reach for `ToArrayAndDispose()` to snapshot and release.
- **`Span<T>`-only callers with a known upper bound.** If the upper bound fits on the stack, `stackalloc` is faster and lifetime-clean.
- **Multi-threaded writes.** The builder is not thread-safe. One writer per builder; coordinate at a higher level.

## See also

- [`PooledBufferBuilder<T>` API reference](xref:Bodu.Buffers.PooledBufferBuilder`1)
- [`Bodu.Buffers` namespace landing](xref:Bodu.Buffers)
- [`Bodu.Collections.Generic.SegmentedBuffer<T>`](xref:Bodu.Collections.Generic.SegmentedBuffer`1) — for streaming scenarios where the total length is unknown and the result is consumed in segments.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
