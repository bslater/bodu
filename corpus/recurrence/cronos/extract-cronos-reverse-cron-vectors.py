#!/usr/bin/env python3
"""Derive a cron vector table from Cronos's ``CronExpressionReverseFacts`` test suite.

Cronos (https://github.com/HangfireIO/Cronos, MIT) added previous-occurrence queries in 0.13.0 and keeps their tests in
``tests/Cronos.Tests/CronExpressionReverseFacts.cs``. This script restates that file for Bodu's ``CronExpression``:

* the ``[Theory]`` rows are parsed from the ``[InlineData]`` attributes, and
* the ``[Fact]`` assertions, one test method each, are a hand-maintained list below, each with its line.

Every test method of the file is accounted for, either as rows or as a counted omission, and the script fails if the
file holds a method the lists do not name, or names a method at a line where the file has none.

Usage:

    python3 -I extract-cronos-reverse-cron-vectors.py <CronExpressionReverseFacts.cs> > cronos-reverse-cron-vectors.csv

The output is deterministic: rerunning the script reproduces the table byte for byte.
"""

from __future__ import annotations

import csv
import hashlib
import io
import re
import sys

UPSTREAM_PATH = 'tests/Cronos.Tests/CronExpressionReverseFacts.cs'
UPSTREAM_COMMIT = '11234183dd5db3795b0b34710b75f8df120847e6'
UPSTREAM_SHA256 = '57f31cced77b73179c2d03c1f14924e5d6242a664d42fd8a3ebb6a921f815753'

COLUMNS = ['name', 'kind', 'format', 'expression', 'from', 'expected', 'expectation', 'flags', 'reference']

# Test methods left out, by reason. Each is counted in the header's note line.
#   api   - the library's own argument guards, with no cron meaning (two of them also read the wall clock)
#   hash  - a seeded H jitter: the answer depends on the seed passed to Parse, which the table has no column for; all
#           five also evaluate in the US Eastern time zone, three of them at a DST transition
#   zone  - the answer depends on a transition of a named time zone (US Eastern, Lord Howe, Santiago, Central
#           European), which a wall-clock reading cannot express
LEFT_OUT_THEORIES = {
    'GetPreviousOccurrence_ThrowsAnException_WhenFromDoesNotHaveUtcKind': 'api',
    'GetPreviousOccurrence_DateTimeTimeZone_ThrowsAnException_WhenFromHasAWrongKind': 'api',
    'GetPreviousOccurrence_MirrorsGetNextOccurrenceAcrossSpringForward': 'zone',
    'GetPreviousOccurrence_RetracesGetNextOccurrenceAroundSpringForward': 'zone',
}

LEFT_OUT_FACTS = {
    # line of the method declaration: (method, reason)
    59: ('GetPreviousOccurrence_DateTimeTimeZone_ThrowsAnException_WhenZoneIsNull', 'api'),
    68: ('GetPreviousOccurrence_DateTimeOffsetTimeZone_ThrowsAnException_WhenZoneIsNull', 'api'),
    132: ('GetPreviousOccurrence_ReturnsCorrectDate_WhenMacroExpressionHasJitterSeed', 'hash'),
    148: ('GetPreviousOccurrence_ReturnsCorrectDate_WhenExpressionContainsHash', 'hash'),
    248: ('GetOccurrencesDescending_ReturnsReverseOfForwardCollection_WhenZoneIsSpecified', 'zone'),
    261: ('GetPreviousOccurrence_CanStepAcrossLordHoweRepeatedHourIntervalSequence', 'zone'),
    274: ('GetOccurrencesDescending_DateTime_ThrowsAnException_WhenFromLessThanTo', 'api'),
    285: ('GetPreviousOccurrence_UsesSingleOccurrenceForAmbiguousNonIntervalTime', 'zone'),
    296: ('GetPreviousOccurrence_UsesSingleOccurrenceForAmbiguousNonIntervalHashTime', 'hash'),
    307: ('GetPreviousOccurrence_PreservesAmbiguousIntervalOccurrencesInReverseOrder', 'zone'),
    322: ('GetPreviousOccurrence_PreservesAmbiguousHashIntervalOccurrencesInReverseOrder', 'hash'),
    337: ('GetPreviousOccurrence_ShiftsInvalidTimeForwardAcrossSpringForward', 'zone'),
    348: ('GetPreviousOccurrence_ShiftsInvalidHashTimeForwardAcrossSpringForward', 'hash'),
    359: ('GetPreviousOccurrence_SkipsInvalidTimeShiftedAfterFrom_AndReturnsEarlierOccurrence', 'zone'),
    370: ('GetPreviousOccurrence_ReturnsShiftedInvalidTime_WhenFromEqualsItAndInclusive', 'zone'),
    381: ('GetPreviousOccurrence_SkipsShiftedInvalidTime_WhenFromEqualsItAndExclusive', 'zone'),
    442: ('GetPreviousOccurrence_HandleLordHoweBackwardShift_ForNonIntervalExpression', 'zone'),
    453: ('GetPreviousOccurrence_HandlePacificBackwardShift_AroundRepeatedHour', 'zone'),
}

