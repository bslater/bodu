---
title: Holiday patterns and examples
---

# Holiday patterns and examples

This page provides end-to-end examples for common real-world holiday types. Each section
shows the `NotableDateRule` in both C# and XML form, explains why a particular strategy and
adjustment combination is used, and calls out variations to watch for.

For field definitions, see [NotableDateRule and ObservanceAdjustment reference](rule-reference.md).
For the full adjustment trigger and action catalogues, see [Observance adjustment rules](adjustment-rules.md).

---

## Fixed-date national holidays

Fixed-date holidays fall on the same month and day every year. `Strategy = Fixed` with
`Month` and `Day` is the correct choice. No adjustment is required unless the territory
substitutes the holiday when it falls on a weekend.

### Christmas Day (global, no substitution)

```csharp
using System.Collections.Immutable;
using Bodu.Globalization.Calendar;

NotableDateRule christmas = new NotableDateRule
{
    Name            = "Christmas Day",
    Strategy        = DateResolutionStrategy.Fixed,
    Category        = NotableDateCategory.Holiday,
    Month           = 12,
    Day             = 25,
    IsNonWorkingDay = true,
    Tags            = ImmutableHashSet.Create("Christian"),
};
```

```xml
<NotableDate name="Christmas Day">
  <Rule name="Christmas Day"
        category="Holiday"
        nonWorking="true">
    <Fixed month="December" day="25" />
    <Tag>Christian</Tag>
  </Rule>
</NotableDate>
```

### New Year's Day with weekend substitution

Many territories substitute a Monday when New Year's Day falls on a Saturday or Sunday.
A single `IfWeekend → MoveToNextWeekday` adjustment covers both cases because
`MoveToNextWeekday` always advances to the following Monday.

```csharp
using System.Collections.Immutable;
using Bodu.Globalization.Calendar;

NotableDateRule newYearsDay = new NotableDateRule
{
    Name            = "New Year's Day",
    Strategy        = DateResolutionStrategy.Fixed,
    Category        = NotableDateCategory.Holiday,
    Month           = 1,
    Day             = 1,
    IsNonWorkingDay = true,
    Adjustments     = ImmutableArray.Create(new ObservanceAdjustment
    {
        Key     = "weekend-roll",
        Trigger = AdjustmentTrigger.IfWeekend,
        Action  = AdjustmentAction.MoveToNextWeekday,
    }),
};
```

```xml
<NotableDate name="New Year's Day">
  <Rule name="New Year's Day"
        category="Holiday"
        nonWorking="true">
    <Fixed month="January" day="1" />
    <Adjustment key="weekend-roll" when="IfWeekend" action="MoveToNextWeekday" />
  </Rule>
</NotableDate>
```

---

## Fixed-date with jurisdiction-specific substitution

