# Bodu.Financial.Samples.MoneyBasics

The core `Bodu.Financial` value types and policies, demonstrated offline with in-code data only.

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.MoneyBasics
```

## What it demonstrates

- `Scenarios/RoundingTiers.cs` — the three-tier rounding model: `Money<T>` (rounds every step),
  `CalculatedMoney` (defers to one settlement), `Fraction<BigInteger>` via `MultiplyExact` (exact).
- `Scenarios/TypedRuntimeBridges.cs` — `Money<TCurrency>` vs runtime `Money`, and the checked
  bridges between them (`As<T>`, `TryAs<T>`, implicit/explicit casts).
- `Scenarios/Allocation.cs` — sum-preserving largest-remainder allocation (equal and weighted),
  zero-decimal currencies, and `RoundToCash` cash-increment snapping.
- `Scenarios/FormattingParsing.cs` — the format-specifier vocabulary (`G`/`C`/`L`/`N`/`R`, `~`
  and precision modifiers), `MoneyFormatterBuilder`, and the four `MoneyParseMode` levels.
- `Scenarios/MoneyBagLedger.cs` — `MoneyBag` multi-currency ledgers and whole-bag conversion,
  including `ConvertToWithAudit` per-line audit trails.
- `Scenarios/JsonPolicies.cs` — `AddFinancialJsonConverters` with the `Strict`, `Lenient`, and
  `Compact` policies.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial
```
