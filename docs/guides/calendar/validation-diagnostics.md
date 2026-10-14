---
title: Calendar validation diagnostics
---

# Calendar validation diagnostics

Every notable-date document - hand-authored XML/JSON, a bundled catalogue, or a document produced by the <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder> - passes through one validation pipeline in <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader>. Each problem the pipeline finds is a <xref:Bodu.Globalization.Calendar.NotableDateValidationDiagnostic> carrying a severity, a stable `BODU-CAL-*` code, and a message naming the offending concept, rule, or policy. This page is the code reference and shows the two ways to consume diagnostics: throwing loads and collect-mode linting.

## Throw or collect

**Throwing (the default).** `NotableDateResourceLoader.Load(...)` / `LoadJson(...)` and `NotableDateDocumentBuilder.Build()` validate the whole document, then throw a single <xref:Bodu.Globalization.Calendar.NotableDateValidationException> when any error-severity diagnostic was produced. The exception's `Diagnostics` property carries the complete list - errors, warnings, and informational messages - not just the first failure.

**Collecting (the lint surface).** The `Try` overloads return the same complete diagnostic list without throwing, so build tooling and editors can lint arbitrary input:

- `NotableDateResourceLoader.TryLoad(xml, resolver, out resource, out diagnostics)` and `TryLoadJson(json, resolver, out resource, out diagnostics)`, each with an overload taking a custom <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithmRegistry> - malformed input becomes a `BODU-CAL-SYNTAX` error diagnostic instead of a `FormatException`.
- `NotableDateDocumentBuilder.Validate()` - lints the authored document and returns the diagnostics; a clean result guarantees `Build()` succeeds for the same document, because both run the same pipeline.
- `NotableDateDocumentBuilder.TryBuild(out resource, out diagnostics)` - the non-throwing counterpart of `Build()`.

<!-- compile -->
```csharp
NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("corp.holidays")
    .AddNotableDate("new-years-day", "New Year's Day", NotableDateCategory.PublicHoliday, d => d
        .AddRule("default", r => r.Fixed(1, 1)));

foreach (NotableDateValidationDiagnostic diagnostic in builder.Validate())
    Console.WriteLine(diagnostic);   // [Error] BODU-CAL-...: message

if (builder.TryBuild(out NotableDateResource? resource, out var diagnostics))
{
    INotableDateService service = new NotableDateService(resource!);
}
```

Argument errors (`null` inputs) still throw from the `Try` overloads - the `Try` contract covers document data, not API usage.

## Severities

| Severity | Meaning |
|---|---|
| `Error` | The document cannot be loaded. Throwing overloads raise `NotableDateValidationException`; `Try` overloads return `false`. |
| `Warning` | The document loads, but something merits attention (currently produced only by non-fatal XSD findings under `BODU-CAL-SCHEMA`). |
| `Information` | Advisory only. |

## Diagnostic codes

Codes are stable identifiers: they are safe to match on, suppress by, or document against. Messages carry the dynamic context (concept, rule, and policy identifiers).

### Input and schema

| Code | Severity | Condition |
|---|---|---|
| `BODU-CAL-SYNTAX` | Error | The input is not well-formed XML/JSON, or the JSON content is empty; or an imported resource's content is not well-formed. For the loaded document's own text it is produced by the `Try` overloads, and the throwing overloads raise `FormatException` for the same input; for an imported resource every overload reports it as a diagnostic naming the import. |
| `BODU-CAL-SCHEMA` | Error or Warning | The XML document violates the embedded notable-date XSD; the severity mirrors the schema validator's. In JSON, which no schema validates, an identifier, reference or territory code that breaks the pattern the XSD requires of it (`"id": "New-Year"`, a territory `"au"`) is reported under the same code as an error naming the property, the value and the pattern. So is an object without a property the XSD requires of it (a concept's `id`, `displayName`, `category` or `rules`, a strategy's `month`, an `AddRule` override's `rule`, the root's `schemaVersion`, …), named with the XSD type and the property; a concept's `rules` or a weekly recurrence's `days` that holds no item; and an integer outside the range the XSD allows (`"durationDays": 0`, a year list item `-5`, a `day` of `32`, an ordinal `0`, `367` or `"+5"`), named with the bound it passes. A missing strategy, recurrence or end strategy is reported as `BODU-CAL-RULE-SOURCE` or `BODU-CAL-DURATION-NOEND` instead, and an empty optional list reads as absent. |

