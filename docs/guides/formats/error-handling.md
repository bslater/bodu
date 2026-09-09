---
title: Errors across the line formats
---

# Errors across the line formats

Each line format — **Delimited**, **DotEnv**, and **INI** — reports failure through exactly two exception types, and the split is the same in all three: a *format* exception for input that is not well-formed, and a *serialization* exception for a well-formed document that does not bind to the target type. This guide shows what each type carries, which inputs trigger it, how to catch it, and which reader-option policies turn a would-be error into tolerated input.

## The two families

| | Format exception | Serialization exception |
|---|---|---|
| Delimited | <xref:Bodu.Text.Delimited.DelimitedFormatException> — `LineNumber`, `Offset` | <xref:Bodu.Text.Delimited.DelimitedSerializationException> |
| DotEnv | <xref:Bodu.Text.DotEnv.DotEnvFormatException> — `LineNumber`, `ColumnNumber`, `Offset` | <xref:Bodu.Text.DotEnv.DotEnvSerializationException> |
| INI | <xref:Bodu.Text.Ini.IniFormatException> — `LineNumber`, `Offset` | <xref:Bodu.Text.Ini.IniSerializationException> |
| Base type | `FormatException` | `Exception` |
| Raised by | the readers, both DOMs, and the serializer's read path | the serializer's bind (read) and shape checks (write) |
| Position members | 1-based `LineNumber`, 0-based byte `Offset` (DotEnv adds a 1-based `ColumnNumber`); all `int?` — `null` when the failure has no single position | none; the message names the key or column |
| `InnerException` | rarely set | the `FormatException` from a failed scalar conversion, when there is one |

Two points follow from the table. First, the position members are nullable: a structural rule that spans the whole document — an INI global key colliding with a section of the same name, for example — reports no line. Second, the serializer's read path can throw *either* type: the document is parsed first (format exception), then bound (serialization exception), so a catch that must handle both needs two clauses.

## Pattern 1 — Delimited: malformed input

The Delimited reader enforces RFC 4180 structure. An unterminated quoted field, a record whose field count disagrees with the header, characters after a closing quote, and a duplicate header name are all `DelimitedFormatException`:

```csharp
using Bodu.Text.Delimited;
using Bodu.Text.Delimited.Document;

try
{
    using DelimitedDocument doc = DelimitedDocument.Parse("symbol,qty\nMSFT,\"10\n"u8);
}
catch (DelimitedFormatException ex)
{
    Console.WriteLine($"{ex.Message} (line {ex.LineNumber}, offset {ex.Offset})");
}
// A quoted delimited field was not terminated before the end of the input. (line 3, offset 20)
```

The other structural failures, with the position each reports:

| Input | Message | Position |
|---|---|---|
| `symbol,qty` / `MSFT,10,extra` | *A delimited record has 3 field(s) but the header declares 2.* | line 3, offset 25 |
| `symbol,qty` / `MSFT,"10"x` | *A delimited record has 1 field(s) but the header declares 2.* | line 3, offset 22 |
| `symbol,symbol` / `MSFT,10` | *The delimited header row contains the duplicate column name 'symbol'.* | line 2, offset 14 |

The line number is the line on which the reader *detected* the problem — for a record error that is the line after the offending record's terminator, which is why single-line records report line 3 here.

## Pattern 2 — Delimited: tolerating dirty input

Three <xref:Bodu.Text.Delimited.Reader.DelimitedReaderOptions> policies convert the errors above into accepted input. Each has a strict default; the leniency is opt-in:

| Policy | Default | Lenient value | Effect |
|---|---|---|---|
| `FieldCountBehavior` | `Strict` | `Ragged` | Records with more or fewer fields than the header are accepted as-is. |
| `MalformedRecordBehavior` | `Throw` | `SkipRecord` | A record with a structural error (text after a closing quote) is truncated at the error and reading continues with the next record. |
| `DuplicateHeaderBehavior` | `Throw` | `TakeFirst` / `TakeLast` | A repeated column name maps to the first or last occurrence. |

```csharp
using Bodu.Text.Delimited.Reader;

using DelimitedDocument ragged = DelimitedDocument.Parse(
    "symbol,qty\nMSFT,10,extra\nAAPL\n"u8,
    new DelimitedReaderOptions { FieldCountBehavior = DelimitedFieldCountBehavior.Ragged });
int raggedCount = ragged.RootElement.GetArrayLength();    // 2 records

using DelimitedDocument skipped = DelimitedDocument.Parse(
    "symbol,qty\nMSFT,\"10\"x\nAAPL,5\n"u8,
    new DelimitedReaderOptions { MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord });
int skippedCount = skipped.RootElement.GetArrayLength();  // 2 records — the malformed record is truncated at the error, not dropped

using DelimitedDocument last = DelimitedDocument.Parse(
    "symbol,symbol\nMSFT,10\n"u8,
    new DelimitedReaderOptions { DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.TakeLast });
string symbol = last.RootElement[0].GetProperty("symbol").GetString();   // "10"
```

