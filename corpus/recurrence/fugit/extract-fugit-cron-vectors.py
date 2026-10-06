#!/usr/bin/env python3
"""Derive Bodu cron vectors from fugit's ``test/cron_test.rb``.

fugit (https://github.com/floraison/fugit, MIT) is a Ruby time-parsing library whose cron parser is
tested through literal tables: ``NEXT_TIMES`` and ``PREVIOUS_TIMES`` (each row also asserts
``#match?``), the ``.parse`` success, failure and impossible-day tables, ``.do_parse``, ``#to_cron_s``,
``#seconds``, the ``sec6`` tables and ``.parse_cron``. This script reads those tables from the upstream
file, checks the file's SHA-256, and writes the table in the PR G vector schema to stdout:

    name,kind,format,expression,from,expected,expectation,flags,reference

The handful of bespoke tests that assert fixed instants (the iterators, ``#within``, ``#==``, the gh-43,
gh-47 and modulo-extension tests, the input-length guards) are a hand-maintained list below, each with its
line; the script checks that the line still says what the list expects.

Usage:

    python3 -I extract-fugit-cron-vectors.py <path to fugit test/cron_test.rb> > fugit-cron-vectors.csv

The output is deterministic: rerunning the script on the same file reproduces it byte for byte.
"""

from __future__ import annotations

import hashlib
import io
import re
import sys

UPSTREAM_PATH = 'test/cron_test.rb'
UPSTREAM_SHA256 = '5345b7650592a8960557291b68d681abc0d727eeeb0ab053b706b01ccdf81028'
COMMIT = 'f571bc080b5445499a81af20254afc057f384ab1'

# fugit's NEXT_TIMES default: NOW = Time.parse('2017-01-02 12:00:00').
NOW = '2017-01-02T12:00:00'

FLAG_TEXT = {
    'dom-dow-union': 'fugit unions a restricted day field led by "*" (such as */2) with the other day field; '
                     'Bodu and Vixie read a star-led field as unrestricted and intersect',
    'impossible-date': 'fugit rejects a day-month combination that never occurs (30 February); Bodu '
                       'accepts it and its searches answer null',
    'macro': 'a macro outside crontab(5)\'s seven (@noon)',
    'syntax-extension': 'fugit-only syntax: % modulo weeks, & (day AND), ~ random values, a step with no '
                        'range (/15), empty list items, hour 24, negative month-day ranges',
    'token-list': 'Quartz-style tokens (L, d#k, dL, or negative month days, which map to L-n) inside lists '
                  'or ranges, which Bodu rejects',
    'wrap-range': 'a reversed range (55-5, fri-sun, 11-2) that fugit wraps around; Bodu rejects it',
}

HEADER_LINES = [
    "# Cron vectors derived from fugit's test/cron_test.rb",
    '# source-class: third-party-comparison (implementation test suite, not an authority)',
    '# source: fugit -- https://github.com/floraison/fugit -- test/cron_test.rb',
    '# commit: ' + COMMIT,
    '# file-sha256: test/cron_test.rb ' + UPSTREAM_SHA256,
    '# licence: MIT (Copyright (c) 2017-2026, John Mettraux, jmettraux+flor@gmail.com). Expressions, instants and '
    'canonical strings are derived from the upstream tests with attribution; the upstream file itself is not '
    'committed.',
    '# method: extracted by extract-fugit-cron-vectors.py (tables parsed from the file; the bespoke '
    'fixed-instant tests are a hand-maintained list in the script, each with its line)',
    '# note: fugit unions the day fields when both are restricted, but counts a star-led step (*/2) as '
    'restricted and 1-31 as not; it adds L / last / negative month days (-1 is L, -n is L-(n-1)), '
    'd#-1 / d#L / d#last (Bodu dL), wrap-around ranges, hour 24, % / & / ~ extensions, /15, extra commas, '
    'a time-zone suffix, @noon, and rejects impossible day-month pairs. Single tokens are adapted to '
    "Bodu's spelling in code; the rest is flagged. Times are wall clock; rows run upstream in a named zone "
    'are kept only where no transition touches the answer.',
    '# note: left out, counted: {left_out}',
]


# --------------------------------------------------------------------------------------------------
# A small reader for the Ruby literals the tables use: arrays, hashes (=> and key: forms), single- and
# double-quoted strings, nil/true/false, integers and bare identifiers. Comments are skipped. Each value
# records the line it starts on. A double-quoted string with #{...} interpolation is kept raw and resolved
# through INTERPOLATIONS below.
# --------------------------------------------------------------------------------------------------

class Raw(str):
    """A double-quoted Ruby string that contained interpolation; resolved by line."""


class Literal:
    def __init__(self, value, line):
        self.value = value
        self.line = line


