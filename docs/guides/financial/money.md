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
using Bodu.Financial;
using Bodu.Financial.Currencies;

Money<USD> dinner = new Money<USD>(54.30m);
Money<USD> tip    = dinner * 0.18m;
Money<USD> total  = dinner + tip;       // OK — same currency

Money<JPY> sushi = new Money<JPY>(2500m);
var oops = dinner + sushi;              // Compile error
```

## The ICurrency tag types

Every currency ships as a sealed tag class in
`Bodu.Financial.Currencies`. The class only exists to carry the static
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

The shipped catalogue covers 155 active ISO 4217 currencies (plus 29
historic ones), including all three minor-unit categories:

- `MinorUnits = 0` — `JPY`, `KRW`, `CLP`, `ISK`, `VND`, `XAF`, `XOF`, etc.
- `MinorUnits = 2` — `USD`, `EUR`, `GBP`, `AUD`, `CAD`, `CHF`, and the
  vast majority of others.
- `MinorUnits = 3` — `BHD`, `IQD`, `JOD`, `KWD`, `LYD`, `OMR`, `TND`.

The bundled tags are not privileged: you can declare your own type
implementing `ICurrency` — its `IsoCode` must be three uppercase ASCII
letters — to mint a `Money<TCurrency>` for a unit outside the shipped
set. Such a tag stays in the generic world; bridging it to the
runtime-tagged `Money` requires a code the shipped `CurrencyCode`
catalogue defines (see [Runtime-tagged amounts](#runtime-tagged-amounts-money)).

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
using static Bodu.Financial.Money;
using Bodu.Financial.Currencies;

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

The residual-distribution rule above is the
<xref:Bodu.Financial.AllocationPolicy> `LargestRemainder` strategy:
each leftover minor unit is handed to the share with the largest
fractional remainder (here, one per share from the start), so the parts
always sum back to the original amount with no penny lost or invented.

## Allocation in depth

The two `Allocate` overloads above cover the common cases; this section pins
down the edge behaviour and how allocation interacts with `MoneyBag` and the
`MonetaryContext`.

**The residual rule is a fixed policy, not a parameter.** Both overloads
distribute leftover minor units by the largest-remainder (Hamilton) method —
each slot receives one extra unit in descending order of its fractional
remainder, ties broken by ascending input order. That algorithm is the single
member of <xref:Bodu.Financial.AllocationPolicy> (`LargestRemainder`), and it is
also the value carried by <xref:Bodu.Financial.MonetaryContext> through its
`Allocation` property. Note that `Allocate(int)` and
`Allocate(ReadOnlySpan<decimal>)` do **not** take a `MonetaryContext` — they
always round to the currency's own minor-unit precision and always sum back to
the original. The context's `Allocation` policy documents the strategy the
library applies; it is not a per-call knob on these methods today.

**Sign is preserved; the residual flows in the amount's direction.** A negative
total distributes its leftover units the same way a positive one does, so the
parts still sum exactly:

```csharp
new Money<USD>(10m).Allocate(3);    // [3.34, 3.33, 3.33]  → 10.00
new Money<USD>(-10m).Allocate(3);   // [-3.34, -3.33, -3.33] → -10.00
```

**Zero-ratio slots and small amounts.** A zero weight always produces a zero
share and never receives a residual unit; a positive total with fewer minor
units than parts fills the leading slots and leaves the rest at zero:

```csharp
decimal[] ratios = { 0m, 1m, 1m };
new Money<USD>(0.03m).Allocate(ratios);   // [0.00, 0.02, 0.01]
new Money<USD>(0.02m).Allocate(5);        // [0.01, 0.01, 0, 0, 0]
```

Ratios that are all zero, contain a negative weight, or are empty throw
`ArgumentException`; `Allocate(0)` or a negative part count throws
`ArgumentOutOfRangeException`.

**`MoneyBag` aggregates across currencies but does not allocate them as a unit.**
A <xref:Bodu.Financial.MoneyBag> tracks one balance per currency, so to split a
multi-currency position you allocate each currency's slot independently — each
`Allocate` call sums back exactly within its own currency:

```csharp
MoneyBag bag = MoneyBag.Empty
    .Add(new Money<USD>(100m))
    .Add(new Money<EUR>(99m));

