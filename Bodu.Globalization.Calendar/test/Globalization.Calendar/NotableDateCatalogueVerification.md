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
| `global-jewish` | 13 Hebrew observances | ✅ exact (Hebcal); 50-year sweep |
| `global-islamic` | 10 tabular-Hijri observances | ✅ exact (BCL tabular; ≤2 d from observed); 50-year sweep |
| `global-islamic-umm-al-qura` | 10 Umm al-Qura observances | ✅ exact (BCL UAQ); 50-year sweep, 5 external sources |
| `global-zoroastrian` | 6 Persian-calendar observances | ✅ exact (BCL Persian; see Nowruz note); 50-year sweep |
| `global-hindu` | Makar Sankranti, Pongal, Saraswati Puja + lunar set | ✅ within ±2; 50-year engine-pinned sweep |
| `global-bahai` | Naw-Ruz + 8 solar holy days | ✅ within ±2; matches official 2025 Badí |
| `global-sikh` | 4 fixed + 3 lunar-derived | ✅ within ±2 (see Guru Arjan Dev note) |
| `global-jain` | lunar-derived set | ✅ within ±2 (**Maun Agiyaras fixed**) |
| `global-buddhist` | Vesak, Buddha's Birthday, fixed + Losar/Asalha/Vassa | ✅ within ±2 (**Losar & Asalha fixed**) |
| `global-all` | aggregate import | ✅ imports resolve end to end |

## Defects found and fixed

Verification uncovered four lunation-selection defects: three from fixed-window lunar
heuristics that selected the wrong lunation (off by ~30 days) in some years, and one
ingress-day boundary error that lost a month entirely (found by the fifty-year Hindu
sweep). None of the affected keys is referenced by any shipping `Data.<Region>` pack —
they live only in these common catalogues — so the fixes are contained. All four are now
corrected and pinned to published dates by the known-answer tests.

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

### 4. Lost Magha month of 2029 — `HinduLunarCalculator` (found by the 50-year sweep)

*Was:* each new moon was classified by the sun's sidereal sign at the **start of the new
moon's civil date**. The Magha-defining conjunction of 14 Jan 2029 (~17:25 UT) falls hours
*after* the sun's sidereal Capricorn ingress on that same day, so the midnight evaluation
read Sagittarius, no Magha lunation was found, and Vasant Panchami / Maha Shivaratri 2029
did not resolve at all.

*Fix:* when the primary sign match finds nothing, boundary days are re-evaluated at the
following midnight before giving up, recovering the lost month without touching any year
that already resolved. 2029 now resolves Vasant Panchami 18 Jan (published 19 Jan) and
Maha Shivaratri 11 Feb (exact).

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
  match 17/17. The table is further **reconciled against ummulqura.org.sa** via full-year
  day-by-day exports sampled across the range — 1420, 1430, 1446, and 1448 AH (Gregorian
  1999–2000, 2008–2009, 2024–2025, 2026–2027): 48/48 month starts, 1,418/1,418 day rows,
  all 50 tagged occasions, and all 40 vector rows those years determine match exactly,
  including the doubled 2008 Islamic New Year (29 Dec 2008 = 1 Muharram 1430). One caveat
  is documented deliberately: for 1410 AH the site's retrospective table runs one day later
  than both the KFUPM print and the KACST table embedded in the BCL for 11 of 12 months
  (only 1 Shawwal / Eid al-Fitr 1990 agrees) — the vectors follow the calendar that was
  contemporaneously printed and used in the Kingdom, which the BCL matches day-for-day.
  A third reconciliation comes from the computed columns of **R.H. van Gent's Umm al-Qura
  comparison table** (Utrecht University): the tabulated 1 Muharram / 1 Ramadan / 1 Shawwal
  / 1 & 10 Dhu al-Hijjah dates for 1422–1448 AH determine 216 vector rows and all 216 match
  exactly. A fourth, **cross-vendor** check covers every month of the range: the ICU
  `islamic-umalqura` table (`corpus/islamic/uaq-icu-month-starts-1410-1462.csv`,
  ICU/Node `Intl`-derived — a platform copy of the KACST data, not a gazette) agrees with
  the BCL 372/372 across the officially published core era 1420–1450; for 1410–1419 ICU
  carries the *retrospective* recalculated table (+1 day in 104/120 months — the same
  divergence class the KFUPM print already arbitrated in favor of the contemporaneous
  calendar the BCL follows); and **1451–1462 lie beyond the commonly published KACST
  horizon**, where vendor extensions legitimately differ (+1 day in 47/144 months, always
  ICU later). The vectors' 1451–1462 rows therefore rest on the BCL's extension, braced by
  the astronomical conjunction bound rather than cross-vendor agreement — future official
  KACST publications for those years should be reconciled as they appear. A targeted check
  (Aug 2026) found none exists yet: the relaunched official ummulqura.org.sa is an
  **interactive** calendar/converter service with no fixed, versioned month-start
  publication for 1451 AH onward, so there is currently nothing official to reconcile —
  interactive outputs, if ever sampled, are a distinct source class
  (`official-interactive-output`), not a published table.
