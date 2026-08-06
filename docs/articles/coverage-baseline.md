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
| `Bodu.Financial.ExchangeRates` | Preview | 88.8% | 81.1% | 324 / 365 |
| `Bodu.Financial.ExchangeRates.Boe` | Stable | 91.1% | 85.3% | 267 / 293 |
| `Bodu.Financial.ExchangeRates.Caching` | Stable | 91.6% | 88.6% | 1368 / 1493 |
| `Bodu.Financial.ExchangeRates.Caching.Distributed` | Stable | 93.5% | 91.9% | 172 / 184 |
| `Bodu.Financial.ExchangeRates.Caching.Sqlite` | Stable | 94.8% | 94.8% | 308 / 325 |
| `Bodu.Financial.ExchangeRates.DependencyInjection` | Stable | 100% | 100% | 70 / 70 |
| `Bodu.Financial.ExchangeRates.Ecb` | Stable | 87.4% | 79.4% | 242 / 277 |
| `Bodu.Financial.ExchangeRates.ExchangeRateHost` | Preview | 95.3% | 73.7% | 143 / 150 |
| `Bodu.Financial.ExchangeRates.Fixer` | Preview | 96.7% | 75% | 147 / 152 |
| `Bodu.Financial.ExchangeRates.Fred` | Preview | 99.3% | 93.5% | 135 / 136 |
| `Bodu.Financial.ExchangeRates.Imf` | Preview | 82.7% | 82.7% | 263 / 318 |
| `Bodu.Financial.ExchangeRates.Oanda` | Stable | 97% | 89.2% | 164 / 169 |
| `Bodu.Financial.ExchangeRates.Ofx` | Stable | 97.6% | 88.9% | 124 / 127 |
| `Bodu.Financial.ExchangeRates.Rba` | Stable | 94.4% | 89.5% | 303 / 321 |
| `Bodu.Financial.ExchangeRates.Xe` | Stable | 95.4% | 86.6% | 228 / 239 |
| `Bodu.Financial.ExchangeRates.Yahoo` | Stable | 96% | 73.1% | 143 / 149 |
| `Bodu.Financial.Serialization.Json` | Stable | 95% | 89.4% | 551 / 580 |
| `Bodu.Formats.Excel.Binary` | Stable | 89.7% | 81.7% | 708 / 789 |
| `Bodu.Formats.Outlook` | Preview | 95.9% | 88.9% | 71 / 74 |
| `Bodu.Formats.Outlook.Msg` | Preview | 86% | 79.5% | 478 / 556 |
| `Bodu.Globalization.Calendar` | Stable | 97% | 89.6% | 3370 / 3474 |
| `Bodu.Globalization.Calendar.Africa` | Stable | 100% | 66.7% | 26 / 26 |
| `Bodu.Globalization.Calendar.Americas` | Stable | 100% | 83.3% | 29 / 29 |
| `Bodu.Globalization.Calendar.AsiaPacific` | Stable | 100% | 75% | 16 / 16 |
| `Bodu.Globalization.Calendar.Build` | Preview | — | — | — |
| `Bodu.Globalization.Calendar.Builder` | Stable | 95.7% | 88% | 1692 / 1768 |
| `Bodu.Globalization.Calendar.Caching` | Stable | 89.1% | 84.2% | 608 / 682 |
| `Bodu.Globalization.Calendar.Caching.Distributed` | Stable | 89.5% | 93.2% | 111 / 124 |
| `Bodu.Globalization.Calendar.Caching.Sqlite` | Stable | 93.2% | 95.8% | 193 / 207 |
| `Bodu.Globalization.Calendar.DependencyInjection` | Stable | 95.7% | 66.7% | 90 / 94 |
| `Bodu.Globalization.Calendar.Europe` | Stable | 100% | 83.3% | 30 / 30 |
| `Bodu.Globalization.Calendar.MiddleEast` | Stable | 100% | 66.7% | 26 / 26 |
| `Bodu.Globalization.Calendar.Plugins` | Stable | 92% | 82.7% | 208 / 226 |
| `Bodu.Globalization.Calendar.Tool` | Preview | 99.2% | 98.4% | 130 / 131 |
| `Bodu.Globalization.Recurrence` | Preview | 88.8% | 87.9% | 1278 / 1439 |
| `Bodu.IO.Compound` | Stable | 98.8% | 94.1% | 2220 / 2247 |
| `Bodu.IO.Hashing` | Stable | 98.7% | 96% | 2518 / 2551 |
| `Bodu.IO.Hashing (shared source)` | Stable | 100% | 100% | 48 / 48 |
| `Bodu.IO.Pst` | Preview | 79.9% | 64.7% | 326 / 408 |
| `Bodu.Numerics` | Stable | 94.2% | 91% | 2090 / 2219 |
| `Bodu.Numerics.Serialization.Json` | Preview | 92.5% | 83.6% | 494 / 534 |
| `Bodu.Security.Cryptography` | Stable | 98.4% | 94.1% | 13684 / 13912 |
| `Bodu.Text.Bencode` | Stable | 93.1% | 91.7% | 1525 / 1638 |
| `Bodu.Text.Configuration` | Stable | 88.3% | 86.5% | 1048 / 1187 |
| `Bodu.Text.Delimited` | Preview | 89.7% | 84.6% | 806 / 899 |
| `Bodu.Text.DotEnv` | Preview | 82% | 79.5% | 592 / 722 |
| `Bodu.Text.Encoding` | Stable | 95.5% | 93.8% | 3036 / 3178 |
| `Bodu.Text.Filtering` | Preview | 98.3% | 97.7% | 458 / 466 |
| `Bodu.Text.Ini` | Preview | 87.3% | 82.6% | 799 / 915 |
| `Bodu.Text.Serialization` | Stable | 88.9% | 76.9% | 64 / 72 |
| `Bodu.Text.Serialization (shared source)` | Stable | 93.4% | 89% | 606 / 649 |
| `Bodu.Text.Toml` | Stable | 95.5% | 92.8% | 2988 / 3130 |
| `Bodu.Text.Yaml` | Preview | 89.8% | 88.5% | 2410 / 2685 |
| `Caching (shared source)` | Stable | 86.4% | 87.5% | 95 / 110 |

**Overall:** 95.4% (66390 / 69560 lines across 59 collected package(s)).

## Baseline evidence

```bash
bld/collect-coverage.sh
bld/merge-coverage.sh
pwsh tools/New-CoverageMatrix.ps1
```

- Commit: `b4c9ce28b2a0317b55e77b352225cef8c8fd6b3b`
- `Avx512F.IsSupported` on the collecting host: `true`
- Phantom rows discarded: 0
- Files with stale line numbering: 0
