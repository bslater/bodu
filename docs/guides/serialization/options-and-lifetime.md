---
title: Serializer options — freezing, caching, and thread safety
---

# Serializer options: freezing, caching, and thread safety

Every Bodu serializer takes a `<Format>SerializerOptions` instance, and every one of the six treats that instance the same way `System.Text.Json` treats `JsonSerializerOptions`: it is mutable until it is first used, it freezes at that moment, and from then on it is an immutable, cache-bearing object that is safe to share across threads and cheaper to reuse than to rebuild. This guide covers that lifecycle once for the family — <xref:Bodu.Text.Toml.TomlSerializerOptions>, <xref:Bodu.Text.Yaml.YamlSerializerOptions>, <xref:Bodu.Text.Bencode.BencodeSerializerOptions>, <xref:Bodu.Text.Delimited.DelimitedSerializerOptions>, <xref:Bodu.Text.DotEnv.DotEnvSerializerOptions>, and <xref:Bodu.Text.Ini.IniSerializerOptions> — and calls out where the three *structured* serializers (Bencode, TOML, YAML), which carry a converter pipeline, differ from the three *line formats* (Delimited, DotEnv, INI), which do not.

| | Bencode · TOML · YAML | Delimited · DotEnv · INI |
|---|---|---|
| Freezes on first `Serialize` / `Deserialize` | yes | yes |
| `IsReadOnly` | yes | yes |
| Public `MakeReadOnly()` | yes | no — freeze only by first use |
| Pre-frozen static `Default` | no (a private default is used when you pass `null`) | yes — `<Format>SerializerOptions.Default`, and what `null` resolves to |
| `Converters` list, converter cache, `GetConverter` | yes | no converter pipeline |
| Per-type metadata cache | yes | binder metadata, per call |

## Pattern 1 — Configure once, then let first use freeze it

Set every property and register every converter *before* the first call that uses the instance. That call flips `IsReadOnly` and every setter afterwards throws:

```csharp
var options = new YamlSerializerOptions();
bool before = options.IsReadOnly;                // false

YamlSerializer.Serialize(new Settings(), options);
bool after = options.IsReadOnly;                 // true

options.PropertyNamingPolicy = NamingPolicy.CamelCase;
// → throws InvalidOperationException: Serializer options cannot be changed after the options instance has been used to serialize or deserialize.

options.Converters.Add(new UpperTagConverter());
// → throws InvalidOperationException: Serializer options cannot be changed after the options instance has been used to serialize or deserialize.
```

The exception type is always `InvalidOperationException`; the message text comes from each package's resources and differs by family:

| Family | Message |
|---|---|
| YAML | *Serializer options cannot be changed after the options instance has been used to serialize or deserialize.* |
| TOML · Bencode | *Serializer options cannot be modified after they have been used to serialize or deserialize a value.* |
| Delimited · DotEnv · INI | *The serializer options are read-only and cannot be modified after first use.* |

The `Converters` collection is guarded the same way as the scalar properties — `Add`, `Insert`, `Remove`, `Clear`, and the indexer setter all go through the same check — so a converter registered "later" is a bug that surfaces on the first line that tries it, not a silently ignored registration.

## Pattern 2 — Freeze eagerly with `MakeReadOnly`

On the structured serializers, call `MakeReadOnly()` to freeze without a serialization call — useful when the options are built in one place and handed out from another, so that a misplaced mutation fails at the hand-off rather than at some later first use:

```csharp
var eager = new TomlSerializerOptions();
eager.MakeReadOnly();
bool frozen = eager.IsReadOnly;                  // true

eager.IncludeFields = true;
// → throws InvalidOperationException: Serializer options cannot be modified after they have been used to serialize or deserialize a value.
```

`MakeReadOnly()` is idempotent, and `GetConverter(Type)` calls it implicitly — asking the options which converter serves a type is itself a "use". The line-format options have no public `MakeReadOnly()`: their `IsReadOnly` becomes `true` on the first serializer call, and the static `Default` instance is created already frozen:

```csharp
var ini = new IniSerializerOptions();
bool iniBefore = ini.IsReadOnly;                 // false
IniSerializer.Serialize(new Settings(), ini);
bool iniAfter = ini.IsReadOnly;                  // true

DelimitedSerializerOptions.Default.NoHeader = true;
// → throws InvalidOperationException: The serializer options are read-only and cannot be modified after first use.
```

