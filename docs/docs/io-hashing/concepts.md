---
title: Bodu.IO.Hashing — Core concepts
---

# Bodu.IO.Hashing — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/io-hashing/index.md), and refer back whenever a term feels imprecise.

Part of the **[Hashing & Cryptography](../topics/hashing-and-cryptography.md)** topic.

For the high-level shape of the library and the family map, start with the [introduction](index.md).

## Adversary model

An **adversary model** is the formal statement of what an attacker is assumed to know, observe, and control. A cryptographic primitive carries an adversary model: even an attacker who knows the algorithm, sees many input/output pairs, and adaptively chooses inputs cannot forge, invert, or find collisions in feasible time.

**Nothing in `Bodu.IO.Hashing` has an adversary model.** Every algorithm here is unkeyed (or trivially keyed) and structurally simple — given the digest, anyone can compute another input that produces the same digest. That is fine for the jobs these algorithms are designed for and disqualifying for everything else.

| Safe for | Not safe for |
|---|---|
| Detecting accidental bit-flips in transit or on disk | Detecting tampering by an attacker |
| Hash-table keys inside a trust boundary | Hash-table keys exposed to attacker-chosen input (hash-flooding) |
| Cache bucketing and content-addressable lookup | Authentication, signatures, password hashing |
| Catching human transcription errors in identifiers | Identifier unforgeability |

When you need an adversary model — keyed hashes, MACs, cryptographic digests, AEAD — see [Bodu.Security.Cryptography](../cryptography/index.md). The line between the two libraries is exactly this distinction.

## Fingerprint vs. checksum vs. check digit

The library is partitioned into three subfamilies, each tuned for a different job. Picking the wrong subfamily produces correct-looking output that fails the requirement.

| Subfamily | Operates on | Optimized for | Typical output | Bodu namespace |
|---|---|---|---|---|
| **Fingerprint** | Arbitrary byte buffer | Even distribution and speed | 32–128 bits | `Bodu.IO.Hashing` |
| **Checksum** | Arbitrary byte buffer | Detecting specific error patterns | 16–64 bits | `Bodu.IO.Hashing.Checksums` |
| **Check digit** | Printed character sequence | Catching human transcription errors | 1–2 characters | `Bodu.IO.Hashing.CheckDigits` (+ multi-char in `Checksums`) |

A fingerprint such as <xref:Bodu.IO.Hashing.Fnv1a64> distributes evenly but offers no guarantees about which error patterns it catches. A checksum such as <xref:Bodu.IO.Hashing.Checksums.Crc> catches characterized error patterns but distributes poorly as a hash-table key. A check digit such as <xref:Bodu.IO.Hashing.CheckDigits.Luhn> guards a printed identifier and is meaningless on a binary buffer.

## NonCryptographicHashAlgorithm contract

Every binary hash in `Bodu.IO.Hashing` derives from <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>. The contract is four members:

| Member | Semantics |
|---|---|
| `Append(ReadOnlySpan<byte>)` | Push more input into the running state. Idempotent across calls — equivalent to concatenating the spans and appending once. |
| `GetCurrentHash()` | Take a **non-destructive snapshot** of the current digest. The internal state continues; you can `Append` more and snapshot again. |
| `TryGetCurrentHash(Span<byte>, out int)` | Same as `GetCurrentHash` but writes into a caller-supplied buffer. Returns `false` when the destination is too small. |
| `Reset()` | Return the instance to its constructed initial state, ready to compute a fresh digest. |

This streaming shape means one instance can hash arbitrarily large inputs in chunks without buffering the whole payload, and the snapshot semantics make rolling integrity tags trivial. The check-digit base classes (<xref:Bodu.IO.Hashing.CheckDigits.CheckDigitAlgorithm>, <xref:Bodu.IO.Hashing.CheckDigits.MultiCharCheckDigitAlgorithm>) mirror the same shape over characters rather than bytes — `Append` / `GetCurrentCheckDigit(s)` / `Reset`.