- **Saudi gazetted announcements (1422–1448)** — the same van Gent table records the dates
  the **High Judiciary Council of Saudi Arabia actually announced** (via Fatwa-Online) for
  the sighting-sensitive month starts. These are embedded as
  `Fixtures/Vectors/SaudiAnnouncedObservances-1422-1448.csv` (171 rows) and the Regression
  sweep `Resolve_WhenSweptAcrossAnnouncedDates_ShouldBeWithinOneDayOfGazettedDate` asserts
  every announced observance falls **within one day** of the catalogue's computed date —
  the empirically measured maximum: across twenty-seven years the announcements moved
  exactly seventeen month starts by a single day (ten later: Ramadan 1424, Muharram 1433,
  Ramadan 1434, Muharram & Ramadan 1435, Dhu al-Hijjah 1436, Muharram & Dhu al-Hijjah 1437,
  Ramadan 1439, Muharram 1443; seven earlier: Shawwal & Dhu al-Hijjah 1425, Ramadan & Dhu
  al-Hijjah 1427, Shawwal & Dhu al-Hijjah 1428, Shawwal 1429) and never more. This replaces
  the assumed ±2-day moon-sighting tolerance with a measured ±1 bound and is the acceptance
  baseline for a future Saudi crescent-sighting observation variant. A month advanced by
  sighting can run 31 days, in which case a day is reckoned twice (both 28 and 29 December
  2007 were 19 Dhu al-Hijjah 1428) — the catalogue does not model this intra-month
  correction, only the month-start dates.
- **Persian 50-year sweep** — all 3 `global-persian` observances are pinned across
  Gregorian 1990–2039 by the embedded vector table
  `Fixtures/Vectors/PersianObservances-1990-2039.csv`, generated by an independent Python
  implementation of the official Solar Hijri new-year rule
  (`tools/generate-persian-observance-vectors.py`: Meeus ch. 27 equinox series,
  Espenak–Meeus ΔT, apparent solar noon at the 52.5°E standard meridian). At generation
  time the table matched the BCL `PersianCalendar` projection 150/150 and the hand-pinned
  2022–2026 rows in `PersianCalendarKnownAnswerTests`; the engine sweep passes 150/150.
  A fourth confirmation comes from the ICU Persian calendar
  (`corpus/persian/icu-nowruz-leap-1369-1418.csv`): all 50 Nowruz dates and all 49
  internally checkable leap-year flags agree, closing the civil-confirmation residual —
  a University of Tehran leap-table or time.ir spot check is now purely optional.
- **Tabular-Hijri 50-year sweep** — all 10 `global-islamic` observances are pinned across
  Gregorian 1990–2039 by `Fixtures/Vectors/TabularHijriObservances-1990-2039.csv` (517
  rows; double-occurrence years asserted as full ordered lists), projected from the BCL's
  arithmetic `HijriCalendar` at `HijriAdjustment = 0` by the same generator as the Umm
  al-Qura table (`hijri` argument). An arithmetic convention has no external gazette, so
  the brace is astronomical: every underlying month start falls 0–2 days after the
  independent Meeus conjunction (517/517: 101 at +0, 358 at +1, 58 at +2).
- **Zoroastrian 50-year sweep** — all 6 `global-zoroastrian` observances are pinned across
  Gregorian 1990–2039 by `Fixtures/Vectors/ZoroastrianObservances-1990-2039.csv` (300
  rows), derived from the same independent Meeus Solar Hijri implementation
  (`zoroastrian` argument) and cross-verified 300/300 against the BCL `PersianCalendar`
  projection. Zartosht No-Diso (Dey 11) straddles the Gregorian new year, so the sweep
  asserts full ordered occurrence lists (zero or two occurrences in some years).
