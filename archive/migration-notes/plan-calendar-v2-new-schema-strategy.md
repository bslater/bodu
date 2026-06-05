# Calendar v2 — New Schema & Library Rearchitecture Instructions

> **Status:** Planning brief for a new session. Read this file top to bottom, then
> the two companion documents in this folder before writing any code.
>
> **Companion documents (same directory, repo root):**
> - `plan-calendar-v2-schema-design.md` — the full revised cookbook schema strategy
>   (resource model, XSD shape, simple types, validation rules, runtime pipeline,
>   worked examples). *Authoritative for the schema.*
> - `plan-calendar-v2-minimal-test-strategy.md` — the minimal first-version test
>   catalogue (fixture, expected-result model, T01–T13, exit criteria). *Authoritative
>   for what "done" means for the first functional cut.*

---

## 1. Objective

Rearchitect the Bodu calendar capability onto a **revised cookbook schema** that
**separates three concerns currently intertwined** in a single flat `Rule`:

1. **The notable-date concept** — *what* the date is (stable identity, display name,
   category, defaults, tags). One concept, e.g. `anzac-day` or `constitution-day`.
2. **The rule(s) that calculate occurrences** — *when/where* it occurs. One concept
   may have **several rules** (per territory, era, calendar, or variant), each with its
   own applicability + a single calculation strategy.
3. **The observance / adjustment policy** — *how* calculated occurrences are
   transformed, supplemented, or suppressed (weekend → observed Monday, substitution,
   additional observance, suppression), expressed as **reusable, scoped policies** with
   explicit emission semantics.

Separating these gives the three properties the current design lacks:

- **Stable identity** — a rule is identified by `resourceId + notableDate.id + rule.id`,
  never by display name.
- **Precise overrides** — imports and overrides target explicit IDs (`PatchRule`,
  `RemoveRule`, …), so a patch/removal never collapses sibling rules.
- **Deterministic priority** — one documented meaning of priority across rule
  selection, adjustment selection, override application, and collisions.

## 2. Approach (high level)

**Build a new calendar library** rather than mutating the existing one in place.
**Borrow heavily** from the current implementation — its proven engine code, BCL-grade
documentation, validation discipline, identity model, test infrastructure, and
conventions — but assemble it on the **revised schema structure**.

Sequence the build so each layer is proven before the next is added:

1. **Schema + load + validate + a straightforward resolution engine first.** Parse the
   revised XML (and JSON) cookbook, validate it against the new XSD/JSON-schema *and*
   the runtime validation rules, and resolve **fixed-date rules** with **territory
   filtering**, **rule identity**, **one adjustment policy (weekend → observed Monday,
   `ObservedOnly`)**, and **basic add/remove/patch overrides**. This is the slice the
   minimal test catalogue (T01–T13) exercises. Getting this right proves the schema and
   the separation-of-concerns model.
2. **Then build the resolution pipeline and replicate the rest.** Layer in the full
   strategy catalogue, the chronological range pipeline (candidate/fringe passes,
   caching), the complete adjustment trigger/action/emission matrix, reusable
   adjustment policies, imports/overrides at full fidelity, collision/duplicate
   policies, non-Gregorian calendars, algorithm registry, DI integration, the source
   generator/builder, and the regional data packs — reaching feature parity with (and
   beyond) the current library.

Keep the **existing** `Bodu.Globalization.Calendar*` projects **intact and shipping**
throughout. The new work lives in new project(s) (suggested name
`Bodu.Globalization.Calendar2` / namespace `Bodu.Globalization.Calendar.V2`, or a name
the maintainer prefers) so the two can coexist until parity + migration are complete.

## 3. Order of execution (required)

1. **Review the existing Calendar codebase.** Understand the current model and, crucially,
   *what to keep* (Section 6) and *what to leave behind* (Section 7).
2. **Review the problem statement** (Section 5) and understand the limitations of the
   current schema design.
3. **Review the proposed schema redesign** — `plan-calendar-v2-schema-design.md` in full.
4. **Plan the new Calendar library** so it inherits all the strong design elements of the
   current library (Section 6) on top of the revised schema. Produce a concrete project
   layout, public surface sketch, and a phased delivery plan. (Use plan mode; get the
   plan approved before building.)
5. **Build a small but functional new library** that exercises all core functionality in
   notable-date resolution and adjustment per the **minimal test catalogue** in
   `plan-calendar-v2-minimal-test-strategy.md`. The first cut must cover at least:
   - load + validate the minimal cookbook (3 concepts, 5 rules, 1 adjustment policy);
   - fixed-date resolution with territory filtering and stable rule identity;
   - a notable date with **multiple rules** (ANZAC AU/NZ; Constitution Day US/PR) that do
     **not** collapse or leak across territories;
   - weekend → observed-Monday adjustment with `ObservedOnly` emission;
   - **single-day and range queries returning consistent observed results**;
   - `RemoveRule` / `PatchRule` overrides that target **exactly one** rule identity.
   Meet the **exit criteria** at the end of the test-strategy document before moving on
   to Phase 2 capabilities.

