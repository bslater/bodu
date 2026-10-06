#!/usr/bin/env python3
"""Derive a Bodu cron vector table from robfig/cron's ``spec_test.go`` and ``parser_test.go``.

robfig/cron (https://github.com/robfig/cron, MIT) is the most widely used Go cron library. Its schedule tests are
tables: ``TestActivation`` lists {time, spec, bool} rows asserting ``Next(t - 1s) == t``, ``TestNext`` and
``TestNextWithTz`` list {from, spec, expected} rows, and ``TestErrors`` lists rejected specs. ``parser_test.go``
holds the parser's own tables (field bit sets, error rows and parsed schedules). This script reads each table and
writes one row of the PR G vector schema per assertion.

Usage:

    python3 extract-robfig-cron-vectors.py <spec_test.go> <parser_test.go> > robfig-cron-vectors.csv

The script checks both files' SHA-256 against the commit the table records and writes the table to stdout. The
output is deterministic.

How the assertions map:

* ``TestActivation`` row (time t, spec, true): ``next`` from t - 1s is t. A ``false`` row asserts only that the
  answer is not t, so its expected value is the actual next occurrence, taken from cronsim or worked out by hand
  (the ACTIVATION_NEXT table below, with the reasoning).
* ``TestNext``: only the zone-free rows; the rows that name a time zone (``TZ=``, ``CRON_TZ=``) are counted and left
  out. A zero ``time.Time`` (no occurrence) is ``unreachable-next``.
* robfig counts a stepped star (``*/5``) in a day field as restricted since #70, so it unions it with the other day
  field, where Bodu (with Vixie cron and cronsim) intersects. Rows whose answer depends on that are restated with
  cronsim's answer (RESTATED below); rows whose answer is the same either way keep robfig's value and say so.
* ``parser_test.go`` tests the field parser with arbitrary bounds. A row whose result does not depend on those
  bounds is placed in the minute field (``equal`` or ``to-string`` against the bit set it asserts); a row whose
  result depends on bounds no cron field has (``*`` and ``*/2`` on 1-3, and the below/above-range errors on 3-5) is
  left out.
"""

from __future__ import annotations

import hashlib
import re
import sys
from datetime import datetime, timedelta

SPEC_SHA256 = 'daca39ba173c7612030c8d9b372312acbf3d5149e1db7d60a862e82ab23497e1'
PARSER_SHA256 = '3a3f0801d58fed2eb64b5c4809eb6bff48c24fa85bfc28003118f9f84bfa9864'

HEADER = """\
# Cron vectors derived from robfig/cron's spec_test.go and parser_test.go
# source-class: third-party-comparison (implementation test suite, not an authority)
# source: robfig/cron -- https://github.com/robfig/cron -- spec_test.go, parser_test.go
# commit: bc59245fe10efaed9d51b56900192527ed733435
# file-sha256: spec_test.go {spec}
# file-sha256: parser_test.go {parser}
# licence: MIT (Copyright (C) 2012 Rob Figueiredo). The rows restate the files' assertions as expressions and instants, with attribution; the upstream files themselves are not committed.
# method: extracted by extract-robfig-cron-vectors.py
# note: robfig reads five fields (ParseStandard) or six with seconds first (TestNext's secondParser); weekdays 0-6, ? in any field, N/step to the field maximum (its MON/2 is 1-6/2, where Bodu's runs to 7 and adds Sunday). Since #70 a stepped star (*/5) in a day field counts as restricted and unions; Bodu intersects, so those rows are restated with cronsim's answer. It also has @every and TZ=/CRON_TZ= prefixes.
# note: left out (counted): {tz} TestNext rows that name a time zone, 5 TestParseSchedule rows with a CRON_TZ=/TZ= prefix, 4 TestRange rows whose result depends on bounds no cron field has, TestNormalizeFields (7) and TestNormalizeFields_Errors (4) (parser-option API); cron_test.go (scheduler) is not a source.
# flags: macro = @every, a macro outside crontab(5)'s seven
"""

COLUMNS = 'name,kind,format,expression,from,expected,expectation,flags,reference'

