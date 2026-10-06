#!/usr/bin/env python3
"""Derive a cron vector table from Quartz.NET's CronExpression tests.

Quartz.NET (https://github.com/quartznet/quartznet, Apache-2.0) keeps its cron tests in
``src/Quartz.Tests.Unit/CronExpressionTest.cs``, ``CronDialectDivergenceTest.cs`` and
``CronExpressionWrappingRangeTest.cs``. This script restates them for Bodu's ``CronExpression``:

* the ``[TestCase]`` rows are parsed from the files, each by a handler for its test method,
* the two ``[TestCaseSource]`` tables (``CronTestScenarios`` and the ``TestGetPreviousValidTimeBefore`` instants and
  gaps) are parsed from the file, and
* the ``[Test]`` assertions, one test method each, are a hand-maintained list below, each with its line.

Every test method of the three files is accounted for, and the script fails if a file holds a method or row that
these lists and handlers do not cover.

Quartz's dialect, and how a row is adapted:

* Seconds come first, as in Bodu's six-field format. A seventh field is a year. It is dropped when the row's answer
  stays inside that year (or the field is ``*``); otherwise the row keeps it and is flagged ``year-field``.
* Day-of-week digits run 1 = Sunday to 7 = Saturday. Where a row depends on them, a digit is written as the day's
  name, which both dialects read alike, or renumbered where the row compares numeric spellings; a row that asserts a
  rejection for another reason keeps its digits as written. A digit outside 1-7 has no Quartz meaning and is kept as
  written; where Bodu accepts it (0, Sunday) the row is flagged ``quartz-dow-numbering``.
* ``IsSatisfiedBy(t)`` true is a ``next-inclusive`` row from ``t`` answering ``t``. False has no kind of its own, so it
  is a ``next-inclusive`` row from ``t`` answering the next occurrence after it, which is worked out by hand.
* Tokens inside lists, ``LW-n``, ``L`` alone in day-of-week, wrapping ranges, increments wider than their field and
  ``H`` are Quartz extensions or Quartz rejections Bodu does not share; those rows are flagged.

Usage:

    python3 -I extract-quartznet-cron-vectors.py <CronExpressionTest.cs> <CronDialectDivergenceTest.cs> \\
        <CronExpressionWrappingRangeTest.cs> > quartznet-cron-vectors.csv

The output is deterministic: rerunning the script reproduces the table byte for byte.
"""

from __future__ import annotations

import csv
import datetime as dt
import hashlib
import io
import re
import sys

UPSTREAM_COMMIT = '605a8cd2d4acadab5e6568943467ffc7dff66561'
UPSTREAM = [
    ('src/Quartz.Tests.Unit/CronExpressionTest.cs',
     'c368193592b136e2c296b69c0f9cc09a779162cdf4046e0f5f8733ddf6e11c5b'),
    ('src/Quartz.Tests.Unit/CronDialectDivergenceTest.cs',
     '91716dcf65d2d2b397ec3546674254e9e451d49a8291147065f662c2c9845a5a'),
    ('src/Quartz.Tests.Unit/CronExpressionWrappingRangeTest.cs',
     '3a0501e5651dade72a8cec8fedfed919ecfa75048ecca9546b0fb80b63b885d7'),
]
MAIN, DIVERGENCE, WRAPPING = (path.split('/')[-1] for path, _ in UPSTREAM)

COLUMNS = ['name', 'kind', 'format', 'expression', 'from', 'expected', 'expectation', 'flags', 'reference']

FLAG_MEANINGS = {
    'hash': 'the Jenkins H token (and ParseWithHash), which Bodu rejects',
    'oversized-step': 'an increment wider than its field, which Quartz rejects and Bodu accepts (selecting the range start)',
    'quartz-dow-numbering': 'a Quartz (1 = Sunday) weekday digit that cannot be renumbered without changing the '
                            "row's meaning: day 0, which Quartz rejects and Bodu reads as Sunday",
    'syntax-extension': 'Quartz-only syntax: LW-n, or L alone in day-of-week (Saturday)',
    'token-list': 'Quartz day tokens inside a day-of-month list, which Bodu rejects',
    'wrap-range': 'a reversed range Quartz wraps; Bodu rejects it',
    'year-field': 'the row depends on the seventh (year) field, which Bodu does not have',
}

# Quartz numbers the week 1 = Sunday .. 7 = Saturday. Rows that keep their meaning write the day by name.
QUARTZ_DAY = {1: 'SUN', 2: 'MON', 3: 'TUE', 4: 'WED', 5: 'THU', 6: 'FRI', 7: 'SAT'}

METHOD = re.compile(r'^\s*public (?:static )?void (\w+)\(')


def fail(message: str) -> None:
    sys.stderr.write(f'error: {message}\n')
    sys.exit(1)


def join(*parts: str) -> str:
    return '; '.join(p for p in parts if p)


def row(kind: str, expression: str, frm: str = '', expected: str = '', expectation: str = 'upstream',
        flags: str = '', note: str = '', fmt: str = 'withSeconds') -> dict:
    """A row before it is named and placed: kind, format, expression, instants, provenance, flags and a note."""
    if kind in ('valid', 'invalid', 'equal', 'not-equal', 'to-string'):
        # Such a row has no instant: it asserts the parse alone, which a valid year field does not decide.
        note = note.replace(': the instant lies in it', ': a valid year, which does not decide the parse')
    return dict(kind=kind, format=fmt, expression=expression, frm=frm, expected=expected, expectation=expectation,
                flags=flags, note=note)


def holds(expression: str, at: str, **kw) -> dict:
    """IsSatisfiedBy(at) is true: the next occurrence at or after the instant is the instant itself."""
    return row('next-inclusive', expression, at, at, note=join('IsSatisfiedBy true', kw.pop('note', '')), **kw)


def misses(expression: str, at: str, following: str, why: str, **kw) -> dict:
    """IsSatisfiedBy(at) is false: the next occurrence at or after the instant is a later one, worked out by hand."""
    return row('next-inclusive', expression, at, following, kw.pop('expectation', 'derived'),
               note=join('IsSatisfiedBy false, as the next occurrence at or after the instant', why,
                         kw.pop('note', '')), **kw)


# ---------------------------------------------------------------------------------------------------------------------
# The [Test] methods, by file and line of the declaration. A value is (method, rows) or (method, reason) for a method
# left out: 'api' (the library's own API with no cron meaning), 'zone' (a named time zone's transition), 'clock' (the
# wall clock) or 'no-assertion'. Rows whose expected value is not the upstream one say how it was worked out.
# ---------------------------------------------------------------------------------------------------------------------
Y2005 = 'year 2005 dropped: the instant lies in it'
Y2010 = 'year 2010 dropped: the instant lies in it'
YSTAR = 'year * dropped'

