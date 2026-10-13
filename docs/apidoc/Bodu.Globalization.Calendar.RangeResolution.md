---
uid: Bodu.Globalization.Calendar.RangeResolution
---

![Bodu.Globalization.Calendar.RangeResolution](~/images/hero-calendar.svg)

# Bodu.Globalization.Calendar.RangeResolution

## Purpose

**Bodu.Globalization.Calendar.RangeResolution** holds the policy vocabulary that governs how a document's duplicate declarations are settled when it is loaded, how same-day collisions and overlapping spans are settled, which priority wins, what an adjustment emits, which span of an occurrence controls range inclusion, and which days count as the working week.

These policies are carried by the document's `<ResolutionPolicy>` element and surfaced as <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy> on the loaded <xref:Bodu.Globalization.Calendar.NotableDateResource>. They are resource-level (authored once per document) rather than per-query.

## Static documentation

- **[The resolution pipeline](~/guides/calendar/resolution-pipeline.md)** - the stages a query runs through and where each policy applies.
- **[Rule identity, priority, and observed-date resolution](~/guides/calendar/identity-and-resolution.md)** - collision settlement, duplicate identifiers, and observed-date range inclusion in detail.

## Key types

- <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy> - the policy bundle: `DuplicatePolicy`, `SameDayCollisionPolicy`, `SpanCollisionPolicy`, `PriorityDirection`, `ObservedDateRangePolicy`, the category precedence, and the working week (a `Bodu.Core` `DayOfWeekSet`, default Monday-Friday). `ResolutionPolicy.Default` is the all-defaults instance.
- <xref:Bodu.Globalization.Calendar.RangeResolution.DuplicatePolicy> - how the loader settles two concepts, or two rules of a concept, that the loaded document declares with one identifier: `Error` (`BODU-CAL-DUP-ND` / `BODU-CAL-DUP-RULE`), `KeepFirst`, `KeepLast`, or `Merge` (a later concept's rules appended to the first).
- <xref:Bodu.Globalization.Calendar.RangeResolution.CollisionPolicy> - how distinct occurrences that collide are settled: `KeepAll`, `HighestPriorityOnly`, `CategoryPriority`, `Custom`. `SameDayCollisionPolicy` applies it to occurrences that share an emitted date, and `SpanCollisionPolicy` then to every chain of occurrences connected through overlapping spans that holds a multi-day occurrence.
- <xref:Bodu.Globalization.Calendar.RangeResolution.PriorityDirection> - whether a higher or lower `Priority` wins: `HigherWins`, `LowerWins`.
- <xref:Bodu.Globalization.Calendar.RangeResolution.EmissionMode> - what an adjustment emits: `ActualOnly`, `ObservedOnly`, `ActualAndObserved`, `Suppress`. `ObservedAsAdditional` is obsolete: it behaves identically to `ActualAndObserved`, is normalized to it, and is kept only for document and binary compatibility.
- <xref:Bodu.Globalization.Calendar.RangeResolution.ObservedDateRangePolicy> - which span a range query tests against the window: the emitted span (`ObservedOccurrenceControlsInclusion`, the default), the actual span (`ActualOccurrenceControlsInclusion`), or either (`BothOccurrencesControlInclusion`).
- <xref:Bodu.Globalization.Calendar.RangeResolution.INotableDateCollisionResolver> - `Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding)`. Implement this to settle collisions yourself; it is consulted only under `CollisionPolicy.Custom`, for a same-day group with its shared date and for an overlap chain with its earliest date, and is supplied through <xref:Bodu.Globalization.Calendar.NotableDateServiceOptions>`.CollisionResolver` when the `NotableDateService` is constructed.

## Authored example

```xml
<ResolutionPolicy duplicatePolicy="KeepFirst"
                  sameDayCollisionPolicy="HighestPriorityOnly"
                  priorityDirection="HigherWins"
                  observedDateRangePolicy="ObservedOccurrenceControlsInclusion"
                  workingDays="0111110" />   <!-- Sunday-first; Mon-Fri working -->
```
