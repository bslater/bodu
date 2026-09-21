# Bodu.Text.Formats.Samples.DelimitedData

RFC 4180 CSV/TSV via **`Bodu.Text.Delimited`** (referenced through the `Bodu.Text.Formats`
umbrella). Everything runs offline against the committed `Data/trades.csv`.

```bash
dotnet run --project samples/Text.Formats/Bodu.Text.Formats.Samples.DelimitedData
```

## Scenario 1 — ParseAndTypedGetters

**Intent.** Show the two read layers and the two traps CSV sets: quoted fields containing the
delimiter, and culture-dependent scalar parsing.

**What it does.** Parses the trades file into the read-only `DelimitedDocument`, reads a field
whose value contains a comma, then binds the whole file onto a typed `Trade` record through
`NamingPolicy.SnakeCaseLower` and aggregates notional value across the records.

**What to expect.**

```text
--- Parsing and typed records over a CSV ---
  What   : Parses the committed trades file through the read-only document, reports its headers and row count, reads
           a field whose value contains a comma, then binds the whole file onto a typed record class and aggregates
           over it.
  Why    : CSV is the format most often parsed by splitting on commas, and that works until a field contains one.
           RFC 4180's answer is quoting, which means a correct reader is a small state machine rather than a split -
           and getting that wrong shifts every later column in the row, silently. The typed layer carries the other
           trap: every CSV value is text, and parsing a decimal or a timestamp under the machine's current culture
           makes the same file mean different things on different machines. Binding parses with invariant culture
           for exactly that reason, once, rather than at each call site.
  Expect : The quoted field comes back as one value with its comma intact and its quotes removed - the quotes were
           structure, not content. The aggregate works on real decimals and timestamps rather than strings, so
           summing and subtracting are ordinary arithmetic, and the result does not depend on the machine's locale.

  headers: trade_id, symbol, side, quantity, price, executed_at (5 rows)  (the header row names the fields, so each record is addressed by name rather than by position)
  quoted field: 'F, ordinary'  (one field containing a comma, not two fields - splitting on commas would have shifted every later column)
  total notional: 73,164.60 across 34 minutes of trading  (real decimals and timestamps, parsed once with invariant culture - the same file gives this answer on any machine)
```

**APIs demonstrated.** `DelimitedDocument.Parse` / `Headers` / `RootElement`,
`DelimitedSerializer.Deserialize<T>`, `DelimitedSerializerOptions.PropertyNamingPolicy`.

## Scenario 2 — PolicyBehaviors

**Intent.** Show why reader leniency is a policy rather than a behaviour: the right response to
a broken row depends on where the file came from.

**What it does.** Parses a file with a short row and a long row under the strict defaults and
again with `FieldCountBehavior.Ragged`, then repeats for a row with stray characters after a
closing quote, first strictly and then with `MalformedRecordBehavior.SkipRecord`.

**What to expect.**

```text
--- Policy knobs for input that breaks the contract ---
  What   : Parses a file with a short row and a long row under the strict defaults and again with ragged field
           counts allowed, then does the same for a structurally malformed quoted field, first strictly and then
           with the malformed record skipped.
  Why    : Real CSV files arrive with rows the header does not describe and quotes that do not close, and there is
           no universally right response - which is why this is a policy rather than a behaviour. A file from a
           known generator should fail loudly, because a ragged row there means a bug upstream and continuing past
           it imports wrong data. A one-off file exported by hand is the opposite case, where stopping on row 4,000
           of 50,000 is not a service to anybody. Making the strict behaviour the default matters: a reader that is
           quietly permissive turns a broken file into a plausible-looking import, and the damage surfaces much
           later.
  Expect : Both strict parses throw. The second reports a field-count mismatch rather than a quoting error, because
           the stray characters after the closing quote collapse that row to two fields and the count check fires
           first - the message names the symptom the reader reached, not the root cause. The lenient parses accept
           the same input and keep the rows, with the field counts showing exactly what was preserved. Note that
           skipping a malformed record keeps the fields already parsed, leaving a short row - which is why that
           policy is paired with ragged rather than used alone.

  Strict (default) : A delimited record has 2 field(s) but the header declares 3.  (the default fails loudly - a ragged row from a known generator means a bug upstream, not data to import)
  Ragged           : accepted 4 rows with field counts [3, 3, 2, 4]  (the counts differ per row and are visible - the policy accepts the variance rather than hiding it)
  Throw (default)  : A delimited record has 2 field(s) but the header declares 3.  (the stray characters after the closing quote collapse the row to two fields, so the field-count check is what rejects it first)
  SkipRecord+Ragged: kept 5 rows, field counts [3, 3, 2, 2, 3] (malformed row truncated)  (skipping keeps the fields already parsed, so the row comes out short - which is why this policy is paired with Ragged)
```

Note the second strict failure reports a field-count mismatch rather than a quoting error: the
stray characters collapse that row to two fields, so the count check fires first.

**APIs demonstrated.** `DelimitedReaderOptions.NoHeader` / `FieldCountBehavior` /
`MalformedRecordBehavior`, `DelimitedFormatException`.

