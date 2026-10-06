# Bodu recurrence validation corpus

External evidence used to validate `Bodu.Globalization.Recurrence`, kept in the repository so
every reconciliation is reproducible from committed artifacts. The reconciliation tests that
consume these tables live with the recurrence tests, which read committed copies of them
(`Bodu.Globalization.Recurrence/test/Fixtures/Vectors/`) and fail when a copy and its source here
differ; this tree holds the *sources they are reconciled against* and the tooling that derives
them.

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
| `ccronexpr/` to `zslayton/` | `third-party-comparison` | Twenty other cron libraries' test suites, restated in Bodu's dialect - evidence, not authority |
| `cron-fixes/` | `third-party-comparison` | Every fix in those 21 libraries' release notes, with its scenario where one could be found |

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

The Quartz day tokens are not a divergence. Bodu accepts `L`, `L-n`, `LW`, `L-nW` and `nW` in the
day-of-month field, `dL` and `d#k` in the day-of-week field, and `?` in either, in exactly the
shapes Cronos accepts, and rejects the shapes Cronos rejects (a token in a list, range or step,
outside the day fields, or with an offset or ordinal out of range), so the rows that use them are
reconciled like any other. They keep their `quartz-ext` flag, which now marks rather than excludes
them: until Bodu 1.3.0 the parser refused the tokens, and the flag still finds the rows that
exercise them. A token row is excluded only for one of the other flags, most often
`dom-dow-intersect`, since Bodu counts a token as a restriction and takes the union where Cronos
intersects. Weekday numbers stay Vixie's, Sunday as 0 or 7, which is also Cronos's numbering.

Regenerate with:

```shell
python3 corpus/recurrence/cronos/extract-cronos-vectors.py <CronExpressionFacts.cs>
```

## The cron library tables

The Cronos table holds the cron engine to one library's tests. Twenty more cron libraries' test suites, and the
reverse-search tests Cronos added later, are restated here in Bodu's dialect, one directory per source:

| Directory | Library | Licence | Read at | Made by | Rows | Run |
|---|---|---|---|---|---:|---:|
| `ccronexpr/` | staticlibs/ccronexpr (C) | Apache-2.0 | `5d7e772` | extractor | 88 | 88 |
| `cron-parser/` | harrisiirak/cron-parser (JavaScript) | MIT | `5b08cdf` | transcription | 285 | 225 |
| `cron-utils/` | jmrozanec/cron-utils (Java) | Apache-2.0 | `bac6e86` | transcription | 196 | 161 |
| `croner/` | Hexagon/croner (JavaScript) | MIT | `713ee72` | transcription | 367 | 297 |
| `croniter/` | pallets-eco/croniter (Python) | MIT | `4be99c3` | transcription | 389 | 264 |
| `cronos/` (reverse) | HangfireIO/Cronos (.NET) | MIT | `1123418` | extractor | 29 | 26 |
| `cronsim/` | cuu508/cronsim (Python) | BSD-3-Clause | `fd2e617` | extractor | 438 | 432 |
| `dragonmantank/` | dragonmantank/cron-expression (PHP) | MIT | `d425a24` | extractor | 213 | 204 |
| `fugit/` | floraison/fugit (Ruby) | MIT | `f571bc0` | extractor | 434 | 309 |
| `gorhill/` | gorhill/cronexpr (Go) | Apache-2.0 (dual-licensed with GPL-3.0-only) | `88b0669` | extractor | 69 | 63 |
| `gronx/` | adhocore/gronx (Go) | MIT | `9c2fea1` | extractor | 434 | 385 |
| `jobrunr/` | jobrunr/jobrunr (Java) | LGPL-3.0-or-later (derived rows only) | `c804c97` | extractor | 441 | 439 |
| `ncrontab/` | atifaziz/NCrontab (.NET) | Apache-2.0 | `d122d58` | extractor | 187 | 184 |
| `node-cron/` | kelektiv/node-cron (JavaScript) | MIT | `37a257b` | transcription | 97 | 91 |
| `quartz/` | quartz-scheduler/quartz (Java) | Apache-2.0 | `741ccc3` | transcription | 120 | 96 |
| `quartznet/` | quartznet/quartznet (.NET) | Apache-2.0 | `605a8cd` | extractor | 385 | 269 |
| `robfig/` | robfig/cron (Go) | MIT | `bc59245` | extractor | 108 | 104 |
| `saffron/` | cloudflare/saffron (Rust) | BSD-3-Clause | `90c2af8` | transcription | 290 | 244 |
| `spring/` | spring-projects/spring-framework (Java) | Apache-2.0 | `3a91d15`, `v5.3.39` | transcription | 156 | 153 |
| `supertinycron/` | exander77/supertinycron (C) | Apache-2.0 | `cd1a408` | extractor | 536 | 478 |
| `zslayton/` | zslayton/cron (Rust) | MIT (dual-licensed with Apache-2.0) | `cf2dc1d` | transcription | 91 | 73 |
| **Total** | | | | | **5,353** | **4,585** |

