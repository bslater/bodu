#!/usr/bin/env python3
"""Derive a Bodu cron vector table from gorhill/cronexpr's ``cronexpr_test.go``.

gorhill/cronexpr (https://github.com/gorhill/cronexpr, archived) is a Go cron parser offered under GPL-3.0 or
Apache-2.0 at the user's choice; this table takes it under Apache-2.0. Its test file is table-driven: the
``crontests`` array lists expressions, each with a layout and a list of {from, next} pairs, and ``TestExpressions``
asserts ``Parse(expr).Next(from)`` formatted with the layout. The remaining tests (``TestZero``, ``TestNextN``,
``TestNextN_every5min`` and ``TestInterval_Interval60Issue``) are transcribed in the hand list below, each with its
line.

Usage:

    python3 extract-gorhill-cron-vectors.py <cronexpr_test.go> > gorhill-cron-vectors.csv

The script checks the file's SHA-256 against the commit the table records and writes the table to stdout. The
output is deterministic.

gorhill's field layout differs from Bodu's. Five fields are minute to day-of-week, six are those five plus a year,
and seven are seconds, the five, and a year. A row is mapped to Bodu's layouts only when its year is ``*``, which
cannot change an answer: the seven-field rows become six-field rows (seconds first) and the six-field rows
five-field ones. A row with any other year is flagged ``year-field``. gorhill also reads full weekday names; the one
row that uses one (``friday``) is written with the three-letter name, which selects the same day.
"""

from __future__ import annotations

import hashlib
import re
import sys
from datetime import datetime

UPSTREAM_SHA256 = 'a46814c746cb731eff8c1e7a1b6c7c5bd2ad9dadb00125f9c3ae5eff853772d4'

HEADER = """\
# Cron vectors derived from gorhill/cronexpr's cronexpr_test.go
# source-class: third-party-comparison (implementation test suite, not an authority)
# source: gorhill/cronexpr -- https://github.com/gorhill/cronexpr -- cronexpr_test.go
# commit: 88b0669f7d75f171bd612b874e52b95c190218df
# file-sha256: cronexpr_test.go {digest}
# licence: GPL-3.0-only OR Apache-2.0, taken under Apache-2.0 (Copyright 2013 Raymond Hill). The rows restate the file's assertions as expressions and instants, with attribution; the upstream file itself is not committed.
# method: extracted by extract-gorhill-cron-vectors.py
# note: gorhill reads five fields (minute first), six (the five plus a year) or seven (seconds, the five, a year); rows whose year is * are mapped to Bodu's five- and six-field layouts, others are flagged. It reads any day field other than a bare * or ? as restricted (so */2 unions), rejects a step above the field maximum, and accepts full names (friday, written FRI here).
# note: left out (counted): 1 TestZero assertion that Next of Go's zero time returns the zero time (the library's sentinel API, L231-234); benchmarkExpressions are benchmark inputs, not assertions.
# flags: year-field = the row depends on gorhill's year field (its six-field form is five fields plus a year)
# flags: oversized-step = a step wider than its range, which gorhill rejects (issue #16) and Bodu reads as the range start
"""

COLUMNS = 'name,kind,format,expression,from,expected,expectation,flags,reference'

LAYOUTS = {
    '2006-01-02 15:04:05': '%Y-%m-%d %H:%M:%S',
    'Mon 2006-01-02 15:04': '%a %Y-%m-%d %H:%M',
}

