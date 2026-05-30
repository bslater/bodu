---
title: Working with Money<TCurrency>
---

# Working with `Money<TCurrency>`

`Money<TCurrency>` is an immutable, value-equatable monetary amount
whose currency is encoded in the type parameter, not stored as a
runtime field. This shifts the compiler from "no idea what currency
this is" to "knows at every call site," so the obvious error —
adding USD to JPY — fails the build instead of slipping through to
production.

```csharp
using Bodu.Numerics;
using Bodu.Numerics.Currencies;

Money<USD> dinner = new Money<USD>(54.30m);
Money<USD> tip    = dinner * 0.18m;
Money<USD> total  = dinner + tip;       // OK — same currency

Money<JPY> sushi = new Money<JPY>(2500m);
var oops = dinner + sushi;              // Compile error
```

## The ICurrency tag types

Every currency ships as a sealed tag class in
`Bodu.Numerics.Currencies`. The class only exists to carry the static
metadata `Money<TCurrency>` needs (the ISO 4217 code and minor-unit
precision) — there is no instance to create:

```csharp
public sealed class USD : ICurrency
{
    public static string IsoCode   => "USD";
    public static int    MinorUnits => 2;
    private USD() { }
}
```

The shipped catalogue covers ~150 active ISO 4217 currencies,
including all three minor-unit categories:

- `MinorUnits = 0` — `JPY`, `KRW`, `CLP`, `ISK`, `VND`, `XAF`, `XOF`, etc.
- `MinorUnits = 2` — `USD`, `EUR`, `GBP`, `AUD`, `CAD`, `CHF`, and the
  vast majority of others.
- `MinorUnits = 3` — `BHD`, `IQD`, `JOD`, `KWD`, `LYD`, `OMR`, `TND`.

To add a custom currency or a deprecated one not in the shipped
catalogue, declare your own type implementing `ICurrency`. There is
nothing privileged about the bundled tags.

## Creating amounts

The primary constructor rounds the amount to the currency's
minor-unit precision using banker's rounding
(`MidpointRounding.ToEven`):

```csharp
new Money<USD>(1.235m);              // 1.24m   — banker's rounding
new Money<USD>(1.245m);              // 1.24m   — round-half-to-even
new Money<JPY>(99.6m);               // 100m    — JPY has 0 minor units
new Money<BHD>(12.3456m);            // 12.346m — BHD has 3 minor units
```

For a different rounding rule, pass it explicitly:

```csharp
new Money<USD>(1.235m, MidpointRounding.AwayFromZero);   // 1.24m
new Money<USD>(1.225m, MidpointRounding.AwayFromZero);   // 1.23m
```

A non-generic helper class makes the syntax less noisy when you
already have a `using` for the currency tag:

```csharp
using static Bodu.Numerics.Money;
using Bodu.Numerics.Currencies;

var price = Of<USD>(19.99m);
var zero  = Zero<JPY>();
```

`default(Money<TCurrency>)` represents zero of the currency, so
`Money<USD> total = default;` is the idiomatic accumulator
initializer.

## Same-currency arithmetic

Addition, subtraction, comparison, and unary negation require the
same `TCurrency` on both sides — that's the whole point of the
type-parameter design. Scalar multiplication and division accept a
`decimal` and round the result to the minor-unit precision:

```csharp
Money<USD> total   = Of<USD>(54.30m) + Of<USD>(8.20m);
Money<USD> doubled = Of<USD>(19.99m) * 2m;
Money<USD> share   = Of<USD>(10m)    / 3m;        // 3.33m  — rounded

// Money / Money produces a dimensionless ratio:
decimal ratio = Of<USD>(10m) / Of<USD>(4m);       // 2.5m
```

Note that scalar division rounds at every call. For splitting an
amount into shares while preserving the original total exactly, use
`Allocate` instead (see below).

## Cross-currency conversion

There is no implicit conversion. To cross currencies, call
`Convert<TTarget>(rate)` and rounding is applied to the destination
precision:

```csharp
Money<USD> usd = new Money<USD>(100m);
Money<JPY> jpy = usd.Convert<JPY>(155.5m);   // 15,550 JPY (0 dp)
Money<EUR> eur = usd.Convert<EUR>(0.93m);    // 93.00 EUR (2 dp)
```

