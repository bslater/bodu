# Implementation plan: the shared recurrence and scheduling requirements

**Status:** Implemented: every capability in §4's Phases 0-8 is in the codebase, and the
package is in the release manifest (first shipped in the 0.6.0 wave). Two phases landed for
1.3.0: Phase 7, the steady-state speed-up, begins the point queries and windows of
`RecurrenceRule` and `RecurrenceSet` at the frequency period that holds the query instead of at
the series start, and Phase 8, beyond the requirements, enumerates the sub-daily frequencies
(outcomes in §4) · **Source:** FallbackPlan requirements document
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
   `ToString`/formatting (REC-F-009), and no value equality (REC-F-010) -
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
REC-F-003 (RDATE/EXDATE composition), REC-F-004 (no Calendar dependency -
occurrence streams are `IEnumerable`, so predicate filtering composes from
outside), REC-N-004 (Core-only closure), REC-N-005 (net8.0 + AOT),
REC-N-008 (conformance suites).

## 2. Placement decision

**Everything lands in the existing `Bodu.Globalization.Recurrence`
package and namespace, including the anchored-interval form.** No new
package, no new namespace. This resolves the requirements document's open
question 1 deliberately rather than by default:

- **The anchored interval is a recurrence form, not a scheduler.** Its
  occurrence series is `anchor + k·interval` - the degenerate,
  calendar-free recurrence. What would *not* fit this package (timers,
  pollers, due-state, job running) is exactly what §6 of the requirements
  rules out of scope for any package. There is therefore no residual
  "scheduling" domain left over that would justify a `Bodu.Scheduling`
  package - creating one for a single value type would be the force-fit.
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
- *A new `Bodu.Scheduling` package*: rejected per above - the name
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
  itself is *not* an occurrence - it models "the last completed run",
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
via the previous-occurrence comparison - the count of missed occurrences
never appears in any answer.

## 4. Work phases

Each phase is one or more commits on the session branch; every phase
leaves the build green (`dotnet test bodu.slnx --settings bvt.runsettings`)
and the public-API baseline regenerated only in the phase that changes
the surface deliberately.

### Phase 0 - repository bookkeeping (no behaviour)

- Add `Bodu.Globalization.Recurrence` to the `CLAUDE.md` project table
  and Key Types section (it is currently absent).
- Confirm the packing metadata (`Description`, `PackageTags`) is staged
  for update in Phase 6 wording.

### Phase 1 - `AnchoredInterval` (REC-F-002, REC-F-008/009/010 for the new form)

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

### Phase 2 - previous-occurrence everywhere (REC-F-005)

- `RecurrenceRule.GetPreviousOccurrence(start, before, inclusive)` over
  `DateTime` and `DateTimeOffset`: scan `Enumerate(start)` retaining the
  last occurrence not past `before` - terminates because the stream is
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

### Phase 3 - parsing, formatting, and equality completion (REC-F-008/009/010)

- **Defect-naming `TryParse`** on all four forms:
  `bool TryParse(string? s, out T result, out string? failureMessage)`
  (message sourced from resx, `CultureInfo.CurrentCulture`, naming the
  offending token/field - "unit must be …" beats "invalid format"). The
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

### Phase 4 - purity, offset, and DST contracts (REC-N-001/002/003)

