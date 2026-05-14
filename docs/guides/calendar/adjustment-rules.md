---
title: Observance adjustment rules
---

# Observance adjustment rules

An `ObservanceAdjustment` shifts a resolved *nominal* date — the date the resolution strategy computed — into an *observed* date when a trigger condition fires. Most real-world public holiday systems require adjustments: Christmas moving to a Monday when it falls on a Saturday, ANZAC Day moving to the next weekday when it collides with a weekend, or a substitute Monday for any date that falls on a Sunday. This page covers the full trigger and action catalogues, how multiple adjustments chain together, how scoping restricts adjustments to specific territories or years, and how to implement a custom adjustment handler.

For the field-by-field reference, see [NotableDateRule and ObservanceAdjustment reference](rule-reference.md).
For where adjustments sit in the overall resolution process, see [The resolution pipeline](resolution-pipeline.md).

---

## Nominal date vs. observed date

![Nominal date vs. observed date — Christmas Day 2027 worked example](../../images/diagrams/calendar-nominal-vs-observed.svg)

Two terms appear throughout this page and the wider documentation:

- **Nominal date** — what the rule's resolution strategy computes, before any adjustment runs. For Christmas Day this is *25 December*; for Easter Monday it is *Easter Sunday + 1 day*. The nominal date depends only on the rule and the year.
- **Observed date** — what the adjustment pipeline emits and what consumers actually see in `NotableDate.Date`. When the nominal date is acceptable (e.g. Christmas falls on a Thursday) the observed date equals the nominal. When an adjustment fires (e.g. Christmas falls on a Saturday and a weekend-rollover adjustment relocates it) the observed date is the *substitute*.

A resolved `NotableDate` always carries the observed date. The nominal-and-trigger pair is preserved in `NotableDate.AdjustmentReason` whenever `WasAdjusted` is `true`. The reason values map back to the rule that fired:

| `AdjustmentReason` value | Means |
|---|---|
| `None` | No adjustment fired; observed date equals nominal date. |
| `Weekend` | Adjustment fired because the nominal date fell on a weekend. |
| `DayOfWeek` | Adjustment fired because the nominal date matched a specific weekday trigger. |
| `NonWorkingDay` | Adjustment fired because the nominal date was already a non-working day. |
| `LeapYear` | Adjustment fired because the year was a leap year. |
| `FixedDateRange` | Adjustment fired because the nominal date was before or after a configured comparison date. |
| `Custom` | A custom `IAdjustmentHandler` produced the observed date. |

Most consumer code only reads `NotableDate.Date` (the observed date). Audit-style code that needs the original nominal date — for example, "show me every holiday whose nominal date fell on a weekend in 2026" — reads `NotableDate.AdjustmentReason` to filter and then inspects `WasAdjusted` + the original date on the reason.

## Adjust in place vs. emit an additional observed date

Two jurisdictional styles dominate real-world public-holiday law, and `ObservanceAdjustment` supports both.

**Adjust in place — the nominal date is gone.** The adjustment relocates the single occurrence to a new observed date. There is one `NotableDate` for the year; `Date` holds the observed date and `AdjustmentReason` records the original. Typical for Australian New Year's Day and most AU / NZ public holidays:

```csharp
// AU New Year's Day — when 1 Jan falls on a weekend, the single occurrence moves.
Adjustments = ImmutableArray.Create(new ObservanceAdjustment
{
    Key     = "weekend-roll",
    Trigger = AdjustmentTrigger.IfWeekend,
    Action  = AdjustmentAction.MoveToNextWeekday,
})
```

**Emit an additional observed date — keep the nominal *and* the substitute.** Author two rules: the nominal rule retains its date with no adjustment, and a companion "substitute holiday" rule fires only when the nominal date falls on a weekend (or another non-working day). The pipeline returns two `NotableDate` instances for the same year — one for the nominal, one for the substitute observed day. Typical for UK bank holidays:

```csharp
// UK Christmas Day — always observed on 25 December (cultural / religious).
new NotableDateRule
{
    Name            = "Christmas Day",
    Strategy        = DateResolutionStrategy.Fixed,
    Month           = 12, Day = 25,
    Category        = NotableDateCategory.Cultural,
    IsNonWorkingDay = false,
}

// UK Christmas Day (substitute) — observed on the next weekday when 25 Dec is a weekend.
new NotableDateRule
{
    Name            = "Christmas Day (substitute)",
    Strategy        = DateResolutionStrategy.Fixed,
    Month           = 12, Day = 25,
    Category        = NotableDateCategory.Holiday,
    IsNonWorkingDay = true,
    Adjustments     = ImmutableArray.Create(new ObservanceAdjustment
    {
        Trigger = AdjustmentTrigger.IfWeekend,
        Action  = AdjustmentAction.MoveToNextWeekday,
    }),
}
```

