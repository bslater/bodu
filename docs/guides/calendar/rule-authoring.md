---
title: Authoring notable date rules
---

# Authoring notable date rules

In v2 a notable date is defined by a **rule document** on the cookbook schema (`urn:bodu:globalization:calendar`): author it as XML or JSON, then load it into an immutable <xref:Bodu.Globalization.Calendar.NotableDateResource> with <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader>. There is no mutable rule-object graph and no rule-provider interface — a rule is a `<Rule>` element, and a service is built over the loaded resource. This guide covers the document model directly; to assemble the same document fluently in C# instead, see [Authoring with the notable-date builder](notable-date-builder.md).

This guide walks the document model top to bottom: the `<NotableDateResource>` root and its child order, how a `<NotableDate>` concept carries one or more `<Rule>` recipes, the six `<Strategy>` elements, importing the bundled common catalogues with `<Imports>`, and ID-targeted edits with `<Overrides>`. For the vocabulary it assumes (document vs. resource, concept vs. rule, nominal vs. observed, territory containment) read [Core concepts](../../docs/calendar/concepts.md) first. For the per-element field reference, see [NotableDateRule and adjustment-policy reference](rule-reference.md).

![Rule authoring — authored document loaded into an immutable resource](../../images/diagrams/calendar-rule-authoring.svg)

---

## Document structure

A document is a single `<NotableDateResource>` element. It declares the schema namespace, a `schemaVersion`, and a `resourceId`, and contains its child sections **in this order**:

| Child element | Required | Purpose |
|---|---|---|
| `<Metadata>` | No | `Name`, `Description`, and zero or more `Source` provenance entries. |
| `<ResolutionPolicy>` | No | Resource-level duplicate / collision / observed-date policy and the working week. |
| `<AdjustmentPolicies>` | No | Reusable, named adjustment policies referenced by rules via `policyRef`. |
| `<Imports>` | No | Pulls concepts in from the bundled common catalogues. |
| `<NotableDates>` | No | The locally declared concepts (each with one or more rules). |
| `<Overrides>` | No | ID-targeted `AddRule` / `PatchRule` / `RemoveRule` edits applied at load time. |

A minimal document declares a single fixed-date concept:

```xml
<?xml version="1.0" encoding="utf-8"?>
<NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="demo">
  <NotableDates>
    <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
      <Rules>
        <Rule id="default">
          <Strategy><Fixed month="January" day="1" /></Strategy>
        </Rule>
      </Rules>
    </NotableDate>
  </NotableDates>
</NotableDateResource>
```

`id` values follow the schema's identifier pattern — lowercase, digits, and hyphens (`new-years-day`, `good-friday`). `resourceId` additionally allows dots (`data.au`, `common.global-buddhist`).

---

## Concepts and rules

A `<NotableDate>` is one notable-date **concept** — an `id`, a `displayName`, a default `category`, and a `<Rules>` block of one or more `<Rule>` recipes. Optional concept-level attributes set defaults the rules inherit: `defaultNonWorkingDay` marks the concept as a closure, and `defaultDurationDays` gives multi-day events their span.

```xml
<NotableDate id="anzac-day" displayName="Anzac Day" category="Remembrance" defaultNonWorkingDay="true">
  <Tags>
    <Tag value="national" />
  </Tags>
  <Rules>
    <Rule id="default">
      <Strategy><Fixed month="April" day="25" /></Strategy>
    </Rule>
  </Rules>
</NotableDate>
```

A `<Rule>` is one calculation recipe. Its required `id` distinguishes it from its siblings under the same concept; the optional attributes (`priority`, `category`, `nonWorking`, `durationDays`, `comment`) override the concept defaults for that recipe. A rule contains, **in order**:

1. an optional `<Applicability>` (calendar, year bounds, territory scope);
2. exactly one `<Strategy>` (the calculation);
3. an optional `<Tags>` block;
4. an optional `<Adjustments>` block of `policyRef` references.

