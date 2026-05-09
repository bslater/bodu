# CLAUDE.md

Guidance for AI assistants working in this repository. Read this file before making changes.

## Repository Overview

**Bodu** is a multi-project C# utility library solution focused on high-performance, well-documented, framework-style building blocks. The solution (`Bodu.sln`) contains four independent projects:

| Project | Path | Responsibility |
|---|---|---|
| `Bodu.Core` | `Bodu.Core/` | Buffers, generic collections (circular buffer, evicting dictionary), extensions, text, XML, argument validation helpers. |
| `Bodu.IO.Hashing` | `Bodu.IO.Hashing/` | Non-cryptographic hashing built on `System.IO.Hashing.NonCryptographicHashAlgorithm` — Fletcher-16/32/64 and the full RevEng CRC catalogue (CRC-3 … CRC-64). |
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
- `Bodu.IO.Hashing` — `net8.0`
- `Bodu.Security.Cryptography` — `net8.0`
- `Bodu.Globalization.Calendar` — `net8.0`

Nullable reference types are enabled everywhere. `ImplicitUsings` is enabled for `Bodu.IO.Hashing`, Cryptography, and Calendar but **disabled** for `Bodu.Core` — when editing files in `Bodu.Core/`, add explicit `using` directives.

## Key Types

- **Bodu.Core**: `CircularBuffer<T>`, `ConcurrentCircularBuffer<T>`, `EvictingDictionary<TKey, TValue>`, `EvictingDictionaryPolicy`, `IRandomGenerator`, `BufferConverter`, `ArrayExtensions`, `BaseEncoding`, `ThrowHelper`.
- **Bodu.IO.Hashing**: `Fletcher16` / `Fletcher32` / `Fletcher64`, `Crc`, `CrcStandard`, `CrcStandards`, `CrcLookupTableBuilder`, `CrcLookupTableCache`, `BlockNonCryptographicHashAlgorithm<T>`, `IResumableHashAlgorithm`.
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
dotnet test  Bodu.sln --settings bvt.runsettings              # default build run (BVT)
dotnet test  Bodu.sln --settings smoke.runsettings            # smoke only
dotnet test  Bodu.sln --settings regression.runsettings       # full regression
dotnet test  Bodu.Core/test/Bodu.Core.Test.csproj --settings bvt.runsettings
```

See **Test Tiers** below for the category convention each runsettings file applies.

`test.runsettings` enables parallel execution (`MaxCpuCount=0`) and disables AppDomains.

## Test Conventions

- Framework: **MSTest** (`Microsoft.VisualStudio.TestTools.UnitTesting`, `[TestClass]` / `[TestMethod]`). Do **not** introduce xUnit or NUnit.
- Tests live in `<Project>/test/` and are organised as **partial classes** that mirror the source layout — e.g. `CircularBuffer.cs` → `CircularBufferTests.Enqueue.cs`, `CircularBufferTests.Dequeue.cs`. Extend the existing partial class when adding tests for an existing type.
- No shared test base classes; each test is self-contained.

### Test Tiers (Smoke / BVT / Regression / Stress)

The suite is partitioned into tiers via `[TestCategory(...)]` so the build can run a fast subset by default and the exhaustive set on demand. Tier names are also exposed as constants on `Bodu.Test.TestCategories` for projects that reference `Bodu.Test`; either the constant or the literal string works.

| Tier | Tag | Purpose |
|---|---|---|
| **Smoke** | `[TestCategory("Smoke")]` | One happy-path test per primary public type. Catches catastrophic breakage. |
| **BVT** *(default)* | *(no category)* | Structural, exception, property, and contract tests. |
| **Regression** | `[TestCategory("Regression")]` | Exhaustive vector tables, full algorithm catalogues, large parameter sweeps, multi-decade calendar tables. Excluded from BVT. |
| **Stress** | `[TestCategory("Stress")]` | Long-running, high-iteration loops. Excluded from BVT. |

Run-settings files at the repository root drive each tier:

```bash
dotnet test Bodu.sln --settings smoke.runsettings        # Smoke only
dotnet test Bodu.sln --settings bvt.runsettings          # BVT (default build run)
dotnet test Bodu.sln --settings regression.runsettings   # Everything
dotnet test Bodu.sln --settings test.runsettings         # Everything (legacy alias)
```

Conventions:

- Default a new test to **BVT** by leaving `TestCategory` unset.
- Mark a test **Regression** when it is data-driven over a published vector table, an exhaustive catalogue, or a wide parameter sweep that duplicates structural coverage.
- Mark a test **Smoke** sparingly — one per primary type, exercising the most important public method on a happy-path input.
- Pre-existing `[TestCategory("Stress")]` tags retain their semantics.

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

### Test Exception Handling

When validating exceptions, always capture them using `Assert.ThrowsExactly<TException>` with the action enclosed in a statement block.

Rules:

- Always use **`Assert.ThrowsExactly<TException>`** for exception assertions.
- Always assert the **specific expected exception type**. Do not use broader base exception types unless that is the exact expected contract.
- Always write the invocation being tested inside a block-bodied lambda:

```csharp
var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
{
    _ = new TestCityHash(hashSize);
});
```

- When applicable, validate the inner exception:
-- Assert that an inner exception exists when one is expected.
-- Assert its exact type.
-- Validate its message and other relevant properties where required by the contract.

```csharp
  var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
{
    sut.Execute();
});

