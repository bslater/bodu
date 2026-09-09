---
title: Bodu.Text.Serialization — Getting started
---

# Getting started

Unfamiliar with terms like *naming policy*, *ignore condition*, *required member*, *extension data*, or *callback*? Read [Core concepts](concepts.md) first.

## Install

You rarely add this package by hand: every format package (`Bodu.Text.Toml`, `Bodu.Text.Yaml`, `Bodu.Text.Bencode`, `Bodu.Text.Delimited`, `Bodu.Text.DotEnv`, `Bodu.Text.Ini`) restores it transitively. Add it directly when a project only *declares* models and should not depend on any one format — a shared DTO or contracts assembly:

```shell
dotnet add package Bodu.Text.Serialization
```

Targets `net8.0`. Depends on `Bodu.Core` only.

A typical split — the model library references the core package; the application references the formats it actually uses:

```xml
<!-- Contracts.csproj — DTOs annotated once -->
<ItemGroup>
  <PackageReference Include="Bodu.Text.Serialization" Version="…" />
</ItemGroup>

<!-- App.csproj — picks the wire formats -->
<ItemGroup>
  <ProjectReference Include="..\Contracts\Contracts.csproj" />
  <PackageReference Include="Bodu.Text.Toml" Version="…" />
  <PackageReference Include="Bodu.Text.Yaml" Version="…" />
</ItemGroup>
```

## Annotate once, serialize twice

The DTO below uses five members of the attribute family and nothing format-specific. It then goes through **TOML** and **YAML** unchanged.

```csharp
using Bodu.Text.Serialization;

[NamingPolicy(KnownNamingPolicy.SnakeCaseLower)]
public sealed class ServiceProfile
{
    [Required]
    public string DisplayName { get; set; } = "";

    [PropertyName("listen-port")]
    public int Port { get; set; }

    [Ignore(Condition = IgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [Ignore]
    public string? ApiKey { get; set; }

    public int MaxRetryCount { get; set; } = 3;
}
```

```csharp
using Bodu.Text.Toml;
using Bodu.Text.Yaml;

var profile = new ServiceProfile
{
    DisplayName = "Billing API",
    Port = 8080,
    Description = null,
    ApiKey = "s3cret",
    MaxRetryCount = 5,
};

string toml = TomlSerializer.Serialize(profile);
// display_name = "Billing API"
// listen-port = 8080
// max_retry_count = 5

string yaml = YamlSerializer.Serialize(profile);
// display_name: Billing API
// listen-port: 8080
// max_retry_count: 5

ServiceProfile fromToml = TomlSerializer.Deserialize<ServiceProfile>(toml);
ServiceProfile fromYaml = YamlSerializer.Deserialize<ServiceProfile>(yaml)!;
```

Reading the output against the attributes:

- `[NamingPolicy(KnownNamingPolicy.SnakeCaseLower)]` turned `DisplayName` and `MaxRetryCount` into `display_name` / `max_retry_count`.
- `[PropertyName("listen-port")]` pinned one key and was **not** passed through the policy.
- `[Ignore(Condition = IgnoreCondition.WhenWritingNull)]` dropped `description` because it was `null`; had it been set, both documents would carry it.
- `[Ignore]` removed `ApiKey` entirely — it is neither written nor read back (`fromToml.ApiKey` is `null`).
- `[Required]` has no effect on the write, but a document missing `display_name` fails to bind with the format's serialization exception:

```csharp
try
{
    TomlSerializer.Deserialize<ServiceProfile>("listen-port = 1\n");
}
catch (TomlSerializationException ex)
{
    // Required member 'display_name' was not present in the input for type '…ServiceProfile'.
    Console.Error.WriteLine(ex.Message);
}
```

## A callback

Implement the interfaces explicitly so the hooks stay off the type's public surface. `OnSerializing` runs before the members are written, so the stamped value lands in the document; `OnDeserialized` runs after every member is assigned, so it can validate across members.

```csharp
using Bodu.Text.Serialization;
using Bodu.Text.Toml;

public sealed class Manifest : IOnSerializing, IOnDeserialized
{
    public string Name { get; set; } = "";
    public int Version { get; set; }
    public long SavedAt { get; set; }

    void IOnSerializing.OnSerializing() =>
        SavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    void IOnDeserialized.OnDeserialized()
    {
        if (Version <= 0)
            throw new InvalidOperationException("Version must be positive.");
    }
}

string text = TomlSerializer.Serialize(new Manifest { Name = "demo", Version = 2 });
// Name = "demo"
// Version = 2
// SavedAt = 1788924353

Manifest back = TomlSerializer.Deserialize<Manifest>(text);          // SavedAt restored
TomlSerializer.Deserialize<Manifest>("Name = \"x\"\nVersion = 0\n");  // throws InvalidOperationException from OnDeserialized
```

The same type fires the same hooks under `YamlSerializer`, `BencodeSerializer`, `IniSerializer`, `DotEnvSerializer`, and `DelimitedSerializer`.

## A naming policy

Set a policy on the options to rename every member of every type, or annotate a single type with `[NamingPolicy]` to rename just that one. The type-level attribute beats the options, and `[PropertyName]` beats both.

```csharp
using Bodu.Text.Serialization;
using Bodu.Text.Yaml;

var options = new YamlSerializerOptions { PropertyNamingPolicy = NamingPolicy.KebabCaseLower };

string yaml = YamlSerializer.Serialize(new Manifest { Name = "demo", Version = 1 }, options);
// name: demo
// version: 1
// saved-at: 1788924353
```

A custom convention is one subclass with one method:

```csharp
using Bodu.Text.Serialization;
using Bodu.Text.Toml;

public sealed class PrefixNamingPolicy : NamingPolicy
{
    public override string ConvertName(string name) =>
        "app_" + NamingPolicy.SnakeCaseLower.ConvertName(name);
}

var options = new TomlSerializerOptions { PropertyNamingPolicy = new PrefixNamingPolicy() };

string toml = TomlSerializer.Serialize(new Manifest { Name = "demo", Version = 1 }, options);
// app_name = "demo"
// app_version = 1
// app_saved_at = 1788924353
```

Serializing the annotated profile type from the first sample with those same options still produces `display_name = …`: its type-level `[NamingPolicy]` wins over `PropertyNamingPolicy`.

## Reuse the options

Once an options instance has been used it is frozen — `IsReadOnly` reports `true`, and setting a property throws `InvalidOperationException`. Configure one instance, then share it; it caches per-type metadata and is safe to use from many threads.

```csharp
var options = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.CamelCase };
options.MakeReadOnly();                                   // optional — first use freezes anyway

_ = TomlSerializer.Serialize(profile, options);
options.PropertyNamingPolicy = NamingPolicy.SnakeCaseLower;   // throws InvalidOperationException
```

## Where to go next

- **[Bodu.Text.Serialization introduction](index.md)** — the full attribute family and which serializers honor which parts.
- **[Core concepts](concepts.md)** — precedence, naming order, required members, extension data, callback order, options freezing, converter resolution.
- **[TOML attribute patterns](../../../guides/serialization/toml/attributes.md)** — the remaining attributes (`[Include]`, `[Constructor]`, `[ExtensionData]`, `[UnmappedMemberHandling]`, `[ObjectCreationHandling]`, `[Converter]`, `[StringEnumMemberName]`) as worked recipes.
- **[Line formats getting started](../../formats/getting-started.md)** — the same attributes over CSV, `.env`, and INI.
- **[Text & Serialization topic overview](../../topics/text-and-serialization.md)** — where the serializers sit among the codecs and document formats.
