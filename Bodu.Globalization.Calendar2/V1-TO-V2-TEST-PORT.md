# v1 -> v2 Calendar test-port traceability report

_Generated 2026-06-03 from `Bodu.Globalization.Calendar/test` (v1) against `Bodu.Globalization.Calendar2/test` (v2). Living document — regenerate with `tools/gen-port-report.py`._

## Purpose

Confirm, for every test in the v1 `Calendar.Test` project, whether its scenario is represented in the v2 test suite. Data-driven tables (known answers, strategy/adjustment matrices) are **replicated** against the v2 schema rather than copied verbatim, because v1 and v2 use different rule shapes (v1 flat `<Rule name>` + provider/pipeline; v2 `NotableDateResource` -> concept -> rule with explicit `Strategy`/`Applicability` and reusable `AdjustmentPolicy`).

## Status legend

| Status | Meaning |
|---|---|
| **Ported** | A 1:1 v2 test exists. |
| **Replicated** | Data-driven rows re-expressed against the v2 schema (known-answer tables). |
| **Covered** | The scenario/behaviour is validated by a v2 test, though not method-for-method. |
| **Deferred** | Needs a v2 engine feature not yet built (named in the note). |
| **N/A** | Tests v1-internal architecture with no v2 analogue (named in the note). |

## Summary

v1: **1645 test methods** (686 `[DataRow]` rows) across **72 test areas** / 228 files.  
v2 (today): **50 test methods** (62 `[DataRow]` rows).

| Disposition | v1 areas | v1 methods |
|---|---:|---:|
| Ported / covered | 24 | 707 |
| Deferred (feature gap) | 23 | 396 |
| Not applicable (v1-internal) | 25 | 542 |
| **Total** | **72** | **1645** |

> The large 'N/A' methods count is dominated by v1's `NotableDate*Extensions*` working-day/traversal extension methods (~350 methods) and the range-pipeline internals (~110) — surfaces v2 deliberately does not reproduce.

## Per-area disposition