# TestActivation rows that assert false: robfig asserts only that Next(t - 1s) is not t. The expected value is the
# actual next occurrence, (instant, expectation, reasoning).
ACTIVATION_NEXT = {
    'TestActivation-03': ('2012-07-09T15:45:00', 'cronsim', 'minutes 0,15,30,45: the next after 15:39:59 is 15:45'),
    'TestActivation-08': ('2013-06-01T00:00:00', 'cronsim', 'June only: the next June minute after 2012-07-15 is 2013-06-01 00:00'),
    'TestActivation-11': ('2012-07-22T08:30:00', 'derived', '? reads as *: the next July Sunday at 08:30 after Mon 2012-07-16 is 2012-07-22'),
    'TestActivation-12': ('2013-07-15T08:30:00', 'derived', '? reads as *: the next 15 July at 08:30 after 2012-07-16 is in 2013'),
    'TestActivation-14': ('2012-07-09T16:00:00', 'derived', '@hourly is 0 * * * *: the next after 15:03:59 is 16:00'),
    'TestActivation-15': ('2012-07-10T00:00:00', 'derived', '@daily is 0 0 * * *: the next after 14:59:59 is midnight'),
    'TestActivation-17': ('2012-07-15T00:00:00', 'derived', '@weekly is 0 0 * * 0: the next Sunday midnight after Sun 2012-07-08 23:59:59'),
    'TestActivation-19': ('2012-07-15T00:00:00', 'derived', '@weekly is 0 0 * * 0: Sunday 00:00 already passed at 00:59:59, so the next Sunday'),
    'TestActivation-20': ('2012-08-01T00:00:00', 'derived', '@monthly is 0 0 1 * *: the next first of the month after 2012-07-07'),
    'TestActivation-26': ('2012-07-16T00:00:00', 'cronsim', 'Mondays only: the next after Sat 2012-07-14 23:59:59 is Mon 2012-07-16'),
    'TestActivation-27': ('2012-07-15T00:00:00', 'cronsim', 'days 1 and 15: the next after 2012-07-08 23:59:59 is 2012-07-15'),
}

# Rows restated in Bodu's dialect: (instant, expectation, reasoning).
RESTATED = {
    'TestActivation-25': ('2012-10-21T00:00:00', 'cronsim',
                          'robfig unions */10 with Sun since #70 and asserts Sun 2012-07-15 matches; Bodu intersects a star-led day field, so the next is the first Sunday on day 1/11/21/31, 2012-10-21'),
    'TestNext-14': ('2012-08-06T00:00:00', 'cronsim',
                    'robfig unions */5 with Mon since #70 and answers Wed 2012-08-01; Bodu intersects a star-led day field: Mon 2012-08-06 (day 6)'),
}

# Rows that keep robfig's value although the dialects differ, because the answer is the same: name -> note.
SAME_ANSWER = {
    'TestActivation-29': 'robfig unions */2 with Sun, Bodu intersects; Sun 2012-07-15 is an odd day, so it matches either way',
    'TestNext-15': 'robfig unions */5 with Mon, Bodu intersects; Mon 2012-10-01 is day 1, so the answer is the same',
    'TestNext-17': 'robfig reads Mon/2 as Mon,Wed,Fri and Bodu as Mon,Wed,Fri,Sun; Fri 2013-02-01 is the answer either way',
}

FIELD_POSITION = {'minutes': (0, 0, 59), 'hours': (1, 0, 23), 'dom': (2, 1, 31), 'months': (3, 1, 12), 'dow': (4, 0, 6)}


def csv_field(value: str) -> str:
    if any(c in value for c in ',"\t') or value != value.strip():
        return '"' + value.replace('"', '""') + '"'
    return value


def go_time(text: str) -> datetime:
    for layout in ('%a %b %d %H:%M %Y', '%a %b %d %H:%M:%S %Y'):
        try:
            return datetime.strptime(text, layout)
        except ValueError:
            pass
    raise SystemExit(f'unparsed time {text!r}')


def iso(value: datetime) -> str:
    return value.strftime('%Y-%m-%dT%H:%M:%S')


def offset_iso(text: str) -> str:
    """Rewrites robfig's 2016-01-03T13:09:03+0530 as 2016-01-03T13:09:03+05:30."""
    return text[:-2] + ':' + text[-2:]


