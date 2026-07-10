# Bodu.Financial.Samples.MoneyBasics

The core `Bodu.Financial` value types and policies, demonstrated offline with in-code data only.
Six scenarios walk the money model from single amounts through multi-currency ledgers to JSON.

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.MoneyBasics
```

## Scenarios

### RoundingTiers (`Scenarios/RoundingTiers.cs`)

**Intent.** Answer the central design question of the money model: *when* should rounding
happen? The library offers three tiers — round every step, round once at settlement, or compute
exactly — and this scenario makes the difference visible on one calculation.

**What it does.** Compounds 10,000.00 USD at 5% p.a. monthly for 12 months, three ways:

1. `Money<USD>` in a loop — every `*=` rounds the running balance to USD's two minor units
   before the next month compounds it.
2. `CalculatedMoney` in the same loop — intermediates keep full `decimal` precision; nothing
   rounds until `RoundToMoney(MidpointRounding.ToEven)` settles once at the end.
3. `MultiplyExact` with the true rational factor `(1205/1200)^12` as a `Fraction<BigInteger>` —
   no decimal truncation at all; one rounding when the product lands back in `Money<USD>`.

**What to expect.**

```
Per-step  (Money<USD>)      : USD 10,511.64
Deferred  (CalculatedMoney) : USD 10,511.62
Exact     (Fraction)        : USD 10,511.62
```

The per-step total is 2 cents higher — that drift is the accumulated per-operation rounding,
which is exactly what tiers 2 and 3 exist to avoid. Deferred and exact agree here because
decimal's 28-digit precision absorbs this particular chain; the fraction tier is the guarantee
that holds even when it would not.

**APIs demonstrated.** `Money.Of<T>`, `Money<T>` arithmetic operators, `Money<T>.ToCalculated()`,
`CalculatedMoney` operators, `CalculatedMoney.RoundToMoney(MidpointRounding)`,
`Money<T>.MultiplyExact(Fraction<BigInteger>)`.

### TypedRuntimeBridges (`Scenarios/TypedRuntimeBridges.cs`)

**Intent.** Show the duality the library is built around: `Money<TCurrency>` fixes the currency
at compile time (mixing currencies is a build error), while runtime `Money` carries the currency
as data — and the bridges between them are *checked*, so a wiring mistake surfaces at the
boundary instead of silently mislabelling an amount.

**What it does.** Builds a typed USD total (19.995 rounds to 20.00 under banker's rounding, then
+10% tax), widens it implicitly to runtime `Money`, casts it back explicitly, and finally calls
`TryAs<JPY>()` to show the checked bridge refusing a currency mismatch. A commented line shows
the cross-currency compile error.

**What to expect.**

```
Typed total : USD 22.00  (Money<USD>, 2 minor units)
Runtime     : USD 22.00  (Money, Code=USD)
Cast back   : USD 22.00
TryAs<JPY>  : false - the runtime value is USD, not JPY
```

The value is identical through every bridge — widening is lossless; only the *static* type
changes. The final line is the safety property: the runtime value knows it is USD and refuses to
become `Money<JPY>`.

**APIs demonstrated.** `Money.Of<T>`, implicit `Money<T>` → `Money` conversion, explicit
`(Money<USD>)` cast, `Money.TryAs<T>`, static `Money<T>.IsoCode` / `Money<T>.MinorUnits`.

### Allocation (`Scenarios/Allocation.cs`)

**Intent.** Splitting money by naive division loses or invents cents (100.00 / 3 → 3 × 33.33 =
99.99). Largest-remainder allocation guarantees the parts always re-total to the original — the
property invoicing, payouts, and cost-splitting all need.

**What it does.** Splits 100.00 USD three ways equally and by 50/30/20 ratios, splits 1,000 JPY
(a zero-decimal currency) three ways, and snaps a CHF card amount to the 0.05 cash increment
declared by the CHF currency tag.

**What to expect.**

```
USD 100.00 into 3     : USD 33.34, USD 33.33, USD 33.33  (sum USD 100.00)
USD 100.00 at 50/30/20: USD 50.00, USD 30.00, USD 20.00
JPY 1,000 into 3    : JPY 334, JPY 333, JPY 333
CHF 7.02 cash      : CHF 7.00  (CHF cash increment 0.05)
```

The extra cent lands on the first part (largest remainder first), the parts differ by at most
one minor unit, and the printed sum proves nothing was lost. JPY allocates in whole yen because
the currency declares zero minor units. Cash rounding is a separate, per-currency policy — the
7.02 electronic amount is valid; only the *cash* form snaps to 7.00.

**APIs demonstrated.** `Money<T>.Allocate(int)`, `Money<T>.Allocate(ReadOnlySpan<decimal>)`,
`Money<T>.RoundToCash()`, static `Money<T>.CashRoundingIncrement`.

### FormattingParsing (`Scenarios/FormattingParsing.cs`)

**Intent.** One amount, many audiences: ledgers want ISO codes, receipts want symbols, wire
formats must round-trip losslessly, and imports arrive sloppy. This scenario shows the shared
format-specifier vocabulary and the four parsing strictness levels that cover those audiences.

**What it does.** Formats `USD 1234.56` under each specifier (`G`, `C`, `L`, `N`, precision
override `C0`, and the culture-match elision `~C`) against an explicit `en-US` culture; builds a
reusable `MoneyFormatter` fluently; round-trips the `"R"` invariant form through
`MoneyParseMode.RoundTripOnly`; parses canonical ISO text and sloppy import text; and shows
`TryParse` rejecting malformed input.

**What to expect.**

```
G  (default)     : USD 1,234.56
C  (currency)    : $1,234.56
L  (long name)   : 1,234.56 US Dollar
N  (number only) : 1,234.56
C0 (0 decimals)  : $1,235
~C (elide match) : 1,234.56
Formatter        : 1,234.56 US Dollar
R round-trip     : "USD 1234.56" -> USD 1,234.56 (equal: True)
StrictIso        : "USD 99.95" -> USD 99.95
LenientImport    : "  1234.56 usd " -> USD 1,234.56
TryParse         : "not money" -> false
```

`~C` prints no `$` because the en-US region currency *is* USD — the designator is elided exactly
when it adds nothing. The `R` line is the storage pattern: culture-independent out, strict parse
back, equality preserved. `LenientImport` trims and upcases `usd` — the spreadsheet-ingestion
mode.

**APIs demonstrated.** `Money.ToString(format, provider)` and the specifier vocabulary,
`MoneyFormatterBuilder` (`WithEnglishName` / `WithCulture` / `Build`), `MoneyFormatter.Format`,
`Money.Parse(string, MoneyParseOptions)`, `MoneyParseMode` (`RoundTripOnly` / `LenientImport` /
`StrictIso`), `Money<T>.Parse`, `Money.TryParse`.

### MoneyBagLedger (`Scenarios/MoneyBagLedger.cs`)

**Intent.** Real systems hold balances in several currencies at once. `MoneyBag` is the
immutable per-currency ledger, and the question this scenario answers: how do you total a
multi-currency ledger into one reporting currency — and *prove* how you got there?

**What it does.** Builds a ledger (AUD + USD, adds EUR, merges more USD with `+=`, subtracts
AUD), prints the per-currency balances, reads a typed balance back, then converts the whole bag
to AUD twice: once through a simple `(from, to) => rate` delegate, and once through a dated
`FixedDatedRateProvider` with `ConvertToWithAudit`, printing the per-line audit trail.

**What to expect.**

```
Balances:
  AUD 1,450.00
  EUR 89.10
  USD 370.75
