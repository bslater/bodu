# Bodu — Forensic Code Review

A whole-solution forensic review of the Bodu production codebase: following the public API surface of every package to assess hot paths, weaknesses, and exploitable conditions; evaluating architectural design and pattern alignment across similar types; and identifying consolidatable duplication.

- **Scope reviewed:** every `*/src/` production project in `bodu.slnx` — ~298,000 lines across ~35 packages. Test projects, `samples/`, `Bodu.Test`, `*.Testing`, the independent `Bodu.CodeStyle` solution, `tools/`, `docs/`, and `local-packages/` were excluded per the review brief.
- **Baseline:** the solution's production libraries build clean (`dotnet build bodu.slnx -c Release`, 0 errors in production projects, 421 warnings). The only compile errors are in the out-of-scope `Bodu.Financial/bench/` benchmark project, which references `CurrencyPair`/`RateSeries` under a namespace that has since moved — noted as a housekeeping item, not a library defect.
- **Convention baselines confirmed independently:** `bld/check-folder-namespace-alignment.sh` passes with no violations; a tree-wide grep found only 8 hard-coded exception-string literals in production source, of which the real offenders are the 5 in `Bodu.Text.Yaml` (the rest are doc-comment examples).

## Headline assessment

**The codebase is, on the whole, high quality.** The cryptography, untrusted-input parsers, lock-free concurrency, and financial arithmetic are carefully engineered and largely hold up to adversarial scrutiny. The review confirmed far more hardening than it faulted: constant-time crypto discipline, verify-before-decrypt AEAD, thorough key zeroization, cycle detection on every graph/chain walk, non-configurable parser depth caps, a memoized billion-laughs guard, hardened XML loading, and exact-reconciliation money allocation were all **verified present**, not assumed.

The findings cluster into two themes:

1. **A small number of real defensive/correctness gaps** — none of them a confirmed remotely-exploitable vulnerability, but several worth fixing. The most pressing are a concurrency race in `AsyncDebouncer`, a memory-amplification DoS in the CFB sector-chain reader, a fail-open in an empty plugin trust-policy combinator, and a precision trap in an `ExchangeRate` constructor.
2. **One large, strategic duplication surface** — the Bencode and Toml serializers are near-verbatim copies of one another (several files byte-identical after name-normalization). This is a maintainability-Critical, not a security issue, and is the single highest-value consolidation in the tree.

## Severity tally (actionable findings)

| Severity | Count | Items |
|---|---|---|
| **Critical** (maintainability) | 1 | Bencode↔Toml serializer duplication |
| **High** | 3 | `AsyncDebouncer` CTS dispose race; FX per-provider cache scaffolding triplication; `Bodu.Text.Yaml` hard-coded exception messages |
| **Medium** | 6 | CFB sector-chain allocation amplification (DoS); async cancel-race policy inconsistency; empty `CompositePluginTrustPolicy` fail-open; `LoadFrom(Assembly)` weaker trust guarantee; `ExchangeRate` `isInverted` precision loss; Yaml structural divergence from serializer siblings |
| **Low / Info** | ~14 | Ed25519 duplicated small-order check; Vyukov non-power-of-two slot caveat; RBA/ECB cache-filename path traversal (config-only); Yahoo/OFX unescaped alias→URL; missing HTTP response-size caps; currency case-sensitivity; and other minor items (see workstream files) |

No **Critical or High severity security** finding was confirmed. The one High-severity reliability finding is the `AsyncDebouncer` race.

## Top findings (act on these first)

1. **`AsyncDebouncer` CTS dispose race — High.** `Cancel()` and `Dispose()` can call `CancellationTokenSource.Cancel()` on a source another thread just disposed, throwing `ObjectDisposedException` out of public members. → `03-concurrency.md` #1.
2. **CFB sector-chain allocation amplification — Medium (DoS).** A crafted self-looping FAT/mini-FAT chain is walked ~`FAT.Length` times, writing into a growing `MemoryStream` before the length-based guard trips — up to ~128×/~1024× the input size in intermediate allocation → OOM. Independent of validation level. → `02-parsers.md` #1.
3. **Empty `CompositePluginTrustPolicy` fails open — Medium.** `new CompositePluginTrustPolicy()` (or an empty/all-filtered set) trusts every assembly. → `05-plugin-trust.md` #1.
4. **`ExchangeRate(isInverted: true)` precision trap — Medium.** The public constructor stores a `decimal` reciprocal and later divides by it, losing precision the internal path is written to avoid. → `07-numerics-calendar-financial.md` #1.
5. **Bencode↔Toml serializer duplication — Critical (maintainability).** Extract a shared `Bodu.Text.Serialization` core. → `06-architecture-duplication.md` #1 and `remediation-plan.md`.
6. **FX per-provider cache scaffolding — High (duplication).** Collapse the triplicated `I<X>Cache`/`Null<X>Cache`/wrapper trios onto a generic core cache. → `06-architecture-duplication.md` #2 (with the reconciliation note from `04-network-filesystem.md`).
7. **`Bodu.Text.Yaml` hard-coded exception messages — High (convention).** Five throw sites bypass `YamlResourceStrings`. → `06-architecture-duplication.md` #3.

## Method

Each package was reviewed against a fixed six-step rubric — public-API enumeration → hot-path trace → weakness/exploit assessment → architecture/alignment → duplication → convention compliance — organized into seven risk-lensed workstreams. Every finding is anchored to a real `file:line` and marked **CONFIRMED** (verified by reading the guarding/failing code) or **PLAUSIBLE** (a hypothesis worth confirming before acting). Hypotheses that turned out guarded are recorded as **cleared** notes so the report documents what was checked, not only what failed.

## Report contents

| File | Workstream | Packages |
|---|---|---|
| `01-cryptography.md` | WS-1 | `Bodu.Security.Cryptography`, `Bodu.IO.Hashing` |
| `02-parsers.md` | WS-2 | `Bodu.IO.Compound`, `Bodu.Formats.Excel.Binary`, `Bodu.Text.{Bencode,Toml,Yaml,Formats,Encoding,Configuration}` |
| `03-concurrency.md` | WS-3 | `Bodu.Collections.Concurrent`, `Bodu.Core/Threading`, `SingleFlightCoordinator` |
| `04-network-filesystem.md` | WS-4 | `Bodu.Financial.ExchangeRates` core + 7 providers + 3 caching packages |
| `05-plugin-trust.md` | WS-5 | `Bodu.Globalization.Calendar.Plugins` |
| `06-architecture-duplication.md` | WS-6 | cross-cutting (all packages) |
| `07-numerics-calendar-financial.md` | WS-7 | `Bodu.Numerics(.Serialization.Json)`, `Bodu.Globalization.Calendar(+bundles)`, `Bodu.Financial(.DI)`, light pass on `Bodu.Core`/`Bodu.Collections` |
| `remediation-plan.md` | — | prioritized backlog with effort and blast-radius |
| `coverage-matrix.md` | — | per-package × rubric-step coverage audit |