TESTS = {
    MAIN: {
        70: ('TestIsSatisfiedBy', [
            holds('0 15 10 * * ?', '2005-06-01T10:15:00', note=Y2005),
            row('unreachable-next', '0 15 10 * * ? 2005', '2006-06-01T10:15:00', '', 'derived', 'year-field',
                'IsSatisfiedBy(2006-06-01 10:15) false because the year field is 2005: nothing fires from 2006 on'),
            misses('0 15 10 * * ?', '2005-06-01T10:16:00', '2005-06-02T10:15:00', '10:15 next day', note=Y2005),
            misses('0 15 10 * * ?', '2005-06-01T10:14:00', '2005-06-01T10:15:00', 'a minute later', note=Y2005),
            misses('0 15 10 ? * MON-FRI', '2007-06-09T10:15:00', '2007-06-11T10:15:00',
                   'Saturday; Monday 11 June is next'),
            misses('0 15 10 ? * MON-FRI', '2007-06-10T10:15:00', '2007-06-11T10:15:00',
                   'Sunday; Monday 11 June is next'),
        ]),
        98: ('TestLastDayOffset', [
            holds('0 15 10 L-2 * ?', '2010-10-29T10:15:00', note=Y2010),
            misses('0 15 10 L-2 * ?', '2010-10-28T10:15:00', '2010-10-29T10:15:00', 'L-2 in October is the 29th',
                   note=Y2010),
            holds('0 15 10 L-5W * ?', '2010-10-26T10:15:00', note=Y2010),
            holds('0 15 10 L-1 * ?', '2010-10-30T10:15:00', note=Y2010),
            holds('0 15 10 L-1W * ?', '2010-10-29T10:15:00', note=Y2010),
        ]),
        # The parsed state is rebuilt from the string after a serializer round trip (not modelled); the firing
        # behaviour it asserts is transcribed. 1, L-1, LW and 2W in one list is a token list.
        171: ('TestSerializationRoundTripWithLastDayAndWeekday', [
            holds('0 15 10 1,L-1,LW,2W * ?', '2010-10-01T10:15:00', flags='token-list', note=Y2010),
            holds('0 15 10 1,L-1,LW,2W * ?', '2010-10-29T10:15:00', flags='token-list', note=Y2010),
            holds('0 15 10 1,L-1,LW,2W * ?', '2010-10-30T10:15:00', flags='token-list', note=Y2010),
            misses('0 15 10 1,L-1,LW,2W * ?', '2010-10-03T10:15:00', '2010-10-29T10:15:00',
                   'under Quartz the list fires on the 1st, 29th and 30th', flags='token-list', note=Y2010),
        ]),
        191: ('TestZeroNearestWeekdayIsRejectedAtParse', [row('invalid', '0 15 10 0W * ?')]),
        348: ('EqualityIsNullSafe', 'api'),
        358: ('TryParseAcceptsAValidExpression', [
            row('valid', '0 15 10 * * ?', note='the CronExpressionString echo is API and not transcribed'),
        ]),
        375: ('ParseThrowsForNullAndForGarbage', [
            row('invalid', 'not a cron expression', note='the null argument part is API and not transcribed'),
        ]),
        390: ('TheConstructorAndParseAgreeOnWhatANullIs', 'api'),
        405: ('TryParseAnswersFalseForAFormatItDoesNotKnow', 'api'),
        418: ('ParseStillThrowsForAFormatItDoesNotKnow', 'api'),
        426: ('TryParseReadsTheUnixDialectAndRejectsAMalformedOne', [
            row('valid', '30 4 * * 1', fmt='standard',
                note='CronFormat.Unix (five fields, 1 is Monday as in Bodu); its Quartz spelling is API'),
            row('invalid', 'not a crontab line', fmt='standard', note='CronFormat.Unix'),
        ]),
        436: ('IsParsable', [
            row('valid', '0 15 10 * * ?', note='IParsable.Parse'),
            row('invalid', 'garbage', note='IParsable.TryParse'),
        ]),
        449: ('WithTimeZoneReturnsARetimedCopyAndLeavesTheOriginalAlone', 'api'),
        462: ('WithTimeZoneNullMeansTheLocalTimeZone', 'api'),
        472: ('WithTimeZoneReturnsTheSameInstanceWhenNothingChanges', 'api'),
        504: ('FiveFieldUnixExpressionsAreRejectedWithGuidance', [
            row('invalid', '30 4 * * 1', note='five fields where six are required; the message text is not '
                                              'transcribed'),
        ]),
        686: ('AStepAfterATextualRangeIsNotDropped', [
            row('equal', '0 0 12 ? * MON-FRI/2', expected='0 0 12 ? * MON,WED,FRI',
                note='GetSet(DayOfWeek) is [2, 4, 6] in Quartz numbering: Monday, Wednesday, Friday'),
        ]),
        709: ('TooFewFieldsNamesTheRequiredFields', [row('invalid', '0 30 4')]),
        755: ('CanGetNextTimeAfterExternal_From_JsonDeserializedExpression', [
            row('next', '0 15 23 * * ?', '2005-06-01T23:16:00', '2005-06-02T23:15:00',
                note='after a JSON round trip, which is not modelled'),
        ]),
        767: ('TestCronExpressionPassingMidnight', [
            row('next', '0 15 23 * * ?', '2005-06-01T23:16:00', '2005-06-02T23:15:00'),
        ]),
        776: ('TestCronExpressionPassingYear', [
            row('next', '0 55 15 1 * ?', '2007-12-01T23:59:59', '2008-01-01T15:55:00', note='QRTZNET-50'),
        ]),
        # TestCorrectWeekFireDays walks the fire times from 2007-06-01 11:00 and asserts which June days fire. The
        # run lists them and then the first July occurrence (derived), which shows that no other June day fires.
        787: ('TestCronExpressionWeekdaysMonFri', [
            row('next-sequence', '0 0 12 ? * MON-FRI', '2007-06-01T11:00:00',
                '|'.join(f'2007-06-{d:02d}T12:00:00' for d in (1, 4, 5, 6, 7, 8, 11, 12, 13, 14, 15, 18, 19, 20, 21,
                                                               22, 25, 26, 27, 28, 29)) + '|2007-07-02T12:00:00',
                expectation='derived',
                note='the June fire days upstream lists, then the first July occurrence, worked out, which closes '
                     'the month'),
        ]),
        798: ('TestCronExpressionWeekdaysFriday', [
            row('next-sequence', '0 0 12 ? * FRI', '2007-06-01T11:00:00',
                '2007-06-01T12:00:00|2007-06-08T12:00:00|2007-06-15T12:00:00|2007-06-22T12:00:00|2007-06-29T12:00:00'
                '|2007-07-06T12:00:00', expectation='derived',
                note='the June fire days upstream lists, then the first July occurrence, worked out; the two '
                     'GetTimeAfter calls from DateTimeOffset.Now assert nothing'),
        ]),
        812: ('TestCronExpressionLastDayOfMonth', [
            row('next-sequence', '0 0 12 L * ?', '2007-06-01T11:00:00', '2007-06-30T12:00:00|2007-07-31T12:00:00',
                expectation='derived', note='the June fire day upstream lists, then the first July occurrence, '
                                            'worked out'),
        ]),
        822: ('TestHourShift', [
            row('next', '0/5 * * * * ?', '2005-06-01T01:59:55', '2005-06-01T02:00:00', note='QRTZNET-20'),
        ]),
        832: ('TestMonthShift', [
            row('next', '* * 1 * * ?', '2005-07-31T22:59:57', '2005-08-01T01:00:00', note='QRTZNET-28'),
        ]),
        842: ('TestYearChange', [
            row('next', '0 12 4 ? * TUE', '2007-12-28T00:00:00', '2008-01-01T04:12:00', expectation='derived',
                note='QRTZNET-85; Quartz 3 (Tuesday) written TUE; the test asserts only that no exception is thrown, '
                     'the value is the first Tuesday after 28 December 2007'),
        ]),
        850: ('TestCronExpressionParsingIncorrectDayOfWeek', [
            row('invalid', ' * * * * * 2026',
                note='the sixth field is DateTime.Now.Year; a four-digit year is outside the day-of-week range in '
                     'either numbering, so the row fixes it at 2026, the year of reading'),
        ]),
        868: ('TestCronExpressionWithExtraWhiteSpace', [
            row('valid', ' 30 *   * * * ?  ',
                note='QRTZNET-22; the IsSatisfiedBy assertion at DateTime.UtcNow.Date + 2 minutes reads the wall '
                     'clock and is left out'),
        ]),
        # 200 IsSatisfiedBy checks every half hour from 2008-12-19 00:00 to 2008-12-23 03:30: only Friday 19 December
        # (the third Friday) at 10:30-13:30 fires. The run lists those four and then the next occurrence (derived).
        907: ('TestNthWeekDayPassingMonth', [
            row('next-sequence', '0 30 10-13 ? * FRI#3', '2008-12-19T00:00:00',
                '2008-12-19T10:30:00|2008-12-19T11:30:00|2008-12-19T12:30:00|2008-12-19T13:30:00|2009-01-16T10:30:00',
                expectation='derived',
                note='QRTZNET-105; the half-hour IsSatisfiedBy sweep to 2008-12-23 03:30 fires only these four, then '
                     'the third Friday of January 2009, worked out'),
        ]),
        926: ('TestNormal', [row('valid', '0 15 10 * * ?', note=join('fields 0-5 parse non-empty', 'year 2005 dropped'))]),
        935: ('TestSecond', [row('valid', '58-4 5 21 ? * MON-FRI', flags='wrap-range')]),
        941: ('TestMinute', [row('valid', '0 58-4 21 ? * MON-FRI', flags='wrap-range')]),
        947: ('TestHour', [row('valid', '0 0/5 21-3 ? * MON-FRI', flags='wrap-range')]),
        953: ('TestDayOfWeekNumber', [row('valid', '58 5 21 ? * 6-2', flags='wrap-range',
                                          note='Quartz 6-2 is Friday to Monday')]),
        959: ('TestDayOfWeek', [row('valid', '58 5 21 ? * FRI-TUE', flags='wrap-range')]),
        965: ('TestDayOfMonth', [row('valid', '58 5 21 28-5 1 ?', flags='wrap-range')]),
        971: ('TestMonth', [row('valid', '58 5 21 ? 11-2 FRI', flags='wrap-range')]),
        977: ('TestAmbiguous', [
            row('valid', '0 0 14-6 ? * FRI-MON', flags='wrap-range', note='hour and day-of-week fields'),
            row('valid', '55-3 56-2 6 ? * FRI', flags='wrap-range', note='second and minute fields'),
        ]),
        1004: ('TestQuartz640', [
            row('invalid', '0 43 9 ? * SAT,SUN,L', note='L with other days of the week'),
            row('invalid', '0 43 9 ? * 6,7,L',
                note='L with other days of the week; the Quartz digits are kept, since the L in the list is what is '
                     'rejected'),
            row('valid', '0 43 9 ? * THUL', note='Quartz 5L (last Thursday) written THUL'),
        ]),
        1043: ('TestGetTimeAfter_QRTZNET149', [
            row('next', '0 0 0 29 * ?', '2009-01-30T00:00:00', '2009-03-29T00:00:00'),
            row('next', '0 0 0 29 * ?', '2009-12-30T00:00:00', '2010-01-29T00:00:00'),
        ]),
        1062: ('TestQRTZNET152_Nearest_Weekday_Expression_W_Does_Not_Work_In_CronTrigger', [
            row('next-sequence', '0 5 13 5W 1-12 ?', '2009-03-08T00:00:00', '2009-04-06T13:05:00|2009-05-05T13:05:00',
                note='5 April 2009 is a Sunday, so Monday the 6th; the second call starts from the first answer'),
        ]),
        1074: ('ShouldThrowExceptionIfWParameterMakesNoSense', [row('invalid', '0/5 * * 32W 1 ?')]),
        1091: ('TestQtz259', 'clock'),
        1109: ('TestQtz259Lw', 'clock'),
        1126: ('TestDaylightSaving_QRTZNETZ186', 'zone'),
        1145: ('TestDaylightSavingsDoesNotMatchAnHourBefore', 'zone'),
        1159: ('TestDaylightSavingsDoesNotMatchAnHourBefore2', 'zone'),
        1173: ('TestSecRangeIntervalAfterSlash', [
            row('invalid', '/120 0 8-18 ? * 2-6', note='a step with no start; Bodu rejects the empty start'),
            row('invalid', '0/120 0 8-18 ? * 2-6', flags='oversized-step', note='Increment > 59 : 120'),
            row('invalid', '/ 0 8-18 ? * 2-6', note="'/' must be followed by an integer"),
            row('invalid', '0/ 0 8-18 ? * 2-6', note="'/' must be followed by an integer"),
        ]),
        1193: ('TestMinRangeIntervalAfterSlash', [
            row('invalid', '0 /120 8-18 ? * 2-6', note='a step with no start; Bodu rejects the empty start'),
            row('invalid', '0 0/120 8-18 ? * 2-6', flags='oversized-step', note='Increment > 59 : 120'),
            row('invalid', '0 / 8-18 ? * 2-6', note="'/' must be followed by an integer"),
            row('invalid', '0 0/ 8-18 ? * 2-6', note="'/' must be followed by an integer"),
        ]),
        1213: ('TestHourRangeIntervalAfterSlash', [
            row('invalid', '0 0 /120 ? * 2-6', note='a step with no start; Bodu rejects the empty start'),
            row('invalid', '0 0 0/120 ? * 2-6', flags='oversized-step', note='Increment > 23 : 120'),
            row('invalid', '0 0 / ? * 2-6', note="'/' must be followed by an integer"),
            row('invalid', '0 0 0/ ? * 2-6', note="'/' must be followed by an integer"),
        ]),
        1233: ('TestDayOfMonthRangeIntervalAfterSlash', [
            row('invalid', '0 0 0 /120 * 2-6', note='a step with no start; Bodu rejects the empty start'),
            row('invalid', '0 0 0 0/120 * 2-6', note='Increment > 31 : 120; Bodu rejects day 0 as well'),
            row('invalid', '0 0 0 / * 2-6', note="'/' must be followed by an integer"),
            row('invalid', '0 0 0 0/ * 2-6', note="'/' must be followed by an integer"),
        ]),
        1253: ('TestMonthRangeIntervalAfterSlash', [
            row('invalid', '0 0 0 ? /120 2-6', note='a step with no start; Bodu rejects the empty start'),
            row('invalid', '0 0 0 ? 0/120 2-6', note='Increment > 12 : 120; Bodu rejects month 0 as well'),
            row('invalid', '0 0 0 ? / 2-6', note="'/' must be followed by an integer"),
            row('invalid', '0 0 0 ? 0/ 2-6', note="'/' must be followed by an integer"),
        ]),
        1273: ('TestDayOfWeekRangeIntervalAfterSlash', [
            row('invalid', '0 0 0 ? * /120', note='a step with no start; Bodu rejects the empty start'),
            row('invalid', '0 0 0 ? * 0/120', flags='oversized-step',
                note='Increment > 7 : 120; Bodu reads 0 as Sunday and the step as selecting it'),
            row('invalid', '0 0 0 ? * /', note="'/' must be followed by an integer"),
            row('invalid', '0 0 0 ? * 0/', note="'/' must be followed by an integer"),
        ]),
        1294: ('TestInvalidCharactersAfterAsterisk', [
            row('invalid', '* * * ? * *A&/5:'),
            row('invalid', '* * * ? *14 ', note='five fields where six are required'),
            row('invalid', ' * * ? *A&/5 *', note='five fields where six are required'),
            row('invalid', '* * ? */5 *', note='five fields where six are required'),
            row('invalid', '* * ? */52 *', note='five fields where six are required'),
            row('valid', '0 0/30 * * * ?'),
            row('valid', '0 0/1 * * * ?'),
            row('valid', '0 0/30 * * */2 ?'),
        ]),
        1311: ('TestInvalidCronExpressionCharacters', [
            row('invalid', '0 0x 0/1 ? * *', note=YSTAR),
            row('invalid', '0 1xdds0 0/1 ? * *', note=YSTAR),
        ]),
        1321: ('TestExtraCharactersAfterWeekDay', [row('invalid', '0 0 15 ? * FRI*')]),
        1327: ('TestHourRangeAndSlash', [row('valid', '0 0 18-21/1 ? * MON,TUE,WED,THU,FRI,SAT,SUN')]),
        1336: ('PerformanceTest', 'no-assertion'),
        1359: ('CanGetNextInvalidTime', 'api'),
        1367: ('CanGetHashCode', [
            row('equal', '0 15 15 5 11 ?', expected='0 15 15 5 11 ?', note='equal hash codes of two parses'),
        ]),
        1381: ('ATimeZoneOfNullIsUnambiguousAndMeansTheLocalZone', 'api'),
        # A custom fixed +11:00 zone: 2026-04-16 00:00Z is Thursday 11:00 there. The test asserts a previous firing
        # exists and is earlier; the value is the Friday before, at midnight in that offset.
        1407: ('GetPreviousValidTimeBeforeDoesNotThrowForPositiveOffsetTimeZone', [
            row('previous', '0 0 0 ? * FRI', '2026-04-16T11:00:00+11:00', '2026-04-10T00:00:00+11:00',
                expectation='derived', note=join('issue #3046', YSTAR, 'the custom +11 zone is the fixed offset '
                                                 '+11:00; upstream asserts only a non-null earlier answer, the value is '
                                                 'the previous Friday midnight')),
        ]),
        1521: ('TooManyTokensShouldThrow', [row('invalid', '0 15 10 * * ? 2005 *', note='eight fields')]),
        1636: ('TestGetPreviousValidTimeBeforeSearchedFromInsideAPairOfFirings', [
            row('next-sequence', '0/30 1 2 * * ?', '2024-03-14T02:01:10', '2024-03-14T02:01:30|2024-03-15T02:01:00',
                note=join(YSTAR, 'the firing after 02:01:10, then 24 h - 30 s later')),
            row('previous', '0/30 1 2 * * ?', '2024-03-14T02:01:30', '2024-03-14T02:01:00',
                note=join(YSTAR, 'the firing 30 s before')),
        ]),
    },
    DIVERGENCE: {
        63: ('ANumericDayOfWeekIsOneDayEarlierThanACrontabDerivedLibraryMeansIt', [
            row('next', '0 0 2 * * SUN', '2026-08-21T00:00:00', '2026-08-23T02:00:00',
                note='Quartz 1 (Sunday) written SUN; Bodu reads the digit 1 as Monday'),
        ]),
        79: ('BothDayFieldsRestrictedFiresOnTheirUnionRatherThanTheirIntersection', [
            row('next', '0 0 2 5 * MON', '2026-08-21T00:00:00', '2026-08-24T02:00:00', note='union, as in Bodu'),
            row('next', '0 0 2 5 * MON', '2026-08-31T03:00:00', '2026-09-05T02:00:00', note='union, as in Bodu'),
        ]),
    },
    WRAPPING: {
        40: ('AnHourRangeWrapsThroughMidnight', [
            row('equal', '0 0 22-2 * * ?', expected='0 0 0,1,2,22,23 * * ?', flags='wrap-range',
                note='GetSet(Hour) is [0, 1, 2, 22, 23]'),
            row('next-sequence', '0 0 22-2 * * ?', '2024-01-01T00:00:00',
                '2024-01-01T01:00:00|2024-01-01T02:00:00|2024-01-01T22:00:00|2024-01-01T23:00:00|2024-01-02T00:00:00'
                '|2024-01-02T01:00:00|2024-01-02T02:00:00', flags='wrap-range'),
            misses('0 0 22-2 * * ?', '2024-01-01T12:00:00', '2024-01-01T22:00:00', 'under Quartz the wrap resumes '
                   'at 22:00', flags='wrap-range'),
        ]),
        63: ('ADayOfWeekRangeWrapsThroughSunday', [
            row('equal', '0 0 12 ? * FRI-MON', expected='0 0 12 ? * SUN,MON,FRI,SAT', flags='wrap-range',
                note='GetSet(DayOfWeek) is [1, 2, 6, 7] in Quartz numbering, written by name'),
            row('next-sequence', '0 0 12 ? * FRI-MON', '2024-01-01T00:00:00',
                '2024-01-01T12:00:00|2024-01-05T12:00:00|2024-01-06T12:00:00|2024-01-07T12:00:00|2024-01-08T12:00:00'
                '|2024-01-12T12:00:00', flags='wrap-range'),
            misses('0 0 12 ? * FRI-MON', '2024-01-03T12:00:00', '2024-01-05T12:00:00', 'Wednesday; under Quartz '
                   'Friday 5 January is next', flags='wrap-range'),
        ]),
        86: ('AMonthRangeWrapsThroughNewYear', [
            row('equal', '0 0 12 1 NOV-FEB ?', expected='0 0 12 1 1,2,11,12 ?', flags='wrap-range',
                note='GetSet(Month) is [1, 2, 11, 12]'),
            row('next-sequence', '0 0 12 1 NOV-FEB ?', '2024-01-01T00:00:00',
                '2024-01-01T12:00:00|2024-02-01T12:00:00|2024-11-01T12:00:00|2024-12-01T12:00:00|2025-01-01T12:00:00',
                flags='wrap-range'),
            misses('0 0 12 1 NOV-FEB ?', '2024-06-01T12:00:00', '2024-11-01T12:00:00', 'June; under Quartz 1 '
                   'November is next', flags='wrap-range'),
        ]),
        111: ('SecondAndMinuteRangesWrapThroughTheTopOfTheirField', [
            row('equal', '58-1 59-0 12 * * ?', expected='0,1,58,59 0,59 12 * * ?', flags='wrap-range',
                note='GetSet(Second) is [0, 1, 58, 59] and GetSet(Minute) [0, 59]'),
            row('next-sequence', '58-1 59-0 12 * * ?', '2024-01-01T11:00:00',
                '2024-01-01T12:00:00|2024-01-01T12:00:01|2024-01-01T12:00:58|2024-01-01T12:00:59|2024-01-01T12:59:00',
                flags='wrap-range'),
        ]),
    },
}

