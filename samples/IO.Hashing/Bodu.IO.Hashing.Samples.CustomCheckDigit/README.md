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
--- SKU-731 - issuing and validating with a custom scheme ---
  What   : Issues three SKUs by appending the computed check digit, validates an intact SKU against a
           wrong-check-digit and a mistyped-payload variant, and reads a digit from a payload fed in two fragments.
  Why    : A check digit turns a transcription error into a local, immediate failure. Without one, a mistyped SKU is
           just a different SKU - the system happily looks it up, finds nothing or finds the wrong thing, and the
           error surfaces somewhere far from where it was made. The point of implementing the library contract
           rather than a loose helper is that issuing and validating then provably share one arithmetic definition:
           the digit a validator recomputes cannot drift from the digit the issuer emitted.
  Expect : Every issued SKU validates. Both corrupted variants are rejected - one altered digit is enough, with no
           catalogue, database or network lookup involved. The streamed payload yields the same digit as the
           one-shot call, because the base class carries position across Append calls rather than restarting the
           weight cycle per fragment.

  payload 123456789 -> SKU 1234567893
  payload 000451    -> SKU 0004516
  payload 998877    -> SKU 9988778
  (000451 keeps its leading zeros: the weights are positional, so a zero still shifts every later digit's weight)
  IsValid('1234567893')  = True  (expected True - validating is the same arithmetic the issuer ran, checked against the digit it emitted)
  IsValid('1234567892') = False  (expected False - the check digit was altered, so it no longer brings the weighted sum to a multiple of ten)
  IsValid('1235567893') = False  (expected False - one mistyped payload digit, caught locally without any lookup)
  streamed 12345|6789 -> check '3' via SKU-731  (expected '3', matching the one-shot digit above - the fragment boundary is not observable)
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
--- CheckDigitAlgorithm polymorphism - a custom scheme beside the built-ins ---
  What   : Drives SkuCheckDigit, Luhn and Damm through one loop typed against the abstract base class, printing each
           scheme's name and the digit it issues for the same payload.
  Why    : This is the reason for deriving the library's base class instead of writing a standalone helper. An
           issuing pipeline, a validation middleware or a test harness can be written once against
           CheckDigitAlgorithm and accept any scheme - including one the library has never heard of. It also means
           the custom scheme inherits the streaming Append surface and the Reset lifecycle for free, and can be held
           to the same contract tests as the built-in catalogue.
  Expect : Three different digits for one payload, which is the correct outcome: each scheme is a different
           function, so a value issued under one is not valid under another. The caller never branches on which
           scheme it holds.

  SKU-731 : 31415926 -> check '9'
  Luhn    : 31415926 -> check '0'
  Damm    : 31415926 -> check '6'
  (three schemes, three digits - a SKU-731 value is not a valid Luhn value, so the scheme travels with the identifier)
  one issuing pipeline, any scheme - the same pattern the contract tests verify.
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
  SampleConsole.cs                  # the What / Why / Expect scenario banner
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
