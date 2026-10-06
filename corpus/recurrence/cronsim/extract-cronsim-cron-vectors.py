#!/usr/bin/env python3
"""Derives cronsim-cron-vectors.csv from cronsim's tests/test_cronsim.py.

Usage:

    python3 extract-cronsim-cron-vectors.py <path to tests/test_cronsim.py> > cronsim-cron-vectors.csv

The upstream file is checked against the SHA-256 it was read at. Two tests are tables and are read from the file:
test_it_rejects_bad_values (every pattern with every bad value) and TestReverse (its samples). Every other test is a
bespoke method, transcribed in HAND_ROWS below; each of those rows names the lines it came from and a fragment of
them, and the script checks that the fragment is on those lines.

TestReverse asserts that iterating backwards from the fifth occurrence after NOW gives back the first four. The test
computes those occurrences at run time, so this script recomputes them the same way, with cronsim 2.7 itself: run it
with a Python that has cronsim 2.7 installed. The output is deterministic.
"""

from __future__ import annotations

import ast
import csv
import hashlib
import io
import sys
from datetime import datetime, timezone
from importlib.metadata import version

from cronsim import CronSim

EXPECTED_SHA256 = '87be87591a61e18350eebdcfe0556cd41c66398e9bc3326d65a7ee71c2b58698'
CRONSIM_VERSION = '2.7'

HEADER = """# Cron vectors derived from cronsim's tests/test_cronsim.py
# source-class: third-party-comparison (implementation test suite, not an authority)
# source: cronsim -- https://github.com/cuu508/cronsim -- tests/test_cronsim.py
# commit: fd2e617787e94b15beee27fee6ebe6cbe79a72a2
# file-sha256: tests/test_cronsim.py 87be87591a61e18350eebdcfe0556cd41c66398e9bc3326d65a7ee71c2b58698
# licence: BSD-3-Clause (Copyright (c) 2021, Peteris Caune). Each row restates one assertion of the named tests (expression, query instant and answer) in Bodu's syntax, with the test and line it came from; no test code is reproduced.
# method: extracted by extract-cronsim-cron-vectors.py: test_it_rejects_bad_values and the TestReverse samples are read from the file, the other tests are a hand-maintained list in the script, each checked against its cited lines
# note: cronsim reads the day fields as Vixie and Bodu do (an intersection when either starts with *) and puts the seconds first in six fields; it rejects at parse time a day-of-month its month never reaches and a weekday name before L, and accepts L/2, LW/2 and a list mixing values with a token; its field-set tests are restated as equality with the set written out, or as acceptance where the field is written as its own set; a forward query is strictly after its instant, and a backward one starts from an occurrence, so it is Bodu's exclusive previous; TestReverse's forward occurrences are cronsim 2.7's own answers, recomputed by the extractor
# note: left out: the 36 TestDstTransitions tests and TestReverse's test_it_handles_dst_mar and test_it_handles_dst_oct (a named time zone across daylight-saving transitions); TestOptimizations (2 tests) and TestExplain (2 tests), which test cronsim's own API
# flags: library-rejects = cronsim rejects an expression Bodu documents as valid: a day-of-month its month never reaches (* * 30 2 *, which Bodu accepts and answers null) or a weekday name before L (MONL, Bodu's last Monday)
# flags: token-list = a Quartz token stepped (L/2, LW/2) or in a list (1,2,3#1), which cronsim accepts and Bodu rejects
"""

COLUMNS = ['name', 'kind', 'format', 'expression', 'from', 'expected', 'expectation', 'flags', 'reference']

S, W = 'standard', 'withSeconds'
UP = 'upstream'
SET = 'restated as equality with the field written as that set'
OWN = 'the asserted set is the field as written, so only the acceptance is transcribed'
NOW_TEXT = '2020-01-01T00:00:00'

