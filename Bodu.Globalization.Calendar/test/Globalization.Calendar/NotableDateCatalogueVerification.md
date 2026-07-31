# Common notable-date catalogue verification

Forensic verification of the new and updated common catalogues in
`Bodu.Globalization.Calendar/src/Globalization.Calendar.Resources/`, comparing the
engine's resolved Gregorian dates against published references.

## Method

Every catalogue was loaded through `CommonNotableDateResources.Resolver` and resolved
for Gregorian years **2023–2027** (a 51-year sweep, 2000–2050, for the structural
contracts). The engine output was compared against published dates from the sources
listed per tradition below. Findings are encoded as data-driven known-answer tests in
`test/Globalization.Calendar/` (one `*KnownAnswerTests` class per tradition).

Two reference classes are used:

- **Deterministic** concepts (Hebrew, tabular Hijri, Umm al-Qura, Persian, Easter
  computus, weekday-near, fixed Gregorian) are pinned to **exact** dates. Their
  reference calendars (BCL `HebrewCalendar`, `HijriCalendar`, `UmAlQuraCalendar`,
  `PersianCalendar`; the Gregorian/Julian paschalion) are themselves the authority.
- **Astronomically approximated** concepts (Hindu lunar, Baha'i equinox, Vesak, the
  East Asian Buddha's birthday, Tibetan Losar, Asalha Puja, Sikh/Jain lunar-derived) are
  pinned to published dates with a **±2-day tolerance**, matching the engine's own
  documented accuracy and the pre-existing `AlgorithmKnownAnswerTests` convention.

## Summary

| Catalogue | Concepts | Verdict |
|---|---|---|
| `christian-protestant` | Transfiguration Sunday, Reformation Sunday/Day, All Saints' Sunday, Aldersgate Day | ✅ exact |
| `christian-anglican` | Baptism of Christ + 21 fixed feasts | ✅ exact (BCP calendar — see notes) |
| `christian-oriental-orthodox` | Pascha cluster + 5 fixed observances | ✅ exact (see Coptic leap-year note) |
| `global-jewish` | 13 Hebrew observances | ✅ exact (Hebcal) |
| `global-islamic` | 10 tabular-Hijri observances | ✅ exact (BCL tabular; ≤2 d from observed) |
| `global-islamic-umm-al-qura` | 10 Umm al-Qura observances | ✅ exact (BCL UAQ) |
| `global-zoroastrian` | 6 Persian-calendar observances | ✅ exact (BCL Persian; see Nowruz note) |
| `global-hindu` | Makar Sankranti, Pongal, Saraswati Puja + lunar set | ✅ within ±2 |
| `global-bahai` | Naw-Ruz + 8 solar holy days | ✅ within ±2; matches official 2025 Badí |
| `global-sikh` | 4 fixed + 3 lunar-derived | ✅ within ±2 (see Guru Arjan Dev note) |
| `global-jain` | lunar-derived set | ✅ within ±2 (**Maun Agiyaras fixed**) |
| `global-buddhist` | Vesak, Buddha's Birthday, fixed + Losar/Asalha/Vassa | ✅ within ±2 (**Losar & Asalha fixed**) |
| `global-all` | aggregate import | ✅ imports resolve end to end |

## Defects found and fixed

Verification uncovered three lunation-selection defects, all from fixed-window lunar
heuristics that selected the wrong lunation (off by ~30 days) in some years. None of the
three keys is referenced by any shipping `Data.<Region>` pack — they live only in these
common catalogues — so the fixes are contained. All three are now corrected and pinned to
published dates by the known-answer tests.

### 1. Tibetan Losar — `global-buddhist`

*Was:* `losar` = *first new moon on or after 20 January* → selected a late-January lunation
a month early when one occurred (2023, 2025).

*Fix:* a new `TibetanLosarCalculator` selects the new moon whose apparent solar longitude
is nearest the Tibetan first-month point (~333.5°), tracking the Phukpa leap-month
divergences. Validated against the published Gyalpo Losar dates 2023–2030 (the engine
returns the astronomical new moon, typically the day before the observed Losar):

| Year | Engine (new) | Published Gyalpo Losar |
|---|---|---|
| 2023 | 20 Feb | 21 Feb |
| 2024 | 9 Feb | 10 Feb |
| 2025 | 28 Feb | 28 Feb |
| 2026 | 17 Feb | 18 Feb |
| 2027 | **8 Mar** | **9 Mar** |
| 2030 | 4 Mar | 5 Mar |

### 2. Asalha Puja & Vassa — `global-buddhist`

*Was:* `asalha-puja` = *first full moon on or after 15 June* → selected a late-June lunation
a month early (2024, 2026, 2027). `vassa` (Asalha + 1) inherited it.

*Fix:* `asalha-puja` now resolves through the engine's leap-month-aware sidereal calculator
as the Asadha full moon (Guru Purnima). Validated against the published dates: 2024 = 21 Jul,
2025 = 10 Jul, **2026 = 29 Jul** (leap year), 2027 = 18 Jul. (2023 follows the Indian Asadha
full moon, 3 Jul; Thailand observed 1 Aug that leap year.)

### 3. Jain Maun Agiyaras — `global-jain`

*Was:* `maun-agiyaras` = *Jain Diwali + 11* (Kartik shukla ekadashi), a lunar month before the
actual festival.

*Fix:* it now resolves as **Margashirsha shukla ekadashi** (the consumer-expected Maun
Ekadashi) through the sidereal calculator: 2024 = 11 Dec, 2025 = 30 Nov, 2026 = 19 Dec,
2027 = 8 Dec. The festival falls in late November–December and can land on 1 January of the
following year, so — like other near-boundary swept dates (e.g. Zartosht No-Diso) — a given
Gregorian year may contain no occurrence (2028 within the validated range).

## Convention notes (defensible authoring choices, not defects)

- **Anglican `matthias-the-apostle` (24 Feb) / `thomas-the-apostle` (21 Dec)** follow the
  Book of Common Prayer (1662). Modern Common Worship transfers them to 14 May / 3 July.
- **Sikh `martyrdom-of-guru-arjan-dev` (fixed 16 Jun)** follows a fixed-civil convention.
  Bikrami community calendars observe it on a year-varying date (≈10–11 Jun in 2024).
- **Zoroastrian `zoroastrian-nowruz` 2025** resolves to 21 Mar (BCL Persian 33-year
  arithmetic) whereas Iran observed 20 Mar (astronomical). A ±1-day calendar artifact
  shared with the existing `PersianCalendarKnownAnswerTests`.
- **Baha'i `naw-ruz` 2023** resolves to 20 Mar (equinox in UT) whereas the Tehran-anchored
  Baha'i date was 21 Mar. 2024–2027 match. All offset holy days inherit the ±1.
- **Oriental Orthodox `coptic-new-year` (11 Sep) / `meskel` (27 Sep)** are fixed; both
  fall a day later (12 / 28 Sep) in the Gregorian year before a leap year. The catalogue
  intentionally leaves the rite-specific shift to territory packs.

## Cross-checks that confirmed correctness

- **Jewish** — all 13 observances match Hebcal exactly across 2023–2026 (BCL
  `HebrewCalendar` ≡ standard arithmetic Hebrew calendar).
- **Jewish 50-year sweep** — the same 13 observances are pinned across Gregorian
  1990–2039 by the embedded vector table
  `Fixtures/Vectors/HebrewObservances-1990-2039.csv`, generated by an independent
  Dershowitz–Reingold implementation (`tools/generate-hebrew-observance-vectors.py`).
  At generation time the table matched the Hebcal-verified 2023–2026 rows above
  (52/52) and the BCL `HebrewCalendar` projection across the full range (650/650);
  the engine sweep (`Resolve_WhenSweptAcrossVectorRange_ShouldMatchIndependentVector`)
  passes 650/650. Note the vectors pin the catalogue's calendar-arithmetic dates —
  e.g. Tisha B'Av is 9 Av itself; the Shabbat fast deferral is an observance
  practice the catalogue deliberately does not encode.
- **Islamic** — tabular and Umm al-Qura match the BCL calendars exactly; Umm al-Qura sits
  within a day of gazetted Saudi dates (e.g. Eid al-Fitr 2024 = 10 Apr, Day of Arafah
  2024 = 15 Jun).
- **Umm al-Qura 50-year sweep** — all 10 `global-islamic-umm-al-qura` observances are
  pinned across Gregorian 1990–2039 by the embedded vector table
  `Fixtures/Vectors/UmmAlQuraObservances-1990-2039.csv` (517 rows; a short lunar date
  lands twice in 17 of the year/observance pairs, and the sweep asserts the full ordered
  occurrence list per pair). The Umm al-Qura month table is calculated KACST data with no
  independent offline oracle, so the vectors are projected from the BCL `UmAlQuraCalendar`
  (which embeds that official table) by `tools/IslamicObservanceVectorGenerator`, and two
  independent checks brace them: every underlying Hijri month start falls +1 or +2 days
  after the geocentric lunar conjunction computed by the standalone Meeus ch. 49 series in
  `tools/verify-islamic-observance-vectors.py` (517/517; 335 at +1, 182 at +2), and the
  table agrees with the hand-pinned published 2023–2025 rows above. The sweep also exposed
  and now guards the `OffsetFromRuleStrategy` multi-occurrence defect (Laylat al-Qadr
  1998-01-25 / 2031-01-21 from the doubled 1997 / 2030 Ramadans). The oldest end of the
  table is additionally **reconciled against a Saudi-published print**: the KFUPM Research
  Institute *Comparison Calendar, 1356–1411 AH* — all 24 month starts of Hijri 1410–1411
  match the BCL/KACST table exactly, and the 17 vector rows they determine (all ten 1990
  observances plus the seven 1991 observances anchored before the print's 11 July 1991 end)
  match 17/17. A reconciliation pass against ummulqura.org.sa for 1412 AH onward remains a
  nicety requiring network access.
- **Persian 50-year sweep** — all 3 `global-persian` observances are pinned across
  Gregorian 1990–2039 by the embedded vector table
  `Fixtures/Vectors/PersianObservances-1990-2039.csv`, generated by an independent Python
  implementation of the official Solar Hijri new-year rule
  (`tools/generate-persian-observance-vectors.py`: Meeus ch. 27 equinox series,
  Espenak–Meeus ΔT, apparent solar noon at the 52.5°E standard meridian). At generation
  time the table matched the BCL `PersianCalendar` projection 150/150 and the hand-pinned
  2022–2026 rows in `PersianCalendarKnownAnswerTests`; the engine sweep passes 150/150.
  A reconciliation pass against time.ir remains a nicety requiring network access.
- **Oriental Orthodox Pascha** — anchor matches published Orthodox Pascha 2023–2027
  (16 Apr, 5 May, 20 Apr, 12 Apr, 2 May); every derived feast lands on its exact offset.
- **Baha'i** — the equinox-plus-offset model reproduces the **official 2025 Badí dates**
  exactly: First Day of Ridván 20 Apr, Declaration of the Báb 23 May, Ascension of
  Bahá'u'lláh 28 May.
- **Hindu** — the lunar set resolves within ±2 of published panchanga dates; the new solar
  harvest festivals are fixed at 14 January as authored.
- **Vesak / East Asian Buddha's Birthday** — within ±2 across 2023–2027 (Vesak 2026 = 1 May
  is correct: that year's leap month follows Vaishakha, so Buddha Purnima is early-May while
  the Asadha full moon is pushed to 29 July).

## Sources

Hebcal; published Orthodox/Western Easter tables; Saudi Umm al-Qura and gazetted Eid dates;
the KFUPM Research Institute *Comparison Calendar, 1356 AH to 1411 AH (14 March 1937 to
11 July 1991)*, King Fahd University of Petroleum & Minerals (print, Hijri–Gregorian
comparison tables; used to reconcile the 1410–1411 AH end of the Umm al-Qura vector table);
Iranian civil calendar (time.ir); the Baha'i World Centre / national Baha'i community holy-day
listings; drikpanchang / published panchanga for Hindu, Sikh and Jain festivals; Tibetan Nuns
Project, qppstudio and publicholidays.asia for Gyalpo Losar; timeanddate and publicholidays.asia
for Asalha Puja / Khao Phansa.
