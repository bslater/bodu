---
title: Bodu.Text.Serialization — Core concepts
---

# Core concepts

This page is the vocabulary of **Bodu.Text.Serialization** — the rules that hold identically in every serializer that consumes the package. Where a rule applies only to the structured serializers ([Bencode](../bencode/index.md), [TOML](../toml/index.md), [YAML](../yaml/index.md)), which compile the shared metadata resolver and converter engine, the page says so; the [line formats](../../formats/index.md) honor the subset listed in the [introduction](index.md#who-uses-what).

Part of the **[Text & Serialization](../../topics/text-and-serialization.md)** topic.

## Attribute versus options precedence

Every setting that can be expressed both on the model and on the serializer options resolves **closest-wins**: a member-level attribute beats a type-level attribute, which beats the options.

| Setting | Member attribute | Type attribute | Options property |
|---|---|---|---|
| Wire name | `[PropertyName]` | `[NamingPolicy]` | `PropertyNamingPolicy` |
| Ignore condition | `[Ignore(Condition = …)]` | — | `DefaultIgnoreCondition` |
| Converter | `[Converter]` | `[Converter]` | `Converters`, then the built-ins |
| Object creation | `[ObjectCreationHandling]` | `[ObjectCreationHandling]` | `PreferredObjectCreationHandling` |
| Unmapped keys | — | `[UnmappedMemberHandling]` | `UnmappedMemberHandling` |
| Field participation | `[Include]` | — | `IncludeFields` |

The options object is therefore the *default* for everything the model has not pinned, which is why one options instance can serve many model types without per-type configuration.

## Naming-policy application order

The wire key of a member is resolved once, when the type's metadata is built, in this order:

1. `[PropertyName("…")]` on the member — always wins, and is never passed through a policy.
2. `[NamingPolicy(KnownNamingPolicy.…)]` on the declaring type — converts the CLR name with that policy.
3. `PropertyNamingPolicy` on the options — converts the CLR name with the configured policy.
4. Otherwise the CLR member name is used verbatim.

Two members that resolve to the **same** wire key (ordinal comparison) are a model error: metadata resolution throws rather than emitting a document that cannot be canonical. Keys that differ only by case remain distinct on the wire; when `PropertyNameCaseInsensitive` is on, reads match either.

The policy converts *member* names. It does not rewrite `[PropertyName]` values, dictionary keys, or `[ExtensionData]` keys, and an enumeration member serialized by name takes `[StringEnumMemberName]` over anything else.

## Ignore conditions

<xref:Bodu.Text.Serialization.IgnoreCondition> has two different kinds of member:

- **`Always`** removes the member at metadata-resolution time. It is neither written nor read — an input key with that name is treated as unmapped.
- **`WhenWritingNull`** and **`WhenWritingDefault`** are evaluated at *write* time only. The effective condition for a member is `[Ignore(Condition = …)]` when present, otherwise the options-level `DefaultIgnoreCondition`; a member-level `Never` therefore opts a member *out* of an options-level default. Reading is never affected by these two values.

Formats with no null literal behave as though `WhenWritingNull` were always on — TOML omits a `null` member regardless of the condition — while YAML writes `null` unless a condition suppresses it.

## Required members and constructor binding

A member is **required** when it carries `[Required]`, when it is declared with the C# `required` keyword, or when it binds to a constructor parameter that has no default value. On deserialization the structured serializers check every required member for presence *before* the instance is constructed and throw the format's serialization exception naming the missing wire key. (INI and DotEnv honor `[Required]` through their own binders; Delimited does not.)

Constructor selection follows a fixed ladder:

1. the constructor marked `[Constructor]`;
2. otherwise a public parameterless constructor (a value type always takes this branch);
3. otherwise the single declared constructor, or the one with the most parameters.

Parameters bind to members by **name, case-insensitively** — a `host` parameter binds the `Host` property. A bound member is passed to the constructor and not assigned again afterwards; an unbound parameter receives its declared default (or `default(T)`). Members with no constructor parameter are assigned through their setters after construction.

## Include, fields, and non-public accessors

By default a property participates when it has a public getter, and is assigned only through a public setter. `[Include]` binds through the declared accessors regardless of visibility, so `{ get; private set; }` and `{ get; init; }` round-trip. Public fields participate only when `IncludeFields` is on or the field carries `[Include]`; non-public fields are never surfaced.

## Extension data

A type may declare at most one `[ExtensionData]` member — two is a metadata error. The member's type must be dictionary-shaped and keyed by `string`: for Bencode and TOML the format's object node (`BencodeObject` / `TomlObject`) or an `IDictionary<string, …Node?>` / `Dictionary<string, …Node?>` of the format's node type; for YAML any `IDictionary<string, object?>`. On read, every key that maps to no member lands in the dictionary — extension data takes precedence over `UnmappedMemberHandling`, so a captured key never triggers `Disallow`. On write, the captured entries are emitted after the type's declared members; an entry whose key collides with a declared member's wire key is a serialization error.

## Unmapped members

<xref:Bodu.Text.Serialization.UnmappedMemberHandling> decides what happens to an input key with no member and no extension-data member: `Skip` (the default) discards it, `Disallow` throws the format's serialization exception naming the key and type. `[UnmappedMemberHandling]` on the type overrides the options.

## Object creation handling

<xref:Bodu.Text.Serialization.ObjectCreationHandling> applies to collection and dictionary members. `Replace` (the default) builds a new instance and assigns it; `Populate` adds the entries read from the input into the instance the member already holds, which is how a get-only `List<T>` initialized in the type round-trips. When the existing value is `null` or the member is not a populatable collection the serializer falls back to `Replace`. A member-level attribute beats a type-level one, and both beat `PreferredObjectCreationHandling`.

## Property order

Members are presented to the writer in ascending `[PropertyOrder]` value, ties broken by declaration order; unannotated members are `0`. This governs the order in which the object mapper *visits* members. A format that canonicalizes its output — Bencode sorts dictionary keys — applies its own on-wire order afterwards, so use `[PropertyOrder]` to control presentation in document-order formats such as TOML and YAML.

## Callback order

The four interfaces are invoked by the object-mapping converter — the catch-all that writes a class or struct as a keyed table. Within one instance the order is always:

| Phase | Deserialization | Serialization |
|---|---|---|
| 1 | Member values are read, each through its member converter. | A non-null value is selected for writing. |
| 2 | Required members are checked; the instance is constructed. | **`OnSerializing`** fires. |
| 3 | **`OnDeserializing`** fires — after construction, before any settable member is assigned. | The table is opened. |
| 4 | Settable members are assigned; extension data is populated. | Members are written in order; extension data follows. |
| 5 | **`OnDeserialized`** fires on the fully materialized instance. | The table is closed; **`OnSerialized`** fires. |

Consequences: `OnDeserializing` cannot influence how values are parsed (phase 1 has already run), only what the instance looks like before assignment; with a parameterized constructor it already observes the constructor-bound members. `OnSerializing` runs before the table opens, so any mutation it makes is reflected in the output. Nested objects fire their own callbacks, innermost completing first. A type claimed by a *custom* converter bypasses the object mapper entirely, so none of its callbacks fire — put that logic inside the converter. The line formats invoke the same four hooks around their own binders.

## Options lifecycle: freezing and thread safety

A `…SerializerOptions` instance is mutable until it is first used. The first `Serialize` / `Deserialize` call (or an explicit `MakeReadOnly()`) freezes it: `IsReadOnly` becomes `true`, every property setter and every mutating operation on `Converters` throws `InvalidOperationException`, and the snapshot of registered converters is captured. A frozen instance caches its resolved converters and per-type metadata in concurrent dictionaries, so it is **safe to share across threads** and cheaper to reuse than to rebuild — configure one instance per configuration, freeze it (or let first use do so), and pass it everywhere. The static `Default` instance on the line-format options types is created already frozen; an instance you construct yourself freezes on first use.

> [!NOTE]
> Freezing is by *instance*. Cloning is not provided; to vary one setting, construct a second options object.

## Converter resolution order

Converters are a structured-serializer concept (the line formats convert scalars with the invariant culture and expose no converter pipeline). For a given type the engine resolves a converter by checking, in order:

1. `[Converter(typeof(…))]` on the **member** — instantiated for that member only;
2. `[Converter(typeof(…))]` on the **type**;
3. the options' `Converters` list, **in registration order** — the first converter whose `CanConvert` returns `true` wins, so register the more specific converter first;
4. the built-in converters.

A converter type named by the attribute must derive from the format's converter base and declare a public parameterless constructor. A converter *factory* found at any step is asked to create the concrete converter, which is then verified to accept the type. The result is cached on the options per type, which is one more reason the options must be frozen before use.

## Where to go next

- **[Bodu.Text.Serialization introduction](index.md)** — the attribute family, naming policies, enums, callbacks, and which serializers use which parts.
- **[Getting started](getting-started.md)** — install and the first annotated DTO.
- **[TOML attribute patterns](../../../guides/serialization/toml/attributes.md)** and **[TOML callbacks](../../../guides/serialization/toml/callbacks.md)** — twelve worked attribute recipes and the callback pipeline in detail (the same recipes apply to Bencode and YAML by swapping the prefix).
- **[Bodu serializers introduction](../index.md)** — the shared architecture of the three structured serializers.
- **API reference** — <xref:Bodu.Text.Serialization>.
