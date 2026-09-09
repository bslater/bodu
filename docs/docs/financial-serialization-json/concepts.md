---
title: Bodu.Financial.Serialization.Json — Core concepts
---

# Bodu.Financial.Serialization.Json — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [financial JSON guide](../../guides/financial/json-serialization.md), and refer back whenever a term feels imprecise.

Part of the **[Numerics & Financial](../topics/numerics-and-financial.md)** topic. For the high-level shape of the package, start with the [introduction](index.md); the money vocabulary — minor units, `Money<TCurrency>` vs `Money`, `CalculatedMoney`, `MoneyBag` — lives in the [Bodu.Financial concepts](../financial/concepts.md) page.

## Serialization-agnostic core

`Bodu.Financial` carries no `[JsonConverter]` attribute and no reference to `System.Text.Json`; this package supplies both. The consequence worth remembering: **registration is required**. Serializing a `Money<USD>` with a `JsonSerializerOptions` that has not had `AddFinancialJsonConverters` called on it does not throw — `JsonSerializer` falls back to its reflection-based object shape, which does not round-trip. Configure the options before first use; once an options instance has been used, its `Converters` collection is read-only and `AddFinancialJsonConverters` throws `InvalidOperationException`.

## Policy

A <xref:Bodu.Financial.Serialization.Json.FinancialJsonPolicy> is passed once — to `AddFinancialJsonConverters`, to `AddFinancialJson`, or to a converter's constructor — and fixes the wire shape and parsing strictness for every financial type serialized with that options instance:

| Policy | Writes | Reads |
|---|---|---|
| `Strict` (`0`, default) | The canonical object shapes. | The canonical shapes only. Property names compare case-insensitively; duplicate properties are rejected; unknown properties are ignored; `Money<TCurrency>` requires `currency == TCurrency.IsoCode`. |
| `Lenient` (`1`) | Exactly what `Strict` writes. | Everything `Strict` reads, after normalizing lowercase ISO codes to uppercase and trimming surrounding whitespace. |
| `Compact` (`2`) | The single-string and flat forms. | The compact forms; money strings in either `"19.99 USD"` or `"USD 19.99"` arrangement; an `ExchangeRate` in either the compact or the canonical object shape. |

A converter constructed without a policy defaults to `Strict`.

## Canonical object shape

Under `Strict` and `Lenient`, every money value is an object with an `amount` (a JSON number) and a `currency` (a three-letter ISO 4217 string):

```json
{ "amount": 19.99, "currency": "USD" }
```

`Money<TCurrency>` reads it by verifying the currency matches the type parameter; `Money` reads it by resolving the code against the shipped catalogue and rounding to that currency's minor units. On read, `amount` may also be a numeric *string*, so a payload produced by a system that cannot carry arbitrary-precision JSON numbers still round-trips.

## Compact form

Under `Compact`, a money value is a single string — the amount rendered in the invariant culture, padded to the value's minor units, a space, and the ISO code:

```json
"19.99 USD"
```

Reads accept either arrangement (`"19.99 USD"` or `"USD 19.99"`) and reuse the type's `TryParse` path for the numeric component. `Strict` and `Lenient` never differ under `Compact` — there is no casing or whitespace ambiguity for the lenient rules to relax.

## Scale

A <xref:Bodu.Financial.Money> can carry an **explicit minor-unit scale** that differs from its currency's registered minor units — a six-decimal unit price in a two-decimal currency, created with `Money.FromExplicitScale(amount, code, minorUnits)`. The converters preserve it:

- The object shapes add a `scale` property — `{ "amount": 145.678912, "currency": "USD", "scale": 6 }` — and the reader reconstructs the value at that scale, including trailing zeros. A payload without `scale` deserializes at the registry precision, so documents written before the property existed remain valid.
- The compact form encodes the scale in the printed digits — `"145.678912 USD"` reads back with `MinorUnits == 6`. Inference applies only to scales *finer* than the registry; a coarser scale is preserved only by the object shapes.

`Money<TCurrency>` has no `scale` property: its precision is fixed by the currency tag.

## Verbatim `CalculatedMoney`

<xref:Bodu.Financial.CalculatedMoney> is the unrounded, deferred-arithmetic tier, so its converter writes the full `decimal` amount exactly as stored — every significant digit and any trailing zeros — with no scale metadata, because the decimal itself carries the precision: `{ "amount": 0.0325125, "currency": "USD" }` or `"0.0325125 USD"`. Settle after transport with `RoundToMoney()`.

## Bag shapes

