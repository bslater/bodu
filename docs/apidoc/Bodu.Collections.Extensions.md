---
uid: Bodu.Collections.Extensions
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

**Bodu.Collections.Extensions** holds the non-generic / shape-agnostic enumeration helpers that complement <xref:Bodu.Collections.Generic.Extensions>. Reach for this namespace when you want recursive-descent helpers or count helpers that work against `IEnumerable` rather than `IEnumerable<T>`.

## Key types

- <xref:Bodu.Collections.Extensions.IEnumerableExtensions> — `CountOrDefault`, `RecursiveSelect` for non-generic shapes.
- <xref:Bodu.Collections.Extensions.RecursiveSelectControl> — `[Flags]` value returned by a recursion-control callback to decide, per element, whether to yield it, descend into its children, and whether to stop: the primitive flags `None`, `Yield`, `Recurse`, `Skip`, `Break`, `Exit`, and the named combinations `YieldOnly`, `RecurseOnly`, `YieldAndRecurse`, `SkipOnly`, `SkipAndRecurse`, `YieldAndBreak`, `SkipAndBreak`, `YieldAndExit`, `SkipAndExit`. Shared with the typed `RecursiveSelect` overloads in <xref:Bodu.Collections.Generic.Extensions>.

## Example

```csharp
using Bodu.Collections.Extensions;

// Counts a non-generic IEnumerable — O(1) when the source is an ICollection, otherwise it enumerates.
int n = source.CountOrDefault();

// Recursive-select over a non-generic IEnumerable: the child selector and the result are untyped.
IEnumerable flattened = root.RecursiveSelect(node => ((Node)node).Children);

// Fine-grained descent control on the non-generic surface: yield visible nodes and descend into
// them, skip invisible subtrees, and stop the whole walk at the first node flagged as terminal.
IEnumerable visible = root.RecursiveSelect(
    node => ((Node)node).Children,
    (node, index, depth) => node,
    node => ((Node)node).IsTerminal ? RecursiveSelectControl.YieldAndExit
          : ((Node)node).IsVisible  ? RecursiveSelectControl.YieldAndRecurse
                                    : RecursiveSelectControl.SkipOnly);
```

When the source is already an `IEnumerable<T>`, prefer the typed overloads in <xref:Bodu.Collections.Generic.Extensions> — the same `RecursiveSelectControl` vocabulary with strongly typed selectors and an `IEnumerable<TResult>` result.

## Notes

- **Counterpart to the generic surface.** Sequence helpers parameterised on `T` live in <xref:Bodu.Collections.Generic.Extensions>; this namespace covers the cases where the source is shape-agnostic.
- **Packaging.** This extension namespace ships in the `Bodu.Core` package; the concrete collection types in the sibling `Bodu.Collections.*` namespaces ship in the `Bodu.Collections` package, except the thread-safe `Bodu.Collections.Generic.Concurrent` variants, which ship in `Bodu.Collections.Concurrent`.
- **See also:** the [Bodu.Core introduction](~/docs/core/index.md), the companion <xref:Bodu.Collections.Generic.Extensions> namespace.
