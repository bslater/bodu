---
title: Check digits
---

# Check digits

Check-digit algorithms validate human-readable identifiers — credit card numbers, barcodes, bank account numbers, securities codes — by appending a short computed suffix that lets any reader confirm the identifier was not mis-typed or mis-transcribed. They target the specific error patterns that humans introduce when copying a string by hand: single-digit substitutions, adjacent transpositions, twin errors (e.g. `11 → 22`), and jump transpositions.

> **Note.** Check-digit algorithms are not cryptographic and must not be used for password hashing, digital signatures, or integrity validation in security-sensitive applications. They are error-detection primitives for human-readable identifiers, nothing more.

## Two namespaces, one purpose

The library splits its check-digit types across two namespaces according to the character set and output structure of the algorithm:

| Namespace | Base class | Input | Output | Types |
|---|---|---|---|---|
| `Bodu.IO.Hashing.CheckDigits` | `CheckDigitAlgorithm` | ASCII decimal digits (`'0'`–`'9'`) | Single `char` | Luhn, Damm, Verhoeff, EAN-8/13, GTIN-14, UPC-A, ISIN, ABA |
| `Bodu.IO.Hashing.Checksums` | `AlphanumericCheckDigitAlgorithm` | Digits and/or letters | One or two `char`s | IBAN, ISBN-10/13, SEDOL, CUSIP, LEI, WeightedMod10 |

Both namespaces expose the same streaming idiom: `Append` digits or characters into the running state; call `GetCurrentCheckDigit()` (or `GetCurrentCheckDigits()`) to read the result non-destructively; call `Reset()` to restart.

---

## Decimal check-digit algorithms — `Bodu.IO.Hashing.CheckDigits`

### Luhn — `Luhn`

The **Luhn algorithm** (ISO/IEC 7812, also called *modulus 10* or *mod 10*) was designed by Hans Peter Luhn at IBM in 1954. It is the check-digit scheme used by virtually every payment card number (Visa, Mastercard, Amex, Discover), IMEI numbers, and many national identification numbers.

**Error detection:** catches all single-digit substitution errors. Catches all adjacent transpositions *except* the `09 ↔ 90` swap.

```csharp
using Bodu.IO.Hashing.CheckDigits;

// Streaming — append the body digits and read the check digit.
var luhn = new Luhn();
luhn.Append("799273987");
char check = luhn.GetCurrentCheckDigit();   // '3'  →  full number "7992739871 3"

// One-shot static helpers.
char computed = Luhn.Compute("7992739871");          // '3'
bool valid    = Luhn.IsValid("79927398713");         // true
bool invalid  = Luhn.IsValid("79927398710");         // false
```

---

### Damm — `Damm`

The **Damm algorithm** uses a quasigroup operation table designed by H. Michael Damm (2004). It detects **all** single-digit substitution errors and **all** adjacent transposition errors — including the `09 ↔ 90` swap that Luhn misses. It also detects many twin errors.

```csharp
using Bodu.IO.Hashing.CheckDigits;

var damm = new Damm();
damm.Append("572");
char check = damm.GetCurrentCheckDigit();   // '4'  →  "5724" is valid

bool valid = Damm.IsValid("5724");          // true
```

---

### Verhoeff — `Verhoeff`

The **Verhoeff algorithm** uses the dihedral group D₅ and a permutation table to detect all single-digit substitution errors, all adjacent transpositions, and all twin errors. It was designed by Jacobus Verhoeff (1969) and is used by the German ID card system and various medical device identifiers.

```csharp
using Bodu.IO.Hashing.CheckDigits;

var verhoeff = new Verhoeff();
verhoeff.Append("236");
char check = verhoeff.GetCurrentCheckDigit();   // '3'  →  "2363" is valid

bool valid = Verhoeff.IsValid("2363");          // true
```

---

### EAN barcodes — `Ean8` / `Ean13`

EAN-8 and EAN-13 use the GS1 weighted-mod-10 algorithm. They are the standard barcodes on retail products worldwide.

```csharp
using Bodu.IO.Hashing.CheckDigits;

// EAN-13: 12-digit body → 1 check digit.
var ean13 = new Ean13();
ean13.Append("590123412345");
char check = ean13.GetCurrentCheckDigit();   // '7'  →  "5901234123457"

bool valid = Ean13.IsValid("5901234123457");   // true

// EAN-8: 7-digit body → 1 check digit.
char check8 = Ean8.Compute("1234567");   // '0'
```

---

### GTIN-14 — `Gtin14`

GTIN-14 extends EAN-13 with a packaging-level indicator digit, using the same GS1 weighted-mod-10 algorithm. It is the standard for shipping cartons and pallet-level barcodes.

```csharp
using Bodu.IO.Hashing.CheckDigits;

char check = Gtin14.Compute("1234567890123");   // '1'
bool valid  = Gtin14.IsValid("12345678901231");  // true
```

---

### UPC-A — `UpcA`

UPC-A is the standard 12-digit barcode used in the United States and Canada. It is structurally identical to EAN-13 with a leading zero, using the same GS1 weighted-mod-10 algorithm.

```csharp
using Bodu.IO.Hashing.CheckDigits;

char check = UpcA.Compute("03600024145");   // '7'
bool valid  = UpcA.IsValid("036000241457");  // true
```

