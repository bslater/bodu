---
title: Migrating from System.Text.Json
---

# Migrating from System.Text.Json

The Bodu serializers are shaped after `System.Text.Json`: a static `<Format>Serializer` facade, a `<Format>SerializerOptions` instance that freezes on first use, `<Format>Converter<T>` and `<Format>ConverterFactory` base classes, a mutable `<Format>Node` DOM, and a read-only `<Format>Document` DOM. Most of a migration is therefore mechanical — drop the `Json` prefix from the attributes, swap the options and converter base types, and pick a format. This guide is the lookup table for that rename, followed by the places where the formats genuinely behave differently from JSON.

The tables cover the six serializers. **Bencode**, **TOML**, and **YAML** are the *structured* serializers: they compile the full converter engine, so every row applies. **Delimited**, **DotEnv**, and **INI** are the *line formats*: their wire is string-only and they use a small binder instead of a converter pipeline, so several rows are marked as not applicable — the authoritative list is the [who-uses-what table](../../docs/serialization/core/index.md#who-uses-what) in the shared package's introduction.

## Pattern 1 — Swap the `using` directives

| `System.Text.Json` | Bodu | Holds |
|---|---|---|
| `System.Text.Json` | `Bodu.Text.Toml` · `Bodu.Text.Yaml` · `Bodu.Text.Bencode` · `Bodu.Text.Delimited` · `Bodu.Text.DotEnv` · `Bodu.Text.Ini` | The serializer facade, its options and defaults enum, and the two exception types. |
| `System.Text.Json.Serialization` (attributes, callbacks) | `Bodu.Text.Serialization` | The attribute family, `NamingPolicy`, the behavior enums, and the four callback interfaces — one shared package for all six formats. |
| `System.Text.Json.Serialization` (converter bases) | `Bodu.Text.<Format>.Serialization` | `<Format>Converter<T>`, `<Format>ConverterFactory`, and the string-enum converters. Structured serializers only. |
| `System.Text.Json` (reader/writer) | `Bodu.Text.<Format>.Reader` · `Bodu.Text.<Format>.Writer` | The `ref struct` token reader and writer a converter uses. |
| `System.Text.Json.Nodes` | `Bodu.Text.<Format>.Nodes` | The mutable DOM. |
| `System.Text.Json` (`JsonDocument`) | `Bodu.Text.<Format>.Document` | The read-only DOM. |

A model file that previously needed only `using System.Text.Json.Serialization;` now needs only `using Bodu.Text.Serialization;` — the same annotated type serializes to every Bodu format without further changes.

## Pattern 2 — Rename the attributes

Every Bodu attribute lives in `Bodu.Text.Serialization` and derives from <xref:Bodu.Text.Serialization.SerializationAttribute>.

| `System.Text.Json` | Bodu | Notes |
|---|---|---|
| `[JsonPropertyName("…")]` | <xref:Bodu.Text.Serialization.PropertyNameAttribute> `[PropertyName("…")]` | Same placement, same precedence over the naming policy. |
| `[JsonIgnore]` | <xref:Bodu.Text.Serialization.IgnoreAttribute> `[Ignore]` | `Condition` defaults to `Always`. |
| `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` | `[Ignore(Condition = IgnoreCondition.WhenWritingNull)]` | <xref:Bodu.Text.Serialization.IgnoreCondition> has the same four members (`Never`, `Always`, `WhenWritingDefault`, `WhenWritingNull`). |
| `[JsonInclude]` | <xref:Bodu.Text.Serialization.IncludeAttribute> `[Include]` | Binds non-public accessors and surfaces a public field. Structured serializers only. |
| `[JsonPropertyOrder(n)]` | <xref:Bodu.Text.Serialization.PropertyOrderAttribute> `[PropertyOrder(n)]` | Ascending; unannotated members are `0`. |
| `[JsonRequired]` / C# `required` | <xref:Bodu.Text.Serialization.RequiredAttribute> `[Required]` / C# `required` | Not honored by Delimited. |
| `[JsonConverter(typeof(…))]` | <xref:Bodu.Text.Serialization.ConverterAttribute> `[Converter(typeof(…))]` | Member, type, or enum placement. Structured serializers only. |
| `[JsonExtensionData]` | <xref:Bodu.Text.Serialization.ExtensionDataAttribute> `[ExtensionData]` | The member's *type* differs per format — see [Pattern 8](#pattern-8--extension-data). Structured serializers only. |
| `[JsonConstructor]` | <xref:Bodu.Text.Serialization.ConstructorAttribute> `[Constructor]` | Structured serializers only. |
| `[JsonUnmappedMemberHandling(…)]` | <xref:Bodu.Text.Serialization.UnmappedMemberHandlingAttribute> `[UnmappedMemberHandling(…)]` | `Skip` / `Disallow`. Structured serializers only. |
| `[JsonObjectCreationHandling(…)]` | <xref:Bodu.Text.Serialization.ObjectCreationHandlingAttribute> `[ObjectCreationHandling(…)]` | `Replace` / `Populate`. Structured serializers only. |
| `[JsonStringEnumMemberName("…")]` | <xref:Bodu.Text.Serialization.StringEnumMemberNameAttribute> `[StringEnumMemberName("…")]` | Structured serializers only. |
| `[JsonNumberHandling(…)]` | — | No member-level equivalent. YAML has an options-level `NumberHandling` (`Strict` / `AllowFloatToInteger`); the other formats have none. |
| `[JsonPolymorphic]` / `[JsonDerivedType]` | — | No attribute-driven polymorphism; write a converter factory with a discriminator — [TOML](toml/polymorphic-converters.md), [YAML](yaml/polymorphic-converters.md), [Bencode](bencode/polymorphic-converters.md). |
| — | <xref:Bodu.Text.Serialization.NamingPolicyAttribute> `[NamingPolicy(KnownNamingPolicy.…)]` | No STJ analogue: a type-level policy that overrides the options-level one. Structured serializers only. |

### Before and after

A model that uses the common attributes, first as written for `System.Text.Json`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ServerConfig : IJsonOnDeserialized
{
    [JsonPropertyOrder(-1)]
    public string Name { get; set; } = "";

    [JsonPropertyName("listen_port")]
    public int Port { get; set; }

    [JsonRequired]
    public string Host { get; set; } = "";

    [JsonIgnore]
    public string? CachedBanner { get; set; }

    void IJsonOnDeserialized.OnDeserialized()
    {
        if (Port is < 1 or > 65535)
            throw new JsonException("Port is out of range.");
    }
}
```

The same model for the Bodu family — the attributes lose their `Json` prefix, the callback interface loses its `Json` infix, and the validation throws the format's serialization exception:

```csharp
using Bodu.Text.Serialization;
using Bodu.Text.Yaml;

public sealed class ServerConfig : IOnDeserialized
{
    [PropertyOrder(-1)]
    public string Name { get; set; } = "";

    [PropertyName("listen_port")]
    public int Port { get; set; }

    [Required]
    public string Host { get; set; } = "";

    [Ignore]
    public string? CachedBanner { get; set; }

    void IOnDeserialized.OnDeserialized()
    {
        if (Port is < 1 or > 65535)
            throw new YamlSerializationException("Port is out of range.");
    }
}
```

One annotated type now serializes to any format in the family:

```csharp
var config = new ServerConfig { Name = "edge", Host = "0.0.0.0", Port = 8443 };

string toml = TomlSerializer.Serialize(config);
string yaml = YamlSerializer.Serialize(config);
```

```toml
Name = "edge"
listen_port = 8443
Host = "0.0.0.0"
```

```yaml
Name: edge
listen_port: 8443
Host: 0.0.0.0
```

`[Required]` behaves as `[JsonRequired]` does — an absent key fails the read, with the format's serialization exception in place of `JsonException`:

```csharp
TomlSerializer.Deserialize<ServerConfig>("Name = \"edge\"\nlisten_port = 8443");
// → throws TomlSerializationException: Required member 'Host' was not present in the input for type 'ServerConfig'.

YamlSerializer.Deserialize<ServerConfig>("Name: edge\nlisten_port: 8443");
// → throws YamlSerializationException: The required member 'Host' of type 'ServerConfig' was not present in the input.
```

## Pattern 3 — Port the options

| `JsonSerializerOptions` | Bodu | Notes |
|---|---|---|
| `JsonSerializerOptions` | <xref:Bodu.Text.Toml.TomlSerializerOptions> · <xref:Bodu.Text.Yaml.YamlSerializerOptions> · <xref:Bodu.Text.Bencode.BencodeSerializerOptions> · <xref:Bodu.Text.Delimited.DelimitedSerializerOptions> · <xref:Bodu.Text.DotEnv.DotEnvSerializerOptions> · <xref:Bodu.Text.Ini.IniSerializerOptions> | One options type per format; none is interchangeable with another. |
| `new JsonSerializerOptions(JsonSerializerDefaults.Web)` | `new <Format>SerializerOptions(<Format>SerializerDefaults.Web)` | The `Web` preset means different things per format, and INI offers `Strict` instead — see [Serializer options: freezing, caching, and thread safety](options-and-lifetime.md#pattern-6--start-from-a-defaults-preset). |
| `PropertyNamingPolicy` | `PropertyNamingPolicy` | Takes a <xref:Bodu.Text.Serialization.NamingPolicy> ([Pattern 5](#pattern-5--naming-policies)). |
| `PropertyNameCaseInsensitive` | `PropertyNameCaseInsensitive` | Bencode defaults to `true`; every other format defaults to `false`. |
| `DefaultIgnoreCondition` | `DefaultIgnoreCondition` | As in STJ, `Always` is rejected at the setter (`ArgumentOutOfRangeException`). DotEnv and INI default to `WhenWritingNull`; the structured serializers default to `Never`. Delimited has no such option. |
| `IncludeFields` | `IncludeFields` | Same semantics. |
| `Converters` | `Converters` | `IList<<Format>Converter>`; registration order matters ([resolution order](options-and-lifetime.md#pattern-3--understand-converter-resolution-order)). Structured serializers only. |
| `MakeReadOnly()` / `IsReadOnly` | `MakeReadOnly()` / `IsReadOnly` | Structured serializers expose both. The line formats expose `IsReadOnly` and a pre-frozen static `Default`, but freeze only on first use — there is no public `MakeReadOnly()`. |
| `MaxDepth` | `MaxDepth` | Structured serializers only; `0` selects the format default (64). |
| `UnmappedMemberHandling` | `UnmappedMemberHandling` | Structured serializers only. |
| `PreferredObjectCreationHandling` | `PreferredObjectCreationHandling` | Structured serializers only. |
| `NumberHandling` | YAML `NumberHandling` only | `YamlNumberHandling.Strict` / `AllowFloatToInteger`. |
| `WriteIndented` | — | The formats are line-oriented and always readable; YAML indentation width is a writer option (`YamlWriterOptions.IndentSize`), not a serializer option. |
| `System.Text.Json.Serialization.ReferenceHandler` | — | Object cycles are detected and fail the write (see the YAML callbacks guide's [invocation order](yaml/callbacks.md#invocation-order)); there is no reference-preserving mode. |
| `AllowTrailingCommas`, `ReadCommentHandling` | — | Comments are part of the TOML, YAML, DotEnv, and INI grammars and are always accepted. |
| `System.Text.Json.JsonSerializerOptions.TypeInfoResolver` / source generation | — | The structured serializers are reflection-based (their `Serialize` / `Deserialize` carry `RequiresUnreferencedCode`). Reflection-free binding exists only for Delimited and INI through the [source generator](../formats/source-generator.md). |

The shape of an options block carries over directly, including the collection initializer on `Converters`:

```csharp
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new PointConverter() },
};
json.MakeReadOnly(populateMissingResolver: true);
```

```csharp
var yamlOptions = new YamlSerializerOptions(YamlSerializerDefaults.Web)
{
    PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = IgnoreCondition.WhenWritingNull,
    Converters = { new PointConverter() },
};
yamlOptions.MakeReadOnly();
```

> [!NOTE]
> The Bodu `MakeReadOnly()` takes no argument: there is no resolver to populate. The STJ overload without the `populateMissingResolver` flag throws on .NET 8 when no `System.Text.Json.JsonSerializerOptions.TypeInfoResolver` has been set; the Bodu call never does.

## Pattern 4 — Port a converter

A converter keeps the STJ shape — `CanConvert`, `Read`, `Write` — but reads through the format's reader and writes through its writer. The signatures differ per format:

| Format | Base | `Read` | `Write` |
|---|---|---|---|
| STJ | `JsonConverter<T>` | `T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)` | `void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)` |
| YAML | <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1> | `T Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)` | `void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options)` |
| TOML | <xref:Bodu.Text.Toml.Serialization.TomlConverter`1> | `T Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)` | `void Write(Utf8TomlWriter writer, T value, TomlSerializerOptions options)` |
| Bencode | <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1> | `T Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)` | `void Write(Utf8BencodeWriter writer, T value, BencodeSerializerOptions options)` |

