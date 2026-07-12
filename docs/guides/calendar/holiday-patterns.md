---
title: Holiday patterns and examples
---

# Holiday patterns and examples

This page collects end-to-end worked patterns for common real-world holiday shapes. Each section pairs a short rule-document snippet on the notable-date schema with a one-to-three-line snippet that loads it and resolves the result. Snippets marked for compilation are built against the current API by the `DocumentationSnippetCompileTests` guard in the engine's test project, so the shown calls cannot silently drift: territories are plain strings, by-year resolution is the `service.Resolve(year, territory)` extension, and `NotableDate.DisplayName` carries the name.

For the element-by-element field reference, see [NotableDateRule and adjustment-policy reference](rule-reference.md). For the adjustment trigger / action / emission catalogues, see [Observance adjustment rules](adjustment-rules.md). For how documents are assembled and loaded, see [Authoring notable date rules](rule-authoring.md).

Throughout, a snippet shows just the relevant document fragment. A complete document wraps the fragments in `<NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="...">`, with `<AdjustmentPolicies>` before `<NotableDates>`. To run a fragment, load it and build a service:

```csharp
using Bodu.Globalization.Calendar;

NotableDateResource resource = NotableDateResourceLoader.Load(xml);   // add CommonNotableDateResources.Resolver if the document imports
NotableDateService  service  = new NotableDateService(resource);
```

---

## Fixed-date holiday

A holiday on the same month and day every year uses `<Fixed>`. No adjustment is needed unless the territory substitutes the day when it falls on a weekend.

```xml
<NotableDate id="christmas-day" displayName="Christmas Day" category="PublicHoliday" defaultNonWorkingDay="true">
  <Rules>
    <Rule id="default">
      <Tags><Tag value="christian" /></Tags>
      <Strategy><Fixed month="December" day="25" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

<!-- compile -->
```csharp
NotableDateService service = AmericasCalendarData.CreateService("US");

IReadOnlyList<NotableDate> onDay = service.Resolve(new DateOnly(2026, 12, 25), "US");
Console.WriteLine(onDay[0].DisplayName);   // Christmas Day
```

---

## Weekend substitution via a reusable adjustment policy

A substitution is authored once as an `<AdjustmentPolicy>` and referenced from any rule by `policyRef`. The policy pairs a `<Trigger>` (when it fires) with an `<Action>` (what it does) and an `<Emission>` (what is emitted).

### AU / NZ — Saturday or Sunday moves to Monday

A single `IfWeekend` trigger covers both weekend days; `MoveToNextWorkingDay` advances to the following Monday, and `ObservedOnly` means the single occurrence moves.

```xml
<AdjustmentPolicies>
  <AdjustmentPolicy id="weekend-roll" priority="100"
                    description="If the holiday falls on a weekend, observe it on the following Monday.">
    <Trigger type="IfWeekend" />
    <Action type="MoveToNextWorkingDay" skipWeekends="true" skipNonWorkingDates="false" maxSearchDays="7" />
    <Emission mode="ObservedOnly" reason="Substitute public holiday" />
  </AdjustmentPolicy>
</AdjustmentPolicies>

<NotableDates>
  <NotableDate id="australia-day" displayName="Australia Day" category="PublicHoliday" defaultNonWorkingDay="true">
    <Rules>
      <Rule id="au">
        <Applicability calendar="Gregorian"><Territory code="AU" /></Applicability>
        <Strategy><Fixed month="January" day="26" /></Strategy>
        <Adjustments><Adjustment policyRef="weekend-roll" /></Adjustments>
      </Rule>
    </Rules>
  </NotableDate>
</NotableDates>
```

```csharp
// 26 January 2025 is a Sunday → observed on Monday 27 January.
NotableDate auDay = service.Resolve(2025, "AU").Single(d => d.NotableDateId == "australia-day");
Console.WriteLine($"{auDay.Date} observed={auDay.IsObserved} (actual {auDay.ActualDate})");
```

### UK — Saturday and Sunday each get their own substitute

UK bank-holiday law gives Saturday and Sunday different outcomes. A general weekend roll already lands both on the next working day, but where the law keeps the nominal day *and* grants an additional substitute, use `ActualAndObserved` so both occurrences are emitted.

```xml
<AdjustmentPolicy id="uk-substitute" priority="100"
                  description="Grant an additional substitute public holiday when the day falls on a weekend.">
  <Trigger type="IfWeekend" />
  <Action type="MoveToNextWorkingDay" skipWeekends="true" skipNonWorkingDates="true" maxSearchDays="7" />
  <Emission mode="ActualAndObserved" reason="Substitute bank holiday" />
