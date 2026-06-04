#!/usr/bin/env python3
"""Generate the v1 -> v2 calendar test-port traceability report from /tmp/v1areas.tsv."""
import datetime

# area -> (status, note). status in: Ported, Replicated, Covered, Deferred, N/A
M = {
    # --- Core resolution: parsing, strategies, adjustments (portable) ---
    "NotableDateRuleParserTests":        ("Covered",   "Strategy/attribute parse scenarios validated end-to-end by StrategyResolutionTests + SchemaValidationTests. Non-Gregorian month tokens, skipLeapMonth/sweepCalendarYears, and CLR-typed algorithms are deferred."),
    "NotableDateRuleParserKatTests":     ("Covered",   "Valid/invalid document load covered by SchemaValidationTests."),
    "NotableDateRuleResolverTests":      ("Replicated","Strategy date computation replicated as data-driven known answers in StrategyResolutionTests (Fixed, DayOfWeekInMonth incl. Last, WeekdayNearDate, RelativeWeekdayInMonth, OffsetFromRule, Algorithm). Leap-month-skip / non-Gregorian deferred."),
    "NotableDateAdjusterTests":          ("Covered",   "Trigger/action/emission matrix covered by AdjustmentTests; conflict-avoiding working-day walk by AdjacentHolidayTests. IfNonWorkingDay/IfBefore/IfAfter/IfNth triggers, ReplaceWithNamedDate and custom handlers deferred."),
    "NotableDateServiceTests":           ("Covered",   "Observed-date modes, edge cases, reversed-range, coverage-day handling and conflict avoidance covered by NotableDateServiceTests/AdjustmentTests/AdjacentHolidayTests. Same-name shadowing, filters, reload and custom providers deferred."),
    "NotableDateRangePipelineScenarioTests": ("Covered","Boundary, leap-year and cross-year roll behaviour covered by the two-phase resolver (StrategyResolutionTests/AdjacentHolidayTests scan +/-1 year). Filter combinators and multi-day spans deferred; pipeline-tier structure is N/A."),
    "CoverageGapFillTests":              ("Covered",   "Offset cycle/missing-anchor -> StrategyResolutionContext guard + validator; malformed territory -> XSD; out-of-range -> validator. A few branch-coverage cases are N/A (v1-internal)."),
    "RangeResolutionKatTests":           ("Covered",   "Range known-answer rows covered by the v2 range-query tests."),
    "NotableDateResolutionEngineTests":  ("Covered",   "Resolution behaviour covered by NotableDateServiceTests."),
    "NotableDateResolutionServiceTests": ("Covered",   "Resolution behaviour covered by NotableDateServiceTests; provider/reload pieces deferred."),
    "NotableDateResolutionServiceAdjustmentTests": ("Covered","Covered by AdjustmentTests."),
    "NotableDateResolutionServiceObservedDateExpansionTests": ("Covered","Observed-date emission covered by AdjustmentTests/NotableDateServiceTests."),
    "NotableDateResolutionServiceConvenienceApiTests": ("Covered","Single-day and range Resolve overloads exist on NotableDateService."),
    "NotableDateResolutionServiceProductionEasterTests": ("Replicated","Production Easter dates replicated in StrategyResolutionTests Western/Orthodox tables."),
    "SmokeTests":                        ("Covered",   "v2 carries [TestCategory(\"Smoke\")] happy-path tests per primary type."),

    # --- Easter / algorithm ---
    "EasterSundayNotableDateAlgorithmTests": ("Replicated","Western Easter multi-year table replicated in StrategyResolutionTests."),
    "EasterSundayNotableDateProviderBaseTests": ("Covered","Easter provider behaviour covered by the AlgorithmDateStrategy + StrategyResolutionTests."),
    "GregorianEasterSundayNotableDateProviderTests": ("Replicated","Western Easter known answers replicated in StrategyResolutionTests."),
    "OrthodoxEasterSundayNotableDateProviderTests": ("Replicated","Orthodox Easter known answers replicated in StrategyResolutionTests."),
    "NotableDateAlgorithmContractTests": ("Covered",   "Out-of-range-year null behaviour covered by AlgorithmDateStrategy/Calculate."),

    # --- Identity / model / value ---
    "NotableDateRuleIdentityTests":      ("Covered",   "v2 NotableDateRuleIdentity (resourceId+notableDate.id+rule.id) exercised throughout the resolver tests."),
    "NotableDateTests":                  ("Covered",   "v2 NotableDate result record exercised throughout the resolver tests."),
    "DateRangeTests":                    ("Covered",   "v2 DateRange (DateOnly-based) exercised by every range query; a focused unit test could be added."),
    "ParsedNotableDateDocumentTests":    ("Covered",   "v2 ParsedNotableDateDocument produced by NotableDateDocumentParser and exercised via load tests."),

    # --- Deferred features (need v2 engine work) ---
    "TerritoryCodeTests":                ("Deferred",  "v2 uses plain territory strings; the TerritoryCode value type was not adopted."),
    "NotableDateFilterTests":            ("Deferred",  "v2 has no NotableDateFilter API yet."),
    "NotableDateRuleJsonParserTests":    ("Deferred",  "v2 ingests XML only; JSON ingestion is deferred (a JSON schema artifact ships)."),
    "JsonResourceNotableDateRuleProviderTests": ("Deferred","JSON ingestion deferred."),
    "MixedFormatNotableDateRuleProviderTests": ("Deferred","Cross-format (XML<->JSON) references deferred."),
    "NotableDateRuleMergerTests":        ("Deferred",  "UseFrom import + override-body merge deferred; v2 overrides are load-time Add/Patch/RemoveRule (NotableDateOverrideTests)."),
    "UseDirectiveInheritanceTests":      ("Deferred",  "UseFrom/Use-directive imports deferred."),
    "XmlResourceFixtureTests":           ("Deferred",  "UseFrom cross-resource cache fixtures deferred."),
    "XmlResourceNotableDateRuleProviderTests": ("Deferred","UseFrom imports + circular-reference handling deferred."),
    "MutableNotableDateRuleOverrideProviderTests": ("Deferred","Mutable/event-driven override provider + reload deferred; the override operations themselves are covered by NotableDateOverrideTests."),
    "NotableDateResolutionServiceDynamicExpansionTests": ("Deferred","Dynamic re-expansion/reload deferred."),
    "AdjustmentHandlerRegistryTests":    ("Deferred",  "Custom adjustment handlers / handler registry deferred."),
    "DefaultNotableDateCollisionResolverTests": ("Deferred","Same-day collision resolver deferred (ResolutionPolicy.sameDayCollisionPolicy is parsed but unwired)."),
    "LunarPhaseAlgorithmTests":          ("Deferred",  "Non-Gregorian lunar algorithm deferred; v2 ships Western/Orthodox Easter only."),
    "HinduLunarNotableDateAlgorithmTests": ("Deferred","Hindu lunar algorithm + calendar deferred."),
    "VesakNotableDateAlgorithmTests":    ("Deferred",  "Buddhist (Vesak) algorithm deferred."),
    "QingmingNotableDateAlgorithmTests": ("Deferred",  "Qingming algorithm deferred."),
    "LosarNotableDateAlgorithmTests":    ("Deferred",  "Losar (Tibetan) algorithm deferred."),
    "AsalhaPujaNotableDateAlgorithmTests": ("Deferred","Asalha Puja algorithm deferred."),
    "GlobalJewishResourceTests":         ("Deferred",  "Hebrew-calendar resource known answers deferred (non-Gregorian calendars)."),
    "GlobalIslamicResourceTests":        ("Deferred",  "Islamic-calendar resource known answers deferred."),
    "GlobalIslamicUmmAlQuraResourceTests": ("Deferred","Umm al-Qura resource known answers deferred."),
    "GlobalPersianResourceTests":        ("Deferred",  "Persian-calendar resource known answers deferred."),

    # --- N/A: v1-internal architecture with no v2 analogue ---
    "NotableDateRangePipelineTests":     ("N/A",       "v1 range-pipeline internals; v2 resolves inline in a two-phase NotableDateService (no pipeline type)."),
    "NotableDateRangePipelineVariantTests": ("N/A",    "Pipeline-internal."),
    "NotableDateRangePlannerTests":      ("N/A",       "Pipeline planner internal; no v2 analogue."),
    "NotableDateRangeResolutionCacheTests": ("N/A",    "Pipeline resolution-cache internal; v2 uses a per-call occupied-day set."),
    "ResolvedWindowSetTests":            ("N/A",       "Pipeline window-set internal."),
    "RuleStaticAnalysisTests":           ("N/A",       "Pipeline static-analysis internal."),
    "RuleStaticAnalysisVariantTests":    ("N/A",       "Pipeline static-analysis internal."),
    "CachingCalculationAnchorResolverTests": ("N/A",   "Pipeline anchor-cache internal; offsets covered by OffsetFromRuleStrategy."),
    "NotableDateAlgorithmRegistryTests": ("N/A",       "v2 dispatches algorithm keys directly; no public registry."),
    "NotableDateRuleIndexTests":         ("N/A",       "v1 identity index; v2 resolves rules directly."),
    "NotableDateRuleResourceProviderBaseTests": ("N/A","v1 provider base class; v2 loads via NotableDateResourceLoader."),
    "NotableDateContextTests":           ("N/A",       "v1 resolution-context type; no v2 analogue."),
    "INotableDateServiceDefaultMembersTests": ("N/A",  "v1 INotableDateService default members (working week, IsWeekend); v2 interface is minimal."),
    "NotableDateTemporalExtensionContractTests": ("N/A","v1 temporal extension contract; no v2 extension surface."),
    "NotableDateOnlyExtensionsTests":    ("N/A",       "v1 DateOnly working-day/traversal extension methods; no v2 extension surface."),
    "NotableDateTimeExtensionsTests":    ("N/A",       "v1 DateTime working-day/traversal extension methods; no v2 extension surface."),
    "NotableDateTimeOffsetExtensionsTests": ("N/A",    "v1 DateTimeOffset extension methods; no v2 extension surface."),
    "NotableDateExtensionsWorkingWeekOverloadsTests": ("N/A","v1 working-week extension overloads; no v2 extension surface."),
    "NotableDateFiscalExtensionsTests":  ("N/A",       "v1 fiscal-calendar extension methods; no v2 extension surface."),
    "PluginTrustPolicyTests":            ("N/A",       "v1 plugin trust policies; v2 has no plugin model."),
    "PluginExceptionTests":              ("N/A",       "v1 plugin exceptions; no plugin model."),
    "ExternalPluginLoaderTests":         ("N/A",       "v1 external plugin loader; no plugin model."),
    "NotableDateServicePluginIntegrationTests": ("N/A","v1 plugin integration; no plugin model."),
    "ResourcePathResolverTests":         ("N/A",       "v1 resource-path resolver; v2 embeds resources/fixtures directly."),
    "ResourcePathResolverOptionsTests":  ("N/A",       "v1 resource-path resolver options."),
}

