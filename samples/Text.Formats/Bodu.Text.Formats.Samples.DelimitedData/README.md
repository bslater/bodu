# Bodu.Text.Formats.Samples.DelimitedData

RFC 4180 CSV/TSV via `Bodu.Text.Delimited`: parsing into a document with culture-safe typed
field access, the policy knobs that make dirty real-world files importable, formatting back
out (including dialect conversion), and the forward-only streaming reader/writer for files
too large to materialize. Everything runs offline against the committed `Data/trades.csv`
plus small inline snippets of deliberately dirty input.

> Note on namespaces: the sample's root namespace is `Bodu.Samples.Text.Formats.DelimitedData`
> rather than `Bodu.Text.Formats.Samples.*`. From inside any namespace under `Bodu.Text`, the
> simple name `Delimited` resolves to the *namespace* `Bodu.Text.Delimited` instead of the
> facade class, which breaks `Delimited.Parse(...)`. Consumers with their own namespace roots
> are unaffected; if your code lives under `Bodu.Text`, qualify the facade or alias it.

```bash
dotnet run --project samples/Text.Formats/Bodu.Text.Formats.Samples.DelimitedData
```

## Scenario 1 — ParseAndTypedGetters

**Intent.** Show the primary read workflow: parse once into a `DelimitedDocument`, then
access fields *by header name* with typed getters. `GetValue<T>` accepts any
`ISpanParsable<T>` and always parses with `InvariantCulture`, so numeric and timestamp
imports never silently change with the machine's locale — the classic CSV bug this API
design eliminates.

**What it does.** Parses `Data/trades.csv` (five trades; one symbol quoted because it
contains a comma), prints the discovered headers, and shows the quoted field arriving
unwrapped. It then aggregates a total notional by combining `GetValue<int>("quantity")` and
`GetValue<decimal>("price")` per row, measures the trading window from two
`GetValue<DateTimeOffset>` reads, and demonstrates `TryGetValue<int>` returning `false` on a
non-numeric field instead of throwing.

**What to expect.**

```text
headers: trade_id, symbol, side, quantity, price, executed_at (5 rows)
quoted field: 'F, ordinary'
total notional: 73,164.60 across 34 minutes of trading
TryGetValue<int>("symbol") on 'AAPL' -> False
```

**APIs demonstrated.** `Delimited.Parse`, `DelimitedDocument.Headers` / `.Rows`, row indexers
by header name, `DelimitedRow.GetValue<T>` / `TryGetValue<T>` (`ISpanParsable`,
`InvariantCulture`).

## Scenario 2 — PolicyBehaviors

**Intent.** Real exports break the RFC contract constantly — short rows, extra columns,
stray characters after a closing quote. Show that the strict defaults surface these as
errors, and exactly which policy admits which defect, so a lenient import is a deliberate,
scoped decision rather than a global "ignore errors" flag.

**What it does.** Parses input whose second row is short and third row is long: the default
`FieldCountBehavior.Strict` throws `DelimitedFormatException` naming the offending line,
while `Ragged` accepts all three rows with their true field counts. It then parses input with
a stray character after a closing quote (`"Bolt"x`): the default
`MalformedRecordBehavior.Throw` rejects it, while `SkipRecord` discards the remainder of the
malformed record but keeps the fields already parsed — leaving a *short* row, which is why
lenient ingestion pairs `SkipRecord` with `Ragged`.

**What to expect.** Each policy's observable outcome, including the truncated middle row
(field count 2) under `SkipRecord+Ragged`:

```text
Strict (default) : Row on line 3 has 2 field(s) but the header row defines 3.
Ragged           : accepted 3 rows with field counts [3, 2, 4]
Throw (default)  : Unexpected character 'x' after closing quote on line 3.
SkipRecord+Ragged: kept 3 rows, field counts [3, 2, 3] (malformed row truncated)
```

**APIs demonstrated.** `DelimitedParseOptions.FieldCountBehavior`
(`DelimitedFieldCountBehavior.Strict` / `.Ragged`), `.MalformedRecordBehavior`
(`DelimitedMalformedRecordBehavior.Throw` / `.SkipRecord`), `DelimitedFormatException`.

## Scenario 3 — FormatAndRoundTrip

**Intent.** Show the write direction: `Delimited.Format` re-emits a document as RFC 4180
text, quoting exactly the fields that need it — and because the same options type drives
both directions, converting between dialects (CSV → TSV) is just a parse plus a format.

**What it does.** Round-trips `Data/trades.csv` through `Format` → `Parse` and verifies the
shape and the comma-containing field survive. It prints the re-emitted last row to show
selective quoting (`"F, ordinary"` is quoted; nothing else is), then formats the same
document with `Delimiter = '\t'` to produce TSV.

**What to expect.**

```text
round trip: shape preserved -> True, quoted comma field preserved -> True
last row re-emitted: 1005,"F, ordinary",Buy,1000,12.3400,2026-03-02T15:04:18Z
as TSV: 1001 <TAB> AAPL <TAB> Buy <TAB> 100 <TAB> 187.4500 <TAB> 2026-03-02T14:30:05Z
```

**APIs demonstrated.** `Delimited.Format(document)` / `Format(document, options)`, selective
RFC 4180 quoting, dialect retargeting via `DelimitedParseOptions.Delimiter`.

## Scenario 4 — StreamingReaderWriter

**Intent.** Show the constant-memory surface for large files: `DelimitedReader` is a
forward-only cursor holding one row at a time, and `DelimitedWriter` emits rows as they are
produced. Composed, they form a filter pipeline that never builds a `DelimitedDocument`.

**What it does.** Opens `Data/trades.csv` through `Delimited.CreateReader`, loops `Read()`,
writes the header once from `reader.Headers`, keeps only `Buy` rows (accumulating their
notional along the way), and forwards them to a `DelimitedWriter` over a `StringWriter`. The
filtered output shows the writer re-quoting `"F, ordinary"` on the way out.

**What to expect.**

```text
streamed 5 rows, kept 3 buys (notional 41,135.00); reader stopped at line 7
filtered output:
  | trade_id,symbol,side,quantity,price,executed_at
  | 1001,AAPL,Buy,100,187.4500,2026-03-02T14:30:05Z
  | 1003,BRK.B,Buy,25,402.0000,2026-03-02T14:32:40Z
  | 1005,"F, ordinary",Buy,1000,12.3400,2026-03-02T15:04:18Z
```

**APIs demonstrated.** `Delimited.CreateReader` / `CreateWriter`, `DelimitedReader.Read()` /
`.Headers` / `.Fields` / `.LineNumber`, `DelimitedWriter.WriteHeader` / `.WriteRow`,
write-side quoting.

## Layout

```text
Bodu.Text.Formats.Samples.DelimitedData/
  Program.cs                          # runs the scenarios in order
  Data/trades.csv                     # committed input (5 trades, one quoted symbol)
  Scenarios/ParseAndTypedGetters.cs
  Scenarios/PolicyBehaviors.cs
  Scenarios/FormatAndRoundTrip.cs
  Scenarios/StreamingReaderWriter.cs
```

## Related

- `Bodu.Text.Formats.Samples.ConfigFiles` — the INI and DotEnv document formats from the
  same package.
- Guides: `docs/guides/formats/`.
