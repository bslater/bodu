---
uid: Bodu.Buffers
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

**Bodu.Buffers** ships the pooled buffer infrastructure used by the rest of the Bodu solution and exposed for consumer use. Reach for this namespace when you need to assemble a byte or character span without allocating an array per resize step.

## Key types

- <xref:Bodu.Buffers.PooledBufferBuilder`1> — `ArrayPool<T>`-backed builder for assembling spans without allocation. Returns the rented array to the pool on `Dispose`; supports `Append`, `Span`, and `ToArray` / `WrittenSpan` access.

## Example

```csharp
using Bodu.Buffers;

using var builder = new PooledBufferBuilder<byte>(initialCapacity: 256);
builder.Append(0xDE);
builder.Append(0xAD);
builder.Append(stackalloc byte[] { 0xBE, 0xEF });

ReadOnlySpan<byte> written = builder.WrittenSpan;
// builder rents from ArrayPool<byte>.Shared; Dispose returns the buffer.
```

## Notes

- **Disposable.** The builder rents from `ArrayPool<T>.Shared`. Dispose at the end of use; `using` is the idiomatic pattern.
- **Single owner.** The builder is not thread-safe — pool-backed buffers are owned by a single writer.
- **See also:** the [Bodu.Core introduction](~/docs/core/index.md), <xref:Bodu.Collections.Generic.SegmentedBuffer`1> for streaming buffers where the total length is not known up front.
