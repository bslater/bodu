# v1 → v2 Notable-Date **entry-level** migration audit

_Generated 2026-06-05. A forensic, entry-by-entry reconciliation of every notable-date definition in the
v1 `Bodu.Globalization.Calendar` resources against the v2 `Bodu.Globalization.Calendar.V2` resources.
Companion to `V1-TO-V2-FUNCTIONAL-AUDIT.md` (which audits *capabilities*); this document audits the
*data* — each individual notable-date entry._

## Scope

| Layer | v1 | v2 |
|---|---|---|
| Catalogues | 26 (`Globalization.Calendar.Resources/*.xml`) | 26 (`Globalization.Calendar.V2/Resources/*.xml`) |
| Region packs | 38 + `europe-common.xml` | 38 |
| Region observed concept-instances | 592 | 581 |

## Method

For every v1 region the audit enumerates each observed entry — both locally-declared `<NotableDate>`
rules and `<UseFrom>/<Use>` imports — and follows v1's transitive import graph
(`region → europe-common → catalogue`) to each concept's real calculation strategy. Each v1 entry is
then matched to a v2 concept by **(1)** normalized name/id (non-consuming, so v1's many single-territory
rules legitimately fold into one consolidated v2 multi-territory rule) and, failing that, **(2)**
date-calculation *strategy signature* (consuming, so a v2 concept already claimed by name cannot absorb
a second, distinct v1 concept that merely shares its date). An entry that matches by neither is a
**drop**. Matched entries are then checked for **territory / non-working / category / adjustment**
fidelity. Findings were confirmed by reading the underlying files.

> **Caveat.** The strategy signature for Easter-derived *algorithm* anchors (`Easter Sunday`,
> `Orthodox Easter Sunday`) and a few lunisolar algorithms differs in string form between v1 and v2 but
> resolves to the same date; these match by name and are **not** drops (spot-verified: every region
> imports `easter-sunday`).

---

## Executive findings

