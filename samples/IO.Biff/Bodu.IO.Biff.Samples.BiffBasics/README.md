# Bodu.IO.Biff.Samples.BiffBasics

A console walk-through of the BIFF record codec beneath the Excel reader:

| Scenario | Shows |
|---|---|
| `RecordCensus` | `BiffReader.Read` over a real workbook stream: every physical record framed, the `BOF` records marking each substream and establishing the version, and a tally by identifier that includes records the codec does not name. |
| `CellsAndSharedStrings` | The typed accessors: the sheet directory from `BOUNDSHEET`, the shared string table read one string at a time through `BiffSstReader` across its `CONTINUE` records, and the first sheet's cell records (`NUMBER`, `RK`, `MULRK`, `LABELSST`, `LABEL`, `BOOLERR`, `FORMULA`) decoded into values with the version seeded from the globals. |
| `WriteAndReadBack` | `BiffWriter` assembling a BIFF8 workbook stream — globals with fonts, formats, a shared string table and a bound sheet, then a sheet with every cell kind — with `BytesCommitted` supplying the sheet offset, and the result read straight back. |
| `MalformedInput` | The error contract: truncated headers and payloads and short known records as `BiffFormatException`, a BIFF4 stream as `BiffUnsupportedVersionException`, an unknown record as no error, and chunked reading with `isFinalBlock: false`. |

Run it from the repository root:

```bash
dotnet run --project samples/IO.Biff/Bodu.IO.Biff.Samples.BiffBasics
```

Everything runs offline against the committed `Data/sample-biff8.xls` (the
BIFF8 fixture the Excel reader's tests use). The codec has no compound-file
dependency, so the sample extracts the `Workbook` stream with
`Bodu.IO.Compound` before handing the bytes to `BiffReader`; point
`Program.SamplePath` at any other `.xls` — BIFF5 (`Book` stream) or BIFF8
(`Workbook` stream) — to explore your own file.
