# v1 → v2 Calendar: Type & Method Migration Audit

A member-level companion to `V1-TO-V2-FUNCTIONAL-AUDIT.md`. Where that document maps the 26 **capability areas**, this one walks **every public and protected type and member** in the v1 library (`Bodu.Globalization.Calendar`) and records its disposition in v2 (`Bodu.Globalization.Calendar2`). Private members are not enumerated individually but their types are noted by purpose.

Scope covered: ~110 v1 top-level types across the five v1 namespaces — `Globalization.Calendar` (core engine + rule ingestion), `.Algorithms`, `.Extensions`, `.RangeResolution`, and `.Plugins`.

## How to read a disposition

| Marker | Meaning |
|---|---|
| ✅ **Migrated** | The member exists in v2. Sub-tags: **[1:1]** same name & shape · **[renamed→X]** · **[reshaped]** signature/semantics changed · **[merged→X]** folded into a broader member · **[split→X/Y]** · **[moved→X]** relocated to another type. |
| 🔵 **Replaced** | The capability exists, delivered through a deliberately different mechanism (named in the row). |
| ⚪ **Internal** | v1 internal plumbing intentionally not reproduced as a distinct type; the behaviour is inlined elsewhere (named). |
| ⛔ **Not migrated** | Absent from v2. **[deliberate: reason]** — an intentional design choice (cited to the functional audit or obvious intent). **[GAP — review]** — no equivalent by name *or* concept; a candidate oversight. |

**Method:** the audit was produced by six parallel per-namespace passes, each enumerating every type and member from the v1 source and confirming the v2 disposition by searching the v2 source. **Every ⛔ [GAP — review] item below was then independently re-verified** by direct search of both v1 and v2 (the v1 member exists; the v2 counterpart is absent by name and concept).

---

## Headline: what was genuinely *not* migrated

Every numbered capability area in the functional audit is implemented or a deliberate design replacement — and that holds up at the member level: the overwhelming majority of v1 members are **✅ migrated** (mostly *reshaped*, as the API moved from `DateTime`→`DateOnly`, name-keyed→id-keyed identity, enum-discriminator→polymorphic strategies, ambient context→explicit service, and per-rule inline adjustments→reusable policies), **🔵 replaced** by a documented mechanism, or **⚪ internal** plumbing folded into the two-phase resolver.

The following are the **only confirmed gaps** — members present in v1 with no v2 equivalent by name or concept. They are the actionable output of this audit.

> **Resolution status (2026-06-04):** **all eight gaps are now CLOSED** — A1, A2, A3, A4, and B3 first, then A5, B1, and B2 — each implemented with tests and marked ✅ in the tables below. Where a per-section detail table further down still carries an `⛔ [GAP — review]` marker for one of these items, treat that marker as **superseded by this banner**. There are no known remaining gaps between the v1 surface and v2.

### A. Material capability gaps (a user-facing feature is absent)

| # | v1 member(s) | What is lost | Where |
|---|---|---|---|
| A1 ✅ | `NextNotableDate`, `PreviousNotableDate` (on `DateOnly` & `DateTime`) | Walk forward/back to the next/previous notable date from a given day. v2 can *enumerate* notable dates in a range but cannot step to the next/previous one. | Extensions |
| A2 ✅ | `NextNonWorkingDay`, `PreviousNonWorkingDay`, `EnumerateNonWorkingDays` (on `DateOnly` & `DateTime`) | The non-working-day mirror of the working-day traversal that v2 *does* ship. v2 has `NextWorkingDay`/`PreviousWorkingDay`/`EnumerateWorkingDays` but not the non-working counterparts. | Extensions |
| A3 ✅ | `DateRange.Contains(DateRange)`, `DateRange.Intersects(DateRange)` | Range-vs-range containment and overlap tests. v2 `DateRange` only has `Contains(DateOnly)`. | Core (`DateRange`) |
| A4 ✅ | `FileHashPluginTrustPolicy`, `StrongNamePluginTrustPolicy` | Two of the four built-in plugin trust policies. Worse for strong-name: v2 changed `PluginTrustContext.AssemblyName` from `AssemblyName` to `string`, dropping the public-key token, so a strong-name policy cannot even be reconstructed from the v2 context. A hash-allowlist can still be hand-rolled via `DelegatingPluginTrustPolicy` (the `FileHash` is still on the context). | Plugins |
| A5 ✅ | `INotableDateProvider` (`GetDates(int year, …)`, `SupportsYear`, `Min/MaxSupportedYear`) | The code-first extensibility seam that returns ready-made `NotableDate`s for a year. v2 plugins/algorithms contribute only an anchor `DateOnly`; full notable dates must be authored as resource rules. | Ingestion |

### B. Expressiveness gaps (the capability is narrower in v2)

| # | v1 member | What is narrower | Where |
|---|---|---|---|
| B1 ✅ | `ObservanceAdjustment.EffectiveFromYear` / `EffectiveToYear` | A v1 adjustment could be year-bounded directly. v2 `AdjustmentPolicy`/`AdjustmentScope` have no year bounds — only *rules* are year-scoped (via `RuleApplicability`), so a year-limited *observance shift* must be modelled as separate rules. | Core (`AdjustmentPolicy`) |
| B2 ✅ | `ObservanceAdjustment.HandlerParameters` | v1 custom adjustment handlers received an author-supplied `IReadOnlyDictionary<string,string>`. v2 custom trigger/action handlers receive `BaseDate`/`Territory`/`Policy`/occupied-set/resolution-context but **no author parameters**. | Core (`AdjustmentPolicy` / handler contexts) |
| B3 ✅ | `TerritoryCode.ParseList(string?)` | Comma-separated multi-territory parsing. The *capability* survives (rule applicability holds a territory list parsed at load), but the convenience parser on the value type is gone. | Core (`TerritoryCode`) |

### C. Deliberate omissions (documented design — not "missed")

For completeness, the notable members intentionally dropped, each with a design rationale in the functional audit or an obvious replacement: the `IfNonWorkingDay` trigger (folded into `IfWeekend` + `skipNonWorkingDates`); `INotableDateRulePlugin` (v2 plugins contribute algorithms only, no rule providers); `NotableDateProvenance` and the whole `RangeResolution` pipeline (internal — replaced by the two-phase resolver); `NotableDateServiceOptions` (→ explicit ctor params); `INotableDateService.GetSupportedTerritories/Calendars`, `Invalidate`, `WorkingWeek` (immutable-resource / hard-coded weekend model); per-result `CalendarType` and `Comment` (carried on the rule); the v1 algorithm *provider* metadata and per-algorithm year-range guards (metadata authored on rules; guarding centralised to a 1–9999 clamp); CLR-typed algorithm references (`AlgorithmType`/`AlgorithmMonth`/`AlgorithmDay`); and `CalendarThrowHelper` (v2 uses `Bodu.ThrowHelper` + `Calendar2ResourceStrings`). Nakshatra-fixed **Onam** is the one intentionally unmodelled festival, and it never existed as a v1 type.

> **Net:** of the entire v1 surface, eight members/families had no v2 equivalent, and **all eight are now closed**: A1 notable-date traversal, A2 non-working-day traversal, A3 `DateRange` set operations, A4 plugin trust policies, B3 territory-list parsing, A5 the code-first `INotableDateProvider` seam, B1 policy-level year scope, and B2 custom-handler parameters. Everything else migrated, was replaced by design, or is internal plumbing — there are no known remaining gaps.

---
## Core engine, model & adjustment

*v1 namespace `Bodu.Globalization.Calendar` — the resolution-engine, result-model and adjustment cluster.*

### AdjustmentAction (enum, public)
**Type disposition:** ✅ Migrated [1:1, reshaped superset] — v2 `AdjustmentAction`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `None`, `AddDays`, `MoveToNextWeekday`, `MoveToPreviousWeekday`, `MoveToNextWorkingDay`, `ReplaceWithNamedDate`, `Custom` | ✅ Migrated | v2 keeps `None`/`AddDays`/`MoveToNextWeekday`/`MoveToPreviousWeekday`/`MoveToNextWorkingDay`/`Custom` 1:1; `ReplaceWithNamedDate` [renamed→`ReplaceWithRule`] (resolves by rule identity, not display name); adds **`MoveToPreviousWorkingDay`** + **`Suppress`**. |

### AdjustmentApplyResult (readonly record struct, internal)
**Type disposition:** ⚪ Internal — v1 adjuster-pipeline DTO; not reproduced. v2 inlines the observed-date outcome in `NotableDateService.EmitCandidate`/`ComputeObservedDate` (a bare `DateOnly`), with `EmissionMode` deciding emission.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor (`Activated`,`AdjustedDate`,`Trigger`,`Action`,`HandlerKey`,`IsNonWorkingOverride`) | ⚪ Internal | No standalone result struct; `ComputeObservedDate` returns the observed `DateOnly`, activation implicit in policy selection. |
| `NotActivated(DateTime)` (static) | ⚪ Internal | "no change" is "observed == base date" by convention. |

### AdjustmentHandlerContext (sealed record, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `AdjustmentHandlerContext` (now a sealed class).

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor (`Date`,`Adjustment`,`Rule`,`TerritoryCode`,`CalendarType`) | ✅ Migrated [reshaped] | v2 ctor is (`baseDate`,`territory`,`policy`,`isOccupied`,`resolutionContext`). |
| `Date` | ✅ Migrated [renamed→`BaseDate`] | |
| `Adjustment` (`ObservanceAdjustment`) | ✅ Migrated [reshaped→`Policy` (`AdjustmentPolicy`)] | |
| `Rule` (`NotableDateRule`) | ⛔ Not migrated [deliberate] | v2 exposes the firing policy + resolution context instead of the rule object. |
| `TerritoryCode` (string?) | ✅ Migrated [renamed→`Territory`, now non-null] | |
| `CalendarType` (`Type?`) | ⛔ Not migrated [deliberate] | v2 uses `CalendarSystem` on rules, not CLR `Type`; not surfaced on the handler context. |
| *(new)* `IsOccupied(DateOnly)`, `ResolutionContext` | (v2 addition) | Gives handlers occupied-day probing + reference resolution. |

### AdjustmentHandlerRegistry (sealed class, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `AdjustmentHandlerRegistry : IAdjustmentHandlerRegistry`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctors ×2 (default; `IEnumerable<KVP>` seed) | ✅ Migrated [reshaped] | v2 has only the default ctor (no seeding ctor). |
| `Register(string, IAdjustmentHandler)` | ✅ Migrated [1:1] | Returns `this`; v2 matches keys ordinally/case-sensitively (v1 was case-insensitive). |
| `TryGet(string, out IAdjustmentHandler)` | ✅ Migrated [1:1] | v2 out-param nullable. |
| *(new)* `Contains(string)` | (v2 addition) | From the interface. |

### AdjustmentHandlerResult (sealed record, public)
**Type disposition:** 🔵 Replaced — v1's rich result record is replaced by `IAdjustmentHandler.Adjust` returning `DateOnly?` (null = no change).

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor (`Activated`,`AdjustedDate`,`IsNonWorkingOverride`) | 🔵 Replaced | v2 handler returns `DateOnly?` directly; activation is "non-null", observed date is the value. |
| `Activated` | 🔵 Replaced | Encoded as non-null return. |
| `AdjustedDate` | 🔵 Replaced | Is the return value. |
| `IsNonWorkingOverride` | ⛔ Not migrated [deliberate] | v2 handler cannot override the non-working flag; the rule/policy owns `NonWorking`. |

### AdjustmentReason (sealed record, public)
**Type disposition:** 🔵 Replaced [reshaped] — v2 carries the reason as plain `string? AdjustmentReason` + `string? AdjustmentPolicyId` + `DateOnly? ActualDate` on the `NotableDate` record. No dedicated reason type.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor (`originalDate`,`trigger`,`action`,`handlerKey`) | 🔵 Replaced | No reason object; the policy's `Reason` string is written onto `NotableDate.AdjustmentReason`. |
| `OriginalDate` (`DateTime`) | ✅ Migrated [moved→`NotableDate.ActualDate` (`DateOnly?`)] | |
| `Trigger` | ⛔ Not migrated [deliberate] | Trigger/action not retained on the result; only free-text reason + policy id. |
| `Action` | ⛔ Not migrated [deliberate] | As above. |
| `HandlerKey` | ⛔ Not migrated [deliberate] | Provenance of the shift is the `AdjustmentPolicyId`, not the handler key. |

### AdjustmentTrigger (enum, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `AdjustmentTrigger`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Always`, `IfDayOfWeek`, `IfWeekend`, `IfWeekday`, `IfBeforeFixedDate`, `IfAfterFixedDate`, `IfLeapYear`, `IfNthOccurrenceInMonth`, `Custom` | ✅ Migrated | All present in v2 (order differs). |
| `IfNonWorkingDay` | 🔵 Replaced [deliberate] | Folded into `IfWeekend` + `skipNonWorkingDates` by design (functional audit area 8). |

### CachingCalculationAnchorResolver (sealed class, internal)
**Type disposition:** ⚪ Internal — performance plumbing (area 25); not reproduced. v2 resolves offset anchors inline via `StrategyResolutionContext.ResolveReference` (cycle-guarded `HashSet`, no caching).

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor (`IReadOnlyList<NotableDateRule>`, `NotableDateRuleResolver`) | ⚪ Internal | Replaced by `StrategyResolutionContext(resource, algorithms?)`. |
| `Resolve(string anchorRuleName, int year)` | ⚪ Internal | Replaced by `ResolveReference(notableDateRef, ruleRef, year)` — keyed by rule identity, not display name; no `Lazy`/`ConcurrentDictionary` cache. |

### CalculationAnchorCacheKey (readonly record struct, internal)
**Type disposition:** ⚪ Internal — cache key for the anchor cache; not reproduced (no anchor cache in v2).

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor (`AnchorRuleName`,`Year`) | ⚪ Internal | v2 keys in-progress resolution by `NotableDateRuleIdentity` only (no year in the key); no persistent cache entry. |

