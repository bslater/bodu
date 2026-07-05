---
title: Bit set
---

# Bit set

`BitSet` is a growable set of non-negative integers stored as a packed bit array — the .NET analogue of Java's `java.util.BitSet`. It packs 64 bits per `ulong` word, grows automatically when a bit beyond the current storage is set or flipped, and answers reads beyond the storage with `false` instead of throwing.

It fits dense integer-keyed membership problems: sieve algorithms, free-slot and allocation maps, column/row selection masks, visited-node tracking over integer-indexed graphs, and compact flag tables. When the members are sparse over a huge universe or are not integers, reach for `HashSet<int>` or [`RangeSet<T>`](range-dictionary.md) instead.

The BCL's `System.Collections.BitArray` covers a different, narrower contract: it has a fixed, explicit length (reads and writes out of range throw), no query surface beyond the indexer, and enumeration yields boxed `bool` values for every position. `BitSet` grows on demand, exposes the set-bit query surface (`NextSetBit`, `NextClearBit`, `Cardinality`, `Intersects`), and enumerates the *indices of the set bits* in ascending order through a non-boxing struct enumerator.

A few contract points worth keeping in mind:

- `Length` is the *logical* length — one greater than the highest set bit (Java's `length()`), 0 when empty. `Capacity` is the *allocated* bit count (always a multiple of 64). Clearing bits shrinks `Length` but never `Capacity`.
- `Get` accepts any non-negative index and returns `false` beyond the capacity; `Clear(index)` beyond the capacity is a no-op; `Set` and `Flip` grow the storage instead.
- Range overloads (`Set(from, to)`, `Clear(from, to)`, `Flip(from, to)`) operate on half-open `[from, to)` ranges; an empty range is a no-op.
- Equality is a value comparison over the logical content — trailing zero words are ignored, so a set that grew and was cleared again equals a fresh empty set.
- `ToString()` returns the summary form `BitSet(Cardinality = n, Length = m)`; enumerate the set to obtain the individual indices.

## Pattern 1 — set, query, enumerate

```csharp
using Bodu.Collections.Generic;

var sieve = new BitSet();
sieve.Set(2);
sieve.Set(3);
sieve.Set(5);
sieve.Set(100_000);                    // grows automatically

bool isSet = sieve.Get(100_000);       // true
bool beyond = sieve.Get(int.MaxValue); // false — never throws for reads

Console.WriteLine(sieve.Cardinality);  // 4
Console.WriteLine(sieve.Length);       // 100001 — highest set bit + 1

foreach (int index in sieve)           // non-boxing struct enumerator
    Console.WriteLine(index);          // 2, 3, 5, 100000 — ascending
```

## Pattern 2 — range operations and bit walks

`NextSetBit` / `NextClearBit` support the classic Java iteration idioms — finding the next allocated slot, the next free slot, or walking set bits from an arbitrary offset:

```csharp
using Bodu.Collections.Generic;

var slots = new BitSet(256);
slots.Set(0, 64);                      // occupy the first word [0, 64)
slots.Clear(10, 12);                   // free two slots in the middle
slots.Flip(62, 66);                    // toggle across the word boundary

int firstFree = slots.NextClearBit(0);   // 10 — never -1; a clear bit always exists
int nextUsed = slots.NextSetBit(10);     // 12

for (int i = slots.NextSetBit(0); i >= 0; i = slots.NextSetBit(i + 1))
{
    // visit every set bit in ascending order
}
```

## Pattern 3 — in-place logical operations

`And`, `Or`, `Xor`, and `AndNot` mutate the receiver in place with Java semantics: `Or`/`Xor` grow the receiver to cover the other operand's logical length, while `And`/`AndNot` never grow (the result cannot exceed the receiver's content). Use the copy constructor when the operand must be preserved:

```csharp
using Bodu.Collections.Generic;

var wantsEmail = new BitSet();
wantsEmail.Set(0, 100);

var unsubscribed = new BitSet();
unsubscribed.Set(17);
unsubscribed.Set(42);

var recipients = new BitSet(wantsEmail);   // copy — wantsEmail is untouched
recipients.AndNot(unsubscribed);           // 0..99 except 17 and 42

bool overlap = wantsEmail.Intersects(unsubscribed);   // true — non-mutating probe
```

> [!NOTE]
> `BitSet` is not thread-safe. Concurrent reads and writes require external synchronization, and any mutation invalidates active enumerators.

## Where to go next

- <xref:Bodu.Collections.Generic.BitSet> — the full API surface.
- [Range-keyed lookups](range-dictionary.md) — interval membership when the set is a few contiguous runs rather than dense bits.
- [Choosing a collection](choosing-a-collection.md) — the full decision guide across the namespace.
- [Core documentation](../../docs/core/index.md) — concepts and getting started for the collections packages.