Money<USD>[] usdSplit = bag.GetBalance<USD>()!.Value.Allocate(3);   // [33.34, 33.33, 33.33]
Money<EUR>[] eurSplit = bag.GetBalance<EUR>()!.Value.Allocate(3);   // [33.00, 33.00, 33.00]
```

**Where `MonetaryContext` does change rounding.** The context governs the
*operation* boundaries — multiplication, division, conversion, and the
settlement of a `CalculatedMoney` — not the residual distribution. So when you
need a non-banker's rounding rule before splitting, apply it at the multiply step
and allocate the rounded result, which then sums back exactly under the fixed
largest-remainder rule:

```csharp
MonetaryContext awayFromZero = MonetaryContext.Default with
{
    Rounding = new MidpointRoundingStrategy(MidpointRounding.AwayFromZero),
};

Money<USD> commission = new Money<USD>(17m).Multiply(0.125m, awayFromZero);  // 2.13 USD (banker's → 2.12)
Money<USD>[] shares = commission.Allocate(3);                                // sums to 2.13
```

`MoneyBag.ConvertTo<TTarget>` takes a related but separate
<xref:Bodu.Financial.MoneyBagConversionRoundingPolicy> — `SumRawThenRound`
(round once after summing every converted balance, the default) or
`RoundEachCurrencyThenSum` (round each converted balance first) — which decides
where the single rounding event lands when collapsing a bag to one currency.

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

## Deferred rounding with `CalculatedMoney`

`Fraction<BigInteger>` is mathematically exact but heavyweight. When
you only need to defer rounding across a chain of `decimal` steps —
not full rational exactness — `CalculatedMoney` is the lighter middle
tier. It is a runtime-tagged, high-precision amount that carries the
full `decimal` precision through arithmetic and rounds **once**, at the
settlement boundary:

```csharp
CalculatedMoney running = new Money<USD>(100m).ToCalculated();
running = running * 1.05m / 3m;                  // no rounding yet
Money settled = running.RoundToMoney();          // single rounding event → Money
Money<USD> usd = settled.As<USD>();
```

`ToCalculated()` is available on both `Money<TCurrency>` and the
runtime `Money`, and always returns the runtime `CalculatedMoney` —
there is no generic `CalculatedMoney<TCurrency>`. Arithmetic (`+`, `-`,
`*`, `/`, and the named `Multiply` / `Divide`) preserves precision, and
mixing two different currencies throws `InvalidOperationException` at
runtime. `RoundToMoney` accepts an optional `MonetaryContext` to
control the rounding strategy, scale, and cash rounding, or a bare
`MidpointRounding`:

```csharp
Money rounded = running.RoundToMoney(MidpointRounding.AwayFromZero);
```

Pick the tier that fits the calculation: `Money<TCurrency>` rounds at
every step (settlement-grade), `CalculatedMoney` defers rounding at
full `decimal` precision (28–29 significant digits), and
`Fraction<BigInteger>` is exact. Reach for `CalculatedMoney` in tax
apportionment and unit-rate products where `decimal` precision is
sufficient, and for `Fraction` only when the chain must be exact.

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

### Reusable formatting with `MoneyFormatter`

The format strings above are convenient for one-offs, but when you
need to apply the *same* formatting decisions repeatedly — across a
report, an export, or a UI surface — build a reusable
<xref:Bodu.Financial.MoneyFormatter> once with
<xref:Bodu.Financial.MoneyFormatterBuilder> and call it per value. The
formatter operates on the runtime-tagged <xref:Bodu.Financial.Money>,
so it can format any currency through a single instance:

```csharp
MoneyFormatter formatter = new MoneyFormatterBuilder()
    .WithSymbol()                       // CurrencyDisplay.Symbol
    .WithCulture(new CultureInfo("en-US"))
    .WithGrouping(includeGrouping: true)
    .ElideWhenCultureMatches()          // the "~" behaviour, baked in
    .Build();

