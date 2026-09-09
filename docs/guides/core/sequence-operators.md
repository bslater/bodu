---
title: Sequence operators and generators
---

# Sequence operators and generators

`Bodu.Core` adds the LINQ operators that `System.Linq` leaves out — batching, windowing, adjacent-pair and run-length operators, combinatorics, hierarchical flattening with traversal control, shuffling — plus a small set of `IList<T>` / `IDictionary<TKey,TValue>` helpers and a `SequenceGenerator` for arithmetic and mathematical sequences. Four static classes are involved:

| Class | Namespace | Role |
|---|---|---|
| <xref:Bodu.Collections.Generic.Extensions.IEnumerableExtensions> | `Bodu.Collections.Generic.Extensions` | The operator catalogue on `IEnumerable<T>`. |
| <xref:Bodu.Collections.Generic.Extensions.IListExtensions> / <xref:Bodu.Collections.Generic.Extensions.IDictionaryExtensions> | `Bodu.Collections.Generic.Extensions` | Predicate index search, replace, swap/move; `GetOrAdd` / `AddOrUpdate`. |
| <xref:Bodu.Collections.Generic.ShuffleHelpers> | `Bodu.Collections.Generic` | In-place and yielding Fisher–Yates shuffles over an <xref:Bodu.IRandomGenerator>. |
| <xref:Bodu.Sequences.SequenceGenerator> | `Bodu.Sequences` | Ranges, iterated functions, and named mathematical sequences. |

Every `IEnumerable<T>` operator follows the LINQ convention: argument validation is eager (a `null` source or an invalid size throws at the call), and the work is deferred until enumeration. The catalogue below marks which operators stream the source lazily and which must **buffer** it (fully or partially) before they can yield.

## Pattern 1 — batching: `Batch` versus `BatchPooled`

```csharp
using Bodu.Collections.Generic.Extensions;

var source = Enumerable.Range(1, 7);

foreach (IEnumerable<string> batch in source.Batch(3, x => $"#{x}"))
    Console.WriteLine(string.Join(", ", batch));          // #1, #2, #3 / #4, #5, #6 / #7

// BatchPooled: one rented buffer, every batch aliases it — copy before the loop advances.
var retained = new List<int[]>();
foreach (ReadOnlyMemory<int> batch in source.BatchPooled(3))
    retained.Add(batch.ToArray());                        // 3 batches; the last is [7]

// Anti-pattern: ToList() over BatchPooled keeps windows over recycled memory.
var wrong = source.BatchPooled(3).ToList();
string stale = string.Join(", ", wrong[0].ToArray());     // "7, 5, 6" — not "1, 2, 3"
```

`Batch(size, selector)` projects each element (with an index-aware overload) and yields independently owned batches; for unprojected batching the BCL's `Enumerable.Chunk` already exists. `BatchPooled(size)` / `BatchPooled(size, selector)` rent **one** `ArrayPool<T>` buffer of `size` elements per enumeration and yield `ReadOnlyMemory<T>` windows over it — nothing is allocated per batch, but:

> [!WARNING]
> A `BatchPooled` batch is valid only until the enumerator advances or is disposed. The buffer is overwritten by the next step and returned to the pool when enumeration ends (the `finally` of the iterator, so `break` and exceptions return it too). Consume it with `foreach`, copy with `.ToArray()` if you need to keep a batch, and never `ToList()` the returned sequence.

## Pattern 2 — `Cache`: enumerate an expensive source once

```csharp
using Bodu.Collections.Generic.Extensions;

int pulls = 0;
IEnumerable<int> expensive = Enumerable.Range(1, 5).Select(x => { pulls++; return x * x; });

IEnumerable<int> cached = expensive.Cache();
string first = string.Join(", ", cached.Take(2));   // "1, 4" — pulls only what is needed
string all   = string.Join(", ", cached);           // "1, 4, 9, 16, 25"
string again = string.Join(", ", cached);           // served from the cache
int total = pulls;                                  // 5 — the source ran exactly once
```

`Cache()` wraps the source in a lazily filled, append-only buffer: the first enumerator pulls from the source on demand and every enumerator — including concurrent ones on other threads — reads from the shared buffer. The wrapper implements `IDisposable`; disposing it (via a cast) releases the source enumerator and the cached items and re-arms the wrapper for a fresh pass, while live enumerators observe `ObjectDisposedException`. It is the one operator in this catalogue that is safe to enumerate from several threads at once.

## Pattern 3 — combinatorics

