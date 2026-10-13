---
title: Rule identity, priority, and observed-date resolution
---

# Rule identity, priority, and observed-date resolution

This guide covers the resolution semantics an author needs once a rule set grows beyond a handful of fixed dates: how occurrences are *identified*, how *priority* arbitrates same-day collisions under the resource's <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy>, how duplicate identifiers are settled, and how <xref:Bodu.Globalization.Calendar.RangeResolution.EmissionMode> decides which observed dates a range query returns. Three of the policy's settings - <xref:Bodu.Globalization.Calendar.RangeResolution.DuplicatePolicy>, the span collision policy, and <xref:Bodu.Globalization.Calendar.RangeResolution.ObservedDateRangePolicy> - are **reserved**: a document may set them and they are stored, but the engine does not apply them yet ([#1119](https://github.com/bslater/bodu/issues/1119) tracks implementing them). Each section below says what happens instead.

For the element-by-element reference, see [NotableDateRule and adjustment-policy reference](rule-reference.md). For the end-to-end materialisation flow, see [The resolution pipeline](resolution-pipeline.md).

---

## Rule identity

Every resolved <xref:Bodu.Globalization.Calendar.NotableDate> carries a <xref:Bodu.Globalization.Calendar.NotableDateRuleIdentity> on its `Identity` property that records exactly which authored recipe produced it. The identity has three parts:

| Part | Meaning |
|---|---|
| `ResourceId` | The `resourceId` of the `<NotableDateResource>` the rule was loaded from. |
| `NotableDateId` | The `id` of the `<NotableDate>` concept (e.g. `easter-sunday`). |
| `RuleId` | The `id` of the specific `<Rule>` within that concept (e.g. `default`, `western`, `orthodox`). |

`NotableDate` also exposes `NotableDateId` and `RuleId` directly as computed shortcuts onto the identity:

```csharp
foreach (NotableDate date in service.Resolve(2026, "GB"))
{
    NotableDateRuleIdentity id = date.Identity;
    Console.WriteLine($"{date.DisplayName}  [{id.ResourceId}/{id.NotableDateId}/{id.RuleId}]");

    // equivalent shortcuts:
    // date.NotableDateId == id.NotableDateId
    // date.RuleId        == id.RuleId
}
```

Because a concept may hold several rules, distinct rules share a `NotableDateId` but differ by `RuleId` - which is how one concept can carry, say, a Gregorian and an Orthodox Easter that both resolve for the same year:

```xml
<NotableDate id="easter-sunday" displayName="Easter Sunday" category="Religious">
  <Rules>
    <Rule id="western"><Strategy><Algorithm key="western-easter" /></Strategy></Rule>
    <Rule id="orthodox"><Strategy><Algorithm key="orthodox-easter" /></Strategy></Rule>
  </Rules>
</NotableDate>
```

Both rules survive import resolution, override application, and assembly, and both produce an occurrence - each with the same `NotableDateId` (`easter-sunday`) but a different `RuleId` (`western` / `orthodox`). The same `notableDateRef` + `ruleRef` pair is what `<OffsetFromRule>`, `ReplaceWithRule`, and `<PatchRule>` / `<RemoveRule>` overrides use to target a single rule unambiguously. Filtering by concept uses the id:

```csharp
// Every occurrence produced by the easter-sunday concept (both variants).
IReadOnlyList<NotableDate> easters =
    service.Resolve(2026, "GB", NotableDateFilter.WithId("easter-sunday"));
```

---

## Priority and same-day collisions

Every rule carries a `Priority` that flows onto the resolved `NotableDate.Priority`. When several distinct occurrences fall on the same day - for example an adjusted holiday landing on another holiday - the resource's <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy> arbitrates them. Two knobs decide the outcome:

- <xref:Bodu.Globalization.Calendar.RangeResolution.CollisionPolicy> - *what* to do with the colliding set.
- <xref:Bodu.Globalization.Calendar.RangeResolution.PriorityDirection> - *which* priority wins when the policy needs a winner.

`PriorityDirection` is `HigherWins` (default) or `LowerWins`; it tells the engine whether a larger or smaller `Priority` value is the more important one. Occurrences are arbitrated together when they share the same emitted `Date`; a multi-day span that began on an earlier day is not part of that group, even though a single-day query for a day it covers returns it.

### Collision policies

| `CollisionPolicy` | Behaviour |
|---|---|
| `KeepAll` *(default)* | Every distinct occurrence is kept; nothing is suppressed. |
| `HighestPriorityOnly` | Only the occurrence(s) with the winning priority (per `PriorityDirection`) survive. |
| `CategoryPriority` | Occurrences are ranked by category precedence first, then by priority. |
| `Custom` | A supplied <xref:Bodu.Globalization.Calendar.RangeResolution.INotableDateCollisionResolver> decides. |

Collisions are governed by `SameDayCollisionPolicy`, a <xref:Bodu.Globalization.Calendar.RangeResolution.CollisionPolicy> that defaults to `KeepAll` and applies to occurrences sharing an emitted `Date`. Each date is settled over every occurrence emitted on it, whatever range is queried: a multi-day occurrence competes only on its emitted date, and if it loses there it is absent from every query its `[Date, EndDate]` span reaches into, including one whose window starts after that date. A window's result is therefore the same as a wider resolution's result restricted to the window. The resource also carries a `SpanCollisionPolicy` of the same type, meant for multi-day occurrences whose `[Date, EndDate]` ranges overlap, but it is **reserved**: it is accepted and stored, and overlapping spans are not reconciled, so every overlapping span is kept whatever its value ([#1119](https://github.com/bslater/bodu/issues/1119)). The policies are authored on the resource's `<ResolutionPolicy>` element:

```xml
<!-- duplicatePolicy, spanCollisionPolicy and observedDateRangePolicy are reserved: accepted and stored, not yet applied. -->
<ResolutionPolicy duplicatePolicy="KeepFirst"
                  sameDayCollisionPolicy="HighestPriorityOnly"
                  spanCollisionPolicy="KeepAll"
                  priorityDirection="HigherWins"
                  observedDateRangePolicy="ObservedOccurrenceControlsInclusion"
                  workingDays="0111110" />   <!-- Sunday-first; Mon-Fri working -->
```

The runtime <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy> carries these as `SameDayCollisionPolicy`, `SpanCollisionPolicy`, `PriorityDirection`, `DuplicatePolicy`, `ObservedDateRangePolicy`, a `WorkingWeek` (a `Bodu.Core` `DayOfWeekSet`), and a `CategoryPrecedence` list (`SpanCollisionPolicy`, `DuplicatePolicy`, and `ObservedDateRangePolicy` are the reserved three); `ResolutionPolicy.Default` is the all-defaults instance (`DuplicatePolicy.Error`, `CollisionPolicy.KeepAll` on both axes, `PriorityDirection.HigherWins`, Monday-Friday working week).

### Category precedence (`CategoryPriority`)

`CollisionPolicy.CategoryPriority` ranks colliding occurrences by **category** first and only falls back to `Priority` within a category. The ranking is the resource's `CategoryPrecedence` list - authored as a `<CategoryPrecedence>` child of `<ResolutionPolicy>` whose `<Category value="…"/>` entries are ordered most-important-first:

```xml
<ResolutionPolicy sameDayCollisionPolicy="CategoryPriority" priorityDirection="HigherWins">
  <CategoryPrecedence>
    <Category value="PublicHoliday" />
    <Category value="BankHoliday" />
    <Category value="Cultural" />
  </CategoryPrecedence>
</ResolutionPolicy>
```

It maps to `ResolutionPolicy.CategoryPrecedence`, an ordered read-only list of <xref:Bodu.Globalization.Calendar.NotableDateCategory> values; earlier entries outrank later ones. This is the policy to reach for when, say, a `PublicHoliday` should always win a shared day over a `Cultural` observance regardless of the numeric priorities the two rules happen to carry. `PriorityDirection` still breaks ties between two occurrences of the *same* category.

### Custom collision resolution

Under `CollisionPolicy.Custom`, supply an <xref:Bodu.Globalization.Calendar.RangeResolution.INotableDateCollisionResolver> to the <xref:Bodu.Globalization.Calendar.NotableDateService> constructor. Its single method receives the shared day and the colliding occurrences and returns the survivors:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.RangeResolution;

public sealed class HighestPriorityResolver : INotableDateCollisionResolver
{
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding)
    {
        // Keep only the highest-priority occurrence on the day.
        NotableDate winner = colliding.OrderByDescending(d => d.Priority).First();
        return new[] { winner };
    }
}

var service = new NotableDateService(resource, new NotableDateServiceOptions
{
    CollisionResolver = new HighestPriorityResolver(),
});
```

The collaborator slots (`Algorithms`, `CollisionResolver`, `Handlers`, `TriggerHandlers`, `Providers`) live on <xref:Bodu.Globalization.Calendar.NotableDateServiceOptions>, not as positional constructor parameters. The resolver is consulted **only** when a `CollisionPolicy` resolves to `Custom`; for the built-in policies the engine settles the day itself.

---

## Duplicate identifiers

Distinct from a *collision* (two different rules on one day), a *duplicate* is two declarations with the same identity - most often when an import and a local concept both contribute the same concept, or two `<Import>` paths reach the same catalogue. The loader settles duplicates the same way for every resource:

- Two concepts with the same `id` in one document, or two rules with the same `id` in one concept, are load errors (`BODU-CAL-DUP-ND`, `BODU-CAL-DUP-RULE`).
- Across imports, the first import that supplies a concept wins and later imports of the same id are skipped, and a local concept replaces an imported concept with the same id.

<xref:Bodu.Globalization.Calendar.RangeResolution.DuplicatePolicy> (`Error`, `KeepFirst`, `KeepLast`, `Merge`) is **reserved**: the `duplicatePolicy` attribute is accepted, kept on `ResolutionPolicy.DuplicatePolicy`, and written to binary packs, but neither the loader nor the service consults it, so every value behaves as described above. [#1119](https://github.com/bslater/bodu/issues/1119) tracks implementing it.

---

## Observed dates and range inclusion

When an adjustment policy shifts a date (for example rolling a Saturday holiday to Monday), two independent decisions apply: *what the policy emits*, and *which emitted occurrence controls range-query inclusion*.

### What is emitted - `EmissionMode`

The policy's `<Emission mode="…">` selects an <xref:Bodu.Globalization.Calendar.RangeResolution.EmissionMode>:

| `EmissionMode` | What is emitted |
|---|---|
| `ActualOnly` | Only the nominal date; the substitute is discarded. |
| `ObservedOnly` | Only the observed (adjusted) date; the nominal is not emitted separately. |
| `ActualAndObserved` | Both, as two occurrences. |
| `ObservedAsAdditional` | `[Obsolete]` - normalised to `ActualAndObserved` at load time; behaves identically. |
| `Suppress` | Nothing - the occurrence is dropped. |

`EmissionMode` is a property of the *adjustment policy*, authored per policy in `<Emission>` - it is not a service-wide option and not a per-query argument. See [Observance adjustment rules](adjustment-rules.md).

### Which date controls inclusion

A range query returns an occurrence when its emitted span - `Date` through `Date + DurationDays - 1` - intersects the window. For an observed occurrence that is the observed date, so a holiday whose substitute rolls just outside the window is not returned, while its actual occurrence is returned only when the policy emits it (`ActualAndObserved`) and it falls inside the window. The single-day result for a given day is independent of any wider query window.

The resource-level <xref:Bodu.Globalization.Calendar.RangeResolution.ObservedDateRangePolicy> (`ObservedOccurrenceControlsInclusion`, the default, `ActualOccurrenceControlsInclusion`, `BothOccurrencesControlInclusion`) is **reserved**: it is accepted and stored but not applied, so inclusion is always decided by the emitted span, as the default describes. [#1119](https://github.com/bslater/bodu/issues/1119) tracks implementing the other two values.

```csharp
using Bodu.Globalization.Calendar;

// Late-December window in 2027, when Christmas (Sat 25th) is observed Mon 27th.
var window = new DateRange(new DateOnly(2027, 12, 26), new DateOnly(2027, 12, 31));
IReadOnlyList<NotableDate> dates = service.Resolve(window, "AU");
// The observed 27 Dec occurrence is inside the window and is returned; the nominal
// 25 Dec falls outside it.
```

> Emission is authored per adjustment policy (`EmissionMode`), and range inclusion follows from what is emitted; the resource-level `ObservedDateRangePolicy` that would change it is reserved.

---

## Validating a rule set

Identity collisions, dangling references, and unknown algorithm keys are caught at **load** time, not at query time. <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader> validates the assembled resource and throws a `NotableDateValidationException` on any error-severity finding; its `Diagnostics` collection reports every duplicate id, missing or ambiguous `<OffsetFromRule>` / `ReplaceWithRule` reference, and unregistered algorithm key (`BODU-CAL-ALGORITHM`) - all errors; see [Calendar validation diagnostics](validation-diagnostics.md) for the code catalogue. A circular chain of rule references is not reported: it is broken when the dates are resolved, and the rule caught in it produces no occurrence. See [The resolution pipeline - semantic validation](resolution-pipeline.md#stage-5---semantic-validation):

```csharp
using Bodu.Globalization.Calendar;

try
{
    NotableDateResource resource = NotableDateResourceLoader.Load(documentXml, resolver);
}
catch (NotableDateValidationException ex)
{
    foreach (NotableDateValidationDiagnostic d in ex.Diagnostics)
        Console.WriteLine($"[{d.Severity}] {d.Code}: {d.Message}");
}
```

---

## Where to go next

- [NotableDateRule and adjustment-policy reference](rule-reference.md) - the element-by-element schema for rules and policies.
- [The resolution pipeline](resolution-pipeline.md) - how identity, priority, and emission are applied end to end.
- [Observance adjustment rules](adjustment-rules.md) - emission modes, triggers, actions, and custom handlers.
- [RangeResolution API reference](xref:Bodu.Globalization.Calendar.RangeResolution) - the policy enums in full.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** - every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
