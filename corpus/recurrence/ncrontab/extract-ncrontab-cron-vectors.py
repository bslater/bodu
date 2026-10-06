#!/usr/bin/env python3
"""Derive a cron vector table from NCrontab's ``CrontabScheduleTests`` test suite.

NCrontab (https://github.com/atifaziz/NCrontab, Apache-2.0) is a long-lived .NET crontab parser. Its single test file,
``NCrontab.Tests/CrontabScheduleTests.cs``, holds 174 NUnit ``[TestCase]`` rows and 10 ``[Test]`` methods. This script
restates them for Bodu's ``CronExpression``:

* the ``[TestCase]`` rows are parsed from the file, each by a handler for its test method, and
* the ``[Test]`` assertions are a hand-maintained list below, each with its line.

Every test method is accounted for, and the script fails if the file holds a method or a row the handlers do not
cover.

Usage:

    python3 -I extract-ncrontab-cron-vectors.py <CrontabScheduleTests.cs> > ncrontab-cron-vectors.csv

The output is deterministic: rerunning the script reproduces the table byte for byte.
"""

from __future__ import annotations

import csv
import hashlib
import io
import re
import sys

UPSTREAM_PATH = 'NCrontab.Tests/CrontabScheduleTests.cs'
UPSTREAM_COMMIT = 'd122d586a0665580f3fde9b8cfc03c5e3abf1909'
UPSTREAM_SHA256 = 'bdcceaf4de1e04527cd05edd471666d782fe8fdce4bd5ddeef9cd0dd2239cfe7'
EXPECTED_TEST_CASES = 174

COLUMNS = ['name', 'kind', 'format', 'expression', 'from', 'expected', 'expectation', 'flags', 'reference']

METHOD = re.compile(r'^\s*public void (\w+)\(')

# The [Test] methods. Key: line of the method declaration; value: (method, rows) where rows is a list of
# (kind, format, expression, from, expected, expectation, flags, note), or (method, None) for a method left out
# because it tests the library's own null-argument guards, which have no cron meaning.
TESTS = {
    43: ('CannotParseNullString', None),
    51: ('CannotParseEmptyString', [
        ('invalid', 'standard', '', '', '', 'upstream', '', 'Parse("") throws CrontabException'),
    ]),
    57: ('TryParseNullString', None),
    61: ('TryParseEmptyString', [
        ('invalid', 'standard', '', '', '', 'upstream', '', 'TryParse("") returns null'),
    ]),
    65: ('AllTimeString', [
        ('to-string', 'standard', '* * * * *', '', '* * * * *', 'upstream', '', ''),
    ]),
    72: ('SixPartAllTimeString', [
        ('to-string', 'withSeconds', '* * * * * *', '', '* * * * * *', 'upstream', '',
         'IncludingSeconds = true; seconds first as in Bodu'),
    ]),
    79: ('CannotParseWhenSecondsRequired', [
        ('invalid', 'withSeconds', '* * * * *', '', '', 'upstream', '',
         'five fields with IncludingSeconds = true'),
    ]),
    # GetNextOccurrences(9988-01-01, DateTime.MaxValue).Last() is 9988-02-29 under NCrontab's intersection: that day is
    # an occurrence (a Monday 29 February), and nothing follows it before the end of the calendar. Under Bodu's union
    # the expression fires on every Monday in February as well, so the second row is flagged; the first holds under
    # either reading, since 9988-02-29 is both a 29 February and a Monday.
    404: ('GetNextOccurrences_NextOccurrenceInvalidTime_ShouldStopAtLastValidTime', [
        ('next-inclusive', 'standard', '0 0 29 Feb Mon', '9988-02-29T00:00:00', '9988-02-29T00:00:00', 'upstream',
         '', 'issue #21; the last occurrence from 9988-01-01 is 9988-02-29, so it is one; a 29 February and a Monday, '
         'it is one under the union too'),
        ('unreachable-next', 'standard', '0 0 29 Feb Mon', '9988-02-29T00:00:00', '', 'upstream',
         'dom-dow-intersect', 'issue #21; nothing follows 9988-02-29 before DateTime.MaxValue (the next Monday 29 '
         'February would be in 10016)'),
    ]),
    430: ('GetNextOccurrencesWithNullSchedule', None),
    447: ('GetNextOccurrencesWithNullResultSelector', None),
}

