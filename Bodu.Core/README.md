# Bodu.Core

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

Foundational building blocks for the Bodu solution and for general .NET 8 use: specialized generic collections, pooled buffers, a broad extension-method surface, text-encoding utilities, the `WeekPattern` value type, and the `ThrowHelper` argument-validation catalogue that the rest of the solution validates against. Every collection ships a struct enumerator for allocation-free iteration and implements the standard BCL interfaces (`IEnumerable<T>`, `ICollection<T>`, `IReadOnlyCollection<T>`, `ISet<T>`, `IList<T>`) so the types drop into existing code.

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
| `SequencedDictionary<TKey,TValue>` | `Bodu.Collections.Generic` | Insertion- or access-ordered dictionary with O(1) first/last access and removal |
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

## Sequence generation

`SequenceGenerator` (`Bodu.Sequences`) provides lazily evaluated `IEnumerable<T>` factories that mirror the conventions of `System.Linq.Enumerable`: general-purpose shapes (`Range`, `Repeat`, `NextWhile`, `Factory`) alongside well-known mathematical series (Fibonacci, Farey, Leibniz, look-and-say, Thue–Morse).

## Extensions

The `Bodu.Extensions` and `Bodu.Collections.*.Extensions` namespaces add focused helpers over the BCL:

- **Strings** — `Slug`, `TitleCase`, `SentenceCase`, `ReplaceMany`, `RemovePunctuation`, `IsOneOf`, with `SlugOptions` / `WordCasingOptions`.
- **Dates** — `DateOnlyExtensions` / `DateTimeExtensions` for week, month, quarter, and fiscal-period math; `IWeekendDefinitionProvider` / `IQuarterDefinitionProvider` abstractions.
- **Numerics & spans** — `ReverseBits`, bit rotation, power-of-two helpers, and `Span<T>` / `ReadOnlySpan<T>` utilities.
- **Comparison** — `ComparableExtensions` (`Min`, `Max`, `Clamp`, `IsBetween`) with custom-comparer overloads.
- **Sequences** — `RecursiveSelect` for pre-order descent over tree-shaped sequences.
- **Randomness** — `IRandomGenerator` with `XorShiftRandom` and a `SystemRandomAdapter`.
- **XML** — `XmlNamespaceResolver` (`Bodu.Xml.Linq`) for namespace-qualified `XElement` / `XName` lookups.

## Text encoding

The `Bodu.Text` namespace covers byte-order-mark detection and a span- and UTF-8-friendly extension surface over `System.Text.Encoding`, `ReadOnlySpan<char>`, `ReadOnlySpan<byte>`, and `string` — filling the gaps the BCL leaves around preamble handling, allocation-free transcoding, exact-length `GetBytes`/`GetChars`, and encoding classification.

- `EncodingDetection.TryDetectByPreamble(ReadOnlySpan<byte>, out Encoding?)` identifies UTF-8, UTF-16 (LE/BE), and UTF-32 (LE/BE) from a leading byte-order mark.
- `EncodingExtensions` (on `Encoding` and spans) and `StringEncodingExtensions` (on `string`) group their helpers by concern:

| Concern | Representative members |
|---|---|
| UTF-8 fast paths | `ToUtf8Bytes`, `GetUtf8ByteCount`, `EncodeUtf8To`, `TryEncodeUtf8To`, `FromUtf8`, `DecodeUtf8To` |
| Preamble handling | `HasPreamble`, `GetPreambleLength`, `TryWritePreamble`, `StripPreamble`, `GetBytesWithPreamble`, `GetStringSkippingPreamble` |
| Transcoding | `Transcode`, `TranscodeTo`, `TryTranscodeTo` |
| Exact / try conversions | `GetBytesExactly`, `GetCharsExactly`, `TryGetBytes`, `TryGetChars` |
| Classification | `IsUtf8`, `IsUtf16LittleEndian`, `IsUtf32BigEndian`, `IsAnyUtf`, `IsAscii`, `GetDisplayName` |
| Fallback control | `WithExceptionFallbacks`, `WithReplacementFallbacks`, `UsesExceptionFallbacks` |
| Buffer writers | `WriteBytes`, `WriteChars`, `WritePreamble`, `WriteBytesWithPreamble` (on `IBufferWriter<>`) |

> Base-N binary encodings (Base16/32/58/64/85, …) live in the sibling `Bodu.Text.Encoding` package; document formats (CSV, INI, .env, Bencode, TOML) live in `Bodu.Text.Formats`.

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