class RubyReader:
    def __init__(self, text: str):
        self.text = text
        self.pos = 0
        self.line_starts = [0]
        for i, ch in enumerate(text):
            if ch == '\n':
                self.line_starts.append(i + 1)

    def line_of(self, pos: int) -> int:
        lo, hi = 0, len(self.line_starts) - 1
        while lo < hi:
            mid = (lo + hi + 1) // 2
            if self.line_starts[mid] <= pos:
                lo = mid
            else:
                hi = mid - 1
        return lo + 1

    def skip(self):
        while self.pos < len(self.text):
            ch = self.text[self.pos]
            if ch in ' \t\r\n,':
                self.pos += 1
            elif ch == '#':
                while self.pos < len(self.text) and self.text[self.pos] != '\n':
                    self.pos += 1
            else:
                return

    def parse(self) -> Literal:
        self.skip()
        start = self.pos
        line = self.line_of(start)
        ch = self.text[self.pos]
        if ch == '[':
            self.pos += 1
            items = []
            while True:
                self.skip()
                if self.text[self.pos] == ']':
                    self.pos += 1
                    return Literal(items, line)
                items.append(self.parse())
        if ch == '{':
            self.pos += 1
            pairs = []
            while True:
                self.skip()
                if self.text[self.pos] == '}':
                    self.pos += 1
                    return Literal(pairs, line)
                m = re.match(r'([a-z_]+):\s', self.text[self.pos:])
                if m:
                    key = Literal(m.group(1), self.line_of(self.pos))
                    self.pos += m.end()
                else:
                    key = self.parse()
                    self.skip_ws()
                    if self.text.startswith('=>', self.pos):
                        self.pos += 2
                    else:
                        raise ValueError(f'expected => at line {self.line_of(self.pos)}')
                pairs.append((key, self.parse()))
        if ch == "'":
            self.pos += 1
            out = []
            while self.text[self.pos] != "'":
                if self.text[self.pos] == '\\' and self.text[self.pos + 1] in "'\\":
                    out.append(self.text[self.pos + 1])
                    self.pos += 2
                else:
                    out.append(self.text[self.pos])
                    self.pos += 1
            self.pos += 1
            return Literal(''.join(out), line)
        if ch == '"':
            self.pos += 1
            out = []
            interpolated = False
            depth = 0
            while depth > 0 or self.text[self.pos] != '"':
                c = self.text[self.pos]
                if depth == 0 and c == '\\':
                    esc = self.text[self.pos + 1]
                    out.append({'n': '\n', 't': '\t', '"': '"', '\\': '\\'}[esc])
                    self.pos += 2
                    continue
                if depth == 0 and self.text.startswith('#{', self.pos):
                    interpolated = True
                    depth = 1
                    out.append('#{')
                    self.pos += 2
                    continue
                if depth > 0 and c == '{':
                    depth += 1
                elif depth > 0 and c == '}':
                    depth -= 1
                out.append(c)
                self.pos += 1
            self.pos += 1
            s = ''.join(out)
            return Literal(Raw(s) if interpolated else s, line)
        m = re.match(r'-?\d+', self.text[self.pos:])
        if m:
            self.pos += m.end()
            return Literal(int(m.group(0)), line)
        m = re.match(r'[A-Za-z_][A-Za-z0-9_:]*', self.text[self.pos:])
        if m:
            self.pos += m.end()
            word = m.group(0)
            return Literal({'nil': None, 'true': True, 'false': False}.get(word, word), line)
        raise ValueError(f'unexpected {ch!r} at line {line}')

    def skip_ws(self):
        while self.pos < len(self.text) and self.text[self.pos] in ' \t\r\n':
            self.pos += 1

    def literal_after(self, marker: str, opener: str) -> Literal:
        """Parses the first literal opened by ``opener`` at or after the first line containing marker."""
        at = self.text.index(marker)
        self.pos = self.text.index(opener, at)
        return self.parse()


def plain(lit: Literal):
    """Strips Literal wrappers recursively."""
    v = lit.value
    if isinstance(v, list):
        if v and isinstance(v[0], tuple):
            return [(plain(k), plain(x)) for k, x in v]
        return [plain(x) for x in v]
    return v


# Interpolated expected strings, evaluated by hand (the Ruby is shown with each).
INTERPOLATIONS = {
    # "* #{(0..23).to_a.collect(&:to_s).join(',')} * * *"
    '* #{(0..23).to_a.collect(&:to_s).join(\',\')} * * *': '* ' + ','.join(str(i) for i in range(24)) + ' * * *',
    # "* * #{(25..31).to_a.map(&:to_s).join(',')} * *"
    '* * #{(25..31).to_a.map(&:to_s).join(\',\')} * *': '* * ' + ','.join(str(i) for i in range(25, 32)) + ' * *',
}


def ruby(v) -> str:
    """Renders a parsed literal the way the Ruby source writes it (nil, [a, b])."""
    if v is None:
        return 'nil'
    if isinstance(v, list):
        return '[ ' + ', '.join(ruby(x) for x in v) + ' ]' if v else '[]'
    return str(v)


def resolve(s):
    if isinstance(s, Raw):
        return INTERPOLATIONS[str(s)]
    return s


# --------------------------------------------------------------------------------------------------
# Instants: fugit tests write 'yyyy-mm-dd[ HH:MM[:SS]]' (and '2017-01-8'); the table wants ISO.
# --------------------------------------------------------------------------------------------------

def iso(text: str) -> str:
    m = re.fullmatch(r'(\d{4})-(\d{1,2})-(\d{1,2})(?:[ T](\d{1,2}):(\d{2})(?::(\d{2}))?)?', text.strip())
    if not m:
        raise ValueError(f'unrecognised instant {text!r}')
    y, mo, d, h, mi, s = m.groups()
    return f'{int(y):04d}-{int(mo):02d}-{int(d):02d}T{int(h or 0):02d}:{int(mi or 0):02d}:{int(s or 0):02d}'


# --------------------------------------------------------------------------------------------------
# Adapting fugit cron text to Bodu's dialect.
# --------------------------------------------------------------------------------------------------

DOW = {'sun': 0, 'mon': 1, 'tue': 2, 'wed': 3, 'thu': 4, 'fri': 5, 'sat': 6}
MON = {'jan': 1, 'feb': 2, 'mar': 3, 'apr': 4, 'may': 5, 'jun': 6, 'jul': 7, 'aug': 8, 'sep': 9,
       'oct': 10, 'nov': 11, 'dec': 12}
BODU_MACROS = {'@yearly', '@annually', '@monthly', '@weekly', '@daily', '@midnight', '@hourly'}
ZONE = re.compile(r'^(?:[A-Z][A-Za-z0-9+\-_]*(?:/[A-Z][A-Za-z0-9+\-_]*){1,2}|UTC|GMT|[-+]\d{2}:?\d{2})$')


