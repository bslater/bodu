---
uid: Bodu.IO.Compound.PropertySets
---

![Bodu.IO.Compound](~/images/hero-io-compound.svg)

## Purpose

**Bodu.IO.Compound.PropertySets** reads and writes OLE property sets — the code-paged, sectioned metadata streams (MS-OLEPS) a compound file stores under well-known names such as `\x05SummaryInformation` and `\x05DocumentSummaryInformation`. It offers two levels: the raw model (<xref:Bodu.IO.Compound.PropertySets.OlePropertySet>, its <xref:Bodu.IO.Compound.PropertySets.OlePropertySection> list, and the `PROPVARIANT`-shaped <xref:Bodu.IO.Compound.PropertySets.OlePropertyValue>) for any property set, and typed views (<xref:Bodu.IO.Compound.PropertySets.SummaryInformation>, <xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformation>) with matching builders for the two standard document-metadata sets.

A property set is a header declaring a class identifier followed by one or two **sections**, each identified by a format identifier (FMTID) and holding properties keyed by an integer property identifier (PID). The first section carries the well-known properties; an optional second, user-defined section carries **custom** properties whose human-readable names live in the section's dictionary. Reading and writing are symmetric — the writer emits every value shape the reader parses, including vector (`VT_VECTOR`) values — so a set read from a real document can be modified and written back.

## Static documentation

- **[Reading property sets](~/guides/io-compound/property-sets.md)** — the typed views, the raw set, and writing sets back.
- **[Authoring compound files](~/guides/io-compound/authoring-compound-files.md)** — embedding an authored set in a new container (pattern 5).
- **[Bodu.IO.Compound introduction](~/docs/io-compound/index.md)** and **[core concepts](~/docs/io-compound/concepts.md)** — where the property-set streams sit in the container.

## Key types

**Raw model**

- <xref:Bodu.IO.Compound.PropertySets.OlePropertySet> — the whole set. Read with `Parse(ReadOnlyMemory<byte>)` or `Read(Stream)` (both throw <xref:Bodu.IO.Compound.CompoundFileFormatException> on a malformed set); author with `new OlePropertySet(formatId, classId, codePage)` and `AddSection`. `FormatId`, `ClassId`, `CodePage`, `Sections`; `TryGetValue(propertyId, out value)` and the indexer `[propertyId]` read the **first** section; `ToArray()`, `WriteTo(Stream)`, `WriteTo(IBufferWriter<byte>)` serialize.
- <xref:Bodu.IO.Compound.PropertySets.OlePropertySection> — one section. `new OlePropertySection(formatId, codePage)`; `FormatId`, `CodePage`, `Properties` (PID → value), `PropertyNames` (PID → name, the dictionary of a user-defined section); `TryGetValue` / indexer, `Set(propertyId, value)`, `SetName(propertyId, name)`, `Remove(propertyId)`, and `GetNamedProperties()` (name → value, joining the dictionary with the values).
- <xref:Bodu.IO.Compound.PropertySets.OlePropertyValue> — one typed value, an immutable record: `Type` (<xref:Bodu.IO.Compound.PropertySets.OlePropertyType>), `IsVector` (the value is an `object[]` of elements), and the boxed CLR `Value`. Factories `Create(int)`, `Create(short)`, `Create(long)`, `Create(double)`, `Create(bool)`, `Create(string, type)` (`AnsiString` by default; pass `UnicodeString` for UTF-16), `Create(DateTimeOffset)` (a FILETIME), and `CreateBlob(byte[])`. Accessors that return `null` on a type mismatch: `AsString()`, `AsInt32()`, `AsInt64()`, `AsBoolean()`, `AsDateTimeOffset()`, `AsTimeSpan()` (a FILETIME read as a duration, as the total-edit-time property uses it), `AsBytes()`, and the generic `GetValueOrDefault<T>()`.
- <xref:Bodu.IO.Compound.PropertySets.OlePropertyType> — the `VT_*` codes: `Empty`, `Null`, `Int16`, `Int32`, `Float32`, `Float64`, `Currency`, `Date`, `BinaryString`, `Boolean`, `Variant`, `Int8`, `UInt8`, `UInt16`, `UInt32`, `Int64`, `UInt64`, `AnsiString`, `UnicodeString`, `FileTime`, `Blob`, `ClipboardData`.

**Typed views**

- <xref:Bodu.IO.Compound.PropertySets.SummaryInformation> — the `\x05SummaryInformation` set (`StreamName`). `Read(Stream)` or `new SummaryInformation(propertySet)`; `PropertySet` exposes the raw set; the nullable fields `Title`, `Subject`, `Author`, `Keywords`, `Comments`, `Template`, `LastAuthor`, `RevisionNumber`, `TotalEditTime`, `LastPrinted`, `CreateTime`, `LastSaveTime`, `PageCount`, `WordCount`, `CharacterCount`, `ApplicationName`, `Security`. On a `CompoundFile`, `TryGetSummaryInformation` / `SetSummaryInformation` read and stage it at the root.
- <xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformation> — the `\x05DocumentSummaryInformation` set (`StreamName`). `Read(Stream)` or the constructor; `Category`, `PresentationTarget`, `Bytes`, `LineCount`, `ParagraphCount`, `SlideCount`, `NoteCount`, `HiddenCount`, `MultimediaClipCount`, `ScaleCrop`, `Manager`, `Company`, `LinksUpToDate`; and `CustomProperties` — the user-defined second section keyed by name, empty when absent. `TryGetDocumentSummaryInformation` / `SetDocumentSummaryInformation` are the `CompoundFile` counterparts.

