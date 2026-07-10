# Bodu.Globalization.Calendar.Samples.CustomCalendar

Authoring your own calendar — company holidays, shutdowns, celebrations — with the fluent
`NotableDateDocumentBuilder`, then treating it exactly like a shipped data pack: adjustment
policies, catalogue imports, and the XML round trip that makes the document a distributable
artifact. Fully offline; the round-trip file is written to the sample's own output directory.

```bash
dotnet run --project samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.CustomCalendar
```

## Scenarios

### AuthoringCompanyHolidays (`Scenarios/AuthoringCompanyHolidays.cs`)

**Intent.** Show that rules are *data*, authored declaratively: a fixed date, an "nth weekday of
month" floater, and a multi-day span — no date mathematics in consumer code.

**What it does.** Builds a three-concept company calendar (fixed Founding Day, first-Friday-of-
December Summer Party, three-day Year-End Shutdown), materializes it with `Build()`, and
resolves 2024 through a plain `NotableDateService`.

**What to expect.**

```
  2024-03-12 (Tuesday  ) Contoso Founding Day   Other, non-working: True, spans 1d
  2024-12-06 (Friday   ) Summer Party           Cultural, non-working: False, spans 1d
  2024-12-27 (Friday   ) Year-End Shutdown      Other, non-working: True, spans 3d
```

The Summer Party floated to the first Friday (12-06) from the `DayOfWeekInMonth` strategy; the
shutdown carries `DurationDays = 3`. The authored resource is served by the same service type
the regional packs return — a custom calendar is a first-class citizen.

**APIs demonstrated.** `NotableDateDocumentBuilder.Create` / `WithMetadata` / `AddNotableDate`,
`NotableDateDefinitionBuilder.AsNonWorkingByDefault` / `AddRule`,
`NotableDateRuleBuilder.Fixed` / `DayOfWeekInMonth` / `WithDurationDays`, `Build()`,
`new NotableDateService(resource)`.

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

## NuGet equivalent

```bash
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Globalization.Calendar.Builder
```
