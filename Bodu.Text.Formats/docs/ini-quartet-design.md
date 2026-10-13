# INI → quartet design note

**Date:** 2026-07-21
**Status:** Implemented. The design implements tranche **T3**, which was to land
atomically with T4; the package shipped in 1.0.0, first published in 0.5.0.
Where the shipped code differs from this design, an **As built** paragraph after
the claim records what the code does, as of master `849e280436`.
**Relates to:** [`line-formats-quartet-redesign-assessment.md`](./line-formats-quartet-redesign-assessment.md).

INI is the hardest of the three: a two-level object-of-objects, duplicate-section
**merge** that declares structure out of source order (forcing a TOML-style
pre-parse reader), and comment trivia that both authoring and the decoupled
`Bodu.Text.Configuration` require (forcing a trivia-bearing mutable DOM). Done
last, with the template proven by DotEnv and Delimited.

## Value model

A **2-level object-of-objects**: the root object's keys are section names (plus
a global/unnamed section) and its values are sub-objects of `string → string`.
No deeper nesting. Structurally analogous to TOML tables but capped at depth 2
and carrying comment trivia TOML discards.

## Token model - two readers (like TOML)

- **Source-order** `Utf8IniReader` (public forward-only surface for
  streaming/tooling): `None, SectionHeader, PropertyName, String, Comment`.
- **Normalized** `IniDocumentReader` (= `FormatReader`, threaded by `ref` into
  converters; also backs the read-only `IniDocument` DOM):
  `None, StartObject, EndObject, PropertyName, String`.

  **As built:** there are no converters and no `FormatReader`; the package
  compiles none of the shared converter engine. `IniDocumentReader` is a
  public `ref struct` cursor over the internal `IniNormalizedDocument`, which
  its constructor builds from the whole input, and `IniDocument` wraps that
  store directly rather than going through the reader
  (`Bodu.Text.Ini/src/Text.Ini.Reader/IniDocumentReader.cs`,
  `IniNormalizedDocument.cs`,
  `Bodu.Text.Ini/src/Text.Ini.Document/IniDocument.cs`).

**Why INI needs the pre-parse reader (unlike DotEnv/Delimited).**
`IniDuplicateSectionBehavior.Preserve`/merge means a later `[foo]` appends to an
earlier `[foo]` - structure out of source order, exactly TOML's condition. Under
non-merging policies (last-wins / error) one forward pass suffices, but under
merge a materialized flat-row pre-parse is required, so the serializer **always**
routes through `IniDocumentReader` (a flat pooled row store, TOML-identical
shape). Normalized stream for `key0=a⏎[db]⏎host=x⏎[db]⏎port=5` under merge:

```
StartObject
  PropertyName "key0"  String "a"      (global-section keys hoisted to root)
  PropertyName "db"
  StartObject
    PropertyName "host"  String "x"
    PropertyName "port"  String "5"    (merged from the second [db])
  EndObject
EndObject
```

**As built:** `IniDuplicateSectionBehavior` has two members, `Merge` (the
default) and `Disallowed`, which throws `IniFormatException`
(`Bodu.Text.Ini/src/Text.Ini/IniDuplicateSectionBehavior.cs`); there is no
`Preserve` and no last-wins section policy. The serializer does not route
through `IniDocumentReader`: `IniSerializer`, including its section-factory
overloads, calls the internal `IniNormalizedDocument.Parse` directly, as
`IniDocument` and `IniDocumentReader` do, under every policy. That store is
plain `List`s, not pooled (`Bodu.Text.Ini/src/Text.Ini/IniSerializer.cs`,
`IniSerializer.Factory.cs`,
`Bodu.Text.Ini/src/Text.Ini.Reader/IniNormalizedDocument.cs`).

## Global/unnamed section mapping (resolved)

Global keys **hoist to the root object** (shown above) for POCO ergonomics.
`IniSerializerOptions.GlobalSectionName` (default `null` = hoist) opts into a
reserved root key that preserves round-trip fidelity. A global key colliding
with a section name is **rejected** with `IniSerializationException`; the
reserved-key mode disambiguates.