class Adapted:
    """An expression in Bodu's form, its format, the flags it earns and the adaptations made."""

    def __init__(self, expression, fmt, flags, notes, zone):
        self.expression = expression
        self.format = fmt
        self.flags = flags
        self.notes = notes
        self.zone = zone


def value_of(token: str, names: dict) -> int | None:
    t = token.lower()
    if t in names:
        return names[t]
    if re.fullmatch(r'\d+', token):
        return int(token)
    return None


def adapt(text: str) -> Adapted:
    """Restates a fugit cron string in Bodu's spelling where that keeps its meaning, else flags it."""
    flags: list[str] = []
    notes: list[str] = []
    stripped = text.strip()
    fields = stripped.split()
    zone = None
    if stripped.startswith('@'):
        if len(fields) > 1:
            zone = fields[1]
        macro = fields[0]
        if macro.lower() not in BODU_MACROS:
            flags.append('macro')
        return Adapted(macro, 'standard', flags, notes, zone)
    if len(fields) in (6, 7) and ZONE.match(fields[-1]) and not re.fullmatch(r'[\d*/,\-]+', fields[-1]):
        zone = fields[-1]
        fields = fields[:-1]
    fmt = 'withSeconds' if len(fields) == 6 else 'standard'
    if len(fields) not in (5, 6):
        return Adapted(text, 'standard', flags, notes, zone)
    names = ['second', 'minute', 'hour', 'dom', 'month', 'dow'][6 - len(fields):]
    # Keep the caller's spacing: rebuild by replacing field texts in order.
    out_fields = []
    for name, field in zip(names, fields):
        new, f_flags, f_notes = adapt_field(name, field)
        out_fields.append(new)
        for fl in f_flags:
            if fl not in flags:
                flags.append(fl)
        notes.extend(f_notes)
    expression = respace(text.strip() if zone is None else text.strip()[:text.strip().rindex(zone)].rstrip(),
                         fields, out_fields)
    return Adapted(expression, fmt, flags, notes, zone)


def respace(original: str, old_fields: list[str], new_fields: list[str]) -> str:
    """Substitutes adapted field texts into the original string, keeping its whitespace."""
    out = []
    pos = 0
    for old, new in zip(old_fields, new_fields):
        at = original.index(old, pos)
        out.append(original[pos:at])
        out.append(new)
        pos = at + len(old)
    out.append(original[pos:])
    return ''.join(out)


def adapt_field(name: str, field: str):
    flags: list[str] = []
    notes: list[str] = []
    low = field.lower()
    if '%' in field:
        return field, ['syntax-extension'], notes
    if '&' in field:
        return field, ['syntax-extension'], notes
    if '~' in field:
        return field, ['syntax-extension'], notes
    items = field.split(',')
    if any(item == '' for item in items):
        flags.append('syntax-extension')
        return field, flags, notes
    if name == 'dom':
        return adapt_dom(field, items)
    if name == 'dow':
        return adapt_dow(field, items)
    limits = {'second': (0, 59), 'minute': (0, 59), 'hour': (0, 23), 'month': (1, 12)}[name]
    for item in items:
        rng, _, step = item.partition('/')
        if rng == '' and step:
            flags.append('syntax-extension')
            continue
        if '-' in rng:
            a, _, b = rng.partition('-')
            va, vb = value_of(a, MON if name == 'month' else {}), value_of(b, MON if name == 'month' else {})
            if name == 'hour' and (va == 24 or vb == 24):
                flags.append('syntax-extension')
            elif va is not None and vb is not None and va > vb:
                flags.append('wrap-range')
        elif rng != '*':
            v = value_of(rng, MON if name == 'month' else {})
            if name == 'hour' and v == 24:
                flags.append('syntax-extension')
    return field, sorted(set(flags)), notes


def adapt_dom(field: str, items: list[str]):
    """Month days: L / l / last and negative days become Bodu's L / L-n when they are the whole field."""
    flags: list[str] = []
    notes: list[str] = []
    out = []
    tokens = 0
    for item in items:
        low = item.lower()
        if low in ('l', 'last'):
            out.append('L')
            tokens += 1
            if item != 'L':
                notes.append(f'{item} written L')
            continue
        m = re.fullmatch(r'-(\d+)', item)
        if m:
            n = int(m.group(1))
            out.append('L' if n == 1 else f'L-{n - 1}')
            tokens += 1
            notes.append(f'{item} (fugit: {n} from the end) written {out[-1]}')
            continue
        rng, _, step = item.partition('/')
        if rng == '' and step:
            flags.append('syntax-extension')
        elif re.search(r'(^|-)-\d', rng):
            flags.append('syntax-extension')  # a range of negative month days
        elif re.search(r'(^|-)(l|last)$', rng.lower()):
            flags.append('token-list')  # a range ending in L, such as 25-L
        elif '-' in rng:
            a, _, b = rng.partition('-')
            if a.isdigit() and b.isdigit() and int(a) > int(b):
                flags.append('wrap-range')
        out.append(item)
    if tokens and len(items) > 1:
        flags.append('token-list')
    return ','.join(out), sorted(set(flags)), notes


