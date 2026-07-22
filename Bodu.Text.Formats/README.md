# Bodu.Text.Formats

> **Umbrella meta-package.** This package carries no code of its own — it references the three standalone line-format libraries so a single package reference brings in all of them.

The Bodu line-oriented text formats on .NET 8, each a self-contained library shaped after `System.Text.Json`:

| Format | Package | Token surface | Serializer | DOMs |
|---|---|---|---|---|
| Delimited (RFC 4180 CSV / TSV) | `Bodu.Text.Delimited` | `Utf8DelimitedReader` / `Utf8DelimitedWriter` | `DelimitedSerializer` | `DelimitedNode` (mutable) / `DelimitedDocument` (read-only) |
| DotEnv | `Bodu.Text.DotEnv` | `Utf8DotEnvReader` / `Utf8DotEnvWriter` | `DotEnvSerializer` | `DotEnvNode` (mutable) / `DotEnvDocument` (read-only) |
| INI | `Bodu.Text.Ini` | `Utf8IniReader` / `Utf8IniWriter` (+ normalized `IniDocumentReader`) | `IniSerializer` | `IniNode` (mutable, comment-preserving) / `IniDocument` (read-only) |

## Installation

```shell
dotnet add package Bodu.Text.Formats
```

Targets `net8.0`. To depend on a single format, reference its package directly instead — for example `dotnet add package Bodu.Text.Delimited`.

## API shape

Every format follows the same quartet:

```csharp
using Bodu.Text.Delimited;
using Bodu.Text.Delimited.Document;

// Read-only document (JsonDocument-shaped).
using DelimitedDocument doc = DelimitedDocument.Parse(csvBytes);
string symbol = doc.RootElement[0].GetProperty("symbol").GetString();

// Typed records via the serializer (JsonSerializer-shaped).
List<Trade> trades = DelimitedSerializer.Deserialize<Trade>(csvText);
string back = DelimitedSerializer.Serialize(trades);
```

The forward-only `Utf8*Reader` / `Utf8*Writer` pairs cover token-level streaming, and the mutable `*Node` DOMs cover authoring — the INI node tree additionally preserves comment trivia for faithful round trips of human-owned files.

See the per-package READMEs and the samples under `samples/Text.Formats/` for full walkthroughs.
