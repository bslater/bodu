# v1 → v2 Test Coverage Reconciliation

The v1 `Bodu.Globalization.Calendar.Test` project reports **~3009 tests**; the v2
`Bodu.Globalization.Calendar2.Test` project reports **270**. This document reconciles that gap: how much
is not comparable (row-expansion and deliberately-removed surface), how much is genuine behavioral
coverage v2 should match, and the plan to close it.

Companion to `V1-TO-V2-FUNCTIONAL-AUDIT.md` (capability areas) and `V1-TO-V2-TYPE-METHOD-AUDIT.md`
(member-level). Dispositions below cross-reference those audits.

## The raw counts are not apples-to-apples

v1's 3009 is **~1,700 `[TestMethod]`s expanded by `[DataRow]`/`[DynamicData]` tables**. Three things
inflate the count relative to v2 without representing missing behavior:

1. **KAT / matrix row-expansion.** A single v1 test method drives a large data table. The Easter
   known-answer table alone is **568 rows** (376 Gregorian 1700–2093 + 192 Julian). Calendar KATs,
   trigger/action matrices, and strategy matrices add hundreds more. v2's equivalents are currently
   *smoke-level* (`AlgorithmKnownAnswerTests` = 15 rows total). This is the **real, portable** gap.
2. **N/A — internal plumbing (⚪).** v1 unit-tests pipeline internals v2 deliberately inlined
   (functional-audit area 25): `NotableDateRangePipeline/Planner/Plan`, `…ResolutionCache`,
   `RuleStaticAnalysis`, `ResolvedWindowSet`, `NotableDateCacheEntry`. **72 `[TestMethod]`s** with no v2
   surface to test.
3. **N/A — replaced surface (🔵).** Layers v2 redesigned away: the provider/path-resolver chain
   (`XmlResourceNotableDateRuleProvider`, `ResourcePathResolver`, `…ResourceProviderBase`) and the
   per-rule mutable override provider (**53 methods**, areas 16/18); and the ambient
   `NotableDateContext` — every v1 extension has a duplicate `…_WhenUsingAmbientService_…` /
   `…_AmbientFilterOverload_…` test (**~75 methods**) that cannot exist in v2 (area 20, explicit
   service). Plus CLR-typed algorithm/calendar parser tests (`AlgorithmType`, `CalendarType` as
   `System.Type`) and provenance/cache tests — area 1/4 deferred-by-design.

**Net N/A: ≈250 `[TestMethod]`s** (≈400–500 tests after row-expansion) that should *not* be ported.

## Disposition by area

| v1 area | v1 `[TestMethod]`s | v2 today | Disposition |
|---|---|---|---|
| **Algorithm KATs** (Easter G/J, Vesak, Losar, Qingming, Asalha, Hindu, LunarPhase) | 64 methods / **596 KAT rows** | `AlgorithmKnownAnswerTests` (15 rows) | ⛔ **GAP — port** the exhaustive tables |
| **Calendar-system KATs** (Islamic, UmmAlQura, Jewish, Persian) | ~150 | `CalendarSystemKnownAnswerTests` (11) + data packs | 🟡 **Partial — port** year×holiday tables |
| **Adjustment trigger/action matrices** (`NotableDateAdjusterTests`, `…AdjustmentMatrix`) | ~140 | `AdjustmentTests`/`ExtendedTriggerTests`/`Custom*` | 🟡 **Partial — port** full matrices |
| **Strategy resolution** (`…RuleResolverTests.*`: Fixed, DoWInMonth, RelativeWeekday, WeekdayNear, OffsetFromRule, calendar-system, Hebrew alias) | ~180 | `StrategyResolutionTests` (13) | ⛔ **GAP — port** the matrices |
| **Filter** (`…FilterTests`, `.Validation`, `…FilterCombinators`) | ~180 | `NotableDateFilterTests` (8) | 🟡 **Partial — port** matrices + combinators |
| **Leap-year scenarios** (`…ScenarioTests.LeapYear`) | ~45 | — | ⛔ **GAP — port** |
| **Resolve behavioral scenarios** (`…ScenarioTests.*` Boundaries/WeakSpots/Radical/EdgeCases/base) | **138** | `NotableDateServiceTests`/`AdjacentHolidayTests` | 🟡 **Partial — port** behavioral cases |
| **Applicability truth tables** (`IsApplicable_TruthTable`) | ~15 | (no dedicated file) | ⛔ **GAP — port** |
| **Working-day / traversal / fiscal extensions** | 352 (− ~75 ambient − count-param) | 6 extension test files | 🟡 **Partial — port** non-ambient matrices |
| **Parsers** (`…RuleParser/RuleJsonParser`: enum surface, month token, validation) | ~200 | `JsonIngestionTests`/`SchemaValidationTests`/`…LoaderTests` | 🟡 **Partial — port** enum/month/validation (skip CLR-type, OverrideBody, UseDirective, ClearFlags) |
| **Plugins** | 46 | `Bodu.Globalization.Calendar2.Plugins.Test` (19) | 🟡 **Partial** |
| `TerritoryCode`, `DateRange` value types | 68 | `TerritoryCodeTests`/`DateRangeTests` | ✅ **Covered** |
| **RangeResolution internals** (pipeline/cache/planner/static-analysis/window) | 72 | — | ⚪ **N/A — internal** |
| **Provider chain / path resolver / mutable override** | 53 | — | 🔵 **N/A — replaced** |
| **Ambient `NotableDateContext`** overloads | ~75 | — | 🔵 **N/A — replaced** |