**When to use which.** Adjust in place when the law (or your authority) treats the holiday as moved — there is no separate observance on the nominal date. Emit an additional observed date when the nominal date retains its religious / cultural significance and the substitute is a working-day closure with its own legal weight (UK bank holidays). The data packs follow each jurisdiction's prevailing convention.

---

## How adjustments sit in the pipeline

After the nominal date is resolved (by `Fixed`, `DayOfWeekInMonth`, `OffsetFromAnchor`, or
`Algorithm` strategy), the adjustment chain runs:

```
Nominal date resolved
    │
    ▼
Adjustment 1 (lowest priority number)
  ├── Trigger condition met? → Action applied → stop evaluating remaining adjustments
  └── Trigger condition not met? → move to next adjustment
    │
    ▼
Adjustment 2
  ├── Trigger condition met? → Action applied → stop
  └── Not met? → move to next adjustment
    │
    ▼
  ... (remaining adjustments)
    │
    ▼
No adjustment fired → date unchanged, WasAdjusted = false
```

**Key behaviours:**

- Adjustments are evaluated in ascending `Priority` order (lower number first).
- The first adjustment whose trigger condition fires wins; all remaining adjustments are skipped.
- When an adjustment fires, `NotableDate.WasAdjusted` is set to `true` and `AdjustmentReason` is populated with the original date, trigger, and action.
- Adjustments that are outside their `EffectiveFromYear` / `EffectiveToYear` range, or scoped to a different `TerritoryCode` or `CalendarType`, are skipped entirely before trigger evaluation.

---

## Trigger catalogue

### `Always`

Fires unconditionally for every occurrence of the rule. Use with `AddDays` to implement a
permanent offset, or with `None` to record that a rule was intentionally reviewed.

```csharp
// Shift a date forward by 1 day regardless of which weekday it falls on
new ObservanceAdjustment
{
    Key     = "permanent-offset",
    Trigger = AdjustmentTrigger.Always,
    Action  = AdjustmentAction.AddDays,
    OffsetDays = 1,
}
```

```xml
<Adjustment key="permanent-offset" when="Always" action="AddDays" offset="1" />
```

### `IfWeekend`

Fires when the nominal date falls on a weekend day as defined by the service's configured
`CalendarWeekendDefinition`. For most territories this is Saturday or Sunday, but the
weekend definition is configurable — Friday/Saturday for some Middle Eastern territories.

```csharp
new ObservanceAdjustment
{
    Key     = "weekend-to-monday",
    Trigger = AdjustmentTrigger.IfWeekend,
    Action  = AdjustmentAction.MoveToNextWeekday,
}
```

```xml
<Adjustment key="weekend-to-monday" when="IfWeekend" action="MoveToNextWeekday" />
```

### `IfWeekday`

Fires when the nominal date falls on a weekday. Less common; useful for rules where the weekday
occurrence requires different handling than the weekend occurrence. Can also be used to
schedule a secondary event on the working-day that precedes or follows a weekend date.

```csharp
new ObservanceAdjustment
{
    Key     = "if-weekday-no-shift",
    Trigger = AdjustmentTrigger.IfWeekday,
    Action  = AdjustmentAction.None,
}
```

### `IfDayOfWeek`

Fires when the nominal date falls on the specific weekday identified by the `DayOfWeek` field.
Use this to model jurisdiction-specific rules, such as an observance that moves only when it
falls on a Tuesday, or a date that absorbs a neighbouring holiday if it lands on a Monday.

```csharp
// Move to previous Friday when the date falls on a Saturday
new ObservanceAdjustment
{
    Key       = "saturday-to-friday",
    Priority  = 1,
    Trigger   = AdjustmentTrigger.IfDayOfWeek,
    DayOfWeek = DayOfWeek.Saturday,
    Action    = AdjustmentAction.MoveToPreviousWeekday,
}
```

```xml
<Adjustment key="saturday-to-friday" priority="1"
            when="IfDayOfWeek" dayOfWeek="Saturday"
            action="MoveToPreviousWeekday" />
```

