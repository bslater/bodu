# Coverage baseline

Per-package line and branch coverage for every packable Bodu package, computed from a merged
Cobertura report. Regenerate with `pwsh tools/New-CoverageMatrix.ps1`; do not hand-edit.

Legend: `-` = not part of this collection · `n/a` = excluded by design (see [Code coverage strategy](code-coverage.md)).

| Package | Status | Line % | Branch % | Covered / total lines |
|---|---|--:|--:|--:|
| `Bodu.Collections` | Stable | 97.7% | 95.9% | 6030 / 6175 |
| `Bodu.Collections.Concurrent` | Stable | 97.6% | 94.8% | 1345 / 1378 |
| `Bodu.Core` | Stable | 99% | 96.7% | 6355 / 6422 |
| `Bodu.Extensions.Configuration.Text` | Stable | 100% | 97.5% | 293 / 293 |
| `Bodu.Financial` | Stable | 97.4% | 87.7% | 3713 / 3812 |
| `Bodu.Financial.DependencyInjection` | Stable | 100% | 100% | 43 / 43 |
| `Bodu.Financial.ExchangeRates` | Stable | 91.7% | 89% | 400 / 436 |
| `Bodu.Financial.ExchangeRates (shared source)` | Stable | 95.7% | 91.1% | 133 / 139 |
| `Bodu.Financial.ExchangeRates.Boe` | Stable | 98.6% | 92.3% | 272 / 276 |
| `Bodu.Financial.ExchangeRates.Caching` | Stable | 92.9% | 89.9% | 1392 / 1499 |
| `Bodu.Financial.ExchangeRates.Caching.Distributed` | Stable | 93.5% | 91.9% | 173 / 185 |
| `Bodu.Financial.ExchangeRates.Caching.Sqlite` | Stable | 95.4% | 94.8% | 310 / 325 |
| `Bodu.Financial.ExchangeRates.DependencyInjection` | Stable | 100% | 100% | 75 / 75 |
| `Bodu.Financial.ExchangeRates.Ecb` | Stable | 100% | 100% | 301 / 301 |
| `Bodu.Financial.ExchangeRates.ExchangeRateHost` | Stable | 96.5% | 75.7% | 137 / 142 |
| `Bodu.Financial.ExchangeRates.Fixer` | Stable | 97.9% | 77.3% | 141 / 144 |
| `Bodu.Financial.ExchangeRates.Fred` | Stable | 99.3% | 93.5% | 135 / 136 |
| `Bodu.Financial.ExchangeRates.Imf` | Experimental | 95.8% | 93.5% | 339 / 354 |
| `Bodu.Financial.ExchangeRates.Oanda` | Experimental | 99.4% | 98.4% | 156 / 157 |
| `Bodu.Financial.ExchangeRates.Ofx` | Experimental | 100% | 97.8% | 126 / 126 |
| `Bodu.Financial.ExchangeRates.Rba` | Stable | 99.7% | 97.4% | 331 / 332 |
| `Bodu.Financial.ExchangeRates.Xe` | Experimental | 95.8% | 92.1% | 230 / 240 |
| `Bodu.Financial.ExchangeRates.Yahoo` | Experimental | 98.9% | 90.8% | 177 / 179 |
| `Bodu.Financial.Serialization.Json` | Stable | 95.1% | 89.5% | 562 / 591 |
| `Bodu.Formats.Excel.Binary` | Stable | 96.4% | 93.9% | 623 / 646 |
| `Bodu.Formats.Outlook` | Stable | 100% | 100% | 106 / 106 |
| `Bodu.Formats.Outlook (shared source)` | Stable | 97.2% | 95.1% | 310 / 319 |
| `Bodu.Formats.Outlook.Msg` | Stable | 94.8% | 93.5% | 436 / 460 |
| `Bodu.Formats.Outlook.Pst` | Stable | 94% | 88.5% | 489 / 520 |
| `Bodu.Globalization.Calendar` | Stable | 98.1% | 93.7% | 4082 / 4162 |
| `Bodu.Globalization.Calendar.Africa` | Stable | 100% | 66.7% | 26 / 26 |
| `Bodu.Globalization.Calendar.Americas` | Stable | 100% | 83.3% | 29 / 29 |
| `Bodu.Globalization.Calendar.AsiaPacific` | Stable | 100% | 75% | 16 / 16 |
| `Bodu.Globalization.Calendar.Build` | Preview | n/a | n/a | n/a |
| `Bodu.Globalization.Calendar.Builder` | Stable | 95.9% | 90.2% | 1935 / 2017 |
| `Bodu.Globalization.Calendar.Caching` | Stable | 92.2% | 88% | 661 / 717 |
| `Bodu.Globalization.Calendar.Caching.Distributed` | Stable | 95.2% | 93.2% | 118 / 124 |
| `Bodu.Globalization.Calendar.Caching.Sqlite` | Stable | 93.2% | 95.8% | 193 / 207 |
| `Bodu.Globalization.Calendar.DependencyInjection` | Stable | 95.7% | 66.7% | 90 / 94 |
| `Bodu.Globalization.Calendar.Europe` | Stable | 100% | 83.3% | 30 / 30 |
| `Bodu.Globalization.Calendar.MiddleEast` | Stable | 100% | 66.7% | 26 / 26 |
| `Bodu.Globalization.Calendar.Plugins` | Stable | 100% | 86.7% | 226 / 226 |
| `Bodu.Globalization.Calendar.Tool` | Preview | 99.2% | 98.4% | 130 / 131 |
| `Bodu.Globalization.Recurrence` | Stable | 95.9% | 94.6% | 1942 / 2025 |
| `Bodu.IO.Biff` | Stable | 99.7% | 97.8% | 1154 / 1158 |
| `Bodu.IO.Compound` | Stable | 99.1% | 95.6% | 2489 / 2512 |
| `Bodu.IO.Hashing` | Stable | 98.7% | 96% | 2519 / 2552 |
| `Bodu.IO.Hashing (shared source)` | Stable | 100% | 100% | 48 / 48 |
| `Bodu.IO.Pst` | Stable | 97.7% | 96.1% | 1254 / 1283 |
| `Bodu.Numerics` | Stable | 94.5% | 91.7% | 2171 / 2297 |
| `Bodu.Numerics.Serialization.Json` | Stable | 92.6% | 84.1% | 501 / 541 |
| `Bodu.Security.Cryptography` | Stable | 97.6% | 93.3% | 20787 / 21289 |
| `Bodu.Text.Bencode` | Stable | 94% | 92.1% | 1582 / 1683 |
| `Bodu.Text.Configuration` | Stable | 95.7% | 95% | 1181 / 1234 |
| `Bodu.Text.Delimited` | Stable | 95.2% | 95.3% | 968 / 1017 |
| `Bodu.Text.DotEnv` | Stable | 94.4% | 96.5% | 645 / 683 |
| `Bodu.Text.Encoding` | Stable | 95.7% | 94.3% | 3009 / 3145 |
| `Bodu.Text.Filtering` | Stable | 98.3% | 97.7% | 458 / 466 |
| `Bodu.Text.Formats` | Stable | n/a | n/a | n/a |
| `Bodu.Text.Ini` | Stable | 95.4% | 94.6% | 879 / 921 |
| `Bodu.Text.Serialization` | Stable | 100% | 100% | 72 / 72 |
| `Bodu.Text.Serialization (line-format shared source)` | Stable | 98.9% | 97.3% | 269 / 272 |
| `Bodu.Text.Serialization (shared source)` | Stable | 99.9% | 97.3% | 921 / 922 |
| `Bodu.Text.Toml` | Stable | 96.5% | 93.8% | 3059 / 3171 |
| `Bodu.Text.Yaml` | Stable | 94.7% | 93.4% | 3160 / 3337 |
| `Caching (shared source)` | Stable | 92.7% | 93.8% | 102 / 110 |

