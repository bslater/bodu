---
title: Bodu.Text.Yaml — Getting started
---

# Getting started

This page installs **Bodu.Text.Yaml**, runs a first round trip, renames members, and edits a document with the DOM. For the format specifics see the [introduction](index.md); for the moving parts see [core concepts](concepts.md).

## Install

Bodu.Text.Yaml targets `net8.0`. Add the package:

```bash
dotnet add package Bodu.Text.Yaml
```

## A first round trip

`Serialize<T>` writes an object graph to a YAML `string`; `Deserialize<T>` binds the text back to a type. Note that `Deserialize<T>` returns `T?`, so use `!` where the document is known to be non-null:

```csharp
using Bodu.Text.Yaml;

public sealed class ServerConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
}

var config = new ServerConfig { Host = "localhost", Port = 8080 };

string yaml = YamlSerializer.Serialize(config);
// Host: localhost
// Port: 8080

ServerConfig back = YamlSerializer.Deserialize<ServerConfig>(yaml)!;
// back.Port → 8080
```

`Deserialize<T>` also accepts UTF-8 bytes directly:

```csharp
ReadOnlySpan<byte> utf8 = Encoding.UTF8.GetBytes(yaml);
ServerConfig fromBytes = YamlSerializer.Deserialize<ServerConfig>(utf8)!;
```

There are no `Stream` or async overloads — read a stream into a `string` or a byte buffer first.

## Rename members

A naming policy renames every member; <xref:Bodu.Text.Serialization.PropertyNameAttribute> pins a single one and always wins over the policy:

```csharp
using Bodu.Text.Serialization;
using Bodu.Text.Yaml;

public sealed class Endpoint
{
    [PropertyName("display-name")]
    public string DisplayName { get; set; } = "Health check";

    public int MaxRetryCount { get; set; } = 5;
}

var options = new YamlSerializerOptions
{
    PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
};

string yaml = YamlSerializer.Serialize(new Endpoint(), options);
// display-name: Health check
// max_retry_count: 5
```

The naming policies are `CamelCase`, `SnakeCaseLower` / `SnakeCaseUpper`, and `KebabCaseLower` / `KebabCaseUpper`; the `YamlSerializerDefaults.Web` preset selects camel-case with case-insensitive matching. Member shaping beyond renaming and ignoring is covered in [Mapping attributes](../../../guides/serialization/yaml/attributes.md).

## Edit a document without a model

When you do not have a POCO, parse to the mutable <xref:Bodu.Text.Yaml.Nodes.YamlNode> DOM, index into the tree, build new scalars with `YamlValue.Create(…)`, and write the document back with `ToYamlString()`:

```csharp
using Bodu.Text.Yaml.Nodes;

string source = """
    server:
      host: localhost
      port: 8080
    """;

YamlNode root = YamlNode.Parse(source)!;
root["server"]!["port"] = YamlValue.Create(9090);

string updated = root.ToYamlString();
// server:
//   host: localhost
//   port: 9090
```

`YamlValue.Create` has overloads for `string`, `long`, `double`, and `bool`. To read a value back out, use `GetValue<T>()` on a `YamlValue`.

## Where to go next

- **[Using YAML](../../../guides/serialization/yaml/using.md)** — the full set of worked patterns, including both DOMs and multi-document streams.
- **[Mapping attributes](../../../guides/serialization/yaml/attributes.md)** — naming policies, `[PropertyName]`, `[Ignore]`, the wider attribute family, and the options flags.
- **[Writing converters](../../../guides/serialization/yaml/converters.md)** — custom shapes with `YamlConverter<T>`.
- **[Core concepts](concepts.md)** and the **[introduction](index.md)** — the family vocabulary and the YAML format specifics.
- **[Bodu serializers introduction](../index.md)** — the family parent.
- **API reference** — <xref:Bodu.Text.Yaml.YamlSerializer>, <xref:Bodu.Text.Yaml.YamlSerializerOptions>.
