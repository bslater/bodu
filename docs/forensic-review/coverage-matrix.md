# Coverage Matrix

Every in-scope production package reviewed against the six-step rubric. `✓` = reviewed; `—` = not applicable to that package. Workstream column links each package to its findings file.

Rubric steps: **API** (public-API enumeration) · **Hot** (hot-path trace) · **Weak** (weakness/exploit) · **Arch** (architecture/alignment) · **Dup** (duplication) · **Conv** (convention compliance).

| Package | WS | API | Hot | Weak | Arch | Dup | Conv |
|---|---|:--:|:--:|:--:|:--:|:--:|:--:|
| `Bodu.Security.Cryptography` | WS-1 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.IO.Hashing` | WS-1 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.IO.Compound` | WS-2 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Formats.Excel.Binary` | WS-2 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Text.Bencode` | WS-2/6 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Text.Toml` | WS-2/6 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Text.Yaml` | WS-2/6 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Text.Formats` | WS-2 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Text.Encoding` | WS-2 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Text.Configuration` | WS-2 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Extensions.Configuration.Text` | WS-2 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Collections.Concurrent` | WS-3 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Core` (Threading) | WS-3 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Core` (rest — buffers, extensions, functional, text) | WS-7 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Collections` | WS-7 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.ExchangeRates` (core) | WS-4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.ExchangeRates.Boe` | WS-4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.ExchangeRates.Ecb` | WS-4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.ExchangeRates.Rba` | WS-4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.ExchangeRates.Yahoo` | WS-4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.ExchangeRates.Ofx` | WS-4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.ExchangeRates.Xe` | WS-4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.ExchangeRates.Oanda` | WS-4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.ExchangeRates.Caching` | WS-4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.ExchangeRates.Caching.Sqlite` | WS-4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.ExchangeRates.Caching.Distributed` | WS-4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.ExchangeRates.DependencyInjection` | WS-4/6 | ✓ | — | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Globalization.Calendar.Plugins` | WS-5 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Numerics` | WS-7 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Numerics.Serialization.Json` | WS-7 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Globalization.Calendar` | WS-7 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Globalization.Calendar.Builder` | WS-7 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Globalization.Calendar.DependencyInjection` | WS-7 | ✓ | — | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Globalization.Calendar.Data.Americas` | WS-7 | ✓ | — | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Globalization.Calendar.Data.AsiaPacific` | WS-7 | ✓ | — | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Globalization.Calendar.Data.Europe` | WS-7 | ✓ | — | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Globalization.Calendar.Data.MiddleEast` | WS-7 | ✓ | — | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Globalization.Calendar.Data.Africa` | WS-7 | ✓ | — | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial` | WS-7 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Bodu.Financial.DependencyInjection` | WS-7/6 | ✓ | — | ✓ | ✓ | ✓ | ✓ |

**Notes on depth.** The rubric was applied uniformly (every package opened and checked against all six steps), but review *depth* was risk-weighted per the brief's intent: crypto, untrusted-input parsers, concurrency, network/FS, and the plugin trust boundary received full line-by-line forensic depth; DI shims and calendar data bundles received a structural pass (factory/registration shape checked against siblings — hence `—` under Hot for those pure-registration/data packages, which have no runtime hot path). `Bodu.Core` and `Bodu.Collections` were split across WS-3 (threading) and WS-7 (a lighter structural pass on the remaining surface).

**Baseline evidence.** Production libraries build clean (`dotnet build bodu.slnx -c Release`, 0 production errors); `bld/check-folder-namespace-alignment.sh` passes with no violations; a tree-wide hard-coded-exception grep found 8 literals, of which 5 (all in `Bodu.Text.Yaml`) are real offenders.
