---
uid: Bodu.Globalization.Calendar.RangeResolution
---

# Bodu.Globalization.Calendar.RangeResolution

## Purpose

**Bodu.Globalization.Calendar.RangeResolution** holds the policy vocabulary that governs how resolved occurrences are de-duplicated, how same-day and overlapping-span collisions are settled, which occurrence (actual or observed) controls range-query inclusion, and which days count as the working week.

These policies are carried by the document's `<ResolutionPolicy>` element and surfaced as <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy> on the loaded <xref:Bodu.Globalization.Calendar.NotableDateResource>. They are resource-level (authored once per document) rather than per-query.

## Static documentation

- **[The resolution pipeline](~/guides/calendar/resolution-pipeline.md)** — the stages a query runs through and where each policy applies.
- **[Rule identity, priority, and observed-date resolution](~/guides/calendar/identity-and-resolution.md)** — duplicate / collision settlement and observed-date range inclusion in detail.

## Key types

- <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy> — the policy bundle: `DuplicatePolicy`, `SameDayCollisionPolicy`, `SpanCollisionPolicy`, `PriorityDirection`, `ObservedDateRangePolicy`, and the working week (a `Bodu.Core` `WeekPattern`, default Monday–Friday). `ResolutionPolicy.Default` is the all-defaults instance.
- <xref:Bodu.Globalization.Calendar.RangeResolution.DuplicatePolicy> — how identical occurrences are reconciled: `Error`, `KeepFirst`, `KeepLast`, `Merge`.
- <xref:Bodu.Globalization.Calendar.RangeResolution.CollisionPolicy> — how distinct rules landing on the same day (or with overlapping spans) are settled: `KeepAll`, `HighestPriorityOnly`, `CategoryPriority`, `Custom`.
- <xref:Bodu.Globalization.Calendar.RangeResolution.PriorityDirection> — whether a higher or lower `Priority` wins: `HigherWins`, `LowerWins`.
- <xref:Bodu.Globalization.Calendar.RangeResolution.EmissionMode> — what an adjustment emits: `ActualOnly`, `ObservedOnly`, `ActualAndObserved`, `ObservedAsAdditional`, `Suppress`.
- <xref:Bodu.Globalization.Calendar.RangeResolution.ObservedDateRangePolicy> — which occurrence date controls inclusion in a range query: `ObservedOccurrenceControlsInclusion`, `ActualOccurrenceControlsInclusion`, `BothOccurrencesControlInclusion`.
- <xref:Bodu.Globalization.Calendar.RangeResolution.INotableDateCollisionResolver> — `Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding)`. Implement this to settle same-day collisions yourself; it is consulted only under `CollisionPolicy.Custom` and is supplied through the `NotableDateService` constructor.

## Authored example

```xml
<ResolutionPolicy duplicatePolicy="KeepFirst"
                  sameDayCollisionPolicy="HighestPriorityOnly"
                  priorityDirection="HigherWins"
                  observedDateRangePolicy="ObservedOccurrenceControlsInclusion"
                  workingDays="0111110" />   <!-- Sunday-first; Mon–Fri working -->
```