Note the TOML row: a converter reads through <xref:Bodu.Text.Toml.Reader.TomlDocumentReader> — the *normalized* reader that projects header-defined and inline tables onto one `StartTable` / `PropertyName` / value / `EndTable` sequence — not the raw `Utf8TomlReader`. The factory bases follow suit: `JsonConverterFactory` becomes <xref:Bodu.Text.Yaml.Serialization.YamlConverterFactory>, <xref:Bodu.Text.Toml.Serialization.TomlConverterFactory>, or <xref:Bodu.Text.Bencode.Serialization.BencodeConverterFactory>, each with the same `CanConvert(Type)` / `CreateConverter(Type, options)` pair.

A scalar converter, before:

```csharp
public sealed class PointConverter : JsonConverter<Point>
{
    public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string[] parts = reader.GetString()!.Split(',');
        return new Point(int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options) =>
        writer.WriteStringValue($"{value.X},{value.Y}");
}
```

And after, for YAML — the reader's `GetString()` returns a non-nullable string, and the writer's string method is `WriteString`:

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Serialization;
using Bodu.Text.Yaml.Writer;

public sealed class PointConverter : YamlConverter<Point>
{
    public override Point Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        string[] parts = reader.GetString().Split(',');
        return new Point(int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    public override void Write(Utf8YamlWriter writer, Point value, YamlSerializerOptions options) =>
        writer.WriteString($"{value.X},{value.Y}");
}
```

The TOML port differs only in the reader type:

```csharp
using Bodu.Text.Toml;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

public sealed class PointTomlConverter : TomlConverter<Point>
{
    public override Point Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        string[] parts = reader.GetString().Split(',');
        return new Point(int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    public override void Write(Utf8TomlWriter writer, Point value, TomlSerializerOptions options) =>
        writer.WriteString($"{value.X},{value.Y}");
}
```

Registered on the options above, a `Shape { Point Origin; string? Label }` writes as `origin: 1,2` in YAML and `origin = "1,2"` in TOML. Three STJ habits carry over unchanged: `CanConvert` defaults to an exact type match, so a converter for a base type does not apply to subclasses; the reader is positioned on the first token of the value on entry; and a converter must throw the format's *serialization* exception for a well-formed value that does not fit, never the format exception. The per-format converter guides — [TOML](toml/converters.md), [YAML](yaml/converters.md), [Bencode](bencode/converters.md) — cover the reader and writer surfaces in full.

## Pattern 5 — Naming policies

| `JsonNamingPolicy` | <xref:Bodu.Text.Serialization.NamingPolicy> |
|---|---|
| `JsonNamingPolicy.CamelCase` | `NamingPolicy.CamelCase` |
| `JsonNamingPolicy.SnakeCaseLower` | `NamingPolicy.SnakeCaseLower` |
| `JsonNamingPolicy.SnakeCaseUpper` | `NamingPolicy.SnakeCaseUpper` |
| `JsonNamingPolicy.KebabCaseLower` | `NamingPolicy.KebabCaseLower` |
| `JsonNamingPolicy.KebabCaseUpper` | `NamingPolicy.KebabCaseUpper` |
| subclass `JsonNamingPolicy`, override `ConvertName` | subclass `NamingPolicy`, override `ConvertName` |

The five singletons produce the same names as their STJ counterparts:

```csharp
JsonNamingPolicy.SnakeCaseLower.ConvertName("MaxRetryCount");   // max_retry_count
NamingPolicy.SnakeCaseLower.ConvertName("MaxRetryCount");       // max_retry_count
NamingPolicy.CamelCase.ConvertName("MaxRetryCount");            // maxRetryCount
NamingPolicy.KebabCaseUpper.ConvertName("MaxRetryCount");       // MAX-RETRY-COUNT
```

Two additions have no STJ analogue: the type-level `[NamingPolicy(KnownNamingPolicy.…)]` attribute, which overrides the options-level policy for one type, and the <xref:Bodu.Text.Serialization.KnownNamingPolicy> enum that names the singletons for it. Enum member names are governed by the string-enum converters (`WriteEnumsAsStrings` in YAML; `<Format>StringEnumConverter` elsewhere), not by `PropertyNamingPolicy` — the same split STJ makes.

## Pattern 6 — Callbacks

| `System.Text.Json` | `Bodu.Text.Serialization` |
|---|---|
| `System.Text.Json.Serialization.IJsonOnSerializing` | <xref:Bodu.Text.Serialization.IOnSerializing> |
| `System.Text.Json.Serialization.IJsonOnSerialized` | <xref:Bodu.Text.Serialization.IOnSerialized> |
| `System.Text.Json.Serialization.IJsonOnDeserializing` | <xref:Bodu.Text.Serialization.IOnDeserializing> |
| `System.Text.Json.Serialization.IJsonOnDeserialized` | <xref:Bodu.Text.Serialization.IOnDeserialized> |

Each interface has the same single parameterless `void` method, is detected by the object-mapping converter without registration, and does not fire for a type claimed by a custom converter — exactly the STJ rules. All six serializers honor the four hooks, the line formats included. The pipeline position of each is tabulated in the [core concepts](../../docs/serialization/core/concepts.md#callback-order) and illustrated in the [TOML](toml/callbacks.md), [YAML](yaml/callbacks.md), and [Bencode](bencode/callbacks.md) callback guides.

## Pattern 7 — The DOMs

| `System.Text.Json` | Bodu (structured) | Bodu (line formats) |
|---|---|---|
| `JsonNode` / `JsonObject` / `JsonArray` / `JsonValue` | `<Format>Node` / `<Format>Object` / `<Format>Array` / `<Format>Value` in `Bodu.Text.<Format>.Nodes` | `DelimitedNode` (`Parse` returns a `DelimitedArray` of records); `DotEnvNode` / `IniNode` (`Parse` returns the `DotEnvObject` / `IniObject` root); values are `<Format>Value` strings. |
| `JsonNode.Parse(string)` | `YamlNode.Parse(string)`; `TomlNode.Parse(ReadOnlySpan<byte>)`; `BencodeNode.Parse(ReadOnlySpan<byte>)` | `DelimitedNode.Parse(ReadOnlySpan<byte>)`; `DotEnvNode.Parse(string)`; `IniNode.Parse(string)` |
| `JsonDocument` / `JsonElement` / `JsonProperty` | `<Format>Document` / `<Format>Element` / `<Format>Property` in `Bodu.Text.<Format>.Document` | Same names — `DelimitedDocument`, `DotEnvDocument`, `IniDocument` — each disposable with a `RootElement`. |
| `JsonSerializer.SerializeToNode` / `SerializeToDocument` | Bencode only (`BencodeSerializer.SerializeToNode` / `SerializeToDocument`); for TOML and YAML, serialize to text and parse it. | — |
| A member typed `JsonElement` / `JsonNode` | Supported: a member typed `<Format>Element`, `<Format>Document`, or `<Format>Node` binds through the DOM bridge converters. | Not supported — the line formats have no DOM bridge; a member must be a scalar (or, for INI, a section object). |

The access idioms translate one for one. The read-only element exposes 64-bit getters (`GetInt64`, `GetDouble`) rather than STJ's full numeric family:

```csharp
JsonNode jn = JsonNode.Parse("{\"port\": 8080, \"tags\": [\"a\",\"b\"]}")!;
int jsonPort = jn["port"]!.GetValue<int>();                    // 8080

YamlNode yn = YamlNode.Parse("port: 8080\ntags: [a, b]")!;
long yamlPort = yn["port"]!.AsValue().GetValue<long>();        // 8080
string tag = yn["tags"]![1]!.AsValue().GetValue<string>();     // b

TomlNode tn = TomlNode.Parse("port = 8080\ntags = [\"a\", \"b\"]"u8)!;
long tomlPort = tn["port"]!.GetValue<long>();                  // 8080

using YamlDocument yd = YamlDocument.Parse("port: 8080");
long yamlDocPort = yd.RootElement.GetProperty("port").GetInt64();   // 8080

using TomlDocument td = TomlDocument.Parse("port = 8080");
long tomlDocPort = td.RootElement.GetProperty("port").GetInt64();   // 8080
```

Editing and re-emitting a node works as with `JsonNode` — `ToYamlString()` for YAML, `ToUtf8Bytes()` for TOML and INI, `ToByteArray()` for Bencode:

```csharp
yn.AsObject()["port"] = YamlValue.Create(9090L);
string editedYaml = yn.ToYamlString();
// port: 9090
// tags:
//   - a
//   - b

tn.AsObject()["port"] = 9090;
string editedToml = Encoding.UTF8.GetString(tn.ToUtf8Bytes());
// port = 9090
// tags = ["a", "b"]
```

## Pattern 8 — Extension data

`[JsonExtensionData]` accepts `Dictionary<string, object>` or `Dictionary<string, JsonElement>`. The Bodu formats are stricter, and differ from one another, about the member's type:

| Format | Accepted extension-data member types | Captured values |
|---|---|---|
| YAML | Any type assignable from `IDictionary<string, object?>` (`Dictionary<string, object?>` is the usual choice). | Loosely-typed scalars per YAML's implicit typing — `string`, `long`, `double`, `bool` — and nested `Dictionary` / `List` values. |
| TOML | `TomlObject`, `IDictionary<string, TomlNode?>`, or `Dictionary<string, TomlNode?>`. | DOM nodes. |
| Bencode | `BencodeObject`, `IDictionary<string, BencodeNode?>`, or `Dictionary<string, BencodeNode?>`. | DOM nodes. |
| Delimited · DotEnv · INI | Not supported. | — |

A member of any other type fails at first use with `InvalidOperationException` (for TOML: *The extension-data member 'Extra' on type '…' must be a TomlObject or IDictionary<string, TomlNode?>.*). The YAML and TOML forms:

```csharp
public sealed class ExtensibleYaml
{
    public string Name { get; set; } = "";