# NextOccurrencesFromMultipleSchedules merges several schedules into one run (an API Bodu does not have). Each row is
# split into each schedule's own run from 2003-01-01 00:00, exclusive: the merged run is the union of those runs, so
# every schedule's first occurrences are exactly the merged instants it selects. Key: the [TestCase] line; value: the
# per-expression runs. The script checks that the runs hold exactly the instants the row lists.
MERGE_SPLITS = {
    455: [('0 * * * *', ['2003-01-01T01:00:00', '2003-01-01T02:00:00', '2003-01-01T03:00:00'])],
    457: [('0 1 * * *', ['2003-01-01T01:00:00', '2003-01-02T01:00:00']),
          (' 0 2 * * *', ['2003-01-01T02:00:00', '2003-01-02T02:00:00']),
          (' 0 3 * * *', ['2003-01-01T03:00:00', '2003-01-02T03:00:00'])],
    460: [('0 2 * * *', ['2003-01-01T02:00:00', '2003-01-02T02:00:00']),
          ('0 3 * * *', ['2003-01-01T03:00:00', '2003-01-02T03:00:00']),
          ('0 1 * * *', ['2003-01-01T01:00:00', '2003-01-02T01:00:00'])],
    # Two copies of one schedule merged up to the end bound 03:30 give each instant once.
    463: [('0 * * * *', ['2003-01-01T01:00:00', '2003-01-01T02:00:00', '2003-01-01T03:00:00'])],
    465: [('0 7-9 * * Mon', ['2003-01-06T07:00:00', '2003-01-06T08:00:00', '2003-01-06T09:00:00']),
          ('0 6,18 * * Tue', ['2003-01-07T06:00:00', '2003-01-07T18:00:00']),
          ('0 3,*/6 * * Fri', ['2003-01-03T00:00:00', '2003-01-03T03:00:00', '2003-01-03T06:00:00',
                               '2003-01-03T12:00:00', '2003-01-03T18:00:00'])],
}

# Hand-checked expected values, keyed by row name, each with its reason. They are needed where the upstream value
# depends on something Bodu reads differently: NCrontab's intersection of two restricted day fields, or its bounded
# GetNextOccurrence(start, end), which returns `end` when nothing comes before it (Bodu has no end bound, so the row is
# restated as the unbounded next occurrence, which lies at or after that end). Values from cronsim 2.7, checked by
# hand, unless marked derived.
OVERRIDES = {
    # 45 16 1 * Mon is "the 1st or a Monday" under the union; NCrontab asserts the next 1st that is a Monday.
    'Evaluations-114': ('2003-01-01T16:45:00', 'cronsim',
                        'NCrontab intersects the day fields and asserts 2003-09-01 16:45:00; restated under the union: '
                        '1 January is the 1st'),
    'Evaluations-115': ('2003-09-08T16:45:00', 'cronsim',
                        'NCrontab intersects the day fields and asserts 2003-12-01 16:45:00; restated under the union: '
                        'the next Monday after 1 September 23:45'),
    'FiniteOccurrences-01': ('2003-01-01T00:01:00', 'cronsim', None),
    'FiniteOccurrences-02': ('2003-01-01T00:00:00', 'cronsim', None),
    'FiniteOccurrences-03': ('2003-01-06T00:00:00', 'cronsim', None),
    'FiniteOccurrences-04': ('2003-01-06T00:00:00', 'cronsim', None),
    'FiniteOccurrences-05': ('2003-01-06T00:00:00', 'cronsim', None),
    'FiniteOccurrences-06': ('2003-01-06T12:30:00', 'cronsim', None),
    'FiniteOccurrences-07': ('2003-01-01T00:00:01', 'cronsim', None),
    'FiniteOccurrences-08': ('2003-01-01T00:00:00', 'cronsim', None),
    'FiniteOccurrences-09': ('2003-01-06T00:00:00', 'cronsim', None),
    'FiniteOccurrences-10': ('2003-01-06T00:00:00', 'cronsim', None),
    'FiniteOccurrences-11': ('2003-01-06T00:00:00', 'cronsim', None),
    'FiniteOccurrences-12': ('2003-01-06T12:30:10', 'cronsim', None),
    # 0 0 29 Feb Mon from 2017-01-01 under NCrontab's intersection: the next Monday 29 February is in 2044. The test
    # asserts only that none comes before 2017-12-31, so the value is derived under NCrontab's reading.
    'GetNextOccurrence-01': ('2044-02-29T00:00:00', 'derived',
                             'it returns that end, as no Monday 29 February comes before it; the value is the next '
                             'one under the intersection, 2044-02-29 (2016 and 2044 are the leap years either side '
                             'whose 29 February is a Monday)'),
}

