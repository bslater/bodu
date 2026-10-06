#!/usr/bin/env python3
"""Derive a Bodu cron vector table from jobrunr's cron test file.

jobrunr (https://github.com/jobrunr/jobrunr) is offered under LGPL-3.0-or-later and a commercial licence. Its test
file core/src/test/java/org/jobrunr/scheduling/cron/CronExpressionTest.java is read here only for data: each table
row's expression, start instant and expected instant. No other text of the file is reproduced, either in the table
or in this script, and the file itself is not committed; the table records the file's SHA-256 so the derivation can
be repeated.

Usage:

    python3 extract-jobrunr-cron-vectors.py <CronExpressionTest.java> > jobrunr-cron-vectors.csv

The script checks the file's SHA-256 and writes the table to stdout. The output is deterministic.

What the rows assert: the main table gives, per row, an expression of five or six fields (seconds first), a start
and the expected next fire time strictly after it, both as UTC wall-clock times; each becomes one ``next`` row. A
second, smaller table runs rows in named zones around daylight-saving changes; only the rows with no change between
start and answer, and so a single offset, can be stated here, with that offset. The two one-off rejection tests are
listed by line below.
"""

from __future__ import annotations

import hashlib
import re
import sys

UPSTREAM_SHA256 = 'e3a3a6ca0f410a7d2c07a9b7187275913451130483dc642407edcaa05c5ca158'
PATH = 'core/src/test/java/org/jobrunr/scheduling/cron/CronExpressionTest.java'

HEADER = f"""\
# Cron vectors derived from jobrunr's CronExpressionTest.java
# source-class: third-party-comparison (implementation test suite, not an authority)
# source: jobrunr -- https://github.com/jobrunr/jobrunr -- {PATH}
# commit: c804c97402080d3226f37ac7866ec53cf591050c
# file-sha256: {PATH} {{digest}}
# licence: LGPL-3.0-or-later (Copyright (c) 2019-2021 Ronald Dehuysser; also offered under a commercial licence). The table holds derived data only - each row's expression, start instant and expected instant - attributed to the file and its SHA-256; no other upstream text is reproduced and the file is not committed.
# method: extracted by extract-jobrunr-cron-vectors.py
# note: jobrunr reads five or six fields (seconds first) in UTC, weekdays 0-6, the same leading-star rule for the two day fields as Bodu, L in the day-of-month field and dL in the day-of-week field (case-insensitive, not both at once), n/m steps and steps wider than their range, and loose whitespace; it rejects reversed ranges and has no ?, W, # or macros. Each table row is the next fire time strictly after the start.
# note: left out (counted): 7 tests whose answers are computed from the wall clock (L76-159), 2 API tests with no cron meaning (L161-181), and 10 of the 13 daylight-saving rows (L796-812), whose start or answer lies in a skipped or repeated hour or carries a different offset; the 3 rows of that table with no change between start and answer are kept with their fixed offsets. One row, args-405, answers 13 years after its start, past the 12-year search Bodu had before issue #799 was fixed.
# flags: long-name = a month name longer than three letters, which jobrunr accepts and Bodu rejects
# flags: token-combination = L in both day fields, which jobrunr rejects and Bodu accepts as the union of the last day and the last weekday
"""

COLUMNS = 'name,kind,format,expression,from,expected,expectation,flags,reference'

ROW3 = re.compile(r'^\s*arguments\("([^"]*)",\s*"([^"]*)",\s*"([^"]*)"\),?\s*(//.*)?$')
ROW5 = re.compile(r'^\s*arguments\("([^"]*)",\s*"([^"]*)",\s*"([^"]*)",\s*(?:"([^"]*)"|null),\s*"([^"]*)"\),?\s*(//.*)?$')

# Main-table rows that need a flag or a note, keyed by row name; the expression is checked.
OVERRIDES = {
    # A full month name; Bodu takes three-letter names only.
    'args-291': {'expr': '0 0 1 Jan,May,August *', 'flags': 'long-name',
                 'note': 'Bodu takes three-letter month names only'},
    # The next 29 February that is a Sunday or a Friday after 2019 is in 2032, 13 years on: Bodu's former 12-year
    # search answered null (D2, issue #799), which its 400-year search fixed.
    'args-405': {'expr': '0 0 0 29 2 */5',
                 'note': 'D2 (issue #799): the answer is 13 years after the start, past the 12-year search Bodu had'},
}

