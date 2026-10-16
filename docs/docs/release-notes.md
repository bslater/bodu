---
title: Release notes
---

# Release notes

Every Bodu release moves one or both of the two version streams, Stable and Preview, as [versioning, stability, and support](versioning-and-support.md) explains. This page says what to change when you upgrade and lists every release. Each release's full notes, with every fix and the issue behind it, are on its [GitHub release](https://github.com/bslater/bodu/releases).

## Upgrading to 1.3.0

1.3.0 moves every Stable package to 1.3.0, promotes ten packages from the preview stream to Stable, and moves the three Experimental exchange-rate providers to 0.7.2. The [1.3.0 release notes](https://github.com/bslater/bodu/releases/tag/v1.3.0) list every change; this section covers what can need a change in your code.

### Update your package references

- **Move every Stable Bodu package to 1.3.0 in the same change.** That includes `Bodu.Core` 1.0.1, `Bodu.Security.Cryptography` 1.2.0, and `Bodu.Globalization.Calendar` 1.0.1, which shipped ahead of the lock-step version and rejoin it here.
- **Ten packages move from 0.7.1 to 1.3.0:** `Bodu.IO.Pst`, `Bodu.Formats.Outlook`, `Bodu.Formats.Outlook.Msg`, `Bodu.Formats.Outlook.Pst`, and the BoE, ECB, RBA, Fixer, exchangerate.host, and FRED exchange-rate providers. From 1.3.0 they keep the Stable promise; their changes since 0.7.1, breaking ones included, are listed below.
- **The IMF, Yahoo, and OFX providers move to 0.7.2.** They are now Experimental, because each reads a feed its operator does not publish as an API, and they depend on the 1.3.0 Stable packages.

### Breaking changes

| Package | What changed | What to do |
|---|---|---|
| `Bodu.Core`, `Bodu.Globalization.Calendar`, `Bodu.Globalization.Calendar.Builder` | `WeekPattern` is replaced by `DayOfWeekSet`, and every member that took or returned a `WeekPattern` takes or returns a `DayOfWeekSet`. Text that a `WeekPattern` wrote, calendar documents' `workingDays` attributes, and compiled `.bcal` packs read unchanged. | Follow the [migration table](../guides/core/day-of-week-set.md#migrating-from-weekpattern). A `WeekPattern.ToByte()` value stored elsewhere held Sunday at bit 6; `DayOfWeekSet` holds it at bit 0. |
| `Bodu.Text.Bencode` | A literal `null` passed to `BencodeElement.GetProperty` or `TryGetProperty` no longer compiles: the new UTF-8 overloads make it ambiguous. | Cast the `null` to `string`. |
| `Bodu.IO.Pst` (against 0.7.1) | `PstNode.Id` and `ParentId` are renamed `NodeId` and `ParentNodeId`, and the `PstFileException(message, innerException, error)` constructor is removed. | Use the new names, and the `(message, error, innerException)` constructor. |
| `Bodu.Financial.ExchangeRates.Boe`, `.Ecb`, `.Rba` (against 0.7.1) | Each options type derives from `WebRateProviderOptions`. The endpoint settings become `BaseAddress`, `HttpTimeout`, and `UserAgent`; `BoeEndpointOptions` and `EcbEndpointOptions` are removed; the BoE's `OnDemandWindowDays` becomes `DefaultLookback` and the RBA's `CurrentEraRefreshInterval` becomes `RefreshInterval`. The configuration keys follow the members. The three file-cache types are internal. | Rename the keys in your configuration, as the [provider configuration guide](../guides/financial/provider-configuration.md) shows. Every option keeps its default. |

### Behavior changes to check

These changes need no code change to compile, but they can change what your code sees at run time.

- **The format libraries reject input they accepted.** The readers and serializers for Bencode, TOML, YAML, CSV, dotenv, INI, and EditorConfig, and the compound-file, `.xls`, `.msg`, and `.pst` readers, were held to the defect fixes in other libraries' release notes, and fix what that found. Some malformed input that 1.0 read now fails with the format's exception, and some valid input reads differently. See [conformance and test vectors](topics/conformance.md) for how each reader is held to its specification.
- **The serializers are stricter about types and values.**
  - An integer outside the range of an enum's underlying type is a serialization error, where it was narrowed into another value.
  - A string enum converter created with `allowIntegerValues: false` reads and writes names only, as System.Text.Json's does.
  - Combined flags, renamed members, and enum dictionary keys are written and read as System.Text.Json writes and reads them, so the text for a flags value or a renamed member can change.
  - The DotEnv and INI serializers reject a scalar root, such as a `decimal` or a `DateOnly`.
  - The Delimited serializer rejects a record type that is a scalar or a collection.
  - The Delimited and DotEnv serializers reject a type with a member that is not a scalar, on read as on write.
- **Exceptions change type.**
  - `Bodu.Formats.Outlook.Pst` reports a fault in the `.pst` container as `OutlookPstFormatException`, with the container's `PstFileException` as its inner exception, so one `catch (OutlookFormatException)` covers the `.msg` and `.pst` readers.
  - The calendar loader reports more malformed documents as load diagnostics, so `Load` throws `NotableDateValidationException` where another exception escaped, and `TryLoad` returns `false`.
  - `ThrowHelper.ThrowIfNotEqual<T>` throws `ArgumentOutOfRangeException`, a subclass of the `ArgumentException` it threw.
  - See [errors and exceptions](topics/errors-and-exceptions.md) for each package's exception family.
- **Exchange-rate defaults change.**
  - The undated lookups of `CachingRateProvider` and `AggregatingRateProvider` return the most recent rate on or before today, as the web providers do, where they required a rate dated today.
  - Today's rates are fetched again after `WebRateProviderOptions.RefreshInterval`, 12 hours by default.
  - The named `HttpClient` that each `Add<Source>ExchangeRates` registers is capped at `MaxResponseContentBufferSize`, 64 MiB by default.
  - The per-observation and cache hit-and-miss log events default to `Trace`.
  - exchangerate.host's default range path is `timeframe`, and Yahoo dates each daily bar in the exchange's time zone.
- **`AddCachedNotableDateService` decorates only the unkeyed service.** Keyed `INotableDateService` registrations stay in place, undecorated; see [dependency injection and configuration](topics/hosting.md).
- **Trimmed and native-AOT applications see new warnings.** The Bencode and YAML serializers, and the JSON converter factories of `Bodu.Numerics.Serialization.Json` and `Bodu.Financial.Serialization.Json`, declare `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]`, so a call to them reports IL2026 or IL3050. [Trimming and native AOT](topics/trimming-and-native-aot.md) gives the alternatives.

### What is new

- **Calendar value sets** in `Bodu.Core`: `MonthSet`, `DayOfMonthSet`, `HourSet`, `MinuteSet`, `SecondSet`, and `DayOfWeekSet`, behind one interface. See [calendar value sets](../guides/core/calendar-value-sets.md).
- **Recurrence:** sub-daily rules (`FREQ=HOURLY`, `MINUTELY`, `SECONDLY`) are enumerated, cron accepts the Quartz day tokens `L`, `W`, `#`, and `?`, and the rule builder takes the calendar value sets. See [sub-daily frequencies](../guides/recurrence/rrule.md#sub-daily-frequencies) and [the Quartz day tokens](../guides/recurrence/cron.md#the-quartz-day-tokens).
- **Salvage reads of damaged `.pst` stores**, which leave out a value whose reference does not resolve instead of failing the object. See [salvaging a damaged store](../guides/outlook/reader-options-and-limits.md#salvaging-a-damaged-store).
- **Smaller additions:** Bencode dictionary keys by their bytes, `LoadJson` overloads that take an algorithm registry, `ConfigurationKeyOptions.LowercaseKeys`, the Delimited writer's `CommentChar`, Base58's Flickr alphabet, and `WebRateProviderOptions.RefreshInterval`.
- **Faster:** recurrence queries no longer replay a series from its start, the BLAKE2 and BLAKE3 kernels shorten each round's critical path, and Argon2 rotates with a single AVX-512 instruction where the processor has one, with output unchanged bit for bit.

## Release history

| Release | Published | What it shipped |
|---|---|---|
| [1.3.0](https://github.com/bslater/bodu/releases/tag/v1.3.0) | 2026-10-16 | Every Stable package at 1.3.0, ten packages promoted to Stable, and the Experimental providers at 0.7.2, as above. |
| [`Bodu.Globalization.Calendar` 1.0.1](https://github.com/bslater/bodu/releases/tag/Bodu.Globalization.Calendar-1.0.1) | 2026-10-01 | Out of band: working-day lookups no longer overflow the stack. |
| [`Bodu.Core` 1.0.1](https://github.com/bslater/bodu/releases/tag/Bodu.Core-1.0.1) and [`Bodu.Security.Cryptography` 1.2.0](https://github.com/bslater/bodu/releases/tag/Bodu.Security.Cryptography-1.2.0) | 2026-10-01 | Out of band: further cryptography speed-ups, with the `Bodu.Core` internals they call. |
| [`Bodu.Security.Cryptography` 1.1.0](https://github.com/bslater/bodu/releases/tag/Bodu.Security.Cryptography-1.1.0) | 2026-09-29 | Out of band: Argon2 and cryptography speed-ups, and two conformance fixes after which data that AES-256-GCM-SIV sealed, and that AES-SIV sealed with an empty plaintext, in earlier releases does not decrypt. See [AEAD modes](../guides/cryptography/aead-modes.md). |
| [1.0.0](https://github.com/bslater/bodu/releases/tag/v1.0.0) | 2026-09-24 | The Stable stream at 1.0.0 and the preview stream at 0.7.1; every library targets `net8.0` and `net10.0`; `ArrayExtensions.Reverse` is renamed `ToReversed`. |
| [0.7.0](https://github.com/bslater/bodu/releases/tag/v0.7.0) | 2026-09-24 | 14 packages: the exchange-rate infrastructure, its dependency injection, nine providers, and the three caching packages. |
| [0.6.0](https://github.com/bslater/bodu/releases/tag/v0.6.0) | 2026-09-24 | 17 packages: the financial core, the calendar engine with its data packs and companions, recurrence, and the configuration bridge. |
| [0.5.0](https://github.com/bslater/bodu/releases/tag/v0.5.0) | 2026-09-23 | 25 packages: the foundation libraries, the format and text libraries, and the `.pst` readers. |

## Where to go next

- **[Versioning, stability, and support](versioning-and-support.md)** - the two streams, the tiers, and which releases receive fixes.
- **[Package matrix](package-matrix.md)** - every package with its tier and version.
- **[Getting started](getting-started.md)** - install commands and a first sample from each library.