> [!NOTE]
> Freezing is by instance and there is no `Clone`. To vary one setting from an existing configuration, construct a second options object (Pattern 7).

## Pattern 3 — Understand converter resolution order

On the structured serializers the frozen instance owns the converter pipeline. For a requested type it consults, in order:

1. `[Converter(typeof(…))]` on the **member** — resolved by the metadata layer, for that member only;
2. `[Converter(typeof(…))]` on the **type** (looked up on the declared type, not inherited);
3. the options' `Converters` list, **in registration order** — the first converter whose `CanConvert` returns `true` wins;
4. the built-in converters, in the library's fixed order.

A converter *factory* found at any step is asked to `CreateConverter`, and the product's own `CanConvert` must accept the type. Because step 3 is first-match-wins, two converters that both accept a type resolve to whichever was registered first:

```csharp
var first = new YamlSerializerOptions();
first.Converters.Add(new UpperTagConverter());
first.Converters.Add(new LowerTagConverter());
YamlSerializer.Serialize(new Tagged { Tag = new("Mixed") }, first);
// Tag: MIXED

var second = new YamlSerializerOptions();
second.Converters.Add(new LowerTagConverter());
second.Converters.Add(new UpperTagConverter());
YamlSerializer.Serialize(new Tagged { Tag = new("Mixed") }, second);
// Tag: mixed
```

Register the most specific converter (or factory) ahead of any broader one whose `CanConvert` would also match. A type with no converter at any step fails with `NotSupportedException` on first use.

## Pattern 4 — Know what is cached, and why converters must be stateless

Freezing snapshots the `Converters` list, and the frozen instance then caches two things in concurrent dictionaries:

- the **resolved converter per type** — `GetConverter(typeof(Tag))` performs the four-step search once and returns the same instance for every later request;
- the **type metadata per type** — the reflected member list, wire names, constructor binding, required flags, and extension-data member that the object converter uses.

```csharp
YamlConverter a = first.GetConverter(typeof(Tag));
YamlConverter b = first.GetConverter(typeof(Tag));
bool same = ReferenceEquals(a, b);               // true — one UpperTagConverter serves every Tag
```

Two consequences follow. **Converters must be stateless**: the one instance is reused across every value, every call, and every thread — keep configuration in `readonly` fields set at construction and derive everything else from the `reader`, `value`, and `options` arguments. And **a fresh options instance per call discards both caches**: the reflection and the resolution walk are repeated, and each factory's `CreateConverter` runs again. Reuse is not merely tidier; it is where the cost goes.

## Pattern 5 — Share one instance across threads

A frozen instance is safe to share: reads of its properties are immutable, and the caches are `ConcurrentDictionary` instances. The idiomatic shape is a static holder that builds, configures, and freezes the options once:

```csharp
public static class YamlDefaults
{
    public static YamlSerializerOptions Options { get; } = Create();

    private static YamlSerializerOptions Create()
    {
        var options = new YamlSerializerOptions
        {
            PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = IgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new UpperTagConverter());
        options.MakeReadOnly();
        return options;
    }
}
```

```csharp
var shared = YamlDefaults.Options;
var results = new string[64];
Parallel.For(0, results.Length, i => results[i] = YamlSerializer.Serialize(new Tagged { Tag = new($"t{i}") }, shared));
// every result is distinct; results[7] → "tag: T7"

YamlSerializer.Serialize(new Settings { DisplayName = null }, YamlDefaults.Options);
// max_retry_count: 3
```

Passing `null` (or omitting the argument) is the same pattern applied for you: each structured serializer keeps one private default-configured instance and each line-format serializer uses its public `Default`, so the argument-free calls already share a frozen, cache-warm object. Only when you need non-default settings do you need a holder of your own.

## Pattern 6 — Start from a defaults preset

Each options type has a constructor that takes the format's `<Format>SerializerDefaults` enum. The preset is applied *before* your own settings, so anything you set afterwards wins. The presets are not the same across the family:

