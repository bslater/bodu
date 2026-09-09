---
title: Bodu.Globalization.Calendar.Builder — Introduction
---

# Bodu.Globalization.Calendar.Builder

![Bodu.Globalization.Calendar.Builder](../../images/hero-calendar-builder.svg)

**Bodu.Globalization.Calendar.Builder** is the fluent C# authoring API for notable-date documents. Instead of hand-writing XML or JSON on the notable-date schema, you compose the document in code — concepts, rules, strategies, adjustment policies, imports, overrides, and the resolution policy — then serialize it, save it, or materialize it straight into a validated <xref:Bodu.Globalization.Calendar.NotableDateResource>. It is a companion to the runtime in the **[Globalization & Calendars](../topics/globalization-and-calendars.md)** topic.

The builder never validates on its own. `Build()` renders the in-memory model to XML and pushes it through <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader>, so a document authored in code is held to exactly the same rules — and produces exactly the same `BODU-CAL-*` diagnostics — as one loaded from a file. What you serialize is what the runtime loads.

## What the builders author

| Builder | Authors | Entry point |
|---|---|---|
| <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder> | The document: resource id, schema version, metadata, resolution policy, adjustment policies, imports, notable-date concepts, overrides. | `Create()` / `Create(resourceId, schemaVersion)`, `FromXml` / `FromJson` / `Load` |
| <xref:Bodu.Globalization.Calendar.Builder.NotableDateDefinitionBuilder> | One notable-date concept: display name, category, default duration, default non-working flag, tags, and its rules. | `AddNotableDate(id, displayName, category, configure)` |
| <xref:Bodu.Globalization.Calendar.Builder.NotableDateRuleBuilder> | One rule: its strategy, territory / calendar scope, year periodicity, priority, category and non-working overrides, duration, tags, and adjustment references. | `definition.AddRule(id, configure)` |
| <xref:Bodu.Globalization.Calendar.Builder.AdjustmentPolicyBuilder> / <xref:Bodu.Globalization.Calendar.Builder.AdjustmentScopeBuilder> | A reusable adjustment policy — trigger (`When`), action (`Then`), emission (`Emit`) — and the scope it applies to. | `AddAdjustmentPolicy(id, configure)` / `WithScope(configure)` |
| <xref:Bodu.Globalization.Calendar.Builder.ImportBuilder> / <xref:Bodu.Globalization.Calendar.Builder.ImportUseBuilder> | An import of a common catalogue and the `<Use>` directives that re-scope individual concepts. | `AddImport(resource, configure)` / `Use(notableDateRef, configure)` |
| <xref:Bodu.Globalization.Calendar.Builder.OverrideBuilder> | ID-targeted `AddRule` / `PatchRule` / `RemoveRule` edits applied at load time. | `AddOverride(configure)` |
| <xref:Bodu.Globalization.Calendar.Builder.ResolutionPolicyBuilder> | The resource-level duplicate, collision, priority, observed-date-range, working-week, and category-precedence settings. | `WithResolutionPolicy(configure)` |

### Rule strategies

`NotableDateRuleBuilder` exposes one method per resolution strategy; a rule takes exactly one. The single-date family:

| Method | Strategy |
|---|---|
| `Fixed(month, day, …)` (month as `int` or name) | A fixed calendar date, with `skipLeapMonth` / `sweepCalendarYears` options. |
| `DayOfWeekInMonth(month, dayOfWeek, weekOrdinal)` | The *n*th (or last) weekday of a month. |
| `WeekdayNearDate(month, day, dayOfWeek, direction)` | The nearest weekday on or before / after a date. |
| `RelativeWeekdayInMonth(…)` | A weekday relative to another weekday-in-month anchor. |
| `OffsetFromRule(notableDateRef, offsetDays, ruleRef)` | A day offset from another rule's resolved date. |
| `Algorithm(key)` | A named algorithm (`western-easter`, `diwali`, …) from the registry. |
| `OrdinalDayOfMonth(month, ordinal)`, `DayOfYear(ordinal)`, `IsoWeekDate(week, dayOfWeek)` | Positional dates. |
| `WeekdayNearRule(…)`, `NthWeekdayFromRule(…)`, `WorkingDayOffsetFromRule(…)`, `WorkingDayInMonth(month, ordinal)` | Weekday- and working-day-relative rules. |

The frequency-based recurrence family — `DailyInterval(anchorDate, intervalDays)`, `Weekly(daysOfWeek, intervalWeeks, anchorDate)`, `MonthlyDay(dayOfMonth, intervalMonths, anchorDate, invalidDayBehavior)`, `MonthlyWeekday(dayOfWeek, weekOrdinal, intervalMonths, anchorDate)` — emits a stream of occurrences per year rather than one, and `UntilDate(endStrategy, …)` turns a single-date rule into a multi-day span that runs until a second strategy's date. Scope and periodicity are set with `ForTerritory` / `ForTerritories`, `ForCalendar`, `FromYear` / `ToYear`, `EveryYears` + `AnchorYear`, and `OnlyYears` / `ExceptYears`; adjustments attach with `WithAdjustment(policyRef)` / `WithAdjustments`. The [rule authoring guide](../../guides/calendar/rule-authoring.md) explains what each strategy computes.

