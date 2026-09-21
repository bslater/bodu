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
--- Option<T> - present-or-absent without null ---
  What   : Looks up a present and a missing key, then chains Map, Filter and GetValueOrDefault over both to show the
           operators running only when there is a value.
  Why    : A nullable return puts the burden on the caller to remember a check, and forgetting is the most common
           bug there is. Option moves absence into the type, so the value cannot be reached without the None case
           being dealt with. The operators are the other half: Map and Filter apply only to Some and pass None
           straight through, so a four-step transformation needs one null check at the end rather than four along
           the way.
  Expect : widget resolves to Some(12) and missing to None. The chained calls never run against an absent value, so
           the missing lookups return the supplied fallback rather than throwing, and a Filter that rejects its
           input turns a Some into a None.

  widget   : Some(12)  (expected Some(12) - present, and the value is only reachable through the Some case)
  missing  : None  (expected None - absence is a value here, not a null to be checked for later)
  sprocket : 7 in stock  (Map ran because the lookup was Some; GetValueOrDefault is where the chain finally leaves Option)
  gadget   : in stock? False  (expected False - Filter rejected the value, turning a Some into a None without any branch being written)
  widget   : reorder yes  (the whole chain expressed once for the present case; an absent lookup would have skipped every step)
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
--- Result<T> - validate -> parse -> transform railway ---
  What   : Runs four inputs through one validate-then-parse-then-double pipeline: a good value and three that fail
           at three different steps.
  Why    : Exceptions for expected failures cost you the control flow: the happy path gets interleaved with
           handlers, and an error is easy to catch too broadly or too late. A railway keeps failure on the same
           return path, so the first failing step short-circuits the rest and the error travels to the end
           untouched. The steps stay individually simple because each is written assuming the previous one succeeded
           - which is guaranteed, since otherwise it does not run at all.
  Expect : Only '42' reaches the end, doubled to 84. The other three each stop at a different step and report why,
           in the vocabulary of the step that failed - blank input, unparseable text, and a negative value - rather
           than one generic message.

  '42'   : ok 84  (expected ok 84 - the only input that clears all three steps)
  ' -3 ' : error: -3 is negative  (parsed fine, then failed the range rule - so the error names the value, not the format)
  'oops' : error: 'oops' is not an integer  (failed one step earlier, at the parse; the doubling step never ran)
  ''     : error: input was blank  (failed at the first step, so neither the parse nor the transform was reached)
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
--- Either<TLeft, TRight> - a typed choice ---
  What   : Parses payment identifiers that are either a card or a bank account, then renders each through Match so
           both shapes are handled at the point of use.
  Why    : Either is for a value that is legitimately one of two things, where neither is a failure. That is what
           separates it from Result: Result privileges one side as the error, and its operators short-circuit on it.
           Either privileges neither, so nothing is skipped and Match forces both cases to be written. The
           alternative in practice is a class with two nullable fields and an informal rule that exactly one is set
           - which the compiler cannot check and which drifts.
  Expect : Three inputs resolve to two different shapes, and IsLeft distinguishes them. Each renders with the
           vocabulary of its own side - a masked card number or a bank identifier - because Match receives the
           correctly typed value rather than a common base.

  card:4111  -> isLeft=True  card ****11  (Match supplied the correctly typed side, so neither branch needs a cast or a null check)
  iban:DE89  -> isLeft=False bank DE89  (Match supplied the correctly typed side, so neither branch needs a cast or a null check)
  card:5500  -> isLeft=True  card ****00  (Match supplied the correctly typed side, so neither branch needs a cast or a null check)
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
--- Memoizer - cache a pure function ---
  What   : Wraps a counting function, calls it ten times across four distinct arguments, and reports how often the
           underlying function actually ran.
  Why    : Memoization is only sound for a pure function - same input, same output, no side effects - because the
           cache will happily serve a stale answer forever otherwise. Given that, it turns repeated work into a
           lookup with no change to the call sites. The invocation counter is the load-bearing evidence here:
           without it, a memoized and a non-memoized function are indistinguishable from their return values alone,
           which is exactly why this scenario counts rather than just printing results.
  Expect : Ten calls, four distinct arguments, and the function body runs four times - once per distinct argument.
           The final call re-requests an argument already seen, and the counter does not move.

  calls made       : 10  (through the memoized wrapper, which is what every call site sees)
  distinct args    : 4 (3, 5, 8, 13)  (the cache is keyed on the argument, so this is the upper bound on real work)
  function invoked : 4 time(s)  (expected 4 - six of the ten calls were served from the cache without entering the function)
  square(13)       : 169  (counter still 4 - an already-seen argument costs a lookup, and the function is not re-entered)
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
--- Result<T> async - awaitable railway ---
  What   : Runs a good and a bad input through the same pipeline as the synchronous railway, except every step is
           awaitable and composed with MapAsync and BindAsync.
  Why    : Real pipelines call out - a database, an HTTP service - so the railway is only useful if it survives
           async. Without the async companions each await forces the chain to be unwound into statements with an
           explicit check between them, which is the shape the railway existed to remove. With them the composition
           is unchanged and short-circuiting still holds: a failed step means the next one is never awaited, so no
           wasted call is made.
  Expect : The same two outcomes as the synchronous version - 21 doubles to 42, and 'nope' reports the parse
           failure. The awaited tasks complete synchronously here so the transcript stays reproducible; nothing
           about the composition depends on that.

  '21'  : ok 42  (expected ok 42 - every awaited step ran in order)
  'nope': error: 'nope' is not an integer  (the parse failed, so the doubling step was never awaited - short-circuiting saves the call, not just the result)
```

**APIs demonstrated.** `ResultAsyncExtensions.BindAsync`, `ResultAsyncExtensions.MapAsync`,
`Result<T>.Match`.

## Layout

```text
Bodu.Core.Samples.FunctionalRailway/
  Program.cs                     # runs the scenarios in order
  SampleConsole.cs               # the what/why/expect banner every scenario opens with
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
