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

## CRC error-detection guarantees

A CRC of width *w* with a well-chosen generator polynomial gives concrete, provable guarantees over the error patterns a transmission or storage channel introduces. These are the properties that make CRC the checksum of choice for wire formats, and they are what a fingerprint or twin-accumulator checksum does *not* promise:

| Error pattern | Guarantee |
|---|---|
| Any single bit-flip | Always detected. |
| Any odd number of bit-flips | Always detected, *provided* the polynomial has `(x + 1)` as a factor (true of most catalogue entries). |
| Any burst error of length ≤ *w* | Always detected — the burst is shorter than the register, so it cannot divide cleanly. |
| A burst of length *w* + 1 | Detected with probability `1 − 2^−(w−1)`. |
| A longer burst | Detected with probability `1 − 2^−w`. |
| Two-bit errors anywhere in the message | Detected as long as the message is shorter than the polynomial's *period* (for a primitive polynomial, `2^w − 1` bits). |

The width is the dominant lever: a 32-bit CRC leaves only one undetected error pattern in roughly four billion for an unstructured corruption, a 16-bit CRC one in 65 536. The polynomial choice is the second lever — the catalogue entries pair each name with a polynomial whose published *Hamming distance* tables state, for a given message length, the smallest number of bit-flips that can slip through undetected. Match the standard to the channel rather than picking a width in the abstract.

> [!NOTE]
> These guarantees describe *accidental* corruption only. A CRC carries no adversary model — an attacker who can edit the payload can recompute the CRC, or craft a change that lands on a CRC collision, in constant time. CRC protects a channel against noise, never against tampering.

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

## Twin-accumulator error coverage

Both Fletcher and Adler catch every single-bit error and every adjacent transposition, and at far lower per-byte cost than a CRC. What they do **not** match is the CRC's burst guarantee or its uniform behaviour across the whole digest space — and both carry documented blind spots a CRC does not:

- **Fletcher's `2^k − 1` modulus** means the value `0` and the value `2^k − 1` (here, `M`) are congruent, so a byte that should push an accumulator to `M` instead leaves it at `0`. A run of bytes that sums to a multiple of `M` can therefore go undetected, and a block of all-`0x00` bytes followed by an all-`0xFF` block of the right length can collide.
- **Adler's weakness on short inputs.** Each byte first lands in accumulator `A`; `B` only accumulates the *running* `A`. For a short message `A` stays small, so `B` grows slowly and the high bits of the digest barely move — the effective digest space is much narrower than 32 bits until the message is a few hundred bytes long. This is the well-known reason Adler-32 is a poor checksum for very short payloads, and why CRC-32 is preferred there.
- **Neither catches a reordering of equal-sum blocks** the way a polynomial CRC does, and neither gives a per-position burst guarantee.

The trade is deliberate: reach for Fletcher or Adler when you control both endpoints, the channel is benign, and throughput matters; reach for CRC when you need the published burst-error guarantee or must match a wire format.

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

## Transcription error classes

A check digit is judged by which of the documented human keying errors it catches. The four that matter, in roughly descending order of frequency, are:

| Error class | Example | Description |
|---|---|---|
| **Single-digit substitution** | `1234` → `1284` | One character mistyped. The most common error and the floor every scheme must clear. |
| **Adjacent transposition** | `1234` → `1324` | Two neighbouring characters swapped. |
| **Twin error** | `1**2**2` → `1**3**3` | A repeated digit changed to a different repeated digit. |
| **Jump transposition** | `1**2**3 → 3**2**1` | Two characters one position apart swapped, across an intervening character. |

How each Bodu scheme fares — grounded in the algorithm each one implements:

