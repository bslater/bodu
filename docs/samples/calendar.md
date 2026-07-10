---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for the calendar packages under
[`samples/Globalization.Calendar/`](https://github.com/bslater/bodu/tree/master/samples/Globalization.Calendar).
Every sample is **offline and deterministic by construction** — the rule data ships as embedded
XML resources in the engine and data-pack assemblies, so there is nothing to configure and no
network to reach. The samples are members of `bodu.slnx` and are built and executed by CI, so
the code they show cannot drift from the current API. Each sample's README documents every
scenario individually: its intent, what the code does, the output to expect, and the APIs
demonstrated.

Run any sample from the repository root:

```bash
dotnet run --project samples/Globalization.Calendar/<SampleName>
```

## The samples

### Bodu.Globalization.Calendar.Samples.NotableDatesBasics

The front door: <xref:Bodu.Globalization.Calendar.AsiaPacificCalendarData> factories over the
embedded packs, year/day/range queries through
<xref:Bodu.Globalization.Calendar.INotableDateService>, ISO 3166-2 subdivision shadowing (AU vs
AU-VIC/AU-NSW Labour Day, pinned to published dates), composable
<xref:Bodu.Globalization.Calendar.NotableDateFilter> predicates, and observed-date substitution
with full lineage (`IsObserved` / `ActualDate` / `AdjustmentReason` — the AU 2021 Christmas
double-substitution). *Packages: `Bodu.Globalization.Calendar`,
`Bodu.Globalization.Calendar.AsiaPacific`.*

### Bodu.Globalization.Calendar.Samples.WorkingDays

The `Bodu.Extensions` business-day surface over a service: predicates, T+2 settlement with
`AddWorkingDays`, next/snap navigation, `WorkingDaysBetween` counting and lazy enumeration,
fiscal-period boundaries, and `WeekPattern` overrides for non-Mon–Fri working weeks. *Packages:
`Bodu.Globalization.Calendar`, `Bodu.Globalization.Calendar.AsiaPacific`.*

### Bodu.Globalization.Calendar.Samples.CustomCalendar

The "bring your own data" story:
<xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder> fluent authoring (fixed,
floating, and multi-day rules), declarative weekend-roll adjustment policies, importing the
shared catalogues through <xref:Bodu.Globalization.Calendar.CommonNotableDateResources> —
including the offset-anchor dependency the validator enforces — and the XML save/load round trip
that makes the document a distributable artifact. *Packages: `Bodu.Globalization.Calendar`,
`Bodu.Globalization.Calendar.Builder`.*

### Bodu.Globalization.Calendar.Samples.ServiceHosting

Container wiring: `AddNotableDateService` singleton registration, and
`AddReloadableNotableDateService` with a mid-run
<xref:Bodu.Globalization.Calendar.MutableNotableDateResourceProvider> `Reload` swap — held
service references immediately serve the new data. *Packages: `Bodu.Globalization.Calendar`,
`Bodu.Globalization.Calendar.DependencyInjection`, `Bodu.Globalization.Calendar.AsiaPacific`.*

### Bodu.Globalization.Calendar.Samples.CustomAlgorithm (+ .Test)

Extending the date-calculation vocabulary: a consumer-written
<xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> registered on a
<xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry>, referenced
declaratively from rules (`Algorithm("key")`), validated by the loader and dispatched by the
service — plus a five-line lambda adapter for one-off calculations. *Packages:
`Bodu.Globalization.Calendar`, `Bodu.Globalization.Calendar.Builder`.*

## Plugins, by choice

No sample ships a plugin assembly — a single-project plugin demo would have to load itself under
the dev-only `AllowAllPluginTrustPolicy`, which is exactly what production hosts must not do.
Instead, the CustomAlgorithm sample carries a fenced comment block showing the real switch: add
`Bodu.Globalization.Calendar.Plugins`, mark the plugin assembly with
`[assembly: NotableDatePlugin(...)]`, and load it through
<xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginLoader> under a pinned
`StrongNamePluginTrustPolicy`, registering the discovered algorithms into the same registry the
sample already uses.

## Testing your own data pack

`Bodu.Globalization.Calendar.Samples.CustomAlgorithm.Test` shows the in-repo pattern for
validating a consumer-built calendar: shape it as a data-pack factory (`SupportedCountries` /
`LoadResource` / `CreateService` — see the sample's `ContosoCalendarData`), derive
`CalendarDataTestsBase` from the `Bodu.Globalization.Calendar.Data.Test.Common` project — the
same base every regional `<Region>CalendarData` test derives — and add known-answer `[DataRow]`
rows pinning deterministic dates (exact assertions for Gregorian/weekend-roll rules; the base's
`AssertWithinDays` for lunar/astronomical tolerance). The base contributes the
load-and-resolve smoke contract; the rows pin your rules to published expectations.

The test-support project is repository-internal today (it is not packaged); if you consume the
packages from NuGet, copy the small base class into your test project — it depends only on the
engine and `MSTest.TestFramework`.