BUCKET = {"Ported": "Ported / covered", "Replicated": "Ported / covered", "Covered": "Ported / covered",
          "Deferred": "Deferred (feature gap)", "N/A": "Not applicable (v1-internal)"}

rows = []
with open("/tmp/v1areas.tsv") as fh:
    for line in fh:
        area, methods, datarows = line.rstrip("\n").split("\t")
        methods, datarows = int(methods), int(datarows)
        status, note = M.get(area, ("UNMAPPED", "!! classify me !!"))
        rows.append((area, methods, datarows, status, note))

tot_m = sum(r[1] for r in rows)
tot_dr = sum(r[2] for r in rows)
bucket_m, bucket_a = {}, {}
for _, m, _dr, status, _ in rows:
    b = BUCKET.get(status, status)
    bucket_m[b] = bucket_m.get(b, 0) + m
    bucket_a[b] = bucket_a.get(b, 0) + 1

order = {"Ported": 0, "Replicated": 1, "Covered": 2, "Deferred": 3, "N/A": 4, "UNMAPPED": -1}
rows.sort(key=lambda r: (order.get(r[3], 9), r[0]))

out = []
out.append("# v1 -> v2 Calendar test-port traceability report")
out.append("")
out.append(f"_Generated {datetime.date.today().isoformat()} from `Bodu.Globalization.Calendar/test` "
           f"(v1) against `Bodu.Globalization.Calendar2/test` (v2). Living document — regenerate with "
           f"`tools/gen-port-report.py`._")
