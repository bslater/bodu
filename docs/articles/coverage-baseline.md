# Coverage baseline

Per-package line and branch coverage for every packable Bodu package, computed from a merged
Cobertura report. Regenerate with `pwsh tools/New-CoverageMatrix.ps1`; do not hand-edit.

Legend: `—` = not part of this collection · `n/a` = excluded by design (see [Code coverage strategy](code-coverage.md)).

| Package | Status | Line % | Branch % | Covered / total lines |
|---|---|--:|--:|--:|
| `Bodu.Collections` | Stable | 97.7% | 95.9% | 6028 / 6171 |
| `Bodu.Collections.Concurrent` | Stable | 94.9% | 91.7% | 1009 / 1063 |
| `Bodu.Core` | Stable | 98.6% | 96.4% | 5631 / 5711 |
| `Bodu.Extensions.Configuration.Text` | Stable | 97.3% | 87.5% | 256 / 263 |
| `Bodu.Financial` | Stable | 97.3% | 86.9% | 3600 / 3700 |
| `Bodu.Financial.DependencyInjection` | Stable | 100% | 100% | 43 / 43 |
| `Bodu.Financial.ExchangeRates` | Preview | 91% | 84.8% | 332 / 365 |
| `Bodu.Financial.ExchangeRates.Boe` | Stable | 91.1% | 85.3% | 267 / 293 |
| `Bodu.Financial.ExchangeRates.Caching` | Stable | 91.6% | 88.6% | 1368 / 1493 |
| `Bodu.Financial.ExchangeRates.Caching.Distributed` | Stable | 93.5% | 91.9% | 172 / 184 |
| `Bodu.Financial.ExchangeRates.Caching.Sqlite` | Stable | 94.8% | 94.8% | 308 / 325 |
| `Bodu.Financial.ExchangeRates.DependencyInjection` | Stable | 100% | 100% | 70 / 70 |
| `Bodu.Financial.ExchangeRates.Ecb` | Stable | 91.7% | 81% | 254 / 277 |
| `Bodu.Financial.ExchangeRates.ExchangeRateHost` | Preview | 95.3% | 73.7% | 143 / 150 |
| `Bodu.Financial.ExchangeRates.Fixer` | Preview | 96.7% | 75% | 147 / 152 |
| `Bodu.Financial.ExchangeRates.Fred` | Preview | 99.3% | 93.5% | 135 / 136 |
| `Bodu.Financial.ExchangeRates.Imf` | Preview | 90.9% | 88.5% | 289 / 318 |
| `Bodu.Financial.ExchangeRates.Oanda` | Stable | 97% | 89.2% | 164 / 169 |
| `Bodu.Financial.ExchangeRates.Ofx` | Stable | 97.6% | 88.9% | 124 / 127 |
| `Bodu.Financial.ExchangeRates.Rba` | Stable | 94.4% | 89.5% | 303 / 321 |
| `Bodu.Financial.ExchangeRates.Xe` | Stable | 95.4% | 86.6% | 228 / 239 |
| `Bodu.Financial.ExchangeRates.Yahoo` | Stable | 96% | 73.1% | 143 / 149 |
| `Bodu.Financial.Serialization.Json` | Stable | 95% | 89.4% | 551 / 580 |
| `Bodu.Formats.Excel.Binary` | Stable | 92.6% | 85.4% | 731 / 789 |
| `Bodu.Formats.Outlook` | Preview | 95.9% | 88.9% | 71 / 74 |
| `Bodu.Formats.Outlook.Msg` | Preview | 86% | 79.5% | 478 / 556 |
| `Bodu.Globalization.Calendar` | Stable | 97% | 89.6% | 3370 / 3474 |
| `Bodu.Globalization.Calendar.Africa` | Stable | 100% | 66.7% | 26 / 26 |
| `Bodu.Globalization.Calendar.Americas` | Stable | 100% | 83.3% | 29 / 29 |
| `Bodu.Globalization.Calendar.AsiaPacific` | Stable | 100% | 75% | 16 / 16 |
| `Bodu.Globalization.Calendar.Build` | Preview | n/a | n/a | n/a |
| `Bodu.Globalization.Calendar.Builder` | Stable | 95.7% | 88% | 1692 / 1768 |
| `Bodu.Globalization.Calendar.Caching` | Stable | 91.2% | 86.5% | 622 / 682 |
| `Bodu.Globalization.Calendar.Caching.Distributed` | Stable | 95.2% | 93.2% | 118 / 124 |
| `Bodu.Globalization.Calendar.Caching.Sqlite` | Stable | 93.2% | 95.8% | 193 / 207 |
| `Bodu.Globalization.Calendar.DependencyInjection` | Stable | 95.7% | 66.7% | 90 / 94 |
| `Bodu.Globalization.Calendar.Europe` | Stable | 100% | 83.3% | 30 / 30 |
| `Bodu.Globalization.Calendar.MiddleEast` | Stable | 100% | 66.7% | 26 / 26 |
| `Bodu.Globalization.Calendar.Plugins` | Stable | 92% | 82.7% | 208 / 226 |
| `Bodu.Globalization.Calendar.Tool` | Preview | 99.2% | 98.4% | 130 / 131 |
| `Bodu.Globalization.Recurrence` | Preview | 91.3% | 89.4% | 1314 / 1439 |
| `Bodu.IO.Compound` | Stable | 98.8% | 94.1% | 2220 / 2247 |
| `Bodu.IO.Hashing` | Stable | 98.7% | 96% | 2518 / 2551 |
| `Bodu.IO.Hashing (shared source)` | Stable | 100% | 100% | 48 / 48 |
| `Bodu.IO.Pst` | Preview | 79.9% | 64.7% | 326 / 408 |
| `Bodu.Numerics` | Stable | 94.2% | 91% | 2090 / 2219 |
| `Bodu.Numerics.Serialization.Json` | Preview | 92.5% | 83.6% | 494 / 534 |
| `Bodu.Security.Cryptography` | Stable | 98.4% | 94.1% | 13684 / 13912 |
| `Bodu.Text.Bencode` | Stable | 93.1% | 91.7% | 1525 / 1638 |
| `Bodu.Text.Configuration` | Stable | 95.5% | 94.7% | 1134 / 1187 |
| `Bodu.Text.Delimited` | Preview | 91.7% | 85.9% | 824 / 899 |
| `Bodu.Text.DotEnv` | Preview | 90.3% | 86.7% | 652 / 722 |
| `Bodu.Text.Encoding` | Stable | 95.5% | 93.8% | 3036 / 3178 |
| `Bodu.Text.Filtering` | Preview | 98.3% | 97.7% | 458 / 466 |
| `Bodu.Text.Formats` | Preview | n/a | n/a | n/a |
| `Bodu.Text.Ini` | Preview | 90.7% | 85.3% | 830 / 915 |
| `Bodu.Text.Serialization` | Stable | 100% | 100% | 72 / 72 |
| `Bodu.Text.Serialization (shared source)` | Stable | 93.4% | 89% | 606 / 649 |
| `Bodu.Text.Toml` | Stable | 95.5% | 92.8% | 2988 / 3130 |
| `Bodu.Text.Yaml` | Preview | 90.7% | 88.9% | 2435 / 2685 |
| `Caching (shared source)` | Stable | 92.7% | 93.8% | 102 / 110 |

### Excluded by design

- `Bodu.Globalization.Calendar.Build` — MSBuild task package. The task runs only inside a child dotnet build process, so the collector attached to the test host never sees it; the package ships to tasks/netstandard2.0 and is never referenced at runtime. Its integration tests cover what actually breaks - targets wiring, incrementality and diagnostic propagation - which no in-process unit test can reach.
- `Bodu.Text.Formats` — Umbrella meta-package: references the three format libraries and ships no source of its own.

**Overall:** 96% (66751 / 69560 lines across 59 collected package(s)).

## Baseline evidence

```bash
bld/collect-coverage.sh
bld/merge-coverage.sh
pwsh tools/New-CoverageMatrix.ps1
```

- Commit: `aaa94ff507e05698aaf44867222a17ec3e680ad7`
- `Avx512F.IsSupported` on the collecting host: `true`
- Phantom rows discarded: 0
- Files with stale line numbering: 0
