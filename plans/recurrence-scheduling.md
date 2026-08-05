# Implementation plan: the shared recurrence and scheduling requirements

**Status:** Proposed · **Source:** FallbackPlan requirements document
(`REC-F-*` / `REC-N-*`, dated 2026-08-05) · **Target:** `Bodu.Globalization.Recurrence`

This plan maps the FallbackPlan requirements statement onto the Bodu
repository as it stands today, decides where each capability lives, and
breaks the work into ordered, independently committable phases.

---

## 1. Verified current state

The requirements document's "current state" section (its §2) is accurate,
with two corrections in Bodu's favour:

| Requirement doc claim | Repository reality |
|---|---|
| `CronExpression`, `RecurrenceRule`, `RecurrenceSet` exist with the described surfaces | Confirmed. `Bodu.Globalization.Recurrence` ships all three, Core-only, `net8.0`, `IsAotCompatible`, resx-backed messages, RFC 5545 §3.8.5.3 conformance corpus. |
| "Not packaged for the committed feed" (its gap 4) | **Already packaged.** `local-packages/Bodu.Globalization.Recurrence.0.1.1.nupkg` exists; the lock-step release model in `bld/RELEASING.md` covers it. The remaining feed work is FallbackPlan-side consumption, not Bodu-side production. |
| API stability gate wanted (REC-N-007) | **Already present.** `PublicApiTests` verifies the assembly against `test/PublicApi/Bodu.Globalization.Recurrence.PublicApi.txt`. |

The real gaps, verified against the code and the committed public-API
baseline:

1. **No anchored-interval form** (REC-F-002). Nothing in the package can
   express "interval after a caller-supplied instant".
2. **`GetPreviousOccurrence` exists only on `CronExpression`**
   (REC-F-005). `RecurrenceRule` and `RecurrenceSet` have next-only.
3. **`RecurrenceSet` is the least complete surface**: no
   `DateTimeOffset` overloads at all, no `GetPreviousOccurrence`, no
   `ToString`/formatting (REC-F-009), and no value equality (REC-F-010) —
   `CronExpression` and `RecurrenceRule` have `Equals`/`GetHashCode`;
   `RecurrenceSet` does not.
4. **`TryParse` reports only `bool`** (REC-F-008). No shape lets a host
   surface *which* defect failed the parse without exception flow.
5. **The purity guarantee is emergent, not enforced** (REC-N-001). No
   test or analyzer bans wall-clock/timezone APIs; the guarantee holds
   today only by inspection.
6. **Offset semantics and DST posture are partially documented**
   (REC-N-002/003). `RecurrenceRule.GetOccurrences(DateTimeOffset)` has a
   good remarks block; the contract is not stated package-wide and the
   test matrix is effectively UTC/unspecified-only.
7. **`CLAUDE.md` and the docs guides don't know the package exists.** The
   project table and Key Types list omit `Bodu.Globalization.Recurrence`;
   `docs/guides/` has no recurrence guide.

Already satisfied and needing no code: REC-F-001 (cron + RRULE),
REC-F-003 (RDATE/EXDATE composition), REC-F-004 (no Calendar dependency —
occurrence streams are `IEnumerable`, so predicate filtering composes from
outside), REC-N-004 (Core-only closure), REC-N-005 (net8.0 + AOT),
REC-N-008 (conformance suites).

## 2. Placement decision

**Everything lands in the existing `Bodu.Globalization.Recurrence`
package and namespace, including the anchored-interval form.** No new
package, no new namespace. This resolves the requirements document's open
question 1 deliberately rather than by default:

- **The anchored interval is a recurrence form, not a scheduler.** Its
  occurrence series is `anchor + k·interval` — the degenerate,
  calendar-free recurrence. What would *not* fit this package (timers,
  pollers, due-state, job running) is exactly what §6 of the requirements
  rules out of scope for any package. There is therefore no residual
  "scheduling" domain left over that would justify a `Bodu.Scheduling`
  package — creating one for a single value type would be the force-fit.
