---
uid: Bodu.Globalization.Calendar.RangeResolution
---

![Bodu.Globalization.Calendar](~/images/hero-calendar.svg)

## Purpose

**Bodu.Globalization.Calendar.RangeResolution** is the chronological-window resolution pipeline used by `NotableDateService.ResolveNotableDatesInRange()` and the related window-scoped query overloads. The namespace is largely internal — it captures the per-request plan, static rule analysis, tiered occurrence materialisation, observance-adjustment evaluation, and emission ordering that turn a `(start, end, territory, calendar, filter)` request into a sorted result set.

Most consumers do not interact with this namespace directly; they call `NotableDateService` methods and let the service drive the pipeline. The types listed here are surfaced primarily for the [resolution pipeline guide](~/guides/calendar/resolution-pipeline.md) so the eight-stage walkthrough can refer to concrete names.

## Key types

- **`NotableDateRangeRequest`** — describes a chronological window query: inclusive date range, optional territory, optional calendar, optional filter context.
- **`NotableDateRangePlan`** — captures the per-request resolution plan computed from a request and the static rule analysis. Exposes which rules are eligible, which civil years to materialise, and which anchor years to compute.
- **`NotableDateRangePlanner`** — builds a `NotableDateRangePlan` from a request and a `RuleStaticAnalysis`.
- **`NotableDateRangePipeline`** — orchestrates the pipeline: planning, tiered occurrence materialisation, observance adjustment, and emission ordering.
- **`NotableDateRangeResolutionCache`** — request-scoped cache: one entry per `NotableDateCacheKey`. Entries carry state flags (`Computed`, `InWindow`, `Adjusted`, `OutOfWindow`).
- **`NotableDateCacheEntry`** — one materialised rule occurrence: originating rule profile, base date, optional adjusted form, emission state.
- **`NotableDateCacheKey`** — `(RuleName, AnchorYear, TerritoryCode, CalendarType)` tuple used as the cache key.
- **`NotableDateCacheState`** — enum: `Computed`, `InWindow`, `Adjusted`, `OutOfWindow`. Distinguishes entries eligible for emission from those present only as adjustment context.
- **`RuleStaticAnalysis`** — aggregates static, year-independent analysis of the rule set: per-rule profiles and offset-dependency index. Computed once at service construction and reused across every range request.
- **`RuleStaticProfile`** — captures static characteristics of a single rule: processing tier, transitive root anchor, base offset, min / max day-delta envelope from adjustments.
- **`RuleTier`** — enum classifying rules by processing tier so each tier can read from the cache populated by earlier tiers: `Tier0` (Fixed, DayOfWeekInMonth), `Tier1` (Algorithm), `Tier2` (OffsetFromAnchor → Tier 0 / 1), `Tier3` (OffsetFromAnchor → Tier 2).
- **`ResolvedWindowSet`** — the union of chronological windows resolved by the pipeline; consumers can introspect what is known without re-querying. Maintains a sorted list of disjoint `DateRange` intervals.

## Notes

- **Internal-leaning surface.** Most types in this namespace are intentionally narrow — they exist to make the pipeline implementation testable and observable, not to be a public extension point. The supported public extension surfaces remain `INotableDateAlgorithm`, `INotableDateCollisionResolver`, `INotableDateRuleProvider`, and the override-provider contracts.
- **Tier ordering matters.** Tier 0 rules (Fixed, DayOfWeekInMonth) resolve first because Tier 1 (Algorithm) and Tier 2 (OffsetFromAnchor) may depend on Tier 0 occurrences. Tier 3 (offset-of-offset) reads from Tier 2. The pipeline guarantees an anchor is resolved before a rule that depends on it.
- **Reach envelope.** Each `RuleStaticProfile` records the worst-case day-delta envelope that the rule's adjustment chain can produce. The planner uses this to decide which adjacent civil years must be materialised — for example, a rule observed on 31 December with a `MoveToNextWorkingDay` adjustment can roll forward into the following January, so the next civil year is materialised; symmetrically, a backward-moving adjustment on an early-January date reaches into the previous December.
- **See also:** the [resolution pipeline guide](~/guides/calendar/resolution-pipeline.md) — the eight-stage walkthrough refers to these types by name.
