---
title: Working with non-Gregorian calendars
---

# Working with non-Gregorian calendars

`Bodu.Globalization.Calendar` resolves notable dates expressed in non-Gregorian calendars, not only the Gregorian calendar. A `<Fixed>` rule can be authored in the Islamic (Hijri), Saudi-aligned Umm al-Qura, Hebrew, Persian (Solar Hijri), or Chinese lunisolar calendar by setting the enclosing `<Applicability calendar="...">`, and the resolver projects the authored (month, day) onto the correct Gregorian date for the requested year.

This article describes how the resolver handles non-Gregorian `<Fixed>` rules, when the `sweepCalendarYears` and `skipLeapMonth` attributes are required, how a short Hijri month can fall twice in one Gregorian year, and how computed lunisolar festivals (Vesak, Diwali, Losar, …) are authored with `<Algorithm>` keys instead.

For the schema vocabulary, see [Authoring notable date rules](rule-authoring.md) and [NotableDateRule and adjustment-policy reference](rule-reference.md). For the end-to-end pipeline, see [The resolution pipeline](resolution-pipeline.md).

---

## The `CalendarSystem` enum

The `calendar` attribute on `<Applicability>` is a <xref:Bodu.Globalization.Calendar.CalendarSystem> value. It defaults to `Gregorian`, so an absent `calendar` (or an absent `<Applicability>` entirely) is a Gregorian rule.

