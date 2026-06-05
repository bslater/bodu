---
uid: Bodu.Globalization.Calendar.Data
---

# Bodu.Globalization.Calendar.Data

## Purpose

The **Bodu.Globalization.Calendar.Data.*** companion packages ship curated public-holiday resources for national and subdivision territories, authored on the v2 cookbook schema and built on the bundled common catalogues. They live in three independently versioned assemblies — **Americas**, **AsiaPacific**, and **Europe** — all exposing types in the `Bodu.Globalization.Calendar.Data` namespace, so a consumer pulls in only the regions it needs.

Each pack loads a country's embedded `region-<cc>.xml`, resolves its `<Imports>` against the common catalogues (Europe additionally serves its own `europe-common` hub), validates, and hands back a <xref:Bodu.Globalization.Calendar.NotableDateResource> — or a <xref:Bodu.Globalization.Calendar.NotableDateService> pre-wired over it.

## Static documentation

- **[Calendar data packs](~/guides/calendar/data-packs.md)** — per-pack install commands, territory coverage, and composition patterns.
- **[Territories and regional composition](~/guides/calendar/territories.md)** — country / subdivision containment and how regional rules compose.
- **[Notable-date catalogue](~/guides/calendar/catalogue/index.md)** — the generated list of which dates each region ships.

## Key types

Three static factories, identical in shape:

- <xref:Bodu.Globalization.Calendar.Data.AmericasCalendarData> — `SupportedCountries` = `CA`, `US`.
- <xref:Bodu.Globalization.Calendar.Data.AsiaPacificCalendarData> — `SupportedCountries` = `AU`, `CN`, `IN`, `JP`, `KR`, `MY`, `NZ`, `SG`.
- <xref:Bodu.Globalization.Calendar.Data.EuropeCalendarData> — 28 EU/EEA territories (`AT`, `BE`, `BG`, `CY`, `CZ`, `DE`, `DK`, `EE`, `ES`, `FI`, `FR`, `GB`, `GR`, `HR`, `HU`, `IE`, `IT`, `LT`, `LU`, `LV`, `MT`, `NL`, `PL`, `PT`, `RO`, `SE`, `SI`, `SK`).

Each exposes:

- `static IReadOnlyList<string> SupportedCountries { get; }` — the countries the pack carries.
- `static NotableDateResource LoadResource(string territory)` — load the resource for a country or one of its subdivisions (e.g. `"US"`, `"CA-ON"`, `"AU-WA"`, `"GB-SCT"`). Throws `ArgumentException` when the country is not in the pack.
- `static NotableDateService CreateService(string territory)` — equivalent to `new NotableDateService(LoadResource(territory))`.

A subdivision argument selects its country's resource; the full territory is honoured when you query, so national and subdivision rules compose.

## Minimal sample

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Data.Americas;
using Bodu.Globalization.Calendar.Data.AsiaPacific;

// A service for United States federal + state coverage:
NotableDateService us = AmericasCalendarData.CreateService("US");
IReadOnlyList<NotableDate> y2026 = us.Resolve(2026, "US");

// Or just the resource, e.g. to register through DI or compose with custom collaborators:
NotableDateResource au = AsiaPacificCalendarData.LoadResource("AU-WA");
```