# Bespoke tests: (name, kind, format, expression, from, expected, flags, (first line, last line), fragment, note).
HAND_ROWS = [
    # TestParse: the field sets cronsim exposes as attributes.
    ('test_it_parses_stars-01', 'equal', S, '* * * * *', '', '0-59 * * * *', '', (15, 17), 'w.minutes', f'minutes; {SET}'),
    ('test_it_parses_stars-02', 'equal', S, '* * * * *', '', '* 0-23 * * *', '', (15, 18), 'w.hours', f'hours; {SET}'),
    ('test_it_parses_stars-03', 'equal', S, '* * * * *', '', '* * 1-31 * *', '', (15, 19), 'w.days', f'days; {SET}'),
    ('test_it_parses_stars-04', 'equal', S, '* * * * *', '', '* * * 1-12 *', '', (15, 20), 'w.months',
     f'months; {SET}'),
    ('test_it_parses_stars-05', 'equal', S, '* * * * *', '', '* * * * 0-7', '', (15, 21), 'w.weekdays',
     f'weekdays; {SET}'),
    ('test_it_parses_6th_field-01', 'equal', W, '* * * * * *', '', '0-59 * * * * *', '', (23, 25), 'w.seconds', SET),
    ('test_it_parses_numbers-01', 'valid', S, '1 * * * *', '', '', '', (27, 29), '"1 * * * *"', OWN),
    ('test_it_parses_weekday-01', 'valid', S, '* * * * 1', '', '', '', (31, 33), '"* * * * 1"', OWN),
    ('test_it_handles_0_sunday-01', 'valid', S, '* * * * 0', '', '', '', (35, 37), '"* * * * 0"', OWN),
    ('test_it_parses_list-01', 'valid', S, '1,2,3 * * * *', '', '', '', (39, 41), '"1,2,3 * * * *"', OWN),
    ('test_it_parses_interval-01', 'equal', S, '1-3 * * * *', '', '1,2,3 * * * *', '', (43, 45), '"1-3 * * * *"', SET),
    ('test_it_parses_two_intervals-01', 'equal', S, '1-3,7-9 * * * *', '', '1,2,3,7,8,9 * * * *', '', (47, 49),
     '"1-3,7-9 * * * *"', SET),
    ('test_it_parses_step-01', 'equal', S, '*/15 * * * *', '', '0,15,30,45 * * * *', '', (51, 53), '"*/15 * * * *"',
     SET),
    ('test_it_parses_interval_with_step-01', 'equal', S, '0-10/2 * * * *', '', '0,2,4,6,8,10 * * * *', '', (55, 57),
     '"0-10/2 * * * *"', SET),
    ('test_it_parses_start_with_step-01', 'equal', S, '5/15 * * * *', '', '5,20,35,50 * * * *', '', (59, 61),
     '"5/15 * * * *"', SET),
    ('test_it_parses_day_l-01', 'valid', S, '* * L * *', '', '', '', (63, 65), '"* * L * *"', OWN),
    ('test_it_parses_day_lw-01', 'valid', S, '* * LW * *', '', '', '', (67, 69), '"* * LW * *"', OWN),
    ('test_it_parses_day_lowercase_l-01', 'equal', S, '* * l * *', '', '* * L * *', '', (71, 73), '"* * l * *"',
     'the lowercase token is the LAST token; restated as equality with L'),
    ('test_it_parses_day_lowercase_lw-01', 'equal', S, '* * lw * *', '', '* * LW * *', '', (75, 77), '"* * lw * *"',
     'the lowercase token is the LAST_WEEKDAY token; restated as equality with LW'),
    ('test_it_parses_unrestricted_day_restricted_dow-01', 'equal', S, '* * * * 1', '', '* * */1 * 1', '', (79, 83),
     'w.day_and', 'days 1-31 with day_and (an intersection); restated as equality with */1, which selects every day '
     'without restricting, so the fields still intersect'),
    ('test_it_parses_restricted_day_unrestricted_dow-01', 'equal', S, '* * 1 * *', '', '* * 1 * */1', '', (85, 89),
     'w.day_and', 'weekdays 0-7 with day_and (an intersection); restated as equality with */1, which selects every day '
     'without restricting, so the fields still intersect'),
    ('test_it_parses_nth_weekday-01', 'valid', S, '* * * * 1#2', '', '', '', (91, 93), '"* * * * 1#2"', OWN),
    ('test_it_parses_symbolic_weekday-01', 'equal', S, '* * * * MON', '', '* * * * 1', '', (95, 97), '"* * * * MON"', SET),
    ('test_it_parses_lowercase_symbolic_weekday-01', 'equal', S, '* * * * mon', '', '* * * * 1', '', (99, 101),
     '"* * * * mon"', SET),
    ('test_it_parses_symbolic_month-01', 'equal', S, '* * * JAN *', '', '* * * 1 *', '', (103, 105), '"* * * JAN *"',
     SET),
    ('test_it_parses_weekday_range_from_zero-01', 'equal', S, '* * * * 0-2', '', '* * * * 0,1,2', '', (107, 109),
     '"* * * * 0-2"', SET),
    ('test_it_parses_sun_tue-01', 'equal', S, '* * * * sun-tue', '', '* * * * 0,1,2', '', (111, 113),
     '"* * * * sun-tue"', SET),
    ('test_it_starts_weekday_step_from_zero-01', 'equal', S, '* * * * */2', '', '* * * * 0,2,4,6', '', (115, 117),
     '"* * * * */2"', SET),
    ('test_it_accepts_l_with_step-01', 'equal', S, '* * L/2 * *', '', '* * L * *', 'token-list', (119, 121),
     '"* * L/2 * *"', 'the step is ignored; restated as equality with L'),
    ('test_it_accepts_lw_with_step-01', 'equal', S, '* * LW/2 * *', '', '* * LW * *', 'token-list', (123, 125),
     '"* * LW/2 * *"', 'the step is ignored; restated as equality with LW'),
    ('test_it_handles_a_mix_of_ints_and_tuples-01', 'valid', S, '* * * * 1,2,3#1', '', '', 'token-list', (127, 129),
     '"* * * * 1,2,3#1"', 'the set mixes values with a token, which has no Bodu spelling; only the acceptance is '
     'transcribed'),
    ('test_it_accepts_weekday_7-01', 'valid', S, '* * * * 7', '', '', '', (131, 133), '"* * * * 7"', OWN),
    ('test_it_accepts_weekday_l-01', 'valid', S, '* * * * 5L', '', '', '', (135, 137), '"* * * * 5L"', OWN),
    # TestValidation, apart from the bad-value table.
    ('test_it_rejects_4_components-01', 'invalid', S, '* * * *', '', '', '', (141, 143), '"* * * *"', ''),
    ('test_it_rejects_lopsided_range-01', 'invalid', S, '* * 5-1 * *', '', '', '', (186, 188), '"* * 5-1 * *"', ''),
    ('test_it_rejects_underscores-01', 'invalid', S, '1-1_0 * * * *', '', '', '', (190, 192), '"1-1_0 * * * *"', ''),
    ('test_it_rejects_zero_step-01', 'invalid', S, '*/0 * * * *', '', '', '', (194, 196), '"*/0 * * * *"', ''),
    ('test_it_rejects_zero_nth-01', 'invalid', S, '* * * * 1#0', '', '', '', (198, 200), '"* * * * 1#0"', ''),
    ('test_it_rejects_big_nth-01', 'invalid', S, '* * * * 1#6', '', '', '', (202, 204), '"* * * * 1#6"', ''),
    ('test_it_checks_day_of_month_range-01', 'invalid', S, '* * 30 2 *', '', '', 'library-rejects', (206, 208),
     '"* * 30 2 *"', ''),
    ('test_it_checks_day_of_month_range-02', 'invalid', S, '* * 31 4 *', '', '', 'library-rejects', (210, 211),
     '"* * 31 4 *"', ''),
    ('test_it_rejects_dow_l_range-01', 'invalid', S, '* * * * 5L-6', '', '', '', (213, 215), '"* * * * 5L-6"', ''),
    ('test_it_rejects_dow_l_hash-01', 'invalid', S, '* * * * 5L#1', '', '', '', (217, 219), '"* * * * 5L#1"', ''),
    ('test_it_rejects_dow_l_slash-01', 'invalid', S, '* * * * 5L/3', '', '', '', (221, 223), '"* * * * 5L/3"', ''),
    ('test_it_rejects_symbolic_dow_l-01', 'invalid', S, '* * * * MONL', '', '', 'library-rejects', (225, 227),
     '"* * * * MONL"', ''),
    # TestIterator: every query starts at NOW (L11), and cronsim answers strictly after it.
    ('test_it_handles_l-01', 'next', S, '1 1 L * *', NOW_TEXT, '2020-01-31T01:01:00', '', (234, 236), '"1 1 L * *"', ''),
    ('test_it_handles_lw-01', 'next', S, '1 1 LW 5 *', NOW_TEXT, '2020-05-29T01:01:00', '', (238, 240), '"1 1 LW 5 *"',
     ''),
    ('test_it_handles_last_friday-01', 'next-sequence', S, '1 1 * * 5L', NOW_TEXT,
     '2020-01-31T01:01:00|2020-02-28T01:01:00|2020-03-27T01:01:00|2020-04-24T01:01:00|2020-05-29T01:01:00|'
     '2020-06-26T01:01:00|2020-07-31T01:01:00|2020-08-28T01:01:00|2020-09-25T01:01:00|2020-10-30T01:01:00|'
     '2020-11-27T01:01:00|2020-12-25T01:01:00', '', (242, 255), '"1 1 * * 5L"', ''),
    ('test_it_handles_last_sunday_two_notations-01', 'next', S, '1 1 * * 0L', NOW_TEXT, '2020-01-26T01:01:00', '',
     (257, 260), '"1 1 * * 0L"', ''),
    ('test_it_handles_last_sunday_two_notations-02', 'next', S, '1 1 * * 7L', NOW_TEXT, '2020-01-26T01:01:00', '',
     (257, 260), '"1 1 * * 7L"', ''),
    ('test_it_handles_nth_weekday-01', 'next', S, '1 1 * * 1#2', NOW_TEXT, '2020-01-13T01:01:00', '', (262, 264),
     '"1 1 * * 1#2"', ''),
    ('test_it_handles_dow_star-01', 'next-sequence', S, '1 1 1-7 * */7', NOW_TEXT,
     '2020-01-05T01:01:00|2020-02-02T01:01:00|2020-03-01T01:01:00|2020-04-05T01:01:00', '', (266, 272),
     '"1 1 1-7 * */7"', ''),
    ('test_it_handles_dom_star-01', 'next-sequence', S, '1 1 */100,1-7 * MON', NOW_TEXT,
     '2020-01-06T01:01:00|2020-02-03T01:01:00|2020-03-02T01:01:00|2020-04-06T01:01:00', '', (274, 280),
     '"1 1 */100,1-7 * MON"', ''),
    ('test_it_handles_no_matches-01', 'unreachable-next', S, '1 1 */100 * MON#4', NOW_TEXT, '', '', (282, 287),
     '"1 1 */100 * MON#4"', 'StopIteration'),
    ('test_it_handles_every_x_weekdays-01', 'next-sequence', S, '1 1 * * */3', NOW_TEXT,
     '2020-01-01T01:01:00|2020-01-04T01:01:00|2020-01-05T01:01:00|2020-01-08T01:01:00', '', (289, 295),
     '"1 1 * * */3"', ''),
    ('test_it_handles_seconds-01', 'next-sequence', W, '*/15 * * * * *', NOW_TEXT,
     '2020-01-01T00:00:15|2020-01-01T00:00:30|2020-01-01T00:00:45|2020-01-01T00:01:00|2020-01-01T00:01:15', '',
     (297, 303), '"*/15 * * * * *"', ''),
    # TestReverse.test_it_handles_no_matches: backwards from NOW, which is already a whole minute.
    ('TestReverse.test_it_handles_no_matches-01', 'unreachable-previous', S, '1 1 */100 * MON#4', NOW_TEXT, '', '',
     (667, 672), '"1 1 */100 * MON#4", NOW, reverse=True', 'StopIteration; reverse=True from a whole minute is '
     'Bodu\'s exclusive previous'),
]