# The [Fact] assertions, transcribed by hand. Key: line of the method declaration. Each row is
# (kind, format, expression, from, expected, expectation, reference note); the row's reference is the method name and
# line followed by the note.
FACT_ROWS = {
    91: ('GetPreviousOccurrence_ReturnsCorrectDate_WhenSecondsAreIncluded', [
        ('previous', 'withSeconds', '20 * * * * *', '2017-03-22T17:35:40', '2017-03-22T17:35:20', 'upstream',
         'CronFormat.IncludeSeconds, seconds first as in Bodu'),
    ]),
    102: ('GetPreviousOccurrence_ReturnsCorrectDate_ForComplexUtcExpression', [
        ('previous-inclusive', 'standard', '*/10 12-20 * DEC 3', '2017-12-06T12:00:00', '2017-12-06T12:00:00',
         'upstream', 'inclusive: true'),
    ]),
    163: ('GetPreviousOccurrence_ReturnsNull_WhenCronExpressionIsUnreachable', [
        ('unreachable-previous', 'standard', '* * 31 2 *', '2017-03-22T00:00:00', '', 'upstream',
         'Cronos searches back to DateTime.MinValue and Bodu twelve years; February never has a 31st'),
    ]),
    174: ('GetPreviousOccurrence_RollsBackAcrossMonthBoundary', [
        ('previous', 'standard', '0 0 1 * *', '2017-03-01T00:00:00', '2017-02-01T00:00:00', 'upstream', ''),
    ]),
    # GetOccurrencesDescending(from 00:02, to 00:00, fromInclusive, toInclusive) asserts the run 00:02, 00:01, 00:00;
    # Bodu has no range enumeration, so the run is an inclusive previous at its start and a previous sequence after it.
    214: ('GetOccurrencesDescending_ReturnsExpectedCollection', [
        ('previous-inclusive', 'standard', '* * * * *', '2017-03-22T00:02:00', '2017-03-22T00:02:00', 'upstream',
         'first of the descending run, fromInclusive: true'),
        ('previous-sequence', 'standard', '* * * * *', '2017-03-22T00:02:00',
         '2017-03-22T00:01:00|2017-03-22T00:00:00', 'upstream', 'the rest of the run, toInclusive: true'),
    ]),
    # The test asserts only that the descending walk from 01:00 to 00:00 equals the ascending walk reversed. The walks
    # are written out: */15 selects minutes 0, 15, 30 and 45, so both hold 00:00, 00:15, 00:30, 00:45 and 01:00.
    235: ('GetOccurrencesDescending_ReturnsReverseOfForwardCollection', [
        ('previous-inclusive', 'standard', '*/15 * * * *', '2017-03-22T01:00:00', '2017-03-22T01:00:00', 'derived',
         'descending walk, first element; upstream asserts only descending == reverse(ascending), values worked out: '
         '*/15 is minutes 0,15,30,45'),
        ('previous-sequence', 'standard', '*/15 * * * *', '2017-03-22T01:00:00',
         '2017-03-22T00:45:00|2017-03-22T00:30:00|2017-03-22T00:15:00|2017-03-22T00:00:00', 'derived',
         'descending walk, the rest; values worked out as above'),
        ('next-inclusive', 'standard', '*/15 * * * *', '2017-03-22T00:00:00', '2017-03-22T00:00:00', 'derived',
         'ascending walk, first element; values worked out as above'),
        ('next-sequence', 'standard', '*/15 * * * *', '2017-03-22T00:00:00',
         '2017-03-22T00:15:00|2017-03-22T00:30:00|2017-03-22T00:45:00|2017-03-22T01:00:00', 'derived',
         'ascending walk, the rest; values worked out as above'),
    ]),
    # The test passes US Eastern, but its instant carries the zone's own offset (-04:00, EDT, two hours past the
    # spring-forward) and the minute 03:00 exists in the zone, so the fixed-offset reading answers the same. It asserts
    # only previous <= from; the value is the whole minute at or before 03:00:00.5.
    464: ('GetPreviousOccurrence_DoesNotReturnValueLaterThanNonRoundInput', [
        ('previous-inclusive', 'standard', '* * * * *', '2017-03-12T03:00:00.5-04:00', '2017-03-12T03:00:00-04:00',
         'derived', 'EasternTimeZone replaced by its offset at the instant (-04:00); upstream asserts only previous <= '
         'from, the value is the last whole minute at or before 03:00:00.5'),
    ]),
    475: ('GetPreviousOccurrence_FromDateTimeMinValueInclusive_SuccessfullyReturned', [
        ('previous-inclusive', 'standard', '* * * * *', '0001-01-01T00:00:00', '0001-01-01T00:00:00', 'upstream',
         'new DateTime(0) is DateTime.MinValue'),
    ]),
}