### Excluded by design

- `Bodu.Globalization.Calendar.Build` - MSBuild task package. The task runs only inside a child dotnet build process, so the collector attached to the test host never sees it; the package ships to tasks/netstandard2.0 and is never referenced at runtime. Its integration tests cover what actually breaks - targets wiring, incrementality and diagnostic propagation - which no in-process unit test can reach.
- `Bodu.Text.Formats` - Umbrella meta-package: references the three format libraries and ships no source of its own.

**Overall:** 97% (81865 / 84354 lines across 64 collected package(s)).

## Baseline evidence

```bash
bld/collect-coverage.sh
bld/merge-coverage.sh
pwsh tools/New-CoverageMatrix.ps1
```

- Commit: `8aba55ac47fff4c32862f42f8c004ff587f551c2`
- `Avx512F.IsSupported` on the collecting hosts, by shard: `full` = `true`
- Architecture of the collecting hosts: `x86_64`
- Phantom rows discarded: 0
- Files with stale line numbering: 0

### Hardware-gating decisions

Each package's hardware-dependent files are judged by the shard that collected its own test
project, whatever the other shards' hosts supported: `measured` means that host could execute
them, `excluded` that it could not. A package no manifest names falls back to the fleet-wide
rule, which judges across every collecting host.

- `Bodu.Core` ARM64: excluded, decided by shard `full`, which collected `Bodu.Core.Test` (architecture `x86_64`)
- `Bodu.Core` AVX-512: measured, decided by shard `full`, which collected `Bodu.Core.Test` (`Avx512F.IsSupported` = `true`)
- `Bodu.Security.Cryptography` ARM64: excluded, decided by shard `full`, which collected `Bodu.Security.Cryptography.Test` (architecture `x86_64`)
- `Bodu.Security.Cryptography` AVX-512: measured, decided by shard `full`, which collected `Bodu.Security.Cryptography.Test` (`Avx512F.IsSupported` = `true`)

### Hardware-gated files excluded from the denominator

The host that collected each package's own tests could not execute these implementations -
AVX-512 code without AVX-512, ARM64 code off ARM64 - so the JIT could not select them there,
whatever the other shards saw. They are exercised by the standard known-answer suites on
capable hardware, including the ARM64 CI job, and are **not** a missing-test gap.

- `Bodu.Core/src/Extensions/VectorRotation.AdvSimd.cs`
- `Bodu.Security.Cryptography/src/Security.Cryptography/Argon2Core.AdvSimd.cs`
- `Bodu.Security.Cryptography/src/Security.Cryptography/Blake2sCore.AdvSimd.cs`
- `Bodu.Security.Cryptography/src/Security.Cryptography/Ghash.PmullIsa.cs`
- `Bodu.Security.Cryptography/src/Security.Cryptography/ScryptCore.AdvSimd.cs`