## Avalanche

**Avalanche** is the property that flipping a single bit of input flips approximately half the bits of the output, and that each output bit is statistically independent of every other. A hash with good avalanche distributes keys evenly across hash-table buckets and resists trivial collisions on near-identical inputs (`"foo1"` vs `"foo2"`).

Avalanche is what distinguishes a *fingerprint* (FNV-1a, MurmurHash3, CityHash, Pearson) from a *checksum* (CRC, Fletcher, Adler). Checksums are tuned for the error patterns of a channel — they can leave entire output regions sparsely populated for typical inputs. That is fine when you only want to detect a bit-flip; it is disastrous when you want a hash-table key.

## Polynomial

In a CRC, the **polynomial** is the divisor of a long polynomial division performed in `GF(2)` — the field of two elements, where addition and subtraction are both XOR. The input bytes form one big polynomial (each bit is a coefficient); dividing by the generator polynomial leaves a remainder, and that remainder is the CRC.

Polynomials are written as their coefficient bit pattern. The CRC-32/ISO-HDLC polynomial `0x04C11DB7` encodes the polynomial `x^32 + x^26 + x^23 + x^22 + …` — bit 26 set, bit 23 set, and so on. The choice of polynomial dictates which error patterns the CRC is guaranteed to detect; a well-chosen 32-bit polynomial catches every burst error of length 32 or fewer and every odd number of bit-flips.

## CRC parameters

