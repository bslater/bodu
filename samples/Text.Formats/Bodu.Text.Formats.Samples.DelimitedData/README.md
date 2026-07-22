# Bodu.Text.Formats.Samples.DelimitedData

RFC 4180 CSV/TSV via **`Bodu.Text.Delimited`** (referenced through the `Bodu.Text.Formats`
umbrella). Everything runs offline against the committed `Data/trades.csv`.

```bash
dotnet run --project samples/Text.Formats/Bodu.Text.Formats.Samples.DelimitedData
```

## Scenarios

### `ParseAndTypedGetters`

Parses the trades file twice: once into the read-only `DelimitedDocument` (records as objects
keyed by header name — quoted fields like `"F, ordinary"` arrive unwrapped), and once through
`DelimitedSerializer.Deserialize<Trade>` with `NamingPolicy.SnakeCaseLower`, so the
`trade_id` / `executed_at` headers bind onto `TradeId` / `ExecutedAt` with invariant-culture
scalar conversion. Aggregates notional value across the typed records.

### `PolicyBehaviors`

Deliberately dirty input under each policy: strict header-mode parsing throws
`DelimitedFormatException` on a short row; `FieldCountBehavior.Ragged` admits short and long
rows; a stray character after a closing quote is a structural error under the defaults; and
`MalformedRecordBehavior.SkipRecord` truncates the broken record — which is why lenient
ingestion pairs it with `Ragged`. Positional (`NoHeader`) parsing keeps the per-row field
counts visible.

### `FormatAndRoundTrip`

Parses into the mutable `DelimitedNode` DOM, writes it back (`ToString()` re-quotes exactly
the fields that need it), verifies the round trip shape- and value-preserving, then retargets
the same tree to TSV with `new DelimitedWriterOptions { Delimiter = '\t' }`.

### `StreamingReaderWriter`

A constant-memory filter pipeline: the forward-only `Utf8DelimitedReader` walks the token
stream one record at a time, `Buy` rows are re-emitted through `Utf8DelimitedWriter` (which
re-quotes on the way out), and nothing materializes a document.
