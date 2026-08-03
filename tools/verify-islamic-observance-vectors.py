#!/usr/bin/env python3
"""Astronomically cross-checks the Umm al-Qura observance vector table.

The Umm al-Qura calendar's month table is calculated data (KACST) rather than arithmetic, so an
independent full-table oracle is not derivable offline. What *is* independently checkable is the
astronomy underneath it: every Hijri month must begin within a few days after the geocentric
lunar conjunction (new moon). This script reimplements the Meeus chapter 49 lunar-phase series
in Python (independent of both the vector generator and the library), reconstructs the Hijri
month start underlying every vector row from the observance's fixed (month, day) definition, and
asserts the start falls 0-3 days after the nearest preceding conjunction. A violation would
expose a gross table, projection, or offset error in the generated vectors.

Usage:
    python3 tools/verify-islamic-observance-vectors.py \
        Bodu.Globalization.Calendar/test/Globalization.Calendar/Fixtures/Vectors/UmmAlQuraObservances-1990-2039.csv
"""

from __future__ import annotations

import datetime
import math
import sys

J2000_JULIAN_DAY = 2451545.0
J2000_EPOCH = datetime.datetime(2000, 1, 1, 12, 0, 0)
SYNODIC_MONTH = 29.530588861

# Hijri (month, day) anchor and day offset per observance, mirroring global-islamic-umm-al-qura.xml.
OBSERVANCES = {
    "islamic-new-year": (1, 1, 0),
    "day-of-ashura": (1, 10, 0),
    "mawlid-al-nabi": (3, 12, 0),
    "isra-and-miraj": (7, 27, 0),
    "ramadan": (9, 1, 0),
    "laylat-al-qadr": (9, 1, 26),
    "eid-al-fitr": (10, 1, 0),
    "hajj": (12, 8, 0),
    "day-of-arafah": (12, 9, 0),
    "eid-al-adha": (12, 10, 0),
}

# Days from the start of Hijri month 1 to the start of each month, at the 29.53-day mean lunation;
# used only to locate the month start from a mid-month day, so mean precision suffices.
def month_start_estimate(date: datetime.date, day_of_month: int) -> datetime.date:
    return date - datetime.timedelta(days=day_of_month - 1)


