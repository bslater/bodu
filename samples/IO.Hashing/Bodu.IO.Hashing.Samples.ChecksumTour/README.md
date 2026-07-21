# Bodu.IO.Hashing.Samples.ChecksumTour

The `Bodu.IO.Hashing` integrity surface: the parametric CRC engine over its 112-standard
RevEng catalogue, the checksum families side by side, the streaming and resumable APIs, and
the classic non-cryptographic hash functions in their natural bucket-assignment role. All
scenarios run offline against fixed inputs and the committed `Data/pangrams.txt` (199 bytes).

> **Not security.** Everything in this package detects *accidental* corruption and
> distributes keys — an adversary can forge all of it. For tamper-proof integrity, signatures,
> or passwords, use `Bodu.Security.Cryptography`.

```bash
dotnet run --project samples/IO.Hashing/Bodu.IO.Hashing.Samples.ChecksumTour
```

## Scenario 1 — CrcCatalogue

**Intent.** Show the package's core design decision: there is one `Crc` engine, and every CRC
in the RevEng catalogue — CRC-3 through CRC-64 — is just an immutable `CrcStandard` parameter
bundle (width, polynomial, init, reflection, final XOR). "Which CRC does this protocol use?"
is answered by picking a catalogue entry, never by writing another implementation.

**What it does.** Reports the catalogue size (112 standards), then runs five well-known
standards (SMBus CRC-8, Modbus and XMODEM CRC-16, the zip/png CRC-32, and the xz CRC-64) over
`"123456789"` — the input every RevEng entry publishes its check value for — printing each
standard's parameters and digest. It notes that digest bytes follow the `System.IO.Hashing`
little-endian convention (the published CRC-32 check `0xCBF43926` appears as bytes
`26 39 F4 CB`), and resolves a standard from its catalogue name with `CrcStandard.FromName`.

**What to expect.**

```text
catalogue: 112 standards, CRC-3 to CRC-64
  CRC-8/SMBUS        width  8, poly 0x7, reflect --/--- -> F4
  CRC-16/MODBUS      width 16, poly 0x8005, reflect in/out -> 374B
  CRC-32/ISO-HDLC    width 32, poly 0x4C11DB7, reflect in/out -> 2639F4CB
  CRC-64/XZ          width 64, poly 0x42F0E1EBA9EA3693, reflect in/out -> FA3919DFBBC95D99
(digest bytes are little-endian: 26 39 F4 CB above == the published check 0xCBF43926)
FromName("CRC-32/ISO-HDLC") == CRC32_ISOHDLC -> True
```

**APIs demonstrated.** `Crc(CrcStandard)`, `Crc.ComputeHash`, the `CrcStandard` static
catalogue properties and `.FromName`, `CrcStandard.Name/Size/Polynomial/ReflectIn/ReflectOut`,
the `CrcStandards` enum.

## Scenario 2 — ChecksumFamilies

**Intent.** Put the three checksum families over the same input so their shared surface is
visible: CRC, Adler (RFC 1950), and Fletcher all derive from the BCL's
`NonCryptographicHashAlgorithm`, so one integrity pipeline can swap families with one line —
and show the property checksums exist for: a single flipped bit changes the digest.

**What it does.** Checksums the committed `Data/pangrams.txt` with CRC-32/ISO-HDLC, Adler-32,
Fletcher-32, and Fletcher-64 through the identical `Append`/`GetHashAndReset` calls, then
flips one bit of the input and shows the CRC digest change.

**What to expect.**

```text
input: 199 bytes
  CRC-32/ISO-HDLC: 2CE47BF8
  Adler-32       : E82B4775
  Fletcher-32    : E5EA4774
  Fletcher-64    : 001BE5CF00004774
one flipped bit  : 2CE47BF8 -> EEE51E0D (detected: True)
```

**APIs demonstrated.** `Adler32`, `Fletcher32`, `Fletcher64`, the shared
`NonCryptographicHashAlgorithm` streaming surface, single-bit corruption detection.

## Scenario 3 — StreamingResumable