### ICalculationAnchorResolver (interface, internal)
**Type disposition:** ⚪ Internal — internal contract for the anchor cache; not reproduced.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Resolve(string anchorRuleName, int year)` | ⚪ Internal | Capability inlined into `StrategyResolutionContext.ResolveReference`; no public/internal interface. |

### CalendarThrowHelper (static partial class, internal)
**Type disposition:** ⛔ Not migrated [deliberate] — no v2 counterpart. v2 validates with `Bodu.ThrowHelper` (Bodu.Core) via ImplicitUsings; domain text lives in `Calendar2ResourceStrings`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `ThrowIfEndDateBeforeStartDate` | ✅ Migrated [merged→`ThrowHelper.ThrowIfGreaterThan`] | `NotableDateService.Resolve(DateRange)` uses `ThrowHelper.ThrowIfGreaterThan(range.StartDate, range.EndDate)`. |
| `ThrowIfKeyNullOrWhiteSpace` | 🔵 Replaced | v2 registries call `ThrowHelper.ThrowIfNull(key)` only (no whitespace guard). |
| `ThrowIfAnchorRuleNameNullOrWhiteSpace` | ⛔ Not migrated [deliberate] | No anchor-by-name path in v2. |
| `ThrowIfYearOutOfRange` | ⚪ Internal | No dedicated calendar year guard; year validation is implicit / via `DateOnly`. |
| `ThrowIfWorkingWeekUndefined`, `ThrowIfWorkingWeekEmpty` | ⛔ Not migrated [deliberate] | v2 hard-codes Sat/Sun weekend; no `WorkingDaysOfWeek`/`WeekPattern` parameter. |
| `ThrowIfUnsupportedCalendarType` | ⛔ Not migrated [deliberate] | v2 uses the `CalendarSystem` enum + `CalendarSystems`, not CLR calendar-type validation. |
| `CalendarThrowHelper.NetStandard.cs` partial | ⛔ Not migrated [deliberate] | v2 targets `net8.0` only; no netstandard companion. |

### DateRange (readonly record struct, public)
**Type disposition:** ✅ Migrated [reshaped: DateTime→DateOnly] — v2 `DateRange`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor (`StartDate`,`EndDate`) `DateTime` | ✅ Migrated [reshaped→`DateOnly`] | |
| `IsValid` | ✅ Migrated [1:1] | |
| `DayCount` | ✅ Migrated [1:1] | v2 uses `DayNumber` arithmetic. |
| `Contains(DateTime)` | ✅ Migrated [reshaped→`Contains(DateOnly)`] | |
| `Contains(DateRange)` | ⛔ **Not migrated [GAP — review]** | No range-in-range containment overload in v2. *(gap A3)* |
| `Intersects(DateRange)` | ⛔ **Not migrated [GAP — review]** | No overlap test in v2 `DateRange`. *(gap A3)* |
| `ToString()` override | ⛔ Not migrated [deliberate] | v2 relies on the default record `ToString`. |

### DateResolutionStrategy (enum, public)
**Type disposition:** ✅ Migrated [reshaped→polymorphic strategy classes] — v2 replaces the enum-of-6 with `IDateCalculationStrategy` implementations.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Fixed` | ✅ Migrated [reshaped→`FixedDateStrategy`] | |
| `DayOfWeekInMonth` | ✅ Migrated [reshaped→`DayOfWeekInMonthStrategy`] | |
| `Algorithm` | ✅ Migrated [reshaped→`AlgorithmDateStrategy`] | Key dispatch. |
| `OffsetFromAnchor` | ✅ Migrated [reshaped→`OffsetFromRuleStrategy`] | References rule identity, not anchor name. |
| `WeekdayNearDate` | ✅ Migrated [reshaped→`WeekdayNearDateStrategy`] | |
| `RelativeWeekdayInMonth` | ✅ Migrated [reshaped→`RelativeWeekdayInMonthStrategy`] | |

### DefaultNotableDateCollisionResolver (sealed class, public)
**Type disposition:** 🔵 Replaced — v1's "keep-all, ordered by provenance/priority/category/name" default folds into `NotableDateService.ApplySameDayCollisionPolicy` driven by `CollisionPolicy` (default `KeepAll`). No standalone default-resolver class.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Resolve(NotableDateCollisionContext)` | 🔵 Replaced | Built-in policy branches (`KeepAll`/`HighestPriorityOnly`/`CategoryPriority`) live in `NotableDateService.ResolveCollision`; ordering is date→priority→identity (provenance tiebreaker dropped). |

### IAdjustmentHandler (interface, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `IAdjustmentHandler`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Apply(AdjustmentHandlerContext) : AdjustmentHandlerResult` | ✅ Migrated [renamed→`Adjust`, reshaped return→`DateOnly?`] | Null = leave on calculated date. |

### IAdjustmentHandlerRegistry (interface, public)
**Type disposition:** ✅ Migrated — v2 `IAdjustmentHandlerRegistry`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `TryGet(string, out IAdjustmentHandler)` | ✅ Migrated [1:1] | v2 out-param nullable. |
| *(new)* `Contains(string)` | (v2 addition) | |

### INotableDateAlgorithm (interface, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `INotableDateAlgorithm`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `GetDate(int year, Calendar? calendar = null) : DateTime?` | ✅ Migrated [renamed→`Calculate(int year) : DateOnly?`] | v2 drops the `calendar` parameter (Gregorian-year only); returns `DateOnly?`. |

### INotableDateAlgorithmRegistry (interface, public)
**Type disposition:** ✅ Migrated [1:1] — v2 `INotableDateAlgorithmRegistry`. (A custom registry supplements the built-in key catalogue; functional-audit area 7.)

| v1 member | v2 disposition | Notes |
|---|---|---|
| `TryGet(string, out INotableDateAlgorithm)` | ✅ Migrated [1:1] | out-param nullable in v2. |
| `Contains(string)` | ✅ Migrated [1:1] | |

### INotableDateCollisionResolver (interface, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `INotableDateCollisionResolver`, consulted only when `CollisionPolicy.Custom`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Resolve(NotableDateCollisionContext) : IReadOnlyList<NotableDate>` | ✅ Migrated [reshaped] | v2 signature is `Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding)` — context object replaced by direct args (no provenance). |

### INotableDateNameLocalizer (interface, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `INotableDateNameLocalizer`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `GetDisplayName(NotableDate, CultureInfo? = null) : string` | ✅ Migrated [reshaped] | v2 `culture` is required and the return is `string?` (null = fall back). Applied on-demand via `NotableDateLocalizationExtensions.Localize`, not inside the service. |

### INotableDateService (interface, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `INotableDateService`; the 6-overload `GetNotableDates(...)` query collapses to `Resolve(DateOnly|DateRange[, filter])` + `NotableDateServiceExtensions`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `WorkingWeek` (`WeekPattern`) property | ⛔ Not migrated [deliberate] | v2 has no configurable working week; Sat/Sun weekend hard-coded. |
| `IsWeekend(DateTime)` | ✅ Migrated [moved→`NotableDate*Extensions`] | Working-day predicates moved off the service to extensions (area 20). |
| `IsNonWorkingDay(DateTime, string?, Type?)` | ✅ Migrated [moved→extension] | Territory required, no `Type?` calendar param. |
| `IsHolidayNonWorkingDay(...)` (default impl) | ✅ Migrated [moved→extension] | Covered by `IsNotableDate`/working-day extensions. |
| `GetNotableDates(int year, …)` ×2 | ✅ Migrated [reshaped→`Resolve(year, territory[, filter])` ext] | `NotableDateServiceExtensions`. |
| `GetNotableDates(DateTime start, DateTime end, …)` ×2 | ✅ Migrated [reshaped→`Resolve(DateRange, territory[, filter])`] | On the interface. |
| `GetNotableDates(DateTime date, …)` ×2 | ✅ Migrated [reshaped→`Resolve(DateOnly, territory[, filter])`] | On the interface. |
| `Invalidate()` | ⛔ Not migrated [deliberate] | Immutable resource model; reload is a separate type. |
| `Reload()` | ✅ Migrated [moved→`ReloadableNotableDateService.Reload` + `MutableNotableDateResourceProvider`] | Not on `INotableDateService` (area 16). |
| `GetSupportedTerritories()` | ⛔ Not migrated [deliberate] | Functional audit area 3: "not exposed". |
| `GetSupportedCalendars()` | ⛔ Not migrated [deliberate] | As above. |

### NotableDate (sealed record, public)
**Type disposition:** ✅ Migrated [reshaped: DateTime→DateOnly] — v2 `NotableDate` positional record.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Date` (`DateTime`) | ✅ Migrated [reshaped→`DateOnly`] | Emitted/observed date. |
| `Name` | ✅ Migrated [renamed→`DisplayName`] + `Identity.NotableDateId`/`RuleId` | |
| `Category` | ✅ Migrated [1:1] | |
| `DurationDays` | ✅ Migrated [1:1] | |
| `EndDate` (computed) | ✅ Migrated [1:1] | |
| `Priority` | ✅ Migrated [1:1] | |
| `AdjustmentReason` (record) | ✅ Migrated [reshaped→`string? AdjustmentReason` + `string? AdjustmentPolicyId` + `DateOnly? ActualDate`] | |
| `WasAdjusted` (computed) | ✅ Migrated [renamed→`IsObserved`] | |
| `IsNonWorkingDay` | ✅ Migrated [1:1] | |
| `CalendarType` (`Type?`) | 🔵 Replaced | Calendar tracked via `CalendarSystem` on the rule (`RuleApplicability.Calendar`), not on the result. |
| `TerritoryCode` (`string?`) | ✅ Migrated [reshaped→`TerritoryCode` (non-null string)] | |
| `Tags` (`ImmutableHashSet<string>`) | ✅ Migrated [reshaped→`IReadOnlyList<string>`] | |
| `Comment` | ⛔ Not migrated [deliberate] | Functional audit area 2: carried on the rule, not the result. |
| `DisplayName` (computed, territory/calendar suffix) | ✅ Migrated [reshaped→stored `DisplayName`] | v2 stores the concept display name; no auto-suffixing. |
| *(new)* `Identity`, `ActualDate`, `IsObserved`, `NotableDateId`, `RuleId`, `AdjustmentPolicyId` | (v2 additions) | Stable identity + audit. |

### NotableDateAdjuster (sealed class, internal)
**Type disposition:** ⚪ Internal — v1's adjuster engine; not reproduced as a distinct type. Its logic splits between `AdjustmentPolicy` (trigger eval `IsTriggered`, action `ApplyAction`/`SeekWorkingDay`) and `NotableDateService` (scope/priority selection `SelectAdjustmentPolicy`, custom-handler dispatch, replacement resolution).

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor (`isWeekend`,`isNonWorkingDay`,`workingWeek`,`handlerRegistry?`,`resolveByName?`) | ⚪ Internal | Collaborators wired into `NotableDateService`. |
| `IsInScope(...)` (static) | ⚪ Internal [→`AdjustmentScope.Matches` + rule applicability] | Year/territory/calendar scoping moved to `AdjustmentScope` + `RuleApplicability`. |
| `Apply(...)` | ⚪ Internal [→`NotableDateService.ComputeObservedDate`] | |
| `EvaluateTrigger` / `ApplyAction` / `ApplyCustomHandler` / `MoveToNextWorkingDay` / `ResolveReplacement` / `ProjectComparisonDate` (private) | ⚪ Internal | Reproduced as `AdjustmentPolicy.IsTriggered`/`ApplyAction`/`SeekWorkingDay`/`ComparesFixedDate` + `NotableDateService.InvokeCustomHandler`/`ResolveReplacementDate`. |

### NotableDateAlgorithmRegistry (sealed class, public)
**Type disposition:** ✅ Migrated [1:1] — v2 `NotableDateAlgorithmRegistry : INotableDateAlgorithmRegistry`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctors ×2 (default; `IEnumerable<KVP>` seed) | ✅ Migrated [reshaped] | v2 has only the default ctor. |
| `Register(string, INotableDateAlgorithm)` | ✅ Migrated [1:1] | v2 keys ordinal/case-sensitive; no whitespace guard. |
| `TryGet(string, out INotableDateAlgorithm)` | ✅ Migrated [1:1] | |
| `Contains(string)` | ✅ Migrated [1:1] | |

### NotableDateCategory (enum, public)
**Type disposition:** ✅ Migrated [reshaped values] — v2 `NotableDateCategory`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `None`, `Observance`, `Remembrance`, `Cultural`, `Religious`, `Seasonal`, `Civic`, `School`, `Regional`, `Other` | ✅ Migrated [1:1] | Names retained. |
| `Holiday` | ✅ Migrated [renamed→`PublicHoliday`] | |
| `Bank` | ✅ Migrated [renamed→`BankHoliday`] | |

### NotableDateCollisionContext (sealed class, public)
**Type disposition:** 🔵 Replaced — v2 passes the colliding set directly to `INotableDateCollisionResolver.Resolve(DateOnly, IReadOnlyList<NotableDate>)`; no context object, no provenance dimension.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor (`day`,`overlapping`,`provenances`) | 🔵 Replaced | Replaced by method args `(date, colliding)`. |
| `Day` (`DateTime`) | ✅ Migrated [→`date` param (`DateOnly`)] | |
| `Overlapping` | ✅ Migrated [→`colliding` param] | |
| `Provenances` | ⛔ Not migrated [deliberate] | v2 has no `NotableDateProvenance`; collisions arbitrate by priority/category only. |

### NotableDateFilter (sealed class, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `NotableDateFilter`, surfaced through filtered `Resolve` overloads.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `ForCategory`, `ForAnyCategory`, `IsNonWorkingDay`, `WasAdjusted`, `WithName`, `WithAnyName`, `WithTag`, `WithAnyTag`, `WithAllTags`, `WithMinDuration`, `InDateRange`, `AllOf`, `AnyOf`, `And`, `Or` | ✅ Migrated | All present in v2; v2 adds `WithId`, `Not` (area 19). |
| `IsMatch`/`IsRuleEligible` (internal two-stage gates) | ✅ Migrated [reshaped→single `Matches(NotableDate)`] | v2 filters post-resolution only; no rule-level primary gate. |

### NotableDateProvenance (enum, public)
**Type disposition:** ⛔ Not migrated [deliberate] — no v2 equivalent. Functional audit area 1: layer-origin tracking is tied to v1 override/import layering and not needed; v2 collision ordering is date→priority→category→identity with no provenance tiebreaker.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Imported`, `Local`, `RuntimeOverride` | ⛔ Not migrated [deliberate] | Override/import precedence handled at load (`NotableDateRuleOverrideApplier`, import merge), not as a runtime tiebreaker. |