# A merged run bounded by an end needs one more instant to show that nothing else falls before the end.
MERGE_BOUND_EXTRA = {463: ('2003-01-01T04:00:00', 'the first occurrence past the end bound 03:30, from cronsim 2.7')}

MONTH_OR_DAY_NAME = re.compile(r'[A-Za-z]+')


def fail(message: str) -> None:
    sys.stderr.write(f'error: {message}\n')
    sys.exit(1)


def test_cases(text: str) -> list[tuple[int, str, list[str]]]:
    """Returns (line, method, arguments) for every [TestCase(...)] attribute, which may span several lines."""
    out = []
    lines = text.split('\n')
    starts = [i for i in range(len(text)) if text.startswith('[TestCase(', i)]
    for start in starts:
        line = text.count('\n', 0, start) + 1
        depth, quoted, i = 0, False, start + len('[TestCase')
        while True:
            ch = text[i]
            if quoted:
                if ch == '\\':
                    i += 1
                elif ch == '"':
                    quoted = False
            elif ch == '"':
                quoted = True
            elif ch in '({':
                depth += 1
            elif ch in ')}':
                depth -= 1
                if depth == 0:
                    break
            i += 1
        args = split_args(text[start + len('[TestCase('):i])
        method = None
        for following in lines[line:]:
            match = METHOD.match(following)
            if match:
                method = match.group(1)
                break
        out.append((line, method, args))
    return out


def split_args(text: str) -> list[str]:
    """Splits an argument list at the commas outside string literals and braces."""
    args, buf, depth, quoted = [], [], 0, False
    for ch in text:
        if quoted:
            quoted = ch != '"'
        elif ch == '"':
            quoted = True
        elif ch == '{':
            depth += 1
        elif ch == '}':
            depth -= 1
        elif ch == ',' and depth == 0:
            args.append(''.join(buf).strip())
            buf = []
            continue
        buf.append(ch)
    args.append(''.join(buf).strip())
    return args


def literal(arg: str) -> str:
    if not (arg.startswith('"') and arg.endswith('"')):
        fail(f'expected a string literal, got {arg!r}')
    return arg[1:-1]


def strings(arg: str) -> list[str]:
    """Returns the string literals of a C# array initialiser."""
    return re.findall(r'"([^"]*)"', arg)


def instant(text: str) -> str:
    """Converts an NCrontab fixture time (dd/MM/yyyy HH:mm:ss or yyyy-MM-dd) to the table's ISO form."""
    text = text.strip()
    match = re.fullmatch(r'(\d{2})/(\d{2})/(\d{4}) (\d{2}:\d{2}:\d{2})', text)
    if match:
        return f'{match.group(3)}-{match.group(2)}-{match.group(1)}T{match.group(4)}'
    match = re.fullmatch(r'(\d{4}-\d{2}-\d{2})', text)
    if match:
        return f'{match.group(1)}T00:00:00'
    fail(f'unrecognised instant {text!r}')
    return ''