**As built:** a global key that collides with a section name throws
`IniFormatException` while the document is normalized, before
`GlobalSectionName` is consulted, so the reserved-key mode does not avoid it;
`IniDocument`, `IniDocumentReader` and `IniNode.Parse` reject it the same way
(`Bodu.Text.Ini/src/Text.Ini.Reader/IniNormalizedDocument.cs`). Hoisting needs a
root with scalar members: a `Dictionary<string, Dictionary<string, string>>`
root cannot hold global keys, so deserializing a document that has them into
one throws `IniSerializationException` unless `GlobalSectionName` is set
(`ReadRootDictionary` in `Bodu.Text.Ini/src/Text.Ini/IniSerializer.Binder.cs`).

## Mutable DOM (`Text.Ini.Nodes`) - trivia-bearing (D5 deviation)

`IniNode` / `IniObject` / `IniValue`. `IniObject` sub-objects and `IniValue`s
carry `LeadingComments` and (on values) `InlineComment` trivia. This DOM is the
successor to today's `IniDocument` / `IniSection` / `IniEntry` / `IniComment`
and is the **one place** the "quartet DOMs are trivia-free" rule is deliberately
broken - INI authoring and (via decouple, D6) `Bodu.Text.Configuration` both
require faithful comment round-trips. Called out explicitly per the assessment.

**As built:** there is no `InlineComment`. `LeadingComments` is on every node
(`IniNode`) and `TrailingComments` on `IniObject`, and the reader does not
recognize inline comments at all, so a `;` or `#` after a value stays part of
the value (`Bodu.Text.Ini/src/Text.Ini.Nodes/IniNode.cs`, `IniObject.cs`,
`Bodu.Text.Ini/src/Text.Ini.Reader/IniReaderOptions.cs`).
`Bodu.Text.Configuration` does not use this DOM: after the decouple it keeps its
own trivia-preserving model (see Downstream).

## Read-only DOM (`Text.Ini.Document`) - trivia-free, `IDisposable`

`IniDocument` / `IniElement` / `IniProperty`, root object-of-objects,
`JsonElement`-shaped accessors, comments dropped.

## Serializer (`IniSerializer`)

Standard facade (buffered-in-full). Binding target: a POCO whose properties are
section POCOs (or `Dictionary<string,string>` values), or
`Dictionary<string, Dictionary<string,string>>`. **Root must be an object**; a
value nested deeper than two levels is rejected with `IniSerializationException`
(the "INI has no deeper nesting" gate). `IniSerializerOptions` (partial,
read-only on first use, `IniSerializerDefaults`) reuses the shared attribute /
`NamingPolicy` / callback layer; scalar converters are format-local (D4).