---

### ISIN — `Isin`

An **ISIN** (International Securities Identification Number, ISO 6166) is a 12-character alphanumeric code identifying a financial security. The check digit is computed by expanding each letter to two digits (`A`=10, `B`=11, …, `Z`=35), concatenating the result with the numeric body, then applying the Luhn algorithm.

```csharp
using Bodu.IO.Hashing.CheckDigits;

// 11-character body (2-letter country code + 9-character NSIN).
char check = Isin.Compute("US037833100");   // '5'  →  "US0378331005"

bool valid = Isin.IsValid("US0378331005");   // true  (Apple Inc.)
```

---

### ABA routing number — `AbaRoutingNumber`

US bank routing numbers use a weighted-mod-10 scheme with weights `[3, 7, 1]` repeating. The ABA (American Bankers Association) routing transit number is always 9 digits.

```csharp
using Bodu.IO.Hashing.CheckDigits;

bool valid = AbaRoutingNumber.IsValid("021000021");   // true (JPMorgan Chase, NY)
```

---

## Alphanumeric check-digit algorithms — `Bodu.IO.Hashing.Checksums`

### IBAN — `Iban`

An **IBAN** (International Bank Account Number, ISO 13616) begins with a two-letter country code followed by two check digits and the country-specific BBAN. The check uses ISO 7064 MOD 97–10 over the rearranged and letter-expanded string.

```csharp
using Bodu.IO.Hashing.Checksums;

// Body = country code + BBAN (without the two check digits).
var iban = new Iban();
iban.Append("GB");       // country code
iban.Append("BARC20201530093459");   // BBAN
Span<char> checkBuf = stackalloc char[2];
iban.GetCurrentCheckDigits(checkBuf);   // "29"  →  "GB29 BARC 2020 1530 0934 59"

bool valid = Iban.IsValid("GB29BARC20201530093459");   // true
```

---

### ISBN — `Isbn10` / `Isbn13`

`Isbn10` uses weighted mod-11 (the check digit may be `'X'` representing 10). `Isbn13` uses GS1 weighted mod-10 and is identical to EAN-13. Both share the same streaming API.

```csharp
using Bodu.IO.Hashing.Checksums;

char check10 = Isbn10.Compute("030640615");   // '2'  →  "0306406152"
char check13 = Isbn13.Compute("978030640615");   // '2'

bool valid10 = Isbn10.IsValid("0306406152");
bool valid13 = Isbn13.IsValid("9780306406157");
```

---

### SEDOL — `Sedol`

SEDOL (Stock Exchange Daily Official List) is a 7-character identifier used by the London Stock Exchange. The 6-character body uses digits and uppercase consonants (vowels are excluded); the check digit is the result of a weighted mod-10 computation.

```csharp
using Bodu.IO.Hashing.Checksums;

char check = Sedol.Compute("710889");   // '2'  →  "7108892"
bool valid  = Sedol.IsValid("7108892");   // true
```

---

### CUSIP — `Cusip`

CUSIP (Committee on Uniform Securities Identification Procedures, ANSI X9.6) identifies North American financial securities with a 9-character identifier. The check is computed from the 8-character body using a modified Luhn algorithm that handles alphanumeric characters.

```csharp
using Bodu.IO.Hashing.Checksums;

char check = Cusip.Compute("037833100");   // '5'  →  "0378331005"  (Apple)
bool valid  = Cusip.IsValid("0378331005");
```

---

### LEI — `Lei`

An **LEI** (Legal Entity Identifier, ISO 17442) is a 20-character alphanumeric code that uniquely identifies legal entities (companies, funds, etc.) globally. The check uses ISO 7064 MOD 97–10 over the letter-expanded string.

```csharp
using Bodu.IO.Hashing.Checksums;

bool valid = Lei.IsValid("5493000IBP32UQZ0KL24");   // true
```

---

## Choosing the right algorithm

| Identifier | Use |
|---|---|
| Credit card, IMEI, SIN | `Luhn` |
| Any general decimal identifier where `09 ↔ 90` matters | `Damm` |
| German ID, medical devices | `Verhoeff` |
| Retail barcode (13-digit EAN) | `Ean13` |
| Retail barcode (8-digit EAN) | `Ean8` |
| Retail barcode (12-digit US/CA) | `UpcA` |
| Shipping carton barcode (GTIN-14) | `Gtin14` |
| International securities (ISIN) | `Isin` |
| North American securities (CUSIP) | `Cusip` |
| London Stock Exchange securities (SEDOL) | `Sedol` |
| Bank account number (IBAN) | `Iban` |
| Book identifier | `Isbn13` (new) · `Isbn10` (legacy) |
| US bank routing number | `AbaRoutingNumber` |
| Legal entity identifier (LEI) | `Lei` |

## Where to go next

- [Algorithm families](../../docs/algorithm-families.md) — how check digits relate to checksums, fingerprints, and cryptographic primitives.
- [Bodu.IO.Hashing overview](index.md) — the broader non-cryptographic hashing landscape.
- [Bodu.IO.Hashing API reference](../../apidoc/Bodu.IO.Hashing.md) — full type documentation.
