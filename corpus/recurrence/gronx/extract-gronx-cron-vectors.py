#!/usr/bin/env python3
"""Derive a Bodu cron vector table from adhocore/gronx's test files.

gronx (https://github.com/adhocore/gronx, MIT) is a Go cron expression library. Its main table, ``testcases()`` in
``gronx_test.go``, lists {expr, ref, isDue, next} cases that three tests reuse:

* ``TestIsDue`` (gronx_test.go) asserts ``IsDue(expr, ref) == isDue``, and the inclusive loop of
  ``TestNextTickAfter`` (next_test.go) asserts ``(NextTickAfter(expr, ref, true) == ref) == isDue``. Together they say
  that the inclusive next occurrence from ``ref`` is ``ref`` when the case is due and the exclusive next otherwise,
  which is one ``next-inclusive`` row (``testcases-NN-due``).
* The exclusive loop of ``TestNextTickAfter`` asserts ``NextTickAfter(expr, ref, false) == next``
  (``testcases-NN-next``).
* ``TestPrevTickBefore`` (prev_test.go) round-trips every case: with ``next1`` the exclusive next, it asserts
  ``PrevTickBefore(next1, true) == next1`` (``testcases-NN-prev-incl``) and, with ``next2 = NextTickAfter(next1,
  false)``, ``PrevTickBefore(next2, false) == next1`` (``testcases-NN-prev``). ``next2`` is computed by gronx at run
  time; the NEXT2 table below records it, recomputed with cronsim 2.7 and checked by hand.

``errcases()`` are rejected expressions (``invalid``). The bespoke tests of all four files are transcribed in the
hand list below, each with its line.

Usage:

    python3 extract-gronx-cron-vectors.py <gronx_test.go> <next_test.go> <prev_test.go> <checker_test.go>

The script checks each file's SHA-256 against the commit the table records and writes the table to stdout. The
output is deterministic.

gronx's dialect is close to Bodu's: the same day-of-month/day-of-week rule, weekdays 0-7, N/step to the field
maximum, and the Quartz tokens. It differs in its year field (a six-field expression whose sixth field is a
four-digit year is five fields plus a year; seven fields are seconds, five fields and a year), ``?`` in any field,
its extra tags (``@always``, ``@5minutes``, ...), names replaced in any field, and ranges such as ``7-4`` that start
at 7 (read as Sunday to Thursday). Those rows are flagged; a seven-field row whose year is ``*`` is restated without
it.
"""

from __future__ import annotations

import hashlib
import re
import sys

SHA256 = {
    'gronx_test.go': '50ed783cc2399fb8407425517d57cc3d1653fe0cd351af2af877fe008ec86135',
    'next_test.go': 'adc02ec76fc363dc6148fa418362e285838e9f8a59e57effaca47932071d3e3e',
    'prev_test.go': '10abd4e92ee6e8cb010fab7584335b363c5da2d07ea668d4c3d6c4c0c930cc23',
    'checker_test.go': '1e59e55aaaabbd88776b1304f5d290c525086245b3ad34faa9ad4988294f173d',
}

HEADER = """\
# Cron vectors derived from adhocore/gronx's gronx_test.go, next_test.go, prev_test.go and checker_test.go
# source-class: third-party-comparison (implementation test suite, not an authority)
# source: gronx -- https://github.com/adhocore/gronx -- gronx_test.go, next_test.go, prev_test.go, checker_test.go
# commit: 9c2fea176d500c5358f59f73f782a312e79d489b
# file-sha256: gronx_test.go {gronx_test}
# file-sha256: next_test.go {next_test}
# file-sha256: prev_test.go {prev_test}
# file-sha256: checker_test.go {checker_test}
# licence: MIT (Copyright (c) 2021-2099 Jitendra Adhikari). The rows restate the files' assertions as expressions and instants, with attribution; the upstream files themselves are not committed.
# method: extracted by extract-gronx-cron-vectors.py
# note: gronx shares Bodu's day-field rule, weekdays 0-7 and the Quartz tokens, but adds a year field (a four-digit sixth field, or a seventh), ? in any field, extra tags (@always, @5minutes), names in any field and ranges that start at 7 (7-4 is Sunday to Thursday); those rows are flagged. A seven-field row whose year is * is restated without it.
# note: left out (counted): 1 TestNormalize row whose input holds a newline (L33), 1 TestIsValid assertion comparing two API entry points (L63), TestAddTag (3, tag-registration API), TestValueByPos (1, API on time.Now), TestNextTick (2) and PrevTick (1, prev_test.go L19-23) on the wall clock, TestNextTickAfterDSTFallBack (2, America/New_York) and TestNextTickAfterRechecksFieldsAcrossDST (6, named zones).
# flags: year-field = the row depends on gronx's year field
# flags: macro = a gronx tag outside crontab(5)'s seven (@always, @5minutes)
# flags: wrap-range = a weekday range that starts at 7 (7-4, 7-3), which gronx reads as Sunday onward and Bodu rejects as reversed
# flags: syntax-extension = gronx-only syntax: ? outside the day fields, or names replaced in any field
"""