```xml
<Rule id="au" priority="100" comment="National; substitute Monday when 26 January falls on a weekend.">
  <Applicability calendar="Gregorian"><Territory code="AU" /></Applicability>
  <Strategy><Fixed month="January" day="26" /></Strategy>
  <Tags><Tag value="national" /></Tags>
  <Adjustments><Adjustment policyRef="weekend-roll" /></Adjustments>
</Rule>
```

A concept holds several rules when the same date is observed differently across subdivisions or years — for example one Labour-Day rule per Australian state, each scoped to its subdivision and carrying its own `<Strategy>`. The engine resolves the most-specific rule that applies to the requested territory and year. See [NotableDateRule and adjustment-policy reference](rule-reference.md) for every attribute.

---

## The six strategy elements

Every rule carries exactly one `<Strategy>` child, and that child is exactly one of six elements. Each maps to a public <xref:Bodu.Globalization.Calendar.Algorithms.IDateCalculationStrategy>:

| `<Strategy>` element | What it computes |
|---|---|
| `<Fixed>` | A specific month + day every year (e.g. 1 January), optionally in a non-Gregorian calendar. |
| `<DayOfWeekInMonth>` | The *n*th or last weekday in a month (e.g. fourth Thursday in November). |
| `<WeekdayNearDate>` | A weekday on / before / after / nearest a fixed reference date (e.g. the Monday on or before 24 May). |
| `<RelativeWeekdayInMonth>` | A weekday positioned relative to a weekday-in-month anchor (e.g. the Tuesday after the first Monday in November). |
| `<OffsetFromRule>` | A signed day-offset from another rule's occurrence (e.g. Easter Sunday − 2 = Good Friday). |
| `<Algorithm>` | Delegated to a named algorithm key for astronomical / ecclesiastical dates (Easter, Vesak, Diwali, …). |

A one-line example of each:

```xml
<Strategy><Fixed month="January" day="1" /></Strategy>
<Strategy><DayOfWeekInMonth month="11" dayOfWeek="Thursday" weekOrdinal="Fourth" /></Strategy>
<Strategy><WeekdayNearDate month="5" day="24" dayOfWeek="Monday" direction="OnOrBefore" /></Strategy>
<Strategy><RelativeWeekdayInMonth month="11" dayOfWeek="Monday" weekOrdinal="First"
                                  relativeDayOfWeek="Tuesday" direction="After" /></Strategy>
<Strategy><OffsetFromRule notableDateRef="easter-sunday" ruleRef="default" offsetDays="-2" /></Strategy>
<Strategy><Algorithm key="western-easter" /></Strategy>
```

`month` accepts either a number (`1`–`12`) or an English month name (`January`). `weekOrdinal` is a <xref:Bodu.Globalization.Calendar.WeekOrdinal> value (`First`…`Fifth`, `Last`); `direction` is a <xref:Bodu.Globalization.Calendar.WeekdayProximity> value (`Before`, `OnOrBefore`, `Nearest`, `OnOrAfter`, `After`). For per-element attribute tables and worked examples see [NotableDateRule and adjustment-policy reference](rule-reference.md); for the `<Algorithm>` key catalogue and custom algorithms see [Date calculation algorithms](algorithms.md).

---

## Adjustment policies

A weekend-substitution or "move-to-next-working-day" shift is authored once as a reusable `<AdjustmentPolicy>` in `<AdjustmentPolicies>`, then referenced from any rule via `<Adjustment policyRef="...">`. Adjustments are **always** referenced by id — there are no inline per-rule adjustment definitions.

```xml
<AdjustmentPolicies>
  <AdjustmentPolicy id="weekend-roll" priority="100"
                    description="If the holiday falls on a weekend, observe it on the following Monday.">
    <Trigger type="IfWeekend" />
    <Action type="MoveToNextWorkingDay" skipWeekends="true" skipNonWorkingDates="false" maxSearchDays="7" />
    <Emission mode="ObservedOnly" reason="Substitute public holiday" />
  </AdjustmentPolicy>
</AdjustmentPolicies>
```