### Output and input

| Direction | XML (full fidelity) | JSON (subset) | Binary pack |
|---|---|---|---|
| To text / object | `ToXml()`, `ToXDocument()` | `ToJson()`, `ToJsonObject()` | — |
| To file | `Save(path)` (by extension) or `Save(path, NotableDateDocumentFormat.Xml)` | `Save(path, NotableDateDocumentFormat.Json)` | `SaveBinary(path)` / `SaveBinary(stream, importResolver)`, or `Save(path, NotableDateDocumentFormat.Binary)` |
| From text / object | `FromXml(xml)`, `FromXDocument(document)` | `FromJson(json)`, `FromJsonObject(json)` | not re-openable (a pack is compiled output; load it with `NotableDateResourceLoader.LoadBinary`) |
| From file | `Load(path)` — `.xml` or `.json` inferred from the extension | | |

XML expresses every feature of the schema. JSON is a narrower subset — it omits imports, restricts calendars to Gregorian, and supports a reduced trigger / action / override surface — so `ToJson()` throws `NotSupportedException` for a document that uses a feature the subset cannot represent. <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentFormat> names the three forms. The [round-trip guarantees guide](../../guides/calendar/round-trip-guarantees.md) states exactly what survives each direction.

### Materializing

- `Build()` / `Build(importResolver)` — load and validate through the canonical loader, returning a `NotableDateResource`; throws `InvalidOperationException` for an incomplete document (no resource id, a concept without rules, a rule without a strategy, a policy missing its trigger, action, or emission) and <xref:Bodu.Globalization.Calendar.NotableDateValidationException> when the loader reports errors.
- `TryBuild(out resource, out diagnostics)` / `Validate()` — the collect-mode equivalents: every <xref:Bodu.Globalization.Calendar.NotableDateValidationDiagnostic> is returned rather than thrown. See [validation diagnostics](../../guides/calendar/validation-diagnostics.md).
- `ToProvider()` — wrap the built resource in an <xref:Bodu.Globalization.Calendar.INotableDateResourceProvider> for the reloadable-service workflow.
- `Clone()` — an independent copy of the in-memory model, for deriving variants.

Pass an import resolver (`CommonNotableDateResources.Resolver`, or your own `Func<string, string?>`) to `Build`, `TryBuild`, `Validate`, and `SaveBinary` whenever the document declares imports.

## Install

```bash
dotnet add package Bodu.Globalization.Calendar.Builder
```

Targets `net8.0`. **Depends on** `Bodu.Globalization.Calendar` (and, through it, `Bodu.Core`). Status: **Stable** (see the [package matrix](../package-matrix.md)).

## Minimal sample

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;
using Bodu.Globalization.Calendar.RangeResolution;   // EmissionMode

NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("contoso-holidays")
    .AddAdjustmentPolicy("weekend-roll", p => p
        .When(AdjustmentTrigger.IfWeekend)
        .Then(AdjustmentAction.MoveToNextWorkingDay)
        .Emit(EmissionMode.ObservedOnly))
    .AddNotableDate("founders-day", "Founders' Day", NotableDateCategory.Civic, c => c
        .AsNonWorkingByDefault()
        .AddRule("fixed", r => r
            .Fixed(3, 14)
            .ForTerritory("US")
            .WithAdjustment("weekend-roll")))
    .AddNotableDate("quarterly-close", "Quarterly Close", NotableDateCategory.Other, c => c
        .AddRule("last-weekday", r => r
            .WorkingDayInMonth(3, -1)
            .WithTags("finance")));

// Materialize through the canonical loader — same validation as a file on disk.
NotableDateResource resource = builder.Build();
var service = new NotableDateService(resource);

foreach (NotableDate date in service.Resolve(2026, "US"))
    Console.WriteLine($"{date.Date:yyyy-MM-dd} {date.DisplayName}");   // 2026-03-16 Founders' Day (14 Mar is a Saturday), 2026-03-31 Quarterly Close

// The same model serializes to XML or JSON, and reloads.
builder.Save("contoso-holidays.xml");
NotableDateDocumentBuilder reloaded = NotableDateDocumentBuilder.Load("contoso-holidays.xml");
string json = reloaded.ToJson();
```

## Where to go next

- **[Authoring with the notable-date builder](../../guides/calendar/notable-date-builder.md)** — the builder hierarchy end to end: rules and strategies, adjustment policies, importing the common catalogues, overrides, resolution policy, materializing, saving, and cloning.
- **[Builder round-trip guarantees](../../guides/calendar/round-trip-guarantees.md)** — the guarantee matrix for XML, JSON, and `Build` parity, and what is not guaranteed.
- **[Authoring notable date rules](../../guides/calendar/rule-authoring.md)** — the schema the builder targets, so you can read the XML it emits.
- **[Binary rule packs](../../guides/calendar/binary-rule-packs.md)** — `SaveBinary` and the `.bcal` format the [tooling](../calendar-tooling/index.md) compiles.
- **[Bodu.Globalization.Calendar.Builder API reference](xref:Bodu.Globalization.Calendar.Builder)** — full type-by-type docs.
- **[Bodu.Globalization.Calendar introduction](../calendar/index.md)** — the runtime the builder feeds.
