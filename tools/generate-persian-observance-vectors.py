#!/usr/bin/env python3
"""Generates the Persian-calendar observance regression vectors for the global-persian catalogue.

The Solar Hijri year begins on the civil day of the true vernal equinox at the Iranian standard
meridian (52.5 deg E, UTC+03:30) when the equinox instant falls before apparent solar noon there,
and on the following day otherwise. The chain below - the Meeus chapter 27 equinox series in
dynamical time, the Espenak-Meeus delta-T reduction to Universal Time, and the Spencer harmonic
equation of time for apparent noon - is implemented here independently in Python, so the emitted
vectors do not derive from the code under test. The catalogue's observances are exact day offsets
from Nowruz: Sizdah Bedar is Farvardin 13 (+12 days) and Yalda Night is the last day of Azar
(+275 days; months 1-6 have 31 days and months 7-9 have 30, so the offset is fixed for every
year regardless of the year's leap status).

Usage:
    python3 tools/generate-persian-observance-vectors.py > \
        Bodu.Globalization.Calendar/test/Globalization.Calendar/Fixtures/Vectors/PersianObservances-1990-2039.csv
"""

from __future__ import annotations

import datetime
import math

FIRST_GREGORIAN_YEAR = 1990
LAST_GREGORIAN_YEAR = 2039

J2000_JULIAN_DAY = 2451545.0
J2000_EPOCH = datetime.datetime(2000, 1, 1, 12, 0, 0)
TEHRAN_MERIDIAN_UTC_OFFSET_HOURS = 3.5

# Periodic correction terms of Meeus table 27.c: (amplitude, phase in degrees, rate in deg/century).
CORRECTION_TERMS = [
    (485, 324.96, 1934.136), (203, 337.23, 32964.467), (199, 342.08, 20.186), (182, 27.85, 445267.112),
    (156, 73.14, 45036.886), (136, 171.52, 22518.443), (77, 222.54, 65928.934), (74, 296.72, 3034.906),
    (70, 243.58, 9037.513), (58, 119.81, 33718.147), (52, 297.17, 150.678), (50, 21.02, 2281.226),
    (45, 247.54, 29929.562), (44, 325.15, 31555.956), (29, 60.93, 4443.417), (18, 155.12, 67555.328),
    (17, 288.79, 4562.452), (16, 198.04, 62894.029), (14, 199.76, 31557.381), (12, 95.39, 14577.848),
    (10, 69.15, 31556.017), (9, 293.38, 149.438), (9, 242.69, 9325.052), (8, 141.01, 31559.532),
    (7, 283.90, 23.000), (7, 306.92, 10977.079), (6, 233.02, 10447.357), (6, 262.55, 31441.677),
]


def vernal_equinox_jde(year: int) -> float:
    """Julian Ephemeris Day of the March equinox (Meeus ch. 27 mean polynomial plus corrections)."""
    y = (year - 2000) / 1000.0
    jde0 = 2451623.80984 + 365242.37404 * y + 0.05169 * y**2 - 0.00411 * y**3 - 0.00057 * y**4

    t = (jde0 - J2000_JULIAN_DAY) / 36525.0
    w = math.radians(35999.373 * t - 2.47)
    lam = 1.0 + 0.0334 * math.cos(w) + 0.0007 * math.cos(2.0 * w)
    s = sum(a * math.cos(math.radians(p + r * t)) for a, p, r in CORRECTION_TERMS)

    return jde0 + 0.00001 * s / lam


def delta_t_seconds(year: int) -> float:
    """Delta-T estimate (TT minus UT) from the Espenak-Meeus polynomial expressions."""
    if 1986 <= year < 2005:
        t = year - 2000
        return (63.86 + 0.3345 * t - 0.060374 * t**2 + 0.0017275 * t**3
                + 0.000651814 * t**4 + 0.00002373599 * t**5)

    if 2005 <= year <= 2050:
        t = year - 2000
        return 62.92 + 0.32217 * t + 0.005589 * t**2

    u = (year - 1820) / 100.0
    return -20.0 + 32.0 * u**2


def equation_of_time_minutes(date: datetime.date) -> float:
    """Apparent minus mean solar time, in minutes, from the Spencer harmonic fit."""
    days_in_year = 366.0 if date.year % 4 == 0 and (date.year % 100 != 0 or date.year % 400 == 0) else 365.0
    gamma = 2.0 * math.pi * (date.timetuple().tm_yday - 1) / days_in_year

    return 229.18 * (0.000075
                     + 0.001868 * math.cos(gamma)
                     - 0.032077 * math.sin(gamma)
                     - 0.014615 * math.cos(2.0 * gamma)
                     - 0.040849 * math.sin(2.0 * gamma))


def nowruz(year: int) -> datetime.date:
    """Gregorian date of Farvardin 1 whose vernal equinox falls in the supplied Gregorian year."""
    dynamical = J2000_EPOCH + datetime.timedelta(days=vernal_equinox_jde(year) - J2000_JULIAN_DAY)
    universal = dynamical - datetime.timedelta(seconds=delta_t_seconds(year))
    local = universal + datetime.timedelta(hours=TEHRAN_MERIDIAN_UTC_OFFSET_HOURS)

    equinox_day = local.date()
    apparent_noon_minutes = 720.0 - equation_of_time_minutes(equinox_day)
    local_minutes = local.hour * 60.0 + local.minute + local.second / 60.0 + local.microsecond / 60_000_000.0

    return equinox_day if local_minutes < apparent_noon_minutes else equinox_day + datetime.timedelta(days=1)


def main() -> None:
    # (observanceId, day offset from Nowruz) mirroring the global-persian catalogue: Sizdah Bedar is
    # Farvardin 13; Yalda Night is Azar 30, the 276th day of the Solar Hijri year (6 * 31 + 3 * 30).
    observances = [
        ("nowruz", 0),
        ("sizdah-bedar", 12),
        ("yalda-night", 275),
    ]

    print("# Persian-calendar observance vectors for the bundled global-persian catalogue.")
    print(f"# Range: Gregorian years {FIRST_GREGORIAN_YEAR}-{LAST_GREGORIAN_YEAR}, one occurrence per observance per year.")
    print("# Source: independent implementation of the official Solar Hijri new-year rule in")
    print("#   tools/generate-persian-observance-vectors.py - Meeus ch. 27 vernal-equinox series in")
    print("#   dynamical time, Espenak-Meeus delta-T, apparent solar noon at the 52.5E standard meridian")
    print("#   (UTC+03:30) via the Spencer equation of time (no dependency on the code under test).")
    print("# Cross-verified at generation time against the System.Globalization.PersianCalendar")
    print("#   projection across the full range and the hand-pinned 2022-2026 rows in")
    print("#   PersianCalendarKnownAnswerTests; see NotableDateCatalogueVerification.md.")
    print("# Regenerate: python3 tools/generate-persian-observance-vectors.py > <this file>")
    print("# Columns: gregorianYear,observanceId,date")
    for year in range(FIRST_GREGORIAN_YEAR, LAST_GREGORIAN_YEAR + 1):
        farvardin1 = nowruz(year)
        if not (datetime.date(year, 3, 19) <= farvardin1 <= datetime.date(year, 3, 22)):
            raise AssertionError(f"Nowruz {year} = {farvardin1} outside the plausible March window")
        for observance_id, offset in observances:
            date = farvardin1 + datetime.timedelta(days=offset)
            print(f"{year},{observance_id},{date.isoformat()}")


if __name__ == "__main__":
    main()
