# Bodu.Text.Formats

Parsers and serializers for common text document formats on .NET 8 — delimited text (RFC 4180 CSV / TSV), DotEnv, and INI. Each format follows the same shape: a static convenience entry point (`Parse` / `Format` / `TryParse`), a low-level `Reader` / `Writer` pair, an immutable document model, and an options struct that controls parsing behaviour and round-trip fidelity.

## Installation

```shell
dotnet add package Bodu.Text.Formats
```

Targets `net8.0`.

## Formats

| Format | Namespace | Entry point | Document model | Options |
|---|---|---|---|---|
| Delimited (RFC 4180) | `Bodu.Text.Delimited` | `Delimited` | `DelimitedDocument` / `DelimitedRow` | `DelimitedParseOptions` |
| DotEnv | `Bodu.Text.DotEnv` | `DotEnv` | `DotEnvDocument` / `DotEnvEntry` | `DotEnvParseOptions` |
| INI | `Bodu.Text.Ini` | `Ini` | `IniDocument` / `IniSection` / `IniEntry` | `IniParseOptions` |

## API shape

```csharp
using Bodu.Text.Delimited;

DelimitedDocument doc = Delimited.Parse(csv, new DelimitedParseOptions { HasHeader = true });
string name = doc.Rows[0]["Name"];
string back = Delimited.Format(doc);

// Streaming
await using DelimitedReader reader = ...;
DelimitedDocument loaded = Delimited.Load(stream);
```

- Static `Parse(ReadOnlySpan<char>, options)` / `Format(document)` with `TryParse` companions.
- Stream `Load` / `Save` with async variants where applicable.
- Comments and layout are preserved in the document model (`IniComment`, `DotEnvComment`) so parse → serialize round-trips faithfully.
- Each format reports failures through a dedicated exception (`DelimitedFormatException`, `DotEnvFormatException`, `IniFormatException`) derived from `TextFormatException`.

Parse options expose the behavioural knobs each format needs — delimiter / quote / comment characters and duplicate-header and malformed-record policies for delimited text; case sensitivity and duplicate-section/key policies for INI; and export-prefix, inline-comment, and interpolation toggles for DotEnv.

## Testing

Tests live in `test/` as MSTest partial classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Text.Formats/test/Bodu.Text.Formats.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Text.Formats/test/Bodu.Text.Formats.Test.csproj --settings regression.runsettings
```

Formats are validated through the shared `TextDocumentFormatContractTests<,>` base, driven by per-format known-answer vectors (`DelimitedKnownAnswerVector`, `DotEnvKnownAnswerVector`, `IniKnownAnswerVector`).

## License

MIT. © Bodu Pty. Ltd.
