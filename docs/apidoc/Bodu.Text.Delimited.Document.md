---
uid: Bodu.Text.Delimited.Document
---

![Bodu.Text.Delimited.Document](~/images/hero-delimited.svg)

## Purpose

**Bodu.Text.Delimited.Document** is the read-only document object model of <xref:Bodu.Text.Delimited>, shaped after `System.Text.Json`'s `JsonDocument` / `JsonElement`. A parse produces a disposable <xref:Bodu.Text.Delimited.Document.DelimitedDocument> whose root is the array of records; each <xref:Bodu.Text.Delimited.Document.DelimitedElement> is a lightweight `struct` cursor over a record (a header-keyed object, or a positional array under `NoHeader`) or a field. Prefer it over the mutable <xref:Bodu.Text.Delimited.Nodes> tier when you only need to query.

## Key types

- <xref:Bodu.Text.Delimited.Document.DelimitedDocument> — the disposable owner: `Parse` (with an optional <xref:Bodu.Text.Delimited.Reader.DelimitedReaderOptions>), `RootElement`, and `Headers`.
- <xref:Bodu.Text.Delimited.Document.DelimitedElement> — the cursor: `ValueKind` (a <xref:Bodu.Text.Delimited.DelimitedValueKind>), `GetString`, `GetProperty` / `TryGetProperty`, an integer indexer, `GetArrayLength`, and `EnumerateArray` / `EnumerateObject`.
- <xref:Bodu.Text.Delimited.Document.DelimitedElement.ArrayEnumerator> / <xref:Bodu.Text.Delimited.Document.DelimitedElement.ObjectEnumerator> — the struct enumerators returned by `EnumerateArray` / `EnumerateObject`.
- <xref:Bodu.Text.Delimited.Document.DelimitedProperty> — a `Name` / `Value` pair yielded by `EnumerateObject`.

## Example

```csharp
using Bodu.Text.Delimited.Document;

using DelimitedDocument document = DelimitedDocument.Parse("symbol,qty\nAAPL,10\nMSFT,5\n"u8);

foreach (DelimitedElement record in document.RootElement.EnumerateArray())
{
    string symbol = record.GetProperty("symbol").GetString();
    string qty = record.GetProperty("qty").GetString();
}
```

## Notes

- **Lifetime.** Elements are only valid while their document is undisposed.
- **String-only values.** Every field is a `String` element; convert numbers and dates yourself or bind through the <xref:Bodu.Text.Delimited.DelimitedSerializer>.
- **Streaming alternative.** For very large files, prefer the serializer's `DeserializeAsyncEnumerableAsync<TRecord>` or the <xref:Bodu.Text.Delimited.Reader.Utf8DelimitedReader> token loop over materializing a document.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [delimited guide](~/guides/formats/delimited.md).