**As built:** `IniSerializerOptions` is a `sealed` class, not `partial`, and
there are no format-local converters: scalars convert through the shared
`InvariantScalarCodec` under its `Configuration` policy
(`Bodu.Text.Serialization/flat/src/InvariantScalarCodec.cs`, compiled into
the package and called by `ValueToString` and `ConvertFromString` in
`Bodu.Text.Ini/src/Text.Ini/IniSerializer.Binder.cs`). A root or section
dictionary may be any `IDictionary<string, TValue>`; one that does not also
implement the non-generic `IDictionary` is read and written through the generic
interface (`FlatBinding.SetEntry` and `FlatBinding.TryGetDictionary`, #1242).

## Dialect migration

Today's `IniParseOptions` knobs move onto the reader/serializer options:
`AllowGlobalSection`, `CaseSensitiveSections`, `CaseSensitiveKeys`,
`PreserveComments`, `DuplicateKeyBehavior` (`Bodu.Text.DuplicateKeyPolicy`),
`DuplicateSectionBehavior` (`IniDuplicateSectionBehavior` -
`Merge`/`Preserve`/`MergeAdjacent`). Both enums relocate into the INI root
bucket. The comment-prefix default (`;` for INI vs `#` for DotEnv) is preserved.

**As built:** `AllowGlobalSection`, `CaseSensitiveSections` and
`CaseSensitiveKeys` do not exist. The readers always accept global keys, and
section names and keys always compare ordinally, so case-sensitively, in the
document models; `PropertyNameCaseInsensitive` affects only how the serializer
matches member names. The comment settings are `DisallowHashComments` and the
inverted `SkipComments` on `IniReaderOptions`, with `PreserveComments` internal
(`Bodu.Text.Ini/src/Text.Ini.Reader/IniReaderOptions.cs`), and `IniSerializer`
always reads with `IniReaderOptions.Default`. `DuplicateKeyBehavior` is typed
`IniDuplicateKeyBehavior` (`LastWins`, `FirstWins`, `Disallowed`) and
`DuplicateSectionBehavior` is `IniDuplicateSectionBehavior` (`Merge`,
`Disallowed`), both on `IniDocumentOptions` and `IniSerializerOptions`.
`Preserve` and `MergeAdjacent` exist only in `Bodu.Text.Configuration`'s own
`IniDuplicateSectionBehavior`
(`Bodu.Text.Configuration/src/Text.Configuration/IniDuplicateSectionBehavior.cs`).

## Tests (colocated in `Bodu.Text.Ini.Test`)

`Utf8IniReaderTests` (source-order token KATs + malformed rejection),
`IniDocumentReaderTests` (normalized stream, merge semantics, global-section
hoist/reserved-key), `Utf8IniWriterTests` (exact canonical bytes incl. comment
round-trip), `IniSerializerTests.*` backbone + subject partials (`.NamingPolicy`,
`.Nulls`, sections-as-nested-objects, depth-2 rejection, duplicate-section
policies), and `IniDocument`/`IniNode` DOM tests (incl. trivia round-trip).
Migrate `IniKnownAnswerVector`. Curated malformed + full-grammar sweep in the
Regression tier. One `Smoke` test.

**As built:** in `Bodu.Text.Ini/test/`, `IniDocumentReaderTests` covers the
normalized stream, merging, the global-key hoist and the policy failures, but no
reserved key, which `IniDocumentReader` does not have; the `GlobalSectionName`
tests are in `IniSerializerTests`. The serializer partials are `Serialize`,
`SerializeAsync`, `Deserialize`, `DeserializeAsync`, `SerializeSection`,
`DeserializeSection`, `RoundTrip` and `Fields`, with no `.NamingPolicy` or
`.Nulls` partial: naming policies are tested in the `Serialize` and `Fields`
partials and in the separate `NamingPolicyTests` class, and the depth-2 and
duplicate-section cases sit in the `Serialize`, `Deserialize` and
`DeserializeSection` partials. The mutable DOM tests, the trivia round trip
included, are `IniObjectTests`; there is no `IniNodeTests`. No
`IniKnownAnswerVector` type exists. The Regression tier holds the release-note
fix catalogue (`IniReleaseNoteCorpusTests`) and the guide-snippet compile check.

## Downstream - the atomic T4 dependency

`Bodu.Text.Configuration.ConfigurationDocument : IniDocumentBase` and
`ConfigurationReader.Helpers.cs` build `IniSection`/`IniEntry`/`IniComment`
directly. Per assessment **D6**, T4 **decouples** Configuration - moving the
trivia-preserving INI model into Configuration as its own internal document model
and dropping the Formats dependency - so the new `Bodu.Text.Ini` is free to be a
clean quartet. T3 and T4 land together or `bodu.slnx` will not build.

**As built:** the decouple is done. `Bodu.Text.Configuration` references only
`Bodu.Core` (`Bodu.Text.Configuration/src/Bodu.Text.Configuration.csproj`) and
owns the trivia-preserving model in its own `Bodu.Text.Configuration` namespace,
as public rather than internal types: `IniDocumentBase`, `IniSection`,
`IniEntry`, `IniComment`, `DuplicateKeyPolicy` and its own
`IniDuplicateSectionBehavior`
(`Bodu.Text.Configuration/src/Text.Configuration/`).