def adapt_dow(field: str, items: list[str]):
    """Weekdays: d#L / d#l / d#last / d#-1 become Bodu's dL when they are the whole field."""
    flags: list[str] = []
    notes: list[str] = []
    out = []
    tokens = 0
    for item in items:
        m = re.fullmatch(r'([a-zA-Z]{3}|\d)#(-1|[lL]|last|LAST)', item)
        if m:
            out.append(m.group(1) + 'L')
            tokens += 1
            notes.append(f'{item} (last {m.group(1)} of the month) written {out[-1]}')
            continue
        m = re.fullmatch(r'([a-zA-Z]{3}|\d)#(-?\d+)', item)
        if m:
            k = int(m.group(2))
            tokens += 1
            if not 1 <= k <= 5:
                flags.append('syntax-extension')
            out.append(item)
            continue
        rng, _, step = item.partition('/')
        if rng == '' and step:
            flags.append('syntax-extension')
        elif '-' in rng:
            a, _, b = rng.partition('-')
            va, vb = value_of(a, DOW), value_of(b, DOW)
            if va is not None and vb is not None and va > vb:
                flags.append('wrap-range')
        out.append(item)
    if tokens and len(items) > 1:
        flags.append('token-list')
    return ','.join(out), sorted(set(flags)), notes


def adapt_canonical(text: str) -> str:
    """Spells a fugit canonical string (to_cron_s) in Bodu's syntax: -1 is L, d#-1 is dL."""
    fields = text.split()
    if len(fields) not in (5, 6):
        return text
    i = len(fields) - 3  # day-of-month
    fields[i] = ','.join('L' if f == '-1' else (f'L-{int(f[1:]) - 1}' if re.fullmatch(r'-\d+', f) else f)
                         for f in fields[i].split(','))
    fields[-1] = ','.join(re.sub(r'#-1$', 'L', f) for f in fields[-1].split(','))
    return ' '.join(fields)


# --------------------------------------------------------------------------------------------------
# Rows.
# --------------------------------------------------------------------------------------------------

ROWS: list[list[str]] = []
LEFT_OUT: dict[str, int] = {}


def leave_out(reason: str, n: int = 1):
    LEFT_OUT[reason] = LEFT_OUT.get(reason, 0) + n


def add(name, kind, fmt, expression, frm, expected, expectation, flags, reference):
    ROWS.append([name, kind, fmt, expression, frm, expected, expectation, ' '.join(flags), reference])


def note_text(a: Adapted, extra: str = '') -> str:
    parts = []
    if a.notes:
        parts.append('; '.join(a.notes))
    if extra:
        parts.append(extra)
    return ('; ' + '; '.join(parts)) if parts else ''


# Rows whose expected value is restated under Bodu's dialect, keyed by row name. Each was checked by hand
# and, where cronsim supports the expression, against cronsim 2.7.
OVERRIDES = {
    # NEXT_TIMES '0 0 */2 * 1-5' in UTC from 2022-08-09: fugit counts */2 as restricted and takes the union
    # (2022-08-10, a Wednesday). In Bodu */2 is star-led, so unrestricted: odd days that are Monday-Friday.
    # 2022-08-10 is even; 2022-08-11 is a Thursday and odd.
    'next-times-054': ('2022-08-11T00:00:00', 'cronsim',
                       'restated: fugit unions the star-led */2 with 1-5 (2022-08-10); Bodu intersects, answer from cronsim'),
    # The #match? assertion for the same row: 2022-08-10 does not match in Bodu (an even day); the first
    # instant at or after it that does is 2022-08-11 00:00.
    'match-054': ('2022-08-11T00:00:00', 'cronsim',
                  'restated: fugit matches 2022-08-10 through its union; Bodu intersects odd days with Monday-Friday, '
                  'so the first match at or after it comes from cronsim'),
    # .parse '0 0 * * 3/2' => '0 0 * * 3': fugit's weekday parser runs n/m only to n (3/2 is {3}); Bodu,
    # like cronsim, runs a single value with a step to the field maximum 7 (Sunday): 3, 5, 7 = {0,3,5}.
    'parse-success-028': ('0 0 * * 0,3,5', 'derived',
                          'restated: fugit reads 3/2 in the weekday field as 3 alone; Bodu runs n/m to the field '
                          'maximum 7, so 3,5,7(=0); canonical rendered by hand per cron.md'),
}


def apply_override(name, expected, expectation, reference):
    if name in OVERRIDES:
        exp, how, why = OVERRIDES[name]
        return exp, how, reference + '; ' + why
    return expected, expectation, reference


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        print('usage: extract-fugit-cron-vectors.py <path to test/cron_test.rb>', file=sys.stderr)
        return 2
    data = open(argv[1], 'rb').read()
    digest = hashlib.sha256(data).hexdigest()
    if digest != UPSTREAM_SHA256:
        print(f'SHA-256 mismatch: {digest} (expected {UPSTREAM_SHA256})', file=sys.stderr)
        return 1
    text = data.decode('utf-8')
    lines = text.split('\n')
    reader = RubyReader(text)

    emit_next_previous(reader)
    emit_parse_tables(reader)
    emit_bespoke(lines)
    count_api_tables(reader)

    out = io.StringIO()
    left = ', '.join(f'{k} {v}' for k, v in sorted(LEFT_OUT.items()))
    for line in HEADER_LINES:
        out.write(line.replace('{left_out}', left) + '\n')
    used = sorted({f for row in ROWS for f in row[7].split()})
    for flag in used:
        out.write(f'# flags: {flag} = {FLAG_TEXT[flag]}\n')
    out.write(csv_line(['name', 'kind', 'format', 'expression', 'from', 'expected', 'expectation', 'flags',
                        'reference']))
    for row in ROWS:
        out.write(csv_line(row))
    sys.stdout.write(out.getvalue())
    return 0


def csv_line(fields):
    """Formats one RFC 4180 record. Fields are quoted when they hold a comma, a quote or a line break, as the csv
    module does, and also when they hold a tab or begin or end with whitespace, so that a reader which trims
    unquoted fields cannot change a whitespace-test expression."""
    out = []
    for value in fields:
        if value and (any(ch in value for ch in ',"\r\n\t') or value != value.strip()):
            out.append('"' + value.replace('"', '""') + '"')
        else:
            out.append(value)
    return ','.join(out) + '\n'


