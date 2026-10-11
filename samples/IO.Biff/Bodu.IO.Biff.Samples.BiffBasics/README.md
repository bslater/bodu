# Bodu.IO.Biff.Samples.BiffBasics

A console walk-through of the BIFF record codec beneath the Excel reader:

Run it from the repository root:

```bash
dotnet run --project samples/IO.Biff/Bodu.IO.Biff.Samples.BiffBasics
```

Everything runs offline against a workbook the sample writes first, to a
temporary file it deletes once the record stream is read: `SampleWorkbook`
builds a two-sheet BIFF8 workbook of synthetic daily readings with
`BiffWriter` and `Bodu.IO.Compound`'s `CompoundStorageBuilder`, so the sample
carries no data file. The codec has no compound-file dependency, so the sample
extracts the `Workbook` stream with `Bodu.IO.Compound` before handing the bytes
to `BiffReader`; pass `Program.ReadWorkbookStream` any other `.xls` - BIFF5
(`Book` stream) or BIFF8 (`Workbook` stream) - to explore your own file.

## Scenario 1 - RecordCensus

**Intent.** The codec's defining property is that an unrecognised record is not an error - a real `.xls` holds records from decades of writers, most of which no consumer cares about.

**What it does.** Walks every record in the workbook stream, counting them by identifier and reporting the version and code page the stream established. The sample workbook's globals include three records Excel writes that the codec does not name (`0x013D`, `0x009C` and `0x0161`); they are framed and counted like the rest, once each, below the eight most frequent.

**What to expect.**

```text
--- A record census over a BIFF stream ---

  BOF at       0: Biff8 WorkbookGlobals (build 0, year 0)
  BOF at   24324: Biff8 Worksheet (build 0, year 0)
  BOF at   49436: Biff8 Worksheet (build 0, year 0)
  1248 records, 49672 bytes, version Biff8, code page 1200

  Most frequent record types:
    0x00FD LabelSst                    412
    0x00BD MulRk                       400
    0x027E Rk                          400
    0x0204 Label                        10
    0x000A Eof                           3
    0x00E0 Xf                            3
    0x0809 Bof                           3
    0x003C Continue                      2
```

## Scenario 2 - CellsAndSharedStrings

**Intent.** BIFF8 stores most strings once in a shared table and has cells reference them by index, so a label cell on its own is meaningless - and the table can span `CONTINUE` records, splitting a string mid-character.

**What it does.** Decodes the cell records through their typed accessors and resolves label cells through `BiffSstReader`, which walks the table across its continuation boundaries.

**What to expect.**

```text
--- Cells and the shared string table ---

  SST: 412 unique / 412 references; 2 straddled a CONTINUE boundary
  2 sheets; 412 shared strings
    'Data' at offset 24324
    'Notes' at offset 49436
    R0C0 LABEL    "Daily readings, four stations"
    R1C0 LABELSST "Station" (index 0)
    R1C1 LABELSST "North" (index 4)
    R1C2 LABELSST "South" (index 5)
    R1C3 LABELSST "East" (index 6)
    R1C4 LABELSST "West" (index 7)
    R2C0 LABELSST "Units" (index 1)
    R2C1 LABEL    "mm"
    R2C2 LABEL    "mm"
    R2C3 LABEL    "mm"
    R2C4 LABEL    "mm"
    R3C0 LABELSST "Series ID" (index 2)
  1217 value-bearing cell records in 'Data' (first 12 shown)
```

## Scenario 3 - WriteAndReadBack

**Intent.** A codec that can only read is half a codec, and the round trip is what proves the framing is understood rather than merely tolerated.

**What it does.** Emits a workbook stream record by record - including a shared string table large enough to need splitting - then reads it back and compares.

**What to expect.**

```text
--- Writing a stream and reading it back ---

  wrote 316 bytes
  Biff8 WorkbookGlobals substream at 0
    SST: "Product", "Widget", "Gadget"
    sheet 'Sales' -> offset 142
  Biff8 Worksheet substream at 142
    R0C0 = "Product"
    R1C0 = "Widget"
    R1C1 = 1234.5
    R1C2 = 0.25 (RK 0x3FD00000)
    R2C0 = "Gadget"
    R2C1 = True
    R2C2 = formula, cached String
      cached text "cached result"
```

## Scenario 4 - MalformedInput

**Intent.** A codec reading untrusted binary has to fail predictably, and an unsupported version is a different conversation from a corrupt file.

**What it does.** Feeds the reader truncated and structurally invalid streams and reports how each failure is classified.

**What to expect.**

```text
--- Malformed input ---

  truncated header          : format error at offset 52 - The BIFF stream ends with 3 trailing byte(s), too few to form a record header.
  payload overruns buffer   : format error at offset 52 - The record 0x0031 at offset 52 declares a 21-byte payload that runs past the end of the data.
  truncated NUMBER payload  : format error at offset n/a - The Number record payload is too short or malformed.
  BIFF4 stream              : unsupported version (marker 0x0409)
  unknown record            : ok, 1 record(s)
  chunked read: 6 complete record(s), 52 bytes consumed, waiting for more data
```

## Layout

```text
Bodu.IO.Biff.Samples.BiffBasics/
  Program.cs                          # writes the workbook, extracts its stream, runs the scenarios
  SampleConsole.cs                    # the What / Why / Expect scenario banner
  SampleWorkbook.cs                   # builds the BIFF8 workbook the scenarios read
  Scenarios/RecordCensus.cs
  Scenarios/CellsAndSharedStrings.cs
  Scenarios/WriteAndReadBack.cs
  Scenarios/MalformedInput.cs
```
