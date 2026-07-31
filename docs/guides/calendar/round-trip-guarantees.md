---
title: Builder round-trip guarantees
---

# Builder round-trip guarantees

The <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder> serializes notable-date documents to XML and to a JSON subset, parses both back, and materializes a <xref:Bodu.Globalization.Calendar.NotableDateResource> through the canonical loader. This page states exactly what those conversions guarantee — and what they do not — so you can rely on the contract rather than infer it. Every guarantee below is pinned by a test in `Bodu.Globalization.Calendar.Builder/test`; the citation names the test so a change to the contract cannot land silently.

## The guarantee matrix

| Conversion | Guarantee | Pinned by |
|---|---|---|
| Builder XML → `FromXml` → `ToXml` | **Byte-identical.** Re-serializing a document parsed from builder-emitted XML reproduces that XML exactly. | `NotableDateDocumentBuilderTests.RoundTrip.RoundTrip_WhenParsedFromXmlAndReserialized_ShouldReproduceXml` |
| Builder JSON → `FromJson` → `ToJson` | **Identical.** Re-serializing a document parsed from builder-emitted JSON reproduces that JSON exactly. | `…RoundTrip.RoundTrip_WhenParsedFromJsonAndReserialized_ShouldReproduceJson` |
| XML round-trip → `Build()` | **Same resolved occurrences.** A document parsed from its own XML resolves the same dates as the original. | `…RoundTrip.RoundTrip_WhenParsedFromXml_ShouldPreserveResolution` |
| JSON round-trip → `Build()` | **Same resolved occurrences**, including resolution-policy semantics such as category precedence. | `…RoundTrip.RoundTrip_WhenParsedFromJson_ShouldPreserveResolution`, `…RoundTrip_WhenCategoryPrecedenceAuthored_ShouldPreserveCollisionResolution` |
| Bundled catalogue XML → `FromXml` → `Build()` | **Semantic equivalence.** Every bundled catalogue re-parsed through the builder produces a resource with the same identifier, rule count, and multi-territory, multi-year resolved-occurrence fingerprint as loading it directly. | `…BundledRoundTrip.FromXml_WhenBundledCatalogue_ShouldRoundTripToEquivalentResource` (all bundled catalogues) |
| `ToXml()` → canonical loader | **Loader parity.** XML the builder emits, loaded through <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader>, assembles the same resource as calling `Build()` directly — `Build()` *is* the canonical loader over `ToXml()`. | `…Serialization.ToXml_WhenLoadedByResourceLoader_ShouldMatchDirectBuild` |
| `ToXml()` vs `ToJson()` of the same document | **Format parity.** When a document fits the JSON subset, its XML and JSON serializations resolve identical occurrences. | `…Serialization.ToXmlAndToJson_WhenResolved_ShouldAgree` |
| `Save(path)` → `Load(path)` | **Disk round-trip.** Saving and re-loading a document (`.xml` or `.json`, format inferred from the extension) resolves identically. | `…SaveLoad.SaveLoad_WhenXmlFile_ShouldRoundTripThroughDisk`, `…SaveLoad_WhenJsonFile_ShouldRoundTripThroughDisk` |
| `Clone()` | **Deep, independent copy.** Mutating a clone never affects the original. | `…Clone` partial |

## XML is the full-fidelity format

Every document the builder can express serializes to XML, and builder-canonical XML round-trips byte-for-byte. The byte-identity guarantee applies to **builder-emitted** XML: parse the output of `ToXml()` (or a file written by `Save`) and re-serialize it, and you get the same bytes back.

Hand-authored XML is normalized, not preserved: the builder parses into its object model and re-emits its canonical form, so insignificant whitespace, attribute ordering, XML comments, and alternative lexical forms from a hand-written file are replaced by the canonical rendering. What *is* guaranteed for any accepted XML — hand-authored or generated — is semantic equivalence: the re-emitted document builds a resource that resolves the same occurrences, which is the guarantee the bundled-catalogue sweep pins across every shipped catalogue.

## JSON is a subset — the lossiness runs one way

`ToJson()` emits the narrower JSON-subset form. The subset is closed under round-trip in both directions **once a document is inside it**:

- **JSON → builder → JSON** is identity.
- **JSON → builder → XML** always succeeds, normalizing JSON lexical forms into the canonical XML rendering (a numeric month becomes its English name, Boolean flags become lowercase literals, `null` attributes become empty attributes — pinned by `…JsonSubset.FromJson_WhenStrategyAttributeVaries_ShouldNormalizeInXml`).
- **XML → builder → JSON** is the lossy direction. A document using a feature outside the subset throws <xref:System.NotSupportedException> from `ToJson()` / `ToJsonObject()` / `Save(*.json)` naming the offending feature — it never silently drops it.

Features outside the JSON subset (each rejection pinned by `…JsonSubset.ToJson_WhenDocumentUsesUnsupportedJsonFeature_ShouldThrowNotSupportedException` and the `…Serialization` import/calendar cases):

| Feature outside the subset | Where it appears |
|---|---|
| Imports (`AddImport`) | Document level |
| Non-Gregorian rule calendars (`CalendarSystem` other than Gregorian) | Rule strategies |
| XML-only adjustment triggers and actions (for example `IfBeforeFixedDate`, `ReplaceWithRule`) | Adjustment policies |
| Trigger refinements: month, day, week ordinal, custom trigger handler keys | Adjustment policies |
| Action refinements: replacement rule references, custom action handler keys, handler parameters | Adjustment policies |
| Scope year bounds (`FromYear` / `ToYear` / `OnlyYears` / `ExceptYears`) and notable-date / rule scope references | Adjustment scopes |
| Override patches carrying applicability, strategy, tag, or adjustment changes | Overrides |

Serialize such documents as XML. Scopes confined to territories, calendars, and categories — and lunisolar strategy flags — are inside the subset and round-trip through JSON unchanged.

## Build parity: what you serialize is what the runtime loads

`Build()` does not maintain a private assembly path: it serializes the document to XML and loads it through the canonical <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader>, so validation and assembly behave identically whether a document is built in-process or shipped as a file and loaded by the runtime. The same applies to the JSON form: a JSON document loaded through the loader's JSON entry point assembles a resource equivalent to `Build()` (`…Serialization.ToJson_WhenLoadedByResourceLoader_ShouldAssembleEquivalentResource`).

## What is not guaranteed

- **Byte identity for hand-authored input.** Formatting, comments, and lexical variants are normalized to the canonical form (see above).
- **JSON representability of every document.** XML → JSON throws for features outside the subset; it never degrades the document.
- **Stability of the canonical rendering across library versions.** The canonical XML/JSON layout may evolve between versions; the semantic (resolved-occurrence) guarantees are the durable contract. Do not diff canonical output across different library versions as a change-detection mechanism.

## Where to go next

- [Authoring with the notable-date builder](notable-date-builder.md) — the authoring API these guarantees apply to.
- [Authoring notable date rules](rule-authoring.md) — the XML / JSON document model.
- [`Bodu.Globalization.Calendar.Builder` API reference](xref:Bodu.Globalization.Calendar.Builder) — the full type list.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