def emit_next_previous(reader: RubyReader):
    nxt = reader.literal_after('  NEXT_TIMES = [', '[')
    for i, entry in enumerate(nxt.value, 1):
        cron, expected, *rest = plain(entry)
        frm = rest[0] if rest else None
        zone = rest[1] if len(rest) > 1 else None
        line = entry.line
        a = adapt(cron)
        tag = f'{i:03d}'
        if a.zone is not None:
            leave_out('cron with its own time zone (next_time and match?)', 2)
            continue
        start = iso(frm) if frm else NOW
        where = f'NEXT_TIMES L{line}' + (f' (run in {zone}; no transition touches the answer)' if zone else '')
        ref = where + ('' if frm else '; from is the file\'s NOW') + note_text(a)
        exp, how, ref1 = apply_override(f'next-times-{tag}', iso(expected), 'upstream', ref)
        add(f'next-times-{tag}', 'next', a.format, a.expression, start, exp, how, a.flags, ref1)
        mref = f'#match? over NEXT_TIMES L{line}' + (f' (run in {zone})' if zone else '') + \
            '; match?(t) written as the next occurrence at or after t being t' + note_text(a)
        exp, how, mref = apply_override(f'match-{tag}', iso(expected), 'upstream', mref)
        add(f'match-{tag}', 'next-inclusive', a.format, a.expression, iso(expected), exp, how, a.flags, mref)

    prv = reader.literal_after('  PREVIOUS_TIMES = [', '[')
    for i, entry in enumerate(prv.value, 1):
        cron, expected, *rest = plain(entry)
        frm = rest[0] if rest else None
        zone = rest[1] if len(rest) > 1 else None
        line = entry.line
        a = adapt(cron)
        tag = f'{i:03d}'
        if a.zone is not None:
            leave_out('cron with its own time zone (previous_time and match?)', 2)
            continue
        start = iso(frm) if frm else NOW
        where = f'PREVIOUS_TIMES L{line}' + (f' (run in {zone})' if zone else '')
        ref = where + note_text(a)
        add(f'previous-times-{tag}', 'previous', a.format, a.expression, start, iso(expected), 'upstream', a.flags, ref)
        mref = f'#previous_time match? check, PREVIOUS_TIMES L{line}; match?(t) written as the next occurrence at ' \
               f'or after t being t' + note_text(a)
        add(f'previous-match-{tag}', 'next-inclusive', a.format, a.expression, iso(expected), iso(expected),
            'upstream', a.flags, mref)


def to_string_rows(prefix: str, entries, group: str):
    """[src, to_cron_s] tables: Bodu's canonical text, adapted from fugit's."""
    for i, entry in enumerate(entries, 1):
        values = plain(entry)
        src, target = values[0], resolve(values[1])
        opts = values[2] if len(values) > 2 else None
        line = entry.line
        name = f'{prefix}-{i:03d}'
        if isinstance(src, str) and '\n' in src:
            leave_out('expression holding a newline, which the line-based table cannot carry')
            continue
        a = adapt(src)
        if a.zone is not None:
            leave_out('cron with a time-zone suffix (parse / to_cron_s)')
            continue
        if target is None:
            add(name, 'invalid', a.format, a.expression, '', '', 'upstream', a.flags,
                f'{group} L{line}; parse returns nil' + note_text(a))
            continue
        flags = list(a.flags)
        ref = f'{group} L{line}; to_cron_s'
        if opts is not None:
            ref += ' with { random: false }'
        if flags:
            add(name, 'to-string', a.format, a.expression, '', target, 'upstream', flags, ref + note_text(a))
            continue
        canonical = adapt_canonical(target)
        extra = f'fugit canonical {target} written {canonical}' if canonical != target else ''
        exp, how, ref1 = apply_override(name, canonical, 'upstream', ref + note_text(a, extra))
        add(name, 'to-string', a.format, a.expression, '', exp, how, flags, ref1)