# ---------------------------------------------------------------------------------------------------------------------
# [TestCase] handlers
# ---------------------------------------------------------------------------------------------------------------------

def quartz_dow(field: str) -> str:
    """Writes Quartz day-of-week digits by name: a day, d#k, dL, a forward range, or a list of these.

    Quartz numbers the week 1 = Sunday to 7 = Saturday, Bodu 0 (or 7) = Sunday to 6 = Saturday, so a digit is written
    as the day's name, which both read alike. Any other shape is rejected so that the rows that need it are adapted by
    hand.
    """
    def day(text: str) -> str:
        if text.isdigit():
            number = int(text)
            if number not in QUARTZ_DAY:
                fail(f'day-of-week digit {number} has no Quartz meaning')
            return QUARTZ_DAY[number]
        return text

    parts = []
    for item in field.split(','):
        match = re.fullmatch(r'(\w+?)(#\d|L)?', item)
        if match and '-' not in item:
            parts.append(day(match.group(1)) + (match.group(2) or ''))
            continue
        match = re.fullmatch(r'(\w+)-(\w+)', item)
        if match:
            parts.append(f'{day(match.group(1))}-{day(match.group(2))}')
            continue
        fail(f'cannot write {item!r} by name')
    return ','.join(parts)


def drop_year(expression: str) -> str:
    fields = expression.split()
    if len(fields) != 7:
        fail(f'{expression!r} has no year field')
    return ' '.join(fields[:6])


