---
title: Release notes
---

# Release notes

Every Bodu release moves one or both of the two version streams, Stable and Preview, as [versioning, stability, and support](versioning-and-support.md) explains. This page says what to change when you upgrade and lists every release. Each release's full notes, with every fix and the issue behind it, are on its [GitHub release](https://github.com/bslater/bodu/releases).

## Upgrading to 1.3.1

1.3.1 moves every Stable package to 1.3.1 with fixes and additions, and the preview stream stays at 0.7.2. No public member is removed or changed. The [1.3.1 release notes](https://github.com/bslater/bodu/releases/tag/v1.3.1) list every fix and the issue behind it; this section covers what can need a change in your code. If you are moving from a release before 1.3.0, read the [1.3.0 release notes](https://github.com/bslater/bodu/releases/tag/v1.3.0) as well: 1.3.0 replaced `WeekPattern` with `DayOfWeekSet` and promoted ten packages to Stable.

### Update your package references

- **Move every Stable Bodu package to 1.3.1 in the same change.** No public member is removed or changed, so code that builds against 1.3.0 builds against 1.3.1.
- **`Bodu.Financial.DependencyInjection` depends on `Microsoft.Extensions.Logging.Abstractions`**, which it uses to report a monetary-context reload it rejects.
- **The IMF, Yahoo, and OFX providers stay at 0.7.2.** They depend on the 1.3.0 Stable packages and run beside the 1.3.1 ones you reference.

### Breaking changes

None.

### Behavior changes to check

These changes need no code change to compile, but they can change what your code sees at run time.

- **Look-alike letters no longer pass for ASCII ones.** Several readers of ASCII text changed the case of their input with .NET's invariant casing, which also maps U+017F LATIN SMALL LETTER LONG S (`ſ`) to `S` and U+212A KELVIN SIGN (`K`) to `k`. Text that holds either character now fails as any other character outside the format does:
  - `Bodu.Text.Encoding`: Bech32 strings, in `Decode`, `TryDecode`, `IsValid`, `DecodeToBytes`, and `TryDecodeToBytes`, and the names `BinaryEncodings.Get` resolves.
  - `Bodu.IO.Hashing`: the symbols `Crockford32` reads.
  - `Bodu.Core`: the letter masks and format strings of `DayOfWeekSet` and `MonthSet`.
  - `Bodu.Globalization.Recurrence`: cron month and day names and `@` macros, the `BYDAY` and `WKST` weekdays of an RRULE, and the designators of an `AnchoredInterval` duration.
  - `Bodu.Financial.Serialization.Json`: currency codes read under `FinancialJsonPolicy.Lenient`.
  - The five regional calendar data packs: the country codes that `LoadResource` and `CreateService` take.
- **The YAML reader rejects a `%TAG` handle that holds a letter or digit outside ASCII**, which YAML 1.2 does not allow in a named handle.
- **`Base62.GetMaxDecodedLength` returns the character count**, a bound the decoded length never exceeds, where it returned about three-quarters of it and was too small for text that leads with `0` digits.
- **The notable-date cache resolves and stores a territory under one normalized form.** `CachingNotableDateService` trims the territory and upper-cases its ASCII letters, then resolves and caches that one form, so `" SE"` resolves Sweden's dates where it resolved none, and can no longer empty Sweden's cached year for every later caller. A territory that holds a character outside ASCII is resolved on every call and never cached. A custom `INotableDateCache` should normalize its keys the same way, as [writing a cache backend](../guides/calendar/caching/custom-backend.md) describes.
- **Calendar diagnostics from XML text carry a position.** `NotableDateValidationDiagnostic.ToString()` appends `(line N, position M)` when a diagnostic has one, and two diagnostics that differ only in position no longer compare equal.
- **Exceptions change type or message.**
  - `NotableDatePluginLoader.LoadFrom` reports a plugin type that cannot be created, such as an open generic type, `void`, or a `ref struct`, as the `PluginActivationException` it documents, where an `ArgumentException` or a `NotSupportedException` escaped.
  - `Bech32.DecodeToBytes` reports a data part that does not convert to whole bytes with a message that says so, where it gave the data-alphabet message with its placeholder unfilled.

### What is new

- **Monetary contexts from configuration.** `FinancialOptions` carries a default monetary context and named ones, bound from the `Financial` configuration section, with a `MonetaryRoundingMode` that covers the five `MidpointRounding` modes and stochastic rounding. The new `IMonetaryContextResolver` (`Default`, `Get`, `TryGet`) follows `IOptionsMonitor<FinancialOptions>`, so a configuration reload changes the contexts it returns without a restart, and a reload with invalid settings is logged and leaves the previous contexts serving. See [configuring monetary contexts](../guides/financial/dependency-injection.md#configuring-monetary-contexts).
- **UTF-8, buffer-writer, and GUID overloads for Base45 and Base62**, the shapes the other five encodings of the `IBinaryEncoding` family already had: `DecodeFromUtf8`, `EncodeToUtf8`, `TryEncodeToUtf8`, `Encode` and `EncodeToUtf8` into an `IBufferWriter<T>`, and `Encode(Guid)`, `DecodeGuid`, and `TryDecodeGuid`. See [Base45](../guides/text-encoding/base45.md#utf-8-buffer-writers-and-guids) and [Base62](../guides/text-encoding/base62.md#utf-8-buffer-writers-and-guids).
- **Calendar diagnostics point at their source.** `NotableDateValidationDiagnostic` gains `LineNumber` and `LinePosition`, counted from one. A document loaded from XML text points each diagnostic at the element or attribute it concerns, a diagnostic about an import at the loaded document's `Import` or `Use` element, and a JSON-subset syntax error at the character the reader stopped at; documents made with the builder, and `.bcal` packs, carry no position. See [validation diagnostics](../guides/calendar/validation-diagnostics.md).
- **`Bodu.IO.Compound` and `Bodu.Text.Encoding` are declared AOT-compatible**, so the trim and AOT analyzers check them on every build. See [trimming and native AOT](topics/trimming-and-native-aot.md).

## Release history

| Release | Published | What it shipped |
|---|---|---|
| 1.3.1 | This release | Every Stable package at 1.3.1, with the fixes and additions above. |
| [1.3.0](https://github.com/bslater/bodu/releases/tag/v1.3.0) | 2026-10-16 | Every Stable package at 1.3.0, ten packages promoted to Stable, and the Experimental providers at 0.7.2. |
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