def emit_parse_tables(reader: RubyReader):
    success = reader.literal_after("    group 'success' do", '[')
    to_string_rows('parse-success', success.value, ".parse success")

    for group, prefix in (("group 'negative monthdays'", 'parse-negative-monthdays'),
                          ("group 'months'", 'parse-months'),
                          ("      group 'weekdays' do", 'parse-weekdays'),
                          ("group 'weekdays #'", 'parse-weekdays-hash')):
        lit = reader.literal_after(group, '[')
        to_string_rows(prefix, lit.value, '.parse ' + group.strip().split("'")[1])

    # timezone group: one test per TZInfo zone plus two offsets, and three unknown-zone rejections.
    leave_out('time-zone suffix tests (the per-zone test over every TZInfo zone counted once, two offsets, three '
              'unknown names)', 1 + 2 + 3)

    failure = reader.literal_after("    group 'failure' do", '[')
    for i, entry in enumerate(failure.value, 1):
        cron = plain(entry)
        name = f'parse-failure-{i:03d}'
        if cron is None:
            leave_out('nil input, which the table cannot express')
            continue
        a = adapt(cron)
        add(name, 'invalid', a.format, a.expression, '', '', 'upstream', a.flags,
            f'.parse failure L{entry.line}; parse returns nil' + note_text(a))

    impossible = reader.literal_after("    group 'impossible days' do", '{')
    for i, (key, val) in enumerate(impossible.value, 1):
        cron, modays = plain(key), plain(val)
        a = adapt(cron)
        name = f'parse-impossible-{i:03d}'
        if modays is None:
            day = int(cron.split()[2].split(',')[0])
            flags = list(a.flags)
            if day <= 31:
                flags.append('impossible-date')
            add(name, 'invalid', a.format, a.expression, '', '', 'upstream', flags,
                f'.parse impossible days L{key.line}; parse returns nil' + note_text(a))
        else:
            add(name, 'valid', a.format, a.expression, '', '', 'upstream', a.flags,
                f'.parse impossible days L{key.line}; asserts fugit\'s months/monthdays arrays {ruby(modays)}; '
                f'only the successful parse is transcribed' + note_text(a))

    weekdays = reader.literal_after("    group 'weekdays' do\n\n      {", '{')
    for i, (key, val) in enumerate(weekdays.value, 1):
        cron = plain(key)
        a = adapt(cron)
        add(f'parse-weekday-arrays-{i:03d}', 'valid', a.format, a.expression, '', '', 'upstream', a.flags,
            f'.parse weekdays L{key.line}; asserts fugit\'s weekdays array {ruby(plain(val))}; only the successful '
            f'parse is transcribed' + note_text(a))

    do_parse = reader.literal_after("  group '.do_parse' do", '[')
    for i, entry in enumerate(do_parse.value, 1):
        cron = plain(entry)
        a = adapt(cron)
        add(f'do-parse-{i:03d}', 'invalid', a.format, a.expression, '', '', 'upstream', a.flags,
            f'.do_parse L{entry.line}; raises ArgumentError' + note_text(a))

    cron_s = reader.literal_after('    cron_s = {', '{')
    for i, (key, val) in enumerate(cron_s.value, 1):
        source, target = plain(key), plain(val)
        a = adapt(source)
        line = key.line
        if a.zone is not None:
            leave_out('#to_cron_s with a time-zone suffix (represents and round-trip tests)', 2)
            continue
        flags = list(a.flags)
        canonical = target if flags else adapt_canonical(target)
        extra = f'fugit canonical {target} written {canonical}' if canonical != target else ''
        add(f'to-cron-s-{i:03d}', 'equal', a.format, a.expression, '', canonical, 'upstream', flags,
            f'#to_cron_s L{line}; assert(parse(source), parse(target))' + note_text(a, extra))
        add(f'to-cron-s-{i:03d}-text', 'to-string', a.format, a.expression, '', canonical, 'upstream', flags,
            f'#to_cron_s L{line}; to_cron_s' + note_text(a, extra))
        add(f'to-cron-s-{i:03d}-roundtrip', 'equal', a.format, a.expression, '', canonical, 'upstream', flags,
            f'#to_cron_s round trip L1587 over the L{line} entry: parse(to_cron_s) equals parse; fugit\'s to_cron_s is '
            f'the target' + note_text(a, extra))

    seconds = reader.literal_after("  group '#seconds' do", '{')
    for i, (key, val) in enumerate(seconds.value, 1):
        cron = plain(key)
        a = adapt(cron)
        add(f'seconds-{i:03d}', 'valid', a.format, a.expression, '', '', 'upstream', a.flags,
            f'#seconds L{key.line}; asserts fugit\'s seconds array {ruby(plain(val))}; only the successful parse is '
            f'transcribed')

    sec6_s = reader.literal_after("  group 'sec6' do", '[')
    for i, entry in enumerate(sec6_s.value, 1):
        s0, s1 = plain(entry)
        a = adapt(s0)
        add(f'sec6-text-{i:03d}', 'to-string', a.format, a.expression, '', s1, 'upstream', a.flags,
            f'sec6 L{entry.line}; to_cron_s')
    reader.pos = reader.text.index('].each do |s0, s1|')
    sec6_a = reader.literal_after('].each do |s0, s1|', '[')
    for i, entry in enumerate(sec6_a.value, 1):
        s, arr = plain(entry)
        a = adapt(s)
        add(f'sec6-array-{i:03d}', 'valid', a.format, a.expression, '', '', 'upstream', a.flags,
            f'sec6 L{entry.line}; asserts fugit\'s to_a {ruby(arr)}; only the successful parse is transcribed')
    sec6_h = reader.literal_after('    [\n      # c: cron, f: from, nt: next_time, pt: previous_time', '[')
    for i, entry in enumerate(sec6_h.value, 1):
        h = dict(plain(entry))
        a = adapt(h['c'])
        if 'nt' in h:
            add(f'sec6-next-{i:03d}', 'next', a.format, a.expression, iso(h['f']), iso(h['nt']), 'upstream', a.flags,
                f'sec6 L{entry.line}; next_time')
        else:
            add(f'sec6-previous-{i:03d}', 'previous', a.format, a.expression, iso(h['f']), iso(h['pt']), 'upstream',
                a.flags, f'sec6 L{entry.line}; previous_time')

    parse_cron = reader.literal_after("  group '#parse_cron' do", '[')
    for i, entry in enumerate(parse_cron.value, 1):
        src, target = plain(entry)
        name = f'parse-cron-{i:03d}'
        if '\n' in src:
            leave_out('expression holding a newline, which the line-based table cannot carry')
            continue
        a = adapt(src)
        if a.zone is not None:
            leave_out('cron with a time-zone suffix (parse / to_cron_s)')
            continue
        canonical = adapt_canonical(target)
        extra = f'fugit canonical {target} written {canonical}' if canonical != target else ''
        add(name, 'to-string', a.format, a.expression, '', canonical, 'upstream', a.flags,
            f'#parse_cron L{entry.line}; class and to_cron_s' + note_text(a, extra))


# --------------------------------------------------------------------------------------------------
# Bespoke tests asserting fixed instants, maintained by hand. Each entry gives the line it reads and a
# snippet the line must contain, so a drift in the upstream file fails loudly.
# --------------------------------------------------------------------------------------------------

LONG = '* * * * * ' * 100

