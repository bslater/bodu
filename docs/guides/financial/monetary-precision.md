---
title: Monetary precision & unit pricing
---

# Monetary precision & unit pricing

A settled <xref:Bodu.Financial.Money> rounds to its currency's registered minor units the moment it
is constructed — two decimal places for USD, zero for JPY, three for BHD. That default is the right
one for cash amounts, but some values are legitimately *finer* than the currency settles at: a share
price quoted to six decimal places, a per-unit energy tariff in fractions of a cent, an FX-adjusted
unit cost. This guide covers how the financial stack represents those values, how their precision
behaves through arithmetic, and how it survives JSON serialization.

For the general three-tier rounding model (`Money<TCurrency>` per-step, `CalculatedMoney` deferred,
`Fraction<BigInteger>` exact), see [Working with `Money<TCurrency>`](money.md) — this page focuses
on the *scale-aware* surface layered on top of it.

## The one-line map

| Value | Type | Precision behaviour |
|---|---|---|
| Settled cash amount | <xref:Bodu.Financial.Money> / `Money<TCurrency>` | Rounds to the currency's registered minor units on construction and after every operation |
| Unit price at a known scale | `Money` via `FromExplicitScale` | Carries an explicit scale (0–28); reports, formats, rounds, and serializes at that scale |
| In-flight computed amount | <xref:Bodu.Financial.CalculatedMoney> | Never rounded; full `decimal` precision until settled once via `RoundToMoney` |

## Constructing a unit price

The direct route is `Money.FromExplicitScale`, which stores the supplied scale with the value:

```csharp
using Bodu.Financial;

Money price = Money.FromExplicitScale(145.678912m, CurrencyCode.USD, 6);

price.MinorUnits;      // → 6 (not USD's registered 2)
price.ToString("R");   // → "USD 145.678912"
```

The scale is a first-class part of the value, not a formatting hint:

- `MinorUnits` reports the explicit scale instead of the registry precision.
- Formatting pads to it — a six-place value of `12.5m` renders as `12.500000`.
- Arithmetic preserves it: adding two scale-6 values yields a scale-6 sum, and scalar
  multiplication or division rounds the result at six places, not two.
- Allocation splits at the value's own precision.

## Scale semantics

The rules for how scales interact follow the established prior art (`decimal` itself, Java's
`BigDecimal` / Joda-Money's `BigMoney`, dinero.js, SQL `NUMERIC`):

- **Mixed-scale addition and subtraction take the finer (maximum) scale.** A scale-2 settled amount
  plus a scale-6 unit price yields a scale-6 result, in either operand order — lossless, exact, no
  rounding. This mirrors `BigDecimal` (`max(scale₁, scale₂)`) and dinero.js's `normalizeScale`.

  ```csharp
  Money settled = new Money(1.00m, CurrencyCode.USD);                        // scale 2
  Money price = Money.FromExplicitScale(0.000001m, CurrencyCode.USD, 6);    // scale 6

  (settled + price).MinorUnits;   // → 6, amount 1.000001 — both operand orders agree
  ```

- **Multiplication and division round at the value's own scale**, each step. For chained
  sub-minor-unit math, carry the calculation in `CalculatedMoney` and settle once.
- **Equality is numeric** (`decimal` semantics): `12.50 USD` at scale 2 equals `12.500000 USD` at
  scale 6; the scale is not part of the identity or hash. Compare `MinorUnits` explicitly when the
  precision itself matters. (This is the `decimal` convention, deliberately unlike Java's
  scale-sensitive `BigDecimal.equals`.)
- **Text round-trip is scale-faithful.** `Money.Parse` reconstructs a finer-than-registry scale from
  the printed fractional digits, so `Money.Parse(value.ToString("R"))` restores the same amount,
  currency, *and* scale.
- **Changing a value's scale is explicit.** `Rescale(minorUnits, rounding)` re-expresses a value at
  a new scale — coarser rounds (a one-value settlement), finer pads losslessly — and `TrimScale()`
  drops trailing-zero precision down to (never below) the registered minor units. These are the
  counterparts of dinero.js's `transformScale`/`trimScale` and Joda `BigMoney.withScale`.

  ```csharp
  Money price = Money.FromExplicitScale(145.678912m, CurrencyCode.USD, 6);

  price.Rescale(2);                                              // USD 145.68  (scale 2)
  Money.FromExplicitScale(12.5m, CurrencyCode.USD, 6).TrimScale(); // USD 12.50 (scale 2, zeros trimmed)
  ```
- **`MoneyBag` and `Money<TCurrency>` are settlement surfaces.** Amounts entering a bag settle to
  the currency's registered minor units (banker's rounding) on entry — the bag's wire form carries
  no per-balance scale, so rounding on entry keeps memory and wire identical. `Money<TCurrency>`
  likewise stays at the registered precision by contract. Settle deliberately (via
  `CalculatedMoney.RoundToMoney`) before crossing either boundary when you need a different
  rounding rule.