| v1 test area | Methods | DataRows | Status | v2 mapping / reason |
|---|---:|---:|---|---|
| `EasterSundayNotableDateAlgorithmTests` | 7 | 4 | Replicated | Western Easter multi-year table replicated in StrategyResolutionTests. |
| `GregorianEasterSundayNotableDateProviderTests` | 8 | 13 | Replicated | Western Easter known answers replicated in StrategyResolutionTests. |
| `NotableDateResolutionServiceProductionEasterTests` | 5 | 0 | Replicated | Production Easter dates replicated in StrategyResolutionTests Western/Orthodox tables. |
| `NotableDateRuleResolverTests` | 80 | 113 | Replicated | Strategy date computation replicated as data-driven known answers in StrategyResolutionTests (Fixed, DayOfWeekInMonth incl. Last, WeekdayNearDate, RelativeWeekdayInMonth, OffsetFromRule, Algorithm). Leap-month-skip / non-Gregorian deferred. |
| `OrthodoxEasterSundayNotableDateProviderTests` | 7 | 10 | Replicated | Orthodox Easter known answers replicated in StrategyResolutionTests. |
| `CoverageGapFillTests` | 23 | 0 | Covered | Offset cycle/missing-anchor -> StrategyResolutionContext guard + validator; malformed territory -> XSD; out-of-range -> validator. A few branch-coverage cases are N/A (v1-internal). |
| `DateRangeTests` | 17 | 0 | Covered | v2 DateRange (DateOnly-based) exercised by every range query; a focused unit test could be added. |
| `EasterSundayNotableDateProviderBaseTests` | 7 | 2 | Covered | Easter provider behaviour covered by the AlgorithmDateStrategy + StrategyResolutionTests. |
| `NotableDateAdjusterTests` | 42 | 23 | Covered | Trigger/action/emission matrix covered by AdjustmentTests; conflict-avoiding working-day walk by AdjacentHolidayTests. IfNonWorkingDay/IfBefore/IfAfter/IfNth triggers, ReplaceWithNamedDate and custom handlers deferred. |
| `NotableDateAlgorithmContractTests` | 5 | 0 | Covered | Out-of-range-year null behaviour covered by AlgorithmDateStrategy/Calculate. |
| `NotableDateRangePipelineScenarioTests` | 138 | 149 | Covered | Boundary, leap-year and cross-year roll behaviour covered by the two-phase resolver (StrategyResolutionTests/AdjacentHolidayTests scan +/-1 year). Filter combinators and multi-day spans deferred; pipeline-tier structure is N/A. |
| `NotableDateResolutionEngineTests` | 3 | 0 | Covered | Resolution behaviour covered by NotableDateServiceTests. |
| `NotableDateResolutionServiceAdjustmentTests` | 2 | 0 | Covered | Covered by AdjustmentTests. |
| `NotableDateResolutionServiceConvenienceApiTests` | 7 | 0 | Covered | Single-day and range Resolve overloads exist on NotableDateService. |
| `NotableDateResolutionServiceObservedDateExpansionTests` | 4 | 0 | Covered | Observed-date emission covered by AdjustmentTests/NotableDateServiceTests. |
| `NotableDateResolutionServiceTests` | 10 | 0 | Covered | Resolution behaviour covered by NotableDateServiceTests; provider/reload pieces deferred. |
| `NotableDateRuleIdentityTests` | 11 | 0 | Covered | v2 NotableDateRuleIdentity (resourceId+notableDate.id+rule.id) exercised throughout the resolver tests. |
| `NotableDateRuleParserKatTests` | 2 | 0 | Covered | Valid/invalid document load covered by SchemaValidationTests. |
| `NotableDateRuleParserTests` | 112 | 11 | Covered | Strategy/attribute parse scenarios validated end-to-end by StrategyResolutionTests + SchemaValidationTests. Non-Gregorian month tokens, skipLeapMonth/sweepCalendarYears, and CLR-typed algorithms are deferred. |
| `NotableDateServiceTests` | 194 | 16 | Covered | Observed-date modes, edge cases, reversed-range, coverage-day handling and conflict avoidance covered by NotableDateServiceTests/AdjustmentTests/AdjacentHolidayTests; territory-specificity shadowing covered by AustraliaKnownAnswerTests. Filters, reload and custom providers deferred. |
| `NotableDateTests` | 16 | 6 | Covered | v2 NotableDate result record exercised throughout the resolver tests. |
| `ParsedNotableDateDocumentTests` | 4 | 0 | Covered | v2 ParsedNotableDateDocument produced by NotableDateDocumentParser and exercised via load tests. |
| `RangeResolutionKatTests` | 1 | 0 | Covered | Range known-answer rows covered by the v2 range-query tests. |
| `SmokeTests` | 2 | 0 | Covered | v2 carries [TestCategory("Smoke")] happy-path tests per primary type. |
| `AdjustmentHandlerRegistryTests` | 9 | 10 | Deferred | Custom adjustment handlers / handler registry deferred. |
| `AsalhaPujaNotableDateAlgorithmTests` | 3 | 0 | Deferred | Asalha Puja algorithm deferred. |
| `DefaultNotableDateCollisionResolverTests` | 8 | 0 | Deferred | Same-day collision resolver deferred (ResolutionPolicy.sameDayCollisionPolicy is parsed but unwired). |
| `GlobalIslamicResourceTests` | 3 | 18 | Deferred | Islamic-calendar resource known answers deferred. |
| `GlobalIslamicUmmAlQuraResourceTests` | 4 | 24 | Deferred | Umm al-Qura resource known answers deferred. |
| `GlobalJewishResourceTests` | 4 | 59 | Deferred | Hebrew-calendar resource known answers deferred (non-Gregorian calendars). |
| `GlobalPersianResourceTests` | 4 | 13 | Deferred | Persian-calendar resource known answers deferred. |
| `HinduLunarNotableDateAlgorithmTests` | 11 | 14 | Deferred | Hindu lunar algorithm + calendar deferred. |
| `JsonResourceNotableDateRuleProviderTests` | 6 | 0 | Deferred | JSON ingestion deferred. |
| `LosarNotableDateAlgorithmTests` | 3 | 0 | Deferred | Losar (Tibetan) algorithm deferred. |
| `LunarPhaseAlgorithmTests` | 7 | 0 | Deferred | Non-Gregorian lunar algorithm deferred; v2 ships Western/Orthodox Easter only. |
| `MixedFormatNotableDateRuleProviderTests` | 2 | 0 | Deferred | Cross-format (XML<->JSON) references deferred. |
| `MutableNotableDateRuleOverrideProviderTests` | 29 | 7 | Deferred | Mutable/event-driven override provider + reload deferred; the override operations themselves are covered by NotableDateOverrideTests. |
| `NotableDateFilterTests` | 100 | 32 | Deferred | v2 has no NotableDateFilter API yet. |
| `NotableDateResolutionServiceDynamicExpansionTests` | 2 | 0 | Deferred | Dynamic re-expansion/reload deferred. |
| `NotableDateRuleJsonParserTests` | 126 | 83 | Deferred | v2 ingests XML only; JSON ingestion is deferred (a JSON schema artifact ships). |
| `NotableDateRuleMergerTests` | 20 | 0 | Deferred | UseFrom import + override-body merge deferred; v2 overrides are load-time Add/Patch/RemoveRule (NotableDateOverrideTests). |
| `QingmingNotableDateAlgorithmTests` | 3 | 0 | Deferred | Qingming algorithm deferred. |
| `TerritoryCodeTests` | 8 | 43 | Deferred | v2 uses plain territory strings; the TerritoryCode value type was not adopted. |
| `UseDirectiveInheritanceTests` | 20 | 10 | Deferred | UseFrom/Use-directive imports deferred. |
| `VesakNotableDateAlgorithmTests` | 3 | 0 | Deferred | Buddhist (Vesak) algorithm deferred. |
| `XmlResourceFixtureTests` | 6 | 0 | Deferred | UseFrom cross-resource cache fixtures deferred. |
| `XmlResourceNotableDateRuleProviderTests` | 15 | 3 | Deferred | UseFrom imports + circular-reference handling deferred. |
| `CachingCalculationAnchorResolverTests` | 7 | 3 | N/A | Pipeline anchor-cache internal; offsets covered by OffsetFromRuleStrategy. |
| `ExternalPluginLoaderTests` | 11 | 2 | N/A | v1 external plugin loader; no plugin model. |
| `INotableDateServiceDefaultMembersTests` | 7 | 0 | N/A | v1 INotableDateService default members (working week, IsWeekend); v2 interface is minimal. |
| `NotableDateAlgorithmRegistryTests` | 9 | 10 | N/A | v2 dispatches algorithm keys directly; no public registry. |
| `NotableDateContextTests` | 5 | 0 | N/A | v1 resolution-context type; no v2 analogue. |
| `NotableDateExtensionsWorkingWeekOverloadsTests` | 7 | 0 | N/A | v1 working-week extension overloads; no v2 extension surface. |
| `NotableDateFiscalExtensionsTests` | 5 | 0 | N/A | v1 fiscal-calendar extension methods; no v2 extension surface. |
| `NotableDateOnlyExtensionsTests` | 155 | 0 | N/A | v1 DateOnly working-day/traversal extension methods; no v2 extension surface. |
| `NotableDateRangePipelineTests` | 28 | 0 | N/A | v1 range-pipeline internals; v2 resolves inline in a two-phase NotableDateService (no pipeline type). |
| `NotableDateRangePipelineVariantTests` | 3 | 0 | N/A | Pipeline-internal. |
| `NotableDateRangePlannerTests` | 9 | 0 | N/A | Pipeline planner internal; no v2 analogue. |
| `NotableDateRangeResolutionCacheTests` | 19 | 0 | N/A | Pipeline resolution-cache internal; v2 uses a per-call occupied-day set. |
| `NotableDateRuleIndexTests` | 7 | 0 | N/A | v1 identity index; v2 resolves rules directly. |
| `NotableDateRuleResourceProviderBaseTests` | 4 | 0 | N/A | v1 provider base class; v2 loads via NotableDateResourceLoader. |
| `NotableDateServicePluginIntegrationTests` | 5 | 0 | N/A | v1 plugin integration; no plugin model. |
| `NotableDateTemporalExtensionContractTests` | 3 | 0 | N/A | v1 temporal extension contract; no v2 extension surface. |
| `NotableDateTimeExtensionsTests` | 174 | 0 | N/A | v1 DateTime working-day/traversal extension methods; no v2 extension surface. |
| `NotableDateTimeOffsetExtensionsTests` | 6 | 0 | N/A | v1 DateTimeOffset extension methods; no v2 extension surface. |
| `PluginExceptionTests` | 11 | 2 | N/A | v1 plugin exceptions; no plugin model. |
| `PluginTrustPolicyTests` | 19 | 0 | N/A | v1 plugin trust policies; v2 has no plugin model. |
| `ResolvedWindowSetTests` | 17 | 0 | N/A | Pipeline window-set internal. |
| `ResourcePathResolverOptionsTests` | 3 | 0 | N/A | v1 resource-path resolver options. |
| `ResourcePathResolverTests` | 13 | 6 | N/A | v1 resource-path resolver; v2 embeds resources/fixtures directly. |
| `RuleStaticAnalysisTests` | 12 | 0 | N/A | Pipeline static-analysis internal. |
| `RuleStaticAnalysisVariantTests` | 3 | 0 | N/A | Pipeline static-analysis internal. |

