---
title: Alphanumeric and encoded check digits
---

# Alphanumeric and encoded check digits

The [check digits overview](check-digits.md) covers the decimal schemes and the financial identifiers. This page is the reference for the rest of the family: the schemes whose **body** is letters, symbols, or an encoded alphabet (Code 39, Crockford Base32, SEDOL, CUSIP, ISIN, ISO 7064), the ones whose **check character** is not a digit (`'X'`, a Code 39 symbol, a Base32 check symbol), and the two-character ISO 7064 MOD 97-10 codes (IBAN, LEI). It also documents the two decimal newcomers the overview does not — <xref:Bodu.IO.Hashing.CheckDigits.Gumm> and <xref:Bodu.IO.Hashing.CheckDigits.Iso7064Mod11_2> — explains the four-level `CheckValueAlgorithm` hierarchy and its alphabet enums, shows how to author a scheme, and closes with an error-class table measured by exhaustive sweep.

> [!IMPORTANT]
> Check digits detect *transcription* errors. They are not cryptographic, and anyone who can edit an identifier can recompute its check. For tamper detection use a MAC from [Bodu.Security.Cryptography](../cryptography/hashing.md).

## The four-level hierarchy

| Level | Type | Adds | Result surface |
|---|---|---|---|
| Root | <xref:Bodu.IO.Hashing.CheckDigits.CheckValueAlgorithm> | `AlgorithmName`, `CheckLength`, `Append(ReadOnlySpan<char>)`, `Append(char)`, `Reset()` | `GetCurrentCheckValue()` → `string` of `CheckLength` characters |
| Decimal, one digit | <xref:Bodu.IO.Hashing.CheckDigits.CheckDigitAlgorithm> | — | `GetCurrentCheckDigit()` → `char` |
| Wider alphabet, one character | <xref:Bodu.IO.Hashing.CheckDigits.AlphanumericCheckDigitAlgorithm> | `InputAlphabet`, `OutputAlphabet` | `GetCurrentCheckDigit()` → `char` |
| Multi-character code | <xref:Bodu.IO.Hashing.CheckDigits.MultiCharCheckDigitAlgorithm> | `InputAlphabet`, abstract `CheckLength` | `GetCurrentCheckDigits(Span<char>)` → count; `GetCurrentCheckDigits()` → `string` |

Every concrete type also exposes static `Compute(body)` and `IsValid(valueIncludingCheck)`. `GetCurrentCheckValue()` is sealed on the three branches and delegates to the branch's own accessor, so a `CheckValueAlgorithm`-typed variable can validate any identifier without knowing which branch it belongs to. Reading the current check is non-destructive and idempotent; instances are not thread-safe. None of these types derive from `NonCryptographicHashAlgorithm` — a check character is `char`-oriented error detection over a constrained alphabet, not a byte digest.

### The alphabets

<xref:Bodu.IO.Hashing.CheckDigits.CheckDigitInputAlphabet> says what `Append` accepts; <xref:Bodu.IO.Hashing.CheckDigits.CheckDigitOutputAlphabet> says what a single-character scheme can emit. Multi-character schemes emit decimal digits only and do not declare an output alphabet.

| Enum value | Members | Declared by |
|---|---|---|
| `CheckDigitInputAlphabet.DecimalDigits` | `0`–`9` | `Isbn10`, `Iso7064Mod11_2` |
| `CheckDigitInputAlphabet.AlphanumericUppercase` | `0`–`9`, `A`–`Z` | `Isin`, `Sedol`, `Cusip`, `Iso7064Mod97_10`, `Iban`, `Lei` |
| `CheckDigitInputAlphabet.Code39` | `0`–`9`, `A`–`Z`, `-` `.` space `$` `/` `+` `%` | `Code39Mod43` |
| `CheckDigitInputAlphabet.CrockfordBase32` | the 32 Crockford symbols (no `I`, `L`, `O`, `U`); case-insensitive, `I`/`L` → `1`, `O` → `0` | `Crockford32` |
| `CheckDigitOutputAlphabet.DecimalDigits` | `0`–`9` | `Isin`, `Sedol`, `Cusip` |
| `CheckDigitOutputAlphabet.DecimalDigitsOrX` | `0`–`9` plus `X` for the value ten | `Isbn10`, `Iso7064Mod11_2` |
| `CheckDigitOutputAlphabet.Code39` | the full 43-symbol Code 39 alphabet | `Code39Mod43` |
| `CheckDigitOutputAlphabet.CrockfordBase32Check` | the 32 symbols plus `*` `~` `$` `=` `U` for 32–36 | `Crockford32` |

