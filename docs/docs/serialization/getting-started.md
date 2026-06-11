---
title: Bodu serializers — Getting started
---

# Getting started

## Install

Add the package for the format you need. Each is self-contained — there is no shared engine package to add.

```shell
dotnet add package Bodu.Text.Toml
dotnet add package Bodu.Text.Bencode
```

Both target `net8.0`.

## A first TOML round trip

```csharp
using Bodu.Text.Toml;

public sealed class ServerConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public bool Secure { get; set; }
}

var config = new ServerConfig { Host = "localhost", Port = 8080, Secure = true };

string text = TomlSerializer.Serialize(config);
// Host = "localhost"
// Port = 8080
// Secure = true

ServerConfig back = TomlSerializer.Deserialize<ServerConfig>(text);
```

## A first Bencode round trip

```csharp
using Bodu.Text.Bencode;

public sealed class FileEntry
{
    public string Name { get; set; } = "";
    public long Length { get; set; }
}

byte[] payload = BencodeSerializer.Serialize(new FileEntry { Name = "ubuntu.iso", Length = 1024 });
// d6:Lengthi1024e4:Name10:ubuntu.isoe   (dictionary keys in canonical order)

FileEntry entry = BencodeSerializer.Deserialize<FileEntry>(payload);
```

## Rename members

```csharp
using Bodu.Text.Toml;

var options = new TomlSerializerOptions
{
    PropertyNamingPolicy = TomlNamingPolicy.SnakeCaseLower,
};

// "Host" is written as "host", "Port" as "port".
string text = TomlSerializer.Serialize(config, options);
```

Or pin a single member's name with `[TomlPropertyName]` (and `[BencodePropertyName]` for Bencode).

## Edit a document without a model

```csharp
using Bodu.Text.Toml.Nodes;

TomlNode node = TomlNode.Parse(utf8Toml)!;
node["server"]!["port"] = 9090;
byte[] back = node.ToUtf8Bytes();
```

## Next

- **[Using TOML](../../guides/serialization/toml.md)** — type mapping, spec versions, the DOMs, and streams.
- **[Using Bencode](../../guides/serialization/bencode.md)** — byte strings, canonical ordering, and unsupported kinds.
- **[Writing converters](../../guides/serialization/converters.md)** — custom shapes with `BencodeConverter<T>` / `TomlConverter<T>`.
