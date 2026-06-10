---
title: Bodu.Text.Serialization — Getting started
---

# Getting started

## Install

Add the package for the format you need; each one pulls in the shared engine.

```shell
dotnet add package Bodu.Text.Serialization.Toml
dotnet add package Bodu.Text.Serialization.Bencode
```

Both target `net8.0`.

## A first TOML round trip

```csharp
using Bodu.Text.Serialization.Toml;

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
using Bodu.Text.Serialization.Bencode;

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
using Bodu.Text.Serialization;

var options = new TomlSerializerOptions
{
    PropertyNamingPolicy = FormatNamingPolicy.SnakeCaseLower,
};

// "Host" is written as "host", "Port" as "port".
string text = TomlSerializer.Serialize(config, options);
```

Or pin a single member's name with <xref:Bodu.Text.Serialization.FormatPropertyNameAttribute>.

## Next

- **[Using TOML](../../guides/serialization/toml.md)** — type mapping, spec versions, and streams.
- **[Using Bencode](../../guides/serialization/bencode.md)** — byte strings, canonical ordering, and unsupported kinds.
- **[Writing converters](../../guides/serialization/converters.md)** — custom shapes with `FormatConverter<T>`.
