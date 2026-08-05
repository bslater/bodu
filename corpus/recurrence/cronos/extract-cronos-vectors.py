#!/usr/bin/env python3
"""Derive a cron validation table from Cronos's ``CronExpressionFacts`` test suite.

Cronos (https://github.com/HangfireIO/Cronos, MIT) is a widely used .NET cron implementation whose
test suite is the densest public collection of cron next-occurrence vectors. It is a *third-party
comparison* source, not an authority: Cronos implements Quartz-flavoured cron and deliberately
differs from Vixie in three places this extractor records rather than resolves.

Usage:

    python3 corpus/recurrence/cronos/extract-cronos-vectors.py <CronExpressionFacts.cs> [out.csv]

The emitted table is a CSV with the columns:

    name,kind,format,expression,from,expected,flags

``kind`` is one of ``next`` (next occurrence, inclusive), ``unreachable`` (no occurrence exists),
``invalid`` (must be rejected), ``equal`` / ``notEqual`` (expression equivalence), or ``toString``
(canonical text). ``flags`` records why a row is out of scope for Bodu; an empty ``flags`` means the
row is reconciled.
"""

from __future__ import annotations

import csv
import datetime as dt
import hashlib
import re
import sys

HEADER = """\
# Cron vectors derived from Cronos's CronExpressionFacts test suite
# source-class: third-party-comparison (implementation test suite, not an authority)
# source: Cronos -- https://github.com/HangfireIO/Cronos -- tests/Cronos.Tests/CronExpressionFacts.cs
# acquired: 2026-08-05 (delivered user-side; host unreachable from the build env)
# facts-sha256: {digest}
# licence: Cronos is MIT (Copyright (c) 2017 Hangfire OU). Deriving this table and redistributing it
#          with attribution is permitted; the upstream file itself is not committed.
# note: Cronos implements Quartz-flavoured cron. Where it deliberately differs from Vixie, the row is
#       flagged and excluded rather than reconciled -- see corpus/recurrence/README.md.
# flags: quartz-ext        = uses the Quartz L / W / # / ? tokens (Bodu rejects them; planned follow-on)
#        hash              = uses the H jitter token (a Jenkins extension Bodu does not model)
#        cronos-macro      = @every_second / @every_minute, macros Cronos adds beyond crontab(5)
#        wrap-range        = a reversed range such as 55-5 (Cronos wraps; Bodu rejects)
#        oversized-step    = a step wider than its range, e.g. */60 (Cronos throws; cronie warns and
#                            keeps the range start, which is what Bodu does)
#        dom-dow-intersect = both day fields restricted (Cronos intersects; Vixie and Bodu union)
#        seconds-elision   = Cronos drops a zero seconds field from ToString; Bodu renders every field
#        dst               = the row sits within a day of a US Eastern DST transition, where a
#                            zone-free reading of the Cronos fixture is not offset-invariant
"""

# The Cronos fixture resolves a bare "HH:mm[:ss]" against this fixed date, so every row is
# deterministic despite the tests reading like they use "today".
TODAY = dt.date(2016, 12, 9)

INLINE = re.compile(r'^\s*\[InlineData\((.*)\)\]\s*$')

MONTH_NAMES = {
    'JAN': 1, 'FEB': 2, 'MAR': 3, 'APR': 4, 'MAY': 5, 'JUN': 6,
    'JUL': 7, 'AUG': 8, 'SEP': 9, 'OCT': 10, 'NOV': 11, 'DEC': 12,
}
DAY_NAMES = {'SUN': 0, 'MON': 1, 'TUE': 2, 'WED': 3, 'THU': 4, 'FRI': 5, 'SAT': 6}

# The macro set Vixie publishes in crontab(5). Cronos adds @every_second and @every_minute of its own.
VIXIE_MACROS = {'@YEARLY', '@ANNUALLY', '@MONTHLY', '@WEEKLY', '@DAILY', '@MIDNIGHT', '@HOURLY', '@REBOOT'}


