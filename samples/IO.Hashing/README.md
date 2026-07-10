# IO.Hashing Samples

Console applications demonstrating the `Bodu.IO.Hashing` package. Each sample is a
standalone project; run one with:

```bash
dotnet run --project samples/IO.Hashing/<SampleName>
```

Every sample is offline and deterministic: fixed inputs plus one small committed text file.
The `CustomCheckDigit.Test` project runs with the library test suites in CI.

> **Not security.** Everything in this package detects *accidental* corruption and validates
> identifier formats — an adversary can forge all of it. Cryptographic integrity lives in
> `Bodu.Security.Cryptography`.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.IO.Hashing.Samples.ChecksumTour` | The parametric `Crc` engine over the 112-standard RevEng catalogue (parameters, check-input digests, `FromName`), CRC/Adler/Fletcher over one file with single-bit corruption detection, chunked `Append` == one-shot == `HashingStream`, the `IResumableHashAlgorithm` append-only-log pattern, and FNV-1a/Murmur3/CityHash bucket routing (labelled non-cryptographic) | `Bodu.IO.Hashing` |
| `Bodu.IO.Hashing.Samples.CheckDigits` | `IsValid` across identifier domains (IBAN, ISBN-10/13, EAN-13, Luhn, ABA) with typo rejection, `Compute` for issuing (including ISBN-10's `'X'`), the streaming `Append` surface, and the Luhn-vs-Damm-vs-Verhoeff transposition error-class comparison | `Bodu.IO.Hashing` |
| `Bodu.IO.Hashing.Samples.CustomCheckDigit` (+ `.Test`) | A weighted mod-10 SKU scheme implementing the four-member `CheckDigitAlgorithm` contract, driven beside built-ins via base-class polymorphism; the test project derives the shared `CheckDigitContractTests<SkuCheckDigit>` with KAT rows | `Bodu.IO.Hashing` |