### `IfNonWorkingDay`

Fires when the nominal date is already a non-working day — that is, either a weekend or a date
marked `IsNonWorkingDay = true` by another rule that has already been resolved for the same
year and territory. This trigger is evaluated using the live generation context, so it can
see dates resolved earlier in the same pass.

This is the correct trigger for Christmas and Boxing Day substitution in the UK and
Australia, where Boxing Day moves when Christmas Day has already taken Monday:

```csharp
// Boxing Day: if Boxing Day is already a non-working day, move to the next non-working day
new ObservanceAdjustment
{
    Key     = "boxing-day-substitute",
    Trigger = AdjustmentTrigger.IfNonWorkingDay,
    Action  = AdjustmentAction.MoveToNextNonWorkingDay,
}
```

```xml
<Adjustment key="boxing-day-substitute"
            when="IfNonWorkingDay"
            action="MoveToNextNonWorkingDay" />
```

> **Note:** Because `IfNonWorkingDay` depends on the live non-working-day set, the output
> may differ depending on the order in which rules are resolved. Rules with `Priority`
> values closer to 0 (higher priority) are resolved before lower-priority rules, so ensure
> that anchor rules (e.g. Christmas Day) have a higher priority than dependent rules
> (e.g. Boxing Day) when the dependent trigger is `IfNonWorkingDay`.

### `IfLeapYear`

Fires when the target resolution year is a leap year. Primarily useful for rules associated
with February 29 or with calendar events that behave differently in leap years.

```csharp
new ObservanceAdjustment
{
    Key     = "leap-year-variant",
    Trigger = AdjustmentTrigger.IfLeapYear,
    Action  = AdjustmentAction.AddDays,
    OffsetDays = 1,
}
```

```xml
<Adjustment key="leap-year-variant" when="IfLeapYear" action="AddDays" offset="1" />
```

### `IfNthOccurrenceInMonth`

Fires when the nominal date falls on the *n*th occurrence of the given weekday within its month.
Requires both `DayOfWeek` and `WeekOrdinal`.

```csharp
// Fire only when the date is the first Monday in the month
new ObservanceAdjustment
{
    Key         = "first-monday-variant",
    Trigger     = AdjustmentTrigger.IfNthOccurrenceInMonth,
    DayOfWeek   = DayOfWeek.Monday,
    WeekOrdinal = WeekOfMonthOrdinal.First,
    Action      = AdjustmentAction.AddDays,
    OffsetDays  = 7,
}
```

### `IfBeforeFixedDate` / `IfAfterFixedDate`

Fire when the nominal date falls before or after a specified calendar date. The comparison date is
provided via `ComparisonDate`. Useful for rules that change behaviour around a known
boundary, such as a legislative change that took effect on a specific date.

```csharp
// Before 1 January 2020: move to previous weekday; from 2020 onwards: move to next weekday
new ObservanceAdjustment
{
    Key            = "pre-2020-rule",
    Priority       = 1,
    Trigger        = AdjustmentTrigger.IfBeforeFixedDate,
    ComparisonDate = new DateOnly(2020, 1, 1),
    Action         = AdjustmentAction.MoveToPreviousWeekday,
    EffectiveToYear = 2019,
},
new ObservanceAdjustment
{
    Key            = "post-2020-rule",
    Priority       = 2,
    Trigger        = AdjustmentTrigger.IfWeekend,
    Action         = AdjustmentAction.MoveToNextWeekday,
    EffectiveFromYear = 2020,
}
```

### `Custom`

