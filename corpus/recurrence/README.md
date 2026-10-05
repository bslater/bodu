# Bodu recurrence validation corpus

External evidence used to validate `Bodu.Globalization.Recurrence`, kept in the repository so
every reconciliation is reproducible from committed artifacts. The reconciliation tests that
consume these tables live with the recurrence tests
(`Bodu.Globalization.Recurrence/test/Fixtures/Vectors/`, copied from here at build time); this
tree holds the *sources they are reconciled against* and the tooling that derives them.

## There is no single official recurrence corpus

RFC 5545 is the only normative authority for recurrence rules, and it publishes its worked
examples as **prose** rather than as data. Everything else in this space is an *implementation*
test suite. The two are not interchangeable, and the corpus records them under different source
classes accordingly:

| Directory | Source class | What it is |
|---|---|---|
| `rfc5545/` | `official-published` | The standard's own §3.8.5.3 examples, transcribed from the normative text |
| `libical/` | `third-party-comparison` | The reference C implementation's test corpus - evidence, not authority |
| `dateutil/` | `third-party-comparison` | python-dateutil's output for generated sub-daily rules - evidence, not authority |
| `cronos/` | `third-party-comparison` | A widely used .NET cron implementation's test suite - evidence, not authority |

Cron is worse off than recurrence rules here. There is no cron RFC at all: the closest thing to a
specification is the `crontab(5)` man page shipped with Vixie cron and its cronie descendant, and
the authority of last resort is `entry.c` itself. Where Cronos and cronie disagree, this corpus
follows cronie and records the disagreement.

## Lineage warning (no majority voting)

The same rule that governs the calendar corpus applies here, and it bites harder than it looks.
There are only **two** independent model families among the widely-cited recurrence
implementations: the libical line (libical, ical.js) and the python-dateutil line (dateutil,
rrule.js, which is an explicit port). Agreement between dateutil and rrule.js is therefore *not*
independent confirmation. Bodu's engine is a third, written from the RFC.

