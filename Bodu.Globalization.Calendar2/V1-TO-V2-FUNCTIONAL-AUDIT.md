# v1 → v2 Calendar functional audit

_Generated 2026-06-03. Audits the **source capabilities / public surface** of the v1
`Bodu.Globalization.Calendar` library against the v2 rewrite `Bodu.Globalization.Calendar2`
(namespace `Bodu.Globalization.Calendar.V2`). Companion to `V1-TO-V2-TEST-PORT.md`, which tracks
test coverage; this document tracks **functionality**._

## Purpose

The test-port report answers "is each v1 *test scenario* represented in v2?". This report answers
the prior question: "is each v1 *capability* implemented in v2, and if not, why?". It walks every
functional area of the v1 `src` tree (four namespace folders: `Globalization.Calendar`,
`.Algorithms`, `.Extensions`, `.Plugins`, `.RangeResolution`) and records the v2 disposition with
the concrete v1 → v2 type mapping.

## Status legend

| Status | Meaning |
|---|---|
| ✅ **Implemented** | v2 has a functional equivalent (shape may differ — identity is structural, not name-based). |
| 🟡 **Partial** | The core capability exists in v2; specific sub-features are missing (named in the note). |
| 🔵 **Replaced by design** | v2 deliberately achieves the same outcome through a different, usually simpler or stronger, mechanism. |
| ⛔ **Deferred** | Genuinely absent from v2; planned for a later phase (see roadmap). |
| ⚪ **Internal — not reproduced** | v1-internal architecture (no public contract); v2 reaches the same result a different way. |

## Headline capability matrix

