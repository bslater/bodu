# Bodu.Core.Samples.FunctionalRailway

The `Bodu.Functional` seam from `Bodu.Core`: the railway-oriented primitives that replace
`null`, out-parameters, and exception-driven control flow with composable values. Five scenarios
cover optional values, fallible pipelines, typed choices, memoized pure functions, and the
awaitable companions.

Everything runs offline with fixed inputs — deterministic output every run.

```bash
dotnet run --project samples/Core/Bodu.Core.Samples.FunctionalRailway
```

## Scenario 1 — OptionBasics

**Intent.** Show `Option<T>` as a total replacement for a nullable return: a lookup either yields
a value (`Some`) or explicitly none (`None`), and callers transform the value without ever
branching on `null`.

**What it does.** Looks items up in a small inventory dictionary, converting a miss into `None`
rather than a sentinel. It then `Map`s a present quantity into a label, uses `Filter` to demote an
out-of-stock `Some` to `None`, and `Match`es both cases into a single reorder verdict.

**What to expect.** `widget` is present (`Some(12)`), `missing` is absent (`None`), the `gadget`
row shows the zero-stock value filtered out to `False`, and `Match` collapses the present case to a
verdict:

```text
widget   : Some(12)
missing  : None
sprocket : 7 in stock
gadget   : in stock? False
widget   : reorder yes
```

**APIs demonstrated.** `Option.Some` / `Option.None<T>`, `Option<T>.Map`, `Option<T>.Filter`,
`Option<T>.Match`, `Option<T>.GetValueOrDefault`, `Option<T>.IsSome`.

## Scenario 2 — ResultRailway

**Intent.** Compose a validate → parse → transform pipeline with `Result<T>` where the first
failing step short-circuits the rest, carrying a `ResultError` to the end instead of throwing.

**What it does.** Threads four inputs through `Validate` (rejects blank), `Bind(Parse)` (rejects
non-integers and negatives), a success-only `Tap`, and a final `Map` that doubles the value. Each
row prints the terminal state via `Match`.

**What to expect.** The valid input doubles to `84`; the other three divert onto the error track at
the step that rejected them, each with its specific message:

```text
'42'   : ok 84
' -3 ' : error: -3 is negative
'oops' : error: 'oops' is not an integer
''     : error: input was blank
```

**APIs demonstrated.** `Result.Success<T>` / `Result.Failure<T>`, `ResultError.FromMessage`,
`Result<T>.Bind`, `Result<T>.Map`, `Result<T>.Tap`, `Result<T>.Match`.

## Scenario 3 — EitherChoice

**Intent.** Demonstrate `Either<TLeft, TRight>` as a typed either/or where neither side is
privileged (unlike `Result`, whose right side is specifically an error).

**What it does.** Classifies each payment descriptor into a left branch (card token) or a right
branch (bank identifier), then uses `MapLeft`/`MapRight` to mask/relabel one branch at a time, and
`Match` to render both as one string.

**What to expect.** The card rows report `isLeft=True` and render a masked card; the bank row
reports `isLeft=False` and renders the bank identifier:

```text
card:4111  -> isLeft=True  card ****11
iban:DE89  -> isLeft=False bank DE89
card:5500  -> isLeft=True  card ****00
```

**APIs demonstrated.** `Either<TLeft, TRight>.Left` / `.Right`, `Either<,>.MapLeft` / `.MapRight`,
`Either<,>.Match`, `Either<,>.IsLeft`.

## Scenario 4 — Memoization

**Intent.** Show `Memoizer` caching a pure function so repeated calls with the same argument return
the stored result. The invocation counter is the evidence — it advances once per distinct argument,
never per call.

**What it does.** Wraps a counted squaring function with `Memoizer.Memoize`, calls it ten times over
four distinct arguments, then calls it once more for an argument already seen.

**What to expect.** Ten calls, four distinct arguments, so the underlying function ran exactly four
times — and the extra `square(13)` is served from the cache without advancing the counter:

```text
calls made       : 10
distinct args    : 4 (3, 5, 8, 13)
function invoked : 4 time(s)
square(13)       : 169 (served from cache, counter unchanged: 4)
```

**APIs demonstrated.** `Memoizer.Memoize<TArg, TResult>`.

## Scenario 5 — AsyncRailway

**Intent.** Show the Task-based companions (`MapAsync`/`BindAsync`/`MatchAsync`): the same railway
composition, but each step is awaitable, so a pipeline of asynchronous operations reads as one
fluent chain.

**What it does.** Starts each row from a `Task<Result<string>>`, then `BindAsync`es a parse step and
`MapAsync`es a scaling step. The awaited tasks complete synchronously so the output stays
deterministic.

**What to expect.** The valid input scales to `42`; the invalid input diverts to the error track,
exactly as the synchronous railway does:

```text
'21'  : ok 42
'nope': error: 'nope' is not an integer
```

**APIs demonstrated.** `ResultAsyncExtensions.BindAsync`, `ResultAsyncExtensions.MapAsync`,
`Result<T>.Match`.

## Layout

```text
Bodu.Core.Samples.FunctionalRailway/
  Program.cs                     # runs the scenarios in order
  Scenarios/OptionBasics.cs
  Scenarios/ResultRailway.cs
  Scenarios/EitherChoice.cs
  Scenarios/Memoization.cs
  Scenarios/AsyncRailway.cs
```

## Related

- `Bodu.Core.Samples.CoreToolbox` — sequences, pooled buffers, the enumerable operators, string and
  numeric extensions, `WeekPattern`, and the async threading primitives.
- `Bodu.Core.Samples.TextEncoding` — BOM detection, transcoding, and pooled string encoding.
