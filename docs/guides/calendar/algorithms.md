---
title: Date calculation algorithms
---

# Date calculation algorithms

A single-date <xref:Bodu.Globalization.Calendar.NotableDateRule> finds its *nominal* date for a year through exactly one <xref:Bodu.Globalization.Calendar.Algorithms.IDateCalculationStrategy>. The loader maps each `<Strategy>` element in a rule document to a strategy implementation. Most compute a date from calendar arithmetic; `<Algorithm>` delegates to a named astronomical or ecclesiastical calculator that cannot be expressed as a formula (Easter, the equinoxes, Vesak, Diwali, Qingming, …). (A rule may alternatively declare a `<Recurrence>` that yields many dates — see [Notable-date rule strategies](strategy-reference.md#recurrence-sources) — but this guide is about the single-date algorithms.)

This guide focuses on the `<Algorithm>` strategy: the full set of built-in keys, `AlgorithmDateStrategy.IsKnownKey`, how to implement and register a custom <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm>, and how reference strategies resolve another rule's date through the strategy resolution context. For the complete catalogue of every strategy element and recurrence source, see [Notable-date rule strategies](strategy-reference.md).

For the conceptual distinction between *algorithm* and *fixed rule*, see [Core concepts — Algorithm vs. fixed rule](../../docs/calendar/concepts.md#algorithm-vs-fixed-rule). For the namespace overview, see the [Algorithms API reference](xref:Bodu.Globalization.Calendar.Algorithms).

---

## The strategy kinds

A single-date `<Rule>` carries exactly one `<Strategy>` child, mapping to a public <xref:Bodu.Globalization.Calendar.Algorithms.IDateCalculationStrategy> implementation. You rarely construct these by hand — the loader builds them from the document — but they are the vocabulary the engine resolves against. The strategy contract is a single method, `DateOnly? Calculate(int year, StrategyResolutionContext context)`, returning `null` when the rule produces no occurrence for that year.

| `<Strategy>` element | Strategy type | What it computes |
|---|---|---|
| `<Fixed>` | <xref:Bodu.Globalization.Calendar.Algorithms.FixedDateStrategy> | A specific month + day every year, optionally in a non-Gregorian calendar. |
| `<DayOfWeekInMonth>` | <xref:Bodu.Globalization.Calendar.Algorithms.DayOfWeekInMonthStrategy> | The *n*th or last weekday in a month. |
| `<RelativeWeekdayInMonth>` | <xref:Bodu.Globalization.Calendar.Algorithms.RelativeWeekdayInMonthStrategy> | A weekday positioned relative to a weekday-in-month anchor. |
| `<WeekdayNearDate>` | <xref:Bodu.Globalization.Calendar.Algorithms.WeekdayNearDateStrategy> | A weekday on / before / after / nearest a fixed reference date. |
| `<OrdinalDayOfMonth>` | <xref:Bodu.Globalization.Calendar.Algorithms.OrdinalDayOfMonthStrategy> | A signed day-of-month from the start or end of a month. |
| `<DayOfYear>` | <xref:Bodu.Globalization.Calendar.Algorithms.DayOfYearStrategy> | A signed day-of-year from 1 January or 31 December. |
| `<IsoWeekDate>` | <xref:Bodu.Globalization.Calendar.Algorithms.IsoWeekDateStrategy> | A weekday within an ISO-8601 week of an ISO week-year. |
| `<OffsetFromRule>` | <xref:Bodu.Globalization.Calendar.Algorithms.OffsetFromRuleStrategy> | A signed calendar-day offset from another rule's occurrence. |
| `<WeekdayNearRule>` | <xref:Bodu.Globalization.Calendar.Algorithms.WeekdayNearRuleStrategy> | A weekday near another rule's occurrence. |
| `<NthWeekdayFromRule>` | <xref:Bodu.Globalization.Calendar.Algorithms.NthWeekdayFromRuleStrategy> | The *n*th weekday before/after another rule's occurrence. |
| `<WorkingDayOffsetFromRule>` | <xref:Bodu.Globalization.Calendar.Algorithms.WorkingDayOffsetFromRuleStrategy> | A signed **working-day** offset from another rule's occurrence. |
| `<WorkingDayInMonth>` | <xref:Bodu.Globalization.Calendar.Algorithms.WorkingDayInMonthStrategy> | The *n*th working day of a month. |
| `<Algorithm>` | <xref:Bodu.Globalization.Calendar.Algorithms.AlgorithmDateStrategy> | Dispatch to a named algorithm key. |

The reference (`<OffsetFromRule>`, `<WeekdayNearRule>`, `<NthWeekdayFromRule>`, `<WorkingDayOffsetFromRule>`) and business-day (`<WorkingDayOffsetFromRule>`, `<WorkingDayInMonth>`) strategies resolve through the same `StrategyResolutionContext` — see [Cross-rule references and the resolution context](#cross-rule-references-and-the-resolution-context) below and [Notable-date rule strategies](strategy-reference.md) for each element's attributes and scenarios. Recurrence sources implement the sibling <xref:Bodu.Globalization.Calendar.Algorithms.IDateRecurrenceStrategy> contract (`IEnumerable<DateOnly> GetOccurrences(DateRange range, StrategyResolutionContext context)`).

### `<Fixed>` — a fixed month and day

The most common strategy: the same calendar position every year. `month` is a number 1–12 or an English month name; `day` is the day of month. An invalid combination (e.g. 29 February in a non-leap year) yields no occurrence for that year and the rule is skipped.

```xml
<NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday">
  <Rules>
    <Rule id="default">
      <Strategy><Fixed month="January" day="1" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

`<Fixed>` also expresses dates in a non-Gregorian calendar via the enclosing `<Applicability calendar="...">` (`Hijri`, `UmmAlQura`, `Hebrew`, `Persian`, `ChineseLunisolar`). Because a short Hijri month can recur twice in a single Gregorian year, <xref:Bodu.Globalization.Calendar.Algorithms.FixedDateStrategy> additionally exposes `CalculateAll`; the optional `skipLeapMonth` and `sweepCalendarYears` attributes tune lunisolar projection. See [Working with non-Gregorian calendars](non-gregorian-calendars.md).

### `<DayOfWeekInMonth>` — the *n*th weekday in a month

Driven by <xref:Bodu.Extensions.WeekOrdinal> (`First`, `Second`, `Third`, `Fourth`, `Fifth`, `Last`). `Fifth` yields no occurrence in months that lack a fifth instance; `Last` always selects the final occurrence.

```xml
<!-- US Thanksgiving — the fourth Thursday in November. -->
<Rule id="default">
  <Strategy><DayOfWeekInMonth month="11" dayOfWeek="Thursday" weekOrdinal="Fourth" /></Strategy>
</Rule>
```

### `<RelativeWeekdayInMonth>` — a weekday relative to an anchor weekday

Locates a weekday-in-month anchor (the `weekOrdinal`-th `dayOfWeek` of `month`), then steps to a `relativeDayOfWeek` positioned by `direction` (a <xref:Bodu.Globalization.Calendar.WeekdayProximity> value) from it.

```xml
<!-- US Election Day — the Tuesday after the first Monday in November. -->
<Rule id="default">
  <Strategy>
    <RelativeWeekdayInMonth month="11" dayOfWeek="Monday" weekOrdinal="First"
                            relativeDayOfWeek="Tuesday" direction="After" />
  </Strategy>
</Rule>
```

### `<WeekdayNearDate>` — a weekday near a fixed date

Driven by <xref:Bodu.Globalization.Calendar.WeekdayProximity> (`Before`, `OnOrBefore`, `Nearest`, `OnOrAfter`, `After`) relative to the `month`/`day` reference.

```xml
<!-- Victoria Day (CA) — the Monday on or before 24 May. -->
<Rule id="default">
  <Strategy><WeekdayNearDate month="5" day="24" dayOfWeek="Monday" direction="OnOrBefore" /></Strategy>
</Rule>
```

### `<OffsetFromRule>` — a signed offset from another rule

References another concept's rule via `notableDateRef` (and optional `ruleRef`) and adds a signed `offsetDays`. This is how the Easter cluster is authored: Good Friday and Easter Monday hang off Easter Sunday. The referenced rule is resolved first; references are resolved cycle-safely within the resource.

```xml
<NotableDate id="easter-sunday" displayName="Easter Sunday" category="Religious">
  <Rules>
    <Rule id="default"><Strategy><Algorithm key="western-easter" /></Strategy></Rule>
  </Rules>
</NotableDate>

<NotableDate id="good-friday" displayName="Good Friday" category="PublicHoliday">
  <Rules>
    <Rule id="default">
      <Strategy><OffsetFromRule notableDateRef="easter-sunday" ruleRef="default" offsetDays="-2" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>

<NotableDate id="easter-monday" displayName="Easter Monday" category="PublicHoliday">
  <Rules>
    <Rule id="default">
      <Strategy><OffsetFromRule notableDateRef="easter-sunday" ruleRef="default" offsetDays="1" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

The referenced algorithm runs once per year per resolution, regardless of how many offset rules consume it. See [Rule references](../../docs/calendar/concepts.md#rule-references-offset-from-rule) and the [resolution context](#cross-rule-references-and-the-resolution-context) below.

### `<Algorithm>` — dispatch to a named calculator

For dates that cannot be expressed as calendar arithmetic. The `key` attribute names a built-in calculator (below) or a custom <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> registered in a <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry>.

```xml
<Rule id="default">
  <Strategy><Algorithm key="western-easter" /></Strategy>
</Rule>
```

### Constructing a strategy directly

Each strategy is a public, sealed type with a constructor mirroring its `<Strategy>` attributes, so you can evaluate one outside a document — useful for unit tests or ad-hoc calculation. `Calculate(year, context)` takes a <xref:Bodu.Globalization.Calendar.Algorithms.StrategyResolutionContext>; pass one built over the resource the strategy resolves against (or, for the year-only strategies that never follow a reference, any context):

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

var context = new StrategyResolutionContext(resource);

// Fourth Thursday in November 2026 (US Thanksgiving).
var thanksgiving = new DayOfWeekInMonthStrategy(11, DayOfWeek.Thursday, WeekOrdinal.Fourth);
DateOnly? date = thanksgiving.Calculate(2026, context);   // → 2026-11-26

// Western Easter Sunday 2026, resolved through the key dispatcher.
var easter = new AlgorithmDateStrategy(AlgorithmDateStrategy.WesternEasterKey);
DateOnly? easter2026 = easter.Calculate(2026, context);   // → 2026-04-05
```

<xref:Bodu.Globalization.Calendar.Algorithms.FixedDateStrategy> additionally exposes `IReadOnlyList<DateOnly> CalculateAll(int year, StrategyResolutionContext context)`, which returns *every* occurrence in the Gregorian year — the surface that surfaces a short Hijri month landing twice. The single-date `Calculate` returns the chronologically first. See [Working with non-Gregorian calendars](non-gregorian-calendars.md).

---

## Built-in algorithm keys

<xref:Bodu.Globalization.Calendar.Algorithms.AlgorithmDateStrategy> resolves a string key to a bundled astronomical or gazetted calculator. The calculators behind these keys are an internal implementation detail reached only through the key — there is no public class to instantiate; reference the key from a rule document instead.

The two Easter keys are also exposed as constants on <xref:Bodu.Globalization.Calendar.Algorithms.AlgorithmDateStrategy>: `WesternEasterKey` (`"western-easter"`) and `OrthodoxEasterKey` (`"orthodox-easter"`).

| Key | Date it computes |
|---|---|
| `western-easter` | Easter Sunday by the Gregorian computus. |
| `orthodox-easter` | Easter Sunday by the Julian computus, projected onto the Gregorian calendar. |
| `vernal-equinox` | The astronomical March (vernal) equinox. |
| `autumnal-equinox` | The astronomical September (autumnal) equinox. |
| `jp-vernal-equinox` | Japan's gazetted Vernal Equinox Day (Shunbun no Hi). |
| `jp-autumnal-equinox` | Japan's gazetted Autumnal Equinox Day (Shūbun no Hi). |
| `qingming` | Qingming (Tomb-Sweeping Day) — the solar term 15° after the March equinox, typically 4–5 April. |
| `vesak` | Vesak (Buddha's birthday) — the full-moon observance in the Theravāda tradition. |
| `asalha-puja` | Asalha Puja (Dhamma Day) — the full moon of the eighth lunar month. |
| `losar` | Losar (Tibetan New Year) — the Tibetan lunisolar new year. |
| `matariki` | Matariki — the Māori new year, set by the gazetted public-holiday calendar. |
| `ram-navami` | Ram Navami — the Hindu festival of Rama's birth. |
| `raksha-bandhan` | Raksha Bandhan. |
| `janmashtami` | Krishna Janmashtami. |
| `ganesh-chaturthi` | Ganesh Chaturthi. |
| `navaratri` | The first day of Navaratri. |
| `dussehra` | Dussehra (Vijayadashami). |
| `karva-chauth` | Karva Chauth. |
| `diwali` | Diwali (Deepavali). |
| `vasant-panchami` | Vasant Panchami. |
| `maha-shivaratri` | Maha Shivaratri. |
| `holi` | Holi. |
| `maun-agiyaras` | Maun Agiyaras — the Jain observance on Margashirsha shukla 11. |

The equinox and Qingming keys are computed astronomically and resolved in a specific time zone: `vernal-equinox` / `autumnal-equinox` use UTC, `jp-vernal-equinox` / `jp-autumnal-equinox` use Japan Standard Time (UTC+9), and `qingming` uses China Standard Time (UTC+8). The Hindu-festival keys are computed against the Hindu lunisolar *panchanga* by the engine's internal `HinduLunarCalculator`; `vesak` and `asalha-puja` resolve full-moon dates; `losar` and `matariki` come from gazetted-date tables.

`AlgorithmDateStrategy.IsKnownKey(key)` reports whether a key is built in:

```csharp
using Bodu.Globalization.Calendar.Algorithms;

bool builtIn = AlgorithmDateStrategy.IsKnownKey("western-easter");          // true
bool custom  = AlgorithmDateStrategy.IsKnownKey("pi-day");                  // false — needs a registry
```

A key that `IsKnownKey` does not recognise is resolved against the custom <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry> supplied at load and construction time. An unregistered, unknown key surfaces as an error-severity validation diagnostic — and a `NotableDateValidationException` — when the document is loaded.

---

## Implementing a custom algorithm

Implement <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> to add a calculator the built-in set does not cover. The single method `Calculate` receives the target year and returns a `DateOnly?` — return `null` for years the algorithm does not support.

```csharp
using System;
using Bodu.Globalization.Calendar.Algorithms;

// Pi Day — 14 March every year.
public sealed class PiDayAlgorithm : INotableDateAlgorithm
{
    public DateOnly? Calculate(int year) => new DateOnly(year, 3, 14);
}
```

A more realistic example computes a weekday-relative date:

```csharp
using System;
using Bodu.Globalization.Calendar.Algorithms;

// Mother's Day (US) — the second Sunday in May.
public sealed class MothersDayAlgorithm : INotableDateAlgorithm
{
    public DateOnly? Calculate(int year)
    {
        DateOnly firstOfMay = new DateOnly(year, 5, 1);
        int daysToFirstSunday = ((int)DayOfWeek.Sunday - (int)firstOfMay.DayOfWeek + 7) % 7;
        return firstOfMay.AddDays(daysToFirstSunday + 7);
    }
}
```

### Registering the algorithm

Register each instance under the key the rule document references, using the chainable <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry>. The same registry must be passed to **both** the loader (so the key passes validation) and the <xref:Bodu.Globalization.Calendar.NotableDateService> constructor (so it resolves at query time):

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

INotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
    .Register("pi-day",      new PiDayAlgorithm())
    .Register("mothers-day", new MothersDayAlgorithm());

// Pass the registry to the loader so "pi-day"/"mothers-day" are whitelisted during validation …
NotableDateResource resource = NotableDateResourceLoader.Load(xml, _ => null, registry);

// … and to the service so AlgorithmDateStrategy can resolve them at query time.
NotableDateService service = new NotableDateService(resource, registry);
```

The custom registry implements <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithmRegistry> (`Contains(key)` and `TryGet(key, out algorithm)`), the same lookup surface the engine consults at resolution time. The rule document references the key exactly like a built-in one:

```xml
<NotableDate id="pi-day" displayName="Pi Day" category="Observance">
  <Rules>
    <Rule id="default"><Strategy><Algorithm key="pi-day" /></Strategy></Rule>
  </Rules>
</NotableDate>
```

Resolve as usual — by-year resolution is the `service.Resolve(year, territory)` extension:

```csharp
using Bodu.Globalization.Calendar;

IReadOnlyList<NotableDate> dates = service.Resolve(2026, "US");

foreach (NotableDate date in dates)
    Console.WriteLine($"{date.Date:d MMM yyyy}  {date.DisplayName}");
// → 14 Mar 2026  Pi Day, 10 May 2026  Mother's Day, …
```

> **Packaging algorithms for reuse.** When a custom calculator ships in its own assembly, expose it as an <xref:Bodu.Globalization.Calendar.Plugins.INotableDateAlgorithmPlugin> and load it through the trust-gated `NotableDatePluginLoader` rather than referencing the type directly. See [Building and extending the service — Plugin system](building-the-service.md#plugin-system).

---

## Cross-rule references and the resolution context

`<OffsetFromRule>` and custom strategies that need another rule's date receive a <xref:Bodu.Globalization.Calendar.Algorithms.StrategyResolutionContext>. It carries the custom algorithm registry (`.Algorithms`) and resolves a referenced rule's occurrence for a year, cycle-safely:

```csharp
using Bodu.Globalization.Calendar.Algorithms;

// context is supplied by the engine to IDateCalculationStrategy.Calculate(year, context).
DateOnly? easter = context.ResolveReference("easter-sunday", "default", 2026);
// → the resolved Easter Sunday date for 2026, or null when the reference produces no occurrence.
```

`ResolveReference(notableDateRef, ruleRef, year)` is the same machinery <xref:Bodu.Globalization.Calendar.Algorithms.OffsetFromRuleStrategy> uses; a self-referential or mutually-referential chain is detected and reported rather than looping. When the referenced rule produces no occurrence for the year, `ResolveReference` returns `null` and the offset rule produces no occurrence in turn.

A `StrategyResolutionContext` can also be constructed directly over a resource (and, optionally, an algorithm registry) when you want to evaluate a reference outside a running query:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

var context = new StrategyResolutionContext(resource, registry);
DateOnly? easterSunday2026 = context.ResolveReference("easter-sunday", "default", 2026);
```

---

## Loading bundled algorithm-backed concepts

Most algorithm-backed dates are already authored in the bundled common catalogues and data packs, so you rarely register the built-in keys yourself. Importing `christian-western` brings in Easter and its offset cluster; `global-buddhist` brings in Vesak and Asalha Puja; `global-hindu` brings in the Hindu-festival keys. Pass <xref:Bodu.Globalization.Calendar.CommonNotableDateResources>'s resolver so `<Imports>` resolve:

```csharp
using Bodu.Globalization.Calendar;

NotableDateResource resource = NotableDateResourceLoader.Load(
    myDocumentXml, CommonNotableDateResources.Resolver);
NotableDateService service = new NotableDateService(resource);
```

See [Authoring notable date rules](rule-authoring.md) for imports and overrides, and the [Calendar data packs](data-packs.md) for the ready-made regional resources.

---

## Where to go next

- [Core concepts](../../docs/calendar/concepts.md) — vocabulary used across this guide.
- [Using NotableDateService](notable-dates.md) — loading resources, querying by date / range / year, and filtering.
- [Authoring notable date rules](rule-authoring.md) — documents, imports, and overrides.
- [NotableDateRule and adjustment-policy reference](rule-reference.md) — the per-element strategy contracts.
- [The resolution pipeline](resolution-pipeline.md) — resolution ordering and reference cycle detection.
- [Working with non-Gregorian calendars](non-gregorian-calendars.md) — `<Fixed>` dates in Hijri / Hebrew / Persian / Chinese lunisolar calendars.
- [Algorithms API reference](xref:Bodu.Globalization.Calendar.Algorithms) — full type reference.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
