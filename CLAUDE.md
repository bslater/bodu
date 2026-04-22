# CLAUDE.md

Guidance for AI assistants working in this repository. Read this file before making changes.

## Repository Overview

**Bodu** is a multi-project C# utility library solution focused on high-performance, well-documented, framework-style building blocks. The solution (`Bodu.sln`) contains four independent projects:

| Project | Path | Responsibility |
|---|---|---|
| `Bodu.Core` | `Bodu.Core/` | Buffers, generic collections (circular buffer, evicting dictionary), extensions, text, XML, argument validation helpers. |
| `Bodu.IO` | `Bodu.IO/` | Non-cryptographic hashing built on `System.IO.Hashing.NonCryptographicHashAlgorithm` — Fletcher-16/32/64 and the full RevEng CRC catalogue (CRC-3 … CRC-64). |
| `Bodu.Security.Cryptography` | `Bodu.Security.Cryptography/` | Block ciphers (Threefish 256/512/1024, Skipjack), keyed and cryptographic hashes (Adler, SipHash, FNV1a, Tiger), crypto helpers. |
| `Bodu.Globalization.Calendar` | `Bodu.Globalization.Calendar/` | Calendar/notable date calculations (Easter, Lunar New Year) and date resolvers. |

Each project has the layout:

```
<Project>/
  src/   # production code, grouped by namespace folder
  test/  # MSTest project mirroring src structure
```

### Target Frameworks

- `Bodu.Core` — `net8.0`
- `Bodu.IO` — `net8.0`
- `Bodu.Security.Cryptography` — `net8.0`
- `Bodu.Globalization.Calendar` — `net8.0`

Nullable reference types are enabled everywhere. `ImplicitUsings` is enabled for `Bodu.IO`, Cryptography, and Calendar but **disabled** for `Bodu.Core` — when editing files in `Bodu.Core/`, add explicit `using` directives.

## Key Types

- **Bodu.Core**: `CircularBuffer<T>`, `ConcurrentCircularBuffer<T>`, `EvictingDictionary<TKey, TValue>`, `EvictingDictionaryPolicy`, `IRandomGenerator`, `BufferConverter`, `ArrayExtensions`, `BaseEncoding`, `ThrowHelper`.
- **Bodu.IO**: `Fletcher16` / `Fletcher32` / `Fletcher64`, `Crc`, `CrcStandard`, `CrcStandards`, `CrcLookupTableBuilder`, `CrcLookupTableCache`, `BlockNonCryptographicHashAlgorithm<T>`, `IResumableHashAlgorithm`.
- **Bodu.Security.Cryptography**: `Threefish256` / `Threefish512` / `Threefish1024`, `Adler32`, `SipHash`, `FNV1a`, `Skipjack`, `Tiger`, `CryptoHelpers`.
- **Bodu.Globalization.Calendar**: `NotableDateService`, `NotableDateResolver`, `NotableDate`, `NotableDateKind`, `EasterSundayNotableDateCalculator`, `LunarNewYearNotableDateCalculator`.

## Build & Tooling

- Shared MSBuild configuration lives in `bld/Bodu.props` (Authors, MIT licence, deterministic builds, package metadata, doc-comment warnings as errors — e.g. CS1591).
- `.editorconfig` lives under `Bodu.Core/src/.editorconfig` and drives formatter settings.
- Analyzers in use: **StyleCop.Analyzers**, **Roslynator.Analyzers**, **Microsoft.CodeAnalysis.NetAnalyzers**, **AsyncFixer**, **VisualStudio.Threading.Analyzers**. Treat analyzer warnings as actionable — fix rather than suppress unless there is a strong reason.
- Licence header template: `Bodu.sln.licenseheader` (the `PlaceholderCompany` string is the project template placeholder — preserve the banner exactly as used in existing files).
- `.filenesting.json` nests partial-class files: any `<Base>.<Part>.cs` file nests under `<Base>.cs`. Keep partial splits consistent with this pattern.
- CI: `.github/workflows/docfx-build-publish.yml` builds DocFX documentation on pushes to `master` and publishes to GitHub Pages.