</AdjustmentPolicy>
```

```xml
<NotableDate id="christmas-day" displayName="Christmas Day" category="BankHoliday" defaultNonWorkingDay="true">
  <Rules>
    <Rule id="gb">
      <Applicability calendar="Gregorian"><Territory code="GB" /></Applicability>
      <Strategy><Fixed month="December" day="25" /></Strategy>
      <Adjustments><Adjustment policyRef="uk-substitute" /></Adjustments>
    </Rule>
  </Rules>
</NotableDate>
```

```csharp
IReadOnlyList<NotableDate> gbXmas = service.Resolve(2027, "GB")
    .Where(d => d.NotableDateId == "christmas-day").ToList();   // 25 Dec 2027 is a Saturday → nominal + substitute Monday
```

### US — Saturday moves to Friday, Sunday moves to Monday

The federal US convention shifts Saturday holidays to the preceding Friday and Sunday holidays to the following Monday. That is two directions, so author two policies and reference both; first-match by priority selects the one whose trigger fires.

```xml
<AdjustmentPolicies>
  <AdjustmentPolicy id="us-saturday-to-friday" priority="1"
                    description="A Saturday holiday is observed on the preceding Friday.">
    <Trigger type="IfDayOfWeek"><Weekday value="Saturday" /></Trigger>
    <Action type="MoveToPreviousWorkingDay" skipWeekends="true" skipNonWorkingDates="false" maxSearchDays="3" />
    <Emission mode="ObservedOnly" reason="Observed (Saturday holiday)" />
  </AdjustmentPolicy>

  <AdjustmentPolicy id="us-sunday-to-monday" priority="2"
                    description="A Sunday holiday is observed on the following Monday.">
    <Trigger type="IfDayOfWeek"><Weekday value="Sunday" /></Trigger>
    <Action type="MoveToNextWorkingDay" skipWeekends="true" skipNonWorkingDates="false" maxSearchDays="3" />
    <Emission mode="ObservedOnly" reason="Observed (Sunday holiday)" />
  </AdjustmentPolicy>
</AdjustmentPolicies>

<NotableDates>
  <NotableDate id="independence-day" displayName="Independence Day" category="PublicHoliday" defaultNonWorkingDay="true">
    <Rules>
      <Rule id="us">
        <Applicability calendar="Gregorian"><Territory code="US" /></Applicability>
        <Strategy><Fixed month="July" day="4" /></Strategy>
        <Adjustments>
          <Adjustment policyRef="us-saturday-to-friday" />
          <Adjustment policyRef="us-sunday-to-monday" />
        </Adjustments>
      </Rule>
    </Rules>
  </NotableDate>
</NotableDates>
```

```csharp
// 4 July 2026 is a Saturday → observed on Friday 3 July.
NotableDate july4 = service.Resolve(2026, "US").Single(d => d.NotableDateId == "independence-day");
Console.WriteLine($"{july4.Date} (actual {july4.ActualDate})");
```

The full trigger / action / emission catalogue, and the AU/NZ, UK, and US patterns in depth, are in [Observance adjustment rules](adjustment-rules.md).

---

## Floating weekday-of-month holiday

A holiday defined as the *n*th occurrence of a weekday in a month uses `<DayOfWeekInMonth>` with a <xref:Bodu.Extensions.WeekOrdinal> (`First`…`Fifth`, `Last`).

```xml
<NotableDate id="thanksgiving" displayName="Thanksgiving Day" category="PublicHoliday" defaultNonWorkingDay="true">
  <Rules>
    <Rule id="us">
      <Applicability calendar="Gregorian"><Territory code="US" /></Applicability>
      <Strategy><DayOfWeekInMonth month="November" dayOfWeek="Thursday" weekOrdinal="Fourth" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

```csharp
NotableDate thanksgiving = service.Resolve(2026, "US").Single(d => d.NotableDateId == "thanksgiving");
Console.WriteLine(thanksgiving.Date);   // 26 November 2026
```

`weekOrdinal="Last"` resolves to the final occurrence regardless of whether it is the fourth or fifth — the shape used for the UK Summer Bank Holiday (last Monday in August) and the WA King's Birthday (last Monday in September).

---

## The Easter cluster

Easter Sunday is computed by the `<Algorithm key="western-easter">` strategy (or `orthodox-easter` for the Julian computus). Good Friday and Easter Monday hang off it with `<OffsetFromRule>`, so they track the anchor rather than re-deriving it.

