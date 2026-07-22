---
title: Line formats — Getting started
---

# Line formats — Getting started

First steps with `Bodu.Text.Delimited`, `Bodu.Text.DotEnv`, and `Bodu.Text.Ini`.

## Install

```shell
dotnet add package Bodu.Text.Formats      # umbrella: all three formats
dotnet add package Bodu.Text.Delimited    # or a single format:
dotnet add package Bodu.Text.DotEnv
dotnet add package Bodu.Text.Ini
```

All packages target `net8.0`.

## Read and write delimited (CSV / TSV)

```csharp
using Bodu.Text.Delimited;
using Bodu.Text.Delimited.Document;

byte[] csv = File.ReadAllBytes("trades.csv");
using DelimitedDocument document = DelimitedDocument.Parse(csv);

string firstSymbol = document.RootElement[0].GetProperty("symbol").GetString();
```

Typed records go through the serializer:

```csharp
using Bodu.Text.Serialization;

sealed class Trade
{
    public string? Symbol { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

var options = new DelimitedSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
List<Trade> trades = DelimitedSerializer.Deserialize<Trade>(File.ReadAllText("trades.csv"), options);
string back = DelimitedSerializer.Serialize(trades, options);
```

## Parse a `.env` file

```csharp
using Bodu.Text.DotEnv;
using Bodu.Text.DotEnv.Document;

using DotEnvDocument env = DotEnvDocument.Parse(File.ReadAllBytes(".env"));
string connection = env.RootElement.GetProperty("DATABASE_URL").GetString();
```

Or bind it straight onto a settings class with the SCREAMING_SNAKE_CASE preset:

```csharp
sealed class Settings
{
    public string? AppEnv { get; set; }   // binds APP_ENV
    public int AppPort { get; set; }      // binds APP_PORT
}

Settings settings = DotEnvSerializer.Deserialize<Settings>(
    File.ReadAllText(".env"),
    new DotEnvSerializerOptions(DotEnvSerializerDefaults.Web));
```

## Round-trip an INI document, preserving comments

The mutable INI DOM keeps every comment line:

```csharp
using Bodu.Text.Ini.Nodes;

IniObject root = IniNode.Parse(File.ReadAllBytes("app.ini"));

root["server"].AsObject()["port"].AsValue().Value = "9090";

var metrics = new IniObject();
metrics["enabled"] = new IniValue("true");
root["metrics"] = metrics;

File.WriteAllBytes("app.ini", root.ToUtf8Bytes());   // original comments intact
```

## Read typed INI values

```csharp
using Bodu.Text.Ini;
using Bodu.Text.Serialization;

sealed class AppConfig
{
    public string? Environment { get; set; }              // global key
    public Dictionary<string, string>? Logging { get; set; } // [logging] section
}

AppConfig config = IniSerializer.Deserialize<AppConfig>(
    File.ReadAllText("app.ini"),
    new IniSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower });
```

## Handle malformed input

Malformed input throws the format's `*FormatException` with the source position; lenient dialect knobs live on the reader options:

```csharp
using Bodu.Text.Delimited.Reader;

var lenient = new DelimitedReaderOptions
{
    FieldCountBehavior = DelimitedFieldCountBehavior.Ragged,
    MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord,
};
using DelimitedDocument dirty = DelimitedDocument.Parse(bytes, lenient);
```

## Where to go next

- [Core concepts](concepts.md) — the quartet vocabulary.
- [Parser policies](parser-policies.md) — every strictness knob.
- The [format guides](../../guides/formats/index.md) — deeper recipes per format.