| # | Capability area | v1 anchor types | v2 disposition |
|---|---|---|---|
| 1 | Concept/rule model & identity | `NotableDateRule`, `NotableDateRuleIdentity` (name-keyed) | ✅ Implemented (re-shaped: concept → rule, identity = resourceId+id+ruleId) |
| 2 | Resolved-occurrence result | `NotableDate` (DateTime) | ✅ Implemented (re-shaped: `DateOnly`, `IsObserved`+`ActualDate`, stable `Identity`) |
| 3 | Single-day & range query | `INotableDateService.GetNotableDates(…)` ×6 | ✅ Implemented (`Resolve(DateOnly/DateRange)` + filtered + by-year `Resolve(year, territory)` + `GetNotableDatesIn{Month,Year}`; working-day predicates as extensions) |
| 4 | Date-calculation strategies | `DateResolutionStrategy` (6 kinds) | ✅ Implemented (all 6: Fixed, DayOfWeekInMonth, WeekdayNearDate, RelativeWeekdayInMonth, OffsetFromRule, Algorithm) |
| 5 | Easter algorithms | Gregorian + Orthodox Easter | ✅ Implemented (`EasterCalculator`, Western + Orthodox) |
| 6 | Lunar / lunisolar / solar-term algorithms | Vesak, Asalha, Qingming, Losar, HinduLunar, LunarPhase | ✅ Implemented (Meeus equinox/lunar-phase + Matariki table; full solar-anchored Hindu festival set bar nakshatra-fixed Onam) |
| 7 | Algorithm registry / dispatch | `INotableDateAlgorithmRegistry`, `NotableDateAlgorithmRegistry` | ✅ Implemented (`INotableDateAlgorithm` + `NotableDateAlgorithmRegistry`, threaded through resolution and validation) |
| 8 | Observance adjustments | `ObservanceAdjustment`, `AdjustmentTrigger`/`Action`, `ObservedDateMode` | 🟡 Partial (reusable `AdjustmentPolicy`; 8/10 triggers, **full** action set incl. `ReplaceWithRule`+`Custom`, superset `EmissionMode`; only `IfNonWorkingDay` (folded into `IfWeekend`+skip) and a standalone Custom trigger absent) |
| 9 | Custom adjustment handlers | `IAdjustmentHandler`, `AdjustmentHandlerRegistry` | ✅ Implemented (`IAdjustmentHandler` + `AdjustmentHandlerRegistry` + `AdjustmentHandlerContext`, threaded through resolution and validation) |
| 10 | Conflict-aware substitution | tier pipeline + non-working context | 🔵 Replaced & **strengthened** (compute-then-place occupied-day set, opt-in `skipNonWorkingDates`) |
| 11 | Territory specificity / shadowing | `ApplySameNameTerritoryShadowing` (the v1 *bug*) | ✅ Implemented (correct redesign: `RuleApplicability.MatchSpecificity`, most-specific match wins) |
| 12 | Same-day collision resolution | `INotableDateCollisionResolver`, `DefaultNotableDateCollisionResolver` | ✅ Implemented (`sameDayCollisionPolicy` wired: KeepAll/HighestPriorityOnly/CategoryPriority/Custom + `INotableDateCollisionResolver` hook) |
| 13 | XML ingestion + schema validation | `NotableDateRuleParser`, `NotableDates.xsd`, `NotableDateRuleValidator` | ✅ Implemented (new schema: `NotableDateDocumentParser`, `NotableDates.v2.xsd`, validator + diagnostics) |
| 14 | JSON ingestion | `NotableDateRuleJsonParser`, `NotableDates.schema.json` | ✅ Implemented (`NotableDateJsonDocumentParser` + `LoadJson`; XML/JSON auto-detected for imports) |
| 15 | Declarative overrides | `INotableDateRuleOverrideProvider`, `RuleRemoval` | 🟡 Partial (`<Overrides>` Add/Patch/Remove at load; no scoped removal, no runtime mutation) |
| 16 | Runtime mutable overrides + reload | `MutableNotableDateRuleOverrideProvider`, `Invalidate`/`Reload` | ✅ Implemented (`MutableNotableDateResourceProvider` + `ReloadableNotableDateService`; reload swaps the resource, `AddReloadableNotableDateService` for DI) |
| 17 | Imports / cross-resource cherry-pick | `<UseFrom>`/`<Use>`, `NotableDateRuleMerger`, use-directives | ✅ Implemented (`<Imports>` resolved by a resolver: import-all / cherry-pick + override, policy merge, cycle detection) |
| 18 | Resource providers + path resolution | `Xml/JsonResourceNotableDateRuleProvider`, `ResourcePathResolver` | 🔵 Replaced (loader + a caller-supplied resource resolver delegate; no provider/path-resolver types) |
| 19 | Filter API | `NotableDateFilter` (14 factories + And/Or) | ✅ Implemented (`NotableDateFilter` + filtered `Resolve` overloads) |
| 20 | Working-day / traversal / fiscal extensions | `NotableDate{Only,Time,TimeOffset}Extensions`, `…FiscalExtensions`, `NotableDateContext` | ✅ Implemented (`DateOnly`/`DateTime`/`DateTimeOffset` + `NotableDateFiscalExtensions`; ambient `NotableDateContext` replaced by explicit service passing) |
| 21 | Plugin model | `ExternalPluginLoader`, trust policies, plugin interfaces | ✅ Implemented (`Bodu.Globalization.Calendar2.Plugins`: loader, trust-policy family, plugin interfaces, algorithm contribution) |
| 22 | Localization hook | `INotableDateNameLocalizer` | ✅ Implemented (`INotableDateNameLocalizer` + `NotableDateNameLocalizer` + `Localize` extensions, parent-culture/invariant fallback) |
| 23 | `TerritoryCode` value type | `TerritoryCode` (Parse/Contains, country+subdivision) | 🔵 Replaced (plain string + parent/child + `MatchSpecificity`) |
| 24 | Non-Gregorian calendars | `CalendarType` on rule, `calendar` on algorithm | ✅ Implemented for fixed dates (Hijri, UmmAlQura, Hebrew, Persian, Chinese lunisolar via the BCL, with sweep / leap-month skip / Hebrew alias) |
| 25 | Range pipeline internals | `NotableDateRangePipeline/Planner/Plan`, `RuleStaticAnalysis`/`Tier`, resolution cache | ⚪ Internal — replaced by inline two-phase resolve |
| 26 | DI registration | `Bodu.Globalization.Calendar.DependencyInjection` (sibling project) | ✅ Implemented (`Bodu.Globalization.Calendar2.DependencyInjection`, `AddNotableDateService`) |
| 27 | Regional data packs | `Bodu.Globalization.Calendar.Data.{Americas,AsiaPacific,Europe}` | ✅ Implemented (all three v2 packs; every v1 territory migrated — see below) |