A <xref:Bodu.Financial.MoneyBag> is a wrapped ISO-keyed map under `Strict` / `Lenient` and a flat map under `Compact`:

```json
{ "balances": { "EUR": 50.00, "USD": 100.00 } }
{ "EUR": 50.00, "USD": 100.00 }
```

Balances are written in lexicographic ISO order and may be read as numbers or numeric strings. Because the bag prunes zero balances on every operation, a bag deserializes to its canonical form — a `"JPY": 0` entry on the wire does not survive the round trip.

## Rate and pair shapes

An <xref:Bodu.Financial.ExchangeRates.ExchangeRate> observation carries its provenance on the wire. `Strict` / `Lenient` write the full canonical object in declaration order — `from`, `to`, `date` (ISO `yyyy-MM-dd`), `rate`, `provider`, `isInverted` — adding `observedRate` when the rate was derived from the reverse pair and `fetchedAtUtc` (round-trip `O` format) when the observation records a fetch instant. `Compact` combines the currencies into one `"pair": "EUR/USD"` property, drops `isInverted` unless it is `true`, and keeps the same optional members. The reader accepts both shapes regardless of policy — the presence of `pair` versus `from` / `to` selects the parse.

A <xref:Bodu.Financial.ExchangeRates.CurrencyPair> is `{ "from": "USD", "to": "JPY" }` in the object shapes and `"USD/JPY"` under `Compact`.

## Factory vs closed converters

<xref:Bodu.Financial.Money`1> is an open generic, so `AddFinancialJsonConverters` registers a <xref:Bodu.Financial.Serialization.Json.MoneyOfTCurrencyJsonConverterFactory> that binds the concrete `TCurrency` per request and produces the matching <xref:Bodu.Financial.Serialization.Json.MoneyOfTCurrencyJsonConverter`1>. The other five types are non-generic and register as single converters. Any of them can be added to `JsonSerializerOptions.Converters` by hand — `new MoneyOfTCurrencyJsonConverter<USD>(FinancialJsonPolicy.Compact)` covers `Money<USD>` only — when a narrower registration is wanted.

## Keyed options in the container

`services.AddFinancialJson(policy)` registers one configured `JsonSerializerOptions` as a **keyed singleton** under `FinancialJsonServiceCollectionExtensions.JsonOptionsKey` (`"Financial"`). Keying keeps the financial converters from leaking into the application's default JSON options — an ASP.NET Core response pipeline, for example — and lets a consumer resolve them explicitly with `GetRequiredKeyedService<JsonSerializerOptions>(JsonOptionsKey)` or a `[FromKeyedServices]` parameter.

## Failure modes

Every malformed payload surfaces as `JsonException`; the converters never coerce silently:

| Input | Result |
|---|---|
| A token that is not an object under `Strict` / `Lenient`, or not a string under `Compact` | `JsonException` naming the expected form. |
| Missing `amount` or `currency`; a missing required `ExchangeRate` property | `JsonException` naming the missing property. |
| Duplicate `amount`, `currency`, `scale`, or any `ExchangeRate` property | `JsonException` — duplicates are rejected. |
| `amount` that is neither a number nor a numeric string; `currency` that is not a string | `JsonException` reporting the type mismatch. |
| `currency` that does not match `TCurrency.IsoCode` on `Money<TCurrency>` | `JsonException` — currency mismatch. |
| A code that is not three uppercase letters, or is not in the shipped ISO 4217 catalogue (`Money`, `CalculatedMoney`, `MoneyBag`) | `JsonException` — unknown currency rejected. |
| `scale` that is not an integer, or outside `0`–`28` | `JsonException`. |
| A compact string `TryParse` rejects (`"19.99"`, `"USD/"`) | `JsonException` carrying the offending text. |
| `balances` that is not an object; a balance that is neither a number nor a numeric string | `JsonException`. |
| A truncated document | `JsonException` — unexpected end. |

## Where to go next

- **[Getting started](getting-started.md)** — install + runnable minimal samples for every concept above.
- **[Introduction](index.md)** — the converter table and scenario index.
- **[Financial JSON serialization guide](../../guides/financial/json-serialization.md)** — every wire shape under every policy, with migration notes.
- **[Monetary precision & unit pricing](../../guides/financial/monetary-precision.md)** — where explicit scale and `CalculatedMoney` come from.
- **[Bodu.Financial concepts](../financial/concepts.md)** — the money vocabulary.
- **[Bodu.Financial.Serialization.Json API reference](xref:Bodu.Financial.Serialization.Json)** — full type-by-type docs.