- **REC-F-005 demands a uniform query surface** ("next and previous,
  everywhere"). Splitting one of the four forms into a sibling package
  fractures the very uniformity the requirement exists to guarantee, and
  puts FallbackPlan's *default* schedule shape (`every 4h`) in a
  different pinned identity from the rest.
- **One package is simpler to pin** (the requirements' own observation),
  and REC-N-004's "depends on `Bodu.Core` and nothing else" is already
  this package's dependency closure.

Two placements considered and rejected:

- *`Bodu.Core`* (beside `WeekPattern` / `DateTimeExtensions`): would
  separate the form from the shared conventions it must match (inclusive
  flags, offset semantics, defect-naming `TryParse`) and from the purity
  guard that must cover it.
- *A new `Bodu.Scheduling` package*: rejected per above — the name
  implies execution machinery the requirements explicitly exclude.

The `Globalization` segment is admittedly broader than a culture-free
interval needs, but the operative domain segment is `Recurrence`, the
package identity is already published at 0.1.1, and renaming a shipped
package is a larger breaking decision than any requirement here calls
for. This is extending an established domain, not force-fitting a new
one.

## 3. Design of the new type: `AnchoredInterval`

New sealed class `AnchoredInterval` in
`src/Globalization.Recurrence/AnchoredInterval.cs` (+ `.Parse.cs` /
`.Occurrences.cs` partials per the repo's partial-file convention).

- **Shape mirrors `RecurrenceRule`.** The type stores only the interval;
  the anchor is a per-query argument, exactly as `RecurrenceRule` takes
  `start` on every query. That is what makes REC-F-002's "the library
  never interprets the anchor" structural rather than aspirational.
- **A sealed class, not a struct**, for surface uniformity with
  `CronExpression`/`RecurrenceRule` (`Parse`/`TryParse`/`ToString`/
  `IEquatable<T>`/`IFormattable`) and to avoid the invalid
  `default(T)` (zero interval) a struct would admit.
- **Occurrence series: `anchor + k·interval` for `k ≥ 1`.** The anchor
  itself is *not* an occurrence — it models "the last completed run",
  so a run completed at `now` must not be immediately due under
  `lastCompleted < GetPreviousOccurrence(now, inclusive: true)`. This is
  documented as the contract and pinned by the acceptance KAT below.
- **Queries** (all O(1) arithmetic, no enumeration):
  - `GetNextOccurrence(DateTime anchor, DateTime after, bool inclusive = false)`
  - `GetPreviousOccurrence(DateTime anchor, DateTime before, bool inclusive = false)`
  - the `DateTimeOffset` pair of both, normalising *between the two
    arguments' offsets* per REC-N-002 (an anchor supplied in UTC compared
    against a `now` in +10:00 must compare instants, then return the
    occurrence carrying `after`'s offset)
  - `GetOccurrences(anchor)` / `GetOccurrences(anchor, from, to)` lazy
    enumeration, terminating at the representable calendar edge
    (overflow past `DateTime.MaxValue` ends the sequence; the point
    queries return `null` there).
- **Validation:** interval must be positive (`> TimeSpan.Zero`);
  `ThrowHelper`/`RecurrenceThrowHelper` guards with resx messages.
- **Text form: the RFC 5545 §3.3.6 DURATION grammar** (`PT4H`, `P1D`,
  `P1DT2H30M`, `P2W`), strict and invariant. Rationale: it ties the
  canonical form to the same defining document as the RRULE surface
  (REC-N-008's independent-verifiability posture), unlike `TimeSpan`'s
  constant format. Canonical rendering re-parses to an equal value
  (REC-F-009); negative and zero durations are parse defects.
- **Value semantics:** `IEquatable<AnchoredInterval>`, `==`/`!=`,
  `GetHashCode` over the interval (REC-F-010).

Acceptance KATs lifted verbatim from REC-F-002: a 4-hour interval with an
anchor at 08:00 has its next occurrence at 12:00 exactly; evaluations at
12:01 and at 20:00 (two missed occurrences) produce identical due-ness
via the previous-occurrence comparison — the count of missed occurrences
never appears in any answer.

## 4. Work phases

Each phase is one or more commits on the session branch; every phase
leaves the build green (`dotnet test bodu.slnx --settings bvt.runsettings`)
and the public-API baseline regenerated only in the phase that changes
the surface deliberately.

### Phase 0 — repository bookkeeping (no behaviour)

- Add `Bodu.Globalization.Recurrence` to the `CLAUDE.md` project table
  and Key Types section (it is currently absent).
- Confirm the packing metadata (`Description`, `PackageTags`) is staged
  for update in Phase 6 wording.

### Phase 1 — `AnchoredInterval` (REC-F-002, REC-F-008/009/010 for the new form)

- Implement the type per §3, including the defect-naming `TryParse`
  overload shape introduced package-wide in Phase 3 (land the shape here
  first so the new type never ships without it).
- New resx entries (`Format_Invalid_Duration*`,
  `Arg_OutOfRange_IntervalNotPositive`, …) per the key-prefix
  convention.
- Tests (`AnchoredIntervalTests.*` partials, member-named backbone):
  `Ctor`, `Parse`, `TryParse`, `ToString`, `Equality`,
  `GetNextOccurrence`, `GetPreviousOccurrence`, `GetOccurrences`, plus a
  `Conformance` partial holding the REC-F-002 acceptance KATs
  (`ValidKat<,>` rows where the shape fits, `InvalidKat<string>` for the
  malformed-duration sweep) and non-UTC/mixed-offset rows. One
  `[TestCategory("Smoke")]` happy-path test; duration-grammar sweeps
  tagged `Regression`.

### Phase 2 — previous-occurrence everywhere (REC-F-005)

- `RecurrenceRule.GetPreviousOccurrence(start, before, inclusive)` over
  `DateTime` and `DateTimeOffset`: scan `Enumerate(start)` retaining the
  last occurrence not past `before` — terminates because the stream is
  ascending and bounded by `before` (correctness first; Phase 7 owns
  speed).
- `RecurrenceSet`: `GetPreviousOccurrence` plus the missing
  `DateTimeOffset` overloads of `GetOccurrences` / `GetNextOccurrence` /
  `GetPreviousOccurrence`, using the same wall-clock-in-argument-offset
  convention as `CronExpression`'s offset overloads.
- Tests extend the existing member-file layout
  (`RecurrenceRuleTests.GetPreviousOccurrence.cs`, set equivalents);
  every new offset overload gets non-UTC rows from day one. The due-ness
  recipe (`lastCompleted < GetPreviousOccurrence(now, inclusive: true)`)
  is pinned as a conformance test on all four forms, including the
  coalescing property (five missed occurrences ⇒ the same boolean as
  one).

### Phase 3 — parsing, formatting, and equality completion (REC-F-008/009/010)

- **Defect-naming `TryParse`** on all four forms:
  `bool TryParse(string? s, out T result, out string? failureMessage)`
  (message sourced from resx, `CultureInfo.CurrentCulture`, naming the
  offending token/field — "unit must be …" beats "invalid format"). The
  existing bool-only overloads remain and delegate. `Parse` exception
  messages are audited to the same naming standard.
- **`RecurrenceSet` round-trip**: canonical `ToString` rendering of the
  iCalendar property block (`DTSTART`/`RRULE`/`RDATE`/`EXDATE`) that
  re-parses equal; `IFormattable` for parity with peers.
- **`RecurrenceSet` value equality**: `IEquatable<RecurrenceSet>` /
  `GetHashCode` over start, rules, dates, and exception dates
  (order-normalised, matching what `Parse` produces).
- Round-trip property tests: for every corpus row, `Parse → ToString →
  Parse` yields an equal value on all four forms.

### Phase 4 — purity, offset, and DST contracts (REC-N-001/002/003)

- **Purity guard as a test** (the requirements' open question 2 —
  resolved in favour of a repo test now; an analyzer can follow later
  without conflicting): a `PurityTests` class in the recurrence test
  project that walks the compiled assembly's member references via
  `System.Reflection.Metadata` (in-box, no new dependency) and fails on
  any reference to `DateTime.Now/UtcNow/Today`,
  `DateTimeOffset.Now/UtcNow`, `DateTime(Offset).ToLocalTime`,
  `DateTimeOffset.LocalDateTime`, `TimeZoneInfo.*`, `Stopwatch.*`, or
  `Environment.TickCount(64)`. Colocated with its sole consumer per the
  test-consolidation rule; promote the helper to `Bodu.Test` only when a
  second project adopts it.
- **REC-N-002 as documentation + tests**: a shared remarks contract on
  every `DateTimeOffset` overload (wall-clock interpreted in the
  argument's own offset; normalisation only *between* supplied
  arguments; result carries the query argument's offset), plus
  regression-tier sweeps where anchor/start and query offsets differ.
- **REC-N-003 DST posture**: documented in the new docs guide (Phase 6)
  and in package-level `<remarks>` — the library is offset-based; the
  twice-occurring and never-occurring local times on transition days are
  worked through explicitly with what the math yields.

### Phase 5 — bounded enumeration pinned (REC-F-006, REC-N-010)

- Verify and pin (tests, then docs) the termination bounds:
  a never-matching rule (`FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30`)
  enumerates empty and `GetNext`/`GetPrevious` return `null`, with the
  representable-calendar edge (year 9999 / `DateTime.MaxValue`) as the
  documented search horizon; confirm `CronExpression`'s existing horizon
  and document it identically. If any code path can scan unboundedly,
  fix it here (test-first, red-green).

### Phase 6 — docs, metadata, and packaging (REC-N-006/007, REC-F-004 guidance)

- New guide `docs/guides/recurrence/` covering: choosing a form; the
  due-ness recipe and coalescing; offset semantics and the DST posture
  (REC-N-002/003 text); composing with `Bodu.Globalization.Calendar`
  ("daily at 02:00, skip holidays") as a *consumer-side* filter example
  (REC-F-004); the bounded-search contract (REC-N-010).
- Update `docs/apidoc/Bodu.Globalization.Recurrence.md`, the package
  matrix, the csproj `Description`/`PackageTags` (anchored intervals,
  previous-occurrence symmetry), and the ROADMAP entry (move
  previous-occurrence off the deferred list; note the new form).
- **Regenerate the public-API baseline once, deliberately**, reviewing
  the diff against this plan — that diff is the artifact REC-N-007
  promises consumers.
- Pack per `bld/RELEASING.md` at the next lock-step `BoduBaseVersion`
  bump and drop the nupkg into `local-packages/` alongside 0.1.1. New
  API, no breaks ⇒ a minor bump (0.2.0) satisfies the requirements'
  pre-1.0 posture; FallbackPlan reads the baseline diff and pins.

### Phase 7 — steady-state performance (REC-N-009) *(optional, recommended)*

- `RecurrenceRule` point queries currently enumerate from `start`; for
  an old `DTSTART` that is O(periods since start). Add a period
  fast-forward (compute the approximate period index containing the
  query instant, then enumerate locally) for DAILY/WEEKLY/MONTHLY/
  YEARLY. Behaviour-preserving: the existing conformance corpus is the
  oracle; add regression rows with decade-old anchors.
- This phase is separable and must not block FallbackPlan adoption —
  correctness lands in Phases 1–6.

## 5. Traceability

| Requirement | Disposition |
|---|---|
| REC-F-001 | Already satisfied — no action |
| REC-F-002 | Phase 1 (`AnchoredInterval`) |
| REC-F-003 | Already satisfied — anchored intervals excluded from set composition in v1, per the requirement |
| REC-F-004 | Already satisfied structurally; Phase 6 guide example |
| REC-F-005 | Phase 2 (+ Phase 1 for the new form) |
| REC-F-006 | Phase 5 (pin + document) |
| REC-F-007 | Satisfied by design — no state APIs are added anywhere in this plan |
| REC-F-008 | Phase 3 (+ Phase 1) |
| REC-F-009 | Phase 3 (`RecurrenceSet.ToString`; new-form canonical duration) |
| REC-F-010 | Phase 3 (`RecurrenceSet` equality; new form ships equatable) |
| REC-N-001 | Phase 4 (banned-API metadata test) |
| REC-N-002 | Phase 4 (contract remarks + non-UTC test matrix) |
| REC-N-003 | Phases 4/6 (documentation) |
| REC-N-004 | Already satisfied; the purity/packaging phases add no dependency |
| REC-N-005 | Already satisfied (`net8.0`, `IsAotCompatible`) |
| REC-N-006 | Already produced at 0.1.1; Phase 6 re-packs the new version |
| REC-N-007 | Already in place; Phase 6 regenerates the baseline deliberately |
| REC-N-008 | Already satisfied; Phases 1/2 extend the conformance suites |
| REC-N-009 | Phase 7 (fast-forward), new form is O(1) by construction |
| REC-N-010 | Phase 5 |

## 6. Out of scope (unchanged from the requirements' §6)

Timers/pollers/job runners, timezone resolution, schedule-text
localisation, calendar data, sub-daily RRULE enumeration (parse-only
today; not required by REC-F-001), and Quartz cron extensions
(`L`/`W`/`#`/`?`) — the latter two remain on the ROADMAP's deferred
list.

## 7. Decisions taken in this plan (previously open)

1. **Anchored-interval home** → third top-level type in
   `Bodu.Globalization.Recurrence` (§2).
2. **Purity guard mechanism** → repository test now (Phase 4); an
   analyzer remains a compatible follow-on, potentially in
   `Bodu.CodeStyle`.
3. **Anchor-is-not-an-occurrence** (`k ≥ 1`) → documented contract (§3).
4. **Canonical interval text** → RFC 5545 §3.3.6 DURATION subset (§3).
5. **Defect-message shape** → `TryParse(s, out result, out string?
   failureMessage)` overloads beside the existing bool-only shape (§4,
   Phase 3).