## The catalogue

| Type | Base | Body | Check | Scheme |
|---|---|---|---|---|
| <xref:Bodu.IO.Hashing.CheckDigits.Code39Mod43> | Alphanumeric | Code 39 symbols | one Code 39 symbol | sum of symbol values mod 43 |
| <xref:Bodu.IO.Hashing.CheckDigits.Crockford32> | Alphanumeric | Crockford Base32 | one of 37 symbols | encoded value mod 37 (Horner reduction — any length) |
| <xref:Bodu.IO.Hashing.CheckDigits.Iso7064Mod11_2> | Alphanumeric | decimal digits | digit or `X` | ISO 7064 MOD 11-2 (pure system, e.g. Chinese resident IDs, ORCID) |
| <xref:Bodu.IO.Hashing.CheckDigits.Isbn10> | Alphanumeric | 9 digits | digit or `X` | weighted mod 11 |
| <xref:Bodu.IO.Hashing.CheckDigits.Sedol> | Alphanumeric | 6 uppercase alphanumerics (no vowels) | digit | weights 1, 3, 1, 7, 3, 9 mod 10 |
| <xref:Bodu.IO.Hashing.CheckDigits.Cusip> | Alphanumeric | 8 uppercase alphanumerics (`*`, `@`, `#` accepted) | digit | Luhn-style over expanded values |
| <xref:Bodu.IO.Hashing.CheckDigits.Isin> | Alphanumeric | 2-letter country + 9 alphanumerics | digit | Luhn over letters expanded to two digits |
| <xref:Bodu.IO.Hashing.CheckDigits.Iso7064Mod97_10> | Multi-char | uppercase alphanumerics | two digits | ISO 7064 MOD 97-10 |
| <xref:Bodu.IO.Hashing.CheckDigits.Iban> | Multi-char | country code + BBAN (`CountryCodeLength = 2`) | two digits (`CheckDigits = 2`) | MOD 97-10 with the country code rotated to the end |
| <xref:Bodu.IO.Hashing.CheckDigits.Lei> | Multi-char | 18 alphanumerics (`BodyLength = 18`) | two digits | MOD 97-10 (`SequenceLength = 20`) |
| <xref:Bodu.IO.Hashing.CheckDigits.Gumm> | Decimal | decimal digits | digit | dihedral group *D₅* with an alternating transform (Gumm, 1985) |

`Gumm` sits on the decimal branch beside Luhn, Damm, and Verhoeff; it appears here because the overview predates it. `Iso7064Mod11_2` is on the alphanumeric branch only because its check may be `X`.

## Pattern 1 — compute and validate

