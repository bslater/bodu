---
title: Thread safety
---

# Thread safety

"Can I share this object between threads?" has a different answer for each kind of Bodu type, and getting it wrong rarely fails loudly: a race corrupts state quietly, or shows up once under load. This page gathers the answer for the types you are most likely to keep in a field, register as a singleton, or hand to parallel work, across the whole suite. Every row comes from the type's own API documentation, or, where the documentation says nothing, from its source, and the table says which.

For `Bodu.Core`, `Bodu.Collections`, and `Bodu.Collections.Concurrent`, the [thread-safety contracts guide](../../guides/core/thread-safety.md) has a row for every public type, with its enumeration behavior; this page summarizes it.

## Terms

| Term | Meaning |
|---|---|
| **Safe** | Designed for concurrent use from any number of threads without your own locking. |
| **Immutable** | Cannot change after construction, so sharing is safe. |
| **Read-only safe** | Concurrent reads are safe while no thread writes; a write needs exclusive access. |
| **Single-threaded** | No internal synchronization: confine the instance to one thread at a time, or lock around every use. |

## Core Foundations

| Types | Contract | Notes |
|---|---|---|
| The calendar value sets (`DayOfWeekSet` and its siblings), `Option<T>`, `Result<T>`, `Either<TLeft,TRight>` | Immutable | Value types; every operation returns a new value. |
| The `Bodu.Threading` primitives (`AsyncLock`, `AsyncSemaphore`, `RateGate`, and the rest), `Memoizer` | Safe | Concurrent use is their purpose. |
| The extension classes and `ThrowHelper` | Safe | Stateless; only as safe as the arguments you pass them. |
| `PooledBufferBuilder<T>`, `XorShiftRandom` | Single-threaded | One owner each. |
| Every mutable collection in `Bodu.Collections` | Single-threaded, or read-only safe | None takes a lock. Where a lookup rewrites state - `EvictingDictionary<TKey,TValue>` under a recency policy, `SequencedDictionary<TKey,TValue>` in access order, `DefaultingDictionary<TKey,TValue>`'s indexer, `DisjointSet<T>.Find` - even concurrent readers race. |
| `ConcurrentCircularBuffer<T>`, `ConcurrentHashSet<T>`, `ConcurrentEvictingDictionary<TKey,TValue>`, `ConcurrentLruCache<TKey,TValue>` | Safe | Enumeration never throws for concurrent modification; the hash set's is weakly consistent and the other three enumerate a snapshot. |

## Hashing and cryptography

| Types | Contract | Notes |
|---|---|---|
| Every `Bodu.IO.Hashing` algorithm (`Crc`, `Fnv1a64`, `Fletcher32`, ...) and check-digit algorithm | Single-threaded | Their remarks say instances are not thread-safe: construct one per thread, or lock. The process-wide CRC lookup-table cache behind `Crc` is shared and safe. |
| Every `Bodu.Security.Cryptography` algorithm instance: the `HashAlgorithm`, `KeyedHashAlgorithm`, `SymmetricAlgorithm`, and `AsymmetricAlgorithm` types, the mode and AEAD transforms, `SecretBytes` | Single-threaded | The BCL's per-instance model; the [security posture guide](../../guides/cryptography/security-posture.md#thread-safety) states the single-caller rule. |
| The static entry points (`Hkdf`, `Hotp`, `Totp`, `Hpke`, `Argon2id.DeriveKey`, `Scrypt.DeriveKey`, `HashAlgorithmHelper`, `BlockCipherModeFactory`, `PaddingFactory`) | Safe | They hold no shared mutable state. |
| `MerkleTree` | Safe | Immutable and stateless between calls, so one instance can be a singleton - **provided its factory returns a fresh `HashAlgorithm` on every call**, as `SHA256.Create` does. A factory that hands back one shared algorithm corrupts concurrent computations. |

## Globalization and calendars

| Types | Contract | Notes |
|---|---|---|
| `NotableDateService` | Safe | Immutable once constructed, with no per-query state; share one instance per resource. |
| `NotableDateResource` | Immutable | Imports and overrides are applied while it loads. |
| `NotableDateAlgorithmRegistry` | Safe | Lookups are lock-free and may run concurrently with registrations. |
| `CachingNotableDateService` | Safe | Built to be a shared decorator: concurrent misses for the same cold year coalesce onto one computation, and a reload swaps its state atomically (from its source; the type has no thread statement). |
| `InMemoryNotableDateCache` | Safe | Reads and writes are individually thread-safe; two concurrent stores for one territory resolve last-write-wins. |
| `RecurrenceRule` | Immutable | Build it once and query it from any thread. |
| `CronExpression`, `RecurrenceSet`, `AnchoredInterval` | Immutable | No thread statement, but every field is fixed when the value is parsed or built. |
| `RecurrenceRuleBuilder` | Single-threaded | A mutable builder; build, then share the rule it produces. |

