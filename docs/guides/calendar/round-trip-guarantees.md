---
title: Builder round-trip guarantees
---

# Builder round-trip guarantees

The <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder> serializes notable-date documents to XML and to a JSON subset, parses both back, and materializes a <xref:Bodu.Globalization.Calendar.NotableDateResource> through the canonical loader. This page states exactly what those conversions guarantee - and what they do not - so you can rely on the contract rather than infer it. Every guarantee below is pinned by a test in `Bodu.Globalization.Calendar.Builder/test`; the citation names the test so a change to the contract cannot land silently.

## The guarantee matrix

| Conversion | Guarantee | Pinned by |
|---|---|---|
| Builder XML → `FromXml` → `ToXml` | **Byte-identical.** Re-serializing a document parsed from builder-emitted XML reproduces that XML exactly. | `NotableDateDocumentBuilderTests.RoundTrip.RoundTrip_WhenParsedFromXmlAndReserialized_ShouldReproduceXml` |
| Builder JSON → `FromJson` → `ToJson` | **Identical.** Re-serializing a document parsed from builder-emitted JSON reproduces that JSON exactly. | `…RoundTrip.RoundTrip_WhenParsedFromJsonAndReserialized_ShouldReproduceJson` |
| XML round-trip → `Build()` | **Same resolved occurrences.** A document parsed from its own XML resolves the same dates as the original. | `…RoundTrip.RoundTrip_WhenParsedFromXml_ShouldPreserveResolution` |
| JSON round-trip → `Build()` | **Same resolved occurrences**, including resolution-policy semantics such as category precedence. | `…RoundTrip.RoundTrip_WhenParsedFromJson_ShouldPreserveResolution`, `…RoundTrip_WhenCategoryPrecedenceAuthored_ShouldPreserveCollisionResolution` |
| Bundled catalogue XML → `FromXml` → `Build()` | **Semantic equivalence.** Every bundled catalogue re-parsed through the builder produces a resource with the same identifier, rule count, and multi-territory, multi-year resolved-occurrence fingerprint as loading it directly. | `…BundledRoundTrip.FromXml_WhenBundledCatalogue_ShouldRoundTripToEquivalentResource` (all bundled catalogues) |
| `ToXml()` → canonical loader | **Loader parity.** XML the builder emits, loaded through <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader>, assembles the same resource as calling `Build()` directly - `Build()` *is* the canonical loader over `ToXml()`. | `…Serialization.ToXml_WhenLoadedByResourceLoader_ShouldMatchDirectBuild` |
| `ToXml()` vs `ToJson()` of the same document | **Format parity.** When a document fits the JSON subset, its XML and JSON serializations resolve identical occurrences. | `…Serialization.ToXmlAndToJson_WhenResolved_ShouldAgree` |
| `Save(path)` → `Load(path)` | **Disk round-trip.** Saving and re-loading a document (`.xml` or `.json`, format inferred from the extension) resolves identically. A `.bcal` save writes a binary rule pack, which `Load` rejects with `NotSupportedException`. | `…SaveLoad.SaveLoad_WhenXmlFile_ShouldRoundTripThroughDisk`, `…SaveLoad_WhenJsonFile_ShouldRoundTripThroughDisk` |
| `Clone()` | **Deep, independent copy.** Mutating a clone never affects the original. | `…Clone` partial |

## XML is the full-fidelity format

Every document the builder can express serializes to XML, and builder-canonical XML round-trips byte-for-byte. The byte-identity guarantee applies to **builder-emitted** XML: parse the output of `ToXml()` (or a file written by `Save`) and re-serialize it, and you get the same bytes back.

Hand-authored XML is normalized, not preserved: the builder parses into its object model and re-emits its canonical form, so insignificant whitespace, attribute ordering, XML comments, and alternative lexical forms from a hand-written file are replaced by the canonical rendering. What *is* guaranteed for any XML the loader accepts - hand-authored or generated - is semantic equivalence: the re-emitted document builds a resource that resolves the same occurrences, which is the guarantee the bundled-catalogue sweep pins across every shipped catalogue.

