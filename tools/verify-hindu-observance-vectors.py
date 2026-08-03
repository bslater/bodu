#!/usr/bin/env python3
"""Astronomically cross-checks the Hindu observance vector table.

The vector rows are engine-pinned (the in-repo HinduLunarCalculator is the model), so this
script provides the independent structural brace: a standalone Python implementation of the
Meeus chapter 49 new-moon series checks that every lunar-festival row sits within 1.5 days of
its defining tithi position (the preceding geocentric conjunction plus tithi x 29.53/30 days),
and that every row falls inside the festival's expected Gregorian season window. Together the
two bounds catch a wrong-lunation error (~30 days, fails the window) and a wrong-tithi error
(fails the proximity bound). Fixed solar rows must be exactly 14 January.

Usage:
    python3 tools/verify-hindu-observance-vectors.py \
        Bodu.Globalization.Calendar/test/Globalization.Calendar/Fixtures/Vectors/HinduObservances-1990-2039.csv
"""

from __future__ import annotations

import datetime
import math
import sys

J2000_JULIAN_DAY = 2451545.0
J2000_EPOCH = datetime.datetime(2000, 1, 1, 12, 0, 0)
SYNODIC_MONTH = 29.530588861
TITHI_DAYS = SYNODIC_MONTH / 30.0
PROXIMITY_DAYS = 1.5

# Festival tithi offset from the amanta month's new moon (shukla tithi T = T - 1; krishna tithi
# T = 14 + T; None = anchored to the actual full moon) and the expected season window as
# (fromMonth, fromDay, toMonth, toDay), mirroring the published festival definitions the
# catalogue models.
OBSERVANCES = {
    "vasant-panchami": (4, (1, 10, 2, 20)),      # Magha shukla 5.
    "saraswati-puja": (4, (1, 10, 2, 20)),       # Shares Vasant Panchami.
    "maha-shivaratri": (28, (1, 30, 3, 15)),     # Magha krishna 14.
    "holi": (None, (2, 20, 3, 31)),              # Phalguna purnima (actual full moon).
    "ram-navami": (8, (3, 15, 4, 25)),           # Chaitra shukla 9.
    "raksha-bandhan": (None, (7, 20, 8, 31)),    # Shravana purnima (actual full moon).
    "janmashtami": (22, (8, 1, 9, 15)),          # Shravana krishna 8.
    "ganesh-chaturthi": (3, (8, 15, 9, 25)),     # Bhadrapada shukla 4.
    "navaratri": (0, (9, 15, 10, 25)),           # Ashvin shukla 1.
    "dussehra": (9, (9, 25, 11, 5)),             # Ashvin shukla 10.
    "karva-chauth": (18, (10, 1, 11, 15)),      # Ashvin krishna 4.
    "diwali": (0, (10, 15, 11, 20)),             # Kartik amavasya-adjacent new moon.
}

FIXED_SOLAR = {"makar-sankranti": (1, 14), "pongal": (1, 14)}


def phase_jde(k: float, full_moon: bool) -> float:
    """Julian Ephemeris Day of the new or full moon at lunation index k (Meeus ch. 49, principal terms)."""
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

    first = -0.40614 if full_moon else -0.40720
    second = 0.17302 if full_moon else 0.17241

    correction = (first * math.sin(mp)
                  + second * e * math.sin(m)
                  + (0.01614 if full_moon else 0.01608) * math.sin(2 * mp)
                  + (0.01043 if full_moon else 0.01039) * math.sin(2 * f)
                  + (0.00734 if full_moon else 0.00739) * e * math.sin(mp - m)
                  - (0.00515 if full_moon else 0.00514) * e * math.sin(mp + m)
                  + (0.00209 if full_moon else 0.00208) * e * e * math.sin(2 * m)
                  - 0.00111 * math.sin(mp - 2 * f)
                  - 0.00057 * math.sin(mp + 2 * f)
                  + 0.00056 * e * math.sin(2 * mp + m)
                  - 0.00042 * math.sin(3 * mp)
                  + 0.00042 * e * math.sin(m + 2 * f)
                  + 0.00038 * e * math.sin(m - 2 * f)
                  - 0.00024 * e * math.sin(2 * mp - m)
                  - 0.00017 * math.sin(omega))

    return jde + correction


def new_moon_jde(k: float) -> float:
    """Julian Ephemeris Day of the new moon at lunation index k."""
    return phase_jde(k, full_moon=False)


def nearest_full_moon_distance(date: datetime.date) -> float:
    """Distance in days from the date to the nearest full moon."""
    approx_year = date.year + (date.timetuple().tm_yday - 1) / 365.25
    k0 = math.floor((approx_year - 2000.0) * 12.3685) + 0.5

    best = None
    for k in (k0 - 1, k0, k0 + 1):
        jde = phase_jde(k, full_moon=True)
        instant = J2000_EPOCH + datetime.timedelta(days=jde - J2000_JULIAN_DAY)
        distance = abs((datetime.datetime(date.year, date.month, date.day, 12) - instant).total_seconds()) / 86400.0
        if best is None or distance < best:
            best = distance

    return best


def preceding_new_moon_offset(date: datetime.date, tithi_offset: int) -> float:
    """Distance in days from (preceding-or-near conjunction + tithi offset) to the date."""
    target = date - datetime.timedelta(days=tithi_offset * TITHI_DAYS)
    approx_year = target.year + (target.timetuple().tm_yday - 1) / 365.25
    k0 = round((approx_year - 2000.0) * 12.3685)

    best = None
    for k in (k0 - 1, k0, k0 + 1):
        jde = new_moon_jde(k)
        conj = J2000_EPOCH + datetime.timedelta(days=jde - J2000_JULIAN_DAY)
        expected = conj + datetime.timedelta(days=tithi_offset * TITHI_DAYS)
        distance = abs((datetime.datetime(date.year, date.month, date.day, 12) - expected).total_seconds()) / 86400.0
        if best is None or distance < best:
            best = distance

    return best


def in_window(date: datetime.date, window: tuple[int, int, int, int]) -> bool:
    from_month, from_day, to_month, to_day = window
    return datetime.date(date.year, from_month, from_day) <= date <= datetime.date(date.year, to_month, to_day)


def main() -> None:
    if len(sys.argv) != 2:
        print(__doc__, file=sys.stderr)
        raise SystemExit(2)

    checked = solar = 0
    worst = 0.0

    with open(sys.argv[1], encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line or line.startswith("#"):
                continue

            _, observance_id, iso_date = line.split(",")
            date = datetime.date.fromisoformat(iso_date)

            if observance_id in FIXED_SOLAR:
                month, day = FIXED_SOLAR[observance_id]
                if (date.month, date.day) != (month, day):
                    raise AssertionError(f"{line}: fixed solar festival not on {month:02d}-{day:02d}")
                solar += 1
                continue

            tithi_offset, window = OBSERVANCES[observance_id]
            if not in_window(date, window):
                raise AssertionError(f"{line}: outside the expected seasonal window {window}")

            distance = (nearest_full_moon_distance(date) if tithi_offset is None
                        else preceding_new_moon_offset(date, tithi_offset))
            if distance > PROXIMITY_DAYS:
                raise AssertionError(f"{line}: {distance:.2f} days from its tithi position (limit {PROXIMITY_DAYS})")

            checked += 1
            worst = max(worst, distance)

    print(f"OK: {checked} lunar rows within {PROXIMITY_DAYS} days of their tithi position "
          f"(worst {worst:.2f}) and inside their seasonal windows; {solar} solar rows exactly 14 January")


if __name__ == "__main__":
    main()