Each directory holds `<source>-cron-vectors.csv`; `cronos/` holds `cronos-reverse-cron-vectors.csv` beside the older
table. An **extracted** table is written by the `extract-*.py` script beside it, which checks the upstream file against
the SHA-256 in the table's header before parsing the test tables in it; assertions with no table shape are a list in
the script, each with its upstream line. A **transcribed** table was written by hand from the tests its header names,
and each row's `reference` cell gives the test and line it restates. Spring's table also reads
`CronSequenceGeneratorTests`, which Spring removed in 6.0, at the tag `v5.3.39`.

No upstream file is committed. Every header records the repository, the commit, each file's SHA-256 and the licence,
and the rows restate assertions (an expression, instants, an answer) with attribution. That matters most for
jobrunr, which is LGPL-3.0-or-later: following the libical precedent above, its directory holds derived rows only.
gorhill/cronexpr, which is dual-licensed, is taken under Apache-2.0, and zslayton/cron, likewise, under MIT. Each
header also states, in its `note` lines, how the library's dialect differs from Bodu's, and which tests were left out
and why, counted: tests of the library's own API, tests that read the wall clock, and tests that depend on a named
time zone's transitions.

Two pairs share code, so their agreement is not independent: supertinycron is a fork of ccronexpr, and Quartz.NET a
port of Quartz.

### One schema

Every table has the columns `name,kind,format,expression,from,expected,expectation,flags,reference`. `format` is
`standard` (five fields) or `withSeconds` (six, seconds first), never inferred. An instant is `yyyy-MM-ddTHH:mm:ss`
with up to seven fractional digits; one that carries an offset (`+10:00`) is queried through the `DateTimeOffset`
overloads, and its answer must carry the same offset.

| Kind | Asserts |
|---|---|
| `next`, `next-inclusive` | `GetNextOccurrence(from)`, exclusive or inclusive, returns `expected` |
| `previous`, `previous-inclusive` | `GetPreviousOccurrence(from)`, exclusive or inclusive, returns `expected` |
| `next-sequence`, `previous-sequence` | Each query, from `from` and then from the previous answer, returns the next instant of `expected` (instants separated by `\|`) |
| `unreachable-next`, `unreachable-previous` | The query returns `null` |
| `invalid`, `valid` | `TryParse` rejects, or accepts, `expression` |
| `equal`, `not-equal` | `expression` and `expected` parse to equal expressions with equal hash codes, or to unequal ones |
| `to-string` | `ToString()` returns `expected` |

`expectation` says where each answer comes from:

- **`upstream`** (4,995 rows): the library's own assertion, unchanged in meaning.
- **`cronsim`** (85): the library's answer rests on a reading Bodu does not share (its day-field rule, say), so the
  row holds cronsim's answer under Bodu's reading, below.
- **`derived`** (273): worked out by hand, with the working in `reference`. Over half come from Quartz and
  Quartz.NET, whose tests assert a gap between occurrences, or that an instant does not match, rather than naming an
  instant, and whose `?` cronsim does not read.

A row is restated (seconds moved first, a weekday renumbered, a year dropped, a long name shortened) only when the
restated row means exactly what the original asserted, and its `reference` says what changed.

### cronsim as the oracle

