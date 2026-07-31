---
title: Calendar validation diagnostics
---

# Calendar validation diagnostics

Every notable-date document — hand-authored XML/JSON, a bundled catalogue, or a document produced by the <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder> — passes through one validation pipeline in <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader>. Each problem the pipeline finds is a <xref:Bodu.Globalization.Calendar.NotableDateValidationDiagnostic> carrying a severity, a stable `BODU-CAL-*` code, and a message naming the offending concept, rule, or policy. This page is the code reference and shows the two ways to consume diagnostics: throwing loads and collect-mode linting.

## Throw or collect

**Throwing (the default).** `NotableDateResourceLoader.Load(...)` / `LoadJson(...)` and `NotableDateDocumentBuilder.Build()` validate the whole document, then throw a single <xref:Bodu.Globalization.Calendar.NotableDateValidationException> when any error-severity diagnostic was produced. The exception's `Diagnostics` property carries the complete list — errors, warnings, and informational messages — not just the first failure.

**Collecting (the lint surface).** The `Try` overloads return the same complete diagnostic list without throwing, so build tooling and editors can lint arbitrary input:

- `NotableDateResourceLoader.TryLoad(xml, resolver, out resource, out diagnostics)` (plus an overload taking a custom <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithmRegistry>) and `TryLoadJson(...)` — malformed input becomes a `BODU-CAL-SYNTAX` error diagnostic instead of a `FormatException`.
- `NotableDateDocumentBuilder.Validate()` — lints the authored document and returns the diagnostics; a clean result guarantees `Build()` succeeds for the same document, because both run the same pipeline.
- `NotableDateDocumentBuilder.TryBuild(out resource, out diagnostics)` — the non-throwing counterpart of `Build()`.

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

Argument errors (`null` inputs) still throw from the `Try` overloads — the `Try` contract covers document data, not API usage.

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
| `BODU-CAL-SYNTAX` | Error | The input is not well-formed XML/JSON, or the JSON content is empty. Produced by the `Try` overloads; the throwing overloads raise `FormatException` for the same input. |
| `BODU-CAL-SCHEMA` | Error or Warning | The XML document violates the embedded notable-date XSD; the severity mirrors the schema validator's. |

### Document structure

| Code | Severity | Condition |
|---|---|---|
| `BODU-CAL-DUP-ND` | Error | Two notable-date concepts share an identifier. |
| `BODU-CAL-DUP-RULE` | Error | Two rules within one concept share an identifier. |
| `BODU-CAL-DUP-POLICY` | Error | Two adjustment policies share an identifier. |
| `BODU-CAL-MONTH` | Error | A strategy's month is outside the valid range for its calendar. |
| `BODU-CAL-DAY` | Error | A strategy's day is outside the valid range for its month/calendar. |
| `BODU-CAL-YEARS` | Error | A rule's or adjustment scope's `fromYear` is after its `toYear`. |
| `BODU-CAL-RECURRENCE` | Error | A recurrence declaration is invalid. |

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
| `BODU-CAL-DURATION-CONFLICT` | Error | A rule declares conflicting duration sources. |
| `BODU-CAL-DURATION-NOEND` | Error | A duration end declaration is incomplete. |
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
| `BODU-CAL-BUILDER-INCOMPLETE` | Error | The authored document is too incomplete to serialize for validation — a missing resource identifier, a concept with no rules, or a rule with no strategy. Produced only by `NotableDateDocumentBuilder.Validate()` / `TryBuild(...)`; the serializing members throw `InvalidOperationException` for the same states. |

## Where to go next

- [Authoring notable date rules](rule-authoring.md) — the document model these diagnostics validate.
- [Authoring with the notable-date builder](notable-date-builder.md) — the fluent authoring surface behind `Validate()` / `TryBuild(...)`.
- [Builder round-trip guarantees](round-trip-guarantees.md) — the serialization contract shared by `Build()` and the lint.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