A policy pairs a `<Trigger>` (when it fires) with an `<Action>` (what it does) and an `<Emission>` (whether the actual day, the observed day, or both are emitted), plus an optional `<Scope>` that limits it to a territory, calendar, category, or year range. A rule opts in by reference:

```xml
<Rule id="au">
  <Applicability calendar="Gregorian"><Territory code="AU" /></Applicability>
  <Strategy><Fixed month="January" day="26" /></Strategy>
  <Adjustments><Adjustment policyRef="weekend-roll" /></Adjustments>
</Rule>
```

The full trigger, action, emission, and scope vocabulary — and the worked weekend-substitution patterns for AU/NZ, the UK, and the US — are covered in [Observance adjustment rules](adjustment-rules.md) and [Holiday patterns and examples](holiday-patterns.md).

---

## Importing the common catalogues

A regional document rarely starts from scratch. The base package ships a set of **common catalogues** — `global-core`, `christian-western`, `global-family`, `global-remembrance`, `global-cultural`, `global-buddhist`, `global-hindu`, and friends — that carry the bare calculation strategy for shared concepts. An `<Import>` pulls those concepts in; the local document supplies the territory scope, category, non-working flag, and any adjustment.

```xml
<Imports>
  <Import resource="global-core">
    <Use notableDateRef="new-years-day" territory="AU">
      <Adjustments><Adjustment policyRef="weekend-roll" /></Adjustments>
    </Use>
  </Import>

  <Import resource="christian-western">
    <Use notableDateRef="good-friday"   territory="AU" />
    <Use notableDateRef="easter-sunday" territory="AU" />
    <Use notableDateRef="easter-monday" territory="AU" />
    <Use notableDateRef="christmas-day" territory="AU">
      <Adjustments><Adjustment policyRef="working-day-substitute" /></Adjustments>
    </Use>
  </Import>
</Imports>
```

Each `<Import resource="...">` names a catalogue. Inside it, a `<Use>` directive cherry-picks one concept by `notableDateRef` and may:

- rename it locally with `as`,
- re-scope it to a `territory`,
- override the `category` or `nonWorking` flag,
- attach adjustment policies via a nested `<Adjustments>` block.

An `<Import>` with **no** `<Use>` children imports every concept in the catalogue. When a local concept and an imported concept share an `id`, the **local** one wins. The catalogue names accepted by the resolver include `default-minimal`, `global-core`, `global-all`, `christian-western`, `christian-orthodox`, `global-islamic`, `global-hindu`, `global-jewish`, `global-buddhist`, `global-cultural`, `global-remembrance`, and the UN / health / science / education / environment / food / family / social families.

### Loading a document that imports

`<Imports>` are resolved by a `Func<string,string?>` passed to the loader. <xref:Bodu.Globalization.Calendar.CommonNotableDateResources> exposes that resolver over the bundled catalogues — pass `CommonNotableDateResources.Resolver`:

```csharp
using Bodu.Globalization.Calendar;

NotableDateResource resource =
    NotableDateResourceLoader.Load(xml, CommonNotableDateResources.Resolver);
NotableDateService service = new NotableDateService(resource);
```

A document with **no** `<Imports>` loads with the single-argument overload, `NotableDateResourceLoader.Load(xml)`. The companion `Bodu.Globalization.Calendar.Data.*` packs are built exactly this way — each region resource imports from the common catalogues and is loaded through the same resolver. See [Calendar data packs](data-packs.md).

---

## ID-targeted overrides

`<Overrides>` are edits applied at load time, after imports are resolved, targeted by id. They let a regional document tweak an imported concept without forking it. Three operations are available:

- **`<AddRule notableDateRef="...">`** wraps a new `<Rule>` and appends it to an existing concept.
- **`<PatchRule notableDateRef="..." ruleRef="...">`** replaces parts of an existing rule. The targeting attributes are required; scalar attributes (`priority`, `category`, `nonWorking`, `durationDays`, `comment`) patch in place, and a nested `<Applicability>`, `<Strategy>`, `<Tags>`, or `<Adjustments>` replaces that section wholesale.
- **`<RemoveRule notableDateRef="..." ruleRef="..."/>`** deletes a single rule.

