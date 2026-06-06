---
title: NotableDateRule and adjustment-policy reference
---

# NotableDateRule and adjustment-policy reference

This page is the authoritative element-by-element reference for the document model on the notable-date schema (`urn:bodu:globalization:calendar`): the `<NotableDate>` concept, the `<Rule>` recipe, the `<Applicability>` filter, each of the six `<Strategy>` elements, and the reusable `<AdjustmentPolicy>` shape. For the vocabulary it assumes, start with [Core concepts](../../docs/calendar/concepts.md). For how to assemble a whole document — imports and overrides — see [Authoring notable date rules](rule-authoring.md). For where the service processes these elements, see [The resolution pipeline](resolution-pipeline.md).

A rule is authored as XML or JSON and loaded into an immutable <xref:Bodu.Globalization.Calendar.NotableDateResource>; the loaded form is exposed through <xref:Bodu.Globalization.Calendar.NotableDateDefinition> and <xref:Bodu.Globalization.Calendar.NotableDateRule>, which are immutable and constructed by the loader — there is no object-initializer authoring API.

---

## `<NotableDate>` — the concept

`<NotableDate>` declares one notable-date concept. It carries an optional `<Tags>` block and a required `<Rules>` block of one or more `<Rule>` elements.

| Attribute | Required | Type | Default | Description |
|---|---|---|---|---|
| `id` | Yes | identifier | — | Stable concept id (lowercase, digits, hyphens), e.g. `easter-sunday`. Unique within the resource. |
| `displayName` | Yes | string | — | Human-readable name surfaced as `NotableDate.DisplayName`. |
| `category` | Yes | category | — | Default <xref:Bodu.Globalization.Calendar.NotableDateCategory> for every rule that does not override it. |
| `defaultDurationDays` | No | positive int | `1` | Default span in days for rules that do not set `durationDays`. |
| `defaultNonWorkingDay` | No | bool | *(unset)* | Default non-working flag inherited by rules that do not set `nonWorking`. |

```xml
<NotableDate id="anzac-day" displayName="Anzac Day" category="Remembrance" defaultNonWorkingDay="true">
  <Tags>
    <Tag value="national" />
  </Tags>
  <Rules>
    <Rule id="default">
      <Strategy><Fixed month="April" day="25" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

A `<Tags>` block contains one or more `<Tag value="..."/>` elements. Concept-level tags apply to every rule; a rule may add its own `<Tags>` block. Tags survive into `NotableDate.Tags` and are matched by `NotableDateFilter.WithTag` / `WithAnyTag` / `WithAllTags`.

---

## `<Rule>` — the recipe

A `<Rule>` is one calculation recipe for its concept. It contains, **in order**, an optional `<Applicability>`, exactly one `<Strategy>`, an optional `<Tags>`, and an optional `<Adjustments>`.

| Attribute | Required | Type | Default | Description |
|---|---|---|---|---|
| `id` | Yes | identifier | — | Distinguishes this rule from its siblings under the same concept (e.g. `default`, `nsw`, `wa`). Targeted by `<OffsetFromRule ruleRef>` and `<Overrides>`. |
| `priority` | No | int | `0` | Tie-break weight when several occurrences share a day; how it is applied is governed by the resource's `priorityDirection`. |
| `category` | No | category | *(concept default)* | Overrides the concept's `category` for this rule. |
| `nonWorking` | No | bool | *(concept default)* | Overrides `defaultNonWorkingDay` for this rule. |
| `durationDays` | No | positive int | *(concept default)* | Overrides `defaultDurationDays` for this rule. |
| `comment` | No | string | — | Authoring annotation; not surfaced to consumers. |

```xml
<Rule id="wa" priority="100" comment="Western Australia observes a substitute Monday when 25 April is a weekend.">
  <Applicability calendar="Gregorian"><Territory code="AU-WA" /></Applicability>
  <Strategy><Fixed month="April" day="25" /></Strategy>
  <Tags><Tag value="public-holiday" /></Tags>
  <Adjustments><Adjustment policyRef="weekend-roll" /></Adjustments>