COLUMNS = 'name,kind,format,expression,from,expected,expectation,flags,reference'

# next2 for prev_test.go's round trip, keyed by testcases index: NextTickAfter(expr, next1, false), recomputed with
# cronsim 2.7 where it reads the expression, and by hand for the rows it cannot (W, ?, tags, years, 7-n ranges).
NEXT2 = {
    1: '2021-04-19 12:56:00',  # @always is every minute (by hand)
    4: '2017-05-10 02:40:00',  # @5minutes is */5 (by hand)
    5: '2017-11-07 00:01:00',  # 7W, minute *: the next minute of the same day (by hand)
    6: '2015-08-10 22:02:00', 7: '2015-08-10 21:52:00', 8: '2015-08-10 21:52:00', 9: '2015-08-10 21:52:00',
    10: '2015-08-10 21:52:00', 11: '2015-08-10 21:52:00', 12: '2015-08-10 21:52:00', 13: '2015-08-10 22:01:00',
    14: '2015-08-10 21:52:00', 15: '2015-08-10 22:08:00', 16: '2015-08-19 00:08:00', 17: '2015-08-16 01:01:00',
    18: '2015-08-12 21:47:00', 19: '2023-07-21 14:00:00', 20: '2023-07-23 00:00:00', 21: '2023-07-23 00:00:00',
    22: '2011-06-19 00:01:00', 23: '2011-06-19 00:01:00', 24: '2011-06-20 00:01:00', 25: '2011-06-20 00:00:00',
    26: '2011-06-20 00:00:00', 27: '2011-06-19 00:00:00',
    28: '2011-06-19 00:00:00',  # 7-4 is Sun-Thu: after Thu 06-16 the next is Sun 06-19 (by hand)
    29: '2011-06-17 00:00:00',
    30: '2011-06-20 00:00:00',  # 7-3 is Sun-Wed: after Sun 06-19 the next is Mon 06-20 (by hand)
    31: '2011-06-17 00:00:00', 32: '2011-06-22 00:00:00', 33: '2011-06-22 00:00:00', 34: '2011-06-22 00:00:00',
    35: '2011-06-21 00:00:00', 36: '2011-07-22 00:00:00', 37: '2011-06-20 12:12:00', 38: '2011-06-20 13:02:00',
    39: '2011-06-20 12:08:00', 40: '2011-06-20 12:10:00', 41: '2011-06-20 12:10:00', 42: '2011-06-22 00:00:00',
    43: '2012-01-08 00:00:00', 44: '2012-01-08 00:00:00', 45: '2011-06-26 00:00:00', 46: '2011-08-31 00:00:00',
    47: '2011-09-02 00:00:00',  # 2W: Fri 2011-09-02 (by hand)
    48: '2011-06-01 00:00:00',  # 1W: Wed 2011-06-01 (by hand)
    49: '2011-09-01 00:00:00',  # 1W: Thu 2011-09-01 (by hand)
    50: '2011-08-03 00:00:00',  # 3W: Wed 2011-08-03 (by hand)
    51: '2011-08-16 00:00:00',  # 16W: Tue 2011-08-16 (by hand)
    52: '2011-08-29 00:00:00',  # 28W: Sun 2011-08-28 moves to Mon 08-29 (by hand)
    53: '2011-08-30 00:00:00',  # 30W: Tue 2011-08-30 (by hand)
    54: '2011-08-31 00:00:00',  # 31W: Wed 2011-08-31 (by hand)
    55: '2011-12-30 00:00:00',  # 31W: November has no 31st; Sat 2011-12-31 moves to Fri 12-30 (by hand)
    56: '2011-04-29 00:00:00',  # 30W: Sat 2011-04-30 moves to Fri 04-29 (by hand)
    57: '2012-01-01 00:01:00',  # five fields plus the year 2012: the next minute (by hand)
    58: '2011-07-29 00:01:00', 59: '2011-07-30 00:01:00', 60: '2011-07-31 00:01:00', 61: '2011-07-25 00:01:00',
    62: '2011-07-26 00:01:00',  # TUEL, minute *: the next minute of the same day (by hand)
    63: '2012-01-27 00:01:00', 64: '2011-07-08 00:01:00', 65: '2011-07-01 00:02:00', 66: '2011-07-27 00:01:00',
    67: '2009-12-07 00:00:00', 68: '2010-01-04 00:00:00', 70: '2018-08-13 00:45:00', 71: '2018-08-13 01:25:00',
    72: '2018-08-13 00:09:00', 73: '2011-06-26 00:00:00', 74: '2017-01-31 06:18:00', 75: '2017-01-09 00:01:00',
    76: '2017-01-10 00:01:00', 77: '2020-07-03 01:00:00', 78: '2020-07-03 01:00:00', 79: '2020-07-02 01:01:00',
    80: '2019-11-24 00:00:00', 81: '2019-11-24 00:00:00', 82: '2019-11-24 00:00:00',  # @weekly (by hand)
    83: '2020-08-21 12:00:00', 84: '2020-08-21 12:00:00',
    85: '2020-08-20 00:00:02',  # * ? * ? * *: ? is * in gronx, every second (by hand)
    86: '2022-01-01 00:00:01',  # seven fields, year */2: every second of 2022 (by hand)
    87: '2021-08-20 00:00:02',  # seven fields, year *: every second
    88: '2023-01-01 00:00:01',  # seven fields, years 2023-2099 (by hand)
    89: '2023-07-31 09:30:00', 90: '2023-10-31 09:30:00',
    91: '2020-01-01 00:01:00',  # seven fields, second 0, year */2 (by hand)
    92: '2019-05-01 09:36:00',
}

