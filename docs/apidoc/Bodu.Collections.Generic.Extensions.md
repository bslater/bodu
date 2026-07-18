---
uid: Bodu.Collections.Generic.Extensions
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

**Bodu.Collections.Generic.Extensions** holds the sequence-shaping helpers for `IEnumerable<T>`, `IList<T>`, and `IDictionary<TKey, TValue>` — recursive selection, batched enumeration, sliding windows, and pluggable randomness-driven shuffles.

## Key types

- <xref:Bodu.Collections.Generic.Extensions.IEnumerableExtensions> — generic enumerable utilities: `Aggregate`, `Batch`, `Cache`, `ContainsAll`, `ContainsAny`, `ForEach`, `Index`, `IsNullOrEmpty`, `Randomize`, `RecursiveSelect`, `WhereNotNull`, and more.
- <xref:Bodu.Collections.Generic.Extensions.IListExtensions> — list-specific utilities: `IndexOf`, `LastIndexOf`, `ReplaceAll`, `TryMove`, `TrySwap`.
- <xref:Bodu.Collections.Generic.Extensions.IDictionaryExtensions> — dictionary utilities: `AddOrUpdate`, `GetOrAdd`.
- <xref:Bodu.Collections.Generic.ShuffleHelpers>, <xref:Bodu.Collections.Generic.Extensions.SystemRandomAdapter>, <xref:Bodu.Collections.Generic.Extensions.RandomizationMode> — pluggable randomness-driven shuffles backed by <xref:Bodu.IRandomGenerator>.

> [!TIP]
> Looking for sequence *producers* — `Range`, `NextWhile`, or named series such as `Fibonacci`? Those live on `SequenceGenerator` in the dedicated <xref:Bodu.Sequences> namespace.

## Example

```csharp
using Bodu.Collections.Generic.Extensions;

// Batched enumeration over an IEnumerable<T>.
foreach (IEnumerable<int> batch in Enumerable.Range(0, 100).Batch(size: 16, selector: static x => x))
    Process(batch);

// Recursive selection.
IEnumerable<DirectoryInfo> allDirs = root.EnumerateDirectories()
    .RecursiveSelect(d => d.EnumerateDirectories());
```

## Notes

- **Lazy where possible.** Helpers like `Batch`, `RecursiveSelect`, and `Cache` are lazy. Materialise to `ToList()` / `ToArray()` when you need a stable snapshot.
- **Argument validation.** Every public extension method validates its arguments via <xref:Bodu.ThrowHelper>.
- **Packaging.** This extension namespace ships in the `Bodu.Core` package; the concrete collection types in the sibling `Bodu.Collections.*` namespaces ship in the `Bodu.Collections` package, except the thread-safe `Bodu.Collections.Generic.Concurrent` variants, which ship in `Bodu.Collections.Concurrent`.
- **See also:** the [Bodu.Core introduction](~/docs/core/index.md), the companion <xref:Bodu.Collections.Extensions> namespace, the <xref:Bodu.Sequences> sequence generators, the [Bodu.Extensions](xref:Bodu.Extensions) date / numeric / string extension surface.