def split_args(text: str) -> list[str]:
    """Splits an ``[InlineData(...)]` argument list, honouring quoted commas."""
    args, buf, quoted = [], [], False
    for ch in text:
        if ch == '"':
            quoted = not quoted
            buf.append(ch)
        elif ch == ',' and not quoted:
            args.append(''.join(buf).strip())
            buf = []
        else:
            buf.append(ch)
    if buf:
        args.append(''.join(buf).strip())
    return args


def unquote(arg: str) -> str:
    return arg[1:-1].strip() if arg.startswith('"') and arg.endswith('"') else arg.strip()


def rows_between(lines: list[str], start: int, end: int) -> list[list[str]]:
    """Collects the ``[InlineData]` rows in the half-open line range [start, end)."""
    out = []
    for line in lines[start:end]:
        m = INLINE.match(line)
        if m:
            out.append([unquote(a) for a in split_args(m.group(1))])
    return out


def resolve_instant(text: str) -> str:
    """Resolves a Cronos fixture time string to an ISO-8601 local wall clock."""
    text = text.strip()
    for fmt, has_date in (
        ('%Y-%m-%d %H:%M:%S', True),
        ('%Y-%m-%d %H:%M', True),
        ('%Y-%m-%d', True),
        ('%H:%M:%S', False),
        ('%H:%M', False),
    ):
        try:
            parsed = dt.datetime.strptime(text, fmt)
        except ValueError:
            continue
        if not has_date:
            parsed = parsed.replace(year=TODAY.year, month=TODAY.month, day=TODAY.day)
        return parsed.strftime('%Y-%m-%dT%H:%M:%S')
    raise ValueError(f'unrecognised instant: {text!r}')


def nth_weekday(year: int, month: int, weekday: int, n: int) -> dt.date:
    """Returns the date of the nth given weekday (0 = Monday) of a month."""
    first = dt.date(year, month, 1)
    offset = (weekday - first.weekday()) % 7
    return first + dt.timedelta(days=offset + 7 * (n - 1))


def is_near_us_dst_transition(iso: str) -> bool:
    """Reports whether an instant sits within a day of a US Eastern DST transition."""
    when = dt.datetime.fromisoformat(iso).date()
    year = when.year
    if not (1900 <= year <= 9999):
        return False
    transitions = [nth_weekday(year, 3, 6, 2), nth_weekday(year, 11, 6, 1)]  # 6 = Sunday
    return any(abs((when - t).days) <= 1 for t in transitions)


def field_bounds(index: int, field_count: int) -> tuple[int, int, dict[str, int]]:
    """Returns the (min, max, names) triple for a field position."""
    offset = 0 if field_count == 6 else 1
    position = index + offset
    return [
        (0, 59, {}),   # seconds
        (0, 59, {}),   # minutes
        (0, 23, {}),   # hours
        (1, 31, {}),   # day of month
        (1, 12, MONTH_NAMES),
        (0, 7, DAY_NAMES),
    ][position]


def token_value(token: str, names: dict[str, int]) -> int | None:
    token = token.strip().upper()
    if token in names:
        return names[token]
    return int(token) if token.isdigit() else None


def has_wrapping_range(expression: str) -> bool:
    """Reports whether any field contains a reversed (wrapping) range, which Bodu rejects."""
    fields = expression.split()
    if len(fields) not in (5, 6):
        return False
    for index, field in enumerate(fields):
        _, _, names = field_bounds(index, len(fields))
        for part in field.split(','):
            part = part.split('/')[0]
            if '-' not in part or part.startswith('-'):
                continue
            lo, _, hi = part.partition('-')
            lo_value, hi_value = token_value(lo, names), token_value(hi, names)
            if lo_value is not None and hi_value is not None and lo_value > hi_value:
                return True
    return False


def has_oversized_step(expression: str) -> bool:
    """Reports whether any field steps by more than its range spans.

    cronie only *warns* here (``entry.c``: "Step size %i higher than possible maximum") and then
    lets ``for (i = low; i <= high; i += step)`` set the single bit at the range start, so Vixie --
    and Bodu with it -- accepts the field. Cronos rejects it outright.
    """
    fields = expression.split()
    if len(fields) not in (5, 6):
        return False
    for index, field in enumerate(fields):
        low, high, names = field_bounds(index, len(fields))
        for part in field.split(','):
            head, sep, step_text = part.partition('/')
            if not sep or not step_text.isdigit():
                continue
            step = int(step_text)
            if head == '*':
                range_low, range_high = low, high
            elif '-' in head and not head.startswith('-'):
                lo, _, hi = head.partition('-')
                range_low, range_high = token_value(lo, names), token_value(hi, names)
            else:
                range_low, range_high = token_value(head, names), high
            if range_low is None or range_high is None:
                continue
            if step > 1 and step > (range_high - range_low):
                return True
    return False