```csharp
using Bodu.Collections.Generic.Extensions;

var letters = new[] { 'a', 'b', 'c' };

var pairs = letters.Combinations(2);      // [a, b], [a, c], [b, c]      — k-subsets, source order within a row
var perms = letters.Permutations();       // 6 rows: [a, b, c], [a, c, b], [b, a, c], [b, c, a], [c, a, b], [c, b, a]
var kPerm = letters.Permutations(2);      // 6 rows: [a, b], [a, c], [b, a], [b, c], [c, a], [c, b]
var cross = new[] { 1, 2 }.CartesianProduct(letters);                    // (1, a), (1, b), (1, c), (2, a), (2, b), (2, c)
var named = new[] { 1, 2 }.CartesianProduct(letters, (n, c) => $"{c}{n}"); // a1, b1, c1, a2, b2, c2
```

`Combinations` and `Permutations` buffer the source in full when enumeration begins (they need random access), then yield rows lazily; each row is an independent `IReadOnlyList<T>` snapshot, and elements are treated positionally, so duplicate values produce duplicate rows. `CartesianProduct` streams `first` and buffers `second` — so `first` may be infinite, `second` must not be.

## Pattern 4 — adjacent-element operators

```csharp
using Bodu.Collections.Generic.Extensions;

var readings = new[] { 1, 1, 2, 3, 3, 3, 10, 11 };

var pairs   = readings.Pairwise();                                // (1, 1), (1, 2), (2, 3), (3, 3), (3, 3), (3, 10), (10, 11)
var deltas  = readings.Pairwise((p, c) => c - p);                 // 0, 1, 1, 0, 0, 7, 1
var runs    = readings.RunLengthEncode();                         // (1, 2), (2, 1), (3, 3), (10, 1), (11, 1)
var chunks  = readings.SplitWhen((p, c) => c - p > 1);            // [1, 1, 2, 3, 3, 3], [10, 11]
var groups  = readings.ChunkBy(x => x < 5);                       // (True, [1, 1, 2, 3, 3, 3]), (False, [10, 11])
var sums    = readings.Windowed(3).Select(w => w.Sum());          // 4, 6, 8, 9, 16, 24
var running = readings.Scan(0, (acc, x) => acc + x);              // 1, 2, 4, 7, 10, 13, 23, 34
```

All seven stream the source lazily. `Pairwise`, `RunLengthEncode`, and `Scan` hold O(1) state; `Windowed(size)` holds the last `size` elements and yields only *complete* windows; `SplitWhen` and `ChunkBy` hold the current chunk until its boundary is seen. Every yielded window, chunk, or group is a stable snapshot that later iteration does not modify. `RunLengthEncode` and `ChunkBy` take an optional `IEqualityComparer<T>` / `IEqualityComparer<TKey>`.

## Pattern 5 — zipping, interleaving, indexing, and null filtering

```csharp
using Bodu.Collections.Generic.Extensions;

var a = new[] { 1, 2, 3 };
var b = new[] { "x", "y" };

var padded   = a.ZipLongest(b);                          // (1, x), (2, y), (3, null)
var defaults = a.ZipLongest(b, -1, "?");                 // (1, x), (2, y), (3, ?)
var joined   = a.ZipLongest(b, -1, "?", (n, s) => $"{n}{s}");   // 1x, 2y, 3?
var woven    = a.Interleave(b.Select(s => s.Length), new[] { 100, 200, 300, 400 });   // 1, 1, 100, 2, 1, 200, 3, 300, 400
var indexed  = b.Index();                                // (0, x), (1, y)
var refs     = new string?[] { "a", null, "b" }.WhereNotNull();   // a, b        (reference types)
var values   = new int?[] { 1, null, 3 }.WhereNotNull();          // 1, 3        (Nullable<T> unwrapped)
bool hasAll  = a.ContainsAll(new[] { 1, 3 });            // true
bool hasAny  = a.ContainsAny(new[] { 9, 3 });            // true
bool empty   = ((int[]?)null).IsNullOrEmpty();           // true
a.ForEach(x => Console.Write(x * 10 + " "));             // 10 20 30
```

`ZipLongest` continues until *both* sequences are exhausted, padding with `default` or the supplied fill values; `Interleave` takes one element from each sequence in turn and drops sequences as they run out. `Index` is the `(Index, Item)` tuple form of `Select((x, i) => …)`. `ContainsAll` / `ContainsAny` buffer `items` into a set and stream `source`; `IsNullOrEmpty` pulls at most one element.

## Pattern 6 — multi-accumulator `Aggregate`

The `Aggregate` overloads fold two or three accumulators in a single pass, optionally with the element index, and optionally project the final tuple through a result selector:

