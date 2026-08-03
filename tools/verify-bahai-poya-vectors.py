#!/usr/bin/env python3
"""Independently verifies the Baha'i 172-221 B.E. and Sri Lanka Poya corpus tables.

Both tables were transcribed from official publications (the Universal House of Justice
50-year Badi table via a national Baha'i institutional reproduction; the Sri Lankan
government holiday gazettes), so this script does not re-derive them - it braces the
transcription against structure and independent astronomy:

Baha'i (corpus/bahai/uhj-holy-days-172-221-be.csv):
  - 50 contiguous B.E. years x 11 holy-day ids.
  - Naw-Ruz always 20 or 21 March (the Tehran-equinox rule's only possible outcomes
    within this era).
  - The eight fixed holy days sit at the uniform Badi offsets from Naw-Ruz
    (+31/+39/+42/+64/+69/+111/+250/+252 days).
  - The Twin Birthdays are consecutive days within the 14 October - 14 November window,
    and the Birth of the Bab falls within one day of (the 8th new moon after Naw-Ruz,
    reckoned at Tehran +3:30) plus one day - the UHJ rule ("the first and second days
    following the occurrence of the eighth new moon after Naw-Ruz"). The one-day slack
    absorbs the sunset-epoch day boundary this script does not model.

Sri Lanka Poya (corpus/sri-lanka/poya-gazette-2023-2027.csv):
  - Consecutive Poya days are 29 or 30 days apart (lunar-month spacing).
  - Every Poya date falls within 1.5 days of the geocentric full moon evaluated at
    Sri Lanka standard time (+5:30).

The lunar-phase series is the Meeus chapter 49 implementation shared with
tools/verify-islamic-observance-vectors.py / verify-hindu-observance-vectors.py,
independent of both the transcription source and the library.

Usage:
    python3 tools/verify-bahai-poya-vectors.py \
        corpus/bahai/uhj-holy-days-172-221-be.csv \
        corpus/sri-lanka/poya-gazette-2023-2027.csv
"""

from __future__ import annotations

import datetime
import math
import sys

J2000_JULIAN_DAY = 2451545.0
J2000_EPOCH = datetime.datetime(2000, 1, 1, 12, 0, 0)
SYNODIC_MONTH = 29.530588861
TEHRAN_UTC_OFFSET_HOURS = 3.5
SRI_LANKA_UTC_OFFSET_HOURS = 5.5

