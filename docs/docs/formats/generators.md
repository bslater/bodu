---
title: Bodu.Text.Formats.Generators — Reflection-free binding
---

# Bodu.Text.Formats.Generators — Reflection-free binding

**Bodu.Text.Formats.Generators** is an incremental Roslyn source generator that removes reflection from the [Delimited](delimited/index.md) and [INI](ini/index.md) serializer paths. Annotate a `partial` POCO with `[DelimitedRecord]` or `[IniSection]` and the generator emits, at build time, an `IDelimitedRecordFactory<TRecord>` or `IIniSectionFactory<TSection>` implementation exposed as a static `DelimitedFactory` / `IniFactory` property on the type. Pass that factory to the serializer's factory overloads and the reflection binder is never entered — which is what makes the path safe for trimming and ahead-of-time compilation.

Part of the **[Text & Serialization](../topics/text-and-serialization.md)** topic, alongside the [line formats](index.md).

> [!IMPORTANT]
> The package is **Preview** and **not yet packable** (`IsPackable=false`): it builds and tests as a Roslyn component inside the solution and is consumed by **project reference** today. It targets `netstandard2.0` and pins `Microsoft.CodeAnalysis.CSharp` 4.8.0 as a private, build-time dependency. Packaging follows in a later release wave; nothing in the generated code or the consuming API will change when it does.

## What the generator emits

For every annotated type the generator adds one file, `<Namespace>.<Type>.Delimited.g.cs` or `<Namespace>.<Type>.Ini.g.cs`, containing a second declaration of the partial type with:

- a public static property — `DelimitedFactory` of type <xref:Bodu.Text.Delimited.IDelimitedRecordFactory`1>, or `IniFactory` of type <xref:Bodu.Text.Ini.IIniSectionFactory`1> — holding a singleton;
- a private nested class implementing the interface: a static array of the resolved wire names in declaration order (`Headers` / `Keys`), a `GetFields` / `GetEntries` method that formats each member, a `Create` method that constructs the instance and binds decoded strings back, and a `Bind` helper that matches names ordinally first and case-insensitively as a fallback.

The factory maps the type's **public read/write instance properties in declaration order**, honors `[PropertyName]` for the wire name, skips members annotated `[Ignore]` (with the default `IgnoreCondition.Always`), formats and parses scalars with `InvariantCulture`, and maps a `Nullable<T>` member to and from the empty string — mirroring the runtime reflection binders so the two paths are interchangeable.

## Wiring the generator into a project

Reference the generator project as an **analyzer**, not as a runtime assembly, and reference the format package(s) whose marker attributes and factory interfaces the POCO uses:

```xml
<ItemGroup>
  <ProjectReference Include="..\Bodu.Text.Delimited\src\Bodu.Text.Delimited.csproj" />
  <ProjectReference Include="..\Bodu.Text.Ini\src\Bodu.Text.Ini.csproj" />
  <ProjectReference Include="..\Bodu.Text.Formats.Generators\src\Bodu.Text.Formats.Generators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

`OutputItemType="Analyzer"` makes the compiler load the generator into the consuming compilation; `ReferenceOutputAssembly="false"` keeps the generator out of the consumer's runtime references (the solution's own test project omits the second attribute only because its driver-based diagnostic tests instantiate the generator at runtime). Once the package ships, the conventional analyzer `PackageReference` form (`PrivateAssets="all"`) replaces the project reference; the generated code is identical.

To inspect the emitted source, set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` and look under `obj/…/generated/Bodu.Text.Formats.Generators/`.

## Rules a POCO must satisfy

