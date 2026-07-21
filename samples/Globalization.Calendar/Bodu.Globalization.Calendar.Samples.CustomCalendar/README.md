# Bodu.Globalization.Calendar.Samples.CustomCalendar

Authoring your own calendar — company holidays, shutdowns, celebrations — with the fluent
`NotableDateDocumentBuilder`, then treating it exactly like a shipped data pack: adjustment
policies, catalogue imports, and the XML and JSON round trips that make the document a
distributable artifact. Fully offline; the round-trip files are written to the sample's own
output directory.

```bash
dotnet run --project samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.CustomCalendar
```

## Scenarios

For the full catalogue of occurrence sources — every single-date strategy, the recurrence
sources, and fixed vs. calculated durations — see the
[Notable-date rule strategies](../../../docs/guides/calendar/strategy-reference.md) guide.

### AuthoringCompanyHolidays (`Scenarios/AuthoringCompanyHolidays.cs`)

**Intent.** Show that rules are *data*, authored declaratively — a fixed date, a fortnightly
recurrence, and a **calculated-duration** span whose length is computed from the calendar — with
no date mathematics in consumer code.

**What it does.** Builds a three-concept company calendar (fixed Founding Day, a fortnightly
All-Hands `DailyInterval` recurrence, and a Year-End Shutdown whose span runs from the Friday
before Boxing Day to the Monday after New Year's Day), materializes it with `Build()`, and
resolves December 2023 → mid-2024 through a plain `NotableDateService`.

**What to expect (excerpt).**

```
  2023-12-23 (Saturday ) Year-End Shutdown      Other, non-working: True, spans 16d (ends 2024-01-07)
  2024-01-05 (Friday   ) Fortnightly All-Hands  Other, non-working: False, spans 1d (ends 2024-01-05)
  2024-03-12 (Tuesday  ) Contoso Founding Day   Other, non-working: True, spans 1d (ends 2024-03-12)
```

The shutdown's span is **calculated**, not a fixed day count — it is 16 days here but 9 in years
where the Friday before Boxing Day is Christmas Day, all from one rule (see the shutdown table in
the [strategy guide](../../../docs/guides/calendar/strategy-reference.md#durations-fixed-or-calculated)).
The All-Hands repeats every 14 days from its anchor. The authored resource is served by the same
service type the regional packs return — a custom calendar is a first-class citizen.

**APIs demonstrated.** `NotableDateDocumentBuilder.Create` / `WithMetadata` / `AddNotableDate`,
`NotableDateDefinitionBuilder.AsNonWorkingByDefault` / `AddRule`,
`NotableDateRuleBuilder.Fixed` / `DailyInterval` / `UntilDate`, `DateBoundary`,
`EndDateSelection`, `Build()`, `new NotableDateService(resource)`.

### FrequencyBasedSchedules (`Scenarios/FrequencyBasedSchedules.cs`)

**Intent.** Frequency-based rules: a `Recurrence` source yields **many** occurrences in a window
instead of one date per year — the four recurrence kinds, authored fluently.

**What it does.** Builds an operations calendar entirely from recurrences — a fortnightly
All-Hands (`DailyInterval`), a twice-weekly Maintenance Window (`Weekly` on Monday + Friday), a
day-15 Payroll Run and a day-31 Month-End Close (`MonthlyDay`, the latter clamping short months),
and a last-Friday Board Report (`MonthlyWeekday`) — then resolves the first quarter of 2026.

**What to expect (excerpt).**

```
  2026-01-05 (Monday   ) Fortnightly All-Hands
  2026-01-15 (Thursday ) Payroll Run
  2026-01-30 (Friday   ) Board Report
  2026-01-31 (Saturday ) Month-End Close
```

Every occurrence flows through the normal pipeline (category, non-working flag, duration,
adjustments) just like a fixed holiday, and generation is **query-window invariant** — resolving
February alone yields exactly the February subset of the quarter. See
[Notable-date rule strategies — Recurrence sources](../../../docs/guides/calendar/strategy-reference.md#recurrence-sources).

**APIs demonstrated.** `NotableDateRuleBuilder.DailyInterval` / `Weekly` / `MonthlyDay` /
`MonthlyWeekday`, `InvalidDayOfMonthBehavior.UseLastDayOfMonth`, `WeekOrdinal.Last`,
`NotableDateService.Resolve(DateRange, territory)`.

### AdjustmentsAndPolicies (`Scenarios/AdjustmentsAndPolicies.cs`)

**Intent.** Weekend/in-lieu substitution as declarative policy: a trigger (when), an action
(what), and an emission mode (what queries return) — declared once, referenced by any rule.

**What it does.** Declares a `weekend-roll` policy (`IfWeekend` → `MoveToNextWorkingDay`,
`ObservedOnly` emission, with a reason string) and attaches it to the Founding Day rule, then
resolves a year where the policy is dormant (2024, a Tuesday) and one where it fires (2022, a
Saturday).

**What to expect.**

```
  2024: 2024-03-12 (Tuesday  ) actual (no adjustment)
  2022: 2022-03-14 (Monday   ) observed (actual 2022-03-12, In-lieu day (weekend substitution))
```

The 2022 occurrence keeps its lineage: the emitted (observed) Monday, the actual Saturday, and
the human-readable reason — everything a leave system needs to explain the in-lieu day.

**APIs demonstrated.** `AddAdjustmentPolicy`, `AdjustmentPolicyBuilder.When` / `Then` / `Emit` /
`WithReason`, `AdjustmentTrigger.IfWeekend`, `AdjustmentAction.MoveToNextWorkingDay`,
`EmissionMode.ObservedOnly`, `NotableDateRuleBuilder.WithAdjustment`.

### ImportingCatalogues (`Scenarios/ImportingCatalogues.cs`)

**Intent.** Do not re-derive Easter. The shared catalogues the regional packs themselves import
(the computus, lunar, and cultural calculations) are available to authored calendars through
`AddImport` + `Use`, resolved by `CommonNotableDateResources.Resolver`.

**What it does.** Imports `christian-western`, cherry-picks `easter-sunday`, `good-friday`, and
`easter-monday` (re-categorised as non-working public holidays for this company), adds the
company's own Founding Day, and builds with the catalogue resolver.

**What to expect.**

```
  2024-03-12 (Tuesday  ) Contoso Founding Day   non-working: True
  2024-03-29 (Friday   ) Good Friday            non-working: True
  2024-03-31 (Sunday   ) Easter Sunday          non-working: False
  2024-04-01 (Monday   ) Easter Monday          non-working: True
```

The Easter dates came from the catalogue's algorithm — nothing was hand-coded. Note the
dependency the validator enforces: Good Friday and Easter Monday are defined as *offsets from*
`easter-sunday`, so the anchor concept must be `Use`d too; omitting it fails the build with a
precise diagnostic (`BODU-CAL-OFFSET-MISSING`).

**APIs demonstrated.** `AddImport(resource, i => i.Use(...))`,
`ImportUseBuilder.WithCategory` / `AsNonWorking`, `Build(CommonNotableDateResources.Resolver)`.

### XmlRoundTrip (`Scenarios/XmlRoundTrip.cs`)

**Intent.** The document, not the builder, is the artifact: save to XML, distribute, and load it
back through either the builder (for further editing) or the plain resource loader (what any
consumer without the Builder package uses).

**What it does.** Saves an authored calendar to the output directory, reloads it via both
`NotableDateDocumentBuilder.Load(path)` and `NotableDateResourceLoader.Load(xml)`, serves the
loader's copy, and compares the two resources' ids.

**What to expect.**

```
Saved: contoso-holidays.xml (576 bytes)
Reloaded and resolved: 2024-03-12 Contoso Founding Day
Builder and loader agree: True
```

576 bytes is the entire distributable calendar. `.json` in the `Save` path switches to the
documented JSON subset.

**APIs demonstrated.** `Save(path)`, `NotableDateDocumentBuilder.Load`,
`NotableDateResourceLoader.Load(string)`, `NotableDateResource.ResourceId`.

### JsonRoundTrip (`Scenarios/JsonRoundTrip.cs`)

**Intent.** The same round trip against the documented JSON subset: XML and JSON are two
encodings of one document model, so an authored calendar persists, distributes, and reloads
identically through either — the builder for further editing, or the plain loader for a consumer
without the Builder package.

**What it does.** Saves the same authored calendar to `contoso-holidays.json` (the `.json`
extension selects the JSON subset in the `Save` path), reloads it via both
`NotableDateDocumentBuilder.Load(path)` and the JSON-specific
`NotableDateResourceLoader.LoadJson(json)`, serves the loader's copy, and compares the two
resources' ids.

**What to expect.**

```
Saved: contoso-holidays.json (500 bytes)
Reloaded and resolved: 2024-03-12 Contoso Founding Day
Builder and loader agree: True
```

The JSON form is 500 bytes to the XML form's 576 — the same distributable calendar, resolving to
the identical Founding Day. `LoadJson` is the JSON counterpart to the XML-accepting `Load`; the
builder's own `Load` infers the format from the file extension.

**APIs demonstrated.** `Save(path)` (JSON format inferred from extension),
`NotableDateDocumentBuilder.Load`, `NotableDateResourceLoader.LoadJson(string)`,
`NotableDateResource.ResourceId`.

## NuGet equivalent

```bash
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Globalization.Calendar.Builder
```
