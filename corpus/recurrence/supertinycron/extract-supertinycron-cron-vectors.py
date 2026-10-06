#!/usr/bin/env python3
"""Derive a Bodu cron vector table from exander77/supertinycron's ``ccronexpr_test.c``.

supertinycron (https://github.com/exander77/supertinycron, Apache-2.0) is the maintained fork of
staticlibs/ccronexpr. Its test file asserts its answers as a list of calls: ``check_fn(cron_next | cron_prev,
pattern, initial, expected)``, ``check_invalid_instant(fn, pattern, initial)`` (no occurrence),
``check_same(expr1, expr2)``, ``check_expr_valid(expr)`` and ``check_expr_invalid(expr)``, plus the hard-coded
``check_calc_invalid()``. This script turns each call into one row of the PR G vector schema.

Usage:

    python3 extract-supertinycron-cron-vectors.py <ccronexpr_test.c> > supertinycron-cron-vectors.csv

The script checks the file's SHA-256 against the commit the table records and writes the table to stdout. The
output is deterministic.

Adaptations, each stated in the row's reference:

* supertinycron reads five fields (minute first), six (seconds first) or seven (six plus a year). A seven-field
  row whose year is ``*`` keeps its meaning without the year, so the year is dropped; any other year is flagged
  ``year-field``.
* The macros are compared with a seven-field expression; Bodu's macros are five-field, so the seven-field side is
  restated in the five-field layout when its seconds are ``0`` and its year is ``*``.
* Rows with supertinycron-only syntax (a bare ``W`` in day-of-month, a bare ``L`` in day-of-week, ``d#-k`` and the
  ``L``-prefixed leap seconds) are flagged ``syntax-extension``; ``@minutely`` and ``@secondly`` are flagged
  ``macro``.
* supertinycron intersects the two day fields; no row restricts both, so no row depends on it.

Calls in the ``CRON_USE_LOCAL_TIME`` block and in the ``right/UTC`` block run only under a named time zone and are
left out, as are assertions commented out in the file.
"""

from __future__ import annotations

import hashlib
import re
import sys

UPSTREAM_SHA256 = '62dae4f66438e2ae557d77fd94cb53cf3b14b06ee9ab91a2725c38041c1ecb97'

HEADER = """\
# Cron vectors derived from exander77/supertinycron's ccronexpr_test.c
# source-class: third-party-comparison (implementation test suite, not an authority)
# source: supertinycron -- https://github.com/exander77/supertinycron -- ccronexpr_test.c
# commit: cd1a408cfbcd539a7696d2985ee1cc9b81b5207e
# file-sha256: ccronexpr_test.c {digest}
# licence: Apache-2.0 (Copyright 2015, alex at staticlibs.net; fork maintained by exander77). The rows restate the file's assertions as expressions and instants, with attribution; the upstream file itself is not committed.
# method: extracted by extract-supertinycron-cron-vectors.py
# note: supertinycron reads five fields (minute first), six (seconds first) or seven (six plus a year 1970-2199), weekdays 0-7, and always intersects the two day fields (no row restricts both). It adds a bare W (weekdays), a bare L in day-of-week (Sunday), d#-k, leap seconds (L60), @minutely and @secondly, all flagged. A seven-field row with year * is restated without the year.
# note: left out (counted): {tz_rows} rows in the CRON_USE_LOCAL_TIME block (L356-410, named time zones), 3 rows in the right/UTC leap-second block (L440-444), test_bits (bit-set helper API); assertions commented out in the file (L436, L450, L453, L892-894, L964) are not assertions. The cron_generate_expr round trip that check_fn adds is the library's own API and is not transcribed.
# flags: year-field = the row depends on supertinycron's year field (a seven-field expression with a restricted year, or the equality of the six- and seven-field forms)
# flags: syntax-extension = supertinycron-only syntax: a bare W in day-of-month, a bare L in day-of-week, d#-k, or an L-prefixed leap second
# flags: macro = @minutely or @secondly, macros outside crontab(5)'s seven
# flags: cross-layout = equates a six-field expression with a five-field one; Bodu's Equals includes the layout by design (cron.md, Canonical text and equality)
"""

COLUMNS = 'name,kind,format,expression,from,expected,expectation,flags,reference'

