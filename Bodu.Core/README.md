# Bodu.Core

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

Foundational building blocks for .NET 8 and .NET 10: extension methods over strings, dates, numerics, spans; 
pooled buffer builders; argument validation helpers; a non-cryptographic RNG; a synchronous 
rate limiter; immutable calendar value sets (days of the week, months, days of the month, hours,
minutes, and seconds); and a Result<T> type for explicit error handling.

## Collections

**Note:** Specialized generic collections (`CircularBuffer<T>`, `Deque<T>`, etc.) have been 
moved to the **separate `Bodu.Collections` package**. The namespace structure is unchanged; 
consumers update the package reference only. `Bodu.Collections` depends on `Bodu.Core`.

## Key Types

- `ThrowHelper` - argument validation helpers (null, range, enum, argument-expression capture)
- `RateGate` (Bodu.Threading) - synchronous rate limiter (leading-edge throttling)
- `DayOfWeekSet` - immutable set of days of the week with set operators and working-week presets; it
  replaces `WeekPattern`, removed in 1.3.0
- `MonthSet`, `DayOfMonthSet`, `HourSet`, `MinuteSet`, `SecondSet` - the same shape over the other
  calendar fields, written as a list of values and ranges (`1-3,12`), every value (`L`), binary text
  (`B`), or, for months, a mask of initials (`J`, `JFM________D`)
- `ICalendarValueSet<TSelf, TValue>` - the interface the six calendar value sets share, for code that
  works with any of them; every set also reads and writes its text over spans of characters and UTF-8
  bytes (`ISpanParsable`, `IUtf8SpanParsable`, `ISpanFormattable`, `IUtf8SpanFormattable`)
- `Result<T>` (Bodu.Functional) - explicit success/failure type
- `XorShiftRandom` - high-performance xorshift128 PRNG (non-cryptographic)
- `SequenceGenerator` (Bodu.Sequences) - lazy sequence factories (Range, NextWhile, Factory, Fibonacci, etc.)

## Extensions

| Target | Methods | Namespace |
|---|---|---|
| `Array<T>` | `PadLeft`, `PadRight` | `Bodu.Extensions` |
| `DateOnly` | Quarter queries, working-day checks | `Bodu.Extensions` |
| `DateTime` | Unix epoch conversions, formatting | `Bodu.Extensions` |
| `string` | Line splitting, case conversion | `Bodu.Extensions` |
| `IEnumerable<T>`, `IList<T>`, `IDictionary<TKey,TValue>` | Sequence operators: batching, caching, recursive selection, shuffling | `Bodu.Collections.Generic.Extensions` |
| `IEnumerable` (non-generic) | `CountOrDefault`, `RecursiveSelect` | `Bodu.Collections.Extensions` |
| `Span<T>`, `ReadOnlySpan<T>` | `AsReadOnly`, `ToReversed` | `Bodu.Extensions` |

## Buffers

- `PooledBufferBuilder<T>` (Bodu.Buffers) - ArrayPool-backed accumulator with auto-growth

## Building and testing

To build this package from source and run its tests, see [CONTRIBUTING.md](https://github.com/bslater/bodu/blob/master/CONTRIBUTING.md).

## License

MIT. © Bodu Pty. Ltd.
