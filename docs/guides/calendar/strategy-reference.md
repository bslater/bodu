---
title: Notable-date rule strategies
---

# Notable-date rule strategies

Every <xref:Bodu.Globalization.Calendar.NotableDateRule> answers three independent questions: **when** does the occurrence start, **how long** does it last, and **how** is it observed. This page is the catalogue for the first two — the occurrence source (a single-date **strategy** or a **recurrence**) and the **duration** (a fixed day count or a calculated end date). Observed-date shifting is a separate concern handled by [Observance adjustment rules](adjustment-rules.md).

A rule carries **exactly one** occurrence source:

- a `<Strategy>` — one of the single-occurrence strategies below, yielding at most one date per year; or
- a `<Recurrence>` — one of the recurrence strategies, yielding zero, one, or many dates within a window.

It may additionally carry a `<Duration>` giving the occurrence a span. For the surrounding document model — concepts, applicability, imports, overrides — read [Authoring notable date rules](rule-authoring.md); for every attribute of the surrounding elements, [NotableDateRule and adjustment-policy reference](rule-reference.md).

---

## Choosing a strategy

Match how the date is *defined* to the strategy that expresses it directly. Prefer the simplest kind; reach for a reference-based or business-day strategy only when the date genuinely depends on another rule or on working days.