- **Purity guard as a test** (the requirements' open question 2 -
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
  and in package-level `<remarks>` - the library is offset-based; the
  twice-occurring and never-occurring local times on transition days are
  worked through explicitly with what the math yields.

### Phase 5 - bounded enumeration pinned (REC-F-006, REC-N-010)

- Verify and pin (tests, then docs) the termination bounds:
  a never-matching rule (`FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30`)
  enumerates empty and `GetNext`/`GetPrevious` return `null`, with the
  representable-calendar edge (year 9999 / `DateTime.MaxValue`) as the
  documented search horizon; confirm `CronExpression`'s existing horizon
  and document it identically. If any code path can scan unboundedly,
  fix it here (test-first, red-green).

### Phase 6 - docs, metadata, and packaging (REC-N-006/007, REC-F-004 guidance)

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
  the diff against this plan - that diff is the artifact REC-N-007
  promises consumers.
- Pack per `bld/RELEASING.md` at the next lock-step `BoduBaseVersion`
  bump and drop the nupkg into `local-packages/` alongside 0.1.1. New
  API, no breaks ⇒ a minor bump (0.2.0) satisfies the requirements'
  pre-1.0 posture; FallbackPlan reads the baseline diff and pins.

### Phase 7 - steady-state performance (REC-N-009) *(done, for 1.3.0)*

- `RecurrenceRule` point queries currently enumerate from `start`; for
  an old `DTSTART` that is O(periods since start). Add a period
  fast-forward (compute the approximate period index containing the
  query instant, then enumerate locally) for DAILY/WEEKLY/MONTHLY/
  YEARLY. Behaviour-preserving: the existing conformance corpus is the
  oracle; add regression rows with decade-old anchors.
- This phase is separable and must not block FallbackPlan adoption -
  correctness lands in Phases 1-6.

**Outcome.**

- **`RecurrenceRule`.** Period p's anchor is a function of p alone: the
  start plus p·k days, weeks, months or years. `FirstPeriodReaching` and
  `FirstPeriodBeyond` bound, from the dates alone, the periods that can
  hold an occurrence on or after a date and those that hold only
  occurrences after it, each widened by a period for the days an
  expansion places outside its own period (a week's days either side of
  its anchor, `BYWEEKNO` days across the turn of a year).
  `GetNextOccurrence` and the windowed `GetOccurrences` enumerate from
  the first period that can reach the bound. `GetPreviousOccurrence`
  enumerates the period below `FirstPeriodBeyond`, then the two below
  those, then four, and stops at the first run that holds an answer, so
  it scans at most twice the periods between the answer and the bound; a
  bound past `UNTIL` searches back from `UNTIL`. The `DateTimeOffset`
  overloads convert the query into the start's wall clock, clamped to
  the calendar, before choosing the period. A rule with `COUNT` is still
  enumerated from the start, because the occurrences before a period
  decide how many it may still produce.
- **`RecurrenceSet`.** The next and windowed queries merge each rule
  from the bound, through the internal `RecurrenceRule.EnumerateFrom`,
  and the explicit dates from the first at or after it; the merge and
  its de-duplication are unchanged. `GetPreviousOccurrence` no longer
  merges: the set is the union of its sources less its exception dates,
  so the answer is the latest of each rule's own previous occurrence and
  the latest explicit date, each skipping the exception dates. An
  internal `RecurrenceRule.GetPreviousOccurrence` overload takes the
  exclusion, so a run of excluded occurrences is passed over as empty
  periods are.
- **Tests.** `RecurrenceRuleTests.StreamAgreement` (37 rules: every
  frequency, intervals, `BYSETPOS`, `BYWEEKNO` weeks straddling a year,
  sparse and never-matching rules, `UNTIL` and `COUNT`, a start that is
  not an occurrence, every `DateTimeKind`, the end of the calendar, and
  `DateTimeOffset` starts and queries in other offsets) and
  `RecurrenceSetTests.StreamAgreement` (15 sets: overlapping rules,
  `COUNT` and `UNTIL` rules beside unbounded ones, explicit dates before
  the start, duplicated and equal to rule occurrences, exception runs up
  to a whole year, explicit dates alone, UTC, the end of the calendar)
  hold every query, at probes across decades, to the stream enumerated
  from the start (Regression). BVT rows in the member files pin
  decade-old anchors. Mutations of the period bounds, the backward runs,
  the `UNTIL` clamp, the set's bounds and exclusions, and the date
  search each fail tests; the one that survives, `UNTIL` clamped a day
  early, is absorbed by the period of margin by design.

Median microseconds per call, timed in one process per runtime on a
4-vCPU Intel Xeon at 2.10 GHz, before (master at `867d5c1fe`) and
after. The answers are the same.

| Query | .NET 8 before | .NET 8 after | .NET 10 before | .NET 10 after |
|---|---:|---:|---:|---:|
| Rule `FREQ=DAILY` since 1990, next | 1,064.74 | 0.42 | 924.10 | 0.49 |
| Same, previous | 967.84 | 0.58 | 950.39 | 0.40 |
| Same, a month's window | 996.04 | 2.58 | 948.77 | 2.18 |
| Rule `FREQ=DAILY` since January 2026, next | 20.32 | 0.41 | 19.11 | 0.27 |
| Rule weekly `MO,WE,FR` since 1993, next | 316.20 | 0.88 | 288.27 | 0.65 |
| Same, previous | 313.80 | 0.98 | 296.99 | 0.82 |
| Rule monthly last weekday since 1975, next | 933.06 | 3.12 | 938.23 | 2.74 |
| Same, previous | 958.48 | 4.85 | 901.75 | 4.34 |
| Rule yearly Thanksgiving since 1950, next | 18.18 | 0.86 | 15.59 | 0.74 |
| Same, previous | 17.28 | 1.80 | 16.17 | 1.63 |
| Rule `FREQ=DAILY;COUNT=20000` since 1990, next (from the start) | 1,087.83 | 1,094.51 | 980.04 | 988.76 |
| Set: daily since 1990 with 17 exception dates, next | 1,269.08 | 2.09 | 1,136.72 | 1.57 |
| Same, previous | 1,373.46 | 4.06 | 1,217.66 | 3.06 |
| Same, a month's window | 1,400.69 | 4.30 | 1,201.48 | 3.53 |
| Set: two weekly rules since 1993, next | 566.74 | 2.14 | 497.95 | 1.66 |
| Same, previous | 614.08 | 1.90 | 498.76 | 1.51 |
| Set: monthly since 1975 and three explicit dates, next | 984.92 | 4.60 | 800.46 | 3.99 |
| Same, previous | 958.06 | 4.60 | 802.82 | 4.49 |
| Set: `COUNT=500` daily and a yearly rule, previous | 52.80 | 41.69 | 46.25 | 38.86 |
| Set: `COUNT=20000` daily and a date, previous | 1,403.94 | 1,080.17 | 1,057.19 | 999.54 |

The `COUNT=20000` rule's row is the median of eight interleaved runs of
each build, since one run apiece on this machine varies by about 10%;
its path is unchanged, and the two builds agree to within 1%. The sets
with `COUNT` rules gain a little because the previous occurrence no
longer passes through the merge's priority queue.

### Phase 8 - sub-daily enumeration *(done, for 1.3.0; beyond the requirements)*

§6 left sub-daily `RRULE` enumeration out of scope: `HOURLY`, `MINUTELY`
and `SECONDLY` rules parsed and round-tripped, and every occurrence
query on them threw `NotSupportedException`. REC-F-001 does not need
them, but the ROADMAP listed them as the first deferred follow-on, and
they complete the RFC 5545 frequency scale.

**Outcome.**

- **The period model.** A sub-daily period is one hour, minute or
  second, and period n begins n × `INTERVAL` units after the unit that
  holds the start, so each period lies within one day and every period
  that passes the limits produces the same offsets. `BYMINUTE` and
  `BYSECOND` expand an hourly period and `BYSECOND` a minutely one,
  defaulting to the start's own minute and second; `BYHOUR`, the finer
  parts the frequency does not expand, `BYMONTH`, `BYMONTHDAY`,
  `BYYEARDAY` and the `BYDAY` weekdays limit; `BYSETPOS` selects within
  the period. Where RFC 5545 leaves room the engine follows the daily
  rule's choices: a `BYDAY` ordinal is ignored below `MONTHLY`,
  `BYWEEKNO` is ignored below `YEARLY`, and `BYSECOND=60` stands for
  second 59.
- **The walk** (`RecurrenceRule.SubDaily.cs`). A limit holds for a whole
  period, so the forward walk moves from a period a date limit rejects
  to the first period of the next day, or of the next month when
  `BYMONTH` rejects it, and from a period whose time of day the rule
  does not allow straight to the next one it does. Moving j periods
  changes the time of day by j·`INTERVAL` modulo a day, so reaching an
  allowed time d units away is the congruence j·`INTERVAL` ≡ d, solved
  with the greatest common divisor and a modular inverse over a per-rule
  bitmap of the allowed times of day, built once and cached on the
  rule. A rule whose interval never reaches an allowed time of day ends
  at once. The backward walk mirrors the forward one from the period
  past the bound, so `GetPreviousOccurrence` costs the distance back to
  the answer; `GetNextOccurrence` and the windows start a period below
  the one that holds the bound. As for the coarser frequencies, a rule
  with `COUNT` is enumerated from its start.
- **Tests.** `RecurrenceRuleTests.SubDaily`: the RFC's three sub-daily
  examples and its 20-minute example against the equivalent daily rule,
  each expansion and limit against python-dateutil's answers, the
  never-reachable and empty-`BYSETPOS` rules, the end of the calendar,
  intervals that span millennia, the leap second, kinds and offsets, and
  rules allowing a single time of day queried from every half hour of
  the day (BVT); and, in the Regression tier, 120 seeded random rules held,
  through the open stream and the next and previous occurrence at
  probes on, around and between occurrences, to `SubDailyReference`, a
  literal period-by-period reading of the RFC kept in the tests.
  `RecurrenceCorpusTests.Dateutil` holds 200 generated rules, their
  streams and the next and previous occurrence at every recorded
  instant, to python-dateutil 2.9.0.post0 (`corpus/recurrence/dateutil/`);
  libical's four sub-daily counts join its reconciliation; and the
  stream-agreement suites gain thirteen sub-daily rules, anchored as far
  back as 1970 or at the end of the calendar, and two sets.
- **Mutation checks.** Each mutation was applied alone to
  `RecurrenceRule.SubDaily.cs` and the rule, set and corpus tests run on
  net10.0; the counts are the tests that failed.

| Mutation | Failing tests |
|---|---:|
| Next and windows start a period later | 225 |
| The backward walk starts a period early | 216 |
| A rejected day resumes a day late | 157 |
| A rejected month resumes a month late | 62 |
| Back over a rejected day, a day too far | 42 |
| Back over a rejected month, a month too far | 17 |
| The forward time-of-day jump one period long | run did not finish |
| The backward time-of-day jump one period long | 101 |
| Jump to the first reachable time rather than the nearest | 142 |
| The bitmap scan skips past the end of the day | 12 |
| The backward bitmap scan skips a word's last time | 7 |
| The bitmap ignores `BYHOUR` | 312 |
| The bitmap reads `BYSECOND=60` as a second 60 | 4 |
| `BYSETPOS` ignored | 72 |
| Occurrences before the start kept | 50 |
| The backward walk ignores `COUNT` | 54 |
| The hourly expansion ignores `BYMINUTE` | 105 |
| The forward date limit honours `BYDAY` ordinals | 25 |
| The backward date limit ignores `BYYEARDAY` | 14 |
| The backward walk ignores a set's exception dates | 3 |
| The backward walk without the `UNTIL` clamp | run did not finish |
| The minutely limit ignores `BYMINUTE` | 116 |

  The two runs that did not finish were stopped after several minutes:
  an overshooting jump never lands on an allowed time again and walks on
  toward year 9999, and so does a query far past `UNTIL` once nothing
  moves its start back to `UNTIL`, which is therefore needed for
  correctness in practice, not only for speed. One mutation survives by
  design: starting the backward walk at the period that holds the bound
  rather than one past it, since the extra period is a margin. The
  backward bitmap scan and the set's exclusion were first caught by only
  two tests each; the one-time-of-day queries in the member partials and
  a set with an excluded run of quarter hours were added for them.

Median microseconds per call, timed in one process per runtime on a
4-vCPU Intel Xeon at 2.10 GHz; the queries are at 12:00:07 on 4 October
2026 unless stated. There is no "before": these rules threw.

| Query | .NET 8 | .NET 10 |
|---|---:|---:|
| `FREQ=MINUTELY;INTERVAL=15` since 1990, next | 0.26 | 0.18 |
| Same, previous | 0.21 | 0.14 |
| Same, a day's window (96 occurrences) | 1.81 | 2.38 |
| `FREQ=HOURLY;INTERVAL=5;BYHOUR=0,12` since 1985, next | 0.32 | 0.23 |
| Same, previous | 0.34 | 0.22 |
| Every 20 minutes, 09:00-16:40 on weekdays since 2000, next from Saturday noon | 0.49 | 0.36 |
| Same, previous from Monday 08:00 | 0.65 | 0.58 |
| `FREQ=HOURLY;BYMONTHDAY=1` since 1990, next | 1.22 | 1.01 |
| Same, previous | 0.34 | 0.24 |
| `FREQ=SECONDLY;INTERVAL=7;BYSECOND=0` since 2000, next | 0.73 | 0.66 |
| Same, previous | 0.86 | 0.80 |
| `FREQ=SECONDLY;BYHOUR=9;BYMINUTE=30;BYSECOND=0` (one second a day) since 2000, next | 9.84 | 9.98 |
| Same, previous | 8.97 | 9.35 |
| `FREQ=SECONDLY;INTERVAL=7` from 2000 until mid-2010, previous | 0.12 | 0.07 |
| `FREQ=SECONDLY`, the first 10,000 occurrences | 119.27 | 119.13 |
| One second a day, the first 1,000 occurrences | 9,017.66 | 9,752.62 |
| `FREQ=MINUTELY;COUNT=20000` from 25 September 2026, next (from the start) | 181.49 | 174.79 |
| Set: quarter hours since 1990 with two exception dates, next | 0.49 | 0.35 |
| Same, previous | 0.28 | 0.18 |

**The bitmap summary.** As first landed, a sparse `SECONDLY` rule spent
its query scanning the empty words of the day's time-of-day bitmap:
about 10 µs for one second a day, against well under one where the
allowed times lie close together. The bitmap is now `TimeOfDayBitmap`,
which keeps a summary, one bit per word, set when the word allows any
time, so a search passes over sixty-four empty words at a time and,
within a word, goes straight to the nearest allowed time by counting
zero bits. The time-of-day jump visits the allowed times in order of
distance and passes over those the interval cannot reach a multiple of
the greatest common divisor at a time, rather than one allowed time at a
time. `TimeOfDayBitmapTests` hold the search, forward and back, from
every start at every minimum distance over hourly and minutely days, and
over seeded days of every sub-daily length from one allowed time in
fifty thousand to all but one in a thousand, to a linear scan.

Median microseconds per call before and after, the two builds run
interleaved in one process per runtime on the same machine, the
queries as in the table above unless stated:

| Query | .NET 8 before | .NET 8 after | .NET 10 before | .NET 10 after |
|---|---:|---:|---:|---:|
| One second a day since 2000, next | 11.34 | 0.21 | 10.95 | 0.15 |
| Same, previous | 10.84 | 0.20 | 10.34 | 0.14 |
| Same, the first 1,000 occurrences | 10,957.22 | 74.62 | 11,017.80 | 72.00 |
| `FREQ=SECONDLY;INTERVAL=7;BYHOUR=9` since 2000, next | 9.63 | 0.32 | 9.59 | 0.27 |
| Same, previous | 1.13 | 0.27 | 1.09 | 0.21 |
| `FREQ=SECONDLY;INTERVAL=7;BYSECOND=0` since 2000, next | 0.85 | 0.28 | 0.69 | 0.23 |
| Same, previous | 1.06 | 0.28 | 0.97 | 0.23 |
| Same from 2026, the first 1,000 occurrences | 1,227.90 | 236.96 | 1,139.82 | 234.69 |
| `FREQ=MINUTELY;INTERVAL=13;BYMINUTE=0` since 2000, next | 1.68 | 0.49 | 1.46 | 0.41 |
| Every 20 minutes, 09:00-16:40 on weekdays, next from Saturday noon | 0.45 | 0.34 | 0.35 | 0.24 |
| Same, previous from Monday 08:00 | 0.70 | 0.46 | 0.61 | 0.33 |
| `FREQ=SECONDLY`, the first 10,000 occurrences | 145.60 | 149.69 | 131.42 | 144.49 |
| `FREQ=MINUTELY;INTERVAL=15` since 1990, a day's window | 2.17 | 2.09 | 1.73 | 1.86 |

The last two rows allow every time of day, so the summary plays no part
in them; four further interleaved runs of each on .NET 10 put both
builds between 131 and 154 µs and between 2.04 and 2.96 µs, the spread
of the machine rather than a difference. The first version of the
summary searched for the nearest allowed time and then for the next one
after it, one allowed time at a time, and lost ground on the dense
weekday row (0.73 to 1.14 µs on .NET 8); passing over the unreachable
times a multiple of the divisor at a time recovered it.

Each mutation was applied alone, to `TimeOfDayBitmap.cs` with the bitmap,
sub-daily and dateutil tests run, and to the jump's search loop in
`RecurrenceRule.SubDaily.cs` with the sub-daily, dateutil and
stream-agreement tests run, on net10.0:

| Mutation | Failing tests |
|---|---:|
| The summary search skips a word | 141 |
| The forward search never wraps past midnight | 176 |
| The backward search never wraps past midnight | 62 |
| `Add` marks the wrong summary bit | 199 |
| The backward word mask one time short | run did not finish |
| The search resumes a multiple of the divisor too far | 54 |
| The next solution is sought a multiple of the divisor too far | 56 |
| The search starts past the first multiple of the divisor | 138 |

  Two mutations survive because they are equivalent: letting either
  stretch's search include the time it is measured from finds a distance
  of zero, which already reads as no allowed time.

## 5. Traceability

| Requirement | Disposition |
|---|---|
| REC-F-001 | Already satisfied - no action |
| REC-F-002 | Phase 1 (`AnchoredInterval`) |
| REC-F-003 | Already satisfied - anchored intervals excluded from set composition in v1, per the requirement |
| REC-F-004 | Already satisfied structurally; Phase 6 guide example |
| REC-F-005 | Phase 2 (+ Phase 1 for the new form) |
| REC-F-006 | Phase 5 (pin + document) |
| REC-F-007 | Satisfied by design - no state APIs are added anywhere in this plan |
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
| REC-N-009 | Phase 7 (fast-forward, done for 1.3.0); new form is O(1) by construction |
| REC-N-010 | Phase 5 |

## 6. Out of scope (unchanged from the requirements' §6)

Timers/pollers/job runners, timezone resolution, schedule-text
localisation, calendar data, and Quartz cron extensions (`L`/`W`/`#`/`?`),
which remain on the ROADMAP's deferred list. Sub-daily RRULE enumeration,
listed here originally as parse-only and not required by REC-F-001,
landed for 1.3.0 as Phase 8.

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
