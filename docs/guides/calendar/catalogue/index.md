---
title: Notable-date catalogue
---

# Notable-date catalogue

What notable dates the **v2** calendar data ships, and how regions and territories differ. This catalogue is generated from the `Bodu.Globalization.Calendar2` XML resources; it lists the dates and their scope, not the calculation recipes (for those, see the linked guides).

Concepts are authored once in a **shared catalogue** and a **region pack** imports the ones it observes, supplying its own territory scope and non-working status. European packs import through the `europe-common` hub, which itself re-exports from the catalogues. The pages below present the same data along two axes.

## How to read these pages

| Column | Meaning |
|---|---|
| Category | `PublicHoliday`, `Religious`, `Cultural`, `Observance`, `Remembrance`, … |
| Non-working | `Yes` = a non-working public holiday for the scope shown; `—` = a working observance |
| Territory scope | `National`, a subdivision list (e.g. `ENG, WLS, NIR`), or `National + …` |
| Calendar | shown only when non-Gregorian (`Hijri`, `Hebrew`, `Persian`, `ChineseLunisolar`, …) |
| Source | `inline` (defined in the region pack) or `← catalogue` (the direct import) |
| When | a one-phrase gloss: `Fixed 25 Dec`, `Easter +1`, `1st Mon May`, `Algorithm: western-easter` — never the recipe |

## By theme

| Page | Catalogues |
|---|---|
| [Civil and Christian catalogues](theme-civil-and-christian.md) | `global-core`, `christian-western`, `christian-orthodox`, `default-minimal`, `europe-common` |
| [Non-Gregorian religious catalogues](theme-religious-non-gregorian.md) | `global-anchors`, `global-islamic`, `global-islamic-umm-al-qura`, `global-jewish`, `global-hindu`, `global-buddhist`, `global-lunar`, `global-persian` |
| [Cultural, family, and remembrance catalogues](theme-cultural-and-family.md) | `global-cultural`, `global-family`, `global-family-social`, `global-remembrance` |
| [Awareness and themed observances](theme-awareness.md) | `global-un`, `global-health`, `global-environment`, `global-education`, `global-science`, `global-social`, `global-food`, `global-animals` |
| [Aggregate and utility catalogues](theme-aggregates.md) | `global-all`, `global-multiday-normalization` |

## By region

| Page | Countries |
|---|---|
| [Americas region packs](region-americas.md) | CA, US |
| [Asia-Pacific region packs](region-asia-pacific.md) | AU, CN, IN, JP, KR, MY, NZ, SG |
| [Europe region packs](region-europe.md) | AT, BE, BG, CY, CZ, DE, DK, EE, ES, FI, FR, GB, GR, HR, HU, IE, IT, LT, LU, LV, MT, NL, PL, PT, RO, SE, SI, SK |

See also the [cross-region comparison matrix](comparison-matrix.md).

## Coverage

- **Catalogues:** 27
- **Region packs:** 38
- **Distinct concepts (catalogues):** 192
- **Comparison-matrix rows:** 18
- **This page regenerated (UTC):** 2026-06-05T03:58:56Z

---

*Generated from the v2 notable-date XML resources by `Bodu.Globalization.Calendar2/Generate-NotableDateCatalogue.ps1`. Regenerated (UTC): 2026-06-05T03:58:56Z.* For the calculation recipes deliberately omitted here, see [Territories and regional composition](../territories.md), [Working with non-Gregorian calendars](../non-gregorian-calendars.md), and [Holiday patterns](../holiday-patterns.md); for the API, the <xref:Bodu.Globalization.Calendar.V2> namespace.

