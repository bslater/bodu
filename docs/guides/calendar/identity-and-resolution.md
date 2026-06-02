---
title: Rule identity, priority, and observed-date resolution
---

# Rule identity, priority, and observed-date resolution

This guide covers the resolution semantics a cookbook author needs to reason about once a rule set grows beyond a
handful of fixed dates: how rules are *identified*, how overrides *target* them, how same-day *collisions* are arbitrated
by priority, how *observed* dates are emitted, how adjustment *scope* is bounded, and how to *validate* a rule set before
it ships.

For the per-property reference, see [NotableDateRule and ObservanceAdjustment reference](rule-reference.md). For the
end-to-end materialisation flow, see [The resolution pipeline](resolution-pipeline.md).

## Rule identity and variants

A rule is identified by its full <xref:Bodu.Globalization.Calendar.NotableDateRule> identity — its canonical `Name`, the
optional rule-level `RuleName` variant, the `TerritoryCode`, and the `CalendarType` — not by `Name` alone. This lets
several rules share a canonical name and **coexist** at runtime:

```xml
<NotableDate name="Easter Sunday">
  <Rule ruleName="western" category="Religious">
    <Algorithm key="easter-sunday" />
  </Rule>
  <Rule ruleName="orthodox" category="Religious" calendarType="System.Globalization.JulianCalendar">
    <Algorithm key="orthodox-easter-sunday" />
  </Rule>
</NotableDate>
```

Both variants survive flattening, override merging, and range caching, and both resolve for the same year. Identity
comparison is case-insensitive and normalises territory codes (`au` ≡ `AU`, but `AU` ≠ `AU-NSW`).

### Referencing a variant

Offset anchors (`OffsetFromAnchor`), replacement targets (`ReplaceWithNamedDate`), removals, and nested overrides resolve
a name to a *single* rule. When a name is unambiguous it resolves directly; when several variants share the name the
reference is disambiguated by the requesting rule's territory and calendar context. If it still cannot be narrowed to one
candidate, resolution fails loudly with an *ambiguous reference* error rather than silently binding to an arbitrary
variant — qualify the reference (for example with a `ruleName`) to resolve it.

## Overrides and partial overrides

A `<Use>` directive imports a rule by canonical name; its nested `<Rule>` body overrides scalars, tags, adjustments, or
the strategy. The body is a **partial** override — only `name`-of-the-`Use` (the canonical source name) is required;
`category` and the body's own identity are optional, so a body may change just one field:

```xml
<Use name="Easter">
  <Rule ruleName="western" priority="10" />
</Use>
```

The body's `ruleName` attribute targets a specific inherited variant (the legacy `name` attribute is still accepted as an
alias). Precedence is innermost-wins: nested body over flat `<Use>` attributes over the inherited source rule.

## Priority and same-day collisions

Every rule carries a `Priority` (default `100`, **lower wins**) that flows onto the resolved
<xref:Bodu.Globalization.Calendar.NotableDate>. When several occurrences fall on the same day, the registered
<xref:Bodu.Globalization.Calendar.INotableDateCollisionResolver> arbitrates them through a
<xref:Bodu.Globalization.Calendar.NotableDateCollisionContext>. The
<xref:Bodu.Globalization.Calendar.DefaultNotableDateCollisionResolver> orders by, most significant first:

1. **Provenance** — a runtime override outranks a local rule, which outranks an imported rule.
2. **Priority** — ascending (lower value wins).
3. **Category** ordinal, then **Name**, then **TerritoryCode**.

It keeps every distinct occurrence; hosts that want a single winner per day supply a custom resolver and take the first
element. For a **single-day** query, every occurrence that *covers* that day — including multi-day spans that start
earlier — is arbitrated together (coverage-day collision).

## Observed-date modes

When an <xref:Bodu.Globalization.Calendar.ObservanceAdjustment> shifts a date (for example, rolling a Saturday holiday to
Monday), <xref:Bodu.Globalization.Calendar.ObservedDateMode> controls what is emitted:

| Mode | Behaviour |
|---|---|
| `ObservedOnly` *(default)* | The observed (adjusted) date supersedes the actual date. |
| `ActualOnly` | The actual date is kept; the substitute is suppressed. |
| `ActualAndObserved` | Both are emitted as separate occurrences. |

Set the service-wide default on <xref:Bodu.Globalization.Calendar.NotableDateServiceOptions.ObservedDates>, or override
per query on `ResolveNotableDatesInRange`. The result for a given day is **independent of the query-window width** — a
holiday whose substitute rolls into the next year is reported in that next year under `ObservedOnly`, whether you query a
single day or a range.

When several adjustments are configured, the **first** that activates (by ascending `Priority`) wins; each is evaluated
against the original calculated date.

## Adjustment scope and reach

- A territory- or calendar-scoped adjustment does **not** apply to a global (territory/calendar-neutral) rule unless
  `AppliesToGlobalRules` is set, so a regional substitute does not silently affect a global rule.
- The adjustment's emitted territory is normalised to a single canonical code; a comma-separated scope keeps the entry's
  own territory.
- `ReplaceWithNamedDate` and custom handlers can move a date far from its anchor. Declare `maxReachDays` so the range
  planner sizes its fringe scan correctly, and forward `<Param>` key/value pairs to a custom handler.

## Validating a rule set

Set <xref:Bodu.Globalization.Calendar.NotableDateServiceOptions.ValidateRules> to validate the effective rule set when the
service is constructed; it throws an `InvalidOperationException` on any error. To inspect findings without throwing, call
`NotableDateService.Validate()`, which returns
<xref:Bodu.Globalization.Calendar.NotableDateValidationDiagnostic> entries reporting duplicate identities, missing or
ambiguous anchors, missing or ambiguous replacement targets (errors), and unregistered algorithm keys (a warning, since
an optional pack may supply them later).
