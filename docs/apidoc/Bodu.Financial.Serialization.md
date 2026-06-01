---
uid: Bodu.Financial.Serialization
---

![Bodu.Financial](~/images/hero-financial.svg)

## Purpose

**Bodu.Financial.Serialization** ships the `System.Text.Json` converters for every monetary type in `Bodu.Financial`, plus the <xref:Bodu.Financial.Serialization.FinancialJsonPolicy> enum that switches between three wire shapes: a canonical object form for ledger storage, a lenient object form for import workflows, and a compact string form for log lines and API payloads where verbosity is undesirable.

## Static documentation

- **[Bodu.Financial introduction](~/docs/financial/index.md)** — how the converters fit into the broader monetary surface.
- **[Bodu.Financial getting started](~/docs/financial/getting-started.md)** — the JSON section shows how to register the policy.

## Key types

- <xref:Bodu.Financial.Serialization.FinancialJsonPolicy> — selects the wire shape (`Strict`, `Lenient`, `Compact`).
- <xref:Bodu.Financial.Serialization.MoneyJsonConverter`1>, <xref:Bodu.Financial.Serialization.MoneyJsonConverterFactory> — converter and factory for <xref:Bodu.Financial.Money`1>.
- <xref:Bodu.Financial.Serialization.MoneyValueJsonConverter> — converter for <xref:Bodu.Financial.MoneyValue>.
- <xref:Bodu.Financial.Serialization.MoneyBagJsonConverter> — converter for <xref:Bodu.Financial.MoneyBag>.
- <xref:Bodu.Financial.Serialization.ExchangeRateJsonConverter>, <xref:Bodu.Financial.Serialization.ExchangeRatePairJsonConverter> — converters for the FX value objects.
- <xref:Bodu.Financial.Serialization.FinancialJsonSerializerOptionsExtensions> — the registration extension method `AddFinancialJsonConverters(options, policy)`.

## Wire shapes

| Policy | `Money<TCurrency>` / `MoneyValue` | `MoneyBag` | Use when |
|---|---|---|---|
| `Strict` *(default)* | `{ "amount": 19.99, "currency": "USD" }` | `{ "balances": { "USD": 100.00, "EUR": 50.00 } }` | Storing canonical ledger payloads. Validates currency match, rejects duplicate keys. |
| `Lenient` | Same as Strict | Same as Strict | Importing third-party data. Normalises lowercase ISO codes, trims whitespace before validation. |
| `Compact` | `"19.99 USD"` | `{ "USD": 100.00, "EUR": 50.00 }` | Log lines, API payloads where verbosity matters. Accepts either `"19.99 USD"` or `"USD 19.99"` on read. |

## Example

```csharp
using System.Text.Json;
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

// Default (Strict) — no registration required, the converters are attached via [JsonConverter].
string strict = JsonSerializer.Serialize(new Money<USD>(19.99m));
// { "amount": 19.99, "currency": "USD" }

// Compact for log lines.
var options = new JsonSerializerOptions();
options.AddFinancialJsonConverters(FinancialJsonPolicy.Compact);

string compact = JsonSerializer.Serialize(new Money<USD>(19.99m), options);
// "19.99 USD"

// Lenient for ingest workflows.
var lenient = new JsonSerializerOptions();
lenient.AddFinancialJsonConverters(FinancialJsonPolicy.Lenient);

Money<USD> imported = JsonSerializer.Deserialize<Money<USD>>(
    "{ \"amount\": 19.99, \"currency\": \"usd\" }", lenient)!;
```

## Notes

- **Currency-mismatch on `Money<TCurrency>`.** Strict and Lenient policies both reject payloads whose `"currency"` field does not match `TCurrency.IsoCode` — drift surfaces as `JsonException` rather than a silently re-interpreted amount. `MoneyValue` accepts any ISO code and rounds to the registry's `MinorUnits` for that code.
- **MoneyBag pruning.** Zero balances are pruned on round-trip — the deserialised bag matches the canonical form, not the verbatim wire shape.
- **Strict vs. Lenient on Compact.** The compact policy accepts both `"19.99 USD"` and `"USD 19.99"` regardless of `Strict` / `Lenient` because there is no ambiguity to be strict about; lenient and strict behave identically under `Compact`.
- **AddFinancialJsonConverters.** Registers every converter on the same `JsonSerializerOptions` instance under a single policy. Call this once per options instance; mixing policies across types is not supported.
- **See also:** the [`Bodu.Financial` reference](~/apidoc/Bodu.Financial.md), the [`Money<TCurrency>` guide](~/guides/financial/money.md).
