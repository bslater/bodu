---
title: Globalization & Calendars — Guides
---

# Globalization & Calendars — Guides

Recipe-style walk-throughs for the calendar package family — the `Bodu.Globalization.Calendar` runtime, its companions (Builder, DependencyInjection, Plugins), and the five regional data packs. Every guide in this topic lives under the [Bodu.Globalization.Calendar guides section](../calendar/index.md); this page is the topic-level map.

If you are new to the family, start with the [topic overview](../../docs/topics/globalization-and-calendars.md) for the package decision table and the [topic concepts](../../docs/topics/globalization-and-calendars-concepts.md) for the shared vocabulary, then come back here for the hands-on material.

## The guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../calendar/index.md">Overview</a></h3>
  <p>The full guide index for the calendar family — resolution pipeline, namespace map, and every walk-through grouped by namespace.</p>
</div>

<div class="bodu-card">
  <h3><a href="../calendar/notable-dates.md">Using NotableDateService</a></h3>
  <p>The main entry point — loading a resource, resolving for a date, range, or year, filtering by territory and category, and the reloadable runtime-swap workflow.</p>
</div>

<div class="bodu-card">
  <h3><a href="../calendar/rule-authoring.md">Authoring notable date rules</a></h3>
  <p>Writing rule documents in XML / JSON — definitions, rules, strategies, importing the bundled common catalogues with <code>&lt;Use&gt;</code> directives, and layering ID-targeted <code>&lt;Overrides&gt;</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="../calendar/working-days.md">Working-day arithmetic</a></h3>
  <p>The <code>Bodu.Extensions</code> surface over <code>DateOnly</code> / <code>DateTime</code> / <code>DateTimeOffset</code> — <code>IsWorkingDay</code>, <code>AddWorkingDays</code>, <code>WorkingDaysBetween</code>, snap operations, and the <code>WeekPattern</code> working week.</p>
</div>

<div class="bodu-card">
  <h3><a href="../calendar/data-packs.md">Calendar data packs</a></h3>
  <p>The regional companion assemblies — <code>CreateService</code> / <code>LoadResource</code> factories, territory coverage, and composing several regions into one service.</p>
</div>

<div class="bodu-card">
  <h3><a href="../calendar/algorithms.md">Date calculation algorithms</a></h3>
  <p>The six resolution strategies and the built-in <code>&lt;Algorithm key="…"&gt;</code> keys — Easter, equinoxes, Qingming, Vesak, Losar, Matariki, Hindu festivals — plus a custom-algorithm walk-through.</p>
</div>

</div>

## Companion packages

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../calendar/notable-date-builder.md">Authoring with the notable-date builder</a></h3>
  <p>The fluent C# peer of XML / JSON authoring — <code>NotableDateDocumentBuilder</code> assembles definitions, rules, policies, imports, and overrides, then serializes, saves, or builds a resource.</p>
</div>

<div class="bodu-card">
  <h3><a href="../calendar/dependency-injection.md">Calendar dependency injection</a></h3>
  <p><code>services.AddNotableDateService(...)</code>, the resource-factory overload, and <code>AddReloadableNotableDateService(...)</code> for the runtime-swap workflow.</p>
</div>

<div class="bodu-card">
  <h3><a href="../calendar/building-the-service.md">Building and extending the service</a></h3>
  <p>Composing the service with collaborators — algorithm and adjustment registries, collision resolvers, localizers, providers, and the trust-gated plugin system.</p>
</div>

</div>

## The data

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../calendar/catalogue/index.md">Notable-date catalogue</a></h3>
  <p>What notable dates the calendar resources include and how regions and territories differ — generated from the XML, organized by theme and by region, with a cross-region comparison matrix.</p>
</div>

</div>

## Suggested reading path

1. **[Using NotableDateService](../calendar/notable-dates.md)** — resolve and filter dates with a data-pack service; the 80 % case.
2. **[Calendar data packs](../calendar/data-packs.md)** — pick your regions and understand territory coverage.
3. **[Working-day arithmetic](../calendar/working-days.md)** — business-day math over the resolved dates.
4. **[Authoring notable date rules](../calendar/rule-authoring.md)** or the **[builder guide](../calendar/notable-date-builder.md)** — when the shipped data is not enough.
5. **[Date calculation algorithms](../calendar/algorithms.md)** and **[Building and extending the service](../calendar/building-the-service.md)** — the extensibility seams, including plugins.

## Where to go next

- [Globalization & Calendars topic overview](../../docs/topics/globalization-and-calendars.md) — collective purpose, package table, and the which-package decision table.
- [Topic concepts](../../docs/topics/globalization-and-calendars-concepts.md) — the cross-package vocabulary.
- [Bodu.Globalization.Calendar introduction](../../docs/calendar/index.md) — the runtime's mental model and headline types.
- [Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar) — full type-by-type docs.