- **Hindu 50-year sweep** — all 14 `global-hindu` observances are pinned across Gregorian
  1990–2039 by `Fixtures/Vectors/HinduObservances-1990-2039.csv` (700 rows). Unlike the
  tables above these rows are **engine-pinned**: the in-repo `HinduLunarCalculator` is the
  model (no offline independent full-range panchanga exists, and regional panchanga
  reckonings themselves differ by a day), so the table is an explicit regression freeze
  braced by two independent checks — every lunar row verifies within 1.5 days of its
  defining tithi position over the standalone Meeus new/full-moon series in
  `tools/verify-hindu-observance-vectors.py` (600/600, worst 0.93 d) inside its seasonal
  window, and the solar rows are exactly 14 January (100/100) — plus the published
  2023–2029 rows above as external anchors. Exactness relative to a specific published
  panchanga is deliberately not claimed beyond the documented ±1–2 day tolerance.
  Generating the sweep surfaced and fixed the lost-Magha-month defect (see below).
- **Indian gazetted 2026 (DoPT)** — the Government of India's official holiday memorandum
  for 2026 (`corpus/india/dopt-holidays-2026.csv`, 51 rows) cross-checks the Hindu,
  Islamic, and Buddhist catalogues against a second national reckoning. Exact agreements:
  Makar Sankranti 14 Jan, Maha Shivaratri 15 Feb, Holika Dahan 3 Mar (the catalogue's
  `holi` purnima day), Raksha Bandhan 28 Aug, Ganesh Chaturthi 14 Sep, Eid al-Adha
  27 May (vs Umm al-Qura), and Buddha Purnima 1 May — independently confirming the
  early-May Vesak 2026 note below. Every divergence is ±1 day with a known convention
  behind it: DoPT's Holi 4 Mar is the Dhulandi day after the purnima; Janmashtami 4 Sep
  is the Vaishnava reckoning (engine 3 Sep, Smarta); Vasant Panchami 23 Jan, Ram Navami
  26 Mar, Dussehra 20 Oct, Karwa Chauth 29 Oct, and Diwali 8 Nov differ by one day of
  udaya-tithi/regional day-selection (engine 22 Jan / 27 Mar / 19 Oct / 28 Oct / 9 Nov);
  and the Islamic dates run one day after the Saudi/tabular tables (Eid al-Fitr 21 Mar vs
  UAQ 20 Mar, Muharram/Ashura 26 Jun vs 25 Jun, Milad-un-Nabi 26 Aug vs 25 Aug) —
  Indian moon-sighting. Documented as reckoning differences, not asserted; no engine
  defect surfaced.
