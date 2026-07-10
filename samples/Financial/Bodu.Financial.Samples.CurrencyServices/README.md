# Bodu.Financial.Samples.CurrencyServices

Currency services and host wiring: the ambient resolution seam that runtime `Money` reads its
currency metadata through, named monetary contexts for per-domain rounding policy, and the
`AddFinancialService` composition root that ties it together in a container.

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.CurrencyServices
```

## Scenarios

### AmbientResolution (`Scenarios/AmbientResolution.cs`)

**Intent.** Runtime `Money` resolves currency metadata (minor units, names, parse validation)
through the ambient `CurrencyResolution.Current` lookup rather than a threaded dependency. The
seam has two levers: `PushScoped` swaps the lookup for a scope (async-flow-safe — the test
lever), and `SetDefault` is the one-time composition-root promotion. This scenario shows the
scoped lever changing observable parsing behaviour.

**What it does.** Queries the default lookup for AUD metadata, parses `"THB 25.00"` (accepted —
THB is in the full ISO registry), then pushes a `RestrictedCurrencyLookup` (a delegating
allow-list decorator over the current lookup, defined in this sample) scoped to AUD/USD/EUR:
inside the scope USD parses but THB is rejected; after disposing the scope THB parses again.

**What to expect.**

```
AUD: Australian Dollar, 2 minor units, cash increment 0.05
Parse "THB 25.00" (default lookup)   : THB 25.00
Parse "USD 10.00" (restricted scope) : USD 10.00
Parse "THB 25.00" (restricted scope) : rejected - not an allowed currency
Parse "THB 25.00" (scope disposed)   : THB 25.00
```

The same parse call gives three different outcomes purely from the ambient scope — that is the
seam working. A system that settles in a fixed currency set installs a lookup like
`RestrictedCurrencyLookup` so unsupported currencies fail *everywhere* the seam is consulted,
not just at hand-written checkpoints.

**APIs demonstrated.** `CurrencyResolution.Current` / `PushScoped` (and `SetDefault` by
contrast), `ICurrencyLookup` (all six members, via the delegating decorator), `Money.Parse` /
`TryParse` consulting the ambient lookup.

### NamedContexts (`Scenarios/NamedContexts.cs`)

**Intent.** Different parts of one application legitimately settle money under different rules —
retail totals round away from zero, treasury keeps banker's rounding. `MonetaryContext` bundles
that policy as an immutable record, and named (keyed) DI registrations let each consumer ask for
its own by name instead of hard-coding policy at call sites.

**What it does.** Derives a "Retail" context from `MonetaryContext.Default` with a `with`
override (`Rounding = AwayFromZero`), settles the same computed value (19.985 USD) under Retail
and Treasury (default) policies, registers both via `AddMonetaryContext("name", ctx)`, and
resolves the Retail context back out of the container as a keyed service.

**What to expect.**

```
Computed value : 19.985 USD (unrounded)
Retail  settle : USD 19.99   (away from zero)
Treasury settle: USD 19.98   (banker's rounding)
Keyed "Retail" : USD 19.99   (resolved from DI)
```

19.985 is a midpoint: away-from-zero pushes it up to 19.99, banker's rounding takes the even
neighbour 19.98. One cent, two policies, both explicit. The keyed resolution proves the DI path
yields the identical settlement — consumers take `[FromKeyedServices("Retail")] MonetaryContext`.

**APIs demonstrated.** `MonetaryContext.Default` + record `with` overrides,
`MidpointRoundingStrategy.AwayFromZero` / `ToEven`, `CalculatedMoney.RoundToMoney(context)`,
`AddMonetaryContext(name, context)`, `GetRequiredKeyedService<MonetaryContext>`.

### FinancialServiceHost (`Scenarios/FinancialServiceHost.cs`)

**Intent.** The composition root, end to end: one `AddFinancialService()` call registers the
core services; the returned builder adds JSON policy and a rate provider; and
`UseCurrencyResolution()` promotes the container's lookup to the ambient seam once at startup.

**What it does.** Composes a `ServiceCollection` with `AddFinancialService()`,
`AddFinancialJson(Compact)`, and an offline `FixedDatedRateProvider` instance registered via
`AddDatedExchangeRateProvider`; builds the provider; calls `UseCurrencyResolution()`; then
consumes each registration — the `ICurrencyLookup` (numeric-code query), the keyed financial
`JsonSerializerOptions`, and the `IDatedRateProvider`.

**What to expect.**

```
Numeric 036   : AUD (Australian Dollar)
Financial JSON: "19.99 USD"  (Compact policy)
AUD/USD       : 0.6580 [Config]
```

The JSON line prints the compact string shape because the *keyed options* carry the policy the
builder configured — consumers resolve `JsonSerializerOptions` by the
`FinancialServiceBuilderExtensions.JsonOptionsKey` key instead of building their own. The rate
line comes from the registered offline instance; a live provider package would replace that one
registration with its `Add<Source>ExchangeRates()` and nothing else changes.

**APIs demonstrated.** `AddFinancialService()`, `AddFinancialJson(FinancialJsonPolicy)`,
`AddDatedExchangeRateProvider(instance)`, `ServiceProviderExtensions.UseCurrencyResolution`,
`ICurrencyLookup.TryByNumericCode`, keyed `JsonSerializerOptions` resolution,
`IDatedRateProvider` consumption.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial.DependencyInjection
```