<!-- compile -->
```csharp
using Bodu.IO.Hashing.CheckDigits;

char code39  = Code39Mod43.Compute("CODE39");                // 'W'  → "CODE39W"
char b32     = Crockford32.Compute("16J");                   // 'D'  → "16JD"   (input is case-insensitive: "16j" also → 'D')
char gumm    = Gumm.Compute("236");                          // '9'  → "2369"
char mod11   = Iso7064Mod11_2.Compute("0794");               // '0'  → "07940"
char mod11x  = Iso7064Mod11_2.Compute("079");                // 'X'  — the check value ten
string mod97 = Iso7064Mod97_10.Compute("794");               // "44" → "79444"
char isbn    = Isbn10.Compute("030640615");                  // '2'
char sedol   = Sedol.Compute("B0YBKJ");                      // '7'
char cusip   = Cusip.Compute("03783310");                    // '0'  (Apple, 037833100)
char isin    = Isin.Compute("US037833100");                  // '5'  (US0378331005)
string lei   = Lei.Compute("5493001KJTIIGC8Y1R");            // "12"
string iban  = Iban.Compute("GBWEST12345698765432");         // "82" → GB82 WEST 1234 5698 7654 32

bool ok1 = Code39Mod43.IsValid("CODE39W");                   // true
bool ok2 = Crockford32.IsValid("16JD");                      // true
bool ok3 = Iban.IsValid("GB82WEST12345698765432");           // true
bool ok4 = Iban.IsValid("GB82WEST12345698765433");           // false — one digit off
bool ok5 = Isbn10.IsValid("080442957X");                     // true — 'X' check
```

`Compute` takes the body without its check; `IsValid` takes the full identifier. IBAN's `Compute` takes the body in its natural order (country code first) and returns the two digits that belong in positions 3–4; `IsValid` takes the whole IBAN.

## Pattern 2 — one code path for every scheme

Because the root type unifies the streaming surface, a validator can hold a `CheckValueAlgorithm` and pattern-match only when it needs the branch-specific detail:

<!-- compile -->
```csharp
using Bodu.IO.Hashing.CheckDigits;

var checks = new (CheckValueAlgorithm Algorithm, string Body)[]
{
    (new Luhn(), "7992739871"), (new Code39Mod43(), "CODE39"), (new Crockford32(), "16J"),
    (new Iso7064Mod11_2(), "0794"), (new Iso7064Mod97_10(), "794"), (new Iban(), "GBWEST12345698765432"),
};

foreach (var (algorithm, body) in checks)
{
    algorithm.Reset();
    algorithm.Append(body);
    string check = algorithm.GetCurrentCheckValue();          // "3", "W", "D", "0", "44", "82"

    string alphabets = algorithm switch
    {
        AlphanumericCheckDigitAlgorithm a => $"in={a.InputAlphabet}, out={a.OutputAlphabet}",
        MultiCharCheckDigitAlgorithm m => $"in={m.InputAlphabet}, out=DecimalDigits×{m.CheckLength}",
        _ => "in=DecimalDigits, out=DecimalDigits",
    };
    Console.WriteLine($"{algorithm.AlgorithmName,-20} {body,-22} -> \"{check}\" ({alphabets})");
}
```

`AlgorithmName` values are stable identifiers such as `"Code 39 Mod 43"`, `"Crockford Base32"`, `"ISO 7064 MOD 11-2"`, `"ISO 7064 MOD 97-10"`, `"IBAN"`, `"LEI"`, `"SEDOL"`, `"CUSIP"`, `"ISIN"`, `"Gumm"`.

## Pattern 3 — streaming a body in chunks

`Append` accumulates; `GetCurrentCheckDigit` / `GetCurrentCheckDigits` read without finalizing; `Reset` starts over. The multi-character branch writes into a caller span and returns the count, or allocates a string:

<!-- compile -->
```csharp
using Bodu.IO.Hashing.CheckDigits;

var iban = new Iban();
iban.Append("GB");                       // country code
iban.Append("WEST1234");                 // BBAN, in whatever chunks arrive
iban.Append("5698765432");

char[] check = new char[iban.CheckLength];
int written = iban.GetCurrentCheckDigits(check);          // "82", written == 2
string asString = iban.GetCurrentCheckDigits();            // "82"
string viaRoot = iban.GetCurrentCheckValue();              // "82"

var mod43 = new Code39Mod43();
mod43.Append("CODE");
mod43.Append('3');
mod43.Append('9');
char symbol = mod43.GetCurrentCheckDigit();                // 'W'
mod43.Reset();                                             // empty body → '0'
```