def october_2010(day: int) -> str:
    return f'2010-10-{day:02d}T10:15:00'


# The first occurrence after October 2010 for each CanUse_DayOfMonth_And_DayOfWeek_Together row, which closes the
# month: with it, the run from 2010-10-01 shows every October day the test asserts does not fire. Worked out by hand
# (1 November 2010 is a Monday; 30 November is the last day; 15 November a Monday; 19 November the third Friday and 26
# November the last), and checked with cronsim 2.7 for the rows it can read.
NOVEMBER_CLOSE = {
    1: 1, 2: 1, 3: 1, 4: 1, 5: 1, 6: 1, 7: 1, 8: 1, 9: 1, 10: 1, 11: 1, 12: 30, 13: 15, 14: 19, 15: 26,
}

# CanUse... and the other IsSatisfiedBy tables write their days as int arrays.
INTS = re.compile(r'-?\d+')


def int_array(arg: str) -> list[int]:
    """Evaluates an int array initialiser such as new[] { 31 - 1, 31 - 2 }."""
    body = arg[arg.index('{') + 1:arg.rindex('}')]
    out = []
    for item in body.split(','):
        item = item.strip()
        if not re.fullmatch(r'\d+( - \d+)?', item):
            fail(f'unexpected array item {item!r}')
        out.append(eval_minus(item))
    return out