An unterminated quoted field is never tolerated — there is no policy for it, because the reader cannot know where the record was meant to end. The full dialect table is on the [parser policies](../../docs/formats/parser-policies.md) page. Note that these policies live on the *reader* options, which the DOMs and the token reader accept; <xref:Bodu.Text.Delimited.DelimitedSerializerOptions> exposes only the dialect characters (`Delimiter`, `Quote`, `NoHeader`), so a typed `DelimitedSerializer.Deserialize<T>` always reads strictly.

## Pattern 3 — Delimited: binding failures

A well-formed document that does not fit the record type raises `DelimitedSerializationException`. The typical cause is a scalar that does not parse; the `FormatException` from the conversion is preserved as the inner exception:

```csharp
public sealed class Trade
{
    public string Symbol { get; set; } = "";
    public int Quantity { get; set; }
}
```

```csharp
try
{
    DelimitedSerializer.Deserialize<Trade>("Symbol,Quantity\nMSFT,ten\n");
}
catch (DelimitedSerializationException ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.InnerException?.Message);
}
// The delimited value for column 'Quantity' could not be converted to type 'System.Int32'.
// The input string 'ten' was not in a correct format.
```

A *missing* column is not an error — Delimited does not honor `[Required]`, so `Symbol\nMSFT\n` binds a `Trade` with `Quantity` left at its default. And because the document is parsed before it is bound, a structural error inside the same call still surfaces as the format exception:

```csharp
DelimitedSerializer.Deserialize<Trade>("Symbol,Quantity\nMSFT,\"1\n");
// → throws DelimitedFormatException: A quoted delimited field was not terminated before the end of the input.
```

## Pattern 4 — DotEnv: malformed input

The DotEnv reader reports a line, a column, and a byte offset on every structural failure:

```csharp
using Bodu.Text.DotEnv;
using Bodu.Text.DotEnv.Document;

try
{
    using DotEnvDocument doc = DotEnvDocument.Parse("API_KEY=\"abc\n");
}
catch (DotEnvFormatException ex)
{
    Console.WriteLine($"{ex.Message} (line {ex.LineNumber}, column {ex.ColumnNumber}, offset {ex.Offset})");
}
// Unterminated double-quoted DotEnv value beginning on line 1. (line 1, column 1, offset 13)
```

| Input | Message | Position |
|---|---|---|
| `=abc` | *Invalid DotEnv key name '=' on line 1; keys must match [A-Za-z_][A-Za-z0-9_]\*.* | line 1, column 1, offset 0 |
| `API KEY=abc` | *Malformed DotEnv entry on line 1; expected 'KEY=VALUE'.* | line 1, column 1, offset 4 |
| `export API_KEY=abc` with `DisallowExportPrefix = true` | *Malformed DotEnv entry on line 1; expected 'KEY=VALUE'.* | line 1, column 1, offset 7 |

The DotEnv knobs on <xref:Bodu.Text.DotEnv.Reader.DotEnvReaderOptions> run in the *strict* direction — the default dialect is already the permissive mainstream `dotenv` reading, and each option removes a feature: `DisallowExportPrefix` makes an `export` line malformed (above), `DisallowInlineComments` stops ` # note` from terminating an unquoted value (so `KEY=abc # note` reads as `abc # note` instead of `abc`), and `SkipComments` drops comment lines from the token stream. None of them turns an error into accepted input.

## Pattern 5 — DotEnv: binding failures

`DotEnvSerializationException` covers the two binding failures the format can have: a `[Required]` key that is absent, and a value that does not convert:

```csharp
public sealed class ApiSettings
{
    [Required]
    public string ApiKey { get; set; } = "";

    public int Timeout { get; set; } = 30;
}
```

```csharp
DotEnvSerializer.Deserialize<ApiSettings>("TIMEOUT=10\n");
// → throws DotEnvSerializationException: Required DotEnv key 'ApiKey' was not present in the document.

DotEnvSerializer.Deserialize<ApiSettings>("ApiKey=k\nTimeout=soon\n");
// → throws DotEnvSerializationException: The DotEnv value for key 'Timeout' could not be converted to type 'System.Int32'.
//   InnerException: FormatException: The input string 'soon' was not in a correct format.
```

As with Delimited, a syntax error in the same call is the format exception (`ApiKey="k` → *Unterminated double-quoted DotEnv value beginning on line 1.*), so handlers around `Deserialize<T>` need both clauses.

## Pattern 6 — INI: malformed input and document-model rules

The INI reader's dialect is deliberately conservative: `=` is the only delimiter, a header must close on its line, and comments are full-line only. Those produce positioned `IniFormatException`s:

```csharp
using Bodu.Text.Ini;
using Bodu.Text.Ini.Document;

try
{
    using IniDocument doc = IniDocument.Parse("[server]\nport: 8080\n");
}
catch (IniFormatException ex)
{
    Console.WriteLine($"{ex.Message} (line {ex.LineNumber}, offset {ex.Offset})");
}
// Malformed INI entry on line 2; expected 'key=value'. (line 2, offset 19)
```

