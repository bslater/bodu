# Bodu.IO.Hashing.Samples.CustomCheckDigit

Extending the check-digit catalogue: a complete custom scheme — `SkuCheckDigit`, a weighted
mod-10 algorithm using the classic repeating `7, 3, 1` cycle (the shape used by several
transport and inventory schemes, deliberately different from Luhn's double-and-fold) —
implementing the library's `CheckDigitAlgorithm` contract, plus a companion test project that
derives the shared `CheckDigitContractTests<TAlgorithm>` base to prove the implementation.
Offline and deterministic; no data files.

```bash
dotnet run --project samples/IO.Hashing/Bodu.IO.Hashing.Samples.CustomCheckDigit
dotnet test samples/IO.Hashing/Bodu.IO.Hashing.Samples.CustomCheckDigit.Test --settings bvt.runsettings
```

## The implementation — `SkuCheckDigit`

`SkuCheckDigit : CheckDigitAlgorithm` weights each payload digit with the repeating
`7, 3, 1` cycle from the left, sums, and emits the digit that brings the total to a multiple
of ten. The whole base contract is four members — `AlgorithmName`,
`Append(ReadOnlySpan<char>)`, `GetCurrentCheckDigit()`, `Reset()` — and the static
`Compute`/`IsValid` pair mirrors the convenience surface every built-in scheme exposes.

## Scenario 1 — IssueAndValidate

**Intent.** Exercise the custom scheme end to end the way an issuing system would: append the
computed digit to each new payload, validate intact and mistyped values, and use the
streaming surface inherited from the base for payloads assembled in fragments.

**What it does.** Issues three SKUs (including one with leading zeros — position-based
weights preserve them), validates the intact SKU plus a wrong-check-digit and a
mistyped-payload variant, and streams a payload in two `Append` fragments before reading the
digit.

**What to expect.**

```text
  payload 123456789 -> SKU 1234567893
  payload 000451    -> SKU 0004516
  IsValid('1234567893')  = True
  IsValid('1234567892') = False (wrong check digit)
  IsValid('1235567893') = False (mistyped payload digit)
  streamed 12345|6789 -> check '3' (SKU-731)
```

**APIs demonstrated.** Deriving `CheckDigitAlgorithm` (the four abstract members), the
static `Compute`/`IsValid` convention, streaming `Append` with positional weights.

## Scenario 2 — BesideTheBuiltIns

**Intent.** Show the payoff of deriving the base class: the custom scheme is a drop-in peer.
An issuing harness typed against `CheckDigitAlgorithm` drives `SkuCheckDigit`, `Luhn`, and
`Damm` identically — the same polymorphism the shared contract tests rely on.

**What it does.** Runs one payload through the three algorithms via base-class-typed calls
(`Append` → `GetCurrentCheckDigit` → `Reset`), printing each scheme's name and digit.

**What to expect.**

```text
  SKU-731 : 31415926 -> check '9'
  Luhn    : 31415926 -> check '0'
  Damm    : 31415926 -> check '6'
```

**APIs demonstrated.** `CheckDigitAlgorithm` polymorphism, `AlgorithmName`, `Reset()`.

## The contract test — `Bodu.IO.Hashing.Samples.CustomCheckDigit.Test`

`SkuCheckDigitContractTests` derives the library test suite's
`CheckDigitContractTests<SkuCheckDigit>` (namespace `Bodu.IO.Hashing.Contracts`) and supplies
only the two adapter members (`Compute`, `IsValid`) plus six `CheckDigitKat` known-answer
rows (covering leading zeros, single-digit, and multi-length payloads). The inherited tests
verify compute-vs-vector parity, canonical full-value acceptance, and corrupted-check-digit
rejection — the same bar `Luhn`, `Damm`, and the rest of the catalogue are held to. The test
project references `Bodu.Test` and the `Bodu.IO.Hashing.Test` project (where the contract
base and KAT record live, per the "colocate with the consumer" rule) and runs in the default
BVT tier.

## Layout

```text
Bodu.IO.Hashing.Samples.CustomCheckDigit/
  Program.cs                        # runs the scenarios in order
  SkuCheckDigit.cs                  # the CheckDigitAlgorithm implementation
  Scenarios/IssueAndValidate.cs
  Scenarios/BesideTheBuiltIns.cs
Bodu.IO.Hashing.Samples.CustomCheckDigit.Test/
  SkuCheckDigitContractTests.cs     # derives CheckDigitContractTests<SkuCheckDigit>
```

## Related

- `Bodu.IO.Hashing.Samples.CheckDigits` — the built-in scheme catalogue the custom algorithm
  joins.
- Guides: `docs/guides/io-hashing/check-digits.md`.
