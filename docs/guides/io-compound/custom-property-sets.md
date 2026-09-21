---
title: Authoring custom property sets
---

# Authoring custom property sets

[Reading property sets](property-sets.md) covers the two standard metadata streams and their typed views. This guide is the write side, and the part the typed builders do not reach: the raw <xref:Bodu.IO.Compound.PropertySets.OlePropertySet> model, user-defined named properties in a document's second section, and property-set streams of your own on any storage.

An OLE property set (MS-OLEPS) is the serialized form of the COM `IPropertyStorage` interface. It begins with a header declaring a class identifier and one or two **sections**; each section is identified by a format identifier (FMTID), carries a code page, and maps integer property identifiers (PIDs) to typed values. The first section holds the well-known properties; an optional second section — the *user-defined* section, FMTID `D5CDD505-2E9C-101B-9397-08002B2CF9AE` — holds custom properties whose human-readable names live in the section's dictionary. Office shows those names on the *Custom* tab of a document's properties.

The samples run against `sample1.doc` from the [runnable compound-file sample](../../samples/io-compound.md), a Word 2000 document copied as `report.doc`; the quoted output is what they print.

## The model

| Type | Role | Members |
|---|---|---|
| <xref:Bodu.IO.Compound.PropertySets.OlePropertySet> | The whole stream: header plus sections. | `.ctor(formatId, classId, codePage)` · `FormatId` · `ClassId` · `CodePage` · `Sections` · `AddSection` · `TryGetValue(pid, out value)` / `this[pid]` (first section) · `Parse(ReadOnlyMemory<byte>)` · `Read(Stream)` · `ToArray()` · `WriteTo(Stream)` · `WriteTo(IBufferWriter<byte>)` |
| <xref:Bodu.IO.Compound.PropertySets.OlePropertySection> | One section: PID → value, plus the optional name dictionary. | `.ctor(formatId, codePage)` · `FormatId` · `CodePage` · `Properties` · `PropertyNames` · `Set(pid, value)` · `SetName(pid, name)` · `Remove(pid)` · `TryGetValue` / `this[pid]` · `GetNamedProperties()` |
| <xref:Bodu.IO.Compound.PropertySets.OlePropertyValue> | One typed value, the managed `PROPVARIANT`. | `Type` · `IsVector` · `Value` · `Create(int / short / long / double / bool / string[, type] / DateTimeOffset)` · `CreateBlob(byte[])` · `AsString` / `AsInt32` / `AsInt64` / `AsBoolean` / `AsDateTimeOffset` / `AsTimeSpan` / `AsBytes` · `GetValueOrDefault<T>()` |
| <xref:Bodu.IO.Compound.PropertySets.OlePropertyType> | The `VT_*` type code. | See [the type table](#property-types). |

Two conventions make a property-set stream findable. The **name** starts with the control character U+0005 — `\x05SummaryInformation` and `\x05DocumentSummaryInformation` for the standard pair, exposed as `SummaryInformation.StreamName` and `DocumentSummaryInformation.StreamName` — a prefix the CFB sort order places first and that ordinary names never use. Your own sets may follow the same convention or use any valid entry name. The **format identifier** of the first section tells a reader what the PIDs mean: `F29F85E0-4FF9-1068-AB91-08002B27B3D9` for summary information, `D5CDD502-2E9C-101B-9397-08002B2CF9AE` for document summary information, your own GUID for a set you define.

## Pattern 1 — write user-defined properties with the builder

<xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformationBuilder> authors the built-in section from typed fields and adds the user-defined section for you: `AddCustomProperty(name, value)` assigns names to PIDs from 2 upward in insertion order and builds the dictionary. On a writable file, `SetDocumentSummaryInformation` stages the result as the well-known root stream.

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

File.Copy("report.doc", "report-tagged.doc", overwrite: true);
using CompoundFile file = CompoundFile.Open("report-tagged.doc", FileMode.Open, FileAccess.ReadWrite);

var builder = new DocumentSummaryInformationBuilder
{
    Category = "Reports",
    Company = "Bodu Pty. Ltd.",
};
builder.AddCustomProperty("ReviewedBy", OlePropertyValue.Create("Ada"));
builder.AddCustomProperty("Reviewed", OlePropertyValue.Create(true));
builder.AddCustomProperty("Revision", OlePropertyValue.Create(7));
builder.AddCustomProperty("ReviewedOn", OlePropertyValue.Create(new DateTimeOffset(2026, 9, 9, 8, 30, 0, TimeSpan.Zero)));

file.SetDocumentSummaryInformation(new DocumentSummaryInformation(builder.ToPropertySet()));
file.Commit();
```

The two builders cover these fields (PID in parentheses); only the fields you set are written, and each builder's `CodePage` (default 1252) encodes its strings:

| <xref:Bodu.IO.Compound.PropertySets.SummaryInformationBuilder> | <xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformationBuilder> |
|---|---|
| `Title` (2) · `Subject` (3) · `Author` (4) · `Keywords` (5) · `Comments` (6) · `LastAuthor` (8) · `RevisionNumber` (9) · `CreateTime` (12) · `LastSaveTime` (13) · `PageCount` (14) · `WordCount` (15) · `CharacterCount` (16) · `ApplicationName` (18) | `Category` (2) · `LineCount` (5) · `ParagraphCount` (6) · `SlideCount` (7) · `Manager` (14) · `Company` (15) · `AddCustomProperty(name, value)` → the user-defined section |

Both expose `ToPropertySet()`, `ToArray()`, and `WriteTo(Stream)`. Fields the readers surface but the builders do not (`Template`, `TotalEditTime`, `LastPrinted`, `Security`, `PresentationTarget`, …) are set on the raw section by PID, as in the next pattern.

> [!NOTE]
> `SetDocumentSummaryInformation` **replaces** the stream. If the document already carries a document-summary stream with fields you want to keep, read it first (`TryGetDocumentSummaryInformation`), edit its `PropertySet`, and write that back — see Pattern 3 below.

## Pattern 1, by hand — assemble the sections yourself

The builder is a convenience over three constructors. Building the set directly lets you choose the string type per value, add blobs and floating-point values, and pick your own PIDs:

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

var documentSummary = new Guid("D5CDD502-2E9C-101B-9397-08002B2CF9AE");   // FMTID_DocSummaryInformation
var userDefined = new Guid("D5CDD505-2E9C-101B-9397-08002B2CF9AE");       // FMTID_UserDefinedProperties

var set = new OlePropertySet(formatId: documentSummary, classId: Guid.Empty, codePage: 1252);

var builtIn = new OlePropertySection(documentSummary, codePage: 1252);
builtIn.Set(2, OlePropertyValue.Create("Reports"));                         // PID 2 = Category
builtIn.Set(15, OlePropertyValue.Create("Bodu Pty. Ltd."));                 // PID 15 = Company
set.AddSection(builtIn);

var custom = new OlePropertySection(userDefined, codePage: 1252);
custom.SetName(2, "ReviewedBy");                                            // PIDs 2.. are yours; 0 and 1 are reserved
custom.Set(2, OlePropertyValue.Create("Ada", OlePropertyType.UnicodeString));
custom.SetName(3, "Revision");
custom.Set(3, OlePropertyValue.Create(7));
custom.SetName(4, "Weight");
custom.Set(4, OlePropertyValue.Create(0.75));
custom.SetName(5, "Thumbnail");
custom.Set(5, OlePropertyValue.CreateBlob(new byte[] { 0x89, 0x50, 0x4E, 0x47 }));
set.AddSection(custom);

Console.WriteLine($"{set.Sections.Count} sections, {custom.Properties.Count} custom properties, names: {string.Join(", ", custom.PropertyNames.Values)}");

using CompoundFile file = CompoundFile.Open("report-tagged.doc", FileMode.Open, FileAccess.ReadWrite);
file.RootStorage.WritePropertySet(DocumentSummaryInformation.StreamName, set);
file.Commit();

// 2 sections, 4 custom properties, names: ReviewedBy, Revision, Weight, Thumbnail
```

<xref:Bodu.IO.Compound.CompoundStorage.WritePropertySet*> serializes any set into a named child stream of any storage, creating or replacing it in the staging tree; `Commit` persists it. PIDs 0 (the dictionary) and 1 (the code page) are reserved in every section, so keep custom PIDs at 2 or above. The `OlePropertySet` constructor's `codePage` and `classId` are header-level values; the section's own `CodePage` is what encodes its `AnsiString` values.

## Pattern 2 — read it back

<xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformation.CustomProperties> joins the user-defined section's names to its values; the raw sections remain reachable for anything else.

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

using CompoundFile file = CompoundFile.OpenRead("report-tagged.doc");

// The typed view joins names to values for you.
if (file.TryGetDocumentSummaryInformation(out DocumentSummaryInformation? summary))
{
    Console.WriteLine($"category={summary.Category} company={summary.Company}");
    foreach ((string name, OlePropertyValue value) in summary.CustomProperties)
        Console.WriteLine($"  {name} ({value.Type}) = {Render(value)}");

    int revision = summary.CustomProperties["Revision"].GetValueOrDefault<int>();
    Console.WriteLine($"revision {revision}");
}

// Or walk the raw sections.
if (file.RootStorage.TryOpenPropertySet(DocumentSummaryInformation.StreamName, out OlePropertySet? set))
{
    foreach (OlePropertySection section in set.Sections)
        Console.WriteLine($"  section {section.FormatId}: {section.Properties.Count} properties, {section.PropertyNames.Count} names");

    IReadOnlyDictionary<string, OlePropertyValue> named = set.Sections[1].GetNamedProperties();
    Console.WriteLine($"  ReviewedBy = {named["ReviewedBy"].AsString()}");
}

static string Render(OlePropertyValue value) =>
    value.AsString()
    ?? value.AsDateTimeOffset()?.ToString("u")
    ?? value.AsInt64()?.ToString()
    ?? value.AsBoolean()?.ToString()
    ?? (value.AsBytes() is byte[] bytes ? Convert.ToHexString(bytes) : value.Value?.ToString() ?? "(null)");

// After the builder version of Pattern 1:
// category=Reports company=Bodu Pty. Ltd.
//   ReviewedBy (AnsiString) = Ada
//   Reviewed (Boolean) = True
//   Revision (Int32) = 7
//   ReviewedOn (FileTime) = 2026-09-09 08:30:00Z
// revision 7
//   section d5cdd502-2e9c-101b-9397-08002b2cf9ae: 3 properties, 0 names
//   section d5cdd505-2e9c-101b-9397-08002b2cf9ae: 5 properties, 4 names
//   ReviewedBy = Ada
//
// After the hand-assembled version:
//   ReviewedBy (UnicodeString) = Ada
//   Revision (Int32) = 7
//   Weight (Float64) = 0.75
//   Thumbnail (Blob) = 89504E47
```

The section's `Properties` count is one higher than the names you gave because the code-page property (PID 1) is written into every section. <xref:Bodu.IO.Compound.CompoundStorage.TryOpenPropertySet*> returns `false` when no stream by that name exists and throws <xref:Bodu.IO.Compound.CompoundFileFormatException> only when one exists but is not a well-formed property set. The `As*` accessors return `null` rather than throwing on a type that does not match; `GetValueOrDefault<T>` returns `default` — so ask for the CLR type the value was boxed as (`int` for `Int32`, `long` for `Int64` and `FileTime`, `bool`, `double`, `string`, `byte[]`).

## Pattern 3 — round-trip a real document's summary with one field changed

Editing what a real writer produced means preserving everything you did not touch — the code page, the application name, the timestamps, and the *string type* the writer chose. Read the set, change one value on its first section, and write the same set back.

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

File.Copy("report.doc", "report-retitled.doc", overwrite: true);

using (CompoundFile file = CompoundFile.Open("report-retitled.doc", FileMode.Open, FileAccess.ReadWrite))
{
    if (!file.TryGetSummaryInformation(out SummaryInformation? summary))
        return;

    Console.WriteLine($"before: title='{summary.Title}' author='{summary.Author}' app='{summary.ApplicationName}' created={summary.CreateTime:u}");

    OlePropertySection section = summary.PropertySet.Sections[0];
    OlePropertyValue existing = section[2]!;                                        // PID 2 = Title
    section.Set(2, OlePropertyValue.Create("Sample document (reviewed)", existing.Type));   // keep the writer's string type

    file.SetSummaryInformation(summary);   // every other property, and the code page, travel unchanged
    file.Commit();
}

using CompoundFile check = CompoundFile.OpenRead("report-retitled.doc");
if (check.TryGetSummaryInformation(out SummaryInformation? after))
    Console.WriteLine($"after:  title='{after.Title}' author='{after.Author}' app='{after.ApplicationName}' created={after.CreateTime:u} codepage={after.PropertySet.CodePage}");

// before: title='Sample document created with MS Word' author='steve' app='Microsoft Word 9.0' created=2001-02-28 23:54:00Z
// after:  title='Sample document (reviewed)' author='steve' app='Microsoft Word 9.0' created=2001-02-28 23:54:00Z codepage=1252
```

`Set` on the parsed section mutates the same <xref:Bodu.IO.Compound.PropertySets.OlePropertySet> the record wraps, so `SetSummaryInformation(summary)` writes your edit alongside every untouched value. The writer re-emits every value shape the reader parses — including vector values such as a document summary's heading pairs — so a set read from any real document can be written back; the one nuance is that variant-vector elements re-emit with a type word inferred from the CLR value (a `FileTime`-tagged element surfaces as `long` and re-emits as `Int64`), giving value rather than byte identity for those.

## Pattern 4 — a property set of your own on any storage

Nothing ties property sets to the root or to the two standard names. Allocate a format identifier for your application, put the set in whatever storage suits, and read it back with the same `TryOpenPropertySet`:

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

var myFormat = new Guid("7C3B9A52-1B1E-4C5A-9B4D-2F3E1D0C9A11");   // an FMTID you allocate for your application

var set = new OlePropertySet(myFormat, classId: Guid.Empty, codePage: 65001);
var section = new OlePropertySection(myFormat, codePage: 65001);
section.SetName(2, "Pipeline");
section.Set(2, OlePropertyValue.Create("nightly-ingest"));          // AnsiString, encoded with code page 65001 (UTF-8)
section.SetName(3, "Sequence");
section.Set(3, OlePropertyValue.Create(42L));
set.AddSection(section);

using CompoundFile file = CompoundFile.Open("report-tagged.doc", FileMode.Open, FileAccess.ReadWrite);
CompoundStorage storage = file.RootStorage.TryOpenStorage("Bodu", out CompoundStorage? existing) ? existing : file.RootStorage.CreateStorage("Bodu");
storage.WritePropertySet("BoduPipeline", set);
file.Commit();

if (storage.TryOpenPropertySet("BoduPipeline", out OlePropertySet? read))
{
    foreach ((string name, OlePropertyValue value) in read.Sections[0].GetNamedProperties())
        Console.WriteLine($"{name} = {value.Value}");
}

// Pipeline = nightly-ingest
// Sequence = 42
```

A name dictionary is legal in any section, not just the user-defined one, so `GetNamedProperties()` works here too. Code page 65001 makes `AnsiString` values UTF-8 on the wire; `UnicodeString` is the alternative that never depends on the section code page. The writer encodes code pages 1200, 1201, and 65001 exactly and every other value — 1252 included — as Latin-1, so for text outside ASCII use one of those three or `UnicodeString`.

## Bytes in and out without a container

The set serializes independently of any file — useful for the builder authoring path, for tests, and for property sets that travel outside a compound file:

```csharp
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.PropertySets;

var builder = new SummaryInformationBuilder { Title = "Quarterly report", Author = "Ada", WordCount = 1280 };

byte[] bytes = builder.ToArray();                       // the serialized stream payload
OlePropertySet parsed = OlePropertySet.Parse(bytes);    // ...and back
Console.WriteLine($"{bytes.Length} bytes; title={parsed[2]?.AsString()} words={parsed[15]?.AsInt32()}");

using var stream = new MemoryStream();
parsed.WriteTo(stream);                                 // Stream and IBufferWriter<byte> overloads
stream.Position = 0;
SummaryInformation summary = SummaryInformation.Read(stream);
Console.WriteLine(summary.Title);

// Embed through the builder path when authoring from scratch.
var root = CompoundStorageBuilder.CreateRoot();
root.AddStream(SummaryInformation.StreamName, bytes);
Console.WriteLine(root.ToArray().Length);

// 144 bytes; title=Quarterly report words=1280
// Quarterly report
// 2560
```

`Parse` takes the bytes you already hold; `Read(Stream)` consumes a stream to its end. Both throw <xref:Bodu.IO.Compound.CompoundFileFormatException> for data that is not a well-formed property set.

## Property types

<xref:Bodu.IO.Compound.PropertySets.OlePropertyType> carries the `VT_*` codes. The `Create*` factories cover the common scalars; anything else is authored by initializing an <xref:Bodu.IO.Compound.PropertySets.OlePropertyValue> directly with `Type` and a `Value` of the CLR shape the writer expects.

| `OlePropertyType` | Code | `VT_*` | CLR `Value` | Factory |
|---|---|---|---|---|
| `Int16` / `UInt16` | 2 / 18 | `I2` / `UI2` | `short` / `ushort` | `Create(short)` |
| `Int32` / `UInt32` | 3 / 19 | `I4` / `UI4` | `int` / `uint` | `Create(int)` |
| `Int64` / `UInt64` | 20 / 21 | `I8` / `UI8` | `long` / `ulong` | `Create(long)` |
| `Int8` / `UInt8` | 16 / 17 | `I1` / `UI1` | `sbyte` / `byte` | — |
| `Float32` / `Float64` | 4 / 5 | `R4` / `R8` | `float` / `double` | `Create(double)` |
| `Currency` | 6 | `CY` | `decimal` (scaled by 10,000 on the wire) | — |
| `Date` | 7 | `DATE` | OLE automation date | — |
| `Boolean` | 11 | `BOOL` | `bool` (`-1` / `0` on the wire) | `Create(bool)` |
| `AnsiString` / `BinaryString` | 30 / 8 | `LPSTR` / `BSTR` | `string`, encoded with the section code page | `Create(string)` (default) |
| `UnicodeString` | 31 | `LPWSTR` | `string`, UTF-16 | `Create(string, OlePropertyType.UnicodeString)` |
| `FileTime` | 64 | `FILETIME` | `long` ticks — read as `AsDateTimeOffset()` or `AsTimeSpan()` | `Create(DateTimeOffset)` |
| `Blob` / `ClipboardData` | 65 / 71 | `BLOB` / `CF` | `byte[]` | `CreateBlob(byte[])` |
| `Variant` | 12 | `VARIANT` | element type inferred from the CLR value (vectors only) | — |
| `Empty` / `Null` | 0 / 1 | `EMPTY` / `NULL` | `null` | — |

A **vector** (`VT_VECTOR`) is an `OlePropertyValue` with `IsVector = true` and an `object?[]` of element values; the writer rejects an empty-typed or null-typed vector, and any type outside the table above, with <xref:Bodu.IO.Compound.CompoundFileSerializationException>:

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

var section = new OlePropertySection(new Guid("D5CDD505-2E9C-101B-9397-08002B2CF9AE"), 1252);
section.SetName(2, "Tags");
section.Set(2, new OlePropertyValue
{
    Type = OlePropertyType.AnsiString,
    IsVector = true,
    Value = new object?[] { "finance", "q3", "draft" },
});

var set = new OlePropertySet(section.FormatId, Guid.Empty, 1252);
set.AddSection(section);
OlePropertySet parsed = OlePropertySet.Parse(set.ToArray());
Console.WriteLine(string.Join("|", (object?[])parsed[2]!.Value!));

section.Set(3, new OlePropertyValue { Type = OlePropertyType.Empty, IsVector = true, Value = Array.Empty<object?>() });
try
{
    set.ToArray();
}
catch (CompoundFileSerializationException ex)
{
    Console.WriteLine(ex.Message);
}

// finance|q3|draft
// The property value type 'Empty' cannot be serialized to an OLE property set.
```

`WritePropertySet` raises the same exception for an invalid stream name.

## Where to go next

- [Reading property sets](property-sets.md) — the typed views and the read side of `TryGet*` / `TryOpenPropertySet`.
- [Editing an existing container in place](editing-in-place.md) — the update-mode session these patterns write through, `Commit`, and `Revert`.
- [Authoring compound files](authoring-compound-files.md) — embedding a serialized set through the builder path.
- [Bodu.IO.Compound.PropertySets API reference](xref:Bodu.IO.Compound.PropertySets).
- [Bodu.IO.Compound guides](index.md) — every guide in this topic.
