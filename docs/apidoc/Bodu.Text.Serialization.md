---
uid: Bodu.Text.Serialization
---

![Bodu.Text.Serialization](~/images/hero-text-serialization.svg)

## Purpose

**Bodu.Text.Serialization** is the shared, format-agnostic core of the Bodu text serializers: the attribute family a model declares once, the naming policies, the ignore / object-creation / unmapped-member enums, and the four serialization-callback interfaces. It contains no reader, writer, or serializer — those live in the six packages that reference it: <xref:Bodu.Text.Bencode>, <xref:Bodu.Text.Toml>, and <xref:Bodu.Text.Yaml> (which additionally compile this package's shared metadata resolver and converter engine under a per-format symbol), and the line formats <xref:Bodu.Text.Delimited>, <xref:Bodu.Text.DotEnv>, and <xref:Bodu.Text.Ini> (which read the attributes, naming policy, and callbacks through their own string-wire binders). Depends on `Bodu.Core` only, so a model or contracts assembly can reference it without taking a dependency on any wire format.

## Static documentation

- **[Bodu.Text.Serialization introduction](~/docs/serialization/core/index.md)** — the attribute family, naming policies, behavior enums, callbacks, and which serializers honor which parts.
- **[Bodu.Text.Serialization core concepts](~/docs/serialization/core/concepts.md)** — attribute-versus-options precedence, naming-policy order, required members and constructor binding, extension data, unmapped members, callback order, options freezing and thread safety, converter resolution.
- **[Bodu.Text.Serialization getting started](~/docs/serialization/core/getting-started.md)** — install, one DTO serialized by TOML and YAML, a callback, a naming policy.
- **[Bodu serializers](~/docs/serialization/index.md)** and the **[line formats](~/docs/formats/index.md)** — the two families that consume this package.

## Key types

**Attribute family** (every attribute derives from the abstract base)

- <xref:Bodu.Text.Serialization.SerializationAttribute> — abstract base that lets the family be discovered as a unit.
- <xref:Bodu.Text.Serialization.PropertyNameAttribute> — pins a member's wire key, overriding the CLR name and any naming policy.
- <xref:Bodu.Text.Serialization.IgnoreAttribute> — excludes a member unconditionally or under an <xref:Bodu.Text.Serialization.IgnoreCondition>.
- <xref:Bodu.Text.Serialization.IncludeAttribute> — binds non-public accessors and surfaces public fields regardless of `IncludeFields`.
- <xref:Bodu.Text.Serialization.PropertyOrderAttribute> — relative write order (ascending; unannotated members are `0`).
- <xref:Bodu.Text.Serialization.RequiredAttribute> — deserialization fails when the key is absent (same effect as C# `required`).
- <xref:Bodu.Text.Serialization.ConverterAttribute> — names the converter type for a member or a type.
- <xref:Bodu.Text.Serialization.ExtensionDataAttribute> — the single dictionary-shaped member that captures unmapped keys.
- <xref:Bodu.Text.Serialization.ConstructorAttribute> — selects the deserialization constructor when several are declared.
- <xref:Bodu.Text.Serialization.NamingPolicyAttribute> — applies a <xref:Bodu.Text.Serialization.KnownNamingPolicy> to one type's members.
- <xref:Bodu.Text.Serialization.UnmappedMemberHandlingAttribute> — per-type skip/reject policy for unknown keys.
- <xref:Bodu.Text.Serialization.ObjectCreationHandlingAttribute> — per-member or per-type replace/populate policy for collections.
- <xref:Bodu.Text.Serialization.StringEnumMemberNameAttribute> — the string written for one enumeration member when serialized by name.

**Naming policies**

- <xref:Bodu.Text.Serialization.NamingPolicy> — abstract base with `ConvertName(string)` and the singletons `CamelCase`, `SnakeCaseLower`, `SnakeCaseUpper`, `KebabCaseLower`, `KebabCaseUpper`.
- <xref:Bodu.Text.Serialization.KnownNamingPolicy> — names each singleton (plus `Unspecified`) for declarative selection.

**Behavior enums**

- <xref:Bodu.Text.Serialization.IgnoreCondition> — `Never`, `Always`, `WhenWritingDefault`, `WhenWritingNull`.
- <xref:Bodu.Text.Serialization.ObjectCreationHandling> — `Replace`, `Populate`.
- <xref:Bodu.Text.Serialization.UnmappedMemberHandling> — `Skip`, `Disallow`.

**Serialization callbacks**

- <xref:Bodu.Text.Serialization.IOnSerializing> / <xref:Bodu.Text.Serialization.IOnSerialized> — before / after a value's members are written.
- <xref:Bodu.Text.Serialization.IOnDeserializing> / <xref:Bodu.Text.Serialization.IOnDeserialized> — after construction but before members are assigned / after every member is assigned.

## Example

```csharp
using Bodu.Text.Serialization;
using Bodu.Text.Toml;
using Bodu.Text.Yaml;

[NamingPolicy(KnownNamingPolicy.SnakeCaseLower)]
public sealed class ServiceProfile : IOnDeserialized
{
    [Required]
    public string DisplayName { get; set; } = "";

    [PropertyName("listen-port")]
    public int Port { get; set; }

    [Ignore(Condition = IgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    void IOnDeserialized.OnDeserialized()
    {
        if (Port is < 1 or > 65535)
            throw new InvalidOperationException("Port is out of range.");
    }
}

var profile = new ServiceProfile { DisplayName = "Billing API", Port = 8080 };

string toml = TomlSerializer.Serialize(profile);   // display_name = "Billing API" / listen-port = 8080
string yaml = YamlSerializer.Serialize(profile);   // display_name: Billing API   / listen-port: 8080
```

## Notes

- **Closest wins.** A member attribute beats a type attribute, which beats the serializer options — the same rule for naming, ignore conditions, converters, object creation, and unmapped-member handling.
- **Two consumption modes.** Bencode, TOML, and YAML honor the whole family through the shared converter engine; Delimited, DotEnv, and INI honor `[PropertyName]`, `[Ignore]`, `[PropertyOrder]`, `[Required]` (INI and DotEnv), the options-level naming policy, and the callbacks, and convert scalars with the invariant culture.
- **No number handling here.** Numeric representation is a per-format option; this package carries nothing format-specific.
- **See also:** the [serializer guides hub](~/guides/serialization/index.md) and the [line-format guides](~/guides/formats/index.md).