# Hand-checked expected values for theory rows whose upstream answer depends on Cronos intersecting two restricted day
# fields. Bodu (Vixie) takes their union, so the row is restated: the value comes from cronsim 2.7 and was checked by
# hand. Key: row name; value: (expected, the reason).
OVERRIDES = {
    # 0 5 18 13 * 5 is "the 13th or a Friday" under the union. From Friday 2017-10-13 18:05:00, exclusive, the last
    # such day is Friday 2017-10-06 (no 13th falls between); Cronos's Friday-the-13th reading gives 2017-01-13.
    'GetPreviousOccurrence_ReturnsCorrectDate_ForBroaderUtcMatrix-05': (
        '2017-10-06T18:05:00', 'Cronos intersects the day fields and asserts 2017-01-13 18:05:00; restated under the union'),
    # 0 0 13 * 5 from Friday 2017-10-13 00:00, exclusive: the previous Friday, 2017-10-06, comes before any 13th.
    'GetPreviousOccurrence_ReturnsCorrectDate_ForAdditionalReverseCases-03': (
        '2017-10-06T00:00:00', 'Cronos intersects the day fields and asserts 2017-01-13 00:00:00; restated under the union'),
}

MONTHS = {'JAN': 1, 'FEB': 2, 'MAR': 3, 'APR': 4, 'MAY': 5, 'JUN': 6,
          'JUL': 7, 'AUG': 8, 'SEP': 9, 'OCT': 10, 'NOV': 11, 'DEC': 12}
DAYS = {'SUN': 0, 'MON': 1, 'TUE': 2, 'WED': 3, 'THU': 4, 'FRI': 5, 'SAT': 6}

INLINE = re.compile(r'^\s*\[InlineData\((.*)\)\]\s*$')
METHOD = re.compile(r'^\s*public void (\w+)\(')


def fail(message: str) -> None:
    sys.stderr.write(f'error: {message}\n')
    sys.exit(1)


def split_args(text: str) -> list[str]:
    """Splits an InlineData argument list at the commas outside string literals."""
    args, buf, quoted = [], [], False
    for ch in text:
        if ch == '"':
            quoted = not quoted
        if ch == ',' and not quoted:
            args.append(''.join(buf).strip())
            buf = []
        else:
            buf.append(ch)
    args.append(''.join(buf).strip())
    return args


def literal(arg: str) -> str:
    """Returns the content of a C# string literal."""
    if not (arg.startswith('"') and arg.endswith('"')):
        fail(f'expected a string literal, got {arg!r}')
    return arg[1:-1]


def instant(text: str) -> str:
    """Converts a fixture time 'yyyy-MM-dd HH:mm[:ss]' to the table's ISO form."""
    match = re.fullmatch(r'(\d{4}-\d{2}-\d{2}) (\d{2}:\d{2})(:\d{2})?', text.strip())
    if not match:
        fail(f'unrecognised instant {text!r}')
    return f'{match.group(1)}T{match.group(2)}{match.group(3) or ":00"}'


def value(token: str, names: dict[str, int]) -> int:
    return names[token.upper()] if token.upper() in names else int(token)


