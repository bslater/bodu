---
uid: Bodu.Xml.Linq
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

**Bodu.Xml.Linq** is a small LINQ-to-XML helper namespace in `Bodu.Core`. Reach for it when a document declares a default namespace and you want to look up child elements by local name, instead of qualifying every `Element` / `Elements` call with that namespace.

## Key types

- <xref:Bodu.Xml.Linq.XmlNamespaceResolver> - captures the namespace of a root `XElement` once and exposes `Element`, `Elements`, and `Name` helpers that qualify a local name with it. It holds no prefix map and implements no interface.

## Example

```csharp
using System.Xml.Linq;
using Bodu.Xml.Linq;

XDocument doc = XDocument.Parse(
    "<library xmlns='urn:books'><book id='1'><title>Foundation</title></book></library>");

var ns = new XmlNamespaceResolver(doc.Root!);

XElement? book  = ns.Element(doc.Root!, "book");
XElement? title = ns.Element(book!, "title");
Console.WriteLine(title?.Value);               // Foundation
Console.WriteLine(book!.Attribute("id")?.Value); // 1 - unprefixed attributes are in no namespace
```

## Notes

- **One namespace per resolver.** The resolver keeps only the root element's namespace (`XNamespace.None` when the root has none); it is fixed for the lifetime of the instance. Create a separate resolver for each namespace a document uses.
- **Elements, not attributes.** An unprefixed attribute belongs to no namespace even under a default namespace, so look it up by plain name; use `Name` for an attribute only when the attribute is explicitly qualified with the resolver's namespace.
- **See also:** the [Bodu.Core introduction](~/docs/core/index.md).