**Builders**

- <xref:Bodu.IO.Compound.PropertySets.SummaryInformationBuilder> — settable `CodePage` (1252 by default), `Title`, `Subject`, `Author`, `Keywords`, `Comments`, `LastAuthor`, `RevisionNumber`, `ApplicationName`, `CreateTime`, `LastSaveTime`, `PageCount`, `WordCount`, `CharacterCount`; only assigned properties are written. `ToPropertySet()`, `ToArray()`, `WriteTo(Stream)`.
- <xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformationBuilder> — `CodePage`, `Category`, `Manager`, `Company`, `LineCount`, `ParagraphCount`, `SlideCount`, plus `AddCustomProperty(name, value)` for the user-defined section. `ToPropertySet()`, `ToArray()`, `WriteTo(Stream)`.

## Example

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

// Read: the typed view, then drop to the raw set for a property it does not surface.
using CompoundFile file = CompoundFile.OpenRead("report.doc");

if (file.TryGetDocumentSummaryInformation(out DocumentSummaryInformation? info))
{
    Console.WriteLine($"{info.Company} / {info.Category}");
    foreach (KeyValuePair<string, OlePropertyValue> custom in info.CustomProperties)
        Console.WriteLine($"{custom.Key} = {custom.Value.Value} ({custom.Value.Type})");

    OlePropertySet raw = info.PropertySet;
    Console.WriteLine($"{raw.Sections.Count} section(s), code page {raw.CodePage}");
}
```

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

// Write: the standard set through a builder, and a custom set through the raw model.
var summary = new SummaryInformationBuilder
{
    Title = "Quarterly report",
    Author = "Ada",
    CreateTime = DateTimeOffset.UtcNow,
};

var formatId = new Guid("6c1f0b1e-2f43-4a89-9d2e-0f5b8a7c3d10");   // your application's FMTID
var custom = new OlePropertySet(formatId, classId: Guid.Empty, codePage: 1252);
var section = new OlePropertySection(formatId, codePage: 1252);
section.Set(2, OlePropertyValue.Create("Northwind", OlePropertyType.UnicodeString));
section.Set(3, OlePropertyValue.Create(42));
section.Set(4, OlePropertyValue.CreateBlob(new byte[] { 1, 2, 3 }));
section.SetName(2, "Tenant");
custom.AddSection(section);

using (CompoundFile file = CompoundFile.Create("report.cfb"))
{
    file.SetSummaryInformation(new SummaryInformation(summary.ToPropertySet()));
    file.RootStorage.WritePropertySet("AppProperties", custom);
    file.Commit();
}

// Round-trip the custom set from any storage.
using CompoundFile reread = CompoundFile.OpenRead("report.cfb");
if (reread.RootStorage.TryOpenPropertySet("AppProperties", out OlePropertySet? back))
    Console.WriteLine(back.Sections[0].GetNamedProperties()["Tenant"].AsString());   // "Northwind"
```

## Notes

- **Typed versus custom.** The two typed views cover the standard PIDs of the two Office metadata sets and expose `PropertySet` for everything else; `DocumentSummaryInformation.CustomProperties` is the convenient view of the user-defined second section. For a non-standard set — any FMTID, any stream name — use the raw model with `CompoundStorage.TryOpenPropertySet` / `WritePropertySet`.
- **First-section lookups.** `OlePropertySet.TryGetValue` and its indexer consult only the first section; address a second section through `Sections[1]`.
- **Strings and code pages.** `Create(string)` produces an `AnsiString` encoded with the section's `CodePage` (1252 by default on the builders); pass `OlePropertyType.UnicodeString` to store UTF-16 instead.
- **FILETIME is dual-natured.** A `FileTime` value is stored as its raw 100-nanosecond tick count, so it reads as a point in time (`AsDateTimeOffset`) or an elapsed duration (`AsTimeSpan`); `Create(DateTimeOffset)` writes the former.
- **Vectors round-trip by value.** `IsVector` values surface as `object[]`. A variant vector's elements re-emit with a type word inferred from each element's CLR value, so the guarantee for variant elements is value identity rather than byte identity.
- **Errors.** A malformed set throws <xref:Bodu.IO.Compound.CompoundFileFormatException> from `Parse` / `Read` and from `TryOpenPropertySet` when a stream by that name exists but does not parse; `null` arguments throw <xref:System.ArgumentNullException>.
- **See also:** the [property-sets guide](~/guides/io-compound/property-sets.md), the [authoring guide](~/guides/io-compound/authoring-compound-files.md), and the container namespace <xref:Bodu.IO.Compound>.
