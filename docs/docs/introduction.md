# Introduction

**Bodu** is a solution that ships three independent .NET NuGet packages, each focused on a narrow, well-defined problem domain. The packages share nothing at runtime — each is self-contained — but share a single set of source and documentation conventions, a single analyzer and test configuration, and a single quality bar.

## The libraries

| Package | Purpose | Target frameworks |
|---|---|---|
| **Bodu.Core** | Fixed-capacity collections, buffer conversion helpers, array and text extensions, argument-validation helpers. | `net8.0` |
| **Bodu.Security.Cryptography** | Managed block ciphers and non-cryptographic / cryptographic hashes and checksums. | `net8.0` |
| **Bodu.Globalization.Calendar** | Notable-date resolution and dynamic calendar calculators (Easter, Lunar New Year). | `net6.0`, `net7.0`, `net8.0` |

Each package is versioned and released independently. Take the one you need and ignore the others — there are no cross-package runtime dependencies.

## Design principles

- **Small by intent.** Each library solves one coherent problem. If something fits better elsewhere in the .NET ecosystem, we don't duplicate it.
- **Nullable reference types** are enabled solution-wide. Public APIs make their null-intent explicit.
- **Analyzer-clean.** The solution runs StyleCop.Analyzers, Roslynator, the .NET analyzers, AsyncFixer, and Microsoft.VisualStudio.Threading.Analyzers at build time. Doc-comment warnings (including `CS1591`) are treated as errors.
- **Deterministic builds** produce reproducible package outputs.
- **Documentation-first.** Every public type and member carries XML documentation in British English, and that documentation is the source of truth for this site. The API reference you see here is generated directly from the source.
- **MIT licensed**, no external runtime dependencies.

## Testing and conventions

The solution uses **MSTest** with a partial-class test layout that mirrors the source layout one-to-one. Test methods follow the naming convention `<MethodOrProperty>_When<Condition>[_For<TypedCondition>]_Should<ExpectedResult>` and carry an XML `<summary>` that starts with "Verifies that …". This makes test intent readable directly in the test explorer.

## Where to go next

- **[Getting started](getting-started.md)** — prerequisites, install commands, and a one-minute sample from each library.
- **[API reference](../api/)** — the full auto-generated type-by-type documentation.
- Library overviews: [Bodu.Core](../api/Bodu.Collections.Generic.html) · [Bodu.Security.Cryptography](../api/Bodu.Security.Cryptography.html) · [Bodu.Globalization.Calendar](../api/Bodu.Globalization.Calendar.html).
