---
uid: Bodu.Globalization.Calendar.Builder
---

# Bodu.Globalization.Calendar.Builder

## Purpose

**Bodu.Globalization.Calendar.Builder** is a fluent, chainable C# API for authoring [`Bodu.Globalization.Calendar`](Bodu.Globalization.Calendar.md) notable-date documents on the v2 cookbook schema. It is the in-code peer of hand-writing the XML or JSON: the same document model, assembled through a nested builder hierarchy, then serialized to a schema-valid form, saved to a file, or materialized straight into a <xref:Bodu.Globalization.Calendar.NotableDateResource>.

Reach for it when rule sets are produced programmatically — exporting curated data packs, composing documents in tests, or transforming an external holiday source into the cookbook schema — rather than editing XML by hand.

## Static documentation

- **[Authoring with the notable-date builder](~/guides/calendar/notable-date-builder.md)** — the end-to-end fluent walkthrough.
- **[Authoring notable date rules](~/guides/calendar/rule-authoring.md)** — the equivalent XML / JSON authoring path and the document model it produces.

## Key types

- <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder> — the entry point, created via `NotableDateDocumentBuilder.Create(resourceId)`. Accumulates metadata, a resolution policy, adjustment policies, imports, notable-date definitions, and overrides, then emits the document as XML (`ToXml()` / `ToXDocument()`), JSON (`ToJson()` / `ToJsonObject()`), a built <xref:Bodu.Globalization.Calendar.NotableDateResource> (`Build()`), or an <xref:Bodu.Globalization.Calendar.INotableDateResourceProvider> (`ToProvider()`). It also offers `Save(path)` / `Load(path)`, the static `FromXml` / `FromJson` parsers for round-trip editing, and `Clone()`.
- <xref:Bodu.Globalization.Calendar.Builder.NotableDateDefinitionBuilder> — one `<NotableDate>` concept (display name, category, default duration / non-working, tags, rules), obtained from `AddNotableDate(id, displayName, category, configure)`.
- <xref:Bodu.Globalization.Calendar.Builder.NotableDateRuleBuilder> — one `<Rule>` (priority, category, applicability, tags, adjustment references) carrying exactly one of the six resolution strategies — `Fixed`, `DayOfWeekInMonth`, `WeekdayNearDate`, `RelativeWeekdayInMonth`, `OffsetFromRule`, `Algorithm`; obtained from `AddRule(id, configure)`. Selecting a second strategy throws.
- <xref:Bodu.Globalization.Calendar.Builder.AdjustmentPolicyBuilder> and <xref:Bodu.Globalization.Calendar.Builder.AdjustmentScopeBuilder> — a reusable `<AdjustmentPolicy>` (scope, trigger, action, emission, handler parameters), added with `AddAdjustmentPolicy(id, configure)` and referenced from a rule by id.
- <xref:Bodu.Globalization.Calendar.Builder.ResolutionPolicyBuilder> — the resource-level `<ResolutionPolicy>` (duplicate / collision / priority / observed-date policy and the working week).
- <xref:Bodu.Globalization.Calendar.Builder.ImportBuilder> and <xref:Bodu.Globalization.Calendar.Builder.ImportUseBuilder> — `<Imports>` that pull concepts from the bundled common catalogues, optionally re-scoping them.
- <xref:Bodu.Globalization.Calendar.Builder.OverrideBuilder> — ID-targeted `<Overrides>` (`AddRule` / `PatchRule` / `RemoveRule`).
- <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentFormat> — selects XML or JSON for the explicit `Save(path, format)` overload.

## Minimal sample

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;
using Bodu.Globalization.Calendar.RangeResolution;   // EmissionMode

NotableDateResource resource = NotableDateDocumentBuilder.Create("contoso.holidays")
    .AddAdjustmentPolicy("weekend-to-monday", a => a
        .When(AdjustmentTrigger.IfWeekend)
        .Then(AdjustmentAction.MoveToNextWorkingDay)
        .Emit(EmissionMode.ObservedOnly))
    .AddNotableDate("new-years-day", "New Year's Day", NotableDateCategory.PublicHoliday, d => d
        .AsNonWorkingByDefault()
        .AddRule("default", r => r
            .ForTerritory("US")
            .Fixed(1, 1)
            .WithAdjustment("weekend-to-monday")))
    .AddNotableDate("thanksgiving", "Thanksgiving Day", NotableDateCategory.PublicHoliday, d => d
        .AsNonWorkingByDefault()
        .AddRule("default", r => r
            .ForTerritory("US")
            .DayOfWeekInMonth(11, DayOfWeek.Thursday, WeekOrdinal.Fourth)))
    .Build();

// Or serialize / persist the authored document:
string xml = NotableDateDocumentBuilder.Create("contoso.holidays") /* … */ .ToXml();
NotableDateDocumentBuilder.Create("contoso.holidays") /* … */ .Save("holidays.json");
```

## Notes

- **XML is full-fidelity; JSON is the documented subset.** `ToXml()` emits the entire schema; `ToJson()` emits the narrower JSON form and throws <xref:System.NotSupportedException> for features the JSON schema cannot model (imports, non-Gregorian calendars, XML-only trigger/action values, handler parameters, scope year-bounds).
- **`Build()` defers to the canonical loader.** It serializes to XML and loads through <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader>, so a built resource is identical to one loaded from the equivalent file — and the same validation applies (a malformed document throws <xref:Bodu.Globalization.Calendar.NotableDateValidationException>). Pass an import resolver (e.g. `CommonNotableDateResources.Resolver`) to `Build(resolver)` when the document imports catalogues.
- **Single strategy per rule.** Each `<Rule>` commits to exactly one strategy; a second strategy call throws <xref:System.InvalidOperationException>.
- **Target framework.** `net8.0`.
