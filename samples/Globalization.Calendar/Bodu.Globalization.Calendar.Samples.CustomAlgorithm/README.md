# Bodu.Globalization.Calendar.Samples.CustomAlgorithm

Extending the date-calculation vocabulary with your own `INotableDateAlgorithm`: register it by
key, reference the key declaratively from rules, and let the loader validate and the service
dispatch it. The companion test project (`../Bodu.Globalization.Calendar.Samples.CustomAlgorithm.Test`)
proves the sample's calendar against the shared data-pack contract base.

```bash
dotnet run --project samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.CustomAlgorithm
dotnet test samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.CustomAlgorithm.Test
```

## Scenarios

### RegistryRegistration (`Scenarios/RegistryRegistration.cs`)

**Intent.** The full custom-algorithm loop, offline and in one project: an algorithm is code
that answers exactly one question — "what date in this year?" — while everything around it
(category, territory, adjustments, emission) stays declarative in the rule that references it.

**What it does.** Registers `CompanyFoundingDayAlgorithm` (the Friday of the week containing the
12 March founding anniversary; `null` before 1998) under the key `company-founding`; authors a
document whose rule is `Algorithm("company-founding")`; loads it with
`NotableDateResourceLoader.Load(xml, resolver, algorithms)` — the loader validates the key
exists — and serves it with `NotableDateServiceOptions.Algorithms` so the service can dispatch.
Resolves 2024, 2026, and 1997.

**What to expect.**

```text
    2024: 2024-03-15 (Friday) Contoso Founding Day
    2026: 2026-03-13 (Friday) Contoso Founding Day
    1997: no occurrence (algorithm returned null)
  (the shift off the anniversary is inside the algorithm, not a declarative adjustment - and 1997 returns null rather than a nonsense date)
```

Both hits land on Fridays (the algorithm's contract); 1997 is silent because the algorithm
returned `null` — the "not applicable this year" convention every built-in calculator follows.
A fenced comment block shows the plugin alternative: loading the same algorithm from an external
assembly through `NotableDatePluginLoader` under a `StrongNamePluginTrustPolicy` (the
`Bodu.Globalization.Calendar.Plugins` package), for hosts that take algorithms as deployment
artifacts rather than compile-time references.

**APIs demonstrated.** `INotableDateAlgorithm`, `NotableDateAlgorithmRegistry.Register`,
`NotableDateRuleBuilder.Algorithm(key)`, `NotableDateResourceLoader.Load(xml, resolver,
algorithms)`, `NotableDateServiceOptions.Algorithms`.

### DelegateAlgorithms (`Scenarios/DelegateAlgorithms.cs`)

**Intent.** `INotableDateAlgorithm` is a single method, so a five-line adapter turns any
`Func<int, DateOnly?>` into a registrable algorithm — the right shape for one-off calculations
that will not be reused or unit-tested on their own.

**What it does.** Defines a private `LambdaAlgorithm` adapter (the whole class is in the
scenario file), registers a lambda computing the last Friday of June ("EOFY Party"), and
resolves two years through the same author-load-serve loop.

**What to expect.**

```text
    2024: 2024-06-28 (Friday) EOFY Party
    2025: 2025-06-27 (Friday) EOFY Party
  (both Fridays, both different calendar dates - the lambda runs per year rather than returning a stored answer)
```

Prefer a named class (like `CompanyFoundingDayAlgorithm`) once the calculation deserves its own
tests — which is exactly what the companion test project then does.

**APIs demonstrated.** The `INotableDateAlgorithm` contract's minimal surface, lambda adaption,
registry + loader + service wiring reused from the first scenario.

### ObservationBasedVariant (`Scenarios/ObservationBasedVariant.cs`)

**Intent.** Not every custom calculation has to be written by the consumer. Show a built-in
observation-based algorithm — `tehran-nowruz` — referenced from a document with nothing
registered.

**What it does.** Authors a rule over the built-in key and resolves three consecutive years across
the March 20/21 boundary.

**What to expect.**

```text
  Nowruz 2024: 2024-03-20
  Nowruz 2025: 2025-03-21
  Nowruz 2026: 2026-03-21
  (the 20/21 March boundary reproduced from the equinox instant at the Tehran meridian - what a fixed date or a tabular approximation gets wrong)
```

Nowruz falls on the day containing the true vernal equinox measured at the Tehran standard
meridian, so which calendar date it lands on depends on whether that instant falls before or after
local apparent noon — which a tabular Persian calendar only approximates, disagreeing with the
official date in some years. Shipping the astronomical computation as a built-in key means a
document can carry the official rule without its author implementing celestial mechanics, and
keeping it opt-in means nothing changes for documents anchored on the tabular calendar on purpose.

**APIs demonstrated.** The built-in `tehran-nowruz` algorithm key via
`NotableDateRuleBuilder.Algorithm`, resolved with no registry supplied.

## The contract-test project

`../Bodu.Globalization.Calendar.Samples.CustomAlgorithm.Test/CompanyCalendarDataTests.cs`
derives `CalendarDataTestsBase` — the same base every regional data-pack test derives — over the
sample's authored calendar, plus known-answer rows pinning the algorithm's output for specific
years. It runs in CI automatically alongside the library suites.

## Layout

```text
Bodu.Globalization.Calendar.Samples.CustomAlgorithm/
  Program.cs                              # runs the scenarios in order
  SampleConsole.cs                        # the What / Why / Expect scenario banner
  CompanyFoundingDayAlgorithm.cs          # the named INotableDateAlgorithm implementation
  ContosoCalendarData.cs                  # the sample data-pack factory
  Scenarios/RegistryRegistration.cs
  Scenarios/DelegateAlgorithms.cs
  Scenarios/ObservationBasedVariant.cs
```

## NuGet equivalent

```bash
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Globalization.Calendar.Builder
# plugin route (optional):
dotnet add package Bodu.Globalization.Calendar.Plugins
```
