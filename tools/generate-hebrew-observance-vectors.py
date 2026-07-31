#!/usr/bin/env python3
"""Generates the Hebrew-calendar observance regression vectors for the global-jewish catalogue.

The Hebrew calendar arithmetic below is an independent implementation of the classical molad /
postponement (dechiyot) rules as presented in Dershowitz & Reingold, *Calendrical Calculations*
(the same fixed arithmetic Hebcal and System.Globalization.HebrewCalendar implement), so the
emitted vectors do not derive from the code under test. Python's ``date.toordinal`` is the
Rata Die fixed-day number used throughout.

Usage:
    python3 tools/generate-hebrew-observance-vectors.py > \
        Bodu.Globalization.Calendar/test/Globalization.Calendar/Fixtures/Vectors/HebrewObservances-1990-2039.csv
"""

from __future__ import annotations

import datetime

FIRST_GREGORIAN_YEAR = 1990
LAST_GREGORIAN_YEAR = 2039

# Rata Die fixed-day number pairing the elapsed-days formulation below with Tishri 1, A.M. 1
# (one less than the -1373427 epoch quoted with the variant that omits the formula's leading day).
HEBREW_EPOCH = -1373428


def is_leap_year(hebrew_year: int) -> bool:
    """A Hebrew year is leap when it inserts Adar I — 7 times per 19-year Metonic cycle."""
    return (7 * hebrew_year + 1) % 19 < 7


def elapsed_days(hebrew_year: int) -> int:
    """Days from the Hebrew epoch to the molad-derived, postponement-adjusted New Year of the year."""
    months_elapsed = (
        235 * ((hebrew_year - 1) // 19)
        + 12 * ((hebrew_year - 1) % 19)
        + (7 * ((hebrew_year - 1) % 19) + 1) // 19
    )
    parts_elapsed = 204 + 793 * (months_elapsed % 1080)
    hours_elapsed = (
        5 + 12 * months_elapsed + 793 * (months_elapsed // 1080) + parts_elapsed // 1080
    )
    day = 1 + 29 * months_elapsed + hours_elapsed // 24
    parts = (hours_elapsed % 24) * 1080 + parts_elapsed % 1080

    # Dechiyot: molad too late in the day, or Tuesday/Monday molads that would make an
    # inadmissible year length for this (or the previous) year's leap status.
    if (
        parts >= 19440
        or (day % 7 == 2 and parts >= 9924 and not is_leap_year(hebrew_year))
        or (day % 7 == 1 and parts >= 16789 and is_leap_year(hebrew_year - 1))
    ):
        day += 1

    # Lo ADU Rosh: New Year may not fall on Sunday, Wednesday, or Friday.
    if day % 7 in (0, 3, 5):
        day += 1

    return day


def new_year(hebrew_year: int) -> int:
    """Rata Die day of Tishri 1 of the year."""
    return HEBREW_EPOCH + elapsed_days(hebrew_year)


def days_in_year(hebrew_year: int) -> int:
    return new_year(hebrew_year + 1) - new_year(hebrew_year)


def long_heshvan(hebrew_year: int) -> bool:
    return days_in_year(hebrew_year) % 10 == 5


def short_kislev(hebrew_year: int) -> bool:
    return days_in_year(hebrew_year) % 10 == 3


def month_lengths(hebrew_year: int) -> list[tuple[str, int]]:
    """Month names and lengths in civil order from Tishri; Adar names the final Adar."""
    months = [
        ("Tishri", 30),
        ("Heshvan", 30 if long_heshvan(hebrew_year) else 29),
        ("Kislev", 29 if short_kislev(hebrew_year) else 30),
        ("Tevet", 29),
        ("Shevat", 30),
    ]
    if is_leap_year(hebrew_year):
        months.append(("AdarI", 30))
    months += [
        ("Adar", 29),
        ("Nisan", 30),
        ("Iyar", 29),
        ("Sivan", 30),
        ("Tammuz", 29),
        ("Av", 30),
        ("Elul", 29),
    ]
    return months


def to_gregorian(hebrew_year: int, month: str, day: int) -> datetime.date:
    """Gregorian date of a Hebrew civil date; ``Adar`` addresses the final Adar of the year."""
    rd = new_year(hebrew_year)
    for name, length in month_lengths(hebrew_year):
        if name == month:
            return datetime.date.fromordinal(rd + day - 1)
        rd += length
    raise ValueError(f"unknown month {month!r} in {hebrew_year}")


def anchor_in_gregorian_year(gregorian_year: int, month: str, day: int) -> datetime.date:
    """The single occurrence of a Hebrew date landing in the requested Gregorian year."""
    hits = [
        date
        for hy in range(gregorian_year + 3759, gregorian_year + 3763)
        if (date := to_gregorian(hy, month, day)).year == gregorian_year
    ]
    if len(hits) != 1:
        raise AssertionError(f"{month} {day} occurs {len(hits)} times in {gregorian_year}")
    return hits[0]


def main() -> None:
    # (observanceId, anchor month, anchor day, offset days) mirroring the global-jewish catalogue:
    # anchors are fixed Hebrew dates; derived observances are exact day offsets from an anchor.
    observances = [
        ("tu-bishvat", "Shevat", 15, 0),
        ("purim", "Adar", 14, 0),
        ("passover", "Nisan", 15, 0),
        ("lag-baomer", "Nisan", 15, 33),
        ("shavuot", "Nisan", 15, 50),
        ("tisha-bav", "Av", 9, 0),
        ("rosh-hashanah", "Tishri", 1, 0),
        ("yom-kippur", "Tishri", 1, 9),
        ("sukkot", "Tishri", 1, 14),
        ("shemini-atzeret", "Tishri", 1, 21),
        ("simchat-torah-israel", "Tishri", 1, 21),
        ("simchat-torah-diaspora", "Tishri", 1, 22),
        ("hanukkah", "Kislev", 25, 0),
    ]

    print("# Hebrew-calendar observance vectors for the bundled global-jewish catalogue.")
    print(f"# Range: Gregorian years {FIRST_GREGORIAN_YEAR}-{LAST_GREGORIAN_YEAR}, one occurrence per observance per year.")
    print("# Source: independent Dershowitz-Reingold Hebrew-calendar arithmetic implemented in")
    print("#   tools/generate-hebrew-observance-vectors.py (no dependency on the code under test).")
    print("# Cross-verified at generation time against the Hebcal-verified 2023-2026 rows in")
    print("#   GlobalJewishCatalogueKnownAnswerTests and against the System.Globalization.HebrewCalendar")
    print("#   projection across the full range; see NotableDateCatalogueVerification.md.")
    print("# Note: dates are the calendar-arithmetic dates the catalogue defines (e.g. Tisha B'Av is 9 Av")
    print("#   itself; the Shabbat fast deferral is an observance practice the catalogue does not encode).")
    print("# Regenerate: python3 tools/generate-hebrew-observance-vectors.py > <this file>")
    print("# Columns: gregorianYear,observanceId,date")
    for year in range(FIRST_GREGORIAN_YEAR, LAST_GREGORIAN_YEAR + 1):
        for observance_id, month, day, offset in observances:
            date = anchor_in_gregorian_year(year, month, day) + datetime.timedelta(days=offset)
            print(f"{year},{observance_id},{date.isoformat()}")


if __name__ == "__main__":
    main()
