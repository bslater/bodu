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
  East Asian Buddha's birthday, Sikh/Jain lunar-derived) are pinned to published dates
  with a **±2-day tolerance**, matching the engine's own documented accuracy and the
  pre-existing `AlgorithmKnownAnswerTests` convention.

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
| `global-sikh` | 4 fixed + 3 lunar-derived | ⚠️ see Guru Arjan Dev note |
| `global-jain` | lunar-derived set | ⚠️ **Maun Agiyaras defect** |
| `global-buddhist` | Vesak, Buddha's Birthday, fixed + Losar/Asalha/Vassa | ⚠️ **Losar & Asalha Puja defects** |
| `global-all` | aggregate import | ✅ imports resolve end to end |

## Defects (engine selects the wrong lunation — off by ~30 days)

These three concepts use fixed-window lunar heuristics that select the lunation a month
before the observed festival in some years. They are **not** within the ±2-day
approximation tolerance.

### 1. Tibetan Losar — `global-buddhist`

`losar` is computed as *first new moon on or after 20 January*. When a new moon falls in
late January, it selects the lunation a month early.

| Year | Engine | Observed Losar | Δ |
|---|---|---|---|
| 2023 | 21 Jan | 21 Feb | −31 d ❌ |
| 2024 | 9 Feb | 10 Feb | −1 d ✅ |
| 2025 | 29 Jan | 28 Feb | −30 d ❌ |
| 2026 | 17 Feb | 18 Feb | −1 d ✅ |
| 2027 | 6 Feb | 7 Feb | −1 d ✅ |

Sources: Tibetan Nuns Project, qppstudio, Wikipedia (Losar).

### 2. Asalha Puja & Vassa — `global-buddhist`

`asalha-puja` is computed as *first full moon on or after 15 June*; `vassa` is one day
later. When a full moon falls in the second half of June, it selects a month early.

| Year | Engine | Observed Asalha Puja | Δ |
|---|---|---|---|
| 2023 | 3 Jul | 3 Jul (India) / 1 Aug (Thai leap year) | ambiguous |
| 2024 | 22 Jun | 21 Jul | −29 d ❌ |
| 2025 | 10 Jul | 10 Jul | ✅ |
| 2026 | 29 Jun | 29 Jul | −30 d ❌ |
| 2027 | 19 Jun | ~19 Jul | −30 d ❌ |

Sources: timeanddate, publicholidays.asia (Asahna Bucha / Khao Phansa), Wikipedia.

### 3. Jain Maun Agiyaras — `global-jain`

`maun-agiyaras` is authored as *Jain Diwali + 11 days* (Kartik shukla ekadashi). The
festival (a.k.a. Maun Ekadashi) is **Margashirsha shukla ekadashi**, ~30 days later. The
catalogue comment ("Kartik shukla ekadashi") names the wrong lunar month. Engine 2024
resolves 12 Nov; the traditional festival is ~11 Dec 2024.

> **Cannot determine intent.** If the author intended the Kartik shukla ekadashi (Dev
> Uthani / Prabodhini Ekadashi), the id/display name is wrong; if they intended Maun
> Agiyaras, the offset is wrong. This needs an authoring decision, so it is left as-is
> and pinned as a characterization test rather than auto-changed.

Source: tattvagyan.com (Maun Ekadashi = Magshar Sud 11).

**Blast radius:** none of `losar`, `asalha-puja`, or `maun-agiyaras` is referenced by any
shipping `Data.<Region>` pack — they live only in these common catalogues. `vesak` (the
only lunar key the region packs use) is correct.

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
- **Islamic** — tabular and Umm al-Qura match the BCL calendars exactly; Umm al-Qura sits
  within a day of gazetted Saudi dates (e.g. Eid al-Fitr 2024 = 10 Apr, Day of Arafah
  2024 = 15 Jun).
- **Oriental Orthodox Pascha** — anchor matches published Orthodox Pascha 2023–2027
  (16 Apr, 5 May, 20 Apr, 12 Apr, 2 May); every derived feast lands on its exact offset.
- **Baha'i** — the equinox-plus-offset model reproduces the **official 2025 Badí dates**
  exactly: First Day of Ridván 20 Apr, Declaration of the Báb 23 May, Ascension of
  Bahá'u'lláh 28 May.
- **Hindu** — the lunar set resolves within ±2 of published panchanga dates (already
  covered by `AlgorithmKnownAnswerTests`); the new solar harvest festivals are fixed at
  14 January as authored.
- **Vesak / East Asian Buddha's Birthday** — within ±2 across 2023–2027.

## Recommendations

1. **Fix the lunation heuristics** (`losar`, `asalha-puja`) so the search window keys off
   the correct month, or compute against the proper Tibetan / Theravāda lunisolar month.
2. **Resolve the Maun Agiyaras ambiguity** — correct either the offset or the id/name.
3. Consider documenting the Coptic / Anglican / Nowruz convention choices in the catalogue
   comments (some already are).

The characterization tests for the three defects assert the engine's *current* output and
the size of each divergence, so a future correction will fail them and prompt an update.
