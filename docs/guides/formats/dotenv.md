---
title: Using DotEnv
---

# Using DotEnv

`DotEnv` is the static codec for the de-facto `.env` file format — `KEY=VALUE` lines with optional `export` prefix, single-quoted or double-quoted values, and full-line comments. The dialect matches what dotenv libraries, Foreman, Docker Compose, and similar tools accept on the wire.

The surface mirrors the [Bencode codec](bencode.md): `Parse` / `TryParse` / `Format` over spans and the document model. Unlike Delimited, there is no streaming reader / writer — `.env` files are not large enough to warrant one.

For the vocabulary used below (document, entry, comment, parse options) see [Core concepts](../../docs/formats/concepts.md).

## Pattern 1 — parse a `.env` file

```csharp
using Bodu.Text.Formats;

string envText = """
# Database connection
DB_HOST=localhost
DB_PORT=5432
DB_PASSWORD="s3cr3t password with spaces"

# Optional knobs
export FEATURE_FLAGS=alpha,beta
""";

DotEnvDocument document = DotEnv.Parse(envText);

string? host = document["DB_HOST"];     // "localhost"
int     port = document.GetValue<int>("DB_PORT");
```

`Parse(ReadOnlySpan<char>)` produces a `DotEnvDocument` with an immutable `Entries` list (one per `KEY=VALUE` line, in source order) and a string-keyed indexer for O(1) lookup. Quotes are stripped, double-quoted escape sequences are resolved, and the `export ` prefix is removed before the key is parsed.

## Pattern 2 — typed lookup

```csharp
using Bodu.Text.Formats;

int port    = document.GetValue<int>("DB_PORT");
bool isDev  = document.TryGetValue("FEATURE_FLAGS", out string? flags)
              && flags!.Contains("alpha");

if (document.TryGetValue<TimeSpan>("REQUEST_TIMEOUT", out TimeSpan timeout))
    Configure(timeout);
```

`GetValue<T>(string key)` throws `KeyNotFoundException` if the key is absent. `TryGetValue<T>` returns `false` on missing key or parse failure. Parsing uses `ISpanParsable<T>` under `CultureInfo.InvariantCulture` — `int`, `double`, `decimal`, `TimeSpan`, `DateTime`, `Guid`, and any consumer-defined `T` that implements the interface are supported.

## Pattern 3 — non-throwing parse

```csharp
using Bodu.Text.Formats;

if (DotEnv.TryParse(source, out DotEnvDocument? document))
{
    Configure(document);
}
else
{
    log.Warn("Malformed .env input");
}
```

`TryParse` returns `false` and sets `document` to `null` on the first parse error rather than raising `DotEnvFormatException`. Use this pattern when reading from an untrusted source.

## Pattern 4 — preserved comments

```csharp
using Bodu.Text.Formats;

foreach (DotEnvEntry entry in document.Entries)
{
    foreach (DotEnvComment comment in entry.LeadingComments)
        Console.WriteLine($"# {comment.Text}");

    Console.WriteLine($"{entry.Key}={entry.Value}");
}
```

By default (`PreserveComments: true`), each full-line comment is attached as a leading-comment trivia entry on the next `DotEnvEntry`. The `DotEnvComment` value struct carries the prefix (`'#'`), the text, and the 1-based `LineNumber`. Inline comments — a `#` preceded by whitespace inside an *unquoted* value — truncate the value when `AllowInlineComments: true` (the default).

## Pattern 5 — round-trip through `Format`

```csharp
using Bodu.Text.Formats;

DotEnvDocument document = DotEnv.Parse(input);
string roundTrip = DotEnv.Format(document);
File.WriteAllText("app.env", roundTrip);
```

