# Bodu.Financial.Serialization.Json

> **API stability - Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

`System.Text.Json` integration for **[Bodu.Financial](https://www.nuget.org/packages/Bodu.Financial)**. The core library is deliberately serialization-agnostic - its monetary types carry no `[JsonConverter]` attribute and take no `System.Text.Json` integration of their own - so JSON support is opt-in through this companion package (the NodaTime companion-package pattern):

- **`AddFinancialJsonConverters(options, policy)`** - one call registers a coherent converter set for every serializable `Bodu.Financial` type.
- Converters (+ a factory for the generic type) for **`Money`**, **`Money<TCurrency>`**, **`CalculatedMoney`**, **`MoneyBag`**, **`ExchangeRate`**, and **`CurrencyPair`**.
- **`FinancialJsonPolicy`** - `Strict` (canonical object shapes, the default), `Lenient` (Strict plus read tolerance for external feeds), `Compact` (single strings such as `"19.99 USD"` and flat ISO→amount maps).
- **`AddFinancialJson(services, policy)`** - dependency-injection registration of a configured `JsonSerializerOptions` as a keyed singleton (key `"Financial"`).

> **Migration note.** Earlier `Bodu.Financial` builds shipped these converters in the core package and serialized through type-level `[JsonConverter]` attributes with zero configuration. The attributes have been removed: **registration is now mandatory**. Serializing a monetary type on an options instance without the converters does not throw - `JsonSerializer` silently falls back to reflection-shaped output, and reads money back as zero or currency-less values - so audit any `JsonSerializer` call sites for these types when upgrading.

## Installation

```shell
dotnet add package Bodu.Financial.Serialization.Json
```

Targets `net8.0` and `net10.0`. Depends on `Bodu.Financial`.

## Usage

Register the converters once, before the options instance is first used:

```csharp
using System.Text.Json;
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization.Json;

var options = new JsonSerializerOptions().AddFinancialJsonConverters();   // Strict

string json = JsonSerializer.Serialize(new Money(19.99m, CurrencyCode.USD), options);
// → {"amount":19.99,"currency":"USD"}

var compact = new JsonSerializerOptions()
    .AddFinancialJsonConverters(FinancialJsonPolicy.Compact);
// Money → "19.99 USD", CurrencyPair → "USD/JPY", MoneyBag → { "EUR": 12.34, "USD": 19.99 }
```

One policy value shapes every registered converter:

| Policy | Money / Money\<TCurrency> | MoneyBag | ExchangeRate / CurrencyPair | Intended use |
|---|---|---|---|---|
| `Strict` (default) | object `{ "amount": 19.99, "currency": "USD" }`; duplicate properties, mismatched or lowercase ISO codes rejected | object with a `"balances"` map; a repeated `balances` property or ISO key rejected | canonical object shapes (`from` / `to` / `date` / `rate` / `provider`; `from` / `to`) | Ledger, persistence, and audit data. |
| `Lenient` | as `Strict`, plus lowercase ISO codes and surrounding whitespace accepted on read | as `Strict`, with the same read tolerance for the ISO keys | as `Strict`, with the same read tolerance | External-feed ingest. Writes as `Strict`. |
| `Compact` | string `"19.99 USD"` | flat ISO→amount object | `ExchangeRate` object with a combined `"pair"`; `CurrencyPair` string `"USD/JPY"` | Compact payloads for APIs and logs. |

Converters can also be registered individually (for example only the typed money form) via `options.Converters.Add(new MoneyOfTCurrencyJsonConverterFactory(FinancialJsonPolicy.Compact))`.

## Dependency injection

For containers, `AddFinancialJson` registers a configured `JsonSerializerOptions` as a keyed singleton under `FinancialJsonServiceCollectionExtensions.JsonOptionsKey` (`"Financial"`):

```csharp
using Bodu.Financial.Serialization.Json;

services.AddFinancialJson(FinancialJsonPolicy.Compact);

var json = provider.GetRequiredKeyedService<JsonSerializerOptions>(
    FinancialJsonServiceCollectionExtensions.JsonOptionsKey);
```

`Bodu.Financial.DependencyInjection`'s `AddFinancialService` does not register JSON options - pair it with the call above when financial JSON is needed.

## Documentation

See the [JSON serialization guide](https://github.com/bslater/bodu/blob/master/docs/guides/financial/json-serialization.md) for the full wire-shape reference and error behaviour.

## License

MIT. © Bodu Pty. Ltd.