</Rule>
```

The `<Adjustments>` block holds one or more `<Adjustment policyRef="..."/>` references to policies declared in `<AdjustmentPolicies>`. Adjustments are always referenced by id; see [`<AdjustmentPolicy>`](#adjustmentpolicy--the-reusable-shift) below.

---

## `<Applicability>` — calendar, year, and territory filtering

`<Applicability>` constrains when and where a rule applies. All of its attributes and children are optional; an absent `<Applicability>` means the rule applies in the Gregorian calendar, in every year, and in every territory.

| Attribute | Type | Default | Description |
|---|---|---|---|
| `calendar` | calendar name | `Gregorian` | The calendar the rule's `<Strategy>` is expressed in: `Gregorian`, `Hijri`, `UmmAlQura`, `Hebrew`, `Persian`, `ChineseLunisolar`. |
| `fromYear` | year | — | Inclusive first Gregorian year the rule is active. |
| `toYear` | year | — | Inclusive last Gregorian year the rule is active. |
| `everyYears` | positive int | — | Periodicity — the rule applies every *n*th year, counted from `anchorYear`. |
| `anchorYear` | year | — | The reference year for `everyYears`. |

| Child element | Repeats | Description |
|---|---|---|
| `<Territory code="..."/>` | Yes | Scopes the rule to an ISO 3166 country or subdivision (`AU`, `AU-NSW`). Multiple entries broaden the scope. A rule with no `<Territory>` applies to every territory. |
| `<OnlyYear value="..."/>` | Yes | Restricts the rule to the listed year(s) only. |
| `<ExceptYear value="..."/>` | Yes | Excludes the listed year(s) from an otherwise-applicable range. |

```xml
<!-- A trial public holiday active only in 2026 and 2027, scoped to New South Wales. -->
<Applicability calendar="Gregorian" fromYear="2026" toYear="2027">
  <Territory code="AU-NSW" />
</Applicability>
```

Territory scoping is **hierarchical**: a rule scoped to `AU` resolves for an `AU` query and for any `AU-XXX` subdivision query; a rule scoped to `AU-NSW` resolves only for `AU-NSW` (and an `AU` query that enumerates its subdivisions). When several rules of one concept match a territory, the engine selects the most specific. See [Territories and regional composition](territories.md).

---

## Strategy elements

Every rule carries exactly one `<Strategy>` child, which is exactly one of the six elements below. Each maps to a public <xref:Bodu.Globalization.Calendar.Algorithms.IDateCalculationStrategy>. Pick the simplest strategy that matches how the date is defined; reach for `<Algorithm>` only when the date cannot be expressed as calendar arithmetic. The strategy kinds are also covered, with the engine's view, in [Date calculation algorithms](algorithms.md).

### `<Fixed>` — a fixed month and day

| Attribute | Required | Type | Description |
|---|---|---|---|
| `month` | Yes | string | Month number `1`–`12` or an English month name. |
| `day` | Yes | day (1–31) | Day of month. |
| `skipLeapMonth` | No | bool | Chinese lunisolar only — advance an ordinal lunar month past an intercalary leap month. |
| `sweepCalendarYears` | No | bool | Non-Gregorian only — evaluate both candidate calendar years overlapping the requested Gregorian year. |

```xml
<!-- Christmas Day — 25 December. -->
<Rule id="default">
  <Strategy><Fixed month="December" day="25" /></Strategy>
</Rule>
```

An impossible date (e.g. 29 February in a non-leap year) yields no occurrence for that year. `skipLeapMonth` and `sweepCalendarYears` apply only when the enclosing `<Applicability calendar="...">` is non-Gregorian — see [Working with non-Gregorian calendars](non-gregorian-calendars.md).

### `<DayOfWeekInMonth>` — the *n*th weekday in a month

| Attribute | Required | Type | Description |
|---|---|---|---|
| `month` | Yes | string | Month number or English month name. |
| `dayOfWeek` | Yes | day of week | The target weekday. |
| `weekOrdinal` | Yes | ordinal | <xref:Bodu.Globalization.Calendar.WeekOrdinal>: `First`, `Second`, `Third`, `Fourth`, `Fifth`, `Last`. |

```xml
<!-- US Thanksgiving — the fourth Thursday in November. -->
<Rule id="default">
  <Strategy><DayOfWeekInMonth month="11" dayOfWeek="Thursday" weekOrdinal="Fourth" /></Strategy>
