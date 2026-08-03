#!/usr/bin/env python3
"""Verifies the IMD Rashtriya Panchang 1947 S.E. principal-festivals transcription.

The table (corpus/hindu/data/normalized/imd-rp-1947se-principal-festivals.csv) was
transcribed from page renders of the official English edition (the born-digital PDF
draws its text as vector outlines, so there is no text layer to extract), and this
script braces the transcription against the redundancy the printed table carries:

  - 102 contiguous rows, numbered 1-102.
  - gregorianDate is exactly the printed day/month composed with the section-context
    year (2025 for rows 1-68, 2026 for rows 69-102).
  - Every printed weekday matches the derived Gregorian date, except the one documented
    printer's erratum: row 52 prints '30 Oct.' with weekday (Tue) between the 29 Sep.
    and 01 Oct. rows - the intended date is 30 Sep. 2025, a Tuesday. The row carries a
    notes annotation and is transcribed verbatim.
  - With that single correction applied, the 102 dates are strictly increasing from
    2025-03-22 (01 Chaitra 1947 S.E.) through 2026-04-20.
  - The Saka era year is 1947 through row 89 and 1948 from row 90 (01 Chaitra); the
    Kali era year steps 5125 -> 5126 at row 10 (01 Vaisakha) and -> 5127 at row 98.

Usage:
    python3 tools/verify-imd-festival-vectors.py

Exits non-zero on any violation.
"""

import csv
import datetime as dt
import sys
from pathlib import Path

CSV_PATH = Path(__file__).resolve().parent.parent / "corpus/hindu/data/normalized/imd-rp-1947se-principal-festivals.csv"

MONTHS = {"Jan": 1, "Feb": 2, "Mar": 3, "Apr": 4, "May": 5, "June": 6, "July": 7,
          "Aug": 8, "Sep": 9, "Oct": 10, "Nov": 11, "Dec": 12}
WEEKDAYS = {"Mon": 0, "Tue": 1, "Wed": 2, "Thu": 3, "Fri": 4, "Sat": 5, "Sun": 6}
ERRATUM_ROW = 52


def main() -> int:
    with CSV_PATH.open(encoding="utf-8") as f:
        rows = list(csv.DictReader(line for line in f if not line.startswith("#")))

    failures = []

    if [int(r["row"]) for r in rows] != list(range(1, 103)):
        failures.append(f"expected rows 1-102 in order, found {len(rows)} rows")

    corrected = []
    for r in rows:
        n = int(r["row"])
        date = dt.date.fromisoformat(r["gregorianDate"])

        day_s, mon_s = r["printedDate"].replace(".", "").split()
        expected_year = 2025 if n <= 68 else 2026
        if date != dt.date(expected_year, MONTHS[mon_s], int(day_s)):
            failures.append(f"row {n}: gregorianDate {date} does not recompose printedDate '{r['printedDate']}' with year {expected_year}")

        weekday_ok = date.weekday() == WEEKDAYS[r["printedWeekday"]]
        if n == ERRATUM_ROW:
            if weekday_ok or not r["notes"]:
                failures.append(f"row {n}: expected the documented printer's-erratum mismatch with a notes annotation")
            date = dt.date(2025, 9, 30)
        elif not weekday_ok:
            failures.append(f"row {n}: {date} is a {date.strftime('%a')}, printed weekday is {r['printedWeekday']}")

        expected_saka = 1947 if n <= 89 else 1948
        expected_kali = 5125 if n <= 9 else 5126 if n <= 97 else 5127
        if int(r["sakaYear"]) != expected_saka or int(r["kaliYear"]) != expected_kali:
            failures.append(f"row {n}: era years {r['sakaYear']}/{r['kaliYear']}, expected {expected_saka}/{expected_kali}")

        corrected.append((n, date))

    for (na, a), (nb, b) in zip(corrected, corrected[1:]):
        if a >= b:
            failures.append(f"rows {na}->{nb}: dates not strictly increasing ({a} >= {b})")

    if corrected and (corrected[0][1] != dt.date(2025, 3, 22) or corrected[-1][1] != dt.date(2026, 4, 20)):
        failures.append("span: expected 2025-03-22 through 2026-04-20")

    for failure in failures:
        print(f"FAIL {failure}")
    print(f"{len(rows)} rows checked, {len(failures)} violations "
          f"(row {ERRATUM_ROW} printer's erratum documented and expected)")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