def has_reversed_range(expression: str, fields: int) -> bool:
    """Reports whether a field holds a range whose start exceeds its end, which Cronos wraps and Bodu rejects."""
    parts = expression.split()
    offset = 1 if fields == 6 else 0
    for index, field in enumerate(parts):
        position = index - offset
        names = MONTHS if position == 3 else DAYS if position == 4 else {}
        for item in field.split(','):
            body = item.split('/')[0]
            if '-' in body and not body.upper().startswith('L'):
                low, high = body.split('-', 1)
                try:
                    if value(low, names) > value(high, names):
                        return True
                except (KeyError, ValueError):
                    continue
    return False


def both_days_restricted(expression: str, fields: int) -> bool:
    """Reports whether neither day field is led by '*' or is '?', the case where Cronos and Bodu combine differently."""
    parts = expression.split()
    offset = 1 if fields == 6 else 0
    dom, dow = parts[2 + offset], parts[4 + offset]
    return dom[0] not in '*?' and dow[0] not in '*?'


def theory_rows(lines: list[str]) -> tuple[list[dict], dict[str, int], set[str]]:
    """Collects the InlineData rows per theory, returning the rows, the rows per left-out theory, and every theory."""
    rows: list[dict] = []
    left_out: dict[str, int] = {}
    seen: set[str] = set()
    pending: list[tuple[int, list[str]]] = []
    for number, line in enumerate(lines, 1):
        match = INLINE.match(line)
        if match:
            pending.append((number, split_args(match.group(1))))
            continue
        method = METHOD.match(line)
        if method and pending:
            name = method.group(1)
            seen.add(name)
            if name in LEFT_OUT_THEORIES:
                left_out[name] = len(pending)
            else:
                rows.extend(theory_method(name, pending))
            pending = []
    return rows, left_out, seen


def theory_method(method: str, pending: list[tuple[int, list[str]]]) -> list[dict]:
    """Restates one theory's rows."""
    out = []
    for counter, (line, args) in enumerate(pending, 1):
        name = f'{method}-{counter:02d}'
        note = ''
        if method == 'GetPreviousOccurrence_ReturnsCorrectUtcDate':
            # The method body fixes the expression (CronExpression.EveryMinute) and the instant (L81).
            inclusive, expected = args[0] == 'true', instant(literal(args[1]))
            row = dict(kind='previous-inclusive' if inclusive else 'previous', format='standard',
                       expression='* * * * *', frm='2017-03-22T09:32:00', expected=expected)
            note = f'inclusive: {args[0]}; CronExpression.EveryMinute is * * * * *, from 2017-03-22 09:32 UTC (L81)'
        elif method in ('GetPreviousOccurrence_ReturnsCorrectDate_ForBroaderUtcMatrix',
                        'GetPreviousOccurrence_ReturnsCorrectDate_ForSpecialDayModifiers',
                        'GetPreviousOccurrence_ReturnsCorrectDate_ForAdditionalReverseCases'):
            expression = literal(args[0])
            # The broader matrix names its format; the other two call Parse(string), which is the five-field format.
            six = len(args) > 3 and args[3] == 'CronFormat.IncludeSeconds'
            row = dict(kind='previous', format='withSeconds' if six else 'standard', expression=expression,
                       frm=instant(literal(args[1])), expected=instant(literal(args[2])))
            if six:
                note = 'CronFormat.IncludeSeconds, seconds first as in Bodu'
        else:
            fail(f'theory {method} has no handler and is not listed as left out')
        fields = 6 if row['format'] == 'withSeconds' else 5
        row.update(name=name, expectation='upstream', flags='', line=line, method=method, note=note)
        if has_reversed_range(row['expression'], fields):
            row['flags'] = 'wrap-range'
            row['note'] = join(row['note'], 'reversed range: Cronos wraps it, Bodu rejects it')
        elif both_days_restricted(row['expression'], fields):
            if name not in OVERRIDES:
                fail(f'{name} restricts both day fields and needs a restated value')
            row['expected'], reason = OVERRIDES[name]
            row['expectation'] = 'cronsim'
            row['note'] = join(row['note'], reason)
        out.append(row)
    return out


def join(*parts: str) -> str:
    return '; '.join(p for p in parts if p)