| Input | Message | Position |
|---|---|---|
| `[server` / `port=8080` | *An INI section header was not terminated with ']' before the end of the line.* | line 1, offset 7 |
| `# c` / `k=1` with `DisallowHashComments = true` | *Malformed INI entry on line 1; expected 'key=value'.* | line 1, offset 3 |
| `server=1` / `[server]` / `port=8080` | *The global INI key 'server' collides with a section of the same name.* | no position (`null`) |

INI has a second source of format exceptions: the **document-model** duplicate policies in <xref:Bodu.Text.Ini.IniDocumentOptions>. The source-order reader reports the file verbatim; it is the normalized document (and therefore both DOMs and the serializer) that decides what a repeated key or section means. The defaults are lenient (`Merge` sections, `LastWins` keys) and the strict values raise the format exception:

```csharp
using Bodu.Text.Ini.Reader;

IniDocument.Parse("[s]\nk=1\nk=2\n"u8, IniReaderOptions.Default,
    new IniDocumentOptions { DuplicateKeyBehavior = IniDuplicateKeyBehavior.Disallowed });
// → throws IniFormatException: The INI section 's' defines the key 'k' more than once. (line 4, offset 12)

IniDocument.Parse("[s]\nk=1\n[s]\nj=2\n"u8, IniReaderOptions.Default,
    new IniDocumentOptions { DuplicateSectionBehavior = IniDuplicateSectionBehavior.Disallowed });
// → throws IniFormatException: The INI document defines the section 's' more than once. (line 4, offset 12)
```

`IniSerializerOptions` surfaces the same two policies, and its `Strict` preset (`IniSerializerDefaults.Strict`) sets both to `Disallowed` — the `configparser` strict mode. The global/section name collision is the one duplicate that no policy tolerates, because both entries would claim the same root property; `IniSerializerOptions.GlobalSectionName` sidesteps it by mapping the global entries to a reserved root key instead of hoisting them.

## Pattern 7 — INI: binding failures

`IniSerializationException` adds a third cause to the two the other formats share — the format's depth limit. INI is two levels deep (section, then key), so a member that would need a third level is rejected, on write as well as on read:

```csharp
public sealed class AppConfig
{
    [Required]
    public string Name { get; set; } = "";

    public Server Server { get; set; } = new();
}

public sealed class Server
{
    public int Port { get; set; } = 80;
}
```

```csharp
IniSerializer.Deserialize<AppConfig>("[Server]\nPort=8080\n");
// → throws IniSerializationException: The INI document does not contain the required key 'Name'.

IniSerializer.Deserialize<AppConfig>("Name=x\n[Server]\nPort=eighty\n");
// → throws IniSerializationException: The INI value for key 'Port' could not be converted to type 'System.Int32'.
//   InnerException: FormatException: The input string 'eighty' was not in a correct format.
```

A `Server` type that itself held an object-typed member — `public Tls? Tls { get; set; }` — fails at first use with *INI supports only two levels (section then key); the member 'Tls' nests deeper.*, whether or not the member is `null`: the check is on the type's shape, not the instance.

## Pattern 8 — Catch both families

Because the serializer's read path parses and then binds, a robust handler distinguishes the two and uses the position when there is one:

```csharp
public static AppConfig? TryLoad(string ini, Action<string> report)
{
    try
    {
        return IniSerializer.Deserialize<AppConfig>(ini);
    }
    catch (IniFormatException ex) when (ex.LineNumber is int line)
    {
        report($"line {line}: {ex.Message}");
    }
    catch (IniFormatException ex)
    {
        report(ex.Message);
    }
    catch (IniSerializationException ex)
    {
        report(ex.InnerException is null ? ex.Message : $"{ex.Message} ({ex.InnerException.Message})");
    }

    return null;
}
```

The format exceptions derive from `FormatException`, so a single `catch (FormatException)` covers all three formats' syntax errors where the format is not known in advance; the serialization exceptions derive directly from `Exception` and have no common base, by design — a caller binding a known format catches the specific type.

## Where to go next

- [Parser policies](../../docs/formats/parser-policies.md) — every reader-option knob per format, with its default.
- [Writer options and DOM options](writer-options-and-dom-options.md) — the writer-side and document-model structs, including the INI duplicate policies from the DOM side.
- The per-format guides — [Delimited](delimited.md), [DotEnv](dotenv.md), [INI](ini.md) — each with an *Exceptions* section in context.
- [Streams and token-level I/O](streaming.md) — what a mid-stream format exception means for the bytes already consumed.
- [Serializer options: freezing, caching, and thread safety](../serialization/options-and-lifetime.md) — the `InvalidOperationException` the options themselves throw once frozen.
- [Runnable samples](../../samples/formats.md), the [line-format guides hub](index.md), and the [Text & Serialization guides](../topics/text-and-serialization.md).
