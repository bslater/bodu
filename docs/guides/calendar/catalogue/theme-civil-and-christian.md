---
title: Civil and Christian catalogues
---

# Civil and Christian catalogues

Concepts defined by the shared catalogues in this theme. A region pack imports the concepts it observes and supplies its own territory scope and non-working status — see the [region pages](index.md#by-region). The **When** column is a one-phrase gloss, not the calculation recipe.

## global-core

| Concept | Category | Non-working | When |
|---|---|---|---|
| New Year's Day | PublicHoliday | Yes | Fixed 1 Jan |
| International Workers' Day | PublicHoliday | Yes | Fixed 1 May |
| New Year's Eve | Cultural | — | Fixed 31 Dec |

_Observed by:_ [AU](region-asia-pacific.md), [CA](region-americas.md), [CN](region-asia-pacific.md), [JP](region-asia-pacific.md), [KR](region-asia-pacific.md), [NZ](region-asia-pacific.md), [SG](region-asia-pacific.md), [US](region-americas.md)

## christian-western

| Concept | Category | Non-working | When |
|---|---|---|---|
| Epiphany | Religious | — | Fixed 6 Jan |
| Candlemas | Religious | — | Fixed 2 Feb |
| Annunciation | Religious | — | Fixed 25 Mar |
| All Saints' Day | Religious | — | Fixed 1 Nov |
| All Souls' Day | Religious | — | Fixed 2 Nov |
| Christmas Eve | Cultural | — | Fixed 24 Dec |
| Christmas Day | PublicHoliday | Yes | Fixed 25 Dec |
| Boxing Day | PublicHoliday | Yes | Fixed 26 Dec |
| Ascension Day | Religious | — | Easter +39 |
| Ash Wednesday | Religious | — | Easter -46 |
| Corpus Christi | Religious | — | Easter +60 |
| Easter Monday | PublicHoliday | Yes | Easter +1 |
| Easter Sunday | Religious | — | Algorithm: western-easter |
| Good Friday | PublicHoliday | Yes | Easter -2 |
| Holy Saturday | Religious | — | Easter -1 |
| Maundy Thursday | Religious | — | Easter -3 |
| Palm Sunday | Religious | — | Easter -7 |
| Shrove Tuesday | Religious | — | Easter -47 |
| Trinity Sunday | Religious | — | Easter +56 |
| Whit Monday | PublicHoliday | Yes | Easter +50 |
| Whit Sunday | Religious | — | Easter +49 |

_Observed by:_ [AU](region-asia-pacific.md), [CA](region-americas.md), [DK](region-europe.md), [IN](region-asia-pacific.md), [KR](region-asia-pacific.md), [NZ](region-asia-pacific.md), [SG](region-asia-pacific.md), [US](region-americas.md)

## christian-orthodox

| Concept | Category | Non-working | When |
|---|---|---|---|
| Orthodox Christmas Eve | Observance | — | Fixed 6 Jan |
| Orthodox Christmas Day | PublicHoliday | Yes | Fixed 7 Jan |
| Orthodox New Year | Observance | — | Fixed 14 Jan |
| Orthodox Epiphany | Observance | — | Fixed 19 Jan |
| Orthodox Ascension Day | Religious | — | Orthodox Easter +39 |
| Orthodox Bright Week | Religious | — | Orthodox Easter +0 |
| Orthodox Clean Monday | Religious | — | Orthodox Easter -48 |
| Orthodox Easter Monday | PublicHoliday | — | Orthodox Easter +1 |
| Orthodox Easter Sunday | Religious | — | Algorithm: orthodox-easter |
| Orthodox Good Friday | PublicHoliday | — | Orthodox Easter -2 |
| Orthodox Holy Saturday | Religious | — | Orthodox Easter -1 |
| Orthodox Holy Thursday | Religious | — | Orthodox Easter -3 |
| Orthodox Lazarus Saturday | Religious | — | Orthodox Easter -8 |
| Orthodox Palm Sunday | Religious | — | Orthodox Easter -7 |
| Orthodox Pentecost | Religious | — | Orthodox Easter +49 |
| Orthodox Pentecost Monday | Religious | — | Orthodox Easter +50 |

## default-minimal

| Concept | Category | Non-working | When |
|---|---|---|---|
| New Year's Day | PublicHoliday | Yes | Fixed 1 Jan |

## europe-common

Pan-European hub ($(@{Stem=europe-common; ResourceId=data.europe-common; Bundle=Europe; Imports=System.Object[]; Concepts=System.Object[]}.ResourceId)): re-exports the common civil, Christian, family, and cultural concepts from the catalogues below, and defines the two Catholic feasts the catalogues do not carry. The 28 European region packs import their shared observances from here.

Re-exports from: `global-core`, `christian-western`, `global-family`, `global-cultural`.

Defines inline:

| Concept | Category | When |
|---|---|---|
| Assumption of Mary | Religious | Fixed 15 Aug |
| Immaculate Conception | Religious | Fixed 8 Dec |

_Observed by:_ [AT](region-europe.md), [BE](region-europe.md), [BG](region-europe.md), [CY](region-europe.md), [CZ](region-europe.md), [DE](region-europe.md), [DK](region-europe.md), [EE](region-europe.md), [ES](region-europe.md), [FI](region-europe.md), [FR](region-europe.md), [GB](region-europe.md), [GR](region-europe.md), [HR](region-europe.md), [HU](region-europe.md), [IE](region-europe.md), [IT](region-europe.md), [LT](region-europe.md), [LU](region-europe.md), [LV](region-europe.md), [MT](region-europe.md), [NL](region-europe.md), [PL](region-europe.md), [PT](region-europe.md), [RO](region-europe.md), [SE](region-europe.md), [SI](region-europe.md), [SK](region-europe.md)

---

*Generated from the notable-date XML resources by `Bodu.Globalization.Calendar/Generate-NotableDateCatalogue.ps1`. Regenerated (UTC): 2026-06-05T03:58:56Z.* For the calculation recipes deliberately omitted here, see [Territories and regional composition](../territories.md), [Working with non-Gregorian calendars](../non-gregorian-calendars.md), and [Holiday patterns](../holiday-patterns.md); for the API, the <xref:Bodu.Globalization.Calendar> namespace.

## See also

- **[Globalization & Calendars guides](../../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
- **[Bodu.Globalization.Calendar guides](../index.md)** — the full guide index for the calendar runtime and its companions.