# The bespoke tests, transcribed by hand:
# (name, kind, format, expression, from, expected, expectation, flags, reference).
HAND_ROWS = [
    # gronx_test.go TestNormalize (L31-48); the newline row (L33) is left out.
    ('TestNormalize-01', 'equal', 'standard', '* * * * * 2021', '', '* * * * * 2021', 'upstream', 'year-field',
     'gronx_test.go TestNormalize L34; normalize keeps the year field'),
    ('TestNormalize-02', 'equal', 'standard', '@hourly', '', '0 * * * *', 'upstream', '',
     'gronx_test.go TestNormalize L35'),
    ('TestNormalize-03', 'equal', 'standard', '0 0 JAN,feb * sun,MON', '', '0 0 1,2 * 0,1', 'upstream',
     'syntax-extension', 'gronx_test.go TestNormalize L36; gronx replaces names in every field, here month names in day-of-month'),
    # gronx_test.go TestIsValid (L50-124).
    ('TestIsValid-01', 'valid', 'standard', '5,10-20/4,55 * * * *', '', '', 'upstream', '', 'gronx_test.go TestIsValid L54'),
    ('TestIsValid-02', 'valid', 'standard', '00 * * * *', '', '', 'upstream', '', 'gronx_test.go TestIsValid L57; leading zero'),
    ('TestIsValid-03', 'valid', 'standard', '* 00 * * *', '', '', 'upstream', '', 'gronx_test.go TestIsValid L60; leading zero'),
    ('TestIsValid-04', 'invalid', 'standard', 'A-B * * * *', '', '', 'upstream', '', 'gronx_test.go TestIsValid L69'),
    ('TestIsValid-05', 'invalid', 'standard', '60 * * * *', '', '', 'upstream', '', 'gronx_test.go TestIsValid L72'),
    ('TestIsValid-06', 'invalid', 'standard', '* 30 * * *', '', '', 'upstream', '', 'gronx_test.go TestIsValid L75'),
    ('TestIsValid-07', 'invalid', 'standard', '* * 99 * *', '', '', 'upstream', '', 'gronx_test.go TestIsValid L78'),
    ('TestIsValid-08', 'invalid', 'standard', '* * * 13 *', '', '', 'upstream', '', 'gronx_test.go TestIsValid L81'),
    ('TestIsValid-09', 'invalid', 'standard', '* * * * 8', '', '', 'upstream', '', 'gronx_test.go TestIsValid L84'),
    ('TestIsValid-10', 'invalid', 'standard', '60-65 * * * *', '', '', 'upstream', '', 'gronx_test.go TestIsValid L88'),
    ('TestIsValid-11', 'invalid', 'standard', '* 24-28/2 * * *', '', '', 'upstream', '', 'gronx_test.go TestIsValid L91'),
    ('TestIsValid-12', 'invalid', 'standard', '* * * *', '', '', 'upstream', '', 'gronx_test.go TestIsValid L94'),
    ('TestIsValid-13', 'invalid', 'standard', '0-0/-005 * * * *', '', '', 'upstream', '',
     'gronx_test.go TestIsValid L97; negative step (#45)'),
    ('TestIsValid-14', 'invalid', 'standard', '*/15, * * * *', '', '', 'upstream', '',
     'gronx_test.go TestIsValid L102-123; empty list item, asserted at five reference times (#60)'),
    # gronx_test.go TestIsDue, "seconds precision" (L161-173).
    ('TestIsDue-01', 'next-inclusive', 'withSeconds', '*/2 * * * * *', '2020-02-02T02:02:04', '2020-02-02T02:02:04',
     'upstream', '', 'gronx_test.go TestIsDue L162-167; IsDue at an even second is true'),
    ('TestIsDue-02', 'next-inclusive', 'withSeconds', '*/2 * * * * *', '2020-02-02T02:02:05', '2020-02-02T02:02:06',
     'cronsim', '', 'gronx_test.go TestIsDue L169-172; IsDue one second later is false, so the inclusive next is the next even second'),
    # next_test.go TestNextTickAfter, bespoke subtests (L32-63).
    ('TestNextTickAfter-01', 'next', 'withSeconds', '*/5 * * * * *', '2020-02-02T02:02:02', '2020-02-02T02:02:05',
     'upstream', '', 'next_test.go TestNextTickAfter L32-38 seconds precision'),
    ('TestNextTickAfter-02', 'next', 'standard', '0 0 31 * *', '2021-01-31T00:00:00', '2021-03-31T00:00:00', 'upstream', '',
     'next_test.go TestNextTickAfter L40-50 (#71); the test compares the date, 00:00 is the expression\'s time'),
    ('TestNextTickAfter-03', 'next', 'standard', '0 0 * * 4-7/3', '2032-05-01T00:00:00', '2032-05-02T00:00:00', 'upstream',
     '', 'next_test.go TestNextTickAfter L52-63 (#72); 4-7/3 is Thursday and Sunday; the test compares the date'),
    # next_test.go TestIsUnreachableYearPrevTickBefore (L102-147) and TestIsUnreachableYearNextTickAfter (L149-194).
    ('TestIsUnreachableYearPrevTickBefore-01', 'previous-inclusive', 'standard', '30 15 4 11 * 2024',
     '2024-11-08T22:18:16', '2024-11-04T15:30:00', 'upstream', 'year-field',
     'next_test.go L110-116 (#51); a four-digit sixth field is a year after five fields; PrevTickBefore(now, true)'),
    ('TestIsUnreachableYearPrevTickBefore-02', 'unreachable-previous', 'standard', '30 15 4 11 * 2025',
     '2024-11-08T22:18:16', '', 'upstream', 'year-field', 'next_test.go L117-122; the unreachable year segment error'),
    ('TestIsUnreachableYearPrevTickBefore-03', 'previous-inclusive', 'standard', '30 15 4 11 * 2023',
     '2024-11-08T22:18:16', '2023-11-04T15:30:00', 'upstream', 'year-field', 'next_test.go L123-128'),
    ('TestIsUnreachableYearNextTickAfter-01', 'next', 'standard', '30 15 31 12 * 2024', '2024-11-08T22:18:16',
     '2024-12-31T15:30:00', 'upstream', 'year-field', 'next_test.go L157-163 (#53)'),
    ('TestIsUnreachableYearNextTickAfter-02', 'next', 'standard', '30 15 31 12 * 2025', '2024-11-08T22:18:16',
     '2025-12-31T15:30:00', 'upstream', 'year-field', 'next_test.go L164-169'),
    ('TestIsUnreachableYearNextTickAfter-03', 'unreachable-next', 'standard', '30 15 31 12 * 2023', '2024-11-08T22:18:16',
     '', 'upstream', 'year-field', 'next_test.go L170-175; the unreachable year segment error'),
    # prev_test.go TestPrevTick (L10-33) and TestPrevTickBefore "seconds precision" (L37-44).
    ('TestPrevTick-01', 'previous-inclusive', 'withSeconds', '* * * * * *', '2020-02-02T02:02:02', '2020-02-02T02:02:02',
     'upstream', '', 'prev_test.go TestPrevTick L13-17'),
    ('TestPrevTick-02', 'previous', 'withSeconds', '* * * * * *', '2020-02-02T02:02:02', '2020-02-02T02:02:01',
     'upstream', '', 'prev_test.go TestPrevTick L26-32'),
    ('TestPrevTickBefore-01', 'previous', 'withSeconds', '*/5 * * * * *', '2020-02-02T02:02:05', '2020-02-02T02:02:00',
     'upstream', '', 'prev_test.go TestPrevTickBefore L37-44; from is NextTickAfter(2020-02-02 02:02:02) = 02:02:05'),
]