CALL = re.compile(
    r'\b(check_fn|check_fn_t|check_same|check_expr_invalid|check_expr_valid|check_invalid_instant)\(([^;]*)\);')
FUNCTION = re.compile(r'^\s*void\s+(\w+)\s*\(')

# Seven-field expressions in check_same whose five-field restatement keeps their meaning: seconds 0, year *.
FIVE_FIELD_MACRO_FORMS = {
    '0 0 0 1 1 * *': '0 0 1 1 *',
    '0 0 0 1 * * *': '0 0 1 * *',
    '0 0 0 * * 0 *': '0 0 * * 0',
    '0 0 0 * * * *': '0 0 * * *',
    '0 0 * * * * *': '0 * * * *',
}


def blank(match: re.Match) -> str:
    """Replaces a comment with spaces, keeping its newlines so line numbers stay true."""
    return re.sub(r'[^\n]', ' ', match.group(0))


def c_string(literal: str) -> str:
    """Decodes the one escape the test file's string literals use."""
    return literal.replace('\\t', '\t')


def instant(text: str) -> str:
    return text.replace('_', 'T')


def csv_field(value: str) -> str:
    if any(c in value for c in ',"\t') or value != value.strip():
        return '"' + value.replace('"', '""') + '"'
    return value


def layout(fields: list[str]) -> str:
    return 'standard' if len(fields) == 5 else 'withSeconds'


def extension_flags(fields: list[str]) -> list[str]:
    """Names the supertinycron-only syntax a five-, six- or seven-field expression uses."""
    if len(fields) == 5:
        seconds, dom, dow = '0', fields[2], fields[4]
    elif len(fields) >= 6:
        seconds, dom, dow = fields[0], fields[3], fields[5]
    else:
        return []
    if dom.upper() == 'W' or dow.upper() == 'L' or '#-' in dow or seconds.upper().startswith('L'):
        return ['syntax-extension']
    return []


def occurrence_row(name, kind, pattern, start, expected, ref):
    """Builds a next/previous/unreachable row, restating or flagging a seven-field pattern."""
    fields = pattern.split()
    flags = extension_flags(fields)
    expression = pattern
    if len(fields) == 7:
        if fields[6] == '*':
            # A year of * cannot change any answer, so the six-field form means exactly the same.
            expression = ' '.join(fields[:6])
            ref += '; year * dropped'
        else:
            flags.append('year-field')
    return [name, kind, layout(fields), expression, start, expected, 'upstream', ' '.join(flags), ref]


def same_row(name, left, right, ref):
    lf, rf = left.split(), right.split()
    flags = extension_flags(lf) + [f for f in extension_flags(rf) if f not in extension_flags(lf)]
    fmt = layout(rf if left.startswith('@') else lf)
    if left.startswith('@'):
        if left.lower() in ('@minutely', '@secondly'):
            return [name, 'equal', 'withSeconds', left, '', right, 'upstream', 'macro', ref]
        five = FIVE_FIELD_MACRO_FORMS[right]
        return [name, 'equal', 'standard', left, '', five, 'upstream', '',
                ref + f'; seven-field "{right}" restated in the five-field layout of Bodu\'s macros (seconds 0, year *)']
    if len(lf) == 7 or len(rf) == 7:
        flags.append('year-field')
        fmt = 'withSeconds'
    elif len(lf) != len(rf):
        flags.append('cross-layout')
    return [name, 'equal', fmt, left, '', right, 'upstream', ' '.join(flags), ref]