### NotableDateRuleIdentity (readonly record struct, internal → public)
**Type disposition:** ✅ Migrated [reshaped + visibility public] — v2 `NotableDateRuleIdentity`, now `public` and structural rather than name-based.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor (`Name`,`RuleName`,`TerritoryCode`,`CalendarType`) | ✅ Migrated [reshaped→(`ResourceId`,`NotableDateId`,`RuleId`)] | Identity is resource/concept/rule ids; never the display name (fixes the v1 name-shadowing bug). |
| `From(NotableDateRule)` (static) | 🔵 Replaced | v2 builds identity via `NotableDateResource.GetIdentity(definition, rule)`. |
| `Equals`/`GetHashCode` (case-insensitive + territory-normalize) | ✅ Migrated [reshaped] | v2 uses default record equality over the three id strings (ordinal). |
| `NormalizeTerritory(string?)` (internal static) | ⛔ Not migrated [deliberate] | No territory component in v2 identity. |
| *(new)* `ToString()` → `resourceId/notableDateId/ruleId` | (v2 addition) | |

### NotableDateService (sealed class, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `NotableDateService : INotableDateService`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctors (rule-provider chain + `WeekPattern`/`WorkingDaysOfWeek` + `NotableDateServiceOptions`) | ✅ Migrated [reshaped] | v2 ctors take a single immutable `NotableDateResource` + optional `INotableDateAlgorithmRegistry`/`INotableDateCollisionResolver`/`IAdjustmentHandlerRegistry`/`IAdjustmentTriggerHandlerRegistry` (5 overloads). No rule-provider chain, no working-week, no options bag. |
| `GetNotableDates(...)` ×6, `IsWeekend`/`IsNonWorkingDay`/`IsHolidayNonWorkingDay`, `Invalidate`/`Reload`, `WorkingWeek`, `GetSupported*`, `Validate` | see `INotableDateService` rows | `Resolve` overloads + extensions; predicates → extensions; reload → `ReloadableNotableDateService`; supported-* dropped; validation → `NotableDateRuleValidator`. |
| (per-request range-pipeline cache) | ⚪ Internal | v2 resolves inline, two-phase, no cache (area 25). |

### NotableDateServiceOptions (sealed class, public)
**Type disposition:** ⛔ Not migrated [deliberate] — no v2 options bag. The optional concerns become explicit ctor parameters on `NotableDateService` and resource-level policy on `NotableDateResource.ResolutionPolicy`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `ResourcePathResolver` | ⛔ Not migrated [deliberate] | Provider/path-resolver layer replaced by `NotableDateResourceLoader` + resolver delegate (area 18). |
| `OverrideProviders` | 🔵 Replaced | Overrides applied at load; runtime via `MutableNotableDateResourceProvider`. |
| `AlgorithmRegistry` | ✅ Migrated [moved→ctor param] | |
| `AdjustmentHandlers` | ✅ Migrated [moved→ctor param `IAdjustmentHandlerRegistry`] | |
| `CollisionResolver` | ✅ Migrated [moved→ctor param `INotableDateCollisionResolver`] | |
| `NameLocalizer` | 🔵 Replaced | On-demand via `NotableDateLocalizationExtensions.Localize`. |
| `Plugins` | ✅ Migrated [moved→`Bodu.Globalization.Calendar2.Plugins`] | Area 21. |
| `ObservedDates` (`ObservedDateMode`) | ✅ Migrated [reshaped→per-policy `EmissionMode`] | Emission per-`AdjustmentPolicy`, not service-wide. |
| `ValidateRules` | 🔵 Replaced | Explicit `NotableDateRuleValidator` pass (+ `NotableDateValidationException`), not a ctor flag. |

### ObservanceAdjustment (sealed record, public)
**Type disposition:** ✅ Migrated [reshaped→`AdjustmentPolicy`] — v1's per-rule adjustment spec becomes the reusable, scope-matched `AdjustmentPolicy` referenced by rules via `AdjustmentPolicyRefs`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Key` (required) | ✅ Migrated [renamed→`Id`] | |
| `Trigger` | ✅ Migrated [1:1] | |
| `Action` | ✅ Migrated [1:1] | |
| `DayOfWeek` (`DayOfWeek?`) | ✅ Migrated [reshaped→`TriggerWeekdays`] | |
| `IsNonWorkingDay` (`bool?`) | ✅ Migrated [renamed→`NonWorking`] | |
| `OffsetDays` | ✅ Migrated [renamed→`ActionDays`] | |
| `TerritoryCode` (string) | ✅ Migrated [reshaped→`Scope.Territories`] | |
| `CalendarType` (`Type?`) | ✅ Migrated [reshaped→`Scope.Calendars` (`CalendarSystem`)] | |
| `EffectiveFromYear` / `EffectiveToYear` | ⛔ **Not migrated [GAP — review]** | `AdjustmentScope` has no year bounds; a policy cannot be year-bounded independently of its rule. *(gap B1)* |
| `ComparisonDate` (`DateTime?`) | ✅ Migrated [reshaped→`TriggerMonth`+`TriggerDay`] | |
| `WeekOrdinal` (`WeekOfMonthOrdinal?`) | ✅ Migrated [reshaped→`TriggerWeekOrdinal` (`WeekOrdinal?`)] | |
| `TargetRuleName` | ✅ Migrated [reshaped→`ActionNotableDateRef`] | |
| `TargetRuleVariant` | ✅ Migrated [reshaped→`ActionRuleRef`] | |
| `Priority` (default 100) | ✅ Migrated [1:1] | |
| `HandlerKey` | ✅ Migrated [split→`ActionHandlerKey` + `TriggerHandlerKey`] | v2 separates action-custom and trigger-custom handler keys. |
| `HandlerParameters` (`IReadOnlyDictionary`) | ⛔ **Not migrated [GAP — review]** | No handler-parameter map on `AdjustmentPolicy` or the handler contexts; v2 custom handlers receive no author-supplied parameters. *(gap B2)* |
| `MaxAdjustmentReachDays` | ✅ Migrated [reshaped→`MaxSearchDays`] | Bounds the working-day search; default 7. |
| `AppliesToGlobalRules` | ⛔ Not migrated [deliberate] | v2 scope: an empty dimension matches all; `MatchesTerritory` does parent/child matching — no opt-in global flag. |
| *(new)* `Emission`, `Reason`, `SkipWeekends`, `SkipNonWorkingDates`, `Scope.Categories`/`NotableDateRefs`/`RuleRefs`, `ActionWeekday` | (v2 additions) | |

### ObservedDateMode (enum, public)
**Type disposition:** ✅ Migrated [reshaped→`EmissionMode`, superset] — v2 `EmissionMode`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `ActualOnly`, `ObservedOnly`, `ActualAndObserved` | ✅ Migrated [1:1] | v2 adds **`ObservedAsAdditional`** + **`Suppress`**. v1 default was `ObservedOnly` (service-wide); v2 default is `ActualOnly`, emission per-policy. |

### TerritoryCode (readonly record struct, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `TerritoryCode` (a `readonly struct`, not an enum, in both versions; re-added per functional-audit area 23).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Country` | ✅ Migrated | |
| `Subdivision` | ✅ Migrated | |
| `HasSubdivision` | ✅ Migrated [renamed→`IsSubdivision`] | |
| `TryParse`, `Parse` | ✅ Migrated [1:1] | |
| `ParseList(string?)` | ⛔ **Not migrated [GAP — review]** | No comma-separated multi-territory parser on the v2 struct; capability survives via `RuleApplicability.Territories`. *(gap B3)* |
| `Contains(TerritoryCode)` | ✅ Migrated [1:1] | |
| `ToString()` | ✅ Migrated [1:1] | Plus v2 adds `Parent` and string-conversion operators. |

### WeekdayProximity (enum, public)
**Type disposition:** ✅ Migrated [reshaped, superset] — v2 `WeekdayProximity`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `OnOrAfter`, `OnOrBefore`, `Nearest` | ✅ Migrated [1:1] | v2 adds strict **`Before`** and **`After`**. |

**GAPS in this scope:** ✅ **all closed (2026-06-04)** — `DateRange.Contains(DateRange)` & `Intersects(DateRange)` (A3) and `TerritoryCode.ParseList` (B3) implemented directly; `ObservanceAdjustment.EffectiveFromYear`/`EffectiveToYear` (B1) reintroduced as `AdjustmentScope` `FromYear`/`ToYear`/`OnlyYears`/`ExceptYears`; `ObservanceAdjustment.HandlerParameters` (B2) reintroduced as `AdjustmentPolicy.HandlerParameters` (surfaced via `context.Parameters`). All other ⛔ rows are deliberate-by-design.

---

## Rule ingestion, parsing, providers, overrides & imports

*v1 namespace `Bodu.Globalization.Calendar` — the rule-authoring, parsing, provider, override and import cluster.*

### INotableDateProvider (interface, public)
**Type disposition:** ⛔ **Not migrated [GAP — review]** — no v2 year-by-year `NotableDate`-producing provider. v2 has `INotableDateResourceProvider` (a `Current` resource accessor), but that is the resource-swap seam, not this code-first per-event provider. *(gap A5)*

| v1 member | v2 disposition | Notes |
|---|---|---|
| `int MinSupportedYear { get; }` | ⛔ [GAP — review] | The only "provider" abstraction in v2 supplies a whole `NotableDateResource`, not per-event year bounds. |
| `int MaxSupportedYear { get; }` | ⛔ [GAP — review] | As above. |
| `bool SupportsYear(int year)` | ⛔ [GAP — review] | As above. |
| `IReadOnlyList<NotableDate> GetDates(int year, Calendar? calendar = null)` | ⛔ [GAP — review] | The v2 code-first seam is `INotableDateAlgorithm` (anchor date only) + authored rules; nothing returns ready-made `NotableDate`s per year. Not covered by functional-audit area 18 (which only addresses the rule-provider/path-resolver chain). |

### INotableDateRuleOverrideProvider (interface, public)
**Type disposition:** 🔵 Replaced — declarative `<Overrides>` (Add/Patch/Remove) applied at load by `NotableDateRuleOverrideApplier`; runtime mutation via `MutableNotableDateResourceProvider` + `ReloadableNotableDateService` (areas 15–16).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `IEnumerable<RuleRemoval> GetRemovals()` | 🔵 Replaced | `RemoveRuleOverride` + year/territory-scoped idioms, applied at load. No runtime "removals" accessor. |
| `IEnumerable<NotableDateRule> GetAdditions()` | 🔵 Replaced | `AddRuleOverride` doc elements; runtime additions by loading a fresh resource + `Reload`. |

### INotableDateRuleProvider (interface, public)
**Type disposition:** 🔵 Replaced — single-resource `NotableDateResourceLoader.Load`/`LoadJson` + a caller-supplied `Func<string,string?>` resolver; no `LoadRules()` interface (area 18).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `IEnumerable<NotableDateRule> LoadRules()` | 🔵 Replaced | No rule-stream contract; a resource loads whole into `NotableDateResource`. The runtime swap seam is `INotableDateResourceProvider.Current` (returns a resource, not a rule stream). |

### IResourcePathResolver (interface, public)
**Type disposition:** 🔵 Replaced — logical-path resolution folded into the caller-supplied resource-resolver delegate (area 18: "no provider/path-resolver types").

| v1 member | v2 disposition | Notes |
|---|---|---|
| `string Resolve(string documentPath, string childPath)` | 🔵 Replaced | The loader passes the raw `<Import>` `Resource` name to the caller's resolver delegate; relative/absolute logical-path semantics are the caller's concern. |

### JsonResourceNotableDateRuleProvider (sealed class, public)
**Type disposition:** 🔵 Replaced — `NotableDateResourceLoader.LoadJson` (+ `ParseAny` auto-detect in import graphs); no JSON resource-provider class (areas 14, 18).

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctors ×2 (`string`, `IResourcePathResolver`, `Assembly?` / `IEnumerable<Assembly>`) | 🔵 Replaced | No assembly-chain/resource-name ctor; JSON loaded by `NotableDateResourceLoader.LoadJson(string|Stream[, resolver])`. |
| `LoadRules()` (inherited) | 🔵 Replaced | See base. |

### XmlResourceNotableDateRuleProvider (sealed class, public)
**Type disposition:** 🔵 Replaced — `NotableDateResourceLoader.Load` (XML, schema `NotableDates.v2.xsd`); no XML resource-provider class (areas 13, 18).

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctors ×2 (`string`, `IResourcePathResolver`, `Assembly?` / `IEnumerable<Assembly>`) | 🔵 Replaced | Cross-assembly resource search replaced by the resolver delegate. XML via `NotableDateResourceLoader.Load(string|Stream[, resolver[, algorithms]])`. |
| `LoadRules()` (inherited) | 🔵 Replaced | See base. |

### MutableNotableDateRuleOverrideProvider (sealed class, public)
**Type disposition:** ✅ Migrated [reshaped→`MutableNotableDateResourceProvider`] — runtime mutation reshaped from per-rule add/remove to whole-resource swap (area 16).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `event EventHandler Changed` | 🔵 Replaced [reshaped] | No change event; `ReloadableNotableDateService` rebuilds lazily on next query after `Reload`. |
| `AddRule(NotableDateRule)` | ✅ Migrated [reshaped] | Folded into `Reload(NotableDateResource)`: addition authored via `<Overrides>`/`AddRuleOverride` in a freshly loaded resource. |
| `RemoveRule(string, int?, int?, string?)` | ✅ Migrated [reshaped] | Via `RemoveRuleOverride`/scoped `PatchRuleOverride`+reload. |
| `Clear()` | 🔵 Replaced [reshaped] | Cleared by reloading a resource without overrides. |
| `GetAdditions()` / `GetRemovals()` | ⚪ Internal | No public snapshot; overrides live inside the loaded resource. |
| `Reload(NotableDateResource)` *(v2 new)* | ✅ Migrated [1:1 concept] | The v2 swap entry point; `Current` exposes the in-effect resource. |