**Tally:** 21 ✅ Implemented · 2 🟡 Partial · 3 🔵 Replaced by design · 1 ⚪ Internal · 0 ⛔ Deferred (counting sub-rows).

The core engine, calendars, full algorithm catalogue, all three data packs, JSON ingestion, the
imports graph, the filter API, the full query surface (single-day / range / by-year /
month / year), the full working-day extension surface (`DateOnly`/`DateTime`/`DateTimeOffset` +
fiscal), the custom-algorithm registry, the custom adjustment-handler and same-day collision hooks,
runtime reload, the localization hook, the plugin model, and DI registration are all **implemented**.
No capability is fully deferred; the two remaining partials are the last two adjustment triggers
(area 8 — `IfNonWorkingDay` folded into `IfWeekend`+skip, and a standalone Custom trigger) and scoped
override removal by year/territory (area 15 — covered today by `PatchRule` on applicability).

### Algorithm catalogue & non-Gregorian calendars (areas 6, 24)

The v2 engine now ships the astronomical/lunisolar algorithm providers dispatched by
`AlgorithmDateStrategy` key: Western/Orthodox Easter, the March/September equinoxes and Qingming
(`SolarTermCalculator`, Meeus ch. 27, with a local-time offset), the new/full-moon series
(`LunarPhaseCalculator`, Meeus ch. 49) backing Vesak / Asalha Puja / Losar, the gazetted Matariki
table, and the verified Hindu festivals (Diwali, Holi, Navaratri) via `HinduLunarCalculator`.
Regionally ambiguous and non-lunar Hindu festivals are intentionally omitted. Non-Gregorian
**fixed dates** project onto the Gregorian year through `CalendarSystems` for the Hijri, Umm al-Qura,
Hebrew, Persian, and Chinese lunisolar calendars (calendar-year sweep, Chinese leap-month skip, and
the Hebrew leap-year month alias). Islamic/Hebrew/Persian dates use the BCL tabular calendars, so
they match the BCL rather than local moon-sighting.

### Regional data packs (area 27)

All three v2 packs mirror the v1 data assemblies (embedded per-country resources + a
`<Region>CalendarData` factory + a test project), and **every v1 territory is migrated** (38
countries): **Americas** (US, CA), **AsiaPacific** (AU, CN, IN, JP, KR, MY, NZ, SG), and **Europe**
(28 countries). Each region file is a self-contained v2-schema migration of its v1 counterpart with
the `<UseFrom>` imports flattened into explicit rules; the European long tail was generated by
`tools/migrate_europe.py`, which resolves the v1 import graph transitively. Islamic, Hindu, and
lunisolar festivals follow the engine's computed reckoning (within a day or two of the gazetted
dates); the full Hindu festival set is computed by the solar-anchored lunar algorithm (only the
nakshatra-fixed Onam is out of scope), and statutory multi-day holidays carry their `durationDays`
span.

---

## Detail by area

### 1. Concept/rule model & identity — ✅ Implemented (re-shaped)

This is the headline redesign. v1 modelled a flat `NotableDateRule` keyed by **display name**
(`NotableDateRuleIdentity` = Name + RuleName + Territory + Calendar), which fused the *concept*, its
*calculation rules*, and its *observance* and produced the name-shadowing defect.

v2 separates them:

| v1 | v2 | Note |
|---|---|---|
| `NotableDateRule` (flat, name-keyed) | `NotableDateDefinition` (concept) → `NotableDateRule` (id, priority, applicability, strategy, policy refs) | One concept owns many explicitly-identified rule variants. |
| `NotableDateRuleIdentity` (Name+RuleName+Territory+Calendar) | `NotableDateRuleIdentity` (resourceId + notableDate.id + rule.id) | Identity is structural, never the display name; `displayName` duplicates are explicitly allowed. |
| `NotableDateRuleReference`, `NotableDateRuleResolution`, `NotableDateRuleIndex` | n/a | v1 name-resolution machinery; v2 references rules by `(notableDateRef, ruleRef)` directly. |
| `NotableDateProvenance` enum | n/a | Layer-origin tracking tied to the v1 override/import layering; not needed without imports. |

"Coexist vs. replace" is now an authoring decision (distinct rule ids + priority/specificity), not
inferred from a shared title — which is precisely what fixes the original bug (see area 11).

### 2. Resolved-occurrence result — ✅ Implemented (re-shaped)

| v1 `NotableDate` | v2 `NotableDate` |
|---|---|
| `Date` (`DateTime`), `EndDate`, `DurationDays` | `Date` (`DateOnly`), `DurationDays`, computed `EndDate`; range inclusion is span-overlap-aware |
| `Name`, `DisplayName` | `DisplayName` + `NotableDateId`/`RuleId` (from `Identity`) |
| `WasAdjusted`, `AdjustmentReason` (record) | `IsObserved`, `ActualDate`, `AdjustmentPolicyId`, `AdjustmentReason` (string) |
| `Category`, `IsNonWorkingDay`, `TerritoryCode` | `Category`, `TerritoryCode` (non-working tracked on the rule/occupied set) |
| `Priority`, `Tags`, `Comment`, `CalendarType` | carried on the rule, not the result; tags/comment deferred; Gregorian-only |

### 3. Single-day & range query — ✅ Implemented

| v1 `INotableDateService` member | v2 |
|---|---|
| `GetNotableDates(date, …)` | ✅ `Resolve(DateOnly, territory)` |
| `GetNotableDates(start, end, …)` | ✅ `Resolve(DateRange, territory)` |
| `GetNotableDates(year, …)` | ✅ `Resolve(year, territory)` extension (plus `GetNotableDatesInMonth`/`InYear`) |
| `…(…, NotableDateFilter, …)` overloads | ✅ `Resolve(DateOnly/DateRange, territory, filter)` (area 19) |
| `IsWeekend`, `IsNonWorkingDay`, `IsHolidayNonWorkingDay`, `WorkingWeek` | ✅ `DateOnly`/`DateTime`/`DateTimeOffset` extensions over the service (area 20) |
| `Invalidate`, `Reload` | ✅ `MutableNotableDateResourceProvider.Reload` + `ReloadableNotableDateService` (area 16) |
| `GetSupportedTerritories`, `GetSupportedCalendars` | ⛔ not exposed |

Inclusion is decided by the **emitted** date in both v1 and v2, so single-day and range queries
agree by construction — the original width-dependence defect does not recur.

### 4. Date-calculation strategies — ✅ Implemented (full parity)

All six v1 `DateResolutionStrategy` kinds have a v2 `IDateCalculationStrategy` implementation:

| v1 strategy | v2 strategy |
|---|---|
| `Fixed` | `FixedDateStrategy` |
| `DayOfWeekInMonth` (incl. `Last`) | `DayOfWeekInMonthStrategy` (+ `WeekOrdinal`) |
| `WeekdayNearDate` (OnOrAfter/OnOrBefore/Nearest) | `WeekdayNearDateStrategy` (+ `WeekdayProximity`) |
| `RelativeWeekdayInMonth` | `RelativeWeekdayInMonthStrategy` |
| `OffsetFromAnchor` | `OffsetFromRuleStrategy` (references rule identity; `StrategyResolutionContext` resolves anchors with a cycle guard) |
| `Algorithm` | `AlgorithmDateStrategy` (key dispatch) |

Sub-features deferred: `skipLeapMonth` / `sweepCalendarYears` (non-Gregorian month arithmetic) and
CLR-typed algorithm references (v2 dispatches by string key only).