| `CalendarSystem` | `calendar` value | Year start | Drift per Gregorian year | Months vary? |
|---|---|---|---|---|
| `Gregorian` | `Gregorian` | 1 January | n/a | No (only February). |
| `Hijri` | `Hijri` | 1 Muharram | ~11 days earlier | Yes (lunar months alternate 29/30 days; year is 354–355 days). |
| `UmmAlQura` | `UmmAlQura` | 1 Muharram | ~11 days earlier | Yes (Saudi observation tables determine each month's start). |
| `Hebrew` | `Hebrew` | 1 Tishri (Sep/Oct) | varies — 12 or 13 months per year | Yes (Adar I appears only in leap years; later months renumber). |
| `Persian` | `Persian` | 1 Farvardin (~20 March) | none (anchored to the vernal equinox) | Slightly (Esfand is 29 or 30 days). |
| `ChineseLunisolar` | `ChineseLunisolar` | varies (lunar new year, late Jan – mid Feb) | varies — 12 or 13 months per year | Yes (intercalary leap months keep lunar months aligned to seasons). |

A service reports the calendar systems any of its rules use through <xref:Bodu.Globalization.Calendar.INotableDateService.GetSupportedCalendars>:

```csharp
using Bodu.Globalization.Calendar;

IReadOnlyList<CalendarSystem> calendars = service.GetSupportedCalendars();
// e.g. [Gregorian, Hijri, Hebrew] for a resource that mixes Gregorian and lunar fixed dates.
```

---

## Why non-Gregorian calendars are different

The Gregorian calendar's year numbering, month numbering, and month lengths are constants. Non-Gregorian calendars violate at least one of those assumptions, with two consequences for a fixed (month, day) rule:

1. **The calendar year number does not match the Gregorian year number.** Hijri year 1445 AH overlaps Gregorian 2023 *and* 2024; Hebrew year 5784 overlaps Gregorian 2023 *and* 2024; Persian year 1402 is roughly Gregorian 2023 but offset by ~621 from the Gregorian year number.

2. **A calendar month can appear in two different Gregorian years**, depending on which calendar year the calendar's new-year date fell in. Conversely, a single Gregorian year can contain *two* instances of the same Hijri month — this happens roughly every 33 years because the Hijri year is shorter than the Gregorian year.

The resolver handles both consequences through the **calendar-year sweep**, enabled by the `sweepCalendarYears` attribute on `<Fixed>`.

---

## The `sweepCalendarYears` attribute

For a non-Gregorian `<Fixed>` rule, set `sweepCalendarYears="true"`. Without the sweep, the requested Gregorian year number would be interpreted directly as a calendar year number in the target calendar — projecting Hijri / Umm al-Qura / Hebrew / Persian rules to an era thousands of years away. The sweep is required for every supported non-Gregorian calendar **except** Chinese lunisolar (which uses `skipLeapMonth` — see below).

```xml
<!-- Islamic New Year — 1 Muharram in the (tabular) Hijri calendar. -->
<NotableDate id="islamic-new-year" displayName="Islamic New Year" category="Religious" defaultNonWorkingDay="true">
  <Rules>
    <Rule id="sa">
      <Applicability calendar="Hijri" />
      <Strategy><Fixed month="1" day="1" sweepCalendarYears="true" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

### How the sweep resolves a date

To project an authored rule for, say, "1 Ramadan" into the Gregorian date for a requested year, the resolver:

1. **Finds the lower-bound calendar year** — the calendar year containing 1 January of the requested Gregorian year. For 1 January 2024 this is 1445 AH (Hijri / Umm al-Qura), 5784 (Hebrew), or 1402 (Persian).
2. **Iterates two candidate calendar years** — the lower-bound year and the next. One covers the calendar year that started in the previous Gregorian year and is still active; the other covers the calendar year that starts during the requested Gregorian year.
3. **Resolves the target month for each candidate.** Hebrew month *names* (see below) are mapped through a leap-year-aware lookup; a name that does not exist in the candidate year (Adar II in a non-leap year) skips that candidate.
4. **Converts (calendar-year, month, day) to a Gregorian date**, skipping a candidate whose day exceeds the month's length (e.g. day 30 in a 29-day Hijri month).
5. **Matches the Gregorian year.** A candidate is accepted only when its result lands in the requested Gregorian year. When *both* candidates land in the requested year — possible only for fast-drifting lunar calendars such as Hijri / Umm al-Qura — the chronologically **earlier** one is returned.
6. **Returns no occurrence** when neither candidate matches — the holiday's Gregorian projection skipped the year entirely.

### A short Hijri month falling twice in a Gregorian year

Because the Hijri year is ~11 days shorter than the Gregorian year, an early-in-the-Hijri-year date drifts earlier each Gregorian year, and roughly every 33 years a single Gregorian year contains the date *twice* — once near the start of January and once near the end of December. The sweep surfaces both: when both candidate Hijri years project into the requested Gregorian year, the engine emits each occurrence. For consumers that want only the first, the by-year resolution returns them in date order, and a range query (`service.Resolve(new DateRange(a, b), territory)`) includes whichever occurrences intersect the window.

The public <xref:Bodu.Globalization.Calendar.Algorithms.FixedDateStrategy> exposes this twice-in-a-year behaviour through `CalculateAll`, alongside the single-date `Calculate` used for the common case.

---

## Worked example: Islamic New Year in 2024 (Hijri)

Resource: `calendars.xml`, concept `islamic-new-year` (1 Muharram, `calendar="Hijri"`, `sweepCalendarYears="true"`).

1. The calendar year containing 1 January 2024 is **1445 AH**.
2. Candidate calendar years are **1445** and **1446**.
3. Try Hijri year **1446**, month 1 (Muharram), day 1 → projects into 2024 — match.

The same concept authored against `calendar="UmmAlQura"` can land one day later than the tabular `Hijri` result, because the Umm al-Qura table is updated from Saudi lunar-observation data. That one-day divergence is exactly why both Islamic calendar systems are offered — see [Choosing between the Islamic calendars](#choosing-between-the-islamic-calendars).

## Worked example: Passover in 2024 (Hebrew, month name)

Resource: `calendars.xml`, concept `passover` (15 Nisan, `calendar="Hebrew"`, `sweepCalendarYears="true"`).

1. The Hebrew year containing 1 January 2024 is **5784** (a leap year, 13 months).
2. Candidate calendar years are **5784** and **5785**.
3. The month name **Nisan** resolves to the correct numeric month in each candidate. `5784` projects 15 Nisan into Gregorian **2024** (late April) — match.

Hebrew rules are authored with a stable month *name* (`Tishri`, `Kislev`, `Nisan`, `LastAdar`, …) rather than a numeric index, because the Hebrew calendar renumbers its months between leap and non-leap years. The resolver maps the name to the correct numeric month for each candidate year automatically.

| Month name | Notes |
|---|---|
| `Tishri` | New year (Rosh Hashanah). |
| `Kislev` | Hanukkah anchor. |
| `AdarI`, `AdarII` | Exist only in leap years; skip the candidate in a non-leap year. |
| `LastAdar` | Always resolves (Adar in a non-leap year, Adar II in a leap year) — preferred for Purim and other "final Adar" observances. |
| `Nisan` | Passover anchor. |
| `Sivan` | Shavuot. |
| `Elul` | Final Hebrew month. |

## Worked example: Nowruz in 2024 (Persian)

Resource: `calendars.xml`, concept `nowruz` (1 Farvardin, `calendar="Persian"`, `sweepCalendarYears="true"`).

1. The Persian year containing 1 January 2024 is **1402** (which began 21 March 2023).
2. Candidate calendar years are **1402** and **1403**.
3. `1402` projects 1 Farvardin to **2023-03-21** (wrong Gregorian year — skip); `1403` projects to **2024-03-20** — match.

Persian's sweep almost always picks the second candidate, because 1 Farvardin falls in March of the requested Gregorian year.

---

## Chinese lunisolar — `skipLeapMonth` instead of the sweep

The Chinese lunisolar calendar is the one supported non-Gregorian calendar that does **not** use `sweepCalendarYears`. Its year numbering aligns with the Gregorian year (the lunar new year falls in late January or mid February), so the calendar year is the same Gregorian year the caller requested.

Instead, the resolver handles the **intercalary leap month** that appears in roughly seven of every nineteen lunar years. Conventional ordinal "lunar months" — "the eighth lunar month" for the Mid-Autumn Festival — refer to a fixed point in the seasonal year, but the underlying calendar numbers months consecutively (1, 2, …, 13 in a leap year), inserting the leap month between conventional months. Setting `skipLeapMonth="true"` tells the resolver to advance the conventional ordinal month past any leap month that precedes it.

```xml
<!-- Mid-Autumn Festival — conventional 15th day of the 8th lunar month. -->
<NotableDate id="mid-autumn" displayName="Mid-Autumn Festival" category="PublicHoliday" defaultNonWorkingDay="true">
  <Rules>
    <Rule id="cn">
      <Applicability calendar="ChineseLunisolar" />
      <Strategy><Fixed month="8" day="15" skipLeapMonth="true" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

Chinese New Year (the 1st day of the 1st lunar month) needs neither attribute, because nothing precedes month 1:

```xml
<NotableDate id="chinese-new-year" displayName="Chinese New Year" category="PublicHoliday" defaultNonWorkingDay="true">
  <Rules>
    <Rule id="cn">
      <Applicability calendar="ChineseLunisolar" />
      <Strategy><Fixed month="1" day="1" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

---

## Lunisolar festivals via `<Algorithm>`

Many lunisolar festivals are *not* a fixed (month, day) in any single calendar system — their date is computed from lunar phases or solar terms. These are authored with an `<Algorithm key="...">` strategy in the Gregorian frame, not a non-Gregorian `<Fixed>` rule. The engine's bundled lunar-phase and solar-term calculators back the keys; the shipping region packs reference the same keys.

```xml
<!-- Vesak (Buddha Purnima) — Theravada full-moon day; computed. -->
<NotableDate id="vesak" displayName="Vesak" category="Religious" defaultNonWorkingDay="false">
  <Rules>
    <Rule id="default"><Strategy><Algorithm key="vesak" /></Strategy></Rule>
  </Rules>
</NotableDate>
```

Built-in festival keys include:

| Key | Festival |
|---|---|
| `vesak` | Vesak (Buddha's birthday) — Theravada full-moon observance. |
| `asalha-puja` | Asalha Puja (Dhamma Day) — full moon of the eighth lunar month. |
| `losar` | Losar — Tibetan lunisolar new year. |
| `qingming` | Qingming (Tomb-Sweeping Day) — solar term 15° after the March equinox. |
| `diwali` | Diwali (Deepavali). |
| `holi` | Holi. |
| `ram-navami`, `janmashtami`, `ganesh-chaturthi`, `navaratri`, `dussehra`, `karva-chauth`, `vasant-panchami`, `maha-shivaratri`, `raksha-bandhan` | Hindu festivals computed against the Hindu lunisolar panchanga. |
| `maun-agiyaras` | Jain observance computed against the same panchanga. |

See [Date calculation algorithms](algorithms.md) for the complete key catalogue and for writing a custom <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm>.

---

## Choosing between the Islamic calendars

Two calendar systems exist for Islamic dates because no single tabular calendar reproduces every official Eid date:

| `calendar` value | Algorithm | When to use |
|---|---|---|
| `Hijri` | Tabular, fixed 30-year arithmetic cycle | Portable / deterministic Islamic dates; non-Saudi jurisdictions that publish dates aligned to tabular Hijri; cross-platform date comparison. |
| `UmmAlQura` | Tabular, updated from Saudi-government lunar observation | Saudi Arabia, the Gulf states, and any jurisdiction whose public-holiday calendar tracks the Saudi Royal Court announcements. |

The two can produce Gregorian dates differing by zero or one day for the same Hijri date. The base package ships both as separate common catalogues — `global-islamic` (tabular) and `global-islamic-umm-al-qura` (Saudi-aligned) — so a region pack imports whichever it tracks. For jurisdictions whose announced civil date occasionally diverges from *both* tabular calendars (a physical-sighting override), ship a custom `<Algorithm>` that consults an authoritative annual source and reference it instead of the tabular `<Fixed>` rule.

---

## Multi-day events that span the Gregorian year boundary

Several non-Gregorian observances are multi-day (Ramadan 30 days, Hanukkah 8 days, Sukkot 7 days, Nowruz up to 13 days). A span can straddle the Gregorian year boundary — a Hanukkah occurrence anchored on 25 Kislev can begin on 26 December and end on 2 January.

A range query includes any occurrence whose span intersects the window, governed by the resource's <xref:Bodu.Globalization.Calendar.RangeResolution.ObservedDateRangePolicy>:

```csharp
using Bodu.Globalization.Calendar;

IReadOnlyList<NotableDate> earlyJanuary = service.Resolve(
    new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 7)), "IL");
