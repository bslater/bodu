# Library Consistency Review

A cross-library consistency assessment of every **non-test production library** in
the Bodu solution, covering four dimensions: **documentation, examples, exceptions,
and public API surface**. Vendor code (`bc-csharp`), all `/test/` projects, the
independent `Bodu.CodeStyle` solution, `docs/`, and `archive/` are out of scope.

**Overall verdict:** the solution is **highly consistent**. File-scoped namespaces,
the file-header banner, field naming (`_camelCase` / `s_camelCase`), one-public-type-per-file,
resx-backed exception text, and domain `ThrowHelper` adoption are uniform across the
libraries. The issues found were a small number of concrete, low-risk divergences —
all but two now remediated on this branch — plus one documentation/intent question
left for a maintainer decision. Several inconsistencies flagged by the initial
automated sweep proved, on verification, to be **false positives** and are recorded
below so they are not re-investigated.

## Libraries assessed

`Bodu.Core`, `Bodu.Numerics`, `Bodu.IO.Hashing`, `Bodu.Security.Cryptography`,
`Bodu.Text.Encoding`, `Bodu.Text.Configuration`, `Bodu.Text.Formats`, `Bodu.Text`,
`Bodu.Extensions.Configuration.Text`, `Bodu.Globalization.Calendar`,
`Bodu.Globalization.Calendar.Builder`, `Bodu.Globalization.Calendar.Plugins`,
`Bodu.Globalization.Calendar.DependencyInjection`, the five
`Bodu.Globalization.Calendar.Data.<Region>` bundles, `Bodu.Financial`, and
`Bodu.Financial.DependencyInjection`.

## Dimension 1 — Documentation

**Consistent and strong.** Every sampled file carries the standard copyright banner,
file-scoped namespace, and BCL-aligned XML documentation: verb-led `<summary>`,
`<param>` for every parameter, `<returns>` on every non-void member (including
properties), `<exception>` coverage, and private members documented to the same
standard as public ones. No namespace-style drift remains (the legacy block-scoped
files once noted for `Bodu.Security.Cryptography` are gone). No remediation required.

## Dimension 2 — Examples

**Consistent at the level that matters.** The headline, consumer-facing type in each
library already carries a type-level `<example>` (`CircularBuffer<T>`, `Deque<T>`,
`EvictingDictionary<,>`, `IndexedSet<T>`, `IndexedPriorityQueue<,>`,
`ConcurrentHashSet<T>`, `ConcurrentCircularBuffer<T>`, `PooledBufferBuilder`,
`WeekPattern`, all five `Base*` encoders, `BaseFormattingOptions`,
`ConfigurationDocument`, `NotableDateService`, `NotableDateResourceLoader`, …).

The low aggregate example percentages reported by the initial sweep (14–22% for some
libraries) are measured across **all** files — including partial member-files,
internal helpers, enums, and options types — not the primary entry points. That is a
normal distribution, not an inconsistency. Only three primary types genuinely lacked
a type-level example; two were addressed on this branch (see Remediation). The third,
`NotableDateRule`, is loader/builder-constructed from heavy domain types, so a
hand-written constructor example would be contrived and is intentionally **not**
added — consumers meet it through `NotableDateService`, which is already exemplified.

## Dimension 3 — Exceptions

**Consistent.** No production library hard-codes an exception message string literal.
Every text-bearing library externalizes messages to a `<Domain>ResourceStrings.resx`
+ `ResXFileCodeGenerator` Designer pair, follows the `Arg_*` / `Op_*` / `Format_*` /
`IO_*` / `Json_*` key-prefix convention, formats with `CultureInfo.CurrentCulture`,
and validates arguments through `Bodu.Core`'s `ThrowHelper` or a domain
`ThrowHelper` partial. The one divergence — `Bodu.Financial.DependencyInjection`
storing its single message as a hand-rolled `const` instead of resx — was remediated
on this branch. `Bodu.Globalization.Calendar.DependencyInjection` correctly has **no**
resource file because it exposes no custom text (only `ThrowHelper.ThrowIfNull`).

## Dimension 4 — Public API surface

**Consistent.** All production `.csproj` files target `net8.0`, enable `Nullable`,
set `LangVersion=latest`, `GenerateDocumentationFile=true`, and `OutputType=Library`.
Naming conventions and Try-parse / span / UTF-8 / `IBufferWriter` overload patterns
are uniform. The two structural divergences found were the TFM element form (singular
vs plural) — remediated — and `Bodu.Core`'s assembly/package name (open decision,
below).

## Per-library compliance matrix

| Library | Docs | Examples | Exceptions | API/csproj |
|---|---|---|---|---|
| Bodu.Core | ✓ | ✓ | ✓ | ⚠ AssemblyName (decision) |
| Bodu.Numerics | ✓ | ✓ (Fraction example added) | ✓ | ✓ |
| Bodu.IO.Hashing | ✓ | ✓ | ✓ | ✓ |
| Bodu.Security.Cryptography | ✓ | ✓ | ✓ | ✓ (TFM normalized) |
| Bodu.Text.Encoding | ✓ | ✓ | ✓ | ✓ |
| Bodu.Text.Configuration | ✓ | ✓ (Profile example added) | ✓ | ✓ |
| Bodu.Text.Formats | ✓ | ✓ | ✓ | ✓ |
| Bodu.Text | ✓ | ✓ | ✓ | ✓ |
| Bodu.Extensions.Configuration.Text | ✓ | ✓ | ✓ | ✓ |
| Bodu.Globalization.Calendar | ✓ | ✓ | ✓ | ✓ (TFM normalized) |
| Bodu.Globalization.Calendar.Builder | ✓ | ✓ | ✓ | ✓ |
| Bodu.Globalization.Calendar.Plugins | ✓ | ✓ | ✓ | ✓ |
| Bodu.Globalization.Calendar.DependencyInjection | ✓ | ✓ | ✓ (no text needed) | ✓ |
| Bodu.Globalization.Calendar.Data.\<Region\> (×5) | ✓ | ✓ | ✓ | ✓ |
| Bodu.Financial | ✓ | ✓ | ✓ | ✓ |
| Bodu.Financial.DependencyInjection | ✓ | ✓ | ✓ (resx conversion) | ✓ |