Delegates trigger evaluation and / or action to a registered `IAdjustmentHandler`. Use this
when the built-in triggers and actions cannot express the required logic. The handler receives
a full `AdjustmentHandlerContext` including access to the generation context for non-working-day
lookups and rule resolution. The handler operates on the nominal date and returns the observed date.
See [Custom IAdjustmentHandler](#custom-iadjustmenthandler) below.

```csharp
new ObservanceAdjustment
{
    Key              = "corporate-closure",
    Trigger          = AdjustmentTrigger.Custom,
    Action           = AdjustmentAction.Custom,
    HandlerKey       = "corporate-closure-handler",
    HandlerParameters = ImmutableDictionary<string, string>.Empty
        .Add("closureType", "earlyClose"),
}
```

```xml
<Adjustment key="corporate-closure"
            when="Custom" action="Custom"
            handlerKey="corporate-closure-handler">
  <Parameter name="closureType" value="earlyClose" />
</Adjustment>
```

---

## Action catalogue

### `None`

Records that the trigger fired but takes no action. The nominal date is preserved as the
observed date. `WasAdjusted` remains `false`. Useful for audit logging or when only the
trigger-match fact matters.

### `AddDays`

Shifts the nominal date by `OffsetDays` calendar days. Positive values move forward; negative
values move backward. No weekday or working-day semantics — the result may land on a weekend
or another holiday.

```csharp
new ObservanceAdjustment
{
    Trigger    = AdjustmentTrigger.IfDayOfWeek,
    DayOfWeek  = DayOfWeek.Sunday,
    Action     = AdjustmentAction.AddDays,
    OffsetDays = 2,  // Sunday → Tuesday
}
```

### `MoveToNextWeekday`

Advances the nominal date to the next calendar day that is not a weekend (as defined by
`CalendarWeekendDefinition`). If the nominal date is already a weekday this action is a no-op —
trigger conditions should ensure it only fires when the nominal date is on a weekend.

Saturday advances to Monday; Sunday advances to Monday (unless Monday is also a weekend,
which can occur for non-standard weekend definitions).

### `MoveToPreviousWeekday`

Retreats to the nearest preceding weekday. Saturday retreats to Friday; Sunday retreats to
Friday.

### `MoveToNextNonWorkingDay`

Advances past all consecutive non-working days (weekends and other notable dates flagged
`IsNonWorkingDay`) until the first day that is a working day. This is the action to use for
Boxing Day substitution in jurisdictions where the substitute must not land on another public
holiday.

For example, if Christmas falls on a Friday (non-working) then Boxing Day (Saturday) is
already non-working, and its substitute must skip Saturday (weekend), Sunday (weekend), and
Monday (Christmas substitute) to land on Tuesday.

### `ReplaceWithNamedDate`

Replaces the nominal date with the resolved date of another rule identified by `TargetRuleName`.
The named rule must have been resolved already (it must appear earlier in the effective rule
list). Use this when one event is defined as "the same day as" another, rather than a fixed
calendar date.

```csharp
new ObservanceAdjustment
{
    Trigger        = AdjustmentTrigger.Always,
    Action         = AdjustmentAction.ReplaceWithNamedDate,
    TargetRuleName = "Eid al-Fitr",
}
```

### `Custom`

Delegates to a registered `IAdjustmentHandler`. May be combined with a built-in trigger
(handler only provides the action) or with `Trigger = Custom` (handler evaluates both the
condition and the resulting date).

---

## Priority and chaining

### Evaluation order

Adjustments are sorted by ascending `Priority` before evaluation begins. The priority value
is an integer — lower numbers run first. The default priority is `100`.

```csharp
// Saturday → previous Friday (priority 1 evaluates first)
new ObservanceAdjustment
{
    Priority  = 1,
    Trigger   = AdjustmentTrigger.IfDayOfWeek,
    DayOfWeek = DayOfWeek.Saturday,
    Action    = AdjustmentAction.MoveToPreviousWeekday,
},
// Sunday → next Monday (priority 2 evaluates if the first did not fire)
new ObservanceAdjustment
{
    Priority  = 2,
    Trigger   = AdjustmentTrigger.IfDayOfWeek,
    DayOfWeek = DayOfWeek.Sunday,
    Action    = AdjustmentAction.MoveToNextWeekday,
}
```

### First-match semantics

Once a trigger fires and an action is applied, all remaining adjustments in the array are
skipped. This means the `IfWeekend` trigger (which matches both Saturday and Sunday) and
separate `IfDayOfWeek(Saturday)` / `IfDayOfWeek(Sunday)` triggers are not interchangeable
when different actions are needed for each day.

Use separate adjustments with different priorities when Saturday and Sunday must produce
different outcomes:

```csharp
// US-style: Saturday → preceding Friday; Sunday → following Monday
ImmutableArray.Create(
    new ObservanceAdjustment
    {
        Priority  = 1,
        Trigger   = AdjustmentTrigger.IfDayOfWeek,
        DayOfWeek = DayOfWeek.Saturday,
        Action    = AdjustmentAction.MoveToPreviousWeekday,
    },
    new ObservanceAdjustment
    {
        Priority  = 2,
        Trigger   = AdjustmentTrigger.IfDayOfWeek,
        DayOfWeek = DayOfWeek.Sunday,
        Action    = AdjustmentAction.MoveToNextWeekday,
    }
)
```

### MaxAdjustmentReachDays

`MaxAdjustmentReachDays` caps how many days the observed date may be moved from the nominal
date. When set to a positive value, the adjustment is not applied if the resulting date would
exceed the cap. This prevents pathological cases near month or year boundaries where a
`MoveToNextNonWorkingDay` action would skip into the wrong month.

---

## Territory and year scoping on adjustments

An adjustment fires only when all of the following hold:

1. `TerritoryCode` is `null`, or the requested territory is contained by the adjustment's territory (same containment rule as `NotableDateRule`).
2. `CalendarType` is `null`, or matches the requested calendar type.
3. `EffectiveFromYear` is `null`, or the target year ≥ `EffectiveFromYear`.
4. `EffectiveToYear` is `null`, or the target year ≤ `EffectiveToYear`.

This lets a single rule carry adjustments that apply only in certain jurisdictions. For
example, Christmas Day in Australia has a different substitution rule for the Northern
Territory compared to other states:

```csharp
Adjustments = ImmutableArray.Create(
    // NT: move to next weekday when on a weekend
    new ObservanceAdjustment
    {
        Key           = "nt-weekend-roll",
        Priority      = 1,
        Trigger       = AdjustmentTrigger.IfWeekend,
        Action        = AdjustmentAction.MoveToNextWeekday,
        TerritoryCode = "AU-NT",
    },
    // All other AU: move to next non-working day
    new ObservanceAdjustment
    {
        Key           = "au-nonworking-roll",
        Priority      = 2,
        Trigger       = AdjustmentTrigger.IfNonWorkingDay,
        Action        = AdjustmentAction.MoveToNextNonWorkingDay,
        TerritoryCode = "AU",
    }
)
```

When resolving for `"AU-NT"`, adjustment 1 is evaluated first (it matches via containment).
If its trigger fires, adjustment 2 is skipped. If adjustment 1 does not fire (e.g. Christmas
is on a weekday), adjustment 2 is then evaluated for its trigger.

---

## Real-world patterns

### Standard AU/NZ weekend roll (Saturday or Sunday → Monday)

```csharp
Adjustments = ImmutableArray.Create(new ObservanceAdjustment
{
    Key     = "weekend-roll",
    Trigger = AdjustmentTrigger.IfWeekend,
    Action  = AdjustmentAction.MoveToNextWeekday,
})
```

```xml
<Adjustment key="weekend-roll" when="IfWeekend" action="MoveToNextWeekday" />
```

### UK bank holiday roll (Saturday → Monday, Sunday → Tuesday)

UK bank holiday law gives each day its own substitute, producing two separate adjustments:

```csharp
Adjustments = ImmutableArray.Create(
    new ObservanceAdjustment
    {
        Key       = "sat-to-mon",
        Priority  = 1,
        Trigger   = AdjustmentTrigger.IfDayOfWeek,
        DayOfWeek = DayOfWeek.Saturday,
        Action    = AdjustmentAction.AddDays,
        OffsetDays = 2,  // Saturday + 2 = Monday
    },
    new ObservanceAdjustment
    {
        Key       = "sun-to-tue",
        Priority  = 2,
        Trigger   = AdjustmentTrigger.IfDayOfWeek,
        DayOfWeek = DayOfWeek.Sunday,
        Action    = AdjustmentAction.AddDays,
        OffsetDays = 2,  // Sunday + 2 = Tuesday
    }
)
```

### US "observed" pattern (Saturday → Friday, Sunday → Monday)

```csharp
Adjustments = ImmutableArray.Create(
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
)
```

### Boxing Day collision avoidance

When Christmas Day moves to Monday, Boxing Day (26 December) must skip to Tuesday rather
than also landing on Monday. `IfNonWorkingDay` with `MoveToNextNonWorkingDay` handles this
automatically by checking the live non-working-day context:

```csharp
// Boxing Day — moves past any non-working day including a relocated Christmas substitute
Adjustments = ImmutableArray.Create(new ObservanceAdjustment
{
    Key     = "boxing-day-collision",
    Trigger = AdjustmentTrigger.IfNonWorkingDay,
    Action  = AdjustmentAction.MoveToNextNonWorkingDay,
})
```

For 2027 (Christmas on Saturday):

| Date | Event | Adjustment |
|---|---|---|
| 25 Dec (Sat) | Christmas Day nominal date | `IfWeekend → MoveToNextWeekday` → observed Mon 27 Dec |
| 26 Dec (Sun) | Boxing Day nominal date | `IfNonWorkingDay → MoveToNextNonWorkingDay`: Sun is non-working → skip to Mon → Mon 27 is already non-working (Christmas substitute) → observed Tue 28 Dec |

### Easter Monday collision with another holiday

```csharp
// Easter Monday — if it falls on another non-working day, move forward
Adjustments = ImmutableArray.Create(new ObservanceAdjustment
{
    Key                   = "collision-skip",
    Trigger               = AdjustmentTrigger.IfNonWorkingDay,
    Action                = AdjustmentAction.MoveToNextNonWorkingDay,
    MaxAdjustmentReachDays = 3,  // never shift more than 3 days
})
```

---

## Custom IAdjustmentHandler

When the built-in trigger and action values cannot express your logic, implement
`IAdjustmentHandler` and register it with an `AdjustmentHandlerRegistry`.

### The contract

```csharp
public interface IAdjustmentHandler
{
    AdjustmentHandlerResult Apply(AdjustmentHandlerContext context);
}
```

`AdjustmentHandlerContext` provides:

| Property | Type | Description |
|---|---|---|
| `CurrentDate` | `DateTime` | The nominal date to evaluate (or the current candidate when chained handlers run). |
| `Rule` | `NotableDateRule` | The rule being resolved. |
| `Adjustment` | `ObservanceAdjustment` | The adjustment being evaluated. |
| `TerritoryCode` | `TerritoryCode?` | The territory being resolved for. |
| `Year` | `int` | The year being resolved. |
| `Parameters` | `IReadOnlyDictionary<string,string>` | Contents of `HandlerParameters`. |
| `GenerationContext` | `NotableDateGenerationContext` | Access to `IsNonWorkingDay(date)` and `ResolveByName(name)` for dependency on other resolved dates. |

`AdjustmentHandlerResult` has two factory methods:

```csharp
// The handler handled the adjustment; provide the new date
AdjustmentHandlerResult.Handled(DateTime adjustedDate)

// The handler declines; the next adjustment in the chain is evaluated
AdjustmentHandlerResult.NotHandled()
```

### Example — skip to the next working day capped at 5 days

```csharp
using Bodu.Globalization.Calendar;

public sealed class NextWorkingDayHandler : IAdjustmentHandler
{
    public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context)
    {
        DateTime candidate = context.CurrentDate.AddDays(1);
        int attempts = 0;

        while (attempts < 5 && context.GenerationContext.IsNonWorkingDay(candidate, context.TerritoryCode))
        {
            candidate = candidate.AddDays(1);
            attempts++;
        }

        if (attempts == 5)
            return AdjustmentHandlerResult.NotHandled();

        return AdjustmentHandlerResult.Handled(candidate);
    }
}
```

Register it when constructing the service:

```csharp
using Bodu.Globalization.Calendar;

AdjustmentHandlerRegistry handlers = new AdjustmentHandlerRegistry()
    .Register("next-working-day", new NextWorkingDayHandler());

var service = new NotableDateService(
    ruleProviders:     new[] { myProvider },
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
    options: new NotableDateServiceOptions { AdjustmentHandlers = handlers });
```

Wire the handler to an adjustment with `HandlerKey`:

```csharp
new ObservanceAdjustment
{
    Key        = "custom-shift",
    Trigger    = AdjustmentTrigger.IfNonWorkingDay,
    Action     = AdjustmentAction.Custom,
    HandlerKey = "next-working-day",
}
```

Or use `Trigger = Custom` to let the handler decide whether the condition applies at all:

```csharp
new ObservanceAdjustment
{
    Key        = "full-custom",
    Trigger    = AdjustmentTrigger.Custom,
    Action     = AdjustmentAction.Custom,
    HandlerKey = "next-working-day",
}
```

When both `Trigger` and `Action` are `Custom`, the handler is responsible for both the
condition check and producing the adjusted date. Return `NotHandled()` to signal that no
shift should occur.

---

## Where to go next

- [NotableDateRule and ObservanceAdjustment reference](rule-reference.md) — field definitions for both types.
- [The resolution pipeline](resolution-pipeline.md) — how adjustments are evaluated within the overall resolution flow.
- [Holiday patterns and examples](holiday-patterns.md) — complete worked examples for UK, US, AU, Easter, and lunar holidays.
- [Building and extending the service](building-the-service.md) — wiring `AdjustmentHandlerRegistry` into a `NotableDateService`.
