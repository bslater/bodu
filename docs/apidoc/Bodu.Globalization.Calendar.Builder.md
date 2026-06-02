---
uid: Bodu.Globalization.Calendar.Builder
---

![Bodu.Globalization.Calendar](~/images/hero-calendar.svg)

## Purpose

**Bodu.Globalization.Calendar.Builder** is a fluent, chainable API for programmatically constructing notable-date rules. It bridges domain code and the serialised XML / JSON rule formats consumed by `XmlResourceNotableDateRuleProvider` / `JsonResourceNotableDateRuleProvider`, supporting both in-memory rule sets (for testing or runtime composition) and schema-valid output (for export to embedded resources).

Reach for this namespace when you need to author calendar rules in C# rather than hand-rolling XML, when you need a programmatic template-and-mutate workflow for variant rules across territories or years, or when you need to round-trip rule sets through a fluent surface before persisting them.

## Static documentation

- **[`Bodu.Globalization.Calendar` introduction](~/docs/calendar/index.md)** — how Builder fits into the broader calendar surface.
- **[`Bodu.Globalization.Calendar.Builder` guide](~/guides/calendar/notable-date-builder.md)** — end-to-end fluent walkthrough.

## Key types

- <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder> — the entry point. Created via `NotableDateDocumentBuilder.Create()`. Accumulates named notable-date entries and emits the rule set as `IReadOnlyList<NotableDateRule>`, schema-valid `XDocument` / `XML` string, schema-valid `JsonNode` / JSON string, or directly as an <xref:Bodu.Globalization.Calendar.INotableDateRuleProvider>.
- <xref:Bodu.Globalization.Calendar.Builder.NotableDateBuilder> — accumulates one or more resolution rules under a single notable-date name. Obtained via `NotableDateDocumentBuilder.AddDate(name, configure)`.
- <xref:Bodu.Globalization.Calendar.Builder.NotableDateRuleBuilder> — the fluent rule configurator. Supports every field of <xref:Bodu.Globalization.Calendar.NotableDateRule> — strategy, category, territory, year bounds, tags, observance adjustments, calendar type, comments. Exactly one resolution strategy must be set; a second strategy call throws `InvalidOperationException`.
- <xref:Bodu.Globalization.Calendar.Builder.ObservanceAdjustmentBuilder> — the fluent adjustment configurator. Supports trigger, action, day-of-week / fixed-date / ordinal conditional parameters, target rule for `ReplaceWithNamedDate`, custom-handler key + parameters, territory / calendar / year scope, priority, and `MaxAdjustmentReachDays` envelope.
- <xref:Bodu.Globalization.Calendar.InlineNotableDateRuleProvider> — in-memory `INotableDateRuleProvider` over a pre-built `IReadOnlyList<NotableDateRule>`. Produced by `NotableDateDocumentBuilder.ToProvider()` and suitable for passing directly to `NotableDateService`.

## Example

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;

INotableDateRuleProvider provider = NotableDateDocumentBuilder.Create()
    .AddDate("Christmas Day", date => date
        .AddRule(rule => rule
            .Category(NotableDateCategory.Holiday)
            .Fixed(month: 12, day: 25)
            .Territory("AU")
            .NonWorking(true)
            .AddAdjustment("weekend-roll", adj => adj
                .When(AdjustmentTrigger.IfWeekend)
                .Action(AdjustmentAction.MoveToNextMonday)
                .NonWorking(true))))
    .AddDate("Good Friday", date => date
        .AddRule(rule => rule
            .Category(NotableDateCategory.Religious)
            .OffsetFromAnchor("Easter Sunday", -2)
            .Territory("AU")))
    .ToProvider();
```

## Notes

- **One strategy per rule.** Resolution strategies are mutually exclusive — `Fixed`, `DayOfWeekInMonth`, `OffsetFromAnchor`, and `Algorithm` cannot be combined on a single rule. Use `ClearStrategy()` to reset before applying a different strategy in template-mutate workflows.
- **Adjustment keys are unique within a rule.** Each `AddAdjustment` call must supply a key that is unique within the rule's adjustment set. The key is consumed during rule inheritance / `<Use>` merging, so it must be present even for programmatic-only rules.
- **Programmatic-only fields.** `AddHandlerParameter(key, value)` and `MaxAdjustmentReachDays(int)` on `ObservanceAdjustmentBuilder` are not part of the XML / JSON schema; they survive into the in-memory rule set and the range-resolution pipeline but are stripped from `ToXDocument()` / `ToJsonNode()` output.
- **Deep clone supported.** Every builder exposes `Clone()` for template-factory patterns: build a baseline rule, clone it per territory or per variant, mutate the clone, append to the document.
- **See also:** the [Notable-date builder guide](~/guides/calendar/notable-date-builder.md), the [`NotableDateRule` reference](~/guides/calendar/rule-reference.md), and the [authoring guide](~/guides/calendar/rule-authoring.md) for the equivalent XML / JSON authoring path.
