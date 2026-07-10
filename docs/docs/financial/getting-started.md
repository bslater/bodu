---
title: Bodu.Financial — Getting started
---

# Bodu.Financial — Getting started

## Install

```bash
dotnet add package Bodu.Financial
```

Targets `net8.0`. References `Bodu.Numerics` (for the `Fraction<BigInteger>` precision escape hatch) and `Bodu.Core` (for shared argument validation).

## Minimal samples

### Typed monetary arithmetic (`Money<TCurrency>`)

```csharp
using Bodu.Financial;
using Bodu.Financial.Currencies;

Money<USD> dinner = new Money<USD>(54.30m);
Money<USD> tip    = dinner * 0.18m;
Money<USD> total  = dinner + tip;       // OK — same currency

Money<JPY> sushi = new Money<JPY>(2500m);
var oops = dinner + sushi;              // Compile error — cannot add USD to JPY
```

Construction rounds to the currency's minor-unit precision using banker's rounding:

- `MinorUnits = 0` — `JPY`, `KRW`, `CLP`, `ISK`, `VND`, `XAF`, `XOF`, …
- `MinorUnits = 2` — `USD`, `EUR`, `GBP`, `AUD`, `CAD`, `CHF`, and most others.
- `MinorUnits = 3` — `BHD`, `IQD`, `JOD`, `KWD`, `LYD`, `OMR`, `TND`.

### Cross-currency conversion

```csharp
Money<USD> usd = new Money<USD>(100m);
Money<JPY> jpy = usd.Convert<JPY>(155.5m);   // 15,550 JPY (0 dp)
Money<EUR> eur = usd.Convert<EUR>(0.93m);    // 93.00 EUR (2 dp)
```

No implicit conversion — the rate must be supplied. Rounding is applied to the destination currency's minor-unit precision.

### Fair allocation

```csharp
Money<USD>[] shares = new Money<USD>(0.10m).Allocate(3);
// [0.04, 0.03, 0.03]  — sums to exactly 0.10

decimal[] ratios = { 1m, 1m, 2m };
Money<USD>[] split = new Money<USD>(100m).Allocate(ratios);
// [25.00, 25.00, 50.00]
```

The residual minor units are distributed one per share from the start of the array, so the sum equals the original exactly.

### Exact intermediate arithmetic via `Fraction<BigInteger>`

```csharp
using System.Numerics;
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Numerics;

Money<USD> principal = new Money<USD>(1000m);
Fraction<BigInteger> growth = Fraction<BigInteger>.One
    + Fraction<BigInteger>.Create(5, 1200);   // 5% / 12 months

Fraction<BigInteger> exact = principal.ToFraction();
for (int i = 0; i < 360; i++) exact *= growth;   // 30-year amortization, no drift

Money<USD> balance = Money<USD>.FromFraction(exact);   // one rounding event
```

Single-step variant: `principal.MultiplyExact(growth)`.

### Deferred rounding with `CalculatedMoney`

When you only need to defer rounding across a chain of `decimal` steps
— not full rational exactness — `Money<T>.ToCalculated()` returns a
runtime-tagged <xref:Bodu.Financial.CalculatedMoney> that carries the
full `decimal` precision through arithmetic and rounds once, at the
settlement boundary:

```csharp
CalculatedMoney running = new Money<USD>(100m).ToCalculated();
running = running * 1.05m / 3m;              // no rounding yet
Money settled = running.RoundToMoney();      // single rounding event → Money
```

### Integer minor-unit storage

`Money<T>.ToMinorUnits()` and `FromMinorUnits(long)` bridge to the
integer-cent representation ledgers and wire formats use:

```csharp
long cents = new Money<USD>(19.99m).ToMinorUnits();    // 1999
Money<USD> back = Money<USD>.FromMinorUnits(1999);     // USD 19.99
```

### Runtime-tagged amounts (`Money`)

```csharp
Money invoice = JsonSerializer.Deserialize<Money>(payload)!;
// invoice could be "USD 19.99", "EUR 19.99", or "JPY 200" — same code path.

Money total = invoice + new Money(5m, invoice.IsoCode);

Money<USD> typed = invoice.As<USD>();        // throws if mismatch
bool ok = invoice.TryAs(out Money<USD> r);   // safe variant
```

### Mixed-currency portfolios (`MoneyBag`)

