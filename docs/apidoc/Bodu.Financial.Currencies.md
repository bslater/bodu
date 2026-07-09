---
uid: Bodu.Financial.Currencies
---

![Bodu.Financial](~/images/hero-financial.svg)

## Purpose

**Bodu.Financial.Currencies** is the currency-metadata namespace of the [`Bodu.Financial`](Bodu.Financial.md) package. It hosts the runtime metadata and lookup surface — the `ICurrency` contract, the `CurrencyInfo` record, the `CurrencyRegistry` catalogue, and the `ICurrencyLookup` resolution seam — alongside the shipped catalogue of 184 ISO 4217 currency tag types used as the `TCurrency` parameter on <xref:Bodu.Financial.Money`1>. Each currency is a sealed class with only static members — there is no instance to create, and the tag exists solely to carry the static metadata (`IsoCode`, `MinorUnits`, `CashRoundingIncrement`, and historic flags where applicable) that `Money<TCurrency>` needs.

## Static documentation

- **[Bodu.Financial introduction](~/docs/financial/index.md)** — how the tag types fit into the broader monetary surface.
- **[Working with `Money<TCurrency>`](~/guides/financial/money.md)** — the `ICurrency` tag pattern in context, including units outside the shipped catalogue.

## Key types

**Runtime metadata and lookup**

- <xref:Bodu.Financial.Currencies.ICurrency> — static-abstract interface with required `IsoCode` and `MinorUnits` plus optional `CashRoundingIncrement`, `IsHistoric`, `DemonetizedOn`, `SuccessorIsoCode`.
- <xref:Bodu.Financial.Currencies.CurrencyInfo> — runtime metadata record carrying the same fields.
- <xref:Bodu.Financial.Currencies.CurrencyRegistry> — static, read-only catalogue over the shipped ISO 4217 currencies (active and historic).
- <xref:Bodu.Financial.Currencies.ICurrencyLookup>, <xref:Bodu.Financial.Currencies.CurrencyLookupService> — the lookup contract and the implementation that resolves ISO codes to metadata (the service registered by `AddFinancialService`).
- <xref:Bodu.Financial.Currencies.CurrencyResolution> — the seam for substituting or restricting the metadata used for the shipped currencies (a test double, an alternate data source).
- <xref:Bodu.Financial.Currencies.CurrencyCode> — the closed enum that identifies a currency on <xref:Bodu.Financial.Money> and the exchange types; one member per shipped ISO 4217 code, valued by its ISO numeric code.
- <xref:Bodu.Financial.Currencies.CurrencyCodeExtensions> — catalogue helpers over `CurrencyCode`: `GetStatus`, `IsActive`, `IsHistoric`, resolving each member's lifecycle status from its declarative attribute (cached at type initialization).
- <xref:Bodu.Financial.Currencies.CurrencyStatusAttribute> — the `[CurrencyStatus(...)]` annotation on each `CurrencyCode` member that is the declarative source of truth for a currency's lifecycle status.

## Catalogue shape

Every shipped currency type follows the same shape:

```csharp
public sealed class USD : ICurrency
{
    public static string IsoCode    => "USD";
    public static int    MinorUnits => 2;
    private USD() { }
}
```

Currencies with a cash-rounding convention declare the increment:

```csharp
public sealed class CHF : ICurrency
{
    public static string  IsoCode               => "CHF";
    public static int     MinorUnits            => 2;
    public static decimal CashRoundingIncrement => 0.05m;
    private CHF() { }
}
```

Historic (demonetised) currencies declare the withdrawal metadata:

```csharp
public sealed class DEM : ICurrency
{
    public static string    IsoCode           => "DEM";
    public static int       MinorUnits        => 2;
    public static bool      IsHistoric        => true;
    public static DateOnly? DemonetizedOn     => new DateOnly(2002, 2, 28);
    public static string?   SuccessorIsoCode  => "EUR";
    private DEM() { }
}
```

## Catalogue contents

The shipped catalogue covers every active ISO 4217 currency plus a curated set of historic / demonetised currencies for legacy ledger processing.

**Active currencies (155)** — including all G20 currencies and every minor-unit category:

- `MinorUnits = 0` — `JPY`, `KRW`, `CLP`, `ISK`, `VND`, `XAF`, `XOF`, `XPF`, `BIF`, `DJF`, `GNF`, `KMF`, `MGA`, `PYG`, `RWF`, `UGX`, `UYI`, `VUV`.
- `MinorUnits = 2` — `USD`, `EUR`, `GBP`, `AUD`, `CAD`, `CHF`, `CNY`, `HKD`, `INR`, `MXN`, `NZD`, `SEK`, `SGD`, …
- `MinorUnits = 3` — `BHD`, `IQD`, `JOD`, `KWD`, `LYD`, `OMR`, `TND`.
- `MinorUnits = 4` — `CLF`, `UYW`.

**Historic currencies (~30)** — the twenty Euro-zone predecessors (`ATS`, `BEF`, `CYP`, `DEM`, `EEK`, `ESP`, `FIM`, `FRF`, `GRD`, `HRK`, `IEP`, `ITL`, `LTL`, `LUF`, `LVL`, `MTL`, `NLG`, `PTE`, `SIT`, `SKK`) plus other notable replacements (`AZM`, `GHC`, `MZM`, `ROL`, `SRG`, `TMM`, `VEB`, `VEF`, `ZWL`).

**Cash rounding** — `CHF`, `AUD`, `CAD`, `NZD`, `SEK`, `NOK`, `ISK` and a handful of others declare a non-zero `CashRoundingIncrement` for physical cash totals (5-rappen / 5-cent / 10-cent / whole-krone). See [Cash rounding](~/guides/financial/money.md#cash-rounding).

## A unit outside the shipped catalogue

The shipped <xref:Bodu.Financial.Currencies.CurrencyCode> catalogue is closed, so the runtime <xref:Bodu.Financial.Money> cannot hold a code it does not define. For a *generic* amount in a unit outside ISO 4217 — a commodity, a loyalty-point unit — implement <xref:Bodu.Financial.Currencies.ICurrency> directly and use `Money<TCurrency>`; the tag carries its own precision and never consults the runtime catalogue (its `IsoCode` must still be three uppercase ASCII letters):

```csharp
public sealed class XPT : ICurrency      // troy ounces of platinum, say
{
    public static string IsoCode    => "XPT";
    public static int    MinorUnits => 4;
    private XPT() { }
}