### Common Commands

```bash
dotnet build Bodu.sln
dotnet test  Bodu.sln --settings test.runsettings
dotnet test  Bodu.Core/test/Bodu.Core.UnitTests.csproj --settings test.runsettings
```

`test.runsettings` enables parallel execution (`MaxCpuCount=0`) and disables AppDomains.

## Test Conventions

- Framework: **MSTest** (`Microsoft.VisualStudio.TestTools.UnitTesting`, `[TestClass]` / `[TestMethod]`). Do **not** introduce xUnit or NUnit.
- Tests live in `<Project>/test/` and are organised as **partial classes** that mirror the source layout — e.g. `CircularBuffer.cs` → `CircularBufferTests.Enqueue.cs`, `CircularBufferTests.Dequeue.cs`. Extend the existing partial class when adding tests for an existing type.
- No shared test base classes; each test is self-contained.

### Test Method Naming

Convention: `<MethodOrProperty>_When<Condition>[_For<TypedCondition>]_Should<ExpectedResult>`

- `When<Condition>` — the input or state under test.
- `_For<TypedCondition>` — optional qualifier for a type/overload variant.
- `Should<ExpectedResult>` — the observable outcome.

Examples:

```csharp
Enqueue_WhenFull_ShouldThrowInvalidOperationException()
Parse_WhenInputIsEmpty_ForNullableInt_ShouldReturnNull()
Capacity_WhenSetToZero_ShouldThrowArgumentOutOfRangeException()
```

### Test Method Documentation

Every test method has an XML `<summary>` starting with **"Verifies that ..."**, describing scenario and expected outcome in 1–2 sentences so intent is clear without reading the body.

```csharp
/// <summary>
/// Verifies that enqueueing an item into a full buffer throws
/// <see cref="InvalidOperationException" />.
/// </summary>
[TestMethod]
public void Enqueue_WhenFull_ShouldThrowInvalidOperationException() { ... }
```

## Source File Conventions

### File Header

Every `.cs` file begins with the standard banner — preserve the separator lines and the `file=` / `company=` attributes exactly:

```csharp
// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------
```

### Namespace Style

- Use **file-scoped** namespaces — terminate the namespace declaration with `;` and do **not** wrap the file contents in `{ }`. This applies to every project in the solution; no exceptions.

  ```csharp
  namespace Bodu.Collections.Generic;

  public sealed class CircularBuffer<T> { ... }
  ```

- `Bodu.Core`, `Bodu.IO`, and `Bodu.Globalization.Calendar` already follow this convention throughout.
- `Bodu.Security.Cryptography` contains legacy block-scoped nested namespace files. Do not mix styles within a file, but when a file's primary type is being edited for other reasons, convert it to the file-scoped `;` form at the same time.

### File Layout

- **One public type per file.** Every `.cs` file declares exactly one top-level type. Nested / child types must live in separate partial-class files nested under the parent file per `.filenesting.json` (see Build & Tooling).
- Partial-file naming is `<Base>.<Part>.cs` where `<Base>.cs` holds the root declaration. Examples:
  - `CircularBuffer.cs` ← root
  - `CircularBuffer.Enumerator.cs`, `CircularBuffer.Debug.cs` ← partials/child-type splits
  - `CrcStandard.cs` ← root; `CrcStandard.Catalog.cs` ← auto-generated catalogue partial
- Don't stack unrelated helper types into the same file. If a type only makes sense alongside its parent (private nested enum, internal helper record), split it into a partial file under the parent rather than co-locating it in the root.

### Naming

- Private instance fields: `_camelCase`.
- Private static fields: `s_camelCase`.
- Explicit types preferred over `var`.
- No primary constructors on documented public types (they conflict with `<param>` XML documentation).
- Expression-bodied members only for trivial properties/accessors; use block bodies for anything with meaningful logic.
- Public argument validation goes through `ThrowHelper` (in `Bodu.Core`).

## C# Code Style Guidelines

### File and Header Formatting

