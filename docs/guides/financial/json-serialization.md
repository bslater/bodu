---
title: JSON serialization
---

# JSON serialization

`Bodu.Financial.Serialization.Json` is the companion package that round-trips the `Bodu.Financial` value types through `System.Text.Json`. It covers <xref:Bodu.Financial.Money`1>, <xref:Bodu.Financial.Money>, <xref:Bodu.Financial.CalculatedMoney>, <xref:Bodu.Financial.MoneyBag>, <xref:Bodu.Financial.ExchangeRates.ExchangeRate>, and <xref:Bodu.Financial.ExchangeRates.CurrencyPair>.

The core `Bodu.Financial` library is deliberately **serialization-agnostic** — the value types carry no `[JsonConverter]` attribute and take no dependency on `System.Text.Json`. JSON support is opt-in through this package, so a consumer of just `Money<TCurrency>` pays nothing for the serializer. Install it alongside the core package:

```shell
dotnet add package Bodu.Financial.Serialization.Json
```

This guide consolidates the JSON material that was previously spread across the [money](money.md#json-wire-shape), [monetary precision](monetary-precision.md#json-wire-shapes), [dependency injection](dependency-injection.md#consuming-the-financial-json-options), and package landing pages; those sections remain as short pointers.

## Registration

Register the converters once per `JsonSerializerOptions` with `AddFinancialJsonConverters`, then serialize as normal:

```csharp
using System.Text.Json;
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization.Json;

var options = new JsonSerializerOptions().AddFinancialJsonConverters();

string json = JsonSerializer.Serialize(new Money<USD>(19.99m), options);
// → {"amount":19.99,"currency":"USD"}

Money<USD> value = JsonSerializer.Deserialize<Money<USD>>(json, options);
// → USD 19.99
```

`AddFinancialJsonConverters` registers a coherent converter set for all six types from one policy value and returns the same options instance for chaining. Without it, `JsonSerializer` silently falls back to reflection-shaped output for these types — it does not throw — so register before the first use of an options instance.

## Choosing a policy

Pass a <xref:Bodu.Financial.Serialization.Json.FinancialJsonPolicy> to select the wire shape:

```csharp
var compact = new JsonSerializerOptions()
    .AddFinancialJsonConverters(FinancialJsonPolicy.Compact);
```

| Policy | Money shape | Bag shape | Use for |
|---|---|---|---|
| `Strict` (default) | object `{ "amount": 19.99, "currency": "USD" }` | wrapped `{ "balances": { "USD": 100.00 } }` | Canonical persistence, ledgers, audit. |
| `Lenient` | as `Strict`; reads also accept lowercase and padded ISO codes | as `Strict` | Spreadsheet / external-feed ingest. |
| `Compact` | string `"19.99 USD"` | flat `{ "USD": 100.00 }` | Compact payloads and readable log lines. |

Under `Strict` and `Lenient`, property names compare case-insensitively, duplicate properties are rejected, and unknown properties are ignored. `Lenient` writes the same shape as `Strict` — it only differs on *read*, where it normalizes lowercase ISO codes to uppercase and trims surrounding whitespace before validation; it is an *import* convenience, so persist with `Strict` or `Compact`. Compact reads accept either the `"19.99 USD"` or the `"USD 19.99"` arrangement and reuse each type's `TryParse` path under the invariant culture, so payloads are stable regardless of the ambient culture.

## Wire shapes by converter

### `Money<TCurrency>`

```csharp
JsonSerializer.Serialize(new Money<USD>(19.99m), options);   // → {"amount":19.99,"currency":"USD"}
JsonSerializer.Serialize(new Money<USD>(19.99m), compact);   // → "19.99 USD"
```

The reader verifies that `currency` matches `TCurrency.IsoCode` and throws `JsonException` on a mismatch — drift between the persisted currency and the code's expectation surfaces as an error rather than a silent re-interpretation. The amount is written as a JSON number and read as either a number or a numeric string. `Money<TCurrency>` never carries a `scale` property; its precision is fixed by the currency tag.

### `Money`

```csharp
JsonSerializer.Serialize(new Money(2500m, CurrencyCode.JPY), options);   // → {"amount":2500,"currency":"JPY"}
JsonSerializer.Serialize(new Money(2500m, CurrencyCode.JPY), compact);   // → "2500 JPY"
```

`Money` accepts any code the shipped `CurrencyCode` catalogue defines, rounding to that currency's registered minor units, and rejects one it does not — an unknown or custom code in the payload throws rather than deserializing. A value carrying an explicit scale additionally emits `scale`; see [Scale and precision](#scale-and-precision).

### `CalculatedMoney`

```csharp
CalculatedMoney running = new CalculatedMoney(12.500000m, CurrencyCode.USD);   // trailing zeros are significant here

