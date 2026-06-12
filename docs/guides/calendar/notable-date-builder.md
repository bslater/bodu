---
title: Authoring with the notable-date builder
---

# Authoring with the notable-date builder

`Bodu.Globalization.Calendar.Builder` is a fluent, chainable API for authoring notable-date documents in C#. It is the programmatic peer of the XML / JSON authoring path in [Authoring notable date rules](rule-authoring.md): both produce the same notable-date document, feed the same resolution pipeline, and validate through the same loader — but the builder keeps everything in code, so documents can be composed, cloned, serialized, and round-tripped without writing XML by hand.

For the vocabulary it assumes (document vs. resource, definition vs. rule, strategy, adjustment policy, nominal vs. observed) read [Core concepts](../../docs/calendar/concepts.md) first. For the full type list see the [`Bodu.Globalization.Calendar.Builder` reference](xref:Bodu.Globalization.Calendar.Builder).

## The builder hierarchy

Each level is reached through a nested `Add*(…, configure)` callback that hands you the child builder and returns the parent for chaining:

| Builder | Configures | Obtained from |
|---|---|---|
| <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder> | The whole `<NotableDateResource>` document | `NotableDateDocumentBuilder.Create(resourceId)` |
| <xref:Bodu.Globalization.Calendar.Builder.ResolutionPolicyBuilder> | The `<ResolutionPolicy>` | `WithResolutionPolicy(configure)` |
| <xref:Bodu.Globalization.Calendar.Builder.AdjustmentPolicyBuilder> | A reusable `<AdjustmentPolicy>` | `AddAdjustmentPolicy(id, configure)` |
| <xref:Bodu.Globalization.Calendar.Builder.NotableDateDefinitionBuilder> | One `<NotableDate>` concept | `AddNotableDate(id, displayName, category, configure)` |
| <xref:Bodu.Globalization.Calendar.Builder.NotableDateRuleBuilder> | One `<Rule>` | `AddRule(id, configure)` |
| <xref:Bodu.Globalization.Calendar.Builder.ImportBuilder> / <xref:Bodu.Globalization.Calendar.Builder.ImportUseBuilder> | `<Imports>` from the common catalogues | `AddImport(resource, configure)` |
| <xref:Bodu.Globalization.Calendar.Builder.OverrideBuilder> | ID-targeted `<Overrides>` | `AddOverride(configure)` |

## Building a document

`Create` starts an empty document; `AddNotableDate` adds a concept, and each concept holds one or more rules:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;

NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("contoso.holidays")
    .WithMetadata(name: "Contoso holidays", description: "Company observances.")
    .AddNotableDate("new-years-day", "New Year's Day", NotableDateCategory.PublicHoliday, d => d
        .AsNonWorkingByDefault()
        .AddRule("default", r => r.ForTerritory("US").Fixed(1, 1)));
```

## Rules and strategies

<xref:Bodu.Globalization.Calendar.Builder.NotableDateRuleBuilder> sets the rule's scalars (`WithPriority`, `WithCategory`, `AsNonWorking`, `WithDurationDays`, `WithComment`, `AddTag`), its applicability (`ForCalendar`, `ForTerritory` / `ForTerritories`, `FromYear`, `ToYear`, `EveryYears`, `AnchorYear`, `OnlyYears`, `ExceptYears`), its adjustment references (`WithAdjustment`), and **exactly one** of the six resolution strategies:

```csharp
r.Fixed(1, 1);                                                       // 1 January (month name or number)
r.DayOfWeekInMonth(11, DayOfWeek.Thursday, WeekOrdinal.Fourth);     // 4th Thursday in November
r.WeekdayNearDate(5, 24, DayOfWeek.Monday, WeekdayProximity.OnOrBefore);
r.RelativeWeekdayInMonth(11, DayOfWeek.Monday, WeekOrdinal.First, DayOfWeek.Tuesday, WeekdayProximity.After);
r.OffsetFromRule("easter-sunday", -2, ruleRef: "default");          // Good Friday = Easter − 2
r.Algorithm("western-easter");                                       // a named algorithm key
```

Selecting a second strategy on the same rule throws <xref:System.InvalidOperationException> — each rule commits to one. See [Date calculation algorithms](algorithms.md) for the strategy semantics and the `<Algorithm>` keys.

## Adjustment policies

A weekend-substitution or "move to the next working day" shift is authored once as a reusable policy and referenced from any rule by id (there are no inline per-rule adjustments):

```csharp
builder
    .AddAdjustmentPolicy("weekend-to-monday", a => a
        .WithDescription("Observe weekend holidays on the next working day.")
        .When(AdjustmentTrigger.IfWeekend)
        .Then(AdjustmentAction.MoveToNextWorkingDay)
        .Emit(Bodu.Globalization.Calendar.RangeResolution.EmissionMode.ObservedOnly))
    .AddNotableDate("anzac-day", "Anzac Day", NotableDateCategory.PublicHoliday, d => d
        .AsNonWorkingByDefault()
        .AddRule("default", r => r.ForTerritory("AU").Fixed(4, 25).WithAdjustment("weekend-to-monday")));