```csharp
using Bodu.Collections.Generic.Extensions;

var values = new[] { 4.0, 8.0, 15.0, 16.0, 23.0, 42.0 };

var (min, max) = values.Aggregate(double.MaxValue, double.MinValue, Math.Min, Math.Max);        // 4, 42
double mean    = values.Aggregate(0.0, 0, (s, x) => s + x, (n, _) => n + 1, (s, n) => s / n);     // 18
int weighted   = new[] { 5, 6, 7 }.Aggregate(0, (acc, x, i) => acc + (x * i));                   // 20 — index-aware fold
```

## Pattern 7 — `RecursiveSelect` and `RecursiveSelectControl`

`RecursiveSelect` flattens a hierarchy depth-first, pre-order, using a child selector. The projecting overloads receive the element, its index among its siblings, and its depth; the fullest overload also takes a **control** delegate that decides, per node, whether to yield it, descend into it, stop its siblings, or abandon the whole walk.

```csharp
using Bodu.Collections.Extensions;
using Bodu.Collections.Generic.Extensions;

public sealed record Node(string Name, List<Node> Children)
{
    public Node(string name, params Node[] children) : this(name, children.ToList()) { }
}
```

```csharp
using Bodu.Collections.Extensions;
using Bodu.Collections.Generic.Extensions;

var root = new Node("root",
    new Node("src", new Node("a.cs"), new Node("b.cs")),
    new Node("bin", new Node("app.dll")),
    new Node("README.md"));

var all = new[] { root }.RecursiveSelect(n => n.Children).Select(n => n.Name);
// root, src, a.cs, b.cs, bin, app.dll, README.md

var outline = new[] { root }.RecursiveSelect(n => n.Children, (n, index, depth) => $"{new string(' ', depth * 2)}{n.Name}");
// "root", "  src", "    a.cs", "    b.cs", "  bin", "    app.dll", "  README.md"

// Yield only files, and do not descend into "bin".
var files = new[] { root }.RecursiveSelect(
    n => n.Children,
    (n, index, depth) => n.Name,
    n => n.Name == "bin" ? RecursiveSelectControl.SkipOnly
       : n.Children.Count > 0 ? RecursiveSelectControl.RecurseOnly
       : RecursiveSelectControl.YieldOnly);
// a.cs, b.cs, README.md

// Stop everything at the first .dll.
var untilDll = new[] { root }.RecursiveSelect(
    n => n.Children,
    (n, index, depth) => n.Name,
    n => n.Name.EndsWith(".dll") ? RecursiveSelectControl.YieldAndExit : RecursiveSelectControl.YieldAndRecurse);
// root, src, a.cs, b.cs, bin, app.dll
```

<xref:Bodu.Collections.Extensions.RecursiveSelectControl> is a `[Flags]` enum of five primitives plus the named combinations you will actually return:

| Primitive flag | Effect on the current node |
|---|---|
| `Yield` | Include the node in the output. |
| `Recurse` | Descend into the node's children. |
| `Skip` | Do not yield the node; overrides `Yield` when both are set. |
| `Break` | Stop visiting the node's remaining **siblings** (the walk continues at the parent's level). |
| `Exit` | Terminate the entire traversal immediately. |

| Combination | Flags | Use when |
|---|---|---|
| `None` | — | Drop the node and its subtree. |
| `YieldOnly` | `Yield` | Emit a leaf (or treat the node as one). |
| `RecurseOnly` | `Recurse` | Pass through a container without emitting it. |
| `YieldAndRecurse` | `Yield \| Recurse` | The default behaviour of the control-free overloads. |
| `SkipOnly` | `Skip` | Same as `None`; explicit. |
| `SkipAndRecurse` | `Skip \| Recurse` | Same as `RecurseOnly`; explicit. |
| `YieldAndBreak` / `SkipAndBreak` | `… \| Break` | Emit (or not) then stop the remaining siblings. |
| `YieldAndExit` / `SkipAndExit` | `… \| Exit` | Emit (or not) then end the walk. |

The walk is iterative (an explicit stack), so deep hierarchies do not overflow the call stack; the child selector must not return `null`.

## Pattern 8 — randomization: `Randomize`, `ShuffleHelpers`, and `IRandomGenerator`

All randomness flows through <xref:Bodu.IRandomGenerator> — one method, `int Next(int maxValue)` — so tests can substitute a deterministic source. Two implementations ship: <xref:Bodu.XorShiftRandom> (fast, seedable, derives from `System.Random`; **not** thread-safe) and <xref:Bodu.Collections.Generic.Extensions.SystemRandomAdapter> (wraps any `System.Random`, including the thread-safe `Random.Shared`).

