---
title: NotableDateRule and ObservanceAdjustment reference
---

# NotableDateRule and ObservanceAdjustment reference

This page is the authoritative field-by-field reference for the two core rule-authoring types:
`NotableDateRule` (the authored recipe that drives date resolution) and `ObservanceAdjustment`
(the conditional shift that turns a *nominal* date into an *observed* date). For the underlying
vocabulary, start with [Core concepts](../../docs/calendar/concepts.md). For loading mechanics,
see [Authoring notable date rules](rule-authoring.md). For a step-by-step description of how
the service processes these types, see [The resolution pipeline](resolution-pipeline.md).

---

## NotableDateRule

`NotableDateRule` is an immutable record that describes a single variant of a notable date.
Every public property listed below may be set in the record initialiser; unset properties take
their documented defaults.

### Identity fields

| Field | Type | Default | Description |
|---|---|---|---|
| `Name` | `string` | — | Canonical notable-date name, e.g. `"Easter Sunday"`. Shared across all variants of the same notable date. Must not be null or empty. |
| `RuleName` | `string?` | `null` | Distinguishes this variant from others under the same `Name`. Required when a notable date has more than one `NotableDateRule`. Corresponds to the `name` attribute on a `<Rule>` element in XML. |
| `Category` | `NotableDateCategory` | `None` | Semantic classification — see [NotableDateCategory](#notabledatecategory) below. |
| `Tags` | `ImmutableHashSet<string>` | empty | Non-exclusive labels for cross-cutting classification, e.g. `"Christian"`, `"Federal"`, `"BankHoliday"`. Filter with `NotableDateFilter.WithTag` / `WithAnyTag` / `WithAllTags`. |
| `Comment` | `string?` | `null` | Authoring annotation carried through to `NotableDate.Comment`. Not exposed as a public query field but available on the result. |
| `Priority` | `int` | `100` | Tie-break when two rules resolve to the same date. Lower value wins. Used by the collision resolver. |

### Strategy selector

`Strategy` is a `DateResolutionStrategy` enum value that selects the resolution path and
determines which other fields are active:

| Value | Active fields | Use for |
|---|---|---|
| `Fixed` | `Month`, `Day` | Dates that always fall on the same month and day every year. |
| `DayOfWeekInMonth` | `Month`, `DayOfWeek`, `WeekOrdinal` | Dates defined as the *n*th occurrence of a weekday within a month. |
| `OffsetFromAnchor` | `AnchorRuleName`, `OffsetDays` | Dates expressed as a signed day offset from another rule's resolved date. |
| `WeekdayNearDate` | `Month`, `Day`, `DayOfWeek`, `WeekdayProximity` | A weekday positioned relative to a fixed reference date — on or after it, on or before it, or nearest to it. |
| `RelativeWeekdayInMonth` | `Month`, `DayOfWeek`, `WeekOrdinal`, `RelativeDayOfWeek`, `WeekdayProximity` | A weekday positioned relative to the *n*th anchor weekday of a month (e.g. the Tuesday after the first Monday in November). |
| `Algorithm` | `AlgorithmKey`, `AlgorithmType`, `AlgorithmMonth`, `AlgorithmDay` | Dates that require an external calculation (Easter, lunar calendars, solar terms). |

### Choosing a strategy

Pick the **simplest strategy that matches how the date is defined**, and reach for `Algorithm`
only when nothing else fits — an unresolved algorithm key produces no occurrence silently, so a
declarative strategy is always preferable when one applies. Work down this list and take the
first match:

1. **Same month and day every year** (Gregorian, or another calendar via `CalendarType`) → **`Fixed`**.
   *Christmas Day (25 December), Bastille Day (14 July).*
2. **The *n*th or last occurrence of a weekday in a month**, where that weekday *is* the result → **`DayOfWeekInMonth`**.
   *Third Monday in January (MLK Day), last Monday in May (Memorial Day).*
3. **A fixed number of days from another date that is itself a rule** → **`OffsetFromAnchor`**, so the date tracks its anchor instead of re-deriving it.
   *Good Friday (Easter − 2), Black Friday (Thanksgiving + 1), Cyber Monday (Thanksgiving + 4).*
4. **A weekday on/before/after/nearest a *fixed calendar date*** → **`WeekdayNearDate`**.
   *The Saturday on or after 20 June (Nordic Midsummer), the Wednesday before 23 November (German Repentance Day).*
5. **A *different* weekday on/before/after/nearest the *n*th weekday of a month**, with no anchor rule to offset from → **`RelativeWeekdayInMonth`**.
   *The Tuesday after the first Monday in November (US Election Day).*
6. **Anything astronomical, ecclesiastical, or lunisolar** → **`Algorithm`** with a registered `INotableDateAlgorithm`.
   *Easter Sunday, Vesak, the Japanese equinoxes, Matariki.*

**Disambiguating the weekday strategies** — the *anchor* is the deciding factor:

| Question | Strategy |
|---|---|
| Is the ordinal weekday itself the answer? (e.g. *the* third Monday) | `DayOfWeekInMonth` |
| Is the anchor a fixed month + day? (e.g. on/after 20 June) | `WeekdayNearDate` |
| Is the anchor an ordinal weekday, with a *different* target weekday? (e.g. the Tuesday after the first Monday) | `RelativeWeekdayInMonth` |
| Is the anchor already modelled as its own rule? (e.g. the Monday after Thanksgiving) | `OffsetFromAnchor` (preferred over the two above) |

> [!NOTE]
> "The next weekday after a known weekday" is always a *fixed* offset, so `RelativeWeekdayInMonth`
> and `OffsetFromAnchor` can describe the same date. Use `OffsetFromAnchor` whenever the anchor
> exists as a rule (it tracks that rule); use `RelativeWeekdayInMonth` only when the ordinal-weekday
> anchor is *not* itself modelled (US Election Day has no "first Monday of November" rule to offset from).

### Fixed strategy fields

| Field | Type | Default | Description |
|---|---|---|---|
| `Month` | `int` | `0` | Calendar month (1–12). |
| `Day` | `int` | `0` | Day of month (1–31). |

### DayOfWeekInMonth strategy fields

| Field | Type | Default | Description |
|---|---|---|---|
| `Month` | `int` | `0` | Calendar month (1–12). |
| `DayOfWeek` | `DayOfWeek?` | `null` | The target weekday. |
| `WeekOrdinal` | `WeekOfMonthOrdinal?` | `null` | Which occurrence: `First`, `Second`, `Third`, `Fourth`, `Fifth`, or `Last`. `Last` resolves to the final occurrence in the month regardless of whether it is the fourth or fifth. |

### OffsetFromAnchor strategy fields

| Field | Type | Default | Description |
|---|---|---|---|
| `AnchorRuleName` | `string?` | `null` | The `Name` of the rule whose resolved date serves as the reference point. The anchor must be present in the same or an earlier provider. |
| `OffsetDays` | `int` | `0` | Signed day offset from the anchor. Negative values move before the anchor; positive values move after. |

The resolver detects and rejects circular chains — a rule may not directly or transitively
anchor to itself.

### WeekdayNearDate strategy fields

| Field | Type | Default | Description |
|---|---|---|---|
| `Month` | `int` | `0` | Reference month (1–12). |
| `Day` | `int` | `0` | Reference day of month (1–31). |
| `DayOfWeek` | `DayOfWeek?` | `null` | The target weekday the rule resolves to. |
| `WeekdayProximity` | `WeekdayProximity?` | `null` | How the target weekday is positioned relative to the reference: `OnOrAfter`, `OnOrBefore`, or `Nearest`. |

Because a weekday recurs every seven days, each direction selects a single, unambiguous
occurrence within a seven-day window anchored at the reference date:

- **`OnOrAfter`** — the reference date itself when it already falls on the target weekday,
  otherwise the first such weekday in the following six days. Models "the Saturday between
  20 and 26 June" (Nordic Midsummer Day) as the Saturday *on or after* 20 June.
- **`OnOrBefore`** — the reference date itself when it already falls on the target weekday,
  otherwise the most recent such weekday in the preceding six days. Models "the Wednesday
  before 23 November" (German Repentance Day) as the Wednesday *on or before* 22 November.
- **`Nearest`** — the closest occurrence in either direction. The forward and backward
  distances always sum to seven, so they are never equal and the nearest occurrence is
  unique. Models "the Monday nearest to" a given date.

This strategy resolves entirely from data, so holidays of this shape need no custom
`INotableDateAlgorithm`. When the reference (year, month, day) is not a valid Gregorian date
(for example 29 February in a non-leap year) the rule resolves to no occurrence for that year.

### RelativeWeekdayInMonth strategy fields

| Field | Type | Default | Description |
|---|---|---|---|
| `Month` | `int` | `0` | The anchor month (1–12). |
| `DayOfWeek` | `DayOfWeek?` | `null` | The anchor weekday — combined with `WeekOrdinal` and `Month` it identifies the reference occurrence (exactly as in `DayOfWeekInMonth`). |
| `WeekOrdinal` | `WeekOfMonthOrdinal?` | `null` | Which occurrence of the anchor weekday: `First`, `Second`, `Third`, `Fourth`, `Fifth`, or `Last`. |
| `RelativeDayOfWeek` | `DayOfWeek?` | `null` | The target weekday the rule resolves to. |
| `WeekdayProximity` | `WeekdayProximity?` | `null` | How the target weekday is positioned relative to the anchor: `OnOrAfter`, `OnOrBefore`, or `Nearest`. |

The strategy first computes the anchor — the `WeekOrdinal`-th `DayOfWeek` of `Month` — then
positions `RelativeDayOfWeek` relative to it using the same window semantics as
`WeekdayNearDate`. For example, "the Tuesday after the first Monday in November" (United States
Election Day) is the anchor *first Monday in November* with `RelativeDayOfWeek = Tuesday` and
`WeekdayProximity = OnOrAfter`. When the anchor occurrence does not exist (a `Fifth` in a month
with only four) the rule resolves to no occurrence for that year.

### Algorithm strategy fields

| Field | Type | Default | Description |
|---|---|---|---|
| `AlgorithmKey` | `string?` | `null` | String key used to look up the `INotableDateAlgorithm` in the `NotableDateAlgorithmRegistry`. |
| `AlgorithmType` | `string?` | `null` | Assembly-qualified type name of the algorithm. Used as a fallback when `AlgorithmKey` is not registered. |
| `AlgorithmMonth` | `int` | `0` | Optional month hint passed to algorithms that accept a calendar-system month (e.g. `HinduLunarNotableDateAlgorithm`). |
| `AlgorithmDay` | `int` | `0` | Optional day hint for the same purpose. |

Prefer `AlgorithmKey` over `AlgorithmType` — key-based lookup is simpler and more testable.

### Scoping fields

| Field | Type | Default | Description |
|---|---|---|---|
| `TerritoryCode` | `TerritoryCode?` | `null` | ISO 3166-1 alpha-2 country code or ISO 3166-2 subdivision code this rule applies to. `null` means the rule applies globally. See [TerritoryCode scoping](#territorycode-scoping) below. |
| `CalendarType` | `Type?` | `null` | The `System.Globalization.Calendar`-derived type the rule's `Month` and `Day` are authored against (e.g. `System.Globalization.HijriCalendar`, `System.Globalization.HebrewCalendar`, `System.Globalization.PersianCalendar`, `System.Globalization.UmAlQuraCalendar`, `System.Globalization.ChineseLunisolarCalendar`). When `null`, the rule is Gregorian and the resolver constructs the date directly without calendar conversion. See [Working with non-Gregorian calendars](non-gregorian-calendars.md). |
| `FirstYear` | `int?` | `null` | Inclusive first year the rule is active. Rules outside the year bounds are skipped. |
| `LastYear` | `int?` | `null` | Inclusive last year the rule is active. |
| `OccurrenceYears` | `ImmutableHashSet<int>` | empty | Explicit set of years this rule applies to. When non-empty, the rule is only resolved for years in the set, regardless of `FirstYear` / `LastYear`. Useful for jubilee events or irregular one-off observances. |

### Behaviour flags

| Field | Type | Default | Description |
|---|---|---|---|
| `IsNonWorkingDay` | `bool` | `false` | Marks the resulting `NotableDate` as a non-working day. Affects `NotableDateService.IsNonWorkingDay` and working-day arithmetic in the extension methods. |
| `DurationDays` | `int` | `1` | Number of calendar days the event spans, inclusive of the anchor date. Multi-day events set `NotableDate.EndDate = Date + DurationDays - 1` (clamped to `DateTime.MaxValue`). The XML and JSON schemas constrain authored values to **1–366**; schema validation rejects anything outside that range. |

### Calendar-system fields

These fields are relevant only for Fixed-strategy rules whose `CalendarType` is non-null
(i.e. authored against a non-Gregorian calendar). See
[Working with non-Gregorian calendars](non-gregorian-calendars.md) for the full pipeline
behaviour and worked examples.

| Field | Type | Default | Description |
|---|---|---|---|
| `SweepCalendarYears` | `bool` | `false` | When `true`, the resolver evaluates the rule's authored (month, day) against both candidate **calendar** years that overlap the requested **Gregorian** year, returning the first projection whose Gregorian year matches the request. Required for `HijriCalendar`, `UmAlQuraCalendar`, `HebrewCalendar`, and `PersianCalendar` — every supported non-Gregorian calendar except `ChineseLunisolarCalendar`. Ignored when `CalendarType` is `null` or `GregorianCalendar`. |
| `SkipLeapMonth` | `bool` | `false` | When `true` and `CalendarType` is `ChineseLunisolarCalendar`, the resolver maps the conventional ordinal lunar month to the calendar's consecutive month numbering by advancing past any intercalary leap month that precedes it. Used for festivals such as the Dragon Boat Festival defined against the conventional fifth lunar month. |
| `CalendarMonthAlias` | `string?` | `null` | Hebrew-only stable month name (e.g. `"Tishri"`, `"LastAdar"`, `"Nisan"`) used in place of a numeric `Month`. The resolver maps the alias to the correct numeric month for each candidate Hebrew year, handling the leap-year renumbering of Adar / Nisan / Iyar / Sivan / Tammuz / Av / Elul automatically. Only consulted by the calendar-year sweep path. |

### Adjustments

`Adjustments` is an `ImmutableArray<ObservanceAdjustment>`. The array may be empty (no
adjustments) or contain any number of adjustments evaluated in ascending `Priority` order
after the anchor date is resolved. See [ObservanceAdjustment](#observanceadjustment) below and
[Observance adjustment rules](adjustment-rules.md) for full behaviour.

---

## NotableDateCategory

`NotableDateCategory` classifies a notable date at the rule level. The value is carried
through unchanged to `NotableDate.Category`.

| Value | Intended use |
|---|---|
| `None` | No category assigned. |
| `Holiday` | Public or statutory holiday with legal status in the territory. |
| `Observance` | Recognised observance without statutory non-working status (e.g. Remembrance Day in some territories). |
| `Remembrance` | Day of national remembrance or mourning. |
| `Cultural` | Cultural event or celebration (e.g. St Patrick's Day, Bastille Day). |
| `Religious` | Religious festival or feast day (e.g. Easter Sunday, Diwali). |
| `Seasonal` | Season boundary or solstice/equinox event. |
| `Civic` | Civic or government-driven observance (e.g. election day, census day). |
| `Bank` | Bank holiday as a distinct category from a public holiday. |
| `School` | School term boundary or teacher-only day. |
| `Regional` | Sub-national regional event not classified under another category. |
| `Other` | Catch-all for dates that do not fit the above categories. |

---

## TerritoryCode scoping

`TerritoryCode` is a readonly struct representing an ISO 3166-1 alpha-2 country code with
an optional ISO 3166-2 subdivision component (e.g. `"AU"`, `"AU-NSW"`, `"US-CA"`).

**Containment rule:** when a caller requests dates for territory `X`, the service returns
all rules whose `TerritoryCode`:

- is `null` (global — applies to every territory)
- equals `X` exactly (e.g. `"AU-NSW"` matches only an `"AU-NSW"` query)
- is a parent of `X` (e.g. `"AU"` is returned for an `"AU-NSW"` query)

A query for `"AU"` therefore includes both country-level (`"AU"`) dates and all subdivision
dates (`"AU-NSW"`, `"AU-VIC"`, …). A query for `"AU-NSW"` includes `"AU-NSW"` and global
dates but not `"AU-VIC"`.

```csharp
// Parsing
TerritoryCode au    = TerritoryCode.Parse("AU");
TerritoryCode nsw   = TerritoryCode.Parse("AU-NSW");

// Properties
string country      = au.Country;        // "AU"
bool hasSub         = nsw.HasSubdivision; // true
string sub          = nsw.Subdivision;    // "NSW"

// Containment check
bool contained = au.Contains(nsw); // true — AU is a parent of AU-NSW
```

The `TerritoryCode` property on `NotableDateRule` accepts an implicit conversion from
`string`, so `TerritoryCode = "AU-NSW"` in a record initialiser is valid.

The same scoping semantics apply to `ObservanceAdjustment.TerritoryCode` — an adjustment
scoped to `"AU"` fires for both `"AU"` and `"AU-NSW"` queries.

---

## ObservanceAdjustment

`ObservanceAdjustment` is an immutable record that shifts an anchor date when a
trigger condition fires. Adjustments are attached to a `NotableDateRule` via the
`Adjustments` array.

### Identity and ordering fields

| Field | Type | Default | Description |
|---|---|---|---|
| `Key` | `string?` | `null` | Authoring identifier. Not used for lookup but appears in `AdjustmentReason.HandlerKey` when a custom handler fires. Useful for audit logging. |
| `Priority` | `int` | `100` | Evaluation order within the rule's adjustment array. Lower value evaluated first. The first adjustment whose trigger fires wins; subsequent adjustments are skipped. |

### Trigger field

`Trigger` is an `AdjustmentTrigger` enum value. See [Trigger catalogue](adjustment-rules.md#trigger-catalogue) for the full description of when each value fires. The fields that become active alongside each trigger are listed here:

| Trigger | Required companion fields |
|---|---|
| `Always` | *(none)* |
| `IfWeekend` | *(none — uses configured `CalendarWeekendDefinition`)* |
| `IfWeekday` | *(none)* |
| `IfDayOfWeek` | `DayOfWeek` |
| `IfNonWorkingDay` | *(none — uses resolved non-working-day set at time of evaluation)* |
| `IfLeapYear` | *(none)* |
| `IfNthOccurrenceInMonth` | `DayOfWeek`, `WeekOrdinal` |
| `IfBeforeFixedDate` | `ComparisonDate` |
| `IfAfterFixedDate` | `ComparisonDate` |
| `Custom` | `HandlerKey`, optionally `HandlerParameters` |

### Action field

`Action` is an `AdjustmentAction` enum value. See [Action catalogue](adjustment-rules.md#action-catalogue) for the full description of what each value does. The fields that become active with each action are listed here:

| Action | Required companion fields |
|---|---|
| `None` | *(none — trigger recorded but date unchanged)* |
| `AddDays` | `OffsetDays` |
| `MoveToNextWeekday` | *(none)* |
| `MoveToPreviousWeekday` | *(none)* |
| `MoveToNextWorkingDay` | *(none — legacy token `MoveToNextNonWorkingDay` still accepted by the parsers)* |
| `ReplaceWithNamedDate` | `TargetRuleName`, optionally `TargetRuleVariant` |
| `Custom` | `HandlerKey`, optionally `HandlerParameters` |

### Condition companion fields

| Field | Type | Default | Description |
|---|---|---|---|
| `DayOfWeek` | `DayOfWeek?` | `null` | Weekday used by `IfDayOfWeek` and `IfNthOccurrenceInMonth` triggers. |
| `WeekOrdinal` | `WeekOfMonthOrdinal?` | `null` | Which occurrence used by the `IfNthOccurrenceInMonth` trigger. |
| `IsNonWorkingDay` | `bool` | `false` | Context hint for the `IfNonWorkingDay` trigger. Not usually set directly — the trigger evaluates the live non-working-day context. |
| `ComparisonDate` | `DateOnly?` | `null` | Fixed date used by `IfBeforeFixedDate` and `IfAfterFixedDate` triggers. |
| `OffsetDays` | `int` | `0` | Day shift used by the `AddDays` action. May be negative. |
| `TargetRuleName` | `string?` | `null` | Name of the rule whose resolved date replaces the anchor when `Action = ReplaceWithNamedDate`. Serialized as the `target` attribute / property. |
| `TargetRuleVariant` | `string?` | `null` | Optional `RuleName` of the `ReplaceWithNamedDate` target, disambiguating when several rules share the canonical `TargetRuleName`. Resolved against the active territory / calendar context when `null`. Programmatic-only — not part of the XML / JSON schema. |
| `MaxAdjustmentReachDays` | `int?` | `null` | Optional symmetric envelope (±days) the range-resolution pipeline uses to size its fringe scan around this rule. `null` falls back to the action's default heuristic (e.g. `MoveToNextWorkingDay` ≈ +7 days). Serialized as the `maxReachDays` attribute / property. |

### Scoping fields

| Field | Type | Default | Description |
|---|---|---|---|
| `TerritoryCode` | `TerritoryCode?` | `null` | Restricts this adjustment to a specific territory. `null` means the adjustment applies regardless of territory. Uses the same containment rule as `NotableDateRule.TerritoryCode`. |
| `CalendarType` | `string?` | `null` | Restricts this adjustment to a specific calendar system. |
| `EffectiveFromYear` | `int?` | `null` | Inclusive first year this adjustment is active. |
| `EffectiveToYear` | `int?` | `null` | Inclusive last year this adjustment is active. |
| `AppliesToGlobalRules` | `bool` | `false` | When this adjustment is territory- or calendar-scoped, controls whether it may also apply to a territory/calendar-neutral (global) rule. `false` (the default) prevents a scoped adjustment from silently applying to a global rule; set `true` to opt in. Serialized as the `appliesToGlobalRules` attribute / property. |

### Custom handler fields

| Field | Type | Default | Description |
|---|---|---|---|
| `HandlerKey` | `string?` | `null` | Key used to look up an `IAdjustmentHandler` in the `AdjustmentHandlerRegistry`. Required when `Trigger = Custom` or `Action = Custom`. |
| `HandlerParameters` | `IReadOnlyDictionary<string,string>?` | `null` | Arbitrary string parameters passed to the handler in `AdjustmentHandlerContext.Parameters`. Serialized as repeated `<Param key="…" value="…"/>` children (XML) or a `handlerParameters` object (JSON). |

---

## NotableDate — reading the output

`NotableDate` is an immutable record returned by `NotableDateService.GetNotableDates`. Each
instance corresponds to one resolved occurrence of a rule for a given year.

| Property | Type | Description |
|---|---|---|
| `Date` | `DateTime` | The resolved anchor date (after adjustments). This is the calendar date the event is observed on. |
| `EndDate` | `DateTime` | The inclusive last day of the event. For a single-day event `EndDate == Date`; for multi-day events `EndDate == Date + DurationDays - 1`. |
| `DurationDays` | `int` | Span in calendar days, inclusive. `1` for single-day events. |
| `Name` | `string` | Canonical English name from `NotableDateRule.Name`. |
| `DisplayName` | `string` | Name optionally qualified by territory and calendar suffix. When a `INotableDateNameLocalizer` is registered and the caller provides a `CultureInfo`, this reflects the localised name. Falls back to `Name` when no localiser is present. |
| `Category` | `NotableDateCategory` | Classification carried from the rule. |
| `TerritoryCode` | `TerritoryCode?` | Territory the date applies to, or `null` for globally scoped dates. |
| `CalendarType` | `string?` | Calendar system the date belongs to, if scoped. |
| `IsNonWorkingDay` | `bool` | `true` when the rule is flagged as a non-working day. Used by `IsNonWorkingDay()` and working-day arithmetic. |
| `WasAdjusted` | `bool` | `true` when at least one `ObservanceAdjustment` fired and moved the date from its raw anchor position. |
| `AdjustmentReason` | `AdjustmentReason?` | Details of the adjustment that fired. `null` when `WasAdjusted` is `false`. |
| `Tags` | `ImmutableHashSet<string>` | Classification tags from the rule. |
| `Comment` | `string?` | Authoring comment from the rule. |

### AdjustmentReason properties

| Property | Type | Description |
|---|---|---|
| `OriginalDate` | `DateTime` | The raw anchor date before adjustment. |
| `Trigger` | `AdjustmentTrigger` | The trigger that fired. |
| `Action` | `AdjustmentAction` | The action that was applied. |
| `HandlerKey` | `string?` | Key of the custom handler, when `Trigger` or `Action` is `Custom`. |

```csharp
foreach (NotableDate date in service.GetNotableDates(2027, "AU"))
{
    Console.WriteLine($"{date.Date:d MMM yyyy}  {date.DisplayName}");

    if (date.DurationDays > 1)
        Console.WriteLine($"  Multi-day: ends {date.EndDate:d MMM yyyy}");

    if (date.WasAdjusted)
    {
        AdjustmentReason reason = date.AdjustmentReason!;
        Console.WriteLine($"  Shifted from {reason.OriginalDate:d MMM yyyy}");
        Console.WriteLine($"  Trigger: {reason.Trigger}  Action: {reason.Action}");
    }
}
```

---

## Strategy examples by holiday type

The sections below show a complete `NotableDateRule` in both C# initialiser form and the
equivalent XML for each major holiday pattern. For fuller coverage of real-world variations,
see [Holiday patterns and examples](holiday-patterns.md).

### Fixed date

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

### Fixed date with weekend substitution

```csharp
using System.Collections.Immutable;
using Bodu.Globalization.Calendar;

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
        Key      = "weekend-roll",
        Priority = 1,
        Trigger  = AdjustmentTrigger.IfWeekend,
        Action   = AdjustmentAction.MoveToNextWeekday,
    }),
};
```

```xml
<NotableDate name="Australia Day">
  <Rule name="Australia Day"
        category="Holiday"
        territory="AU"
        nonWorking="true">
    <Fixed month="January" day="26" />
    <Tag>NationalHoliday</Tag>
    <Adjustment key="weekend-roll" priority="1"
                when="IfWeekend" action="MoveToNextWeekday" />
  </Rule>
</NotableDate>
```

### Floating weekday-of-month

```csharp
using Bodu.Extensions;
using Bodu.Globalization.Calendar;

// Third Monday in January — Martin Luther King Jr. Day (US)
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

### Offset from an anchor

```csharp
using Bodu.Globalization.Calendar;

// Good Friday — 2 days before Easter Sunday
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

// Easter Monday — 1 day after Easter Sunday
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
```

```xml
<NotableDate name="Good Friday">
  <Rule name="Good Friday"
        category="Holiday"
        nonWorking="true">
    <OffsetFromAnchor name="Easter Sunday" offset="-2" />
    <Tag>Christian</Tag>
  </Rule>
</NotableDate>

<NotableDate name="Easter Monday">
  <Rule name="Easter Monday"
        category="Holiday"
        nonWorking="true">
    <OffsetFromAnchor name="Easter Sunday" offset="1" />
    <Tag>Christian</Tag>
  </Rule>
</NotableDate>
```

### Weekday near a reference date

```csharp
using System.Collections.Immutable;
using Bodu.Globalization.Calendar;

// Midsummer Day (Sweden, Finland) — the Saturday between 20 and 26 June,
// i.e. the Saturday on or after 20 June.
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

// Buß- und Bettag (Germany, Saxony) — the Wednesday before 23 November,
// i.e. the Wednesday on or before 22 November.
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
<NotableDate name="Midsummer Day">
  <Rule name="Midsummer Day"
        category="Holiday"
        nonWorking="true">
    <WeekdayNearDate dayOfWeek="Saturday" month="June" day="20" direction="OnOrAfter" />
  </Rule>
</NotableDate>

<NotableDate name="Repentance Day">
  <Rule name="Repentance Day"
        category="Holiday"
        territory="DE-SN"
        nonWorking="true">
    <WeekdayNearDate dayOfWeek="Wednesday" month="November" day="22" direction="OnOrBefore" />
  </Rule>
</NotableDate>
```

### Weekday relative to an ordinal weekday in a month

```csharp
using Bodu.Globalization.Calendar;

// US Election Day — the Tuesday after the first Monday in November.
NotableDateRule electionDay = new NotableDateRule
{
    Name              = "Election Day",
    Strategy          = DateResolutionStrategy.RelativeWeekdayInMonth,
    Category          = NotableDateCategory.Civic,
    Month             = 11,
    DayOfWeek         = DayOfWeek.Monday,     // anchor: the first Monday...
    WeekOrdinal       = WeekOfMonthOrdinal.First,
    RelativeDayOfWeek = DayOfWeek.Tuesday,    // ...then the Tuesday on or after it
    WeekdayProximity  = WeekdayProximity.OnOrAfter,
    TerritoryCode     = "US",
};
```

```xml
<NotableDate name="Election Day">
  <Rule name="Election Day" category="Civic" territory="US">
    <RelativeWeekdayInMonth month="November" weekOrdinal="First" dayOfWeek="Monday"
                            relativeDayOfWeek="Tuesday" direction="OnOrAfter" />
  </Rule>
</NotableDate>
```

### Algorithm-based

```csharp
using System.Collections.Immutable;
using Bodu.Globalization.Calendar;

// Easter Sunday resolved via the registered "easter-sunday" algorithm
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

### Multi-day event

```csharp
using Bodu.Globalization.Calendar;

// Easter weekend — Good Friday through Easter Monday (4 days)
// The anchor is Good Friday; EndDate = Good Friday + 3 days = Easter Monday
NotableDateRule easterWeekend = new NotableDateRule
{
    Name            = "Easter Weekend",
    Strategy        = DateResolutionStrategy.OffsetFromAnchor,
    Category        = NotableDateCategory.Holiday,
    AnchorRuleName  = "Easter Sunday",
    OffsetDays      = -2,   // start on Good Friday
    DurationDays    = 4,    // Good Friday, Easter Saturday, Easter Sunday, Easter Monday
    IsNonWorkingDay = true,
};
```

```xml
<NotableDate name="Easter Weekend">
  <Rule name="Easter Weekend"
        category="Holiday"
        nonWorking="true"
        durationDays="4">
    <OffsetFromAnchor name="Easter Sunday" offset="-2" />
  </Rule>
</NotableDate>
```

### Year-bounded rule

```csharp
using Bodu.Globalization.Calendar;

// A national day established in 2007 and still active
NotableDateRule foundingDay = new NotableDateRule
{
    Name            = "Founding Day",
    Strategy        = DateResolutionStrategy.Fixed,
    Category        = NotableDateCategory.Holiday,
    Month           = 9,
    Day             = 23,
    TerritoryCode   = "SA",
    IsNonWorkingDay = true,
    FirstYear       = 2007,
};
```

```xml
<NotableDate name="Founding Day">
  <Rule name="Founding Day"
        category="Holiday"
        territory="SA"
        nonWorking="true"
        firstYear="2007">
    <Fixed month="September" day="23" />
  </Rule>
</NotableDate>
```

---

## Where to go next

- [Observance adjustment rules](adjustment-rules.md) — the full trigger and action catalogues, chaining, scoping, and custom handlers.
- [The resolution pipeline](resolution-pipeline.md) — how rules and adjustments are processed to produce `NotableDate` results.
- [Holiday patterns and examples](holiday-patterns.md) — end-to-end examples for common real-world holiday types.
- [Building and extending the service](building-the-service.md) — registries, filter composition, override providers, and extension points.