def has_both_day_fields_restricted(expression: str) -> bool:
    """Reports whether both day fields are restricted, where Vixie unions and Cronos intersects."""
    fields = expression.split()
    if len(fields) not in (5, 6):
        return False
    offset = 1 if len(fields) == 6 else 0
    dom, dow = fields[2 + offset], fields[4 + offset]
    return not dom.startswith('*') and not dow.startswith('*')


def classify(expression: str, *, kind: str, instants: list[str] = ()) -> str:
    """Returns the space-separated scope flags for one row.

    A flag is applied only where it can change the row's outcome. Syntax the library rejects
    (``quartz-ext``, ``hash``, ``wrap-range``) matters wherever the row asserts that an expression
    *parses*, but not on a rejection row -- Bodu rejects a superset of what Cronos rejects, so
    "must throw" still holds there. The day-field semantics (``dom-dow-intersect``) change which
    instants match, so they matter only on the occurrence kinds.
    """
    flags = []
    # Strip the three-letter month and weekday names first: they are the only alphabetic tokens Vixie
    # recognises, so any letter left over ("SUNL", "10W", "L-1") belongs to a Quartz extension.
    residue = re.sub(r'(?<![A-Z])(' + '|'.join(list(MONTH_NAMES) + list(DAY_NAMES)) + r')', '', expression.upper())
    is_macro = expression.startswith('@')
    asserts_acceptance = kind != 'invalid'
    if asserts_acceptance and is_macro:
        # A macro is a single word, not a field list, so the letter-residue tests do not apply to it.
        if expression.upper() not in VIXIE_MACROS:
            flags.append('cronos-macro')
    elif asserts_acceptance:
        if re.search(r'[LW]', residue) or '#' in expression or '?' in expression:
            flags.append('quartz-ext')
        if 'H' in residue:
            flags.append('hash')
        if has_wrapping_range(expression):
            flags.append('wrap-range')
    # An oversized step is the one divergence that also lands on rejection rows: Cronos throws where
    # Vixie warns and accepts, so Bodu parses an expression Cronos will not.
    if has_oversized_step(expression):
        flags.append('oversized-step')
    if kind in ('next', 'unreachable') and has_both_day_fields_restricted(expression):
        flags.append('dom-dow-intersect')
    if any(is_near_us_dst_transition(i) for i in instants):
        flags.append('dst')
    return ' '.join(dict.fromkeys(flags))


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        print(__doc__, file=sys.stderr)
        return 2

    source = argv[1]
    destination = argv[2] if len(argv) > 2 else 'cronos-cron-vectors.csv'
    with open(source, encoding='utf-8') as handle:
        lines = handle.read().splitlines()

    def method_line(name: str) -> int:
        for index, line in enumerate(lines):
            if f'public void {name}(' in line or f'public void {name}()' in line:
                return index
        raise KeyError(name)

    def block(name: str) -> list[list[str]]:
        end = method_line(name)
        start = end
        while start > 0 and '[Theory]' not in lines[start]:
            start -= 1
        return rows_between(lines, start, end)

    records: list[dict[str, str]] = []
    counters: dict[str, int] = {}

    def add(kind: str, expression: str, source_from: str, expected: str, flags: str) -> None:
        counters[kind] = counters.get(kind, 0) + 1
        records.append({
            'name': f'{kind}-{counters[kind]:03d}',
            'kind': kind,
            'format': 'withSeconds' if len(expression.split()) == 6 else 'standard',
            'expression': expression,
            'from': source_from,
            'expected': expected,
            'flags': flags,
        })

    # Next-occurrence rows: six-field (seconds), five-field, and the @macro forms. Every one of these
    # is asserted inclusively against an Eastern wall clock, so a zone-free engine reproduces them
    # verbatim except where the row straddles a DST transition.
    for method in (
        'GetNextOccurrence_ReturnsCorrectDate',
        'GetNextOccurrence_ReturnsCorrectDate_WhenExpressionContains5FieldsAndInclusiveIsTrue',
        'GetNextOccurrence_ReturnsCorrectDate_WhenExpressionIsMacros',
    ):
        for expression, from_text, expected_text in block(method):
            expression = ' '.join(expression.split())
            resolved_from = resolve_instant(from_text)
            resolved_expected = resolve_instant(expected_text)
            add('next', expression, resolved_from, resolved_expected,
                classify(expression, kind='next', instants=[resolved_from, resolved_expected]))

    # Rules that can never fire: February 30th and friends.
    for method in (
        'GetNextOccurrence_ReturnsNull_WhenCronExpressionIsUnreachable',
        'GetNextOccurrence_ReturnsNull_WhenCronExpressionIsUnreachableAndFromIsDateTime',
    ):
        for expression, from_text in block(method):
            expression = ' '.join(expression.split())
            resolved_from = resolve_instant(from_text)
            add('unreachable', expression, resolved_from, '',
                classify(expression, kind='unreachable', instants=[resolved_from]))

    # Malformed expressions. Bodu rejects a strict superset of what Cronos rejects (it has no Quartz
    # extensions to accept), so these rows carry no acceptance-dependent flags.
    for expression, _, invalid_field in block('Parse_ThrowsCronFormatException_WhenCronExpressionIsInvalid'):
        expression = ' '.join(expression.split())
        add('invalid', expression, '', invalid_field, classify(expression, kind='invalid'))

    # Expression equivalence: distinct spellings that must (or must not) compare equal.
    for kind, method in (
        ('equal', 'Equals_ReturnsTrue_WhenCronExpressionsAreEqual'),
        ('notEqual', 'Equals_ReturnsFalse_WhenCronExpressionsAreNotEqual'),
    ):
        for left, right in block(method):
            left, right = ' '.join(left.split()), ' '.join(right.split())
            flags = ' '.join(dict.fromkeys(
                (classify(left, kind=kind) + ' ' + classify(right, kind=kind)).split()))
            add(kind, left, '', right, flags)

    # Canonical text. Cronos normalizes each field to a sorted value list, or "*" when the field spans
    # its whole range -- the same shape Bodu emits.
    for expression, expected, _ in block('ToString_ReturnsCorrectString'):
        expression = ' '.join(expression.split())
        expected = ' '.join(expected.split())
        flags = classify(expression, kind='toString')
        if len(expected.split()) != len(expression.split()):
            # Cronos drops a seconds field that is exactly 0, so a six-field expression can render as
            # five. Bodu always renders every field of the format it was parsed with.
            flags = ' '.join(dict.fromkeys((flags + ' seconds-elision').split()))
        add('toString', expression, '', expected, flags)

    with open(source, 'rb') as handle:
        digest = hashlib.sha256(handle.read()).hexdigest()

    with open(destination, 'w', encoding='utf-8', newline='') as handle:
        handle.write(HEADER.format(digest=digest))
        writer = csv.DictWriter(
            handle,
            fieldnames=['name', 'kind', 'format', 'expression', 'from', 'expected', 'flags'],
            lineterminator='\n')
        writer.writeheader()
        writer.writerows(records)

    total = len(records)
    in_scope = sum(1 for r in records if not r['flags'])
    print(f'wrote {total} rows to {destination} ({in_scope} in scope)')
    by_kind: dict[str, list[int]] = {}
    for record in records:
        bucket = by_kind.setdefault(record['kind'], [0, 0])
        bucket[0] += 1
        if not record['flags']:
            bucket[1] += 1
    for kind, (rows, scoped) in sorted(by_kind.items()):
        print(f'  {kind:12} {rows:4} rows, {scoped:4} in scope')
    excluded: dict[str, int] = {}
    for record in records:
        for flag in record['flags'].split():
            excluded[flag] = excluded.get(flag, 0) + 1
    for flag, count in sorted(excluded.items()):
        print(f'  excluded by {flag}: {count}')
    return 0


if __name__ == '__main__':
    raise SystemExit(main(sys.argv))
