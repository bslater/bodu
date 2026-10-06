#!/usr/bin/env python3
"""Cross-check the occurrence rows of the cron library tables and fix catalogues against cronsim.

cronsim (https://github.com/cuu508/cronsim, BSD-3-Clause) is a Debian-compatible cron evaluator: it reads the
day-of-month and day-of-week fields as Vixie cron and Bodu do, taking their union only when neither field starts with
'*', so for five- and six-field expressions without Quartz tokens its answers are an oracle independent of both the
upstream library and Bodu. A table row whose expected value is not the library's own assertion, marked 'cronsim' in
its expectation column, was computed this way; rerunning the check reproduces it.

Usage, with cronsim 2.7 in a virtual environment:

    python3 -m venv venv && venv/bin/pip install cronsim==2.7
    venv/bin/python -I check-cron-vectors-with-cronsim.py [--all] [--quiet] <table.csv>...

Without --all, rows carrying an exclusion flag are skipped. cronsim does not read '?', 'W', 'L-n', 'LW', the '@'
macros or a day the month never has; such rows, and rows whose search would leave cronsim's calendar (Python's
datetime, years 1 to 9999), are reported as unsupported rather than as mismatches. cronsim searches 50 years and Bodu
400, so an 'unreachable' row that cronsim answers is reported as a horizon difference.

Prints one line per mismatch and a summary, and exits 1 when any supported row disagrees.
"""

from __future__ import annotations

import csv
import sys
from datetime import datetime, timedelta

from cronsim import CronSim, CronSimError

ONE_MICROSECOND = timedelta(microseconds=1)
OCCURRENCE_KINDS = {
    'next', 'next-inclusive', 'previous', 'previous-inclusive', 'next-sequence', 'previous-sequence',
    'unreachable-next', 'unreachable-previous',
}


def parse_instant(text: str) -> tuple[datetime, bool]:
    """Reads a table instant as a naive wall-clock time, dropping any offset.

    Returns the instant truncated to microseconds, Python's resolution, and whether the table's tenths of a
    microsecond were not zero, so that the truncated instant lies just before the table's.
    """
    core = text
    rest = text[19:].lstrip('.0123456789')
    if rest and rest[0] in '+-':
        core = text[:len(text) - len(rest)]
    if '.' not in core:
        return datetime.strptime(core, '%Y-%m-%dT%H:%M:%S'), False
    head, fraction = core.split('.', 1)
    fraction = (fraction + '0000000')[:7]
    value = datetime.strptime(f'{head}.{fraction[:6]}', '%Y-%m-%dT%H:%M:%S.%f')
    return value, fraction[6] != '0'


def fmt(value: datetime | None) -> str:
    return '' if value is None else value.strftime('%Y-%m-%dT%H:%M:%S')


def rows(path: str):
    """Yields (name, kind, format, expression, from, expected, flags) for a vector table or a fix catalogue.

    Any other table, such as cronos/cronos-cron-vectors.csv, which predates the schema and records its next
    occurrences inclusively, is skipped with a note, so the tables can be named by one glob.
    """
    with open(path, encoding='utf-8') as handle:
        lines = [line for line in handle if line.strip() and not line.startswith('#')]
    reader = csv.reader(lines)
    header = next(reader)
    if header[0] != 'library' and 'expectation' not in header:
        print(f'skipped      {path}: not a cron library table or fix catalogue', file=sys.stderr)
        return
    for record in reader:
        if header[0] == 'library':
            if record[4] not in ('applies', 'dialect'):
                continue
            name = f'{record[0]} {record[1]} {record[2]}' + (f' #{record[5]}' if record[5] else '')
            yield name, record[6], record[7], record[8], record[9], record[10], ''
        else:
            yield record[0], record[1], record[2], record[3], record[4], record[5], record[7]


def floor_unit(value: datetime, unit: timedelta) -> datetime:
    """Rounds down to the expression's unit: a whole minute for five fields, a whole second for six."""
    if unit == timedelta(minutes=1):
        return value.replace(second=0, microsecond=0)
    return value.replace(microsecond=0)


def answers(expression: str, start: datetime, below: bool, kind: str, count: int) -> list[datetime | None]:
    """Returns cronsim's answers to a row's query, read as Bodu reads it.

    'below' says that the table's instant lies just above 'start', by a fraction of a microsecond. Occurrences fall on
    whole seconds, so the query is answered exactly on Python's microsecond grid: an exclusive next or an inclusive
    previous from the instant is one from the microsecond below it, and an inclusive next or an exclusive previous one
    from the microsecond above it.
    """
    reverse = kind.startswith('previous') or kind == 'unreachable-previous'
    inclusive = kind.endswith('-inclusive')
    if below and (inclusive != reverse):
        start += ONE_MICROSECOND
    unit = timedelta(seconds=1) if len(expression.split()) == 6 else timedelta(minutes=1)
    if not reverse:
        # cronsim answers strictly after its origin, which is Bodu's exclusive next; a microsecond earlier makes it
        # inclusive.
        origin = start - ONE_MICROSECOND if inclusive else start
    else:
        # Backwards, cronsim floors its origin to the unit and answers strictly before that. Bodu's exclusive previous
        # is the last occurrence strictly before the instant, its inclusive one the last at or before it; occurrences
        # are unit-aligned, so those are "before the instant rounded up" and "before the floored instant plus a unit".
        floored = floor_unit(start, unit)
        if inclusive:
            origin = floored + unit
        else:
            origin = start if floored == start else floored + unit
    result: list[datetime | None] = []
    iterator = CronSim(expression, origin, reverse=reverse)
    for _ in range(count):
        try:
            result.append(next(iterator))
        except StopIteration:
            result.append(None)
            break
    return result


def main(argv: list[str]) -> int:
    run_all = '--all' in argv
    quiet = '--quiet' in argv
    files = [a for a in argv if not a.startswith('--')]
    checked = agreed = mismatched = unsupported = horizon = 0
    for path in files:
        for name, kind, fmt_name, expression, start_text, expected, flags in rows(path):
            if kind not in OCCURRENCE_KINDS or (flags and not run_all):
                continue
            try:
                start, below = parse_instant(start_text)
                expected_list = [] if kind.startswith('unreachable') else expected.split('|')
                got = answers(expression, start, below, kind, max(1, len(expected_list)))
            except CronSimError as error:
                unsupported += 1
                if not quiet:
                    print(f'unsupported  {path}:{name} {expression!r}: {error}')
                continue
            except OverflowError:
                unsupported += 1
                if not quiet:
                    print(f'unsupported  {path}:{name} {expression!r}: the search leaves the calendar')
                continue
            checked += 1
            if kind.startswith('unreachable'):
                if got and got[0] is not None:
                    horizon += 1
                    print(f'HORIZON      {path}:{name} {kind} {expression!r} from {start_text}: cronsim finds {fmt(got[0])}')
                else:
                    agreed += 1
                continue
            want = [fmt(parse_instant(x)[0]) for x in expected_list]
            have = [fmt(x) for x in got]
            if want == have:
                agreed += 1
            else:
                mismatched += 1
                print(f'MISMATCH     {path}:{name} {kind} {fmt_name} {expression!r} from {start_text}: '
                      f'table {"|".join(want)} cronsim {"|".join(have)}')
    print(f'checked {checked}, agreed {agreed}, mismatched {mismatched}, horizon {horizon}, unsupported {unsupported}')
    return 1 if mismatched else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