[cronsim](https://github.com/cuu508/cronsim) 2.7 (BSD-3-Clause) is Debian-compatible: it takes the union of the two
day fields only when neither starts with `*`, as Vixie cron and Bodu do. That makes it an oracle independent of both
the upstream library and Bodu for any expression it reads. `check-cron-vectors-with-cronsim.py` recomputes every
occurrence row it can, in every table and fix catalogue:

```shell
python3 -m venv venv && venv/bin/pip install cronsim==2.7
venv/bin/python -I corpus/recurrence/check-cron-vectors-with-cronsim.py --quiet \
    corpus/recurrence/*/*-cron-vectors.csv corpus/recurrence/cron-fixes/*-fixes.csv
```

On the committed tables it reports `checked 2625, agreed 2625, mismatched 0, horizon 0, unsupported 825`. The
unsupported rows use syntax cronsim does not read (`?`, `W`, `L-n`, `LW`, the macros, a day its month never has) or
would take cronsim past the end of Python's calendar; the older Cronos table, whose `next` rows are inclusive, is
skipped.

### Flags

A flagged row is excluded, and the test run reports each table's exclusions by flag. These are the 768 excluded rows:

| Flag | Rows | Excluded because |
|---|---:|---|
| `token-list` | 167 | A Quartz token (`L`, `W`, `#`) in a list, range or step, which Bodu rejects |
| `syntax-extension` | 159 | Syntax only the library has: `?` outside the day fields, `%`, `&`, `~` and `+`, hour 24, `L` alone in day-of-week, a bare `W`, `d#-k`, leap seconds, and the like |
| `year-field` | 145 | The row depends on a year field, which Bodu does not have |
| `wrap-range` | 132 | A reversed range (`55-5`, `FRI-TUE`, `7-5`) that the library wraps or renumbers; Bodu rejects it |
| `hash` | 37 | The Jenkins `H` token |
| `oversized-step` | 37 | A step wider than its range, which the library rejects; Bodu, like cronie, takes the range start |
| `dom-dow-intersect` | 20 | Both day fields restricted, which the library (or the test's option) intersects; Bodu takes the union |
| `macro` | 19 | A macro outside `crontab(5)`'s seven, such as `@every`, `@minutely` or `@noon` |
| `duplicate-value` | 13 | A value listed twice, which cron-parser rejects; Bodu reads a list as the set it names |
| `strict-step` | 12 | croner 10 rejects a single value with a step (`5/5`); Bodu reads `n/m` as `n` to the field maximum |
| `equal-range` | 9 | croniter reads an equal range (`5-5`) as the whole cycle; Bodu reads it as the one value |
| `quartz-dow-numbering` | 7 | Quartz's 1 = Sunday weekday digits that cannot be renumbered without changing the row's meaning |
| `impossible-date` | 7 | fugit rejects a day the month never has (`30 2`); Bodu accepts it, and its searches return `null` |
| `question-mark` | 6 | The library requires `?` in exactly one day field, or rejects it in both; Bodu reads `?` as `*` |
| `single-value-step` | 4 | dragonmantank rejects a step on a single value (`1/10`) |
| `library-rejects` | 3 | cronsim rejects syntax Bodu documents as valid (`* * 30 2 *`, `MONL`) |
| `long-name` | 3 | A name longer than three letters (`March`) |
| `dom-pruning` | 2 | cron-parser drops days the named month cannot have before comparing; Bodu compares the sets as written |
| `dow-seven` | 2 | croniter's day of week runs 0-6, so it reads `7/2` and `6/1` differently |
| `zero-offset` | 2 | saffron rejects `L-0`; Bodu reads it as `L` |
| `dom-dow-union` | 1 | croniter's default unions a star-led day field (`*/2`) with the other; Bodu intersects them |
| `token-combination` | 1 | jobrunr rejects `L` in both day fields; Bodu takes their union |
| `layout-equality` | 1 | node-cron equates a five-field expression with the six-field one at second 0; Bodu's equality includes the layout |
| `cross-layout` | 1 | supertinycron equates a six-field expression with a five-field one, likewise |
| `quartz-unsupported` | 1 | node-cron has no Quartz tokens and rejects `L`, which Bodu accepts |

### Where the libraries read cron differently

The libraries part most over the two day fields. Vixie cron, cronsim, gronx, jobrunr and Bodu take the union of the
day of the month and the day of the week when neither field starts with `*`, and the intersection otherwise.
NCrontab, Spring, ccronexpr, supertinycron and zslayton always intersect, and so do Cronos when both fields are
restricted, croniter with `day_or=False` and croner with `domAndDow`. cron-parser, croner, croniter, cron-utils'
UNIX definition, dragonmantank, gorhill and robfig (since its #70) take the union unless a field is exactly `*` (or
`?`), so `*/2` counts as restricted; fugit and node-cron decide by whether the field selects every value. A row whose
answer depends on the rule holds cronsim's answer under Bodu's, or is flagged `dom-dow-intersect` or `dom-dow-union`.

The other differences are restated where the meaning survives and flagged where it does not:

- **Weekday numbers.** Quartz, Quartz.NET, saffron and zslayton number Sunday 1; jobrunr, robfig and croniter run
  0-6. Bodu runs 0-7, with 0 and 7 both Sunday.
- **Seconds.** croniter puts them last; every other six-field form puts them first, as Bodu does.
- **Years.** Quartz, Quartz.NET, croniter, croner, gorhill, gronx, supertinycron, zslayton and cron-utils' Quartz
  definition have a year field. A row whose year is `*`, or whose instants all lie in its year, is restated without
  it.
- **Reversed ranges.** Cronos, Quartz.NET, cron-utils, croniter, fugit and saffron wrap them; Spring, gronx and
  dragonmantank read a range from 7 as starting on Sunday, and croner reads `SUN` at the end of a range as 7. Bodu
  rejects them all; `MON-SUN` is one, because `SUN` is 0.

Regenerate an extracted table with its script and the upstream file it names:

```shell
python3 corpus/recurrence/<source>/extract-<table>-cron-vectors.py <upstream files...>
```

Each script's docstring gives its arguments, and the table's header gives the commit to fetch them at.

## `cron-fixes/` - the release-note fix catalogue

The release notes of the same 21 libraries were read in full, and every fix they list is catalogued (for the
schedulers and frameworks, Quartz, Quartz.NET, Spring and jobrunr, every fix to their cron support): one file per
library, `cron-fixes/<library>-fixes.csv`, one row per fix, or one per case where a fix needs several. Where the
notes are missing or incomplete (ccronexpr, gorhill, robfig, saffron, supertinycron, and zslayton after 0.15.0), the
commit history was read as well, and the file's header says so. Each row records the version, the reference (an
issue, a pull request or a commit) and a summary, then a class:

| Class | Rows | Meaning |
|---|---:|---|
| `applies` | 946 | The row carries the scenario of the fix, in Bodu's dialect, and runs: Bodu must do what the fix established |
| `dialect` | 192 | The row runs, asserting Bodu's documented behaviour where it differs from the fix's; `reason` names it |
| `n/a` | 876 | The fix concerns packaging, an API Bodu does not have, time zones, performance or the library's language; `reason` says which |
| `unknown` | 18 | No scenario could be found; `reason` links the issue or commit |

That is 1,383 fixes in 2,032 rows. A scenario is, first, the regression test the fix added, read from its pull
request or commit, and otherwise the scenario its issue describes. The runnable rows use the vector tables' columns
and kinds, and run through the same tests, each named after its library, version, reference and summary;
`RecurrenceCorpusTests` also holds each catalogue's count in each class, and fails on a `dialect`, `n/a` or `unknown`
row without a reason.

| Library | Fixes | Rows | applies | dialect | n/a | unknown |
|---|---:|---:|---:|---:|---:|---:|
| ccronexpr | 19 | 26 | 9 | 0 | 15 | 2 |
| cron-parser | 160 | 199 | 81 | 17 | 101 | 0 |
| cron-utils | 285 | 422 | 214 | 44 | 163 | 1 |
| croner | 155 | 202 | 62 | 20 | 118 | 2 |
| croniter | 173 | 228 | 85 | 24 | 117 | 2 |
| cronos | 78 | 129 | 66 | 5 | 58 | 0 |
| cronsim | 19 | 26 | 13 | 0 | 13 | 0 |
| dragonmantank | 100 | 150 | 74 | 7 | 67 | 2 |
| fugit | 67 | 92 | 38 | 16 | 38 | 0 |
| gorhill | 10 | 23 | 17 | 4 | 2 | 0 |
| gronx | 33 | 63 | 40 | 6 | 16 | 1 |
| jobrunr | 14 | 55 | 45 | 3 | 6 | 1 |
| ncrontab | 7 | 10 | 6 | 0 | 4 | 0 |
| node-cron | 34 | 44 | 14 | 4 | 26 | 0 |
| quartz | 19 | 42 | 27 | 4 | 11 | 0 |
| quartznet | 79 | 100 | 47 | 10 | 40 | 3 |
| robfig | 30 | 33 | 7 | 2 | 24 | 0 |
| saffron | 4 | 5 | 0 | 2 | 3 | 0 |
| spring | 48 | 106 | 75 | 8 | 23 | 0 |
| supertinycron | 34 | 51 | 20 | 4 | 23 | 4 |
| zslayton | 15 | 26 | 6 | 12 | 8 | 0 |
| **Total** | **1,383** | **2,032** | **946** | **192** | **876** | **18** |

### What the tables found

Two defects, both now fixed, red then green:

- **A search near either end of the calendar threw** ([#798](https://github.com/bslater/bodu/issues/798)). A search
  that would step past 9999-12-31 or before 0001-01-01 threw `ArgumentOutOfRangeException` instead of returning
  `null`. Cronos 0.10.0 (PR #80), NCrontab 3.3.1 (#21) and saffron's calendar-end iterator tests assert `null` there.
- **The search stopped after twelve years** ([#799](https://github.com/bslater/bodu/issues/799)). It returned `null`
  for satisfiable expressions whose next occurrence was further away. jobrunr's table expects `0 0 0 29 2 */5` from
  2019-01-01 to give 2032-02-29 (`args-405`), 13 years on. The search now covers 400 years, a whole cycle of the
  Gregorian calendar.

Every other difference is a dialect difference, restated or flagged as above.

## Scope exclusions (recorded, never silently skipped)

Every table carries a `flags` column, and the reconciliation tests **report every excluded row**
rather than quietly passing over it - by name for the RFC and libical tables, and as a per-flag
tally for the much larger Cronos table and the cron library tables, whose flags are listed
[above](#flags); the dateutil table excludes nothing. The exclusions are deliberate scope boundaries
of the library, not gaps in the corpus. The flags of the recurrence and Cronos tables:

| Flag | Why it is excluded |
|---|---|
| `time-expansion` | `BYHOUR`/`BYMINUTE`/`BYSECOND` expansion within the day; the corpus records dates only |
| `elided` | The RFC abbreviates the occurrence list with `...`, so it is not fully enumerable |
| `exrule` | `EXRULE` is not modelled; `RecurrenceSet` composes `RDATE`/`EXDATE` only |
| `hash` | The Jenkins `H` jitter token, which is not a cron dialect this library models |
| `cronos-macro` | `@every_second` / `@every_minute`, macros Cronos adds beyond `crontab(5)` |
| `wrap-range` | A reversed cron range; Cronos wraps, Bodu rejects |
| `oversized-step` | A step wider than its range; Cronos rejects, cronie and Bodu accept |
| `dom-dow-intersect` | Both cron day fields restricted; Cronos intersects, Vixie and Bodu union |
| `seconds-elision` | Cronos drops a zero seconds field from `ToString`; Bodu renders every field |
| `dst` | The Cronos row sits within a day of a US Eastern DST transition, where a zone-free reading of its fixture is not offset-invariant |

`quartz-ext`, `sub-daily`, `tzid` and `utc-until` are **not** exclusions. `quartz-ext` marks the
Cronos rows that use the Quartz day tokens, which Bodu accepts as Cronos does. The `sub-daily` rows
run like any other: libical's four counts are reconciled, and the RFC's three sub-daily examples, whose
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

## Reconciliation status (2026-10-06)

| Corpus | Rows | Run | Result |
|---|---:|---:|---|
| RFC 5545 §3.8.5.3 | 39 | 23 | 23 exact date-list matches, 0 differences; the three sub-daily examples are pinned separately |
| libical `recur.txt` | 57 | 56 | 56 count matches, 0 differences (the `EXDATE` row runs through `RecurrenceSet`) |
| python-dateutil, generated sub-daily rules | 200 | 200 | 200 occurrence-list matches, and the next and previous occurrence at every recorded instant, 0 differences |
| Cronos `CronExpressionFacts` | 1,354 | 1,074 | 1,074 matches, 0 differences, the 319 rows that use the Quartz day tokens included |
| Cronos `CronExpressionReverseFacts` and twenty other cron libraries' test suites | 5,353 | 4,585 | 4,585 matches, 0 differences, once #798 and #799 were fixed |
| The same 21 libraries' release-note fixes | 2,032 | 1,138 | 1,138 matches (946 `applies`, 192 `dialect`), 0 differences; 876 `n/a` and 18 `unknown` rows each give their reason |

`AnchoredInterval` durations are still not covered by a corpus: RFC 5545 §3.3.6 states its duration
grammar as ABNF and publishes no vector table, so that form stays pinned by tests transcribed into
the test project with their upstream issue citations.