</Rule>
```

`Last` resolves to the final occurrence in the month regardless of whether it is the fourth or fifth; a `Fifth` that does not exist yields no occurrence for that year.

### `<WeekdayNearDate>` — a weekday near a fixed date

| Attribute | Required | Type | Description |
|---|---|---|---|
| `month` | Yes | string | Reference month (number or name). |
| `day` | Yes | day (1–31) | Reference day of month. |
| `dayOfWeek` | Yes | day of week | The target weekday. |
| `direction` | Yes | proximity | <xref:Bodu.Globalization.Calendar.WeekdayProximity>: `Before`, `OnOrBefore`, `Nearest`, `OnOrAfter`, `After`. |

```xml
<!-- Victoria Day (CA) — the Monday on or before 24 May. -->
<Rule id="default">
  <Strategy><WeekdayNearDate month="5" day="24" dayOfWeek="Monday" direction="OnOrBefore" /></Strategy>
</Rule>
```

Because a weekday recurs every seven days, each direction selects a single unambiguous occurrence in the seven-day window anchored at the reference date. `OnOrBefore` / `OnOrAfter` include the reference date itself when it already falls on the target weekday; `Nearest` picks the closest in either direction (the forward and backward distances sum to seven, so the result is never a tie). When the reference (year, month, day) is not a valid date, the rule produces no occurrence.

### `<RelativeWeekdayInMonth>` — a weekday relative to an anchor weekday

| Attribute | Required | Type | Description |
|---|---|---|---|
| `month` | Yes | string | The anchor month. |
| `dayOfWeek` | Yes | day of week | The anchor weekday — combined with `weekOrdinal` it identifies the reference occurrence. |
| `weekOrdinal` | Yes | ordinal | Which occurrence of the anchor weekday. |
| `relativeDayOfWeek` | Yes | day of week | The target weekday the rule resolves to. |
| `direction` | Yes | proximity | How the target weekday is positioned relative to the anchor. |

```xml
<!-- US Election Day — the Tuesday after the first Monday in November. -->
<Rule id="default">
  <Strategy>
    <RelativeWeekdayInMonth month="11" dayOfWeek="Monday" weekOrdinal="First"
                            relativeDayOfWeek="Tuesday" direction="After" />
  </Strategy>
</Rule>
```

The strategy first computes the anchor (the `weekOrdinal`-th `dayOfWeek` of `month`), then positions `relativeDayOfWeek` relative to it using the same window semantics as `<WeekdayNearDate>`. When the anchor occurrence does not exist (a `Fifth` in a month with only four) the rule produces no occurrence.

> [!NOTE]
> "The next weekday after a known weekday" is always a fixed offset, so `<RelativeWeekdayInMonth>` and `<OffsetFromRule>` can describe the same date. Prefer `<OffsetFromRule>` whenever the anchor already exists as a rule (it tracks that rule); use `<RelativeWeekdayInMonth>` when the ordinal-weekday anchor is not itself a modelled concept (US Election Day has no "first Monday of November" rule to offset from).

### `<OffsetFromRule>` — a signed offset from another rule

| Attribute | Required | Type | Description |
|---|---|---|---|
| `notableDateRef` | Yes | identifier | The `id` of the concept whose occurrence is the reference point. |
| `ruleRef` | No | identifier | The `id` of a specific rule within that concept (e.g. `default`). |
| `offsetDays` | Yes | int | Signed day offset — negative moves before the reference, positive after. |

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
```

The referenced rule is resolved first and the offset applied — this is how Good Friday and Easter Monday hang off Easter Sunday. References are resolved cycle-safely within the resource; a self- or mutually-referential chain is reported rather than looping. When the referenced rule produces no occurrence for the year, the offset rule produces none in turn.

### `<Algorithm>` — dispatch to a named calculator

| Attribute | Required | Type | Description |
|---|---|---|---|
| `key` | Yes | string | Names a built-in calculator or a custom <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> registered in a <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry>. |

```xml
<!-- Easter Sunday by the Gregorian computus. -->
<Rule id="default">
  <Strategy><Algorithm key="western-easter" /></Strategy>
</Rule>
```

Built-in keys include `western-easter`, `orthodox-easter`, `vernal-equinox`, `autumnal-equinox`, `qingming`, `vesak`, `asalha-puja`, `losar`, `matariki`, and the Hindu-festival keys (`diwali`, `holi`, `ram-navami`, …). An unknown key resolves against a custom registry; an unregistered unknown key surfaces as a validation diagnostic at load time. See [Date calculation algorithms](algorithms.md) for the full key catalogue and how to register a custom algorithm.

---

## `<AdjustmentPolicy>` — the reusable shift