# The daylight-saving table's rows with no change between start and answer, keyed by line: the zone and the fixed
# offset it has at both instants (Brussels is on standard time the day before its 2025-03-30 change; Tokyo and UTC
# have no changes).
FIXED_OFFSET_ROWS = {
    801: ('Europe/Brussels', '+01:00'),
    814: ('Asia/Tokyo', '+09:00'),
    815: ('UTC', '+00:00'),
}

# The two one-off rejection tests, by line: (name, kind, format, expression, flags, line, note).
ONE_OFF = [
    ('invalid-01', 'invalid', 'standard', 'invalid', '', 185, 'a single word is rejected'),
    ('invalid-02', 'invalid', 'withSeconds', '0 0 0 l * 5L', 'token-combination', 190,
     'jobrunr rejects L in both day fields'),
]


def csv_field(value: str) -> str:
    if any(c in value for c in ',"\t\r\n') or value != value.strip():
        return '"' + value.replace('"', '""') + '"'
    return value


def iso(text: str) -> str:
    if not re.fullmatch(r'\d{4}-\d\d-\d\d \d\d:\d\d:\d\d', text):
        raise ValueError(text)
    return text.replace(' ', 'T')


def layout(expression: str) -> str:
    count = len(expression.split())
    if count == 5:
        return 'standard'
    if count == 6:
        return 'withSeconds'
    raise ValueError(expression)


def main(argv: list[str]) -> int:
    paths = argv[1:]
    if len(paths) != 1:
        print(__doc__, file=sys.stderr)
        return 2
    data = open(paths[0], 'rb').read()
    digest = hashlib.sha256(data).hexdigest()
    if digest != UPSTREAM_SHA256:
        print(f'{paths[0]}: SHA-256 {digest} is not the recorded {UPSTREAM_SHA256}', file=sys.stderr)
        return 1
    lines = data.decode('utf-8').splitlines()

    main_rows: list[list[str]] = []
    count = 0
    seen = set()
    for number, line in enumerate(lines, start=1):
        if 'arguments(' not in line:
            continue
        three = ROW3.match(line)
        five = ROW5.match(line)
        if three:
            count += 1
            name = f'args-{count:03d}'
            expr, start, expected = three.group(1), three.group(2), three.group(3)
            override = OVERRIDES.get(name, {})
            if override:
                seen.add(name)
                if override['expr'] != expr:
                    print(f'{name}: override is for {override["expr"]!r}, L{number} holds {expr!r}', file=sys.stderr)
                    return 1
            reference = f'L{number}'
            if 'note' in override:
                reference += f'; {override["note"]}'
            row = [name, 'next', layout(expr), expr, iso(start), iso(expected), 'upstream', override.get('flags', ''),
                   reference]
            main_rows.append(row)
        elif five:
            if number not in FIXED_OFFSET_ROWS:
                continue
            zone, offset = FIXED_OFFSET_ROWS[number]
            if five.group(3) != zone or five.group(4) not in (None, offset):
                print(f'L{number}: expected a row in {zone}', file=sys.stderr)
                return 1
            expr = five.group(1)
            name = f'fixed-offset-L{number}'
            main_rows.append([name, 'next', layout(expr), expr, iso(five.group(2)) + offset,
                              iso(five.group(5)) + offset, 'upstream', '',
                              f'L{number}; the zone is at {offset} at both instants, with no change between them'])
        else:
            print(f'L{number}: unparsed row', file=sys.stderr)
            return 1
    if set(OVERRIDES) - seen:
        print(f'overrides for missing rows: {sorted(set(OVERRIDES) - seen)}', file=sys.stderr)
        return 1

    for name, kind, fmt, expr, flags, number, note in ONE_OFF:
        if f'"{expr}"' not in lines[number - 1]:
            print(f'{name}: L{number} does not hold the expression', file=sys.stderr)
            return 1
        main_rows.append([name, kind, fmt, expr, '', '', 'upstream', flags, f'L{number}; {note}'])

    rows = main_rows
    out = [HEADER.format(digest=digest).rstrip('\n'), COLUMNS]
    out += [','.join(csv_field(v) for v in row) for row in rows]
    sys.stdout.buffer.write(('\n'.join(out) + '\n').encode('utf-8'))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