### Document structure

| Code | Severity | Condition |
|---|---|---|
| `BODU-CAL-DUP-ND` | Error | Two notable-date concepts share an identifier and the document's `duplicatePolicy` is `Error`, the default; `KeepFirst`, `KeepLast` and `Merge` settle the document's own duplicate concepts instead. |
| `BODU-CAL-DUP-RULE` | Error | Two rules within one concept share an identifier and the document's `duplicatePolicy` is `Error` or `Merge`; `KeepFirst` and `KeepLast` settle the document's own duplicate rules instead. An `<AddRule>` override that repeats a rule identifier, and two rules sharing one in an imported concept, are reported under every policy. |
| `BODU-CAL-DUP-POLICY` | Error | Two adjustment policies share an identifier. |
| `BODU-CAL-MONTH` | Error | A strategy's month is not one its rule accepts. A `Fixed` rule in a calendar other than Gregorian takes an integer from 1 to the most months its calendar's year can have: 13 in the Hebrew calendar, and in the Chinese lunisolar calendar unless the month skips the leap month (`skipLeapMonth`), and 12 in the Hijri, Umm al-Qura and Persian calendars. A Hebrew rule also takes a Hebrew month name (`Tishri`, `Heshvan`, `Kislev`, `Tevet`, `Shevat`, `AdarI`, `AdarII`, `LastAdar`, `Nisan`, `Iyar`, `Sivan`, `Tammuz`, `Av` or `Elul`, matched ignoring case as an English month name is), which no other calendar takes. Any other month takes a full English month name or an integer from 1 to 12. The message names the forms the rule's calendar takes. |
| `BODU-CAL-DAY` | Error | A strategy's day is outside the valid range for its month/calendar. |
| `BODU-CAL-ENUM` | Error | A field that takes one of a fixed set of names (a category, a collision or duplicate policy, a trigger, action or emission mode, a weekday, a week ordinal, a direction, a boundary, a calendar, an override's `operation`, …) holds a value that names none of them, such as a misspelling or a number; in JSON, an item of a list of such names (`categories`, `weekdays`, `days`, …) that is a number, a Boolean or `null` is reported the same way, naming the item's JSON text. Names are matched ignoring case; an absent value keeps the field's default. In XML the schema reports it too. |
| `BODU-CAL-WORKING-DAYS` | Error | The resolution policy's `workingDays` is not the seven-character, Sunday-first binary pattern (`0111110`); a letter mask such as `_MTWTF_` is rejected too. In XML the schema reports it too. |
| `BODU-CAL-VALUE` | Error | A field that takes an integer (a priority, a year, a day count, an offset, an ordinal, an interval, or an item of an `onlyYears` / `exceptYears` list), a Boolean (`true`, `false`, `1` or `0`), a `yyyy-MM-dd` date (a recurrence's `anchorDate`), or an adjustment trigger's `month` (a full English month name or 1-12) holds text that is not one, such as `high` for a priority, a fraction, or a number beyond the 32-bit range; or, in JSON, a field that holds a value holds an object or an array (`"priority": {}`, `"displayName": []`), a property that holds an object or a list holds a value of another kind (`"applicability": 5`, `"tags": "civic"`), an item of a list of territory codes, references, policy references or tags is not a string, an item of a scope's `ruleRefs` is neither a rule identifier nor a reference object, an item of a list of names or years is an object or an array, or an item of a list of concepts, rules, adjustment policies, imports, uses or overrides is not an object (`"notableDates": [5]`), which is left out rather than read as an object with every property absent. Such a value or item is named by its JSON kind (`object`, `array`, `number`, `Boolean` or `null`) rather than quoted, so `LoadJson` and `NotableDateDocumentBuilder.FromJson` report it with the same message. An absent value keeps the field's default. In XML the schema reports most of these too; a trigger's `month`, which the schema types as text, is reported only here. |
| `BODU-CAL-YEARS` | Error | A rule's or adjustment scope's `fromYear` is after its `toYear`. |
| `BODU-CAL-RECURRENCE` | Error | A recurrence declaration is invalid. |
| `BODU-CAL-STRATEGY` | Error | A strategy value lies outside the strategy's range (for example a `<Fixed>` or `<WeekdayNearDate>` day outside 1-31, an `<OrdinalDayOfMonth>` ordinal beyond 31, ISO week 54, or a zero ordinal), or a strategy that refers to another rule names it by a `notableDateRef` or `ruleRef` that is not an identifier (such as `New-Year`, or an empty reference); such a reference is not resolved, so no cross-reference error is reported for it. In XML the schema reports most of these too. |
| `BODU-CAL-RULE-SOURCE` | Error | A rule declares no recognized strategy or recurrence, or more than one (a strategy and a recurrence, or two strategy kinds); or a `PatchRule` override that replaces the occurrence source names no recognized kind, or more than one. XML documents also fail the schema for the same rule; the code exists for JSON, which is not schema-validated, and for a `PatchRule` carrying both a `<Strategy>` and a `<Recurrence>`, which the schema permits. |

### Cross-references

| Code | Severity | Condition |
|---|---|---|
| `BODU-CAL-ALGORITHM` | Error | An `<Algorithm>` strategy references a key that is neither built in nor declared by the supplied custom registry. |
| `BODU-CAL-ADJREF` | Error | A rule references an adjustment policy that does not exist. |
| `BODU-CAL-OFFSET-MISSING` | Error | An `<OffsetFromRule>` reference does not resolve to any rule. |
| `BODU-CAL-OFFSET-AMBIGUOUS` | Error | An `<OffsetFromRule>` reference resolves to more than one rule. |
| `BODU-CAL-REF-RECURRING` | Error | A cross-rule reference targets a recurring rule, which cannot anchor a single-occurrence offset. |
| `BODU-CAL-REPLACE-MISSING` | Error | An adjustment policy's `ReplaceWithRule` reference is missing or does not resolve. |
| `BODU-CAL-REPLACE-AMBIGUOUS` | Error | An adjustment policy's `ReplaceWithRule` reference resolves to more than one rule. |
| `BODU-CAL-HANDLER-MISSING` | Error | A policy declares `AdjustmentAction.Custom` without an action handler key. |
| `BODU-CAL-TRIGGER-HANDLER-MISSING` | Error | A policy declares `AdjustmentTrigger.Custom` without a trigger handler key. |

### Durations

| Code | Severity | Condition |
|---|---|---|
| `BODU-CAL-DURATION-CONFLICT` | Error | A rule declares conflicting duration sources: both a `durationDays` and an `untilDate`, or an `untilDate` end strategy naming more than one strategy kind. |
| `BODU-CAL-DURATION-NOEND` | Error | A duration end declaration is incomplete: an `untilDate` has no end strategy, or its end strategy names no recognized strategy kind. |
| `BODU-CAL-DURATION-ALGORITHM` | Error | A duration end strategy references an unknown algorithm key. |
| `BODU-CAL-DURATION-OFFSET-MISSING` | Error | A duration end `OffsetFromRule` reference does not resolve. |
| `BODU-CAL-DURATION-OFFSET-AMBIGUOUS` | Error | A duration end `OffsetFromRule` reference resolves to more than one rule. |

### Imports and overrides

| Code | Severity | Condition |
|---|---|---|
| `BODU-CAL-IMPORT-MISSING` | Error | An import targets a resource the resolver could not supply. |
| `BODU-CAL-IMPORT-CYCLE` | Error | An import chain forms a cycle; the offending import is skipped. |
| `BODU-CAL-IMPORT-CONCEPT` | Error | An import cherry-picks a concept the imported resource does not declare. |
| `BODU-CAL-OVERRIDE-ND` | Error | An override targets a notable-date concept that does not exist. |
| `BODU-CAL-OVERRIDE-RULE` | Error | An override targets a rule that does not exist. |

### Builder

| Code | Severity | Condition |
|---|---|---|
| `BODU-CAL-BUILDER-INCOMPLETE` | Error | The authored document is too incomplete to serialize for validation - a missing resource identifier, a concept with no rules, or a rule with no strategy. Produced only by `NotableDateDocumentBuilder.Validate()` / `TryBuild(...)`; the serializing members throw `InvalidOperationException` for the same states. |

## Where to go next

- [Authoring notable date rules](rule-authoring.md) - the document model these diagnostics validate.
- [Authoring with the notable-date builder](notable-date-builder.md) - the fluent authoring surface behind `Validate()` / `TryBuild(...)`.
- [Builder round-trip guarantees](round-trip-guarantees.md) - the serialization contract shared by `Build()` and the lint.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** - every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