JsonSerializer.Serialize(running, options);   // → {"amount":12.500000,"currency":"USD"}
JsonSerializer.Serialize(running, compact);   // → "12.500000 USD"
```

The unrounded tier is written **verbatim** — every significant digit and any trailing zeros the stored `decimal` carries — and read back unchanged, with no scale metadata, because the decimal itself carries the precision. Settle after transport with `RoundToMoney()`.

### `MoneyBag`

```csharp
MoneyBag wallet = MoneyBag.Empty.Add(new Money<USD>(100m)).Add(new Money<EUR>(50m));

JsonSerializer.Serialize(wallet, options);   // → {"balances":{"EUR":50.00,"USD":100.00}}
JsonSerializer.Serialize(wallet, compact);   // → {"EUR":50.00,"USD":100.00}
```

Balances are written in lexicographic ISO order and read as numbers or numeric strings. Zero balances are pruned on every bag operation, so the deserialized bag matches the canonical form, not the verbatim wire shape.

### `ExchangeRate`

```csharp
var observation = new ExchangeRate(CurrencyCode.EUR, CurrencyCode.USD, new DateOnly(2024, 1, 3), 1.0956m, "ECB");

JsonSerializer.Serialize(observation, options);
// → {"from":"EUR","to":"USD","date":"2024-01-03","rate":1.0956,"provider":"ECB","isInverted":false}

JsonSerializer.Serialize(observation, compact);
// → {"pair":"EUR/USD","date":"2024-01-03","rate":1.0956,"provider":"ECB"}
```

The canonical object writes `from`, `to`, `date` (ISO `yyyy-MM-dd`), `rate`, `provider`, and `isInverted` in that order. Two members are conditional under every policy: `observedRate` (the natively quoted rate) is written only when `isInverted` is `true`, and `fetchedAtUtc` (round-trip `O` format) only when the observation records a fetch instant. `Compact` folds the currencies into a single `"pair": "FROM/TO"` property and omits `isInverted` when it is `false`. The reader accepts **both** shapes regardless of policy — the presence of `pair` versus `from` / `to` selects the parse — so a `Compact` writer and a `Strict` reader interoperate.

### `CurrencyPair`

```csharp
JsonSerializer.Serialize(new CurrencyPair(CurrencyCode.USD, CurrencyCode.JPY), options);   // → {"from":"USD","to":"JPY"}
JsonSerializer.Serialize(new CurrencyPair(CurrencyCode.USD, CurrencyCode.JPY), compact);   // → "USD/JPY"
```

`Lenient` applies the same uppercase-and-trim normalization to both codes. The compact string must contain exactly one slash with a code on each side.

## Scale and precision

All three policies round-trip the precision of a <xref:Bodu.Financial.Money> that carries an **explicit minor-unit scale** — a unit price finer than its currency's registered precision, created with `Money.FromExplicitScale(amount, code, minorUnits)`:

```csharp
Money unitPrice = Money.FromExplicitScale(145.678912m, CurrencyCode.USD, 6);