**Intent.** Show the three incremental surfaces: chunked `Append` equals the one-shot digest
(split points don't matter); `HashingStream` checksums bytes as a side effect of ordinary
stream I/O (the copy-while-verifying pattern); and `IResumableHashAlgorithm` extends a
*stored* digest with new data — the append-only log pattern, where yesterday's log never
needs re-reading.

**What it does.** Computes the file's CRC one-shot, then via three arbitrary `Append` chunks,
then through a `HashingStream` wrapping the file during a `CopyTo` — all three digests equal.
It then simulates a two-day append-only ledger: day 1's digest is stored, day 2's records are
folded in with `ComputeHashFrom(storedDigest, day2)`, and the result equals a full replay of
the whole log.

**What to expect.**

```text
one-shot        : 2CE47BF8
chunked Append  : 2CE47BF8
HashingStream   : 2CE47BF8
resumable       : stored+day2 7F945ED5 == full replay 7F945ED5 -> True
```

**APIs demonstrated.** Chunked `Append`/`GetHashAndReset`,
`HashingStream(Stream, NonCryptographicHashAlgorithm)` + `.Algorithm.GetCurrentHash()`,
`IResumableHashAlgorithm.ComputeHashFrom`.

## Scenario 4 — FnvAndAdlerVariants

**Intent.** Two families demonstrated so far each ship in more than one width. Put the FNV-1a
hash and the Adler checksum side by side across their variants — FNV-1a in 32 and 64 bits, and
Adler in its RFC 1950 32-bit, SIMD-friendly power-of-two-modulus (`Adler32C`), and 64-bit forms —
so the shared `NonCryptographicHashAlgorithm` surface is visible while only the digest width and
mixing change.

**What it does.** Hashes one fixed in-code input (the 43-byte
`"The quick brown fox jumps over the lazy dog"`) with `Fnv1a32`, `Fnv1a64`, `Adler32`,
`Adler32C`, and `Adler64` through the identical `Append`/`GetHashAndReset` calls, printing each
digest as hex. FNV lives in `Bodu.IO.Hashing`; the Adler variants in `Bodu.IO.Hashing.Checksums`.

**What to expect.**

```text
input: 43 bytes
  FNV-1a/32 : 048FFF90
  FNV-1a/64 : F3F9B7F5E7E47110
  Adler-32  : 5BDC0FDA
  Adler-32C : 5BCD0FDA
  Adler-64  : 00015BCD00000FDA
wider digests spread the same input over more state - fewer accidental collisions.
```

The two Adler-32 forms differ only in their combining modulus — `Adler32` uses the RFC 1950
prime 65521, `Adler32C` the power-of-two 65536 for cheaper vectorized reduction — so their
digests are close but not interchangeable (`Adler32C` is an internal-only variant; anything
touching zlib/PNG must use `Adler32`). The wider 64-bit forms of each family spread the same
input over more state — the trade of a longer digest for fewer accidental collisions.

**APIs demonstrated.** `Fnv1a32`, `Fnv1a64`, `Adler32`, `Adler32C`, `Adler64`, the shared
`NonCryptographicHashAlgorithm` streaming surface.

## Scenario 5 — NonCryptoHashes

**Intent.** Show the classic hash functions doing the job they're built for — fast,
well-distributed, *deterministic* bucket assignment for sharding and routing — while stating
plainly what they are not: none of this is cryptographic, and an adversary can craft
collisions at will.

**What it does.** Routes eight fixed keys onto four shards with FNV-1a/32, MurmurHash3/32,
and CityHash/32 through the shared algorithm surface, printing each function's assignment.
The assignments differ between functions but are identical on every run — routing without
coordination.

**What to expect.**

```text
  FNV-1a/32   : alpha->1 bravo->2 charlie->0 delta->3 ...
  Murmur3/32  : alpha->1 bravo->0 charlie->3 delta->0 ...
  CityHash/32 : alpha->0 bravo->3 charlie->3 delta->0 ...
same keys, same shard, every run - deterministic routing without coordination.
```

**APIs demonstrated.** `Fnv1a32`, `MurmurHash3_32`, `CityHash32`, digest-to-`uint` bucket
mapping.

## Layout

```text
Bodu.IO.Hashing.Samples.ChecksumTour/
  Program.cs                        # runs the scenarios in order
  Data/pangrams.txt                 # committed 199-byte input
  Scenarios/CrcCatalogue.cs
  Scenarios/ChecksumFamilies.cs
  Scenarios/StreamingResumable.cs
  Scenarios/FnvAndAdlerVariants.cs
  Scenarios/NonCryptoHashes.cs
```

## Related

- `Bodu.IO.Hashing.Samples.CheckDigits` — the identifier-validation half of the package.
- `Bodu.IO.Hashing.Samples.CustomCheckDigit` — extending the check-digit contract yourself.
- Guides: `docs/guides/io-hashing/`.
