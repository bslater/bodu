---
uid: Bodu.Financial.Serialization.Json
---

![Bodu.Financial.Serialization.Json](~/images/hero-financial-json.svg)

# Bodu.Financial.Serialization.Json

## Purpose

**Bodu.Financial.Serialization.Json** carries the `System.Text.Json` integration for [`Bodu.Financial`](Bodu.Financial.md). It supplies the converters that round-trip <xref:Bodu.Financial.Money>, <xref:Bodu.Financial.Money`1>, <xref:Bodu.Financial.MoneyBag>, <xref:Bodu.Financial.ExchangeRates.ExchangeRate>, and <xref:Bodu.Financial.ExchangeRates.CurrencyPair> to and from JSON, together with a one-call extension that registers them under a chosen policy and a dependency-injection registration for containers.

The core `Bodu.Financial` library is serialization-agnostic — its monetary types carry no `[JsonConverter]` attribute. Add this package and call `AddFinancialJsonConverters` to opt into JSON support and select a wire policy across a whole `JsonSerializerOptions` instance.

## Static documentation

- **[Bodu.Financial introduction](~/docs/financial/index.md)** — how the converters fit into the broader monetary surface.
- **[Bodu.Financial getting started](~/docs/financial/getting-started.md)** — the JSON section shows how to register the policy.

## Key types

- <xref:Bodu.Financial.Serialization.Json.FinancialJsonSerializerOptionsExtensions> — `AddFinancialJsonConverters(JsonSerializerOptions, FinancialJsonPolicy)` registers the converter set on an options instance and returns it for chaining.
- <xref:Bodu.Financial.Serialization.Json.FinancialJsonPolicy> — selects the wire shape (`Strict`, `Lenient`, `Compact`).
- <xref:Bodu.Financial.Serialization.Json.MoneyOfTCurrencyJsonConverter`1>, <xref:Bodu.Financial.Serialization.Json.MoneyOfTCurrencyJsonConverterFactory> — converter and factory for <xref:Bodu.Financial.Money`1>.
- <xref:Bodu.Financial.Serialization.Json.MoneyJsonConverter> — converter for <xref:Bodu.Financial.Money>.
- <xref:Bodu.Financial.Serialization.Json.MoneyBagJsonConverter> — converter for <xref:Bodu.Financial.MoneyBag>.
- <xref:Bodu.Financial.Serialization.Json.ExchangeRateJsonConverter>, <xref:Bodu.Financial.Serialization.Json.CurrencyPairJsonConverter> — converters for the FX value objects.
- <xref:Bodu.Financial.Serialization.Json.FinancialJsonServiceCollectionExtensions> — the dependency-injection registration `AddFinancialJson(services, policy)`, a keyed `JsonSerializerOptions` singleton under `JsonOptionsKey` (`"Financial"`).

## Wire shapes

| Policy | `Money<TCurrency>` / `Money` | `MoneyBag` | Use when |
|---|---|---|---|
| `Strict` *(default)* | `{ "amount": 19.99, "currency": "USD" }` | `{ "balances": { "USD": 100.00, "EUR": 50.00 } }` | Storing canonical ledger payloads. Validates currency match, rejects duplicate keys. |
| `Lenient` | Same as Strict | Same as Strict | Importing third-party data. Normalises lowercase ISO codes, trims whitespace before validation. |
| `Compact` | `"19.99 USD"` | `{ "USD": 100.00, "EUR": 50.00 }` | Log lines, API payloads where verbosity matters. Accepts either `"19.99 USD"` or `"USD 19.99"` on read. |

## Example

```csharp
using System.Text.Json;
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization.Json;

// Registration is required — the core types carry no [JsonConverter] attribute.
var strict = new JsonSerializerOptions().AddFinancialJsonConverters();

string ledger = JsonSerializer.Serialize(new Money<USD>(19.99m), strict);
// { "amount": 19.99, "currency": "USD" }

// Compact for log lines.
var options = new JsonSerializerOptions()
    .AddFinancialJsonConverters(FinancialJsonPolicy.Compact);

string compact = JsonSerializer.Serialize(new Money<USD>(19.99m), options);
// "19.99 USD"

// Lenient for ingest workflows.
var lenient = new JsonSerializerOptions()
    .AddFinancialJsonConverters(FinancialJsonPolicy.Lenient);

Money<USD> imported = JsonSerializer.Deserialize<Money<USD>>(
    "{ \"amount\": 19.99, \"currency\": \"usd\" }", lenient)!;
```

## Notes

- **Registration is required.** The core monetary types carry no `[JsonConverter]` attribute, so JSON support is opt-in: call `AddFinancialJsonConverters` (or add converters to `JsonSerializerOptions.Converters`) before serializing. Without registration, `JsonSerializer` silently falls back to reflection-shaped output rather than throwing.
- **Currency-mismatch on `Money<TCurrency>`.** Strict and Lenient policies both reject payloads whose `"currency"` field does not match `TCurrency.IsoCode` — drift surfaces as `JsonException` rather than a silently re-interpreted amount. `Money` accepts any ISO code and rounds to the registry's `MinorUnits` for that code.
- **MoneyBag pruning.** Zero balances are pruned on round-trip — the deserialised bag matches the canonical form, not the verbatim wire shape.
- **Strict vs. Lenient on Compact.** The compact policy accepts both `"19.99 USD"` and `"USD 19.99"` regardless of `Strict` / `Lenient` because there is no ambiguity to be strict about; lenient and strict behave identically under `Compact`.
- **AddFinancialJsonConverters.** Registers every converter on the same `JsonSerializerOptions` instance under a single policy. Call this once per options instance; mixing policies across types is not supported.
- **See also:** the [`Bodu.Financial` reference](xref:Bodu.Financial), the [`Money<TCurrency>` guide](~/guides/financial/money.md).
