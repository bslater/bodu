---
title: Globalization & Calendars — Concepts
---

# Globalization & Calendars — Concepts

The packages in this topic share one vocabulary. This page defines the terms that cross package boundaries — the words you will meet whether you are consuming a regional data pack, authoring rules with the Builder, or extending the runtime with plugins. The runtime's own [core concepts page](../calendar/concepts.md) covers each term in full depth; this page is the topic-level orientation.

## Rule, document, resource

A **rule document** is authored text — XML or JSON on the notable-date schema (`urn:bodu:globalization:calendar`). It declares notable-date **definitions** (a concept such as "New Year's Day": an id, display name, default category), each carrying one or more **rules** — calculation recipes with an applicability window, exactly one resolution strategy, and optional adjustment-policy references. <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader> turns a document into a **resource** (<xref:Bodu.Globalization.Calendar.NotableDateResource>): the immutable, fully validated value a <xref:Bodu.Globalization.Calendar.NotableDateService> is built over. Documents are mutable text; resources are immutable values — runtime change always means loading a new resource and swapping it in.

## Territory and regional composition

A <xref:Bodu.Globalization.Calendar.TerritoryCode> is an ISO 3166 country code with an optional subdivision — `AU`, `AU-NSW`, `GB-ENG`, `US-CA`. Territory codes are **hierarchical**: a country contains all of its subdivisions, so a query for `AU-NSW` returns rules authored at `AU` *and* at `AU-NSW`. This containment is what lets national and regional rules compose without duplication — the data packs author national rules once at the country level and add subdivision-specific variants only where law or custom actually differs.

## Observance adjustment

A rule's strategy computes a **nominal date** — 25 December for Christmas Day. An **adjustment policy** (<xref:Bodu.Globalization.Calendar.AdjustmentPolicy>) is a reusable, named post-resolution shift that can move it to an **observed date** — the substitute Monday when the nominal date lands on a weekend. A policy pairs a trigger (*when it fires* — weekend, non-working day, specific weekday) with an action (*what it does* — move to the next working day, add days, suppress), and rules reference policies by id. Resolved occurrences record whether they were adjusted (`IsObserved`) and by which policy, so consumers can distinguish the gazetted day from the day off work.

## Date-calculation algorithm

Most rules are calendar formulas — a fixed month and day, or the *n*th weekday in a month. Dates that cannot be expressed that way (Easter Sunday, Vesak, Diwali, Qingming, the equinoxes) are computed by a named **algorithm** referenced from the rule via `<Algorithm key="…">`. Built-in keys (`western-easter`, `orthodox-easter`, `vesak`, `losar`, `matariki`, the Hindu-festival keys, …) are backed by bundled calculators; custom algorithms implement <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> and register through a <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry>.

## Data pack

A **data pack** is a companion assembly of curated, embedded rule documents — one per country, national rules plus ISO 3166-2 subdivisions — fronted by a static `<Region>CalendarData` factory (`SupportedCountries`, `LoadResource(territory)`, `CreateService(territory)`). Packs do not duplicate shared concepts: each country document **imports** from the bundled **common catalogues** (`global-core`, `christian-western`, `global-islamic`, `global-hindu`, and friends, resolved by <xref:Bodu.Globalization.Calendar.CommonNotableDateResources>), re-scoping each imported concept to its territory and attaching its local adjustment policies. Five packs ship — Americas, Asia-Pacific, Europe, Africa, Middle East — each an independent NuGet package.

## Trust policy (Plugins)

The `Bodu.Globalization.Calendar.Plugins` package loads custom `INotableDateAlgorithm` implementations from *external* assemblies. Loading is **trust-gated and default-deny**: an assembly is admitted only when it satisfies an explicit trust policy (strong-name, file-hash, composite, or delegating), so untrusted or user-writable locations are rejected rather than executed, and an unavailable algorithm surfaces as a clear failure rather than silently resolving. Applications that register algorithms in-process, or consume only the built-in algorithms and the data packs, never need this package.

