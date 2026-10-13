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

Collisions are governed by `SameDayCollisionPolicy`, a <xref:Bodu.Globalization.Calendar.RangeResolution.CollisionPolicy> that defaults to `KeepAll` and applies to occurrences sharing an emitted `Date`. Each date is settled over every occurrence emitted on it, whatever range is queried: a multi-day occurrence competes only on its emitted date, and if it loses there it is absent from every query its `[Date, EndDate]` span reaches into, including one whose window starts after that date. A window's result is therefore the same as a wider resolution's result restricted to the window. The resource also carries a `SpanCollisionPolicy` of the same type, which settles multi-day occurrences whose `[Date, EndDate]` ranges overlap once the same-day groups are settled; see [Overlapping spans](#overlapping-spans) below. The policies are authored on the resource's `<ResolutionPolicy>` element:

```xml
<ResolutionPolicy duplicatePolicy="KeepFirst"
                  sameDayCollisionPolicy="HighestPriorityOnly"
                  spanCollisionPolicy="KeepAll"
                  priorityDirection="HigherWins"
                  observedDateRangePolicy="ObservedOccurrenceControlsInclusion"
                  workingDays="0111110" />   <!-- Sunday-first; Mon-Fri working -->
```

The runtime <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy> carries these as `SameDayCollisionPolicy`, `SpanCollisionPolicy`, `PriorityDirection`, `DuplicatePolicy`, `ObservedDateRangePolicy`, a `WorkingWeek` (a `Bodu.Core` `DayOfWeekSet`), and a `CategoryPrecedence` list; `ResolutionPolicy.Default` is the all-defaults instance (`DuplicatePolicy.Error`, `CollisionPolicy.KeepAll` on both axes, `PriorityDirection.HigherWins`, Monday-Friday working week).

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

The collaborator slots (`Algorithms`, `CollisionResolver`, `Handlers`, `TriggerHandlers`, `Providers`) live on <xref:Bodu.Globalization.Calendar.NotableDateServiceOptions>, not as positional constructor parameters. The resolver is consulted **only** when a `CollisionPolicy` resolves to `Custom`; for the built-in policies the engine settles the day itself. One resolver serves both policies: for a same-day group it receives the shared emitted date, and for an overlap chain under `SpanCollisionPolicy="Custom"` it receives the chain's earliest emitted date. Without a resolver, `Custom` keeps every occurrence of the group.

### Overlapping spans

`SpanCollisionPolicy` settles occurrences that overlap without sharing an emitted date: a five-day festival from 1 June and a public holiday on 3 June, say. It applies after the same-day groups are settled, to every *chain* of surviving occurrences connected through overlapping `[Date, EndDate]` spans that holds at least one multi-day occurrence. A chain is the whole connected run, not each overlapping pair: if A overlaps B and B overlaps C, then A, B and C form one group even when A and C do not touch. A chain is ranked as a same-day group is (`HighestPriorityOnly` keeps the best priority, `CategoryPriority` the best category and then the best priority, `Custom` defers to the resolver with the chain's earliest emitted date), and `KeepAll`, the default, keeps every overlapping occurrence. Occurrences that overlap only by sharing their emitted date, none of them multi-day, are left to `SameDayCollisionPolicy`.

```xml
<ResolutionPolicy spanCollisionPolicy="CategoryPriority" />
<!-- The 3 June public holiday outranks the cultural festival that spans it, so a June query returns the holiday alone. -->
```

A chain is settled whole, whatever window is queried, so a window's result is still a wider resolution's result restricted to the window. A query gathers the occurrences of one civil year either side of its window; while a chain that reaches the window may continue into a year it has not gathered, it widens what it gathers one civil year at a time on that side, up to ten civil years beyond the one-year margin. A chain that never ends, such as a weekly rule whose `durationDays` is at least its interval, is still open at that limit and is settled over what was gathered, so an occurrence more than eleven civil years from the window takes no part in it. The resolver is consulted only for a group that can reach the result: a same-day group whose chain holds an occurrence the window includes, or such a chain.

---

## Duplicate identifiers

Distinct from a *collision* (two different rules on one day), a *duplicate* is two declarations with the same identity - most often when an import and a local concept both contribute the same concept, or two `<Import>` paths reach the same catalogue.

The document's <xref:Bodu.Globalization.Calendar.RangeResolution.DuplicatePolicy> settles the concepts and rules the loaded document declares, when it is loaded, in document order, before its imports are composed and its overrides applied:

| `duplicatePolicy` | Two concepts with one `id` | Two rules with one `id` in a concept |
|---|---|---|
| `Error` *(default)* | Load error `BODU-CAL-DUP-ND`. | Load error `BODU-CAL-DUP-RULE`. |
| `KeepFirst` | The first declaration is kept, at its own position; the later one is discarded with its rules. | The first is kept; the later one is discarded. |
| `KeepLast` | The last declaration is kept, at its own position; the earlier one is discarded with its rules. | The last is kept; the earlier one is discarded. |
| `Merge` | The later declaration's rules are appended to the first declaration, whose own fields (display name, category, defaults, tags) win. | Load error `BODU-CAL-DUP-RULE`, whether the rules were declared in one concept or brought together by the merge. |

A policy that settles duplicates reports nothing. Its reach is the loaded document's own declarations, and nothing else:

- Across imports, the first import that supplies a concept wins and later imports of the same id are skipped, and a local concept replaces an imported concept with the same id, under every policy.
- An imported document's own `duplicatePolicy` is not applied: two rules that share an id in an imported concept stay `BODU-CAL-DUP-RULE`.
- An `<AddRule>` override whose rule id already exists in its target concept is `BODU-CAL-DUP-RULE` under every policy, an authoring error like a `<PatchRule>` that names a missing rule. Overrides apply to the concept the policy keeps.

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

The resource-level <xref:Bodu.Globalization.Calendar.RangeResolution.ObservedDateRangePolicy> decides which span of an occurrence a range query tests against the window:

| `observedDateRangePolicy` | An occurrence is included when |
|---|---|
| `ObservedOccurrenceControlsInclusion` *(default)* | its emitted span, `Date` through `Date + DurationDays - 1`, meets the window. |
| `ActualOccurrenceControlsInclusion` | its actual span, `ActualDate` through `ActualDate + DurationDays - 1`, meets the window. |
| `BothOccurrencesControlInclusion` | either span meets the window. |

Under the default, a holiday whose substitute rolls just outside the window is not returned, while its actual occurrence is returned only when the policy emits it (`ActualAndObserved`) and it falls inside the window. Under `ActualOccurrenceControlsInclusion`, the same holiday is returned by a window that holds its actual date, as the emitted occurrence with its observed `Date`. The policy applies to `Resolve(DateRange)` and everything built on it: by-year and single-day queries, the working-day extensions (so under the actual policy a moved holiday makes its actual day the non-working one, and leaves its observed day a working day unless something else claims it) and the caching decorator. Collision groups stay keyed by the emitted date, so an occurrence that loses its emitted day's collision is absent whichever span would include it, and the actual and observed occurrences an `ActualAndObserved` policy emits, which share their actual date, enter and leave an actual-controlled window together. Whatever the policy, the single-day result for a given day is independent of any wider query window.

```csharp
using Bodu.Globalization.Calendar;

// Late-December window in 2027, when Christmas (Sat 25th) is observed Mon 27th.
var window = new DateRange(new DateOnly(2027, 12, 26), new DateOnly(2027, 12, 31));
IReadOnlyList<NotableDate> dates = service.Resolve(window, "AU");
// The observed 27 Dec occurrence is inside the window and is returned; the nominal
// 25 Dec falls outside it.
```

> Emission is authored per adjustment policy (`EmissionMode`); the resource-level `ObservedDateRangePolicy` decides which span of each emitted occurrence controls range inclusion.

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
