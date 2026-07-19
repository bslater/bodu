# Bodu.Core.Samples.CoreToolbox

A guided tour of the general-purpose building blocks in `Bodu.Core`: the `SequenceGenerator`
catalogue, the pooled `PooledBufferBuilder<T>`, the LINQ-style enumerable operators, the string /
comparable / numeric extension surfaces, `WeekPattern`, and the `Bodu.Threading` async primitives.
Six scenarios, each over fixed inputs.

Everything runs offline with fixed inputs — deterministic output every run.

```bash
dotnet run --project samples/Core/Bodu.Core.Samples.CoreToolbox
```

## Scenario 1 — SequenceGenerators

**Intent.** Show the `SequenceGenerator` catalogue as a set of lazily evaluated, deterministic
number/string sequences, and — crucially — how to keep them *bounded* so enumeration terminates.

**What it does.** Materializes six sequences: an inclusive `Range`, a descending `Range` with an
explicit step, the Fibonacci numbers inside a value window, a fixed-length Thue-Morse prefix, the
first six look-and-say terms, and the order-5 Farey fractions.

**What to expect.** Each generator is bounded by either a value window (`Fibonacci[10,1000)` stops
before 1000) or an element count (`ThueMorse(16)`, `LookAndSay(6)`), so every line is finite and
identical run to run:

```text
Range(1, 10)      : 1 2 3 4 5 6 7 8 9 10
Range(20, 0, -5)  : 20 15 10 5 0
Fibonacci[10,1000): 13 21 34 55 89 144 233 377 610 987
ThueMorse(16)     : 0 1 1 0 1 0 0 1 1 0 0 1 0 1 1 0
LookAndSay(6)     : 1, 11, 21, 1211, 111221, 312211
Farey(5)          : 0/1 1/5 1/4 1/3 2/5 1/2 3/5 2/3 3/4 4/5 1/1
```

**APIs demonstrated.** `SequenceGenerator.Range` (two- and three-argument), `SequenceGenerator.Fibonacci`,
`SequenceGenerator.ThueMorse`, `SequenceGenerator.LookAndSay`, `SequenceGenerator.Farey`.

## Scenario 2 — PooledBuffers

**Intent.** Show `PooledBufferBuilder<T>` assembling a variable-length buffer from pooled storage —
the allocation-light alternative to a `List<T>` plus repeated `Array.Resize`.

**What it does.** Starts with a deliberately small capacity (4) so a later span append forces an
internal grow, appends single items, a span, and a repeated fill, reads the total back through the
zero-copy `WrittenSpan`, and finally snapshots the written region into a right-sized array while
returning the rented buffer to the pool in one call.

**What to expect.** Nine elements are written (`3 + 4 + 2`), the span sum is `1+2+…+7 = 28`, and the
snapshot is the exact written region including the two trailing zero fills:

```text
WrittenCount     : 9
IsEmpty          : False
WrittenSpan sum  : 28
Snapshot         : [1, 2, 3, 4, 5, 6, 7, 0, 0]
```

**APIs demonstrated.** `PooledBufferBuilder<T>` constructor, `Append`, `AppendRange(ReadOnlySpan<T>)`,
`AddMany`, `WrittenCount`, `IsEmpty`, `WrittenSpan`, `ToArrayAndDispose`.

## Scenario 3 — EnumerableOperators

**Intent.** Show the sequence-shaping combinators `IEnumerableExtensions` adds on top of LINQ —
grouping, sliding, pairing, folding, encoding, merging — that the BCL does not ship.

**What it does.** Runs seven operators over a fixed `1..7` source (plus a small character run and a
second sequence): `Batch`, `Windowed`, `Pairwise`, `Scan`, `RunLengthEncode`, `Interleave`, and
`ZipLongest`.

**What to expect.** `Batch(3)` leaves a short final group `[7]`; `Windowed(3)` slides one element at
a time; `Scan` emits every prefix-sum; `ZipLongest` pads the shorter side with `0` (the `int`
default):

```text
Batch(3)         : [1 2 3] [4 5 6] [7]
Windowed(3)      : [1 2 3] [2 3 4] [3 4 5] [4 5 6] [5 6 7]
Pairwise         : (1,2) (2,3) (3,4) (4,5) (5,6) (6,7)
Scan (prefix +)  : 1 3 6 10 15 21 28
RunLengthEncode  : ax3 bx2 cx1
Interleave       : 1 10 2 20 3 30
ZipLongest       : (1,100) (2,200) (3,0)
```

**APIs demonstrated.** `IEnumerableExtensions.Batch`, `.Windowed`, `.Pairwise`, `.Scan`,
`.RunLengthEncode`, `.Interleave`, `.ZipLongest`.

## Scenario 4 — StringTransforms

**Intent.** Show three extension surfaces at once — `StringExtensions` casing/slug/fold/truncate,
`ComparableExtensions` range helpers, and `NumericExtensions` number-theory helpers — so a reader
sees the breadth of the `Bodu.Extensions` namespace.