### 5–7. Algorithms & registry

- **5. Easter — ✅** `EasterCalculator` computes Western (Gregorian Computus) and Orthodox
  (Julian, projected to Gregorian), reachable via `AlgorithmDateStrategy` keys
  (`western-easter`, `orthodox-easter`). Good-Friday / Easter-Monday derive via `OffsetFromRule`.
- **6. Lunar / lunisolar / solar-term — ⛔** `Vesak`, `AsalhaPuja`, `Qingming`, `Losar`,
  `HinduLunar` (+ `HinduLunarMonth`/`HinduPaksha`) and the `LunarPhaseAlgorithm` ephemeris helper
  are not ported; they require lunar/lunisolar machinery and (mostly) non-Gregorian calendars.
- **7. Registry — 🔵** v1's `INotableDateAlgorithmRegistry`/`NotableDateAlgorithmRegistry` (and
  plugin-contributed algorithms) are replaced by direct key dispatch inside `AlgorithmDateStrategy`;
  there is no public registry to register into.

### 8–9. Adjustments — 🟡 Partial; custom handlers ✅ Implemented

v1 attached a rich `ObservanceAdjustment` to each rule; v2 hoists adjustments into reusable,
scope-matched `AdjustmentPolicy` objects referenced by rules — an improvement in authoring reuse.

| Facet | v1 | v2 | Status |
|---|---|---|---|
| Triggers | Always, IfDayOfWeek, IfWeekend, IfWeekday, IfNonWorkingDay, IfBeforeFixedDate, IfAfterFixedDate, IfLeapYear, IfNthOccurrenceInMonth, Custom | Always, IfDayOfWeek, IfWeekend, IfWeekday, **IfLeapYear**, **IfBeforeFixedDate**, **IfAfterFixedDate**, **IfNthOccurrenceInMonth** | 🟡 8 of 10 (IfNonWorkingDay folded into IfWeekend+`skipNonWorkingDates`; no standalone Custom trigger) |
| Actions | None, AddDays, MoveToNextWeekday, MoveToPreviousWeekday, MoveToNextWorkingDay, ReplaceWithNamedDate, Custom | None, AddDays, MoveToNextWeekday, MoveToPreviousWeekday, MoveToNextWorkingDay, **MoveToPreviousWorkingDay**, **ReplaceWithRule**, **Suppress**, **Custom** | ✅ full superset (`ReplaceWithRule` = v1 `ReplaceWithNamedDate`; + prev-working-day & suppress) |
| Emission | `ObservedDateMode`: ActualOnly, ObservedOnly, ActualAndObserved | `EmissionMode`: ActualOnly, ObservedOnly, ActualAndObserved, **ObservedAsAdditional**, **Suppress** | ✅ superset |
| Reason | `AdjustmentReason` record | `AdjustmentPolicyId` + reason string on `NotableDate` | ✅ re-shaped |
| Custom handlers | `IAdjustmentHandler` + `AdjustmentHandlerRegistry` + context/result | `IAdjustmentHandler` + `AdjustmentHandlerRegistry` + `AdjustmentHandlerContext` | ✅ Implemented |

`ReplaceWithRule` resolves the observed date from another rule's occurrence for the same year (via
`StrategyResolutionContext.ResolveReference`); `Custom` dispatches to an `IAdjustmentHandler`
registered on the service, falling back to the calculated date when no handler is bound. Both are
validated at load (reference resolution / handler-key presence) and proven by `CustomAdjustmentTests`;
the extended triggers are proven by `ExtendedTriggerTests`.

### 10. Conflict-aware substitution — 🔵 Replaced & strengthened

