---
title: Using delimited (CSV / TSV)
---

# Using delimited (CSV / TSV)

`Delimited` is the static codec for [RFC 4180](https://datatracker.ietf.org/doc/html/rfc4180) delimited text — comma-separated values, tab-separated values, and the broader family of single-character-delimited tabular formats. It exposes the package's common codec shape — `Parse` / `TryParse` / `Format` over spans and the document model — plus a streaming `DelimitedReader` / `DelimitedWriter` pair for large files where the document model would not fit in memory.

For the vocabulary used below (document, row, header, field, parse options vs format options) see [Core concepts](../../docs/formats/concepts.md).

## Pattern 1 — parse a string you already have

```csharp
using Bodu.Text.Delimited;

string csv = """
name,age,department
Ada,37,Computing
Charles,28,Mechanics
""";

DelimitedDocument document = Delimited.Parse(csv);

Console.WriteLine(document.Headers[0]);          // "name"
Console.WriteLine(document.Rows[0]["age"]);     // "37"
Console.WriteLine(document.Rows[1].Fields[2]); // "Mechanics"
```

`Parse(ReadOnlySpan<char>)` reads the entire source into a `DelimitedDocument` — an immutable `Headers` list (the first row, unless `HasHeader: false`) and an immutable `Rows` collection (every subsequent row in source order). Fields are accessible by ordinal via `row.Fields[i]` or `row[i]`, and by column name via `row[headerName]` when headers are present.

## Pattern 2 — TSV and other delimiters

```csharp
using Bodu.Text.Delimited;

DelimitedDocument tsv = Delimited.Parse(source, new DelimitedParseOptions { Delimiter = '\t' });
DelimitedDocument psv = Delimited.Parse(source, new DelimitedParseOptions { Delimiter = '|' });
```

The delimiter is configurable per call. The default is `','`; switching to `'\t'` produces TSV. There is no restriction on the choice, but multi-character delimiters are not supported — pick a single character that does not appear unescaped in your data.

## Pattern 3 — non-throwing parse

```csharp
using Bodu.Text.Delimited;

if (Delimited.TryParse(source, out DelimitedDocument? document))
{
    Process(document);
}
else
{
    log.Warn("Malformed delimited input");
}
```

`TryParse` returns `false` and sets `document` to `null` on the first parse error rather than raising `DelimitedFormatException`. Use this pattern when the input is untrusted and you want a single early exit instead of a try/catch wrapper.

## Pattern 4 — typed field access

```csharp
using Bodu.Text.Delimited;

DelimitedDocument document = Delimited.Parse(csv);
foreach (DelimitedRow row in document.Rows)
{
    int    age      = row.GetValue<int>("age");
    string name     = row["name"];
    string dept     = row["department"];

    if (row.TryGetValue("salary", out decimal salary))
        Process(name, age, dept, salary);
}
```

`GetValue<T>(int index)` and `GetValue<T>(string header)` parse a field via `ISpanParsable<T>` under `CultureInfo.InvariantCulture`. `TryGetValue<T>` returns `false` on parse failure rather than throwing. The header indexer throws `KeyNotFoundException` when the supplied header is not declared, so for optional columns reach for `TryGetValue` instead.

## Pattern 5 — streaming over a large file

```csharp
using Bodu.Text.Delimited;

using var reader = new StreamReader("transactions.csv");
using var dlm = Delimited.CreateReader(reader, new DelimitedParseOptions { HasHeader = true });

// Resolve column ordinals once, from the header exposed after the first Read().
int idCol = 0, amountCol = 1;

while (dlm.Read())
{
    string id     = dlm.Fields[idCol];
    decimal value = decimal.Parse(dlm.Fields[amountCol], CultureInfo.InvariantCulture);

    Console.WriteLine($"Line {dlm.LineNumber} / Row {dlm.RowNumber}: {id} -> {value}");
}
```

`DelimitedReader` is a forward-only buffered reader (4096-character internal buffer by default; a constructor overload accepts a different `bufferSize`) for files that do not fit in memory. After each successful `Read()` the `Fields` property (`IReadOnlyList<string>`) exposes the current row; `Headers` is populated after the first read when the options declare `HasHeader: true`. The reader strips a leading UTF-8 BOM if present, supports multiline quoted fields, and tracks `LineNumber` (1-based source line) and `RowNumber` (data-row count, excluding the header and skipped blanks / comments).

> [!NOTE]
> `Fields` and `Headers` are `IReadOnlyList<string>`, so they have no `IndexOf`. To resolve a column ordinal from its name on the streaming reader, build a `Dictionary<string,int>` from `Headers` once after the first read (or use the in-memory `DelimitedDocument`, whose `DelimitedRow` indexer accepts a column name directly).

`DelimitedWriter` is the symmetrical writer:

```csharp
using var writer = new StreamWriter("output.csv");
using var dlm = Delimited.CreateWriter(writer);

dlm.WriteHeader(new[] { "id", "amount" });
dlm.WriteRow(new[] { "T-1001", "19.95" });
dlm.WriteRow(new[] { "T-1002", "5.10"  });
```

`WriteRow` applies RFC 4180 quoting automatically — a field is quoted when it is empty or contains the delimiter, the quote character, `\n`, or `\r`; embedded quote characters are doubled. Empty fields are quoted as `""` so a leading or trailing empty field survives a round trip. Each row is terminated with a bare line feed (`\n`), not `\r\n`. `WriteHeader` should be called once, before the first `WriteRow`; `RowsWritten` counts data rows only (the header is excluded). A `null` element in the `fields` enumerable raises `ArgumentException`.

## Pattern 6 — round-trip through `Format`

```csharp
using Bodu.Text.Delimited;

DelimitedDocument document = Delimited.Parse(input);
string roundTrip = Delimited.Format(document);
```

`Format` writes the document with the same delimiter and quote behaviour as `Parse`. The output is RFC 4180-conformant: fields that need quoting are quoted, embedded quotes are doubled, and empty fields are always quoted.

Round-tripping is **not** byte-for-byte stable: parse discards blank lines, comment lines, and whitespace inside unquoted fields — those are not retained in the document model. Field ordering, header ordering, and the values themselves do round-trip.

## Behaviour options

The headline `DelimitedParseOptions` fields control the format dialect:

| Field | Default | Controls |
|---|---|---|
| `Delimiter` | `','` | Field separator. |
| `Quote` | `'"'` | Field-quoting character. |
| `HasHeader` | `true` | Whether the first row is the header. |
| `TrimFields` | `false` | Trim leading / trailing whitespace from *unquoted* fields. Whitespace inside quoted fields is always preserved. |
| `AllowComments` | `false` | Recognise comment lines starting with `CommentChar`. |
| `CommentChar` | `'#'` | Comment-line marker (only honoured when `AllowComments` is true). |
| `FieldCountBehavior` | `Strict` | See below. |
| `DuplicateHeaderBehavior` | `Throw` | See below. |
| `MalformedRecordBehavior` | `Throw` | See below. |

### Field-count behaviour

`DelimitedFieldCountBehavior` controls what happens when a row has fewer or more fields than the header (or, if there is no header, the first data row):

| Member | Effect |
|---|---|
| `Strict` *(default)* | Throws `DelimitedFormatException` with the offending line number. |
| `Ragged` | Tolerates ragged rows; a short row's missing trailing fields return the empty string via the column-name indexer, and a long row keeps its extra fields accessible by ordinal. |

The count is checked only against a declared header, so `FieldCountBehavior` has no effect when `HasHeader` is `false` — there is no reference width to compare against.

```csharp
var ragged = new DelimitedParseOptions { FieldCountBehavior = DelimitedFieldCountBehavior.Ragged };

DelimitedDocument doc = Delimited.Parse("a,b,c\n1,2\n4,5,6,7", ragged);
// Row 0: "1","2" — doc.Rows[0]["c"] is "" (missing trailing field)
// Row 1: "4","5","6","7" — the extra field 7 is reachable via doc.Rows[1].Fields[3]
```

### Duplicate header behaviour

`DelimitedDuplicateHeaderBehavior` controls how duplicate column names are resolved:

| Member | Effect |
|---|---|
| `Throw` *(default)* | Throws `DelimitedFormatException`. |
| `FirstWins` | First occurrence wins in the name → index map. |
| `LastWins` | Last occurrence wins in the name → index map. |
| `AllowDuplicates` | Duplicates tolerated but excluded from the name → index map; access by column name throws. |

### Malformed record behaviour

`DelimitedMalformedRecordBehavior` controls what happens when the parser encounters text after a closing quote:

| Member | Effect |
|---|---|
| `Throw` *(default)* | Throws `DelimitedFormatException` with the line number. |
| `SkipRecord` | Silently discards the remainder of the malformed record after the closing quote. |

## Exceptions

`DelimitedFormatException` derives from `TextFormatException` and carries a `LineNumber` property (1-based; 0 when the source line is unknown). Thrown for unterminated quoted fields, field-count mismatches under `Strict`, malformed records under `Throw`, and duplicate headers under `Throw`.

## Extension methods

The `DelimitedExtensions` helpers add fluent overloads to strings and spans:

```csharp
using Bodu.Text.Delimited;

DelimitedDocument doc = csv.ParseDelimited();
string output         = doc.FormatDelimited();
```

`ParseDelimited`, `TryParseDelimited`, and `FormatDelimited` mirror the static-class entry points but read better in pipelines that already have an `IEnumerable<string>` or a span in hand.

## When *not* to use `Delimited`

- **Genuinely streaming input where you cannot buffer.** The static `Parse` / `Format` surface materialises the whole document. Reach for `DelimitedReader` / `DelimitedWriter` instead.
- **Heterogeneous columns where every row has its own shape.** Delimited assumes a single column schema; if every row is structurally different, [DotEnv](dotenv.md) or [INI](ini.md) may be a better fit.
- **Round-trip fidelity at the byte level.** Parse discards blank lines, comment lines, and unquoted whitespace. If you need a verbatim round trip, hold on to the original bytes.
- **Multiline non-quoted fields.** Carriage returns and line feeds in field values must be enclosed in the quote character. Non-quoted CRLF is treated as a row terminator.

## See also

- [DotEnv](dotenv.md), [INI](ini.md) — the other formats in the package.
- [`Bodu.Text.Delimited` API reference](xref:Bodu.Text.Delimited)
- [Streams and async I/O](streaming.md) — buffer-lifecycle details for stream-based pipelines.
- **[Text & Serialization guides](../topics/text-and-serialization.md)** — every guide in this topic, across Bodu.Text.Encoding, Bodu.Text.Formats, and the Bencode / TOML serializers.