JsonSerializer.Serialize(unitPrice, options);   // → {"amount":145.678912,"currency":"USD","scale":6}
JsonSerializer.Serialize(unitPrice, compact);   // → "145.678912 USD"
```

- **Object shapes (`Strict` / `Lenient`).** Ordinary money keeps the two-field object. A value whose precision differs from the registry additionally emits `scale`, and the reader reconstructs the value at that scale — including trailing zeros, so `{ "amount": 12.5, "currency": "USD", "scale": 6 }` deserializes to a value that formats as `12.500000`. A payload *without* `scale` deserializes at the registry precision, so documents written before the property existed remain valid and writers that ignore it keep producing ordinary money. `scale` must be an integer between 0 and 28 (`decimal`'s ceiling).
- **Compact.** The string form pads the amount to the value's minor units, so the fractional-digit count *is* the scale and the reader infers it: `"145.678912 USD"` yields a `Money` reporting `MinorUnits == 6`; `"19.99 USD"` yields ordinary registry-precision USD. Inference applies only to scales *finer* than the registry — a coarser scale (whole-dollar pricing in a two-decimal currency) is preserved by the object shapes' `scale` property, not by the compact form.
- **`CalculatedMoney`.** No scale metadata at all — the verbatim decimal carries it.

See [Monetary precision & unit pricing](monetary-precision.md) for the three-tier model these shapes serve.

## Registration surfaces

Three surfaces exist; pick the narrowest one that covers your need:

| Surface | Scope | Policy selection |
|---|---|---|
| `options.AddFinancialJsonConverters(policy)` | Everything serialized with that `JsonSerializerOptions` | Any policy; registers all six converters coherently. |
| `services.AddFinancialJson(policy)` | A keyed `JsonSerializerOptions` singleton in the container | Any policy; see [Dependency injection](#dependency-injection). |
| Manual `options.Converters.Add(...)` of a converter or the factory | Whatever you add — e.g. only pairs, or only one `TCurrency` | Any policy, per instance: `new MoneyOfTCurrencyJsonConverterFactory(FinancialJsonPolicy.Compact)` covers every `Money<TCurrency>`; `new MoneyOfTCurrencyJsonConverter<USD>(FinancialJsonPolicy.Compact)` covers `Money<USD>` only. A converter constructed without a policy defaults to `Strict`. |

`AddFinancialJsonConverters` throws `ArgumentNullException` for a null options instance, `ArgumentOutOfRangeException` for an undefined policy value, and `InvalidOperationException` when the options instance has already been used for (de)serialization and its `Converters` collection has become read-only — configure options before first use. Mixing policies across types on one options instance is not supported.

## Dependency injection

`services.AddFinancialJson(policy)` registers a configured `JsonSerializerOptions` as a **keyed singleton** under `FinancialJsonServiceCollectionExtensions.JsonOptionsKey` (`"Financial"`). Keying keeps the financial converters out of the application's default JSON options (an ASP.NET Core response pipeline, for example):

```csharp
using System.Text.Json;
using Bodu.Financial.Serialization.Json;
using Microsoft.Extensions.DependencyInjection;

services.AddFinancialJson(FinancialJsonPolicy.Strict);

JsonSerializerOptions financialJson =
    provider.GetRequiredKeyedService<JsonSerializerOptions>(
        FinancialJsonServiceCollectionExtensions.JsonOptionsKey);

