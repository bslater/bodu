---
title: Bodu.Text.Toml — Getting started
---

# Getting started

## Install

Add the package. Bodu.Text.Toml is self-contained — there is no shared engine package to add.

```shell
dotnet add package Bodu.Text.Toml
```

It targets `net8.0`.

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

Or pin a single member's name with `[TomlPropertyName]`.

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

`TomlDocument` is disposable — wrap it in `using` and copy out any values that must outlive it.

## Round-trip through a Stream

`TomlSerializer` reads and writes `Stream` directly, with async variants:

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

Synchronous `Stream` overloads are also provided.

## When something goes wrong

Failures split into two exception types, so you can tell *bad input* apart from *wrong type*:

- A **malformed document** — input the grammar rejects — raises <xref:Bodu.Text.Toml.TomlFormatException>. Because TOML files are edited by hand, the exception carries the **line, column, and offset** of the failure.
- A document that **parses but cannot bind** to your type — a type mismatch, a missing required member, a value the format cannot represent — raises <xref:Bodu.Text.Toml.TomlSerializationException>.

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

- **[Bodu.Text.Toml introduction](index.md)** — what is specific to TOML: the rich value model, spec versions, positioned diagnostics.
- **[Core concepts](concepts.md)** — the TOML vocabulary, including the full value-mapping table.
- **[Using TOML](../../../guides/serialization/toml/using.md)** — type mapping, spec versions, the DOMs, and streams.
- **[Writing converters](../../../guides/serialization/toml/converters.md)** — custom shapes with `TomlConverter<T>`.
- **[Text & Serialization topic overview](../../topics/text-and-serialization.md)** — where the serializers sit among the codecs and document formats.