- **IMD Rashtriya Panchang 1947 S.E. (2025–26)** — the Government of India's official
  almanac (English edition; acquisition records and the 102-row Principal Festivals
  transcription under `corpus/hindu/data/`, verified by
  `tools/verify-imd-festival-vectors.py`) gives a second official Indian reckoning
  spanning 22 Mar 2025 – 20 Apr 2026, and its divergence pattern reproduces the DoPT
  2026 findings across a different window. Exact agreements: Rama Navami 6 Apr and
  Raksha Bandhan 9 Aug 2025, Janmashtami 15 Aug 2025 (IMD's Smarta row; its Vaishnava
  row is the next day, the same split DoPT resolved the other way for 2026), Makara
  Samkranti/Pongal 14 Jan, Maha Shivaratri 15 Feb, and Holika Dahan 3 Mar 2026 (IMD
  `Holikadahana`; its `Holi` 4 Mar is the Dhulandi day, as with DoPT), plus Buddha
  Purnima 12 May 2025 coinciding with the gazetted Vesak Poya. One-day
  udaya-tithi/day-selection divergences, each matching the DoPT-documented direction
  where the windows overlap: Ganesh Chaturthi 27 Aug 2025 (engine 26 Aug), Karaka
  Chaturthi 10 Oct 2025 (engine 9 Oct), Dipavali 20 Oct 2025 (engine 21 Oct), Vasanta
  Panchami 23 Jan 2026 (engine 22 Jan), Rama Navami 26 Mar 2026 (engine 27 Mar). The
  widest gap is Vijaya Dasami 2 Oct 2025 (engine `dussehra` 30 Sep, two days inside the
  documented ±1–2 engine-pin tolerance; IMD reaches 2 Oct via Mahanavami 1 Oct, and its
  Mahashtami row is the verbatim-preserved 30-Sep printer's erratum). All nine Islamic
  rows in the overlap run **exactly one day after the Umm al-Qura table with no
  exception** — Sab-e-Qadr 28 Mar, Id-ul-Fitr 31 Mar, Id-uz-Zuha 7 Jun, Hejira New Year
  27 Jun, Muharram/Ashura 6 Jul, Milad-un-Nabi 5 Sep 2025; first day of Ramadan 19 Feb,
  Sab-e-Qadr 17 Mar, Id-ul-Fitr 21 Mar 2026 — the Indian moon-sighting convention
  observed uniformly. Documented, not asserted; no engine defect surfaced.
- **Oriental Orthodox Pascha** — anchor matches published Orthodox Pascha 2023–2027
  (16 Apr, 5 May, 20 Apr, 12 Apr, 2 May); every derived feast lands on its exact offset.
- **Baha'i** — the equinox-plus-offset model reproduces the **official 2025 Badí dates**
  exactly: First Day of Ridván 20 Apr, Declaration of the Báb 23 May, Ascension of
  Bahá'u'lláh 28 May. The **official 183 B.E. (2026) calendar** anchors all nine mappable
  holy days at a measured one-day bound
  (`Resolve_BahaiHolyDay_WhenComparedWith183BEOfficialDate_ShouldBeWithinOneDay`): 2026 is
  a Tehran-sunset boundary year (equinox ≈ 14:46 UT on 20 March, after Tehran sunset), so
  the engine's Universal-Time equinox model runs uniformly one day early against the
  official dates (Naw-Rúz 21 Mar, Riḍván 21/29 Apr & 2 May, Declaration 24 May, Ascension
  of Bahá'u'lláh 29 May, Martyrdom 10 Jul, Covenant 26 Nov, Ascension of 'Abdu'l-Bahá
  28 Nov). The official table's Twin Birthdays (Birth of the Báb 10 Nov, Birth of
  Bahá'u'lláh 11 Nov 2026) are recorded here but deliberately not asserted — the catalogue
  omits those concepts until the eighth-new-moon-after-Naw-Rúz algorithm exists.
- **Baha'i 50-year official sweep** — the full Universal House of Justice Badí table for
  172–221 B.E. (2015–2064) is transcribed at `corpus/bahai/uhj-holy-days-172-221-be.csv`
  (550 rows, all 11 holy days; via the NZ national Bahá'í institution's reproduction of
  the UHJ table) and its nine catalogue-modelled ids are embedded as
  `Fixtures/Vectors/BahaiHolyDays-172-221BE.csv` (450 rows). Pre-commit verification
  (`tools/verify-bahai-poya-vectors.py`): the eight fixed holy days sit at uniform Badí
  offsets from Naw-Rúz (+31/+39/+42/+64/+69/+111/+250/+252), Naw-Rúz is always 20/21
  March, and every Twin Birthday pair falls within one day of the independent Meeus
  eighth-new-moon computation at Tehran — which also pinned the UHJ counting convention
  ("after the *day* of Naw-Rúz": the 2034 midday new moon on Naw-Rúz does not count, the
  2023 after-sunset one does). The sweep
  (`Resolve_WhenSweptAcrossOfficialBadiTable_ShouldMatchOfficialDateOrRunOneDayEarly`)
  asserts the measured signed bound: the engine's UT-equinox model is **exact in all 30
  years whose official Naw-Rúz is 20 March and exactly one day early in all 20 boundary
  years whose official Naw-Rúz is 21 March** — never late, never mixed within a year
  (270 exact / 180 one-early of 450). The corpus Twin Birthday columns are the acceptance
  dataset for the future eighth-new-moon algorithm, and the Naw-Rúz column for a
  Tehran-sunset equinox variant.
- **Hindu** — the lunar set resolves within ±2 of published panchanga dates; the new solar
  harvest festivals are fixed at 14 January as authored.
- **Vesak / East Asian Buddha's Birthday** — within ±2 across 2023–2027 (Vesak 2026 = 1 May
  is correct: that year's leap month follows Vaishakha, so Buddha Purnima is early-May while
  the Asadha full moon is pushed to 29 July).

## Territory packs

