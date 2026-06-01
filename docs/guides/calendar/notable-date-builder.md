---
title: Authoring rules with the notable-date builder
---

# Authoring rules with the notable-date builder

`Bodu.Globalization.Calendar.Builder` is a fluent, chainable API for authoring `NotableDateRule` instances in C#. It is the programmatic peer of the XML / JSON authoring path documented in [Authoring notable date rules](rule-authoring.md) — both produce the same rule shape and feed into the same resolution pipeline, but the builder keeps everything in code so consumers can clone, mutate, and round-trip rule sets without serialisation hops.

The fluent surface is layered:

| Builder | Configures | Obtained from |
|---|---|---|
| `NotableDateDocumentBuilder` | A complete document | Static factory `NotableDateDocumentBuilder.Create()` |
| `NotableDateBuilder` | A single named notable date | `NotableDateDocumentBuilder.AddDate(name, configure)` |
| `NotableDateRuleBuilder` | A single resolution rule | `NotableDateBuilder.AddRule(configure)` |
| `ObservanceAdjustmentBuilder` | A single adjustment on a rule | `NotableDateRuleBuilder.AddAdjustment(key, configure)` |

Every method returns its parent builder, so the entire document composes as one fluent expression.

## Building a document

The entry point is the document builder. It accumulates named notable-date entries and emits the result in five forms.

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;

NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create();
```

Each `AddDate(name, configure)` call registers one notable date — the name corresponds to the `<NotableDate name="…">` element in XML.

```csharp
builder.AddDate("Christmas Day", date =>
{
    date.AddRule(rule => rule
        .Category(NotableDateCategory.Holiday)
        .Fixed(month: 12, day: 25)
        .NonWorking(true));
});
```

## Producing output

When the document is complete, choose the emission form:

```csharp
IReadOnlyList<NotableDateRule> rules     = builder.Build();
XDocument                     xmlDoc    = builder.ToXDocument();
string                        xmlString = builder.ToXml();
JsonObject                    jsonNode  = builder.ToJsonNode();
string                        jsonString = builder.ToJson();
INotableDateRuleProvider      provider  = builder.ToProvider();
```

- `Build()` — returns the in-memory rule set. Use this when wiring the builder directly into a test, the range-resolution pipeline, or a one-off `NotableDateService` instance.
- `ToXDocument()` / `ToXml()` — emits the document in the schema-valid XML form recognised by <xref:Bodu.Globalization.Calendar.XmlResourceNotableDateRuleProvider>. The XML conforms to the `urn:bodu:globalization:calendar` namespace and is suitable for embedding in a resource assembly.
- `ToJsonNode()` / `ToJson()` — emits the JSON peer, conforming to `NotableDates.schema.json` and consumable by <xref:Bodu.Globalization.Calendar.JsonResourceNotableDateRuleProvider>.
- `ToProvider()` — wraps `Build()` in an <xref:Bodu.Globalization.Calendar.InlineNotableDateRuleProvider> for passing directly to `NotableDateService`.

The XML / JSON forms strip programmatic-only fields that have no schema representation: `AddHandlerParameter` entries on adjustments and `MaxAdjustmentReachDays` are preserved in `Build()` / `ToProvider()` but dropped from `ToXDocument()` / `ToJsonNode()`.

## Configuring a rule

`NotableDateRuleBuilder` is the most-used builder. Set the required fields (category, strategy), then add scope (territory, year bounds, calendar) and adjustments as needed.

### Category

Every rule requires a <xref:Bodu.Globalization.Calendar.NotableDateCategory>:

```csharp
rule.Category(NotableDateCategory.Holiday);
```

### Resolution strategy

Exactly one resolution strategy must be set. Calling a second strategy method throws `InvalidOperationException` — use `ClearStrategy()` first if you are mutating a cloned rule.

| Method | Strategy |
|---|---|
| `.Fixed(month, day, skipLeapMonth?, sweepCalendarYears?)` | Fixed Gregorian month / day. |
| `.Fixed(monthToken, day, …)` | Fixed calendar-specific month token — `"January"`, `"Tishri"`, `"LastAdar"` (Hebrew), or an integer month. |
| `.DayOfWeekInMonth(month, dayOfWeek, weekOrdinal)` | Nth occurrence of a weekday in a month. |
| `.OffsetFromAnchor(anchorRuleName, offsetDays)` | Signed day offset from another rule (typically Easter). |
| `.Algorithm(key?, algorithmType?, month?, day?)` | Delegated to a registered <xref:Bodu.Globalization.Calendar.INotableDateAlgorithm>. |

### Scope

```csharp
rule
    .Territory("AU")                 // single country or comma-separated subdivisions
    .FirstYear(1901)
    .LastYear(2099)
    .OccurrenceYears(4)              // resolves only when (year - firstYear) % 4 == 0
    .CalendarType(typeof(HebrewCalendar))
    .Duration(3)                     // multi-day span (default 1)
    .Priority(50)                    // tiebreaker on same-date collisions (lower wins)
    .NonWorking(true);
