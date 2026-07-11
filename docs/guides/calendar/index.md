---
title: Bodu.Globalization.Calendar guides
---

# Bodu.Globalization.Calendar guides

Recipe-style walk-throughs for **Bodu.Globalization.Calendar**, organized by namespace.

Part of the **[Globalization & Calendars](../topics/globalization-and-calendars.md)** topic.

If you are new to the library, start with the [introduction](../../docs/calendar/index.md), the [Core concepts](../../docs/calendar/concepts.md) glossary, and the [getting-started page](../../docs/calendar/getting-started.md). For the auto-generated API reference, see the [Bodu.Globalization.Calendar namespace page](xref:Bodu.Globalization.Calendar).

> **Looking for the data?** The [Notable-date catalogue](catalogue/index.md) lists what notable dates the calendar resources include and how regions and territories differ — generated from the XML, organized by theme and by region, with a cross-region comparison matrix.

## How the library works

![NotableDateService resolution pipeline](../../images/diagrams/calendar-resolution-pipeline.svg)

A **rule document** is authored on the notable-date schema and loaded into an immutable **`NotableDateResource`**. A **`NotableDateService`** is built over that resource; for each requested date, range, or year it resolves every applicable rule via the rule's **strategy**, runs the referenced **adjustment policies**, settles same-day **collisions**, and returns the resolved **`NotableDate`** set.

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| `Bodu.Globalization.Calendar` | Service, resource/definition/rule model, loader, adjustment policies, validation — the resolution pipeline. | [Using NotableDateService](notable-dates.md) · [Authoring notable date rules](rule-authoring.md) · [Territories and regional composition](territories.md) |
| `Bodu.Globalization.Calendar.Algorithms` | The date-calculation strategies, the `<Algorithm key="…">` keys and bundled calculators, and the `INotableDateAlgorithm` / `NotableDateAlgorithmRegistry` custom-algorithm contract. | [Date calculation algorithms](algorithms.md) |
| `Bodu.Globalization.Calendar.RangeResolution` | Duplicate / collision / priority / observed-date policies on `ResolutionPolicy`. | [The resolution pipeline](resolution-pipeline.md) · [Identity, priority, observed dates](identity-and-resolution.md) |
| `Bodu.Globalization.Calendar.Plugins` | Trust-gated loading of external algorithm assemblies — `NotableDatePluginLoader`, `IPluginTrustPolicy`, and the deny-by-default trust policies. | [Building and extending the service — Plugin system](building-the-service.md#plugin-system) |
| `Bodu.Extensions` | Working-day arithmetic over `DateOnly`, `DateTime`, and `DateTimeOffset` — `IsWorkingDay`, `NextWorkingDay`, `AddWorkingDays`, … | [Working-day arithmetic](working-days.md) |
| `Bodu.Globalization.Calendar.Data` | Region-specific public-holiday resources shipped in the `Bodu.Globalization.Calendar.Americas`, `.Europe`, and `.AsiaPacific` companion packages. | [Calendar data packs](data-packs.md) |
| `Bodu.Globalization.Calendar` (DI) | `IServiceCollection.AddNotableDateService(...)` / `AddReloadableNotableDateService(...)` from the DI companion package. | [Calendar dependency injection](dependency-injection.md) |
| `Bodu.Globalization.Calendar.Builder` | Fluent C# authoring of notable-date documents — `NotableDateDocumentBuilder`, XML / JSON serialization, and load/save. | [Authoring with the notable-date builder](notable-date-builder.md) |

## Guides

### `Bodu.Globalization.Calendar` — Service

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="notable-dates.md">Using NotableDateService</a></h3>
  <p>The main entry point — loading a resource, resolving notable dates for a date, range, or year, filtering by territory and category, the reloadable runtime-swap workflow, and working-day arithmetic over <code>DateOnly</code> / <code>DateTime</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="rule-authoring.md">Authoring notable date rules</a></h3>
  <p>How to author your own rule documents in XML / JSON — definitions, rules, strategies, importing the bundled common catalogues with <code>&lt;Use&gt;</code> directives, and layering ID-targeted <code>&lt;Overrides&gt;</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="notable-date-builder.md">Authoring with the notable-date builder</a></h3>
  <p>The fluent C# peer of XML / JSON authoring — <code>NotableDateDocumentBuilder</code> assembles definitions, rules, adjustment policies, imports, and overrides, then serializes to XML / JSON, saves to a file, or builds a <code>NotableDateResource</code>.</p>
</div>

</div>

### `Bodu.Globalization.Calendar` — Reference

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="strategy-reference.md">Notable-date rule strategies</a></h3>
  <p>The full catalogue of occurrence sources — every single-date strategy (fixed, positional, weekday, reference-based, business-day, algorithm), the recurrence sources, and fixed vs. calculated durations — with a choosing guide and a common-scenarios cookbook.</p>
</div>

<div class="bodu-card">
  <h3><a href="rule-reference.md">NotableDateRule and adjustment-policy reference</a></h3>
  <p>Authoritative element-by-element reference for the rule document — every strategy element, the applicability window, and the reusable <code>&lt;AdjustmentPolicy&gt;</code> shape — with worked examples.</p>
</div>

<div class="bodu-card">
  <h3><a href="territories.md">Territories and regional composition</a></h3>
  <p>How <code>TerritoryCode</code> works — ISO 3166 country / subdivision codes, parsing, containment semantics (<code>AU</code> ⊇ <code>AU-NSW</code>), authoring rules with territory scope, and composing national and regional rules.</p>
</div>

<div class="bodu-card">
  <h3><a href="adjustment-rules.md">Observance adjustment rules</a></h3>
  <p>Nominal date vs. observed date; the full trigger and action catalogues — every <code>AdjustmentTrigger</code> and <code>AdjustmentAction</code> value, emission modes, real-world weekend-substitution patterns, and custom trigger / action handlers.</p>
</div>

<div class="bodu-card">
  <h3><a href="resolution-pipeline.md">The resolution pipeline</a></h3>
  <p>Walkthrough of the load and query stages — parse, import resolution, override application, validation; then strategy resolution, adjustment evaluation, collision settlement, and emission — with a concrete worked trace.</p>
</div>

</div>

### `Bodu.Globalization.Calendar` — Patterns

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="holiday-patterns.md">Holiday patterns and examples</a></h3>
  <p>End-to-end examples for fixed-date holidays, weekend substitution policies (AU/NZ, UK, US), floating weekday-of-month holidays, Easter clusters, lunar and algorithmic dates, multi-day events, and subdivision-level variants.</p>
</div>

<div class="bodu-card">
  <h3><a href="working-days.md">Working-day arithmetic</a></h3>
  <p>The <code>Bodu.Extensions</code> surface over <code>DateOnly</code> / <code>DateTime</code> / <code>DateTimeOffset</code> — <code>IsWorkingDay</code>, <code>AddWorkingDays</code>, <code>WorkingDaysBetween</code>, <code>NextWorkingDay</code>, snap operations, fiscal-period helpers, and the <code>WeekPattern</code> working week.</p>
</div>

<div class="bodu-card">
  <h3><a href="building-the-service.md">Building and extending the service</a></h3>
  <p>Composing the service with collaborators — <code>NotableDateAlgorithmRegistry</code>, adjustment handler / trigger registries, <code>INotableDateCollisionResolver</code>, <code>INotableDateNameLocalizer</code>, <code>INotableDateProvider</code>, the reloadable provider, and the trust-gated plugin system.</p>
</div>

<div class="bodu-card">
  <h3><a href="dependency-injection.md">Calendar dependency injection</a></h3>
  <p>The <code>Bodu.Globalization.Calendar.DependencyInjection</code> companion package — <code>services.AddNotableDateService(...)</code>, the resource-factory overload, and <code>AddReloadableNotableDateService(...)</code> for the runtime-swap workflow.</p>
</div>

</div>

### `Bodu.Globalization.Calendar.Algorithms`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="algorithms.md">Date calculation algorithms</a></h3>
  <p>The six resolution strategies, the built-in <code>&lt;Algorithm key="…"&gt;</code> keys — Easter (Gregorian / Orthodox), equinoxes, Qingming, Vesak, Losar, Matariki, Hindu festivals — and a custom-algorithm walk-through.</p>
</div>

</div>

### `Bodu.Globalization.Calendar.Data.*` — Data packs

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="data-packs.md">Calendar data packs</a></h3>
  <p>The official <code>Bodu.Globalization.Calendar.Data.*</code> companion assemblies — Americas, Europe, and Asia-Pacific — their <code>CreateService</code> / <code>LoadResource</code> factories, and territory coverage.</p>
</div>

</div>

## Where to go next

- **[Runnable samples](../../samples/calendar.md)** — offline sample projects under `samples/Globalization.Calendar/` composing the data packs, working-day arithmetic, the builder, DI, and custom algorithms end to end.
- [Bodu.Globalization.Calendar introduction](../../docs/calendar/index.md) — mental model, headline types, scenarios.
- [Core concepts](../../docs/calendar/concepts.md) — vocabulary used throughout these guides.
- [Bodu.Globalization.Calendar getting started](../../docs/calendar/getting-started.md) — install and minimal samples.
- [Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar) — full namespace overview.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