## Resolved date

A **resolved date** (<xref:Bodu.Globalization.Calendar.NotableDate>) is the year-specific concrete output of one rule for one occurrence: the emitted `Date` (observed, after any adjustment), the calculated `ActualDate` (nominal), `IsObserved`, display name, category, territory, free-form tags, optional multi-day span, and the `IsNonWorkingDay` flag. That last flag matters across the topic — not every notable date is a closure. Mother's Day is notable but working; working-day arithmetic skips only occurrences with `IsNonWorkingDay = true`, combined with the configured working week (a `Bodu.Core` `WeekPattern`, default Monday–Friday).

## Common catalogue

A **common catalogue** is a bundled, importable rule set — `global-core`, `christian-western`, `christian-orthodox`, `global-islamic`, `global-jewish`, `global-hindu`, `global-buddhist`, and friends — that documents pull from via `<Imports>` instead of redefining shared concepts. An import can take every concept or cherry-pick with `<Use>` directives that rename, re-scope to a territory, override the category, or attach adjustment policies; local concepts win over imported concepts of the same id. The regional data packs are built exactly this way, which is why a date like Easter Sunday is defined once and observed, with different adjustments, in dozens of territories. The [notable-date catalogue](../../guides/calendar/catalogue/index.md) documents what each catalogue contains.

## Runtime change

A resource is immutable, so *runtime* rule changes are modelled as load-and-swap rather than mutation: a <xref:Bodu.Globalization.Calendar.MutableNotableDateResourceProvider> holds the current resource and `Reload(...)` replaces it, while a <xref:Bodu.Globalization.Calendar.ReloadableNotableDateService> reads the provider per query and rebuilds atomically. The DependencyInjection companion exposes the same workflow as `AddReloadableNotableDateService(...)`. The swap is the only mutation seam in the topic — everything downstream of the loader stays immutable.

## How the companions compose

Every companion depends on the runtime and attaches at one seam of the pipeline:

| Companion | Seam | What it contributes |
|---|---|---|
| `…Calendar.Builder` | Authoring (before load) | <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder> constructs documents fluently in C#, serializes to XML / JSON, and materializes a resource. |
| `…Calendar.<Region>` data packs | Loading | Curated documents plus the load-and-import wiring, behind a `<Region>CalendarData` factory. |
| `…Calendar.Plugins` | Resolution (algorithms) | Trust-gated discovery of external algorithm assemblies. |
| `…Calendar.DependencyInjection` | Hosting | `services.AddNotableDateService(resource)` and the reloadable runtime-swap registration. |

The runtime never depends on a companion; the composition is strictly one-directional, which is why each piece can be adopted — or skipped — independently.

## Category vs. tag

Two classification axes run through the whole family. <xref:Bodu.Globalization.Calendar.NotableDateCategory> is the well-known enum — `PublicHoliday`, `BankHoliday`, `Observance`, `Remembrance`, `Cultural`, `Religious`, and friends — and every resolved date carries exactly one value; use it for coarse filtering. **Tags** are free-form strings authored on the rule and surfaced on the resolved date for fine-grained, application-specific filtering. <xref:Bodu.Globalization.Calendar.NotableDateFilter> composes over both: `ForCategory(...)` and `WithTag(...)` combine with `And` / `Or` / `Not`.

## Going deeper

| Topic | Where the full treatment lives |
|---|---|
| The complete runtime vocabulary — nominal vs. observed, imports vs. overrides, collisions, categories vs. tags, working days | [Bodu.Globalization.Calendar — Core concepts](../calendar/concepts.md) |
| The pipeline, headline types, and scenarios | [Bodu.Globalization.Calendar introduction](../calendar/index.md) |
| Topic overview and the package decision table | [Globalization & Calendars](globalization-and-calendars.md) |
| Runnable minimal samples | [Getting started](../calendar/getting-started.md) |
| Hands-on walk-throughs | [Globalization & Calendars guides](../../guides/topics/globalization-and-calendars.md) |