Parsing is not validation, but `FromXml`, `FromJson`, and `Load` read every value with the loader's rules: an enumeration value must name a member (ignoring case; a number is not a name), `workingDays` must be the seven-character, Sunday-first binary pattern, an integer must be a 32-bit integer, a Boolean must be `true`, `false`, `1` or `0`, and a trigger `month` must be a month name or a number from 1 to 12. `FromJson` holds a JSON document to the values the XSD requires and the ranges it allows, as `LoadJson` does: a missing or `null` required property (the root's `schemaVersion` among them, for which `FromJson` supplies no default), an empty `rules` or `days` list, or an integer outside the XSD's range throws `FormatException` with the message of the loader's `BODU-CAL-SCHEMA` diagnostic rather than reading a default (pinned by `…FromJson.FromJson_WhenSchemaRequiredPropertyIsAbsent_ShouldThrowLoadersMessage` and `…FromJson_WhenIntegerIsOutsideSchemaRange_ShouldThrowLoadersMessage`). It holds each identifier, reference and territory code to the pattern the XSD requires of it, as `LoadJson` does, so `"id": "New-Year"` throws `FormatException` with the message of the loader's `BODU-CAL-SCHEMA` diagnostic rather than reading and failing only at `Build` (pinned by `…FromJson.FromJson_WhenIdentifierBreaksSchemaPattern_ShouldThrowLoadersMessage`). It holds the attribute values of a strategy, a recurrence and an end strategy to the same rules as other values, and a strategy's month to the loader's month rules for the rule's calendar (pinned by `…FromJson.FromJson_WhenStrategyAttributeValueIsMalformed_ShouldThrowFormatException` and `…FromJson_WhenStrategyAttributeValueIsLoaderForm_ShouldBuildWhatLoadJsonLoads`). Any other value throws `FormatException` with the message of the loader's `BODU-CAL-ENUM`, `BODU-CAL-WORKING-DAYS`, `BODU-CAL-MONTH` or `BODU-CAL-VALUE` diagnostic, so a misspelled value is never read as a default, and a value the loader accepts (a JSON number in quotes, for example) reads as the loader reads it. The readers do not otherwise validate: a document the loader would reject for another reason, such as a dangling reference, can still parse, so the equivalence above holds only for input the loader accepts. Validate untrusted input with `Validate()` / `TryBuild(...)` or by loading it with <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader>. `FromJson` reads text as the loader reads it too: a string verbatim, a number by its digits and a Boolean as `true` or `false` (so `"displayName": 5` reads as the text `5`), and an item of a list of identifiers, territory codes or tags that is not a string throws `FormatException` with the message of the loader's `BODU-CAL-VALUE` diagnostic. The metadata, an adjustment policy's description and a rule's comment, which the loader does not read, accept the same scalars; an object or an array there throws `NotSupportedException`, since the builder holds those fields only as text. In any other field that holds a value, an object or an array throws `FormatException` with the message of the `BODU-CAL-VALUE` diagnostic the loader reports for it, as does a value of another kind in a property that holds an object or a list (`"applicability": 5`, `"tags": "civic"`), an object or an array in a list of names or years, or anything but an object in a list of concepts, rules, adjustment policies, imports, uses or overrides (pinned by `…FromJson.FromJson_WhenObjectListHoldsNonObjectItem_ShouldThrowFormatException`), while the JSON literal `null` reads as absent in both. Both readers name such a value or item by its JSON kind rather than quoting it, so the messages match however the document is spaced (pinned by `…FromJson.FromJson_WhenScalarValueHoldsSpacedObjectOrArray_ShouldThrowLoadersMessage` and `…FromJson_WhenListItemIsSpacedObjectOrArray_ShouldThrowLoadersMessage`). `FromJson` matches property names ignoring case, as the loader does, so `"Strategy"` and `"Month"` read as `"strategy"` and `"month"`, writes each strategy and recurrence attribute under its schema name, and ignores a property the loader does not read, such as a note in a strategy (pinned by `…FromJson.FromJson_WhenPropertyNameDiffersInCase_ShouldBuildWhatLoadJsonLoads` and `…FromJson_WhenOccurrenceSourceHoldsPropertyTheLoaderIgnores_ShouldBuildWhatLoadJsonLoads`). A rule's `strategy` and `recurrence`, and a `PatchRule` override's when it carries either, must name exactly one recognized kind between them, matched ignoring case as the loader matches it, and an `untilDate` must declare an end strategy that names exactly one, or `FromJson` throws `FormatException` with the message of the loader's `BODU-CAL-RULE-SOURCE`, `BODU-CAL-DURATION-NOEND` or `BODU-CAL-DURATION-CONFLICT` diagnostic, rather than building the rule, the patch or the span without the source or with the first kind alone. An override's `operation` is matched ignoring case, as the loader matches it; one that is absent or names no operation throws `FormatException` with the message of the loader's `BODU-CAL-ENUM` diagnostic rather than being read as a `PatchRule`, and an `AddRule` override without a `rule` throws with the message of its `BODU-CAL-SCHEMA` diagnostic rather than adding a rule with an empty identifier (pinned by `…FromJson.FromJson_WhenOverrideOperationIsUnrecognized_ShouldThrowFormatException` and `…FromJson_WhenAddRuleHasNoRule_ShouldThrowFormatException`).

## JSON is a subset - the lossiness runs one way

`ToJson()` emits the narrower JSON-subset form. The subset is closed under round-trip in both directions **once a document is inside it**:

- **JSON → builder → JSON** is identity.
- **JSON → builder → XML** succeeds for any document `FromJson` accepts, normalizing JSON lexical forms into the canonical XML rendering (a numeric month becomes its English name, except a fixed month on a rule in a calendar other than the Gregorian, which keeps its number; Boolean flags become lowercase literals; a weekday, ordinal or direction named in another case becomes the member's name; and an attribute named in another case takes its schema name - pinned by `…JsonSubset.FromJson_WhenStrategyAttributeVaries_ShouldNormalizeInXml`; a `null` attribute is left out, as the loader reads it as absent - pinned by `…JsonSubset.FromJson_WhenStrategyAttributeIsNull_ShouldOmitItFromXml`).
- **XML → builder → JSON** is the lossy direction. A document using a feature outside the subset throws <xref:System.NotSupportedException> from `ToJson()` / `ToJsonObject()` / `Save(*.json)` naming the offending feature - it never silently drops it.

Features outside the JSON subset (each rejection pinned by `…JsonSubset.ToJson_WhenDocumentUsesUnsupportedJsonFeature_ShouldThrowNotSupportedException` and the `…Serialization` import/calendar cases):

| Feature outside the subset | Where it appears |
|---|---|
| Imports (`AddImport`) | Document level |
| Non-Gregorian rule calendars (`CalendarSystem` other than Gregorian) | Rule strategies |
| XML-only adjustment triggers and actions (for example `IfBeforeFixedDate`, `ReplaceWithRule`) | Adjustment policies |
| Trigger refinements: month, day, week ordinal, custom trigger handler keys | Adjustment policies |
| Action refinements: replacement rule references, custom action handler keys, handler parameters | Adjustment policies |
| Scope year bounds (`FromYear` / `ToYear` / `OnlyYear` / `ExceptYear`) and notable-date / rule scope references | Adjustment scopes |
| Override patches carrying applicability, strategy, recurrence, duration-end, tag, or adjustment changes | Overrides |

Serialize such documents as XML. Scopes confined to territories, calendars, and categories - and lunisolar strategy flags - are inside the subset and round-trip through JSON unchanged.

The subset limits only what `ToJson()` writes. `FromJson` reads every field <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader> reads from JSON, the features above included, so `FromJson(json).Build()` produces the same resource as `NotableDateResourceLoader.LoadJson(json)` for any document both accept (pinned by `…FromJson.FromJson_WhenDocumentDeclaresField_ShouldBuildWhatLoadJsonLoads`, which compares the two as binary rule packs). A few JSON fields have no counterpart in the builder's document model, whose full form is the XML schema, and `FromJson` throws <xref:System.NotSupportedException> for them rather than dropping them:

| JSON field the builder cannot hold | Why |
|---|---|
| An empty `tags` or `adjustments` list on a `PatchRule` override | The loader reads an empty list as "clear the rule's list"; the schema has no empty `<Tags>` or `<Adjustments>` element. |
| An empty `adjustments` list on an import `use` | The same, for the imported concept's rules. |
| A scope `ruleRefs` item that is a bare rule identifier, which names no `notableDateRef` | The schema's scope `<Rule>` element requires both references. An object without either throws `FormatException` with the loader's `BODU-CAL-SCHEMA` message. |
| A handler parameter whose value is an object or an array | The loader keeps it as its original JSON text, which a parsed document no longer carries. |

Load such documents with `NotableDateResourceLoader.LoadJson` instead.

## Build parity: what you serialize is what the runtime loads

`Build()` does not maintain a private assembly path: it serializes the document to XML and loads it through the canonical <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader>, so validation and assembly behave identically whether a document is built in-process or shipped as a file and loaded by the runtime. The same applies to the JSON form: a JSON document loaded through the loader's JSON entry point assembles a resource equivalent to `Build()` (`…Serialization.ToJson_WhenLoadedByResourceLoader_ShouldAssembleEquivalentResource`).

## What is not guaranteed

- **Byte identity for hand-authored input.** Formatting, comments, and lexical variants are normalized to the canonical form (see above).
- **JSON representability of every document.** XML → JSON throws for features outside the subset; it never degrades the document.
- **Stability of the canonical rendering across library versions.** The canonical XML/JSON layout may evolve between versions; the semantic (resolved-occurrence) guarantees are the durable contract. Do not diff canonical output across different library versions as a change-detection mechanism.

## Where to go next

- [Authoring with the notable-date builder](notable-date-builder.md) - the authoring API these guarantees apply to.
- [Authoring notable date rules](rule-authoring.md) - the XML / JSON document model.
- [`Bodu.Globalization.Calendar.Builder` API reference](xref:Bodu.Globalization.Calendar.Builder) - the full type list.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** - every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
