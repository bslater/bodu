# Bodu.IO.Hashing

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

Non-cryptographic hashing, checksums, and check-digit algorithms for .NET 8. Hash algorithms derive from `System.IO.Hashing.NonCryptographicHashAlgorithm`, so they expose the standard `Append` / `GetCurrentHash` / `GetHashAndReset` streaming surface and slot into existing pipelines. This package covers data-integrity and identifier-validation use cases — **not** security. For keyed/cryptographic hashing use the sibling `Bodu.Security.Cryptography` package or the platform `System.Security.Cryptography` types.

## Installation

```shell
dotnet add package Bodu.IO.Hashing
```

Targets `net8.0`.

## Quick start

```csharp
using Bodu.IO.Hashing.Checksums;
using Bodu.IO.Hashing.CheckDigits;

// CRC: one parametric engine, 112 catalogued standards.
var crc = new Crc(CrcStandard.CRC32_ISOHDLC);           // the ubiquitous zip/png CRC-32
byte[] digest = crc.ComputeHash("123456789"u8);          // one-shot over bytes

// Or stream it: the standard NonCryptographicHashAlgorithm surface.
crc.Append("1234"u8);
crc.Append("56789"u8);
byte[] same = crc.GetHashAndReset();                     // equals the one-shot digest

// Check digits: static Compute / IsValid per identifier scheme.
bool ok = Iban.IsValid("GB82WEST12345698765432");        // true (electronic format, no spaces)
char check = Luhn.Compute("7992739871");                 // '3' -> card number 79927398713
bool valid = Isbn13.IsValid("9780306406157");            // true
```

`Crc` implements `IResumableHashAlgorithm`, so a stored digest can be extended with more data
without replaying the original input. See the [guides](../docs/guides/io-hashing/index.md) for
the full catalogue, streaming, and verification surfaces.

## Checksums and non-cryptographic hashes

| Family | Algorithms | Output (bits) | Notes |
|---|---|---|---|
| Fletcher | `Fletcher16`, `Fletcher32`, `Fletcher64` | 16 / 32 / 64 | Position-sensitive checksum |
| Adler | `Adler32`, `Adler32C`, `Adler64` | 32 / 64 | RFC 1950 family |
| FNV | `Fnv132`, `Fnv164`, `Fnv1a32`, `Fnv1a64` | 32 / 64 | Fowler–Noll–Vo |
| CityHash | `CityHash32`, `CityHash64`, `CityHash128` | 32 / 64 / 128 | Google CityHash |
| MurmurHash3 | `MurmurHash3_32`, `MurmurHash3_128` | 32 / 128 | |
| Pearson | `Pearson` (selectable `PearsonTableType`) | 8 | |
| String hashes | `ApHash`, `Bernstein`, `BKDR`, `Elf64`, `JSHash`, `Pjw32`, `SDBM`, `SuperFastHash` | varies | Classic table / multiplicative hashes |

## CRC

A single parametric `Crc` engine drives the full RevEng catalogue — **112 standards** spanning widths from CRC-3 to CRC-64 (CRC-3, -4, -5, -6, -7, -8, -10…-17, -21, -24, -30, -31, -32, -40, -64). `CrcStandard` is the immutable parameter bundle (polynomial, init, reflect-in/out, final XOR); the catalogue is exposed through the `CrcStandards` enum, and `CrcLookupTableCache` shares lookup tables across instances with matching `(width, polynomial, reflectIn)`. `Crc` implements `IResumableHashAlgorithm`, so a digest can be extended from a prior hash without replaying the original input.

## Check digits

Streaming check-digit algorithms (`Append(ReadOnlySpan<char>)` → `GetCurrentCheckDigit()`) for identifier validation and generation:

| Domain | Algorithms |
|---|---|
| General | `Luhn`, `Damm`, `Verhoeff`, `Gumm`, `Iso7064Mod11_2`, `Iso7064Mod97_10` |
| Banking | `AbaRoutingNumber`, `Iban` |
| Retail / barcode | `Ean8`, `Ean13`, `Gtin14`, `UpcA`, `Code39Mod43` |
| Securities | `Isin`, `Cusip`, `Sedol`, `Lei` |
| Publishing | `Isbn10`, `Isbn13` |
| Encoding | `Crockford32` |

Decimal algorithms derive from `CheckDigitAlgorithm`; alphanumeric and multi-character schemes derive from `AlphanumericCheckDigitAlgorithm` / `MultiCharCheckDigitAlgorithm`, with `CheckDigitInputAlphabet` / `CheckDigitOutputAlphabet` selecting the permitted character sets.

## Streaming and one-shot APIs

The extension surface on `NonCryptographicHashAlgorithm` adds `AppendData(Stream)`, one-shot `ComputeHash(...)` / `ComputeHashAsync(Stream)`, and constant-time `VerifyHash` / `TryVerifyHash` (sync and async) over the standard incremental `Append` / `GetCurrentHash` / `Reset` methods.

## Runnable samples

The repository ships offline, `dotnet run`-able sample projects for this package — the CRC
catalogue and checksum families, streaming and resumable digests, identifier check digits
across domains, and a custom check-digit scheme proven by the shared contract-test base —
under [`samples/IO.Hashing/`](https://github.com/bslater/bodu/tree/master/samples/IO.Hashing).

## Testing

Tests live in `test/` as MSTest partial classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.IO.Hashing/test/Bodu.IO.Hashing.Test.csproj --settings smoke.runsettings
dotnet test Bodu.IO.Hashing/test/Bodu.IO.Hashing.Test.csproj --settings bvt.runsettings
dotnet test Bodu.IO.Hashing/test/Bodu.IO.Hashing.Test.csproj --settings regression.runsettings
```

Algorithms are validated against published known-answer vectors through the shared `NonCryptographicHashAlgorithmContractTests<TAlgorithm>`, `CheckDigitContractTests<TAlgorithm>`, and `MultiCharCheckDigitContractTests<TAlgorithm>` bases, with the full CRC catalogue exercised in the Regression tier.

## License

MIT. © Bodu Pty. Ltd.
