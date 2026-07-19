# Bodu.Financial.Samples.JsonSerialization

`System.Text.Json` integration for `Bodu.Financial`: the `Bodu.Financial.Serialization.Json`
companion package registers converters on a `JsonSerializerOptions` so the serialization-agnostic
core types — `Money`, `Money<TCurrency>`, `MoneyBag`, `ExchangeRate`, `CurrencyPair` — round-trip
through a coherent wire shape chosen by a single `FinancialJsonPolicy`, including the keyed
dependency-injection registration.

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.JsonSerialization
```

## Scenarios

### RegisterConverters (`Scenarios/RegisterConverters.cs`)

**Intent.** The core `Bodu.Financial` types carry no `[JsonConverter]` attribute — the library is
serialization-agnostic — so nothing round-trips until the converters are registered. This scenario
shows the one call that wires them all up and proves each monetary type re-reads to an equal value.

**What it does.** Calls `new JsonSerializerOptions().AddFinancialJsonConverters()` once (defaulting
to the `Strict` policy), then serializes and deserializes a `Money<USD>` (compile-time-typed
amount), a `Money` (runtime-typed amount carrying its `CurrencyCode`), and a two-currency
`MoneyBag`, comparing each restored value against the original.

**What to expect.**

```text
--- Register converters: Money, Money<TCurrency>, MoneyBag ---
Money<USD> : {"amount":19.99,"currency":"USD"}
           -> USD 19.99 (round-trips equal: True)
Money      : {"amount":12.34,"currency":"EUR"}
           -> EUR 12.34 (round-trips equal: True)
MoneyBag   : {"balances":{"EUR":12.34,"USD":19.99}}
           -> USD 19.99, EUR 12.34
```

`Money` and `Money<USD>` are value types, so a failed round-trip would show `False`; both report
`True`. The `MoneyBag` serializes as an object with a `"balances"` map, and the restored bag still
answers `TryGetBalance` for both currencies.

**APIs demonstrated.** `FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters`,
`Money.Of<TCurrency>`, `Money.From(decimal, CurrencyCode)`, `MoneyBag.Of`,
`MoneyBag.TryGetBalance`, `JsonSerializer.Serialize` / `Deserialize`.

### ExchangeRateJson (`Scenarios/ExchangeRateJson.cs`)

**Intent.** The same registration also covers the exchange-rate value types, so a rate observation
and a currency-pair identity persist to JSON without any bespoke wiring. This scenario round-trips
both from fixed inputs.

**What it does.** Constructs a fixed `ExchangeRate` (USD→JPY, a fixed date and provider) and a
`CurrencyPair` (EUR/USD), serializes each under the Strict policy, deserializes them back, and
prints the reconstructed fields.

**What to expect.**

```text
--- ExchangeRate and CurrencyPair round-trips ---
ExchangeRate : {"from":"USD","to":"JPY","date":"2024-03-15","rate":148.25,"provider":"SampleFeed","isInverted":false}
             -> USD/JPY @ 148.25 on 2024-03-15 [SampleFeed]
CurrencyPair : {"from":"EUR","to":"USD"}
             -> EUR/USD (round-trips equal: True)
```

The Strict `ExchangeRate` shape is the full canonical object (`from`/`to`/`date`/`rate`/`provider`/
`isInverted`); `CurrencyPair` is just the ordered `{"from":..,"to":..}` identity. Because both are
`record struct` types, the `CurrencyPair` equality check confirms an exact round-trip.

**APIs demonstrated.** `ExchangeRate` constructor, `CurrencyPair` constructor,
`ExchangeRate.From` / `To` / `Rate` / `Date` / `Provider`, `CurrencyPair.From` / `To`,
`AddFinancialJsonConverters` (Strict).

### PolicyShapes (`Scenarios/PolicyShapes.cs`)

**Intent.** One `FinancialJsonPolicy` argument selects the wire shape for *every* registered
converter at once. This scenario serializes one set of fixed values under all three policies so the
shapes sit side by side, and shows the `Lenient` read normalising a dirty ISO code.

**What it does.** Registers three separate options instances — `Strict`, `Compact`, `Lenient` —
and serializes the same `Money<USD>`, `MoneyBag`, and `ExchangeRate` under Strict and Compact.
Finally it deserializes `{"amount":12.34,"currency":"  usd  "}` under the Lenient policy.

**What to expect.**

```text
--- Policy shapes: Strict vs Lenient vs Compact ---
Strict :
  Money<USD>   {"amount":19.99,"currency":"USD"}
  MoneyBag     {"balances":{"EUR":12.34,"USD":19.99}}
  ExchangeRate {"from":"USD","to":"JPY","date":"2024-03-15","rate":148.25,"provider":"SampleFeed","isInverted":false}