USD balance      : USD 370.75
Total (delegate) : AUD 2,159.97
Total (audited)  : AUD 2,159.97
  AUD    1450.00 x 1 (identity)         = 1450.00
  EUR      89.10 x 1.6310 [Treasury]    = 145.322100
  USD     370.75 x 1.5230 [Treasury]    = 564.652250
```

Balances iterate in stable ISO order. The two totals agree — the audit changes *explainability*,
not arithmetic. Each audit line carries the source amount, the resolved rate with its provider
label, and the raw (pre-rounding-policy) converted value; the AUD line shows `1 (identity)`
because the target currency's own balance needs no rate (its `Rate` is `null`).

**APIs demonstrated.** `MoneyBag.Of`, `MoneyBag.Add`, the `+`/`-` operators,
`MoneyBag.GetBalance<T>`, `ConvertTo<T>(Func<string,string,decimal>)`,
`ConvertToWithAudit<T>(IDatedRateProvider, DateOnly, RateLookupOptions)`,
`MoneyBagConversionAudit<T>` / `MoneyBagConversionLine`, plus `RateTableBuilder` →
`FixedDatedRateProvider` for the in-code rates.

### JsonPolicies (`Scenarios/JsonPolicies.cs`)

**Intent.** One wire shape does not fit ledgers, APIs, and imports alike. The three
`FinancialJsonPolicy` values pick the trade-off explicitly; this scenario shows each on the same
values.

**What it does.** Registers the financial converters on three `JsonSerializerOptions` instances
(`Strict` default, `Compact`, `Lenient`), serializes a `Money<USD>` and a `MoneyBag` under them,
round-trips the strict shape, and deserializes a lowercase-ISO document under `Lenient`.

**What to expect.**

```
Strict  : {"amount":19.99,"currency":"USD"} -> USD 19.99
Compact : "19.99 USD"
Compact : {"EUR":12.34,"USD":19.99}  (MoneyBag)
Lenient : lowercase "usd" accepted -> USD 12.34
```

Strict is the canonical object shape for persistence and audit (duplicate properties and
currency mismatches are rejected). Compact collapses money to a single string and bags to a flat
ISO→amount map — for APIs and logs. Lenient is Strict's shape with whitespace/casing forgiveness
for external feeds — not a storage format.

**APIs demonstrated.** `JsonSerializerOptions.AddFinancialJsonConverters(FinancialJsonPolicy)`,
`FinancialJsonPolicy.Strict` / `Compact` / `Lenient`, the `Money<T>` / `Money` / `MoneyBag`
converters.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial
```