## Genuine gap, prioritized

Estimated **~1,800–2,200 portable tests** of behavioral/KAT coverage, in priority order:

1. **Algorithm KATs** — Easter Gregorian (376) + Julian (192) tables; exhaustive supported-range and
   known-date rows for Vesak/Losar/Qingming/Asalha/Hindu/LunarPhase. *(highest value, most mechanical)*
2. **Strategy-resolution matrices** — `RelativeWeekdayInMonth`, `WeekdayNearDate`, `DayOfWeekInMonth`
   (election-day), `Fixed` across calendar systems (Hijri/Hebrew/Chinese leap-month/sweep), offset chains.
3. **Adjustment trigger/action matrices** — every trigger and action over a parameterized date table.
4. **Calendar-system KATs** — Islamic/UmmAlQura/Jewish/Persian year×holiday reference tables.
5. **Resolve behavioral scenarios + leap-year** — boundaries (year 1/9999, overflow), multi-day spans,
   cross-year substitution, N-consecutive holidays, leap-day handling.
6. **Filter matrices + combinators**, **applicability truth tables**.
7. **Extension matrices** (non-ambient): signed-day add, snap family, territory containment,
   `DateTimeKind`/time-of-day preservation, enumerate.
8. **Parser enum-surface / month-token / validation** (excluding the N/A CLR-typed fields).

## Plan

Port in the priority order above, one batch per commit, each batch adapted to v2 conventions
(`DateOnly`, explicit `(service, territory)`, id-keyed identity, resource-authored fixtures,
`[DynamicData]` KAT records per the repo test guidelines). N/A buckets are explicitly **not** ported.
Progress is tracked by re-running the v2 suite after each batch.

## Outcome

Completed across two parallel waves. The v2 suite grew from **270 to 1,646** green tests: Easter KAT
(Gregorian + Julian, 561), calendar-system KATs (Islamic/UmmAlQura/Jewish/Persian), resolve/leap
scenarios, strategy-resolution matrices, adjustment trigger/action matrices, filter + applicability
truth tables, working-day/traversal/fiscal extension matrices, and parser enum/month/validation/
field-mapping. The remaining distance to v1's ~3009 is the N/A buckets above (internal pipeline,
replaced provider/override/path-resolver layers, ambient `NotableDateContext`, CLR-typed calendar/
algorithm) plus the deliberate-difference collapses below. The port also surfaced two robustness bugs
(weekday/offset strategies threw at year 1/9999), now fixed to skip gracefully.

## Deliberate v2 semantic differences (documented during the port)

The port asserts v2 behaviour where it intentionally diverges from v1; these are by design, not gaps:

- **`OffsetFromRule` does not honour the referenced rule's year bounds.** A dependent resolves the
  anchor's strategy regardless of the anchor rule's `FromYear`/`ToYear`/`OnlyYears`/`ExceptYears`, so it
  emits even in years the anchor's own window would exclude (v1 produced nothing). Kept by decision.
- **String filter predicates are case-sensitive** (`WithTag`/`WithName`/`WithId`/`WithAnyTag`/
  `WithAllTags`/`WithAnyName`, `StringComparison.Ordinal`); v1 was case-insensitive.
- **Filtering is single-stage** `NotableDateFilter.Matches(NotableDate)` applied post-resolution; v1's
  two-stage `IsRuleEligible`/`IsMatch` distinction is gone. `InDateRange` matches the occurrence's
  `Date` (a `DateOnly`) only — no time-stripping, no span-overlap. Filter factories validate `null`
  only (no empty/whitespace/negative throws).
- **Territory containment is one-directional** in resolution/extensions: a parent rule (`AU`) matches a
  child query (`AU-NSW`), but a child rule does not match a parent query.
- **`AdarII` is a leap-dependent alias** (`Month = 0` + alias), not numeric Hebrew month 7.
- **Validation surfaces as `NotableDateValidationException.Diagnostics`** with stable `BODU-CAL2-*`
  codes (schema faults → `BODU-CAL2-SCHEMA`), rather than v1's per-fault typed exceptions.
- **Extensions take an explicit `(service, territory)`** (no ambient context); `Next`/`Previous`
  traversal is single-step (no `count`; use `AddWorkingDays`); `DateTimeOffset` overloads are
  offset-based (no `TimeZoneInfo`).
- **Weekday/offset strategies skip gracefully** (yield no occurrence) at the year-1/9999 representable
  bounds instead of throwing.