The first multi-year official sweeps at the territory-pack layer (the catalogue
cross-checks above validate the shared concept catalogues; these validate a national
pack's substitution and gazetted rules end to end):

- **United States federal holidays 2011–2030 (OPM)** — every federal holiday across the
  twenty schedule years is pinned to the observed date published by the U.S. Office of
  Personnel Management in `…Americas/test/Globalization.Calendar/Fixtures/Vectors/`
  `UsFederalHolidays-2011-2030.csv` (210 rows) and swept by
  `Resolve_WhenSweptAcrossOpmSchedule_ShouldMatchObservedFederalDate`. The sweep proves
  the statutory 5 U.S.C. 6103 substitution (25 rows shifted Saturday→Friday or
  Sunday→Monday, with `ActualDate` asserted equal to the statutory nominal date), the
  three cross-year in-lieu New Year's Days observed on 31 December (2010, 2021, 2027),
  and Juneteenth's 2021 introduction. `isObserved` was re-derived from the nominal dates
  with weekday guards on transcription; OPM's 2011 Christmas row carries a typographical
  weekday and the table uses the statutory observed date 2011-12-26.
- **New Zealand national holidays 2026–2027** — twenty-two known-answer rows in the
  AsiaPacific pack tests pin the Employment New Zealand published dates, covering
  Mondayisation (`weekend-roll` on Waitangi and Anzac Days), the conflict-aware
  `working-day-substitute` chains of the New Year and Christmas pairs — including
  Boxing Day 2027 observed **Tuesday 28 December**, stepping past Christmas Day's own
  Monday 27 December in-lieu day — and the gazetted Matariki dates (10 Jul 2026,
  25 Jun 2027).

## Sources

Hebcal; published Orthodox/Western Easter tables; Saudi Umm al-Qura and gazetted Eid dates;
the KFUPM Research Institute *Comparison Calendar, 1356 AH to 1411 AH (14 March 1937 to
11 July 1991)*, King Fahd University of Petroleum & Minerals (print, Hijri–Gregorian
comparison tables; used to reconcile the 1410–1411 AH end of the Umm al-Qura vector table);
ummulqura.org.sa full-year day-by-day exports for 1410, 1420, 1430, 1446, and 1448 AH
(official Umm al-Qura calendar site; used to reconcile sampled years across the vector
range, with the 1410 retrospective-table divergence documented above);
R.H. van Gent, *The Umm al-Qura Calendar of Saudi Arabia* (Utrecht University,
webspace.science.uu.nl/~gent0113/islam/ummalqura.htm) — the computed-versus-announced
comparison table for 1422–1448 AH, whose announced column records the High Judiciary
Council of Saudi Arabia announcements as published by Fatwa-Online;
ICU/Node `Intl` platform calendar tables (`islamic-umalqura` for AH 1410–1462, `persian`
for SH 1369–1418; committed under `corpus/islamic/` and `corpus/persian/` as
independent-reconciliation data, delivered 3 Aug 2026);
Iranian civil calendar (time.ir); the Baha'i World Centre / national Baha'i community holy-day
listings, the official Baha'i calendar for 183 B.E. (2026) used for the nine-anchor
sweep, and the Universal House of Justice 50-year Badí table 172–221 B.E. (10 July 2014
message; transcribed via the NZ national Bahá'í institution's Holy Days PDF,
guidelines.bahai.org.nz) driving the 450-row official sweep;
Sri Lankan government holiday gazettes 2287/04, 2341/46, 2395/33, 2438/22, and 2493/05
(documents.gov.lk; gazetted Poya days 2023–2027, `corpus/sri-lanka/`);
Bank of Thailand financial-institution holiday notices 2020–2026 (bot.or.th; unofficial
English translations, Thai text prevails, `corpus/thailand/`);
the Government of India DoPT Office Memorandum F.No.12/2/2023-JCA for 2026
(dopt.gov.in, `corpus/india/`);
drikpanchang / published panchanga for Hindu, Sikh and Jain festivals; Tibetan Nuns
Project, qppstudio and publicholidays.asia for Gyalpo Losar; timeanddate and publicholidays.asia
for Asalha Puja / Khao Phansa; the U.S. Office of Personnel Management federal-holiday
schedules (opm.gov/policy-data-oversight/pay-leave/federal-holidays/) for the 2011–2030
territory sweep; Employment New Zealand public-holiday dates
(employment.govt.nz/leave-and-holidays/public-holidays/public-holidays-and-anniversary-dates)
for the 2026–2027 New Zealand rows.