### NotableDateRule (sealed record, public)
**Type disposition:** ✅ Migrated [split→`NotableDateRule`/`NotableDateDefinition`/`NotableDateResource` + reshaped] — the flat, name-keyed v1 rule splits into the v2 concept (`NotableDateDefinition`) and the v2 `NotableDateRule` (id, priority, applicability, one strategy object, adjustment refs); identity moves to `NotableDateRuleIdentity`. Strategy scalars collapse into `IDateCalculationStrategy` objects; territory/calendar/year move to `RuleApplicability`; adjustments become reusable `AdjustmentPolicy` referenced by id.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Name` (required) | ✅ [moved→`NotableDateDefinition.DisplayName`] | Display name is concept-level; never identity in v2. |
| `Strategy` (`DateResolutionStrategy`) | ✅ [reshaped→`NotableDateRule.Strategy : IDateCalculationStrategy`] | Enum discriminator → strategy object. |
| `Category` | ✅ [split] | `NotableDateDefinition.Category` + optional `NotableDateRule.Category` override. |
| `RuleName` | ✅ [renamed→`NotableDateRule.Id`] | Promoted to the mandatory stable rule id. |
| `FirstYear` / `LastYear` | ✅ [moved→`RuleApplicability.FromYear`/`ToYear`] | |
| `OccurrenceYears` | ✅ [reshaped→`RuleApplicability.OnlyYears`/`ExceptYears`] | Explicit year sets rather than a modulus. |
| `CalendarType` (`Type`) | ✅ [reshaped→`RuleApplicability.Calendar : CalendarSystem`] | Area 24. |
| `TerritoryCode` (string) | ✅ [moved→`RuleApplicability.Territories`] | |
| `IsNonWorkingDay` (bool?) | ✅ [split→`NotableDateRule.NonWorking` + `NotableDateDefinition.DefaultNonWorkingDay`] | |
| `DurationDays` | ✅ [split→`NotableDateRule.DurationDays?` + `NotableDateDefinition.DefaultDurationDays`] | |
| `Priority` | ✅ [1:1] | |
| `Tags` (`ImmutableHashSet<string>`) | ✅ [split→`NotableDateRule.Tags` + `NotableDateDefinition.Tags`] | |
| `Day` / `Month` | ✅ [moved→`FixedDateStrategy`/`DayOfWeekInMonthStrategy`] | |
| `SkipLeapMonth` | ✅ [reshaped] | Inside the fixed-date strategy + `CalendarSystems` leap-month skip (area 24). |
| `SweepCalendarYears` | ✅ [reshaped] | Intrinsic to `CalendarSystems` fixed-date projection (area 24). |
| `CalendarMonthAlias` | ✅ [reshaped] | Hebrew month alias via `CalendarSystems` (area 24). |
| `DayOfWeek` | ✅ [moved→`DayOfWeekInMonthStrategy`/`WeekdayNearDateStrategy`] | |
| `WeekOrdinal` | ✅ [moved→`DayOfWeekInMonthStrategy`; enum→`WeekOrdinal`] | |
| `WeekdayProximity?` | ✅ [moved→`WeekdayNearDateStrategy`] | |
| `RelativeDayOfWeek` | ✅ [moved→`RelativeWeekdayInMonthStrategy`] | |
| `AnchorRuleName`/`AnchorRuleVariant`/`AnchorTerritoryCode`/`AnchorCalendarType` | ✅ [reshaped→`OffsetFromRuleStrategy` `notableDateRef`/`ruleRef`] | Name-based anchor → id-based reference via `StrategyResolutionContext.ResolveReference` (area 4). |
| `OffsetDays` | ✅ [moved→`OffsetFromRuleStrategy`] | |
| `AlgorithmKey` | ✅ [moved→`AlgorithmDateStrategy` key] | |
| `AlgorithmType` (`Type`) | ⛔ [deliberate: algorithms dispatch by string key only — area 4] | No CLR-type algorithm reference. |
| `AlgorithmMonth` / `AlgorithmDay` | ⛔ [deliberate: tied to the dropped `AlgorithmType` ctor-arg path] | Calendar festivals use dedicated calculators. |
| `Adjustments` (`ImmutableArray<ObservanceAdjustment>`) | ✅ [reshaped→`AdjustmentPolicyRefs` + `AdjustmentPolicy`] | Inline adjustments hoisted to reusable policies (areas 8–9). |
| `Comment` | ⚪ Internal | Authoring comment not carried on the v2 rule object. |

### NotableDateRuleJsonParser (static class, public)
**Type disposition:** ✅ Migrated [renamed→`NotableDateJsonDocumentParser`, reshaped to internal] — JSON parse into `ParsedNotableDateDocument`; entry via `NotableDateResourceLoader.LoadJson` (area 14).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `List<NotableDateRule> ParseJson(string json)` | ✅ [reshaped] | Parser is `internal`; public entry is `NotableDateResourceLoader.LoadJson(string)` → validated `NotableDateResource`. |
| `ParsedNotableDateDocument ParseDocument(string json)` | ✅ [renamed→`NotableDateJsonDocumentParser.Parse(string, ICollection<diagnostics>)`] | Internal, diagnostic-collecting. |

### NotableDateRuleMerger (static class, internal)
**Type disposition:** ⚪ Internal — v2 import/override merge inlined into `NotableDateResourceLoader.ApplyUse`/`ResolveImports` and `NotableDateRuleOverrideApplier`; no standalone merger type.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `NotableDateRule Apply(NotableDateRule, NotableDateRuleUseDirective)` | ⚪ Internal [inlined] | Per-directive override merge performed inline at load; patch merge in the applier. |

### NotableDateRuleOverrideBody (sealed record, public)
**Type disposition:** ✅ Migrated [reshaped→`PatchRuleOverride` (internal)] — the nested `<Rule>` override payload becomes the id-targeted patch operation; the field set is narrower (first-cut patch).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Name` | ⛔ [deliberate: display name is concept-level; patches target by id, never rename] | Import rename uses `NotableDateImportUse.As`. |
| `RuleName` | ✅ [reshaped→`PatchRuleOverride.RuleRef`/`NotableDateRef`] | Target is an explicit id pair. |
| `Category` | ✅ [1:1] | |
| `TerritoryCode` | ✅ [reshaped→`PatchRuleOverride.Applicability`] | |
| `IsNonWorkingDay` | ✅ [renamed→`PatchRuleOverride.NonWorking`] | |
| `FirstYear`/`LastYear`/`OccurrenceYears` | ✅ [reshaped→`PatchRuleOverride.Applicability`] | |
| `DurationDays` / `Priority` | ✅ [1:1] | |
| `Comment` | ⚪ Internal | Not patchable in v2. |
| `CalendarType` | ✅ [reshaped→`PatchRuleOverride.Applicability` (`Calendar`)] | |
| `Strategy` + all strategy fields | ✅ [reshaped→`PatchRuleOverride.Strategy : IDateCalculationStrategy`] | Whole-strategy replacement; individual scalars are not separate patch fields. |
| `Tags` (additive) | ✅ [reshaped→`PatchRuleOverride.Tags` (replace)] | v2 patch replaces tags rather than additive-merge. |
| `Adjustments` (key-merge) | ✅ [reshaped→`PatchRuleOverride.AdjustmentPolicyRefs` (replace)] | Replaces the policy-ref list. |

### NotableDateRuleParser (static class, public)
**Type disposition:** ✅ Migrated [renamed→`NotableDateDocumentParser`, reshaped to internal; new schema `NotableDates.v2.xsd`] — XML parse into `ParsedNotableDateDocument`; entry via `NotableDateResourceLoader.Load` (area 13).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `List<NotableDateRule> ParseXml(string xml)` | ✅ [reshaped] | Public entry is `NotableDateResourceLoader.Load(string)` → `NotableDateResource`; parser internal. |
| `List<NotableDateRule> ParseXml(XDocument)` | ⛔ [deliberate: v2 parser takes `string`/`Stream` only] | No `XDocument` overload. |
| `ParsedNotableDateDocument ParseDocument(string xml)` | ✅ [renamed→`NotableDateDocumentParser.Parse(string, ICollection<diagnostics>)`] | Internal, diagnostic-collecting; validates against `NotableDates.v2.xsd`. |
| `ParsedNotableDateDocument ParseDocument(XDocument)` | ⛔ [deliberate: no `XDocument` overload] | As above. |

### NotableDateRuleReference (readonly record struct, internal)
**Type disposition:** ⚪ Internal — the name-keyed partial selector is not reproduced; v2 references rules by explicit `(notableDateRef, ruleRef)` id pairs.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor `(Name, RuleName?, TerritoryCode?, CalendarType?)` | ⚪ Internal [inlined] | v2 references are plain id strings on `OffsetFromRuleStrategy` and `AdjustmentPolicy.ActionNotableDateRef`/`ActionRuleRef`. |
| `static ForName(string)` | ⚪ Internal | References are id-based, not name-based. |

### NotableDateRuleResolution (file: enum `RuleReferenceMatch`, struct `RuleReferenceResult`, class `NotableDateRuleIndex` — all internal)
**Type disposition:** ⚪ Internal — v1 name-resolution/disambiguation machinery; v2 resolves id references directly (no ambiguity) in `StrategyResolutionContext.ResolveReference` and the validator's `CountReferenceMatches`. Functional audit area 1 lists `NotableDateRuleIndex` as "n/a".

| v1 member | v2 disposition | Notes |
|---|---|---|
| `enum RuleReferenceMatch { None, Unique, Ambiguous }` | ⚪ Internal | Ids are exact; the validator instead counts matches (0/1/>1) inline. |
| `struct RuleReferenceResult` | ⚪ Internal | No result struct; validator computes an `int matches`. |
| `NotableDateRuleIndex` ctor / `TryGetByIdentity` / `Resolve(reference, …)` | ⚪ Internal [inlined] | No identity/name index; lookups iterate by id. `Resolve` → `StrategyResolutionContext.ResolveReference(notableDateRef, ruleRef, year)` (id-based, cycle-guarded). |

### NotableDateRuleResolver (sealed class, internal)
**Type disposition:** ⚪ Internal — anchor/strategy resolution reshaped into per-strategy `IDateCalculationStrategy.Resolve` + `StrategyResolutionContext`; applicability → `RuleApplicability`; bulk date production → `NotableDateService`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor `(IReadOnlyList<NotableDateRule>, INotableDateAlgorithmRegistry?)` | ⚪ Internal | `NotableDateService` holds the resource + a `StrategyResolutionContext`. |
| `static bool IsApplicable(NotableDateRule, int year)` | ✅ [reshaped→`RuleApplicability` year/`OnlyYears`/`ExceptYears`] | Evaluated in `GatherCandidates`. |
| `DateTime? ResolveAnchorDate(NotableDateRule, int year)` | ✅ [reshaped→`IDateCalculationStrategy.Resolve(...) : DateOnly?`] | Polymorphic dispatch; offset chains via `StrategyResolutionContext` (cycle-guarded). |

### NotableDateRuleResourceProviderBase (abstract class, public)
**Type disposition:** 🔵 Replaced — assembly-chain search + format dispatch + flatten pipeline collapses into `NotableDateResourceLoader.Load`/`LoadJson` + `ResolveImports`/`ApplyUse` (area 18).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `protected` ctors ×2 (`string`, `IResourcePathResolver`, `Assembly?` / `IEnumerable<Assembly>`) | 🔵 Replaced | No provider base; content supplied as `string`/`Stream` + a resolver delegate. |
| `IEnumerable<NotableDateRule> LoadRules()` | 🔵 Replaced | `NotableDateResourceLoader.Load(...)` → `NotableDateResource`; internal flatten maps to `ResolveImports`. |
| internal flatten/recursion/format-dispatch (private) | ⚪ Internal [inlined into loader] | Cycle detection, `ParseAny` XML/JSON dispatch, import flattening inlined. |

### NotableDateRuleUseDirective (sealed record, public)
**Type disposition:** ✅ Migrated [reshaped→`NotableDateImportUse` (internal)] — per-concept cherry-pick with rename + a small override set, resolved at load by `NotableDateResourceLoader.ApplyUse` (area 17). v2 cherry-picks at the **concept** grain with a narrow override set.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `SourceRuleName` | ✅ [renamed→`NotableDateImportUse.NotableDateRef`] | Now the concept id. |
| `LocalName` | ✅ [renamed→`NotableDateImportUse.As`] | Rename on import. |
| `Category` | ✅ [1:1] | |
| `TerritoryCode` | ✅ [renamed→`NotableDateImportUse.Territory`] | Applied to every rule of the imported concept. |
| `IsNonWorkingDay` | ✅ [renamed→`NotableDateImportUse.NonWorking`] | |
| `FirstYear`/`LastYear`/`OccurrenceYears`/`DurationDays`/`Priority`/`Comment` | ⛔ [deliberate: import-use override set is intentionally minimal (As/Territory/Category/NonWorking)] | Finer adjustments authored as local rules or overrides. |
| `ClearTags` / `ClearAdjustments` / `ClearInherited` | ⛔ [deliberate: no inherit-then-clear model] | Imports bring whole concepts; local concepts win by id. |
| `OverrideBody` | 🔵 Replaced | Replaced by document-level `<Overrides>` applied after import. |
| `ClearFields` | ⛔ [deliberate: no field-clear verb] | Field-level null-clearing not reproduced. |

### NotableDateRuleUseGroup (sealed record, public)
**Type disposition:** ✅ Migrated [renamed→`NotableDateImport` (internal)] — a single `<Import>` binding a source resource to a set of cherry-picks (area 17).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `SourceResource` | ✅ [renamed→`NotableDateImport.Resource`] | |
| `UseAll` (bool) | ✅ [reshaped→empty `Uses`] | Empty `Uses` ⇒ import every concept; non-empty ⇒ cherry-pick set (`SelectImportedConcepts`). |
| `Uses` | ✅ [renamed→`NotableDateImport.Uses : IReadOnlyList<NotableDateImportUse>`] | |