## 4. Design north star (from the schema strategy)

```text
NotableDateResource (resourceId)
 ├─ Metadata
 ├─ Imports            (ID-based: <Import resource><Include notableDateRef><PatchRule ruleRef>)
 ├─ ResolutionPolicy   (duplicate / collision / priorityDirection / observedDateRange)
 ├─ AdjustmentPolicies (reusable, scoped: Scope + Trigger + Action + Emission)
 ├─ NotableDates
 │   └─ NotableDate (id, displayName, category, defaults, Tags)
 │       └─ Rules
 │           └─ Rule (id, priority, Applicability, Strategy, Adjustments→policyRef)
 └─ Overrides          (AddRule/PatchRule/ReplaceRule/RemoveRule/…, ID-targeted)
```

Identity: `resourceId + notableDate.id + rule.id`. Display names are presentation only —
never lookup, merge, override, or shadow keys. Territories are explicit
`<Territory code="…"/>` elements (no comma-delimited strings). Emission modes
(`ActualOnly` / `ObservedOnly` / `ActualAndObserved` / `ObservedAsAdditional` /
`Suppress`) make observed-date behaviour explicit and query-width independent. See the
schema document for the complete model, XSD shape, simple types, validation table, and
the runtime resolution pipeline.

## 5. Problem statement this rearchitecture resolves

The current schema models a notable date as a flat list of `<Rule>`s under a
`<NotableDate name="X">`, where the rule's canonical `Name` is the `<NotableDate name>`
and `RuleName` is a per-rule (effectively unique) id. Several design flaws follow from
using **display/canonical name as identity**:

- **Name-based territory shadowing.** The range planner groups eligible rules by
  canonical `Name` and suppresses any same-name rule whose territory is *strictly
  broader* than the query when a narrower one exists. This is correct for territory
  *specializations* of one holiday (e.g. **ANZAC Day** AU vs AU-NSW — same date, NSW just
  adds a substitute) but **wrong** when two rules merely *share a title*:
  - **Constitution Day** — US (17 Sep) vs Puerto Rico (25 Jul). A `US-PR` query suppresses
    the broader US rule and **loses 17 Sep**, though both are real, different dates.
  - **Family Day** — a global awareness observance (15 May) vs a Canadian statutory
    holiday (3rd Mon Feb). A `CA-ON` query drops the global observance.
  - **Foundation Day** — national vs provincial, different dates; the national one is
    suppressed for a provincial query.
  Because `RuleName` is per-rule unique, the "should shadow" and "should coexist" cases
  are **structurally identical** under the current model — the engine cannot tell them
  apart, so any purely-name-based fix either keeps the bug or breaks the legitimate
  cases.
- **Ambiguous identity / fragile overrides.** Name-keyed lookups, merges, and removals
  can collapse or over-target sibling rules.
- **Unclear priority and observed-date semantics.** Priority lacked a single meaning;
  observed-date emission could differ between single-day and range queries.

The revised schema **resolves this structurally**: a single `NotableDate` concept owns
multiple explicitly-identified `Rule` variants; territory filtering returns the rule for
the requested territory; nothing is keyed on display name; and "coexist vs replace" is an
authoring decision expressed through distinct rules, explicit priority, and explicit
collision policy — not inferred from a shared name. (For context, the prior single-schema
remediation that was *deferred* in favour of this rearchitecture is the same
shadowing/coexistence problem; v2 removes the need for name-based shadowing entirely.)

## 6. What to BORROW from the current library (strong elements to inherit)

Carry these across — port the code where it fits the new model, otherwise mirror the
pattern:

- **Argument validation discipline** — `Bodu.Core` `ThrowHelper.ThrowIf…` plus the
  per-domain `CalendarThrowHelper` partial pattern; group guards at the top of members.
- **Resourced exception/diagnostic text** — `CalendarResourceStrings.{resx,Designer.cs}`
  with the `Arg_/Op_/Format_/…` key conventions. No hard-coded message literals.
- **The identity model** — `NotableDateRuleIdentity`, `NotableDateRuleReference`, and the
  identity-keyed `NotableDateRuleIndex` with deterministic, context-aware reference
  resolution and explicit *ambiguous* results. v2's `resourceId+notableDate.id+rule.id`
  is the natural evolution; reuse the disambiguation/ambiguity-reporting approach.
- **The validator** — `NotableDateRuleValidator`'s pre-execution pass (duplicate
  identities, missing/ambiguous anchors and replacement targets, unregistered algorithms)
  and the `NotableDateValidationDiagnostic` severity model. Expand to the new validation
  table in the schema doc (Section 21 there).
