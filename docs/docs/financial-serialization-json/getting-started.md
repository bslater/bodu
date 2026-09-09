---
title: Bodu.Financial.Serialization.Json — Getting started
---

# Bodu.Financial.Serialization.Json — Getting started

Unfamiliar with terms like *policy*, *canonical object shape*, *compact form*, *scale*, or *keyed options*? Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.Financial.Serialization.Json
```

Targets `net8.0`. Depends on `Bodu.Financial` (and transitively `Bodu.Numerics` and `Bodu.Core`) and on `Microsoft.Extensions.DependencyInjection.Abstractions` for the `AddFinancialJson` registration; `System.Text.Json` is part of the shared framework.

## Minimal samples

### Register the converters and round-trip a typed amount

```csharp
using System.Text.Json;
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization.Json;

// Registration is required — the core types carry no [JsonConverter] attribute.
var options = new JsonSerializerOptions().AddFinancialJsonConverters();   // Strict

string json = JsonSerializer.Serialize(new Money<USD>(19.99m), options);
// {"amount":19.99,"currency":"USD"}

Money<USD> back = JsonSerializer.Deserialize<Money<USD>>(json, options);
```

`AddFinancialJsonConverters` returns the same options instance, so it chains inline. Call it once per options instance, before that instance is first used.

### Reject currency drift

```csharp
try
{
    JsonSerializer.Deserialize<Money<USD>>("""{ "amount": 19.99, "currency": "EUR" }""", options);
}
catch (JsonException ex)
{
    Console.WriteLine(ex.Message);   // the 'currency' value 'EUR' does not match the expected currency 'USD'
}
```

### Runtime-tagged amounts and bags

```csharp
Money invoice = JsonSerializer.Deserialize<Money>("""{ "amount": 2500, "currency": "JPY" }""", options);
// JPY 2500 — rounded to the registry's zero minor units

MoneyBag wallet = MoneyBag.Empty
    .Add(new Money<USD>(100m))
    .Add(new Money<EUR>(50m));

string bag = JsonSerializer.Serialize(wallet, options);
// {"balances":{"EUR":50.00,"USD":100.00}}
```

### Choose a policy

```csharp
var lenient = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Lenient);
var compact = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Compact);

// Lenient tolerates spreadsheet-style casing and whitespace on read.
Money<USD> imported = JsonSerializer.Deserialize<Money<USD>>(
    """{ "amount": 19.99, "currency": " usd " }""", lenient);

// Compact collapses each value to its smallest readable form.
JsonSerializer.Serialize(new Money<USD>(19.99m), compact);   // "19.99 USD"
JsonSerializer.Serialize(wallet, compact);                   // {"EUR":50.00,"USD":100.00}

Money<USD> either = JsonSerializer.Deserialize<Money<USD>>("\"USD 19.99\"", compact);   // ISO-prefix accepted too
```

### Keep a unit price's precision

```csharp
Money unitPrice = Money.FromExplicitScale(145.678912m, CurrencyCode.USD, 6);

JsonSerializer.Serialize(unitPrice, options);   // {"amount":145.678912,"currency":"USD","scale":6}
JsonSerializer.Serialize(unitPrice, compact);   // "145.678912 USD"

Money restored = JsonSerializer.Deserialize<Money>("\"145.678912 USD\"", compact);
Console.WriteLine(restored.MinorUnits);         // 6 — inferred from the printed digits
```

### Transport an unsettled amount

```csharp
CalculatedMoney running = new CalculatedMoney(0.0325125m, CurrencyCode.USD);   // e.g. a per-unit rate not yet settled

string wire = JsonSerializer.Serialize(running, options);
// {"amount":0.0325125,"currency":"USD"} — the unrounded decimal verbatim

CalculatedMoney received = JsonSerializer.Deserialize<CalculatedMoney>(wire, options);
Money settled = received.RoundToMoney();        // one rounding event, after transport
```

### Serialize a rate observation and a pair

```csharp
using Bodu.Financial.ExchangeRates;

var observation = new ExchangeRate(CurrencyCode.EUR, CurrencyCode.USD, new DateOnly(2024, 1, 3), 1.0956m, "ECB");

JsonSerializer.Serialize(observation, options);
// {"from":"EUR","to":"USD","date":"2024-01-03","rate":1.0956,"provider":"ECB","isInverted":false}

JsonSerializer.Serialize(observation, compact);
// {"pair":"EUR/USD","date":"2024-01-03","rate":1.0956,"provider":"ECB"}

JsonSerializer.Serialize(new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD), options);   // {"from":"EUR","to":"USD"}
JsonSerializer.Serialize(new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD), compact);   // "EUR/USD"
```

The rate reader accepts both shapes under any policy, so a `Compact` writer and a `Strict` reader interoperate.

### Register one converter by hand

When only one type needs a converter — or a single `TCurrency` — add it directly instead of the whole set:

```csharp
var narrow = new JsonSerializerOptions();
narrow.Converters.Add(new MoneyOfTCurrencyJsonConverter<USD>(FinancialJsonPolicy.Compact));   // Money<USD> only
narrow.Converters.Add(new CurrencyPairJsonConverter());                                       // Strict by default
```

### Register through dependency injection

`AddFinancialJson` registers a configured `JsonSerializerOptions` as a keyed singleton under `FinancialJsonServiceCollectionExtensions.JsonOptionsKey` (`"Financial"`), so the financial converters never leak into the application's default JSON options:

```csharp
using System.Text.Json;
using Bodu.Financial.Serialization.Json;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();
services.AddFinancialJson(FinancialJsonPolicy.Strict);

using ServiceProvider provider = services.BuildServiceProvider();

JsonSerializerOptions financialJson = provider.GetRequiredKeyedService<JsonSerializerOptions>(
    FinancialJsonServiceCollectionExtensions.JsonOptionsKey);

string payload = JsonSerializer.Serialize(new Money<USD>(19.99m), financialJson);
```

In a class, inject it with `[FromKeyedServices(FinancialJsonServiceCollectionExtensions.JsonOptionsKey)] JsonSerializerOptions options`.

## Where to go next

- **[Core concepts](concepts.md)** — vocabulary refresher.
- **[Introduction](index.md)** — the converter table and scenario index.
- **[Financial JSON serialization guide](../../guides/financial/json-serialization.md)** — every wire shape under every policy, the failure-mode table, and migration notes.
- **[Bodu.Financial getting started](../financial/getting-started.md)** — the money types being serialized.
- **[Bodu.Financial.Serialization.Json API reference](xref:Bodu.Financial.Serialization.Json)** — full type-by-type docs.
- **[Runnable samples](../../samples/financial.md)** — `Bodu.Financial.Samples.JsonSerialization` runs the three policies side by side; `Bodu.Financial.Samples.UnitPricing` runs every scale shape.