### NotableDateRuleValidator (static class, internal)
**Type disposition:** ✅ Migrated [reshaped] — same role (post-assembly semantic validation), reshaped to operate on a `NotableDateResource` and accumulate into a diagnostics collection; loader throws `NotableDateValidationException` on errors (area 13).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Validate(IReadOnlyList<NotableDateRule>, INotableDateAlgorithmRegistry?)` | ✅ [reshaped→`void Validate(NotableDateResource, ICollection<diagnostics>, INotableDateAlgorithmRegistry?)`] | Appends to a passed collection; loader throws on error count. |
| *DuplicateIdentity* | ✅ [reshaped→`BODU-CAL2-DUP-POLICY`] | Duplicate **policy** ids; rule/concept id uniqueness is structural so the v1 defect cannot recur. |
| *MissingAnchor / AmbiguousAnchor (offset)* | ✅ [reshaped] | Validates `OffsetFromRuleStrategy` references via `CountReferenceMatches`. |
| *Missing/AmbiguousReplacementTarget* | ✅ [renamed→`BODU-CAL2-ADJREF`] | Adjustment/custom-action target resolution. |
| *UnregisteredAlgorithm (warning)* | ✅ [renamed→`BODU-CAL2-ALGORITHM`] | Registry-aware key validation. |
| *(v2 new) inverted year bounds / impossible fixed date* | ✅ [added→`BODU-CAL2-YEARS` / `BODU-CAL2-DAY`] | Checks the XSD cannot express (area 13). |

### NotableDateValidationDiagnostic (file: enum `NotableDateValidationSeverity`, record `NotableDateValidationDiagnostic` — public)
**Type disposition:** ✅ Migrated [1:1, split into separate files] — `NotableDateValidationDiagnostic` + `NotableDateValidationSeverity` (+ new `NotableDateValidationException`) (area 13).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `enum NotableDateValidationSeverity { Warning, Error }` | ✅ [reshaped→`{ Information, Warning, Error }`] | v2 adds an `Information` tier (superset). |
| `record NotableDateValidationDiagnostic(Severity, Code, Message)` | ✅ [1:1] | v2 adds an overridden `ToString()`. |
| *(v2 new) `NotableDateValidationException`* | ✅ [added] | Carries `IReadOnlyList<Diagnostics>`; thrown by the loader on any error diagnostic. |

### ParsedNotableDateDocument (sealed record, public)
**Type disposition:** ✅ Migrated [reshaped→internal class] — preserved as the parse-stage intermediate, broadened to the full v2 document vocabulary (areas 13–14, 17).

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor `(ImmutableArray<NotableDateRuleUseGroup>, ImmutableArray<NotableDateRule>)` | ✅ [reshaped] | v2 ctor takes `(resourceId, schemaVersion, ResolutionPolicy, adjustmentPolicies, notableDates, overrides, imports)`. Now `internal`. |
| `UseGroups` | ✅ [renamed→`Imports : IReadOnlyList<NotableDateImport>`] | |
| `LocalRules` | ✅ [reshaped→`NotableDates : IReadOnlyList<NotableDateDefinition>`] | Local rules become concept definitions. |
| *(v2 new)* `ResourceId`/`SchemaVersion`/`ResolutionPolicy`/`AdjustmentPolicies`/`Overrides` | ✅ [added] | New document-level surface reflecting the v2 schema. |

### ResourcePathResolver (sealed class, public)
**Type disposition:** 🔵 Replaced — logical-path resolution is the caller's responsibility via the resolver delegate; no path-resolver type (area 18).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `string Resolve(string documentPath, string childPath)` | 🔵 Replaced | The `<Import>` `Resource` string is passed verbatim to `Func<string,string?>`; `.`/`..` normalization is not provided by v2. |
| private helpers (`Normalize`, `IsRooted`, `Combine`, …) | ⚪ Internal | Not reproduced. |

### ResourcePathResolverOptions (sealed class, public)
**Type disposition:** ⛔ Not migrated [deliberate] — no path-resolver, so no path-resolver options (area 18 removes the whole provider/path-resolver layer).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `FullyQualifiedResourcePrefixes` | ⛔ [deliberate] | Resource-name prefixing gone; the resolver delegate maps names to content however it likes. |

### RuleRemoval (sealed record, public)
**Type disposition:** ✅ Migrated [reshaped→`RemoveRuleOverride` + scoped idioms] — declarative removal at load (areas 15–16). Unconditional → `RemoveRuleOverride`; year-scoped → `PatchRuleOverride`+`ExceptYears`; territory-scoped → `AddRuleOverride` shadow + `Suppress`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `RuleName` | ✅ [reshaped→`RemoveRuleOverride.NotableDateRef`/`RuleRef`] | Target is an explicit id pair. |
| `FromYear` / `ToYear` | ✅ [reshaped→`PatchRuleOverride.Applicability` `ExceptYears`/`ToYear`] | Area 15. |
| `TerritoryCode` | ✅ [reshaped→`AddRuleOverride` more-specific shadow emitting `Suppress`] | Specificity shadowing (area 15). |
| `RuleVariant` | ✅ [renamed→`RemoveRuleOverride.RuleRef`] | |
| `CalendarType` | ✅ [reshaped→targeted rule id / `Applicability.Calendar`] | Each calendar variant is a distinct rule. |

**GAPS in this scope:** ✅ **closed (2026-06-04)** — `INotableDateProvider` (gap A5) is reintroduced as a code-first provider seam: `INotableDateProvider.GetNotableDates(DateRange, territory)` returns finished `NotableDate`s, registered through a provider-aware `NotableDateService` constructor and merged terminally into resolution (range-clamped, filtered, and subject to the same-day collision policy, but bypassing adjustments/overrides/specificity by design). The v2 shape is range-based rather than v1's per-year `GetDates`/`SupportsYear`/`Min`/`MaxSupportedYear`.

---

## Notable-date algorithms

*v1 namespace `Bodu.Globalization.Calendar.Algorithms` (12 files).* The v1 algorithm contract `INotableDateAlgorithm.GetDate(int year, Calendar? calendar)` is replaced by v2 `INotableDateAlgorithm.Calculate(int year) -> DateOnly?` plus internal calculators. The built-in algorithms are **not** registered through `INotableDateAlgorithm` in v2; they are hard-dispatched by string key inside `AlgorithmDateStrategy.Calculate`. `INotableDateAlgorithm`/`INotableDateAlgorithmRegistry`/`NotableDateAlgorithmRegistry` exist in v2 only as the **extension** path for caller-supplied custom algorithms.

### EasterSundayNotableDateAlgorithm (sealed class, public)
**Type disposition:** ✅ Migrated [merged -> `EasterCalculator`] — Western+Julian computus folds into the internal `EasterCalculator` (`Western`/`Orthodox`), dispatched by `AlgorithmDateStrategy` keys `western-easter`/`orthodox-easter`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `DateTime? GetDate(int year, Calendar? calendar)` (public; implements v1 `INotableDateAlgorithm`) | ✅ Migrated [reshaped] | Split into `EasterCalculator.Western(int) -> DateOnly` and `EasterCalculator.Orthodox(int) -> DateOnly`. `DateTime?` -> `DateOnly`; `calendar` dropped (key picks the reckoning). Year-range guard moved to `AlgorithmDateStrategy.Calculate` (returns null outside 1–9999 instead of throwing). |
| `static ConcurrentDictionary s_easterCache` (private) | ⚪ Internal | Per-process memoization not reproduced; `EasterCalculator` recomputes (cheap modular arithmetic). |
| `static DateTime GetOrAddEasterSunday(int, Calendar?)` (private) | ⚪ Internal | Cache-fold plumbing; Gregorian/Julian branch selection inlined into the two public `EasterCalculator` methods. |

### EasterSundayNotableDateProviderBase (abstract class, public)
**Type disposition:** ⚪ Internal — the v1 `INotableDateProvider` provider-pattern (per-year cache + `NotableDate` materialization carrying Name/Category/Tags/Comment) is not reproduced; v2 algorithms return a bare `DateOnly?` and all surrounding metadata is carried by the `NotableDateDefinition`/`NotableDateRule` authored in the resource.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `abstract string Name { get; }` (protected) | ⚪ Internal | Replaced by `NotableDateDefinition.DisplayName`. |
| `abstract NotableDateCategory Category { get; }` (protected) | ⚪ Internal | Replaced by `NotableDateDefinition.Category` (v2 `NotableDateCategory` exists). |
| `abstract int MinSupportedYear { get; }` (public) | ⛔ Not migrated [deliberate: range guarding centralized] | No per-algorithm supported-year contract; `AlgorithmDateStrategy.Calculate` applies a single 1–9999 clamp. |
| `virtual int MaxSupportedYear => 9999` (public) | ⛔ Not migrated [deliberate] | Same as above. |
| `virtual string? Comment => null` (protected) | ⚪ Internal | Comment authored on the rule. |
| `bool SupportsYear(int year)` (public) | ⛔ Not migrated [deliberate: folded into the 1–9999 clamp] | No per-provider year-support predicate in v2. |
| `virtual ImmutableHashSet<string> Tags` (protected) | ⚪ Internal | Tags authored on the rule (filterable via `NotableDateFilter.WithTag`), not emitted by the algorithm. |
| `virtual Type? DefaultCalendarType => null` (protected) | 🔵 Replaced | Calendar association moves to `CalendarSystem`/`CalendarSystems` on the rule. |
| `IReadOnlyList<NotableDate> GetDates(int year, Calendar? calendar = null)` (public; implements `INotableDateProvider`) | 🔵 Replaced | v2 `INotableDateAlgorithm.Calculate(int) -> DateOnly?` returns the date only; `NotableDate` construction is the service's job. |
| `abstract void ValidateCalendar(Calendar? calendar)` (protected) | ⛔ Not migrated [deliberate: no calendar argument] | Unknown-calendar validation replaced by load-time validation of the `CalendarSystem` enum + unknown-algorithm-key. |
| `abstract DateTime CalculateDate(int year)` (protected) | ✅ Migrated [reshaped -> `EasterCalculator.Western`/`Orthodox`] | Derived computus bodies become the two `EasterCalculator` methods (`DateTime` -> `DateOnly`). |
| `ConcurrentDictionary<int,DateTime> _dateCache` (private) | ⚪ Internal | Per-year memoization not reproduced. |

### GregorianEasterSundayNotableDateProvider (sealed class, public)
**Type disposition:** ✅ Migrated [merged -> `EasterCalculator.Western`] — reached via the `western-easter` key.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `override int MinSupportedYear => 1583` (public) | ⛔ Not migrated [deliberate: range guarding centralized] | No 1583 floor; `AlgorithmDateStrategy` clamps to 1–9999 only. Behavior below 1583 is undefined-but-non-throwing rather than rejected. |
| `override string Name => "Easter Sunday"` (protected) | ⚪ Internal | Authored on the rule's `DisplayName`. |
| `override NotableDateCategory Category => Religious` (protected) | ⚪ Internal | Authored on the rule. |
| `override Type? DefaultCalendarType => GregorianCalendar` (protected) | 🔵 Replaced | Gregorian is the implicit projection of `EasterCalculator.Western`. |
| `override string? Comment` (protected) | ⚪ Internal | Authored on the rule. |
| `override void ValidateCalendar(Calendar?)` (protected) | ⛔ Not migrated [deliberate: no calendar argument] | Calendar restriction removed with the parameter. |
| `override DateTime CalculateDate(int year)` (protected) | ✅ Migrated [1:1 algorithm] | Identical anonymous-Gregorian computus, now `EasterCalculator.Western` returning `DateOnly`. |

### OrthodoxEasterSundayNotableDateProvider (sealed class, public)
**Type disposition:** ✅ Migrated [merged -> `EasterCalculator.Orthodox`] — reached via the `orthodox-easter` key.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `override int MinSupportedYear => 1` (public) | ⛔ Not migrated [deliberate: range guarding centralized] | Subsumed by the 1–9999 clamp. |
| `static JulianCalendar s_julianCalendar` (private) | ✅ Migrated [reshaped] | v2 constructs a local `JulianCalendar` inside `EasterCalculator.Orthodox` rather than caching a static instance. |
| `override string Name => "Orthodox Easter Sunday"` (protected) | ⚪ Internal | Authored on the rule. |
| `override NotableDateCategory Category => Religious` (protected) | ⚪ Internal | Authored on the rule. |
| `override Type? DefaultCalendarType => JulianCalendar` (protected) | 🔵 Replaced | v2 emits the Gregorian-projected date directly; no Julian-calendar tag on the result. |
| `override string? Comment` (protected) | ⚪ Internal | Authored on the rule. |
| `override void ValidateCalendar(Calendar?)` (protected) | ⛔ Not migrated [deliberate: no calendar argument] | Removed with the parameter. |
| `override DateTime CalculateDate(int year)` (protected) | ✅ Migrated [1:1 algorithm] | Identical Julian computus + Julian->Gregorian projection, now `EasterCalculator.Orthodox`. |

### VesakNotableDateAlgorithm (sealed class, public)
**Type disposition:** ✅ Migrated [merged -> `AlgorithmDateStrategy`+`LunarPhaseCalculator`] — the `vesak` key resolves `LunarPhaseCalculator.FullMoonOnOrAfter(new DateOnly(year, 5, 1))`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `DateTime? GetDate(int year, Calendar? calendar = null)` (public) | ✅ Migrated [reshaped] | Inlined into the `"vesak"` case; same rule (first full moon on/after 1 May). `DateTime?` -> `DateOnly?`; calendar dropped; year-guard now a non-throwing clamp. |
| `static DateTime? ProjectToCalendar(DateTime, Calendar?)` (private) | ⛔ Not migrated [deliberate: non-Gregorian result projection dropped] | v2 emits the Gregorian `DateOnly` only. |

### AsalhaPujaNotableDateAlgorithm (sealed class, public)
**Type disposition:** ✅ Migrated [merged -> `AlgorithmDateStrategy`+`LunarPhaseCalculator`] — the `asalha-puja` key resolves `LunarPhaseCalculator.FullMoonOnOrAfter(new DateOnly(year, 6, 15))`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `DateTime? GetDate(int year, Calendar? calendar = null)` (public) | ✅ Migrated [reshaped] | Same rule (first full moon on/after 15 June). The +1-day Vassa observance remains an `OffsetFromRule` rule. |

### LosarNotableDateAlgorithm (sealed class, public)
**Type disposition:** ✅ Migrated [merged -> `AlgorithmDateStrategy`+`LunarPhaseCalculator`] — the `losar` key resolves `LunarPhaseCalculator.NewMoonOnOrAfter(new DateOnly(year, 1, 20))`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `DateTime? GetDate(int year, Calendar? calendar = null)` (public) | ✅ Migrated [reshaped] | Same approximation (first new moon on/after 20 Jan), carrying the same Phugpa-divergence caveat. |

### QingmingNotableDateAlgorithm (sealed class, public)
**Type disposition:** ✅ Migrated [merged -> `SolarTermCalculator`] — the `qingming` key resolves `SolarTermCalculator.Qingming(year, ChinaStandardTimeOffset)`. **v2 improvement:** applies a real China-Standard-Time (UTC+8) offset, fixing the v1-documented near-midnight East-Asia edge case.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `const double DegreesToDays15` (private) | ✅ Migrated [renamed -> `QingmingDegreeDays`] | Same `15.0 * 365.2422 / 360.0`. |
| `static DateTime s_j2000Epoch` (private) | ✅ Migrated [1:1] | Same J2000 epoch instant. |
| `DateTime? GetDate(int year, Calendar? calendar = null)` (public) | ✅ Migrated [reshaped] | `SolarTermCalculator.Qingming(int, double) -> DateOnly`; CST offset added; calendar dropped. |
| `static double ComputeVernalEquinoxJde(int)` (private) | ✅ Migrated [reshaped -> `ComputeEquinoxJulianDay(int,bool)`] | Generalized to both equinoxes via a `vernal` flag; vernal branch identical (Meeus 27.a + 27.c). |
| `static DateTime JdeToDateTime(double)` (private) | ✅ Migrated [reshaped -> `JulianDayToLocalDate(double,double)`] | Applies the UTC offset before truncating to `DateOnly`. |
| `static double DegToRad(double)` (private) | ✅ Migrated [renamed -> `DegreesToRadians`] | Identical. |
| `static (double,double,double)[] s_correctionTerms` (private) | ✅ Migrated [1:1] | Identical 28-term Meeus Table 27.c array. |

### HinduLunarNotableDateAlgorithm (sealed class, public)
**Type disposition:** ✅ Migrated [reshaped -> `HinduLunarCalculator`] — v1 was an instantiable (month, paksha, tithi) algorithm; v2 stores festival coordinates in `HinduLunarCalculator.s_festivals` keyed by algorithm key (`diwali`, `holi`, `navaratri`, `ram-navami`, `janmashtami`, `ganesh-chaturthi`, `dussehra`, `karva-chauth`, `raksha-bandhan`, `vasant-panchami`, `maha-shivaratri`) and resolves by `Resolve(key, year)`. **v2 improvement:** month selection by the sun's sidereal sign (Lahiri ayanamsa) with explicit adhika-maasa (leap-month) skip, replacing v1's fixed Gregorian month-offset seed.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `const double TithiDays` (private) | ✅ Migrated [1:1] | Same `29.530588861 / 30.0`. |
| `readonly HinduLunarMonth _month` / `HinduPaksha _paksha` / `int _tithi` (private fields) | ✅ Migrated [reshaped] | Per-instance state becomes per-key table rows `(int Month, int Offset, bool Purnima)`; paksha+tithi collapse to a single tithi `Offset` (shukla T -> T−1; krishna T -> 14+T). |
| `HinduLunarNotableDateAlgorithm(HinduLunarMonth, HinduPaksha, int tithi)` (public ctor) | ⚪ Internal | No public per-festival ctor in v2; festivals are predefined table entries. The tithi 1–15 / enum guards are not reproduced. |
| `DateTime? GetDate(int year, Calendar? calendar = null)` (public) | ✅ Migrated [reshaped -> `Resolve(string,int)`+`Compute`] | Solar-anchored month identification; `DateTime?` -> `DateOnly?`; calendar dropped. |
| `static int GetSearchMonth(HinduLunarMonth)` (private) | 🔵 Replaced | Fixed month->Gregorian-month seed table superseded by `SiderealSunSign`/`GatherNewMoons`. |

### HinduLunarMonth (enum, public)
**Type disposition:** ⚪ Internal — the twelve Sanskrit months are not exposed as a v2 enum; the amanta month is an `int` (1=Chaitra … 12=Phalguna) embedded in `HinduLunarCalculator.s_festivals`/`Compute`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Chaitra=1 … Phalguna=12` (12 members) | ⚪ Internal | Encoded as the `Month` int; month->solar-sign mapping is `(((month-2)%12)+12)%12` in `Compute`. |