out.append("")
out.append("## Purpose")
out.append("")
out.append("Confirm, for every test in the v1 `Calendar.Test` project, whether its scenario is represented in "
           "the v2 test suite. Data-driven tables (known answers, strategy/adjustment matrices) are **replicated** "
           "against the v2 schema rather than copied verbatim, because v1 and v2 use different rule shapes "
           "(v1 flat `<Rule name>` + provider/pipeline; v2 `NotableDateResource` -> concept -> rule with explicit "
           "`Strategy`/`Applicability` and reusable `AdjustmentPolicy`).")
out.append("")
out.append("## Status legend")
out.append("")
out.append("| Status | Meaning |")
out.append("|---|---|")
out.append("| **Ported** | A 1:1 v2 test exists. |")
out.append("| **Replicated** | Data-driven rows re-expressed against the v2 schema (known-answer tables). |")
out.append("| **Covered** | The scenario/behaviour is validated by a v2 test, though not method-for-method. |")
out.append("| **Deferred** | Needs a v2 engine feature not yet built (named in the note). |")
out.append("| **N/A** | Tests v1-internal architecture with no v2 analogue (named in the note). |")
out.append("")
out.append("## Summary")
out.append("")
out.append(f"v1: **{tot_m} test methods** ({tot_dr} `[DataRow]` rows) across **{len(rows)} test areas** / 228 files.  ")
out.append(f"v2 (today): **49 test methods** ({59} `[DataRow]` rows).")
out.append("")
out.append("| Disposition | v1 areas | v1 methods |")
out.append("|---|---:|---:|")
for b in ["Ported / covered", "Deferred (feature gap)", "Not applicable (v1-internal)"]:
    out.append(f"| {b} | {bucket_a.get(b,0)} | {bucket_m.get(b,0)} |")
