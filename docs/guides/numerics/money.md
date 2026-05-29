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

## When not to use `Money<TCurrency>`

- **Calculations that genuinely span unknown currencies.** When you
  cannot fix the currency at the type-system level (for example, a
  generic invoicing engine that handles arbitrary user-supplied
  currencies), the type-parameter form does not help. Use the
  `Fraction<BigInteger>` exact-arithmetic path or model the currency
  at the entity layer.
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
- [`Money` static factory helpers](xref:Bodu.Numerics.Money)
- [`ICurrency` interface](xref:Bodu.Numerics.ICurrency)
- [`Fraction<T>` API reference](xref:Bodu.Numerics.Fraction`1) — the
  exact-arithmetic escape hatch.