string payload = JsonSerializer.Serialize(new Money<USD>(19.99m), financialJson);
```

Inject it into a service with `[FromKeyedServices(FinancialJsonServiceCollectionExtensions.JsonOptionsKey)] JsonSerializerOptions options`. The registration is a plain `IServiceCollection` extension and does not require `AddFinancialService` from `Bodu.Financial.DependencyInjection`; the two compose freely.

## Failure modes

Malformed payloads surface as `JsonException` on read; the converters never silently coerce:

| Input | Policy | Result |
|---|---|---|
| Token is not an object (e.g. a bare string) | `Strict`, `Lenient` | `JsonException` — object form expected. |
| Token is not a string (money, `CalculatedMoney`, `CurrencyPair`) | `Compact` | `JsonException` — compact string expected. |
| Missing `"amount"` or `"currency"`; missing required `ExchangeRate` property | `Strict`, `Lenient` | `JsonException` naming the missing property. |
| Duplicate `"amount"`, `"currency"`, `"scale"`, or any `ExchangeRate` property | `Strict`, `Lenient` | `JsonException` — duplicates are rejected, never last-wins. |
| `"currency"` that does not match `TCurrency.IsoCode` (`Money<TCurrency>`) | `Strict`, `Lenient` | `JsonException` — currency mismatch. |
| Code that is not three uppercase letters, or is outside the shipped catalogue (`Money`, `CalculatedMoney`, `MoneyBag`) | all | `JsonException` — unknown currency rejected. |
| `"amount"` that is neither a number nor a numeric string; `"currency"` that is not a string | `Strict`, `Lenient` | `JsonException` reporting the type mismatch. |
| `"scale"` non-integer, or outside 0–28 | `Strict`, `Lenient` | `JsonException`. |
| `"rate"` not a number, `"date"` not `yyyy-MM-dd`, `"isInverted"` not a boolean | `Strict`, `Lenient` | `JsonException` naming the property. |
| Compact string `TryParse` rejects (e.g. `"19.99"`, `"USD/"`, `"USD/JPY/EUR"`) | `Compact` | `JsonException` carrying the offending text. |
| `"balances"` not an object; a balance neither a number nor a numeric string | `Strict`, `Lenient` | `JsonException`. |
| Truncated document | all | `JsonException` — unexpected end. |

Unknown properties are *ignored* (skipped), matching the BCL convention for forward compatibility.

## Migration notes

- **The converters live in the companion package.** Code that serialized `Money<TCurrency>` through `System.Text.Json` needs a reference to `Bodu.Financial.Serialization.Json` and a `using Bodu.Financial.Serialization.Json;`; the core package exposes no JSON surface.
- **JSON registration is not part of `AddFinancialService`.** The container registration is `services.AddFinancialJson(policy)` in this package, not a member of the `Bodu.Financial.DependencyInjection` builder; the financial DI package deliberately carries no `System.Text.Json` dependency.
- **`CalculatedMoney` is covered.** `AddFinancialJsonConverters` registers the <xref:Bodu.Financial.Serialization.Json.CalculatedMoneyJsonConverter> alongside the other five; older material that lists five converters predates it.
- **`scale` is additive.** Documents written before the `scale` property existed deserialize unchanged at the registry precision; a reader that predates it ignores the property (unknown properties are skipped) and settles the amount at the registry precision — acceptable for ordinary money, lossy for unit prices, so upgrade readers before writers when unit prices are in play.
- **`ExchangeRate` readers accept both shapes.** Switching a writer from `Strict` to `Compact` (or back) does not require coordinating readers; `isInverted`, `observedRate`, and `fetchedAtUtc` are read whenever present.
- **`Lenient` writes `Strict`.** Selecting `Lenient` for an import pipeline does not change what that pipeline writes back out.

## See also

- [Working with `Money<TCurrency>`](money.md) — the value types being serialized.
- [Monetary precision & unit pricing](monetary-precision.md) — explicit scale and the `CalculatedMoney` tier.
- [Financial dependency injection](dependency-injection.md) — the rest of the container surface the keyed options compose with.
- [Bodu.Financial.Serialization.Json introduction](../../docs/financial-serialization-json/index.md) · [concepts](../../docs/financial-serialization-json/concepts.md) · [getting started](../../docs/financial-serialization-json/getting-started.md) — the package landing pages.
- [Numerics JSON serialization](../numerics/json-serialization.md) — the sibling companion package with the same policy model.
- [Bodu.Financial guides](index.md) — the member overview for this package.
- [`FinancialJsonPolicy`](xref:Bodu.Financial.Serialization.Json.FinancialJsonPolicy) · [`FinancialJsonSerializerOptionsExtensions`](xref:Bodu.Financial.Serialization.Json.FinancialJsonSerializerOptionsExtensions) · [`FinancialJsonServiceCollectionExtensions`](xref:Bodu.Financial.Serialization.Json.FinancialJsonServiceCollectionExtensions)
- [Bodu.Financial.Serialization.Json API reference](xref:Bodu.Financial.Serialization.Json) — full namespace overview.
- The `Bodu.Financial.Samples.JsonSerialization` and `Bodu.Financial.Samples.UnitPricing` samples ([samples catalogue](../../samples/financial.md)).
