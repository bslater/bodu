---
uid: Bodu.Globalization.Calendar.RangeResolution
---

![Bodu.Globalization.Calendar.RangeResolution](~/images/hero-calendar.svg)

# Bodu.Globalization.Calendar.RangeResolution

## Purpose

**Bodu.Globalization.Calendar.RangeResolution** holds the policy vocabulary that governs how same-day collisions are settled, which priority wins, what an adjustment emits, and which days count as the working week. It also defines three settings that are **reserved** - a document may set them and they are stored, but the engine does not apply them yet ([#1119](https://github.com/bslater/bodu/issues/1119)): the duplicate policy (duplicate identifiers are always load errors), the span collision policy (overlapping spans are never reconciled), and the observed-date range policy (a query always includes an occurrence whose emitted span intersects the window).

These policies are carried by the document's `<ResolutionPolicy>` element and surfaced as <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy> on the loaded <xref:Bodu.Globalization.Calendar.NotableDateResource>. They are resource-level (authored once per document) rather than per-query.

## Static documentation

- **[The resolution pipeline](~/guides/calendar/resolution-pipeline.md)** - the stages a query runs through and where each policy applies.
- **[Rule identity, priority, and observed-date resolution](~/guides/calendar/identity-and-resolution.md)** - collision settlement, duplicate identifiers, and observed-date range inclusion in detail.

## Key types

- <xref:Bodu.Globalization.Calendar.RangeResolution.ResolutionPolicy> - the policy bundle: `SameDayCollisionPolicy`, `PriorityDirection`, the category precedence, the working week (a `Bodu.Core` `DayOfWeekSet`, default Monday-Friday), and the reserved `DuplicatePolicy`, `SpanCollisionPolicy`, and `ObservedDateRangePolicy`. `ResolutionPolicy.Default` is the all-defaults instance.
- <xref:Bodu.Globalization.Calendar.RangeResolution.DuplicatePolicy> - reserved: `Error`, `KeepFirst`, `KeepLast`, `Merge` are accepted and stored, and duplicate concept or rule identifiers are load errors whatever the value.
- <xref:Bodu.Globalization.Calendar.RangeResolution.CollisionPolicy> - how distinct rules landing on the same emitted date are settled: `KeepAll`, `HighestPriorityOnly`, `CategoryPriority`, `Custom`. Only `SameDayCollisionPolicy` is applied; the span collision policy takes the same values but is reserved.
- <xref:Bodu.Globalization.Calendar.RangeResolution.PriorityDirection> - whether a higher or lower `Priority` wins: `HigherWins`, `LowerWins`.
- <xref:Bodu.Globalization.Calendar.RangeResolution.EmissionMode> - what an adjustment emits: `ActualOnly`, `ObservedOnly`, `ActualAndObserved`, `Suppress`. `ObservedAsAdditional` is obsolete: it behaves identically to `ActualAndObserved`, is normalized to it, and is kept only for document and binary compatibility.
- <xref:Bodu.Globalization.Calendar.RangeResolution.ObservedDateRangePolicy> - reserved: `ObservedOccurrenceControlsInclusion`, `ActualOccurrenceControlsInclusion`, `BothOccurrencesControlInclusion` are accepted and stored, and every query includes an occurrence by its emitted span, as the first value describes.
- <xref:Bodu.Globalization.Calendar.RangeResolution.INotableDateCollisionResolver> - `Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding)`. Implement this to settle same-day collisions yourself; it is consulted only under `CollisionPolicy.Custom` and is supplied through <xref:Bodu.Globalization.Calendar.NotableDateServiceOptions>`.CollisionResolver` when the `NotableDateService` is constructed.

## Authored example

```xml
<!-- duplicatePolicy and observedDateRangePolicy are reserved: accepted and stored, not yet applied. -->
<ResolutionPolicy duplicatePolicy="KeepFirst"
                  sameDayCollisionPolicy="HighestPriorityOnly"
                  priorityDirection="HigherWins"
                  observedDateRangePolicy="ObservedOccurrenceControlsInclusion"
                  workingDays="0111110" />   <!-- Sunday-first; Mon-Fri working -->
```