The rate must be non-negative; the rounding rule defaults to
`MidpointRounding.ToEven` and can be overridden.

## Allocation

`Allocate(parts)` splits the amount into the requested number of
shares whose sum equals the original — the residual minor units are
distributed one per share from the start of the array:

```csharp
Money<USD>[] shares = new Money<USD>(0.10m).Allocate(3);
// [0.04, 0.03, 0.03]  — sums to exactly 0.10
```

This is sign-stable: a negative amount distributes the residual in
the same direction.

```csharp
Money<USD>[] losses = new Money<USD>(-10m).Allocate(3);
// [-3.34, -3.33, -3.33]  — sums to exactly -10
```

Ratio-based allocation handles weighted splits:

```csharp
decimal[] ratios = { 1m, 1m, 2m };
Money<USD>[] split = new Money<USD>(100m).Allocate(ratios);
// [25.00, 25.00, 50.00]
```

Allocation throws `ArgumentException` for empty, negative, or
all-zero ratios; `Allocate(int)` throws
`ArgumentOutOfRangeException` for zero or negative parts.

## Exact arithmetic for long chains

Every operation that needs to round (`*`, `/`, `Convert`) rounds at
the call site. When a calculation chains several such steps, the
errors accumulate. For exact intermediate arithmetic — compound
interest, tax stacking, percentage-of-percentage — round-trip
through `Fraction<BigInteger>`:

```csharp
Money<USD> principal = new Money<USD>(1000m);
Fraction<BigInteger> monthlyRate = Fraction<BigInteger>.Create(5, 1200);  // 5%/12
Fraction<BigInteger> growth = (Fraction<BigInteger>.One + monthlyRate);

Fraction<BigInteger> exact = principal.ToFraction();
for (int i = 0; i < 24; i++)
    exact *= growth;

Money<USD> balance = Money<USD>.FromFraction(exact);   // one rounding event
```

The common case — multiply once by a fraction — has a shortcut:

```csharp
Money<USD> result = principal.MultiplyExact(growth);
```

## Formatting

`ToString()` returns the ISO code followed by the amount at minor-unit
precision, with culture-aware grouping:

```csharp
new Money<USD>(1234.56m).ToString();     // "USD 1,234.56"
new Money<JPY>(2500m).ToString();        // "JPY 2,500"
new Money<BHD>(12.345m).ToString();      // "BHD 12.345"
```

Supported format specifiers:

| Specifier | Output                       |
|-----------|------------------------------|
| `null`, `""`, `"G"`, `"C"` | ISO code + grouped number at minor-unit precision |
| `"C4"`, `"G0"` | ISO code + grouped number with explicit precision |
| `"L"` | Locale-aware: culture's native currency symbol when matched, ISO code substituted into the locale's pattern when mismatched |
| `"N"` | Grouped number only, no ISO code |
| `"F"`, `"D"` | Bare number without grouping or ISO code |
| `"N4"`, `"F0"` | Same as above with explicit precision |
| Prefix `"~"` on `C`/`G`/`L` | Elide the currency designator when the culture's currency matches `TCurrency` |

```csharp
var m = new Money<USD>(1234.56m);

m.ToString("C", CultureInfo.InvariantCulture);    // "USD 1,234.56"
m.ToString("C4", CultureInfo.InvariantCulture);   // "USD 1,234.5600"
m.ToString("N", CultureInfo.InvariantCulture);    // "1,234.56"
m.ToString("F0", CultureInfo.InvariantCulture);   // "1235"
```

`Money<TCurrency>` implements `IFormattable`, `ISpanFormattable`, and
`IUtf8SpanFormattable`, so it composes with the high-performance
formatting APIs in modern .NET.

### Locale-aware formatting with `L`

The `L` specifier renders the amount through the culture's native
`NumberFormatInfo.CurrencyPositivePattern` — symbol position,
decimal separator, grouping separator, and parenthesised negatives
all follow what the locale would do for `decimal.ToString("C")`. The
catch is the currency symbol itself: the locale picks a symbol from
its own currency, not yours. To stay unambiguous, `L` substitutes the
ISO code when the locale's currency differs from `TCurrency`:

```csharp
var usd = new Money<USD>(1234.56m);
var jpy = new Money<JPY>(1234m);
var eur = new Money<EUR>(1234.56m);

// Culture's region currency matches — use the local symbol:
usd.ToString("L", new CultureInfo("en-US"));   // "$1,234.56"
jpy.ToString("L", new CultureInfo("ja-JP"));   // "¥1,234"
eur.ToString("L", new CultureInfo("de-DE"));   // "1.234,56 €"
eur.ToString("L", new CultureInfo("fr-FR"));   // "1 234,56 €"

// Currencies differ — substitute the ISO code in the locale's slot:
jpy.ToString("L", new CultureInfo("en-US"));   // "JPY 1,234"
usd.ToString("L", new CultureInfo("de-DE"));   // "1.234,56 USD"
```

The currency's minor-unit precision wins over the locale's
`CurrencyDecimalDigits`, so `Money<JPY>` always formats with zero
fractional digits and `Money<BHD>` always with three, regardless of
the culture's defaults. Explicit precision suffixes (`"L0"`, `"L4"`)
override both.

The current culture's region is unreachable from a `CultureInfo`
that has no country (neutral cultures such as `"en"`, `"fr"`) and
from `CultureInfo.InvariantCulture`; in those cases `L` falls back
to the ISO-substitution form, which always works regardless of
region.

### Eliding the currency when redundant

Prefixing any of `C`, `G`, or `L` with `~` drops the currency
designator *only when the culture already implies it* — useful for
logs and exports where the active culture is uniform and the ISO
code adds noise on every line, but you still want a guard against a
stray foreign-currency value sneaking through:

```csharp
var usd = new Money<USD>(1234.56m);
var jpy = new Money<JPY>(1234m);

usd.ToString("~C", new CultureInfo("en-US"));   // "1,234.56"    — elided
jpy.ToString("~C", new CultureInfo("en-US"));   // "JPY 1,234"   — kept

usd.ToString("~L", new CultureInfo("en-US"));   // "19.99"       — elided
jpy.ToString("~L", new CultureInfo("en-US"));   // "JPY 1,234"   — kept
```

The "matches" test uses `RegionInfo.ISOCurrencySymbol` for the
culture passed to the formatter (not `CultureInfo.CurrentCulture`
unless that's what was passed). Neutral cultures and the invariant
culture never match, so `~` is safe to apply unconditionally — when
the formatter has no region context, the ISO code stays in the
output.

## Parsing

Parsing is strict: a bare decimal, or `"<ISO> <decimal>"` /
`"<decimal> <ISO>"`. Currency symbols like `$` are rejected because
they are ambiguous across USD, CAD, AUD, SGD, etc.

```csharp
Money<USD>.Parse("19.99", CultureInfo.InvariantCulture);       // OK
Money<USD>.Parse("USD 19.99", CultureInfo.InvariantCulture);   // OK
Money<USD>.Parse("19.99 USD", CultureInfo.InvariantCulture);   // OK
Money<USD>.Parse("JPY 19.99", CultureInfo.InvariantCulture);   // FormatException
Money<USD>.Parse("$19.99", CultureInfo.InvariantCulture);      // FormatException
```

`IParsable<Money<TCurrency>>` and `ISpanParsable<Money<TCurrency>>`
are implemented for the generic-math interface set.

## JSON

`Money<TCurrency>` carries a `JsonConverter` attribute so
`System.Text.Json` works without extra wiring:

```json
{ "amount": 19.99, "currency": "USD" }
```

The deserializer verifies the `"currency"` field matches
`TCurrency.IsoCode` and throws `JsonException` on mismatch — drift
between the persisted currency and the code's expectation surfaces as
an error rather than a silent re-interpretation.

## Cash rounding

A handful of currencies round physical cash transactions to a coarser
increment than their electronic minor unit. Switzerland's 5-rappen
coin, Canada's 5-cent cash totals (after the penny was withdrawn in
2013), Australia's 5-cent cash totals (since 1992), New Zealand's
10-cent rounding (since 2006), and Sweden/Norway's whole-krone cash
rounding all fall into this bucket.

The shipped catalogue surfaces the convention through
`ICurrency.CashRoundingIncrement` (the smallest denomination in the
major unit, or `0m` when no special rounding applies). `RoundToCash()`
snaps an amount to the nearest multiple of that increment using
banker's rounding by default:

```csharp
Money<CHF>(12.34m).RoundToCash();    // CHF 12.35
Money<NZD>(5.07m).RoundToCash();     // NZD 5.10
Money<USD>(19.99m).RoundToCash();    // USD 19.99 — no-op, no cash increment
```

