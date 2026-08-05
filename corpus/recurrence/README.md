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
| `libical/` | `third-party-comparison` | The reference C implementation's test corpus — evidence, not authority |

## Lineage warning (no majority voting)

The same rule that governs the calendar corpus applies here, and it bites harder than it looks.
There are only **two** independent model families among the widely-cited recurrence
implementations: the libical line (libical, ical.js) and the python-dateutil line (dateutil,
rrule.js, which is an explicit port). Agreement between dateutil and rrule.js is therefore *not*
independent confirmation. Bodu's engine is a third, written from the RFC.

This is not hypothetical. The cross-library review that preceded this corpus found cases where
**python-dateutil is wrong and Bodu is right** — most notably `BYSETPOS` truncating the first
weekly period to the part on or after `DTSTART` instead of indexing the whole period
([libical#795](https://github.com/libical/libical/issues/795),
[dateutil#1398](https://github.com/dateutil/dateutil/issues/1398)). A loader that treated any one
implementation's output as ground truth would have regressed that. Disagreements are classified,
never silently resolved.

## `rfc5545/` — the normative examples

- `rfc5545-recurrence-examples.csv` — 39 examples: description, `DTSTART`, `RRULE`, flags, and the
  expected occurrence dates.
- `extract-rfc5545-examples.py` — the extractor, committed so the table is reproducible from the
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

## `libical/` — the reference-implementation corpus

`libical-recur-expectations.csv` — 57 rules derived from libical's `test-data/recur.txt`. libical
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

## Scope exclusions (recorded, never silently skipped)

Both tables carry a `flags` column, and the reconciliation tests **report every excluded row by
name and reason** rather than quietly passing over it. The exclusions are deliberate scope
boundaries of the library, not gaps in the corpus:

| Flag | Why it is excluded |
|---|---|
| `sub-daily` | `FREQ=HOURLY/MINUTELY/SECONDLY` parses and round-trips but does not yet enumerate |
| `time-expansion` | `BYHOUR`/`BYMINUTE`/`BYSECOND` expansion within the day; the corpus records dates only |
| `elided` | The RFC abbreviates the occurrence list with `...`, so it is not fully enumerable |
| `exrule` | `EXRULE` is not modelled; `RecurrenceSet` composes `RDATE`/`EXDATE` only |

`tzid` and `utc-until` are **not** exclusions. The library is offset-based and resolves no timezone identifiers,
but the RFC and libical examples state their occurrences in the `DTSTART` zone's own wall clock,
and both the date lists and the counts are invariant under a fixed offset. The rules therefore run
against the wall-clock reading of `DTSTART`, which is what a zone-free engine should reproduce.

A UTC-valued `UNTIL` against a zoned start is the one case where that reasoning needed checking
rather than assuming. The library compares the bound as a wall clock, so its cutoff differs from
the zone-resolved one by the start's offset. Every such row in both corpora was run and matched:
no occurrence falls inside that offset window, so the two readings bracket the same set. That is a
property of these particular vectors, not a theorem — a future row where an occurrence does land
in the window would surface as a difference, which is the signal we want rather than a silent
exclusion.

## Reconciliation status (2026-08-05)

| Corpus | Rows | Run | Result |
|---|---:|---:|---|
| RFC 5545 §3.8.5.3 | 39 | 23 | 23 exact date-list matches, 0 differences |
| libical `recur.txt` | 57 | 52 | 52 count matches, 0 differences (the `EXDATE` row runs through `RecurrenceSet`) |

Cron and duration vectors are not held here: neither Vixie cron nor RFC 5545 §3.3.6 durations
publish a machine-readable corpus, so those forms are pinned by vectors transcribed into the test
project with their upstream issue citations.
