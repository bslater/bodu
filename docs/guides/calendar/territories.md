---
title: Territories and regional composition
---

# Territories and regional composition

<xref:Bodu.Globalization.Calendar.TerritoryCode> is the strongly-typed scope used by rules, adjustments, and queries. It wraps an ISO 3166-1 alpha-2 country code with an optional ISO 3166-2 subdivision (`AU`, `AU-NSW`, `GB-ENG`, `US-CA`) and exposes containment semantics that let national and regional rules compose naturally.

For the conceptual overview, see [Core concepts](../../docs/calendar/concepts.md#territory). For per-property reference, see [NotableDateRule and ObservanceAdjustment reference](rule-reference.md).

## Anatomy

A `TerritoryCode` carries two pieces of information:

| Property | Description |
|---|---|
| `Country` | The ISO 3166-1 alpha-2 country code (`AU`, `GB`, `US`). Always upper-case in canonical form. |
| `Subdivision` | The optional ISO 3166-2 subdivision suffix (`NSW`, `ENG`, `CA`). `null` when the territory refers to the whole country. |
| `HasSubdivision` | Convenience flag — `true` when `Subdivision` is non-null. |

The canonical string form is `Country` for a country-level code and `Country-Subdivision` (with a hyphen) for a subdivision-level code.

## Parsing

`TerritoryCode` provides both throwing and non-throwing parsers, plus a list parser for comma-separated input:

```csharp
using Bodu.Globalization.Calendar;

// Throwing parse — use when the input is trusted authored data.
TerritoryCode au    = TerritoryCode.Parse("AU");
TerritoryCode auNsw = TerritoryCode.Parse("AU-NSW");

// Try-parse — use for user input or external data.
if (TerritoryCode.TryParse(userInput, out TerritoryCode territory))
{
    // territory is canonical here.
}

// Comma-separated list — convenient for multi-territory query parameters.
IReadOnlyList<TerritoryCode> territories =
    TerritoryCode.ParseList("AU-NSW, AU-VIC, NZ");
```

Implicit conversion from `string` works for fluent call sites too:

```csharp
IReadOnlyList<NotableDate> dates =
    service.GetNotableDates(year: 2026, territoryCode: "AU-NSW");
```

The library normalises whitespace and casing during parsing, so `"  au-nsw  "` and `"AU-NSW"` parse to the same value.

## Containment

![TerritoryCode containment hierarchy](../../images/diagrams/calendar-territory-containment.svg)

`TerritoryCode.Contains(other)` returns `true` when `other` is within the scope of the current territory:

| Receiver | Argument | `Contains` returns |
|---|---|---|
| `AU` | `AU` | `true` |
| `AU` | `AU-NSW` | `true` |
| `AU` | `AU-VIC` | `true` |
| `AU` | `NZ` | `false` |
| `AU-NSW` | `AU` | `false` |
| `AU-NSW` | `AU-NSW` | `true` |
| `AU-NSW` | `AU-VIC` | `false` |

The rule is simple: a country contains itself and every subdivision; a subdivision contains only itself.

This is the same containment relation that `NotableDateService` applies during resolution: a rule authored at `AU` resolves for queries at `AU` and at any `AU-XX`, and an adjustment scoped to `AU-NT` only fires for `AU-NT` queries.

## Authoring rules with territory scope

Set `NotableDateRule.TerritoryCode` to limit the rule's applicability. Leave it `null` for a globally applicable rule.

```csharp
// National holiday — applies to every Australian subdivision.
NotableDateRule australiaDay = new NotableDateRule
{
    Name          = "Australia Day",
    Strategy      = DateResolutionStrategy.Fixed,
    Category      = NotableDateCategory.Holiday,
    Month         = 1,
    Day           = 26,
    TerritoryCode = "AU",
    IsNonWorkingDay = true,
};

// Regional public holiday — applies only to Victoria.
NotableDateRule melbourneCup = new NotableDateRule
{
    Name          = "Melbourne Cup Day",
    Strategy      = DateResolutionStrategy.DayOfWeekInMonth,
    Category      = NotableDateCategory.Holiday,
    Month         = 11,
    DayOfWeek     = DayOfWeek.Tuesday,
    WeekOrdinal   = WeekOfMonthOrdinal.First,
    TerritoryCode = "AU-VIC",
    IsNonWorkingDay = true,
};
```

In XML / JSON, the same attribute is `territory`:

```xml
<Rule name="Australia Day" category="Holiday" territory="AU" nonWorking="true">
  <Fixed month="January" day="26" />
</Rule>
```

## Querying composition

A query passes a single `territoryCode`, and the service returns every rule whose scope **contains** that code (plus globally-scoped rules). For Australia:

```csharp
// Returns rules scoped to AU, AU-NSW, and any globally-scoped rules.
IReadOnlyList<NotableDate> nsw = service.GetNotableDates(year: 2026, "AU-NSW");

// Returns rules scoped to AU, AU-VIC (including Melbourne Cup), and globals.
IReadOnlyList<NotableDate> vic = service.GetNotableDates(year: 2026, "AU-VIC");
```

The same applies to adjustments: an adjustment scoped to `AU` fires for any `AU-XX` query, while an `AU-NT` adjustment only fires for NT-specific queries. See [Observance adjustment rules](adjustment-rules.md#territory-and-year-scoping-on-adjustments).

## Avoiding duplicate regional rules

The containment relation is the right way to model "national rule + a few regional variations":

- Author the national rule **once** at the country level (`AU`).
- For each subdivision that differs, override only the variant — typically with `clearInherited="true"` in XML, or with an `INotableDateRuleOverrideProvider` at runtime — and scope it to the subdivision (`AU-NT`).

Authoring the same rule for every subdivision under the same country produces duplicate occurrences and is rarely correct. When you genuinely need the same rule at multiple subdivisions but not the whole country (e.g. Boxing Day in some AU states but not all), pick the smaller scope: author one rule per state, each scoped to its subdivision. The [holiday patterns guide](holiday-patterns.md) shows worked examples for both shapes.

## Data-pack conventions

The official `Bodu.Globalization.Calendar.Data.*` companion packages follow the conventions above:

- National rules are authored at the country level (`AU`, `US`, `GB`).
- State / province / region variants use the canonical ISO 3166-2 subdivision suffix (`AU-NSW`, `US-CA`, `GB-SCT`).
- Cross-country composition (e.g. *EU bank holidays*) is **not** modelled via territory code — each country ships its own rule set under its own ISO code. Cross-cutting groupings belong in tags.

See [Calendar data packs](data-packs.md) for the per-country helpers and assembly-chain mechanics.

## Where to go next

- **[Core concepts — Territory](../../docs/calendar/concepts.md#territory)** — the vocabulary at a glance.
- **[NotableDateRule and ObservanceAdjustment reference](rule-reference.md)** — the `TerritoryCode` property contract on rules and adjustments.
- **[Authoring notable date rules](rule-authoring.md)** — full authoring workflow with worked examples.
- **[Calendar data packs](data-packs.md)** — region-specific bundled rule sets.
- **[Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar)** — generated API surface.