A weekend substitution or "move to next working day" shift is declared once in `<AdjustmentPolicies>` and referenced from rules by id. It models the runtime <xref:Bodu.Globalization.Calendar.AdjustmentPolicy>. A policy contains, **in order**, an optional `<Scope>`, a required `<Trigger>`, a required `<Action>`, a required `<Emission>`, and optional `<Parameters>`.

| `<AdjustmentPolicy>` attribute | Required | Type | Default | Description |
|---|---|---|---|---|
| `id` | Yes | identifier | — | Policy id referenced by `<Adjustment policyRef="...">`. |
| `priority` | No | int | `0` | Evaluation order when a rule references several policies. |
| `description` | No | string | — | Authoring annotation. |

```xml
<AdjustmentPolicy id="weekend-roll" priority="100"
                  description="If the holiday falls on a weekend, observe it on the following Monday.">
  <Trigger type="IfWeekend" />
  <Action type="MoveToNextWorkingDay" skipWeekends="true" skipNonWorkingDates="false" maxSearchDays="7" />
  <Emission mode="ObservedOnly" reason="Substitute public holiday" />
</AdjustmentPolicy>
```

### `<Scope>` — where the policy applies

`<Scope>` is optional; an absent scope means the policy applies wherever it is referenced. It carries optional `fromYear` / `toYear` attributes and any mix of these children: `<Territory code="..."/>`, `<Calendar name="..."/>`, `<Category value="..."/>`, `<NotableDate ref="..."/>`, `<Rule notableDateRef="..." ruleRef="..."/>`, `<OnlyYear value="..."/>`, `<ExceptYear value="..."/>`. It maps to <xref:Bodu.Globalization.Calendar.AdjustmentScope>.

### `<Trigger>` — when it fires

`<Trigger>` has a required `type` (an <xref:Bodu.Globalization.Calendar.AdjustmentTrigger> value) and optional `month`, `day`, `weekOrdinal`, and `handlerKey` attributes, plus zero or more `<Weekday value="..."/>` children for day-specific triggers.

| `type` | Fires when… |
|---|---|
| `Always` | Unconditionally. |
| `IfDayOfWeek` | The actual date falls on a listed `<Weekday>`. |
| `IfWeekend` | The actual date is a weekend day per the resource's working week. |
| `IfWeekday` | The actual date is a working-week day. |
| `IfNonWorkingDay` | The actual date is already a non-working day (weekend or another non-working occurrence). |
| `IfWorkingDay` | The actual date is a working day. |
| `IfLeapYear` | The resolution year is a leap year. |
| `IfBeforeFixedDate` / `IfAfterFixedDate` | The actual date is before / after the `month`+`day` reference. |
| `IfNthOccurrenceInMonth` | The actual date is the `weekOrdinal`-th `<Weekday>` of its month. |
| `Custom` | A registered <xref:Bodu.Globalization.Calendar.IAdjustmentTriggerHandler> (named by `handlerKey`) returns true. |

### `<Action>` — what it does

`<Action>` has a required `type` (an <xref:Bodu.Globalization.Calendar.AdjustmentAction> value) and optional `days`, `dayOfWeek`, `maxSearchDays`, `skipWeekends`, `skipNonWorkingDates`, `notableDateRef`, `ruleRef`, and `handlerKey` attributes.

| `type` | Effect |
|---|---|
| `None` | Records the trigger but leaves the date unchanged. |
| `AddDays` | Adds `days` (may be negative). |
| `MoveToNextWeekday` / `MoveToPreviousWeekday` | Moves to the next / previous working-week day. |
| `MoveToNextWorkingDay` / `MoveToPreviousWorkingDay` | Skips past non-working days (honouring `skipWeekends` / `skipNonWorkingDates`, capped by `maxSearchDays`) to the next / previous working day. |
| `ReplaceWithRule` | Replaces the date with the occurrence of the rule named by `notableDateRef` / `ruleRef`. |
| `Suppress` | Drops the occurrence. |
| `Custom` | Delegates to a registered <xref:Bodu.Globalization.Calendar.IAdjustmentHandler> named by `handlerKey`. |

### `<Emission>` — what is emitted

`<Emission>` has a required `mode` (a <xref:Bodu.Globalization.Calendar.RangeResolution.EmissionMode> value) and optional `reason` and `nonWorking` attributes. The `reason` is carried into `NotableDate.AdjustmentReason`.