## v2 test inventory (current)

| v2 test file | Methods | DataRows | Covers |
|---|---:|---:|---|
| `NotableDateResourceLoaderTests` | 1 | 0 | T01 load + validate counts |
| `NotableDateServiceTests` | 10 | 0 | T02-T11 fixed-date resolution, territory filtering, observed-date consistency |
| `NotableDateOverrideTests` | 2 | 0 | T12-T13 RemoveRule/PatchRule targeting |
| `StrategyResolutionTests` | 13 | 16 | every strategy with real holidays; Western+Orthodox Easter regression tables |
| `AdjustmentTests` | 10 | 0 | all emission modes, actions and triggers |
| `AdjacentHolidayTests` | 3 | 0 | Christmas/Boxing conflict-avoiding substitution (2021 + 2016) |
| `SchemaValidationTests` | 5 | 10 | valid load + 10 invalid documents rejected with the right diagnostic |
| `AustraliaKnownAnswerTests` | 4 | 22 | ported AU static definitions vs v1 known answers; WA/NT Anzac substitutes + a dedicated territory-shadowing test |
| `UnitedStatesKnownAnswerTests` | 2 | 14 | ported US federal static definitions vs known answers |

## Deferred-feature roadmap (unblocks the Deferred rows)

1. ~~**Territory-specificity shadowing**~~ — ✅ **done**: a narrower `AU-WA` rule now shadows the broader `AU` rule for that territory (`RuleApplicability.MatchSpecificity` + `NotableDateService.GatherCandidates`). Unblocked the AU state Anzac substitutes (WA/NT, now ported); remains available for the broader regional tables (NSW trial with adjustment-level year bounds, full US state, GB regional) once those data sets are ported.
2. **Non-Gregorian calendars** — Hebrew / Islamic / Hindu / Persian / lunar; unblocks the `Global*ResourceTests` and the lunar/Buddhist/Hindu algorithm tests.
3. **Imports** (`UseFrom`/`Use`/override-body merge) — unblocks the merger/inheritance/provider fixtures.
4. **Filter API** — unblocks `NotableDateFilterTests`.
5. **JSON ingestion** — unblocks the JSON parser/provider tests.
6. **Same-day collision resolver** (wire `ResolutionPolicy.sameDayCollisionPolicy`).
7. **Custom adjustment handlers** + richer triggers (`IfNonWorkingDay`, `CollidesWith`) / actions (`ReplaceWithRule`, `AddObservedOccurrence`).
8. **TerritoryCode value type**, and the **NotableDate working-day/traversal/fiscal extension** surface (if v2 chooses to reproduce it).

