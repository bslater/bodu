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
--- IsValid across identifier domains ---
  What   : Validates a known-good identifier from six schemes - IBAN, ISBN-10, ISBN-13, EAN-13, a Luhn card number
           and an ABA routing number - then alters exactly one digit in each and validates again.
  Why    : A check digit is arithmetic the issuer folded into the identifier so a recipient can reject a mistyped
           one before it reaches a system that would act on it. Validating locally turns a failed payment, a wrong
           book or a misrouted transfer into an input-validation error - free, instant, and with no lookup. Each
           scheme uses different arithmetic, which is why one IsValid per domain exists rather than a single generic
           check.
  Expect : Every genuine identifier validates and every single-digit alteration is rejected. Detecting any single
           wrong digit is the weakest guarantee all six schemes make - the next scenario shows where they start to
           differ.

  IBAN (mod 97-10): 'GB82WEST12345698765432' -> True,  typo 'GB82XEST12345698765432' -> False  (expected True then False - one altered digit, caught locally without any lookup)
  ISBN-10        : '0306406152' -> True,  typo '0306506152' -> False  (expected True then False - one altered digit, caught locally without any lookup)
  ISBN-13        : '9780306406157' -> True,  typo '9780406406157' -> False  (expected True then False - one altered digit, caught locally without any lookup)
  EAN-13 barcode : '4006381333931' -> True,  typo '4006481333931' -> False  (expected True then False - one altered digit, caught locally without any lookup)
  Card (Luhn)    : '79927398713' -> True,  typo '79928398713' -> False  (expected True then False - one altered digit, caught locally without any lookup)
  ABA routing    : '011000015' -> True,  typo '011010015' -> False  (expected True then False - one altered digit, caught locally without any lookup)
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
--- Compute - issuing identifiers with their check digit ---
  What   : Computes the check digit for a payload under four schemes and appends it, including the ISBN-10 case that
           produces 'X' and an EAN-13 computed from a streamed payload.
  Why    : Validation and issuance are the same arithmetic run in opposite directions, so the compute side is what
           an issuer needs - generating an account number, a barcode or an ISBN that every downstream validator will
           accept. The ISBN-10 case is the one worth knowing: its check value ranges 0-10, so ten is written as 'X'.
           Code that assumes a check digit is always a digit produces an identifier that fails everywhere.
  Expect : Each payload plus its computed digit validates as a whole, which is the round trip. ISBN-10 yields the
           literal character X for the value ten, and the streamed EAN-13 gives the same answer as a one-shot call.

  Luhn    : payload 7992739871 + check '3' = 79927398713  (valid: True - issuing and validating are the same arithmetic run in opposite directions)
  ISBN-13 : payload 978030640615 + check '7' = 9780306406157  (the same digit any downstream validator will recompute and compare)
  ISBN-10 : payload 097522980 + check 'X'  (X is the value TEN - code that assumes a check digit is always a digit issues identifiers that fail everywhere)
  EAN-13  : streamed 400638|133393 -> check '1'  (EAN-13 fed in two chunks gives the same answer as one call - the state is incremental)
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
--- Error classes - Luhn vs Damm vs Verhoeff on transpositions ---
  What   : Issues the same payload under three schemes, then swaps each adjacent pair of digits in turn and records
           whether the check digit catches it.
  Why    : All three catch any single wrong digit, so on that test they look equivalent. Transposition is where they
           separate, and it matters because swapping two adjacent digits is the second most common human typing
           error. Luhn - the scheme on every payment card - provably cannot detect the 09 to 90 swap, a documented
           gap in the algorithm rather than a bug. Damm and Verhoeff catch every adjacent transposition, which is
           why a new identifier scheme should not default to Luhn out of familiarity.
  Expect : The Luhn row shows an M at exactly one position - the 09/90 swap it is known to miss - and dots
           everywhere else. Damm and Verhoeff show dots throughout: no adjacent transposition gets past either.

  payload 1234567890: Luhn=12345678903, Damm=12345678906, Verhoeff=12345678902  (one payload, three schemes - each appends a different check digit)

  swap adjacent digits at each position; '.' = error detected, 'M' = MISSED:
  Luhn     12345678903: [........M.]  (one slot per adjacent pair: . caught it, M let it through)
  Damm     12345678906: [..........]  (one slot per adjacent pair: . caught it, M let it through)
  Verhoeff 12345678902: [..........]  (one slot per adjacent pair: . caught it, M let it through)

  Luhn misses the 09<->90 swap - a documented gap in the algorithm, not a bug here; Damm and Verhoeff detect every adjacent transposition, which is why a NEW scheme should not default to Luhn out of familiarity.
```

**APIs demonstrated.** `Luhn.Compute/IsValid`, `Damm.Compute/IsValid`,
`Verhoeff.Compute/IsValid`, adjacent-transposition detection behaviour.

## Layout

```text
Bodu.IO.Hashing.Samples.CheckDigits/
  Program.cs                        # runs the scenarios in order
  SampleConsole.cs                  # the what/why/expect banner every scenario opens with
  Scenarios/ValidateIdentifiers.cs
  Scenarios/ComputeAndAppend.cs
  Scenarios/TransposedDigits.cs
```

## Related

- `Bodu.IO.Hashing.Samples.CustomCheckDigit` — implementing the `CheckDigitAlgorithm`
  contract yourself, proven by the shared contract-test base.
- `Bodu.IO.Hashing.Samples.ChecksumTour` — the byte-integrity half of the package.
- Guides: `docs/guides/io-hashing/check-digits.md`.