Assert.IsNotNull(ex.InnerException);
Assert.IsInstanceOfType<ArgumentException>(ex.InnerException);
Assert.IsTrue(ex.InnerException.Message.Contains("Invalid state", StringComparison.Ordinal));
```

- Guidance:
-- Validate only the exception details that form part of the public contract.
-- For argument exceptions, prefer asserting:
--- exact exception type
--- ParamName
--- relevant message content where useful
-- For wrapped exceptions, also validate the InnerException chain where that wrapping is intentional and contractually significant.
-- Keep exception assertions explicit and local to the test; do not hide them behind helper methods unless already established in the test suite.

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

- `Bodu.Core`, `Bodu.IO.Hashing`, and `Bodu.Globalization.Calendar` already follow this convention throughout.
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
- Expression-bodied members for methods, properties, and accessors with a small implementation footprint — see **Expression-Bodied Members** below for the required layout.
- Public argument validation goes through the `ThrowHelper.ThrowIf…` members (in `Bodu.Core`) — see **Parameter Validation** below.

## C# Code Style Guidelines

### File and Header Formatting

- Include the copyright header in the standard format with separator banner lines.
- Preserve consistent spacing and alignment within the header.
- Follow the established file presentation style for partial classes and related files.

### XML Documentation

**All documentation must be in British English.**

**Documentation scope**
- Provide complete XML documentation for **every** member of a declared type — `public`, `protected`, `internal`, **and** `private`. Private members are documented to the same standard as public members.
- The only exception is `<remarks>`: it is optional on private members and should be added only when the private implementation genuinely warrants it (for example, a subtle concurrency protocol, a lock-free state transition, or a non-obvious invariant that aids future maintainers).

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
- **Properties must always include a `<returns>` element** describing what the property yields on read, in addition to any `<value>` element that clarifies semantics.

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

### Parameter Validation

All public interfaces (public methods, constructors, protected-virtual extension points, indexers) must validate their parameters using the `ThrowHelper.ThrowIf…` members declared in `Bodu.Core`.

- Prefer an existing `ThrowIf…` helper over hand-rolled checks. The catalogue covers nulls, ranges, enum values, array offsets/lengths, span sizes, type compatibility, and related cases.
- If no existing helper fits a validation rule, **add a new `ThrowIf…` member** to `ThrowHelper` when the rule is general-purpose enough to be reused. Follow the naming, signature, and XML-doc conventions established by the existing helpers (including the `CallerArgumentExpression`-driven `paramName`).
- Inline `if`-statement validation is permitted only for rules that are specific to a single call site and do not justify a shared helper. In that case, format the check on a **single line**:

  ```csharp
  if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentNullException(nameof(xml));
  ```

- **Group validation statements together** at the top of the member, before any real work. Keep helper calls and single-line `if` checks in a single contiguous block, then a blank line, then the method body.

Example:

```csharp
public static NotableDateRule Create(string name, int dayOffset, string? culture)
{
    ThrowHelper.ThrowIfNull(name);
    ThrowHelper.ThrowIfGreaterThan(dayOffset, MaxOffset);
    if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must not be blank.", nameof(name));

    return new NotableDateRule(name, dayOffset, culture);
}
```

### Expression-Bodied Members

Use the `=>` expression-bodied form for methods, properties, and accessors whose implementation is small (a single expression or trivial delegation). Format with `=>` on the declaring line and the expression on the **next** line, indented one level:

```csharp
public static List<NotableDateRule> ParseXml(string xml) =>
    ParseDocument(xml).LocalRules.ToList();
```

- The `=>` token stays at the end of the signature line, not on the body line.
- A single level of indentation separates the body from the declaration.
- Use a block body instead when the implementation spans multiple statements or needs intermediate locals, guard clauses, or inline documentation.
- Trivial property and accessor bodies (e.g. `=> _field;`) may remain on one line.

### Formatting and Layout

**Blank Lines**
- Insert blank lines between logical groups of code to make structure visually clear.
- Separate guard clauses and validation, field assignments, setup and initialization, core logic branches, success and failure paths, event invocation or side effects, and return statements.

**Member Layout**
- Maintain consistent spacing between members.
- Group related members logically.
- Use expression-bodied members (per **Expression-Bodied Members** above) where the body is a small, single expression.
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