# checker_test.go TestDOWRangeTrailingSeven (L13-140): IsDue on 2023-01-01 (Sunday) to 2023-01-07 (Saturday) at
# 00:00. A due day is a next-inclusive row equal to itself; a day that is not due gives the next occurrence.
DAY = {'Sun': '2023-01-01', 'Mon': '2023-01-02', 'Tue': '2023-01-03', 'Wed': '2023-01-04', 'Thu': '2023-01-05',
       'Fri': '2023-01-06', 'Sat': '2023-01-07'}
CHECKER_DUE = [
    # (line, expression, day, due, next when not due, expectation of that next, flags)
    (39, '0 0 * * 5-7', 'Sun', True, '', '', ''),
    (43, '0 0 * * 6-7', 'Sun', True, '', '', ''),
    (47, '0 0 * * 7-7', 'Sun', True, '', '', ''),
    (50, '0 0 * * 7-7', 'Mon', False, '2023-01-08', 'cronsim', ''),
    (62, '0 0 * * 5-7', 'Fri', True, '', '', ''),
    (65, '0 0 * * 5-7', 'Sat', True, '', '', ''),
    (69, '0 0 * * 7-4', 'Sun', True, '', '', 'wrap-range'),
    (72, '0 0 * * 7-4', 'Thu', True, '', '', 'wrap-range'),
    (75, '0 0 * * 7-4', 'Fri', False, '2023-01-08', 'derived', 'wrap-range'),
    (80, '0 0 * * 0-7', 'Sun', True, '', '', ''),
    (80, '0 0 * * 0-7', 'Mon', True, '', '', ''),
    (80, '0 0 * * 0-7', 'Tue', True, '', '', ''),
    (80, '0 0 * * 0-7', 'Wed', True, '', '', ''),
    (80, '0 0 * * 0-7', 'Thu', True, '', '', ''),
    (80, '0 0 * * 0-7', 'Fri', True, '', '', ''),
    (80, '0 0 * * 0-7', 'Sat', True, '', '', ''),
    (85, '0 0 * * 7', 'Sun', True, '', '', ''),
    (89, '0 0 * * 5,6,7', 'Sun', True, '', '', ''),
]
CHECKER_OTHER = [
    ('equal', '0 0 * * 5-7', '', '0 0 * * 5,6,7', 'L99; range and list agree on every weekday'),
    ('equal', '0 0 * * 6-7', '', '0 0 * * 6,0', 'L100'),
    ('equal', '0 0 * * 7-7', '', '0 0 * * 0', 'L101'),
    ('equal', '0 0 * * 0-7', '', '0 0 * * 0,1,2,3,4,5,6', 'L102'),
    ('next', '0 0 * * 5-7', '2023-01-07T12:00:00', '2023-01-08T00:00:00', 'L117-130; NextTickAfter reaches Sunday (#69)'),
    ('next', '0 0 * * 5,6,7', '2023-01-07T12:00:00', '2023-01-08T00:00:00',
     'L131-138; the list form must give the same next tick as the range'),
]