# Uniform day offsets of the eight fixed holy days from Naw-Ruz in the Badi calendar.
FIXED_OFFSETS = {
    "first-day-of-ridvan": 31,
    "ninth-day-of-ridvan": 39,
    "twelfth-day-of-ridvan": 42,
    "declaration-of-the-bab": 64,
    "ascension-of-bahaullah": 69,
    "martyrdom-of-the-bab": 111,
    "day-of-the-covenant": 250,
    "ascension-of-abdul-baha": 252,
}


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

    correction = ((-0.40614 if full_moon else -0.40720) * math.sin(mp)
                  + (0.17302 if full_moon else 0.17241) * e * math.sin(m)
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


def phase_instant_utc(k: float, full_moon: bool) -> datetime.datetime:
    """Universal-Time instant of the phase at lunation index k (Delta-T ignored: ~1-2 min in this era)."""
    return J2000_EPOCH + datetime.timedelta(days=phase_jde(k, full_moon) - J2000_JULIAN_DAY)


def eighth_new_moon_after(naw_ruz: datetime.date) -> datetime.datetime:
    """Universal-Time instant of the eighth new moon after Naw-Ruz in Badi reckoning.

    The UHJ table's counting convention, pinned by its own boundary years, is "after the
    day of Naw-Ruz": a new moon during the Badi day of Naw-Ruz itself does not count
    (191 B.E. / 2034, new moon midday 20 March Tehran), while one just after that day
    ends at sunset does (180 B.E. / 2023, new moon 21 March 20:53 Tehran). The threshold
    is therefore approximated as 18:00 Tehran on the civil Naw-Ruz date; March sunset in
    Tehran is within minutes of it, and no new moon in the table's era falls inside that
    sliver.
    """
    threshold_utc = (datetime.datetime.combine(naw_ruz, datetime.time(18))
                     - datetime.timedelta(hours=TEHRAN_UTC_OFFSET_HOURS))
    approx_year = naw_ruz.year + (naw_ruz.timetuple().tm_yday - 1) / 365.25
    k = round((approx_year - 2000.0) * 12.3685) - 2

    while phase_instant_utc(k, full_moon=False) <= threshold_utc:
        k += 1

    return phase_instant_utc(k + 7, full_moon=False)


def nearest_full_moon_distance_days(date: datetime.date, utc_offset_hours: float) -> float:
    """Distance in days from local noon on the date to the nearest full moon."""
    approx_year = date.year + (date.timetuple().tm_yday - 1) / 365.25
    k0 = math.floor((approx_year - 2000.0) * 12.3685) + 0.5
    local_noon_utc = datetime.datetime(date.year, date.month, date.day, 12) - datetime.timedelta(hours=utc_offset_hours)

    return min(
        abs((local_noon_utc - phase_instant_utc(k, full_moon=True)).total_seconds()) / 86400.0
        for k in (k0 - 1, k0, k0 + 1))


def read_rows(path: str) -> list[list[str]]:
    """Reads the data rows of a corpus CSV, skipping comment lines and the header row."""
    rows = []
    with open(path, encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            rows.append(line.split(","))

    return rows[1:]


def verify_bahai(path: str) -> None:
    years: dict[int, dict[str, datetime.date]] = {}
    for be_year, _, holy_day_id, iso_date in read_rows(path):
        years.setdefault(int(be_year), {})[holy_day_id] = datetime.date.fromisoformat(iso_date)

    assert sorted(years) == list(range(172, 222)), f"B.E. years not contiguous 172-221: {sorted(years)[:3]}..."
    assert all(len(v) == 11 for v in years.values()), "a year does not carry exactly 11 holy-day ids"

    max_twin_gap = 0.0
    for be_year, days in sorted(years.items()):
        naw_ruz = days["naw-ruz"]
        assert naw_ruz.month == 3 and naw_ruz.day in (20, 21), f"{be_year}: Naw-Ruz {naw_ruz} outside 20/21 Mar"

        for holy_day_id, offset in FIXED_OFFSETS.items():
            actual = (days[holy_day_id] - naw_ruz).days
            assert actual == offset, f"{be_year}: {holy_day_id} at +{actual}d from Naw-Ruz, expected +{offset}d"

        birth_bab, birth_bahaullah = days["birth-of-the-bab"], days["birth-of-bahaullah"]
        assert (birth_bahaullah - birth_bab).days == 1, f"{be_year}: Twin Birthdays not consecutive"
        gregorian_year = naw_ruz.year
        window = (datetime.date(gregorian_year, 10, 14), datetime.date(gregorian_year, 11, 14))
        assert window[0] <= birth_bab <= window[1], f"{be_year}: Birth of the Bab {birth_bab} outside Oct/Nov window"

        # UHJ rule: the Twin Birthdays are the first and second days following the eighth
        # new moon after Naw-Ruz (Tehran reckoning). One day of slack absorbs the
        # sunset-epoch boundary.
        new_moon_tehran = eighth_new_moon_after(naw_ruz) + datetime.timedelta(hours=TEHRAN_UTC_OFFSET_HOURS)
        expected = new_moon_tehran.date() + datetime.timedelta(days=1)
        gap = abs((birth_bab - expected).days)
        max_twin_gap = max(max_twin_gap, gap)
        assert gap <= 1, (f"{be_year}: Birth of the Bab {birth_bab} is {gap}d from day after "
                          f"8th new moon ({new_moon_tehran:%Y-%m-%d %H:%M} Tehran)")

    print(f"OK: Baha'i {sum(len(v) for v in years.values())} rows - structure exact; "
          f"Twin Birthdays within {max_twin_gap:.0f}d of the 8th-new-moon rule across all 50 years")


def verify_poya(path: str) -> None:
    dates = sorted(datetime.date.fromisoformat(row[2]) for row in read_rows(path))

    gaps = [(b - a).days for a, b in zip(dates, dates[1:])]
    assert all(gap in (29, 30) for gap in gaps), f"non-lunar Poya spacing found: {sorted(set(gaps))}"

    worst = 0.0
    for date in dates:
        distance = nearest_full_moon_distance_days(date, SRI_LANKA_UTC_OFFSET_HOURS)
        worst = max(worst, distance)
        assert distance <= 1.5, f"Poya {date} is {distance:.2f}d from the nearest full moon"

    print(f"OK: Poya {len(dates)} rows - spacing all 29/30d; worst full-moon distance {worst:.2f}d")


def main() -> None:
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        raise SystemExit(2)

    verify_bahai(sys.argv[1])
    verify_poya(sys.argv[2])


if __name__ == "__main__":
    main()
