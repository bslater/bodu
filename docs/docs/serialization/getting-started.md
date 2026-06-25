---
title: Bodu serializers — Getting started
---

# Getting started

## Install

Add the package for the format you need. Each is self-contained — there is no shared engine package to add.

```shell
dotnet add package Bodu.Text.Toml
dotnet add package Bodu.Text.Bencode
dotnet add package Bodu.Text.Yaml
```

All target `net8.0`.

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

## A first YAML round trip

```csharp
using Bodu.Text.Yaml;

public sealed class ServiceConfig
{
    public string Name { get; set; } = "";
    public int Replicas { get; set; }
    public bool Enabled { get; set; }
}

string yaml = YamlSerializer.Serialize(new ServiceConfig { Name = "api", Replicas = 3, Enabled = true });
// Name: api
// Replicas: 3
// Enabled: true

ServiceConfig back = YamlSerializer.Deserialize<ServiceConfig>(yaml);
```

By default YAML uses the 1.2 core schema, so an unquoted `no` or `yes` stays a string — set `SpecVersion = YamlSpecVersion.V1_1` on the options to opt in to 1.1 Boolean typing.

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

When you need to change a value but do not want a POCO, parse to the **mutable DOM**:

```csharp
using Bodu.Text.Toml.Nodes;

TomlNode node = TomlNode.Parse(utf8Toml)!;
node["server"]!["port"] = 9090;
byte[] back = node.ToUtf8Bytes();
```

## Read a document without a model

For inspection only, the **read-only DOM** is the lighter choice — a low-allocation view over the parsed buffer, walked through `RootElement`:

```csharp
using Bodu.Text.Toml.Document;

string toml = """
[server]
host = "localhost"
port = 8080
""";

using TomlDocument doc = TomlDocument.Parse(toml);

TomlElement server = doc.RootElement.GetProperty("server");
string host = server.GetProperty("host").GetString();   // "localhost"
long   port = server.GetProperty("port").GetInt64();    // 8080
```

`TomlDocument` is disposable — wrap it in `using` and copy out any values that must outlive it. The same shape exists for Bencode (`BencodeDocument.Parse(payload)` and its `RootElement`).

## Round-trip through a Stream

Both serializers read and write `Stream` directly, with async variants:

```csharp
using Bodu.Text.Toml;

await using (FileStream stream = File.Create("server.toml"))
{
    await TomlSerializer.SerializeAsync(stream, config);
}

await using (FileStream stream = File.OpenRead("server.toml"))
{
    ServerConfig loaded = await TomlSerializer.DeserializeAsync<ServerConfig>(stream);
}
```

`BencodeSerializer` exposes the identical `SerializeAsync` / `DeserializeAsync` pair over `Stream`, plus synchronous `Stream` overloads on both libraries.

## When something goes wrong

Failures split into two exception types per library, so you can tell *bad input* apart from *wrong type*:

- A **malformed document** — input the grammar rejects — raises <xref:Bodu.Text.Toml.TomlFormatException> or <xref:Bodu.Text.Bencode.BencodeFormatException>. The TOML exception carries the **line, column, and offset** of the failure, since TOML files are edited by hand.
- A document that **parses but cannot bind** to your type — a type mismatch, a missing required member, a value the format cannot represent — raises <xref:Bodu.Text.Toml.TomlSerializationException> or <xref:Bodu.Text.Bencode.BencodeSerializationException>.

```csharp
try
{
    ServerConfig loaded = TomlSerializer.Deserialize<ServerConfig>(text);
}
catch (TomlFormatException ex)
{
    Console.Error.WriteLine($"Malformed TOML at line {ex.LineNumber}, column {ex.ColumnNumber}: {ex.Message}");
}
catch (TomlSerializationException ex)
{
    Console.Error.WriteLine($"Document does not match ServerConfig: {ex.Message}");
}
```

## Where to go next

- **[Bodu.Text.Toml introduction](toml.md)** — what is specific to TOML: the rich value model, spec versions, positioned diagnostics.
- **[Bodu.Text.Bencode introduction](bencode.md)** — what is specific to Bencode: byte strings, canonical output, the kinds it cannot represent.
- **[Using TOML](../../guides/serialization/toml.md)** — type mapping, spec versions, the DOMs, and streams.
- **[Using Bencode](../../guides/serialization/bencode.md)** — byte strings, canonical ordering, and unsupported kinds.
- **[Writing converters](../../guides/serialization/converters.md)** — custom shapes with `BencodeConverter<T>` / `TomlConverter<T>`.
- **[Text & Serialization topic overview](../topics/text-and-serialization.md)** — where the serializer twins sit among the codecs and document formats.
