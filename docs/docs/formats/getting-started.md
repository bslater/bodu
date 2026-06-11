---
title: Bodu.Text.Formats — Getting started
---

# Bodu.Text.Formats — Getting started

Unfamiliar with terms like *self-framing format*, *value model*, *codec*, or *round-trip rules*? Read [Core concepts](concepts.md) first.

> Looking for **TOML**, **Bencode**, or a `System.Text.Json`-style POCO serializer? Those live in the standalone <xref:Bodu.Text.Toml> and <xref:Bodu.Text.Bencode> packages — see the [Bodu serializers getting started](../serialization/getting-started.md).

## Install

```bash
dotnet add package Bodu.Text.Formats
```

Targets `net8.0`. The package depends on `Bodu.Core` for shared throw-helpers and on `Bodu.Text.Encoding` for the embedded `Base16` helpers used in test vectors; no other NuGet references.

## Read and write delimited (CSV / TSV)

```csharp
using Bodu.Text.Delimited;

DelimitedDocument doc = Delimited.Parse("name,score\nAda,99\nGrace,100");
foreach (DelimitedRow row in doc.Rows)
    Console.WriteLine($"{row.Fields[0]} = {row.Fields[1]}");

string csv = Delimited.Format(doc);
```

For large files, stream one row at a time with `Delimited.CreateReader` / `CreateWriter` (see [Streaming](../../guides/formats/streaming.md)).

## Parse a `.env` file

```csharp
using Bodu.Text.DotEnv;

DotEnvDocument env = DotEnv.Parse("""
    # API key for production
    API_KEY=secret
    PORT=8080
    """);

string key = env["API_KEY"];
```

`DotEnv` preserves leading comments by default, so a `Parse` → `Format` round trip is lossless for them.

## Round-trip an INI document, preserving comments

```csharp
using Bodu.Text.Ini;

IniDocument ini = Ini.Parse("""
    ; connection settings
    [database]
    host = localhost
    port = 5432
    """);

ini.GetOrAddSection("database").SetEntry("port", "5433");
string text = Ini.Format(ini);   // comments, ordering, and whitespace preserved
```

## Decode without throwing

Each format offers a `Try…` overload that swaps the exception for a `bool`:

```csharp
using Bodu.Text.Ini;

if (Ini.TryParse(userInput, out IniDocument? parsed))
{
    Use(parsed);
}
else
{
    // input was malformed — show an error
}
```

## Validate a payload before processing

```csharp
using Bodu.Text.Ini;

try
{
    IniDocument config = Ini.Parse(source);
    Process(config);
}
catch (TextFormatException ex)
{
    log.Warn("Malformed config at line {Line}, column {Column}: {Message}",
        ex.LineNumber, ex.ColumnNumber, ex.Message);
}
```

Every format's exception derives from <xref:Bodu.Text.TextFormatException>, so a single `catch (TextFormatException)` handles parse failures uniformly. See [parser policies](parser-policies.md) for the per-format diagnostics and strictness options.

## Where to go next

- **[Bodu.Text.Formats guides](../../guides/formats/index.md)** — per-API deep dives.
- **[Core concepts](concepts.md)** — vocabulary refresher.
- **[Parser policies](parser-policies.md)** — strictness options and diagnostics.
- **[Introduction](index.md)** — type map and scenario index.
- **API reference** — per-namespace pages: [Delimited](xref:Bodu.Text.Delimited), [DotEnv](xref:Bodu.Text.DotEnv), [Ini](xref:Bodu.Text.Ini).