### HinduPaksha (enum, public)
**Type disposition:** ⚪ Internal — the fortnight distinction is folded into the integer tithi `Offset`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Shukla=0`, `Krishna=1` (2 members) | ⚪ Internal | Krishna handled by `14 + T`; shukla by `T − 1`. No public paksha type. |

### LunarPhaseAlgorithm (static class, internal)
**Type disposition:** ✅ Migrated [renamed -> `LunarPhaseCalculator`] — the Meeus ch. 49 new/full-moon helper, reproduced as internal `LunarPhaseCalculator`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `static DateTime s_j2000Epoch` (private) | ✅ Migrated [1:1] | Same J2000 epoch. |
| `const double SynodicMonth` (private) | ✅ Migrated [1:1] | Same `29.530588861`. |
| `static DateTime? GetFullMoonOnOrAfter(DateTime)` (internal) | ✅ Migrated [renamed -> `FullMoonOnOrAfter`] | `DateTime` -> `DateOnly`/`DateOnly?`; identical 3-attempt lunation advance. |
| `static DateTime? GetNewMoonOnOrAfter(DateTime)` (internal) | ✅ Migrated [renamed -> `NewMoonOnOrAfter`] | Same reshape; identical logic. |
| `static double EstimateK(int, int, bool)` (private) | ✅ Migrated [1:1] | Identical lunation-index estimate. |
| `static double ComputeLunarPhaseJde(double k)` (private) | ✅ Migrated [renamed -> `ComputeLunarPhaseJulianDay(double, bool)`] | v2 passes `fullMoon` explicitly; the 49.a/49.b correction series and W term are identical. |
| `static DateTime JdeToDate(double)` (private) | ✅ Migrated [renamed -> `JulianDayToDate`] | Returns `DateOnly`. |
| `static double DegToRad(double)` (private) | ✅ Migrated [renamed -> `DegreesToRadians`] | Identical. |

**Cross-cutting (applies to every `GetDate` row):** every v1 algorithm threw `ArgumentOutOfRangeException` for year < 1 or > 9999 and `NotSupportedException` for an unsupported `calendar`. v2 replaces both — the year guard becomes a non-throwing `null` return in `AlgorithmDateStrategy`, and the calendar argument (with its `NotSupportedException`) is removed entirely; an unknown algorithm key is surfaced as a **load-time validation error** (`AlgorithmDateStrategy.IsKnownKey`) rather than a runtime throw.

**GAPS in this scope: none.** All ⛔ rows are deliberate (centralized year-range guarding, dropped calendar parameter, dropped non-Gregorian result projection, provider metadata moved onto the authored rule). Nakshatra-fixed **Onam** is a documented deliberate omission and was never present in v1's algorithm scope; v2 ships a *broader* named-festival set than v1 exposed here. "Lunar New Year" has no v1 algorithm type — v2 handles it as a `FixedDateStrategy` on the `ChineseLunisolar` `CalendarSystem`.

---

## Extension surface (working-day, traversal, fiscal, query, localization)

*v1 namespace `Bodu.Globalization.Calendar.Extensions` (55 files).* Across the board, v1's ambient `NotableDateContext` is gone, so the 2–4 optional-overload variants per method collapse into one required-`(service, territory)` overload, and the `Type? calendarType` parameter is dropped (v2 carries the calendar on the rule, area 24).

### NotableDateContext (static class, public)
**Type disposition:** 🔵 Replaced — v2 has NO ambient context; every v2 extension takes `INotableDateService service` + `string territory` explicitly (areas 3 & 20).

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Default` (property, get/set) | 🔵 Replaced | Ambient/lazy default service removed by design; service is passed per call. |
| `Reset()` | 🔵 Replaced | Existed only to reset the ambient default in tests; obsolete once the context is gone. |

### NotableDateOnlyExtensions (static class, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `NotableDateOnlyExtensions` (the richest v2 extension class, 16 methods). Every optional ambient path collapses to a single explicit overload.

| v1 method (×overloads) | v2 disposition | Notes |
|---|---|---|
| `IsWorkingDay` (×4) | ✅ [reshaped] | `IsWorkingDay` (×1, explicit service). |
| `IsNonWorkingDay` (×4) | ✅ [reshaped] | `IsNonWorkingDay` (×1). |
| `IsNotableDate` (×4) | ✅ [reshaped] | `IsNotableDate` (×1), takes `NotableDateFilter?`. |
| `GetNotableDates` (×4) | ✅ [reshaped] | `GetNotableDates` (×1). |
| `GetNotableDatesInMonth` (×4) | ✅ [reshaped] | `GetNotableDatesInMonth` (×1). |
| `GetNotableDatesInYear` (×4) | ✅ [reshaped] | `GetNotableDatesInYear` (×1). |
| `NextWorkingDay` (×4) | ✅ [reshaped] | `NextWorkingDay` (×1). |
| `PreviousWorkingDay` (×4) | ✅ [reshaped] | `PreviousWorkingDay` (×1). |
| `SnapToWorkingDay` (×2) | ✅ [reshaped] | `SnapToWorkingDay` (×1). |
| `SnapToWorkingDayBackward` (×2) | ✅ [reshaped] | `SnapToWorkingDayBackward` (×1). |
| `SnapToNearestWorkingDay` (×2) | ✅ [reshaped] | `SnapToNearestWorkingDay` (×1). |
| `AddWorkingDays` (×4) | ✅ [reshaped] | `AddWorkingDays` (×1). |
| `WorkingDaysBetween` (×4) | ✅ [reshaped] | `WorkingDaysBetween` (×1). |
| `EnumerateWorkingDays` (×2) | ✅ [reshaped] | `EnumerateWorkingDays` (×1). |
| `EnumerateNotableDates` (×4) | ✅ [reshaped] | `EnumerateNotableDates` (×1). |
| `EnumerateNonWorkingDays` (×2) | ⛔ **Not migrated [GAP — review]** | No v2 method by name or concept. *(gap A2)* |
| `NextNonWorkingDay` (×2) | ⛔ **Not migrated [GAP — review]** | No v2 counterpart anywhere. *(gap A2)* |
| `PreviousNonWorkingDay` (×2) | ⛔ **Not migrated [GAP — review]** | No v2 counterpart anywhere. *(gap A2)* |
| `NextNotableDate` (×4) | ⛔ **Not migrated [GAP — review]** | No "next notable date" traversal in v2. *(gap A1)* |
| `PreviousNotableDate` (×4) | ⛔ **Not migrated [GAP — review]** | No "previous notable date" traversal in v2. *(gap A1)* |

*(v2 adds `IsWeekend(this DateOnly, WeekPattern?)` — a new no-service helper with no v1 extension equivalent.)*

### NotableDateTimeExtensions (static class, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `NotableDateTimeExtensions`; same reshaping; traversal preserves time-of-day/`Kind`. (v2's class does **not** carry `GetNotableDatesInMonth`/`InYear` — those are `DateOnly`-only.)

| v1 method (×overloads) | v2 disposition | Notes |
|---|---|---|
| `IsWorkingDay` (×4) | ✅ [reshaped] | `IsWorkingDay` (×1). |
| `IsNonWorkingDay` (×4) | ✅ [reshaped] | `IsNonWorkingDay` (×1). |
| `IsNotableDate` (×4) | ✅ [reshaped] | `IsNotableDate` (×1). |
| `GetNotableDates` (×4) | ✅ [reshaped] | `GetNotableDates` (×1). |
| `GetNotableDatesInMonth` (×4) | ✅ [merged→`NotableDateOnlyExtensions`] | `DateOnly`-only in v2; reachable via `DateOnly.FromDateTime`. |
| `GetNotableDatesInYear` (×4) | ✅ [merged→`NotableDateOnlyExtensions`] | As above. |
| `NextWorkingDay` (×4) | ✅ [reshaped] | `NextWorkingDay` (×1). |
| `PreviousWorkingDay` (×4) | ✅ [reshaped] | `PreviousWorkingDay` (×1). |
| `SnapToWorkingDay` (×2) | ✅ [reshaped] | `SnapToWorkingDay` (×1). |
| `SnapToWorkingDayBackward` (×2) | ✅ [reshaped] | `SnapToWorkingDayBackward` (×1). |
| `SnapToNearestWorkingDay` (×2) | ✅ [reshaped] | `SnapToNearestWorkingDay` (×1). |
| `AddWorkingDays` (×4) | ✅ [reshaped] | `AddWorkingDays` (×1). |
| `WorkingDaysBetween` (×4) | ✅ [reshaped] | `WorkingDaysBetween` (×1). |
| `EnumerateWorkingDays` (×2) | ✅ [reshaped] | `EnumerateWorkingDays` (×1). |
| `EnumerateNotableDates` (×4) | ✅ [reshaped] | `EnumerateNotableDates` (×1). |
| `EnumerateNonWorkingDays` (×2) | ⛔ **Not migrated [GAP — review]** | No v2 counterpart. *(gap A2)* |
| `NextNonWorkingDay` (×2) | ⛔ **Not migrated [GAP — review]** | No v2 counterpart. *(gap A2)* |
| `PreviousNonWorkingDay` (×2) | ⛔ **Not migrated [GAP — review]** | No v2 counterpart. *(gap A2)* |
| `NextNotableDate` (×4) | ⛔ **Not migrated [GAP — review]** | No v2 counterpart. *(gap A1)* |
| `PreviousNotableDate` (×4) | ⛔ **Not migrated [GAP — review]** | No v2 counterpart. *(gap A1)* |

### NotableDateTimeOffsetExtensions (static class, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `NotableDateTimeOffsetExtensions` is a *superset* of v1 (v1 had 6 methods; v2 has 14, adding `IsNotableDate`/`GetNotableDates`/`Snap*`/`Enumerate*`). v1's `TimeZoneInfo`-aware overloads are not reproduced — v2 derives `DateOnly` from `date.DateTime`.

| v1 method (×overloads) | v2 disposition | Notes |
|---|---|---|
| `IsWorkingDay` (×1) | ✅ [reshaped] | `IsWorkingDay`. |
| `IsNonWorkingDay` (×1) | ✅ [reshaped] | v1 took a `TimeZoneInfo`; v2 uses `date.DateTime` (no timezone parameter). |
| `NextWorkingDay` (×1) | ✅ [reshaped] | Preserves offset. |
| `PreviousWorkingDay` (×1) | ✅ [reshaped] | |
| `AddWorkingDays` (×1) | ✅ [reshaped] | |
| `WorkingDaysBetween` (×1) | ✅ [reshaped] | |