def eval_minus(text: str) -> int:
    parts = [int(p) for p in text.split(' - ')]
    return parts[0] - sum(parts[1:])


def is_token_list(dom: str) -> bool:
    return ',' in dom and bool(re.search(r'L|W', dom))


def handle(method: str, counter: int, args: list[str]) -> list[dict]:
    """Restates one [TestCase] row of CronExpressionTest.cs or CronDialectDivergenceTest.cs."""
    if method == 'ExpressionToString':
        expression, expected = literal(args[0]), literal(args[1])
        return [row('to-string', expression, expected=expected, flags='year-field',
                    note='Quartz echoes the text it was given, year included; Bodu has no year field')]

    if method in ('CanUseMultipleLastDayOfMonthInArray', 'CanUseLastDayOfMonthInArray'):
        expression, days = drop_year(literal(args[0])), int_array(args[1])
        dom = expression.split()[3]
        flags = []
        if is_token_list(dom):
            flags.append('token-list')
        if 'LW-' in dom:
            flags.append('syntax-extension')
        return [holds(expression, october_2010(d), flags=' '.join(flags), note=join(Y2010, f'day {d}'))
                for d in sorted(set(days))]

    if method == 'CanUse_DayOfMonth_And_DayOfWeek_Together':
        upstream, days = literal(args[0]), sorted(set(int_array(args[1])))
        fields = drop_year(upstream).split()
        fields[5] = quartz_dow(fields[5]) if fields[5] not in '*?' else fields[5]
        expression = ' '.join(fields)
        close = NOVEMBER_CLOSE[counter]
        run = [october_2010(d) for d in days] + [f'2010-11-{close:02d}T10:15:00']
        renamed = ' (Quartz digits written by name)' if expression.split()[5] != upstream.split()[5] else ''
        return [row('next-sequence', expression, '2010-10-01T00:00:00', '|'.join(run), 'derived',
                    note=join(Y2010 + renamed, 'IsSatisfiedBy over every day of October 2010: the run from 1 October '
                              'lists the days upstream asserts fire, then the first November occurrence, worked out, '
                              'so that every other October day is shown not to fire'))]

    if method == 'LastWeekDayWithOffset':
        expression, day = drop_year(literal(args[0])), int(args[1])
        return [holds(expression, october_2010(day), flags='syntax-extension', note=Y2010)]

    if method == 'Ensure_NthWeek_IsBetween1And5':
        upstream, valid = literal(args[0]), args[1] == 'true'
        fields = drop_year(upstream).split()
        fields[5] = 'SUN' + fields[5][1:]  # Quartz 1#k, the k-th Sunday
        return [row('valid' if valid else 'invalid', ' '.join(fields), note=join(Y2010, 'Quartz 1 (Sunday) written SUN'))]

    if method == 'Ensure_NthWeek_Day_IsBetween1And7':
        upstream, valid = literal(args[0]), args[1] == 'true'
        fields = drop_year(upstream).split()
        digit = int(fields[5].split('#')[0])
        if not valid:
            if digit == 0:
                return [row('invalid', ' '.join(fields), flags='quartz-dow-numbering',
                            note=join(Y2010, 'Quartz has no day 0; Bodu reads it as Sunday'))]
            return [row('invalid', ' '.join(fields),
                        note=join(Y2010, f'day {digit} is outside both Quartz 1-7 and Bodu 0-7, kept as written'))]
        fields[5] = quartz_dow(fields[5])
        at = literal(args[2])
        return [holds(' '.join(fields), at, note=join(Y2010, f'Quartz {digit}#1 written {fields[5]}'))]

    if method == 'ExpressionEquality':
        expression = drop_year(literal(args[0]))
        flags = 'token-list' if is_token_list(expression.split()[3]) else ''
        return [row('equal', expression, expected=expression, flags=flags,
                    note=join(Y2010, 'Equals and GetHashCode of two parses'))]

    if method == 'TryParseRejectsAnInvalidExpression':
        if args[0] == 'null':
            return []  # the null argument is API, counted as left out
        return [row('invalid', literal(args[0]), note='TryParse returns false and a null result')]

    if method == 'OffSetValue_CannontBe_GreaterThan30':
        return [row('invalid', drop_year(literal(args[0])),
                    note=join(Y2010, 'Offset from last day must be <= 30; Bodu rejects L-31, and a token in a list'))]

    if method == 'Ensure_L_Token_CanOnlyBeUsedIn_DayOfWeek_ORDayOfMonth':
        upstream, valid = literal(args[0]), args[1] == 'true'
        if upstream.split()[6] == 'L':
            return [row('invalid', upstream, flags='year-field', note='L in the year field')]
        expression = drop_year(upstream)
        if expression.split()[5] == 'L':
            return [row('valid', expression, flags='syntax-extension',
                        note=join(Y2010, 'L alone in day-of-week is Saturday in Quartz'))]
        return [row('valid' if valid else 'invalid', expression, note=Y2010)]

    if method == 'TheFiveFieldMessageRenumbersTheDayOfWeekItWasGiven':
        return [row('invalid', literal(args[0]), note='five fields where six are required; the message is not '
                                                      'transcribed')]

    if method == 'TheFiveFieldMessageIsTheSameOnTheHashResolvingPath':
        return [row('invalid', literal(args[0]), flags='hash',
                    note='ParseWithHash; five fields where six are required')]

    if method in ('ExpressionsThatSaidOneThingAndDidAnotherAreRejected', 'ARejectionNamesWhatIsWrong',
                  'ATextualDayOfWeekStepThatIsNotAStepIsRejected', 'AStepInsideARangeIsRangeCheckedLikeAnyOtherStep'):
        expression = literal(args[0])
        message = literal(args[1])
        fields = expression.split()
        if len(fields) == 7:
            return [row('invalid', expression, flags='year-field', note=f'{message}: the year field')]
        if method == 'AStepInsideARangeIsRangeCheckedLikeAnyOtherStep' or expression.endswith('SUN/9'):
            return [row('invalid', expression, flags='oversized-step', note=message)]
        return [row('invalid', expression, note=f'message: {message}')]

    if method == 'ExpressionsTheNewRejectionsMustNotCatchStillParse':
        upstream = literal(args[0])
        fields = upstream.split()
        expression = drop_year(upstream) if len(fields) == 7 else upstream
        note = Y2010 if len(fields) == 7 else ''
        dom, dow = expression.split()[3], expression.split()[5]
        if dow == 'L':
            return [row('valid', expression, flags='syntax-extension', note=join(note, 'L alone in day-of-week'))]
        if dom.startswith('LW-'):
            return [row('valid', expression, flags='syntax-extension', note=join(note, 'LW-n'))]
        if re.fullmatch(r'\d+[L#]\d?', dow):
            renamed = quartz_dow(dow)
            expression = ' '.join(expression.split()[:5] + [renamed])
            note = join(note, f'Quartz {dow} written {renamed}')
        elif dow == '2/2':
            expression = ' '.join(expression.split()[:5] + ['MON-SAT/2'])
            note = join(note, 'Quartz 2/2 (Monday to Saturday, every second day) written MON-SAT/2')
        elif dow == 'MON/2':
            note = join(note, 'Bodu runs a stepped name to 7, Sunday, so MON/2 also selects Sunday; the row asserts '
                              'only that it parses')
        return [row('valid', expression, note=note)]

    if method == 'ATextualDayOfWeekStepIsItsNumericTwin':
        textual, numeric = literal(args[0]), literal(args[1])
        start, _, rest = numeric.partition('/')
        if '-' in start:
            low, high = (int(day) for day in start.split('-'))
            twin = f'{low - 1}-{high - 1}/{rest}'
        else:
            low = high = int(start)
            twin = f'{low - 1}/{rest}'
        note = f'the Quartz twin {numeric} renumbered to {twin}; the two spellings are one schedule'
        if low > high:
            # A range that wraps the week (FRI-MON) is renumbered like the others and flagged: Bodu rejects it.
            return [row('equal', f'0 0 12 ? * {textual}', expected=f'0 0 12 ? * {twin}', flags='wrap-range',
                        note=note)]
        return [row('equal', f'0 0 12 ? * {textual}', expected=f'0 0 12 ? * {twin}',
                    note=f'{note} (Bodu runs a stepped day to 7, Sunday, in both)')]

    if method == 'Should_Throw_Error_When_Extra_NonWhitespace_Character_After_QuestionMark':
        return [row('invalid', f'0 0 * * * ?{char(args[0])}', note="Illegal character after '?'")]

    if method == 'QuestionMark_With_ExtraWhitespace_Should_Be_Valid':
        return [row('valid', f'0 0 * * * ?{char(args[0])}', note='trailing whitespace after ?')]

    if method == 'QuestionMark_IsRejectedOutsideTheTwoDayFields':
        expression, field = literal(args[0]), literal(args[1])
        if field == 'year':
            return [row('invalid', expression, flags='year-field', note="'?' in the year field")]
        return [row('invalid', expression, note=f"'?' in the {field} field")]

    if method == 'GivenMonthAbbreviation_ShouldGetTimeAfter':
        month, number = literal(args[0]), int(args[1])
        return [row('next', f'0 0 0 1 {month} ?', '2024-07-22T12:00:00', f'2024-{number:02d}-01T00:00:00',
                    note=join(YSTAR, 'UTC zone and offset, read as wall clock'))]

    if method == 'AnExpressionFromACrontabDerivedLibraryUsuallyMeansTheSameThing':
        expression, expected = literal(args[0]), literal(args[1])
        if not expected.endswith('Z'):
            fail(f'unexpected instant {expected!r}')
        return [row('next', expression, '2026-08-21T00:00:00', expected[:-1],
                    note='UTC zone, searched from SearchFrom 2026-08-21T00:00Z (L47); read as wall clock')]

    fail(f'[TestCase] method {method} has no handler')
    return []