For amounts that are *computed* rather than quoted, prefer the settlement route: accumulate in
<xref:Bodu.Financial.CalculatedMoney> (which never rounds) and settle exactly once through a
<xref:Bodu.Financial.MonetaryContext> whose <xref:Bodu.Financial.ScalePolicy> requests a custom
scale:

```csharp
var ctx = MonetaryContext.Default with
{
    ScalePolicy = ScalePolicy.Custom,
    CustomScale = 6,
};

Money price = new CalculatedMoney(145.678912m, CurrencyCode.USD).RoundToMoney(ctx);
```

Both routes produce the same explicit-scale `Money`; `FromExplicitScale` is the one-liner for values
whose precision is known up front, `RoundToMoney` is the single-rounding-decision boundary for
multi-step calculations.

## JSON wire shapes

With the `Bodu.Financial.Serialization.Json` converters registered
(`options.AddFinancialJsonConverters()`), all three
<xref:Bodu.Financial.Serialization.Json.FinancialJsonPolicy> shapes round-trip the precision.

### Strict / Lenient — the `scale` property

Ordinary money keeps the canonical two-field object shape. A value whose precision differs from its
currency's registered minor units additionally emits a `scale` property:

```json
{ "amount": 19.99, "currency": "USD" }
{ "amount": 145.678912, "currency": "USD", "scale": 6 }
```

On read, a payload carrying `scale` is reconstructed at that scale — including trailing zeros the
stored decimal cannot carry on its own, so `{ "amount": 12.5, "currency": "USD", "scale": 6 }`
deserializes to a value that formats as `12.500000`. A payload *without* `scale` deserializes at the
registry precision, so documents written before the property existed remain valid, and writers that
ignore the property keep producing ordinary money.

### Compact — scale by printed digits

The compact string form pads the amount to the value's minor units, so the fractional-digit count
*is* the scale, and the reader infers it — no separate metadata token:

```json
"19.99 USD"
"145.678912 USD"
```

Reading `"145.678912 USD"` yields a `Money` reporting `MinorUnits == 6`; reading `"19.99 USD"`
yields ordinary registry-precision USD. Inference applies only to scales *finer* than the
registered precision — a scale coarser than the registry (whole-dollar pricing in a two-decimal
currency) is preserved by the object shapes' `scale` property, not by the compact form.

### `CalculatedMoney` — verbatim

An unrounded <xref:Bodu.Financial.CalculatedMoney> serializes its full `decimal` amount exactly as
stored — every significant digit and any trailing zeros — with no scale metadata, because the
decimal itself carries the precision:

```json
{ "amount": 0.0325125, "currency": "USD" }
{ "amount": 12.500000, "currency": "USD" }
```

Use it as the wire form for prices that have not yet been settled to cash; settle after transport
with `RoundToMoney`.

## Worked example — a price list

A six-decimal-place price inside a POCO round-trips through `System.Text.Json` like any other
property:

```csharp
public sealed record Holding(string Ticker, long Shares, Money UnitPrice);

var options = new JsonSerializerOptions().AddFinancialJsonConverters();

var holding = new Holding("ACME", 1_250, Money.FromExplicitScale(145.678912m, CurrencyCode.USD, 6));

string json = JsonSerializer.Serialize(holding, options);
// {"Ticker":"ACME","Shares":1250,"UnitPrice":{"amount":145.678912,"currency":"USD","scale":6}}

Holding restored = JsonSerializer.Deserialize<Holding>(json, options)!;
restored.UnitPrice.MinorUnits;   // → 6

// Settle the position once, at the currency's precision:
Money position = (new CalculatedMoney(restored.UnitPrice.Amount, restored.UnitPrice.Code)
    * restored.Shares).RoundToMoney();   // → USD 182098.64
```

## Failure modes

| Payload | Outcome |
|---|---|
| `"scale"` outside 0–28 | `JsonException` (`Money.FromExplicitScale` bounds the scale to `decimal`'s ceiling) |
| `"scale"` non-integer or a string | `JsonException` |
| Duplicate `"scale"` (or `"amount"` / `"currency"`) | `JsonException` — last-write-wins on financial payloads is a data-integrity hazard |
| Unknown or malformed ISO code | `JsonException` |

## See also

- [Working with `Money<TCurrency>`](money.md) — the three-tier rounding model, allocation,
  formatting, and the base JSON wire shapes.
- The `Bodu.Financial.Samples.UnitPricing` sample
  ([samples catalogue](../../samples/financial.md)) — runs every shape on this page end to end.
- The `Bodu.Financial.Samples.JsonSerialization` sample — the full converter registration and the
  three policy shapes side by side.
- [`Money` API reference](xref:Bodu.Financial.Money)
- [`CalculatedMoney` API reference](xref:Bodu.Financial.CalculatedMoney)
- [`MonetaryContext` API reference](xref:Bodu.Financial.MonetaryContext)
- [`ScalePolicy` API reference](xref:Bodu.Financial.ScalePolicy)
- [`FinancialJsonPolicy` API reference](xref:Bodu.Financial.Serialization.Json.FinancialJsonPolicy)