**Headline:** the v2 migration is largely faithful (≈97% of region entries migrated by identity), but it
is **not** the complete, loss-free migration the functional audit claims ("every v1 territory … a
self-contained v2-schema migration of its v1 counterpart"). The audit finds **15 dropped region
entries, 8 territory/status degradations, and 10 dropped catalogue entries.**

### A. Dropped region entries (15 across 6 regions)

| v1 region file | v1 entry (concept · rule) | source | v2 status |
|---|---|---|---|
| `region-ca.xml` | **Christmas Eve** · `Christmas Eve` | import ← christian-gregorian.xml | ✗ **DROPPED** |
| `region-in.xml` | **Saraswati Puja** · `Saraswati Puja` | import ← global-hindu.xml | ✗ **DROPPED** |
| `region-in.xml` | **Onam** · `Onam` | import ← global-hindu.xml | ✗ **DROPPED** |
| `region-jp.xml` | **Golden Week** · `fixed-apr-29-jp` | local | ✗ **DROPPED** |
| `region-jp.xml` | **Obon** · `fixed-aug-13-jp` | local | ✗ **DROPPED** |
| `region-de.xml` | **Christmas Eve** · `Christmas Eve` | import ← europe-common.xml | ✗ **DROPPED** |
| `region-de.xml` | **Father's Day** · `Father's Day` | import ← europe-common.xml | ✗ **DROPPED** |
| `region-fr.xml` | **Good Friday (Alsace-Moselle)** · `offset-easter-sunday-2-fr-67` | local | ✗ **DROPPED** |
| `region-fr.xml` | **Pentecost Sunday** · `Pentecost Sunday` | import ← europe-common.xml | ✗ **DROPPED** |
| `region-fr.xml` | **Christmas Eve** · `Christmas Eve` | import ← europe-common.xml | ✗ **DROPPED** |
| `region-fr.xml` | **Father's Day** · `Father's Day` | import ← europe-common.xml | ✗ **DROPPED** |
| `region-fr.xml` | **April Fool's Day** · `April Fool's Day` | import ← global-all.xml | ✗ **DROPPED** |
| `region-gb.xml` | **Christmas Eve** · `Christmas Eve` | import ← europe-common.xml | ✗ **DROPPED** |
| `region-gb.xml` | **April Fool's Day** · `April Fool's Day` | import ← global-all.xml | ✗ **DROPPED** |
| `region-gb.xml` | **Remembrance Day** · `Remembrance Day` | import ← global-all.xml | ✗ **DROPPED** |

### B. Territory / non-working / category degradations (present in v2 but altered)

These entries exist in v2 but with **broadened territory and/or lost public-holiday status**. The v2
`region-de.xml` header asserts *"The v1 data modelled them at the NATIONAL level"* — this is incorrect:
the v1 `region-de.xml` scopes each to specific Bundesländer (shown below). The migration both
over-broadened the territory **and** flipped `nonWorking` from `true`→`false` (Holiday→Religious), so
these days no longer resolve as non-working public holidays in the states that observe them.

| v1 region file | concept | v1 territory (nonWorking) | v2 territory (nonWorking) | assessment |
|---|---|---|---|---|
| `region-de.xml` | Epiphany | DE-BW,DE-BY,DE-ST (true) | DE (false) | over-broad + status lost |
| `region-de.xml` | Corpus Christi | DE-BW,DE-BY,DE-HE,DE-NW,DE-RP,DE-SL (true) | DE (shared default) | over-broad + status lost |
| `region-de.xml` | Assumption of Mary | DE-BY,DE-SL (true) | DE (false) | over-broad + status lost |
| `region-de.xml` | Reformation Day | 9 states (true) | DE (false) | over-broad + status lost |
| `region-de.xml` | All Saints' Day | DE-BW,DE-BY,DE-NW,DE-RP,DE-SL (true) | DE (shared default) | over-broad + status lost |
| `region-de.xml` | Repentance and Prayer Day | DE-SN (true) | DE (false) | over-broad + status lost |
| `region-de.xml` | International Women's Day | DE + state holiday DE-BE,DE-MV (true) | DE (false) | state public-holiday rule dropped |
| `region-fr.xml` | Saint Stephen's Day | FR-57,FR-67,FR-68 (true) | FR (true) | over-broad (national instead of Alsace-Moselle) |

> **Contrast — a v2 *correction*:** `region-gb.xml` Easter Monday was `territory="GB"` in v1 (wrongly
> including Scotland, which has no Easter Monday bank holiday); v2 correctly narrows it to
> `GB-ENG, GB-WLS, GB-NIR`. This is an intentional improvement, not a regression.

### C. Dropped catalogue entries (10)

| v1 catalogue | dropped concept | v2 catalogue | region impact |
|---|---|---|---|
| `christian-gregorian.xml` | Epiphany | `christian-western.xml` | 11 regions need it → all re-declare it inline |
| `christian-gregorian.xml` | Candlemas | `christian-western.xml` | none |
| `christian-gregorian.xml` | Annunciation | `christian-western.xml` | none |
| `christian-gregorian.xml` | Shrove Tuesday | `christian-western.xml` | none |
| `christian-gregorian.xml` | Ash Wednesday | `christian-western.xml` | none |
| `christian-gregorian.xml` | Palm Sunday | `christian-western.xml` | none |
| `christian-gregorian.xml` | Holy Saturday | `christian-western.xml` | AU, NZ → re-declared inline |
| `christian-gregorian.xml` | Trinity Sunday | `christian-western.xml` | none |
| `christian-gregorian.xml` | All Souls' Day | `christian-western.xml` | LT → re-declared inline |
| `global-hindu.xml` | Onam | `global-hindu.xml` | IN → **also dropped from region** |

No v2 region imports a dropped catalogue concept (the build would fail), so the drops do not break
loading; they reduce the **reusable catalogue surface** (a consumer can no longer cherry-pick these by
import as in v1). `christian-western.xml` carries 12 concepts vs v1 `christian-gregorian.xml`'s 21.

### D. Confirmed renames (migrated correctly under a new name — *not* drops)

| concept (v1) | v2 displayName / id | matched by | regions |
|---|---|---|---|
| International Workers' Day | Labour Day · `workers-day` / `labour-day` / Fête du Travail | fixed 1 May | most of Europe |
| Boxing Day | Saint Stephen's Day / Second Day of Christmas | fixed 26 Dec | AT, DE, IT, SE, … (GB keeps `boxing-day`) |
| Sovereign's Birthday | King's Birthday | strategy | NZ |
| Vesak | Vesak Day | strategy | SG |
| Martin Luther King Jr. Day | Birthday of Martin Luther King, Jr. | strategy | US |
| Repentance Day | Repentance and Prayer Day | name | DE |

### E. `europe-common.xml` disposition

v1's `europe-common.xml` is a **curated re-export hub** (no territory of its own) that 29 European
regions import from. v2 has **no equivalent file**: the migration flattened each region's cherry-picks
into inline rules + direct catalogue imports. Its two locally-defined concepts migrated as follows:
**Assumption of Mary** → inlined per-region (FR, DE, …); **Immaculate Conception** → no region currently
declares it (it had no v1 region consumer either). The flattening is the mechanism by which the
Christmas Eve / Father's Day import omissions in §A occurred.

---

## Full per-region cross-reference

Legend: ✓ name-identity · ↪ rename (same date) · ⊕ consolidated into a multi-territory v2 rule ·
✗ dropped.  *adj* column: `y→y` adjustment migrated, `y→n` adjustment lost, `·` none either side.


### `region-ca.xml`  →  `Bodu.Globalization.Calendar2.Data.Americas/src/Resources/region-ca.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Victoria Day · `weekday-mon-onorbefore-may-24-ca` | local | `victoria-day` | inline | ✓ | CA→CA | · |
| Canada Day · `fixed-jul-01-weekend-roll-ca` | local | `canada-day` | inline | ✓ | CA→CA | y→y |
| Labour Day · `weekday-1st-mon-sep-ca` | local | `labour-day` | inline | ✓ | CA→CA | · |
| National Day for Truth and Reconciliation · `fixed-sep-30-weekend-roll-ca` | local | `truth-and-reconciliation-day` | inline | ✓ | CA→CA | y→y |
| Thanksgiving · `weekday-2nd-mon-oct-ca` | local | `thanksgiving` | inline | ✓ | CA→CA | · |
| Boxing Day · `fixed-dec-26-nonworking-roll-ca` | local | `boxing-day` | ← christian-western | ✓ | CA→CA | y→y |
| National Indigenous Peoples Day · `fixed-jun-21-ca` | local | `national-indigenous-peoples-day` | inline | ✓ | CA→CA | · |
| Family Day · `weekday-3rd-mon-feb-ca-ab` | local | `family-day` | inline | ✓ ⊕ | CA-AB→CA-AB,CA-BC,CA-NB,CA-ON,CA-SK | · |
| Family Day · `weekday-3rd-mon-feb-ca-sk` | local | `family-day` | inline | ✓ ⊕ | CA-SK→CA-AB,CA-BC,CA-NB,CA-ON,CA-SK | · |
| Family Day · `weekday-3rd-mon-feb-ca-on` | local | `family-day` | inline | ✓ ⊕ | CA-ON→CA-AB,CA-BC,CA-NB,CA-ON,CA-SK | · |
| Family Day · `weekday-3rd-mon-feb-ca-bc` | local | `family-day` | inline | ✓ ⊕ | CA-BC→CA-AB,CA-BC,CA-NB,CA-ON,CA-SK | · |
| Family Day · `weekday-3rd-mon-feb-ca-nb` | local | `family-day` | inline | ✓ ⊕ | CA-NB→CA-AB,CA-BC,CA-NB,CA-ON,CA-SK | · |
| Island Day · `weekday-3rd-mon-feb-ca-pe` | local | `island-day` | inline | ✓ | CA-PE→CA-PE | · |
| Louis Riel Day · `weekday-3rd-mon-feb-ca-mb` | local | `louis-riel-day` | inline | ✓ | CA-MB→CA-MB | · |
| Nova Scotia Heritage Day · `weekday-3rd-mon-feb-ca-ns` | local | `nova-scotia-heritage-day` | inline | ✓ | CA-NS→CA-NS | · |
| Fête nationale du Québec · `fixed-jun-24-weekend-roll-ca-qc` | local | `fete-nationale-quebec` | inline | ✓ | CA-QC→CA-QC | y→y |
| Nunavut Day · `fixed-jul-09-weekend-roll-ca-nu` | local | `nunavut-day` | inline | ✓ | CA-NU→CA-NU | y→y |
| British Columbia Day · `weekday-1st-mon-aug-ca-bc` | local | `british-columbia-day` | inline | ✓ | CA-BC→CA-BC | · |
| Civic Holiday · `weekday-1st-mon-aug-ca-on` | local | `civic-holiday` | inline | ✓ | CA-MB,CA-NU,CA-ON,CA-SK→CA-MB,CA-NU,CA-ON,CA-SK | · |
| Heritage Day · `weekday-1st-mon-aug-ca-ab` | local | `heritage-day` | inline | ✓ | CA-AB→CA-AB | · |
| New Brunswick Day · `weekday-1st-mon-aug-ca-nb` | local | `new-brunswick-day` | inline | ✓ | CA-NB→CA-NB | · |
| Discovery Day · `weekday-3rd-mon-aug-ca-yt` | local | `discovery-day` | inline | ✓ | CA-YT→CA-YT | · |
| New Year's Day · `New Year's Day` | ← global-all | `new-years-day` | ← global-core | ✓ | CA→CA | ·→y |
| Valentine's Day · `Valentine's Day` | ← global-all | `valentines-day` | inline | ✓ | CA→CA | · |
| International Women's Day · `International Women's Day` | ← global-all | `womens-day` | inline | ✓ | CA→CA | · |
| Halloween · `Halloween` | ← global-all | `halloween` | inline | ✓ | CA→CA | · |
| Remembrance Day · `Remembrance Day` | ← global-all | `remembrance-day` | inline | ✓ | CA→CA | y→y |
| Good Friday · `Good Friday` | ← christian-gregorian | `good-friday` | ← christian-western | ✓ | CA→CA | · |
| Easter Sunday · `Easter Sunday` | ← christian-gregorian | `easter-sunday` | ← christian-western | ✓ | CA→CA | · |
| Easter Monday · `Easter Monday` | ← christian-gregorian | `easter-monday` | ← christian-western | ✓ | CA→CA | · |
| Christmas Eve · `Christmas Eve` | ← christian-gregorian | — | — | ✗ **DROP** | CA→— | · |
| Christmas Day · `Christmas Day` | ← christian-gregorian | `christmas-day` | ← christian-western | ✓ | CA→CA | y→y |
| Mother's Day · `Mother's Day` | ← global-family | `mothers-day` | inline | ✓ | CA→CA | · |
| Father's Day · `Father's Day` | ← global-family | `fathers-day` | inline | ✓ | CA→CA | · |

### `region-us.xml`  →  `Bodu.Globalization.Calendar2.Data.Americas/src/Resources/region-us.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Martin Luther King Jr. Day · `weekday-3rd-mon-jan-us` | local | `mlk-day` | inline | ↪ rename | US→US | · |
| Presidents' Day · `weekday-3rd-mon-feb-us` | local | `presidents-day` | inline | ✓ | US→US | · |
| St. Patrick's Day · `fixed-mar-17-us` | local | `st-patricks-day` | inline | ✓ | US→US | · |
| Memorial Day · `weekday-last-mon-may-us` | local | `memorial-day` | inline | ✓ | US→US | · |
| Flag Day · `fixed-jun-14-us` | local | `flag-day` | inline | ✓ | US→US | · |
| Juneteenth · `fixed-jun-19-weekend-roll-us` | local | `juneteenth` | inline | ✓ | US→US | y→y |
| Independence Day · `fixed-jul-04-weekend-roll-us` | local | `independence-day` | inline | ✓ | US→US | y→y |
| Labor Day · `weekday-1st-mon-sep-us` | local | `labor-day` | inline | ✓ | US→US | · |
| Columbus Day · `weekday-2nd-mon-oct-us` | local | `columbus-day` | inline | ✓ | US→US | · |
| Indigenous Peoples' Day · `weekday-2nd-mon-oct-us` | local | `indigenous-peoples-day` | inline | ✓ | US→US | · |
| Veterans Day · `fixed-nov-11-weekend-roll-us` | local | `veterans-day` | inline | ✓ | US→US | y→y |
| Election Day · `weekday-tue-after-1st-mon-nov-us` | local | `election-day` | inline | ✓ | US→US | · |
| Thanksgiving · `weekday-4th-thu-nov-us` | local | `thanksgiving` | inline | ✓ | US→US | · |
| Black Friday · `offset-thanksgiving+1-us` | local | `black-friday` | inline | ✓ | US→US | · |
| Cyber Monday · `offset-thanksgiving+4-us` | local | `cyber-monday` | inline | ✓ | US→US | · |
| Pearl Harbor Remembrance Day · `fixed-dec-07-us` | local | `pearl-harbor-day` | inline | ✓ | US→US | · |
| New Year's Day · `New Year's Day` | ← global-all | `new-years-day` | ← global-core | ✓ | US→US | ·→y |
| Valentine's Day · `Valentine's Day` | ← global-all | `valentines-day` | inline | ✓ | US→US | · |
| Earth Day · `Earth Day` | ← global-all | `earth-day` | inline | ✓ | US→US | · |
| Halloween · `Halloween` | ← global-all | `halloween` | inline | ✓ | US→US | · |
| Good Friday · `Good Friday` | ← christian-gregorian | `good-friday` | ← christian-western | ✓ | US→US | · |
| Easter Sunday · `Easter Sunday` | ← christian-gregorian | `easter-sunday` | ← christian-western | ✓ | US→US | · |
| Christmas Eve · `Christmas Eve` | ← christian-gregorian | `christmas-eve` | ← christian-western | ✓ | US→US | · |
| Christmas Day · `Christmas Day` | ← christian-gregorian | `christmas-day` | ← christian-western | ✓ | US→US | y→y |
| Mother's Day · `Mother's Day` | ← global-family | `mothers-day` | inline | ✓ | US→US | · |
| Father's Day · `Father's Day` | ← global-family | `fathers-day` | inline | ✓ | US→US | · |

### `region-au.xml`  →  `Bodu.Globalization.Calendar2.Data.AsiaPacific/src/Resources/region-au.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Australia Day · `fixed-jan-26-weekend-roll-au` | local | `australia-day` | inline | ✓ | AU→AU | y→y |
| Anzac Day · `fixed-apr-25-au` | local | `anzac-day` | inline | ✓ ⊕ | AU→AU,AU-NSW,AU-NT,AU-WA | ·→y |
| Anzac Day · `fixed-apr-25-au-wa` | local | `anzac-day` | inline | ✓ ⊕ | AU-WA→AU,AU-NSW,AU-NT,AU-WA | y→y |
| Anzac Day · `fixed-apr-25-au-nt` | local | `anzac-day` | inline | ✓ ⊕ | AU-NT→AU,AU-NSW,AU-NT,AU-WA | y→y |
| Anzac Day · `fixed-apr-25-au-nsw` | local | `anzac-day` | inline | ✓ ⊕ | AU-NSW→AU,AU-NSW,AU-NT,AU-WA | y→y |
| Boxing Day · `fixed-dec-26-nonworking-roll-au` | local | `boxing-day` | ← christian-western | ✓ | AU→AU | y→y |
| Harmony Day · `fixed-mar-21-au` | local | `harmony-day` | inline | ✓ | AU→AU | · |
| National Sorry Day · `fixed-may-26-au` | local | `national-sorry-day` | inline | ✓ | AU→AU | · |
| National Reconciliation Week · `fixed-may-27-au` | local | `national-reconciliation-week` | inline | ✓ | AU→AU | · |
| Mabo Day · `fixed-jun-03-au` | local | `mabo-day` | inline | ✓ | AU→AU | · |
| NAIDOC Week · `weekday-1st-sun-jul-au` | local | `naidoc-week` | inline | ✓ | AU→AU | · |
| Father's Day · `weekday-1st-sun-sep-au` | local | `fathers-day` | inline | ✓ | AU→AU | · |
| R U OK? Day · `weekday-2nd-thu-sep-au` | local | `r-u-ok-day` | inline | ✓ | AU→AU | · |
| King's Birthday · `weekday-2nd-mon-jun-au-nsw` | local | `kings-birthday` | inline | ✓ ⊕ | AU-NSW→AU-ACT,AU-NSW,AU-NT,AU-QLD,AU-SA,AU-TAS,AU-VIC,AU-WA | · |
| King's Birthday · `weekday-2nd-mon-jun-au-vic` | local | `kings-birthday` | inline | ✓ ⊕ | AU-VIC→AU-ACT,AU-NSW,AU-NT,AU-QLD,AU-SA,AU-TAS,AU-VIC,AU-WA | · |
| King's Birthday · `weekday-1st-mon-oct-au-qld` | local | `kings-birthday` | inline | ✓ ⊕ | AU-QLD→AU-ACT,AU-NSW,AU-NT,AU-QLD,AU-SA,AU-TAS,AU-VIC,AU-WA | · |
| King's Birthday · `weekday-2nd-mon-jun-au-sa` | local | `kings-birthday` | inline | ✓ ⊕ | AU-SA→AU-ACT,AU-NSW,AU-NT,AU-QLD,AU-SA,AU-TAS,AU-VIC,AU-WA | · |
| King's Birthday · `weekday-last-mon-sep-au-wa` | local | `kings-birthday` | inline | ✓ ⊕ | AU-WA→AU-ACT,AU-NSW,AU-NT,AU-QLD,AU-SA,AU-TAS,AU-VIC,AU-WA | · |
| King's Birthday · `weekday-2nd-mon-jun-au-tas` | local | `kings-birthday` | inline | ✓ ⊕ | AU-TAS→AU-ACT,AU-NSW,AU-NT,AU-QLD,AU-SA,AU-TAS,AU-VIC,AU-WA | · |
| King's Birthday · `weekday-2nd-mon-jun-au-nt` | local | `kings-birthday` | inline | ✓ ⊕ | AU-NT→AU-ACT,AU-NSW,AU-NT,AU-QLD,AU-SA,AU-TAS,AU-VIC,AU-WA | · |
| King's Birthday · `weekday-2nd-mon-jun-au-act` | local | `kings-birthday` | inline | ✓ ⊕ | AU-ACT→AU-ACT,AU-NSW,AU-NT,AU-QLD,AU-SA,AU-TAS,AU-VIC,AU-WA | · |
| Bank Holiday · `weekday-1st-mon-aug-au-nsw` | local | `bank-holiday` | inline | ✓ | AU-NSW→AU-NSW | · |
| Labour Day · `weekday-1st-mon-oct-au-nsw` | local | `labour-day` | inline | ✓ ⊕ | AU-NSW→AU-ACT,AU-NSW,AU-QLD,AU-SA,AU-VIC,AU-WA | · |
| Labour Day · `weekday-2nd-mon-mar-au-vic` | local | `labour-day` | inline | ✓ ⊕ | AU-VIC→AU-ACT,AU-NSW,AU-QLD,AU-SA,AU-VIC,AU-WA | · |
| Labour Day · `weekday-1st-mon-may-au-qld` | local | `labour-day` | inline | ✓ ⊕ | AU-QLD→AU-ACT,AU-NSW,AU-QLD,AU-SA,AU-VIC,AU-WA | · |
| Labour Day · `weekday-1st-mon-oct-au-sa` | local | `labour-day` | inline | ✓ ⊕ | AU-SA→AU-ACT,AU-NSW,AU-QLD,AU-SA,AU-VIC,AU-WA | · |
| Labour Day · `weekday-1st-mon-mar-au-wa` | local | `labour-day` | inline | ✓ ⊕ | AU-WA→AU-ACT,AU-NSW,AU-QLD,AU-SA,AU-VIC,AU-WA | · |
| Labour Day · `weekday-1st-mon-oct-au-act` | local | `labour-day` | inline | ✓ ⊕ | AU-ACT→AU-ACT,AU-NSW,AU-QLD,AU-SA,AU-VIC,AU-WA | · |
| AFL Grand Final Friday · `weekday-last-fri-sep-au-vic` | local | `afl-grand-final-friday` | inline | ✓ | AU-VIC→AU-VIC | · |
| Melbourne Cup Day · `weekday-1st-tue-nov-au-vic` | local | `melbourne-cup-day` | inline | ✓ | AU-VIC→AU-VIC | · |
| Royal Queensland Show · `weekday-2nd-wed-aug-au-qld` | local | `royal-queensland-show` | inline | ✓ | AU-QLD→AU-QLD | · |
| Adelaide Cup Day · `weekday-2nd-mon-mar-au-sa` | local | `adelaide-cup-day` | inline | ✓ | AU-SA→AU-SA | · |
| Proclamation Day · `fixed-dec-26-weekend-roll-au-sa` | local | `proclamation-day` | inline | ✓ | AU-SA→AU-SA | y→y |
| Western Australia Day · `weekday-1st-mon-jun-au-wa` | local | `western-australia-day` | inline | ✓ | AU-WA→AU-WA | · |
| Eight Hours Day · `weekday-2nd-mon-mar-au-tas` | local | `eight-hours-day` | inline | ✓ | AU-TAS→AU-TAS | · |
| Recreation Day · `weekday-1st-mon-nov-au-tas` | local | `recreation-day` | inline | ✓ | AU-TAS→AU-TAS | · |
| May Day · `weekday-1st-mon-may-au-nt` | local | `may-day` | inline | ✓ | AU-NT→AU-NT | · |
| Picnic Day · `weekday-1st-mon-aug-au-nt` | local | `picnic-day` | inline | ✓ | AU-NT→AU-NT | · |
| Canberra Day · `weekday-2nd-mon-mar-au-act` | local | `canberra-day` | inline | ✓ | AU-ACT→AU-ACT | · |
| Reconciliation Day · `weekday-mon-onorafter-may-27-au-act` | local | `reconciliation-day` | inline | ✓ | AU-ACT→AU-ACT | · |
| New Year's Day · `New Year's Day` | ← global-all | `new-years-day` | ← global-core | ✓ | —→AU | y→y |
| Valentine's Day · `Valentine's Day` | ← global-all | `valentines-day` | ← global-cultural | ✓ | AU→AU | · |
| April Fool's Day · `April Fool's Day` | ← global-all | `april-fools-day` | ← global-cultural | ✓ | AU→AU | · |
| Halloween · `Halloween` | ← global-all | `halloween` | ← global-cultural | ✓ | AU→AU | · |
| Remembrance Day · `Remembrance Day` | ← global-all | `remembrance-day` | ← global-remembrance | ✓ | AU→AU | · |
| Good Friday · `Good Friday` | ← christian-gregorian | `good-friday` | ← christian-western | ✓ | AU→AU | · |
| Easter Saturday · `Holy Saturday` | ← christian-gregorian | `easter-saturday` | inline | ✓ | AU→AU | · |
| Easter Sunday · `Easter Sunday` | ← christian-gregorian | `easter-sunday` | ← christian-western | ✓ | AU→AU | · |
| Easter Monday · `Easter Monday` | ← christian-gregorian | `easter-monday` | ← christian-western | ✓ | AU→AU | · |
| Christmas Eve · `Christmas Eve` | ← christian-gregorian | `christmas-eve` | ← christian-western | ✓ | AU→AU | · |
| Christmas Day · `Christmas Day` | ← christian-gregorian | `christmas-day` | ← christian-western | ✓ | AU→AU | y→y |
| Mother's Day · `Mother's Day` | ← global-family | `mothers-day` | ← global-family | ✓ | AU→AU | · |

### `region-cn.xml`  →  `Bodu.Globalization.Calendar2.Data.AsiaPacific/src/Resources/region-cn.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| New Year's Day · `fixed-jan-01-cn` | local | `new-years-day` | ← global-core | ✓ | CN→CN | · |
| International Labour Day · `fixed-may-01-cn` | local | `labour-day` | inline | ✓ | CN→CN | · |
| National Day · `fixed-oct-01-cn` | local | `national-day` | inline | ✓ | CN→CN | · |
| Lunar New Year · `Lunar New Year` | ← global-lunar | `lunar-new-year` | inline | ✓ | CN→CN | · |
| Lantern Festival · `Lantern Festival` | ← global-lunar | `lantern-festival` | inline | ✓ | CN→CN | · |
| Qingming Festival · `Qingming Festival` | ← global-lunar | `qingming-festival` | inline | ✓ | CN→CN | · |
| Dragon Boat Festival · `Dragon Boat Festival` | ← global-lunar | `dragon-boat-festival` | inline | ✓ | CN→CN | · |
| Qixi Festival · `Qixi Festival` | ← global-lunar | `qixi-festival` | inline | ✓ | CN→CN | · |
| Hungry Ghost Festival · `Hungry Ghost Festival` | ← global-lunar | `hungry-ghost-festival` | inline | ✓ | CN→CN | · |
| Mid-Autumn Festival · `Mid-Autumn Festival` | ← global-lunar | `mid-autumn-festival` | inline | ✓ | CN→CN | · |
| Double Ninth Festival · `Double Ninth Festival` | ← global-lunar | `double-ninth-festival` | inline | ✓ | CN→CN | · |
| Winter Solstice Festival · `Winter Solstice Festival` | ← global-lunar | `winter-solstice-festival` | inline | ✓ | CN→CN | · |

### `region-in.xml`  →  `Bodu.Globalization.Calendar2.Data.AsiaPacific/src/Resources/region-in.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Republic Day · `fixed-jan-26-in` | local | `republic-day` | inline | ✓ | IN→IN | · |
| Independence Day · `fixed-aug-15-in` | local | `independence-day` | inline | ✓ | IN→IN | · |
| Gandhi Jayanti · `fixed-oct-02-in` | local | `gandhi-jayanti` | inline | ✓ | IN→IN | · |
| Makar Sankranti · `Makar Sankranti` | ← global-hindu | `makar-sankranti` | inline | ✓ | IN→IN | · |
| Pongal · `Pongal` | ← global-hindu | `pongal` | inline | ✓ | IN→IN | · |
| Saraswati Puja · `Saraswati Puja` | ← global-hindu | — | — | ✗ **DROP** | IN→— | · |
| Maha Shivaratri · `Maha Shivaratri` | ← global-hindu | `maha-shivaratri` | inline | ✓ | IN→IN | · |
| Holi · `Holi` | ← global-hindu | `holi` | inline | ✓ | IN→IN | · |
| Ram Navami · `Ram Navami` | ← global-hindu | `ram-navami` | inline | ✓ | IN→IN | · |
| Raksha Bandhan · `Raksha Bandhan` | ← global-hindu | `raksha-bandhan` | inline | ✓ | IN→IN | · |
| Janmashtami · `Janmashtami` | ← global-hindu | `janmashtami` | inline | ✓ | IN→IN | · |
| Ganesh Chaturthi · `Ganesh Chaturthi` | ← global-hindu | `ganesh-chaturthi` | inline | ✓ | IN→IN | · |
| Onam · `Onam` | ← global-hindu | — | — | ✗ **DROP** | IN→— | · |
| Navaratri · `Navaratri` | ← global-hindu | `navaratri` | inline | ✓ | IN→IN | · |
| Dussehra · `Dussehra` | ← global-hindu | `dussehra` | inline | ✓ | IN→IN | · |
| Diwali · `Diwali` | ← global-hindu | `diwali` | inline | ✓ | IN→IN | · |
| Eid al-Fitr · `Eid al-Fitr` | ← global-islamic | `eid-al-fitr` | inline | ✓ | IN→IN | · |
| Eid al-Adha · `Eid al-Adha` | ← global-islamic | `eid-al-adha` | inline | ✓ | IN→IN | · |
| Day of Ashura · `Day of Ashura` | ← global-islamic | `day-of-ashura` | inline | ✓ | IN→IN | · |
| Good Friday · `Good Friday` | ← christian-gregorian | `good-friday` | ← christian-western | ✓ | IN→IN | · |
| Christmas Day · `Christmas Day` | ← christian-gregorian | `christmas-day` | ← christian-western | ✓ | IN→IN | · |

### `region-jp.xml`  →  `Bodu.Globalization.Calendar2.Data.AsiaPacific/src/Resources/region-jp.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Coming of Age Day · `weekday-2nd-mon-jan-jp` | local | `coming-of-age-day` | inline | ✓ | JP→JP | · |
| Setsubun · `fixed-feb-03-jp` | local | `setsubun` | inline | ✓ | JP→JP | · |
| National Foundation Day · `fixed-feb-11-jp` | local | `national-foundation-day` | inline | ✓ | JP→JP | · |
| Emperor's Birthday · `fixed-feb-23-jp` | local | `emperors-birthday` | inline | ✓ | JP→JP | · |
| Vernal Equinox Day · `algo-jp-vernal-equinox` | local | `vernal-equinox-day` | inline | ✓ | JP→JP | · |
| Golden Week · `fixed-apr-29-jp` | local | — | — | ✗ **DROP** | JP→— | · |
| Showa Day · `fixed-apr-29-jp` | local | `showa-day` | inline | ✓ | JP→JP | · |
| Constitution Memorial Day · `fixed-may-03-jp` | local | `constitution-memorial-day` | inline | ✓ | JP→JP | · |
| Greenery Day · `fixed-may-04-jp` | local | `greenery-day` | inline | ✓ | JP→JP | · |
| Children's Day · `fixed-may-05-jp` | local | `childrens-day` | inline | ✓ | JP→JP | · |
| Marine Day · `weekday-3rd-mon-jul-jp` | local | `marine-day` | inline | ✓ | JP→JP | · |
| Mountain Day · `fixed-aug-11-jp` | local | `mountain-day` | inline | ✓ | JP→JP | · |
| Obon · `fixed-aug-13-jp` | local | — | — | ✗ **DROP** | JP→— | · |
| Respect for the Aged Day · `weekday-3rd-mon-sep-jp` | local | `respect-for-the-aged-day` | inline | ✓ | JP→JP | · |
| Autumnal Equinox Day · `algo-jp-autumnal-equinox` | local | `autumnal-equinox-day` | inline | ✓ | JP→JP | · |
| Sports Day · `weekday-2nd-mon-oct-jp` | local | `sports-day` | inline | ✓ | JP→JP | · |
| Culture Day · `fixed-nov-03-jp` | local | `culture-day` | inline | ✓ | JP→JP | · |
| Labour Thanksgiving Day · `fixed-nov-23-jp` | local | `labour-thanksgiving-day` | inline | ✓ | JP→JP | · |
| New Year's Day · `New Year's Day` | ← global-core | `new-years-day` | ← global-core | ✓ | JP→JP | · |
| Bodhi Day · `Bodhi Day` | ← global-buddhist | `bodhi-day` | inline | ✓ | JP→JP | · |

### `region-kr.xml`  →  `Bodu.Globalization.Calendar2.Data.AsiaPacific/src/Resources/region-kr.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| New Year's Day · `fixed-jan-01-kr` | local | `new-years-day` | ← global-core | ✓ | KR→KR | · |
| Independence Movement Day · `fixed-mar-01-kr` | local | `independence-movement-day` | inline | ✓ | KR→KR | · |
| Children's Day · `fixed-may-05-kr` | local | `childrens-day` | inline | ✓ | KR→KR | · |
| Memorial Day · `fixed-jun-06-kr` | local | `memorial-day` | inline | ✓ | KR→KR | · |
| Constitution Day · `fixed-jul-17-kr` | local | `constitution-day` | inline | ✓ | KR→KR | · |
| Liberation Day · `fixed-aug-15-kr` | local | `liberation-day` | inline | ✓ | KR→KR | · |
| National Foundation Day · `fixed-oct-03-kr` | local | `national-foundation-day` | inline | ✓ | KR→KR | · |
| Hangul Day · `fixed-oct-09-kr` | local | `hangul-day` | inline | ✓ | KR→KR | · |
| Christmas Day · `fixed-dec-25-kr` | local | `christmas-day` | ← christian-western | ✓ | KR→KR | · |
| Seollal · `Lunar New Year` | ← global-lunar | `seollal` | inline | ✓ | KR→KR | · |
| Chuseok · `Mid-Autumn Festival` | ← global-lunar | `chuseok` | inline | ✓ | KR→KR | · |
| Buddha's Birthday · `Vesak` | ← global-buddhist | `buddhas-birthday` | inline | ✓ | KR→KR | · |

### `region-my.xml`  →  `Bodu.Globalization.Calendar2.Data.AsiaPacific/src/Resources/region-my.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Hari Raya Aidilfitri · `Eid al-Fitr` | ← global-islamic | `hari-raya-aidilfitri` | inline | ✓ | MY→MY | · |
| Hari Raya Aidiladha · `Eid al-Adha` | ← global-islamic | `hari-raya-aidiladha` | inline | ✓ | MY→MY | · |

### `region-nz.xml`  →  `Bodu.Globalization.Calendar2.Data.AsiaPacific/src/Resources/region-nz.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Day after New Year's Day · `fixed-jan-02-weekend-roll-nz` | local | `day-after-new-years-day` | inline | ✓ | NZ→NZ | y→y |
| Waitangi Day · `fixed-feb-06-weekend-roll-nz` | local | `waitangi-day` | inline | ✓ | NZ→NZ | y→y |
| Anzac Day · `fixed-apr-25-weekend-roll-nz` | local | `anzac-day` | inline | ✓ | NZ→NZ | y→y |
| Sovereign's Birthday · `weekday-1st-mon-jun-nz` | local | `kings-birthday` | inline | ↪ rename | NZ→NZ | · |
| Matariki · `algo-matariki-nz` | local | `matariki` | inline | ✓ | NZ→NZ | · |
| Labour Day · `weekday-4th-mon-oct-nz` | local | `labour-day` | inline | ✓ | NZ→NZ | · |
| Father's Day · `weekday-1st-sun-sep-nz` | local | `fathers-day` | inline | ✓ | NZ→NZ | · |
| Guy Fawkes Night · `fixed-nov-05-nz` | local | `guy-fawkes-night` | inline | ✓ | NZ→NZ | · |
| New Year's Day · `New Year's Day` | ← global-all | `new-years-day` | ← global-core | ✓ | NZ→NZ | y→y |
| Valentine's Day · `Valentine's Day` | ← global-all | `valentines-day` | inline | ✓ | NZ→NZ | · |
| Halloween · `Halloween` | ← global-all | `halloween` | inline | ✓ | NZ→NZ | · |
| Remembrance Day · `Remembrance Day` | ← global-all | `remembrance-day` | inline | ✓ | NZ→NZ | · |
| Good Friday · `Good Friday` | ← christian-gregorian | `good-friday` | ← christian-western | ✓ | NZ→NZ | · |
| Easter Saturday · `Holy Saturday` | ← christian-gregorian | `easter-saturday` | inline | ✓ | NZ→NZ | · |
| Easter Sunday · `Easter Sunday` | ← christian-gregorian | `easter-sunday` | ← christian-western | ✓ | NZ→NZ | · |
| Easter Monday · `Easter Monday` | ← christian-gregorian | `easter-monday` | ← christian-western | ✓ | NZ→NZ | · |
| Christmas Day · `Christmas Day` | ← christian-gregorian | `christmas-day` | ← christian-western | ✓ | NZ→NZ | y→y |
| Boxing Day · `Boxing Day` | ← christian-gregorian | `boxing-day` | ← christian-western | ✓ | NZ→NZ | y→y |
| Mother's Day · `Mother's Day` | ← global-family | `mothers-day` | inline | ✓ | NZ→NZ | · |

### `region-sg.xml`  →  `Bodu.Globalization.Calendar2.Data.AsiaPacific/src/Resources/region-sg.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| New Year's Day · `fixed-jan-01-sg` | local | `new-years-day` | ← global-core | ✓ | SG→SG | · |
| Labour Day · `fixed-may-01-sg` | local | `labour-day` | inline | ✓ | SG→SG | · |
| National Day · `fixed-aug-09-sg` | local | `national-day` | inline | ✓ | SG→SG | · |
| Chinese New Year · `Lunar New Year` | ← global-lunar | `chinese-new-year` | inline | ✓ | SG→SG | · |
| Hari Raya Puasa · `Eid al-Fitr` | ← global-islamic | `hari-raya-puasa` | inline | ✓ | SG→SG | · |
| Hari Raya Haji · `Eid al-Adha` | ← global-islamic | `hari-raya-haji` | inline | ✓ | SG→SG | · |
| Deepavali · `Diwali` | ← global-hindu | `deepavali` | inline | ✓ | SG→SG | · |
| Vesak · `Vesak` | ← global-buddhist | `vesak-day` | inline | ↪ rename | SG→SG | · |
| Good Friday · `Good Friday` | ← christian-gregorian | `good-friday` | ← christian-western | ✓ | SG→SG | · |
| Christmas Day · `Christmas Day` | ← christian-gregorian | `christmas-day` | ← christian-western | ✓ | SG→SG | · |

### `region-at.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-at.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| National Day · `fixed-oct-26-at` | local | `national-day` | inline | ✓ | AT→AT | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | AT→AT | ·→y |
| Epiphany · `Epiphany` | ← europe-common | `epiphany` | inline | ✓ | AT→AT | · |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | AT→AT | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | AT→AT | · |
| State Holiday · `International Workers' Day` | ← europe-common | `state-holiday` | inline | ✓ | AT→AT | · |
| Ascension Day · `Ascension Day` | ← europe-common | `ascension-day` | ← christian-western | ✓ | AT→AT | · |
| Whit Monday · `Whit Monday` | ← europe-common | `whit-monday` | ← christian-western | ✓ | AT→AT | · |
| Corpus Christi · `Corpus Christi` | ← europe-common | `corpus-christi` | ← christian-western | ✓ | AT→AT | · |
| Assumption of Mary · `Assumption of Mary` | ← europe-common | `assumption-of-mary` | inline | ✓ | AT→AT | · |
| All Saints' Day · `All Saints' Day` | ← europe-common | `all-saints-day` | ← christian-western | ✓ | AT→AT | · |
| Immaculate Conception · `Immaculate Conception` | ← europe-common | `immaculate-conception` | inline | ✓ | AT→AT | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | AT→AT | · |
| Saint Stephen's Day · `Boxing Day` | ← europe-common | `saint-stephens-day` | inline | ✓ | AT→AT | · |

### `region-be.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-be.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Belgian National Day · `fixed-jul-21-be` | local | `belgian-national-day` | inline | ✓ | BE→BE | · |
| Armistice Day · `fixed-nov-11-be` | local | `armistice-day` | inline | ✓ | BE→BE | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | BE→BE | ·→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | BE→BE | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | BE→BE | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | BE→BE | · |
| Ascension Day · `Ascension Day` | ← europe-common | `ascension-day` | ← christian-western | ✓ | BE→BE | · |
| Whit Monday · `Whit Monday` | ← europe-common | `whit-monday` | ← christian-western | ✓ | BE→BE | · |
| Assumption of Mary · `Assumption of Mary` | ← europe-common | `assumption-of-mary` | inline | ✓ | BE→BE | · |
| All Saints' Day · `All Saints' Day` | ← europe-common | `all-saints-day` | ← christian-western | ✓ | BE→BE | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | BE→BE | · |

### `region-bg.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-bg.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Liberation Day · `fixed-mar-03-bg` | local | `liberation-day` | inline | ✓ | BG→BG | · |
| Saint George's Day · `fixed-may-06-bg` | local | `saint-georges-day` | inline | ✓ | BG→BG | · |
| Bulgarian Education and Culture and Slavonic Literature Day · `fixed-may-24-bg` | local | `bulgarian-education-and-culture-and-slavonic-literature-day` | inline | ✓ | BG→BG | · |
| Unification Day · `fixed-sep-06-bg` | local | `unification-day` | inline | ✓ | BG→BG | · |
| Independence Day · `fixed-sep-22-bg` | local | `independence-day` | inline | ✓ | BG→BG | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | BG→BG | ·→y |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | BG→BG | · |
| Christmas Eve · `Christmas Eve` | ← europe-common | `christmas-eve` | ← christian-western | ✓ | BG→BG | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | BG→BG | · |
| Second Day of Christmas · `Boxing Day` | ← europe-common | `second-day-of-christmas` | inline | ✓ | BG→BG | · |
| Good Friday · `Orthodox Good Friday` | ← christian-orthodox | `good-friday` | inline | ✓ | BG→BG | · |
| Holy Saturday · `Orthodox Holy Saturday` | ← christian-orthodox | `holy-saturday` | inline | ✓ | BG→BG | · |
| Orthodox Easter Sunday · `Orthodox Easter Sunday` | ← christian-orthodox | `orthodox-easter-sunday` | inline | ✓ | BG→BG | · |
| Orthodox Easter Monday · `Orthodox Easter Monday` | ← christian-orthodox | `orthodox-easter-monday` | inline | ✓ | BG→BG | · |

### `region-cy.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-cy.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Greek Independence Day · `fixed-mar-25-cy` | local | `greek-independence-day` | inline | ✓ | CY→CY | · |
| Cyprus National Day · `fixed-apr-01-cy` | local | `cyprus-national-day` | inline | ✓ | CY→CY | · |
| Cyprus Independence Day · `fixed-oct-01-cy` | local | `cyprus-independence-day` | inline | ✓ | CY→CY | · |
| Ochi Day · `fixed-oct-28-cy` | local | `ochi-day` | inline | ✓ | CY→CY | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | CY→CY | ·→y |
| Epiphany · `Epiphany` | ← europe-common | `epiphany` | inline | ✓ | CY→CY | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | CY→CY | · |
| Dormition of the Mother of God · `Assumption of Mary` | ← europe-common | `dormition-of-the-mother-of-god` | inline | ✓ | CY→CY | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | CY→CY | · |
| Second Day of Christmas · `Boxing Day` | ← europe-common | `second-day-of-christmas` | inline | ✓ | CY→CY | · |
| Green Monday · `Orthodox Clean Monday` | ← christian-orthodox | `green-monday` | inline | ✓ | CY→CY | · |
| Orthodox Good Friday · `Orthodox Good Friday` | ← christian-orthodox | `orthodox-good-friday` | inline | ✓ | CY→CY | · |
| Orthodox Holy Saturday · `Orthodox Holy Saturday` | ← christian-orthodox | `orthodox-holy-saturday` | inline | ✓ | CY→CY | · |
| Orthodox Easter Sunday · `Orthodox Easter Sunday` | ← christian-orthodox | `orthodox-easter-sunday` | inline | ✓ | CY→CY | · |
| Orthodox Easter Monday · `Orthodox Easter Monday` | ← christian-orthodox | `orthodox-easter-monday` | inline | ✓ | CY→CY | · |
| Holy Spirit Monday · `Orthodox Pentecost Monday` | ← christian-orthodox | `holy-spirit-monday` | inline | ✓ | CY→CY | · |

### `region-cz.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-cz.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Victory Day · `fixed-may-08-cz` | local | `victory-day` | inline | ✓ | CZ→CZ | · |
| Saints Cyril and Methodius Day · `fixed-jul-05-cz` | local | `saints-cyril-and-methodius-day` | inline | ✓ | CZ→CZ | · |
| Jan Hus Day · `fixed-jul-06-cz` | local | `jan-hus-day` | inline | ✓ | CZ→CZ | · |
| Statehood Day · `fixed-sep-28-cz` | local | `statehood-day` | inline | ✓ | CZ→CZ | · |
| Independent Czechoslovak State Day · `fixed-oct-28-cz` | local | `independent-czechoslovak-state-day` | inline | ✓ | CZ→CZ | · |
| Struggle for Freedom and Democracy Day · `fixed-nov-17-cz` | local | `struggle-for-freedom-and-democracy-day` | inline | ✓ | CZ→CZ | · |
| Restoration Day of the Independent Czech State · `New Year's Day` | ← europe-common | `restoration-day-of-the-independent-czech-state` | inline | ✓ | CZ→CZ | ·→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | CZ→CZ | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | CZ→CZ | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | CZ→CZ | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | CZ→CZ | · |
| Christmas Eve · `Christmas Eve` | ← europe-common | `christmas-eve` | ← christian-western | ✓ | CZ→CZ | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | CZ→CZ | · |
| Saint Stephen's Day · `Boxing Day` | ← europe-common | `saint-stephens-day` | inline | ✓ | CZ→CZ | · |

### `region-de.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-de.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| German Unity Day · `fixed-oct-03-de` | local | `german-unity-day` | inline | ✓ | DE→DE | · |
| Epiphany · `fixed-jan-06-de-bw` | local | `epiphany` | inline | ✓ | DE-BW,DE-BY,DE-ST→DE ⚠ | · |
| International Women's Day · `fixed-mar-08-de-be` | local | `womens-day` | inline | ✓ ⊕ | DE-BE,DE-MV→DE | · |
| Corpus Christi · `offset-easter-sunday+60-de-bw` | local | `corpus-christi` | ← christian-western | ✓ | DE-BW,DE-BY,DE-HE,DE-NW,DE-RP,DE-SL→DE ⚠ | · |
| Assumption of Mary · `fixed-aug-15-de-by` | local | `assumption-of-mary` | inline | ✓ | DE-BY,DE-SL→DE ⚠ | · |
| Reformation Day · `fixed-oct-31-de-bb` | local | `reformation-day` | inline | ✓ | DE-BB,DE-HB,DE-HH,DE-MV,DE-NI,DE-SH,DE-SN,DE-ST,DE-TH→DE ⚠ | · |
| All Saints' Day · `fixed-nov-01-de-bw` | local | `all-saints-day` | ← christian-western | ✓ | DE-BW,DE-BY,DE-NW,DE-RP,DE-SL→DE ⚠ | · |
| Repentance Day · `weekday-wed-onorbefore-nov-22-de-sn` | local | `repentance-day` | inline | ✓ | DE-SN→DE ⚠ | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | DE→DE | · |
| Valentine's Day · `Valentine's Day` | ← europe-common | `valentines-day` | inline | ✓ | DE→DE | · |
| International Workers' Day · `International Workers' Day` | ← europe-common | `workers-day` | ← global-core | ✓ | DE→DE | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | DE→DE | · |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | DE→DE | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | DE→DE | · |
| Ascension Day · `Ascension Day` | ← europe-common | `ascension-day` | ← christian-western | ✓ | DE→DE | · |
| Whit Monday · `Whit Monday` | ← europe-common | `whit-monday` | ← christian-western | ✓ | DE→DE | · |
| Christmas Eve · `Christmas Eve` | ← europe-common | — | — | ✗ **DROP** | DE→— | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | DE→DE | · |
| Second Day of Christmas · `Boxing Day` | ← europe-common | `second-day-of-christmas` | inline | ✓ | DE→DE | · |
| Mother's Day · `Mother's Day` | ← europe-common | `mothers-day` | inline | ✓ | DE→DE | · |
| Father's Day · `Father's Day` | ← europe-common | — | — | ✗ **DROP** | DE→— | · |
| International Women's Day · `International Women's Day` | ← global-all | `womens-day` | inline | ✓ ⊕ | DE→DE | · |

### `region-dk.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-dk.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Maundy Thursday · `offset-easter-sunday-3-dk` | local | `maundy-thursday` | ← christian-western | ✓ | DK→DK | · |
| Constitution Day · `fixed-jun-05-dk` | local | `constitution-day` | inline | ✓ | DK→DK | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | DK→DK | ·→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | DK→DK | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | DK→DK | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | DK→DK | · |
| Ascension Day · `Ascension Day` | ← europe-common | `ascension-day` | ← christian-western | ✓ | DK→DK | · |
| Whit Sunday · `Pentecost Sunday` | ← europe-common | `whit-sunday` | ← christian-western | ✓ | DK→DK | · |
| Whit Monday · `Whit Monday` | ← europe-common | `whit-monday` | ← christian-western | ✓ | DK→DK | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | DK→DK | · |
| Second Day of Christmas · `Boxing Day` | ← europe-common | `second-day-of-christmas` | inline | ✓ | DK→DK | · |

### `region-ee.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-ee.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Independence Day · `fixed-feb-24-ee` | local | `independence-day` | inline | ✓ | EE→EE | · |
| Victory Day · `fixed-jun-23-ee` | local | `victory-day` | inline | ✓ | EE→EE | · |
| Midsummer Day · `fixed-jun-24-ee` | local | `midsummer-day` | inline | ✓ | EE→EE | · |
| Day of Restoration of Independence · `fixed-aug-20-ee` | local | `day-of-restoration-of-independence` | inline | ✓ | EE→EE | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | EE→EE | ·→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | EE→EE | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | EE→EE | · |
| Spring Day · `International Workers' Day` | ← europe-common | `spring-day` | inline | ✓ | EE→EE | · |
| Whit Sunday · `Pentecost Sunday` | ← europe-common | `whit-sunday` | ← christian-western | ✓ | EE→EE | · |
| Christmas Eve · `Christmas Eve` | ← europe-common | `christmas-eve` | ← christian-western | ✓ | EE→EE | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | EE→EE | · |
| Second Day of Christmas · `Boxing Day` | ← europe-common | `second-day-of-christmas` | inline | ✓ | EE→EE | · |

### `region-es.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-es.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| National Day of Spain · `fixed-oct-12-es` | local | `national-day-of-spain` | inline | ✓ | ES→ES | · |
| Constitution Day · `fixed-dec-06-es` | local | `constitution-day` | inline | ✓ | ES→ES | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | ES→ES | ·→y |
| Epiphany · `Epiphany` | ← europe-common | `epiphany` | inline | ✓ | ES→ES | · |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | ES→ES | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | ES→ES | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | ES→ES | · |
| Assumption of Mary · `Assumption of Mary` | ← europe-common | `assumption-of-mary` | inline | ✓ | ES→ES | · |
| All Saints' Day · `All Saints' Day` | ← europe-common | `all-saints-day` | ← christian-western | ✓ | ES→ES | · |
| Immaculate Conception · `Immaculate Conception` | ← europe-common | `immaculate-conception` | inline | ✓ | ES→ES | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | ES→ES | · |

### `region-fi.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-fi.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Midsummer Day · `weekday-sat-onorafter-jun-20-fi` | local | `midsummer-day` | inline | ✓ | FI→FI | · |
| All Saints' Day · `weekday-sat-onorafter-oct-31-fi` | local | `all-saints-day` | inline | ✓ | FI→FI | · |
| Independence Day · `fixed-dec-06-fi` | local | `independence-day` | inline | ✓ | FI→FI | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | FI→FI | ·→y |
| Epiphany · `Epiphany` | ← europe-common | `epiphany` | inline | ✓ | FI→FI | · |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | FI→FI | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | FI→FI | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | FI→FI | · |
| May Day · `International Workers' Day` | ← europe-common | `may-day` | inline | ✓ | FI→FI | · |
| Ascension Day · `Ascension Day` | ← europe-common | `ascension-day` | ← christian-western | ✓ | FI→FI | · |
| Whit Sunday · `Pentecost Sunday` | ← europe-common | `whit-sunday` | ← christian-western | ✓ | FI→FI | · |
| Christmas Eve · `Christmas Eve` | ← europe-common | `christmas-eve` | ← christian-western | ✓ | FI→FI | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | FI→FI | · |
| Saint Stephen's Day · `Boxing Day` | ← europe-common | `saint-stephens-day` | inline | ✓ | FI→FI | · |

### `region-fr.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-fr.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Fête du Travail · `fixed-may-01-fr` | local | `labour-day` | inline | ✓ | FR→FR | · |
| Victory in Europe Day · `fixed-may-08-fr` | local | `victory-in-europe-day` | inline | ✓ | FR→FR | · |
| Bastille Day · `fixed-jul-14-fr` | local | `bastille-day` | inline | ✓ | FR→FR | · |
| Assumption of Mary · `fixed-aug-15-fr` | local | `assumption-of-mary` | inline | ✓ | FR→FR | · |
| All Saints' Day · `fixed-nov-01-fr` | local | `all-saints-day` | ← christian-western | ✓ | FR→FR | · |
| Armistice Day · `fixed-nov-11-fr` | local | `armistice-day` | inline | ✓ | FR→FR | · |
| Mother's Day · `weekday-last-sun-may-fr` | local | `mothers-day` | inline | ✓ | FR→FR | · |
| World Music Day · `fixed-jun-21-fr` | local | `world-music-day` | inline | ✓ | FR→FR | · |
| Good Friday (Alsace-Moselle) · `offset-easter-sunday-2-fr-67` | local | — | — | ✗ **DROP** | FR-57,FR-67,FR-68→— | · |
| Saint Stephen's Day · `fixed-dec-26-fr-67` | local | `saint-stephens-day` | inline | ✓ | FR-57,FR-67,FR-68→FR ⚠ | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | FR→FR | · |
| Valentine's Day · `Valentine's Day` | ← europe-common | `valentines-day` | inline | ✓ | FR→FR | · |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | FR→FR | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | FR→FR | · |
| Ascension Day · `Ascension Day` | ← europe-common | `ascension-day` | ← christian-western | ✓ | FR→FR | · |
| Pentecost Sunday · `Pentecost Sunday` | ← europe-common | — | — | ✗ **DROP** | FR→— | · |
| Whit Monday · `Whit Monday` | ← europe-common | `whit-monday` | ← christian-western | ✓ | FR→FR | · |
| Christmas Eve · `Christmas Eve` | ← europe-common | — | — | ✗ **DROP** | FR→— | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | FR→FR | · |
| Father's Day · `Father's Day` | ← europe-common | — | — | ✗ **DROP** | FR→— | · |
| International Women's Day · `International Women's Day` | ← global-all | `womens-day` | inline | ✓ | FR→FR | · |
| April Fool's Day · `April Fool's Day` | ← global-all | — | — | ✗ **DROP** | FR→— | · |

### `region-gb.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-gb.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| New Year's Day · `fixed-jan-01-weekend-roll-gb` | local | `new-years-day` | ← global-core | ✓ | GB→GB | y→y |
| Mothering Sunday · `offset-easter-sunday-21-gb` | local | `mothering-sunday` | inline | ✓ | GB→GB | · |
| Early May Bank Holiday · `weekday-1st-mon-may-gb` | local | `early-may-bank-holiday` | inline | ✓ | GB→GB | · |
| Spring Bank Holiday · `weekday-last-mon-may-gb` | local | `spring-bank-holiday` | inline | ✓ | GB→GB | · |
| Bonfire Night · `fixed-nov-05-gb` | local | `bonfire-night` | inline | ✓ | GB→GB | · |
| Remembrance Sunday · `weekday-2nd-sun-nov-gb` | local | `remembrance-sunday` | inline | ✓ | GB→GB | · |
| Boxing Day · `fixed-dec-26-weekend-roll-gb` | local | `boxing-day` | ← christian-western | ✓ | GB→GB | y→y |
| Saint George's Day · `fixed-apr-23-gb-eng` | local | `saint-georges-day` | inline | ✓ | GB-ENG→GB-ENG | · |
| Saint David's Day · `fixed-mar-01-gb-wls` | local | `saint-davids-day` | inline | ✓ | GB-WLS→GB-WLS | · |
| 2 January · `fixed-jan-02-weekend-roll-gb-sct` | local | `day-after-new-years-day` | inline | ✓ | GB-SCT→GB-SCT | y→y |
| Burns Night · `fixed-jan-25-gb-sct` | local | `burns-night` | inline | ✓ | GB-SCT→GB-SCT | · |
| Summer Bank Holiday (Scotland) · `weekday-1st-mon-aug-gb-sct` | local | `summer-bank-holiday-scotland` | inline | ✓ | GB-SCT→GB-SCT | · |
| Saint Andrew's Day · `fixed-nov-30-weekend-roll-gb-sct` | local | `saint-andrews-day` | inline | ✓ | GB-SCT→GB-SCT | y→y |
| Saint Patrick's Day · `fixed-mar-17-weekend-roll-gb-nir` | local | `saint-patricks-day` | inline | ✓ | GB-NIR→GB-NIR | y→y |
| Battle of the Boyne · `fixed-jul-12-weekend-roll-gb-nir` | local | `battle-of-the-boyne` | inline | ✓ | GB-NIR→GB-NIR | y→y |
| Summer Bank Holiday · `weekday-last-mon-aug-gb-eng` | local | `summer-bank-holiday` | inline | ✓ | GB-ENG,GB-NIR,GB-WLS→GB-ENG,GB-NIR,GB-WLS | · |
| Valentine's Day · `Valentine's Day` | ← europe-common | `valentines-day` | inline | ✓ | GB→GB | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | GB→GB | · |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | GB→GB | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | inline | ✓ | GB→GB-ENG,GB-NIR,GB-WLS ⚠ | · |
| Christmas Eve · `Christmas Eve` | ← europe-common | — | — | ✗ **DROP** | GB→— | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | GB→GB | y→y |
| Father's Day · `Father's Day` | ← europe-common | `fathers-day` | inline | ✓ | GB→GB | · |
| International Women's Day · `International Women's Day` | ← global-all | `womens-day` | inline | ✓ | GB→GB | · |
| April Fool's Day · `April Fool's Day` | ← global-all | — | — | ✗ **DROP** | GB→— | · |
| Halloween · `Halloween` | ← global-all | `halloween` | inline | ✓ | GB→GB | · |
| Remembrance Day · `Remembrance Day` | ← global-all | — | — | ✗ **DROP** | GB→— | · |

### `region-gr.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-gr.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Independence Day · `fixed-mar-25-gr` | local | `independence-day` | inline | ✓ | GR→GR | · |
| Ochi Day · `fixed-oct-28-gr` | local | `ochi-day` | inline | ✓ | GR→GR | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | GR→GR | ·→y |
| Epiphany · `Epiphany` | ← europe-common | `epiphany` | inline | ✓ | GR→GR | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | GR→GR | · |
| Dormition of the Mother of God · `Assumption of Mary` | ← europe-common | `dormition-of-the-mother-of-god` | inline | ✓ | GR→GR | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | GR→GR | · |
| Synaxis of the Mother of God · `Boxing Day` | ← europe-common | `synaxis-of-the-mother-of-god` | inline | ✓ | GR→GR | · |
| Orthodox Clean Monday · `Orthodox Clean Monday` | ← christian-orthodox | `orthodox-clean-monday` | inline | ✓ | GR→GR | · |
| Orthodox Good Friday · `Orthodox Good Friday` | ← christian-orthodox | `orthodox-good-friday` | inline | ✓ | GR→GR | · |
| Orthodox Holy Saturday · `Orthodox Holy Saturday` | ← christian-orthodox | `orthodox-holy-saturday` | inline | ✓ | GR→GR | · |
| Orthodox Easter Sunday · `Orthodox Easter Sunday` | ← christian-orthodox | `orthodox-easter-sunday` | inline | ✓ | GR→GR | · |
| Orthodox Easter Monday · `Orthodox Easter Monday` | ← christian-orthodox | `orthodox-easter-monday` | inline | ✓ | GR→GR | · |
| Holy Spirit Monday · `Orthodox Pentecost Monday` | ← christian-orthodox | `holy-spirit-monday` | inline | ✓ | GR→GR | · |

### `region-hr.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-hr.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Statehood Day · `fixed-may-30-hr` | local | `statehood-day` | inline | ✓ | HR→HR | · |
| Anti-Fascist Struggle Day · `fixed-jun-22-hr` | local | `anti-fascist-struggle-day` | inline | ✓ | HR→HR | · |
| Victory and Homeland Thanksgiving Day · `fixed-aug-05-hr` | local | `victory-and-homeland-thanksgiving-day` | inline | ✓ | HR→HR | · |
| Remembrance Day · `fixed-nov-18-hr` | local | `remembrance-day` | inline | ✓ | HR→HR | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | HR→HR | ·→y |
| Epiphany · `Epiphany` | ← europe-common | `epiphany` | inline | ✓ | HR→HR | · |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | HR→HR | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | HR→HR | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | HR→HR | · |
| Corpus Christi · `Corpus Christi` | ← europe-common | `corpus-christi` | ← christian-western | ✓ | HR→HR | · |
| Assumption of Mary · `Assumption of Mary` | ← europe-common | `assumption-of-mary` | inline | ✓ | HR→HR | · |
| All Saints' Day · `All Saints' Day` | ← europe-common | `all-saints-day` | ← christian-western | ✓ | HR→HR | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | HR→HR | · |
| Saint Stephen's Day · `Boxing Day` | ← europe-common | `saint-stephens-day` | inline | ✓ | HR→HR | · |

### `region-hu.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-hu.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| 1848 Revolution Memorial Day · `fixed-mar-15-hu` | local | `revolution-memorial-day-1848` | inline | ✓ | HU→HU | · |
| State Foundation Day · `fixed-aug-20-hu` | local | `state-foundation-day` | inline | ✓ | HU→HU | · |
| 1956 Revolution Memorial Day · `fixed-oct-23-hu` | local | `revolution-memorial-day-1956` | inline | ✓ | HU→HU | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | HU→HU | ·→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | HU→HU | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | HU→HU | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | HU→HU | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | HU→HU | · |
| Whit Sunday · `Pentecost Sunday` | ← europe-common | `whit-sunday` | ← christian-western | ✓ | HU→HU | · |
| Whit Monday · `Whit Monday` | ← europe-common | `whit-monday` | ← christian-western | ✓ | HU→HU | · |
| All Saints' Day · `All Saints' Day` | ← europe-common | `all-saints-day` | ← christian-western | ✓ | HU→HU | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | HU→HU | · |
| Second Day of Christmas · `Boxing Day` | ← europe-common | `second-day-of-christmas` | inline | ✓ | HU→HU | · |

### `region-ie.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-ie.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Saint Brigid's Day · `weekday-1st-mon-feb-ie` | local | `saint-brigids-day` | inline | ✓ | IE→IE | · |
| Saint Patrick's Day · `fixed-mar-17-ie` | local | `saint-patricks-day` | inline | ✓ | IE→IE | y→y |
| May Day · `weekday-1st-mon-may-ie` | local | `may-day` | inline | ✓ | IE→IE | · |
| June Bank Holiday · `weekday-1st-mon-jun-ie` | local | `june-bank-holiday` | inline | ✓ | IE→IE | · |
| August Bank Holiday · `weekday-1st-mon-aug-ie` | local | `august-bank-holiday` | inline | ✓ | IE→IE | · |
| October Bank Holiday · `weekday-last-mon-oct-ie` | local | `october-bank-holiday` | inline | ✓ | IE→IE | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | IE→IE | y→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | IE→IE | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | IE→IE | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | IE→IE | y→y |
| Saint Stephen's Day · `Boxing Day` | ← europe-common | `saint-stephens-day` | inline | ✓ | IE→IE | y→y |

### `region-it.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-it.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Liberation Day · `fixed-apr-25-it` | local | `liberation-day` | inline | ✓ | IT→IT | · |
| Republic Day · `fixed-jun-02-it` | local | `republic-day` | inline | ✓ | IT→IT | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | IT→IT | ·→y |
| Epiphany · `Epiphany` | ← europe-common | `epiphany` | inline | ✓ | IT→IT | · |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | IT→IT | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | IT→IT | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | IT→IT | · |
| Assumption of Mary (Ferragosto) · `Assumption of Mary` | ← europe-common | `assumption-of-mary-ferragosto` | inline | ✓ | IT→IT | · |
| All Saints' Day · `All Saints' Day` | ← europe-common | `all-saints-day` | ← christian-western | ✓ | IT→IT | · |
| Immaculate Conception · `Immaculate Conception` | ← europe-common | `immaculate-conception` | inline | ✓ | IT→IT | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | IT→IT | · |
| Saint Stephen's Day · `Boxing Day` | ← europe-common | `saint-stephens-day` | inline | ✓ | IT→IT | · |

### `region-lt.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-lt.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Day of Restoration of the State · `fixed-feb-16-lt` | local | `day-of-restoration-of-the-state` | inline | ✓ | LT→LT | · |
| Day of Restoration of Independence · `fixed-mar-11-lt` | local | `day-of-restoration-of-independence` | inline | ✓ | LT→LT | · |
| Saint John's Day · `fixed-jun-24-lt` | local | `saint-johns-day` | inline | ✓ | LT→LT | · |
| Statehood Day · `fixed-jul-06-lt` | local | `statehood-day` | inline | ✓ | LT→LT | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | LT→LT | ·→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | LT→LT | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | LT→LT | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | LT→LT | · |
| Assumption of Mary · `Assumption of Mary` | ← europe-common | `assumption-of-mary` | inline | ✓ | LT→LT | · |
| All Saints' Day · `All Saints' Day` | ← europe-common | `all-saints-day` | ← christian-western | ✓ | LT→LT | · |
| All Souls' Day · `All Souls' Day` | ← europe-common | `all-souls-day` | inline | ✓ | LT→LT | · |
| Christmas Eve · `Christmas Eve` | ← europe-common | `christmas-eve` | ← christian-western | ✓ | LT→LT | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | LT→LT | · |
| Second Day of Christmas · `Boxing Day` | ← europe-common | `second-day-of-christmas` | inline | ✓ | LT→LT | · |

### `region-lu.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-lu.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Europe Day · `fixed-may-09-lu` | local | `europe-day` | inline | ✓ | LU→LU | · |
| National Day · `fixed-jun-23-lu` | local | `national-day` | inline | ✓ | LU→LU | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | LU→LU | ·→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | LU→LU | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | LU→LU | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | LU→LU | · |
| Ascension Day · `Ascension Day` | ← europe-common | `ascension-day` | ← christian-western | ✓ | LU→LU | · |
| Whit Monday · `Whit Monday` | ← europe-common | `whit-monday` | ← christian-western | ✓ | LU→LU | · |
| Assumption of Mary · `Assumption of Mary` | ← europe-common | `assumption-of-mary` | inline | ✓ | LU→LU | · |
| All Saints' Day · `All Saints' Day` | ← europe-common | `all-saints-day` | ← christian-western | ✓ | LU→LU | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | LU→LU | · |
| Saint Stephen's Day · `Boxing Day` | ← europe-common | `saint-stephens-day` | inline | ✓ | LU→LU | · |

### `region-lv.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-lv.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Restoration of Independence Day · `fixed-may-04-lv` | local | `restoration-of-independence-day` | inline | ✓ | LV→LV | · |
| Midsummer Eve · `fixed-jun-23-lv` | local | `midsummer-eve` | inline | ✓ | LV→LV | · |
| Saint John's Day · `fixed-jun-24-lv` | local | `saint-johns-day` | inline | ✓ | LV→LV | · |
| Proclamation Day of the Republic of Latvia · `fixed-nov-18-lv` | local | `proclamation-day-of-the-republic-of-latvia` | inline | ✓ | LV→LV | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | LV→LV | ·→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | LV→LV | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | LV→LV | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | LV→LV | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | LV→LV | · |
| Christmas Eve · `Christmas Eve` | ← europe-common | `christmas-eve` | ← christian-western | ✓ | LV→LV | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | LV→LV | · |
| Second Day of Christmas · `Boxing Day` | ← europe-common | `second-day-of-christmas` | inline | ✓ | LV→LV | · |
| New Year's Eve · `New Year's Eve` | ← europe-common | `new-years-eve` | ← global-core | ✓ | LV→LV | · |

### `region-mt.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-mt.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Feast of Saint Paul's Shipwreck · `fixed-feb-10-mt` | local | `feast-of-saint-pauls-shipwreck` | inline | ✓ | MT→MT | · |
| Feast of Saint Joseph · `fixed-mar-19-mt` | local | `feast-of-saint-joseph` | inline | ✓ | MT→MT | · |
| Freedom Day · `fixed-mar-31-mt` | local | `freedom-day` | inline | ✓ | MT→MT | · |
| Sette Giugno · `fixed-jun-07-mt` | local | `sette-giugno` | inline | ✓ | MT→MT | · |
| Feast of Saint Peter and Saint Paul · `fixed-jun-29-mt` | local | `feast-of-saint-peter-and-saint-paul` | inline | ✓ | MT→MT | · |
| Victory Day · `fixed-sep-08-mt` | local | `victory-day` | inline | ✓ | MT→MT | · |
| Independence Day · `fixed-sep-21-mt` | local | `independence-day` | inline | ✓ | MT→MT | · |
| Republic Day · `fixed-dec-13-mt` | local | `republic-day` | inline | ✓ | MT→MT | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | MT→MT | ·→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | MT→MT | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | MT→MT | · |
| Worker's Day · `International Workers' Day` | ← europe-common | `workers-day` | ← global-core | ✓ | MT→MT | · |
| Feast of the Assumption (Santa Marija) · `Assumption of Mary` | ← europe-common | `feast-of-the-assumption-santa-marija` | inline | ✓ | MT→MT | · |
| Immaculate Conception · `Immaculate Conception` | ← europe-common | `immaculate-conception` | inline | ✓ | MT→MT | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | MT→MT | · |

### `region-nl.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-nl.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| King's Day · `fixed-apr-27-nl` | local | `kings-day` | inline | ✓ | NL→NL | y→y |
| Liberation Day · `fixed-may-05-nl` | local | `liberation-day` | inline | ✓ | NL→NL | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | NL→NL | ·→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | NL→NL | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | NL→NL | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | NL→NL | · |
| Ascension Day · `Ascension Day` | ← europe-common | `ascension-day` | ← christian-western | ✓ | NL→NL | · |
| Whit Sunday · `Pentecost Sunday` | ← europe-common | `whit-sunday` | ← christian-western | ✓ | NL→NL | · |
| Whit Monday · `Whit Monday` | ← europe-common | `whit-monday` | ← christian-western | ✓ | NL→NL | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | NL→NL | · |
| Second Day of Christmas · `Boxing Day` | ← europe-common | `second-day-of-christmas` | inline | ✓ | NL→NL | · |

### `region-pl.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-pl.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Constitution Day · `fixed-may-03-pl` | local | `constitution-day` | inline | ✓ | PL→PL | · |
| Independence Day · `fixed-nov-11-pl` | local | `independence-day` | inline | ✓ | PL→PL | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | PL→PL | ·→y |
| Epiphany · `Epiphany` | ← europe-common | `epiphany` | inline | ✓ | PL→PL | · |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | PL→PL | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | PL→PL | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | PL→PL | · |
| Whit Sunday · `Pentecost Sunday` | ← europe-common | `whit-sunday` | ← christian-western | ✓ | PL→PL | · |
| Corpus Christi · `Corpus Christi` | ← europe-common | `corpus-christi` | ← christian-western | ✓ | PL→PL | · |
| Assumption of Mary · `Assumption of Mary` | ← europe-common | `assumption-of-mary` | inline | ✓ | PL→PL | · |
| All Saints' Day · `All Saints' Day` | ← europe-common | `all-saints-day` | ← christian-western | ✓ | PL→PL | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | PL→PL | · |
| Second Day of Christmas · `Boxing Day` | ← europe-common | `second-day-of-christmas` | inline | ✓ | PL→PL | · |

### `region-pt.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-pt.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Freedom Day · `fixed-apr-25-pt` | local | `freedom-day` | inline | ✓ | PT→PT | · |
| Portugal Day · `fixed-jun-10-pt` | local | `portugal-day` | inline | ✓ | PT→PT | · |
| Republic Day · `fixed-oct-05-pt` | local | `republic-day` | inline | ✓ | PT→PT | · |
| Restoration of Independence · `fixed-dec-01-pt` | local | `restoration-of-independence` | inline | ✓ | PT→PT | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | PT→PT | ·→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | PT→PT | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | PT→PT | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | PT→PT | · |
| Corpus Christi · `Corpus Christi` | ← europe-common | `corpus-christi` | ← christian-western | ✓ | PT→PT | · |
| Assumption of Mary · `Assumption of Mary` | ← europe-common | `assumption-of-mary` | inline | ✓ | PT→PT | · |
| All Saints' Day · `All Saints' Day` | ← europe-common | `all-saints-day` | ← christian-western | ✓ | PT→PT | · |
| Immaculate Conception · `Immaculate Conception` | ← europe-common | `immaculate-conception` | inline | ✓ | PT→PT | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | PT→PT | · |

### `region-ro.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-ro.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Day after New Year's Day · `fixed-jan-02-ro` | local | `day-after-new-years-day` | inline | ✓ | RO→RO | · |
| Union Day · `fixed-jan-24-ro` | local | `union-day` | inline | ✓ | RO→RO | · |
| Children's Day · `fixed-jun-01-ro` | local | `childrens-day` | inline | ✓ | RO→RO | · |
| Saint Andrew's Day · `fixed-nov-30-ro` | local | `saint-andrews-day` | inline | ✓ | RO→RO | · |
| Great Union Day · `fixed-dec-01-ro` | local | `great-union-day` | inline | ✓ | RO→RO | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | RO→RO | ·→y |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | RO→RO | · |
| Dormition of the Mother of God · `Assumption of Mary` | ← europe-common | `dormition-of-the-mother-of-god` | inline | ✓ | RO→RO | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | RO→RO | · |
| Second Day of Christmas · `Boxing Day` | ← europe-common | `second-day-of-christmas` | inline | ✓ | RO→RO | · |
| Good Friday · `Orthodox Good Friday` | ← christian-orthodox | `good-friday` | inline | ✓ | RO→RO | · |
| Orthodox Easter Sunday · `Orthodox Easter Sunday` | ← christian-orthodox | `orthodox-easter-sunday` | inline | ✓ | RO→RO | · |
| Orthodox Easter Monday · `Orthodox Easter Monday` | ← christian-orthodox | `orthodox-easter-monday` | inline | ✓ | RO→RO | · |
| Pentecost · `Orthodox Pentecost` | ← christian-orthodox | `pentecost` | inline | ✓ | RO→RO | · |
| Pentecost Monday · `Orthodox Pentecost Monday` | ← christian-orthodox | `pentecost-monday` | inline | ✓ | RO→RO | · |

### `region-se.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-se.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Midsummer Day · `weekday-sat-onorafter-jun-20-se` | local | `midsummer-day` | inline | ✓ | SE→SE | · |
| All Saints' Day · `weekday-sat-onorafter-oct-31-se` | local | `all-saints-day` | inline | ✓ | SE→SE | · |
| National Day of Sweden · `fixed-jun-06-se` | local | `national-day-of-sweden` | inline | ✓ | SE→SE | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | SE→SE | ·→y |
| Epiphany · `Epiphany` | ← europe-common | `epiphany` | inline | ✓ | SE→SE | · |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | SE→SE | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | SE→SE | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | SE→SE | · |
| May Day · `International Workers' Day` | ← europe-common | `may-day` | inline | ✓ | SE→SE | · |
| Ascension Day · `Ascension Day` | ← europe-common | `ascension-day` | ← christian-western | ✓ | SE→SE | · |
| Whit Sunday · `Pentecost Sunday` | ← europe-common | `whit-sunday` | ← christian-western | ✓ | SE→SE | · |
| Christmas Eve · `Christmas Eve` | ← europe-common | `christmas-eve` | ← christian-western | ✓ | SE→SE | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | SE→SE | · |
| Second Day of Christmas · `Boxing Day` | ← europe-common | `second-day-of-christmas` | inline | ✓ | SE→SE | · |
| New Year's Eve · `New Year's Eve` | ← europe-common | `new-years-eve` | ← global-core | ✓ | SE→SE | · |

### `region-si.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-si.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| New Year Holiday · `fixed-jan-02-si` | local | `new-year-holiday` | inline | ✓ | SI→SI | · |
| Prešeren Day · `fixed-feb-08-si` | local | `pre-eren-day` | inline | ✓ | SI→SI | · |
| Day of Uprising Against Occupation · `fixed-apr-27-si` | local | `day-of-uprising-against-occupation` | inline | ✓ | SI→SI | · |
| Labour Day Holiday · `fixed-may-02-si` | local | `labour-day-holiday` | inline | ✓ | SI→SI | · |
| Statehood Day · `fixed-jun-25-si` | local | `statehood-day` | inline | ✓ | SI→SI | · |
| Reformation Day · `fixed-oct-31-si` | local | `reformation-day` | inline | ✓ | SI→SI | · |
| Independence and Unity Day · `fixed-dec-26-si` | local | `independence-and-unity-day` | inline | ✓ | SI→SI | · |
| New Year's Day · `New Year's Day` | ← europe-common | `new-years-day` | ← global-core | ✓ | SI→SI | ·→y |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | SI→SI | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | SI→SI | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | SI→SI | · |
| Whit Sunday · `Pentecost Sunday` | ← europe-common | `whit-sunday` | ← christian-western | ✓ | SI→SI | · |
| Assumption of Mary · `Assumption of Mary` | ← europe-common | `assumption-of-mary` | inline | ✓ | SI→SI | · |
| Day of Remembrance of the Dead · `All Saints' Day` | ← europe-common | `day-of-remembrance-of-the-dead` | inline | ✓ | SI→SI | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | SI→SI | · |

### `region-sk.xml`  →  `Bodu.Globalization.Calendar2.Data.Europe/src/Resources/region-sk.xml`

| v1 entry (concept · rule) | source | v2 id | loc | status | territory v1→v2 | adj |
|---|---|---|---|---|---|---|
| Day of Our Lady of Sorrows · `fixed-sep-15-sk` | local | `day-of-our-lady-of-sorrows` | inline | ✓ | SK→SK | · |
| Day of Victory over Fascism · `fixed-may-08-sk` | local | `day-of-victory-over-fascism` | inline | ✓ | SK→SK | · |
| Saints Cyril and Methodius Day · `fixed-jul-05-sk` | local | `saints-cyril-and-methodius-day` | inline | ✓ | SK→SK | · |
| Slovak National Uprising Anniversary · `fixed-aug-29-sk` | local | `slovak-national-uprising-anniversary` | inline | ✓ | SK→SK | · |
| Constitution Day · `fixed-sep-01-sk` | local | `constitution-day` | inline | ✓ | SK→SK | · |
| Struggle for Freedom and Democracy Day · `fixed-nov-17-sk` | local | `struggle-for-freedom-and-democracy-day` | inline | ✓ | SK→SK | · |
| Day of the Establishment of the Slovak Republic · `New Year's Day` | ← europe-common | `day-of-the-establishment-of-the-slovak-republic` | inline | ✓ | SK→SK | ·→y |
| Epiphany · `Epiphany` | ← europe-common | `epiphany` | inline | ✓ | SK→SK | · |
| Easter Sunday · `Easter Sunday` | ← europe-common | `easter-sunday` | ← christian-western | ✓ | SK→SK | · |
| Good Friday · `Good Friday` | ← europe-common | `good-friday` | ← christian-western | ✓ | SK→SK | · |
| Easter Monday · `Easter Monday` | ← europe-common | `easter-monday` | ← christian-western | ✓ | SK→SK | · |
| Labour Day · `International Workers' Day` | ← europe-common | `labour-day` | inline | ✓ | SK→SK | · |
| All Saints' Day · `All Saints' Day` | ← europe-common | `all-saints-day` | ← christian-western | ✓ | SK→SK | · |
| Christmas Eve · `Christmas Eve` | ← europe-common | `christmas-eve` | ← christian-western | ✓ | SK→SK | · |
| Christmas Day · `Christmas Day` | ← europe-common | `christmas-day` | ← christian-western | ✓ | SK→SK | · |
| Saint Stephen's Day · `Boxing Day` | ← europe-common | `saint-stephens-day` | inline | ✓ | SK→SK | · |

---

## Catalogue cross-reference


### `christian-gregorian.xml` → `christian-western.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Epiphany · `fixed-jan-06` | — | ✗ **DROP** |
| Candlemas · `fixed-feb-02` | — | ✗ **DROP** |
| Annunciation · `fixed-mar-25` | — | ✗ **DROP** |
| Easter Sunday · `algo-easter-sunday` | `easter-sunday` | ✓ |
| Shrove Tuesday · `offset-easter-sunday-47` | — | ✗ **DROP** |
| Ash Wednesday · `offset-easter-sunday-46` | — | ✗ **DROP** |
| Palm Sunday · `offset-easter-sunday-7` | — | ✗ **DROP** |
| Maundy Thursday · `offset-easter-sunday-3` | `maundy-thursday` | ✓ |
| Good Friday · `offset-easter-sunday-2` | `good-friday` | ✓ |
| Holy Saturday · `offset-easter-sunday-1` | — | ✗ **DROP** |
| Easter Monday · `offset-easter-sunday+1` | `easter-monday` | ✓ |
| Ascension Day · `offset-easter-sunday+39` | `ascension-day` | ✓ |
| Pentecost Sunday · `offset-easter-sunday+49` | `whit-sunday` | ↪ rename |
| Whit Monday · `offset-easter-sunday+50` | `whit-monday` | ✓ |
| Trinity Sunday · `offset-easter-sunday+56` | — | ✗ **DROP** |
| Corpus Christi · `offset-easter-sunday+60` | `corpus-christi` | ✓ |
| All Saints' Day · `fixed-nov-01` | `all-saints-day` | ✓ |
| All Souls' Day · `fixed-nov-02` | — | ✗ **DROP** |
| Christmas Eve · `fixed-dec-24` | `christmas-eve` | ✓ |
| Christmas Day · `fixed-dec-25-weekend-roll` | `christmas-day` | ✓ |
| Boxing Day · `fixed-dec-26` | `boxing-day` | ✓ |

### `christian-orthodox.xml` → `christian-orthodox.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Orthodox Christmas Eve · `fixed-jan-06` | `orthodox-christmas-eve` | ✓ |
| Orthodox Christmas Day · `fixed-jan-07` | `orthodox-christmas-day` | ✓ |
| Orthodox New Year · `fixed-jan-14` | `orthodox-new-year` | ✓ |
| Orthodox Epiphany · `fixed-jan-19` | `orthodox-epiphany` | ✓ |
| Orthodox Easter Sunday · `algo-orthodox-easter-sunday` | `orthodox-easter-sunday` | ✓ |
| Orthodox Clean Monday · `offset-orthodox-easter-sunday-48` | `orthodox-clean-monday` | ✓ |
| Orthodox Lazarus Saturday · `offset-orthodox-easter-sunday-8` | `orthodox-lazarus-saturday` | ✓ |
| Orthodox Palm Sunday · `offset-orthodox-easter-sunday-7` | `orthodox-palm-sunday` | ✓ |
| Orthodox Holy Thursday · `offset-orthodox-easter-sunday-3` | `orthodox-holy-thursday` | ✓ |
| Orthodox Good Friday · `offset-orthodox-easter-sunday-2` | `orthodox-good-friday` | ✓ |
| Orthodox Holy Saturday · `offset-orthodox-easter-sunday-1` | `orthodox-holy-saturday` | ✓ |
| Orthodox Bright Week · `offset-orthodox-easter-sunday+0` | `orthodox-bright-week` | ✓ |
| Orthodox Easter Monday · `offset-orthodox-easter-sunday+1` | `orthodox-easter-monday` | ✓ |
| Orthodox Ascension Day · `offset-orthodox-easter-sunday+39` | `orthodox-ascension-day` | ✓ |
| Orthodox Pentecost · `offset-orthodox-easter-sunday+49` | `orthodox-pentecost` | ✓ |
| Orthodox Pentecost Monday · `offset-orthodox-easter-sunday+50` | `orthodox-pentecost-monday` | ✓ |

### `default-minimal.xml` → `default-minimal.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| New Year's Day · `fixed-jan-01-weekend-roll` | `new-years-day` | ✓ |

### `global-all.xml` → `global-all.xml`  — aggregator (no own entries)


### `global-anchors.xml` → `global-anchors.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Lunar New Year · `fixed-lunisolar-01-01` | `lunar-new-year` | ✓ |
| Ramadan Start · `fixed-hijri-09-01` | `ramadan-start` | ✓ |
| Orthodox Easter · `algo-orthodox-easter` | `orthodox-easter` | ✓ |

### `global-animals.xml` → `global-animals.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| World Wildlife Day · `fixed-mar-03` | `world-wildlife-day` | ✓ |
| World Bee Day · `fixed-may-20` | `world-bee-day` | ✓ |
| International Tiger Day · `fixed-jul-29` | `international-tiger-day` | ✓ |
| International Cat Day · `fixed-aug-08` | `international-cat-day` | ✓ |
| World Elephant Day · `fixed-aug-12` | `world-elephant-day` | ✓ |
| International Dog Day · `fixed-aug-26` | `international-dog-day` | ✓ |
| World Animal Day · `fixed-oct-04` | `world-animal-day` | ✓ |

### `global-buddhist.xml` → `global-buddhist.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Parinirvana Day · `fixed-feb-15` | `parinirvana-day` | ✓ |
| Losar · `algo-losar` | `losar` | ✓ |
| Vesak · `algo-vesak` | `vesak` | ✓ |
| Asalha Puja · `algo-asalha-puja` | `asalha-puja` | ✓ |
| Vassa · `offset-asalha-puja+1` | `vassa` | ✓ |
| Bodhi Day · `fixed-dec-08` | `bodhi-day` | ✓ |

### `global-core.xml` → `global-core.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| New Year's Day · `fixed-jan-01-weekend-roll` | `new-years-day` | ✓ |
| International Workers' Day · `fixed-may-01` | `workers-day` | ✓ |
| New Year's Eve · `fixed-dec-31` | `new-years-eve` | ✓ |

### `global-cultural.xml` → `global-cultural.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Valentine's Day · `fixed-feb-14` | `valentines-day` | ✓ |
| International Mother Language Day · `fixed-feb-21` | `international-mother-language-day` | ✓ |
| World Poetry Day · `fixed-mar-21` | `world-poetry-day` | ✓ |
| World Theatre Day · `fixed-mar-27` | `world-theatre-day` | ✓ |
| April Fool's Day · `fixed-apr-01` | `april-fools-day` | ✓ |
| International Jazz Day · `fixed-apr-30` | `international-jazz-day` | ✓ |
| Star Wars Day · `fixed-may-04` | `star-wars-day` | ✓ |
| World Music Day · `fixed-jun-21` | `world-music-day` | ✓ |
| International Friendship Day · `fixed-jul-30` | `international-friendship-day` | ✓ |
| Halloween · `fixed-oct-31` | `halloween` | ✓ |

### `global-education.xml` → `global-education.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| International Day of Education · `fixed-jan-24` | `international-day-of-education` | ✓ |
| World Book and Copyright Day · `fixed-apr-23` | `world-book-and-copyright-day` | ✓ |
| International Literacy Day · `fixed-sep-08` | `international-literacy-day` | ✓ |
| World Teachers' Day · `fixed-oct-05` | `world-teachers-day` | ✓ |
| Safer Internet Day · `weekday-2nd-tue-feb` | `safer-internet-day` | ✓ |

### `global-environment.xml` → `global-environment.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| World Wetlands Day · `fixed-feb-02` | `world-wetlands-day` | ✓ |
| International Day of Forests · `fixed-mar-21` | `international-day-of-forests` | ✓ |
| World Water Day · `fixed-mar-22` | `world-water-day` | ✓ |
| Earth Hour · `weekday-last-sat-mar` | `earth-hour` | ✓ |
| Earth Day · `fixed-apr-22` | `earth-day` | ✓ |
| International Day for Biological Diversity · `fixed-may-22` | `international-day-for-biological-diversity` | ✓ |
| World Environment Day · `fixed-jun-05` | `world-environment-day` | ✓ |
| World Oceans Day · `fixed-jun-08` | `world-oceans-day` | ✓ |
| World Day to Combat Desertification and Drought · `fixed-jun-17` | `world-day-to-combat-desertification-and-drought` | ✓ |
| World Soil Day · `fixed-dec-05` | `world-soil-day` | ✓ |
| Plastic Free July · `fixed-jul-01` | `plastic-free-july` | ✓ |
| Clean Up the World Weekend · `weekday-3rd-fri-sep` | `clean-up-the-world-weekend` | ✓ |

### `global-family-social.xml` → `global-family-social.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Friendship Day · `weekday-1st-sun-aug` | `friendship-day` | ✓ |
| Grandparents Day · `weekday-1st-sun-sep` | `grandparents-day` | ✓ |

### `global-family.xml` → `global-family.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Mother's Day · `weekday-2nd-sun-may` | `mothers-day` | ✓ |
| Father's Day · `weekday-3rd-sun-jun` | `fathers-day` | ✓ |

### `global-food.xml` → `global-food.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| World Pulses Day · `fixed-feb-10` | `world-pulses-day` | ✓ |
| International Tea Day · `fixed-may-21` | `international-tea-day` | ✓ |
| World Food Safety Day · `fixed-jun-07` | `world-food-safety-day` | ✓ |
| International Beer Day · `weekday-1st-fri-aug` | `international-beer-day` | ✓ |
| International Coffee Day · `fixed-oct-01` | `international-coffee-day` | ✓ |
| World Vegetarian Day · `fixed-oct-01` | `world-vegetarian-day` | ✓ |
| World Food Day · `fixed-oct-16` | `world-food-day` | ✓ |
| World Vegan Day · `fixed-nov-01` | `world-vegan-day` | ✓ |

### `global-health.xml` → `global-health.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| World Cancer Day · `fixed-feb-04` | `world-cancer-day` | ✓ |
| World Autism Awareness Day · `fixed-apr-02` | `world-autism-awareness-day` | ✓ |
| World Health Day · `fixed-apr-07` | `world-health-day` | ✓ |
| World No Tobacco Day · `fixed-may-31` | `world-no-tobacco-day` | ✓ |
| World Blood Donor Day · `fixed-jun-14` | `world-blood-donor-day` | ✓ |
| World Hepatitis Day · `fixed-jul-28` | `world-hepatitis-day` | ✓ |
| World Suicide Prevention Day · `fixed-sep-10` | `world-suicide-prevention-day` | ✓ |
| World Mental Health Day · `fixed-oct-10` | `world-mental-health-day` | ✓ |
| World Diabetes Day · `fixed-nov-14` | `world-diabetes-day` | ✓ |
| World AIDS Day · `fixed-dec-01` | `world-aids-day` | ✓ |
| Breast Cancer Awareness Month · `fixed-oct-01` | `breast-cancer-awareness-month` | ✓ |
| Movember · `fixed-nov-01` | `movember` | ✓ |

### `global-hindu.xml` → `global-hindu.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Makar Sankranti · `fixed-jan-14` | `makar-sankranti` | ✓ |
| Pongal · `fixed-jan-14` | `pongal` | ✓ |
| Saraswati Puja · `algo-saraswati-puja` | `saraswati-puja` | ✓ |
| Maha Shivaratri · `algo-maha-shivaratri` | `maha-shivaratri` | ✓ |
| Holi · `algo-holi` | `holi` | ✓ |
| Ram Navami · `algo-ram-navami` | `ram-navami` | ✓ |
| Raksha Bandhan · `algo-raksha-bandhan` | `raksha-bandhan` | ✓ |
| Janmashtami · `algo-janmashtami` | `janmashtami` | ✓ |
| Ganesh Chaturthi · `algo-ganesh-chaturthi` | `ganesh-chaturthi` | ✓ |
| Onam · `algo-onam` | — | ✗ **DROP** |
| Navaratri · `algo-navaratri` | `navaratri` | ✓ |
| Dussehra · `algo-dussehra` | `dussehra` | ✓ |
| Karva Chauth · `algo-karva-chauth` | `karva-chauth` | ✓ |
| Diwali · `algo-diwali` | `diwali` | ✓ |

### `global-islamic-umm-al-qura.xml` → `global-islamic-umm-al-qura.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Ramadan · `fixed-uaq-09-01` | `ramadan` | ✓ |
| Eid al-Fitr · `fixed-uaq-10-01` | `eid-al-fitr` | ✓ |
| Eid al-Adha · `fixed-uaq-12-10` | `eid-al-adha` | ✓ |
| Islamic New Year · `fixed-uaq-01-01` | `islamic-new-year` | ✓ |
| Day of Ashura · `fixed-uaq-01-10` | `day-of-ashura` | ✓ |
| Mawlid al-Nabi · `fixed-uaq-03-12` | `mawlid-al-nabi` | ✓ |

### `global-islamic.xml` → `global-islamic.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Ramadan · `fixed-hijri-09-01` | `ramadan` | ✓ |
| Eid al-Fitr · `fixed-hijri-10-01` | `eid-al-fitr` | ✓ |
| Eid al-Adha · `fixed-hijri-12-10` | `eid-al-adha` | ✓ |
| Islamic New Year · `fixed-hijri-01-01` | `islamic-new-year` | ✓ |
| Day of Ashura · `fixed-hijri-01-10` | `day-of-ashura` | ✓ |
| Mawlid al-Nabi · `fixed-hijri-03-12` | `mawlid-al-nabi` | ✓ |

### `global-jewish.xml` → `global-jewish.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Purim · `fixed-hebrew-last-adar-14` | `purim` | ✓ |
| Passover · `fixed-hebrew-nisan-15` | `passover` | ✓ |
| Shavuot · `offset-passover+50` | `shavuot` | ✓ |
| Rosh Hashanah · `fixed-hebrew-tishri-01` | `rosh-hashanah` | ✓ |
| Yom Kippur · `offset-rosh-hashanah+9` | `yom-kippur` | ✓ |
| Sukkot · `offset-rosh-hashanah+14` | `sukkot` | ✓ |
| Hanukkah · `fixed-hebrew-kislev-25` | `hanukkah` | ✓ |

### `global-lunar.xml` → `global-lunar.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Lunar New Year · `fixed-lunisolar-01-01` | `lunar-new-year` | ✓ |
| Lantern Festival · `offset-lunar-new-year+14` | `lantern-festival` | ✓ |
| Qingming Festival · `algo-qingming` | `qingming-festival` | ✓ |
| Dragon Boat Festival · `fixed-lunisolar-05-05` | `dragon-boat-festival` | ✓ |
| Qixi Festival · `fixed-lunisolar-07-07` | `qixi-festival` | ✓ |
| Hungry Ghost Festival · `fixed-lunisolar-07-15` | `hungry-ghost-festival` | ✓ |
| Mid-Autumn Festival · `fixed-lunisolar-08-15` | `mid-autumn-festival` | ✓ |
| Double Ninth Festival · `fixed-lunisolar-09-09` | `double-ninth-festival` | ✓ |
| Winter Solstice Festival · `fixed-dec-22` | `winter-solstice-festival` | ✓ |

### `global-multiday-normalization.xml` → `global-multiday-normalization.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| NAIDOC Week · `weekday-1st-sun-jul-au` | `naidoc-week` | ✓ |
| World Space Week · `fixed-oct-04` | `world-space-week` | ✓ |

### `global-persian.xml` → `global-persian.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| Nowruz · `fixed-persian-01-01` | `nowruz` | ✓ |
| Sizdah Bedar · `fixed-persian-01-13` | `sizdah-bedar` | ✓ |
| Yalda Night · `fixed-persian-09-30` | `yalda-night` | ✓ |

### `global-remembrance.xml` → `global-remembrance.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| International Holocaust Remembrance Day · `fixed-jan-27` | `international-holocaust-remembrance-day` | ✓ |
| International Day for the Elimination of Racial Discrimination · `fixed-mar-21` | `international-day-for-the-elimination-of-racial-discrimination` | ✓ |
| International Day for the Remembrance of the Slave Trade and its Abolition · `fixed-aug-23` | `international-day-for-the-remembrance-of-the-slave-trade-and-its-abolition` | ✓ |
| Remembrance Day · `fixed-nov-11` | `remembrance-day` | ✓ |

### `global-science.xml` → `global-science.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| International Day of Women and Girls in Science · `fixed-feb-11` | `international-day-of-women-and-girls-in-science` | ✓ |
| International Day of Mathematics · `fixed-mar-14` | `international-day-of-mathematics` | ✓ |
| World Meteorological Day · `fixed-mar-23` | `world-meteorological-day` | ✓ |
| World Intellectual Property Day · `fixed-apr-26` | `world-intellectual-property-day` | ✓ |
| International Day of Light · `fixed-may-16` | `international-day-of-light` | ✓ |
| Pi Day · `fixed-mar-14` | `pi-day` | ✓ |
| World Space Week · `fixed-oct-04` | `world-space-week` | ✓ |
| World Science Day for Peace and Development · `fixed-nov-10` | `world-science-day-for-peace-and-development` | ✓ |

### `global-social.xml` → `global-social.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| World Day of Social Justice · `fixed-feb-20` | `world-day-of-social-justice` | ✓ |
| International Day of Families · `fixed-may-15` | `international-day-of-families` | ✓ |
| International Day Against Homophobia, Biphobia and Transphobia · `fixed-may-17` | `international-day-against-homophobia-biphobia-and-transphobia` | ✓ |
| Pride Month · `fixed-jun-01` | `pride-month` | ✓ |
| World Day Against Child Labour · `fixed-jun-12` | `world-day-against-child-labour` | ✓ |
| World Refugee Day · `fixed-jun-20` | `world-refugee-day` | ✓ |
| Nelson Mandela International Day · `fixed-jul-18` | `nelson-mandela-international-day` | ✓ |
| International Day of Charity · `fixed-sep-05` | `international-day-of-charity` | ✓ |
| International Day of Older Persons · `fixed-oct-01` | `international-day-of-older-persons` | ✓ |
| International Day of the Girl Child · `fixed-oct-11` | `international-day-of-the-girl-child` | ✓ |
| World Kindness Day · `fixed-nov-13` | `world-kindness-day` | ✓ |
| International Men's Day · `fixed-nov-19` | `international-mens-day` | ✓ |
| Universal Children's Day · `fixed-nov-20` | `universal-childrens-day` | ✓ |
| International Day of Persons with Disabilities · `fixed-dec-03` | `international-day-of-persons-with-disabilities` | ✓ |

### `global-un.xml` → `global-un.xml`

| v1 concept · rule | v2 id | status |
|---|---|---|
| International Women's Day · `fixed-mar-08` | `international-womens-day` | ✓ |
| International Day of Happiness · `fixed-mar-20` | `international-day-of-happiness` | ✓ |
| International Youth Day · `fixed-aug-12` | `international-youth-day` | ✓ |
| International Day of Peace · `fixed-sep-21` | `international-day-of-peace` | ✓ |
| United Nations Day · `fixed-oct-24` | `united-nations-day` | ✓ |
| Human Rights Day · `fixed-dec-10` | `human-rights-day` | ✓ |
| World Interfaith Harmony Week · `fixed-feb-01` | `world-interfaith-harmony-week` | ✓ |
| World Press Freedom Day · `fixed-may-03` | `world-press-freedom-day` | ✓ |
| International Day of Democracy · `fixed-sep-15` | `international-day-of-democracy` | ✓ |
| World Habitat Day · `weekday-1st-mon-oct` | `world-habitat-day` | ✓ |