// includes the December-anchored Hanukkah occurrence that overlaps into January 2025.
```

To keep only the occurrence whose actual date falls inside a given year, filter on the occurrence's `ActualDate`:

```csharp
NotableDate? inYear = service.Resolve(2025, "IL")
    .FirstOrDefault(d => d.DisplayName == "Hanukkah" && d.ActualDate?.Year == 2025);
```

---

## Authoring checklist

When authoring a `<Fixed>` rule against a non-Gregorian calendar:

- [ ] Set `<Applicability calendar="...">` to the target <xref:Bodu.Globalization.Calendar.CalendarSystem> (`Hijri`, `UmmAlQura`, `Hebrew`, `Persian`, or `ChineseLunisolar`).
- [ ] For Hijri / Umm al-Qura / Hebrew / Persian, set `sweepCalendarYears="true"` on `<Fixed>`.
- [ ] For Chinese lunisolar, set `skipLeapMonth="true"` on `<Fixed>` for any ordinal month after the first; Chinese New Year needs neither attribute.
- [ ] For Hebrew, author the month with its stable *name* (`Nisan`, `Kislev`, `LastAdar`, …) rather than a numeric index.
- [ ] For a festival that is computed (lunar phase / solar term) rather than a fixed (month, day), use an `<Algorithm key="...">` strategy in the Gregorian frame instead.
- [ ] Verify expected dates against an authoritative source, and add a regression test row asserting the projected Gregorian date for at least one year.

---

## Limitations and edge cases

- **Supported date range.** Each calendar system has a supported range; a query for a Gregorian year outside it produces no occurrence rather than throwing.
- **Tabular versus observation.** Every shipped non-Gregorian resource uses a *tabular* calendar. When a civil authority announces a date by physical observation that diverges from the tabular calculation, no tabular calendar reproduces the announced date for those years — pair the tabular rule with an `<Algorithm>`-keyed override for the affected years.
- **Saudi-aligned dates.** Use `calendar="UmmAlQura"` for Saudi-aligned dates rather than attempting to adjust the tabular `Hijri` calendar.

---

## See also

- [Authoring notable date rules](rule-authoring.md) — the `<Fixed>` element and the `sweepCalendarYears` / `skipLeapMonth` attributes.
- [NotableDateRule and adjustment-policy reference](rule-reference.md) — the per-element field reference, including `<Applicability calendar="...">`.
- [Date calculation algorithms](algorithms.md) — the `<Algorithm>` keys for computed lunisolar festivals.
- [The resolution pipeline](resolution-pipeline.md) — territory scoping, adjustment chains, and the optional filter gate.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
