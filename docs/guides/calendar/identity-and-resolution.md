---
title: Rule identity, priority, and observed-date resolution
---

# Rule identity, priority, and observed-date resolution

This guide covers the resolution semantics an author needs once a rule set grows beyond a handful of fixed dates: how occurrences are *identified*, how *priority* arbitrates same-day collisions, how the resource's <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy> settles duplicates and collisions, and how the <xref:Bodu.Globalization.Calendar.RangeResolution.EmissionMode> / <xref:Bodu.Globalization.Calendar.RangeResolution.ObservedDateRangePolicy> pair governs observed-date range inclusion.

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

Because a concept may hold several rules, distinct rules share a `NotableDateId` but differ by `RuleId` — which is how one concept can carry, say, a Gregorian and an Orthodox Easter that both resolve for the same year:

```xml
<NotableDate id="easter-sunday" displayName="Easter Sunday" category="Religious">
  <Rules>
    <Rule id="western"><Strategy><Algorithm key="western-easter" /></Strategy></Rule>
    <Rule id="orthodox"><Strategy><Algorithm key="orthodox-easter" /></Strategy></Rule>
  </Rules>
</NotableDate>
```

Both rules survive import resolution, override application, and assembly, and both produce an occurrence — each with the same `NotableDateId` (`easter-sunday`) but a different `RuleId` (`western` / `orthodox`). The same `notableDateRef` + `ruleRef` pair is what `<OffsetFromRule>`, `ReplaceWithRule`, and `<PatchRule>` / `<RemoveRule>` overrides use to target a single rule unambiguously. Filtering by concept uses the id:

```csharp
// Every occurrence produced by the easter-sunday concept (both variants).
IReadOnlyList<NotableDate> easters =
    service.Resolve(2026, "GB", NotableDateFilter.WithId("easter-sunday"));
```

---

## Priority and same-day collisions

Every rule carries a `Priority` that flows onto the resolved `NotableDate.Priority`. When several distinct occurrences fall on the same day — for example an adjusted holiday landing on another holiday — the resource's <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy> arbitrates them. Two knobs decide the outcome:

- <xref:Bodu.Globalization.Calendar.RangeResolution.CollisionPolicy> — *what* to do with the colliding set.
- <xref:Bodu.Globalization.Calendar.RangeResolution.PriorityDirection> — *which* priority wins when the policy needs a winner.

`PriorityDirection` is `HigherWins` (default) or `LowerWins`; it tells the engine whether a larger or smaller `Priority` value is the more important one. For a **single-day** query, every occurrence that *covers* that day — including a multi-day span that started earlier — is arbitrated together.

### Collision policies

| `CollisionPolicy` | Behaviour |
|---|---|
| `KeepAll` *(default)* | Every distinct occurrence is kept; nothing is suppressed. |
| `HighestPriorityOnly` | Only the occurrence(s) with the winning priority (per `PriorityDirection`) survive. |
| `CategoryPriority` | Occurrences are ranked by category precedence first, then by priority. |
| `Custom` | A supplied <xref:Bodu.Globalization.Calendar.RangeResolution.INotableDateCollisionResolver> decides. |

Same-day collisions are governed by `SameDayCollisionPolicy` and overlapping multi-day spans by `SpanCollisionPolicy`; both default to `KeepAll`. The policies are authored on the resource's `<ResolutionPolicy>` element:

```xml
<ResolutionPolicy duplicatePolicy="KeepFirst"
                  sameDayCollisionPolicy="HighestPriorityOnly"
                  spanCollisionPolicy="KeepAll"
                  priorityDirection="HigherWins"
                  observedDateRangePolicy="ObservedOccurrenceControlsInclusion"
                  workingDays="0111110" />   <!-- Sunday-first; Mon–Fri working -->
```

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

var service = new NotableDateService(
    resource,
    algorithms:        null,
    collisionResolver: new HighestPriorityResolver());