string a = formatter.Format(new Money<USD>(1234.56m).ToMoney());  // "1,234.56"
string b = formatter.Format(new Money<JPY>(1234m).ToMoney());     // "JPY 1,234"
```

The builder mirrors the format-string options as fluent calls —
`WithIsoCode()` / `WithSymbol()` / `WithEnglishName()` /
`WithNumericOnly()` select the <xref:Bodu.Financial.CurrencyDisplay>
mode (`IsoCode`, `Symbol`, `EnglishName`, `None`); `WithCulture`,
`WithGrouping`, `WithMinorUnits`, and `ElideWhenCultureMatches` set the
remaining knobs. For ad-hoc construction, set the same fields directly
on a <xref:Bodu.Financial.MoneyFormatOptions> and pass it to the
`MoneyFormatter` constructor; `MoneyFormatOptions.Default` is the
process-wide fallback.

### Compact formatting

For dashboards and summaries,
<xref:Bodu.Financial.Extensions.MoneyCompactFormattingExtensions> renders large
amounts in abbreviated form (`1.2K`, `3.4M`, `5.6B`) directly on both
`Money<TCurrency>` and `Money`:

```csharp
new Money<USD>(1_234_567m).ToCompactString();   // "USD 1.2M" (default "C" specifier)
```

The optional `format`, `provider`, and `precision` arguments mirror the
`Money` format specifiers, so `ToCompactString("~C", culture)` elides a
redundant currency designator exactly as the full formatter does.

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

### Tuning parse behaviour with `MoneyParseOptions`

The strictness above is the default. To relax or retarget it — for
example when importing a spreadsheet column or round-tripping a value
your own formatter produced — pass a
<xref:Bodu.Financial.MoneyParseOptions> whose
<xref:Bodu.Financial.MoneyParseMode> selects the policy:

| `MoneyParseMode` | Accepts |
|---|---|
| `StrictIso` | A bare decimal or `"<ISO> <decimal>"` / `"<decimal> <ISO>"` only. The default. |
| `CultureAware` | Adds the active culture's number formatting (grouping, decimal separator). |
| `LenientImport` | Tolerant of spreadsheet / external-feed quirks; for ingest, not canonical storage. |
| `RoundTripOnly` | Accepts exactly the shape this library emits, for loss-free round trips. |

`MoneyParseOptions` also carries the `FormatProvider` and an optional
`ICurrencyLookup` (consulted for symbol resolution under
`CultureAware`); an ISO code that does not resolve to a shipped
currency is rejected. The runtime-tagged <xref:Bodu.Financial.Money>
uses the same options object when parsing a value whose currency is not
known until run time.

## JSON

JSON support ships in the companion `Bodu.Financial.Serialization.Json`
package; register its converters before serializing — the core types
carry no `[JsonConverter]` attribute:

```csharp
using Bodu.Financial.Serialization.Json;

var options = new JsonSerializerOptions().AddFinancialJsonConverters();
```

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

The shipped catalogue includes 29 demonetized currencies — the
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

## Runtime-tagged amounts: `Money`

`Money` is the runtime-tagged sister of `Money<TCurrency>`. The
currency is carried as a <xref:Bodu.Financial.Currencies.CurrencyCode>
field rather than a type parameter, so the same code path handles any
shipped currency at runtime — useful for deserialisation, generic
invoicing engines, and FX systems where the currency comes from data,
not type.

```csharp
Money invoice = JsonSerializer.Deserialize<Money>(payload)!;
// invoice could be "USD 19.99", "EUR 19.99", or "JPY 200" — same code.
```

Arithmetic semantics match `Money<T>` but cross-currency operations
throw `InvalidOperationException` at runtime instead of failing the
build:

```csharp
Money usd = new Money(10m, CurrencyCode.USD);
Money eur = new Money(10m, CurrencyCode.EUR);