*(v1's `DateTimeOffset` class has no traversal/notable-date families, so there are no `DateTimeOffset` gap rows.)*

### NotableDateFiscalExtensions (static class, public)
**Type disposition:** ✅ Migrated [1:1 family] — v2 `NotableDateFiscalExtensions`. Reshaping: explicit `service`; `calendarType` dropped; configurable fiscal-year start month preserved.

| v1 method (×overloads) | v2 disposition | Notes |
|---|---|---|
| `FirstWorkingDayOfFiscalYear` (×1) | ✅ [1:1] | |
| `LastWorkingDayOfFiscalYear` (×1) | ✅ [1:1] | |
| `FirstWorkingDayOfFiscalQuarter` (×1) | ✅ [1:1] | |
| `LastWorkingDayOfFiscalQuarter` (×1) | ✅ [1:1] | |

*(v2-only extension classes with no v1 member in this folder: `NotableDateServiceExtensions.Resolve` (by-year query, area 3) and `NotableDateLocalizationExtensions.Localize` (area 22). v1's by-year query lived on `INotableDateService.GetNotableDates(year,…)` and localization on `INotableDateNameLocalizer`.)*

**GAPS in this scope (two cross-cutting families, on both `DateOnly` and `DateTime`) — ✅ NOW CLOSED (2026-06-04):**
- **Notable-date traversal** — `NextNotableDate`, `PreviousNotableDate` *(gap A1)* — ✅ added to both extension classes.
- **Non-working-day traversal** — `NextNonWorkingDay`, `PreviousNonWorkingDay`, `EnumerateNonWorkingDays` *(gap A2)* — ✅ added, mirroring the working-day traversal.

At the time of the audit, v2 reproduced only the *working-day* traversal and *notable-date enumeration*, not these. Note functional-audit area 20 lists `Enumerate*` generically, but v2 in fact ships only `EnumerateWorkingDays` + `EnumerateNotableDates`, so the area-20 line slightly overstates parity.

---

## Range-resolution pipeline (v1 internal performance architecture)

*v1 namespace `Bodu.Globalization.Calendar.RangeResolution` (13 files).* **All 13 top-level types are `internal`** — none has a public contract. v2 has no `RangeResolution` namespace; the entire scope is replaced by the inline two-phase resolve in `NotableDateService.Resolve` → `GatherCandidates` (phase one: compute every actual occurrence, seed an `occupied` `HashSet<DateOnly>`) → `EmitCandidate`/`ComputeObservedDate` (phase two: place observed dates in `CompareForPlacement` precedence), with offset anchors resolved through `StrategyResolutionContext` (cycle-guarded). Per functional-audit area 25, this is ⚪ Internal throughout — identical observable results, no public tuning knob or result shape lost.

### RuleTier (enum, internal)
**Type disposition:** ⚪ Internal — tier classification (Fixed / OffsetFromFixed / Algorithmic / OffsetFromAlgorithmic) ordering the v1 tiered passes. v2 resolves all rules in a single `GatherCandidates` loop; offset ordering is on-demand via `StrategyResolutionContext.ResolveReference`.

### RuleStaticProfile (record, internal)
**Type disposition:** ⚪ Internal — per-rule year-independent static analysis (tier, root anchor, offset-from-root, reach envelope) built once and reused. v2 computes per call; nothing is precomputed or surfaced.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor + positional props (`Rule`,`Tier`,`RootAnchorIdentity`,`OffsetFromRoot`,`MinObservedReach`,`MaxObservedReach`) | ⚪ Internal | v2 reads rule data off `NotableDateRule`/`NotableDateDefinition` each resolve; offset held on `OffsetFromRuleStrategy`. |
| `RootAnchorRuleName` / `DependsOnAlgorithmicAnchor` / `MaxForwardReach` / `MinBackwardReach` | ⚪ Internal | Reach-envelope optimization replaced by the unconditional ±1-year fringe. |

### RuleStaticAnalysis (class, internal)
**Type disposition:** ⚪ Internal — rule-set-wide static analysis (profiles, identity/name indexes, ambiguous-name set, dependents-by-anchor, global fringe reach). v2 has no precomputed analysis object; lookups happen inline against `_resource.NotableDates`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `static Build(IReadOnlyList<NotableDateRule>)` | ⚪ Internal | No build step; the service holds the immutable `NotableDateResource`. |
| `Profiles` | ⚪ Internal | v2 iterates `definition.Rules` per concept in `GatherCandidates`. |
| `GlobalFringeReach` | ⚪ Internal | Worst-case-reach sizing replaced by the constant ±1-year window (`StartDate.Year - 1` / `EndDate.Year + 1`). |
| `TryGetProfile(in NotableDateRuleIdentity, out …)` | ⚪ Internal | Superseded by `NotableDateResource.GetIdentity` + reference resolution. |
| `TryGetProfile(string, out …)` (name overload) | ⚪ Internal | Name-keyed binding eliminated by design — v2 references by structural `(notableDateRef, ruleRef)` (area 1). |
| `GetDependents(string anchorRuleName)` | ⚪ Internal | Reverse dependency index unused; offsets resolve forward on demand. |
| private helpers (`BuildProfile`, `ClassifyRule`, `ClassifyOffsetChain`, `ComputeAdjustmentReach`, `EstimateAdjustmentReach`) | ⚪ Internal | Chain walk → `StrategyResolutionContext.ResolveReference` (live, cycle-guarded); per-action reach heuristics have no v2 counterpart (v2 does not size a fringe from reach). |

### NotableDateRangeRequest (record, internal)
**Type disposition:** ⚪ Internal — input DTO (start/end/territory/calendar/filter/observed-mode). v2 passes these as direct `Resolve` arguments.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor (`startDate`,`endDate`,`territoryCode?`,`calendarType?`,`filter?`,`observedDates`) | 🔵 Replaced | → `Resolve(DateRange, string territory)` / `Resolve(DateRange, string, NotableDateFilter)`. |
| `StartDate` / `EndDate` | 🔵 Replaced | Carried by the `DateRange` value type. |
| `TerritoryCode` | 🔵 Replaced | `territory` string parameter (`TerritoryCode` interops via implicit conversion). |
| `CalendarType` | ⚪ Internal | No per-request CLR calendar scope; calendar is a per-rule `CalendarSystem` (area 24). |
| `Filter` | 🔵 Replaced | `NotableDateFilter` parameter on filtered overloads. |
| `ObservedDates` (`ObservedDateMode`) | 🔵 Replaced | Per-policy `AdjustmentPolicy.Emission` (`EmissionMode`) + resource-level `ResolutionPolicy.ObservedDateRangePolicy`. |

### NotableDateRangePlan (class, internal)
**Type disposition:** ⚪ Internal — the per-request execution plan (eligible rules, candidate years, fringe years/window, per-anchor year sets). v2 builds no plan; year iteration is the inline `for (year = firstYear; year <= lastYear; year++)` loop.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor + `Request`/`EligibleRules`/`CandidateYears`/`FringeYears`/`FringeStartDate`/`FringeEndDate` | ⚪ Internal | Eligibility is inline `rule.Applicability.AppliesTo(territory, year)`; cross-year roll-over is the fixed ±1-year scan. |
| `GetAnchorYears(in NotableDateRuleIdentity)` / `RequiredAnchorIdentities()` | ⚪ Internal | Algorithmic anchors compute per year when their strategy runs; no per-anchor year planning. |

### NotableDateRangePlanner (class, internal)
**Type disposition:** ⚪ Internal — builds the plan: rule eligibility, the v1 same-name territory shadowing, candidate/fringe-year computation. Behavior split across v2 `NotableDateService.GatherCandidates` (eligibility + most-specific-wins shadowing) and the inline ±1-year scan.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `const int DefaultFringeDays = 7` | ⛔ Not migrated [deliberate: internal-only; no public knob existed] | `public const` on an `internal` class, never user-observable. v2 uses a hardcoded ±1 civil year. |
| ctors ×2 (`RuleStaticAnalysis` / `…, int fringeDays`) | ⚪ Internal | No planner type; `fringeDays` has no public analogue. |
| `Plan(NotableDateRangeRequest)` | ⚪ Internal | → `GatherCandidates` + the inline year loop. |
| `IsRuleEligible` / `RuleMayApplyToTerritory` (private) | 🔵 Replaced | → `RuleApplicability.AppliesTo(territory, year)` (+ filter post-resolve via `NotableDateFilter.Matches`). |
| `ApplySameNameTerritoryShadowing` (private) | 🔵 Replaced (the v1 *bug*) | Name-based shadowing replaced by per-concept most-specific-wins: `GatherCandidates` keeps only rules whose `RuleApplicability.MatchSpecificity(territory)` equals the per-concept-per-year max (areas 10–11). |
| `HasTerritoryNarrowerOrEqualToRequest` / `IsRuleStrictlyBroaderThanRequest` / `AddDaysClamped` (private) | 🔵 Replaced | Subsumed by `RuleApplicability.MatchSpecificity`; clamping by the `Math.Max(1,…)` / `Math.Min(9999,…)` bounds. |

### NotableDateRangePipeline (class, internal)
**Type disposition:** ⚪ Internal — the orchestrator (four tiered passes + fringe pass + adjustment phase + emission ordering), reached in v1 via `NotableDateService.ResolveNotableDatesInRange`. Entirely replaced by `NotableDateService.Resolve`'s two-phase compute-then-place. Behavior preserved (area 25).

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor (`analysis`,`ruleResolver`,`workingWeek`,`handlerRegistry?`,`overrideRemovals?`,`overrideAdditions?`) | ⚪ Internal | `NotableDateService` ctors take `(resource, algorithms?, collisionResolver?, handlers?, triggerHandlers?)`; overrides applied at load. |
| `ResolveWithProvenance(request)` | ⚪ Internal → 🔵 (provenance dropped) | Two-phase `Resolve` replaces the four passes; provenance not carried. |
| `Resolve(request)` | 🔵 Replaced | Public surface is `INotableDateService.Resolve(DateRange/DateOnly, territory[, filter])`. |
| `ProcessFringePass` (private) | 🔵 Replaced | Cross-year roll-over via the ±1-year scan; observed-date inclusion decided by emitted date in `AddIfInRange` (area 10). |
| `ResolveFringeAnchor` (private) | 🔵 Replaced | Lazy anchor resolution → `StrategyResolutionContext.ResolveReference`. |
| `ProcessDirect` (Tier 1) | 🔵 Replaced | `GatherCandidates` enumerates fixed dates via `FixedDateStrategy.CalculateAll`. |
| `ProcessAlgorithmicAnchors` (Tier 3) | 🔵 Replaced (partial behavior change) | v2 invokes algorithm strategies inline per year; the v1 "compute each anchor once / swallow algorithm exceptions so one bad algorithm can't abort the request" guard is **not** reproduced (internal robustness only — no result loss for conforming algorithms). |
| `ProcessOffsetFromCached` (Tier 2/4) | 🔵 Replaced | Offsets computed live by `OffsetFromRuleStrategy` via `StrategyResolutionContext`. |
| `AddEntries` (territory expansion + override removal) | 🔵 Replaced | Territory matching via `RuleApplicability`; override removal at load via `RemoveRuleOverride` (area 15). |
| `ApplyAdjustments` (first-active-wins) | 🔵 Replaced & strengthened | `SelectAdjustmentPolicy` does ascending-priority first-active-wins; observed dates placed against the live `occupied` set (area 10). |
| `BuildEmissionList` (window intersection + ordering) | 🔵 Replaced | `EmitCandidate` + `AddIfInRange` emit per `EmissionMode`; final `OrderBy(Date).ThenBy(NotableDateId).ThenBy(RuleId)`; same-day arbitration in `ApplySameDayCollisionPolicy`. |
| `IsNonWorkingDay` / `IsWeekend` (cache-as-context) | 🔵 Replaced | Non-working context is the per-call `occupied` set; weekend/working-day predicates are extensions (area 20). |
| `BuildNotableDate` | 🔵 Replaced | `AddIfInRange` constructs the v2 `NotableDate` (`DateOnly`, `IsObserved`/`ActualDate`, `Identity`). |
| `ResolveAdjustmentTerritory` / `EnumerateApplicableTerritories` / `Intersects` (private) | 🔵 Replaced | Territory normalization via `TerritoryCode`/`RuleApplicability`; span-overlap inclusion inlined in `AddIfInRange`. |
| `IsRemovedByOverride` (runtime `RuleRemoval` scoping) | 🔵 Replaced | Scoped removal is load-time: `PatchRule`+`ExceptYear` (year) and `AddRule` shadow+`Suppress` (territory) (area 15). |

### NotableDateRangeResolutionCache (class, internal)
**Type disposition:** ⚪ Internal — per-request entry store + anchor/observed lookup + non-working probe. Replaced by two transient per-call collections: `List<ResolutionCandidate> candidates` and `HashSet<DateOnly> occupied`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Entries` / `Count` / `Add` / `Contains` / `TryGet` | ⚪ Internal | Candidates live in a local `List<ResolutionCandidate>`; no keyed store. |
| `ResolveAnchor(...)` ×2 | 🔵 Replaced | → `StrategyResolutionContext.ResolveReference(notableDateRef, ruleRef, year)`, computed live (no cached reuse). |
| `EmissableEntries()` | ⚪ Internal | All candidates are emission-considered; no Candidate/ContextOnly split. |
| `IsNonWorkingDay(date, territory, calendar)` | 🔵 Replaced | → `occupied.Contains` (seeded in phase one; updated by `Claim` in phase two). |
| `ResolveObservedByName(...)` | 🔵 Replaced | → `StrategyResolutionContext.ResolveReference` (used by `ReplaceWithRule`); by structural id, not name. |
| `EntryCoversDay` / `ContextMatches` (private) | ⚪ Internal | Subsumed by `occupied` membership + `RuleApplicability`. |

### NotableDateCacheEntry (class, internal)
**Type disposition:** ⚪ Internal — mutable cache row. Replaced by the immutable `ResolutionCandidate` record + the `occupied` set; observed date computed in `EmitCandidate`, not mutated onto the entry.

| v1 member | v2 disposition | Notes |
|---|---|---|
| ctor + `Profile`/`AnchorYear`/`BaseNotable` | ⚪ Internal | `ResolutionCandidate(identity, displayName, category, baseDate, policy, priority, nonWorking, durationDays, tags)`; year folds into `BaseDate`. |
| `Adjusted` / `State` / `AdjustmentActivated` / `Rule` / `IsEmissable` | ⚪ Internal | Observed date is a local in `ComputeObservedDate`; activation expressed by `NotableDate.IsObserved`. |
| `Provenance` (`NotableDateProvenance`) | ⛔ Not migrated [deliberate: layer-origin tracking tied to v1 override/import layering] | `NotableDateProvenance`/`RuntimeOverride` absent from all v2 src (area 1); never reached the public `NotableDate`. |

### NotableDateCacheKey (readonly record struct, internal)
**Type disposition:** ⚪ Internal — `(NotableDateRuleIdentity, int AnchorYear)` cache key. No keyed cache in v2.

### NotableDateCacheState (enum, internal)
**Type disposition:** ⚪ Internal — distinguished emission `Candidate` rows from `ContextOnly` rows. v2 has no context-only rows; every candidate is emission-considered (context resolution is on-demand via `StrategyResolutionContext`).

### ResolvedNotableDate (record, internal)
**Type disposition:** ⚪ Internal — pairs a `NotableDate` with `NotableDateProvenance` for collision arbitration without exposing provenance. v2 carries `NotableDate` directly; provenance dropped.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Notable` | 🔵 Replaced | v2 results are plain `NotableDate` (`IReadOnlyList<NotableDate>`). |
| `Provenance` | ⛔ Not migrated [deliberate: provenance not modeled in v2] | Collision arbitration uses priority/category, not provenance. Not user-observable. |

### ResolvedWindowSet (class, internal)
**Type disposition:** ⚪ Internal — a prototype "what windows have been resolved" introspection helper merging disjoint `DateRange` intervals. No v2 counterpart; v2 resolves each call independently with no cross-call window memory.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Ranges` / `Add(DateRange)` (interval-merge) / `Covers(DateRange)` / `Clear()` | ⚪ Internal | No persisted resolved-window union; merge logic not reproduced. |
| `Contains(DateTime)` | 🔵 Replaced (shape) | v2 `DateRange.Contains(DateOnly)` exists, but tests a single range, not a tracked union. |

**GAPS in this scope: none.** Every type here is `internal` performance/plumbing whose observable results are reproduced by `NotableDateService`'s two-phase resolve, `StrategyResolutionContext`, `RuleApplicability`, the `occupied` set, and load-time override application. The only members with no v2 behavior — `NotableDateProvenance` and `NotableDateRangePlanner.DefaultFringeDays`/`fringeDays` — were never user-observable, so each is ⛔ [deliberate], not a GAP.

---

## Plugin model

*v1 namespace `Bodu.Globalization.Calendar.Plugins` (17 files) → v2 project `Bodu.Globalization.Calendar2.Plugins`.* The plugin loader, trust-policy family, plugin interfaces and exceptions largely migrate, but the loader becomes static, trust is passed per-call, the trust *context* is reshaped (losing the strong-name token), and **two built-in trust policies plus the rule-provider plugin are dropped**.

### ExternalPluginLoader (sealed class, public)
**Type disposition:** ✅ Migrated [renamed→`NotableDatePluginLoader`, reshaped] — v1's instance-based loader becomes the **static** `NotableDatePluginLoader`; trust is passed per-call; a new `RegisterAlgorithms` helper and an assembly overload are added.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `.ctor(IPluginTrustPolicy trustPolicy)` | 🔵 Replaced | No ctor — class is static. Trust supplied per call as the 2nd argument. |
| `INotableDatePlugin Load(string assemblyPath)` | ✅ Migrated [renamed→`LoadFrom`, reshaped] | `static LoadFrom(string assemblyPath, IPluginTrustPolicy)`. Path overload loads via `AssemblyLoadContext.LoadFromAssemblyPath` (v1 read bytes + `LoadFromStream`). Hashing now best-effort (only if the file exists). |
| *(new)* `LoadFrom(Assembly, IPluginTrustPolicy)` | (v2 addition) | Load an already-loaded assembly. |
| *(new)* `int RegisterAlgorithms(INotableDatePlugin, NotableDateAlgorithmRegistry)` | (v2 addition) | Pushes `INotableDateAlgorithmPlugin.GetAlgorithms()` into the registry (area 21). v1 left registry wiring to the caller. |
| `private Assembly? ResolveFromHostOrAlongside(...)` | ⛔ Not migrated [deliberate] | v1's `Resolving +=` host/default-context type-unification handler is not reproduced (no `Resolving` hook, no collectible-context isolation). Affects plugins bundling private copies of `Bodu.*`; internal plumbing, not part of the public contract. |

### IPluginTrustPolicy (interface, public)
**Type disposition:** ✅ Migrated [1:1] — `IPluginTrustPolicy`, identical shape.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `PluginTrustResult Evaluate(PluginTrustContext context)` | ✅ Migrated [1:1] | Same signature. |

### PluginTrustContext (readonly record struct, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `PluginTrustContext` is a **`sealed record` (class)**, not a `readonly record struct`; positional members reordered and retyped.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `string AssemblyPath` (#1) | ✅ Migrated [reshaped] | Now #2, **nullable** (`string?`) — null when loaded in memory. |
| `AssemblyName AssemblyName` (#2) | ✅ Migrated [reshaped→`string AssemblyName`] | Retyped `AssemblyName`→**`string`** (simple name), now #1. **Consequence: the public-key token is no longer on the context** (breaks strong-name policy reconstruction). |
| `byte[] FileHash` (#3) | ✅ Migrated [reshaped] | Now **nullable** (`byte[]?`); null when no file. v1 guaranteed non-null. |
| value semantics (`record struct`) | 🔵 Replaced | Reference-type `record` with by-value equality. |

### PluginTrustResult (readonly record struct, public)
**Type disposition:** ✅ Migrated [reshaped] — v2 `PluginTrustResult` is a **`sealed record` (class)** with a renamed member and factory methods.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `bool Trusted` (#1) | ✅ Migrated [renamed→`IsTrusted`] | |
| `string? Reason` (#2) | ✅ Migrated [1:1] | |
| *(new)* `Trusted()` / `Rejected(string)` (static factories) | (v2 addition) | |
| value semantics (`record struct`) | 🔵 Replaced | Reference-type `record`. |

### AllowAllPluginTrustPolicy (sealed class, public)
**Type disposition:** ✅ Migrated [1:1] — `AllowAllPluginTrustPolicy`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `Evaluate(PluginTrustContext)` | ✅ Migrated [reshaped] | Same behavior (always trusted); v2 adds a `ThrowHelper.ThrowIfNull(context)` guard, returns `PluginTrustResult.Trusted()`. |

### DelegatingPluginTrustPolicy (sealed class, public)
**Type disposition:** ✅ Migrated [1:1] — `DelegatingPluginTrustPolicy`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `.ctor(Func<PluginTrustContext, PluginTrustResult> evaluator)` | ✅ Migrated [reshaped] | Param renamed `evaluator`→`decide`; guard → `ThrowHelper.ThrowIfNull`. |
| `Evaluate(PluginTrustContext)` | ✅ Migrated [reshaped] | Same delegation; adds a `ThrowIfNull(context)` guard. |

### CompositePluginTrustPolicy (sealed class, public)
**Type disposition:** ✅ Migrated [reshaped] — `CompositePluginTrustPolicy`; AND/short-circuit preserved, input validation relaxed.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `.ctor(params IPluginTrustPolicy[] policies)` | ✅ Migrated [reshaped] | v1 threw `ArgumentException` for an empty array and a null element; **v2 drops both** (only `ThrowIfNull` on the array). Backing store `ImmutableArray`→`IPluginTrustPolicy[]`. |
| `Evaluate(PluginTrustContext)` | ✅ Migrated [1:1] | Same conjunction + first-rejection short-circuit; adds `ThrowIfNull(context)`. |

### FileHashPluginTrustPolicy (sealed class, public)
**Type disposition:** ⛔ **Not migrated [GAP — review]** — no v2 type by name or concept. v2 keeps the `FileHash` on `PluginTrustContext` (SHA-256 still computed) but ships **no built-in policy that compares it**; a consumer must hand-roll one via `DelegatingPluginTrustPolicy`. Not in functional-audit area 21. *(gap A4)*

| v1 member | v2 disposition | Notes |
|---|---|---|
| `.ctor(IReadOnlyDictionary<string, byte[]> allowedHashesByAssemblyName)` | ⛔ [GAP — review] | No counterpart. |
| `Evaluate(PluginTrustContext)` | ⛔ [GAP — review] | Hash-allowlist (constant-time compare, name-keyed) not reproduced. |

### StrongNamePluginTrustPolicy (sealed class, public)
**Type disposition:** ⛔ **Not migrated [GAP — review]** — no v2 type, and the public-key token it relied on was **dropped from `PluginTrustContext`** (v1 `AssemblyName` → v2 `string`), so a strong-name policy cannot even be reconstructed from the v2 context alone. Not in functional-audit area 21. *(gap A4)*

| v1 member | v2 disposition | Notes |
|---|---|---|
| `.ctor(IEnumerable<string> allowedPublicKeyTokens)` | ⛔ [GAP — review] | No counterpart. |
| `Evaluate(PluginTrustContext)` | ⛔ [GAP — review] | Public-key-token allowlist not reproduced; underlying token dropped from the v2 context. |
| private helpers (`NormalizeToken`, `ToHexString`, `GetHexChar`) | ⚪ Internal | n/a. |

### INotableDatePlugin (interface, public)
**Type disposition:** ✅ Migrated [1:1] — `INotableDatePlugin`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `string Name { get; }` | ✅ Migrated [1:1] | |
| `Version Version { get; }` | ✅ Migrated [1:1] | |

### INotableDateAlgorithmPlugin (interface, public)
**Type disposition:** ✅ Migrated [1:1] — `INotableDateAlgorithmPlugin : INotableDatePlugin`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms()` | ✅ Migrated [1:1] | Consumed by `NotableDatePluginLoader.RegisterAlgorithms` into `NotableDateAlgorithmRegistry.Register`. |

### INotableDateRulePlugin (interface, public)
**Type disposition:** ⛔ Not migrated [deliberate: v2 has no rule-provider plugin pathway] — no v2 type, and the underlying `INotableDateRuleProvider` concept does not exist in v2. v2 sources rules through `INotableDateResourceProvider`/`MutableNotableDateResourceProvider`. Functional-audit area 21 lists only the base/algorithm plugin interfaces.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `IEnumerable<INotableDateRuleProvider> GetRuleProviders()` | ⛔ Not migrated [deliberate] | Plugins in v2 contribute **algorithms only**, not rule providers. |

### NotableDatePluginAttribute (sealed class : Attribute, public)
**Type disposition:** ✅ Migrated [reshaped] — `NotableDatePluginAttribute`; `AttributeUsage` slightly relaxed.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `[AttributeUsage(Assembly, AllowMultiple=false, Inherited=false)]` | ✅ Migrated [reshaped] | v2 drops the explicit `Inherited = false` (immaterial for assembly targets). |
| `.ctor(Type pluginType)` | ✅ Migrated [reshaped] | Guard → `ThrowHelper.ThrowIfNull`. |
| `Type PluginType { get; }` | ✅ Migrated [1:1] | |

### NotableDatePluginException (class : Exception, public)
**Type disposition:** ✅ Migrated [reshaped] — base `NotableDatePluginException`; one ctor dropped.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `.ctor(string message)` | ✅ Migrated [1:1] | |
| `.ctor(string message, Exception innerException)` | ✅ Migrated [1:1] | |
| `.ctor()` (parameterless) | ⛔ Not migrated [deliberate] | v2 messages always sourced from `PluginsResourceStrings`; a message-less base is unused. |

### PluginActivationException (sealed class, public)
**Type disposition:** ✅ Migrated [reshaped] — `PluginActivationException`; ctor set reworked, one property dropped.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `.ctor()` / `.ctor(string message)` | ⛔ Not migrated [deliberate] | Dropped. |
| `.ctor(string message, Exception innerException)` | ✅ Migrated [reshaped→`.ctor(string, Type?, Exception)`] | Inner-exception folded into the type-carrying overload. |
| `.ctor(string assemblyPath, Type? pluginType, Exception innerException)` | ✅ Migrated [reshaped] | First arg is now a preformatted **message**, not a path; v2 also adds `(string message, Type? pluginType)`. |
| `string AssemblyPath { get; }` | ⛔ Not migrated [deliberate] | Path is baked into the message string instead. |
| `Type? PluginType { get; }` | ✅ Migrated [1:1] | |

### PluginMissingAttributeException (sealed class, public)
**Type disposition:** ✅ Migrated [reshaped] — `PluginMissingAttributeException`; ctors collapsed, property renamed.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `.ctor()` / `.ctor(string message)` / `.ctor(string message, Exception innerException)` | ⛔ Not migrated [deliberate] | Dropped. |
| `.ctor(string assemblyPath, string reason)` | ✅ Migrated [reshaped→`.ctor(string message, string assemblyName)`] | First arg now a message; second is the assembly **name** (v1's was the failure reason). |
| `string AssemblyPath { get; }` | ✅ Migrated [renamed→`AssemblyName`, reshaped] | Simple name instead of path. |
| `string Reason { get; }` | ⛔ Not migrated [deliberate] | The explanation lives only in the message. |

### PluginNotTrustedException (sealed class, public)
**Type disposition:** ✅ Migrated [reshaped] — `PluginNotTrustedException`; ctors collapsed, `AssemblyPath`→`AssemblyName`.

| v1 member | v2 disposition | Notes |
|---|---|---|
| `.ctor()` / `.ctor(string message)` / `.ctor(string message, Exception innerException)` | ⛔ Not migrated [deliberate] | Dropped. |
| `.ctor(string assemblyPath, string? reason)` | ✅ Migrated [reshaped→`.ctor(string message, string assemblyName, string? reason)`] | Adds an explicit message; carries the assembly **name**. |
| `string AssemblyPath { get; }` | ✅ Migrated [renamed→`AssemblyName`, reshaped] | |
| `string? Reason { get; }` | ✅ Migrated [1:1] | |

**GAPS in this scope — ✅ NOW CLOSED (2026-06-04):**
- `FileHashPluginTrustPolicy` (whole type) *(gap A4)* — ✅ re-added (SHA-256 allowlist, constant-time compare; rejects in-memory assemblies with no hash).
- `StrongNamePluginTrustPolicy` (whole type) *(gap A4)* — ✅ re-added; `PluginTrustContext` regained a `PublicKeyToken` (lowercase-hex) member, populated by the loader, so the policy can be evaluated from the v2 context again.

*(`INotableDateRulePlugin` is also absent but classified ⛔ [deliberate] — v2 has no rule-provider plugin pathway at all.)*

---

_Generated 2026-06-04 by a six-way parallel per-namespace pass over the v1 source, with every ⛔ GAP independently re-verified against both v1 and v2. Companion to `V1-TO-V2-FUNCTIONAL-AUDIT.md` (capability-area view)._