```xml
<NotableDate id="easter-sunday" displayName="Easter Sunday" category="Religious" defaultNonWorkingDay="true">
  <Rules>
    <Rule id="default"><Strategy><Algorithm key="western-easter" /></Strategy></Rule>
  </Rules>
</NotableDate>

<NotableDate id="good-friday" displayName="Good Friday" category="PublicHoliday" defaultNonWorkingDay="true">
  <Rules>
    <Rule id="default">
      <Strategy><OffsetFromRule notableDateRef="easter-sunday" ruleRef="default" offsetDays="-2" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>

<NotableDate id="easter-monday" displayName="Easter Monday" category="PublicHoliday" defaultNonWorkingDay="true">
  <Rules>
    <Rule id="default">
      <Strategy><OffsetFromRule notableDateRef="easter-sunday" ruleRef="default" offsetDays="1" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

```csharp
IReadOnlyList<NotableDate> easter = service.Resolve(2026, "AU")
    .Where(d => d.NotableDateId is "good-friday" or "easter-sunday" or "easter-monday").ToList();
// Good Friday 3 Apr, Easter Sunday 5 Apr, Easter Monday 6 Apr 2026.
```

Extend the cluster with the same shape: Easter Saturday is `offsetDays="-1"`, Ascension Thursday `offsetDays="39"`, Whit Monday `offsetDays="50"`. The two Easter keys are also exposed as constants `AlgorithmDateStrategy.WesternEasterKey` and `OrthodoxEasterKey`. See [Date calculation algorithms](algorithms.md).

---

## A lunar / algorithmic date

Lunisolar festivals cannot be expressed as calendar arithmetic, so they use `<Algorithm key="...">`. The bundled calculators back the keys; no registration is needed for built-in keys.

```xml
<NotableDate id="vesak" displayName="Vesak" category="Religious" defaultNonWorkingDay="true">
  <Rules>
    <Rule id="default">
      <Applicability calendar="Gregorian"><Territory code="MY" /></Applicability>
      <Strategy><Algorithm key="vesak" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

```csharp
NotableDate vesak = service.Resolve(2026, "MY").Single(d => d.NotableDateId == "vesak");
Console.WriteLine($"{vesak.Date}  {vesak.DisplayName}");
```

Diwali (`diwali`), Holi (`holi`), Qingming (`qingming`), Losar (`losar`), and the other Hindu and Buddhist festival keys follow the same shape. A fixed date in a non-Gregorian calendar (Chinese New Year, Nowruz, Passover) is authored differently — see [Working with non-Gregorian calendars](non-gregorian-calendars.md).

---

## A multi-day event

Set `durationDays` (on the rule, or `defaultDurationDays` on the concept) to the number of calendar days the event spans, inclusive of the start date. `NotableDate.EndDate` is then `Date + DurationDays − 1`.

> [!TIP]
> When the span's length varies year to year — a year-end shutdown that runs to the first working day back, say — compute its end from a second strategy with a `<Duration><UntilDate>` instead of a fixed `durationDays`. For repeating events (every fortnight, every Monday, the last Friday of each month), author a `<Recurrence>` source. Both are covered in [Notable-date rule strategies](strategy-reference.md).

```xml
<NotableDate id="national-reconciliation-week" displayName="National Reconciliation Week"
             category="Observance" defaultNonWorkingDay="false" defaultDurationDays="7">
  <Rules>
    <Rule id="au">
      <Applicability calendar="Gregorian"><Territory code="AU" /></Applicability>
      <Strategy><Fixed month="May" day="27" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

```csharp
// A single-day query for any day inside the span returns the occurrence.
NotableDate week = service.Resolve(new DateOnly(2026, 5, 30), "AU")
    .Single(d => d.NotableDateId == "national-reconciliation-week");
Console.WriteLine($"{week.Date} – {week.EndDate} ({week.DurationDays} days)");
```

A multi-day occurrence is included in a range query when its span intersects the window; which occurrence controls inclusion is governed by the resource's <xref:Bodu.Globalization.Calendar.RangeResolution.ObservedDateRangePolicy>.

---

## A subdivision variant

When sub-regions observe a holiday on different dates, declare one concept with one `<Rule>` per subdivision, each scoped with `<Territory>`. The engine selects the most-specific rule for the requested territory, so an `AU-VIC` query resolves the Victorian rule and a national `AU` query resolves none of the subdivision rules.

```xml
<NotableDate id="labour-day" displayName="Labour Day" category="PublicHoliday" defaultNonWorkingDay="true">
  <Rules>
    <Rule id="nsw">
      <Applicability calendar="Gregorian"><Territory code="AU-NSW" /></Applicability>
      <Strategy><DayOfWeekInMonth month="October" dayOfWeek="Monday" weekOrdinal="First" /></Strategy>
    </Rule>
    <Rule id="vic">
      <Applicability calendar="Gregorian"><Territory code="AU-VIC" /></Applicability>
      <Strategy><DayOfWeekInMonth month="March" dayOfWeek="Monday" weekOrdinal="Second" /></Strategy>
    </Rule>
    <Rule id="wa">
      <Applicability calendar="Gregorian"><Territory code="AU-WA" /></Applicability>
      <Strategy><DayOfWeekInMonth month="March" dayOfWeek="Monday" weekOrdinal="First" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