def csv_field(value: str) -> str:
    if any(c in value for c in ',"\t') or value != value.strip():
        return '"' + value.replace('"', '""') + '"'
    return value


def iso(text: str) -> str:
    return text.replace(' ', 'T')


def classify(expr: str) -> tuple[str, str, str, str]:
    """Returns (format, Bodu expression, flags, adaptation note) for a testcases expression."""
    fields = expr.split()
    if expr.startswith('@'):
        return 'standard', expr, ('' if expr == '@weekly' else 'macro'), ''
    if len(fields) == 7:
        if fields[6] == '*':
            return 'withSeconds', ' '.join(fields[:6]), '', 'seven fields with year *, year dropped'
        return 'withSeconds', expr, 'year-field', ''
    if len(fields) == 6 and len(fields[5]) == 4 and fields[5].isdigit():
        return 'standard', expr, 'year-field', 'a four-digit sixth field is a year after five fields'
    fmt = 'withSeconds' if len(fields) == 6 else 'standard'
    if len(fields) == 6 and '?' in (fields[0], fields[1], fields[2], fields[4]):
        return fmt, expr, 'syntax-extension', '? outside the day fields'
    if fields[-1].startswith('7-'):
        return fmt, expr, 'wrap-range', 'a weekday range starting at 7'
    return fmt, expr, '', ''


