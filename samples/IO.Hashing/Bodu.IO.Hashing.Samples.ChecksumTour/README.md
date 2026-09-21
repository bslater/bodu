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
--- The CRC catalogue - one engine, every standard ---
  What   : Reports the catalogue size, then computes the published check value for five standards across four
           widths, showing each one's polynomial and reflection settings, and resolves a standard by name.
  Why    : "CRC-32" names a family, not an algorithm. The width, polynomial, initial value, reflection and final XOR
           all vary between standards, and two implementations that disagree on any of them produce different
           digests for the same bytes - which is why interoperating with a device or format means matching its exact
           standard rather than reaching for whatever CRC is nearest. One parameterised engine plus a catalogue
           makes that a lookup instead of a reimplementation.
  Expect : Each standard reproduces its published check value over the canonical "123456789" input, which is how a
           CRC implementation is conventionally verified. Note the digest bytes are little-endian, so
           CRC-32/ISO-HDLC prints 2639F4CB for the published 0xCBF43926.

  catalogue: 112 standards, CRC-3 to CRC-64  (every entry is a parameter row, not a code path - supporting a new protocol adds no implementation)
  CRC-8/SMBUS        width  8, poly 0x7, reflect --/--- -> F4
  CRC-16/MODBUS      width 16, poly 0x8005, reflect in/out -> 374B
  CRC-16/XMODEM      width 16, poly 0x1021, reflect --/--- -> C331
  CRC-32/ISO-HDLC    width 32, poly 0x4C11DB7, reflect in/out -> 2639F4CB
  CRC-64/XZ          width 64, poly 0x42F0E1EBA9EA3693, reflect in/out -> FA3919DFBBC95D99
  (every digest above is that standard's published check value - reproducing them is how a CRC implementation is conventionally verified)
  (digest bytes are little-endian: 26 39 F4 CB above == the published check 0xCBF43926)
  FromName("CRC-32/ISO-HDLC") == CRC32_ISOHDLC -> True  (expected True - a spec naming its CRC in prose resolves to the same value as the named constant)
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
--- Checksum families over one input ---
  What   : Runs CRC-32, Adler-32, Fletcher-32 and Fletcher-64 over the same file, then flips a single bit and
           recomputes.
  Why    : These are error-detection codes, not hashes - they exist to catch accidental corruption on a wire or a
           disk, and they are cheap enough to run on every frame. None of them is a security primitive: an attacker
           can construct a colliding message trivially, so a checksum tells you a message arrived intact, never that
           it came from who you think. Adler and Fletcher trade detection strength for speed against CRC, which is
           why all three still ship in real protocols.
  Expect : Four different digests over identical bytes, because each algorithm is a different function rather than a
           different encoding of one answer. A single flipped bit changes the CRC completely - that avalanche is
           exactly the property that makes corruption detectable.

  input: 199 bytes  (one committed file, so every digest below is reproducible on any machine)
  CRC-32/ISO-HDLC: 2CE47BF8
  Adler-32       : E82B4775
  Fletcher-32    : E5EA4774
  Fletcher-64    : 001BE5CF00004774
  (identical bytes in, four unrelated digests out - these are different functions, not different encodings of one answer)
  one flipped bit  : 2CE47BF8 -> EEE51E0D (detected: True)  (expected True, and note the digest changes wholesale rather than in one bit - that avalanche is what makes small corruptions loud)
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
--- Streaming and resumable hashing ---
  What   : Computes one digest four ways: a one-shot call, chunked Append calls, through a HashingStream, and by
           saving state after day one and resuming on day two.
  Why    : Feeding a hash in pieces has to give the same answer as feeding it whole, or the API is unusable for
           anything that does not fit in memory. Resumability goes further and is rarer: saving the internal state
           lets a digest span a process restart, so an append-only log can be checksummed incrementally forever
           instead of re-reading it from the beginning each night. HashingStream is the same capability shaped as a
           pass-through, so bytes can be hashed while they are being copied rather than in a separate pass.
  Expect : The first three routes produce byte-identical digests. The resumed digest equals a full replay over both
           days' data, which is the property that makes stored state trustworthy - not merely that it produced some
           stable value.

  one-shot        : 2CE47BF8  (the reference digest the next three routes must match)
  chunked Append  : 2CE47BF8  (expected to equal the one-shot digest - the split points are arbitrary and must not be observable)
  HashingStream   : 2CE47BF8  (same digest again - the bytes were hashed while being copied, not in a second pass over the file)
  resumable       : stored+day2 7F945ED5 == full replay 7F945ED5 -> True  (expected True - day one's bytes were never re-read, so the stored digest alone carried the history forward)
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
--- FNV-1a and Adler width variants over one input ---
  What   : Hashes the same input with FNV-1a at 32 and 64 bits, and with Adler-32, Adler-32C and Adler-64.
  Why    : Width is a collision-probability decision. By the birthday bound a 32-bit digest reaches a 50% chance of
           some collision at roughly 77,000 items - fine for a hash-table bucket, not fine as a content identifier
           for a large corpus. Moving to 64 bits pushes that to billions. Adler-32C is the worked example of why the
           variant matters: same width, different parameters, and the digests differ - so two systems must agree on
           the exact variant, not just the family and size.
  Expect : Five different digests from one input. Adler-32 and Adler-32C differ despite sharing a width, and the
           64-bit forms visibly carry the 32-bit halves in their structure.

  input: 43 bytes  (a fixed in-code string, so the digests below are stable across runs and machines)
  FNV-1a/32 : 048FFF90
  FNV-1a/64 : F3F9B7F5E7E47110
  Adler-32  : 5BDC0FDA
  Adler-32C : 5BCD0FDA
  Adler-64  : 00015BCD00000FDA
  (Adler-32 and Adler-32C share a width yet disagree - the variant is part of the contract, not just the family and size)
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
--- Non-cryptographic hashes - bucket assignment (NOT security) ---
  What   : Routes the same eight keys to four buckets through FNV-1a, Murmur3 and CityHash, printing each key's
           destination.
  Why    : This is what a non-cryptographic hash is for: mapping keys to shards or buckets, fast, with a spread that
           avoids hot spots. Determinism is the load-bearing property - every node computes the same destination for
           a key with no coordination, which is what makes sharding work at all. What these must never do is
           authenticate. They are not one-way and not collision-resistant, so using one for a token, a signature or
           a password is a vulnerability rather than a shortcut.
  Expect : The three algorithms disagree on where a given key lands - which is fine and expected, since a system
           only needs one of them, applied consistently. Each is stable across runs, so the same key reaches the
           same shard every time.

  FNV-1a/32   : alpha->1 bravo->2 charlie->0 delta->3 echo->0 foxtrot->2 golf->2 hotel->1
  Murmur3/32  : alpha->1 bravo->0 charlie->3 delta->0 echo->2 foxtrot->3 golf->0 hotel->1
  CityHash/32 : alpha->0 bravo->3 charlie->3 delta->0 echo->1 foxtrot->0 golf->1 hotel->3
  (the three rows disagree on where a key lands, which is fine - a system needs one function applied consistently, not the same one everywhere)
  same keys, same shard, every run - deterministic routing without coordination.
```

**APIs demonstrated.** `Fnv1a32`, `MurmurHash3_32`, `CityHash32`, digest-to-`uint` bucket
mapping.

## Layout

```text
Bodu.IO.Hashing.Samples.ChecksumTour/
  Program.cs                        # runs the scenarios in order
  SampleConsole.cs                  # the What / Why / Expect scenario banner
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