def fact_rows(lines: list[str]) -> tuple[list[dict], set[str]]:
    """Checks the hand-maintained fact lists against the file and expands the transcribed facts."""
    facts: dict[int, str] = {}
    for number, line in enumerate(lines, 1):
        if line.strip() == '[Fact]':
            for follower in range(number, min(number + 3, len(lines))):
                method = METHOD.match(lines[follower])
                if method:
                    facts[follower + 1] = method.group(1)
                    break
    listed = {line: method for line, (method, _) in FACT_ROWS.items()}
    listed.update({line: method for line, (method, _) in LEFT_OUT_FACTS.items()})
    if facts != listed:
        missing = sorted(set(facts.items()) - set(listed.items()))
        extra = sorted(set(listed.items()) - set(facts.items()))
        fail(f'the fact lists disagree with the file: unlisted {missing}, not in file {extra}')
    rows = []
    for line in sorted(FACT_ROWS):
        method, specs = FACT_ROWS[line]
        for counter, (kind, fmt, expression, frm, expected, expectation, note) in enumerate(specs, 1):
            rows.append(dict(name=f'{method}-{counter:02d}', kind=kind, format=fmt, expression=expression, frm=frm,
                             expected=expected, expectation=expectation, flags='', line=line, method=method,
                             note=note))
    return rows, set(facts.values())


def render(rows: list[dict], left_out_theories: dict[str, int]) -> str:
    counts = {'api': [0, 0, 0], 'hash': [0, 0, 0], 'zone': [0, 0, 0]}  # theories, theory rows, facts
    for method, size in left_out_theories.items():
        counts[LEFT_OUT_THEORIES[method]][0] += 1
        counts[LEFT_OUT_THEORIES[method]][1] += size
    for _, reason in LEFT_OUT_FACTS.values():
        counts[reason][2] += 1

    def tally(reason: str, label: str) -> str:
        theories, theory_rows_, facts = counts[reason]
        parts = []
        if theories:
            parts.append(f'{theories} theories ({theory_rows_} rows)')
        if facts:
            parts.append(f'{facts} facts')
        return f'{" and ".join(parts)} {label}'

    flags = sorted({f for r in rows for f in r['flags'].split()})
    meanings = {'wrap-range': 'a reversed range (20-5/5, 12-2, sat-tue) that Cronos wraps; Bodu rejects it'}
    header = [
        "# Cron vectors derived from Cronos's CronExpressionReverseFacts.cs",
        '# source-class: third-party-comparison (implementation test suite, not an authority)',
        f'# source: Cronos -- https://github.com/HangfireIO/Cronos -- {UPSTREAM_PATH}',
        f'# commit: {UPSTREAM_COMMIT}',
        f'# file-sha256: {UPSTREAM_PATH} {UPSTREAM_SHA256}',
        '# licence: MIT (Copyright (c) 2017 Hangfire OU). The rows restate the assertions (expressions and instants) '
        'with attribution; the upstream file is not committed.',
        '# method: extracted by extract-cronos-reverse-cron-vectors.py ([Theory] rows parsed from [InlineData]; '
        '[Fact] assertions from a hand-maintained list in the script, each with its line)',
        '# note: Cronos intersects two restricted day fields (Bodu takes the union: 2 rows restated under cronsim), '
        'wraps reversed ranges (Bodu rejects them), and evaluates in named time zones; '
        f'left out: {tally("zone", "that depend on a named time zone transition")}, '
        + tally('hash', 'with an H jitter seeded through Parse, which no column carries (all five in US Eastern, '
                        'three at a DST transition)')
        + f', {tally("api", "of argument guards with no cron meaning")}',
    ]
    header += [f'# flags: {flag} = {meanings[flag]}' for flag in flags]
    buffer = io.StringIO()
    writer = csv.writer(buffer, lineterminator='\n')
    writer.writerow(COLUMNS)
    for r in rows:
        reference = join(f'{r["method"]} L{r["line"]}', r['note'])
        writer.writerow([r['name'], r['kind'], r['format'], r['expression'], r['frm'], r['expected'],
                         r['expectation'], r['flags'], reference])
    return '\n'.join(header) + '\n' + buffer.getvalue()


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        sys.stderr.write(__doc__)
        return 2
    data = open(argv[1], 'rb').read()
    digest = hashlib.sha256(data).hexdigest()
    if digest != UPSTREAM_SHA256:
        fail(f'{argv[1]} has SHA-256 {digest}, expected {UPSTREAM_SHA256} ({UPSTREAM_PATH} at {UPSTREAM_COMMIT})')
    lines = data.decode('utf-8').split('\n')
    theory, left_out, theories = theory_rows(lines)
    unknown = set(LEFT_OUT_THEORIES) - theories
    if unknown:
        fail(f'left-out theories not in the file: {sorted(unknown)}')
    facts, _ = fact_rows(lines)
    rows = sorted(theory + facts, key=lambda r: (r['line'], r['name']))
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
