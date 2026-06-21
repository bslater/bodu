---
title: Reading property sets
---

# Reading property sets

Many compound files carry authored document metadata — title, author, timestamps, page and word counts — in two conventionally-named OLE property-set streams: `\x05SummaryInformation` and `\x05DocumentSummaryInformation`. The <xref:Bodu.IO.Compound.PropertySets> namespace parses those streams into typed views, and <xref:Bodu.IO.Compound.CompoundFile> exposes convenience `TryGet*` methods for the standard pair.

A property set is a code-paged, sectioned key/value map: integer property IDs map to typed values (strings, integers, booleans, `FILETIME` timestamps). The typed views translate the well-known IDs into named properties so callers never touch the raw IDs.

## Pattern 1 — summary information

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

using CompoundFile file = CompoundFile.Open(File.OpenRead("report.doc"));

if (file.TryGetSummaryInformation(out SummaryInformation? summary))
{
    Console.WriteLine(summary.Title);
    Console.WriteLine(summary.Author);
    Console.WriteLine(summary.LastSaveTime);   // DateTimeOffset?
    Console.WriteLine(summary.WordCount);       // int?
}
```

`TryGetSummaryInformation` returns `false` when the root storage has no summary-information stream, so it is safe to call on any file. Every property is nullable — an absent or wrong-typed value reads as `null` rather than throwing. The view covers the standard fields: `Title`, `Subject`, `Author`, `Keywords`, `Comments`, `Template`, `LastAuthor`, `RevisionNumber`, `TotalEditTime`, `LastPrinted`, `CreateTime`, `LastSaveTime`, `PageCount`, `WordCount`, `CharacterCount`, `ApplicationName`, and `Security`.

## Pattern 2 — document summary information

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

if (file.TryGetDocumentSummaryInformation(out DocumentSummaryInformation? docSummary))
{
    Console.WriteLine(docSummary.Category);
    Console.WriteLine(docSummary.SlideCount);   // int?
    Console.WriteLine(docSummary.ScaleCrop);     // bool?
}
```

<xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformation> reads the second standard stream, with the presentation- and document-oriented fields: `Category`, `PresentationTarget`, `Bytes`, `LineCount`, `ParagraphCount`, `SlideCount`, `NoteCount`, `HiddenCount`, `MultimediaClipCount`, `ScaleCrop`, and more.

## Pattern 3 — the raw property set

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

// Both typed views expose the underlying set for non-standard properties.
OlePropertySet set = summary.PropertySet;

Console.WriteLine(set.FormatId);
Console.WriteLine(set.CodePage);

if (set.TryGetValue(propertyId: 2, out OlePropertyValue? value))
    Console.WriteLine(value.AsString());
```

When you need a property the typed views do not surface, drop to <xref:Bodu.IO.Compound.PropertySets.OlePropertySet>. It exposes the `FormatId`, `ClassId`, `CodePage`, and `Sections`, and a `TryGetValue(int propertyId, out OlePropertyValue)` lookup. Each <xref:Bodu.IO.Compound.PropertySets.OlePropertyValue> carries `As*` accessors (`AsString`, `AsInt32`, `AsBoolean`, `AsDateTimeOffset`, `AsTimeSpan`) that return the value when the stored type matches and `null` otherwise.

You can also reach a property set on any storage — not just the well-known two on the root — by opening the stream and parsing it:

```csharp
if (storage.TryOpenPropertySet("\x05SummaryInformation", out OlePropertySet? set))
{
    // ...
}
```

`TryOpenPropertySet` opens the named child stream and parses it as a property set in one step, raising <xref:Bodu.IO.Compound.CompoundFileFormatException> only when a stream by that name exists but is not a well-formed property set.

## Where to go next

- [Reading compound files](reading-compound-files.md) — open and navigate to reach a storage.
- [Buffered vs streaming access](streaming-and-buffering.md) — how the underlying stream bytes are read.
- [Bodu.IO.Compound.PropertySets API reference](xref:Bodu.IO.Compound.PropertySets).