Note: `Bodu.Core`'s assembly/package name was aligned to `Bodu.Core` (see Remediation);
it is now fully consistent on all four dimensions.

## Remediation performed on this branch

| Commit | Change |
|---|---|
| TFM normalization | `Bodu.Globalization.Calendar` and `Bodu.Security.Cryptography` switched from singular `<TargetFramework>` to the plural `<TargetFrameworks>` used by the other 13 net8.0 libraries. |
| DI resx conversion | `Bodu.Financial.DependencyInjection`'s `const`-string message holder replaced with a `DependencyInjectionResourceStrings.{resx,Designer.cs}` pair wired into the project; class/member names preserved so the throw site is unchanged. Verified the resource embeds and resolves at runtime; DI tests pass 15/15. |
| Examples | Type-level `<example>` added to `Fraction<T>` and the `ConfigurationProfile` enum, in the established `<code language="csharp">` + CDATA house style. Both projects build clean under the doc-comment-as-error settings. |
| CLAUDE.md | Added the previously-undocumented `Bodu.Text`, `Bodu.Financial`, and `Bodu.Financial.DependencyInjection` rows to the project table; corrected the Test Tiers commands to `bodu.slnx`; corrected the `ImplicitUsings` statement (enabled across all projects, including `Bodu.Core`). |
| Core assembly name | `Bodu.Core` was the only library renaming its assembly (`Bodu.CoreLib`), which — with no `PackageId` in `bld/Bodu.props` — also made its NuGet id `Bodu.CoreLib`. Aligned it to the convention (`$(MSBuildProjectName)` → `Bodu.Core`), updating all four references: the src `AssemblyName`, the audit comment, the `InternalsVisibleTo` grant, and the test project's `AssemblyName` (now `$(MSBuildProjectName)` → `Bodu.Core.Test`, matching every sibling test project). Verified: Core emits `Bodu.Core.dll`, the full Core test suite passes (24,784/24,784) proving the `InternalsVisibleTo` grant still resolves, and downstream consumers (IO.Hashing, Calendar) build clean. |

## False positives corrected (no action needed)

- **"`var` is a `Bodu.Text.Encoding`-specific violation (~700 sites)."** False. `var`
  is used pervasively in *every* library (e.g. Crypto ~1246, Core ~944, Hashing ~492,
  Encoding ~453). It is uniform, not an inter-library inconsistency. See note below.
- **"`Base16` exposes array overloads the other encoders lack."** False. All five
  `Base*` encoders expose the `byte[]` and `(byte[], offset, count)` Encode overloads.
  Remaining surface differences (Base58/Base85 use `GetMaxEncodedLength` / span-based
  length; Base16 has case-only variants) are justified by each encoding's mathematics.
- **"`Bodu.Core` source already uses explicit usings, so `ImplicitUsings` can be
  disabled to match the doc."** False. Disabling it produces 1374 compile errors;
  the source relies on implicit usings. The documentation was corrected instead.

## Resolved — `Bodu.Core` assembly/package name

`Bodu.Core` was the only library renaming its assembly to `Bodu.CoreLib`; because
`bld/Bodu.props` sets no `PackageId`, this also made Core's NuGet package id
`Bodu.CoreLib` while every peer is `Bodu.*`. The name was self-consistent across four
sites (the src `AssemblyName`, the `InternalsVisibleTo` grant, the audit comment, and
the test project's `Bodu.CoreLib.Test` assembly name), so it was carried to a decision
rather than assumed. **Decision:** align it to the convention. The src `AssemblyName`
and the test `AssemblyName` now use `$(MSBuildProjectName)` (yielding `Bodu.Core` and
`Bodu.Core.Test`, matching every sibling), and the comment and `InternalsVisibleTo`
grant were updated to `Bodu.Core.Test`. This is an outward-facing package-identity
change: the published package/assembly id moves from `Bodu.CoreLib` to `Bodu.Core`, so
any downstream reference to the old id must be updated. Verified the full Core test
suite (24,784 tests) passes and downstream consumers build clean.

## Note — `var` versus explicit types

`CLAUDE.md` states "Explicit types preferred over `var`," but `var` is the de-facto
style throughout the codebase (thousands of sites in every library). This is recorded
here per the review's agreed scope; **no code or convention text was changed** — a
mass `var` → explicit-type refactor is out of scope and the existing usage is
internally consistent.

## Verification

- Each remediated project builds individually under Debug with doc-comment warnings as
  errors; `Bodu.Financial.DependencyInjection.Test` passes 15/15 and the resx resource
  was confirmed to embed and resolve to the expected text at runtime.
- Solution-level `dotnet build bodu.slnx` requires a newer SDK than the review
  container's 8.0.127 (which cannot parse `.slnx`); per-project builds were used in its
  place.