def bits(expression: str) -> set[int]:
    """Evaluates a Go bit-set literal such as ``1<<5 | 1<<7`` or ``zero``."""
    if expression.strip() == 'zero':
        return set()
    values = set()
    for term in expression.split('|'):
        match = re.fullmatch(r'\s*1\s*<<\s*(\d+)\s*', term)
        if not match:
            raise SystemExit(f'unexpected bit literal {expression!r}')
        values.add(int(match.group(1)))
    return values


def mask_bits(mask: int) -> set[int]:
    return {i for i in range(64) if mask >> i & 1}


def set_text(values: set[int]) -> str:
    return ','.join(str(v) for v in sorted(values))


def minute_row(name: str, field: str, values: set[int], ref: str) -> list[str]:
    """A bit-set assertion placed in the minute field: to-string when the text is already the list, else equal."""
    listed = set_text(values)
    if field == listed:
        return [name, 'to-string', 'standard', f'{field} * * * *', '', f'{listed} * * * *', 'upstream', '', ref]
    return [name, 'equal', 'standard', f'{field} * * * *', '', f'{listed} * * * *', 'upstream', '', ref]


def lines_of(text: str, function: str) -> list[tuple[int, str]]:
    """Returns the numbered lines of one top-level Go function."""
    out, inside = [], False
    for number, line in enumerate(text.splitlines(), start=1):
        if line.startswith(f'func {function}('):
            inside = True
        elif inside and line == '}':
            break
        if inside:
            out.append((number, line))
    return out


def spec_rows(text: str) -> tuple[list[list[str]], int]:
    rows: list[list[str]] = []

    counter = 0
    for number, line in lines_of(text, 'TestActivation'):
        match = re.fullmatch(r'\t\t\{"([^"]*)", "([^"]*)", (true|false)\},(?:\s*//.*)?', line)
        if not match:
            continue
        counter += 1
        name = f'TestActivation-{counter:02d}'
        when, spec, truth = match.groups()
        t = go_time(when)
        start = iso(t - timedelta(seconds=1))
        ref = f'TestActivation L{number}; Next(t - 1s) == t is {truth}'
        if name in RESTATED:
            expected, expectation, why = RESTATED[name]
            ref += f'; restated: {why}'
        elif truth == 'true':
            expected, expectation = iso(t), 'upstream'
            if name in SAME_ANSWER:
                ref += f'; {SAME_ANSWER[name]}'
        else:
            expected, expectation, why = ACTIVATION_NEXT[name]
            ref += f'; robfig asserts only that the answer is not t, so the expected value is the next occurrence: {why}'
        rows.append([name, 'next', 'standard', spec, start, expected, expectation, '', ref])

    counter, left_out = 0, 0
    for number, line in lines_of(text, 'TestNext'):
        match = re.fullmatch(r'\t\t\{"([^"]*)", "([^"]*)", "([^"]*)"\},', line)
        if not match:
            continue
        when, spec, expected = match.groups()
        if 'TZ=' in when or 'TZ=' in spec:
            left_out += 1
            continue
        counter += 1
        name = f'TestNext-{counter:02d}'
        start = iso(go_time(when))
        ref = f'TestNext L{number}; secondParser (seconds first)'
        if not expected:
            rows.append([name, 'unreachable-next', 'withSeconds', spec, start, '', 'upstream', '',
                         ref + '; robfig returns the zero time.Time (no occurrence within its 5-year search)'])
            continue
        value, expectation = iso(go_time(expected)), 'upstream'
        if name in RESTATED:
            value, expectation, why = RESTATED[name]
            ref += f'; restated: {why}'
        elif name in SAME_ANSWER:
            ref += f'; {SAME_ANSWER[name]}'
        rows.append([name, 'next', 'withSeconds', spec, start, value, expectation, '', ref])

    counter = 0
    for number, line in lines_of(text, 'TestErrors'):
        match = re.fullmatch(r'\t\t"([^"]*)",', line)
        if match:
            counter += 1
            rows.append([f'TestErrors-{counter:02d}', 'invalid', 'standard', match.group(1), '', '', 'upstream', '',
                         f'TestErrors L{number}; ParseStandard'])

    counter = 0
    for number, line in lines_of(text, 'TestNextWithTz'):
        match = re.fullmatch(r'\t\t\{"([^"]*)", "([^"]*)", "([^"]*)"\},', line)
        if match:
            counter += 1
            when, spec, expected = match.groups()
            rows.append([f'TestNextWithTz-{counter:02d}', 'next', 'standard', spec, offset_iso(when),
                         offset_iso(expected), 'upstream', '',
                         f'TestNextWithTz L{number}; a fixed +05:30 offset, read through the DateTimeOffset overload'])

    # TestSlash0NoHang (L294-300): the TZ= prefix has no bearing on the rejection of the 0 step (#144), so it is
    # dropped; Bodu has no TZ= prefix.
    rows.append(['TestSlash0NoHang-01', 'invalid', 'standard', '15/0 * * * *', '', '', 'upstream', '',
                 'TestSlash0NoHang L295-298; "TZ=America/New_York 15/0 * * * *" with the TZ= prefix dropped'])
    return rows, left_out