Money total = usd + new Money(5m, CurrencyCode.USD);   // OK
total = usd + eur;                                     // throws InvalidOperationException
```

Bridge to and from a typed `Money<T>` when the boundary is known:

```csharp
Money<USD> typed = runtime.As<USD>();                  // throws on mismatch
bool ok = runtime.TryAs(out Money<USD> result);        // safe, returns false on mismatch
Money runtime = new Money<USD>(19.99m).ToMoney();      // typed → runtime-tagged
```

`Money` rounds to the `MinorUnits` resolved for its
<xref:Bodu.Financial.Currencies.CurrencyCode> on construction. The
runtime currency set is the shipped ISO 4217 catalogue — active and
historic — that the enum enumerates; a code outside it cannot be
constructed, so a mistyped or unsupported currency fails fast rather
than silently adopting a default precision.

## Mixed-currency portfolios: `MoneyBag`

`MoneyBag` aggregates per-currency balances. Useful for tracking
ledger positions across currencies without silently merging them:

```csharp
MoneyBag wallet = MoneyBag.Empty
    .Add(new Money<USD>(100m))
    .Add(new Money<EUR>(50m))
    .Add(new Money<JPY>(10_000m));

wallet.GetBalance<USD>();              // Money<USD> 100.00
wallet.GetBalance(CurrencyCode.EUR);   // Money — EUR 50.00
wallet.Count;                          // 3
```

Operators chain naturally:

```csharp
MoneyBag updated = wallet
    + new Money(25m, CurrencyCode.USD)
    - new Money(10m, CurrencyCode.EUR);
```

Bags are immutable; every operation returns a new bag. Zero balances
are pruned automatically.

To convert the bag to a single target currency, supply an
`IRateProvider`:

```csharp
Dictionary<(string From, string To), decimal> rates = new()
{
    { ("EUR", "USD"), 1.10m },
    { ("JPY", "USD"), 0.0067m },
};
FixedRateTable table = new(rates);

Money<USD> totalInUsd = wallet.ConvertTo<USD>(table);
// 100 + 50×1.10 + 10000×0.0067 = $222.00
```

`FixedRateTable` short-circuits same-currency lookups to `1`
and falls back to the inverse rate `1 / rate` when only the reverse
pair is in the table, so a typical "USD → X" set of rates is enough
to convert in both directions.

## Currencies outside the shipped catalogue

The runtime `Money` identifies its currency with the
<xref:Bodu.Financial.Currencies.CurrencyCode> enum, which enumerates the
full ISO 4217 set — every active code plus the historic ones above. That
set is closed: there is no runtime registration seam, so a code the enum
does not define cannot be constructed as a `Money`. The trade-off is
deliberate — a mistyped or unsupported currency fails fast instead of
flowing through the system as a silently accepted value.

For a *generic* amount in a unit outside that set — a commodity weight, a
loyalty-point unit, a pre-decimal currency — declare your own `ICurrency`
tag and use `Money<TCurrency>`. The tag supplies its own precision and
never consults the runtime catalogue:

```csharp
public sealed class XPT : ICurrency      // troy ounces of platinum, say
{
    public static string IsoCode   => "XPT";
    public static int    MinorUnits => 4;
    private XPT() { }
}

Money<XPT> holding = new Money<XPT>(12.3456m);   // generic arithmetic only
```

A custom tag's `IsoCode` must be three uppercase ASCII letters, and the
value stays in the generic world: because `XPT` is not a `CurrencyCode`
member, it cannot bridge to the runtime-tagged `Money`. To substitute or
restrict the metadata used for the *shipped* currencies — for a test, or
an alternate data source — install a custom `ICurrencyLookup` through
`CurrencyResolution` (next section).

## Swapping the currency catalogue: `CurrencyResolution`

Runtime `Money` resolves its <xref:Bodu.Financial.Currencies.CurrencyCode>
to minor-unit precision through an *ambient* `ICurrencyLookup`, exposed
as `CurrencyResolution.Current`. By default this is a registry-backed
lookup, so ordinary construction and metadata resolution behave exactly
as described above — you only need this seam when you want to substitute
the catalogue (a custom data source, or a fixed set for a test).

Replace the process-wide default once at start-up:

```csharp
CurrencyResolution.SetDefault(myCurrencyLookup);
```

Or install a temporary, flow-scoped override — ideal for tests, since
it is restored on dispose and isolated per async control flow:

```csharp
using (CurrencyResolution.PushScoped(myCurrencyLookup))
{
    // Money construction, parsing, and formatting in this scope
    // resolve currency metadata through myCurrencyLookup.
    var m = new Money(1.239m, CurrencyCode.BHD);   // precision via the scoped catalogue
}   // previous lookup restored here
```

`Money<TCurrency>` is unaffected — its precision comes from the
`TCurrency` tag, not the ambient lookup. Only the runtime `Money`
resolution paths (construction, `MinorUnits`, `From`, parsing, and
formatting) consult `CurrencyResolution.Current`.

When you compose the library through dependency injection, register an
`ICurrencyLookup` and promote it to the ambient default after building
the provider:

```csharp
using Bodu.Financial;
using Microsoft.Extensions.DependencyInjection;

