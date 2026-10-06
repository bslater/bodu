#!/usr/bin/env python3
"""Derive a Bodu cron vector table from dragonmantank/cron-expression's tests (tests/Cron/*Test.php).

dragonmantank/cron-expression (https://github.com/dragonmantank/cron-expression, MIT) is the PHP cron library
Laravel uses, continuing mtdowling/cron-expression. Its CronExpressionTest.php drives most occurrence checks through
one data provider, ``scheduleProvider``, whose rows are ``[expression, start, nextRun, isDue]``; this script parses
those rows from the file. The other assertions in the seven test files are one-off, so they are listed below by
hand, each with the file and line it comes from, and the script checks that the cited line still holds the text the
row was taken from.

Usage:

    python3 extract-dragonmantank-cron-vectors.py <tests/Cron directory> > dragonmantank-cron-vectors.csv

The script checks every file's SHA-256 against the commit the table records and writes the table to stdout. The
output is deterministic.

How a schedule row maps to Bodu: the test calls ``isDue(start)`` and ``getNextRunDate(start, 0, true)``. The library
drops the seconds of the start before it searches, and ``true`` lets the start's own minute count, so the row is an
inclusive next from the start truncated to the minute; isDue is true exactly when that answer is the start's minute,
which the script checks. The library's five fields are Bodu's standard layout, weekdays 0-7 with 0 and 7 for
Sunday, and its macros are case-insensitive, as Bodu's are.
"""

from __future__ import annotations

import hashlib
import os
import re
import sys

COMMIT = 'd425a2403c17d7cf911c55a7170f073979a9f382'

# The seven files the rows come from, with the SHA-256 of each at COMMIT.
FILES = {
    'CronExpressionTest.php': '53aaa3c42028e7c6e47b0bafd8d0d0a00734472097675c3327d6dabc3ec1115c',
    'DayOfMonthFieldTest.php': '66b02bffae4712ef036ef83e036ad963c8e112f2ab21248f67987b5ba58345d8',
    'DayOfWeekFieldTest.php': 'c02e3758e974e4ea78c7618161624f0b1d32707a0a5fd418e1ccf24be1928173',
    'HoursFieldTest.php': '145fe1d1277be30ae9ff0d7032e9b56ff39c2fc3c8581bd43c735057ca36850f',
    'MinutesFieldTest.php': '71d9639ac4dd9986b16399ce58446bc55977dda74cdb6f794917e11be2cb25d3',
    'MonthFieldTest.php': '01505a1cca780c61b5d8a82630c11d856ee96288240268d4b1959ddb0daa1f81',
    'AbstractFieldTest.php': '82688244a7fa0c6386abb15ddd58a6b0dc3ca0228f6e315b3e5e54de4045428b',
}

HEADER = """\
# Cron vectors derived from dragonmantank/cron-expression's tests/Cron/*Test.php
# source-class: third-party-comparison (implementation test suite, not an authority)
# source: dragonmantank/cron-expression -- https://github.com/dragonmantank/cron-expression -- {sources}
# commit: {commit}
{hashes}
# licence: MIT (Copyright (c) 2011 Michael Dowling <mtdowling@gmail.com>, 2016 Chris Tankersley <chris@ctankersley.com>, and contributors). The rows restate the files' assertions as expressions and instants, with attribution; the upstream files are not committed.
# method: extracted by extract-dragonmantank-cron-vectors.py (scheduleProvider rows parsed; the other assertions listed in the script with their lines)
# note: the library reads five fields, weekdays 0-7 (7-4 and 6-0 renumber Sunday instead of failing), and takes the union of the day fields whenever neither is literally * or ?, so a star-led */15 still counts as restricted; it drops the start's seconds before searching, rejects a step on a single value (1/10, 0/5) and ? in both day fields, wraps a step wider than its range around the field (*/123 in months is April), and accepts # tokens in a list. Rows that hinge on the union of a star-led field or on the wrapping step are restated under cronsim; the rest of the differences are flagged.
# note: left out (counted): in CronExpressionTest.php, 5 time-zone and daylight-saving tests (testIsDueHandlesDifferentDefaultTimezones, testIsDueHandlesDifferentSuppliedTimezones, testIsDueHandlesDifferentTimezonesAsArgument, testRecognisesTimezonesAsPartOfDateTime, testBerlinShouldAdvanceProperlyOverDST) and testIssue131's Berlin next run, whose start and answer carry different offsets; 3 tests that read the wall clock (testIsDueHandlesDifferentDates, testCanGetPreviousRunDates, testSkipsCurrentDateByDefault); 3 accessor tests (testParsesCronSchedule, testKeepOriginalTime, testGetParts); and 6 alias-registry tests. In the field tests, 21 tests of the increment helper (9 of them in named zones), 4 testChecksIfSatisfied tests that read the wall clock, and AbstractFieldTest's 6 string-level helper predicates. FieldFactoryTest.php (SHA-256 a96800b0887d676dce1b6563a0951b7b42967dcf37d20e14545b4dda02b903b1) holds only factory API tests. testGetRunDateHandlesDifferentDates passes one start in three PHP types; it is one row.
# flags: wrap-range = a range from 7 (or to 0), which the library renumbers as Sunday and Bodu rejects as reversed
# flags: token-list = # tokens inside a list, which the library accepts and Bodu rejects
# flags: question-mark = ? in both day fields, which the library rejects and Bodu reads as * in each
# flags: single-value-step = a step on a single value (1/10, 0/5), which the library rejects and Bodu runs from the value to the field maximum
"""

COLUMNS = 'name,kind,format,expression,from,expected,expectation,flags,reference'

SCHEDULE_ROW = re.compile(
    r"^\s*\[\s*'(?P<expr>[^']*)'\s*,\s*(?:strtotime\('(?P<f1>[^']*)'\)|'(?P<f2>[^']*)')\s*,"
    r"\s*(?:strtotime\('(?P<n1>[^']*)'\)|'(?P<n2>[^']*)')\s*,\s*(?P<due>true|false)\s*\],\s*$")

