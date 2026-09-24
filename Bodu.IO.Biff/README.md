# Bodu.IO.Biff

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

A low-level **codec for the Excel Binary Interchange File Format (BIFF)** record streams found
inside legacy `.xls` workbooks, covering **BIFF5** and **BIFF8**.

It understands how BIFF is *encoded*, not what a workbook *means*. There is no compound-file
container, no workbook or cell model, and no formula evaluation — it frames records, resolves
their strings and numbers, and hands you the values. That separation is deliberate: it is the
substrate beneath [`Bodu.Formats.Excel.Binary`](https://www.nuget.org/packages/Bodu.Formats.Excel.Binary),
in the same relation `Bodu.IO.Pst` has to `Bodu.Formats.Outlook.Pst`.

```csharp
using Bodu.IO.Biff;

// BiffReader is a forward-only ref struct over a span — no allocation per record.
var reader = new BiffReader(workbookStreamBytes);

while (reader.Read())
{
    switch (reader.RecordType)
    {
        case BiffRecordType.Bof:
            BiffBofRecord bof = reader.GetBof();
            Console.WriteLine($"{bof.Version} {bof.SubstreamType}");
            break;

        case BiffRecordType.BoundSheet:
            BiffBoundSheetRecord sheet = reader.GetBoundSheet();
            Console.WriteLine($"{sheet.Name} at 0x{sheet.StreamOffset:X} ({sheet.State})");
            break;

        case BiffRecordType.Number:
            BiffNumberRecord number = reader.GetNumber();
            Console.WriteLine($"({number.Row},{number.Column}) = {number.Value}");
            break;

        // An unknown record is never an error — its identifier and raw payload are still exposed.
        default:
            Process(reader.RecordId, reader.ValueSpan);
            break;
    }
}
```

## Capabilities

- **Forward-only, allocation-free framing.** `BiffReader` is a `ref struct` over a
  `ReadOnlySpan<byte>`. Every record exposes its identifier (`RecordId` / `RecordType`), length,
  start offset, and raw `ValueSpan`; an unrecognized record is data, never a failure.
- **Resumable across buffers.** The `(data, isFinalBlock, state)` constructor plus
  `CurrentState` / `BytesConsumed` let a caller feed the stream in chunks, so a workbook need not
  be buffered whole.
- **Version and code page established from the stream itself** — `BOF` sets `Version`, `CODEPAGE`
  sets `CodePage` — so BIFF5 byte strings decode in the workbook's own encoding rather than a
  guess.
- **Typed accessors for the common structural and cell records**: `GetBof`, `GetBoundSheet`,
  `GetDimensions`, `GetRow`, `GetNumber`, `GetRk`, `GetMulRk`, `GetLabel`, `GetLabelSst`,
  `GetRString`, `GetBoolErr`, `GetBlank`, `GetMulBlank`, `GetFormula`, `GetString`, `GetXf`,
  `GetFormat`, `GetFont`, `GetCodePage`, `GetDateMode`, `GetFilePass`, `GetSstHeader` — each
  returning a typed `Biff*Record`.
- **Shared string table across `CONTINUE` boundaries.** `BiffSstReader` walks the SST even when a
  single string is split across records, reporting `IsFragmented` and exposing `GetString` /
  `CopyTo`.
- **Span-backed text.** `BiffString` is a view over BIFF8 Unicode or BIFF5 code-page text; no
  string is materialized until you ask for one.
- **RK numbers** encode and decode through `BiffRk` (`Decode` / `TryEncode`), including the
  ×100 and integer-flag variants.
- **Writing.** `BiffWriter` over an `IBufferWriter<byte>` emits conformant BIFF5 or BIFF8:
  `WriteRecord` / `WriteContinuedRecord`, the `BOF`/`EOF` and cell and globals writers, and
  `WriteSst` with automatic `CONTINUE` splitting.
- **Diagnosable failures.** `BiffFormatException` carries the byte `Offset`;
  `BiffUnsupportedVersionException` carries the `RawVersion` it saw. Encrypted workbooks are
  identified (`FILEPASS` / `BiffEncryptionType`) rather than silently mis-parsed.

## Runnable sample

The repository ships an offline, `dotnet run`-able sample for this package under
[`samples/IO.Biff/`](https://github.com/bslater/bodu/tree/master/samples/IO.Biff).

## Out of scope

The compound-file (OLE2) container that holds the `Workbook` stream — use
[`Bodu.IO.Compound`](https://www.nuget.org/packages/Bodu.IO.Compound). A workbook or cell object
model, number-format application, and formula evaluation — use
[`Bodu.Formats.Excel.Binary`](https://www.nuget.org/packages/Bodu.Formats.Excel.Binary), which is
built on this package. Decrypting a password-protected workbook is not supported; such a stream is
reported, not read.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
