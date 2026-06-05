---
title: Americas region packs
---

# Americas region packs

Notable dates observed by each country in the **Americas** v2 data pack, grouped by category. **Territory scope** shows national vs subdivision scoping; **Source** is the direct origin (`inline` or the imported catalogue/hub). See the [comparison matrix](comparison-matrix.md) for a cross-region overview.

## CA

**Subdivisions:** CA-AB, CA-BC, CA-MB, CA-NB, CA-NS, CA-NU, CA-ON, CA-PE, CA-QC, CA-SK, CA-YT. A national `CA` query does not return subdivision-only rules.

### PublicHoliday

| Concept | Non-working | Territory scope | Source | When |
|---|---|---|---|---|
| New Year's Day | Yes | National | [← global-core](theme-civil-and-christian.md#global-core) | Fixed 1 Jan |
| Family Day | Yes | AB, SK, ON, BC, NB | inline | 3rd Mon Feb |
| Islander Day | Yes | PE | inline | 3rd Mon Feb |
| Louis Riel Day | Yes | MB | inline | 3rd Mon Feb |
| Nova Scotia Heritage Day | Yes | NS | inline | 3rd Mon Feb |
| Victoria Day | Yes | National | inline | Mon on/before 24 May |
| Fête nationale du Québec | Yes | QC | inline | Fixed 24 Jun |
| Canada Day | Yes | National | inline | Fixed 1 Jul |
| Nunavut Day | Yes | NU | inline | Fixed 9 Jul |
| British Columbia Day | Yes | BC | inline | 1st Mon Aug |
| Civic Holiday | Yes | ON, MB, SK, NU | inline | 1st Mon Aug |
| Discovery Day | Yes | YT | inline | 3rd Mon Aug |
| New Brunswick Day | Yes | NB | inline | 1st Mon Aug |
| Labour Day | Yes | National | inline | 1st Mon Sep |
| National Day for Truth and Reconciliation | Yes | National | inline | Fixed 30 Sep |
| Thanksgiving | Yes | National | inline | 2nd Mon Oct |
| Christmas Day | Yes | National | [← christian-western](theme-civil-and-christian.md#christian-western) | Fixed 25 Dec |
| Boxing Day | Yes | National | [← christian-western](theme-civil-and-christian.md#christian-western) | Fixed 26 Dec |
| Easter Monday | Yes | National | [← christian-western](theme-civil-and-christian.md#christian-western) | Easter +1 |
| Good Friday | Yes | National | [← christian-western](theme-civil-and-christian.md#christian-western) | Easter -2 |

### Religious

| Concept | Non-working | Territory scope | Source | When |
|---|---|---|---|---|
| Easter Sunday | — | National | [← christian-western](theme-civil-and-christian.md#christian-western) | Algorithm: western-easter |

### Cultural

| Concept | Non-working | Territory scope | Source | When |
|---|---|---|---|---|
| Valentine's Day | — | National | inline | Fixed 14 Feb |
| Halloween | — | National | inline | Fixed 31 Oct |
| Christmas Eve | — | National | [← christian-western](theme-civil-and-christian.md#christian-western) | Fixed 24 Dec |

### Observance

| Concept | Non-working | Territory scope | Source | When |
|---|---|---|---|---|
| International Women's Day | — | National | inline | Fixed 8 Mar |
| Mother's Day | — | National | inline | 2nd Sun May |
| Father's Day | — | National | inline | 3rd Sun Jun |
| National Indigenous Peoples Day | — | National | inline | Fixed 21 Jun |
| Heritage Day | — | AB | inline | 1st Mon Aug |

### Remembrance

| Concept | Non-working | Territory scope | Source | When |
|---|---|---|---|---|
| Remembrance Day | Yes | National | inline | Fixed 11 Nov |

## US

### PublicHoliday

| Concept | Non-working | Territory scope | Source | When |
|---|---|---|---|---|
| New Year's Day | Yes | National | [← global-core](theme-civil-and-christian.md#global-core) | Fixed 1 Jan |
| Birthday of Martin Luther King, Jr. | Yes | National | inline | 3rd Mon Jan |
| Presidents' Day | Yes | National | inline | 3rd Mon Feb |
| Memorial Day | Yes | National | inline | last Mon May |
| Juneteenth National Independence Day | Yes | National | inline | Fixed 19 Jun |
| Independence Day | Yes | National | inline | Fixed 4 Jul |
| Labor Day | Yes | National | inline | 1st Mon Sep |
| Columbus Day | Yes | National | inline | 2nd Mon Oct |
| Veterans Day | Yes | National | inline | Fixed 11 Nov |
| Thanksgiving Day | Yes | National | inline | 4th Thu Nov |
| Christmas Day | Yes | National | [← christian-western](theme-civil-and-christian.md#christian-western) | Fixed 25 Dec |

### Religious

| Concept | Non-working | Territory scope | Source | When |
|---|---|---|---|---|
| Easter Sunday | — | National | [← christian-western](theme-civil-and-christian.md#christian-western) | Algorithm: western-easter |
| Good Friday | — | National | [← christian-western](theme-civil-and-christian.md#christian-western) | Easter -2 |

### Cultural

| Concept | Non-working | Territory scope | Source | When |
|---|---|---|---|---|
| Valentine's Day | — | National | inline | Fixed 14 Feb |
| St. Patrick's Day | — | National | inline | Fixed 17 Mar |
| Halloween | — | National | inline | Fixed 31 Oct |
| Christmas Eve | — | National | [← christian-western](theme-civil-and-christian.md#christian-western) | Fixed 24 Dec |
| Black Friday | — | National | inline | thanksgiving +1 |
| Cyber Monday | — | National | inline | thanksgiving +4 |

### Observance

| Concept | Non-working | Territory scope | Source | When |
|---|---|---|---|---|
| Earth Day | — | National | inline | Fixed 22 Apr |
| Mother's Day | — | National | inline | 2nd Sun May |
| Flag Day | — | National | inline | Fixed 14 Jun |
| Father's Day | — | National | inline | 3rd Sun Jun |
| Indigenous Peoples' Day | — | National | inline | 2nd Mon Oct |

### Remembrance

| Concept | Non-working | Territory scope | Source | When |
|---|---|---|---|---|
| Pearl Harbor Remembrance Day | — | National | inline | Fixed 7 Dec |

### Civic

| Concept | Non-working | Territory scope | Source | When |
|---|---|---|---|---|
| Election Day | — | National | inline | 1st Mon Nov (relative) |

---

*Generated from the v2 notable-date XML resources by `Bodu.Globalization.Calendar/Generate-NotableDateCatalogue.ps1`. Regenerated (UTC): 2026-06-05T03:58:56Z.* For the calculation recipes deliberately omitted here, see [Territories and regional composition](../territories.md), [Working with non-Gregorian calendars](../non-gregorian-calendars.md), and [Holiday patterns](../holiday-patterns.md); for the API, the <xref:Bodu.Globalization.Calendar> namespace.

