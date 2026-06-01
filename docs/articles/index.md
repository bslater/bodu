# Articles

Narrative documentation and library overviews complement the auto-generated [API reference](xref:Bodu).

If you are new to Bodu, start with the [project introduction](../docs/introduction.md) and the [cross-library getting-started page](../docs/getting-started.md). The [Bodu.IO.Hashing](../docs/io-hashing/index.md) and [Bodu.Security.Cryptography](../docs/cryptography/index.md) introductions map the hashing and cryptography type hierarchy and explain how to choose between types that sound similar.

## API namespace landing pages

Each top-level namespace has a landing page that introduces its purpose, lists its key types, and shows a minimal usage example before handing off to the auto-generated reference.

- **[Bodu.Collections.Generic — Bodu.Core collections and utilities](xref:Bodu.Collections.Generic)**
  Fixed-capacity circular buffers (single-threaded and lock-free), the `Deque<T>` value-and-reference type, the `EvictingDictionary<TKey, TValue>` with six eviction policies, range-keyed dictionaries, the `WeekPattern` value type, buffer conversion, base encoding, and centralized argument validation.

- **[Bodu.IO.Hashing — fingerprints, checksums, and check digits](xref:Bodu.IO.Hashing)**
  Non-cryptographic hashes on `System.IO.Hashing.NonCryptographicHashAlgorithm` — the full CRC RevEng catalogue (widths 1–64 bits), the Fletcher 16 / 32 / 64 family, Adler-32 / 32C / 64, FNV-1 / 1a, CityHash, MurmurHash3, Pearson, classic string hashes — plus single- and multi-character check digits (Luhn, Damm, Verhoeff, EAN, GTIN, IBAN, ISBN, SEDOL, CUSIP, LEI).

- **[Bodu.Security.Cryptography — ciphers, hashes, AEAD, and Merkle trees](xref:Bodu.Security.Cryptography)**
  Managed block ciphers (Threefish 256 / 512 / 1024, Serpent 128 / 256 / 512 / 1024, Camellia, Twofish, Blowfish, Skipjack), an `AesBlockCipher` adapter over the BCL AES engine paired with six AEAD mode transforms (GCM, CCM, OCB, EAX, SIV, GCM-SIV), keyed hashes (SipHash, Poly1305), cryptographic digests (Tiger, CubeHash, Snefru, Whirlpool, BLAKE2/3, Skein, Shake, ASCON), Merkle-tree hashing, and full ASCON-AEAD support.

- **[Bodu.Globalization.Calendar — notable-date resolution](xref:Bodu.Globalization.Calendar)**
  Rule-driven notable-date resolution with fixed, day-of-week-in-month, offset, and algorithm strategies — including Gregorian and Orthodox Easter, Hindu Lunar dates, Losar, Vesak, Asalha Puja, and Qingming — driven from pluggable XML or JSON rule sources, an observance-adjustment pipeline, and a trust-policy-driven plugin host.

- **[Bodu.Text.Formats — self-framing binary serialization formats](../docs/formats/index.md)**
  Strongly-typed value model and a span- and stream-friendly codec for self-framing binary formats. Ships **Bencode** (the BitTorrent BEP 3 grammar) as the first format — `Bencode.Encode` / `Decode` / `TryEncode` / `TryDecode` / `GetEncodedLength` over `ReadOnlySpan<byte>`, `byte[]`, and `Stream`, an immutable `BencodedValue` tree (`Integer`, `String`, `List`, `Dictionary`), and full canonicality enforcement on both sides of the pipeline.

- **[Bodu.Numerics — exact rational arithmetic and bounded intervals](xref:Bodu.Numerics)**
  `Fraction<T>` for canonical rational arithmetic over any `IBinaryInteger<T>` backing type, with `BigInteger`-promoted intermediates, the full `INumber<T>` / `ISignedNumber<T>` surface, mixed-number and Unicode-vulgar-fraction formatting, continued-fraction expansion, and best rational approximation. `Interval<T>` for closed / open / half-open intervals with intersection, union, and adjacency.

- **[Bodu.Financial — type-safe monetary primitives](xref:Bodu.Financial)**
  `Money<TCurrency>` where the currency is encoded as the type parameter so cross-currency arithmetic fails the build; `MoneyValue` for runtime-tagged scenarios; `MoneyBag` for multi-currency portfolios; a shipped catalogue of ~185 ISO 4217 currencies (active + historic with demonetisation metadata); an audit-grade `IDatedExchangeRateProvider` stack; fair allocation; cash rounding; `Fraction<BigInteger>` interop for sub-minor-unit-precise chains; and three JSON wire shapes (strict / lenient / compact).

## Guides

- **[Bodu.Core guides](../guides/core/index.md)** — circular buffer, deque, evicting dictionary, week pattern.
- **[Bodu.IO.Hashing guides](../guides/io-hashing/index.md)** — fingerprints (FNV, CityHash, MurmurHash3, Pearson, classic string hashes), checksums (CRC, Fletcher, Adler), and check digits.
- **[Bodu.Security.Cryptography guides](../guides/cryptography/index.md)** — encryption basics, cipher block modes, AEAD, padding, composing primitives, keyed and cryptographic hashing, the ASCON family.
- **[Bodu.Globalization.Calendar guides](../guides/calendar/index.md)** — `NotableDateService`, built-in algorithms, rule authoring (XML / JSON / [fluent builder](../guides/calendar/notable-date-builder.md)), [data packs](../guides/calendar/data-packs.md).
- **[Bodu.Text.Formats guides](../guides/formats/index.md)** — using the `Bencode` codec, the `BencodedValue` tree model, and stream support.
- **[Bodu.Numerics guides](../guides/numerics/index.md)** — [`Fraction<T>`](../guides/numerics/fraction.md), [`Interval<T>`](../guides/numerics/interval.md).
- **[Bodu.Financial guides](../guides/financial/index.md)** — [`Money<TCurrency>`](../guides/financial/money.md).

## Project documentation

- [Introduction](../docs/introduction.md) — project overview, design principles, and the per-library map.
- [Getting started](../docs/getting-started.md) — prerequisites, install commands, and one-minute samples per library.
- Per-library introductions: [Bodu.Core](../docs/core/index.md) · [Bodu.IO.Hashing](../docs/io-hashing/index.md) · [Bodu.Security.Cryptography](../docs/cryptography/index.md) · [Bodu.Globalization.Calendar](../docs/calendar/index.md) · [Bodu.Text.Formats](../docs/formats/index.md) · [Bodu.Numerics](../docs/numerics/index.md) · [Bodu.Financial](../docs/financial/index.md).