- Include the copyright header in the standard format with separator banner lines.
- Preserve consistent spacing and alignment within the header.
- Follow the established file presentation style for partial classes and related files.

### XML Documentation

**All documentation must be in British English.**

**`<summary>`**
- Write a concise, professional summary describing the purpose, intent, or responsibility of the type or member.
- Keep the tone factual and API-consumer focused.
- Do not mechanically repeat the member name.
- Prefer strong verb-led phrasing: *Provides…*, *Gets…*, *Initializes…*, *Attempts to…*, *Returns…*, *Removes…*, *Adds…*.

**`<param>`**
- Add a `<param>` for every parameter.
- Keep descriptions concise — ideally a single line.
- Describe the parameter in the context of the member's behaviour.
- Use `Must not be <see langword="null" />.` style wording where applicable.

**`<returns>`**
- Add `<returns>` for every non-void member.
- Describe the result in the context of the method's purpose, not merely the raw type.

**`<exception>`**
- Document all exceptions the member can throw, including `ArgumentNullException`, `ArgumentException`, `ArgumentOutOfRangeException`, and `InvalidOperationException`.
- Describe the exact condition that causes each exception using the established style:
  - `<paramref name="capacity" /> ≤ 0.`
  - `The buffer is empty.`
  - `Thrown if <paramref name="owner" /> is <see langword="null" />.`

**`<remarks>`**
- Add `<remarks>` when it materially helps the consumer understand concurrency behaviour, snapshot semantics, ordering guarantees, side effects, edge cases, stability caveats, performance trade-offs, or design intent.
- Use `<para>` blocks within remarks where appropriate to maintain visual structure.

**`<example>`**
- Add examples when they improve usability or remove ambiguity.
- Keep examples minimal, realistic, and consumer-focused.
- Prefer examples for public types or members where usage is not immediately obvious.

**`<value>`**
- Include `<value>` on properties where the semantics require clarification beyond the summary.

**`<inheritdoc />`**
- Use `<inheritdoc />` where the implementation intentionally inherits interface or base member documentation and no further clarification is needed.

### Documentation Tone

- Be concise, but not abrupt.
- Be precise, but not overly academic.
- Explain observable behaviour, guarantees, and limitations.
- Use standard XML documentation idioms consistently.
- Do not write vague or filler summaries.
- Do not repeat obvious type information unnecessarily.
- Do not over-explain trivial members.
- Do not use casual or conversational wording.

### Inline Comments

- Add inline comments only where they provide real value.
- Use them to explain non-obvious logic, concurrency coordination, lock-free or low-level state transitions, defensive clamping, important sequencing requirements, or why a block exists when it is not self-evident.
- Explain *why*, the protocol intent, or subtle state meaning — not basic syntax.
- Do not add comments that merely narrate obvious code.

### Formatting and Layout

**Blank Lines**
- Insert blank lines between logical groups of code to make structure visually clear.
- Separate guard clauses and validation, field assignments, setup and initialization, core logic branches, success and failure paths, event invocation or side effects, and return statements.

**Member Layout**
- Maintain consistent spacing between members.
- Group related members logically.
- Use expression-bodied members where the body is trivially concise.
- Use block bodies for members with meaningful logic.

**Braces and Wrapping**
- Follow standard modern C# brace style as shown in the examples.
- Wrap long XML documentation lines and remarks sensibly for readability.

**Naming and Qualification**
- Use consistent naming and qualification patterns aligned to the examples.
- Retain explicit interface qualification where it improves clarity.
- Use framework types and language keywords consistently.

### Code Quality

- Write code that is clear, maintainable, consistent, review-friendly, defensive where appropriate, and idiomatic C#.
- Prefer clarity over cleverness.
- All code must be suitable for shared library or framework-style use.

### Updating Existing Code

- Preserve the original intent and behaviour unless explicitly instructed otherwise.
- Improve documentation, formatting, naming clarity, and readability without introducing unnecessary rewrites.
- Keep style consistent across the file — do not mix documentation styles.
- Avoid excessive comments or overlong XML documentation.
- Extend an established style consistently rather than replacing it.