- **The tiered range-resolution pipeline** — `RuleStaticAnalysis`, `NotableDateRangePlan`,
  `NotableDateRangePlanner`, `NotableDateRangePipeline`, `NotableDateRangeResolutionCache`
  (Fixed → OffsetFromFixed → Algorithmic → OffsetFromAlgorithmic, candidate + fringe
  passes, identity-keyed caching). This is the most valuable engine asset — keep its
  structure, re-key it on v2 identities and `OffsetFromRule`.
- **Observed-date modes & first-active-wins adjustments** — `ObservedDateMode`, the
  adjuster's ascending-priority first-active-wins evaluation, `MaxAdjustmentReachDays`
  reach hints, scoped-adjustment gating (`AppliesToGlobalRules`). Generalize into the
  reusable `AdjustmentPolicy` with `Scope`/`Trigger`/`Action`/`Emission`.
- **Resolution strategies** — the calculation logic in `NotableDateRuleResolver`
  (Fixed incl. non-Gregorian calendar sweeps/leap-month handling, DayOfWeekInMonth,
  WeekdayNearDate, RelativeWeekdayInMonth, offset chains, algorithm registry dispatch).
  Re-home under the single `<Strategy>` wrapper and `OffsetFromRule`.
- **Algorithm registry & DI** — `INotableDateAlgorithmRegistry`, the plugin model, and
  `…Calendar.DependencyInjection` `IServiceCollection` extensions.
- **Source generator / builder** — `…Calendar.Builder` (rule XML/JSON → resource
  assemblies) and the regional **data-pack bundling** (Americas / AsiaPacific / Europe).
  Port once the v2 schema is stable; regenerate packs against the new shape.
- **Test infrastructure & conventions** — `Bodu.Test` KAT primitives (`IKat`, `ValidKat`,
  `BinaryKat`, `KatDisplayName`, `ExceptionAssert`/`AssertGuard`), the domain contract
  bases, MSTest tiers (`Smoke`/BVT/`Regression`/`Stress`), the member- and subject-based
  partial-file organization, the "Verifies that …" XML-doc convention, and
  `Assert.ThrowsExactly<T>` exception assertions.
- **Source conventions** — net8.0, nullable enabled, file-scoped namespaces, one public
  type per file with `.filenesting.json` partials, the licence-header banner, BCL-grade
  XML documentation on all members, expression-bodied member layout. Follow root
  `CLAUDE.md` exactly.

## 7. What to LEAVE BEHIND (the things v2 fixes)

- Display/canonical **name as identity** (lookup, merge, override, shadow keys).
- **Name-based territory shadowing** in the planner — replaced by explicit multi-rule
  concepts + territory filtering + explicit priority/collision policy.
- **Comma-delimited territory** strings — replaced by `<Territory code="…"/>` elements.
- **Rule-local-only adjustments** with ambiguous `when/action/days/target` — replaced by
  reusable, scoped `AdjustmentPolicy` (inline still allowed for simple cases).
- Implicit/under-specified **priority** and **observed-date** behaviour — replaced by
  `ResolutionPolicy` + explicit `Emission` modes.
- Silent failures (missing algorithm, ambiguous anchor) — replaced by validation
  diagnostics before resolution.

## 8. First-version scope & exit criteria (authoritative: test-strategy doc)

Implement only what T01–T13 require for the first cut (fixed-date, territory filtering,
identity, one `ObservedOnly` weekend policy, add/remove/patch overrides). Defer
algorithm/offset/nth-weekday/relative/multi-day/collision/non-Gregorian/custom-adjustment/
large-import features to Phase 2. The first version is structurally sound when **all**
exit criteria in `plan-calendar-v2-minimal-test-strategy.md` hold:

```text
The minimal cookbook loads and validates successfully.
The 5 fixed-date rules resolve correctly with correct territory filtering.
One notable-date id safely contains multiple rule ids (no collapse, no leak).
Observed-only adjustment behaviour is deterministic.
Range and single-day queries return consistent observed-date results.
Remove and patch overrides target only the intended rule identity.
```

## 9. Deliverables for the new session

1. An approved implementation plan (project layout, public surface, phasing) — Step 4.
2. New project(s) for the v2 library + a v2 test project, following repo conventions.
3. The revised **XSD** + **JSON schema** per `plan-calendar-v2-schema-design.md`.
4. The minimal cookbook fixture + the resolution engine slice that passes **T01–T13**.
5. Green build and tests (BVT; regression where data-driven). The existing calendar
   library and its tests remain green and untouched.

## 10. Constraints

- Develop on the designated session branch; do not push to `master` directly.
- Do not modify or break the existing `Bodu.Globalization.Calendar*` projects while
  building v2.
- Honour `CLAUDE.md` (conventions, resourced strings, validation, tests, file layout).
- Use plan mode for Step 4 and get approval before implementing Step 5.