```csharp
using Bodu;
using Bodu.Collections.Generic;
using Bodu.Collections.Generic.Extensions;

var deck = Enumerable.Range(1, 10).ToArray();
IRandomGenerator rng = new XorShiftRandom(seed: 42);       // deterministic; not thread-safe

var sample  = deck.Randomize(RandomizationMode.ReservoirSample, rng, count: 3);   // 3 items, e.g. 10, 1, 8
var stream  = deck.Randomize(RandomizationMode.StreamWindowed, rng);              // all 10, locally shuffled
var lazy    = deck.Randomize(RandomizationMode.LazyShuffle, rng, count: 4);       // 4 items

int[] copy = deck.ToArray();
ShuffleHelpers.Shuffle(copy, rng);                          // in place, full Fisher–Yates; deck untouched
var three = ShuffleHelpers.ShuffleAndYield(deck, rng, count: 3);   // lazy partial shuffle of a copy

IRandomGenerator shared = new SystemRandomAdapter(Random.Shared);  // adapter over System.Random
int count = ShuffleHelpers.ShuffleAndYield(deck, shared).Count();  // 10
```

| Need | Use | Buffers? |
|---|---|---|
| A fixed-size random subset of a stream, single pass | `Randomize(RandomizationMode.ReservoirSample, rng, count)` | O(`count`) reservoir; `count` required |
| A locally shuffled stream in bounded memory | `Randomize(RandomizationMode.StreamWindowed, rng[, count])` | a sliding window |
| A reservoir sample that is then shuffled and yielded | `Randomize(RandomizationMode.LazyShuffle, rng, count)` | O(`count`); `count` required |
| Shuffle an array, span, or memory in place | `ShuffleHelpers.Shuffle(target, rng)` | none (in place) |
| A shuffled copy, or the first `count` of one, as a lazy sequence | `ShuffleHelpers.ShuffleAndYield(source, rng[, count])` | copies the source (or buffers an `IEnumerable<T>`) |

`Randomize` validates its arguments eagerly: a negative `count`, an undefined <xref:Bodu.Collections.Generic.Extensions.RandomizationMode>, or a missing `count` for the two modes that need one all throw at the call; a `count` larger than the source is detected during enumeration.

## Pattern 9 — `IList<T>` and `IDictionary<TKey,TValue>` helpers

```csharp
using Bodu.Collections.Generic.Extensions;

var list = new List<string> { "alpha", "beta", "gamma", "beta" };

int firstG   = list.IndexOf(s => s.StartsWith('g'));        // 2
int lastBeta = list.LastIndexOf(s => s == "beta");          // 3
int replaced = list.ReplaceAll("beta", "b");                // 2 — occurrences replaced in place
bool swapped = list.TrySwap(0, 2);                          // true  → gamma, b, alpha, b
bool moved   = list.TryMove(3, 0);                          // true  → b, gamma, b, alpha
bool ignored = list.TryMove(9, 0);                          // false — out of range, no throw

var counts = new Dictionary<string, int>();
counts.AddOrUpdate("a", 1);
counts.AddOrUpdate("a", 2);                                 // replaces
int fromFactory = counts.GetOrAdd("b", key => key.Length * 10);   // 10 — factory runs once
int existing    = counts.GetOrAdd("a", 99);                       // 2  — present, value ignored
```

`IndexOf` / `LastIndexOf` take an optional start index and count; `ReplaceAll` has value, comparer, and predicate forms. The dictionary helpers perform **no synchronization** — `GetOrAdd`'s lookup and insert are two operations, so on a shared dictionary use `ConcurrentDictionary<TKey,TValue>` instead.

## Pattern 10 — `SequenceGenerator`