```

The resolver is consulted **only** when a `CollisionPolicy` resolves to `Custom`; for the built-in policies the engine settles the day itself.

---

## Duplicate reconciliation

Distinct from a *collision* (two different rules on one day), a *duplicate* is the same occurrence appearing more than once — most often when an import and a local concept both contribute the same rule, or two `<Import>` paths reach the same catalogue. <xref:Bodu.Globalization.Calendar.RangeResolution.DuplicatePolicy> reconciles them:

| `DuplicatePolicy` | Behaviour |
|---|---|
| `Error` *(default)* | Duplicate occurrences are treated as an authoring error. |
| `KeepFirst` | The first occurrence is kept; later duplicates are dropped. |
| `KeepLast` | The last occurrence is kept. |
| `Merge` | Duplicates are merged into a single occurrence. |

Because local concepts already win over imported ones of the same id at load time, `DuplicatePolicy` is the safety net for the cases that survive that rule rather than the primary composition mechanism.

---

## Observed dates and range inclusion

When an adjustment policy shifts a date (for example rolling a Saturday holiday to Monday), two independent decisions apply: *what the policy emits*, and *which emitted occurrence controls range-query inclusion*.

### What is emitted — `EmissionMode`

The policy's `<Emission mode="…">` selects an <xref:Bodu.Globalization.Calendar.RangeResolution.EmissionMode>:

| `EmissionMode` | What is emitted |
|---|---|
| `ActualOnly` | Only the nominal date; the substitute is discarded. |
| `ObservedOnly` | Only the observed (adjusted) date; the nominal is not emitted separately. |
| `ActualAndObserved` | Both, as two occurrences. |
| `ObservedAsAdditional` | The nominal date plus an additional observed occurrence (the substitute). |
| `Suppress` | Nothing — the occurrence is dropped. |

`EmissionMode` is a property of the *adjustment policy*, authored per policy in `<Emission>` — it is not a service-wide option and not a per-query argument. See [Observance adjustment rules](adjustment-rules.md).

### Which date controls inclusion — `ObservedDateRangePolicy`

For a range query, the resource-level <xref:Bodu.Globalization.Calendar.RangeResolution.ObservedDateRangePolicy> decides which date of an occurrence must fall inside the window for it to be returned:

| `ObservedDateRangePolicy` | An occurrence is included when … |
|---|---|
| `ObservedOccurrenceControlsInclusion` *(default)* | its observed `Date` falls inside the range. |
| `ActualOccurrenceControlsInclusion` | its nominal `ActualDate` falls inside the range. |
| `BothOccurrencesControlInclusion` | either its observed or its nominal date falls inside the range. |

This is what makes range results stable: a holiday whose substitute rolls just outside the queried window is included or excluded by a deliberate, resource-level rule rather than by accident. The single-day result for a given day is independent of any wider query window.

```csharp
using Bodu.Globalization.Calendar;

// Late-December window in 2027, when Christmas (Sat 25th) is observed Mon 27th.
var window = new DateRange(new DateOnly(2027, 12, 26), new DateOnly(2027, 12, 31));
IReadOnlyList<NotableDate> dates = service.Resolve(window, "AU");
// Under the default ObservedOccurrenceControlsInclusion the observed 27 Dec occurrence
// is inside the window and is returned; the nominal 25 Dec falls outside it.
```

> Emission and range inclusion are two separate concerns: *emission* is authored per adjustment policy (`EmissionMode`), and *range inclusion* is authored once per resource (`ObservedDateRangePolicy`).

---

## Validating a rule set

Identity collisions, dangling references, and unknown algorithm keys are caught at **load** time, not at query time. <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader> validates the assembled resource and throws a `NotableDateValidationException` on any error-severity finding; its `Diagnostics` collection reports every duplicate id, missing or ambiguous `<OffsetFromRule>` / `ReplaceWithRule` reference, reference cycle (errors), and unregistered algorithm key (a warning, since an optional pack may supply it later). See [The resolution pipeline — semantic validation](resolution-pipeline.md#stage-5--semantic-validation):

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

- [NotableDateRule and adjustment-policy reference](rule-reference.md) — the element-by-element schema for rules and policies.
- [The resolution pipeline](resolution-pipeline.md) — how identity, priority, and emission are applied end to end.
- [Observance adjustment rules](adjustment-rules.md) — emission modes, triggers, actions, and custom handlers.
- [RangeResolution API reference](xref:Bodu.Globalization.Calendar.RangeResolution) — the policy enums in full.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