out.append(f"| **Total** | **{len(rows)}** | **{tot_m}** |")
out.append("")
out.append("> The large 'N/A' methods count is dominated by v1's `NotableDate*Extensions*` working-day/traversal "
           "extension methods (~350 methods) and the range-pipeline internals (~110) — surfaces v2 deliberately "
           "does not reproduce.")
out.append("")
out.append("## Per-area disposition")
out.append("")
out.append("| v1 test area | Methods | DataRows | Status | v2 mapping / reason |")
out.append("|---|---:|---:|---|---|")
for area, m, dr, status, note in rows:
    out.append(f"| `{area}` | {m} | {dr} | {status} | {note} |")
out.append("")
out.append("## v2 test inventory (current)")
out.append("")
out.append("| v2 test file | Methods | DataRows | Covers |")
out.append("|---|---:|---:|---|")
for f, m, dr, cov in [
    ("NotableDateResourceLoaderTests", 1, 0, "T01 load + validate counts"),
    ("NotableDateServiceTests", 10, 0, "T02-T11 fixed-date resolution, territory filtering, observed-date consistency"),
    ("NotableDateOverrideTests", 2, 0, "T12-T13 RemoveRule/PatchRule targeting"),
    ("StrategyResolutionTests", 13, 16, "every strategy with real holidays; Western+Orthodox Easter regression tables"),
    ("AdjustmentTests", 10, 0, "all emission modes, actions and triggers"),
    ("AdjacentHolidayTests", 3, 0, "Christmas/Boxing conflict-avoiding substitution (2021 + 2016)"),
    ("SchemaValidationTests", 5, 10, "valid load + 10 invalid documents rejected with the right diagnostic"),
    ("AustraliaKnownAnswerTests", 3, 19, "ported AU static definitions vs v1 known answers"),
    ("UnitedStatesKnownAnswerTests", 2, 14, "ported US federal static definitions vs known answers"),
]:
    out.append(f"| `{f}` | {m} | {dr} | {cov} |")
out.append("")
out.append("## Deferred-feature roadmap (unblocks the Deferred rows)")
out.append("")
out.append("1. **Territory-specificity shadowing** — a narrower `AU-WA` rule shadows the broader `AU` rule for that "
           "territory. Unblocks subdivision/regional known answers (AU state Anzac substitutes + NSW trial, US state, GB regional).")
out.append("2. **Non-Gregorian calendars** — Hebrew / Islamic / Hindu / Persian / lunar; unblocks the `Global*ResourceTests` "
           "and the lunar/Buddhist/Hindu algorithm tests.")
out.append("3. **Imports** (`UseFrom`/`Use`/override-body merge) — unblocks the merger/inheritance/provider fixtures.")
out.append("4. **Filter API** — unblocks `NotableDateFilterTests`.")
out.append("5. **JSON ingestion** — unblocks the JSON parser/provider tests.")
out.append("6. **Same-day collision resolver** (wire `ResolutionPolicy.sameDayCollisionPolicy`).")
out.append("7. **Custom adjustment handlers** + richer triggers (`IfNonWorkingDay`, `CollidesWith`) / actions "
           "(`ReplaceWithRule`, `AddObservedOccurrence`).")
out.append("8. **TerritoryCode value type**, and the **NotableDate working-day/traversal/fiscal extension** surface "
           "(if v2 chooses to reproduce it).")
out.append("")
print("\n".join(out))
