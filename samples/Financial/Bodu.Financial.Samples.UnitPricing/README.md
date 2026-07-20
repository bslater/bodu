# Bodu.Financial.Samples.UnitPricing

Carrying prices at a higher precision than a currency's minor units — a six-decimal-place share
price in two-decimal USD — and serializing and deserializing them so the extra decimal places
survive the round-trip. A plain `Money` is settlement-grade and rounds to the currency's registered
minor units on construction; this sample shows the two ways to keep more precision (`Money` with an
explicit scale, and unrounded `CalculatedMoney`) and how the `Bodu.Financial.Serialization.Json`
converters preserve it on the wire.

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.UnitPricing
```

## Scenarios

### ExplicitScaleMoney (`Scenarios/ExplicitScaleMoney.cs`)

**Intent.** A `Money` normally rounds to its currency's minor units (USD = 2) the moment it is
constructed, so a six-place quote loses its extra places immediately. This scenario keeps the six
places with the explicit-scale factory `Money.FromExplicitScale` (showing the settlement route
through a custom-scale `MonetaryContext` produces the same value), then shows the Strict JSON shape
recording that scale on the wire so deserialization rebuilds the same precision.

**What it does.** Builds a plain `Money` from `145.678912` (which settles to `145.68`), then mints
the six-place price via `Money.FromExplicitScale(quoted, CurrencyCode.USD, 6)` and confirms the
`CalculatedMoney.RoundToMoney` settlement route with `ScalePolicy.Custom` / `CustomScale = 6`
yields an equal value. It serializes both shapes under Strict, deserializes the six-place value,
and finally round-trips a price of exactly `12.5` held at six places to show trailing zeros are
preserved.

**What to expect.**

```text
--- Explicit-scale Money: a 6-dp share price ---
Plain Money (settles to 2 dp) : USD 145.68  (MinorUnits = 2)
Unit-price Money (6 dp)       : USD 145.678912  (MinorUnits = 6)
Same via settlement route     : USD 145.678912  (equal: True)
Plain Money JSON              : {"amount":145.68,"currency":"USD"}
Unit-price JSON               : {"amount":145.678912,"currency":"USD","scale":6}
Deserialized unit price       : USD 145.678912  (MinorUnits = 6)
Trailing zeros preserved      : {"amount":12.5,"currency":"USD","scale":6} -> USD 12.500000
Compact form (scale kept)     : "145.678912 USD" -> USD 145.678912  (MinorUnits = 6)
```

The `"scale"` property appears **only** when a value's precision differs from its currency's
registered minor units — ordinary money keeps the two-field `{"amount","currency"}` shape, so
existing payloads are unaffected. On read, the reported `MinorUnits` is restored to `6`; the trailing
zeros are carried by that scale, not by the stored decimal, so `12.5` still formats as
`12.500000`. The terse Compact string form round-trips the precision too — the amount is printed at
the value's minor units (`"145.678912 USD"`) and the reader infers the scale from the number of
fractional digits, so no separate `scale` token is needed.

**APIs demonstrated.** `Money.FromExplicitScale`, `MonetaryContext` with `ScalePolicy.Custom` /
`CustomScale`, `CalculatedMoney.RoundToMoney(MonetaryContext)`, `Money.MinorUnits`,
`Money.ToString("R")`, `AddFinancialJsonConverters(FinancialJsonPolicy.Strict` / `Compact)`,
`JsonSerializer.Serialize` / `Deserialize`.

### CalculatedUnitPrice (`Scenarios/CalculatedUnitPrice.cs`)

**Intent.** When a price is genuinely un-settled — a per-unit rate that has not yet become a cash
amount — `CalculatedMoney` is the right carrier. It is never rounded on construction, so its decimal
already holds every significant digit (and trailing zeros) and serializes verbatim, with no scale
metadata required. Rounding happens exactly once, at settlement.

**What it does.** Round-trips a `CalculatedMoney` of `0.0325125` USD through the Strict shape,
multiplies the restored value by a quantity while still unrounded, and settles the line total to the
currency's minor units with a single `RoundToMoney` call.

**What to expect.**

```text
--- CalculatedMoney: unrounded unit price on the wire ---
Unit price (unrounded) : 0.0325125 USD
Serialized             : {"amount":0.0325125,"currency":"USD"}
Deserialized           : 0.0325125 USD
x 40,000 units (unrounded): 1300.5000000 USD
Settled line total     : USD 1300.50
```

`CalculatedMoney` serializes as the same `{"amount","currency"}` object as `Money`, but without a
`scale` property — the unrounded decimal is the precision. The line total stays unrounded through the
multiplication and only becomes `1300.50` at the final settlement.

**APIs demonstrated.** `CalculatedMoney` constructor, the `CalculatedMoney * long` operator,
`CalculatedMoney.RoundToMoney()`, `CalculatedMoneyJsonConverter` (via `AddFinancialJsonConverters`),
`JsonSerializer.Serialize` / `Deserialize`.

### PriceListDocument (`Scenarios/PriceListDocument.cs`)

**Intent.** Precision has to survive inside real object graphs, not just a bare value. This scenario
puts six-place prices inside a POCO portfolio, serializes the whole document, reads it back, and
settles each position — proving the scale round-trips through nested properties.

**What it does.** Defines a `Holding(string Ticker, long Shares, Money UnitPrice)` record, builds a
three-line portfolio with six-place unit prices, serializes it indented under Strict, deserializes
the array, and computes each settled position as `unitPrice × shares` rounded once.

**What to expect.**

```text
--- Price-list document: 6-dp prices inside a POCO ---
[
  {
    "Ticker": "ACME",
    "Shares": 1250,
    "UnitPrice": {
      "amount": 145.678912,
      "currency": "USD",
      "scale": 6
    }
  },
  ...
]
Settled positions (unit price kept at 6 dp, position settled to 2 dp):
  ACME      1,250 @ USD 145.678912   = USD 182098.64
  GLOBEX      800 @ USD 12.500000    = USD 10000.00
  INITECH   3,400 @ USD 7.049999     = USD 23970.00
```

Each `UnitPrice` serializes with its `scale`, and `GLOBEX`'s `12.500000` keeps its full six-place
form because the source literal already carried them. After deserialization every price still reports
six minor units, so the position settlement multiplies the exact per-share price before rounding once
to cents.

**APIs demonstrated.** `Money` as a POCO property, `Money.FromExplicitScale`,
`CalculatedMoney` settlement, `AddFinancialJsonConverters(FinancialJsonPolicy.Strict)` with
`JsonSerializerOptions { WriteIndented = true }`, `JsonSerializer.Serialize` / `Deserialize<T[]>`.

## Layout

```text
Bodu.Financial.Samples.UnitPricing/
  Program.cs                          # runs the scenarios in order
  Scenarios/ExplicitScaleMoney.cs
  Scenarios/CalculatedUnitPrice.cs
  Scenarios/PriceListDocument.cs
```

## Related

- `Bodu.Financial.Samples.MoneyBasics` — the rounding tiers (`Money`, `Money<TCurrency>`,
  `CalculatedMoney`), allocation, and formatting that this sample builds on.
- `Bodu.Financial.Samples.JsonSerialization` — the full converter registration, the three
  `FinancialJsonPolicy` wire shapes, and the keyed dependency-injection registration.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial
dotnet add package Bodu.Financial.Serialization.Json
```