def parser_rows(text: str) -> list[list[str]]:
    rows: list[list[str]] = []

    counter = 0
    for number, line in lines_of(text, 'TestRange'):
        match = re.fullmatch(r'\t\t\{"([^"]*)", (\d+), (\d+), ([^,]*), "([^"]*)"\},', line)
        if not match:
            continue
        expr, low, high, expected, error = match.groups()
        low, high = int(low), int(high)
        bounds_dependent = expr in ('*', '*/2') or error in ('below minimum', 'above maximum')
        if bounds_dependent:
            continue
        counter += 1
        name = f'TestRange-{counter:02d}'
        ref = f'TestRange L{number}; getRange with bounds {low}-{high}, placed in the minute field'
        if error:
            if expr.lower().startswith('jan'):
                rows.append([name, 'invalid', 'standard', f'* * * {expr} *', '', '', 'upstream', '',
                             f'TestRange L{number}; "{error}"; placed in the month field, where jan is a name'])
            else:
                rows.append([name, 'invalid', 'standard', f'{expr} * * * *', '', '', 'upstream', '',
                             f'{ref}; "{error}"'])
        else:
            rows.append(minute_row(name, expr, bits(expected), ref))

    counter = 0
    for number, line in lines_of(text, 'TestField'):
        match = re.fullmatch(r'\t\t\{"([^"]*)", (\d+), (\d+), ([^}]*)\},', line)
        if match:
            counter += 1
            expr, low, high, expected = match.groups()
            rows.append(minute_row(f'TestField-{counter:02d}', expr, bits(expected),
                                   f'TestField L{number}; getField with bounds {low}-{high}, placed in the minute field'))

    counter = 0
    for number, line in lines_of(text, 'TestAll'):
        match = re.fullmatch(r'\t\t\{(\w+), (0x[0-9a-f]+)\},.*', line)
        if match:
            counter += 1
            field, mask = match.group(1), int(match.group(2), 16)
            position, low, high = FIELD_POSITION[field]
            if mask_bits(mask) != set(range(low, high + 1)):
                raise SystemExit(f'TestAll L{number}: mask does not cover {low}-{high}')
            fields = ['*'] * 5
            fields[position] = f'{low}-{high}'
            rows.append([f'TestAll-{counter:02d}', 'equal', 'standard', '* * * * *', '', ' '.join(fields), 'upstream', '',
                         f'TestAll L{number}; all({field}) sets every value {low}-{high}'])

    counter = 0
    for number, line in lines_of(text, 'TestBits'):
        match = re.fullmatch(r'\t\t\{(\d+), (\d+), (\d+), (0x[0-9a-f]+)\},.*', line)
        if match:
            counter += 1
            low, high, step, mask = match.groups()
            rows.append(minute_row(f'TestBits-{counter:02d}', f'{low}-{high}/{step}', mask_bits(int(mask, 16)),
                                   f'TestBits L{number}; getBits({low}, {high}, {step}) as the minute range'))

    counter = 0
    for number, line in lines_of(text, 'TestParseScheduleErrors'):
        match = re.fullmatch(r'\t\t\{"([^"]*)", "([^"]*)"\},', line)
        if match:
            counter += 1
            expr, error = match.groups()
            macro = expr.startswith('@')
            flags = 'macro' if expr.startswith('@every') else ''
            rows.append([f'TestParseScheduleErrors-{counter:02d}', 'invalid', 'standard' if macro else 'withSeconds',
                         expr, '', '', 'upstream', flags, f'TestParseScheduleErrors L{number}; secondParser; "{error}"'])

    # TestParseSchedule (L140-182), TestOptionalSecondSchedule (L184-204), TestStandardSpecSchedule (L315-351) and
    # TestNoDescriptorParser (L353-359) compare parsed schedule structs; the struct's field sets are written as the
    # canonical text (to-string) or as the equivalent expression (equal). Rows with a time zone prefix are left out.
    hand = [
        ('TestParseSchedule-01', 'to-string', 'withSeconds', '0 5 * * * *', '0 5 * * * *', '', 'L147; secondParser; every5min: second 0, minute 5'),
        ('TestParseSchedule-02', 'to-string', 'standard', '5 * * * *', '5 * * * *', '', 'L148; standardParser; every5min'),
        ('TestParseSchedule-03', 'valid', 'standard', '@every 5m', '', 'macro', 'L152; ConstantDelaySchedule{5m}'),
        ('TestParseSchedule-04', 'equal', 'standard', '@midnight', '0 0 * * *', '', 'L153; secondParser midnight: second 0, minute 0, hour 0; Bodu macros are five-field'),
        ('TestParseSchedule-05', 'equal', 'standard', '@yearly', '0 0 1 1 *', '', 'L156; annual: minute 0, hour 0, day 1, month 1'),
        ('TestParseSchedule-06', 'equal', 'standard', '@annually', '0 0 1 1 *', '', 'L157; annual'),
        ('TestParseSchedule-07', 'to-string', 'withSeconds', '* 5 * * * *', '* 5 * * * *', '', 'L158-170; SpecSchedule{all seconds, minute 5, all others}'),
        ('TestOptionalSecondSchedule-01', 'to-string', 'withSeconds', '0 5 * * * *', '0 5 * * * *', '', 'L190; SecondOptional parser; every5min'),
        ('TestOptionalSecondSchedule-02', 'to-string', 'withSeconds', '5 5 * * * *', '5 5 * * * *', '', 'L191; every5min5s: second 5, minute 5'),
        ('TestOptionalSecondSchedule-03', 'to-string', 'standard', '5 * * * *', '5 * * * *', '', 'L192; seconds omitted: every5min'),
        ('TestStandardSpecSchedule-01', 'to-string', 'standard', '5 * * * *', '5 * * * *', '', 'L321-324; ParseStandard; second 0, minute 5'),
        ('TestStandardSpecSchedule-02', 'valid', 'standard', '@every 5m', '', 'macro', 'L325-328; ConstantDelaySchedule{5m}'),
        ('TestStandardSpecSchedule-03', 'invalid', 'standard', '5 j * * *', '', '', 'L329-332; "failed to parse int from"'),
        ('TestStandardSpecSchedule-04', 'invalid', 'standard', '* * * *', '', '', 'L333-336; "expected exactly 5 fields"'),
        ('TestNoDescriptorParser-01', 'invalid', 'standard', '@every 1m', '', 'macro', 'L353-359; a parser without the Descriptor option rejects @every'),
    ]
    for name, kind, fmt, expr, expected, flags, where in hand:
        test = name.rsplit('-', 1)[0]
        rows.append([name, kind, fmt, expr, '', expected, 'upstream', flags, f'{test} {where}'])
    return rows


def checked(path: str, digest: str) -> str:
    data = open(path, 'rb').read()
    actual = hashlib.sha256(data).hexdigest()
    if actual != digest:
        raise SystemExit(f'{path}: SHA-256 {actual} is not the recorded {digest}')
    return data.decode('utf-8')


def main(argv: list[str]) -> int:
    if len(argv) != 3:
        print(__doc__, file=sys.stderr)
        return 2
    spec = checked(argv[1], SPEC_SHA256)
    parser = checked(argv[2], PARSER_SHA256)
    rows, tz_rows = spec_rows(spec)
    rows += parser_rows(parser)
    out = sys.stdout
    out.write(HEADER.format(spec=SPEC_SHA256, parser=PARSER_SHA256, tz=tz_rows))
    out.write(COLUMNS + '\n')
    for row in rows:
        out.write(','.join(csv_field(v) for v in row) + '\n')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
