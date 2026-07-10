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

```
  2024: 2024-03-15 (Friday) Contoso Founding Day
  2026: 2026-03-13 (Friday) Contoso Founding Day
  1997: no occurrence (algorithm returned null)
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

```
  2024: 2024-06-28 (Friday) EOFY Party
  2025: 2025-06-27 (Friday) EOFY Party
```

Prefer a named class (like `CompanyFoundingDayAlgorithm`) once the calculation deserves its own
tests — which is exactly what the companion test project then does.

**APIs demonstrated.** The `INotableDateAlgorithm` contract's minimal surface, lambda adaption,
registry + loader + service wiring reused from the first scenario.

## The contract-test project

`../Bodu.Globalization.Calendar.Samples.CustomAlgorithm.Test/CompanyCalendarDataTests.cs`
derives `CalendarDataTestsBase` — the same base every regional data-pack test derives — over the
sample's authored calendar, plus known-answer rows pinning the algorithm's output for specific
years. It runs in CI automatically alongside the library suites.

## NuGet equivalent

```bash
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Globalization.Calendar.Builder
# plugin route (optional):
dotnet add package Bodu.Globalization.Calendar.Plugins
```