```

### Tags

Tags are free-form classification strings — `"NationalHoliday"`, `"BankClosed"`, `"SchoolHoliday"`, `"Christian"` — preserved into the resolved `NotableDate.Tags` for app-specific filtering.

```csharp
rule
    .AddTag("Federal")
    .AddTag("BankClosed")
    .RemoveTag("Optional")           // case-insensitive; no-op if absent
    .ClearTags();                    // start over
```

### Rule-level identifier

Use `RuleName(string)` when a single notable-date entry has multiple rules and you need to target a specific rule from a `<Use>` directive elsewhere or from an override provider.

```csharp
rule.RuleName("au-melbourne-cup-2023-+");
```

## Configuring an adjustment

`ObservanceAdjustmentBuilder` covers every field of <xref:Bodu.Globalization.Calendar.ObservanceAdjustment>. Trigger and action are required; everything else is optional.

```csharp
rule.AddAdjustment("weekend-roll", adj => adj
    .When(AdjustmentTrigger.IfWeekend)
    .Action(AdjustmentAction.MoveToNextMonday)
    .NonWorking(true));
```

The `key` argument (`"weekend-roll"` above) must be unique within the rule. It is used during rule inheritance / `<Use>` merging so a base adjustment can be overridden or removed by name from a derived rule.

### Trigger-specific parameters

Some triggers require additional context:

| Trigger | Parameter method | Notes |
|---|---|---|
| `IfDayOfWeek` | `.OnDayOfWeek(DayOfWeek)` | Required — the day to test. |
| `IfBeforeFixedDate`, `IfAfterFixedDate` | `.ComparisonDate(month, day)` | Required — the reference date. |
| `IfNthOccurrenceInMonth` | `.OrdinalOccurrence(WeekOfMonthOrdinal)` | Required — First, Second, Third, Fourth, Fifth, or Last. |
| `Custom` | `.HandlerKey(string)` + `.AddHandlerParameter(key, value)` | Required handler key plus optional parameters delivered to the custom handler. |

### Action-specific parameters

| Action | Parameter method | Notes |
|---|---|---|
| `AddDays` | `.OffsetDays(int)` | Required — can be negative. |
| `ReplaceWithNamedDate` | `.Target(ruleName)` | Required — the rule whose date replaces this one. |
| `UseCustomHandler` | `.HandlerKey(string)` | Required — the registered custom-handler key. |

### Adjustment scope

```csharp
adj
    .Territory("AU-NSW,AU-VIC")
    .CalendarType(typeof(GregorianCalendar))
    .FromYear(2010)
    .ToYear(2099)
    .Priority(10)                    // evaluated first when multiple adjustments fire
    .MaxAdjustmentReachDays(7);      // limits the range-resolution envelope
```

`MaxAdjustmentReachDays` and `AddHandlerParameter` are programmatic-only fields — they are honoured by the range-resolution pipeline and the custom-handler dispatch but are not part of the XML / JSON schema.

### Removing adjustments

```csharp
rule.RemoveAdjustment("weekend-roll");   // case-insensitive
rule.ClearAdjustments();                 // remove everything
```

## Cloning for template-mutate workflows

Every builder exposes `Clone()` for the common pattern of building a baseline rule and mutating per-territory or per-year variants.

```csharp
NotableDateRuleBuilder baseline = new NotableDateRuleBuilder()
    .Category(NotableDateCategory.Holiday)
    .Fixed(month: 12, day: 25)
    .NonWorking(true);

builder.AddDate("Christmas Day", date =>
{
    foreach (string country in new[] { "AU", "NZ", "GB", "US", "CA" })
    {
        date.AddRule(_ => baseline.Clone().Territory(country));
    }
});
```

For `NotableDateDocumentBuilder.Clone()`, the deep clone copies every nested rule and adjustment.

## End-to-end example

```csharp
using System.Linq;
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;

