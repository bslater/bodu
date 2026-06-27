---
uid: Bodu.IO.Hashing.CheckDigits
---

![Bodu.IO.Hashing](~/images/hero-io.svg)

## Purpose

**Bodu.IO.Hashing.CheckDigits** ships the catalogue of single-character and multi-character check-digit algorithms — Luhn, Damm, Verhoeff, plus the standard catalogues used by financial, retail, securities, and publishing identifiers (IBAN, LEI, EAN, GTIN, UPC, ISBN, ISIN, SEDOL, CUSIP, ABA routing, …). Every algorithm derives from <xref:Bodu.IO.Hashing.CheckDigits.CheckDigitAlgorithm>, <xref:Bodu.IO.Hashing.CheckDigits.MultiCharCheckDigitAlgorithm>, or <xref:Bodu.IO.Hashing.CheckDigits.AlphanumericCheckDigitAlgorithm>.

## Key types

**Generic algorithms** — applicable to any identifier shape:

- <xref:Bodu.IO.Hashing.CheckDigits.Luhn>, <xref:Bodu.IO.Hashing.CheckDigits.Damm>, <xref:Bodu.IO.Hashing.CheckDigits.Verhoeff>, <xref:Bodu.IO.Hashing.CheckDigits.Gumm>

**Financial / banking identifiers:**

- <xref:Bodu.IO.Hashing.CheckDigits.AbaRoutingNumber> — ABA routing number (US).
- <xref:Bodu.IO.Hashing.CheckDigits.Iban> — IBAN MOD-97.
- <xref:Bodu.IO.Hashing.CheckDigits.Lei> — Legal Entity Identifier (ISO 17442).

**Retail / GS1:**

- <xref:Bodu.IO.Hashing.CheckDigits.Ean8>, <xref:Bodu.IO.Hashing.CheckDigits.Ean13>, <xref:Bodu.IO.Hashing.CheckDigits.Gtin14>, <xref:Bodu.IO.Hashing.CheckDigits.UpcA>

**Securities:**

- <xref:Bodu.IO.Hashing.CheckDigits.Cusip>, <xref:Bodu.IO.Hashing.CheckDigits.Isin>, <xref:Bodu.IO.Hashing.CheckDigits.Sedol>

**Publishing:**

- <xref:Bodu.IO.Hashing.CheckDigits.Isbn10>, <xref:Bodu.IO.Hashing.CheckDigits.Isbn13>

**Encoded identifiers:**

- <xref:Bodu.IO.Hashing.CheckDigits.Code39Mod43>, <xref:Bodu.IO.Hashing.CheckDigits.Crockford32>

**ISO 7064:**

- <xref:Bodu.IO.Hashing.CheckDigits.Iso7064Mod11_2>, <xref:Bodu.IO.Hashing.CheckDigits.Iso7064Mod97_10>

## Example

```csharp
using Bodu.IO.Hashing.CheckDigits;

// Validate or compute a Luhn check digit.
bool ok    = Luhn.Instance.IsValid("79927398713");          // True
char digit = Luhn.Instance.ComputeCheckDigit("7992739871"); // '3'

// IBAN validation.
bool ibanOk = Iban.Instance.IsValid("DE89370400440532013000");
```

## Notes

- **Stateless / static instances.** Every algorithm exposes an `Instance` singleton; the instances are stateless and safe to share across threads.
- **Shaped vs. generic.** Identifier-shaped algorithms (`Iban`, `Cusip`, `Sedol`) validate length, character set, and any embedded structure as well as the check digit; the generic algorithms (`Luhn`, `Damm`, `Verhoeff`) validate only the digit. Pick the shaped algorithm when you have a specific identifier type.
- **See also:** the [Check-digits guide](~/guides/io-hashing/check-digits.md), the parent <xref:Bodu.IO.Hashing> landing page.
