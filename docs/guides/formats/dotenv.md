---
title: Using DotEnv
---

# Using DotEnv

`Bodu.Text.DotEnv` reads and writes `.env` files — a flat object of `KEY=value` entries with `export` prefixes, quoting, and comments. Values are deliberately **literal**: no `${VAR}` interpolation happens at parse time.

## Pattern 1 — query a document

```csharp
using Bodu.Text.DotEnv.Document;

using DotEnvDocument env = DotEnvDocument.Parse(File.ReadAllBytes(".env"));
DotEnvElement root = env.RootElement;

string url = root.GetProperty("DATABASE_URL").GetString();
bool hasOptional = root.TryGetProperty("OPTIONAL_KEY", out DotEnvElement value);

foreach (DotEnvProperty property in root.EnumerateObject())
{
    Console.WriteLine($"{property.Name} = {property.Value.GetString()}");
}
```

## Pattern 2 — typed settings via the serializer

The `Web` defaults apply the SCREAMING_SNAKE_CASE naming policy with case-insensitive matching, so conventional env keys bind onto PascalCase members:

```csharp
using Bodu.Text.DotEnv;

sealed class Settings
{
    public string? AppEnv { get; set; }       // APP_ENV
    public int AppPort { get; set; }          // APP_PORT
    public string? DatabaseUrl { get; set; }  // DATABASE_URL
}

Settings settings = DotEnvSerializer.Deserialize<Settings>(
    envText, new DotEnvSerializerOptions(DotEnvSerializerDefaults.Web));
```

`Deserialize<Dictionary<string, string>>` binds the whole file as a dictionary. The write direction (`Serialize`) emits `KEY=value` lines; `DotEnvSerializerOptions.WriteExportPrefix` adds the `export` keyword.

## Pattern 3 — author and round-trip with the mutable DOM

```csharp
using Bodu.Text.DotEnv.Nodes;

DotEnvObject root = DotEnvNode.Parse(File.ReadAllBytes(".env"));
root["APP_PORT"] = new DotEnvValue("9090");
root.SetExport("APP_PORT", true);              // emit "export APP_PORT=9090"
File.WriteAllBytes(".env", root.ToUtf8Bytes());
```

The mutable DOM preserves each entry's `export` flag through the round trip.

## Quoting rules

- **Double quotes** delimit values, resolve escape sequences, and may span lines.
- **Single quotes** delimit literal values.
- **Unquoted** values are trimmed and end at an inline `#` comment.
- An empty value (`KEY=`) is a real value, distinct from an absent key.

`DotEnvReaderOptions` can disable the `export` prefix or inline comments (`DisallowExportPrefix`, `DisallowInlineComments`).

## Exceptions

`DotEnvFormatException` for malformed input (unterminated quote, missing `=`), with line/offset. `DotEnvSerializationException` for binding failures.

## When *not* to use it

Anything with sections or nesting (INI or TOML), and layered configuration (`Bodu.Text.Configuration`).

## See also

- [Streams and token-level I/O](streaming.md) for the `Utf8DotEnvReader` token loop.
- [Using INI](ini.md) for sectioned configuration.