def fmt(including_seconds: str) -> str:
    return {'true': 'withSeconds', 'false': 'standard'}[including_seconds]


def three_letter_names(expression: str, six: bool) -> tuple[str, bool]:
    """Cuts month and weekday names longer than three letters to their first three letters.

    NCrontab accepts any prefix of the full English name (February, Augu, DECEM, sunday); Bodu takes the three-letter
    abbreviations, which are those names' first three letters. The selected months and days are unchanged.
    """
    fields = re.split(r'(\s+)', expression)
    offset = 1 if six else 0
    seen = -1
    changed = False
    for index, field in enumerate(fields):
        if not field or field.isspace():
            continue
        seen += 1
        if seen - offset in (3, 4):
            cut = MONTH_OR_DAY_NAME.sub(lambda m: m.group(0)[:3], field)
            changed = changed or cut != field
            fields[index] = cut
    return ''.join(fields), changed


def both_days_restricted(expression: str, six: bool) -> bool:
    parts = expression.split()
    offset = 1 if six else 0
    return parts[2 + offset][0] != '*' and parts[4 + offset][0] != '*'


def join(*parts: str) -> str:
    return '; '.join(p for p in parts if p)


def rows_for(method: str, cases: list[tuple[int, list[str]]]) -> list[dict]:
    out = []
    for counter, (line, args) in enumerate(cases, 1):
        name = f'{method}-{counter:02d}' if len(cases) < 100 else f'{method}-{counter:03d}'
        base = dict(name=name, method=method, line=line, frm='', expected='', expectation='upstream', flags='',
                    note='')
        if method == 'Formatting':
            # Parse(expression).ToString() is the other text. Bodu renders a different canonical text (value lists,
            # not ranges), so the row is restated as the equality of the two spellings.
            canonical, expression, six = literal(args[0]), literal(args[1]), args[2] == 'true'
            out.append(dict(base, kind='equal', format=fmt(args[2]), expression=expression, expected=canonical,
                            note='NCrontab renders the expression as the other text; restated as the equality of '
                                 "the two spellings, since Bodu's canonical text lists values"))
        elif method == 'Evaluations':
            start, expression, expected, six = instant(literal(args[0])), literal(args[1]), instant(literal(args[2])), args[3] == 'true'
            expression, renamed = three_letter_names(expression, six)
            row = dict(base, kind='next', format=fmt(args[3]), expression=expression, frm=start, expected=expected)
            if renamed:
                row['note'] = join(row['note'], f'names cut to three letters (upstream {literal(args[1]).strip()})')
            if six:
                row['note'] = join(row['note'], 'IncludingSeconds = true, seconds first as in Bodu')
            if both_days_restricted(expression, six):
                apply_override(row)
            out.append(row)
        elif method == 'FiniteOccurrences':
            expression, start, end = literal(args[0]), instant(literal(args[1])), instant(literal(args[2]))
            row = dict(base, kind='next', format=fmt(args[3]), expression=expression, frm=start,
                       note=f'GetNextOccurrence(start, end) returns the end {end}: nothing before it; restated as the '
                            f'unbounded next occurrence, at or after the end')
            apply_override(row)
            out.append(row)
        elif method == 'DontLoopIndefinitely':
            # The method body fixes the search to 2001-01-01 .. 2010-01-01 (L330).
            out.append(dict(base, kind='unreachable-next', format=fmt(args[1]), expression=literal(args[0]),
                            frm='2001-01-01T00:00:00', expectation='derived',
                            note='the end-bounded search from 2001-01-01 returns its end 2010-01-01 (L330); February '
                                 'never has a 31st, so there is no next occurrence'))
        elif method in ('BadSecondsField', 'BadMinutesField', 'BadHoursField', 'BadDayField', 'BadMonthField',
                        'BadDayOfWeekField', 'OutOfRangeField', 'NonNumberValueInNumericOnlyField',
                        'NonNumericFieldInterval', 'NonNumericFieldRangeComponent'):
            out.append(dict(base, kind='invalid', format=fmt(args[1]), expression=literal(args[0]),
                            note='Parse throws CrontabException and TryParse returns null'))
        elif method == 'GetNextOccurrence':
            expression, start, end, expected = (literal(a) for a in args)
            row = dict(base, kind='next', format='standard', expression=expression, frm=instant(start),
                       expected=instant(expected), flags='dom-dow-intersect',
                       note=f'end bound {end}; NCrontab intersects the day fields (a Monday 29 February)')
            if instant(expected) == instant(end):
                apply_override(row)
            out.append(row)
        elif method == 'NextOccurrencesFromMultipleSchedules':
            out.extend(merge_rows(base, line, args))
        else:
            fail(f'test method {method} has no handler')
    return out


