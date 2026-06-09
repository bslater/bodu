# Bodu.Core

Foundational building blocks for the Bodu solution and for general .NET 8 use: specialized generic collections, pooled buffers, a broad extension-method surface, the `WeekPattern` value type, and the `ThrowHelper` argument-validation catalogue that the rest of the solution validates against. Every collection ships a struct enumerator for allocation-free iteration and implements the standard BCL interfaces (`IEnumerable<T>`, `ICollection<T>`, `IReadOnlyCollection<T>`, `ISet<T>`, `IList<T>`) so the types drop into existing code.

## Installation

```shell
dotnet add package Bodu.Core
```

Targets `net8.0`.

## Collections

| Type | Namespace | Summary |
|---|---|---|
| `CircularBuffer<T>` | `Bodu.Collections.Generic` | Fixed-capacity FIFO ring buffer with optional overwrite-on-full |
| `ConcurrentCircularBuffer<T>` | `Bodu.Collections.Generic.Concurrent` | Thread-safe bounded FIFO buffer with optional overwrite |
| `Deque<T>` | `Bodu.Collections.Generic` | Double-ended queue over a circular array; O(1) at either end |
| `EvictingDictionary<TKey,TValue>` | `Bodu.Collections.Generic` | Fixed-capacity dictionary with FIFO / LRU / LFU eviction |
| `IndexedSet<T>` | `Bodu.Collections.Generic` | Insertion-ordered unique set with index-addressable `IList<T>` access |
| `OrderedSet<T>` | `Bodu.Collections.Generic` | Insertion-ordered unique set implementing `ISet<T>` |
| `IndexedPriorityQueue<TElement,TPriority>` | `Bodu.Collections.Generic` | Binary-heap priority queue with O(log n) re-prioritization and removal |
| `MultiValueDictionary<TKey,TValue>` | `Bodu.Collections.Generic` | Multiple values per key, exposed as `IReadOnlyList<TValue>` |
| `Multiset<T>` | `Bodu.Collections.Generic` | Unordered collection tracking element multiplicities |
| `RangeSet<T>` / `RangeDictionary<TKey,TValue>` | `Bodu.Collections.Generic` | Non-overlapping range containment / range-to-value mapping |
| `ConcurrentHashSet<T>` | `Bodu.Collections.Generic.Concurrent` | Thread-safe unordered set of unique elements |

## Buffers

- `PooledBufferBuilder<T>` (`Bodu.Buffers`) — accumulates into an `ArrayPool`-backed buffer with automatic growth and fast paths for single-segment output.

## `WeekPattern`

`WeekPattern` (root `Bodu` namespace) is an immutable bitmask over the seven days of the week, with composition operators, parsing, formatting, and a struct enumerator over the selected `DayOfWeek` values. It underpins weekend / working-day definitions across the solution.

## Extensions

The `Bodu.Extensions` and `Bodu.Collections.*.Extensions` namespaces add focused helpers over the BCL:

- **Strings** — `Slug`, `TitleCase`, `SentenceCase`, `ReplaceMany`, `RemovePunctuation`, `IsOneOf`, with `SlugOptions` / `WordCasingOptions`.
- **Dates** — `DateOnlyExtensions` / `DateTimeExtensions` for week, month, quarter, and fiscal-period math; `IWeekendDefinitionProvider` / `IQuarterDefinitionProvider` abstractions.
- **Numerics & spans** — `ReverseBits`, bit rotation, power-of-two helpers, and `Span<T>` / `ReadOnlySpan<T>` utilities.
- **Comparison** — `IComparableExtensions` (`Min`, `Max`, `Clamp`, `IsBetween`) with custom-comparer overloads.
- **Sequences** — `SequenceGenerator` (Fibonacci, Look-and-Say, Farey, Leibniz, Thue–Morse) and `RecursiveSelect`.
- **Randomness** — `IRandomGenerator` with `XorShiftRandom` and a `SystemRandomAdapter`.
- **XML** — `XmlNamespaceResolver` (`Bodu.Xml.Linq`) for namespace-qualified `XElement` / `XName` lookups.

## `ThrowHelper`

`ThrowHelper` (root `Bodu` namespace) is the solution-wide argument-validation catalogue — roughly 140 `ThrowIf…` guards, each driven by `CallerArgumentExpression` so the caller never passes a parameter name. Categories include null / empty, numeric comparisons (`ThrowIfLessThan`, `ThrowIfGreaterThan`, `ThrowIfZero`, `ThrowIfNotFinite`, …), range and index bounds, array / span length and offset validation, enum-value checks, type compatibility, and mathematical predicates (`ThrowIfNotPowerOfTwo`, `ThrowIfNotPositiveMultipleOf`). Public APIs across the solution validate through these helpers rather than hand-rolled checks.

## Testing

Tests live in `test/` as MSTest partial classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Core/test/Bodu.Core.Test.csproj --settings smoke.runsettings
dotnet test Bodu.Core/test/Bodu.Core.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Core/test/Bodu.Core.Test.csproj --settings regression.runsettings
```

Collection behaviour is validated through shared contract bases (`CollectionContractTests<>`, `ReadOnlyCollectionContractTests<>`, `SetContractTests<>`, `EnumeratorContractTests<>`, `DebugViewContractTests<>`, `NonGenericCollectionContractTests<>`) so every collection is held to the same interface contract.

## License

MIT. © Bodu Pty. Ltd.