BESPOKE = [
    # (line, snippet, name, kind, format, expression, from, expected, expectation, flags, reference)
    (692, "test \"doesn't skip\" do", 'gh43-next', 'next', 'standard', '0 8-19/4 * * *',
     '2020-09-11T12:00:00', '2020-09-11T16:00:00', 'upstream', '',
     "New York skip (gh-43) L692 \"doesn't skip\"; asserts nt.to_s =~ / 16:00:00 / (same day)"),
    (707, "test \"doesn't skip (TZ UTC)\" do", 'gh43-next-utc', 'next', 'standard', '0 8-19/4 * * *',
     '2020-09-11T12:00:00', '2020-09-11T16:00:00', 'upstream', '',
     "New York skip (gh-43) L707 \"doesn't skip (TZ UTC)\"; run in UTC; asserts nt.utc.to_s =~ / 16:00:00 /"),
    (746, "test 'does not break on \"* * * * 1%2+2\" (gh-47)' do", 'gh47-next-a', 'next', 'standard',
     '0 8 * * 1%2+2', '2021-04-21T07:00:00', '2021-05-03T08:00:00', 'upstream', 'syntax-extension',
     'gh-47 L746; next_time(2021-04-21 07:00:00).to_s =~ /^2021-05-03 08:00:00 /'),
    (746, "test 'does not break on \"* * * * 1%2+2\" (gh-47)' do", 'gh47-next-b', 'next', 'standard',
     '0 8 * * 1%2', '2021-04-21T07:00:00', '2021-05-03T08:00:00', 'upstream', 'syntax-extension',
     'gh-47 L746; asserts 0 8 * * 1%2 gives the same next_time as 0 8 * * 1%2+2'),
    (1081, "test 'returns nil if test cannot parse' do", 'parse-nada', 'invalid', 'standard', 'nada', '', '',
     'upstream', '', ".parse L1081 'returns nil if test cannot parse'; parse('nada') is nil"),
    (1528, "test 'returns true when equal' do", 'equality-001', 'equal', 'standard', '* * * * *', '',
     '* * * * *', 'upstream', '', "#== L1528 'returns true when equal'"),
    (1528, "test 'returns true when equal' do", 'equality-002', 'equal', 'standard', '* * * * *', '',
     '* * */1 * *', 'upstream', '', "#== L1528 'returns true when equal'"),
    (1539, "test 'returns false else' do", 'equality-003', 'not-equal', 'standard', '* * * * *', '',
     '* * * * 1', 'upstream', '', "#== L1539 'returns false else'; == is false"),
    (1539, "test 'returns false else' do", 'equality-004', 'not-equal', 'standard', '* * * * *', '',
     '* * * * 1', 'upstream', '', "#== L1539 'returns false else'; != is true"),
    (1714, "test 'returns an iterator' do", 'iterator-next', 'next-sequence', 'standard', '0 12 * * mon#2',
     '2024-02-16T12:00:00',
     '2024-03-11T12:00:00|2024-04-08T12:00:00|2024-05-13T12:00:00|2024-06-10T12:00:00|2024-07-08T12:00:00',
     'upstream', '', "#next L1714 'returns an iterator'; run in UTC; next('2024-02-16 12:00:00').take(5)"),
    (1736, "test 'returns an iterator' do", 'iterator-prev', 'previous-sequence', 'standard', '0 12 * * mon#2',
     '2024-02-16T12:00:00',
     '2024-02-12T12:00:00|2024-01-08T12:00:00|2023-12-11T12:00:00|2023-11-13T12:00:00|2023-10-09T12:00:00',
     'upstream', '', "#prev L1736 'returns an iterator'; run in UTC; prev(Time.parse('2024-02-16 12:00:00')).take(5)"),
    (1757, "test 'returns all the occurrences within a given time period' do", 'within-range', 'next-sequence',
     'standard', '0 12 * * mon#2', '2024-02-16T12:00:00',
     '2024-03-11T12:00:00|2024-04-08T12:00:00|2024-05-13T12:00:00|2024-06-10T12:00:00|2024-07-08T12:00:00',
     'upstream', '', "#within L1757; run in UTC; within(2024-02-16 12:00..2024-08-01 12:00); the occurrences "
     "after the start, in order (the end bound itself is not a table kind)"),
    (1776, "test 'returns all the occurrences within a start time and an end time' do", 'within-start-end',
     'next-sequence', 'standard', '0 12 * * mon#2', '2024-01-16T12:00:00',
     '2024-02-12T12:00:00|2024-03-11T12:00:00|2024-04-08T12:00:00|2024-05-13T12:00:00|2024-06-10T12:00:00',
     'upstream', '', "#within L1776; run in UTC; within(2024-01-16 12:00, '2024-07-01 12:00'); the occurrences "
     "after the start, in order (the end bound itself is not a table kind)"),
    (1803, "test 'runs \"0 12 * * sun%2+1,wed%3+1\" as Wed+Sun then 1w pause' do", 'modulo-monday-ref',
     'next-sequence', 'standard', '0 12 * * 0%2+1,3%2+1', '2025-09-25T00:00:00',
     '2025-09-28T12:00:00|2025-10-08T12:00:00|2025-10-12T12:00:00|2025-10-22T12:00:00|2025-10-26T12:00:00|'
     '2025-11-05T12:00:00|2025-11-09T12:00:00', 'upstream', 'syntax-extension',
     "modulo extension L1803; EtOrbi.rweek_ref = :monday; next('2025-09-25').take(7)"),
    (1824, "test 'runs \"0 12 * * sun%2+1,wed%3+1\" as Sun+Wed then 1w pause' do", 'modulo-sunday-ref',
     'next-sequence', 'standard', '0 12 * * 0%2+1,3%2+1', '2025-09-25T00:00:00',
     '2025-10-05T12:00:00|2025-10-08T12:00:00|2025-10-19T12:00:00|2025-10-22T12:00:00|2025-11-02T12:00:00|'
     '2025-11-05T12:00:00|2025-11-16T12:00:00', 'upstream', 'syntax-extension',
     "modulo extension L1824; EtOrbi.rweek_ref = :sunday; next('2025-09-25').take(7)"),
    (1847, "test 'runs every fourth week on Tuesday' do", 'modulo-tuesday', 'next-sequence', 'standard',
     '20 0 * * 2%4', '2025-09-25T00:00:00',
     '2025-09-30T00:20:00|2025-10-28T00:20:00|2025-11-25T00:20:00|2025-12-23T00:20:00|2026-01-20T00:20:00|'
     '2026-02-17T00:20:00|2026-03-17T00:20:00|2026-04-14T00:20:00|2026-05-12T00:20:00|2026-06-09T00:20:00|'
     '2026-07-07T00:20:00|2026-08-04T00:20:00|2026-09-01T00:20:00|2026-09-29T00:20:00', 'upstream',
     'syntax-extension', "modulo extension L1847; next('2025-09-25').take(14)"),
    (1877, "test 'runs every fourth week and fourth week + 1 on Wednesday' do", 'modulo-wednesday-a',
     'next-sequence', 'standard', '12 0 * * wed%4,wed%4+1', '2025-09-25T00:00:00',
     '2025-10-01T00:12:00|2025-10-08T00:12:00|2025-10-29T00:12:00|2025-11-05T00:12:00|2025-11-26T00:12:00|'
     '2025-12-03T00:12:00|2025-12-24T00:12:00|2025-12-31T00:12:00|2026-01-21T00:12:00|2026-01-28T00:12:00|'
     '2026-02-18T00:12:00|2026-02-25T00:12:00|2026-03-18T00:12:00|2026-03-25T00:12:00', 'upstream',
     'syntax-extension', "modulo extension L1877; next('2025-09-25').take(14), first assertion"),
    (1877, "test 'runs every fourth week and fourth week + 1 on Wednesday' do", 'modulo-wednesday-b',
     'next-sequence', 'standard', '12 0 * * wed%4+1,wed%4', '2025-09-25T00:00:00',
     '2025-10-01T00:12:00|2025-10-08T00:12:00|2025-10-29T00:12:00|2025-11-05T00:12:00|2025-11-26T00:12:00|'
     '2025-12-03T00:12:00|2025-12-24T00:12:00|2025-12-31T00:12:00|2026-01-21T00:12:00|2026-01-28T00:12:00|'
     '2026-02-18T00:12:00|2026-02-25T00:12:00|2026-03-18T00:12:00|2026-03-25T00:12:00', 'upstream',
     'syntax-extension', "modulo extension L1877; next('2025-09-25').take(14), second assertion"),
    (1940, "test 'chokes on input that is too long' do", 'do-parse-cron-too-long', 'invalid', 'standard', LONG,
     '', '', 'upstream', '',
     "#do_parse_cron L1940; '* * * * * ' * 100 raises (fugit: input longer than 256; Bodu: 500 fields)"),
    (1971, "test 'returns nil if the input is too long' do", 'parse-cron-too-long', 'invalid', 'standard', LONG,
     '', '', 'upstream', '',
     "#parse_cron L1971; '* * * * * ' * 100 is nil (fugit: input longer than 256; Bodu: 500 fields)"),
]