| Scheme | Single substitution | Adjacent transposition | Twin | Notes |
|---|---|---|---|---|
| <xref:Bodu.IO.Hashing.CheckDigits.Luhn> (mod 10) | All | All **except** `09 ↔ 90` | No | The `09 ↔ 90` swap preserves the Luhn sum and slips through. |
| <xref:Bodu.IO.Hashing.CheckDigits.Damm> (quasigroup) | All | **All**, including `09 ↔ 90` | Many | Closes the one gap Luhn leaves, with a single character. |
| <xref:Bodu.IO.Hashing.CheckDigits.Verhoeff> (dihedral D₅) | All | All | All | The widest coverage of any single decimal check digit. |
| Mod 11 — <xref:Bodu.IO.Hashing.CheckDigits.Isbn10>, <xref:Bodu.IO.Hashing.CheckDigits.Sedol>, <xref:Bodu.IO.Hashing.CheckDigits.Cusip> | All | Most | Some | A check value of 10 needs an extra symbol (ISBN-10 uses `X`). |
| Mod 97-10 — <xref:Bodu.IO.Hashing.CheckDigits.Iban>, <xref:Bodu.IO.Hashing.CheckDigits.Lei> | Effectively all | Effectively all | Effectively all | Two check digits over a large modulus catch essentially every realistic transcription error. |

Damm and Verhoeff buy strictly better coverage than Luhn from the *same* single character; the price is a small lookup table rather than a weighted sum. Reach for Luhn only when an existing standard mandates it (payment cards, IMEI). For a free choice of a general decimal identifier, Damm is the better default, and Verhoeff better still where the table cost is acceptable.

## Endianness

Two different bit-order questions arise in this library and they are easy to confuse:

- **CRC bit reflection** — controlled by the `ReflectIn` and `ReflectOut` parameters on a `CrcStandard`. `ReflectIn = true` means every input byte is bit-reversed before being fed into the CRC register; `ReflectOut = true` means the final register is bit-reversed before the XOR-out step. The two together capture whether a channel transmits its bytes least-significant-bit-first (Ethernet, MODBUS, USB) or most-significant-bit-first. They are part of the algorithm, not a presentation choice — `CRC-32/ISO-HDLC` and `CRC-32/BZIP2` differ exactly here.
- **Digest byte order on the wire** — the order in which the final hash word is serialized into the byte array returned by `GetCurrentHash`. This is **not** uniform across the library, so it is the detail most likely to trip up a cross-tool comparison:

| Family | Byte order of the digest | Matches |
|---|---|---|
| Fingerprints — FNV, CityHash, MurmurHash3, Pearson, the classic string hashes | **Little-endian** | `BitConverter.ToUInt32` / `ToUInt64` on a little-endian platform. |
| <xref:Bodu.IO.Hashing.Checksums.Crc> | **Little-endian** | the width-byte register, low byte first. |
| <xref:Bodu.IO.Hashing.Checksums.Fletcher16> / `Fletcher32` / `Fletcher64` | **Big-endian**, `B ‖ A` | the high accumulator `B` precedes `A`. |
| <xref:Bodu.IO.Hashing.Checksums.Adler32> / `Adler32C` / `Adler64` | **Big-endian**, `(B << k) | A` | RFC 1950 §2.2 — the zlib trailer is written big-endian directly. |

When you compare digests across platforms or against published test vectors, confirm both: that the algorithm parameters match (for CRC, the reflection bits) and that the byte order on the wire matches the source you are comparing to. The Adler and Fletcher big-endian layout is what makes `GetCurrentHash` drop straight into a zlib trailer without a byte swap.

## Where to go next

- **[Introduction](index.md)** — the high-level shape of the library.
- **[Getting started](getting-started.md)** — install + runnable minimal samples.
- **[Bodu.IO.Hashing guides](../../guides/io-hashing/index.md)** — per-algorithm walk-throughs.
- **For keyed and cryptographic hashes** — SipHash, Poly1305, Tiger, ASCON, Merkle trees — see [Bodu.Security.Cryptography](../cryptography/index.md).
- **[Hashing & Cryptography topic](../topics/hashing-and-cryptography.md)** — this package and its sibling Bodu.Security.Cryptography; the [topic concepts](../topics/hashing-and-cryptography-concepts.md) page collects the shared vocabulary.