# Schedule rows whose upstream answer depends on a dialect difference, keyed by row name. Each either carries a flag
# (and keeps the upstream answer) or is restated under Bodu's rules with an answer computed by cronsim 2.7.
OVERRIDES = {
    # 7-4 is Sunday to Thursday to the library (it renumbers the 7); Bodu rejects the reversed range.
    'scheduleProvider-14': {'expr': '0 0 * * 7-4', 'flags': 'wrap-range'},
    # 7-3 likewise.
    'scheduleProvider-16': {'expr': '0 0 * * 7-3', 'flags': 'wrap-range'},
    # The library takes the union because */15 is not literally *; Bodu, like Vixie, treats the star-led day-of-month
    # as unrestricted and intersects, so the first Tuesday-to-Friday among the 1st, 16th and 31st of January is the 31st.
    'scheduleProvider-45': {'expr': '3-59/15 6-12 */15 1 2-5', 'expected': '2017-01-31T06:03:00', 'expectation': 'cronsim',
                            'note': 'the library takes the union of the star-led */15 and 2-5 (2017-01-10T06:03); '
                                    'restated under Bodu\'s intersection'},
    # The library wraps the step of 65 around the minutes (0, then 5); Bodu selects the range start, 0, alone.
    'scheduleProvider-58': {'expr': '0-59/65 10 * * *', 'expected': '2021-08-26T10:00:00', 'expectation': 'cronsim',
                            'note': 'the library wraps the step past 59 and answers 10:05; restated under Bodu\'s '
                                    'reading of a step wider than its range as the range start alone'},
}

# The other assertions, transcribed by hand: (name, kind, format, expression, from, expected, expectation, flags,
# file, lines, probe, note). The probe is text the first cited line must contain.
CE = 'CronExpressionTest.php'
DM = 'DayOfMonthFieldTest.php'
DW = 'DayOfWeekFieldTest.php'
HF = 'HoursFieldTest.php'
MF = 'MinutesFieldTest.php'
MO = 'MonthFieldTest.php'
AF = 'AbstractFieldTest.php'
VALID_FIELD = "the field's validate() restated as the expression's validity, other fields *"
DUE = 'isDue true, restated as an inclusive next equal to the instant'
NOT_DUE = 'isDue false, restated as the next occurrence at or after the instant'
NTH = 'the nth argument skips matches; the sequence lists the skipped ones, which upstream does not assert'
WRAP = 'the library wraps the step around the field (April); restated under Bodu\'s reading of a step wider than its range as the range start alone'
S5 = 'standard'