Pass `MidpointRounding.AwayFromZero` to round midpoints up instead of
toward the nearest even denomination:

```csharp
Money<NZD>(5.05m).RoundToCash();                                // NZD 5.00 (banker's down to even)
Money<NZD>(5.05m).RoundToCash(MidpointRounding.AwayFromZero);   // NZD 5.10
```

Cash rounding is for physical cash totals only — electronic
transactions retain the full `MinorUnits` precision. Use the method at
the point where the total becomes a cash payment, not at every
intermediate step.

## Historic currencies

The shipped catalogue includes ~29 demonetized currencies — the
twenty Euro-zone predecessors (ATS, BEF, CYP, DEM, EEK, ESP, FIM,
FRF, GRD, HRK, IEP, ITL, LTL, LUF, LVL, MTL, NLG, PTE, SIT, SKK) plus
nine other notable replacements (AZM, GHC, MZM, ROL, SRG, TMM, VEB,
VEF, ZWL). They participate in arithmetic and formatting like any
other currency so legacy data remains processable; the difference is
that `IsHistoric`, `DemonetizedOn`, and `SuccessorIsoCode` carry the
withdrawal metadata:

```csharp
Money<DEM>.IsHistoric;            // true
Money<DEM>.DemonetizedOn;         // 2002-02-28
Money<DEM>.SuccessorIsoCode;      // "EUR"

// Arithmetic still works normally:
Money<DEM> total = new Money<DEM>(100m) + new Money<DEM>(50m);   // DEM 150.00
```

For runtime processing of legacy ledgers — for example, validating
that an imported journal entry's currency was active on its posting
date — read the metadata from `CurrencyRegistry`:

```csharp
CurrencyInfo info = CurrencyRegistry.Get(entry.IsoCode);
if (info.IsHistoric && entry.PostedOn > info.DemonetizedOn)
    throw new InvalidOperationException(
        $"{entry.IsoCode} was demonetized {info.DemonetizedOn:d} (replaced by {info.SuccessorIsoCode}).");
```

## Runtime-tagged amounts: `MoneyValue`

`MoneyValue` is the runtime-tagged sister of `Money<TCurrency>`. The
currency is carried as a string field rather than a type parameter,
so the same code path handles any ISO code at runtime — useful for
deserialisation, generic invoicing engines, and FX systems where the
currency comes from data, not type.

```csharp
MoneyValue invoice = JsonSerializer.Deserialize<MoneyValue>(payload)!;
// invoice could be "USD 19.99", "EUR 19.99", or "JPY 200" — same code.
```

Arithmetic semantics match `Money<T>` but cross-currency operations
throw `InvalidOperationException` at runtime instead of failing the
build:

```csharp
MoneyValue usd = new MoneyValue(10m, "USD");
MoneyValue eur = new MoneyValue(10m, "EUR");

MoneyValue total = usd + new MoneyValue(5m, "USD");   // OK
total = usd + eur;                                     // throws InvalidOperationException
```

Bridge to and from a typed `Money<T>` when the boundary is known:

```csharp
Money<USD> typed = runtime.ToTyped<USD>();                 // throws on mismatch
bool ok = runtime.TryToTyped(out Money<USD> result);       // safe, returns false on mismatch
MoneyValue runtime = MoneyValue.FromTyped(new Money<USD>(19.99m));
```