This is not hypothetical. The cross-library review that preceded this corpus found cases where
**python-dateutil is wrong and Bodu is right** - most notably `BYSETPOS` truncating the first
weekly period to the part on or after `DTSTART` instead of indexing the whole period
([libical#795](https://github.com/libical/libical/issues/795),
[dateutil#1398](https://github.com/dateutil/dateutil/issues/1398)). A loader that treated any one
implementation's output as ground truth would have regressed that. Disagreements are classified,
never silently resolved.

## `rfc5545/` - the normative examples

- `rfc5545-recurrence-examples.csv` - 39 examples: description, `DTSTART`, `RRULE`, flags, and the
  expected occurrence dates.
- `extract-rfc5545-examples.py` - the extractor, committed so the table is reproducible from the
  RFC rather than hand-maintained.

The RFC writes expected occurrences as prose: `(1997 9:00 AM EDT) September 2-11`, with month
groups, day ranges, comma lists, and DST annotations. `expectedDates` is that notation expanded.
Because the transcription is the risky step, the extractor **self-checks** it: every rule bounded
by `COUNT=n` must expand to exactly *n* dates, and every expanded date must be a real calendar
date. The committed table was generated with **0 self-check problems across all 39 examples**.

Regenerate with:

```shell
python3 corpus/recurrence/rfc5545/extract-rfc5545-examples.py <rfc5545.html>
```

## `libical/` - the reference-implementation corpus

`libical-recur-expectations.csv` - 57 rules derived from libical's `test-data/recur.txt`. libical
asserts occurrence **counts** (`X-EXPECT-NUMEVENTS`), not date lists. Counts are a weaker oracle
than dates but catch exactly the two failure modes that matter most: emitting an occurrence twice,
and dropping one. Both were real Bodu defects found and fixed immediately before this corpus
landed.

**The upstream file is not committed verbatim.** libical is dual MPL-2.0/LGPL-2.1; MPL-2.0 is
file-level copyleft, so committing `recur.txt` into this MIT repository would carry that licence
into the tree. Following the corpus policy of not committing third-party sources until
redistribution rights are confirmed, this directory holds a **derived** table plus the upstream
SHA-256, the same link-and-hash pattern used for the IMD and SGPC material. If the maintainer
decides the MPL file may be vendored, the derived table can be replaced by the original.

## `dateutil/` - the sub-daily comparison table

`dateutil-subdaily-vectors.csv` - 200 rules at the `HOURLY`, `MINUTELY` and `SECONDLY` frequencies,
each with a `DTSTART` and up to the first 30 occurrences python-dateutil 2.9.0.post0 enumerates for
it. Neither RFC 5545 nor libical says much below a day: the RFC has three sub-daily examples and
libical asserts four counts. So this table is generated rather than transcribed: a seeded script
composes the rules - intervals that do and do not divide a day, every limit and expansion,
`BYSETPOS`, ordinal `BYDAY` values, `COUNT` and `UNTIL` - and records what dateutil produces.

- `generate-dateutil-subdaily-vectors.py` - the generator, committed so the table is reproducible.
  Seed 5545; a rule dateutil does not finish within two seconds is discarded and counted in the
  header (one was).
- `truncated` marks a rule that continues past the 30 recorded occurrences; any other row records
  its whole stream.
- `empty-set` marks a rule dateutil rejects because its interval never reaches a `BY` value at its
  own level (`FREQ=MINUTELY;INTERVAL=15;BYMINUTE=35` from minute 33, say). Its expected stream is
  empty, and Bodu, which treats such a rule as one with no occurrences, is held to that.

The rows are dateutil's output for rules the generator composed; dateutil itself (Apache-2.0 /
BSD-3-Clause) is not redistributed. Being one implementation's output, the table is evidence from
the dateutil line only, and the generator stays inside the part of the grammar where Bodu and
dateutil read RFC 5545 alike: it emits no `BYWEEKNO`, which RFC 5545 allows only with `YEARLY` and
which dateutil applies at every frequency while Bodu ignores it below `YEARLY`, and no `BYSECOND=60`.

Regenerate with:

```shell
python3 corpus/recurrence/dateutil/generate-dateutil-subdaily-vectors.py
```

`RecurrenceCorpusTests.Dateutil` holds the open-ended stream to every row, and holds the next and
previous occurrence, queried on, between, and a tick either side of the recorded instants, to the
table. Two further oracles stand beside it in the test project: `SubDailyReference`, a literal
reading of the RFC that visits every period, against which `RecurrenceRuleTests.SubDaily` checks 120
seeded rules; and the RFC's own sub-daily examples, which list times of day the date-only RFC table
does not transcribe, pinned there as well.

## `cronos/` - the cron vector table

`cronos-cron-vectors.csv` - 1,354 rows derived from Cronos's `CronExpressionFacts`, the densest
public collection of cron vectors we could find. Unlike the two recurrence tables it asserts several
different things, recorded in a `kind` column: `next` (the next occurrence, inclusive),
`unreachable` (an expression that can never fire), `invalid` (must be rejected), `equal` /
`notEqual` (two spellings of the same or different schedules), and `toString` (canonical text).

Cronos is MIT (Copyright (c) 2017 Hangfire OÜ), so deriving this table and redistributing it with
attribution is permitted; the upstream file is still not committed, and the header records its
SHA-256 so the derivation is reproducible.

### Where Cronos and Bodu deliberately differ

Cronos implements *Quartz*-flavoured cron. Bodu implements *Vixie*. That is a real difference of
dialect, not a defect on either side, and it accounts for most of the excluded rows. Each divergence
below was established by running the rows rather than assumed:

| Divergence | Cronos | Bodu (Vixie) |
|---|---|---|
| `L`, `W`, `#`, `?` tokens | Accepted | Rejected - a planned follow-on, not silently ignored |
| Reversed ranges (`55-5`, `FRI-TUE`) | Wrap around the field | Rejected |
| Both day fields restricted | Intersection | **Union**, per `entry.c`'s `DOM_STAR`/`DOW_STAR` flags |
| Step wider than its range (`*/60`) | Rejected | Accepted, selecting the range start - cronie only *warns* |
| `@every_second`, `@every_minute` | Accepted | Rejected - not in `crontab(5)`'s macro set |
| `ToString` of a zero seconds field | Elided | Rendered, like every other field of the declared format |

The DOM/DOW rule is the one worth dwelling on, because it is the divergence most likely to be read
as a Bodu bug. Vixie decides whether a day field is "restricted" from its **leading character**, so
`*/2` is unrestricted (leading `*`) while the set-equivalent `1-31/2` is restricted - and the two
therefore select different days when a day-of-week field is also present. That is not a rationalizable
rule; it is what `src/entry.c` does.

There is no table for it here, because the one implementation that models both readings - croniter,
whose `implement_cron_bug` flag switches between them - **defaults to the reading cronie does not
have**, so a bulk derivation from croniter would disagree with us on exactly the rows we would want
it for. Its `test_dom_dow_vixie_cron_bug` is used directly instead: both of its four-occurrence
sequences are asserted verbatim in `CronExpressionTests`, the intersection against
`implement_cron_bug=True` and the union against croniter's default. Sequences rather than single
points, since a first occurrence can agree by accident where the stride does not.

The oversized-step case is the only divergence where Bodu accepts what Cronos rejects, so it is
flagged on the rejection rows too. cronie prints `Warning: Step size %i higher than possible maximum
of %i` and then runs `for (i = low; i <= high; i += step)`, which sets exactly one bit. Bodu matches
that, pinned by `CronExpressionTests.Parse_WhenStepExceedsItsRange_ShouldSelectOnlyTheRangeStart`
rather than left to the corpus to imply.

Regenerate with:

```shell
python3 corpus/recurrence/cronos/extract-cronos-vectors.py <CronExpressionFacts.cs>
```

## Scope exclusions (recorded, never silently skipped)

Every table carries a `flags` column, and the reconciliation tests **report every excluded row**
rather than quietly passing over it - by name for the RFC and libical tables, and as a per-flag
tally for the much larger Cronos table; the dateutil table excludes nothing. The exclusions are
deliberate scope boundaries of the library, not gaps in the corpus:

| Flag | Why it is excluded |
|---|---|
| `time-expansion` | `BYHOUR`/`BYMINUTE`/`BYSECOND` expansion within the day; the corpus records dates only |
| `elided` | The RFC abbreviates the occurrence list with `...`, so it is not fully enumerable |
| `exrule` | `EXRULE` is not modelled; `RecurrenceSet` composes `RDATE`/`EXDATE` only |
| `quartz-ext` | The Quartz `L` / `W` / `#` / `?` cron tokens (a planned follow-on) |
| `hash` | The Jenkins `H` jitter token, which is not a cron dialect this library models |
| `cronos-macro` | `@every_second` / `@every_minute`, macros Cronos adds beyond `crontab(5)` |
| `wrap-range` | A reversed cron range; Cronos wraps, Bodu rejects |
| `oversized-step` | A step wider than its range; Cronos rejects, cronie and Bodu accept |
| `dom-dow-intersect` | Both cron day fields restricted; Cronos intersects, Vixie and Bodu union |
| `seconds-elision` | Cronos drops a zero seconds field from `ToString`; Bodu renders every field |
| `dst` | The Cronos row sits within a day of a US Eastern DST transition, where a zone-free reading of its fixture is not offset-invariant |

`sub-daily`, `tzid` and `utc-until` are **not** exclusions. The `sub-daily` rows run like any
other: libical's four counts are reconciled, and the RFC's three sub-daily examples, whose
occurrence lists the date-only table leaves empty, are pinned in `RecurrenceRuleTests.SubDaily`.
As for `tzid` and `utc-until`, the library is offset-based and resolves no timezone identifiers,
but the RFC and libical examples state their occurrences in the `DTSTART` zone's own wall clock,
and both the date lists and the counts are invariant under a fixed offset. The rules therefore run
against the wall-clock reading of `DTSTART`, which is what a zone-free engine should reproduce.

A UTC-valued `UNTIL` against a zoned start is the one case where that reasoning needed checking
rather than assuming. The library compares the bound as a wall clock, so its cutoff differs from
the zone-resolved one by the start's offset. Every such row in both corpora was run and matched:
no occurrence falls inside that offset window, so the two readings bracket the same set. That is a
property of these particular vectors, not a theorem - a future row where an occurrence does land
in the window would surface as a difference, which is the signal we want rather than a silent
exclusion.

RFC 5545's own "every 3 hours from 9:00 AM to 5:00 PM" example is the first row where an occurrence
does land in the window. Its `UNTIL=19970902T170000Z` is 13:00 in New York, which would end the
series after 12:00, yet the RFC lists 09:00, 12:00 and 15:00: the wall-clock reading. Bodu gives the
three the RFC lists, pinned in `RecurrenceRuleTests.SubDaily`.

## Reconciliation status (2026-10-05)

| Corpus | Rows | Run | Result |
|---|---:|---:|---|
| RFC 5545 §3.8.5.3 | 39 | 23 | 23 exact date-list matches, 0 differences; the three sub-daily examples are pinned separately |
| libical `recur.txt` | 57 | 56 | 56 count matches, 0 differences (the `EXDATE` row runs through `RecurrenceSet`) |
| python-dateutil, generated sub-daily rules | 200 | 200 | 200 occurrence-list matches, and the next and previous occurrence at every recorded instant, 0 differences |
| Cronos `CronExpressionFacts` | 1,354 | 755 | 755 matches, 0 differences |

`AnchoredInterval` durations are still not covered by a corpus: RFC 5545 §3.3.6 states its duration
grammar as ABNF and publishes no vector table, so that form stays pinned by tests transcribed into
the test project with their upstream issue citations.