def literal(arg: str) -> str:
    if not (arg.startswith('"') and arg.endswith('"')):
        fail(f'expected a string literal, got {arg!r}')
    return arg[1:-1]


def char(arg: str) -> str:
    match = re.fullmatch(r"'(\\?.)'", arg)
    if not match:
        fail(f'expected a char literal, got {arg!r}')
    return {'\\t': '\t'}.get(match.group(1), match.group(1))


# ---------------------------------------------------------------------------------------------------------------------
# Parsing
# ---------------------------------------------------------------------------------------------------------------------

def attributes(text: str, name: str) -> list[tuple[int, str]]:
    """Returns (line, argument text) for every [name(...)] attribute, which may span several lines."""
    out = []
    marker = f'[{name}('
    start = text.find(marker)
    while start >= 0:
        line = text.count('\n', 0, start) + 1
        depth, quoted, quote, i = 0, False, '', start + len(marker) - 1
        while True:
            ch = text[i]
            if quoted:
                if ch == '\\':
                    i += 1
                elif ch == quote:
                    quoted = False
            elif ch in '"\'':
                quoted, quote = True, ch
            elif ch in '({':
                depth += 1
            elif ch in ')}':
                depth -= 1
                if depth == 0:
                    break
            i += 1
        out.append((line, text[start + len(marker):i]))
        start = text.find(marker, i)
    return out


def split_args(text: str) -> list[str]:
    args, buf, depth, quoted, quote = [], [], 0, False, ''
    i = 0
    while i < len(text):
        ch = text[i]
        if quoted:
            buf.append(ch)
            if ch == '\\':
                buf.append(text[i + 1])
                i += 1
            elif ch == quote:
                quoted = False
        elif ch in '"\'':
            quoted, quote = True, ch
            buf.append(ch)
        elif ch in '{(':
            depth += 1
            buf.append(ch)
        elif ch in '})':
            depth -= 1
            buf.append(ch)
        elif ch == ',' and depth == 0:
            args.append(''.join(buf).strip())
            buf = []
        else:
            buf.append(ch)
        i += 1
    args.append(''.join(buf).strip())
    return args


def method_after(lines: list[str], line: int) -> str:
    for following in lines[line:]:
        match = METHOD.match(following)
        if match:
            return match.group(1)
    fail(f'no method after line {line}')
    return ''


def declared_tests(lines: list[str]) -> dict[int, str]:
    """Returns {declaration line: method} for every method carrying [Test]."""
    out = {}
    for number, line in enumerate(lines, 1):
        if line.strip() == '[Test]':
            for follower in range(number, min(number + 4, len(lines))):
                match = METHOD.match(lines[follower])
                if match:
                    out[follower + 1] = match.group(1)
                    break
    return out


def iso(year: int, month: int, day: int, hour: int, minute: int, second: int, millisecond: int = 0) -> str:
    text = f'{year:04d}-{month:02d}-{day:02d}T{hour:02d}:{minute:02d}:{second:02d}'
    return text + (f'.{millisecond:03d}' if millisecond else '')


