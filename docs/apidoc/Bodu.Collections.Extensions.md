---
uid: Bodu.Collections.Extensions
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

**Bodu.Collections.Extensions** holds the non-generic / shape-agnostic enumeration helpers that complement <xref:Bodu.Collections.Generic.Extensions>. Reach for this namespace when you want recursive-descent helpers or count helpers that work against `IEnumerable` rather than `IEnumerable<T>`.

## Key types

- <xref:Bodu.Collections.Extensions.IEnumerableExtensions> — `CountOrDefault`, `RecursiveSelect` for non-generic shapes.
- <xref:Bodu.Collections.Extensions.RecursiveSelectControl> — flag returned by recursive-selection callbacks to control descent: `IncludeAndDescend`, `IncludeAndStop`, `SkipAndDescend`, `SkipAndStop`.

## Example

```csharp
using Bodu.Collections.Extensions;

// Counts an enumerable without forcing enumeration when the source is already a collection.
int n = source.CountOrDefault(@default: 0);

// Recursive-select with fine-grained descent control.
IEnumerable<Node> visible = root.RecursiveSelect(
    n => n.Children,
    n => n.IsVisible ? RecursiveSelectControl.IncludeAndDescend
                     : RecursiveSelectControl.SkipAndStop);
```

## Notes

- **Counterpart to the generic surface.** Sequence helpers parameterised on `T` live in <xref:Bodu.Collections.Generic.Extensions>; this namespace covers the cases where the source is shape-agnostic.
- **Packaging.** This extension namespace ships in the `Bodu.Core` package; the concrete collection types in the sibling `Bodu.Collections.*` namespaces ship in the `Bodu.Collections` package, except the thread-safe `Bodu.Collections.Generic.Concurrent` variants, which ship in `Bodu.Collections.Concurrent`.
- **See also:** the [Bodu.Core introduction](~/docs/core/index.md), the companion <xref:Bodu.Collections.Generic.Extensions> namespace.