## Pattern 4 — authoring a scheme

Derive from the branch that matches the check character you emit. Implement `AlgorithmName`, the alphabet properties, `Append` (validate and accumulate), the result accessor, and `Reset`; add static `Compute` / `IsValid` to match the built-in surface. Below is a weighted mod-10 scheme over an uppercase alphanumeric body (illustrative — it detects fewer errors than the ISO 7064 schemes above).

```csharp
using Bodu.IO.Hashing.CheckDigits;

public sealed class Mod36WeightedCheck : AlphanumericCheckDigitAlgorithm
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private int _sum;
    private int _position;

    public override string AlgorithmName => "Mod 36 weighted";
    public override CheckDigitInputAlphabet InputAlphabet => CheckDigitInputAlphabet.AlphanumericUppercase;
    public override CheckDigitOutputAlphabet OutputAlphabet => CheckDigitOutputAlphabet.DecimalDigits;

    public static char Compute(ReadOnlySpan<char> body)
    {
        var algorithm = new Mod36WeightedCheck();
        algorithm.Append(body);
        return algorithm.GetCurrentCheckDigit();
    }

    public static bool IsValid(ReadOnlySpan<char> valueIncludingCheck) =>
        valueIncludingCheck.Length >= 2 && Compute(valueIncludingCheck[..^1]) == valueIncludingCheck[^1];

    public override void Append(ReadOnlySpan<char> body)
    {
        foreach (char ch in body)
        {
            int value = Alphabet.IndexOf(char.ToUpperInvariant(ch));
            if (value < 0) throw new ArgumentException($"'{ch}' is outside the uppercase alphanumeric alphabet.", nameof(body));
            _sum += value * (_position % 2 == 0 ? 3 : 1);      // alternate weights 3,1,3,1,…
            _position++;
        }
    }

    public override char GetCurrentCheckDigit() => (char)('0' + ((10 - (_sum % 10)) % 10));

    public override void Reset() { _sum = 0; _position = 0; }
}
```

```csharp
using Bodu.IO.Hashing.CheckDigits;

char check = Mod36WeightedCheck.Compute("SKU7A");            // '9'
bool valid = Mod36WeightedCheck.IsValid("SKU7A9");           // true
bool typo  = Mod36WeightedCheck.IsValid("SKU7B9");           // false

AlphanumericCheckDigitAlgorithm viaBase = new Mod36WeightedCheck();
viaBase.Append("SKU7A");
string value = viaBase.GetCurrentCheckValue();               // "9"
```

The repository's `Bodu.IO.Hashing.Samples.CustomCheckDigit` sample and its `.Test` companion derive the library's `CheckDigitContractTests<TAlgorithm>` (the decimal branch); the multi-character branch has `MultiCharCheckDigitContractTests<TAlgorithm>` in the same test project, and the alphanumeric branch currently has no dedicated contract base — pin your scheme with known-answer rows as the built-in tests do. See the [runnable samples](../../samples/io-hashing.md).

## Error classes — measured

The source documents coverage for the decimal schemes: Luhn misses the `09 ↔ 90` adjacent transposition and twin errors; Damm, Verhoeff, and Gumm detect every adjacent transposition; Code 39's commutative sum cannot see any transposition. To put every scheme on one footing, the table below was produced by exhaustive sweep with the library — every ordered pair `ab` (`a ≠ b`) for adjacent transposition, and every `aab` versus `ccb` (`c ≠ a`) for twin errors — over each scheme's own alphabet:

| Scheme | Body swept | Adjacent transpositions missed | Twin errors missed |
|---|---|---|---|
| `Luhn` | 2 / 3 decimal digits (90 / 900 cases) | **2** (`09` ↔ `90`) | 60 |
| `Damm` | same | 0 | 80 |
| `Verhoeff` | same | 0 | 40 |
| `Gumm` | same | 0 | 400 |
| `Iso7064Mod11_2` | same | 0 | 0 |
| `Crockford32` | 2 / 3 Base32 symbols (992 / 31 744 cases) | 0 | 0 |
| `Code39Mod43` | 2 / 3 Code 39 symbols (1 806 / 77 658 cases) | **all 1 806** | 0 |
| `Sedol` (last two body positions) | 2 / 3 SEDOL symbols | 164 | 5 084 |
| `Iso7064Mod97_10` | 2 / 3 uppercase alphanumerics (1 260 / 45 360 cases) | 6 | 216 |

Read the table with its definition in mind. "Twin" here is the leading pair of a three-character body changed to another repeated pair, which is one position of one error class; Verhoeff's published figure (all adjacent transpositions, most twins) matches the sweep, and the source's description of Gumm as detecting "the `aa ↔ bb` twin cases" is not borne out at this position. The ISO 7064 schemes catch everything at the digit level (MOD 11-2 is perfect on decimal bodies); MOD 97-10's handful of misses come from *character* swaps that expand letters into two digits, where a swap is no longer a digit transposition. Code 39's check is a plain sum — it is a substitution detector only. Crockford's check, being a modular reduction of the encoded value, is equivalent to a positional weighting and detects both classes in this sweep.

<!-- compile -->
```csharp
using Bodu.IO.Hashing.CheckDigits;

// The classic Luhn blind spot beside the schemes that close it.
bool luhn = Luhn.Compute("1209") != Luhn.Compute("1290");          // false — undetected
bool damm = Damm.Compute("1209") != Damm.Compute("1290");          // true
bool gumm = Gumm.Compute("1209") != Gumm.Compute("1290");          // true
bool mod43 = Code39Mod43.Compute("AB") != Code39Mod43.Compute("BA"); // false — a sum cannot see order
```

## API summary

| Type | Static | Instance |
|---|---|---|
| <xref:Bodu.IO.Hashing.CheckDigits.CheckValueAlgorithm> | — | `AlgorithmName`, `CheckLength`, `Append(ReadOnlySpan<char>)`, `Append(char)`, `GetCurrentCheckValue()`, `Reset()` |
| <xref:Bodu.IO.Hashing.CheckDigits.AlphanumericCheckDigitAlgorithm> | — | `InputAlphabet`, `OutputAlphabet`, `GetCurrentCheckDigit()` |
| <xref:Bodu.IO.Hashing.CheckDigits.MultiCharCheckDigitAlgorithm> | — | `InputAlphabet`, `CheckLength`, `GetCurrentCheckDigits(Span<char>)`, `GetCurrentCheckDigits()` |
| `Code39Mod43`, `Crockford32`, `Iso7064Mod11_2`, `Isbn10`, `Sedol`, `Cusip`, `Isin`, `Gumm` | `Compute(body)` → `char`, `IsValid(value)` | as the branch above |
| `Iso7064Mod97_10`, `Iban`, `Lei` | `Compute(body)` → `string`, `IsValid(value)`; `CheckDigits = 2` | as the multi-character branch; `Iban.CountryCodeLength`, `Lei.BodyLength` / `SequenceLength` |
| <xref:Bodu.IO.Hashing.CheckDigits.CheckDigitInputAlphabet> / <xref:Bodu.IO.Hashing.CheckDigits.CheckDigitOutputAlphabet> | — | the alphabet enums above |

## Where to go next

- [Check digits overview](check-digits.md) — the decimal schemes, ISBN-13, EAN/GTIN/UPC, and the ABA routing number.
- [Core concepts › Transcription error classes](../../docs/io-hashing/concepts.md#transcription-error-classes) — what each error class means.
- [Runnable samples](../../samples/io-hashing.md) — the `CheckDigits` tour and the custom-scheme contract test.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
