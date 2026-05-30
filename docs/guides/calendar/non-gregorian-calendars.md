---
title: Working with non-Gregorian calendars
---

# Working with non-Gregorian calendars

`Bodu.Globalization.Calendar` resolves notable dates against any
`System.Globalization.Calendar`-derived calendar, not just the Gregorian
calendar. Rules can be authored in the Islamic (Hijri), Saudi-aligned
Umm al-Qura, Hebrew, Persian (Solar Hijri), or Chinese lunisolar
calendars and the resolver projects each authored (month, day) into the
correct Gregorian date for the requested year.

This article describes how the pipeline handles non-Gregorian rules,
when each calendar-specific flag (`sweepCalendarYears`, `skipLeapMonth`,
`calendarMonthAlias`) is required, and how to choose between the
calendar resources that ship in the box.

For the schema vocabulary and authoring syntax, see
[Authoring notable date rules](rule-authoring.md). For the end-to-end
pipeline, see [The resolution pipeline](resolution-pipeline.md).

---

## Why non-Gregorian calendars are different

The Gregorian calendar's year numbering, month numbering, and month
lengths are constants — `new DateTime(2024, 3, 11)` is meaningful
without any other context, and the same constants produce the same date
every year. Non-Gregorian calendars violate at least one of these
assumptions:

| Calendar | Year start | Drift per year | Months are variable? |
|---|---|---|---|
| Gregorian | 1 January | n/a | No (only February). |
| Hijri (tabular) | 1 Muharram | ~11 days earlier each Gregorian year | Yes (lunar months alternate 29/30 days; year is 354 or 355 days). |
| Umm al-Qura | 1 Muharram | ~11 days earlier each Gregorian year | Yes (Saudi observation tables determine each month's start). |
| Hebrew | 1 Tishri (Sep/Oct) | varies — 12 or 13 months per year | Yes (Adar I appears only in leap years; later months renumber). |
| Persian (Solar Hijri) | 1 Farvardin (~20 March) | none (anchored to vernal equinox) | Slightly (Esfand is 29 or 30 days). |
| Chinese lunisolar | varies (lunar new year, late Jan – mid Feb) | varies — 12 or 13 months per year | Yes (intercalary leap months inserted to keep lunar months aligned with seasons). |

Two consequences follow:

1. **The calendar year number does not match the Gregorian year
   number.** Hijri year 1445 AH overlaps Gregorian 2023 *and* 2024.
   Hebrew year 5784 overlaps Gregorian 2023 *and* 2024. Persian year
   1402 is roughly Gregorian 2023, but is offset by ~621 from the
   Gregorian year number.

2. **A calendar month can appear in two different Gregorian years**
   depending on which calendar year the calendar's new-year date
   fell in. Conversely, a single Gregorian year can contain two
   instances of the same Hijri month (this happens every ~33 years
   because the Hijri year is shorter than the Gregorian year).

The resolver handles both consequences through the **Fixed-strategy
calendar-year sweep**, controlled by the `sweepCalendarYears`
attribute.

---

## The Fixed-strategy decision tree

When `NotableDateService` resolves a Fixed-strategy rule, it routes
through one of four paths depending on the rule's
`CalendarType` and `SweepCalendarYears` settings:

```
DateResolutionStrategy = Fixed
│
├─ CalendarType is null  (or GregorianCalendar)
│  └─► new DateTime(year, Month, Day)                          ─ Gregorian path
│
└─ CalendarType is non-null
   │
   ├─ SweepCalendarYears = true
   │  └─► ResolveCalendarYearSweep(rule, year, cal, day)       ─ Sweep path
   │
   ├─ SkipLeapMonth = true AND CalendarType = ChineseLunisolarCalendar
   │  └─► ResolveChineseLeapMonthSkip(rule, year, cal, day)    ─ Lunisolar leap-skip path
   │
   └─ (neither flag set)
      └─► cal.ToDateTime(year, Month, Day, ...)                ─ Direct projection
            *** rarely the right choice — see the warning below ***
```

> [!IMPORTANT]
> The fourth path (direct projection without sweep) interprets the
> requested Gregorian year number as a calendar year number in the
> target calendar. This is almost never what you want for Hijri,
> Umm al-Qura, Hebrew, or Persian rules — it would project to an
> era thousands of years away. If you author a rule against a
> non-Gregorian calendar, set either `sweepCalendarYears="true"` or
> (for Chinese lunisolar ordinal months) `skipLeapMonth="true"`.

---

## The calendar-year sweep, step by step

`ResolveCalendarYearSweep` is the engine for all date-aligned
non-Gregorian calendars. Here is how it converts an authored rule for
"Ramadan 1" into the Gregorian date for a given year:

1. **Find the lower-bound calendar year.** The resolver asks the
   target calendar for `GetYear(new DateTime(year, 1, 1))` — the
   calendar year that contains 1 January of the requested Gregorian
   year. For 1 January 2024, this returns:

   | Calendar | Result |
   |---|---|
   | Hijri (tabular) | 1445 AH |
   | Umm al-Qura | 1445 AH |
   | Hebrew | 5784 |
   | Persian | 1402 |

2. **Iterate two candidate calendar years** — the lower-bound year
   and the lower-bound year plus one. One candidate covers the
   calendar year that started in the previous Gregorian year and is
   still active; the other covers the calendar year that starts
   during the requested Gregorian year.

3. **Resolve the target month for each candidate.** When the rule's
   `CalendarMonthAlias` is set (Hebrew rules), the alias is mapped
   through a leap-year-aware lookup that handles the renumbering of
   Adar, Nisan, and the months that follow them. When the alias
   returns `-1` (the month does not exist in the candidate year —
   for example Adar II in a non-leap Hebrew year), that candidate
   is skipped.

4. **Convert (calendar-year, month, day) to a Gregorian
   `DateTime`** via the calendar's `ToDateTime` method. Skip the
   candidate if the day exceeds the month's length in that calendar
   year (e.g. day 30 in a 29-day Hijri month).

5. **Match the Gregorian year.** Accept the candidate only when its
   resulting `Year` equals the requested Gregorian year. When both
   candidate calendar years produce a date in the requested year —
   possible only for fast-drifting lunar calendars such as
   Hijri / Umm al-Qura — the chronologically **earlier** one is
   returned.

6. **Return `null` if neither candidate matches.** This happens
   when a holiday's Gregorian projection skips the year entirely —
   for example, an early Hijri date might fall in late December of
   year *N − 1* and the next occurrence not until January of year
   *N + 1*, leaving year *N* with no Gregorian occurrence.

---

## Worked example: Ramadan in 2024 (Umm al-Qura)

Resource: `global-islamic-umm-al-qura.xml`, rule
`fixed-uaq-09-01` (`Ramadan` — 1 Ramadan).

1. `cal.GetYear(new DateTime(2024, 1, 1))` → **1445 AH** (the lunar
   year that contained 1 January 2024).
2. Candidate calendar years are **1445** and **1446**.
3. **Try Hijri year 1445, month 9 (Ramadan), day 1:**
   `cal.ToDateTime(1445, 9, 1, ...)` → **2024-03-11** (Monday).
   The Gregorian year is 2024 — match. Return.
4. The 1446 candidate is not evaluated because the 1445 candidate
   already matched.

The same rule resolved against `HijriCalendar` (used by
`global-islamic.xml`) returns **2024-03-10** — one day earlier than
the Saudi-announced 11 March. This is the design rationale for
shipping both resources; see [Choosing between Islamic
resources](#choosing-between-islamic-resources) below.

## Worked example: Hanukkah in 2024 (Hebrew with month alias)

Resource: `global-jewish.xml`, rule `fixed-hebrew-kislev-25`
(`Hanukkah` — 25 Kislev).

1. `cal.GetYear(new DateTime(2024, 1, 1))` → **5784**. 5784 is a
   Hebrew leap year (13 months).
2. Candidate calendar years are **5784** and **5785**.
3. **Try Hebrew year 5784, alias "Kislev":** alias resolves to
   month 3 in every Hebrew year. `cal.ToDateTime(5784, 3, 25, ...)`
   → **2023-12-08**. The Gregorian year is 2023, not 2024 — skip.
4. **Try Hebrew year 5785, alias "Kislev":** 5785 is non-leap,
   alias still resolves to month 3. `cal.ToDateTime(5785, 3, 25,
   ...)` → **2024-12-26**. The Gregorian year is 2024 — match.
   Return.

> [!NOTE]
> Hanukkah lasts eight days, so the 2024-12-26 anchor combined with
> `durationDays="8"` produces an end date of 2025-01-02. Querying
> `GetNotableDates(2025)` will return both the 2024-anchored
> Hanukkah (which overlaps into 2025) *and* the 2025-anchored
> Hanukkah (anchored 2025-12-15). Filter on `Date.Year == year` when
> you want only the in-year-anchored occurrence.

## Worked example: Purim in 2023 (Hebrew non-leap year)

Resource: `global-jewish.xml`, rule `fixed-hebrew-lastadar-14`
(`Purim` — 14 LastAdar).

1. `cal.GetYear(new DateTime(2023, 1, 1))` → **5783**. 5783 is a
   non-leap Hebrew year (12 months).
2. Candidate calendar years are **5783** and **5784**.
3. **Try Hebrew year 5783, alias "LastAdar":** alias resolves to
   month 6 (the single Adar month in a non-leap year).
   `cal.ToDateTime(5783, 6, 14, ...)` → **2023-03-07**. Match.
   Return.

When the same rule resolves for 2024, the sweep visits 5784 (a leap
year). The alias resolves to month 7 (Adar II), and
`cal.ToDateTime(5784, 7, 14, ...)` → **2024-03-24** — confirming the
Rabbinic convention that Purim falls in Adar II in leap years.

## Worked example: Nowruz in 2024 (Persian)

Resource: `global-persian.xml`, rule `fixed-persian-01-01`
(`Nowruz` — 1 Farvardin).

1. `cal.GetYear(new DateTime(2024, 1, 1))` → **1402**. The Persian
   year 1402 began on 21 March 2023.
2. Candidate calendar years are **1402** and **1403**.
3. **Try Persian year 1402, month 1 (Farvardin), day 1:**
   `cal.ToDateTime(1402, 1, 1, ...)` → **2023-03-21**. Gregorian
   year is 2023, not 2024 — skip.
4. **Try Persian year 1403, month 1, day 1:**
   `cal.ToDateTime(1403, 1, 1, ...)` → **2024-03-20**. Match.
   Return.

Persian's sweep almost always picks the second candidate year because
1 Farvardin falls in March of the requested Gregorian year. The
exception is queries against a year that contains *no* Nowruz, which
cannot happen within the BCL's Persian calendar range (1 AP – 9378
AP, covering ~622 CE – ~9999 CE).

---

## Choosing between Islamic resources

`Bodu.Globalization.Calendar` ships two Islamic resources because no
single tabular calendar reproduces every official Eid date:

| Resource | Calendar | Algorithmic | When to use |
|---|---|---|---|
| `global-islamic.xml` | `System.Globalization.HijriCalendar` | Tabular, fixed 30-year arithmetic cycle | Portable / deterministic Islamic dates; non-Saudi jurisdictions that publish dates aligned to tabular Hijri; cross-platform date comparison. |
| `global-islamic-umm-al-qura.xml` | `System.Globalization.UmAlQuraCalendar` | Tabular but updated from Saudi-government lunar observation data | Saudi Arabia, the Gulf states, and any jurisdiction whose public-holiday calendar tracks the announcements of the Saudi Royal Court. |

The two resources can produce Gregorian dates that differ by zero
or one day in the same Hijri year. They are not authored as a `UseFrom`
chain; if you want both, register both providers and let the regional
override layer suppress the one you do not want for a given territory.
`global-all.xml` includes only `global-islamic.xml` (the tabular
default) to avoid declaring two rules for `"Ramadan"`, `"Eid al-Fitr"`,
and so on.

> [!TIP]
> For jurisdictions where the announced civil date can sometimes
> diverge from both tabular calendars by an additional day (the
> Royal Court occasionally overrides the Umm al-Qura table based on
> physical sighting), neither tabular calendar will be authoritative
> in every year. In those cases, ship a custom `INotableDateAlgorithm`
> that consults an authoritative annual announcement source, and
> override the tabular rule via a mutable provider for the specific
> year(s) that diverge.

---

## Multi-day events that span the Gregorian year boundary

Several non-Gregorian observances are multi-day:

- Ramadan — 30 days
- Eid al-Adha — 4 days (Saudi-aligned regions)
- Hanukkah — 8 days
- Rosh Hashanah — 2 days
- Sukkot — 7 days
- Nowruz — 13 days (the full holiday period through Sizdah Bedar)

Multi-day events can straddle the Gregorian year boundary. For
example, Hanukkah anchored on 25 Kislev 5785 begins on
**2024-12-26** and ends on **2025-01-02**. The service surfaces this
occurrence in *both* `GetNotableDates(2024)` and
`GetNotableDates(2025)` because the eight-day span overlaps both
Gregorian years.

If you want only the occurrence whose anchor falls inside the
queried year, filter on `Date.Year`:

```csharp
NotableDate? inYearAnchor = service.GetNotableDates(2025)
    .FirstOrDefault(d => d.Name == "Hanukkah" && d.Date.Year == 2025);
```

If you want all overlapping occurrences (for example, to count the
days of Hanukkah that fall in January 2025 against a working-day
calendar), do not filter — the service already returned both
occurrences.

---

## Calendar-month aliases (Hebrew)

The Hebrew calendar renumbers its months between leap and non-leap
years. Adar exists alone in non-leap years (month 6) but splits into
Adar I (month 6) and Adar II (month 7) in leap years, pushing Nisan
through Elul down by one. Authoring against the BCL's numeric month
index would require leap-year branching on every Hebrew rule.

`CalendarMonthAlias` solves this by accepting a stable Hebrew month
name. The resolver maps it to the correct numeric month for each
candidate calendar year:

| Alias | Non-leap year | Leap year | Notes |
|---|---|---|---|
| `Tishri` | 1 | 1 | New year (Rosh Hashanah). |
| `Heshvan` | 2 | 2 | |
| `Kislev` | 3 | 3 | Hanukkah anchor. |
| `Tevet` | 4 | 4 | |
| `Shevat` | 5 | 5 | |
| `AdarI` | — | 6 | Returns `-1` in non-leap years; that candidate year is skipped. |
| `AdarII` | — | 7 | Returns `-1` in non-leap years. |
| `LastAdar` | 6 | 7 | Always resolves; preferred for Purim and other "final Adar" observances. |
| `Nisan` | 7 | 8 | Passover anchor. |
| `Iyar` | 8 | 9 | |
| `Sivan` | 9 | 10 | Shavuot. |
| `Tammuz` | 10 | 11 | |
| `Av` | 11 | 12 | |
| `Elul` | 12 | 13 | Final Hebrew month. |

`CalendarMonthAlias` is only consulted by the calendar-year sweep
path; it has no effect on rules whose `CalendarType` is `null` or
whose `CalendarType` is not `HebrewCalendar`.

---

## Chinese lunisolar — the `skipLeapMonth` sibling path

The Chinese lunisolar calendar is the one supported non-Gregorian
calendar that does **not** use `sweepCalendarYears`. Its
year-numbering aligns with the Gregorian year (the lunar new year
falls in late January or mid February), so the calendar year passed
to `cal.ToDateTime` is the same Gregorian year requested by the
caller.

Instead, the resolver handles the **intercalary leap month** that
appears in roughly seven of every nineteen lunar years. Conventional
ordinal "lunar months" — for example, "the fifth lunar month" used
for the Dragon Boat Festival — refer to a fixed point in the seasonal
year. The BCL's `ChineseLunisolarCalendar` numbers months
consecutively (1, 2, …, 13 in a leap year), with the intercalary
month sometimes inserted between conventional months 5 and 6 (and
sometimes elsewhere).

Setting `skipLeapMonth="true"` tells the resolver to advance the
conventional ordinal month past any leap month that precedes it.
For the Dragon Boat Festival in a leap year where the intercalary
month is the fifth in calendar numbering, the resolver maps
conventional month 5 to calendar month 5 (the festival falls before
the leap month) but Mid-Autumn Festival's conventional month 8 maps
to calendar month 9.

This path is only triggered when `CalendarType` is
`ChineseLunisolarCalendar` *and* `SkipLeapMonth` is true.

---

## Authoring checklist

When authoring a new rule against a non-Gregorian calendar:

- [ ] Set `calendarType` to the assembly-qualified or BCL type name
      of the target calendar (e.g.
      `System.Globalization.HijriCalendar`).
- [ ] Set `sweepCalendarYears="true"` on the `<Fixed>` element
      (except for `ChineseLunisolarCalendar`).
- [ ] For Hebrew rules whose month renumbers between leap years,
      prefer `month="<HebrewMonthName>"` over a numeric `month` —
      the resolver matches the name against the
      `CalendarMonthAlias` lookup automatically.
- [ ] For Chinese lunisolar rules, set `skipLeapMonth="true"`
      instead of `sweepCalendarYears`.
- [ ] Verify expected dates against an authoritative source — see
      the per-resource header comments in
      `Bodu.Globalization.Calendar/src/Globalization.Calendar.Resources/`
      for the references used by each existing rule set.
- [ ] Write a regression test row asserting the projected
      Gregorian date for at least one year. The
      `GlobalIslamicResourceTests`, `GlobalIslamicUmmAlQuraResourceTests`,
      `GlobalJewishResourceTests`, and `GlobalPersianResourceTests`
      classes in the calendar test project demonstrate the pattern.

---

## Limitations and edge cases

- **BCL calendar supported range.** Each BCL calendar declares
  `MinSupportedDateTime` and `MaxSupportedDateTime`. Queries for
  Gregorian years outside the calendar's range cause the sweep to
  return `null` rather than throw — the rule simply produces no
  occurrence.
- **Tabular versus observation.** Every shipped resource uses a
  *tabular* calendar — Hijri's 30-year arithmetic cycle, the
  Umm al-Qura published table, the Persian 33-year intercalation
  cycle, the Rabbinic Hebrew computation. When a civil authority
  announces a date based on physical observation that diverges
  from the tabular calculation (most commonly for Saudi-announced
  Eids before 1999 or for jurisdictions outside the Gulf that use
  local crescent sighting), no tabular calendar will reproduce the
  announced date for those years. Pair the tabular rule with an
  algorithm-keyed override for the affected years.
- **Persian boundary years.** The BCL `PersianCalendar` uses the
  33-year intercalation cycle adopted in .NET, which matches the
  Iranian civil calendar in years where the tabular and
  vernal-equinox-observed Nowruz dates agree. For boundary years
  where they diverge by one day (rare; centered around the
  cycle-correction years near 1488 AP / 1525 AP), ship a custom
  algorithm.
- **`HijriCalendar.HijriAdjustment`.** The resolver always
  instantiates `HijriCalendar` via its parameterless constructor,
  which uses `HijriAdjustment = 0` (the tabular default). To
  produce Saudi-aligned dates, use `UmAlQuraCalendar` rather than
  attempting to adjust `HijriCalendar`.

---

## See also

- [Authoring notable date rules](rule-authoring.md) — XML / JSON
  schema vocabulary for `<Fixed>` and the
  `sweepCalendarYears` / `skipLeapMonth` attributes.
- [The resolution pipeline](resolution-pipeline.md) — the full
  eight-stage pipeline that runs around the Fixed-strategy
  resolver, including territory scoping, adjustment chains, and
  the optional filter gate.
- [`NotableDateRule.SweepCalendarYears` API reference](xref:Bodu.Globalization.Calendar.NotableDateRule.SweepCalendarYears)
- [`NotableDateRule.CalendarType` API reference](xref:Bodu.Globalization.Calendar.NotableDateRule.CalendarType)
- [`DateResolutionStrategy` API reference](xref:Bodu.Globalization.Calendar.DateResolutionStrategy)
