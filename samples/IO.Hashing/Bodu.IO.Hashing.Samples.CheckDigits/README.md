# Bodu.IO.Hashing.Samples.CheckDigits

The `Bodu.IO.Hashing.CheckDigits` identifier surface: ~20 published check-digit schemes
behind one shape — static `Compute`/`IsValid` per scheme plus the streaming
`CheckDigitAlgorithm` base. Three scenarios cover validating identifiers across domains,
generating check digits when issuing new identifiers, and the error classes that
differentiate the schemes. All scenarios run offline over fixed, published example
identifiers.

```bash
dotnet run --project samples/IO.Hashing/Bodu.IO.Hashing.Samples.CheckDigits
```

## Scenario 1 — ValidateIdentifiers

**Intent.** Show that a form-validation layer treats every identifier domain identically:
IBAN, ISBN-10/13, EAN-13 barcodes, Luhn card numbers, and ABA routing numbers all expose the
same static `IsValid`, and a single mistyped character flips each from valid to invalid —
the exact failure the schemes exist to catch before a record hits a downstream system.

**What it does.** Validates one well-known published value per scheme, then corrupts one
interior character of each (a realistic typo) and validates again.

**What to expect.** Every intact value `True`, every typo `False`:

```text
  IBAN (mod 97-10): 'GB82WEST12345698765432' -> True,  typo 'GB82XEST12345698765432' -> False
  ISBN-13        : '9780306406157' -> True,  typo '9780406406157' -> False
  Card (Luhn)    : '79927398713' -> True,  typo '79928398713' -> False
  ABA routing    : '011000015' -> True,  typo '011010015' -> False
```

**APIs demonstrated.** `Iban.IsValid`, `Isbn10.IsValid` / `Isbn13.IsValid`, `Ean13.IsValid`,
`Luhn.IsValid`, `AbaRoutingNumber.IsValid`.

## Scenario 2 — ComputeAndAppend

**Intent.** Show the generation direction — issuing identifiers means computing the check
digit for a payload, one static `Compute` call — plus the streaming
`Append`/`GetCurrentCheckDigit` surface for payloads assembled in fragments, mirroring the
hashing side's `Append`/`GetCurrentHash` shape.

**What it does.** Appends the Luhn digit to a card payload (and re-validates the result),
derives an ISBN-13 check digit, computes an ISBN-10 check that lands on `'X'` (value 10 —
the check alphabet is not always decimal), and streams an EAN-13 payload in two fragments
before reading the digit.

**What to expect.**

```text
Luhn    : payload 7992739871 + check '3' = 79927398713 (valid: True)
ISBN-13 : payload 978030640615 + check '7' = 9780306406157
ISBN-10 : payload 097522980 + check 'X' ('X' = value 10)
EAN-13  : streamed 400638|133393 -> check '1' (EAN-13)
```

**APIs demonstrated.** `Luhn.Compute`, `Isbn13.Compute`, `Isbn10.Compute` (the `X` output
alphabet), instance `Append(ReadOnlySpan<char>)` / `GetCurrentCheckDigit()` /
`AlgorithmName`.

## Scenario 3 — TransposedDigits

**Intent.** Explain why multiple schemes exist: they detect different *error classes*. Luhn
catches every single-digit error but provably misses one adjacent transposition (`09 ↔ 90`);
Damm and Verhoeff were invented to detect all single-digit errors **and** all adjacent
transpositions. Choosing a scheme is choosing an error model.

**What it does.** Builds one valid identifier per scheme from the same payload, then swaps
every adjacent digit pair in turn and validates the damaged value, printing `.` where the
scheme caught the swap and `M` where it missed. Swaps of equal digits are skipped (they
change nothing).

**What to expect.** Exactly one `M` in the Luhn row — the `90` pair — and clean rows for
Damm and Verhoeff:

```text
  Luhn     12345678903: [........M.]
  Damm     12345678906: [..........]
  Verhoeff 12345678902: [..........]
```

**APIs demonstrated.** `Luhn.Compute/IsValid`, `Damm.Compute/IsValid`,
`Verhoeff.Compute/IsValid`, adjacent-transposition detection behaviour.

## Layout

```text
Bodu.IO.Hashing.Samples.CheckDigits/
  Program.cs                        # runs the scenarios in order
  Scenarios/ValidateIdentifiers.cs
  Scenarios/ComputeAndAppend.cs
  Scenarios/TransposedDigits.cs
```

## Related

- `Bodu.IO.Hashing.Samples.CustomCheckDigit` — implementing the `CheckDigitAlgorithm`
  contract yourself, proven by the shared contract-test base.
- `Bodu.IO.Hashing.Samples.ChecksumTour` — the byte-integrity half of the package.
- Guides: `docs/guides/io-hashing/check-digits.md`.