v1 relied on the tiered pipeline reading a mutable non-working-day context during adjustment. v2
replaces this with an explicit, deterministic two-phase resolve in `NotableDateService`: phase one
computes every actual occurrence and seeds an `occupied` `HashSet<DateOnly>`; phase two places
observed dates in an explicit precedence order (earliest actual date → priority → identity) so an
opt-in `skipNonWorkingDates` substitute advances past days already claimed by another holiday. This
correctly resolves the Christmas-Sat→Mon / Boxing-Sun→Tue-when-Mon-taken case that motivated the
rewrite (proven by `AdjacentHolidayTests`, 2021 and 2016).

### 11. Territory specificity / shadowing — ✅ Implemented (correct redesign)

v1's `NotableDateRangePlanner.ApplySameNameTerritoryShadowing` shadowed by **shared display name**,
which is the documented bug (it could not distinguish a true specialization from two unrelated dates
that merely share a title). v2 shadows by **territory specificity within a concept**:
`RuleApplicability.MatchSpecificity` returns the length of the most-specific matching territory code,
and `NotableDateService.GatherCandidates` keeps only the rules whose specificity equals the
per-concept-per-year maximum. A narrower `AU-WA` rule shadows the broader `AU` rule for WA, while
`AU-VIC` (no subdivision rule) falls back to the national rule. Proven by
`AustraliaKnownAnswerTests.Resolve_WhenSubdivisionRuleExists_ShadowsNationalRuleForThatTerritory`
plus the WA/NT Anzac substitute rows.

### 12. Same-day collision resolution — ✅ Implemented

v2 wires `ResolutionPolicy.sameDayCollisionPolicy` (`CollisionPolicy`: KeepAll, HighestPriorityOnly,
CategoryPriority, Custom) in `NotableDateService.ApplySameDayCollisionPolicy`: occurrences sharing an
emitted date are grouped and arbitrated by priority (honouring `PriorityDirection`), then by category
rank, or delegated to a caller-supplied `INotableDateCollisionResolver` when the policy is `Custom`.
`KeepAll` preserves the prior stable-sort behaviour. Proven by `CollisionResolutionTests`.

### 13–14. Ingestion & schema

- **13. XML + validation — ✅** v1's `NotableDateRuleParser` + embedded `NotableDates.xsd` +
  `NotableDateRuleValidator` become v2's `NotableDateDocumentParser` + embedded `NotableDates.v2.xsd`
  + `NotableDateRuleValidator` (→ `NotableDateValidationDiagnostic`/`Severity`, throwing
  `NotableDateValidationException`). Root element changes `<NotableDates>` → `<NotableDateResource>`
  (same `urn:bodu:globalization:calendar` namespace family). `ParsedNotableDateDocument` is preserved
  as an intermediate. The XSD carries the full forward-looking vocabulary; the runtime validator
  enforces the implemented subset.
- **14. JSON — ✅** v2 ships `NotableDates.v2.schema.json` and `NotableDateJsonDocumentParser` +
  `NotableDateResourceLoader.LoadJson`, a first-class equal to the XML loader. The JSON shape mirrors
  the XML vocabulary one-to-one (camelCase strategy discriminators, a `rules[].adjustments` array),
  and import graphs auto-detect JSON vs. XML per resource (`ParseAny`).

### 15–18. Overrides, imports, providers

- **15. Declarative overrides — 🟡** v2 parses an `<Overrides>` block into
  `AddRuleOverride` / `PatchRuleOverride` / `RemoveRuleOverride` and applies them at load via
  `NotableDateRuleOverrideApplier` (an override whose target matches zero rules is an error). v1's
  `RuleRemoval` scoping (by year/territory) is not reproduced — v2 removes/patches by exact rule
  identity.
- **16. Runtime mutable overrides + reload — ✅** a `NotableDateResource` stays immutable, but
  `MutableNotableDateResourceProvider` swaps the resource currently in effect and
  `ReloadableNotableDateService` (an `INotableDateService`) rebuilds its resolution state on the next
  query, so a long-lived consumer — including the `AddReloadableNotableDateService` DI singleton —
  observes reloaded data. Runtime override mutation is performed by loading a fresh resource (whose
  `<Overrides>` are applied at load) and calling `Reload`. Proven by `ReloadableNotableDateServiceTests`.
