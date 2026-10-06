# Bodu.Core

> **API stability - Stable.**

Foundational building blocks for .NET 8 and .NET 10: extension methods over strings, dates, numerics, spans; 
pooled buffer builders; argument validation helpers; a non-cryptographic RNG; a synchronous 
rate limiter; immutable calendar value sets (days of the week, months, days of the month, hours,
minutes, and seconds); and a Result<T> type for explicit error handling.

## Collections

**Note:** Specialized generic collections (`CircularBuffer<T>`, `Deque<T>`, etc.) have been 
moved to the **separate `Bodu.Collections` package**. The namespace structure is unchanged; 
consumers update the package reference only. `Bodu.Collections` depends on `Bodu.Core`.

(Remove the 12-item table entirely)

## Key Types

- `ThrowHelper` - argument validation helpers (null, range, enum, argument-expression capture)
- `RateGate` (Bodu.Threading) - synchronous rate limiter (leading-edge throttling)
- `DayOfWeekSet` - immutable set of days of the week with set operators and working-week presets; it
  replaces `WeekPattern`, removed in 1.3.0
- `MonthSet`, `DayOfMonthSet`, `HourSet`, `MinuteSet`, `SecondSet` - the same shape over the other
  calendar fields, written as a list of values and ranges (`1-3,12`)
- `Result<T>` (Bodu.Functional) - explicit success/failure type
- `XorShiftRandom` - high-performance xorshift128 PRNG (non-cryptographic)
- `SequenceGenerator` (Bodu.Sequences) - lazy sequence factories (Range, Repeat, Fibonacci, etc.)

## Extensions

| Target | Methods | Namespace |
|---|---|---|
| `Array<T>` | `PadLeft`, `PadRight` | `Bodu.Extensions` |
| `DateOnly` | Quarter queries, working-day checks | `Bodu.Extensions` |
| `DateTime` | Unix epoch conversions, formatting | `Bodu.Extensions` |
| `string` | Line splitting, case conversion | `Bodu.Extensions` |
| `span<T>` / `IEnumerable` | Collection and sequence helpers | `Bodu.Collections.Extensions` |

## Buffers

- `PooledBufferBuilder<T>` (Bodu.Buffers) - ArrayPool-backed accumulator with auto-growth

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