| Format | Enum | `General` (the parameterless constructor) | Second preset |
|---|---|---|---|
| YAML | <xref:Bodu.Text.Yaml.YamlSerializerDefaults> | no naming policy; case-sensitive names | `Web`: `PropertyNamingPolicy = NamingPolicy.CamelCase`, `PropertyNameCaseInsensitive = true` |
| TOML | <xref:Bodu.Text.Toml.TomlSerializerDefaults> | no naming policy; case-sensitive names | `Web`: camel case, case-insensitive |
| Bencode | <xref:Bodu.Text.Bencode.BencodeSerializerDefaults> | no naming policy; **case-insensitive names by default** | `Web`: camel case, case-insensitive |
| Delimited | <xref:Bodu.Text.Delimited.DelimitedSerializerDefaults> | no naming policy; case-sensitive names | `Web`: `PropertyNamingPolicy = NamingPolicy.SnakeCaseLower`, case-insensitive |
| DotEnv | <xref:Bodu.Text.DotEnv.DotEnvSerializerDefaults> | no naming policy; case-sensitive names | `Web`: `PropertyNamingPolicy = NamingPolicy.SnakeCaseUpper`, case-insensitive |
| INI | <xref:Bodu.Text.Ini.IniSerializerDefaults> | duplicate sections merge, last duplicate key wins | `Strict`: `DuplicateSectionBehavior = Disallowed`, `DuplicateKeyBehavior = Disallowed` (Python `configparser` strict mode) |

The `Web` presets pick the naming convention native to each format's usual habitat — camel case for the structured formats, `snake_case` headers for CSV, `SCREAMING_SNAKE_CASE` keys for `.env` files — and all of them turn on case-insensitive reads:

```csharp
YamlSerializer.Serialize(new Settings(), new YamlSerializerOptions(YamlSerializerDefaults.Web));
// maxRetryCount: 3
// displayName: null

DelimitedSerializer.Serialize(new[] { new Settings() }, new DelimitedSerializerOptions(DelimitedSerializerDefaults.Web));
// max_retry_count,display_name
// 3,

DotEnvSerializer.Serialize(new Settings(), new DotEnvSerializerOptions(DotEnvSerializerDefaults.Web));
// MAX_RETRY_COUNT=3

int web = YamlSerializer.Deserialize<Settings>("MAXRETRYCOUNT: 9", new YamlSerializerOptions(YamlSerializerDefaults.Web))!.MaxRetryCount;   // 9
int general = YamlSerializer.Deserialize<Settings>("MAXRETRYCOUNT: 9")!.MaxRetryCount;                                                    // 3 — key not matched
```

INI's `Strict` preset turns tolerated duplicates into hard errors, raised as the *format* exception because duplicate policy is a document-model rule:

```csharp
IniSerializer.Deserialize<Settings>("MaxRetryCount=1\nMaxRetryCount=2", new IniSerializerOptions(IniSerializerDefaults.Strict));
// → throws IniFormatException: The INI section '' defines the key 'MaxRetryCount' more than once.

int lastWins = IniSerializer.Deserialize<Settings>("MaxRetryCount=1\nMaxRetryCount=2")!.MaxRetryCount;   // 2 — General: last wins
```

## Pattern 7 — Vary one setting without a clone

There is no copy constructor. Keep a small factory that applies the shared configuration, and have each variant call it and adjust before its own freeze:

```csharp
public static class TomlProfiles
{
    public static TomlSerializerOptions Strict { get; } = Configure(o => o.UnmappedMemberHandling = UnmappedMemberHandling.Disallow);

    public static TomlSerializerOptions Lenient { get; } = Configure(o => o.UnmappedMemberHandling = UnmappedMemberHandling.Skip);

    private static TomlSerializerOptions Configure(Action<TomlSerializerOptions> adjust)
    {
        var options = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        options.Converters.Add(new PointTomlConverter());
        adjust(options);
        options.MakeReadOnly();
        return options;
    }
}
```

Each variant carries its own caches, which is the point: the converter and metadata resolved under one configuration must not be reused under another.

## Where to go next

- [Core concepts](../../docs/serialization/core/concepts.md#options-lifecycle-freezing-and-thread-safety) — the lifecycle and [converter resolution order](../../docs/serialization/core/concepts.md#converter-resolution-order) in the family vocabulary.
- [Migrating from System.Text.Json](migrating-from-system-text-json.md) — the `JsonSerializerOptions` → `<Format>SerializerOptions` property table.
- Per-format converter guides — [TOML](toml/converters.md), [YAML](yaml/converters.md), [Bencode](bencode/converters.md) — for the statelessness rules from the converter author's side.
- [Writer options and DOM options](../formats/writer-options-and-dom-options.md) — the *other* option structs: the per-writer and per-DOM knobs that sit beside the serializer options.
- [Line-format guides](../formats/index.md) — the three serializers without a converter pipeline.
- [Bodu serializer guides](index.md) and the [Text & Serialization guides](../topics/text-and-serialization.md).
