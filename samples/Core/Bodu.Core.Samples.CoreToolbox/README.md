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
--- SequenceGenerator - bounded, deterministic sequences ---
  What   : Produces arithmetic ranges in both directions, then four classic integer sequences - Fibonacci windowed
           by value, Thue-Morse, look-and-say, and the Farey fractions of order 5.
  Why    : Every one of these is lazy and bounded at the source rather than by the caller. That matters for the
           unbounded ones: Fibonacci is filtered by value range, not by taking a count and hoping, so nothing
           computes a term past the bound. Generating a sequence and then trimming it is the version that either
           overshoots or runs forever, and it is what these exist to avoid.
  Expect : Range counts down as readily as up when given a negative step. The Fibonacci window starts at 13 - the
           first term at or above 10 - and stops below 1000. Farey(5) lists every fraction in lowest terms between
           0/1 and 1/1 with denominator at most 5, in ascending order.

  Range(1, 10)      : 1 2 3 4 5 6 7 8 9 10  (ascending, inclusive of both ends)
  Range(20, 0, -5)  : 20 15 10 5 0  (a negative step counts down - no separate Reverse and no manual loop)
  Fibonacci[10,1000): 13 21 34 55 89 144 233 377 610 987  (starts at 13, the first term >= 10, and stops below 1000; bounded by VALUE, so no term past the limit is ever computed)
  ThueMorse(16)     : 0 1 1 0 1 0 0 1 1 0 0 1 0 1 1 0  (each bit is the parity of the set bits in its index - famously cube-free, and generated lazily)
  LookAndSay(6)     : 1, 11, 21, 1211, 111221, 312211  (each term describes the previous one aloud: 1211 reads as one 1, one 2, two 1s)
  Farey(5)          : 0/1 1/5 1/4 1/3 2/5 1/2 3/5 2/3 3/4 4/5 1/1  (every fraction in lowest terms with denominator <= 5, already in ascending order)
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
--- PooledBufferBuilder<T> - pooled, growable buffers ---
  What   : Appends single values and spans into a pooled builder past its initial size, reads back the written
           region, and takes an independent snapshot.
  Why    : This is the array-pool pattern with the two mistakes designed out. A rented buffer is longer than what
           you wrote, so reading the whole array yields stale data from a previous tenant - WrittenSpan exists so
           that cannot happen by accident. And the span is a view over memory that returns to the pool on dispose,
           so anything outliving the builder must be copied out. Getting either wrong produces corruption that
           appears only under load, when buffers are actually reused.
  Expect : Nine elements written, and the sum is taken over the written region rather than the rented capacity. The
           snapshot is an independent copy whose trailing zeros are its own, not leftovers from the pool.

  WrittenCount     : 9  (expected 9 - what was written, which is smaller than the rented array behind it)
  IsEmpty          : False  (expected False - reports on written content, not on whether a buffer is rented)
  WrittenSpan sum  : 28  (expected 28 - summed over the written region only; summing the whole rented array would add stale values from a previous tenant)
  Snapshot         : [1, 2, 3, 4, 5, 6, 7, 0, 0]  (an independent copy - safe to keep after the builder is disposed, where WrittenSpan would dangle over returned memory)
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
--- IEnumerableExtensions - sequence-shaping operators ---
  What   : Runs one seven-element sequence through Batch, Windowed, Pairwise, Scan, RunLengthEncode, Interleave and
           ZipLongest.
  Why    : These are the operators LINQ leaves out, and the hand-written versions are where off-by-one errors live.
           Batch and Windowed are the pair most often confused: batching partitions, so every element appears once
           and the final batch may be short; windowing slides, so elements repeat and the count is n - size + 1.
           ZipLongest matters for the opposite reason - Zip stops at the shorter input and silently drops the tail,
           which is a data-loss bug rather than a formatting one.
  Expect : Batch(3) yields three groups with a short final one; Windowed(3) yields five overlapping groups from the
           same seven elements. Scan shows running totals rather than just the final sum, and ZipLongest pads the
           shorter side with a default instead of truncating.

  Batch(3)         : [1 2 3] [4 5 6] [7]  (partitions: every element appears exactly once and the last batch is short)
  Windowed(3)      : [1 2 3] [2 3 4] [3 4 5] [4 5 6] [5 6 7]  (slides: elements repeat across windows, and seven elements give n - size + 1 = 5 of them)
  Pairwise         : (1,2) (2,3) (3,4) (4,5) (5,6) (6,7)  (Windowed(2) in tuple form - the idiomatic way to compare each element with its predecessor)
  Scan (prefix +)  : 1 3 6 10 15 21 28  (running totals, so the final value equals Aggregate while every intermediate step stays visible)
  RunLengthEncode  : ax3 bx2 cx1  (collapses only CONSECUTIVE equal elements, which is what makes it a streaming operation)
  Interleave       : 1 10 2 20 3 30  (takes alternately from each source rather than concatenating them)
  ZipLongest       : (1,100) (2,200) (3,0)  (the third pair is (3,0): the shorter side is padded with a default, where Zip would have dropped the row entirely)
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
--- StringExtensions / ComparableExtensions / NumericExtensions ---
  What   : Converts one phrase through five casing conventions, strips diacritics, truncates, then clamps and
           range-tests values and runs the integer helpers.
  Why    : Each of these is a one-liner people rewrite per project and get subtly wrong. Casing conversions have to
           decide what counts as a word boundary before they can be consistent; slug and diacritic-stripping have to
           be stable, because a URL that changes is a broken link. Clamp and IsBetween replace the min/max and
           double-comparison idioms where the argument order is easy to invert and the inclusivity easy to get
           backwards.
  Expect : The same phrase yields five different shapes from one parse of its word boundaries. RemoveDiacritics
           keeps the letters and drops the marks rather than dropping the characters. Clamp returns the nearer
           bound, so 42 becomes 10 and -3 becomes 0.

  ToSlug           : hello-world-foo-bar  (URL-safe and stable - the same input must always give the same slug, or every link built from it breaks)
  ToKebabCase      : hello-world-foo-bar  (same word boundaries as the slug, applied to a different separator)
  ToSnakeCase      : hello_world_foo_bar  (one parse of word boundaries drives every casing form, which is what keeps them consistent)
  ToPascalCase     : HelloWorldFooBar  (boundaries become capitals rather than separators)
  ToConstantCase   : HELLO_WORLD_FOO_BAR  (snake case upper-cased - the conventional shape for an environment variable)
  RemoveDiacritics : Creme brulee a la mode  (keeps the letters and drops the marks: e-acute becomes e, not nothing)
  Truncate(12)     : The quick br  (a hard cut at 12 characters - no ellipsis is added unless one is asked for)
  Clamp 42 to 0..10: 10  (expected 10 - returns the nearer bound; this replaces Math.Min(Math.Max(..)), where the nesting is easy to invert)
  Clamp -3 to 0..10: 0  (expected 0 - the other bound, from the same call)
  5 IsBetween 1..10: True  (expected True - inclusive at both ends, stated once rather than as two comparisons that can disagree)
  15 IsBetween 1.10: False
  17 IsPrime       : True  (expected True)
  18 IsPrime       : False  (expected False - even, so it fails at the first check)
  GCD(48, 36)      : 12  (expected 12)
  LCM(4, 6)        : 12  (expected 12 - coincidentally the same value as the GCD above, from unrelated inputs)
  RoundToSig(3)    : 3.14  (three SIGNIFICANT figures, not three decimal places - the distinction that matters for very large or very small values)
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
--- WeekPattern - seven-day selection sets ---
  What   : Builds weekday patterns, formats each through the four format specifiers, and parses the text back to
           confirm the round trip.
  Why    : A set of weekdays is otherwise a bool[7] or a flags enum, and both lose to the same problem: nobody
           agrees which day index zero is. WeekPattern fixes the order and makes the text form canonical, so a
           pattern written in configuration, stored in a database and parsed back is the same pattern. The round
           trip is the property worth demonstrating, because that is what makes the text form safe to persist.
  Expect : One pattern renders four ways - masked, binary, and two annotated forms - all describing the same five
           days. Parsing any of them returns an equal pattern, so the round trip is True.

  MondayToFriday   : Count=5, S-format='_MTWTF_'  (five days selected; the S format marks unselected days with an underscore so position is unambiguous)
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
--- Bodu.Threading - async coordination primitives ---
  What   : Exercises the async coordination primitives - waits that are awaited rather than blocked on - with every
           wait completing deterministically.
  Why    : The BCL's coordination types predate async and block a thread while waiting, which on a thread pool is
           the shape that produces starvation: threads sitting idle inside a wait cannot run the work that would
           release them. These primitives suspend the continuation instead, so a waiting operation costs no thread
           at all. That is the entire reason to prefer them over a lock or a ManualResetEventSlim in asynchronous
           code.
  Expect : Every wait completes and each primitive reports the state a correct run produces. Nothing here depends on
           timing - the scenario signals before it waits where it can, so the transcript is the same on every run
           rather than merely usually.

  AsyncLazy value  : 42 (second await: 42)  (both awaits see the same value - AsyncLazy caches the completed task, not just the result)
  factory runs   : 1 (initialized once, then cached)  (expected 1 - two awaits, one initialization; concurrent awaiters join the in-flight task rather than racing)
  gate IsSet       : False  (expected False - nothing has signalled it yet)
  gate IsSet       : True (WaitAsync passed through)  (expected True - the awaiting continuation resumed on the signal, having held no thread while it waited)
  guarded counter  : 5 (5 lock/increment/release cycles)  (expected 5 - AsyncLock serialises the increments without blocking a thread while waiting for the lock)
```

**APIs demonstrated.** `AsyncLazy<T>` (constructor + `GetAwaiter` via `await`),
`AsyncManualResetEvent.IsSet` / `.Set` / `.WaitAsync`, `AsyncLock.LockAsync` and the `Releaser`.

## Layout

```text
Bodu.Core.Samples.CoreToolbox/
  Program.cs                       # runs the scenarios in order
  SampleConsole.cs                 # the what/why/expect banner every scenario opens with
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