HAND = [
    ('testConstructorRecognizesTemplates-01', 'equal', S5, '@annually', '', '0 0 1 1 *', 'upstream', '', CE, 'L45', '@annually', 'getExpression() of the macro'),
    ('testConstructorRecognizesTemplates-02', 'equal', S5, '@yearly', '', '0 0 1 1 *', 'upstream', '', CE, 'L46', '@yearly', 'getExpression() of the macro'),
    ('testConstructorRecognizesTemplates-03', 'equal', S5, '@weekly', '', '0 0 * * 0', 'upstream', '', CE, 'L47', '@weekly', 'getExpression() of the macro'),
    ('testConstructorRecognizesTemplates-04', 'equal', S5, '@daily', '', '0 0 * * *', 'upstream', '', CE, 'L48', '@daily', 'getExpression() of the macro'),
    ('testConstructorRecognizesTemplates-05', 'equal', S5, '@midnight', '', '0 0 * * *', 'upstream', '', CE, 'L49', '@midnight', 'getExpression() of the macro'),
    ('testParsesCronScheduleThrowsAnException-01', 'invalid', S5, 'A 1 2 3 4', '', '', 'upstream', '', CE, 'L70', 'A 1 2 3 4', ''),
    ('scheduleWithDifferentSeparatorsProvider-01', 'equal', S5, '*\t*\t*\t*\t*\t', '', '* * * * *', 'upstream', '', CE, 'L92', '["*\\t*', 'tabs as separators; every field reads *'),
    ('scheduleWithDifferentSeparatorsProvider-02', 'equal', S5, '*  *  *  *  *  ', '', '* * * * *', 'upstream', '', CE, 'L93', "['*  *", 'repeated and trailing spaces; every field reads *'),
    ('scheduleWithDifferentSeparatorsProvider-03', 'equal', S5, '* \t * \t * \t * \t * \t', '', '* * * * *', 'upstream', '', CE, 'L94', '["* \\t *', 'mixed spaces and tabs; every field reads *'),
    ('scheduleWithDifferentSeparatorsProvider-04', 'equal', S5, '*\t \t*\t \t*\t \t*\t \t*\t \t', '', '* * * * *', 'upstream', '', CE, 'L95', '["*\\t \\t*', 'mixed tabs and spaces; every field reads *'),
    ('testInvalidCronsWillFail-01', 'invalid', S5, '* * * 1', '', '', 'upstream', '', CE, 'L103', "'* * * 1'", 'four fields'),
    ('testInvalidPartsWillFail-01', 'invalid', S5, '* abc * * *', '', '', 'upstream', '', CE, 'L110-111', "'* * * * *'", "setPart(1, 'abc') on * * * * *, restated as the whole expression"),
    ('testProvidesMultipleRunDates-01', 'next-inclusive', S5, '*/2 * * * *', '2008-11-09T00:00:00', '2008-11-09T00:00:00', 'upstream', '', CE, 'L361-367', '*/2 * * * *', 'the first of getMultipleRunDates(4, start, false, true), which may be the start'),
    ('testProvidesMultipleRunDates-02', 'next-sequence', S5, '*/2 * * * *', '2008-11-09T00:00:00', '2008-11-09T00:02:00|2008-11-09T00:04:00|2008-11-09T00:06:00', 'upstream', '', CE, 'L361-367', '*/2 * * * *', 'the other three of the four run dates'),
    ('testProvidesMultipleRunDatesForTheFarFuture-01', 'next-sequence', S5, '0 0 12 1 *', '2015-04-28T00:00:00', '2016-01-12T00:00:00|2017-01-12T00:00:00|2018-01-12T00:00:00|2019-01-12T00:00:00|2020-01-12T00:00:00|2021-01-12T00:00:00|2022-01-12T00:00:00|2023-01-12T00:00:00|2024-01-12T00:00:00', 'upstream', '', CE, 'L373-385', '0 0 12 1 *', 'getMultipleRunDates(9, start, false, true); the start is not a run date'),
    ('testCanIterateOverNextRuns-01', 'next', S5, '@weekly', '2008-11-09T08:00:00', '2008-11-16T00:00:00', 'upstream', '', CE, 'L390-392', '@weekly', ''),
    ('testCanIterateOverNextRuns-02', 'next-sequence', S5, '@weekly', '2008-11-09T00:00:00', '2008-11-16T00:00:00|2008-11-23T00:00:00', 'derived', '', CE, 'L395-396', 'getNextRunDate($cron->getNextRunDate', 'nested getNextRunDate(start, 1, true): upstream asserts the outer 2008-11-23; the inner run date is the first instant here'),
    ('testCanIterateOverNextRuns-03', 'next-sequence', S5, '@weekly', '2008-11-09T00:00:00', '2008-11-16T00:00:00|2008-11-23T00:00:00', 'derived', '', CE, 'L399-400', ', 2, true)', f'getNextRunDate(start, 2, true): {NTH}'),
    ('testCanIterateOverNextRuns-04', 'next-sequence', S5, '@weekly', '2008-11-09T00:00:00', '2008-11-16T00:00:00|2008-11-23T00:00:00|2008-11-30T00:00:00', 'derived', '', CE, 'L401-402', ', 3, true)', f'getNextRunDate(start, 3, true): {NTH}'),
    ('testGetRunDateHandlesDifferentDates-01', 'next', S5, '@weekly', '2019-03-03T08:00:00', '2019-03-10T00:00:00', 'upstream', '', CE, 'L407-411', '@weekly', 'the same start as a string, a DateTime and a DateTimeImmutable'),
    ('testGetRunDateHandlesSimultaneousDayOfMonthAndDayOfWeek-01', 'next', S5, '0 0 13 * 3', '2021-07-15T00:00:00', '2021-07-21T00:00:00', 'upstream', '', CE, 'L423-425', '0 0 13 * 3', 'union of the two restricted day fields, as in Bodu'),
    ('testGetRunDateHandlesSimultaneousDayOfMonthAndDayOfWeek-02', 'previous', S5, '0 0 13 * 3', '2021-07-15T00:00:00', '2021-07-14T00:00:00', 'upstream', '', CE, 'L426', 'getPreviousRunDate', 'issue #121'),
    ('testStripsForSeconds-01', 'next', S5, '* * * * *', '2011-09-27T10:10:54', '2011-09-27T10:11:00', 'upstream', '', CE, 'L441-443', '* * * * *', 'ticket 7'),
    ('testFixesPhpBugInDateIntervalMonth-01', 'previous', S5, '0 0 27 JAN *', '2011-08-22T00:00:00', '2011-01-27T00:00:00', 'upstream', '', CE, 'L448-449', '0 0 27 JAN *', ''),
    ('testIssue29-01', 'previous', S5, '@weekly', '2013-03-17T00:00:00', '2013-03-10T00:00:00', 'upstream', '', CE, 'L454-457', '@weekly', ''),
    ('testIssue20-01', 'next-inclusive', S5, '* * * * MON#1', '2014-04-07T00:00:00', '2014-04-07T00:00:00', 'upstream', '', CE, 'L466-467', 'MON#1', DUE),
    ('testIssue20-02', 'next-inclusive', S5, '* * * * MON#1', '2014-04-14T00:00:00', '2014-05-05T00:00:00', 'cronsim', '', CE, 'L468', '2014-04-14', NOT_DUE),
    ('testIssue20-03', 'next-inclusive', S5, '* * * * MON#1', '2014-04-21T00:00:00', '2014-05-05T00:00:00', 'cronsim', '', CE, 'L469', '2014-04-21', NOT_DUE),
    ('testIssue20-04', 'next-inclusive', S5, '* * * * SAT#2', '2014-04-05T00:00:00', '2014-04-12T00:00:00', 'cronsim', '', CE, 'L471-472', 'SAT#2', NOT_DUE),
    ('testIssue20-05', 'next-inclusive', S5, '* * * * SAT#2', '2014-04-12T00:00:00', '2014-04-12T00:00:00', 'upstream', '', CE, 'L473', '2014-04-12', DUE),
    ('testIssue20-06', 'next-inclusive', S5, '* * * * SAT#2', '2014-04-19T00:00:00', '2014-05-10T00:00:00', 'cronsim', '', CE, 'L474', '2014-04-19', NOT_DUE),
    ('testIssue20-07', 'next-inclusive', S5, '* * * * SUN#3', '2014-04-13T00:00:00', '2014-04-20T00:00:00', 'cronsim', '', CE, 'L476-477', 'SUN#3', NOT_DUE),
    ('testIssue20-08', 'next-inclusive', S5, '* * * * SUN#3', '2014-04-20T00:00:00', '2014-04-20T00:00:00', 'upstream', '', CE, 'L478', '2014-04-20', DUE),
    ('testIssue20-09', 'next-inclusive', S5, '* * * * SUN#3', '2014-04-27T00:00:00', '2014-05-18T00:00:00', 'cronsim', '', CE, 'L479', '2014-04-27', NOT_DUE),
    ('testValidationWorks-01', 'invalid', S5, '* * * 1', '', '', 'upstream', '', CE, 'L494', "'* * * 1'", 'four fields'),
    ('testValidationWorks-02', 'valid', S5, '* * * * 1', '', '', 'upstream', '', CE, 'L496', "'* * * * 1'", ''),
    ('testValidationWorks-03', 'invalid', S5, '* * * 13 * ', '', '', 'upstream', '', CE, 'L499', "'* * * 13 * '", 'issue #156'),
    ('testValidationWorks-04', 'invalid', S5, '90 * * * *', '', '', 'upstream', '', CE, 'L502', "'90 * * * *'", 'issue #155'),
    ('testValidationWorks-05', 'invalid', S5, '0 24 1 12 0', '', '', 'upstream', '', CE, 'L505', "'0 24 1 12 0'", 'issue #154'),
    ('testValidationWorks-06', 'invalid', S5, '990 14 * * mon-fri0345345', '', '', 'upstream', '', CE, 'L508', 'mon-fri0345345', 'issue #125'),
    ('testValidationWorks-07', 'invalid', S5, '0 8 ? * ?', '', '', 'upstream', 'question-mark', CE, 'L511', "'0 8 ? * ?'", 'issue #137'),
    ('testValidationWorks-08', 'invalid', S5, '? * * * *', '', '', 'upstream', '', CE, 'L513', "'? * * * *'", ''),
    ('testValidationWorks-09', 'invalid', S5, '* ? * * *', '', '', 'upstream', '', CE, 'L514', "'* ? * * *'", ''),
    ('testValidationWorks-10', 'invalid', S5, '* * * ? *', '', '', 'upstream', '', CE, 'L515', "'* * * ? *'", ''),
    ('testValidationWorks-11', 'valid', S5, '2,17,35,47 5-7,11-13 * * *', '', '', 'upstream', '', CE, 'L518', '2,17,35,47 5-7,11-13', 'issue #5'),
    ('testDoubleZeroIsValid-01', 'valid', S5, '00 * * * *', '', '', 'upstream', '', CE, 'L529', "'00 * * * *'", 'issue #12'),
    ('testDoubleZeroIsValid-02', 'valid', S5, '01 * * * *', '', '', 'upstream', '', CE, 'L530', "'01 * * * *'", 'issue #12'),
    ('testDoubleZeroIsValid-03', 'valid', S5, '* 00 * * *', '', '', 'upstream', '', CE, 'L531', "'* 00 * * *'", 'issue #12'),
    ('testDoubleZeroIsValid-04', 'valid', S5, '* 01 * * *', '', '', 'upstream', '', CE, 'L532', "'* 01 * * *'", 'issue #12'),
    ('testDoubleZeroIsValid-05', 'next-inclusive', S5, '00 * * * *', '2014-04-07T00:00:00', '2014-04-07T00:00:00', 'upstream', '', CE, 'L534-535', "'00 * * * *'", DUE),
    ('testDoubleZeroIsValid-06', 'next-inclusive', S5, '01 * * * *', '2014-04-07T00:01:00', '2014-04-07T00:01:00', 'upstream', '', CE, 'L536-537', "'01 * * * *'", DUE),
    ('testDoubleZeroIsValid-07', 'next-inclusive', S5, '* 00 * * *', '2014-04-07T00:00:00', '2014-04-07T00:00:00', 'upstream', '', CE, 'L539-540', "'* 00 * * *'", DUE),
    ('testDoubleZeroIsValid-08', 'next-inclusive', S5, '* 01 * * *', '2014-04-07T01:00:00', '2014-04-07T01:00:00', 'upstream', '', CE, 'L541-542', "'* 01 * * *'", DUE),
    ('testRangesWrapAroundWithLargeSteps-01', 'valid', S5, '* * * */123 *', '', '', 'upstream', '', CE, 'L555', "validate('*/123')", f'issue #6; month {VALID_FIELD}'),
    ('testRangesWrapAroundWithLargeSteps-02', 'equal', S5, '* * * */123 *', '', '* * * 1 *', 'derived', '', CE, 'L556', 'getRangeForExpression', f'issue #6; upstream expands */123 to [4]; {WRAP}'),
    ('testRangesWrapAroundWithLargeSteps-03', 'next-inclusive', S5, '* * * */123 *', '2014-04-07T00:00:00', '2015-01-01T00:00:00', 'cronsim', '', CE, 'L558-559', "'* * * */123 *'", f'issue #6; upstream: isDue true; {WRAP}'),
    ('testRangesWrapAroundWithLargeSteps-04', 'next', S5, '* * * */123 *', '2014-04-07T00:00:00', '2015-01-01T00:00:00', 'cronsim', '', CE, 'L561-562', '2014-04-07 00:00:00', f'issue #6; upstream: 2014-04-07T00:01; {WRAP}'),
    ('testRangesWrapAroundWithLargeSteps-05', 'next', S5, '* * * */123 *', '2014-05-07T00:00:00', '2015-01-01T00:00:00', 'cronsim', '', CE, 'L564-565', '2014-05-07 00:00:00', f'issue #6; upstream: 2015-04-01; {WRAP}'),
    ('testFieldPositionIsHumanAdjusted-01', 'invalid', S5, '0 * * * * ? *', '', '', 'upstream', '', CE, 'L578', "'0 * * * * ? *'", 'issue #29; seven fields'),
    ('testMakeDayOfWeekAnOrSometimes-01', 'next-sequence', S5, '30 0 1 * 1', '2019-10-10T23:20:00', '2019-10-14T00:30:00|2019-10-21T00:30:00|2019-10-28T00:30:00|2019-11-01T00:30:00|2019-11-04T00:30:00', 'upstream', '', CE, 'L586-593', '30 0 1 * 1', 'issue #35; getMultipleRunDates(5, start, false, true); the start is not a run date'),
    ('testNextRunDateShouldNotAddMinutes-01', 'next', S5, '* 19 * * *', '2021-05-31T18:15:00+01:00', '2021-05-31T19:00:00+01:00', 'derived', '', CE, 'L603-608', '* 19 * * *', 'mtdowling issue #152; upstream gives the start in Europe/London (+01:00 that day) and asserts only that the minute is 00; 19:00 is the next selected hour'),
    ('testIssue131-01', 'previous', S5, '* * * * 2', '2020-10-23T15:31:45+02:00', '2020-10-20T23:59:00+02:00', 'upstream', '', CE, 'L641-648', "'* * * * 2'", 'issue #131; the previous run (L646-648); Europe/Berlin is +02:00 at both instants (the next run, across the 2020-10-25 change, is left out)'),
    ('testIssue131-02', 'next', S5, '15 1 1 9,11 *', '2022-08-20T03:44:02', '2022-09-01T01:15:00', 'upstream', '', CE, 'L650-653', '15 1 1 9,11 *', 'issue #131'),
    ('testIssue131-03', 'previous', S5, '15 1 1 9,11 *', '2022-08-20T03:44:02', '2021-11-01T01:15:00', 'upstream', '', CE, 'L655-657', '2021-11-01 01:15:00', 'issue #131'),
    ('testIssue128-01', 'next', S5, '0 20 L 6,12 ?', '2022-08-20T03:44:02', '2022-12-31T20:00:00', 'upstream', '', CE, 'L662-665', '0 20 L 6,12 ?', 'issue #128'),
    ('testIssue128-02', 'next-sequence', S5, '0 20 L 6,12 ?', '2022-08-20T03:44:02', '2022-12-31T20:00:00|2023-06-30T20:00:00|2023-12-31T20:00:00', 'derived', '', CE, 'L667-669', '2023-12-31 20:00:00', f'issue #128; getNextRunDate(start, 2): {NTH}'),
    ('testIssue128-03', 'previous', S5, '0 20 L 6,12 ?', '2022-08-20T03:44:02', '2022-06-30T20:00:00', 'upstream', '', CE, 'L671-673', '2022-06-30 20:00:00', 'issue #128'),
    ('testIssue128-04', 'previous-sequence', S5, '0 20 L 6,12 ?', '2022-08-20T03:44:02', '2022-06-30T20:00:00|2021-12-31T20:00:00', 'upstream', '', CE, 'L671-677', '2022-06-30 20:00:00', 'issue #128; getPreviousRunDate(start, 1) after getPreviousRunDate(start): both instants are asserted'),
    ('testIssue128-05', 'next-sequence', S5, '0 20 L 6,12 0-6', '2022-08-20T03:44:02', '2022-12-01T20:00:00|2022-12-02T20:00:00|2022-12-03T20:00:00', 'upstream', '', CE, 'L679-692', '0 20 L 6,12 0-6', 'issue #128; getNextRunDate(start, 0), (start, 1) and (start, 2): all three instants are asserted; the union of L and 0-6, as in Bodu'),
    ('testIssue128-06', 'next', S5, '0 20 * 6,12 *', '2022-08-20T03:44:02', '2022-12-01T20:00:00', 'upstream', '', CE, 'L694-697', '0 20 * 6,12 *', 'issue #128'),
    ('testIssue128-07', 'next-sequence', S5, '0 20 * 6,12 *', '2022-08-20T03:44:02', '2022-12-01T20:00:00|2022-12-02T20:00:00|2022-12-03T20:00:00|2022-12-04T20:00:00|2022-12-05T20:00:00|2022-12-06T20:00:00', 'derived', '', CE, 'L699-702', '0 20 * 6,12 *', f'issue #128; getNextRunDate(start, 5): {NTH}'),
    ('testIssue134ForeachInvalidArgumentOnHours-01', 'previous', S5, '0 0 1 1 *', '2021-09-07T09:36:00+00:00', '2021-01-01T00:00:00+00:00', 'upstream', '', CE, 'L763-765', '0 0 1 1 *', 'issue #134; the start is given in UTC'),
    ('testIssue151ExpressionSupportLW-01', 'next-inclusive', S5, '0 10 LW * *', '2023-08-31T10:00:00', '2023-08-31T10:00:00', 'upstream', '', CE, 'L770-771', '0 10 LW * *', f'issue #151; {DUE}'),
    ('testIssue151ExpressionSupportLW-02', 'next-inclusive', S5, '0 10 LW * *', '2023-08-30T10:00:00', '2023-08-31T10:00:00', 'derived', '', CE, 'L772', '2023-08-30 10:00:00', f'issue #151; {NOT_DUE}; 31 August 2023 is a Thursday'),
    # DayOfMonthFieldTest.php
    ('dom-testValidatesField-01', 'valid', S5, '* * 1 * *', '', '', 'upstream', '', DM, 'L24', "validate('1')", f'day-of-month {VALID_FIELD}'),
    ('dom-testValidatesField-02', 'valid', S5, '* * * * *', '', '', 'upstream', '', DM, 'L25', "validate('*')", f'day-of-month {VALID_FIELD}'),
    ('dom-testValidatesField-03', 'valid', S5, '* * L * *', '', '', 'upstream', '', DM, 'L26', "validate('L')", f'day-of-month {VALID_FIELD}'),
    ('dom-testValidatesField-04', 'valid', S5, '* * 5W * *', '', '', 'upstream', '', DM, 'L27', "validate('5W')", f'day-of-month {VALID_FIELD}'),
    ('dom-testValidatesField-05', 'valid', S5, '* * ? * *', '', '', 'upstream', '', DM, 'L28', "validate('?')", f'day-of-month {VALID_FIELD}'),
    ('dom-testValidatesField-06', 'valid', S5, '* * 01 * *', '', '', 'upstream', '', DM, 'L29', "validate('01')", f'day-of-month {VALID_FIELD}'),
    ('dom-testValidatesField-07', 'invalid', S5, '* * 5W,L * *', '', '', 'upstream', '', DM, 'L30', "validate('5W,L')", f'day-of-month {VALID_FIELD}'),
    ('dom-testValidatesField-08', 'invalid', S5, '* * 1. * *', '', '', 'upstream', '', DM, 'L31', "validate('1.')", f'day-of-month {VALID_FIELD}'),
    ('dom-testDoesNotAccept0Date-01', 'invalid', S5, '* * 0 * *', '', '', 'upstream', '', DM, 'L70', "validate('0')", f'issue #120; day-of-month {VALID_FIELD}'),
    ('dom-testIssue151DOMFieldSupportLW-01', 'valid', S5, '* * LW * *', '', '', 'upstream', '', DM, 'L108', "validate('LW')", f'issue #151; day-of-month {VALID_FIELD}'),
    # DayOfWeekFieldTest.php
    ('dow-testValidatesField-01', 'valid', S5, '* * * * 1', '', '', 'upstream', '', DW, 'L25', "validate('1')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidatesField-02', 'valid', S5, '* * * * 01', '', '', 'upstream', '', DW, 'L26', "validate('01')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidatesField-03', 'valid', S5, '* * * * 00', '', '', 'upstream', '', DW, 'L27', "validate('00')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidatesField-04', 'valid', S5, '* * * * *', '', '', 'upstream', '', DW, 'L28', "validate('*')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidatesField-05', 'valid', S5, '* * * * ?', '', '', 'upstream', '', DW, 'L29', "validate('?')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidatesField-06', 'invalid', S5, '* * * * */3,1,1-12', '', '', 'upstream', '', DW, 'L30', "validate('*/3,1,1-12')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidatesField-07', 'valid', S5, '* * * * SUN-2', '', '', 'upstream', '', DW, 'L31', "validate('SUN-2')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidatesField-08', 'invalid', S5, '* * * * 1.', '', '', 'upstream', '', DW, 'L32', "validate('1.')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidatesHashValueWeekday-01', 'invalid', S5, '* * * * 12#1', '', '', 'upstream', '', DW, 'L67', "'12#1'", 'isSatisfiedBy throws for weekday 12; validate() rejects it as well, so the expression is invalid'),
    ('dow-testValidatesHashValueNth-01', 'invalid', S5, '* * * * 3#6', '', '', 'upstream', '', DW, 'L75', "'3#6'", 'isSatisfiedBy throws for a sixth weekday; validate() rejects it as well, so the expression is invalid'),
    ('dow-testValidateWeekendHash-01', 'valid', S5, '* * * * MON#1', '', '', 'upstream', '', DW, 'L81', "validate('MON#1')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidateWeekendHash-02', 'valid', S5, '* * * * TUE#2', '', '', 'upstream', '', DW, 'L82', "validate('TUE#2')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidateWeekendHash-03', 'valid', S5, '* * * * WED#3', '', '', 'upstream', '', DW, 'L83', "validate('WED#3')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidateWeekendHash-04', 'valid', S5, '* * * * THU#4', '', '', 'upstream', '', DW, 'L84', "validate('THU#4')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidateWeekendHash-05', 'valid', S5, '* * * * FRI#5', '', '', 'upstream', '', DW, 'L85', "validate('FRI#5')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidateWeekendHash-06', 'valid', S5, '* * * * SAT#1', '', '', 'upstream', '', DW, 'L86', "validate('SAT#1')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidateWeekendHash-07', 'valid', S5, '* * * * SUN#3', '', '', 'upstream', '', DW, 'L87', "validate('SUN#3')", f'day-of-week {VALID_FIELD}'),
    ('dow-testValidateWeekendHash-08', 'valid', S5, '* * * * MON#1,MON#3', '', '', 'upstream', 'token-list', DW, 'L88', "validate('MON#1,MON#3')", f'day-of-week {VALID_FIELD}'),
    ('dow-testHandlesZeroAndSevenDayOfTheWeekValues-01', 'next-inclusive', S5, '* * * * 0-2', '2011-09-04T00:00:00', '2011-09-04T00:00:00', 'upstream', '', DW, 'L94', "'0-2'", 'the field\'s isSatisfiedBy true, restated as an inclusive next equal to the instant'),
    ('dow-testHandlesZeroAndSevenDayOfTheWeekValues-02', 'next-inclusive', S5, '* * * * 6-0', '2011-09-04T00:00:00', '2011-09-04T00:00:00', 'upstream', 'wrap-range', DW, 'L95', "'6-0'", 'the field\'s isSatisfiedBy true, restated as an inclusive next equal to the instant'),
    ('dow-testHandlesZeroAndSevenDayOfTheWeekValues-03', 'next-inclusive', S5, '* * * * SUN', '2014-04-20T00:00:00', '2014-04-20T00:00:00', 'upstream', '', DW, 'L97', "'SUN'", 'the field\'s isSatisfiedBy true, restated as an inclusive next equal to the instant'),
    ('dow-testHandlesZeroAndSevenDayOfTheWeekValues-04', 'next-inclusive', S5, '* * * * SUN#3', '2014-04-20T00:00:00', '2014-04-20T00:00:00', 'upstream', '', DW, 'L98', "'SUN#3'", 'the field\'s isSatisfiedBy true, restated as an inclusive next equal to the instant'),
    ('dow-testHandlesZeroAndSevenDayOfTheWeekValues-05', 'next-inclusive', S5, '* * * * 0#3', '2014-04-20T00:00:00', '2014-04-20T00:00:00', 'upstream', '', DW, 'L99', "'0#3'", 'the field\'s isSatisfiedBy true, restated as an inclusive next equal to the instant'),
    ('dow-testHandlesZeroAndSevenDayOfTheWeekValues-06', 'next-inclusive', S5, '* * * * 7#3', '2014-04-20T00:00:00', '2014-04-20T00:00:00', 'upstream', '', DW, 'L100', "'7#3'", 'the field\'s isSatisfiedBy true, restated as an inclusive next equal to the instant'),
    ('dow-testHandlesLastWeekdayOfTheMonth-01', 'next-inclusive', S5, '* * * * FRIL', '2018-12-28T00:00:00', '2018-12-28T00:00:00', 'upstream', '', DW, 'L106', "'FRIL'", 'the field\'s isSatisfiedBy true, restated as an inclusive next equal to the instant'),
    ('dow-testHandlesLastWeekdayOfTheMonth-02', 'next-inclusive', S5, '* * * * 5L', '2018-12-28T00:00:00', '2018-12-28T00:00:00', 'upstream', '', DW, 'L107', "'5L'", 'the field\'s isSatisfiedBy true, restated as an inclusive next equal to the instant'),
    ('dow-testHandlesLastWeekdayOfTheMonth-03', 'next-inclusive', S5, '* * * * FRIL', '2018-12-21T00:00:00', '2018-12-28T00:00:00', 'derived', '', DW, 'L108', "'FRIL'", 'the field\'s isSatisfiedBy false, restated as the next occurrence at or after the instant: the last Friday of December 2018 is the 28th'),
    ('dow-testHandlesLastWeekdayOfTheMonth-04', 'next-inclusive', S5, '* * * * 5L', '2018-12-21T00:00:00', '2018-12-28T00:00:00', 'cronsim', '', DW, 'L109', "'5L'", 'the field\'s isSatisfiedBy false, restated as the next occurrence at or after the instant'),
    ('dow-testIssue47-01', 'invalid', S5, '* * * * mon,', '', '', 'upstream', '', DW, 'L118', "validate('mon,')", f'mtdowling issue #47; day-of-week {VALID_FIELD}'),
    ('dow-testIssue47-02', 'invalid', S5, '* * * * mon-', '', '', 'upstream', '', DW, 'L119', "validate('mon-')", f'mtdowling issue #47; day-of-week {VALID_FIELD}'),
    ('dow-testIssue47-03', 'invalid', S5, '* * * * */2,', '', '', 'upstream', '', DW, 'L120', "validate('*/2,')", f'mtdowling issue #47; day-of-week {VALID_FIELD}'),
    ('dow-testIssue47-04', 'invalid', S5, '* * * * -mon', '', '', 'upstream', '', DW, 'L121', "validate('-mon')", f'mtdowling issue #47; day-of-week {VALID_FIELD}'),
    ('dow-testIssue47-05', 'invalid', S5, '* * * * ,1', '', '', 'upstream', '', DW, 'L122', "validate(',1')", f'mtdowling issue #47; day-of-week {VALID_FIELD}'),
    ('dow-testIssue47-06', 'invalid', S5, '* * * * *-', '', '', 'upstream', '', DW, 'L123', "validate('*-')", f'mtdowling issue #47; day-of-week {VALID_FIELD}'),
    ('dow-testIssue47-07', 'invalid', S5, '* * * * ,-', '', '', 'upstream', '', DW, 'L124', "validate(',-')", f'mtdowling issue #47; day-of-week {VALID_FIELD}'),
    ('dow-testLiteralsExpandProperly-01', 'valid', S5, '* * * * MON-FRI', '', '', 'upstream', '', DW, 'L133', "validate('MON-FRI')", f'day-of-week {VALID_FIELD}'),
    ('dow-testLiteralsExpandProperly-02', 'equal', S5, '* * * * MON-FRI', '', '* * * * 1,2,3,4,5', 'upstream', '', DW, 'L134', 'getRangeForExpression', 'the field\'s expansion [1, 2, 3, 4, 5] restated as an equal expression'),
    ('dow-testLiteralsIgnoreCasingProperly-01', 'valid', S5, '* * * * MON', '', '', 'upstream', '', DW, 'L147', "validate('MON')", f'issue #24; day-of-week {VALID_FIELD}'),
    ('dow-testLiteralsIgnoreCasingProperly-02', 'valid', S5, '* * * * Mon', '', '', 'upstream', '', DW, 'L148', "validate('Mon')", f'issue #24; day-of-week {VALID_FIELD}'),
    ('dow-testLiteralsIgnoreCasingProperly-03', 'valid', S5, '* * * * mon', '', '', 'upstream', '', DW, 'L149', "validate('mon')", f'issue #24; day-of-week {VALID_FIELD}'),
    ('dow-testLiteralsIgnoreCasingProperly-04', 'valid', S5, '* * * * Mon,Wed,Fri', '', '', 'upstream', '', DW, 'L150', "validate('Mon,Wed,Fri')", f'issue #24; day-of-week {VALID_FIELD}'),
    # HoursFieldTest.php
    ('hours-testValidatesField-01', 'valid', S5, '* 1 * * *', '', '', 'upstream', '', HF, 'L24', "validate('1')", f'hour {VALID_FIELD}'),
    ('hours-testValidatesField-02', 'valid', S5, '* 00 * * *', '', '', 'upstream', '', HF, 'L25', "validate('00')", f'hour {VALID_FIELD}'),
    ('hours-testValidatesField-03', 'valid', S5, '* 01 * * *', '', '', 'upstream', '', HF, 'L26', "validate('01')", f'hour {VALID_FIELD}'),
    ('hours-testValidatesField-04', 'valid', S5, '* * * * *', '', '', 'upstream', '', HF, 'L27', "validate('*')", f'hour {VALID_FIELD}'),
    ('hours-testValidatesField-05', 'valid', S5, '* */3,1,1-12 * * *', '', '', 'upstream', '', HF, 'L28', "validate('*/3,1,1-12')", f'hour {VALID_FIELD}'),
    ('hours-testValidatesField-06', 'invalid', S5, '* 1/10 * * *', '', '', 'upstream', 'single-value-step', HF, 'L29', "validate('1/10')", f'hour {VALID_FIELD}'),
    # MinutesFieldTest.php
    ('minutes-testValidatesField-01', 'valid', S5, '1 * * * *', '', '', 'upstream', '', MF, 'L25', "validate('1')", f'minute {VALID_FIELD}'),
    ('minutes-testValidatesField-02', 'valid', S5, '* * * * *', '', '', 'upstream', '', MF, 'L26', "validate('*')", f'minute {VALID_FIELD}'),
    ('minutes-testValidatesField-03', 'valid', S5, '*/3,1,1-12 * * * *', '', '', 'upstream', '', MF, 'L27', "validate('*/3,1,1-12')", f'minute {VALID_FIELD}'),
    ('minutes-testValidatesField-04', 'invalid', S5, '1/10 * * * *', '', '', 'upstream', 'single-value-step', MF, 'L28', "validate('1/10')", f'minute {VALID_FIELD}'),
    ('minutes-testBadSyntaxesShouldNotValidate-01', 'invalid', S5, '*-1 * * * *', '', '', 'upstream', '', MF, 'L68', "validate('*-1')", f'minute {VALID_FIELD}'),
    ('minutes-testBadSyntaxesShouldNotValidate-02', 'invalid', S5, '1-2-3 * * * *', '', '', 'upstream', '', MF, 'L69', "validate('1-2-3')", f'minute {VALID_FIELD}'),
    ('minutes-testBadSyntaxesShouldNotValidate-03', 'invalid', S5, '-1 * * * *', '', '', 'upstream', '', MF, 'L70', "validate('-1')", f'minute {VALID_FIELD}'),
    ('minutes-testInvalidRangeShouldNotValidate-01', 'invalid', S5, '0/5 * * * *', '', '', 'upstream', 'single-value-step', MF, 'L84', "validate('0/5')", f'issue #18; minute {VALID_FIELD}'),
    # MonthFieldTest.php
    ('month-testValidatesField-01', 'valid', S5, '* * * 12 *', '', '', 'upstream', '', MO, 'L24', "validate('12')", f'month {VALID_FIELD}'),
    ('month-testValidatesField-02', 'valid', S5, '* * * * *', '', '', 'upstream', '', MO, 'L25', "validate('*')", f'month {VALID_FIELD}'),
    ('month-testValidatesField-03', 'valid', S5, '* * * */10,2,1-12 *', '', '', 'upstream', '', MO, 'L26', "validate('*/10,2,1-12')", f'month {VALID_FIELD}'),
    ('month-testValidatesField-04', 'invalid', S5, '* * * 1.fix-regexp *', '', '', 'upstream', '', MO, 'L27', "validate('1.fix-regexp')", f'month {VALID_FIELD}'),
    ('month-testValidatesField-05', 'invalid', S5, '* * * 1/10 *', '', '', 'upstream', 'single-value-step', MO, 'L28', "validate('1/10')", f'month {VALID_FIELD}'),
    ('month-testLiteralsIgnoreCasingProperly-01', 'valid', S5, '* * * JAN *', '', '', 'upstream', '', MO, 'L99', "validate('JAN')", f'issue #24; month {VALID_FIELD}'),
    ('month-testLiteralsIgnoreCasingProperly-02', 'valid', S5, '* * * Jan *', '', '', 'upstream', '', MO, 'L100', "validate('Jan')", f'issue #24; month {VALID_FIELD}'),
    ('month-testLiteralsIgnoreCasingProperly-03', 'valid', S5, '* * * jan *', '', '', 'upstream', '', MO, 'L101', "validate('jan')", f'issue #24; month {VALID_FIELD}'),
    # AbstractFieldTest.php
    ('abstract-testAllowRangesAndLists-01', 'valid', S5, '* 5-7,11-13 * * *', '', '', 'upstream', '', AF, 'L112-114', "'5-7,11-13'", f'issue #5; hour {VALID_FIELD}'),
    ('abstract-testGetRangeForExpressionExpandsCorrectly-01', 'equal', S5, '* 5-7,11-13 * * *', '', '* 5,6,7,11,12,13 * * *', 'upstream', '', AF, 'L125', "'5-7,11-13', 23", 'issue #5; the hour field\'s expansion restated as an equal expression'),
    ('abstract-testGetRangeForExpressionExpandsCorrectly-02', 'to-string', S5, '* 5,6,7,11,12,13 * * *', '', '* 5,6,7,11,12,13 * * *', 'upstream', '', AF, 'L126', "'5,6,7,11,12,13', 23", 'issue #5; the hour field\'s expansion of a plain list, as canonical text'),
    ('abstract-testGetRangeForExpressionExpandsCorrectly-03', 'equal', S5, '* */6 * * *', '', '* 0,6,12,18 * * *', 'upstream', '', AF, 'L127', "'*/6', 23", 'issue #5; the hour field\'s expansion restated as an equal expression'),
    ('abstract-testGetRangeForExpressionExpandsCorrectly-04', 'equal', S5, '* 5-13/6 * * *', '', '* 5,11 * * *', 'upstream', '', AF, 'L128', "'5-13/6', 23", 'issue #5; the hour field\'s expansion restated as an equal expression'),
    ('abstract-testGetRangeForExpressionExpandsCorrectly-05', 'equal', S5, '1-4,11-14/2,21-27/3,40-59/10 * * * *', '', '1,2,3,4,11,13,21,24,27,40,50 * * * *', 'upstream', '', AF, 'L129', '40-59/10', 'issue #5; upstream expands it with the hour field\'s helper against a maximum of 59, restated in the minute field, whose range that is'),
    ('abstract-testGetRangeForExpressionExpandsCorrectly-06', 'equal', S5, '* 1,3,5-7,11-15/1,17-22/2,23 * * *', '', '* 1,3,5,6,7,11,12,13,14,15,17,19,21,23 * * *', 'upstream', '', AF, 'L130', "17-22/2", 'issue #5; the hour field\'s expansion restated as an equal expression'),
]


FIELD_PREFIXES = ('dom-', 'dow-', 'hours-', 'minutes-', 'month-', 'abstract-')


def csv_field(value: str) -> str:
    if any(c in value for c in ',"\t\r\n') or value != value.strip():
        return '"' + value.replace('"', '""') + '"'
    return value


def iso(text: str) -> str:
    return text.replace(' ', 'T')


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        print(__doc__, file=sys.stderr)
        return 2
    texts: dict[str, list[str]] = {}
    hashes = []
    for name, expected in FILES.items():
        data = open(os.path.join(argv[1], name), 'rb').read()
        digest = hashlib.sha256(data).hexdigest()
        if digest != expected:
            print(f'{name}: SHA-256 {digest} is not the recorded {expected}', file=sys.stderr)
            return 1
        texts[name] = data.decode('utf-8').splitlines()
        hashes.append(f'# file-sha256: tests/Cron/{name} {digest}')

    rows: list[list[str]] = []

    # scheduleProvider: every [expression, start, nextRun, isDue] row, in file order.
    lines = texts[CE]
    start = next(i for i, l in enumerate(lines) if 'function scheduleProvider()' in l)
    count = 0
    for number in range(start + 1, len(lines)):
        line = lines[number]
        if line.strip() == '];':
            break
        match = SCHEDULE_ROW.match(line)
        if not match:
            if line.strip().startswith('['):
                print(f'{CE} L{number + 1}: unparsed schedule row', file=sys.stderr)
                return 1
            continue
        count += 1
        name = f'scheduleProvider-{count:02d}'
        expr = match['expr']
        given = match['f1'] or match['f2']
        nxt = iso(match['n1'] or match['n2'])
        due = match['due'] == 'true'
        truncated = iso(given[:17] + '00')
        if due != (nxt == truncated):
            print(f'{name}: isDue {due} disagrees with nextRun {nxt} from {truncated}', file=sys.stderr)
            return 1
        notes = [f'CronExpressionTest.scheduleProvider L{number + 1}', f'isDue {str(due).lower()}']
        if given[17:] != '00':
            notes.append(f'start {iso(given)} truncated to the minute, as the library drops seconds')
        override = OVERRIDES.get(name, {})
        if override and override['expr'] != expr:
            print(f'{name}: override is for {override["expr"]!r}, row holds {expr!r}', file=sys.stderr)
            return 1
        if 'note' in override:
            notes.append(override['note'])
        rows.append([name, 'next-inclusive', 'standard', expr, truncated, override.get('expected', nxt),
                     override.get('expectation', 'upstream'), override.get('flags', ''), '; '.join(notes)])
    unknown = set(OVERRIDES) - {r[0] for r in rows}
    if unknown:
        print(f'overrides for missing rows: {sorted(unknown)}', file=sys.stderr)
        return 1

    for (name, kind, fmt, expr, frm, exp, expectation, flags, file, where, probe, note) in HAND:
        first = int(where[1:].split('-')[0])
        if probe not in texts[file][first - 1]:
            print(f'{name}: {file} L{first} does not contain {probe!r}', file=sys.stderr)
            return 1
        method = name.rsplit('-', 1)[0]
        if method.startswith(FIELD_PREFIXES):
            method = method.split('-', 1)[1]
        reference = f'{file[:-len(".php")]}.{method} {where}'
        if note:
            reference += f'; {note}'
        rows.append([name, kind, fmt, expr, frm, exp, expectation, flags, reference])

    sources = ', '.join(f'tests/Cron/{name}' for name in FILES)
    out = [HEADER.format(sources=sources, commit=COMMIT, hashes='\n'.join(hashes)).rstrip('\n'), COLUMNS]
    out += [','.join(csv_field(v) for v in row) for row in rows]
    sys.stdout.buffer.write(('\n'.join(out) + '\n').encode('utf-8'))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
