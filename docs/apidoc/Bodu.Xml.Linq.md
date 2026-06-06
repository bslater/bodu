---
uid: Bodu.Xml.Linq
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

**Bodu.Xml.Linq** is a small XML / LINQ-to-XML helper namespace used internally by `Bodu.Globalization.Calendar`'s rule parsers and exposed for consumers who need the same primitives. Reach for it when you need to resolve namespace prefixes against a fragment of LINQ-to-XML without standing up a full `XmlNameTable`.

## Key types

- <xref:Bodu.Xml.Linq.XmlNamespaceResolver> — implements `IXmlNamespaceResolver` over an `XElement` so XPath / prefix-aware queries can be evaluated against a LINQ-to-XML fragment.

## Example

```csharp
using System.Xml.Linq;
using System.Xml.XPath;
using Bodu.Xml.Linq;

XElement root = XElement.Parse(xml);
var resolver = new XmlNamespaceResolver(root);

IEnumerable<XElement> matches = root.XPathSelectElements(
    "//bc:NotableDate[@territory='AU']",
    resolver);
```

## Notes

- **Single-fragment scope.** The resolver reflects the element's in-scope namespaces only; declarations on ancestors outside the fragment are not visible.
- **Calendar rule documents.** Authored notable-date rule files use the `urn:bodu:globalization:calendar` namespace; this helper resolves in-scope namespace prefixes when reading prefixed elements from such a fragment.
- **See also:** the [Bodu.Core introduction](~/docs/core/index.md) and <xref:Bodu.Globalization.Calendar>.