```csharp
using Bodu.Sequences;

var up      = SequenceGenerator.Range(1, 5);                 // 1, 2, 3, 4, 5
var down    = SequenceGenerator.Range(5, 1);                 // 5, 4, 3, 2, 1 — direction inferred from the endpoints
var stepped = SequenceGenerator.Range(0, 10, 3);             // 0, 3, 6, 9  — inclusive stop, explicit step
var wide    = SequenceGenerator.Range(4_000_000_000L, 3);    // 4000000000, 4000000001, 4000000002 — 64-bit, count-based

var powers  = SequenceGenerator.NextWhile(1, x => x < 100, x => x * 2);          // 1, 2, 4, 8, 16, 32, 64
var indexed = SequenceGenerator.NextWhile(1, x => x < 100, (x, i) => x + i);     // 1, 1, 2, 4, 7, 11, 16, …, 92
var facts   = SequenceGenerator.NextWhile((n: 0, f: 1), s => s.n < 5, s => (s.n + 1, s.f * (s.n + 1)), s => s.f);   // 1, 1, 2, 6, 24

var fib     = SequenceGenerator.Fibonacci(10, 100);          // 13, 21, 34, 55, 89 — values in [min, max)
var farey   = SequenceGenerator.Farey(4);                    // 0/1, 1/4, 1/3, 1/2, 2/3, 3/4, 1/1
var leibniz = SequenceGenerator.Leibniz(0.1, 1.5);           // 1, -1/3, 1/5, -1/7, 1/9 — |term| in [min, max)
var conway  = SequenceGenerator.LookAndSay(5);               // 1, 11, 21, 1211, 111221
var thue    = SequenceGenerator.ThueMorse(8);                // 0, 1, 1, 0, 1, 0, 0, 1

IEnumerable<Guid> ids = SequenceGenerator.Factory(() => Enumerable.Range(0, 2).Select(_ => Guid.NewGuid()).GetEnumerator());
int twoFresh = ids.Count();                                  // 2 — re-enumerable, a fresh enumerator each pass

// NextWhile is open-ended: a predicate that never fails runs forever, so bound it.
var unbounded = SequenceGenerator.NextWhile(1L, _ => true, x => x * 3).Take(6);   // 1, 3, 9, 27, 81, 243
```

| Member | Yields | Finite? |
|---|---|---|
| `Range(int start, int stop)` | consecutive integers from `start` to `stop` inclusive, ascending or descending | yes |
| `Range(int start, int stop, int step)` | `start`, `start + step`, … while within `stop` | yes |
| `Range(long start, int count)` | `count` consecutive 64-bit integers | yes |
| `NextWhile(initial, condition, successor)` (+ index-aware and custom-state overloads) | `initial`, then `successor(previous)` while `condition` holds | **only if `condition` eventually fails** — the one open-ended member; bound with `Take` |
| `Factory(Func<IEnumerator<T>>)` | whatever the factory's enumerator yields, fresh per enumeration | as finite as the enumerator you supply |
| `Fibonacci(long min, long max)` | Fibonacci numbers in `[min, max)` | yes |
| `Farey(int order)` | the Farey sequence Fₙ as `(Numerator, Denominator)` in lowest terms | yes |
| `Leibniz(double min, double max)` | terms `(−1)ⁿ / (2n + 1)` whose magnitude lies in `[min, max)`, in signed order | yes when `min > 0`; `min == 0` never terminates — bound with `Take` |
| `LookAndSay(int count)` | the first `count` terms of Conway's look-and-say sequence, from `"1"` | yes |
| `ThueMorse(int count)` | the first `count` Thue–Morse digits | yes |

Note that `Leibniz` compares the *first* term's magnitude (1.0) against the exclusive `max` too, so `Leibniz(0.1, 1.0)` yields nothing — start `max` above 1 to include the leading term.

## Laziness at a glance

| Streams the source lazily (O(1) or O(window) state) | Buffers the source (fully or in part) before yielding |
|---|---|
| `Batch`, `BatchPooled`, `Cache` (fills on demand), `Pairwise`, `RunLengthEncode`, `Scan`, `Windowed`, `SplitWhen`, `ChunkBy`, `ZipLongest`, `Interleave`, `Index`, `WhereNotNull`, `ForEach`, `Aggregate`, `IsNullOrEmpty`, `RecursiveSelect`, `Randomize(StreamWindowed)`, every `SequenceGenerator` member | `Combinations`, `Permutations` (whole source), `CartesianProduct` (`second` only), `ContainsAll` / `ContainsAny` (`items` only), `Randomize(ReservoirSample)` / `Randomize(LazyShuffle)` (a `count`-sized reservoir), `ShuffleHelpers.ShuffleAndYield` (a copy of the source) |

## Where to go next

- [N-ary tree](tree.md) — `Tree<T>`, when the hierarchy should be a first-class structure rather than a child selector.
- [Choosing a collection](choosing-a-collection.md) — the collection catalogue these operators feed.
- [Bodu.Core introduction](../../docs/core/index.md) — the package's namespaces and headline types.
- [`Bodu.Collections.Generic.Extensions` API reference](xref:Bodu.Collections.Generic.Extensions) · [`Bodu.Sequences` API reference](xref:Bodu.Sequences)
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