def scenario_rows(text: str) -> list[dict]:
    """CronTestScenarios: the next fire time after each scenario's instant, compared by date upstream."""
    out = []
    pattern = re.compile(
        r'CronExpression = new CronExpression\("([^"]+)"\),\s*'
        r'TimeAfterDate = new DateTimeOffset\((\d+), (\d+), (\d+), (\d+), (\d+), (\d+), TimeSpan\.Zero\),\s*'
        r'ExpectedNextFireTime = new DateTimeOffset\((\d+), (\d+), (\d+), (\d+), (\d+), (\d+), TimeSpan\.Zero\),\s*'
        r'TestCase = "([^"]+)"')
    for counter, match in enumerate(pattern.finditer(text), 1):
        line = text.count('\n', 0, match.start()) + 1
        upstream = match.group(1)
        after = iso(*map(int, match.group(2, 3, 4, 5, 6, 7)))
        expected = iso(*map(int, match.group(8, 9, 10, 11, 12, 13)))
        fields = upstream.split()
        note = 'the test compares dates only; the instant is the scenario ExpectedNextFireTime'
        expectation = 'upstream'
        if re.fullmatch(r'\d[L#]\d?', fields[5]):
            fields[5] = quartz_dow(fields[5])
            note = join(note, f'Quartz {upstream.split()[5]} written {fields[5]}')
        elif fields[5] == '2/2':
            fields[5] = 'MON-SAT/2'
            note = join(note, 'Quartz 2/2 (Monday to Saturday, every second day) written MON-SAT/2')
        if fields[3] == '31W':
            # Quartz clamps 31W into a shorter month (issue #2330: Friday 28 February 2025). Bodu's documented reading
            # passes over a month without a 31st, so the next is 31 March 2025, a Monday.
            note = join(note, 'Quartz clamps 31W to 2025-02-28 (issue #2330); restated in Bodu reading, which passes '
                              'over a month without a 31st: 31 March 2025 is a Monday')
            expected, expectation = '2025-03-31T12:00:00', 'derived'
        out.append(dict(row('next', ' '.join(fields), after, expected, expectation, note=join(match.group(14), note)),
                        name=f'CronExpressionReturnsExpectedNextFireTime-{counter:02d}', line=line,
                        method='CronExpressionReturnsExpectedNextFireTime', file=MAIN))
    if len(out) != 10:
        fail(f'expected 10 CronTestScenarios, found {len(out)}')
    return out


# The first firing after each search instant for the six repeating expressions of FiringGapsAround, worked out by hand
# and checked with cronsim 2.7: the test measures the gaps around that firing but does not state it. Key: (instant
# label, expression index); value: the firing.
AFTER = {
    ('mid-minute', 0): '2024-03-14T08:37:24', ('mid-minute', 1): '2024-03-14T08:38:00',
    ('mid-minute', 2): '2024-03-14T08:37:30', ('mid-minute', 3): '2024-03-15T05:00:00',
    ('mid-minute', 4): '2024-03-15T00:00:00', ('mid-minute', 5): '2024-03-15T02:01:00',
    ('second boundary', 0): '2024-03-14T08:37:24', ('second boundary', 1): '2024-03-14T08:38:00',
    ('second boundary', 2): '2024-03-14T08:37:30', ('second boundary', 3): '2024-03-15T05:00:00',
    ('second boundary', 4): '2024-03-15T00:00:00', ('second boundary', 5): '2024-03-15T02:01:00',
    ('minute boundary', 0): '2024-03-14T08:37:01', ('minute boundary', 1): '2024-03-14T08:38:00',
    ('minute boundary', 2): '2024-03-14T08:37:15', ('minute boundary', 3): '2024-03-15T05:00:00',
    ('minute boundary', 4): '2024-03-15T00:00:00', ('minute boundary', 5): '2024-03-15T02:01:00',
    ('hour boundary', 0): '2024-03-14T08:00:01', ('hour boundary', 1): '2024-03-14T08:01:00',
    ('hour boundary', 2): '2024-03-14T08:00:15', ('hour boundary', 3): '2024-03-15T05:00:00',
    ('hour boundary', 4): '2024-03-15T00:00:00', ('hour boundary', 5): '2024-03-15T02:01:00',
    ('day boundary', 0): '2024-03-14T00:00:01', ('day boundary', 1): '2024-03-14T00:01:00',
    ('day boundary', 2): '2024-03-14T00:00:15', ('day boundary', 3): '2024-03-14T05:00:00',
    ('day boundary', 4): '2024-03-15T00:00:00', ('day boundary', 5): '2024-03-14T02:01:00',
    ('year boundary', 0): '2024-01-01T00:00:01', ('year boundary', 1): '2024-01-01T00:01:00',
    ('year boundary', 2): '2024-01-01T00:00:15', ('year boundary', 3): '2024-01-01T05:00:00',
    ('year boundary', 4): '2024-01-02T00:00:00', ('year boundary', 5): '2024-01-01T02:01:00',
}


def add_ms(instant: str, milliseconds: int) -> str:
    """Adds a gap, which is a whole number of seconds, to an ISO instant."""
    value = dt.datetime.fromisoformat(instant) + dt.timedelta(milliseconds=milliseconds)
    return value.strftime('%Y-%m-%dT%H:%M:%S')


def previous_gap_rows(text: str) -> list[dict]:
    """TestGetPreviousValidTimeBefore: the gaps back and forward from the firing after each search instant."""
    gaps = []
    for match in re.finditer(r'new FiringGaps\(\$?"([^"]+)", ([^,]+), ([^)]+)\)', text):
        expression = match.group(1)
        back, forward = (eval_gap(g) for g in match.group(2, 3))
        gaps.append((text.count('\n', 0, match.start()) + 1, expression, back, forward))
    instants = []
    for match in re.finditer(r'new TestCaseData\("([^"]+)", new DateTimeOffset\(([\d, ]+), TimeSpan\.Zero\)\)', text):
        values = [int(v) for v in match.group(2).split(',')]
        instants.append((text.count('\n', 0, match.start()) + 1, match.group(1), iso(*values)))
    if len(gaps) != 8 or len(instants) != 6:
        fail(f'expected 8 gaps and 6 instants, found {len(gaps)} and {len(instants)}')
    out = []
    method = 'TestGetPreviousValidTimeBefore'
    for instant_line, label, now in instants:
        slug = label.replace(' ', '-')
        year = int(now[:4])
        for index, (gap_line, expression, back, forward) in enumerate(gaps):
            base = dict(method=method, file=MAIN, line=instant_line)
            where = f'instant L{instant_line}, gaps L{gap_line}'
            if back is None:
                pinned = expression.replace('{year + 2}', str(year + 2)).replace('{year - 2}', str(year - 2))
                pinned_year = int(pinned.split()[6])
                if pinned_year > year:
                    first = f'{pinned_year}-01-01T00:00:00'
                    out.append(dict(row('unreachable-previous', pinned, first, '', 'derived', 'year-field',
                                        join(where, f'the year {pinned_year} is ahead: from its first firing {first} '
                                                    '(worked out) nothing fires before')),
                                    name=f'{method}-{slug}-{index + 1}-previous', **base))
                else:
                    last = f'{pinned_year}-12-31T23:59:59'
                    out.append(dict(row('unreachable-next', pinned, now, '', 'derived', 'year-field',
                                        join(where, f'the year {pinned_year} is past: nothing fires after the '
                                                    'instant')),
                                    name=f'{method}-{slug}-{index + 1}-next', **base))
                    out.append(dict(row('previous', pinned, now, last, 'derived', 'year-field',
                                        join(where, 'upstream asserts a previous firing exists; it is the last second '
                                                    f'of {pinned_year}, worked out')),
                                    name=f'{method}-{slug}-{index + 1}-previous', **base))
                continue
            plain = ' '.join(expression.split()[:6])
            after = AFTER[(label, index)]
            out.append(dict(row('next-sequence', plain, now, f'{after}|{add_ms(after, forward)}', 'derived',
                                note=join(where, YSTAR, f'the firing after the instant (worked out), then '
                                                        f'{forward // 1000} s later as upstream asserts')),
                            name=f'{method}-{slug}-{index + 1}-next', **base))
            out.append(dict(row('previous', plain, after, add_ms(after, -back), 'derived',
                                note=join(where, YSTAR, f'{back // 1000} s before that firing, as upstream asserts')),
                            name=f'{method}-{slug}-{index + 1}-previous', **base))
    return out


