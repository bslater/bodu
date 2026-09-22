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
<xref:Bodu.Globalization.Calendar.NotableDateFilter> predicates, observed-date substitution
with full lineage (`IsObserved` / `ActualDate` / `AdjustmentReason` — the AU 2021 Christmas
double-substitution), and expanding an observed-only result into the full actual + observed
timeline with `WithActualOccurrences()` on
<xref:Bodu.Globalization.Calendar.NotableDateSequenceExtensions>. *Packages:
`Bodu.Globalization.Calendar`, `Bodu.Globalization.Calendar.AsiaPacific`.*

### Bodu.Globalization.Calendar.Samples.WorkingDays

The `Bodu.Extensions` business-day surface over a service: predicates, T+2 settlement with
`AddWorkingDays`, next/snap navigation, `WorkingDaysBetween` counting and lazy enumeration,
fiscal-period boundaries, and `WeekPattern` overrides for non-Mon–Fri working weeks. *Packages:
`Bodu.Globalization.Calendar`, `Bodu.Globalization.Calendar.AsiaPacific`.*

### Bodu.Globalization.Calendar.Samples.CustomCalendar

The "bring your own data" story:
<xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder> fluent authoring — fixed
and floating rules, a **calculated-duration** year-end shutdown (`UntilDate`), and
**frequency-based** recurrences (daily-interval, weekly, monthly day, and monthly ordinal-weekday
schedules) — declarative weekend-roll adjustment policies, importing the shared catalogues through
<xref:Bodu.Globalization.Calendar.CommonNotableDateResources> (including the offset-anchor
dependency the validator enforces), and the XML and JSON save/load round trips that make the
document a distributable artifact. Its `FrequencyBasedSchedules` and `AuthoringCompanyHolidays` scenarios are
the runnable companions to the [Notable-date rule strategies](../guides/calendar/strategy-reference.md)
guide. *Packages: `Bodu.Globalization.Calendar`, `Bodu.Globalization.Calendar.Builder`.*

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

### Bodu.Globalization.Calendar.Samples.Caching

The caching layer: the read-through
<xref:Bodu.Globalization.Calendar.Caching.CachingNotableDateService> decorator over the
in-memory and JSON / TOML file backends (whole-territory, civil-year cache entries — warm queries,
sub-range clipping, and filtered overloads all served without re-resolving; a file-backed cache
lets a fresh service instance start warm), explicit `Warm` pre-resolution of a serving window,
and `AddCachedNotableDateService` decorating an already-registered `INotableDateService`. The
durable backends now run rather than being described: a SQLite-backed cache is read back by a
second service after the first is disposed, and two services over one `IDistributedCache`
stand in for two processes sharing a store — both proved by the engine call count not moving.
The hosted warm-up remains a fenced comment block. A
`CountingNotableDateService` wrapper counts the resolutions that reach the real engine, so every
cache hit is proved by call count rather than by timing. *Packages: `Bodu.Globalization.Calendar`,
`Bodu.Globalization.Calendar.Caching`, `Bodu.Globalization.Calendar.DependencyInjection`,
`Bodu.Globalization.Calendar.AsiaPacific`.*

### Bodu.Globalization.Calendar.Samples.RegionalData

The five regional data packs — `Bodu.Globalization.Calendar.Americas`, `.AsiaPacific`, `.Europe`,
`.MiddleEast`, and `.Africa`. Lists what each pack covers (63 territories between them) and
resolves a year from each, then shows why a notable-date lookup is territory-scoped: 1 January is
a public holiday in all five sample territories while 25 December is not one in `AE` at all. A
closing pass prints AU's 2027 weekend substitutions with the `ActualDate` each shifted from, so
the adjustment is auditable rather than implied. Every pack is an embedded resource, so nothing
here reads a file or makes a request. *Packages: the five `Bodu.Globalization.Calendar.<Region>`
data packs.*

### Bodu.Globalization.Calendar.Samples.Plugins (+ .Plugin.Contoso)

Trust-gated plugin loading with `Bodu.Globalization.Calendar.Plugins`. A date-calculation algorithm
is loaded out of a separate assembly at runtime and registered into a
<xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry>, after which a rule
document referencing it by key (`contoso.founding-day`) resolves alongside ordinary fixed rules —
neither side needing a type from the other. The second scenario is the reason the package exists:
loading a plugin executes arbitrary code in-process, so the loader takes no policy-free overload.
It pins the assembly with a SHA-256 allow-list, shows the same load refused under a wrong hash with
`PluginNotTrustedException` raised before any plugin code runs, then a `DelegatingPluginTrustPolicy`
for a host's own rule and a `CompositePluginTrustPolicy` that can only narrow trust.

The plugin is its own project, `…Samples.Plugin.Contoso`, referenced with
`ReferenceOutputAssembly="false"` so its types are genuinely unavailable to the host at compile
time — the host reaches it only through the loader. That project is a class library with no entry
point and is not run by the samples sweep. *Packages: `Bodu.Globalization.Calendar.Plugins`.*

### Bodu.Globalization.Calendar.Samples.RulePackToolchain

The rule-pack toolchain — `Bodu.Globalization.Calendar.Build` and the `bodu-calendar` tool from
`Bodu.Globalization.Calendar.Tool`. Unlike every other sample the interesting part happens at
**build** time: the csproj declares `rules/company-holidays.xml` as a `NotableDatePack` item, the
`CompileNotableDatePack` task lints and compiles it with the tool, and the sealed `.bcal` pack is
copied beside the application. The program is the consumer side, loading that pack with
<xref:Bodu.Globalization.Calendar.NotableDateResourceLoader.LoadBinary*> and comparing it against
parsing the same XML at runtime — same three holidays, 255 bytes against 1,946, and no schema
validation on the deployment path. A NuGet consumer writes only the item group; the sample's
explicit `.targets` import and task/tool path overrides are repository plumbing, needed because
here the task and tool are projects rather than a restored package. *Packages:
`Bodu.Globalization.Calendar.Build`, `Bodu.Globalization.Calendar.Tool`.*

### Bodu.Globalization.Calendar.Samples.ValidationLint

Collect-mode linting: `NotableDateDocumentBuilder.Validate()` / `TryBuild(...)` on clean,
semantically invalid, and structurally incomplete builders, and
`NotableDateResourceLoader.TryLoad` over arbitrary rule-pack text — malformed XML surfacing as
`BODU-CAL-SYNTAX`, semantic problems as their stable codes, and a valid pack loading — every
problem reported as a <xref:Bodu.Globalization.Calendar.NotableDateValidationDiagnostic> with a
stable `BODU-CAL-*` code instead of an exception, the shape build tasks and editor integrations
want. The code catalogue lives in the
[validation diagnostics guide](../guides/calendar/validation-diagnostics.md). *Packages:
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