## Scenario 3 — FormatAndRoundTrip

**Intent.** Show the write direction, where the interesting decision is which fields need
quoting — and prove the round trip is safe enough to edit a parsed file in place.

**What it does.** Parses into the mutable `DelimitedNode` DOM, writes it back, re-parses to
compare shape and the quoted value, prints the re-emitted last row, then writes the same tree
with a tab delimiter.

**What to expect.**

```text
--- Writing - round-tripping a CSV and converting its dialect ---
  What   : Parses the trades file into the mutable DOM, writes it back out, re-parses the result to compare shape
           and the quoted field, prints the re-emitted last row, then writes the same tree again with a tab
           delimiter.
  Why    : The interesting half of writing CSV is quoting, and the rule is that a field is quoted when it has to be
           - because it contains the delimiter, a quote or a line break - and left bare otherwise. Quoting
           everything is valid but produces a file that diffs badly against its source and that some consumers
           mishandle; quoting nothing produces a file that is simply wrong. Getting this right is also what makes
           the round trip meaningful: re-emitting a parsed file and re-parsing it has to give back the same values,
           or the DOM is not a safe place to edit. Dialect conversion then falls out for free, since the delimiter
           is a writer option rather than part of the tree.
  Expect : The re-parsed document has the same shape and the same quoted value, so nothing was lost in the round
           trip. Looking at the re-emitted row, only the field containing a comma carries quotes - the others are
           bare. The TSV output is the same records with a different separator, produced by a write rather than by a
           conversion step.

  round trip: shape preserved -> True, quoted comma field preserved -> True  (expected True twice - parse, emit and re-parse must agree, or the DOM is not a safe place to edit a file)
  last row re-emitted: 1005,"F, ordinary",Buy,1000,12.3400,2026-03-02T15:04:18Z  (only the field containing a comma is quoted - quoting everything would be valid but would diff badly against the source)
  as TSV: 1001 <TAB> AAPL <TAB> Buy <TAB> 100 <TAB> 187.4500 <TAB> 2026-03-02T14:30:05Z  (the same tree, a different writer option - the delimiter is not part of the parsed data)
```

**APIs demonstrated.** `DelimitedNode.Parse` / `ToString` / `WriteTo`, `Utf8DelimitedWriter`,
`DelimitedWriterOptions.Delimiter`.

## Scenario 4 — StreamingReaderWriter

**Intent.** Show the constant-memory shape: a file larger than memory filtered from reader
straight into writer, with no document in between.

**What it does.** Walks the token stream one record at a time, accumulates each row's fields,
writes only the `Buy` rows through to a new CSV, and sums their notional during the same pass.

**What to expect.**

```text
--- Streaming - reader to filter to writer, with no document in between ---
  What   : Walks the trades file one token at a time, accumulating each row's fields, and writes only the Buy rows
           through to a new CSV while accumulating their notional - then prints the filtered output.
  Why    : This is the shape for a file bigger than memory, and the reason the reader and writer share a token model
           rather than each having their own. Only one row is held at a time, so the cost of the pipeline is fixed
           regardless of how long the file is - which is the difference between a job that runs on a large export
           and one that runs out of memory partway through. The writer re-applies the quoting rules independently on
           the way out, so a field that needed quotes in the source still has them in the output even though the
           pipeline only ever saw its unquoted value.
  Expect : Fewer rows out than in, because only Buy rows pass the filter, with the notional accumulated during the
           same single pass. The reader reports the line it finished on, which is what makes a failure mid-stream
           diagnosable. In the output, the symbol containing a comma is quoted again - the writer decided that, not
           the reader.

  streamed 5 rows, kept 3 buys (notional 41,135.00); reader stopped at line 7  (one row held at a time, so the cost is fixed no matter how long the file is)
  filtered output (note 'F, ordinary' was re-quoted by the writer, which never saw the source quotes):
  | trade_id,symbol,side,quantity,price,executed_at
  | 1001,AAPL,Buy,100,187.4500,2026-03-02T14:30:05Z
  | 1003,BRK.B,Buy,25,402.0000,2026-03-02T14:32:40Z
  | 1005,"F, ordinary",Buy,1000,12.3400,2026-03-02T15:04:18Z
```

**APIs demonstrated.** `Utf8DelimitedReader.Read` / `TokenType` / `GetString` / `Headers` /
`LineNumber`, `Utf8DelimitedWriter.WriteStartArray` / `WriteStartObject` / `WritePropertyName`
/ `WriteString` / `Flush`.

## Layout

```text
Bodu.Text.Formats.Samples.DelimitedData/
  Program.cs                        # runs the scenarios in order
  SampleConsole.cs                  # the What / Why / Expect scenario banner
  Data/trades.csv                   # committed input (5 rows, one quoted comma field)
  Scenarios/ParseAndTypedGetters.cs
  Scenarios/PolicyBehaviors.cs
  Scenarios/FormatAndRoundTrip.cs
  Scenarios/StreamingReaderWriter.cs
```

## Related

- `Bodu.Text.Formats.Samples.ConfigFiles` — the INI and DotEnv half of the umbrella package.
- Guides: `docs/guides/text-formats/`.