def fail(message: str) -> None:
    sys.exit(f'extract-cronsim-cron-vectors: {message}')


def fmt(value: datetime) -> str:
    text = value.strftime('%Y-%m-%dT%H:%M:%S')
    if value.tzinfo is not None:
        offset = value.utcoffset()
        minutes = int(offset.total_seconds()) // 60
        sign = '+' if minutes >= 0 else '-'
        text += f'{sign}{abs(minutes) // 60:02}:{abs(minutes) % 60:02}'
    return text


def layout(expression: str) -> str:
    """Five fields are the standard layout; six or more are read with the seconds layout (cronsim's sixth field is
    the seconds, first, as in Bodu)."""
    return S if len(expression.split()) == 5 else W


def find_class(tree: ast.Module, name: str) -> ast.ClassDef:
    for node in tree.body:
        if isinstance(node, ast.ClassDef) and node.name == name:
            return node
    fail(f'class {name} not found')


def find_assignment(scope: ast.AST, target: str) -> ast.expr:
    for node in ast.walk(scope):
        if isinstance(node, ast.Assign) and any(isinstance(t, ast.Name) and t.id == target for t in node.targets):
            return node.value
    fail(f'assignment to {target} not found')


def literal_elements(value: ast.expr) -> list[tuple[str, int]]:
    """Returns each string element of a tuple or list literal with its line number."""
    if not isinstance(value, (ast.Tuple, ast.List)):
        fail('expected a tuple or list literal')
    return [(ast.literal_eval(element), element.lineno) for element in value.elts]


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        fail('usage: extract-cronsim-cron-vectors.py <tests/test_cronsim.py>')
    data = open(argv[1], 'rb').read()
    digest = hashlib.sha256(data).hexdigest()
    if digest != EXPECTED_SHA256:
        fail(f'SHA-256 {digest} does not match the recorded {EXPECTED_SHA256}')
    if version('cronsim') != CRONSIM_VERSION:
        fail(f'cronsim {version("cronsim")} is installed; the TestReverse occurrences were recorded with '
             f'{CRONSIM_VERSION}')
    source = data.decode('utf-8')
    lines = source.split('\n')
    tree = ast.parse(source)

    # NOW is the module-level datetime(2020, 1, 1) every iterator and reverse test starts from.
    now_node = find_assignment(tree, 'NOW')
    now = datetime(*[ast.literal_eval(a) for a in now_node.args])
    if now != datetime(2020, 1, 1):
        fail(f'NOW is {now}, not 2020-01-01')

    rows: list[list[str]] = []

    # Bespoke tests, each checked against its cited lines.
    for name, kind, form, expression, start, expected, flags, (first, last), fragment, note in HAND_ROWS:
        cited = '\n'.join(lines[first - 1:last])
        if fragment not in cited:
            fail(f'{name}: {fragment!r} is not on lines {first}-{last}')
        test = name.rsplit('-', 1)[0]
        reference = f'{test} L{first}-{last}' + (f'; {note}' if note else '')
        rows.append([name, kind, form, expression, start, expected, UP, flags, reference])

    # test_it_rejects_bad_values: every pattern with every bad value must be rejected.
    validation = find_class(tree, 'TestValidation')
    bad_test = next(n for n in validation.body if isinstance(n, ast.FunctionDef) and n.name == 'test_it_rejects_bad_values')
    patterns = literal_elements(find_assignment(bad_test, 'patterns'))
    bad_values = literal_elements(find_assignment(bad_test, 'bad_values'))
    count = 0
    for p_index, (pattern, p_line) in enumerate(patterns, 1):
        for v_index, (value, v_line) in enumerate(bad_values, 1):
            count += 1
            # One pattern ends in a space ("* * * %s * "); cron trims the text, so the trailing space is dropped
            # rather than carried into a CSV field where readers may trim it inconsistently.
            expression = (pattern % value).strip()
            rows.append([f'test_it_rejects_bad_values-{count:03}', 'invalid', layout(expression), expression, '', '',
                         UP, '', f'test_it_rejects_bad_values L182-184: pattern {p_index} (L{p_line}) with bad value '
                         f'{v_index} (L{v_line})'])

    # TestReverse: five occurrences forward from NOW, then backwards from the fifth must give back the other four.
    reverse = find_class(tree, 'TestReverse')
    samples = literal_elements(find_assignment(reverse, 'samples'))
    for index, (expression, line) in enumerate(samples, 1):
        forward = CronSim(expression, now)
        crumbs = [next(forward) for _ in range(5)]
        rows.append([f'TestReverse.test_it_handles_naive_datetime-{index:02}', 'previous-sequence', layout(expression),
                     expression, fmt(crumbs[4]), '|'.join(fmt(c) for c in reversed(crumbs[:4])), UP, '',
                     f'test_it_handles_naive_datetime L648-650 via _test L640-646, sample {index} (L{line}): backwards '
                     'from the fifth occurrence after NOW, which cronsim 2.7 gives forward, the four before it'])
    utc_now = now.replace(tzinfo=timezone.utc)
    count = 0
    for index, (expression, line) in enumerate(samples, 1):
        forward = CronSim(expression, utc_now)
        crumbs = [next(forward) for _ in range(5)]
        # A sequence cannot carry an offset in this schema, so each backward step is a row of its own.
        for step in range(4, 0, -1):
            count += 1
            rows.append([f'TestReverse.test_it_handles_utc-{count:03}', 'previous', layout(expression), expression,
                         fmt(crumbs[step]), fmt(crumbs[step - 1]), UP, '',
                         f'test_it_handles_utc L652-655 via _test L640-646, sample {index} (L{line}), step {5 - step} '
                         'of 4: the occurrence before the one recorded forward, in UTC'])

    out = io.StringIO()
    out.write(HEADER)
    writer = csv.writer(out, lineterminator='\n')
    writer.writerow(COLUMNS)
    writer.writerows(rows)
    sys.stdout.write(out.getvalue())
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