def validity_row(name, kind, pattern, ref):
    fields = pattern.split()
    # A rejected expression asserts only that it fails to parse, which Bodu shares even for syntax it does not
    # model (1#-6), so only accepted expressions carry the extension flag.
    flags = extension_flags(fields) if kind == 'valid' else []
    expression = pattern
    if len(fields) == 7:
        if fields[6] == '*':
            expression = ' '.join(fields[:6])
            ref += '; year * dropped'
        else:
            flags.append('year-field')
    return [name, kind, layout(fields), expression, '', '', 'upstream', ' '.join(flags), ref]


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        print(__doc__, file=sys.stderr)
        return 2
    data = open(argv[1], 'rb').read()
    digest = hashlib.sha256(data).hexdigest()
    if digest != UPSTREAM_SHA256:
        print(f'{argv[1]}: SHA-256 {digest} is not the recorded {UPSTREAM_SHA256}', file=sys.stderr)
        return 1
    source = data.decode('utf-8')
    code = re.sub(r'/\*.*?\*/', blank, source, flags=re.S)
    code = re.sub(r'//[^\n]*', blank, code)

    rows: list[list[str]] = []
    counters: dict[str, int] = {}
    function = ''
    local_time_depth = 0
    in_right_utc = False
    tz_rows = 0
    for number, line in enumerate(code.splitlines(), start=1):
        stripped = line.strip()
        match = FUNCTION.match(line)
        if match:
            function = match.group(1)
        # Inside test_expr the CRON_USE_LOCAL_TIME block holds only rows gated on a named TZ; the helpers use the
        # same macro for local-time formatting, which holds no calls.
        if stripped.startswith('#ifdef CRON_USE_LOCAL_TIME') and function == 'test_expr':
            local_time_depth = 1
            continue
        if local_time_depth and stripped.startswith('#endif'):
            local_time_depth = 0
            continue
        if stripped.startswith('if (tz && !strcmp("right/UTC", tz))'):
            in_right_utc = True
            continue
        if in_right_utc and stripped == '}':
            in_right_utc = False
            continue
        call = CALL.search(line)
        if not call or stripped.startswith('void'):
            continue
        if local_time_depth:
            tz_rows += 1
            continue
        if in_right_utc:
            continue
        helper, body = call.groups()
        args = [c_string(a) for a in re.findall(r'"((?:[^"\\]|\\.)*)"', body)]
        fn = re.match(r'\s*(cron_next|cron_prev)', body)
        counters[function] = counters.get(function, 0) + 1
        name = f'{function}-{counters[function]:03d}'
        ref = f'{function} L{number}'
        if helper == 'check_fn':
            kind = 'next' if fn.group(1) == 'cron_next' else 'previous'
            rows.append(occurrence_row(name, kind, args[0], instant(args[1]), instant(args[2]), f'{ref} {fn.group(1)}'))
        elif helper == 'check_invalid_instant':
            kind = 'unreachable-next' if fn.group(1) == 'cron_next' else 'unreachable-previous'
            rows.append(occurrence_row(name, kind, args[0], instant(args[1]), '',
                                       f'{ref} check_invalid_instant({fn.group(1)}) expects CRON_INVALID_INSTANT'))
        elif helper == 'check_same':
            rows.append(same_row(name, args[0], args[1], f'{ref} check_same'))
        elif helper == 'check_expr_valid':
            rows.append(validity_row(name, 'valid', args[0], f'{ref} check_expr_valid'))
        elif helper == 'check_expr_invalid':
            rows.append(validity_row(name, 'invalid', args[0], f'{ref} check_expr_invalid'))
        else:
            raise SystemExit(f'unexpected {helper} outside the time-zone blocks at L{number}')

    # check_calc_invalid() (L288-314) hard-codes three queries, each asserting that neither cron_next nor cron_prev
    # finds an occurrence. The second query's comment ties it to DST, but in the UTC build it is a plain instant.
    calc = [
        ('0 0 0 31 6 *', '2012-07-01T09:53:50', 'L290-297'),
        ('0 0 0 31 6 *', '2009-06-20T00:59:59', 'L300-305'),
        ('0 0 0 30 2 *', '2012-07-01T09:53:50', 'L307-313'),
    ]
    number = 0
    for pattern, start, lines in calc:
        for kind, fn in (('unreachable-next', 'cron_next'), ('unreachable-previous', 'cron_prev')):
            number += 1
            rows.append([f'check_calc_invalid-{number:02d}', kind, 'withSeconds', pattern, start, '', 'upstream', '',
                         f'check_calc_invalid {lines}; {fn} returns CRON_INVALID_INSTANT'])

    out = sys.stdout
    out.write(HEADER.format(digest=digest, tz_rows=tz_rows))
    out.write(COLUMNS + '\n')
    for row in rows:
        out.write(','.join(csv_field(v) for v in row) + '\n')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