services.AddFinancialService(b => b.AddCurrencyLookup<MyCurrencyLookup>());
// ...
IServiceProvider provider = services.BuildServiceProvider();
provider.UseCurrencyResolution();   // ambient default = the DI lookup
```

## JSON wire shape

With the `Bodu.Financial.Serialization.Json` converters registered
(`options.AddFinancialJsonConverters()`), `Money<TCurrency>` and
`Money` both serialise as:

```json
{ "amount": 19.99, "currency": "USD" }
```

Deserialisation on `Money<TCurrency>` rejects payloads whose
`"currency"` field does not match `TCurrency.IsoCode` —
currency drift surfaces as `JsonException`, not as a silently
re-interpreted amount. `Money` accepts any code the shipped
`CurrencyCode` catalogue defines, rounding to that currency's
`MinorUnits`, and rejects one it does not — an unknown or custom
code in the payload throws rather than deserialising.

`MoneyBag` uses a `{ "balances": { ... } }` wrapper:

```json
{ "balances": { "USD": 100.00, "EUR": 50.00, "JPY": 10000 } }
```

Amounts are emitted as JSON numbers; the reader also accepts string
amounts to round-trip large values through systems that lack
arbitrary-precision number support.

To switch the wire shape, register the converters under an explicit
<xref:Bodu.Financial.Serialization.Json.FinancialJsonPolicy>. The `Compact`
policy collapses each money to a single string and a bag to a flat
ISO-keyed object — smaller on the wire and readable in a log line:

```csharp
using Bodu.Financial.Serialization.Json;

var options = new JsonSerializerOptions();
options.AddFinancialJsonConverters(FinancialJsonPolicy.Compact);

JsonSerializer.Serialize(new Money<USD>(19.99m), options);   // "19.99 USD"
JsonSerializer.Serialize(wallet, options);                   // { "USD": 100.00, "EUR": 50.00 }
```

`Lenient` keeps the `Strict` shape but normalises lowercase ISO codes
to uppercase and trims surrounding whitespace before validation — for
ingesting spreadsheets and external feeds, not as a canonical storage
shape. The same call registers converters for <xref:Bodu.Financial.ExchangeRates.ExchangeRate>
and <xref:Bodu.Financial.ExchangeRates.CurrencyPair> too.

## When not to use `Money<TCurrency>`

- **Calculations that genuinely span unknown currencies.** When you
  cannot fix the currency at the type-system level (for example, a
  generic invoicing engine that handles arbitrary user-supplied
  currencies), use `Money` instead — the trade-off is runtime
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

- [`Money<TCurrency>` API reference](xref:Bodu.Financial.Money`1)
- [`Money` API reference](xref:Bodu.Financial.Money)
- [`MoneyBag` API reference](xref:Bodu.Financial.MoneyBag)
- [`CalculatedMoney` API reference](xref:Bodu.Financial.CalculatedMoney) — the deferred-rounding tier.
- [`CurrencyRegistry`](xref:Bodu.Financial.Currencies.CurrencyRegistry)
- [`CurrencyResolution`](xref:Bodu.Financial.Currencies.CurrencyResolution) — the ambient currency-lookup seam.
- [`IRateProvider`](xref:Bodu.Financial.ExchangeRates.IRateProvider)
- [`Money` static factory helpers](xref:Bodu.Financial.Money)
- [`ICurrency` interface](xref:Bodu.Financial.Currencies.ICurrency)
- [`Fraction<T>` API reference](xref:Bodu.Numerics.Fraction`1) — the
  exact-arithmetic escape hatch.
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic, across Bodu.Numerics and Bodu.Financial.
