---
uid: Bodu.Text.Delimited.Nodes
---

![Bodu.Text.Delimited.Nodes](~/images/hero-delimited.svg)

## Purpose

**Bodu.Text.Delimited.Nodes** is the mutable document object model of <xref:Bodu.Text.Delimited>: parse a delimited document into an editable array of records, add or change fields, and write it back through the <xref:Bodu.Text.Delimited.Writer.Utf8DelimitedWriter>. Records are header-keyed objects (or positional arrays under `NoHeader`) and every field is a string. For read-only inspection use the sibling <xref:Bodu.Text.Delimited.Document> tier; for typed records use the <xref:Bodu.Text.Delimited.DelimitedSerializer>.

## Key types

- <xref:Bodu.Text.Delimited.Nodes.DelimitedNode> — abstract base: `Parse` (returning the record array), `AsArray` / `AsObject` / `AsValue`, `ValueKind`, `DeepClone`, `WriteTo`, and `ToUtf8Bytes`.
- <xref:Bodu.Text.Delimited.Nodes.DelimitedArray> — the record set (and a positional record): `Add`, `RemoveAt`, `Count`, and an integer indexer.
- <xref:Bodu.Text.Delimited.Nodes.DelimitedObject> — a header-keyed record: `Keys`, `ContainsKey` / `TryGetValue` / `Remove`, and a string indexer that adds or replaces.
- <xref:Bodu.Text.Delimited.Nodes.DelimitedValue> — a single field with a settable `Value`.

## Example

```csharp
using Bodu.Text.Delimited.Nodes;

DelimitedArray records = DelimitedNode.Parse("symbol,qty\nAAPL,10\n"u8);
records[0].AsObject()["qty"].AsValue().Value = "15";

var extra = new DelimitedObject();
extra["symbol"] = new DelimitedValue("MSFT");
extra["qty"] = new DelimitedValue("5");
records.Add(extra);

byte[] csv = records.ToUtf8Bytes();
```

## Notes

- **Trivia-free.** Comments and original quoting are not preserved; the writer re-quotes minimally on output.
- **Consistent shape.** Every record in the array should share the first record's keys — the header is derived from the first object when writing.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [delimited guide](~/guides/formats/delimited.md).
