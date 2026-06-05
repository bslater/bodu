---
title: Aggregate and utility catalogues
---

# Aggregate and utility catalogues

Concepts defined by the shared v2 catalogues in this theme. A region pack imports the concepts it observes and supplies its own territory scope and non-working status — see the [region pages](index.md#by-region). The **When** column is a one-phrase gloss, not the calculation recipe.

## global-all

Aggregate catalogue ($(@{Stem=global-all; ResourceId=common.global-all; Bundle=; Imports=System.Object[]; Concepts=System.Object[]}.ResourceId)): imports every other catalogue with no cherry-picks, so each contributes its full concept set. Identifiers shared across sources are de-duplicated first-source-wins. Intended for consumers that want every shared observance at once; territory packs cherry-pick instead.

| Imports catalogue |
|---|
| `global-core` |
| `global-anchors` |
| `christian-western` |
| `christian-orthodox` |
| `global-cultural` |
| `global-education` |
| `global-environment` |
| `global-family` |
| `global-family-social` |
| `global-food` |
| `global-health` |
| `global-remembrance` |
| `global-science` |
| `global-social` |
| `global-un` |
| `global-animals` |
| `global-multiday-normalization` |
| `global-lunar` |
| `global-islamic` |
| `global-islamic-umm-al-qura` |
| `global-jewish` |
| `global-hindu` |
| `global-buddhist` |
| `global-persian` |
| `default-minimal` |

_Observed by:_ [GB](region-europe.md)

## global-multiday-normalization

| Concept | Category | Non-working | When |
|---|---|---|---|
| NAIDOC Week | Observance | — | 1st Sun Jul |
| World Space Week | Observance | — | Fixed 4 Oct |

---

*Generated from the v2 notable-date XML resources by `Bodu.Globalization.Calendar2/Generate-NotableDateCatalogue.ps1`. Regenerated (UTC): 2026-06-05T03:58:56Z.* For the calculation recipes deliberately omitted here, see [Territories and regional composition](../territories.md), [Working with non-Gregorian calendars](../non-gregorian-calendars.md), and [Holiday patterns](../holiday-patterns.md); for the API, the <xref:Bodu.Globalization.Calendar.V2> namespace.

