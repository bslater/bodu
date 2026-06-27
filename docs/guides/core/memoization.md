---
title: Memoization
---

# Memoization

*Memoization* caches the result of a pure function so each distinct argument is computed once and subsequent calls return the stored value. <xref:Bodu.Functional.Memoizer> applies that pattern to a `Func<…>` delegate: it returns a new delegate of the same shape, backed by a thread-safe cache.

It fits functions that are **pure** (the result depends only on the arguments) and **expensive** relative to a dictionary lookup. The cache is **unbounded** — every distinct argument is retained for the lifetime of the returned delegate — so use it over a small or bounded argument domain. When you need eviction, reach for [`EvictingDictionary`](evicting-dictionary.md) instead.

A few contract points worth keeping in mind:

- Only **successful** results are cached; if the function throws, nothing is stored and the next call retries.
- A `null` result is a valid cached value.
- Argument types are constrained to `notnull` because they are used as cache keys.

## Pattern 1 — Wrap a pure function

```csharp
using Bodu.Functional;

static int ExpensiveSquare(int n) { /* slow */ return n * n; }

var square = Memoizer.Memoize<int, int>(n => ExpensiveSquare(n));

int a = square(8);   // runs ExpensiveSquare
int b = square(8);   // cache hit — ExpensiveSquare is not called again
```

The returned `square` is a `Func<int, int>`; pass it around like any other delegate. The cache lives as long as that delegate reference does.

## Pattern 2 — Control argument matching with a comparer

The single-argument overload accepts an `IEqualityComparer<TArg>` so you can decide when two arguments are "the same" key. A case-insensitive comparer folds `"Hello"` and `"HELLO"` onto one cache entry:

```csharp
using Bodu.Functional;

var lengthOf = Memoizer.Memoize<string, int>(
    s => s.Length,
    StringComparer.OrdinalIgnoreCase);

int x = lengthOf("Hello");   // computed
int y = lengthOf("HELLO");   // cache hit
```

Pass `null` for the comparer (or use the single-argument overload) to match on the default equality comparer for the argument type.

## Pattern 3 — Multi-argument functions

The two-argument overload keys the cache on the `(T1, T2)` pair, so the result is reused only when both arguments match:

```csharp
using Bodu.Functional;

static int SlowAdd(int x, int y) { /* slow */ return x + y; }

var add = Memoizer.Memoize<int, int, int>((x, y) => SlowAdd(x, y));

int s1 = add(3, 4);   // computed
int s2 = add(3, 4);   // cache hit
int s3 = add(4, 3);   // computed — different key
```

> [!NOTE]
> The cache is thread-safe. For sequential use the function runs exactly once per distinct argument; under concurrent first access for the *same* argument the function may run more than once, but only one result is published. Memoizing a recursive function requires the function to call the **memoized** delegate for its recursive step.

## Where to go next

- <xref:Bodu.Functional> — the full API surface for `Memoizer`.
- [Evicting dictionary](evicting-dictionary.md) — a bounded-capacity cache when memoization's unbounded retention is the wrong trade-off.
- [Choosing a collection](choosing-a-collection.md) — picking the right backing store for cached data.
- [Core foundations](../topics/core-foundations.md) — the wider `Bodu.Core` toolbox.
- [Core documentation](../../docs/core/index.md) — concepts and getting started for `Bodu.Core`.