def testcase_rows(text: str) -> list[list[str]]:
    rows: list[list[str]] = []
    lines = text.splitlines()
    start = next(i for i, line in enumerate(lines) if line.startswith('func testcases()'))
    index = 0
    for number in range(start + 1, len(lines) + 1):
        line = lines[number - 1]
        if line == '}':
            break
        match = re.fullmatch(r'\t\t\{"(.*?)", "(.*?)", (true|false), "(.*?)"\},', line)
        if not match:
            continue
        index += 1
        expr, ref, due, nxt = match.groups()
        fmt, bodu, flags, note = classify(expr)
        tag = f'testcases-{index:02d}'
        base = f'gronx_test.go testcases L{number}' + (f'; {note}' if note else '')
        due_ref = (f'{base}; IsDue(ref) is {due} (TestIsDue L175-183) and NextTickAfter(ref, true) == ref is {due} '
                   f'(next_test.go L65-75)')
        if re.search(r'/0(?![0-9])', expr):
            # IsDue returns false with the step-0 error, and NextTickAfter fails, so the next column is never compared.
            rows.append([f'{tag}-due', 'invalid', fmt, bodu, '', '', 'upstream', flags,
                         f'{base}; IsDue is false with a step-0 parse error and NextTickAfter fails (the next column is unused)'])
            continue
        if nxt == 'err':
            rows.append([f'{tag}-due', 'unreachable-next', fmt, bodu, iso(ref), '', 'upstream', flags,
                         f'{base}; IsDue is false and NextTickAfter returns the unreachable year error'])
            continue
        if due == 'true':
            rows.append([f'{tag}-due', 'next-inclusive', fmt, bodu, iso(ref), iso(ref), 'upstream', flags, due_ref])
        else:
            rows.append([f'{tag}-due', 'next-inclusive', fmt, bodu, iso(ref), iso(nxt), 'upstream', flags,
                         due_ref + '; not due, so the inclusive next is the exclusive one'])
        rows.append([f'{tag}-next', 'next', fmt, bodu, iso(ref), iso(nxt), 'upstream', flags,
                     f'{base}; NextTickAfter(ref, false) (next_test.go L78-97)'])
        rows.append([f'{tag}-prev-incl', 'previous-inclusive', fmt, bodu, iso(nxt), iso(nxt), 'upstream', flags,
                     f'{base}; PrevTickBefore(next, true) == next (prev_test.go L46-64)'])
        rows.append([f'{tag}-prev', 'previous', fmt, bodu, iso(NEXT2[index]), iso(nxt), 'upstream', flags,
                     f'{base}; PrevTickBefore(next2, false) == next, next2 = NextTickAfter(next, false) recomputed '
                     f'(prev_test.go L66-77)'])
    return rows