**What it does.** Converts one phrase into slug, kebab, snake, Pascal, and constant casing; folds
diacritics off an accented string; truncates a sentence; clamps and range-tests integers; and
computes primality, GCD, LCM, and a significant-figure round.

**What to expect.** The casing conventions differ only in separator and letter case; `Clamp` pins
`42` to `10` and `-3` to `0`; `RoundToSignificantDigits(3)` gives `3.14`:

```text
ToSlug           : hello-world-foo-bar
ToKebabCase      : hello-world-foo-bar
ToSnakeCase      : hello_world_foo_bar
ToPascalCase     : HelloWorldFooBar
ToConstantCase   : HELLO_WORLD_FOO_BAR
RemoveDiacritics : Creme brulee a la mode
Truncate(12)     : The quick br
Clamp 42 to 0..10: 10
Clamp -3 to 0..10: 0
5 IsBetween 1..10: True
15 IsBetween 1.10: False
17 IsPrime       : True
18 IsPrime       : False
GCD(48, 36)      : 12
LCM(4, 6)        : 12
RoundToSig(3)    : 3.14
```

**APIs demonstrated.** `StringExtensions.ToSlug` / `.ToKebabCase` / `.ToSnakeCase` / `.ToPascalCase` /
`.ToConstantCase` / `.RemoveDiacritics` / `.Truncate`; `ComparableExtensions.Clamp` / `.IsBetween`;
`NumericExtensions.IsPrime` / `.GreatestCommonDivisor` / `.LeastCommonMultiple` / `.RoundToSignificantDigits`.

## Scenario 5 — WeekPatterns

**Intent.** Show `WeekPattern` as a compact seven-bit day-of-week set: presets, the text formats it
round-trips through, the set-style bitwise operators, and driving a working-day query from a date
range.

**What it does.** Takes the `MondayToFriday` preset, prints it in the Sunday-first, Monday-first,
binary, and asterisk formats, parses its default rendering back to prove the round-trip, ORs in
Saturday and complements `Weekdays` to derive the weekend, then walks the first week of 2024 asking
each day whether it is selected.

**What to expect.** The preset has `Count=5`; the formats agree on which five days are set; the
round-trip is `True`; the complement of `Weekdays` is the two weekend days; and the date walk marks
Mon–Fri as `work` and Sat/Sun as `off`:

```text
MondayToFriday   : Count=5, S-format='_MTWTF_'
  ToString("M")   : MTWTF__
  ToString("B")   : 0111110
  ToString("A")   : *MTWTF*
  Parse round-trip: True
  | Saturday      : Count=6, '_MTWTFS'
  ~Weekdays       : Count=2, 'S_____S' (the weekend)
  Working days 2024-01-01 .. 2024-01-07:
    2024-01-01 Monday    -> work
    2024-01-02 Tuesday   -> work
    2024-01-03 Wednesday -> work
    2024-01-04 Thursday  -> work
    2024-01-05 Friday    -> work
    2024-01-06 Saturday  -> off
    2024-01-07 Sunday    -> off
```

**APIs demonstrated.** `WeekPattern.MondayToFriday` / `.Weekdays`, `WeekPattern.Parse`,
`WeekPattern.ToString(string)`, `WeekPattern.Count`, `WeekPattern.Contains`, the `|` and `~`
operators, and the `==` equality operator.

## Scenario 6 — AsyncPrimitives

**Intent.** Show three `Bodu.Threading` coordination primitives in one deterministic, single-threaded
flow: an at-most-once initializer, an awaitable latch, and an async mutex.

**What it does.** Wraps a counted factory in `AsyncLazy<T>` and awaits it twice; sets an
`AsyncManualResetEvent` and awaits the already-open gate; and guards five increments with `AsyncLock`
released by `using`.

**What to expect.** The lazy factory runs exactly once despite two awaits; the gate reports set and
its `WaitAsync` passes straight through; the guarded counter reaches 5:

```text
AsyncLazy value  : 42 (second await: 42)
  factory runs   : 1 (initialized once, then cached)
gate IsSet       : False
gate IsSet       : True (WaitAsync passed through)
guarded counter  : 5 (5 lock/increment/release cycles)
```

**APIs demonstrated.** `AsyncLazy<T>` (constructor + `GetAwaiter` via `await`),
`AsyncManualResetEvent.IsSet` / `.Set` / `.WaitAsync`, `AsyncLock.LockAsync` and the `Releaser`.

## Layout

```text
Bodu.Core.Samples.CoreToolbox/
  Program.cs                       # runs the scenarios in order
  Scenarios/SequenceGenerators.cs
  Scenarios/PooledBuffers.cs
  Scenarios/EnumerableOperators.cs
  Scenarios/StringTransforms.cs
  Scenarios/WeekPatterns.cs
  Scenarios/AsyncPrimitives.cs
```

## Related

- `Bodu.Core.Samples.FunctionalRailway` — the `Bodu.Functional` seam: `Option<T>`, `Result`/`Result<T>`,
  `Either<,>`, `Memoizer`, and the async companions.
- `Bodu.Core.Samples.TextEncoding` — BOM detection, transcoding with fallbacks, and pooled string encoding.
```