- **17. Imports / cherry-pick — ✅** `<Imports>`/`<Import>`/`<Use>` are resolved recursively by
  `NotableDateResourceLoader` against a caller-supplied resource resolver: import-all or per-concept
  cherry-pick with rename (`as`) and territory/category/non-working overrides (`ApplyUse`), adjustment
  policy and concept merging (local wins on conflict), and cycle / missing-resource / missing-concept
  diagnostics.
- **18. Providers + path resolution — 🔵** v1's `INotableDateRuleProvider`,
  `Xml/JsonResourceNotableDateRuleProvider`, `NotableDateRuleResourceProviderBase` (assembly-chain
  search, format dispatch, flatten pipeline) and `IResourcePathResolver`/`ResourcePathResolver`/
  `ResourcePathResolverOptions` are replaced by the single-resource `NotableDateResourceLoader.Load`.
  The multi-resource provider chain is tied to imports (area 17) and deferred with it.

### 19–22. Filter, extensions, plugins, localization

- **19. Filter API — ✅** `NotableDateFilter` is reproduced as a composable predicate (`ForCategory`,
  `ForAnyCategory`, `IsNonWorkingDay`, `WasAdjusted`, `WithName`/`WithAnyName`, `WithId`,
  `WithTag`/`WithAnyTag`/`WithAllTags`, `WithMinDuration`, `InDateRange`, `AllOf`/`AnyOf`,
  `And`/`Or`/`Not`), surfaced through filtered `Resolve(date|range, territory, filter)` overloads.
- **20. Extensions — ✅** the working-day / traversal surface is reproduced over `DateOnly`
  (`NotableDateOnlyExtensions`), `DateTime` (`NotableDateTimeExtensions`), and `DateTimeOffset`
  (`NotableDateTimeOffsetExtensions`) — `IsWeekend`, `IsWorkingDay`/`IsNonWorkingDay`, `IsNotableDate`,
  `Next/Previous WorkingDay`, `SnapToWorkingDay[Backward]`/`SnapToNearestWorkingDay`, `AddWorkingDays`,
  `WorkingDaysBetween`, `Enumerate*`, `GetNotableDates` — with the `DateTime`/`DateTimeOffset` traversal
  results preserving the time-of-day (and kind/offset). `NotableDateFiscalExtensions` adds the
  first/last working day of the fiscal year and quarter (configurable start month). The ambient
  `NotableDateContext` is intentionally **not** reproduced: every extension takes the service and
  territory explicitly.
- **21. Plugins — ✅** `Bodu.Globalization.Calendar2.Plugins` ships `NotableDatePluginLoader`, the
  trust-policy family (`AllowAll`/`Delegating`/`Composite`, `IPluginTrustPolicy`), plugin interfaces
  (`INotableDatePlugin`/`INotableDateAlgorithmPlugin`), `NotableDatePluginAttribute`, and the plugin
  exception hierarchy; algorithm plugins contribute into the `NotableDateAlgorithmRegistry`.
- **22. Localization — ✅** `INotableDateNameLocalizer.GetDisplayName(notableDate, culture)` is
  reproduced, with a dictionary-backed `NotableDateNameLocalizer` (keyed by concept id + culture, with
  `fr-FR` → `fr` → invariant fallback) and `NotableDate.Localize(localizer, culture)` /
  `IReadOnlyList<NotableDate>.Localize(…)` extensions that return occurrences with localized
  `DisplayName`. Resolution stays culture-agnostic; localization is applied on demand.

### 23–24. Territory & calendar value types

- **23. `TerritoryCode` — 🔵** v1's `TerritoryCode` value type (`Parse`/`TryParse`/`ParseList`/
  `Contains`, `Country`/`Subdivision`/`HasSubdivision`) is replaced by plain `string` territory codes
  with parent/child matching (`RuleApplicability.MatchesTerritory`) and specificity ranking
  (`MatchSpecificity`). The matching *behaviour* is implemented; the value type is not adopted.