def eval_gap(text: str) -> int | None:
    """Evaluates a gap written as null, N_NNNL, OneDayInMilliseconds or OneDayInMilliseconds - 30_000L."""
    text = text.strip()
    if text == 'null':
        return None
    terms = [t.strip() for t in text.split(' - ')]
    values = [86_400_000 if t == 'OneDayInMilliseconds' else int(t.rstrip('L').replace('_', '')) for t in terms]
    return values[0] - sum(values[1:])


# ---------------------------------------------------------------------------------------------------------------------

def main(argv: list[str]) -> int:
    if len(argv) != 4:
        sys.stderr.write(__doc__)
        return 2
    texts = {}
    for (path, digest), local in zip(UPSTREAM, argv[1:]):
        data = open(local, 'rb').read()
        actual = hashlib.sha256(data).hexdigest()
        if actual != digest:
            fail(f'{local} has SHA-256 {actual}, expected {digest} ({path} at {UPSTREAM_COMMIT})')
        texts[path.split('/')[-1]] = data.decode('utf-8')

    rows: list[dict] = []
    left_out: dict[str, int] = {}
    for name, text in texts.items():
        lines = text.split('\n')
        # [TestCase] rows, grouped by method in file order.
        counters: dict[str, int] = {}
        for line, argument_text in attributes(text, 'TestCase'):
            method = method_after(lines, line)
            counters[method] = counters.get(method, 0) + 1
            produced = handle(method, counters[method], split_args(argument_text))
            if not produced:
                left_out['api-row'] = left_out.get('api-row', 0) + 1
            for index, item in enumerate(produced, 1):
                suffix = f'-{index}' if len(produced) > 1 else ''
                rows.append(dict(item, name=f'{method}-{counters[method]:02d}{suffix}', method=method, file=name,
                                 line=line))
        # [Test] methods.
        declared = declared_tests(lines)
        listed = {line: method for line, (method, _) in TESTS.get(name, {}).items()}
        if declared != listed:
            fail(f'{name}: [Test] list disagrees with the file: file {sorted(set(declared.items()) - set(listed.items()))}'
                 f', list {sorted(set(listed.items()) - set(declared.items()))}')
        for line, (method, spec) in sorted(TESTS.get(name, {}).items()):
            if isinstance(spec, str):
                left_out[spec] = left_out.get(spec, 0) + 1
                continue
            for index, item in enumerate(spec, 1):
                rows.append(dict(item, name=f'{method}-{index:02d}', method=method, file=name, line=line))

    sources = sorted(method_after(texts[MAIN].split('\n'), line) for line, _ in attributes(texts[MAIN], 'TestCaseSource'))
    if sources != ['CronExpressionReturnsExpectedNextFireTime', 'TestGetPreviousValidTimeBefore'] or any(
            attributes(texts[name], 'TestCaseSource') for name in (DIVERGENCE, WRAPPING)):
        fail(f'unexpected [TestCaseSource] methods: {sources}')
    rows += scenario_rows(texts[MAIN])
    rows += previous_gap_rows(texts[MAIN])

    names = [r['name'] for r in rows]
    if len(names) != len(set(names)):
        dupes = sorted({n for n in names if names.count(n) > 1})
        fail(f'duplicate row names: {dupes}')
    order = {MAIN: 0, DIVERGENCE: 1, WRAPPING: 2}
    rows.sort(key=lambda r: (order[r['file']], r['line'], r['name']))
    sys.stdout.write(render(rows, left_out))
    return 0


def render(rows: list[dict], left_out: dict[str, int]) -> str:
    flags = sorted({f for r in rows for f in r['flags'].split()})
    header = [
        "# Cron vectors derived from Quartz.NET's CronExpressionTest.cs, CronDialectDivergenceTest.cs and "
        'CronExpressionWrappingRangeTest.cs',
        '# source-class: third-party-comparison (implementation test suite, not an authority)',
        '# source: Quartz.NET -- https://github.com/quartznet/quartznet -- '
        + ', '.join(path for path, _ in UPSTREAM),
        f'# commit: {UPSTREAM_COMMIT}',
    ]
    header += [f'# file-sha256: {path} {digest}' for path, digest in UPSTREAM]
    header += [
        '# licence: Apache-2.0 (Copyright Marko Lahma and the Quartz.NET contributors). The rows restate the '
        'assertions (expressions and instants) with attribution; the upstream files are not committed.',
        '# method: extracted by extract-quartznet-cron-vectors.py ([TestCase] rows and the two [TestCaseSource] '
        'tables parsed from the files; [Test] assertions from a hand-maintained list in the script, each with its '
        'line)',
        '# note: Quartz puts seconds first, has an optional seventh year field (dropped where the answer stays in '
        'that year), numbers weekdays 1 = Sunday (digits written by name, or renumbered, wherever the row depends on '
        'them; a row asserting a rejection for another reason keeps them as written), takes the union of two restricted '
        'day fields as Bodu does, and accepts token lists, LW-n, L alone in day-of-week and wrapping ranges; '
        'IsSatisfiedBy true is a next-inclusive row answering the instant, false a next-inclusive row answering the '
        'next occurrence. Left out: '
        f'{left_out.get("api", 0)} tests and {left_out.get("api-row", 0)} [TestCase] row of the library API with no '
        f'cron meaning, {left_out.get("zone", 0)} tests that depend on a time-zone transition, '
        f'{left_out.get("clock", 0)} that read the wall clock and {left_out.get("no-assertion", 0)} [Explicit] '
        'benchmark with no assertion; message-text assertions, and the wall-clock IsSatisfiedBy check of '
        'TestCronExpressionWithExtraWhiteSpace, are not transcribed',
    ]
    header += [f'# flags: {flag} = {FLAG_MEANINGS[flag]}' for flag in flags]
    buffer = io.StringIO()
    writer = csv.writer(buffer, lineterminator='\n')
    writer.writerow(COLUMNS)
    for r in rows:
        where = f'{r["method"]} L{r["line"]}' if r['file'] == MAIN else f'{r["file"]}: {r["method"]} L{r["line"]}'
        writer.writerow([r['name'], r['kind'], r['format'], r['expression'], r['frm'], r['expected'],
                         r['expectation'], r['flags'], join(where, r['note'])])
    return '\n'.join(header) + '\n' + quote_padded(buffer.getvalue())


def quote_padded(text: str) -> str:
    """Quotes the fields that begin or end with whitespace, which csv leaves bare, so that no reader trims them."""
    lines = []
    for record in csv.reader(io.StringIO(text)):
        cells = []
        for field in record:
            if field != field.strip() or any(c in field for c in ',"\n\t'):
                cells.append('"' + field.replace('"', '""') + '"')
            else:
                cells.append(field)
        lines.append(','.join(cells))
    return '\n'.join(lines) + '\n'


if __name__ == '__main__':
    sys.exit(main(sys.argv))