# Bespoke tests left out, by reason (line numbers in the comment).
BESPOKE_LEFT_OUT = [
    # 201 (implicit tz DST transition), 214, 236, 258, 281, 305, 329, 354, 380, 404, 430, 455, 482, 509, 539, 566
    ('time-zone transition (DST groups)', 16),
    # 594 'returns a plain second' reads the wall clock; 932 gh-15 previous_time of the wall-clock occurrence
    ('relative to the wall clock', 2),
    # 604, 614 explicit timezone; 725 New York skip in an ActiveSupport zone; 759 gh-76 (zone and %);
    # 803, 810 gh-31 match? in a cron zone
    ('time-zone dependent (cron or from in a named zone)', 6),
    # 626, 917 loop breaker over a forged cron; 654, 953 defective et-orbi; 679 Chronic/ActiveSupport (no
    # fixed instant); 1071 returns the same object; 1081 parse(true); 1087 'parses @reboot' (pending, no
    # body); 1539 parse == 1
    ('library API with no cron meaning', 9),
]


def emit_bespoke(lines: list[str]):
    for (line, snippet, name, kind, fmt, expression, frm, expected, expectation, flags, reference) in BESPOKE:
        if snippet not in lines[line - 1]:
            raise SystemExit(f'line {line} no longer reads {snippet!r}: {lines[line - 1]!r}')
        add(name, kind, fmt, expression, frm, expected, expectation, flags.split(), reference)
    for reason, n in BESPOKE_LEFT_OUT:
        leave_out(reason, n)


def count_api_tables(reader: RubyReader):
    brute = reader.literal_after("  group '#brute_frequency' do", '[')
    rough = reader.literal_after("  group '#rough_frequency' do", '{')
    # #range's table holds a Ruby expression ((0..23).to_a) the reader does not evaluate, so its rows are
    # counted by their => lines, comments excluded.
    at = reader.text.index("  group '#range (protected)' do")
    end = reader.text.index('}.each do |args, result|', at)
    ranges = sum(1 for ln in reader.text[at:end].split('\n') if '=>' in ln and not ln.strip().startswith('#'))
    leave_out('library API with no cron meaning (#brute_frequency, #rough_frequency, #range)',
              len(brute.value) + 1 + len(rough.value) + ranges)


if __name__ == '__main__':
    sys.exit(main(sys.argv))