```csharp
MoneyBag wallet = MoneyBag.Empty
    .Add(new Money<USD>(100m))
    .Add(new Money<EUR>(50m))
    .Add(new Money<JPY>(10_000m));

wallet.GetBalance<USD>();              // Money<USD>? 100.00
wallet.GetBalance(CurrencyCode.EUR);   // Money? — EUR 50.00
wallet.Count;                          // 3
```

Convert the bag to a single target currency via an `IRateProvider`:

```csharp
var rates = new Dictionary<(string From, string To), decimal>
{
    { ("EUR", "USD"), 1.10m },
    { ("JPY", "USD"), 0.0067m },
};
FixedRateTable table = new(rates);

Money<USD> totalInUsd = wallet.ConvertTo<USD>(table);
// 100 + 50×1.10 + 10000×0.0067 = $222.00
```

### Dated FX lookup with audit metadata

```csharp
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Extensions;   // IsExactDate / ResolvedDate extension members

IDatedRateProvider provider = …;
RateLookupResult lookup = provider.GetRate(
    "USD", "EUR",
    new DateOnly(2024, 6, 15),
    RateLookupOptions.NearestWithin(7));

Console.WriteLine($"Used {lookup.Rate.Date} from {lookup.Rate.Provider}");
Console.WriteLine($"Offset: {lookup.OffsetDays} day(s), exact: {lookup.IsExactDate()}");
```

The lookup result is a `readonly record struct` carrying the resolved
`Rate` (an <xref:Bodu.Financial.ExchangeRates.ExchangeRate>), the `RequestedDate`, the
`Resolution` policy that fired, the absolute `OffsetDays`, and the
`Provenance`. `IsExactDate`, `ResolvedDate`, `SignedOffsetDays`,
`IsPreviousDate`, and `IsFutureDate` are extension members in
<xref:Bodu.Financial.Extensions> rather than properties on the record.

### Cash rounding

```csharp
new Money<CHF>(12.34m).RoundToCash();    // CHF 12.35
new Money<NZD>(5.07m).RoundToCash();     // NZD 5.10
new Money<USD>(19.99m).RoundToCash();    // USD 19.99 — no-op, no cash increment
```

The currency's `CashRoundingIncrement` (e.g. `0.05m` for CHF) drives the snap. Electronic transactions retain full minor-unit precision; use `RoundToCash()` only at the point a total becomes a cash payment.

### JSON

`Money<T>`, `Money`, and `MoneyBag` all carry `[JsonConverter]` attributes, so the default `Strict` policy works without extra wiring:

```json
{ "amount": 19.99, "currency": "USD" }
```

To switch to lenient parsing or the compact string form (`"19.99 USD"`), register the converters with an explicit policy:

```csharp
using Bodu.Financial.Serialization;

var options = new JsonSerializerOptions();
options.AddFinancialJsonConverters(FinancialJsonPolicy.Compact);
```

### A unit outside the shipped catalogue

The runtime `Money` is closed to the shipped `CurrencyCode` set. For a
generic amount in a unit outside ISO 4217, declare your own `ICurrency`
tag (its `IsoCode` must be three uppercase ASCII letters) and use
`Money<TCurrency>`; it carries its own precision and stays in the
generic world — it cannot bridge to the runtime `Money`.

```csharp
public sealed class XPT : ICurrency      // troy ounces of platinum, say
{
    public static string IsoCode   => "XPT";
    public static int    MinorUnits => 4;
    private XPT() { }
}

Money<XPT> holding = new Money<XPT>(12.3456m);
```

## Where to go next

- **[Bodu.Financial introduction](index.md)** — namespaces, headline types, scenarios.
- **[Working with `Money<TCurrency>`](../../guides/financial/money.md)** — the full reference for typed money, including formatting/parsing, locale-aware output, cash rounding, historic-currency metadata, `Money` interop, and `MoneyBag` portfolios.
- **[Bodu.Numerics getting started](../numerics/getting-started.md)** — for the `Fraction<BigInteger>` precision escape hatch used by `Money<T>.ToFraction()`.
- **[Bodu.Financial API reference](xref:Bodu.Financial)** — full type-by-type docs.
- **[Runnable samples](../../guides/financial/samples.md)** — offline sample projects under `samples/financial/` you can `dotnet run` and copy from.