## Text and serialization

| Types | Contract | Notes |
|---|---|---|
| The `Utf8…Reader` and `Utf8…Writer` types of every format, and `BiffReader` / `BiffWriter` | Single-threaded | `ref struct`s: they live on one thread's stack by construction. |
| `…SerializerOptions` | Safe once frozen | An options instance is mutable until its first use (or `MakeReadOnly()`), then read-only, with its resolved converters cached in concurrent dictionaries. Configure one, and share it. See [options lifecycle](../serialization/core/concepts.md#options-lifecycle-freezing-and-thread-safety). |
| The static serializers (`TomlSerializer`, `YamlSerializer`, `DelimitedSerializer`, and the rest) | Safe | Static entry points; thread safety is that of the options and values you pass. |
| The mutable node DOMs (`TomlNode`, `IniObject`, ...) | Single-threaded | Ordinary object graphs. |
| `YamlDocument` | Read-only safe | Element views stay valid until the document is disposed, and disposing it while another thread reads is not safe. The other formats' read-only documents carry no thread statement. |
| `TextFilter` | Safe | The compiled state is immutable, so matching and filtering are exact under concurrency; only the statistics counters are unsynchronized and may undercount. |
| `BinaryEncodings` instances | Safe | Each property returns a thread-safe singleton. |
| `ConfigurationDocument` | Single-threaded | Its section list is fixed, but sections and entries stay mutable through `IniSection`. |

## Numerics and financial

| Types | Contract | Notes |
|---|---|---|
| `Fraction<T>`, `BigDecimal`, `Complex<T>`, `Interval<T>`, `Money`, `Money<TCurrency>` | Immutable | `readonly struct`s. |
| `MoneyBag` | Immutable | Every mutator returns a new instance. |
| `CurrencyRegistry` | Safe | Lookups read an immutable frozen dictionary. |
| The web exchange-rate providers (every `WebRateProvider`, such as `EcbRateProvider`) | Safe | Lookups read an immutable snapshot and never block behind a fetch; concurrent loads for one window share a single request. See [exchange-rate concepts](../exchange-rates/concepts.md#thread-safety). |
| `InMemoryRateCache` | Safe | Reads and writes are individually thread-safe; concurrent stores for one pair resolve last-write-wins. `SqliteRateCache` runs each write in a transaction, so concurrent writes for one pair lose neither. |
| `RateSeriesBuilder`, `RateTableBuilder` | Single-threaded | Their remarks say so. |

## Binary formats

| Types | Contract | Notes |
|---|---|---|
| `CompoundFile`, opened read-only and buffered (the default) | Safe | The whole source is read at open, so access never touches it again. Opened unbuffered, reads are serialized against the shared source position: concurrent, but not parallel. |
| `CompoundFile`, opened for editing | Not stated | The thread statement covers read-only instances only. Edits are staged until `Commit`; make them from one thread. |
| `ExcelBinaryWorkbook` | Single-threaded | Its remarks say to call its members from one thread at a time; an `ExcelWorksheetReader` is a forward-only reader for one consumer. |
| `PstFile`, `OutlookMailStore` | Single-threaded | Their remarks say a session's members, and those of every view obtained from it, must not be called concurrently. |
| `OutlookMessage` | Single-threaded | Its remarks say a message's members, and those of every recipient, attachment, and nested message obtained from it, must not be called concurrently; its recipients, attachments, and named-property map are decoded on first access and cached without synchronization. |

## Rules of thumb

1. **Share immutable values and frozen configuration; confine builders, sessions, and algorithm instances.** The value types, resources, compiled filters, and frozen serializer options are made to be shared; a hash algorithm, a document session, or a builder is not.
2. **Register singletons only for types marked Safe or Immutable.** Every Bodu dependency-injection registration adds its services as singletons, so a type you plug into one - a currency lookup, a rate provider, a cache backend - must be safe for concurrent use too; see [dependency injection and configuration](hosting.md).
3. **Do not use a reader-writer lock for a type whose reads write.** The collections listed above rewrite state on lookup.
4. **Give each worker its own algorithm.** For parallel hashing, share a factory (`Func<HashAlgorithm>` or `IHashAlgorithmFactory<T>`), never an instance.

## Where to go next

- **[Thread-safety contracts guide](../../guides/core/thread-safety.md)** - every Core Foundations type, with its enumeration behavior.
- **[Concurrent collections](../../guides/core/concurrent-collections.md)** - the four thread-safe collections in depth.
- **[Async coordination primitives](../../guides/core/async-primitives.md)** - the locks and gates for coordinating the rest.