def apply_override(row: dict) -> None:
    if row['name'] not in OVERRIDES:
        fail(f'{row["name"]} needs a hand-checked value')
    row['expected'], row['expectation'], reason = OVERRIDES[row['name']]
    row['note'] = join(row['note'], reason or 'value from cronsim 2.7, checked by hand')


def merge_rows(base: dict, line: int, args: list[str]) -> list[dict]:
    expressions, times = strings(args[0]), [instant(t) for t in strings(args[2])]
    splits = MERGE_SPLITS.get(line)
    if splits is None:
        fail(f'NextOccurrencesFromMultipleSchedules L{line} has no split')
    if sorted(e for e, _ in splits) != sorted(set(expressions)):
        fail(f'L{line}: the split names {sorted(e for e, _ in splits)}, the row {sorted(set(expressions))}')
    if sorted(t for _, run in splits for t in run) != sorted(times):
        fail(f'L{line}: the split runs do not hold exactly the listed instants')
    out = []
    for counter, (expression, run) in enumerate(splits, 1):
        note = (f"NCrontab merges the runs of {len(expressions)} schedule(s); restated as each schedule's own run, "
                f'this one of schedule {expressions.index(expression) + 1}')
        expected, expectation = run, 'upstream'
        if line in MERGE_BOUND_EXTRA:
            extra, why = MERGE_BOUND_EXTRA[line]
            expected, expectation = run + [extra], 'cronsim'
            note = join(note, f'end bound {instant(literal(args[1]))}, so {extra} is appended: {why}')
        out.append(dict(base, name=f'{base["name"]}-{counter}', kind='next-sequence', format='standard',
                        expression=expression, frm='2003-01-01T00:00:00', expected='|'.join(expected),
                        expectation=expectation, note=note))
    return out


def test_rows(lines: list[str]) -> tuple[list[dict], int]:
    """Checks the [Test] list against the file and expands it."""
    found = {}
    for number, line in enumerate(lines, 1):
        if line.strip() == '[Test]':
            for follower in range(number, min(number + 3, len(lines))):
                match = METHOD.match(lines[follower])
                if match:
                    found[follower + 1] = match.group(1)
                    break
    listed = {line: method for line, (method, _) in TESTS.items()}
    if found != listed:
        fail(f'the [Test] list disagrees with the file: file {sorted(found.items())}, list {sorted(listed.items())}')
    out, left_out = [], 0
    for line in sorted(TESTS):
        method, specs = TESTS[line]
        if specs is None:
            left_out += 1
            continue
        for counter, (kind, fmt_, expression, frm, expected, expectation, flags, note) in enumerate(specs, 1):
            out.append(dict(name=f'{method}-{counter:02d}', kind=kind, format=fmt_, expression=expression, frm=frm,
                            expected=expected, expectation=expectation, flags=flags, method=method, line=line,
                            note=note))
    return out, left_out