| Rule | Why |
|---|---|
| Annotated with <xref:Bodu.Text.Delimited.DelimitedRecordAttribute> or <xref:Bodu.Text.Ini.IniSectionAttribute> (a class, struct, record, or record struct). | The generator keys on the attribute's metadata name. |
| Every declaration of the type — **and of every containing type** — is `partial`. | The factory is added as a second partial declaration (`BTFG001` otherwise). |
| Not generic, and not nested inside a generic type. | A static factory property cannot be expressed for an open type (`BTFG003` otherwise). |
| Constructible with `new T()` — a parameterless constructor reachable from inside the type. | `Create` instantiates the type before binding; the nested factory can reach a private constructor. |
| Mapped members are **public instance properties with a public getter and a public, non-`init` setter**. | Static members, indexers, non-public accessors, `init`-only setters, and fields are not mapped (silently). |
| Each mapped property's type is a supported scalar or its `Nullable<T>`: `string`, `bool`, `char`, `sbyte`/`byte`/`short`/`ushort`/`int`/`uint`/`long`/`ulong`, `float`/`double`/`decimal`, `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, or any enum. | Anything else — `Uri`, collections, nested objects — is skipped with `BTFG002`; the factory is still generated for the remaining members. |
| `[Ignore]` with the default condition excludes a member; `[Ignore(Condition = …)]` with any *other* condition leaves it mapped. | The factory has no write-time conditional path — the wire is string-only. |

Two behaviors differ from the reflection binder by design: the options-level `PropertyNamingPolicy` is **not** applied by a factory (its header and key names are fixed at compile time, so pin names with `[PropertyName]`), and `IncludeFields` has no effect (fields are never mapped).

## Diagnostics

All three diagnostics share the category `Bodu.Text.Formats.Generators` and are enabled by default.

| Id | Severity | Message | Cause |
|---|---|---|---|
| `BTFG001` | Error | The type '{0}' is annotated with [{1}] but it (or a containing type) is not declared partial, so no factory is generated | The annotated type, or one of the types it is nested in, lacks the `partial` modifier. No source is emitted. |
| `BTFG002` | Warning | The property '{0}' on '{1}' has type '{2}', which is not a supported scalar; the generated factory skips it | A public read/write property has a type outside the scalar set above. The factory is emitted without that member. |
| `BTFG003` | Error | The type '{0}' is annotated with [{1}] but is generic (or nested in a generic type), so no factory is generated | The annotated type has type parameters, or a containing type does. No source is emitted. |

Because `BTFG001` and `BTFG003` are errors, the missing `DelimitedFactory` / `IniFactory` property surfaces as a compile error at the call site as well; fix the declaration rather than suppressing the diagnostic.

## The serializer overloads that consume a factory

The factory overloads are exact counterparts of the reflection entry points, with the factory as an extra parameter, and carry **no** trimming annotations:

| Serializer | Factory overloads |
|---|---|
| <xref:Bodu.Text.Delimited.DelimitedSerializer> | `Serialize<TRecord>(IEnumerable<TRecord> records, IDelimitedRecordFactory<TRecord> factory, DelimitedSerializerOptions? options = null)` → `string`; `Serialize<TRecord>(IBufferWriter<byte> destination, IEnumerable<TRecord> records, IDelimitedRecordFactory<TRecord> factory, …)`; `Deserialize<TRecord>(string text, IDelimitedRecordFactory<TRecord> factory, …)`, `Deserialize<TRecord>(ReadOnlySpan<byte> utf8Delimited, …)`, and `Deserialize<TRecord>(Stream source, …)` → `List<TRecord>`. |
| <xref:Bodu.Text.Ini.IniSerializer> | `SerializeSection<TSection>(string sectionName, TSection value, IIniSectionFactory<TSection> factory, IniSerializerOptions? options = null)` → `string`; `SerializeSection<TSection>(IBufferWriter<byte> destination, string sectionName, TSection value, IIniSectionFactory<TSection> factory, …)`; `DeserializeSection<TSection>(string text, string sectionName, IIniSectionFactory<TSection> factory, …)` and `DeserializeSection<TSection>(ReadOnlySpan<byte> utf8Ini, string sectionName, …)` → `TSection`. |

