# Bodu.IO.Biff.Samples.BiffBasics

A console walk-through of the BIFF record codec beneath the Excel reader:

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

## Scenario 1 — RecordCensus

**Intent.** The codec's defining property is that an unrecognised record is not an error — a real `.xls` holds records from decades of writers, most of which no consumer cares about.

**What it does.** Walks every record in the workbook stream, counting them by identifier and reporting the version and code page the stream established.

**What to expect.**

```text
--- A record census over a BIFF stream ---

  BOF at       0: Biff8 WorkbookGlobals (build 20314, year 1997)
  BOF at   56082: Biff8 Worksheet (build 20314, year 1997)
  BOF at  458860: Biff8 Worksheet (build 20314, year 1997)
  26912 records, 461504 bytes, version Biff8, code page 1200

  Most frequent record types:
    0x0203 Number                     9236
    0x027E Rk                         4578
    0x0201 Blank                      3314
    0x00BE MulBlank                   3069
    0x0208 Row                        2214
    0x00BD MulRk                      1855
    0x005A (not named by the codec)   1787
    0x00FD LabelSst                    215
```

## Scenario 2 — CellsAndSharedStrings

**Intent.** BIFF8 stores most strings once in a shared table and has cells reference them by index, so a label cell on its own is meaningless — and the table can span `CONTINUE` records, splitting a string mid-character.

**What it does.** Decodes the cell records through their typed accessors and resolves label cells through `BiffSstReader`, which walks the table across its continuation boundaries.

**What to expect.**

```text
--- Cells and the shared string table ---

  SST: 151 unique / 215 references; 0 straddled a CONTINUE boundary
  2 sheets; 151 shared strings
    'Data' at offset 56082
    'Notes' at offset 458860
    R0C0 LABELSST "F11.1  EXCHANGE RATES " (index 61)
    R1C0 LABELSST "Title" (index 28)
    R1C1 LABELSST "A$1=USD" (index 62)
    R1C2 LABELSST "Trade-weighted Index May 1970 = 100" (index 29)
    R1C3 LABELSST "A$1=CNY" (index 63)
    R1C4 LABELSST "A$1=JPY" (index 64)
    R1C5 LABELSST "A$1=EUR" (index 65)
    R1C6 LABELSST "A$1=KRW" (index 66)
    R1C7 LABELSST "A$1=GBP" (index 67)
    R1C8 LABELSST "A$1=SGD" (index 68)
    R1C9 LABELSST "A$1=INR" (index 69)
    R1C10 LABELSST "A$1=THB" (index 70)
  15821 value-bearing cell records in 'Data' (first 12 shown)
```

## Scenario 3 — WriteAndReadBack

**Intent.** A codec that can only read is half a codec, and the round trip is what proves the framing is understood rather than merely tolerated.

**What it does.** Emits a workbook stream record by record — including a shared string table large enough to need splitting — then reads it back and compares.

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

## Scenario 4 — MalformedInput

**Intent.** A codec reading untrusted binary has to fail predictably, and an unsupported version is a different conversation from a corrupt file.

**What it does.** Feeds the reader truncated and structurally invalid streams and reports how each failure is classified.

**What to expect.**

```text
--- Malformed input ---

  truncated header          : format error at offset 36 — The BIFF stream ends with 3 trailing byte(s), too few to form a record header.
  payload overruns buffer   : format error at offset 36 — The record 0x005C at offset 36 declares a 112-byte payload that runs past the end of the data.
  truncated NUMBER payload  : format error at offset n/a — The Number record payload is too short or malformed.
  BIFF4 stream              : unsupported version (marker 0x0409)
  unknown record            : ok, 1 record(s)
  chunked read: 4 complete record(s), 36 bytes consumed, waiting for more data
```

## Layout

```text
Bodu.IO.Biff.Samples.BiffBasics/
  Program.cs                          # runs the scenarios in order
  SampleConsole.cs                    # the What / Why / Expect scenario banner
  Data/sample-biff8.xls               # the committed BIFF8 fixture
  Scenarios/RecordCensus.cs
  Scenarios/CellsAndSharedStrings.cs
  Scenarios/WriteAndReadBack.cs
  Scenarios/MalformedInput.cs
```
