---
uid: Bodu.IO.Hashing.CheckDigits
---

![Bodu.IO.Hashing](~/images/hero-io.svg)

## Purpose

**Bodu.IO.Hashing.CheckDigits** ships the catalogue of single-character and multi-character check-digit algorithms — Luhn, Damm, Verhoeff, plus the standard catalogues used by financial, retail, securities, and publishing identifiers (IBAN, LEI, EAN, GTIN, UPC, ISBN, ISIN, SEDOL, CUSIP, ABA routing, …). Every algorithm derives from the root <xref:Bodu.IO.Hashing.CheckDigits.CheckValueAlgorithm> through one of its three abstract bases — <xref:Bodu.IO.Hashing.CheckDigits.CheckDigitAlgorithm> (decimal payload, single check digit), <xref:Bodu.IO.Hashing.CheckDigits.AlphanumericCheckDigitAlgorithm> (alphanumeric payload, single check character), or <xref:Bodu.IO.Hashing.CheckDigits.MultiCharCheckDigitAlgorithm> (fixed-length multi-character check).

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

// One-shot static helpers: compute the check digit for a body, or validate a full value.
char digit = Luhn.Compute("7992739871");                 // '3'
bool ok    = Luhn.IsValid("79927398713");                // true

// Multi-character schemes return a string check value.
string ibanCheck = Iban.Compute("DE00370400440532013000"); // "89"
bool   ibanOk    = Iban.IsValid("DE89370400440532013000");  // true

// Streaming: feed the body in chunks and read the check non-destructively.
var luhn = new Luhn();
luhn.Append("79927");
luhn.Append("39871");
char streamed = luhn.GetCurrentCheckDigit();            // '3'
luhn.Reset();
```

## Notes

- **Static one-shots, stateful instances.** Every algorithm exposes static `Compute` / `IsValid` helpers for the common one-shot case; an instance (`new Luhn()`) carries running state for the streaming `Append` / `GetCurrentCheckDigit` (or `GetCurrentCheckDigits` / `GetCurrentCheckValue`) / `Reset` idiom, so share instances across threads only with external synchronization.
- **Shaped vs. generic.** Identifier-shaped algorithms (`Iban`, `Cusip`, `Sedol`) validate length, character set, and any embedded structure as well as the check digit; the generic algorithms (`Luhn`, `Damm`, `Verhoeff`) validate only the digit. Pick the shaped algorithm when you have a specific identifier type.
- **See also:** the [Check-digits guide](~/guides/io-hashing/check-digits.md), the parent <xref:Bodu.IO.Hashing> landing page.
