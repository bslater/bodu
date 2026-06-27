---
uid: Bodu.Functional
---

![Bodu.Functional](~/images/hero-core.svg)

## Purpose

**Bodu.Functional** holds functional-style helpers for `Bodu.Core`. Its single public type, <xref:Bodu.Functional.Memoizer>, wraps a pure function with a thread-safe cache so each distinct argument is computed at most once and later calls return the stored result — the classic memoization pattern, applied to a `Func<…>` delegate.

The cache is unbounded: every distinct argument seen is retained for the lifetime of the returned delegate. That makes <xref:Bodu.Functional.Memoizer> a fit for functions over a small, fixed argument domain (or a long-lived application where the working set is bounded), not for arbitrary user input.

## Static documentation

- **[Introduction](~/docs/core/index.md)** — where the functional helpers sit in the wider `Bodu.Core` surface.
- **[Memoization](~/guides/core/memoization.md)** — wrapping a pure function, the comparer overload, multi-argument keys, and the thread-safety and unbounded-cache caveats.

## Key types

- <xref:Bodu.Functional.Memoizer> — a static factory. `Memoize<TArg, TResult>(Func<TArg, TResult>)` and its `IEqualityComparer<TArg>` overload wrap a single-argument function; `Memoize<T1, T2, TResult>(Func<T1, T2, TResult>)` wraps a two-argument function keyed on the `(T1, T2)` pair. Argument types are constrained to `notnull` because they are used as cache keys.

## Example

```csharp
using Bodu.Functional;

var square = Memoizer.Memoize<int, int>(n => ExpensiveSquare(n));

int a = square(8);   // runs ExpensiveSquare
int b = square(8);   // returns the cached result
```

## Notes

- **Only successful results are cached.** If the wrapped function throws, nothing is stored and the next call retries. A `null` result is a valid cached value.
- **Thread-safe.** Results are held in a <xref:System.Collections.Concurrent.ConcurrentDictionary`2>. For sequential use the function runs exactly once per distinct argument; under concurrent first access for the same argument the function may run more than once, but only one result is published.
- **Unbounded cache.** Every distinct argument is retained for the lifetime of the returned delegate. Use it where the argument domain is small or bounded; for an eviction policy, reach for <xref:Bodu.Collections.Generic.EvictingDictionary`2> instead.
- **Arguments are non-nullable.** `TArg` (and `T1` / `T2`) are constrained to `notnull` because they serve as dictionary keys; supply a custom `IEqualityComparer<TArg>` to control how arguments are matched.
- **Recursion.** Memoizing a recursive function requires the function to call the *memoized* delegate for its recursive step, not itself.
