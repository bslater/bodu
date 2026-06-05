---
title: Bodu.Globalization.Calendar guides
---

# Bodu.Globalization.Calendar guides

Recipe-style walk-throughs for **Bodu.Globalization.Calendar**, organized by namespace.

If you are new to the library, start with the [introduction](../../docs/calendar/index.md), the [Core concepts](../../docs/calendar/concepts.md) glossary, and the [getting-started page](../../docs/calendar/getting-started.md). For the auto-generated API reference, see the [Bodu.Globalization.Calendar namespace page](xref:Bodu.Globalization.Calendar).

> **Looking for the data?** The [Notable-date catalogue](catalogue/index.md) lists what notable dates the **v2** calendar resources include and how regions and territories differ — generated from the XML, organized by theme and by region, with a cross-region comparison matrix.

## How the library works

![NotableDateService resolution pipeline](../../images/diagrams/calendar-resolution-pipeline.svg)

A **`NotableDateRule`** is an authored recipe — strategy, category, territory, adjustments, duration. **`NotableDateService`** loads rules from one or more providers, layers optional override providers, resolves each rule for the requested year via the calculator, runs the adjustment pipeline, and caches the resolved set per year in a thread-safe dictionary.

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| `Bodu.Globalization.Calendar` | Service, rules, results, providers, adjustments, registries, parsers — the resolution pipeline. | [Using NotableDateService](notable-dates.md) · [Authoring notable date rules](rule-authoring.md) · [Territories and regional composition](territories.md) |
| `Bodu.Globalization.Calendar.Algorithms` | Built-in date calculators — `EasterSundayNotableDateAlgorithm`, `HinduLunarNotableDateAlgorithm`, `LosarNotableDateAlgorithm`, `VesakNotableDateAlgorithm`, `AsalhaPujaNotableDateAlgorithm`, `QingmingNotableDateAlgorithm`. | [Date calculation algorithms](algorithms.md) |
| `Bodu.Globalization.Calendar.Providers` | Bundled `EasterSundayNotableDateProviderBase` implementations — `GregorianEasterSundayNotableDateProvider`, `OrthodoxEasterSundayNotableDateProvider`. | [Date calculation algorithms](algorithms.md) |
| `Bodu.Globalization.Calendar.Plugins` | Plugin host with trust policies for loading rules / algorithms from external assemblies — `ExternalPluginLoader`, `IPluginTrustPolicy`, and the deny-by-default trust policies. | [Building and extending the service — Plugin system](building-the-service.md#plugin-system) |
| `Bodu.Extensions` | Working-day arithmetic over `DateOnly` and `DateTime` — `IsWorkingDay`, `NextWorkingDay`, `AddWorkingDays`, … (`NotableDateOnlyExtensions`, `NotableDateTimeExtensions`). | [Working-day arithmetic](working-days.md) |
| `Bodu.Globalization.Calendar.Data.*` | Region-specific public-holiday rule providers shipped in `Bodu.Globalization.Calendar.Data.Americas`, `.Europe`, and `.AsiaPacific` companion packages. | [Calendar data packs](data-packs.md) |
| `Bodu.Globalization.Calendar.DependencyInjection` | `IServiceCollection.AddNotableDates(...)`, `INotableDateServiceBuilder`, `NotableDateOptions`, and the `PostConfigure` consumer-options projection hook. | [Calendar dependency injection](dependency-injection.md) |

## Guides

### `Bodu.Globalization.Calendar` — Service

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="notable-dates.md">Using NotableDateService</a></h3>
  <p>The main entry point — resolving notable dates for a year, filtering by territory and category, querying a date range, layering override providers, and working-day arithmetic over <code>DateOnly</code> / <code>DateTime</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="rule-authoring.md">Authoring notable date rules</a></h3>
  <p>How to add your own rules — as in-code objects, embedded XML / JSON resource files, or companion assemblies — and how to layer runtime overrides.</p>
</div>

</div>

### `Bodu.Globalization.Calendar` — Reference

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="rule-reference.md">NotableDateRule and ObservanceAdjustment reference</a></h3>
  <p>Authoritative field-by-field reference for <code>NotableDateRule</code> and <code>ObservanceAdjustment</code> — every property, what it controls, and worked examples for each resolution strategy.</p>
</div>

<div class="bodu-card">
  <h3><a href="territories.md">Territories and regional composition</a></h3>
  <p>How <code>TerritoryCode</code> works — ISO 3166 country / subdivision codes, parsing, containment semantics (<code>AU</code> ⊇ <code>AU-NSW</code>), authoring rules with territory scope, and avoiding duplicate regional rules.</p>
</div>

<div class="bodu-card">
  <h3><a href="adjustment-rules.md">Observance adjustment rules</a></h3>
  <p>Nominal date vs. observed date; the full trigger and action catalogues — every <code>AdjustmentTrigger</code> and <code>AdjustmentAction</code> value with descriptions, companion fields, real-world patterns, and custom <code>IAdjustmentHandler</code> implementation.</p>
</div>

<div class="bodu-card">
  <h3><a href="resolution-pipeline.md">The resolution pipeline</a></h3>
  <p>Step-by-step walkthrough of all eight pipeline stages — from rule loading through adjustment evaluation, collision resolution, and per-year caching — with a concrete trace for Christmas Day 2027 in Australia.</p>
</div>

</div>

### `Bodu.Globalization.Calendar` — Patterns

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="holiday-patterns.md">Holiday patterns and examples</a></h3>
  <p>End-to-end examples for fixed-date holidays, weekend substitution rules (AU/NZ, UK, US), floating weekday-of-month holidays, Easter clusters, lunar and algorithmic dates, multi-day events, and subdivision-level variants.</p>
</div>

<div class="bodu-card">
  <h3><a href="working-days.md">Working-day arithmetic</a></h3>
  <p>The <code>Bodu.Extensions</code> surface over <code>DateOnly</code> / <code>DateTime</code> — <code>IsWorkingDay</code>, <code>AddWorkingDays</code>, <code>WorkingDaysBetween</code>, <code>NextWorkingDay</code>, snap operations, and the ambient <code>NotableDateContext</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="building-the-service.md">Building and extending the service</a></h3>
  <p>How to use the registry and factory types — <code>NotableDateAlgorithmRegistry</code>, <code>AdjustmentHandlerRegistry</code>, <code>NotableDateFilter</code> composition, <code>INotableDateRuleOverrideProvider</code>, <code>MutableNotableDateRuleOverrideProvider</code>, <code>INotableDateNameLocalizer</code>, <code>INotableDateCollisionResolver</code>, and the plugin system.</p>
</div>

<div class="bodu-card">
  <h3><a href="dependency-injection.md">Calendar dependency injection</a></h3>
  <p>The <code>Bodu.Globalization.Calendar.DependencyInjection</code> companion package — <code>services.AddNotableDates(...)</code>, the fluent <code>INotableDateServiceBuilder</code>, <code>IConfiguration</code>-bound <code>NotableDateOptions</code>, ambient default wiring, runtime-mutable overrides, and the <code>PostConfigure</code> hook for projecting from consumer-defined POCOs.</p>
</div>

</div>

### `Bodu.Globalization.Calendar.Algorithms`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="algorithms.md">Date calculation algorithms</a></h3>
  <p>The built-in algorithm types — Easter (Gregorian / Orthodox), Hindu Lunar, Losar, Vesak, Asalha Puja, Qingming — with registration guidance and a custom-algorithm walk-through.</p>
</div>

</div>

### `Bodu.Globalization.Calendar.Data.*` — Data packs

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="data-packs.md">Calendar data packs</a></h3>
  <p>The official <code>Bodu.Globalization.Calendar.Data.*</code> companion assemblies — Americas, Europe, and Asia-Pacific — and how to compose them with the resolution pipeline.</p>
</div>

</div>

## Where to go next

- [Bodu.Globalization.Calendar introduction](../../docs/calendar/index.md) — mental model, headline types, scenarios.
- [Core concepts](../../docs/calendar/concepts.md) — vocabulary used throughout these guides.
- [Bodu.Globalization.Calendar getting started](../../docs/calendar/getting-started.md) — install and minimal samples.
- [Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar) — full namespace overview.
