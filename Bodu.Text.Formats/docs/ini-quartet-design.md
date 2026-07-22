# INI → quartet design note

**Date:** 2026-07-21
**Status:** Design — implements tranche **T3** (lands atomically with T4).
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

## Token model — two readers (like TOML)

- **Source-order** `Utf8IniReader` (public forward-only surface for
  streaming/tooling): `None, SectionHeader, PropertyName, String, Comment`.
- **Normalized** `IniDocumentReader` (= `FormatReader`, threaded by `ref` into
  converters; also backs the read-only `IniDocument` DOM):
  `None, StartObject, EndObject, PropertyName, String`.

**Why INI needs the pre-parse reader (unlike DotEnv/Delimited).**
`IniDuplicateSectionBehavior.Preserve`/merge means a later `[foo]` appends to an
earlier `[foo]` — structure out of source order, exactly TOML's condition. Under
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

## Global/unnamed section mapping (resolved)

Global keys **hoist to the root object** (shown above) for POCO ergonomics.
`IniSerializerOptions.GlobalSectionName` (default `null` = hoist) opts into a
reserved root key that preserves round-trip fidelity. A global key colliding
with a section name is **rejected** with `IniSerializationException`; the
reserved-key mode disambiguates.

## Mutable DOM (`Text.Ini.Nodes`) — trivia-bearing (D5 deviation)

`IniNode` / `IniObject` / `IniValue`. `IniObject` sub-objects and `IniValue`s
carry `LeadingComments` and (on values) `InlineComment` trivia. This DOM is the
successor to today's `IniDocument` / `IniSection` / `IniEntry` / `IniComment`
and is the **one place** the "quartet DOMs are trivia-free" rule is deliberately
broken — INI authoring and (via decouple, D6) `Bodu.Text.Configuration` both
require faithful comment round-trips. Called out explicitly per the assessment.

## Read-only DOM (`Text.Ini.Document`) — trivia-free, `IDisposable`

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

## Dialect migration

Today's `IniParseOptions` knobs move onto the reader/serializer options:
`AllowGlobalSection`, `CaseSensitiveSections`, `CaseSensitiveKeys`,
`PreserveComments`, `DuplicateKeyBehavior` (`Bodu.Text.DuplicateKeyPolicy`),
`DuplicateSectionBehavior` (`IniDuplicateSectionBehavior` —
`Merge`/`Preserve`/`MergeAdjacent`). Both enums relocate into the INI root
bucket. The comment-prefix default (`;` for INI vs `#` for DotEnv) is preserved.

## Tests (colocated in `Bodu.Text.Ini.Test`)

`Utf8IniReaderTests` (source-order token KATs + malformed rejection),
`IniDocumentReaderTests` (normalized stream, merge semantics, global-section
hoist/reserved-key), `Utf8IniWriterTests` (exact canonical bytes incl. comment
round-trip), `IniSerializerTests.*` backbone + subject partials (`.NamingPolicy`,
`.Nulls`, sections-as-nested-objects, depth-2 rejection, duplicate-section
policies), and `IniDocument`/`IniNode` DOM tests (incl. trivia round-trip).
Migrate `IniKnownAnswerVector`. Curated malformed + full-grammar sweep in the
Regression tier. One `Smoke` test.

## Downstream — the atomic T4 dependency

`Bodu.Text.Configuration.ConfigurationDocument : IniDocumentBase` and
`ConfigurationReader.Helpers.cs` build `IniSection`/`IniEntry`/`IniComment`
directly. Per assessment **D6**, T4 **decouples** Configuration — moving the
trivia-preserving INI model into Configuration as its own internal document model
and dropping the Formats dependency — so the new `Bodu.Text.Ini` is free to be a
clean quartet. T3 and T4 land together or `bodu.slnx` will not build.