- **24. Non-Gregorian calendars — ⛔** v1 carried a `CalendarType` per rule and a `calendar`
  parameter on every algorithm (Hebrew/Islamic/Hindu/Persian/Chinese). v2's `CalendarSystem` enum is
  **Gregorian-only** and the validator rejects anything else. This is the single largest deferred
  capability and gates the `Global*ResourceTests` and the lunar/lunisolar algorithms (area 6).

### 25. Range pipeline internals — ⚪ Internal — not reproduced

v1's performance architecture — `NotableDateRangePipeline`/`Planner`/`Plan`/`Request`, the
`RuleStaticAnalysis`/`RuleStaticProfile`/`RuleTier` reach-hint static analysis, the
`NotableDateRangeResolutionCache` (+ cache key/entry/state), `ResolvedNotableDate`,
`ResolvedWindowSet`, and `CachingCalculationAnchorResolver` — has no public contract and is **not**
reproduced. v2 resolves inline in a two-phase `NotableDateService` with a ±1-year fringe scan and a
per-call occupied-day set; offset anchors resolve through `StrategyResolutionContext` (cycle-guarded)
rather than a tiered cache. The observable results match; the tiering/caching/reach-hint machinery is
a Phase-2 optimization if profiling ever demands it.

### 26. DI registration — ⛔ Deferred

The sibling v1 project `Bodu.Globalization.Calendar.DependencyInjection` (`IServiceCollection`
extensions) has no v2 counterpart; v2 ships `src` + `test` only.

---

## Deferred roadmap (functional)

Ordered roughly by unblocking value, reconciled with `V1-TO-V2-TEST-PORT.md`:

1. ~~**Territory-specificity shadowing**~~ — ✅ **done**.
2. ~~**Non-Gregorian calendars** + the algorithm catalogue~~ — ✅ **done** for fixed dates (Hijri /
   Umm al-Qura / Hebrew / Persian / Chinese lunisolar) and the astronomical/lunisolar algorithms
   (equinox, lunar-phase, Qingming, Matariki). The twice-in-a-Gregorian-year Islamic case now emits
   both occurrences, and the full Hindu festival set is computed by a solar-anchored lunar algorithm
   (only nakshatra-fixed Onam remains out of scope).
2a. ~~**Regional data packs**~~ — ✅ **done**: all three packs migrated for every v1 territory (38
    countries), with multi-day `durationDays` spans on the statutory multi-day holidays.
3. ~~**JSON ingestion**~~ — ✅ **done** (`NotableDateJsonDocumentParser` + `LoadJson`).
4. ~~**Imports / cherry-pick**~~ — ✅ **done** (`<Imports>` + a resource-resolver delegate; a typed
   provider/path-resolver layer remains optional, area 18).
5. ~~**Filter API**~~ — ✅ **done** (`NotableDateFilter` + filtered `Resolve`).
6. ~~**Custom algorithm registry / plugin model / DI / extension surface**~~ — ✅ **done**
   (`NotableDateAlgorithmRegistry`; `Bodu.Globalization.Calendar2.Plugins`;
   `Bodu.Globalization.Calendar2.DependencyInjection`; `NotableDateOnlyExtensions`).
7. ~~**Multi-day spans, tags, the twice-Islamic case, the full Hindu set**~~ — ✅ **done**.

Remaining (small, peripheral):

8. **Same-day collision resolver** — wire `ResolutionPolicy.sameDayCollisionPolicy`/`DuplicatePolicy`
   and add a custom-resolver hook (area 12).
9. **Custom adjustment handlers** + the remaining triggers (`IfNonWorkingDay`, `IfBefore/AfterFixedDate`,
   `IfLeapYear`, `IfNthOccurrenceInMonth`) and actions (`ReplaceWithNamedDate`/`ReplaceWithRule`,
   `Custom`) (areas 8–9).
10. **Runtime mutable overrides + reload** (area 16); **localization hook** (area 22); the
    **DateTime/DateTimeOffset/fiscal extension** overloads and the `TerritoryCode` value type
    (areas 20, 23).