<!-- compile -->
```csharp
NotableDateService service = AsiaPacificCalendarData.CreateService("AU-VIC");

NotableDate vicLabour = service.Resolve(2026, "AU-VIC").Single(d => d.NotableDateId == "labour-day");
Console.WriteLine($"{vicLabour.Date}  {vicLabour.DisplayName} (Victoria)");
```

A rule scoped to a country (`<Territory code="AU" />`) also resolves for every subdivision query, so national and regional rules compose: a national rule plus a subdivision-specific shadow rule both surface for the subdivision under the resource's collision policy. See [Territories and regional composition](territories.md).

### Year-bounded subdivision variant

Combine `<Territory>` with `fromYear` / `toYear` (or `<OnlyYear>` / `<ExceptYear>`) on `<Applicability>` to gate a rule to specific years — for example a trial public holiday active only in 2026–2027:

```xml
<Rule id="nsw">
  <Applicability calendar="Gregorian" fromYear="2026" toYear="2027"><Territory code="AU-NSW" /></Applicability>
  <Strategy><Fixed month="April" day="25" /></Strategy>
  <Adjustments><Adjustment policyRef="weekend-roll" /></Adjustments>
</Rule>
```

```csharp
IReadOnlyList<NotableDate> nsw2026 = service.Resolve(2026, "AU-NSW");   // includes the trial rule
IReadOnlyList<NotableDate> nsw2030 = service.Resolve(2030, "AU-NSW");   // outside the window — excluded
```

---

## A periodic (every-n-years) event

For an event that recurs every *n*th year — a quadrennial census day, a leap-year-aligned civic observance — pair `everyYears` with `anchorYear` on `<Applicability>`. The rule is active only in years congruent to `anchorYear` modulo `everyYears`.

```xml
<NotableDate id="census-day" displayName="Census Day" category="Civic" defaultNonWorkingDay="false">
  <Rules>
    <Rule id="au">
      <Applicability calendar="Gregorian" everyYears="5" anchorYear="2021"><Territory code="AU" /></Applicability>
      <Strategy><Fixed month="August" day="10" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

```csharp
IReadOnlyList<NotableDate> in2026 = service.Resolve(2026, "AU");   // 2026 ≡ 2021 (mod 5) → Census Day present
IReadOnlyList<NotableDate> in2027 = service.Resolve(2027, "AU");   // off-cycle → absent
```

`everyYears` combines with `fromYear` / `toYear` and `<OnlyYear>` / `<ExceptYear>` — every applicability constraint must hold for the rule to fire in a given year.

---

## An offset from an algorithmic anchor

Movable feasts that hang off Easter at a longer offset use the same `<OffsetFromRule>` shape as Good Friday — the anchor is computed once by its `<Algorithm>` rule and every dependant tracks it. Corpus Christi is Easter Sunday + 60 days:

```xml
<NotableDate id="corpus-christi" displayName="Corpus Christi" category="Religious" defaultNonWorkingDay="true">
  <Rules>
    <Rule id="default">
      <Strategy><OffsetFromRule notableDateRef="easter-sunday" ruleRef="default" offsetDays="60" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

```csharp
NotableDate corpus = service.Resolve(2026, "DE-BY").Single(d => d.NotableDateId == "corpus-christi");
Console.WriteLine($"{corpus.Date}  {corpus.DisplayName}");   // tracks the western-easter anchor
```

When the anchor rule produces no occurrence for the year, the offset rule produces none either; references are resolved cycle-safely within the resource.

---

## Where to go next

- [NotableDateRule and adjustment-policy reference](rule-reference.md) — the element-by-element field reference for every fragment above.
- [Observance adjustment rules](adjustment-rules.md) — the full trigger / action / emission catalogues for `<AdjustmentPolicy>`.
- [Authoring notable date rules](rule-authoring.md) — assembling whole documents, importing the common catalogues, and overrides.
- [Date calculation algorithms](algorithms.md) — the six strategies and the built-in `<Algorithm>` keys.
- [Working with non-Gregorian calendars](non-gregorian-calendars.md) — fixed dates in Hijri / Hebrew / Persian / Chinese lunisolar calendars.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
