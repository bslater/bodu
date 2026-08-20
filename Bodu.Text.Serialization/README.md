# Bodu.Text.Serialization

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

The shared serialization core for the Bodu `System.Text.Json`-shaped text serializers ([Bencode](https://www.nuget.org/packages/Bodu.Text.Bencode), [TOML](https://www.nuget.org/packages/Bodu.Text.Toml), [YAML](https://www.nuget.org/packages/Bodu.Text.Yaml), [Delimited](https://www.nuget.org/packages/Bodu.Text.Delimited), [DotEnv](https://www.nuget.org/packages/Bodu.Text.DotEnv), [INI](https://www.nuget.org/packages/Bodu.Text.Ini)). It carries the format-agnostic pieces those libraries have in common: the declarative attribute family, the shaping enums, the serialization callback interfaces, and the property naming policies — all in the single `Bodu.Text.Serialization` namespace, so one `[PropertyName]` or `NamingPolicy.CamelCase` works identically across every format.

## Installation

```shell
dotnet add package Bodu.Text.Serialization
```

Targets `net8.0`. You rarely install this package directly — each per-format serializer package references it, so it arrives transitively with `Bodu.Text.Toml`, `Bodu.Text.Yaml`, and their siblings. Reference it directly only when a model assembly should carry the mapping attributes without depending on any specific format.

## API shape

| Type(s) | Role |
|---|---|
| `[PropertyName]` / `[Ignore]` / `[Converter]` / `[PropertyOrder]` / `[Required]` / `[Include]` / `[ExtensionData]` / `[Constructor]` / `[ObjectCreationHandling]` / `[NamingPolicy]` / `[UnmappedMemberHandling]` / `[StringEnumMemberName]` | The declarative member-shaping attribute family, rooted at `SerializationAttribute`. Applied once on a model, honored by every Bodu text serializer. |
| `IgnoreCondition`, `ObjectCreationHandling`, `UnmappedMemberHandling`, `KnownNamingPolicy` | The shaping enums the attributes and per-format options share (conditional omission, replace-vs-populate on read, unknown-key policy, attribute-addressable naming policies). |
| `IOnSerializing` / `IOnSerialized` / `IOnDeserializing` / `IOnDeserialized` | Serialization lifecycle callbacks a model opts into by implementing the interface. |
| `NamingPolicy` (with `CamelCase`, `SnakeCaseLower` / `SnakeCaseUpper`, `KebabCaseLower` / `KebabCaseUpper`) | Property naming policies converting CLR member names to wire keys. |

```csharp
using Bodu.Text.Serialization;

public sealed class Profile
{
    [PropertyName("display-name")]
    public string DisplayName { get; set; } = "Ada";

    [Ignore(Condition = IgnoreCondition.WhenWritingNull)]
    public string? Comment { get; set; }
}

// The same model serializes consistently through any sibling:
//   TomlSerializer.Serialize(profile)   ->  display-name = "Ada"
//   YamlSerializer.Serialize(profile)   ->  display-name: Ada
```

## Shared engine source

Beyond the compiled assembly, the package's repository folder also hosts the `shared/**` engine source — the metadata resolver (`MetadataResolver` / `TypeMetadata` / `PropertyMetadata`), the structural converter factories (nullable / dictionary / collection / object), and the converter pipeline — which the per-format packages compile into themselves under their format symbol (`BENCODE`, `TOML`, `YAML`, …). That source is a repository-level implementation detail: it ships inside each format assembly, not in this package, so this package stays a small attribute/contract library whose only dependency is `Bodu.Core`.

## Testing

The package has no test project of its own: every public type is exercised end to end by the consumer test suites (`Bodu.Text.Bencode.Test`, `Bodu.Text.Toml.Test`, `Bodu.Text.Yaml.Test`, `Bodu.Text.Delimited.Test`, `Bodu.Text.DotEnv.Test`, `Bodu.Text.Ini.Test`), which validate the attribute family, callbacks, and naming policies against each format's wire form.

## License

MIT. © Bodu Pty. Ltd.
