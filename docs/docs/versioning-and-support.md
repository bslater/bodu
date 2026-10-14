---
title: Versioning, stability, and support
---

# Versioning, stability, and support

Bodu packages install separately, but they do not version separately. Every package ships in one of two lock-step version streams, chosen by its API-stability tier, and one release moves a whole stream at once. This page explains the two streams, what each tier promises about your code, how a package moves between tiers, which frameworks the packages target, and which releases receive fixes. The [package matrix](package-matrix.md) lists every package with its tier and its version.

## Two version streams

| Stream | Version in the 1.3.0 release | Packages on it |
|---|---|---|
| **Stable stream** | 1.3.0 | Every Stable package: 53 packages, all published to nuget.org. |
| **Preview stream** | 0.7.2 | Every Preview and Experimental package: `Bodu.Financial.ExchangeRates.Imf`, `.Yahoo`, and `.Ofx` (Experimental, published), plus four packages that are [not published](#packages-that-are-not-on-nugetorg). |

Within a stream every package carries the same version, and a matched version across `Bodu.*` packages is a set that was built, tested, and released together. Two rules follow for your project file:

- **Keep every Bodu package from the same stream on the same version.** When you move `Bodu.Core` to 1.3.0, move the other Stable Bodu packages you reference to 1.3.0 in the same change. NuGet resolves a mismatched set, but only the matched one was tested together.
- **A Preview-stream package depends on the Stable packages of its own release.** `Bodu.Financial.ExchangeRates.Imf` 0.7.2 depends on `Bodu.Financial.ExchangeRates` and `Bodu.Core` 1.3.0, so referencing it alongside the Stable packages raises nothing below 1.3.0.

A release is cut from one tag, which publishes every package in the shipping manifest at the version its stream is at. A stream whose version did not move publishes nothing new.

### Out-of-band releases

A Stable package can ship on its own, ahead of its stream, when a fix or a feature is worth releasing before the next lock-step release. The 1.0 line had four: `Bodu.Security.Cryptography` 1.1.0 and 1.2.0, `Bodu.Core` 1.0.1, and `Bodu.Globalization.Calendar` 1.0.1. Each is tagged `<PackageId>-<version>` on GitHub, with its own release notes. The next lock-step release always moves past them: 1.3.0 is above `Bodu.Security.Cryptography` 1.2.0, the highest out-of-band version, so from 1.3.0 every Stable package is back on one version.

## API-stability tiers

Each package's `README.md`, which is also its nuget.org page, opens with its tier, and the **Status** column of the [package matrix](package-matrix.md) repeats it. A tier describes the API contract: what a new version may do to code you have already written. It says nothing about whether the package is on nuget.org.

| Tier | Version range | What it commits to |
|---|---|---|
| **Stable** | 1.0 and later | The public API surface is committed. Breaking changes wait for a major version, except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. Additive changes ship in minor versions, and fixes in patch versions. |
| **Preview** | Below 1.0 | Offered for early evaluation. The public API is still taking shape and may change between releases without a major-version bump. |
| **Experimental** | Below 1.0 | Behavior depends on an external resource with no versioned contract, such as a feed its operator does not publish as an API, so it can stop working without any change in Bodu. Treat it as best-effort and pair it with a Stable alternative. |

### The recorded break in 1.3.0

1.3.0 carries one recorded break of a Stable API. `DayOfWeekSet`, one of the calendar value sets, replaces `WeekPattern`, and every signature that named `WeekPattern` changes with it:

| Package | What changes |
|---|---|
| `Bodu.Core` | `WeekPattern` is removed; APIs that took or returned one take or return a `DayOfWeekSet`. `ToWeekPattern()` becomes `ToDayOfWeekSet()` on `WorkingDaysOfWeek` and `ToWorkingWeek()` on `IWeekendDefinitionProvider`, which returns the working days rather than the weekend. |
| `Bodu.Globalization.Calendar` | The `workingWeek` parameter of the working-day and fiscal extensions, the `ResolutionPolicy` constructor and its `WorkingWeek` property, and `AdjustmentPolicy.IsTriggered` and `ApplyAction` take a `DayOfWeekSet`. |
| `Bodu.Globalization.Calendar.Builder` | `ResolutionPolicyBuilder.WithWorkingWeek` takes a `DayOfWeekSet`. |

Stored data is unchanged: calendar documents keep `workingDays="0111110"`, and compiled `.bcal` packs keep their byte layout. The [migration table](../../guides/core/day-of-week-set.md#migrating-from-weekpattern) maps each `WeekPattern` member to its `DayOfWeekSet` replacement.

### How compatibility is checked

Every published package is compared, when it is packed, against the last release of its own stream with the .NET SDK's package validation: the Stable packages against 1.0.0, and the Preview-stream packages against 0.7.1. A removed or changed public API fails the pack unless it is recorded, with its reason, in the package's `CompatibilitySuppressions.xml`; additions pass. The comparison runs on every pull request, so a Stable break cannot reach a release by accident. The baselines move up after each release.

## Moving between tiers

A Preview package is **promoted** to Stable once its API has settled. It joins the Stable stream in the release that promotes it, so its version jumps: the ten packages promoted in 1.3.0 go from 0.7.1 to 1.3.0.

| Promoted in 1.3.0 | Packages |
|---|---|
| Container and mail-store readers | `Bodu.IO.Pst`, `Bodu.Formats.Outlook`, `Bodu.Formats.Outlook.Msg`, `Bodu.Formats.Outlook.Pst` |
| Exchange-rate providers over published APIs | `Bodu.Financial.ExchangeRates.Boe`, `.Ecb`, `.Rba`, `.Fixer`, `.ExchangeRateHost`, `.Fred` |

A package settles its API while it is still Preview, so the release that promotes it can still change its API: read the release notes when you move one of these packages from 0.7.1 to 1.3.0. From 1.3.0 on, each is held to the Stable promise.

A package can also move the other way. In 1.3.0 the IMF, Yahoo, and OFX providers were retiered from Preview to Experimental, because each reads a feed its operator does not publish as an API. They stay on the Preview stream, at 0.7.2.

## Packages that are not on nuget.org

Four packages build and pack with the solution but are deliberately not published, and the [package matrix](package-matrix.md#not-published-to-nugetorg) says why: `Bodu.Financial.ExchangeRates.Xe` and `Bodu.Financial.ExchangeRates.Oanda` (Experimental), and the calendar toolchain `Bodu.Globalization.Calendar.Tool` and `Bodu.Globalization.Calendar.Build` (Preview). They version on the Preview stream when you build them from a clone, but no release publishes them, and the security policy does not cover them. The source generator `Bodu.Text.Formats.Generators` is not packaged at all.

## Target frameworks

Every runtime package targets `net8.0` and `net10.0`. The one build-time package, `Bodu.Globalization.Calendar.Build`, targets `netstandard2.0`, so its MSBuild task loads in any MSBuild host.

Keeping `net8.0` is deliberate. Adding a target framework is invisible to existing consumers, but removing one strands every application still on that runtime, so `net8.0` will be dropped only in a major version. Building the packages from source needs the .NET 10 SDK that the repository's `global.json` pins, because the sources use C# 14.

## Which releases receive fixes

A fix ships in a new version of the affected package: in the next release of its stream, or out of band when it cannot wait. The repository's security policy sets which releases receive security fixes, and they are not backported to earlier release lines:

| Tier | Receives security fixes |
|---|---|
| **Stable** | The latest release of the current major version. |
| **Preview** | The latest release only. |
| **Experimental** | The latest release only. |

Packages that are not published to nuget.org are not covered. To report a vulnerability privately, follow the repository's [security policy](https://github.com/bslater/bodu/blob/master/SECURITY.md).

## Reading versions on this site

Every API page names the package its type ships in and that package's version, under **Package** in the type's definition; a package that is not on nuget.org is marked so. The site has three kinds of version slot:

| Address | Documents |
|---|---|
| The site root | The latest release. |
| `/<series>/`, for example `/1.0/` | Each release series, archived when it ships. |
| `/dev/` | The current `master` branch, with the versions it would pack. |

## Where to go next

- **[Package matrix](package-matrix.md)** - every package with its tier, version, and dependencies.
- **[Getting started](getting-started.md)** - install commands and a first sample from each library.
- **[Calendar value sets](../../guides/core/day-of-week-set.md)** - `DayOfWeekSet` and the migration from `WeekPattern`.