The CRC family is one engine with five free parameters. The RevEng catalogue (Greg Cook's *Catalogue of parametrised CRC algorithms*) names them as follows; <xref:Bodu.IO.Hashing.Checksums.CrcStandard> exposes them with the same semantics:

| Parameter | Bodu property | Meaning |
|---|---|---|
| **width** | `Size` | Output bit width (1–64). Determines the register size and the final mask. |
| **poly** | `Polynomial` | The generator polynomial, encoded as its coefficient bit pattern. |
| **init** | `InitialValue` | The value the working register starts at before any input is consumed. |
| **refin** | `ReflectIn` | When `true`, each input byte is bit-reversed before being fed into the register. |
| **refout** | `ReflectOut` | When `true`, the final register is bit-reversed before the XOR-out step. |
| **xorout** | `XOrOut` | A constant XOR-ed into the (possibly reflected) final register. |

Two variants that share `poly` and disagree on `refin`/`refout` produce completely different digests for the same input — `CRC-32/ISO-HDLC` and `CRC-32/BZIP2` are exactly that pair.

## CRC catalogue

The **CRC catalogue** is the set of ~113 named, parameter-fixed CRC variants Bodu ships out of the box, sourced from the RevEng project. Each entry pairs a name (`CRC32_ISOHDLC`, `CRC16_MODBUS`, `CRC64_XZ`, …) with a `CrcStandard` value bundling the five parameters above plus the width.

Three ways to obtain a `CrcStandard`:

- **Catalogue properties** — `CrcStandard.CRC32_ISOHDLC`, `CrcStandard.CRC16_MODBUS`, … for the well-known entries.
- **`CrcStandard.Get(CrcStandards)`** — materialize any catalogue entry from the <xref:Bodu.IO.Hashing.Checksums.CrcStandards> enum, useful when the choice is data-driven.
- **`CrcStandard.FromName(string)`** — look up by canonical name string.

Custom variants (not in the catalogue) are constructed by passing the six parameters to `new CrcStandard(name, size, polynomial, initialValue, reflectIn, reflectOut, xorOut)`.

## Fletcher twin-sum

Fletcher's checksum maintains **two running accumulators**, A and B. A is the modular sum of all input bytes; B is the modular sum of *A after each byte*. The final digest concatenates the two.

The cross-position coupling — B grows faster from a byte near the start than from the same byte near the end — is what makes Fletcher catch adjacent transpositions that a plain sum or XOR misses. <xref:Bodu.IO.Hashing.Checksums.Fletcher16>, <xref:Bodu.IO.Hashing.Checksums.Fletcher32>, and <xref:Bodu.IO.Hashing.Checksums.Fletcher64> differ only in the word size and the modulus. Adler-32 follows the same twin-accumulator shape with one critical change — see *Modular checksum* below.

## Modular checksum

Both Fletcher and Adler reduce their accumulators **modulo** some value after each byte to keep them in a fixed word size. The choice of modulus matters:

| Algorithm | Modulus | Why |
|---|---|---|
| Fletcher-16 | 255 (`2^8 - 1`) | Largest 8-bit value; fast reduction. |
| Fletcher-32 | 65535 (`2^16 - 1`) | Largest 16-bit value; fast reduction. |
| <xref:Bodu.IO.Hashing.Checksums.Adler32> | **65521** (largest prime ≤ `2^16`) | Prime modulus catches more error patterns than the next-larger composite. |
| <xref:Bodu.IO.Hashing.Checksums.Adler32C> | `2^16` | The "C" variant trades error coverage for a power-of-two modulus that branchlessly compiles to a bitmask. |
| <xref:Bodu.IO.Hashing.Checksums.Adler64> | Largest prime ≤ `2^32` | Wider variant of the same idea. |

Adler-32 is the canonical zlib trailer checksum.

## Table-driven evaluation

A naive CRC implementation processes one bit at a time. A **table-driven** implementation pre-computes a 256-entry lookup table for the CRC of every possible byte value at a given register position, then advances the register one byte at a time with a table lookup and an XOR. The cost is a small amount of memory (256 × *width-in-bytes* per parameter tuple); the gain is roughly an order of magnitude on throughput.

Bodu's `Crc` engine builds the table lazily per *(width, polynomial, reflectIn)* tuple and caches it in <xref:Bodu.IO.Hashing.Checksums.CrcLookupTableCache>. Multiple `Crc` instances that share parameters share the same table — building a hundred `Crc(CrcStandard.CRC32_ISOHDLC)` objects allocates one table, not one hundred.

## Check digit

A **check digit** is one or two characters appended to a printed identifier so a later reader can confirm it was not mis-typed. It is a *transcription guard*, not an integrity check and not an authenticator:

- It catches the error classes the algorithm was designed for — single-digit substitutions, adjacent transpositions, sometimes twin errors and jump transpositions.
- It does **not** catch deliberate forgery: anyone who knows the algorithm can compute a valid check digit for any payload they choose.
- It is meaningless when applied to a binary buffer; check digits are defined against a printed-character payload.

The classic example is the Luhn check digit on the last digit of a credit-card number — if you transpose two adjacent digits, the Luhn sum changes and the card number is rejected before the network call.

## Single-character vs. multi-character check digit

Most check-digit schemes emit **one character** — a decimal digit drawn from `0`–`9`. Luhn, Damm, Verhoeff, EAN-8, EAN-13, GTIN-14, UPC-A, the ABA routing-number check, and the ISIN check all fit this shape. They live in `Bodu.IO.Hashing.CheckDigits` and share the <xref:Bodu.IO.Hashing.CheckDigits.CheckDigitAlgorithm> abstract base.

A few high-stakes identifiers use **multi-character** checks — two or more characters, often drawn from a larger alphabet:

| Identifier | Check length | Alphabet | Algorithm |
|---|---|---|---|
| IBAN | 2 digits | Alphanumeric (uppercase) payload, decimal check | <xref:Bodu.IO.Hashing.CheckDigits.Iban> (ISO 7064 Mod-97-10) |
| LEI | 2 digits | Alphanumeric (uppercase) payload, decimal check | <xref:Bodu.IO.Hashing.CheckDigits.Lei> (ISO 7064 Mod-97-10) |
| ISBN-13 | 1 digit | Decimal | <xref:Bodu.IO.Hashing.CheckDigits.Isbn13> |
| ISBN-10 | 1 character (`0`–`9` or `X`) | Decimal payload, `X` permitted as check | <xref:Bodu.IO.Hashing.CheckDigits.Isbn10> |
| CUSIP / SEDOL | 1 digit | Alphanumeric | <xref:Bodu.IO.Hashing.CheckDigits.Cusip>, <xref:Bodu.IO.Hashing.CheckDigits.Sedol> |

Multi-character check algorithms live in `Bodu.IO.Hashing.Checksums` alongside the generic ISO 7064 building blocks (<xref:Bodu.IO.Hashing.CheckDigits.Iso7064Mod11_2>, <xref:Bodu.IO.Hashing.CheckDigits.Iso7064Mod97_10>) and share the <xref:Bodu.IO.Hashing.CheckDigits.MultiCharCheckDigitAlgorithm> abstract base.

## Validate vs. compute

Every check-digit type exposes two surfaces that consumers reach for in different contexts:

```csharp
// Single-character schemes — Bodu.IO.Hashing.CheckDigits
char  digit = Luhn.Compute(payload);                  // payload excludes the check
bool  ok    = Luhn.IsValid(payloadWithCheck);         // payload includes the check

// Multi-character schemes — Bodu.IO.Hashing.Checksums
string check = Iban.Compute(body);                    // body excludes the two-digit check
bool   ok    = Iban.IsValid(ibanWithCheck);           // full IBAN including the check
```

- **Compute** runs the algorithm over a payload that does *not* yet contain the check, and emits the character(s) to append. Use it when you are minting a new identifier.
- **Validate (`IsValid`)** runs the algorithm over a payload that *does* contain the check, and returns whether the check is consistent. Use it on user input.

The streaming instance API (`Append` plus `GetCurrentCheckDigit` / `GetCurrentCheckDigits`) is available on every derivative of the base classes when you need to feed the payload in chunks.

## Endianness

Two different bit-order questions arise in this library and they are easy to confuse:

- **CRC bit reflection** — controlled by the `ReflectIn` and `ReflectOut` parameters on a `CrcStandard`. `ReflectIn = true` means every input byte is bit-reversed before being fed into the CRC register; `ReflectOut = true` means the final register is bit-reversed before the XOR-out step. The two together capture whether a channel transmits its bytes least-significant-bit-first (Ethernet, MODBUS, USB) or most-significant-bit-first. They are part of the algorithm, not a presentation choice — `CRC-32/ISO-HDLC` and `CRC-32/BZIP2` differ exactly here.
- **Digest byte order on the wire** — the order in which the final hash word is serialized into the byte array returned by `GetCurrentHash`. Bodu's fingerprints (FNV, CityHash, MurmurHash3, …) emit their hash word in **little-endian** byte order, matching `BitConverter.ToUInt32` / `ToUInt64` on little-endian platforms. `Crc.GetCurrentHash` emits the width-byte CRC in the same little-endian order.

When you compare digests across platforms or against published test vectors, confirm both: that the algorithm parameters match (for CRC, the reflection bits) and that the byte order on the wire matches the source you are comparing to.

## Where to go next

- **[Introduction](index.md)** — the high-level shape of the library.
- **[Getting started](getting-started.md)** — install + runnable minimal samples.
- **[Bodu.IO.Hashing guides](../../guides/io-hashing/index.md)** — per-algorithm walk-throughs.
- **For keyed and cryptographic hashes** — SipHash, Poly1305, Tiger, ASCON, Merkle trees — see [Bodu.Security.Cryptography](../cryptography/index.md).
- **[Hashing & Cryptography topic](../topics/hashing-and-cryptography.md)** — this package and its sibling Bodu.Security.Cryptography; the [topic concepts](../topics/hashing-and-cryptography-concepts.md) page collects the shared vocabulary.