    [ExtensionData]
    public Dictionary<string, object?>? Extra { get; set; }
}

public sealed class ExtensibleToml
{
    public string Name { get; set; } = "";

    [ExtensionData]
    public Dictionary<string, TomlNode?>? Extra { get; set; }
}
```

```csharp
var round = YamlSerializer.Deserialize<ExtensibleYaml>("Name: edge\nregion: eu\ntier: 2\n")!;
// round.Extra → region = "eu" (String), tier = 2 (Int64)
YamlSerializer.Serialize(round);
// Name: edge
// region: eu
// tier: 2

var roundT = TomlSerializer.Deserialize<ExtensibleToml>("Name = \"edge\"\nregion = \"eu\"\ntier = 2\n")!;
TomlSerializer.Serialize(roundT);
// Name = "edge"
// region = "eu"
// tier = 2
```

## Behavioral differences

The rename is the easy half. These are the points where a format's value model, not the API, changes what your program observes.

### Null handling is per format

STJ writes `null` for a null member unless told otherwise. The Bodu formats each do what their grammar allows — for the same `{ Port = 8080, Name = null }` instance with default options:

| Format | Output for a null member | Why |
|---|---|---|
| YAML | `Name: null` | YAML has a null scalar. |
| TOML | key omitted | TOML has no null; the member is skipped. |
| Bencode | key omitted | Bencode has no null. |
| DotEnv · INI | key omitted | `DefaultIgnoreCondition` defaults to `WhenWritingNull`. |
| Delimited | `8080,` — an empty field | Every record has every column; an empty field reads back as `null` for a nullable member. |

On read, an absent key leaves the member at its initializer in every format, as in STJ.

### The line formats carry strings only

Delimited, DotEnv, and INI have no typed tokens: every value is text, converted to and from the member type with the invariant culture by the serializer-local binder. Consequently `[Converter]`, custom converters, `[Include]`, `[Constructor]`, `[ExtensionData]`, and the enum converters do not apply to them, and there is no `Utf8JsonReader`-style typed getter beyond `GetString()`. A member of type `int` binds from the text `3` in all three; a value that does not parse fails with the format's serialization exception and the `FormatException` as its inner exception (see [Errors across the line formats](../formats/error-handling.md)).

### TOML is token-strict; YAML coerces

`Utf8JsonReader` rejects a string where a number is expected, and so does TOML: binding `Count = "3"` to an `int` member throws *TomlSerializationException: Expected an integer but found 'String'.* YAML's scalars are implicitly typed, so its converters coerce across scalar kinds — `Count: '3'` binds `3` to an `int` — and writing a string that *looks* like a number quotes it so it reads back as a string:

```csharp
YamlSerializer.Serialize(new Named { Name = "3" });
// Name: "3"
```

The flip side is that a loosely typed read produces the YAML scalar's resolved type, not a string:

```csharp
var typed = YamlSerializer.Deserialize<Dictionary<string, object?>>("count: 3\nratio: 0.5\nflag: true\nlegacy: yes\nname: '3'\n")!;
// count  → Int64   3
// ratio  → Double  0.5
// flag   → Boolean true
// legacy → String  yes      (1.2 core schema: only true/false are booleans)
// name   → String  3        (quoted, so never a number)
```

Opting into `YamlSpecVersion.V1_1` on the options restores the 1.1 typing, under which `legacy: yes` resolves to a boolean — see the [YAML introduction](../../docs/serialization/yaml/index.md).

### Two exception types instead of one

`JsonException` covers both malformed text and failed binding. Every Bodu format splits the two: a `<Format>FormatException` for syntactically invalid input (a `FormatException` subclass carrying the position), and a `<Format>SerializationException` for a well-formed document that does not fit the target type — a missing `[Required]` member, an unmapped key under `Disallow`, a converter rejection, a depth overrun. Catch both when a caller cannot distinguish them; catch only the serialization exception around validation logic such as the `OnDeserialized` check above.

### An empty YAML stream is `null`

`JsonSerializer.Deserialize<T>("")` throws. `TomlSerializer.Deserialize<T>("")` returns an instance with every member at its initializer (an empty TOML document is an empty root table), while `YamlSerializer.Deserialize<T>("")` returns `null` — an empty YAML stream has no document. Use `{}` for an "empty mapping" in YAML, and note that the YAML `Deserialize<T>` signature is nullable (`T?`) for this reason.

### Polymorphism is a converter, not an attribute

There is no `[JsonPolymorphic]` / `[JsonDerivedType]`. On write, the structured serializers already dispatch on the *runtime* type of a value held in a base-typed or `object`-typed member; on read, a member typed as an abstract class or interface fails unless a converter factory supplies the concrete type from a discriminator. The recipe is in the per-format polymorphic-converter guides.

## Where to go next

- [Bodu.Text.Serialization](../../docs/serialization/core/index.md) — the shared package: every attribute, enum, policy, and callback, and the who-uses-what table for the line formats.
- [Core concepts](../../docs/serialization/core/concepts.md) — precedence, required members and constructor binding, callback order, converter resolution.
- [Serializer options: freezing, caching, and thread safety](options-and-lifetime.md) — the options lifecycle STJ users expect, across all six serializers.
- Per-format mapping-attribute guides — [TOML](toml/attributes.md), [YAML](yaml/attributes.md), [Bencode](bencode/attributes.md) — and the line-format guides — [Delimited](../formats/delimited.md), [DotEnv](../formats/dotenv.md), [INI](../formats/ini.md).
- Runnable samples — [TOML](../../samples/toml.md), [YAML](../../samples/yaml.md), [Bencode](../../samples/bencode.md), [line formats](../../samples/formats.md).
- [Bodu serializer guides](index.md) and the [Text & Serialization guides](../topics/text-and-serialization.md).
