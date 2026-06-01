---
uid: Bodu.Financial.Currencies
---

![Bodu.Financial](~/images/hero-core.svg)

## Purpose

**Bodu.Financial.Currencies** is the shipped catalogue of ~185 ISO 4217 currency tag types used as the `TCurrency` parameter on <xref:Bodu.Financial.Money`1>. Each currency is a sealed class with only static members — there is no instance to create, and the tag exists solely to carry the static metadata (`IsoCode`, `MinorUnits`, `CashRoundingIncrement`, and historic flags where applicable) that `Money<TCurrency>` needs.

## Static documentation

- **[Bodu.Financial introduction](~/docs/financial/index.md)** — how the tag types fit into the broader monetary surface.
- **[Working with `Money<TCurrency>`](~/guides/financial/money.md)** — the `ICurrency` tag pattern in context, including custom currencies.

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

**Active currencies (~150)** — including all G20 currencies and every minor-unit category:

- `MinorUnits = 0` — `JPY`, `KRW`, `CLP`, `ISK`, `VND`, `XAF`, `XOF`, `XPF`, `BIF`, `DJF`, `GNF`, `KMF`, `MGA`, `PYG`, `RWF`, `UGX`, `UYI`, `VUV`.
- `MinorUnits = 2` — `USD`, `EUR`, `GBP`, `AUD`, `CAD`, `CHF`, `CNY`, `HKD`, `INR`, `MXN`, `NZD`, `SEK`, `SGD`, …
- `MinorUnits = 3` — `BHD`, `IQD`, `JOD`, `KWD`, `LYD`, `OMR`, `TND`.
- `MinorUnits = 4` — `CLF`, `UYW`.

**Historic currencies (~30)** — the twenty Euro-zone predecessors (`ATS`, `BEF`, `CYP`, `DEM`, `EEK`, `ESP`, `FIM`, `FRF`, `GRD`, `HRK`, `IEP`, `ITL`, `LTL`, `LUF`, `LVL`, `MTL`, `NLG`, `PTE`, `SIT`, `SKK`) plus other notable replacements (`AZM`, `GHC`, `MZM`, `ROL`, `SRG`, `TMM`, `VEB`, `VEF`, `ZWL`).

**Cash rounding** — `CHF`, `AUD`, `CAD`, `NZD`, `SEK`, `NOK`, `ISK` and a handful of others declare a non-zero `CashRoundingIncrement` for physical cash totals (5-rappen / 5-cent / 10-cent / whole-krone). See [Cash rounding](~/guides/financial/money.md#cash-rounding).

## Adding a custom currency

Implement <xref:Bodu.Financial.ICurrency> directly and register the metadata with <xref:Bodu.Financial.CurrencyRegistry> so `MoneyValue` and `MoneyBag` round at the correct precision when they see the ISO code at runtime:

```csharp
public sealed class DOGE : ICurrency
{
    public static string IsoCode    => "DOGE";
    public static int    MinorUnits => 8;
    private DOGE() { }
}

CurrencyRegistry.Register(
    new CurrencyInfo("DOGE", MinorUnits: 8, CashRoundingIncrement: 0m,
                     IsHistoric: false, DemonetizedOn: null, SuccessorIsoCode: null));
```

## Notes

- **Sealed + private constructor.** Every tag type seals itself and hides its constructor, so the tag can only ever exist statically. `Money<USD>` is the only way to materialise a value tagged as USD.
- **Static-abstract metadata.** All members are static via the `ICurrency` static-abstract pattern; consumers read them as `TCurrency.IsoCode` in generic code.
- **Optional members default sensibly.** `CashRoundingIncrement`, `IsHistoric`, `DemonetizedOn`, `SuccessorIsoCode` have sensible defaults via `static virtual` so most currencies declare only `IsoCode` and `MinorUnits`.
- **See also:** the [`Bodu.Financial` reference](~/apidoc/Bodu.Financial.md), the [`Money<TCurrency>` guide](~/guides/financial/money.md), [`CurrencyRegistry`](xref:Bodu.Financial.CurrencyRegistry).
