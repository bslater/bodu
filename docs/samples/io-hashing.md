---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for `Bodu.IO.Hashing` under
[`samples/IO.Hashing/`](https://github.com/bslater/bodu/tree/master/samples/IO.Hashing). All
three are **offline and deterministic** — fixed inputs plus one small committed text file —
and are members of `bodu.slnx`, built and executed by CI; the contract-test companion runs
with the test suites. Each README documents every scenario individually: its intent, what the
code does, the output to expect, and the APIs demonstrated.

Run any of them from the repository root:

```bash
dotnet run --project samples/IO.Hashing/<SampleName>
```

## The samples

### Bodu.IO.Hashing.Samples.ChecksumTour

The byte-integrity surface: the parametric <xref:Bodu.IO.Hashing.Checksums.Crc> engine over
its 112-standard RevEng catalogue (<xref:Bodu.IO.Hashing.Checksums.CrcStandard> parameter
bundles, `FromName` resolution, the little-endian digest convention); CRC / Adler / Fletcher
side by side over one committed file through the shared `NonCryptographicHashAlgorithm`
surface, with single-bit corruption detection; the incremental surfaces — chunked `Append`,
<xref:Bodu.IO.Hashing.HashingStream> checksumming as a side effect of stream I/O, and
<xref:Bodu.IO.Hashing.IResumableHashAlgorithm> extending a stored digest without replaying
the original input (the append-only-log pattern); and FNV-1a / MurmurHash3 / CityHash doing
deterministic bucket routing, clearly labelled *not cryptographic*. *Package:
`Bodu.IO.Hashing`.*

### Bodu.IO.Hashing.Samples.CheckDigits

The identifier-validation surface: `IsValid` across domains (IBAN, ISBN-10/13, EAN-13, Luhn
card numbers, ABA routing) with one-character typos rejected; `Compute` for issuing —
including ISBN-10's `'X'` check digit and the streaming `Append`/`GetCurrentCheckDigit`
shape; and the error-class comparison that explains why multiple schemes exist — Luhn
provably misses the `09 ↔ 90` adjacent transposition that
<xref:Bodu.IO.Hashing.CheckDigits.Damm> and <xref:Bodu.IO.Hashing.CheckDigits.Verhoeff>
always catch. *Package: `Bodu.IO.Hashing`.*

### Bodu.IO.Hashing.Samples.CustomCheckDigit (+ .Test)

Extending the catalogue: a weighted mod-10 SKU scheme (the repeating `7, 3, 1` cycle)
implementing the four-member <xref:Bodu.IO.Hashing.CheckDigits.CheckDigitAlgorithm>
contract, exercised through its own surface and beside `Luhn`/`Damm` via base-class
polymorphism. The companion test project derives the library's
`CheckDigitContractTests<SkuCheckDigit>` with six known-answer rows, inheriting the
compute/validate/corruption contract the built-in schemes pass. *Package:
`Bodu.IO.Hashing`.*

## Guarded documentation

The guides under [`docs/guides/io-hashing/`](../guides/io-hashing/index.md) carry
compile-guarded snippets: examples marked with a `<!-- compile -->` sentinel are compiled
against the current public API by `DocumentationSnippetCompileTests` in the library's test
project (Regression tier). Wiring the guard immediately caught — and fixed — 39 guide
declarations that wrapped non-disposable hash types in `using var`.

## Related

- [IO.Hashing guides](../guides/io-hashing/index.md) — per-family pages, the CRC catalogue,
  and the check-digit reference.
- [IO.Compound samples](io-compound.md) and [Excel samples](excel.md) — the sibling IO-group
  sample families.