Dialect options still apply on the factory path — `Delimiter`, `Quote`, and `NoHeader` for Delimited (a headerless document binds **positionally** in `Headers` order), and the duplicate-section / duplicate-key policies for INI (merging runs before the factory sees the entries). An empty INI section name writes or binds the document's **global keys**; a section that is absent from the input raises <xref:Bodu.Text.Ini.IniSerializationException>. `DotEnvSerializer` has no factory surface — it remains reflection-only.

## Writing a factory by hand

The interfaces are small enough to implement directly when the generator cannot be used — a type you do not own, a non-scalar column, or a build that cannot host analyzers. The contract for Delimited: `Headers` (column names in field order), `GetFields(record)` (values in `Headers` order), and `Create(fields, headers)` where `headers` is the document's header row, or **empty** for a headerless document, in which case bind positionally.

```csharp
using System.Globalization;
using Bodu.Text.Delimited;

public sealed class TradeFactory : IDelimitedRecordFactory<Trade>
{
    private static readonly string[] s_headers = ["Symbol", "Quantity", "Price", "traded_at", "Venue"];

    public IReadOnlyList<string> Headers => s_headers;

    public string[] GetFields(Trade record) =>
    [
        record.Symbol,
        record.Quantity.ToString(CultureInfo.InvariantCulture),
        record.Price.ToString(CultureInfo.InvariantCulture),
        record.TradedAt.ToString(CultureInfo.InvariantCulture),
        record.Venue ?? string.Empty,
    ];

    public Trade Create(string[] fields, IReadOnlyList<string> headers)
    {
        var trade = new Trade();
        IReadOnlyList<string> names = headers.Count == 0 ? s_headers : headers;
        for (int i = 0; i < fields.Length && i < names.Count; i++)
        {
            switch (names[i])
            {
                case "Symbol": trade.Symbol = fields[i]; break;
                case "Quantity": trade.Quantity = int.Parse(fields[i], CultureInfo.InvariantCulture); break;
                case "Price": trade.Price = decimal.Parse(fields[i], CultureInfo.InvariantCulture); break;
                case "traded_at": trade.TradedAt = DateTimeOffset.Parse(fields[i], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind); break;
                case "Venue": trade.Venue = fields[i].Length == 0 ? null : fields[i]; break;
            }
        }

        return trade;
    }
}
```

Format with `InvariantCulture` on the way out and parse with it on the way in, and the hand-written factory produces byte-identical output to both the generated factory and the reflection binder. The INI contract is the same shape over key/value pairs: `Keys`, `GetEntries(section)` returning `IEnumerable<KeyValuePair<string, string>>`, and `Create(entries)`.

## Trimming and AOT

The reflection entry points of all three line-format serializers are annotated: `DelimitedSerializer.Serialize<T>` / `Deserialize<TRecord>` / `SerializeAsync` / `DeserializeAsyncEnumerableAsync`, `IniSerializer.Serialize<T>` / `Deserialize<T>` and their stream and async variants, and every `DotEnvSerializer` entry point carry `[RequiresUnreferencedCode]` **and** `[RequiresDynamicCode]`. A project that publishes trimmed or native-AOT therefore reports `IL2026` / `IL3050` at each reflection call site. The factory overloads carry neither attribute, so routing Delimited and INI binding through a generated (or hand-written) factory is the supported way to publish trimmed or AOT-compiled binaries with these packages. The structured serializers are annotated as well — `TomlSerializer` with both attributes, `YamlSerializer` with `[RequiresUnreferencedCode]` — and have no reflection-free path today.

## Where to go next

- **[Source generator guide](../../guides/formats/source-generator.md)** — the end-to-end walk-through: POCO, generated factory, both serializers, parity, diagnostics.
- **[Line formats introduction](index.md)**, **[Core concepts](concepts.md)**, and **[Getting started](getting-started.md)** — the umbrella trio.
- **[Bodu.Text.Delimited](delimited/index.md)** and **[Bodu.Text.Ini](ini/index.md)** — the two packages whose serializers accept a factory.
- **[Package matrix](../package-matrix.md)** — status and dependencies.