`Format` writes the document back to text. Quoting follows a conservative rule: empty values render as `KEY=` (unquoted), values containing only "safe ASCII" (`[A-Za-z0-9_.,:/@+\-]`) render unquoted, and everything else is double-quoted with `"`, `\`, `$`, newline, tab, and carriage return escaped (`\"`, `\\`, `\$`, `\n`, `\t`, `\r`). Round-tripping preserves keys, values, and comment attachment, but bare blank lines from the source are not retained.

## Behaviour options

The `DotEnvParseOptions` fields control the format dialect:

| Field | Default | Controls |
|---|---|---|
| `DuplicateKeyBehavior` | `LastWins` | How duplicate keys are resolved. |
| `AllowExportPrefix` | `true` | Strip a leading `export ` before parsing the key. |
| `AllowInlineComments` | `true` | Treat `#` preceded by whitespace in an unquoted value as a comment. |
| `PreserveComments` | `true` | Retain full-line comments as `LeadingComments` on the next entry. |

### Duplicate key behaviour

`DuplicateKeyPolicy`:

| Member | Effect |
|---|---|
| `LastWins` *(default)* | Last occurrence wins; earlier values discarded. |
| `FirstWins` | First occurrence wins; subsequent occurrences ignored. |
| `Disallowed` | Duplicate key raises `DotEnvFormatException`. |

## Quoting rules

Three forms of quoting are recognised on input:

- **Unquoted** — `KEY=value`. The value runs to the first inline-comment boundary, end-of-line, or end-of-input. Leading and trailing whitespace are trimmed.
- **Single-quoted** — `KEY='value'`. The value is literal; no escape sequences are processed, and the value cannot span lines.
- **Double-quoted** — `KEY="value"`. The value supports escape sequences (`\"`, `\\`, `\n`, `\t`, `\r`, `\$`, and `\xHH` hex escapes) and may span multiple source lines when no intervening comment line separates them.

`Format` always uses unquoted form for safe values and double-quoted form otherwise — single-quoted output is not emitted. If you need byte-stable round trips with single-quoted input, hold on to the original bytes.

## Key syntax

Keys must match the regular expression `[A-Za-z_][A-Za-z0-9_]*` — strict identifier syntax, no spaces, no dots. A `KEY=` line with an empty key, an invalid character, or no `=` separator raises `DotEnvFormatException` with the offending line number.

## Exceptions

`DotEnvFormatException` derives from `TextFormatException` and carries a `LineNumber` property (1-based; 0 when the source line is unknown). Thrown for malformed `KEY=VALUE` lines, invalid key syntax, unterminated quoted values, and duplicate keys under `Disallowed`.

## Extension methods

The `DotEnvExtensions` helpers add fluent overloads:

```csharp
using Bodu.Text.Formats;

DotEnvDocument doc = envText.ParseDotEnv();
string output      = doc.FormatDotEnv();
```

`ParseDotEnv`, `TryParseDotEnv`, and `FormatDotEnv` mirror the static-class entry points.

## When *not* to use `DotEnv`

- **Hierarchical configuration.** `.env` is flat — there are no sections, no nested keys, and no array values. Use [INI](ini.md) or [`Bodu.Text.Configuration`](../text-configuration/index.md) for structured configuration with sections and dotted paths.
- **Tabular data.** Reach for [Delimited](delimited.md) instead.
- **Secrets in production.** `.env` files are convenient for local development, but secrets in plain text on disk are not a substitute for a managed secret store. The codec doesn't address the threat model — it just reads and writes the file.
- **Strict environment-variable injection.** The codec produces a `DotEnvDocument`; it does not modify `Environment.GetEnvironmentVariable`. Apply the values yourself via `Environment.SetEnvironmentVariable` or the configuration system of your choice.

## See also

- [Bencode](bencode.md), [Delimited](delimited.md), [INI](ini.md) — the other formats in the package.
- [`Bodu.Text.DotEnv` API reference](xref:Bodu.Text.DotEnv)
- [`Bodu.Text.Configuration` overview](../text-configuration/index.md) — for hierarchical key / value configuration with sections.