Compact :
  Money<USD>   "19.99 USD"
  MoneyBag     {"EUR":12.34,"USD":19.99}
  ExchangeRate {"pair":"USD/JPY","date":"2024-03-15","rate":148.25,"provider":"SampleFeed"}
Lenient :
  read {"currency":"  usd  "} -> USD 12.34
```

`Strict` is the canonical object form for ledgers and audit data. `Compact` collapses money to a
single `"amount ISO"` string, the bag to a flat `{ "ISO": amount }` map, and the rate to a `"pair"`
property (dropping `isInverted` when false). `Lenient` shares the Strict shape on the wire but reads
forgivingly — the padded lowercase `"  usd  "` is trimmed and upcased to `USD` rather than rejected.

**APIs demonstrated.** `FinancialJsonPolicy.Strict` / `Lenient` / `Compact`,
`AddFinancialJsonConverters(FinancialJsonPolicy)`, `Money.Of<TCurrency>`, `MoneyBag.Of`,
`ExchangeRate` constructor, `JsonSerializer.Serialize` / `Deserialize`.

### DiKeyedOptions (`Scenarios/DiKeyedOptions.cs`)

**Intent.** In a DI application the configured options should be registered once and resolved by
key, not rebuilt at each call site. This scenario shows the companion package's DI entry point and
the keyed resolution consumers use.

**What it does.** Registers `services.AddFinancialJson(FinancialJsonPolicy.Compact)` on a
`ServiceCollection`, builds the provider, resolves the `JsonSerializerOptions` with the
`FinancialJsonServiceCollectionExtensions.JsonOptionsKey` key (the literal string `"Financial"`),
serializes a `Money<USD>` and a `MoneyBag` with it, and resolves the key a second time to confirm
the same singleton comes back.

**What to expect.**

```text
--- DI keyed JsonSerializerOptions (key "Financial") ---
Resolved key : "Financial"
Money<USD>   : "19.99 USD"
MoneyBag     : {"EUR":12.34,"USD":19.99}
Same instance: True
```

The serialized output is the Compact shape because the *keyed options* carry the policy passed at
registration — consumers ask for `JsonSerializerOptions` by key rather than configuring their own.
`Same instance: True` confirms `AddFinancialJson` registers a keyed singleton. This entry point
lives in the serialization companion and does not require the core `AddFinancialService`.

**APIs demonstrated.** `FinancialJsonServiceCollectionExtensions.AddFinancialJson`,
`FinancialJsonServiceCollectionExtensions.JsonOptionsKey`,
`IServiceProvider.GetRequiredKeyedService<JsonSerializerOptions>`, `ServiceCollection` /
`BuildServiceProvider`.

## Layout

```text
Bodu.Financial.Samples.JsonSerialization/
  Program.cs                          # runs the scenarios in order
  Scenarios/RegisterConverters.cs
  Scenarios/ExchangeRateJson.cs
  Scenarios/PolicyShapes.cs
  Scenarios/DiKeyedOptions.cs
```

## Related

- `Bodu.Financial.Samples.MoneyBasics` — money arithmetic, allocation, rounding tiers, formatting
  and parsing; its `JsonPolicies` scenario is the lighter, single-scenario tour of the policies this
  sample covers in depth.
- `Bodu.Financial.Samples.CurrencyServices` — the ambient currency-resolution seam, named monetary
  contexts, and the `AddFinancialService` composition root that also registers financial JSON.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial
dotnet add package Bodu.Financial.Serialization.Json
```
