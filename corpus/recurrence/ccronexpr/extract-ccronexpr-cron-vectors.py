#!/usr/bin/env python3
"""Derive a Bodu cron vector table from staticlibs/ccronexpr's ``ccronexpr_test.c``.

ccronexpr (https://github.com/staticlibs/ccronexpr, Apache-2.0) is a small C cron library whose test file
asserts its answers as a list of calls: ``check_next(pattern, initial, expected)`` for the next occurrence,
``check_same(expr1, expr2)`` for two spellings of one schedule, ``check_expr_invalid(expr)`` for a rejected
expression, and ``check_calc_invalid()`` for a schedule that never fires. This script turns each call into one
row of the PR G vector schema.

Usage:

    python3 extract-ccronexpr-cron-vectors.py <ccronexpr_test.c> > ccronexpr-cron-vectors.csv

The script checks the file's SHA-256 against the commit the table records and writes the table to stdout. The
output is deterministic.

ccronexpr's dialect, as far as these rows go, is Bodu's: exactly six fields with the seconds first, weekdays
0-7 with 0 and 7 for Sunday, and ``N/step`` running to the field maximum. It intersects the day-of-month and
day-of-week fields where Bodu takes the union of two restricted fields, but no row here restricts both, so
every row runs unchanged with the library's own expected value.
"""

from __future__ import annotations

import hashlib
import re
import sys

UPSTREAM_SHA256 = '59d7c406580b2bed0f3b87ba950b164f35cc882adffc84df837a7c0c7d97204b'

HEADER = """\
# Cron vectors derived from staticlibs/ccronexpr's ccronexpr_test.c
# source-class: third-party-comparison (implementation test suite, not an authority)
# source: ccronexpr -- https://github.com/staticlibs/ccronexpr -- ccronexpr_test.c
# commit: 5d7e772df34aadc938f1246ebe2551b9c1c19012
# file-sha256: ccronexpr_test.c {digest}
# licence: Apache-2.0 (Copyright 2015, alex at staticlibs.net). The rows restate the file's assertions as expressions and instants, with attribution; the upstream file itself is not committed.
# method: extracted by extract-ccronexpr-cron-vectors.py
# note: ccronexpr reads exactly six fields, seconds first, weekdays 0-7, and intersects the two day fields; no row restricts both day fields, so every row runs unchanged. cron_next returning -1 (no occurrence) is read as unreachable-next.
# note: left out (counted): 1 check_next row compiled only under CRON_USE_LOCAL_TIME (L250, needs the machine's time zone), test_bits (the bit-set helper API) and test_memory (allocation counting).
# flags: none
"""

COLUMNS = 'name,kind,format,expression,from,expected,expectation,flags,reference'

CALL = re.compile(r'\b(check_next|check_same|check_expr_invalid)\(\s*"([^"]*)"\s*(?:,\s*"([^"]*)"\s*)?(?:,\s*"([^"]*)"\s*)?\)')
FUNCTION = re.compile(r'^\s*void\s+(\w+)\s*\(')


def instant(text: str) -> str:
    """Converts the test file's ``2012-07-01_09:53:50`` instants to the table's ISO form."""
    return text.replace('_', 'T')


def csv_field(value: str) -> str:
    if any(c in value for c in ',"') or value != value.strip():
        return '"' + value.replace('"', '""') + '"'
    return value


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
    counters: dict[str, int] = {}
    function = ''
    local_time_branch = False
    for number, line in enumerate(data.decode('utf-8').splitlines(), start=1):
        stripped = line.strip()
        match = FUNCTION.match(line)
        if match:
            function = match.group(1)
        # The CRON_USE_LOCAL_TIME branch of test_expr holds the one row that depends on the machine's time zone;
        # the #else branch is the UTC build the rows below come from.
        if stripped.startswith('#ifdef CRON_USE_LOCAL_TIME'):
            local_time_branch = True
            continue
        if stripped.startswith('#else') or stripped.startswith('#endif'):
            local_time_branch = False
            continue
        if local_time_branch:
            continue
        call = CALL.search(line)
        if not call or stripped.startswith('void'):
            continue
        helper, a, b, c = call.groups()
        counters[function] = counters.get(function, 0) + 1
        name = f'{function}-{counters[function]:02d}'
        if helper == 'check_next':
            rows.append([name, 'next', 'withSeconds', a, instant(b), instant(c), 'upstream', '', f'{function} L{number}'])
        elif helper == 'check_same':
            rows.append([name, 'equal', 'withSeconds', a, '', b, 'upstream', '', f'{function} L{number} check_same'])
        else:
            rows.append([name, 'invalid', 'withSeconds', a, '', '', 'upstream', '', f'{function} L{number} check_expr_invalid'])

    # check_calc_invalid() (L231-239) hard-codes its one scenario: June 31 never exists, so cron_next answers -1.
    rows.append(['check_calc_invalid-01', 'unreachable-next', 'withSeconds', '0 0 0 31 6 *', '2012-07-01T09:53:50', '',
                 'upstream', '', 'check_calc_invalid L233-237; cron_next returns INVALID_INSTANT (-1)'])

    out = sys.stdout
    out.write(HEADER.format(digest=digest))
    out.write(COLUMNS + '\n')
    for row in rows:
        out.write(','.join(csv_field(v) for v in row) + '\n')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
