---
uid: Bodu.Globalization.Calendar
---

![Bodu.Globalization.Calendar](~/images/hero-calendar.svg)

## Purpose

**Bodu.Globalization.Calendar** resolves culturally and algorithmically significant dates — holidays, observances, and recurring notable dates — across a mixture of definition styles: fixed dates, rule-based recurrences (e.g. *fourth Thursday in November*), offsets from other notable dates, and dynamic calculators (e.g. the Gregorian Computus for Easter).

Reach for this library when a `DateTime.DayOfWeek` check is not enough: when you need Easter Sunday in year *N*, when a business-day rule shifts a fixed holiday because it fell on a weekend, or when you need a cached, culture-aware calendar of notable dates for a range of years driven from an XML rule source.

## Key types

**Entry points and results**

- <xref:Bodu.Globalization.Calendar.NotableDateService> — the main entry point (and sole <xref:Bodu.Globalization.Calendar.INotableDateService> implementation). Composes a rule provider, calculator registry, adjustment-handler registry, and collision resolver to materialise <xref:Bodu.Globalization.Calendar.NotableDate> instances for a year or range, with internal caching.
- <xref:Bodu.Globalization.Calendar.NotableDate> — the materialised result record: the resolved occurrence plus metadata (name, <xref:Bodu.Globalization.Calendar.NotableDateCategory>, cultural applicability via <xref:Bodu.Globalization.Calendar.TerritoryCode>, and the original pre-adjustment date if a rollover rule moved it).
- <xref:Bodu.Globalization.Calendar.NotableDateCategory> — categorisation: Holiday, Observance, Remembrance, Cultural, Christian, Other, or None.

**Rules and resolution**

- <xref:Bodu.Globalization.Calendar.NotableDateRule> — an immutable rule record describing how a notable date is defined (fixed, rule-based, offset-based, or delegated to a named calculator).
- <xref:Bodu.Globalization.Calendar.NotableDateRuleResolver> — resolves the base date of a rule according to its type and its dependencies on other rules.
- <xref:Bodu.Globalization.Calendar.NotableDateRuleParser> — parses rule expressions from the XML source into strongly-typed <xref:Bodu.Globalization.Calendar.NotableDateRule> instances.
- <xref:Bodu.Globalization.Calendar.INotableDateRuleProvider>, <xref:Bodu.Globalization.Calendar.INotableDateRuleOverrideProvider>, <xref:Bodu.Globalization.Calendar.XmlResourceNotableDateRuleProvider> — plug-points for the rule source (built-in XML, overlays, custom).
- <xref:Bodu.Globalization.Calendar.INotableDateProvider>, <xref:Bodu.Globalization.Calendar.INotableDateNameLocalizer> — extension surfaces for custom date sources and culture-specific naming.
- <xref:Bodu.Globalization.Calendar.INotableDateCollisionResolver>, <xref:Bodu.Globalization.Calendar.DefaultNotableDateCollisionResolver> — decide what happens when two rules resolve to the same date.

**Dynamic calculators**

- <xref:Bodu.Globalization.Calendar.INotableDateCalculator>, <xref:Bodu.Globalization.Calendar.INotableDateCalculatorRegistry>, <xref:Bodu.Globalization.Calendar.NotableDateCalculatorRegistry> — the contract and registry for year-keyed date computation.
- <xref:Bodu.Globalization.Calendar.Calculators.EasterSundayNotableDateCalculator> — Gregorian Computus from 1583 onwards; falls back to the Julian algorithm for earlier years. Results are cached per year.
- <xref:Bodu.Globalization.Calendar.Calculators.LunarNewYearNotableDateCalculator> — Lunar New Year from lunar-calendar computation.

**Adjustment pipeline**

- <xref:Bodu.Globalization.Calendar.NotableDateAdjuster>, <xref:Bodu.Globalization.Calendar.IAdjustmentHandler>, <xref:Bodu.Globalization.Calendar.IAdjustmentHandlerRegistry>, <xref:Bodu.Globalization.Calendar.AdjustmentHandlerRegistry> — apply observance rules (e.g. *if a fixed holiday falls on a Saturday, observe it on the preceding Friday*) after the base date is resolved.
- <xref:Bodu.Globalization.Calendar.AdjustmentTrigger>, <xref:Bodu.Globalization.Calendar.AdjustmentAction>, <xref:Bodu.Globalization.Calendar.AdjustmentReason>, <xref:Bodu.Globalization.Calendar.ObservanceAdjustment> — the adjustment-rule vocabulary.

## Example

```csharp
using Bodu.Globalization.Calendar.Calculators;

var easter = new EasterSundayNotableDateCalculator();
DateTime easter2026 = easter.Calculate(2026); // 2026-04-05

// Good Friday is always two days before Easter.
DateTime goodFriday2026 = easter2026.AddDays(-2);
```

## Notes

- **Thread safety.** Calculators cache their results per year internally and are **safe for concurrent reads** after any first-compute. A `NotableDateService` built with a stable set of definitions may be shared across requests.
- **Culture and adjustment.** A <xref:Bodu.Globalization.Calendar.NotableDate> tracks both its original calculated date and its adjusted date — so a rule like "if a fixed holiday falls on a Saturday, observe it on the preceding Friday" is applied transparently while still preserving the original for audit and display.
- **Target framework.** `net8.0`.
- **Extensibility.** Implement <xref:Bodu.Globalization.Calendar.INotableDateCalculator> to add your own dynamic calculator (e.g. Orthodox Easter, Rosh Hashanah, Diwali) and register it with <xref:Bodu.Globalization.Calendar.NotableDateService> alongside the built-in fixed, rule-based, and offset-based definitions — or plug a custom <xref:Bodu.Globalization.Calendar.INotableDateRuleProvider> to source rules from somewhere other than the embedded XML.