| If the date is defined as… | Use |
|---|---|
| A fixed month + day | [`<Fixed>`](#fixed--a-fixed-month-and-day) |
| A position from the start or end of a month | [`<OrdinalDayOfMonth>`](#ordinaldayofmonth--a-signed-day-of-month) |
| A position from the start or end of a year | [`<DayOfYear>`](#dayofyear--a-signed-day-of-year) |
| A weekday within an ISO-8601 week | [`<IsoWeekDate>`](#isoweekdate--a-weekday-in-an-iso-week) |
| The *n*th or last weekday of a month | [`<DayOfWeekInMonth>`](#weekday-in-month) |
| A weekday near a fixed date | [`<WeekdayNearDate>`](#weekday-in-month) |
| A weekday relative to a weekday-in-month anchor | [`<RelativeWeekdayInMonth>`](#weekday-in-month) |
| A fixed day offset from another rule | [`<OffsetFromRule>`](#offsetfromrule--a-signed-offset-from-another-rule) |
| A weekday near another rule's date | [`<WeekdayNearRule>`](#weekdaynearrule--a-weekday-near-another-rule) |
| The *n*th weekday before/after another rule | [`<NthWeekdayFromRule>`](#nthweekdayfromrule--the-nth-weekday-from-another-rule) |
| A count of **working** days from another rule | [`<WorkingDayOffsetFromRule>`](#workingdayoffsetfromrule--a-working-day-offset-from-another-rule) |
| The *n*th working day of a month | [`<WorkingDayInMonth>`](#workingdayinmonth--the-nth-working-day-of-a-month) |
| An astronomical / ecclesiastical date | [`<Algorithm>`](#algorithm--a-named-calculator) |
| A repeating cadence (every *n* days / weeks / months) | a [recurrence source](#recurrence-sources) |

> [!NOTE]
> Yearly periodicity ("every second year") is **not** a strategy — it is `everyYears` / `anchorYear` on `<Applicability>`. See [Year periodicity](rule-authoring.md#year-periodicity). Observed-date movement is **not** a strategy either — model it with an [adjustment policy](adjustment-rules.md).

---

## Single-occurrence strategies

Each maps to a public <xref:Bodu.Globalization.Calendar.Algorithms.IDateCalculationStrategy>; the contract is `DateOnly? Calculate(int year, StrategyResolutionContext context)`, returning `null` when the rule produces no occurrence for the year. Out-of-range and overflowing dates return `null` rather than throwing.

### Fixed and positional

The first six kinds pin a date by calendar position, with no dependency on any other rule.

#### `<Fixed>` — a fixed month and day

The same calendar position every year (see [reference](rule-reference.md#fixed--a-fixed-month-and-day) for `skipLeapMonth` / `sweepCalendarYears` and non-Gregorian calendars).

```xml
<Strategy><Fixed month="December" day="25" /></Strategy>
```

#### `<OrdinalDayOfMonth>` — a signed day-of-month

A day by its ordinal position from the **start** (positive) or **end** (negative) of a Gregorian month — the natural way to express "the last day of February" without a leap-year special case.

| Attribute | Required | Type | Description |
|---|---|---|---|
| `month` | Yes | string | Month number `1`–`12` or an English month name. |
| `ordinal` | Yes | int | Non-zero, `-31`…`31`. `1` = first day, `-1` = last day, `-2` = second-last. |

```xml
<!-- Last day of February (28 or 29). -->
<Strategy><OrdinalDayOfMonth month="February" ordinal="-1" /></Strategy>
```

A positive ordinal beyond the month's length (e.g. `31` in April) yields no occurrence; the result always stays within the named month. Maps to <xref:Bodu.Globalization.Calendar.Algorithms.OrdinalDayOfMonthStrategy>.

#### `<DayOfYear>` — a signed day-of-year

A date by its ordinal position from **1 January** (positive) or **31 December** (negative).

| Attribute | Required | Type | Description |
|---|---|---|---|
| `ordinal` | Yes | int | Non-zero, `-366`…`366`. `1` = 1 January, `-1` = 31 December. |

```xml
<!-- Programmers' Day — the 256th day of the year. -->
<Strategy><DayOfYear ordinal="256" /></Strategy>
```

`366` exists only in a leap year; a positive ordinal beyond the year's length yields no occurrence. Maps to <xref:Bodu.Globalization.Calendar.Algorithms.DayOfYearStrategy>.

#### `<IsoWeekDate>` — a weekday in an ISO week

A weekday within an ISO-8601 week of an ISO **week-year**. The `year` passed to the rule is interpreted as the ISO week-year, so the resolved date can fall in the previous or following Gregorian year.

| Attribute | Required | Type | Description |
|---|---|---|---|
| `week` | Yes | int | ISO week `1`–`53`. |
| `dayOfWeek` | Yes | day of week | The weekday within the ISO week. |

```xml
<!-- Monday of ISO week 1. -->
<Strategy><IsoWeekDate week="1" dayOfWeek="Monday" /></Strategy>
```

Week 53 is valid only for ISO years that contain 53 weeks; otherwise the rule produces no occurrence. Maps to <xref:Bodu.Globalization.Calendar.Algorithms.IsoWeekDateStrategy>.

### Weekday-in-month

Three kinds resolve a weekday by its position in a month (see the [reference](rule-reference.md#strategy-elements) for full attribute tables).

- `<DayOfWeekInMonth>` — the *n*th or last weekday of a month (fourth Thursday in November).
- `<WeekdayNearDate>` — a weekday on / before / after / nearest a fixed reference date (Monday on or before 24 May).
- `<RelativeWeekdayInMonth>` — a weekday positioned relative to a weekday-in-month anchor (the Tuesday after the first Monday in November).

```xml
<Strategy><DayOfWeekInMonth month="11" dayOfWeek="Thursday" weekOrdinal="Fourth" /></Strategy>
<Strategy><WeekdayNearDate month="5" day="24" dayOfWeek="Monday" direction="OnOrBefore" /></Strategy>
<Strategy><RelativeWeekdayInMonth month="11" dayOfWeek="Monday" weekOrdinal="First"
                                  relativeDayOfWeek="Tuesday" direction="After" /></Strategy>
```

### Reference-based

These derive a date from **another rule's** occurrence, resolved cycle-safely within the resource. Each takes `notableDateRef` (the referenced concept id) and an optional `ruleRef`; all but `<OffsetFromRule>` also accept `referenceYearOffset`, a signed year offset applied to the reference (so a December rule can anchor to a reference in the following year). A reference that is missing, ambiguous, circular, or itself a **recurrence** is reported as a load-time diagnostic.

#### `<OffsetFromRule>` — a signed offset from another rule

A fixed **calendar-day** offset (see [reference](rule-reference.md#offsetfromrule--a-signed-offset-from-another-rule)). This is how Good Friday hangs off Easter Sunday.

```xml
<Strategy><OffsetFromRule notableDateRef="easter-sunday" ruleRef="default" offsetDays="-2" /></Strategy>
```

#### `<WeekdayNearRule>` — a weekday near another rule

The dynamic-reference twin of `<WeekdayNearDate>`: seek a weekday on / before / after / nearest another rule's date rather than a fixed month and day.

| Attribute | Required | Type | Description |
|---|---|---|---|
| `notableDateRef` | Yes | identifier | The referenced concept id. |
| `ruleRef` | No | identifier | A specific rule within that concept. |
| `dayOfWeek` | Yes | day of week | The target weekday. |
| `direction` | Yes | proximity | <xref:Bodu.Globalization.Calendar.WeekdayProximity>: `Before`, `OnOrBefore`, `Nearest`, `OnOrAfter`, `After`. |
| `referenceYearOffset` | No | int | Signed year offset applied to the reference (default `0`). |

```xml
<!-- The first Monday after Easter Sunday. -->
<Strategy><WeekdayNearRule notableDateRef="easter-sunday" dayOfWeek="Monday" direction="After" /></Strategy>
```

Maps to <xref:Bodu.Globalization.Calendar.Algorithms.WeekdayNearRuleStrategy>.

#### `<NthWeekdayFromRule>` — the *n*th weekday from another rule

The *n*th matching weekday strictly **after** (positive ordinal) or **before** (negative ordinal) another rule's date. The reference date itself is never counted.

| Attribute | Required | Type | Description |
|---|---|---|---|
| `notableDateRef` | Yes | identifier | The referenced concept id. |
| `ruleRef` | No | identifier | A specific rule within that concept. |
| `dayOfWeek` | Yes | day of week | The target weekday. |
| `ordinal` | Yes | int | Non-zero. Positive counts strictly after, negative strictly before. |
| `referenceYearOffset` | No | int | Signed year offset applied to the reference (default `0`). |

```xml
<!-- The second Monday after a referenced festival. -->
<Strategy><NthWeekdayFromRule notableDateRef="festival" dayOfWeek="Monday" ordinal="2" /></Strategy>
```

Maps to <xref:Bodu.Globalization.Calendar.Algorithms.NthWeekdayFromRuleStrategy>.

#### `<WorkingDayOffsetFromRule>` — a working-day offset from another rule

A count of **working days** before or after another rule's date. Unlike `<OffsetFromRule>` (which counts calendar days), this skips rest days and applicable non-working occurrences — so "one working day before Boxing Day" is correct even when the preceding calendar day is Christmas Day.

| Attribute | Required | Type | Description |
|---|---|---|---|
| `notableDateRef` | Yes | identifier | The referenced concept id. |
| `ruleRef` | No | identifier | A specific rule within that concept. |
| `offsetWorkingDays` | Yes | int | Signed working-day count. The reference is the origin and is not counted; `0` returns it unchanged. |
| `referenceYearOffset` | No | int | Signed year offset applied to the reference (default `0`). |

```xml
<!-- The last working day before Boxing Day. -->
<Strategy><WorkingDayOffsetFromRule notableDateRef="boxing-day" offsetWorkingDays="-1" /></Strategy>
```

The working-day set is computed deterministically and independently of the order concepts are declared — see [Business-day strategies](#business-day-strategies). Maps to <xref:Bodu.Globalization.Calendar.Algorithms.WorkingDayOffsetFromRuleStrategy>.

### Business-day strategies

`<WorkingDayOffsetFromRule>` (above) and `<WorkingDayInMonth>` resolve against **working days** — days that are neither a rest day (outside the resource's working week, set on `<ResolutionPolicy>`) nor claimed by an applicable non-working notable-date occurrence. That working-day view is computed deterministically and is **independent of resource-declaration order**, so a business-day rule resolves the same regardless of where the holidays it depends on appear in the document. It is also cycle-safe: a business-day rule consulted while the working-day set is being built falls back to rest-days-only rather than recursing.

#### `<WorkingDayInMonth>` — the *n*th working day of a month

The *n*th working day from the **start** (positive) or **end** (negative) of a month.

| Attribute | Required | Type | Description |
|---|---|---|---|
| `month` | Yes | string | Month number `1`–`12` or an English month name. |
| `ordinal` | Yes | int | Non-zero. `1` = first working day, `-1` = last working day. |

```xml
<!-- The first working day of January (skips New Year's Day and the weekend). -->
<Strategy><WorkingDayInMonth month="January" ordinal="1" /></Strategy>
```

When the requested ordinal does not exist within the month the rule produces no occurrence rather than spilling into an adjacent month. Maps to <xref:Bodu.Globalization.Calendar.Algorithms.WorkingDayInMonthStrategy>. See also [Working-day arithmetic](working-days.md).

### Algorithm

#### `<Algorithm>` — a named calculator

Delegates to a built-in astronomical / ecclesiastical calculator (Easter, the equinoxes, Vesak, …) or a registered custom one (see [reference](rule-reference.md#algorithm--dispatch-to-a-named-calculator) and [Date calculation algorithms](algorithms.md)).

```xml
<Strategy><Algorithm key="western-easter" /></Strategy>
```

---

## Recurrence sources

A `<Recurrence>` replaces `<Strategy>` when a rule repeats on a sub-yearly cadence — every Monday, every 14 days, the 15th of every month, the last Friday of every month. It maps to a <xref:Bodu.Globalization.Calendar.Algorithms.IDateRecurrenceStrategy> and generates **many** base occurrences within the requested window. Every generated occurrence is a normal candidate: it independently receives the rule's category, non-working flag, duration/span, adjustment policy, and collision handling.

Results are deterministic, chronological, de-duplicated, and **query-window invariant** — whether a given date is an occurrence never depends on the size or start of the query range, because occurrences are computed from the anchor, not from the range. Recurrence never consults `CultureInfo.CurrentCulture`.

**Anchors.** An anchor is required only when the interval creates a phase that cannot otherwise be determined:

- every day / week / month (interval `1`) needs **no** anchor — every unit participates;
- every *n* days, every *n* weeks, or every *n* months (interval `> 1`) **requires** an `anchorDate`.

### `<DailyInterval>` — every *n* days

| Attribute | Required | Type | Description |
|---|---|---|---|
| `anchorDate` | Yes | date | `yyyy-MM-dd`; occurrence zero and the phase of the series. |
| `intervalDays` | No | int ≥ 1 | Days between occurrences (default `1`). |

```xml
<!-- Every 14 days from 1 January 2026 (fortnightly). -->
<Recurrence><DailyInterval anchorDate="2026-01-01" intervalDays="14" /></Recurrence>
```

Maps to <xref:Bodu.Globalization.Calendar.Algorithms.DailyIntervalRecurrenceStrategy>.

### `<Weekly>` — selected weekdays every *n* weeks

| Attribute / child | Required | Type | Description |
|---|---|---|---|
| `<Day dayOfWeek="…" />` | Yes (1+) | day of week | One per selected weekday; duplicates are rejected. |
| `intervalWeeks` | No | int ≥ 1 | Weeks between occurrences of each weekday (default `1`). |
| `anchorDate` | No* | date | Required when `intervalWeeks > 1`; phases each weekday series. |

```xml
<!-- Every Monday and Friday. -->
<Recurrence>
  <Weekly intervalWeeks="1">
    <Day dayOfWeek="Monday" />
    <Day dayOfWeek="Friday" />
  </Weekly>
</Recurrence>

<!-- Every second Tuesday, phased from 6 January 2026. -->
<Recurrence>
  <Weekly intervalWeeks="2" anchorDate="2026-01-06">
    <Day dayOfWeek="Tuesday" />
  </Weekly>
</Recurrence>
```

Occurrences are emitted in chronological order regardless of the order the `<Day>` elements are declared. Maps to <xref:Bodu.Globalization.Calendar.Algorithms.WeeklyRecurrenceStrategy>.

### `<MonthlyDay>` — a day-of-month every *n* months

| Attribute | Required | Type | Description |
|---|---|---|---|
| `dayOfMonth` | Yes | int 1–31 | The calendar day. |
| `intervalMonths` | No | int ≥ 1 | Months between occurrences (default `1`; `3` = quarterly, `6` = semi-annual). |
| `anchorDate` | No* | date | Required when `intervalMonths > 1`; its year/month is month zero. |
| `invalidDayBehavior` | No | enum | `Skip` (default) or `UseLastDayOfMonth` when a month lacks the day. |

```xml
<!-- The 31st of every month, clamped to the last day of shorter months. -->
<Recurrence><MonthlyDay dayOfMonth="31" invalidDayBehavior="UseLastDayOfMonth" /></Recurrence>
```

Month iteration uses a stable month anchor, so day-31 still evaluates March as the 31st even after February clamps — there is no month-to-month drift. Maps to <xref:Bodu.Globalization.Calendar.Algorithms.MonthlyDayRecurrenceStrategy> / <xref:Bodu.Globalization.Calendar.Algorithms.InvalidDayOfMonthBehavior>.

### `<MonthlyWeekday>` — an ordinal weekday every *n* months

| Attribute | Required | Type | Description |
|---|---|---|---|
| `dayOfWeek` | Yes | day of week | The target weekday. |
| `weekOrdinal` | Yes | ordinal | `First`…`Fifth`, `Last`. |
| `intervalMonths` | No | int ≥ 1 | Months between occurrences (default `1`). |
| `anchorDate` | No* | date | Required when `intervalMonths > 1`; its year/month is month zero. |

```xml
<!-- The last Friday of every month. -->
<Recurrence><MonthlyWeekday dayOfWeek="Friday" weekOrdinal="Last" /></Recurrence>
```

`Last` is the final matching weekday; a `Fifth` that a month lacks simply skips that month. Maps to <xref:Bodu.Globalization.Calendar.Algorithms.MonthlyWeekdayRecurrenceStrategy>.

---

## Durations: fixed or calculated

A duration gives an occurrence a **span**. It is independent of the occurrence source — a recurrence occurrence gets its own span exactly like a single-date one — and the resolved <xref:Bodu.Globalization.Calendar.NotableDate> always exposes a concrete `Date`, `DurationDays`, and inclusive `EndDate`.

**Fixed** — the `durationDays` attribute on the rule (or `defaultDurationDays` on the concept) sets a fixed day count, inclusive of the start; `EndDate = Date + DurationDays − 1`.

```xml
<Rule id="default" durationDays="7">
  <Strategy><Fixed month="October" day="4" /></Strategy>
</Rule>
```

**Calculated** — a `<Duration><UntilDate>` computes the span's end from a **second strategy**, producing a length that varies year to year and can cross the year boundary. A rule may declare a fixed `durationDays` **or** a calculated `<Duration>`, never both.

| `<UntilDate>` attribute | Required | Type | Description |
|---|---|---|---|
| `startBoundary` | No | enum | <xref:Bodu.Globalization.Calendar.DateBoundary>: `Inclusive` (default) or `Exclusive` — is the start anchor part of the span? |
| `endBoundary` | No | enum | `Inclusive` (default) or `Exclusive` — is the end anchor part of the span? |
| `selection` | No | enum | <xref:Bodu.Globalization.Calendar.EndDateSelection>: `FirstOnOrAfterStart` (default) or `FirstAfterStart`. |

The end strategy is evaluated for the start anchor's civil year and, when that yields nothing on/after the start, the following civil year. This models a **year-end shutdown** that begins the Friday before Boxing Day and ends the Monday after New Year's Day — 16 days some years, 9 in others:

```xml
<Rule id="default">
  <Strategy><WeekdayNearDate month="12" day="26" dayOfWeek="Friday" direction="Before" /></Strategy>
  <Duration>
    <UntilDate startBoundary="Exclusive" endBoundary="Exclusive" selection="FirstOnOrAfterStart">
      <Strategy><WeekdayNearDate month="1" day="1" dayOfWeek="Monday" direction="After" /></Strategy>
    </UntilDate>
  </Duration>
</Rule>
```

With both boundaries exclusive, the last day worked (that Friday) and the return-to-work Monday are outside the span; the occurrence's `Date` is the day after the Friday and its `EndDate` the day before the Monday. A span that resolves to fewer than one day, or whose end cannot be found in the two-year window, is simply not emitted. Maps to <xref:Bodu.Globalization.Calendar.CalculatedEndDateDurationDefinition>.

---

## Common scenarios

| I want to express… | Author it as |
|---|---|
| New Year's Day | `<Fixed month="1" day="1" />` |
| US Thanksgiving (4th Thursday of November) | `<DayOfWeekInMonth month="11" dayOfWeek="Thursday" weekOrdinal="Fourth" />` |
| Good Friday (2 days before Easter) | `<OffsetFromRule notableDateRef="easter-sunday" offsetDays="-2" />` |
| First Monday after Easter | `<WeekdayNearRule notableDateRef="easter-sunday" dayOfWeek="Monday" direction="After" />` |
| Second Monday after a festival | `<NthWeekdayFromRule notableDateRef="festival" dayOfWeek="Monday" ordinal="2" />` |
| Last day of February | `<OrdinalDayOfMonth month="2" ordinal="-1" />` |
| Last working day before Boxing Day | `<WorkingDayOffsetFromRule notableDateRef="boxing-day" offsetWorkingDays="-1" />` |
| First working day of the month | `<WorkingDayInMonth month="1" ordinal="1" />` |
| Monday of ISO week 1 | `<IsoWeekDate week="1" dayOfWeek="Monday" />` |
| Fortnightly event | `<Recurrence><DailyInterval anchorDate="2026-01-01" intervalDays="14" /></Recurrence>` |
| Every Monday and Friday | `<Recurrence><Weekly><Day dayOfWeek="Monday" /><Day dayOfWeek="Friday" /></Weekly></Recurrence>` |
| The 15th of every month | `<Recurrence><MonthlyDay dayOfMonth="15" /></Recurrence>` |
| Last Friday of every month | `<Recurrence><MonthlyWeekday dayOfWeek="Friday" weekOrdinal="Last" /></Recurrence>` |
| Quarterly on the 20th | `<Recurrence><MonthlyDay dayOfMonth="20" intervalMonths="3" anchorDate="2026-01-01" /></Recurrence>` |
| A multi-day festival | `durationDays="7"` on the rule |
| A variable-length year-end shutdown | a `<Duration><UntilDate>` calculated span (above) |
| Every second year | `everyYears="2"` on `<Applicability>` (not a strategy) |
| Move a holiday off the weekend | an [adjustment policy](adjustment-rules.md) (not a strategy) |

All of the above are equally expressible in JSON (lowercased property names) and through the fluent builder — see [Authoring with the notable-date builder](notable-date-builder.md).

---

## Where to go next

- [NotableDateRule and adjustment-policy reference](rule-reference.md) — the per-element field reference for every strategy, recurrence, and duration attribute.
- [Authoring notable date rules](rule-authoring.md) — the surrounding document model: concepts, applicability, imports, overrides.
- [Date calculation algorithms](algorithms.md) — the built-in `<Algorithm>` keys and custom algorithms.
- [Working-day arithmetic](working-days.md) — the working-day model the business-day strategies resolve against.
- [Holiday patterns and examples](holiday-patterns.md) — end-to-end worked patterns.
- [Observance adjustment rules](adjustment-rules.md) — observed-date shifting, the third concern.
- [Runnable samples](../../samples/calendar.md) — the `CustomCalendar` sample's `FrequencyBasedSchedules` and `AuthoringCompanyHolidays` scenarios author the recurrence and calculated-duration rules shown here.