INotableDateRuleProvider provider = NotableDateDocumentBuilder.Create()

    // Easter anchor — algorithm-based.
    .AddDate("Easter Sunday", date => date
        .AddRule(rule => rule
            .Category(NotableDateCategory.Religious)
            .Algorithm("easter")))

    // Christmas Day — fixed date with weekend rollover per territory.
    .AddDate("Christmas Day", date => date
        .AddRule(rule => rule
            .Category(NotableDateCategory.Holiday)
            .Fixed(month: 12, day: 25)
            .Territory("AU,NZ,GB")
            .NonWorking(true)
            .AddAdjustment("weekend-roll", adj => adj
                .When(AdjustmentTrigger.IfWeekend)
                .Action(AdjustmentAction.MoveToNextMonday)
                .NonWorking(true))))

    // Good Friday — anchored to Easter.
    .AddDate("Good Friday", date => date
        .AddRule(rule => rule
            .Category(NotableDateCategory.Religious)
            .OffsetFromAnchor("Easter Sunday", -2)
            .Territory("AU,NZ,GB")
            .NonWorking(true)))

    // Australia Day — fixed, with a substitute-day adjustment.
    .AddDate("Australia Day", date => date
        .AddRule(rule => rule
            .Category(NotableDateCategory.Civic)
            .Fixed(month: 1, day: 26)
            .Territory("AU")
            .NonWorking(true)
            .AddAdjustment("weekend-roll", adj => adj
                .When(AdjustmentTrigger.IfWeekend)
                .Action(AdjustmentAction.MoveToNextWeekday))))

    .ToProvider();

var registry = new NotableDateAlgorithmRegistry()
    .Register("easter", new EasterSundayNotableDateAlgorithm());

var service = new NotableDateService(
    ruleProviders: new[] { provider },
    options: new NotableDateServiceOptions { AlgorithmRegistry = registry });

IReadOnlyList<NotableDate> christmas2027 = service.GetNotableDates(
    new DateTime(2027, 12, 25), "AU");
// Christmas Day 2027 (Saturday) → adjusted to Monday 27 December.
```

## Round-tripping through XML

When you want to author rules programmatically and then ship them as embedded XML in your own resource assembly, build → export → embed:

```csharp
string xml = NotableDateDocumentBuilder.Create()
    .AddDate("…", date => /* … */)
    .ToXml();

File.WriteAllText("MyApp/Calendar/Resources/region-custom.xml", xml);
```

The emitted XML conforms to the schema consumed by <xref:Bodu.Globalization.Calendar.XmlResourceNotableDateRuleProvider>, so it can be loaded by the standard XML pipeline at consumer time — including from a custom data-pack assembly that ships alongside the [region bundles](data-packs.md).

## When *not* to use the builder

- **Static, hand-authored rule files.** If you have an existing well-maintained XML rule resource, parse it with `XmlResourceNotableDateRuleProvider` and skip the builder entirely. The builder is for programmatic construction, not for editing XML.
- **Override providers.** Use <xref:Bodu.Globalization.Calendar.MutableNotableDateRuleOverrideProvider> for runtime additions / removals layered on top of a base rule set; the builder is for the base rule set itself.
- **Algorithm bodies.** The builder configures *references* to algorithms via `Algorithm(key)`; it does not register the algorithm implementations. Use <xref:Bodu.Globalization.Calendar.NotableDateAlgorithmRegistry> for that.

## See also

- [Notable-date builder API reference](xref:Bodu.Globalization.Calendar.Builder) — `NotableDateDocumentBuilder`, `NotableDateBuilder`, `NotableDateRuleBuilder`, `ObservanceAdjustmentBuilder`, `InlineNotableDateRuleProvider`.
- [Authoring notable date rules](rule-authoring.md) — the XML / JSON authoring path.
- [`NotableDateRule` reference](rule-reference.md) — every field the builder configures.
- [Observance adjustment rules](adjustment-rules.md) — full trigger and action catalogue.
- [Date calculation algorithms](algorithms.md) — algorithm-key list referenced by `Algorithm(key)`.
- [Resolution pipeline](resolution-pipeline.md) — the eight-stage pipeline that consumes built rules.