| `mode` | Emits |
|---|---|
| `ActualOnly` | Only the nominal (actual) date. |
| `ObservedOnly` | Only the shifted (observed) date — the single occurrence moves. |
| `ActualAndObserved` | Both, as one occurrence pair. |
| `ObservedAsAdditional` | The actual date **and** an additional observed occurrence (e.g. a substitute Monday granted on top of a remembrance day kept on its fixed date). |
| `Suppress` | Nothing — drops the occurrence. |

### `<Parameters>` — handler inputs

When `type="Custom"` on a trigger or action, an optional `<Parameters>` block of `<Param key="..." value="..."/>` entries is passed to the registered handler. See [Observance adjustment rules](adjustment-rules.md) for custom trigger / action handlers.

---

## Reading the resolved `NotableDate`

`NotableDateService.Resolve(...)` returns <xref:Bodu.Globalization.Calendar.NotableDate> records — one per resolved occurrence. The record is positional and immutable; its key members:

| Member | Type | Description |
|---|---|---|
| `Date` | `DateOnly` | The emitted (observed) date — the date to display, after any adjustment. |
| `ActualDate` | `DateOnly?` | The originally calculated (nominal) date. |
| `IsObserved` | `bool` | Whether `Date` differs from `ActualDate` because an adjustment applied. |
| `EndDate` | `DateOnly` | The inclusive last day (`Date + DurationDays − 1`). |
| `DurationDays` | `int` | Span in days (`1` for single-day events). |
| `DisplayName` | `string` | The display name (subject to optional localization). |
| `Category` | `NotableDateCategory` | The classification carried from the rule. |
| `Priority` | `int` | Tie-break weight consulted by the collision policy. |
| `TerritoryCode` | `string` | The territory the occurrence applies to. |
| `IsNonWorkingDay` | `bool` | Whether working-day arithmetic should skip this date. |
| `Tags` | `IReadOnlyList<string>` | Free-form classification tags from the rule. |
| `AdjustmentPolicyId`, `AdjustmentReason` | `string?` | Which adjustment policy moved the date, and the `reason` text — set when `IsObserved`. |
| `Identity` (`NotableDateId`, `RuleId`) | <xref:Bodu.Globalization.Calendar.NotableDateRuleIdentity> | The originating concept and rule ids. |

```csharp
foreach (NotableDate date in service.Resolve(2027, "AU"))
{
    Console.WriteLine($"{date.Date:d MMM yyyy}  {date.DisplayName}");

    if (date.DurationDays > 1)
        Console.WriteLine($"  Multi-day: ends {date.EndDate:d MMM yyyy}");

    if (date.IsObserved)
        Console.WriteLine($"  Observed (nominal {date.ActualDate:d MMM yyyy}, via {date.AdjustmentPolicyId}: {date.AdjustmentReason})");
}
```

There is no `Name` property (use `DisplayName`) and no `WasAdjusted` property (use `IsObserved`).

---

## `NotableDateCategory`

`category` on a concept or rule is a <xref:Bodu.Globalization.Calendar.NotableDateCategory> value, carried unchanged into `NotableDate.Category`.

| Value | Intended use |
|---|---|
| `None` | No category assigned. |
| `PublicHoliday` | Statutory public holiday in the territory. |
| `BankHoliday` | Bank / financial-sector holiday, distinct from a general public holiday. |
| `Observance` | Recognised observance without statutory non-working status. |
| `Remembrance` | Day of national remembrance or mourning. |
| `Cultural` | Cultural event or celebration. |
| `Religious` | Religious festival or feast day. |
| `Seasonal` | Season boundary, solstice, or equinox event. |
| `Civic` | Civic or government-driven observance (election day, census day). |
| `School` | School term boundary or teacher-only day. |
| `Regional` | Sub-national regional event not classified elsewhere. |
| `Other` | Catch-all. |

---

## Where to go next

- [Authoring notable date rules](rule-authoring.md) — assembling a whole document: imports and ID-targeted overrides.
- [Date calculation algorithms](algorithms.md) — the six strategies in depth, built-in `<Algorithm>` keys, and custom algorithms.
- [Observance adjustment rules](adjustment-rules.md) — the full `<AdjustmentPolicy>` trigger / action / emission catalogues and custom handlers.
- [Holiday patterns and examples](holiday-patterns.md) — end-to-end worked examples for common holiday types.
- [The resolution pipeline](resolution-pipeline.md) — how these elements are processed to produce `NotableDate` results.