def new_moon_jde(k: float) -> float:
    """Julian Ephemeris Day of the new moon at lunation index k (Meeus ch. 49, principal terms)."""
    t = k / 1236.85
    jde = (2451550.09766 + SYNODIC_MONTH * k
           + 0.00015437 * t**2 - 0.000000150 * t**3 + 0.00000000073 * t**4)

    e = 1.0 - 0.002516 * t - 0.0000074 * t**2
    m = math.radians(2.5534 + 29.10535670 * k - 0.0000014 * t**2 - 0.00000011 * t**3)
    mp = math.radians(201.5643 + 385.81693528 * k + 0.0107582 * t**2
                      + 0.00001238 * t**3 - 0.000000058 * t**4)
    f = math.radians(160.7108 + 390.67050284 * k - 0.0016118 * t**2
                     - 0.00000227 * t**3 + 0.000000011 * t**4)
    omega = math.radians(124.7746 - 1.56375588 * k + 0.0020672 * t**2 + 0.00000215 * t**3)

    correction = (-0.40720 * math.sin(mp)
                  + 0.17241 * e * math.sin(m)
                  + 0.01608 * math.sin(2 * mp)
                  + 0.01039 * math.sin(2 * f)
                  + 0.00739 * e * math.sin(mp - m)
                  - 0.00514 * e * math.sin(mp + m)
                  + 0.00208 * e * e * math.sin(2 * m)
                  - 0.00111 * math.sin(mp - 2 * f)
                  - 0.00057 * math.sin(mp + 2 * f)
                  + 0.00056 * e * math.sin(2 * mp + m)
                  - 0.00042 * math.sin(3 * mp)
                  + 0.00042 * e * math.sin(m + 2 * f)
                  + 0.00038 * e * math.sin(m - 2 * f)
                  - 0.00024 * e * math.sin(2 * mp - m)
                  - 0.00017 * math.sin(omega)
                  - 0.00007 * math.sin(mp + 2 * m)
                  + 0.00004 * math.sin(2 * mp - 2 * f)
                  + 0.00004 * math.sin(3 * m)
                  + 0.00003 * math.sin(mp + m - 2 * f)
                  + 0.00003 * math.sin(2 * mp + 2 * f)
                  - 0.00003 * math.sin(mp + m + 2 * f)
                  + 0.00003 * math.sin(mp - m + 2 * f)
                  - 0.00002 * math.sin(mp - m - 2 * f)
                  - 0.00002 * math.sin(3 * mp + m)
                  + 0.00002 * math.sin(4 * mp))

    additional = (0.000325 * math.sin(math.radians(299.77 + 0.107408 * k - 0.009173 * t**2))
                  + 0.000165 * math.sin(math.radians(251.88 + 0.016321 * k))
                  + 0.000164 * math.sin(math.radians(251.83 + 26.651886 * k))
                  + 0.000126 * math.sin(math.radians(349.42 + 36.412478 * k))
                  + 0.000110 * math.sin(math.radians(84.66 + 18.206239 * k))
                  + 0.000062 * math.sin(math.radians(141.74 + 53.303771 * k))
                  + 0.000060 * math.sin(math.radians(207.14 + 2.453732 * k))
                  + 0.000056 * math.sin(math.radians(154.84 + 7.306860 * k))
                  + 0.000047 * math.sin(math.radians(34.52 + 27.261239 * k))
                  + 0.000042 * math.sin(math.radians(207.19 + 0.121824 * k))
                  + 0.000040 * math.sin(math.radians(291.34 + 1.844379 * k))
                  + 0.000037 * math.sin(math.radians(161.72 + 24.198154 * k))
                  + 0.000035 * math.sin(math.radians(239.56 + 25.513099 * k))
                  + 0.000023 * math.sin(math.radians(331.55 + 3.592518 * k)))

    return jde + correction + additional


def conjunction_before(date: datetime.date) -> datetime.date:
    """Universal-Time date of the last geocentric new moon on or before the supplied date."""
    approx_year = date.year + (date.timetuple().tm_yday - 1) / 365.25
    k = round((approx_year - 2000.0) * 12.3685)

    for _ in range(4):
        jde = new_moon_jde(k)
        conj = (J2000_EPOCH + datetime.timedelta(days=jde - J2000_JULIAN_DAY)).date()
        if conj <= date:
            # Delta-T (~1-2 minutes in this era) is far below day granularity; skip the reduction.
            return conj
        k -= 1

    raise AssertionError(f"no conjunction found before {date}")


def main() -> None:
    if len(sys.argv) != 2:
        print(__doc__, file=sys.stderr)
        raise SystemExit(2)

    checked = 0
    distribution: dict[int, int] = {}

    with open(sys.argv[1], encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line or line.startswith("#"):
                continue

            _, observance_id, iso_date = line.split(",")
            month, day, offset = OBSERVANCES[observance_id]
            date = datetime.date.fromisoformat(iso_date)
            month_start = month_start_estimate(date - datetime.timedelta(days=offset), day)

            gap = (month_start - conjunction_before(month_start)).days
            if not 0 <= gap <= 3:
                raise AssertionError(
                    f"{line}: month start {month_start} is {gap} days after conjunction "
                    f"{conjunction_before(month_start)} (expected 0-3)")

            checked += 1
            distribution[gap] = distribution.get(gap, 0) + 1

    summary = ", ".join(f"+{gap}d: {count}" for gap, count in sorted(distribution.items()))
    print(f"OK: {checked} rows verified; month-start gap after conjunction - {summary}")


if __name__ == "__main__":
    main()