# The rows of TestZero, TestNextN, TestNextN_every5min and TestInterval_Interval60Issue, transcribed by hand:
# (name, kind, format, expression, from, expected, expectation, flags, reference).
HAND_ROWS = [
    ('TestZero-01', 'unreachable-next', 'standard', '* * * * * 1980', '2013-08-31T00:00:00', '', 'upstream',
     'year-field', 'TestZero L220-224; six fields are five plus the year 1980, already past'),
    ('TestZero-02', 'next', 'standard', '* * * * * 2050', '2013-08-31T00:00:00', '2050-01-01T00:00:00', 'derived',
     'year-field', 'TestZero L226-229 asserts only a non-zero answer; gorhill\'s first minute of the year 2050'),
    ('TestNextN-01', 'next-sequence', 'standard', '0 0 * * 6#5', '2013-09-02T08:44:30',
     '2013-11-30T00:00:00|2014-03-29T00:00:00|2014-05-31T00:00:00|2014-08-30T00:00:00|2014-11-29T00:00:00',
     'upstream', '', 'TestNextN L239-258; NextN(from, 5)'),
    ('TestNextN_every5min-01', 'next-sequence', 'standard', '*/5 * * * *', '2013-09-02T08:44:32',
     '2013-09-02T08:45:00|2013-09-02T08:50:00|2013-09-02T08:55:00|2013-09-02T09:00:00|2013-09-02T09:05:00',
     'upstream', '', 'TestNextN_every5min L262-281; NextN(from, 5)'),
    ('TestInterval_Interval60Issue-01', 'invalid', 'standard', '*/60 * * * *', '', '', 'upstream', 'oversized-step',
     'TestInterval_Interval60Issue L287; six-field "*/60 * * * * *" is five fields plus year *, year dropped'),
    ('TestInterval_Interval60Issue-02', 'invalid', 'standard', '*/61 * * * *', '', '', 'upstream', 'oversized-step',
     'TestInterval_Interval60Issue L292; six-field "*/61 * * * * *", year * dropped'),
    ('TestInterval_Interval60Issue-03', 'invalid', 'standard', '2/60 * * * *', '', '', 'upstream', 'oversized-step',
     'TestInterval_Interval60Issue L297; six-field "2/60 * * * * *", year * dropped'),
    ('TestInterval_Interval60Issue-04', 'invalid', 'standard', '2-20/61 * * * *', '', '', 'upstream', 'oversized-step',
     'TestInterval_Interval60Issue L302; six-field "2-20/61 * * * * *", year * dropped'),
]


def csv_field(value: str) -> str:
    if any(c in value for c in ',"\t') or value != value.strip():
        return '"' + value.replace('"', '""') + '"'
    return value


def go_instant(text: str, layout: str) -> str:
    """Parses one of the test's formatted instants and checks that its weekday name agrees with the date."""
    value = datetime.strptime(text, LAYOUTS[layout])
    if layout.startswith('Mon ') and value.strftime('%a') != text[:3]:
        raise SystemExit(f'{text}: the weekday name does not match the date')
    return value.strftime('%Y-%m-%dT%H:%M:%S')


def bodu_form(expression: str) -> tuple[str, str, str]:
    """Maps a gorhill expression to Bodu's layout: (format, expression, adaptation note)."""
    fields = expression.split()
    notes = []
    if 'friday' in fields[-1 if len(fields) == 5 else 5]:
        fields = [f.replace('friday', 'FRI') for f in fields]
        notes.append('full name friday written FRI')
    if len(fields) == 7 and fields[6] == '*':
        return 'withSeconds', ' '.join(fields[:6]), '; '.join(['seven fields with year *, year dropped'] + notes)
    if len(fields) == 5:
        return 'standard', ' '.join(fields), '; '.join(notes)
    raise SystemExit(f'unexpected crontests expression {expression!r}')


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        print(__doc__, file=sys.stderr)
        return 2
    data = open(argv[1], 'rb').read()
    digest = hashlib.sha256(data).hexdigest()
    if digest != UPSTREAM_SHA256:
        print(f'{argv[1]}: SHA-256 {digest} is not the recorded {UPSTREAM_SHA256}', file=sys.stderr)
        return 1

    rows: list[list[str]] = []
    in_table = False
    strings: list[str] = []
    counter = 0
    for number, line in enumerate(data.decode('utf-8').splitlines(), start=1):
        if line.startswith('var crontests = []crontest{'):
            in_table = True
            continue
        if not in_table:
            continue
        if line == '}':
            break
        single = re.fullmatch(r'\t\t"([^"]*)",', line)
        if single:
            strings.append(single.group(1))
            continue
        pair = re.fullmatch(r'\t\t\t\{"([^"]*)", "([^"]*)"\},', line)
        if pair:
            expression, layout = strings[-2], strings[-1]
            fmt, bodu, note = bodu_form(expression)
            counter += 1
            ref = f'crontests L{number}' + (f'; {note}' if note else '')
            # TestExpressions parses every from value with the first layout and formats Next with the row's own.
            rows.append([f'TestExpressions-{counter:02d}', 'next', fmt, bodu, go_instant(pair.group(1), '2006-01-02 15:04:05'),
                         go_instant(pair.group(2), layout), 'upstream', '', ref])

    rows.extend(list(r) for r in HAND_ROWS)

    out = sys.stdout
    out.write(HEADER.format(digest=digest))
    out.write(COLUMNS + '\n')
    for row in rows:
        out.write(','.join(csv_field(v) for v in row) + '\n')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