def render(rows: list[dict], left_out: int) -> str:
    flags = sorted({f for r in rows for f in r['flags'].split()})
    meanings = {'dom-dow-intersect': 'both day fields restricted: NCrontab intersects them, Bodu takes the union'}
    header = [
        "# Cron vectors derived from NCrontab's CrontabScheduleTests.cs",
        '# source-class: third-party-comparison (implementation test suite, not an authority)',
        f'# source: NCrontab -- https://github.com/atifaziz/NCrontab -- {UPSTREAM_PATH}',
        f'# commit: {UPSTREAM_COMMIT}',
        f'# file-sha256: {UPSTREAM_PATH} {UPSTREAM_SHA256}',
        '# licence: Apache-2.0 (Copyright (c) 2008 Atif Aziz; portions Copyright (c) 2001 The OpenSymphony Group). '
        'The rows restate the assertions (expressions and instants) with attribution; the upstream file is not '
        'committed.',
        '# method: extracted by extract-ncrontab-cron-vectors.py (the 174 [TestCase] rows parsed per test method; the '
        '[Test] assertions from a hand-maintained list in the script, each with its line)',
        '# note: NCrontab always intersects the two day fields, accepts any prefix of a month or day name, and returns '
        'the end bound from GetNextOccurrence(start, end) when nothing comes first; its six-part form puts seconds '
        f'first as Bodu does. Left out: {left_out} [Test] methods of null-argument guards with no cron meaning',
    ]
    header += [f'# flags: {flag} = {meanings[flag]}' for flag in flags]
    buffer = io.StringIO()
    writer = csv.writer(buffer, lineterminator='\n')
    writer.writerow(COLUMNS)
    for r in rows:
        writer.writerow([r['name'], r['kind'], r['format'], r['expression'], r['frm'], r['expected'],
                         r['expectation'], r['flags'], join(f'{r["method"]} L{r["line"]}', r['note'])])
    return '\n'.join(header) + '\n' + quote_padded(buffer.getvalue())


def quote_padded(text: str) -> str:
    """Quotes the fields that begin or end with a space, which csv leaves bare, so that no reader trims them."""
    lines = []
    for record in csv.reader(io.StringIO(text)):
        cells = []
        for field in record:
            if field != field.strip() or any(c in field for c in ',"\n'):
                cells.append('"' + field.replace('"', '""') + '"')
            else:
                cells.append(field)
        lines.append(','.join(cells))
    return '\n'.join(lines) + '\n'


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        sys.stderr.write(__doc__)
        return 2
    data = open(argv[1], 'rb').read()
    digest = hashlib.sha256(data).hexdigest()
    if digest != UPSTREAM_SHA256:
        fail(f'{argv[1]} has SHA-256 {digest}, expected {UPSTREAM_SHA256} ({UPSTREAM_PATH} at {UPSTREAM_COMMIT})')
    text = data.decode('utf-8')
    cases = test_cases(text)
    if len(cases) != EXPECTED_TEST_CASES:
        fail(f'found {len(cases)} [TestCase] rows, expected {EXPECTED_TEST_CASES}')
    by_method: dict[str, list[tuple[int, list[str]]]] = {}
    for line, method, args in cases:
        by_method.setdefault(method, []).append((line, args))
    rows = []
    for method in by_method:
        rows.extend(rows_for(method, by_method[method]))
    tests, left_out = test_rows(text.split('\n'))
    rows = sorted(rows + tests, key=lambda r: (r['line'], r['name']))
    names = [r['name'] for r in rows]
    if len(names) != len(set(names)):
        fail('duplicate row names')
    unused = set(OVERRIDES) - set(names)
    if unused:
        fail(f'overrides for rows that do not exist: {sorted(unused)}')
    sys.stdout.write(render(rows, left_out))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