```xml
<Overrides>
  <!-- Suppress an imported rule for this consumer … -->
  <RemoveRule notableDateRef="boxing-day" ruleRef="default" />

  <!-- … bump another rule's priority and re-scope it … -->
  <PatchRule notableDateRef="labour-day" ruleRef="default" priority="200">
    <Applicability calendar="Gregorian"><Territory code="AU-VIC" /></Applicability>
  </PatchRule>

  <!-- … and add a company event to an existing concept. -->
  <AddRule notableDateRef="company-founding-day">
    <Rule id="hq">
      <Applicability calendar="Gregorian"><Territory code="AU" /></Applicability>
      <Strategy><Fixed month="June" day="15" /></Strategy>
    </Rule>
  </AddRule>
</Overrides>
```

Overrides run during loading and produce a normal immutable resource. *Runtime* change (swapping the rule set after the service is built) is a separate mechanism: load a new resource and hand it to a <xref:Bodu.Globalization.Calendar.MutableNotableDateResourceProvider>, then resolve through a <xref:Bodu.Globalization.Calendar.ReloadableNotableDateService>. See [Using NotableDateService](notable-dates.md#pattern-8--swap-the-rule-set-at-runtime).

---

## Authoring in JSON

JSON is an equivalent surface for the same document model; the choice is presentation-only. Load it with <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader>'s `LoadJson` overloads (`LoadJson(json)`, `LoadJson(json, resolver)`, `LoadJson(Stream)`). The element names map directly to JSON property names:

```json
{
  "schemaVersion": "1.0",
  "resourceId": "demo",
  "notableDates": [
    {
      "id": "new-years-day",
      "displayName": "New Year's Day",
      "category": "PublicHoliday",
      "defaultNonWorkingDay": true,
      "rules": [
        {
          "id": "default",
          "strategy": { "fixed": { "month": "January", "day": 1 } }
        }
      ]
    }
  ]
}
```

```csharp
using Bodu.Globalization.Calendar;

NotableDateResource resource = NotableDateResourceLoader.LoadJson(json, CommonNotableDateResources.Resolver);
```

---

## Validation

Loading parses the document, resolves `<Imports>`, applies `<Overrides>`, assembles the concepts, and runs semantic validation. Any **error**-severity diagnostic throws a <xref:Bodu.Globalization.Calendar.NotableDateValidationException>; its `Diagnostics` collection carries every <xref:Bodu.Globalization.Calendar.NotableDateValidationDiagnostic> ( `Severity`, `Code`, `Message`), so informational and warning diagnostics are visible even when the load succeeds:

```csharp
using Bodu.Globalization.Calendar;

try
{
    NotableDateResource resource = NotableDateResourceLoader.Load(xml, CommonNotableDateResources.Resolver);
}
catch (NotableDateValidationException ex)
{
    foreach (NotableDateValidationDiagnostic d in ex.Diagnostics)
        Console.WriteLine($"{d.Severity} {d.Code}: {d.Message}");
}
```

Typical errors include a duplicate concept or rule id, an unknown adjustment `policyRef`, an unknown `<Algorithm>` key, an impossible fixed date, or a `fromYear` after `toYear`.

---

## Where to go next

- [Using NotableDateService](notable-dates.md) — loading resources, querying by date / range / year, and filtering.
- [NotableDateRule and adjustment-policy reference](rule-reference.md) — the per-element field reference for the document model.
- [Date calculation algorithms](algorithms.md) — the six strategies, the built-in `<Algorithm>` keys, and custom algorithms.
- [Observance adjustment rules](adjustment-rules.md) — the full trigger / action / emission catalogues for `<AdjustmentPolicy>`.
- [Working with non-Gregorian calendars](non-gregorian-calendars.md) — `<Fixed>` dates in Hijri / Hebrew / Persian / Chinese lunisolar calendars.
- [Calendar data packs](data-packs.md) — the official Americas / Europe / Asia-Pacific resources, built from these same imports.
- [Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar) — full type reference.