```

<xref:Bodu.Globalization.Calendar.Builder.AdjustmentPolicyBuilder> also exposes the trigger modifiers (`OnTriggerWeekdays`, `WithTriggerMonth`, …), action modifiers (`WithActionDays`, `WithMaxSearchDays`, `SkipWeekends`, `WithReplacementRule`, …), emission (`WithReason`, `EmitNonWorking`), handler parameters (`WithParameter`), and a `WithScope(configure)` callback over <xref:Bodu.Globalization.Calendar.Builder.AdjustmentScopeBuilder>. See [Observance adjustment rules](adjustment-rules.md) for the full trigger / action / emission catalogues.

## Importing the common catalogues

`AddImport` pulls concepts from the bundled common catalogues; `Use` cherry-picks and re-scopes them:

```csharp
builder.AddImport("global-core", i => i
    .Use("new-years-day", u => u.ForTerritory("US").WithAdjustment("weekend-to-monday")));
```

## Overrides

`AddOverride` authors ID-targeted edits applied at load time:

```csharp
builder.AddOverride(o => o
    .RemoveRule("boxing-day", "default")
    .AddRule("company-founding-day", "hq", r => r.ForTerritory("US").Fixed(6, 15)));
```

## Resolution policy

```csharp
builder.WithResolutionPolicy(p => p
    .WithDuplicatePolicy(Bodu.Globalization.Calendar.RangeResolution.DuplicatePolicy.KeepFirst)
    .WithWorkingWeek(WeekPattern.MondayToFriday));
```

## Materializing, serializing, and saving

A finished builder produces the document in several forms:

```csharp
// 1. A built, validated resource — ready for a NotableDateService.
NotableDateResource resource = builder.Build();                  // Build(resolver) when the document imports
NotableDateService  service  = new NotableDateService(resource);

// 2. An INotableDateResourceProvider (for the reloadable service / DI).
INotableDateResourceProvider provider = builder.ToProvider();

// 3. Serialized text — full-fidelity XML, or the JSON subset.
string xml  = builder.ToXml();      // also ToXDocument()
string json = builder.ToJson();     // also ToJsonObject()

// 4. Straight to a file (format inferred from the extension).
builder.Save("holidays.xml");
builder.Save("holidays.json");
```

`Build()` serializes to XML and loads through <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader>, so the built resource is exactly what the runtime would load — and the same validation applies (`Build()` throws <xref:Bodu.Globalization.Calendar.NotableDateValidationException> on an invalid document).

> [!NOTE]
> XML is the full-fidelity format. `ToJson()` / `Save(*.json)` emit the narrower JSON subset and throw <xref:System.NotSupportedException> when the document uses a feature the JSON schema cannot model — imports, a non-Gregorian calendar, an XML-only trigger/action value, handler parameters, or scope year-bounds. Serialize those documents as XML.

## Round-tripping and cloning

`FromXml` / `FromJson` parse a document back into a builder for editing, and `Load(path)` reads a file by extension — so you can load, mutate, and re-save:

```csharp
NotableDateDocumentBuilder edited = NotableDateDocumentBuilder.Load("holidays.xml");
edited.AddNotableDate("juneteenth", "Juneteenth", NotableDateCategory.PublicHoliday, d => d
    .AddRule("default", r => r.ForTerritory("US").Fixed(6, 19)));
edited.Save("holidays.xml");

NotableDateDocumentBuilder copy = builder.Clone();   // deep, independent copy
```

## Where to go next

- [Authoring notable date rules](rule-authoring.md) — the XML / JSON document model the builder produces.
- [NotableDateRule and adjustment-policy reference](rule-reference.md) — the per-element field reference.
- [Date calculation algorithms](algorithms.md) — the six strategies and the `<Algorithm>` keys.
- [Using NotableDateService](notable-dates.md) — resolving the documents you build.
- [`Bodu.Globalization.Calendar.Builder` API reference](xref:Bodu.Globalization.Calendar.Builder) — the full type list.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
