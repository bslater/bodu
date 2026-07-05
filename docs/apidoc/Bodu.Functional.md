---
uid: Bodu.Functional
---

![Bodu.Functional](~/images/hero-core.svg)

## Purpose

**Bodu.Functional** holds the functional-style building blocks of `Bodu.Core`: the <xref:Bodu.Functional.Memoizer> caching factory and the railway-oriented outcome primitives — <xref:Bodu.Functional.Option`1> (a value that may be absent), <xref:Bodu.Functional.Result> / <xref:Bodu.Functional.Result`1> with <xref:Bodu.Functional.ResultError> (success-or-described-failure), and <xref:Bodu.Functional.Either`2> (a symmetric disjoint union). The outcome types are `readonly struct`s that compose through `Map` / `Bind` / `Match` chains, with Task-based async companions.

Three contracts define the family: present values are never `null` (the strict factories throw; the lenient lifts map `null` to absence), each type documents its `default` (`Option` defaults to `None`, the `Result` pair to a failure with an empty error, `Either` to an explicit uninitialized state whose side-requiring operations throw), and only `Result<T>` is railway-biased — `Either` deliberately exposes `MapLeft` / `MapRight` rather than an unqualified `Map`.

## Static documentation

- **[Introduction](~/docs/core/index.md)** — where the functional types sit in the wider `Bodu.Core` surface.
- **[Options, results, and eithers](~/guides/core/functional-results.md)** — the railway patterns, the null and `default` contracts, type bridges, and async composition.
- **[Memoization](~/guides/core/memoization.md)** — wrapping a pure function, the comparer overload, multi-argument keys, and the thread-safety and unbounded-cache caveats.

## Key types

- <xref:Bodu.Functional.Option`1> — `Some` / `None` with `Map`, `Bind`, `Filter`, `Match`, the `GetValueOrDefault` family, and `ToResult`; the non-generic <xref:Bodu.Functional.Option> companion adds inference-friendly factories and `FromNullable`.
- <xref:Bodu.Functional.Result`1> / <xref:Bodu.Functional.Result> — the success/failure rail: `Map`, `Bind`, `MapError`, `Tap` / `TapError`, `Match`, `GetValueOrThrow`, `ToOption`; factories live on the non-generic `Result`.
- <xref:Bodu.Functional.ResultError> — the failure descriptor: optional `Code`, never-null `Message`, optional captured `Exception` (attached as `InnerException` by `GetValueOrThrow`, never rethrown directly).
- <xref:Bodu.Functional.Either`2> — `Left` / `Right` with `TryGetLeft` / `TryGetRight`, `Match`, `MapLeft` / `MapRight`, and `Swap`.
- <xref:Bodu.Functional.OptionAsyncExtensions> / <xref:Bodu.Functional.ResultAsyncExtensions> — Task-based `MapAsync` / `BindAsync` / `MatchAsync` (plus `TapAsync` / `TapErrorAsync`) over both task and sync sources.
- <xref:Bodu.Functional.Memoizer> — wraps a pure function with a thread-safe unbounded cache (single- and two-argument overloads, optional key comparer).

## Example

```csharp
using Bodu.Functional;

Result<Order> ParseOrder(string json) =>
    TryParse(json, out var order)
        ? Result.Success(order)
        : Result.Failure<Order>(ResultError.FromMessage("The payload is not a valid order.", code: "ORD001"));

var confirmation = ParseOrder(payload)
    .Bind(Validate)
    .Map(o => o.ConfirmationNumber)
    .TapError(e => log.Warn(e.Message))
    .GetValueOrDefault("(none)");
```

## Notes

- **Absence is typed, never null.** `Option<T>.Some`, `Result.Success<T>`, and the `Either` factories reject `null` with `ArgumentNullException`; the implicit `T` → `Option<T>` conversion and `Option.FromNullable` are the lenient lifts that map `null` to `None`.
- **Totality by documented `default`.** `default(Option<T>)` is `None` and `default(Result<T>)` is a failure carrying an empty `ResultError`, so fields and array elements are safe uninitialized. `default(Either<,>)` is the deliberate exception: with no privileged side, it throws from side-requiring operations (the `ImmutableArray<T>.IsDefault` precedent).
- **Async combinators take no `CancellationToken`.** They perform no I/O of their own; cancellation belongs inside the caller's delegates. Guards throw synchronously and every await uses `ConfigureAwait(false)`.
- **Memoizer caveats.** Only successful results are cached, the cache is unbounded, and argument types are `notnull` — see the guide for details; reach for <xref:Bodu.Collections.Generic.EvictingDictionary`2> when eviction is needed.