Different territories apply different substitution rules to the same underlying date.
Model each jurisdiction as a separate `<Rule>` element (or a separate `NotableDateRule` in
C#) under the same canonical notable-date name.

### Australia Day (AU) — Saturday or Sunday moves to Monday

```csharp
NotableDateRule australiaDay = new NotableDateRule
{
    Name            = "Australia Day",
    Strategy        = DateResolutionStrategy.Fixed,
    Category        = NotableDateCategory.Holiday,
    Month           = 1,
    Day             = 26,
    TerritoryCode   = "AU",
    IsNonWorkingDay = true,
    Tags            = ImmutableHashSet.Create("NationalHoliday"),
    Adjustments     = ImmutableArray.Create(new ObservanceAdjustment
    {
        Key     = "weekend-roll",
        Trigger = AdjustmentTrigger.IfWeekend,
        Action  = AdjustmentAction.MoveToNextWeekday,
    }),
};
```

### UK Christmas Day — two-step UK bank holiday roll

UK legislation gives each calendar day its own substitute, so Saturday and Sunday produce
different outcomes. Two `IfDayOfWeek` adjustments with different priorities model this:

```csharp
NotableDateRule ukChristmas = new NotableDateRule
{
    Name            = "Christmas Day",
    RuleName        = "Christmas Day (UK)",
    Strategy        = DateResolutionStrategy.Fixed,
    Category        = NotableDateCategory.Holiday,
    Month           = 12,
    Day             = 25,
    TerritoryCode   = "GB",
    IsNonWorkingDay = true,
    Tags            = ImmutableHashSet.Create("Christian", "BankHoliday"),
    Adjustments     = ImmutableArray.Create(
        new ObservanceAdjustment
        {
            Key       = "sat-to-mon",
            Priority  = 1,
            Trigger   = AdjustmentTrigger.IfDayOfWeek,
            DayOfWeek = DayOfWeek.Saturday,
            Action    = AdjustmentAction.AddDays,
            OffsetDays = 2,  // Saturday + 2 → Monday
        },
        new ObservanceAdjustment
        {
            Key       = "sun-to-tue",
            Priority  = 2,
            Trigger   = AdjustmentTrigger.IfDayOfWeek,
            DayOfWeek = DayOfWeek.Sunday,
            Action    = AdjustmentAction.AddDays,
            OffsetDays = 2,  // Sunday + 2 → Tuesday
        }
    ),
};
```

```xml
<NotableDate name="Christmas Day">
  <!-- UK bank holiday roll: Saturday → Monday; Sunday → Tuesday -->
  <Rule name="Christmas Day (UK)"
        category="Holiday"
        territory="GB"
        nonWorking="true">
    <Fixed month="December" day="25" />
    <Tag>Christian</Tag>
    <Tag>BankHoliday</Tag>
    <Adjustment key="sat-to-mon" priority="1"
                when="IfDayOfWeek" dayOfWeek="Saturday"
                action="AddDays" offset="2" />
    <Adjustment key="sun-to-tue" priority="2"
                when="IfDayOfWeek" dayOfWeek="Sunday"
                action="AddDays" offset="2" />
  </Rule>
</NotableDate>
```

### US "observed" pattern — Saturday → Friday, Sunday → Monday

The federal US convention shifts Saturday holidays to the preceding Friday and Sunday
holidays to the following Monday:

```csharp
NotableDateRule usIndependenceDay = new NotableDateRule
{
    Name            = "Independence Day",
    Strategy        = DateResolutionStrategy.Fixed,
    Category        = NotableDateCategory.Holiday,
    Month           = 7,
    Day             = 4,
    TerritoryCode   = "US",
    IsNonWorkingDay = true,
    Tags            = ImmutableHashSet.Create("Federal"),
    Adjustments     = ImmutableArray.Create(
        new ObservanceAdjustment
        {
            Key       = "sat-to-fri",
            Priority  = 1,
            Trigger   = AdjustmentTrigger.IfDayOfWeek,
            DayOfWeek = DayOfWeek.Saturday,
            Action    = AdjustmentAction.MoveToPreviousWeekday,
        },
        new ObservanceAdjustment
        {
            Key       = "sun-to-mon",
            Priority  = 2,
            Trigger   = AdjustmentTrigger.IfDayOfWeek,
            DayOfWeek = DayOfWeek.Sunday,
            Action    = AdjustmentAction.MoveToNextWeekday,
        }
    ),
};
```

```xml
<NotableDate name="Independence Day">
  <Rule name="Independence Day (US)"
        category="Holiday"
        territory="US"
        nonWorking="true">
    <Fixed month="July" day="4" />
    <Tag>Federal</Tag>
    <Adjustment key="sat-to-fri" priority="1"
                when="IfDayOfWeek" dayOfWeek="Saturday"
                action="MoveToPreviousWeekday" />
    <Adjustment key="sun-to-mon" priority="2"
                when="IfDayOfWeek" dayOfWeek="Sunday"
                action="MoveToNextWeekday" />
  </Rule>
</NotableDate>
```

---

## Floating weekday-of-month holidays

Some holidays are defined as the *n*th occurrence of a particular weekday in a given month
rather than a fixed calendar date. Use `Strategy = DayOfWeekInMonth` with `Month`,
`DayOfWeek`, and `WeekOrdinal`.

### Third Monday in January — Martin Luther King Jr. Day (US)

```csharp
using Bodu.Extensions;
using Bodu.Globalization.Calendar;

NotableDateRule mlkDay = new NotableDateRule
{
    Name            = "Martin Luther King Jr. Day",
    Strategy        = DateResolutionStrategy.DayOfWeekInMonth,
    Category        = NotableDateCategory.Holiday,
    Month           = 1,
    DayOfWeek       = DayOfWeek.Monday,
    WeekOrdinal     = WeekOfMonthOrdinal.Third,
    TerritoryCode   = "US",
    IsNonWorkingDay = true,
    FirstYear       = 1986,
    Tags            = ImmutableHashSet.Create("Federal"),
};
```

```xml
<NotableDate name="Martin Luther King Jr. Day">
  <Rule name="Martin Luther King Jr. Day"
        category="Holiday"
        territory="US"
        nonWorking="true"
        firstYear="1986">
    <DayOfWeekInMonth month="January" dayOfWeek="Monday" weekOrdinal="Third" />
    <Tag>Federal</Tag>
  </Rule>
</NotableDate>
```

### Fourth Thursday in November — Thanksgiving (US)

```csharp
NotableDateRule thanksgiving = new NotableDateRule
{
    Name            = "Thanksgiving Day",
    Strategy        = DateResolutionStrategy.DayOfWeekInMonth,
    Category        = NotableDateCategory.Holiday,
    Month           = 11,
    DayOfWeek       = DayOfWeek.Thursday,
    WeekOrdinal     = WeekOfMonthOrdinal.Fourth,
    TerritoryCode   = "US",
    IsNonWorkingDay = true,
    Tags            = ImmutableHashSet.Create("Federal"),
};
```

```xml
<NotableDate name="Thanksgiving Day">
  <Rule name="Thanksgiving Day (US)"
        category="Holiday"
        territory="US"
        nonWorking="true">
    <DayOfWeekInMonth month="November" dayOfWeek="Thursday" weekOrdinal="Fourth" />
    <Tag>Federal</Tag>
  </Rule>
</NotableDate>
```

### Second Monday in October — Thanksgiving (CA)

Canada's Thanksgiving falls on the second Monday in October — a different month and ordinal
from the US holiday but the same strategy:

```csharp
NotableDateRule canadaThanksgiving = new NotableDateRule
{
    Name            = "Thanksgiving Day",
    RuleName        = "Thanksgiving Day (Canada)",
    Strategy        = DateResolutionStrategy.DayOfWeekInMonth,
    Category        = NotableDateCategory.Holiday,
    Month           = 10,
    DayOfWeek       = DayOfWeek.Monday,
    WeekOrdinal     = WeekOfMonthOrdinal.Second,
    TerritoryCode   = "CA",
    IsNonWorkingDay = true,
};
```

### Last Monday in August — UK Summer Bank Holiday (England and Wales)

`WeekOrdinal = Last` resolves to the final occurrence of the weekday in the month,
regardless of whether it is the fourth or fifth:

```csharp
NotableDateRule ukSummerBankHoliday = new NotableDateRule
{
    Name            = "Summer Bank Holiday",
    Strategy        = DateResolutionStrategy.DayOfWeekInMonth,
    Category        = NotableDateCategory.Holiday,
    Month           = 8,
    DayOfWeek       = DayOfWeek.Monday,
    WeekOrdinal     = WeekOfMonthOrdinal.Last,
    TerritoryCode   = "GB-ENG",
    IsNonWorkingDay = true,
    Tags            = ImmutableHashSet.Create("BankHoliday"),
};
```

```xml
<NotableDate name="Summer Bank Holiday">
  <Rule name="Summer Bank Holiday (England and Wales)"
        category="Holiday"
        territory="GB-ENG"
        nonWorking="true">
    <DayOfWeekInMonth month="August" dayOfWeek="Monday" weekOrdinal="Last" />
    <Tag>BankHoliday</Tag>
  </Rule>
</NotableDate>
```

---

## Weekday near a reference date

Some holidays are a weekday positioned relative to a fixed reference date rather than an
*n*th-of-the-month occurrence: "the Saturday between 20 and 26 June", "the Wednesday before
23 November", or "the Monday nearest to" a date. These cannot be expressed with
`DayOfWeekInMonth` because the target is not a fixed ordinal — and the All Saints' window even
straddles a month boundary. Use `Strategy = WeekdayNearDate` with `Month`, `Day`, `DayOfWeek`,
and a `WeekdayProximity` direction (`OnOrAfter`, `OnOrBefore`, or `Nearest`). The reference
date plus the direction defines the seven-day window in which the single matching weekday is
selected.

### Midsummer Day (SE, FI) — the Saturday on or after 20 June

The Saturday falling between 20 and 26 June is the first Saturday on or after 20 June.

```csharp
using Bodu.Globalization.Calendar;

NotableDateRule midsummerDay = new NotableDateRule
{
    Name             = "Midsummer Day",
    Strategy         = DateResolutionStrategy.WeekdayNearDate,
    Category         = NotableDateCategory.Holiday,
    Month            = 6,
    Day              = 20,
    DayOfWeek        = DayOfWeek.Saturday,
    WeekdayProximity = WeekdayProximity.OnOrAfter,
    IsNonWorkingDay  = true,
};
```

```xml
<NotableDate name="Midsummer Day">
  <Rule name="Midsummer Day" category="Holiday" nonWorking="true">
    <WeekdayNearDate dayOfWeek="Saturday" month="June" day="20" direction="OnOrAfter" />
  </Rule>
</NotableDate>
```

### Repentance Day (DE-SN) — the Wednesday before 23 November

Buß- und Bettag is the Wednesday before 23 November, i.e. the Wednesday on or before 22
November. It is a public holiday only in Saxony.

```csharp
using Bodu.Globalization.Calendar;

NotableDateRule repentanceDay = new NotableDateRule
{
    Name             = "Repentance Day",
    Strategy         = DateResolutionStrategy.WeekdayNearDate,
    Category         = NotableDateCategory.Holiday,
    Month            = 11,
    Day              = 22,
    DayOfWeek        = DayOfWeek.Wednesday,
    WeekdayProximity = WeekdayProximity.OnOrBefore,
    TerritoryCode    = "DE-SN",
    IsNonWorkingDay  = true,
};
```

```xml
<NotableDate name="Repentance Day">
  <Rule name="Repentance Day" category="Holiday" territory="DE-SN" nonWorking="true">
    <WeekdayNearDate dayOfWeek="Wednesday" month="November" day="22" direction="OnOrBefore" />
  </Rule>
</NotableDate>
```

### Monday nearest to a date — the `Nearest` direction

`Nearest` selects the closest occurrence of the weekday in either direction. Because the
forward and backward distances to the same weekday always sum to seven, they are never equal,
so the result is unambiguous.

```xml
<NotableDate name="Example Observed Day">
  <Rule name="Example Observed Day" category="Observance">
    <WeekdayNearDate dayOfWeek="Monday" month="October" day="9" direction="Nearest" />
  </Rule>
</NotableDate>
```

---

## Easter and Easter-relative dates

Easter Sunday is determined by the Gregorian or Orthodox computus algorithm. Easter-relative
dates use `Strategy = OffsetFromAnchor` to express their position in days relative to Easter
Sunday.

### Registering the algorithm

Before Easter-relative rules can resolve, the Easter algorithm must be registered:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;

NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
    .Register("easter-sunday", new EasterSundayNotableDateAlgorithm());
```

### Easter Sunday (Gregorian)

```csharp
NotableDateRule easterSunday = new NotableDateRule
{
    Name            = "Easter Sunday",
    Strategy        = DateResolutionStrategy.Algorithm,
    Category        = NotableDateCategory.Holiday,
    AlgorithmKey    = "easter-sunday",
    IsNonWorkingDay = true,
    Tags            = ImmutableHashSet.Create("Christian"),
};
```

```xml
<NotableDate name="Easter Sunday">
  <Rule name="Easter Sunday"
        category="Holiday"
        nonWorking="true">
    <Algorithm key="easter-sunday" />
    <Tag>Christian</Tag>
  </Rule>
</NotableDate>
```

### Easter cluster — Good Friday through Whit Monday

The following rules express the complete Western Easter cluster. The anchor for each is
`Easter Sunday`; each rule carries a signed `OffsetDays`:

| Holiday | Offset | Notes |
|---|---|---|
| Good Friday | −2 | Non-working in most Christian-tradition territories |
| Easter Saturday | −1 | Non-working in some AU states |
| Easter Sunday | 0 | The algorithm anchor |
| Easter Monday | +1 | Non-working in most territories |
| Ascension Thursday | +39 | Non-working in some European territories |
| Pentecost Sunday | +49 | |
| Whit Monday | +50 | Non-working in some European territories |

```csharp
NotableDateRule goodFriday = new NotableDateRule
{
    Name            = "Good Friday",
    Strategy        = DateResolutionStrategy.OffsetFromAnchor,
    Category        = NotableDateCategory.Holiday,
    AnchorRuleName  = "Easter Sunday",
    OffsetDays      = -2,
    IsNonWorkingDay = true,
    Tags            = ImmutableHashSet.Create("Christian"),
};

NotableDateRule easterMonday = new NotableDateRule
{
    Name            = "Easter Monday",
    Strategy        = DateResolutionStrategy.OffsetFromAnchor,
    Category        = NotableDateCategory.Holiday,
    AnchorRuleName  = "Easter Sunday",
    OffsetDays      = 1,
    IsNonWorkingDay = true,
    Tags            = ImmutableHashSet.Create("Christian"),
};

NotableDateRule ascensionThursday = new NotableDateRule
{
    Name            = "Ascension Thursday",
    Strategy        = DateResolutionStrategy.OffsetFromAnchor,
    Category        = NotableDateCategory.Religious,
    AnchorRuleName  = "Easter Sunday",
    OffsetDays      = 39,
    Tags            = ImmutableHashSet.Create("Christian"),
};

NotableDateRule whitMonday = new NotableDateRule
{
    Name            = "Whit Monday",
    Strategy        = DateResolutionStrategy.OffsetFromAnchor,
    Category        = NotableDateCategory.Holiday,
    AnchorRuleName  = "Easter Sunday",
    OffsetDays      = 50,
    IsNonWorkingDay = true,
    Tags            = ImmutableHashSet.Create("Christian"),
};
```

```xml
<NotableDate name="Good Friday">
  <Rule name="Good Friday" category="Holiday" nonWorking="true">
    <OffsetFromAnchor name="Easter Sunday" offset="-2" />
    <Tag>Christian</Tag>
  </Rule>
</NotableDate>

<NotableDate name="Easter Monday">
  <Rule name="Easter Monday" category="Holiday" nonWorking="true">
    <OffsetFromAnchor name="Easter Sunday" offset="1" />
    <Tag>Christian</Tag>
  </Rule>
</NotableDate>

<NotableDate name="Ascension Thursday">
  <Rule name="Ascension Thursday" category="Religious">
    <OffsetFromAnchor name="Easter Sunday" offset="39" />
    <Tag>Christian</Tag>
  </Rule>
</NotableDate>

<NotableDate name="Whit Monday">
  <Rule name="Whit Monday" category="Holiday" nonWorking="true">
    <OffsetFromAnchor name="Easter Sunday" offset="50" />
    <Tag>Christian</Tag>
  </Rule>
</NotableDate>
```

### Orthodox Easter

The `OrthodoxEasterSundayNotableDateProvider` computes Easter Sunday per the Julian
computus and projects the result to a Gregorian date. Register it under a distinct key to
keep it separate from the Gregorian Easter algorithm:

```csharp
NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
    .Register("easter-sunday",          new EasterSundayNotableDateAlgorithm())
    .Register("orthodox-easter-sunday", new OrthodoxEasterSundayNotableDateAlgorithm());

// Orthodox Easter Sunday rule
NotableDateRule orthodoxEaster = new NotableDateRule
{
    Name            = "Orthodox Easter Sunday",
    Strategy        = DateResolutionStrategy.Algorithm,
    Category        = NotableDateCategory.Religious,
    AlgorithmKey    = "orthodox-easter-sunday",
    IsNonWorkingDay = true,
    Tags            = ImmutableHashSet.Create("Orthodox", "Christian"),
};
```

---

## Lunar and algorithmic dates

Lunar and lunisolar holidays require algorithmic calculation. Register the appropriate
`INotableDateAlgorithm` and wire it via `AlgorithmKey`.

### Chinese New Year (Lunar New Year)

Chinese New Year falls on the second new moon after the winter solstice, which can land in
either January or February of the Gregorian year. `SweepCalendarYears = true` ensures the
resolver checks both the current and adjacent Gregorian years when projecting from the
Chinese calendar:

```csharp
NotableDateRule lunarNewYear = new NotableDateRule
{
    Name              = "Lunar New Year",
    Strategy          = DateResolutionStrategy.Algorithm,
    Category          = NotableDateCategory.Cultural,
    AlgorithmKey      = "lunar-new-year",
    IsNonWorkingDay   = true,
    SweepCalendarYears = true,
    Tags              = ImmutableHashSet.Create("Chinese", "LunarCalendar"),
};
```

```xml
<NotableDate name="Lunar New Year">
  <Rule name="Lunar New Year"
        category="Cultural"
        nonWorking="true"
        sweepCalendarYears="true">
    <Algorithm key="lunar-new-year" />
    <Tag>Chinese</Tag>
    <Tag>LunarCalendar</Tag>
  </Rule>
</NotableDate>
```

### Diwali (Hindu lunar)

`HinduLunarNotableDateAlgorithm` accepts `AlgorithmMonth` and `AlgorithmDay` as hints for
identifying the target festival within the Hindu panchanga:

```csharp
NotableDateRule diwali = new NotableDateRule
{
    Name              = "Diwali",
    Strategy          = DateResolutionStrategy.Algorithm,
    Category          = NotableDateCategory.Religious,
    AlgorithmKey      = "diwali",
    AlgorithmMonth    = 8,   // Kartik (month 8 in the Hindu lunar calendar)
    AlgorithmDay      = 1,   // Amavasya (new moon day)
    SweepCalendarYears = true,
    Tags              = ImmutableHashSet.Create("Hindu"),
};
```

### Vesak (Buddha's Birthday)

```csharp
NotableDateRule vesak = new NotableDateRule
{
    Name            = "Vesak",
    Strategy        = DateResolutionStrategy.Algorithm,
    Category        = NotableDateCategory.Religious,
    AlgorithmKey    = "vesak",
    IsNonWorkingDay = true,
    Tags            = ImmutableHashSet.Create("Buddhist"),
};
```

```xml
<NotableDate name="Vesak">
  <Rule name="Vesak" category="Religious" nonWorking="true">
    <Algorithm key="vesak" />
    <Tag>Buddhist</Tag>
  </Rule>
</NotableDate>
```

### Qingming (Tomb-Sweeping Day)

Qingming falls on the solar term 15° after the Spring Equinox — typically 4 or 5 April:

```csharp
NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
    .Register("qingming", new QingmingNotableDateAlgorithm());

NotableDateRule qingming = new NotableDateRule
{
    Name            = "Qingming",
    Strategy        = DateResolutionStrategy.Algorithm,
    Category        = NotableDateCategory.Cultural,
    AlgorithmKey    = "qingming",
    IsNonWorkingDay = true,
    TerritoryCode   = "CN",
};
```

---

## Multi-day events

Set `DurationDays` to the number of calendar days the event spans, inclusive of the anchor
date. `NotableDate.EndDate` is set to `Date + DurationDays - 1`.

### Hanukkah (8 days)

The anchor is the first day of Hanukkah. The algorithm produces the first night's date; the
event spans eight days:

```csharp
NotableDateRule hanukkah = new NotableDateRule
{
    Name         = "Hanukkah",
    Strategy     = DateResolutionStrategy.Algorithm,
    Category     = NotableDateCategory.Religious,
    AlgorithmKey = "hanukkah",
    DurationDays = 8,
    Tags         = ImmutableHashSet.Create("Jewish"),
};
```

```xml
<NotableDate name="Hanukkah">
  <Rule name="Hanukkah" category="Religious" durationDays="8">
    <Algorithm key="hanukkah" />
    <Tag>Jewish</Tag>
  </Rule>
</NotableDate>
```

### Easter weekend (Good Friday to Easter Monday — 4 days)

Express the multi-day span as a single rule anchored on Good Friday:

```csharp
NotableDateRule easterWeekend = new NotableDateRule
{
    Name            = "Easter Weekend",
    Strategy        = DateResolutionStrategy.OffsetFromAnchor,
    Category        = NotableDateCategory.Holiday,
    AnchorRuleName  = "Easter Sunday",
    OffsetDays      = -2,    // anchor on Good Friday
    DurationDays    = 4,     // Good Friday + Easter Saturday + Easter Sunday + Easter Monday
    IsNonWorkingDay = true,
};
```

Multi-day events are included in range queries when **any** day of their span falls within
the queried range:

```csharp
// Returns Easter Weekend even if 'from' is Easter Saturday
IReadOnlyList<NotableDate> results = service.GetNotableDates(
    new DateTime(2026, 4, 4),   // Easter Saturday 2026
    new DateTime(2026, 4, 10));
```

---

## Subdivision-level variants

When a holiday has different rules in different sub-regions, model each variant as a
separate `<Rule>` element scoped to its territory under the same `<NotableDate>` parent.

### Boxing Day across Australian states

Boxing Day is observed nationally in Australia, but the Northern Territory applies a
standard weekend roll, while other states use a non-working-day check to avoid collision
with Christmas Day substitutes:

```xml
<NotableDate name="Boxing Day">

  <!-- Northern Territory: simple weekend roll -->
  <Rule name="Boxing Day (NT)"
        category="Holiday"
        territory="AU-NT"
        nonWorking="true">
    <Fixed month="December" day="26" />
    <Adjustment key="nt-weekend-roll"
                when="IfWeekend" action="MoveToNextWeekday" />
  </Rule>

  <!-- All other Australian states: skip past any non-working day -->
  <Rule name="Boxing Day (AU)"
        category="Holiday"
        territory="AU"
        nonWorking="true">
    <Fixed month="December" day="26" />
    <Adjustment key="au-nonworking-roll"
                when="IfNonWorkingDay" action="MoveToNextNonWorkingDay" />
  </Rule>

</NotableDate>
```

When resolving for `"AU-NT"`, the first rule matches by containment. If its trigger fires,
the second rule is still evaluated independently (rules are not mutually exclusive between
territories — they are separate rules under the same name). To ensure the NT rule takes
precedence, assign it a lower `Priority` value than the general AU rule.

### Scotland vs England bank holidays

Scotland has different bank holidays from England and Wales. Model them as separately scoped
rules under the same `<NotableDate>` name, using `GB-SCT` and `GB-ENG` territory codes:

```xml
<NotableDate name="St Andrew's Day">
  <Rule name="St Andrew's Day (Scotland)"
        category="Holiday"
        territory="GB-SCT"
        nonWorking="true"
        firstYear="2007">
    <Fixed month="November" day="30" />
    <Adjustment key="weekend-roll" when="IfWeekend" action="MoveToNextWeekday" />
  </Rule>
</NotableDate>
```

---

## Year-bounded and occurrence-filtered rules

### Rule active from a specific year onwards

```csharp
// National holiday established by legislation effective 2007
NotableDateRule founderDay = new NotableDateRule
{
    Name            = "Founder's Day",
    Strategy        = DateResolutionStrategy.Fixed,
    Category        = NotableDateCategory.Holiday,
    Month           = 10,
    Day             = 3,
    TerritoryCode   = "GH",
    IsNonWorkingDay = true,
    FirstYear       = 2007,
};
```

### Rule active for a specific year range only

```csharp
// Special bank holiday for a platinum jubilee, year 2022 only
NotableDateRule jubileeBankHoliday = new NotableDateRule
{
    Name            = "Platinum Jubilee Bank Holiday",
    Strategy        = DateResolutionStrategy.Fixed,
    Category        = NotableDateCategory.Holiday,
    Month           = 6,
    Day             = 3,
    TerritoryCode   = "GB",
    IsNonWorkingDay = true,
    FirstYear       = 2022,
    LastYear        = 2022,
};
```

### Rule applying to specific years only (OccurrenceYears)

Use `OccurrenceYears` when the applicable years are not a continuous range — for example,
an event that occurred in 2002, 2012, 2022, and 2032:

```csharp
NotableDateRule diamondJubilee = new NotableDateRule
{
    Name             = "Diamond Jubilee Bank Holiday",
    Strategy         = DateResolutionStrategy.Fixed,
    Category         = NotableDateCategory.Holiday,
    Month            = 6,
    Day              = 5,
    TerritoryCode    = "GB",
    IsNonWorkingDay  = true,
    OccurrenceYears  = ImmutableHashSet.Create(2002, 2012, 2022, 2032),
};
```

---

## Priority and collision

When two rules resolve to the same date in a given year, both appear in the result by
default. The `DefaultNotableDateCollisionResolver` keeps all distinct entries, ordered by
category then name. Supply a custom `INotableDateCollisionResolver` to change this.

### ANZAC Day and Easter Monday coinciding

In some years ANZAC Day (25 April, Australia) and Easter Monday fall on the same date. Both
are independent rules with their own substitution logic. The default resolver keeps both:

```
25 Apr 2038 — ANZAC Day    (IsNonWorkingDay=true)
25 Apr 2038 — Easter Monday (IsNonWorkingDay=true)
```

Both entries are returned by `GetNotableDates`. To keep only one, supply a
`PriorityCollisionResolver` (see [Building and extending the service](building-the-service.md#inotabledatecollisionresolver)):

```csharp
// Rule with lower Priority value wins
NotableDateRule anzacDay = new NotableDateRule
{
    Name     = "ANZAC Day",
    Priority = 10,   // wins over default priority 100
    // ...
};
```

---

## Where to go next

- [NotableDateRule and ObservanceAdjustment reference](rule-reference.md) — field definitions for every property used above.
- [Observance adjustment rules](adjustment-rules.md) — the full trigger and action catalogues.
- [The resolution pipeline](resolution-pipeline.md) — how rules are processed to produce `NotableDate` results.
- [Building and extending the service](building-the-service.md) — assembling a service with registries, override providers, and collision resolvers.
