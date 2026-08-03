#!/usr/bin/env python3
"""Verifies the IMD Rashtriya Panchang principal-festivals transcriptions.

Covers the five English-edition tables under corpus/hindu/data/normalized/
(imd-rp-<saka>se-principal-festivals.csv, 1944-1948 S.E. = 2022-2027 A.D.). The 1947
S.E. edition was transcribed from page renders (its born-digital PDF draws text as
vector outlines with no text layer); the other four were machine-extracted from their
PDFs' text layers. This script braces every table against the redundancy the printed
tables carry:

  - rows are numbered contiguously from 1 and the expected row count matches;
  - gregorianDate agrees with the printed day/month, every printed weekday matches the
    derived date, and the dates are strictly increasing across the expected span;
  - the S.E. and K.E. era-year columns advance exactly where their month columns roll
    over (S.E. years begin at Chaitra, K.E. years at Vaisakha);
  - the documented printer's errata - and only those - deviate, each carrying a notes
    annotation: 1947 row 52 prints '30 Oct.' for 30 Sep. 2025 (confirmed English-edition-
    only against the Bengali edition), and 1948 row 82 prints the Kali month 'Chaitra'
    for 'Phalguna'.

Usage:
    python3 tools/verify-imd-festival-vectors.py

Exits non-zero on any violation.
"""

import csv
import datetime as dt
import sys
from pathlib import Path

NORMALIZED = Path(__file__).resolve().parent.parent / "corpus/hindu/data/normalized"

MONTHS = {"Jan": 1, "Feb": 2, "Mar": 3, "Apr": 4, "May": 5, "Jun": 6, "June": 6,
          "Jul": 7, "July": 7, "Aug": 8, "Sep": 9, "Sept": 9, "Oct": 10, "Nov": 11, "Dec": 12}
WEEKDAYS = {"Mon": 0, "Tue": 1, "Wed": 2, "Thu": 3, "Fri": 4, "Sat": 5, "Sun": 6}

CANON = {"Vaishakha": "Vaisakha", "Asadha": "Ashadha", "Aswina": "Asvina",
         "Kartik": "Kartika", "Jyaistha": "Jyaishtha", "Agrahayan": "Agrahayana",
         "Pousha": "Pausha"}
ORDER = ["Chaitra", "Vaisakha", "Jyaishtha", "Ashadha", "Sravana", "Bhadra",
         "Asvina", "Kartika", "Agrahayana", "Pausha", "Magha", "Phalguna"]

# fileKey -> (rowCount, firstDate, lastDate, {row: correctedGregorianDate}, {row: correctedKaliMonth})
TABLES = {
    "1944": (93, dt.date(2022, 3, 22), dt.date(2023, 4, 19), {}, {}),
    "1945": (102, dt.date(2023, 3, 22), dt.date(2024, 4, 19), {}, {}),
    "1946": (108, dt.date(2024, 3, 21), dt.date(2025, 4, 20), {}, {}),
    "1947": (102, dt.date(2025, 3, 22), dt.date(2026, 4, 20), {52: dt.date(2025, 9, 30)}, {}),
    "1948": (96, dt.date(2026, 3, 22), dt.date(2027, 4, 19), {}, {82: "Phalguna"}),
}


def month_index(name, base):
    i = ORDER.index(CANON.get(name, name))
    # rotate so the era's first month sits at 0 (Chaitra for S.E., Vaisakha for K.E.)
    return (i - ORDER.index(base)) % 12


def verify(key):
    count, first, last, greg_errata, kali_errata = TABLES[key]
    path = NORMALIZED / f"imd-rp-{key}se-principal-festivals.csv"
    failures = []

    with path.open(encoding="utf-8") as f:
        rows = list(csv.DictReader(line for line in f if not line.startswith("#")))

    if [int(r["row"]) for r in rows] != list(range(1, count + 1)):
        failures.append(f"expected rows 1-{count} in order, found {len(rows)} rows")

    corrected = []
    for r in rows:
        n = int(r["row"])
        date = dt.date.fromisoformat(r["gregorianDate"])

        day_s, mon_s = r["printedDate"].replace(".", "").split()
        if (date.day, date.month) != (int(day_s), MONTHS[mon_s]):
            failures.append(f"row {n}: gregorianDate {date} does not agree with printedDate '{r['printedDate']}'")

        weekday_ok = date.weekday() == WEEKDAYS[r["printedWeekday"]]
        if n in greg_errata:
            if weekday_ok or not r["notes"]:
                failures.append(f"row {n}: expected the documented printer's-erratum mismatch with a notes annotation")
            date = greg_errata[n]
        elif not weekday_ok:
            failures.append(f"row {n}: {date} is a {date.strftime('%a')}, printed weekday is {r['printedWeekday']}")

        if n in kali_errata and not r["notes"]:
            failures.append(f"row {n}: expected a notes annotation for the documented Kali-month erratum")

        corrected.append((n, date, r))

    for (na, a, ra), (nb, b, rb) in zip(corrected, corrected[1:]):
        if a >= b:
            failures.append(f"rows {na}->{nb}: dates not strictly increasing ({a} >= {b})")
        for col, year_key, base in (("sakaDate", "sakaYear", "Chaitra"), ("kaliDate", "kaliYear", "Vaisakha")):
            month_a = kali_errata.get(na, ra[col].split()[-1]) if col == "kaliDate" else ra[col].split()[-1]
            month_b = kali_errata.get(nb, rb[col].split()[-1]) if col == "kaliDate" else rb[col].split()[-1]
            wraps = month_index(month_b, base) < month_index(month_a, base)
            bump = int(rb[year_key]) - int(ra[year_key])
            if bump != (1 if wraps else 0):
                failures.append(f"rows {na}->{nb}: {col} {'wraps' if wraps else 'no wrap'} but {year_key} {ra[year_key]}->{rb[year_key]}")

    if corrected and (corrected[0][1] != first or corrected[-1][1] != last):
        failures.append(f"span: expected {first} through {last}, found {corrected[0][1]} through {corrected[-1][1]}")

    return len(rows), failures


def main() -> int:
    total_failures = 0
    for key in sorted(TABLES):
        n, failures = verify(key)
        for failure in failures:
            print(f"FAIL {key} S.E.: {failure}")
        total_failures += len(failures)
        print(f"{key} S.E.: {n} rows checked, {len(failures)} violations")
    print(f"total: {total_failures} violations "
          "(documented errata: 1947 row 52 Gregorian month, 1948 row 82 Kali month)")
    return 1 if total_failures else 0


if __name__ == "__main__":
    sys.exit(main())
