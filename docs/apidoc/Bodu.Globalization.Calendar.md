---
uid: Bodu.Globalization.Calendar
---

![Bodu.Globalization.Calendar](~/images/hero-calendar.svg)

## Purpose

**Bodu.Globalization.Calendar** resolves culturally and algorithmically significant dates — holidays, observances, and recurring notable dates — across a mixture of definition styles: fixed dates, rule-based recurrences (e.g. *fourth Thursday in November*), offsets from other notable dates, and dynamic calculators (e.g. the Gregorian Computus for Easter).

Reach for this library when a `DateTime.DayOfWeek` check is not enough: when you need Easter Sunday in year *N*, or when a business-day rule shifts a fixed holiday because it fell on a weekend, or when you need a cached, culture-aware calendar of notable dates for a range of years.

## Key types

- <xref:Bodu.Globalization.Calendar.NotableDateService> — the main entry point. Manages a catalogue of notable-date definitions and materialises <xref:Bodu.Globalization.Calendar.NotableDate> instances for a year or range, caching results and generating beyond the initial bounds on demand.
- <xref:Bodu.Globalization.Calendar.NotableDateResolver> — resolves the base date of a definition according to its type (fixed, rule-based, offset-based, or dynamic) and its dependencies on other definitions.
- <xref:Bodu.Globalization.Calendar.NotableDate> — the materialised result: the calculated occurrence plus metadata (name, <xref:Bodu.Globalization.Calendar.NotableDateKind>, cultural applicability, and original pre-adjustment date if a rollover rule moved it).
- <xref:Bodu.Globalization.Calendar.NotableDateKind> — categorisation: Holiday, Observance, Remembrance, Cultural, Christian, Other, or None.
- <xref:Bodu.Globalization.Calendar.INotableDateCalculator> — the contract for year-keyed date computation, implemented by the calculators below and by any custom calculator you add.

**Calculators** (in <xref:Bodu.Globalization.Calendar.Calculators>)

- <xref:Bodu.Globalization.Calendar.Calculators.EasterSundayNotableDateCalculator> — Gregorian Computus from 1583 onwards; falls back to the Julian algorithm for earlier years. Results are cached per year.
- <xref:Bodu.Globalization.Calendar.Calculators.LunarNewYearNotableDateCalculator> — Lunar New Year from lunar-calendar computation.

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
- **Culture and adjustment.** A `NotableDate` tracks both its original calculated date and its adjusted date — so a rule like "if a fixed holiday falls on a Saturday, observe it on the preceding Friday" is applied transparently while still preserving the original for audit and display.
- **Target frameworks.** This library multi-targets `net6.0`, `net7.0`, and `net8.0` — the widest reach of any library in the Bodu solution.
- **Extensibility.** Implement <xref:Bodu.Globalization.Calendar.INotableDateCalculator> to add your own dynamic calculator (e.g. Orthodox Easter, Rosh Hashanah, Diwali) and register it with <xref:Bodu.Globalization.Calendar.NotableDateService> alongside the built-in fixed, rule-based, and offset-based definitions.
