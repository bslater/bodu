# Articles

Narrative documentation and library overviews complement the auto-generated [API reference](../api/).

## Library overviews

Each library has a dedicated overview page that introduces its purpose, lists its key types, and shows a minimal usage example before handing off to the API reference.

- **[Bodu.Core — collections, buffers, and text utilities](../apidoc/Bodu.Collections.Generic.md)**
  Fixed-capacity circular buffers (single-threaded and lock-free), evicting dictionary with six policies, buffer conversion, array and base-encoding utilities, and centralised argument validation.

- **[Bodu.IO.Hashing — CRC and Fletcher checksums](../apidoc/Bodu.IO.Hashing.md)**
  Non-cryptographic checksums on `System.IO.Hashing.NonCryptographicHashAlgorithm` — the full CRC RevEng catalogue (widths 1–64 bits) and the Fletcher 16 / 32 / 64 family, with shared lookup-table caching and resumable hashing.

- **[Bodu.Security.Cryptography — ciphers, hashes, and Merkle trees](../apidoc/Bodu.Security.Cryptography.md)**
  Managed block ciphers (Threefish 256 / 512 / 1024, Serpent 128 / 256 / 512 / 1024, Camellia, Twofish, Blowfish, Skipjack), an `AesBlockCipher` adapter over the BCL AES engine paired with AEAD mode transforms, keyed and cryptographic hashes (SipHash, Tiger, ASCON), Merkle-tree hashing, and the classic non-cryptographic hash families (Adler, FNV, CityHash) that plug into the standard .NET cryptography contracts.

- **[Bodu.Globalization.Calendar — notable dates](../apidoc/Bodu.Globalization.Calendar.md)**
  Notable-date resolution with fixed, rule-based, offset-based, and dynamic calculators — including Gregorian-computus Easter and lunar-calendar Lunar New Year — driven from a pluggable XML rule source and an observance-adjustment pipeline.

## Guides

- **[Bodu.Core guides](../guides/core/)** — circular buffer, deque, evicting dictionary, week pattern.
- **[Bodu.IO.Hashing guides](../guides/io-hashing/)** — fingerprints, checksums (CRC, Fletcher, Adler), and check digits.
- **[Bodu.Security.Cryptography guides](../guides/cryptography/)** — encryption basics, cipher block modes, AEAD, padding, composing primitives, keyed and cryptographic hashing, the ASCON family.
- **[Bodu.Globalization.Calendar guides](../guides/calendar/)** — `NotableDateService`, calculators, rule authoring, data packs.

## Project documentation

- [Introduction](../docs/introduction.md) — project overview and design principles.
- [Getting started](../docs/getting-started.md) — prerequisites, install, and one-minute samples per library.
- [Algorithm families](../docs/algorithm-families.md) — cross-library taxonomy of fingerprints, checksums, check digits, cryptographic hashes, keyed hashes, and the three symmetric-cipher subtypes.
- Per-library introductions: [Bodu.Core](../docs/core/index.md) · [Bodu.IO.Hashing](../docs/io-hashing/index.md) · [Bodu.Security.Cryptography](../docs/cryptography/index.md) · [Bodu.Globalization.Calendar](../docs/calendar/index.md).