Money<XPT> holding = new Money<XPT>(12.3456m);   // generic arithmetic only
```

Because `XPT` is not a `CurrencyCode` member, the value cannot bridge to the runtime-tagged `Money`. To substitute or restrict the metadata used for the *shipped* currencies — for a test, or an alternate data source — install a custom `ICurrencyLookup` through <xref:Bodu.Financial.Currencies.CurrencyResolution>.

## Notes

- **Sealed + private constructor.** Every tag type seals itself and hides its constructor, so the tag can only ever exist statically. `Money<USD>` is the only way to materialise a value tagged as USD.
- **Static-abstract metadata.** All members are static via the `ICurrency` static-abstract pattern; consumers read them as `TCurrency.IsoCode` in generic code.
- **Optional members default sensibly.** `CashRoundingIncrement`, `IsHistoric`, `DemonetizedOn`, `SuccessorIsoCode` have sensible defaults via `static virtual` so most currencies declare only `IsoCode` and `MinorUnits`.
- **ISO numeric codes.** <xref:Bodu.Financial.Currencies.CurrencyCode> is an enum whose members are the three-letter ISO 4217 alphabetic codes and whose values are the corresponding ISO 4217 numeric codes; both active and historic codes are members — each annotated with a `[CurrencyStatus]` attribute — alongside a `None` sentinel valued `0`.
- **See also:** the [`Bodu.Financial` reference](xref:Bodu.Financial), the [`Money<TCurrency>` guide](~/guides/financial/money.md), [`CurrencyRegistry`](xref:Bodu.Financial.Currencies.CurrencyRegistry).