`MoneyValue` rounds to the registry's `MinorUnits` for the supplied
ISO code on construction. Custom currencies not in the catalogue can
be pre-registered (see [Custom currencies](#custom-currencies)) so
the rounding follows your declared precision.

## Mixed-currency portfolios: `MoneyBag`

`MoneyBag` aggregates per-currency balances. Useful for tracking
ledger positions across currencies without silently merging them:

```csharp
MoneyBag wallet = MoneyBag.Empty
    .Add(new Money<USD>(100m))
    .Add(new Money<EUR>(50m))
    .Add(new Money<JPY>(10_000m));

wallet.GetBalance<USD>();           // Money<USD> 100.00
wallet.GetBalance("EUR");           // MoneyValue { 50, "EUR" }
wallet.Count;                       // 3
```

Operators chain naturally:

```csharp
MoneyBag updated = wallet
    + new MoneyValue(25m, "USD")
    - new MoneyValue(10m, "EUR");
```

Bags are immutable; every operation returns a new bag. Zero balances
are pruned automatically.

To convert the bag to a single target currency, supply an
`IExchangeRateProvider`:

```csharp
Dictionary<(string From, string To), decimal> rates = new()
{
    { ("EUR", "USD"), 1.10m },
    { ("JPY", "USD"), 0.0067m },
};
FixedExchangeRateTable table = new(rates);

Money<USD> totalInUsd = wallet.ConvertTo<USD>(table);
// 100 + 50×1.10 + 10000×0.0067 = $222.00
```

`FixedExchangeRateTable` short-circuits same-currency lookups to `1`
and falls back to the inverse rate `1 / rate` when only the reverse
pair is in the table, so a typical "USD → X" set of rates is enough
to convert in both directions.

## Custom currencies

Implement `ICurrency` directly to add a currency that isn't in the
shipped catalogue:

```csharp
public sealed class DOGE : ICurrency
{
    public static string IsoCode => "DOGE";
    public static int    MinorUnits => 8;
    private DOGE() { }
}
```

Register it with `CurrencyRegistry` so `MoneyValue` and `MoneyBag`
round to the right precision when they see the ISO code at runtime:

```csharp
CurrencyRegistry.Register(
    new CurrencyInfo(IsoCode: "DOGE", MinorUnits: 8,
        CashRoundingIncrement: 0m, IsHistoric: false,
        DemonetizedOn: null, SuccessorIsoCode: null));

Money<DOGE> tip = new Money<DOGE>(0.12345678m);
MoneyValue runtime = JsonSerializer.Deserialize<MoneyValue>(
    """{ "amount": 0.12345678, "currency": "DOGE" }""");
```

## JSON wire shape

`Money<TCurrency>` and `MoneyValue` both serialise as:

```json
{ "amount": 19.99, "currency": "USD" }
```

Deserialisation on `Money<TCurrency>` rejects payloads whose
`"currency"` field does not match `TCurrency.IsoCode` —
currency drift surfaces as `JsonException`, not as a silently
re-interpreted amount. `MoneyValue` accepts any ISO code (and rounds
to the registry's `MinorUnits` for that code).

`MoneyBag` uses a `{ "balances": { ... } }` wrapper:

```json
{ "balances": { "USD": 100.00, "EUR": 50.00, "JPY": 10000 } }
```

Amounts are emitted as JSON numbers; the reader also accepts string
amounts to round-trip large values through systems that lack
arbitrary-precision number support.

## When not to use `Money<TCurrency>`

- **Calculations that genuinely span unknown currencies.** When you
  cannot fix the currency at the type-system level (for example, a
  generic invoicing engine that handles arbitrary user-supplied
  currencies), use `MoneyValue` instead — the trade-off is runtime
  cross-currency checks rather than compile-time ones.
- **Mixed-currency totals.** Use `MoneyBag` and a single
  `ConvertTo<TTarget>` call at the boundary where the total is
  materialised.
- **Storage of foreign-exchange spot rates or other ratios.** Use
  `Fraction<BigInteger>` directly — those values are dimensionless
  and benefit from exact rational arithmetic.
- **Sub-minor-unit precision.** `Money<TCurrency>` rounds to the
  currency's minor-unit precision on construction. If you need
  sub-cent precision (for example, half-pennies in gas pricing),
  promote to `Fraction<BigInteger>` for the calculation and snap to
  `Money<TCurrency>` only at the persistence boundary.

## See also

- [`Money<TCurrency>` API reference](xref:Bodu.Numerics.Money`1)
- [`MoneyValue` API reference](xref:Bodu.Numerics.MoneyValue)
- [`MoneyBag` API reference](xref:Bodu.Numerics.MoneyBag)
- [`CurrencyRegistry`](xref:Bodu.Numerics.CurrencyRegistry)
- [`IExchangeRateProvider`](xref:Bodu.Numerics.IExchangeRateProvider)
- [`Money` static factory helpers](xref:Bodu.Numerics.Money)
- [`ICurrency` interface](xref:Bodu.Numerics.ICurrency)
- [`Fraction<T>` API reference](xref:Bodu.Numerics.Fraction`1) — the
  exact-arithmetic escape hatch.