def errcase_rows(text: str) -> list[list[str]]:
    rows: list[list[str]] = []
    lines = text.splitlines()
    start = next(i for i, line in enumerate(lines) if line.startswith('func errcases()'))
    index = 0
    for number in range(start + 1, len(lines) + 1):
        line = lines[number - 1]
        if line == '}':
            break
        match = re.fullmatch(r'\t\t\{"(.*?)", "(.*?)", false, ""\},', line)
        if match:
            index += 1
            expr = match.group(1)
            fmt = 'standard' if len(expr.split()) <= 5 else 'withSeconds'
            rows.append([f'errcases-{index:02d}', 'invalid', fmt, expr, '', '', 'upstream', '',
                         f'gronx_test.go errcases L{number}; IsDue returns false with an error (TestIsDue L185-196)'])
    return rows


def checker_rows() -> list[list[str]]:
    rows: list[list[str]] = []
    counter = 0
    for line, expr, day, due, nxt, expectation, flags in CHECKER_DUE:
        counter += 1
        start = f'{DAY[day]}T00:00:00'
        ref = f'checker_test.go TestDOWRangeTrailingSeven L{line}; IsDue({day} {DAY[day]}) is {str(due).lower()}'
        if due:
            rows.append([f'TestDOWRangeTrailingSeven-{counter:02d}', 'next-inclusive', 'standard', expr, start, start,
                         'upstream', flags, ref])
        else:
            why = ('7-4 is Sunday to Thursday in gronx, so the next is Sunday' if flags else
                   'only Sundays match, so the inclusive next is the following Sunday')
            rows.append([f'TestDOWRangeTrailingSeven-{counter:02d}', 'next-inclusive', 'standard', expr, start,
                         f'{nxt}T00:00:00', expectation, flags, f'{ref}; {why}'])
    for kind, expr, start, expected, where in CHECKER_OTHER:
        counter += 1
        rows.append([f'TestDOWRangeTrailingSeven-{counter:02d}', kind, 'standard', expr, start, expected, 'upstream', '',
                     f'checker_test.go TestDOWRangeTrailingSeven {where}'])
    return rows


def main(argv: list[str]) -> int:
    if len(argv) != 5:
        print(__doc__, file=sys.stderr)
        return 2
    texts = {}
    for path in argv[1:]:
        name = path.replace('\\', '/').rsplit('/', 1)[-1]
        data = open(path, 'rb').read()
        digest = hashlib.sha256(data).hexdigest()
        if SHA256.get(name) != digest:
            print(f'{path}: SHA-256 {digest} is not the recorded {SHA256.get(name)}', file=sys.stderr)
            return 1
        texts[name] = data.decode('utf-8')

    rows = testcase_rows(texts['gronx_test.go'])
    rows += errcase_rows(texts['gronx_test.go'])
    rows += [list(r) for r in HAND_ROWS]
    rows += checker_rows()

    out = sys.stdout
    out.write(HEADER.format(**{name[:-3]: digest for name, digest in SHA256.items()}))
    out.write(COLUMNS + '\n')
    for row in rows:
        out.write(','.join(csv_field(v) for v in row) + '\n')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
