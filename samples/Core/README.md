# Core Samples

Console applications demonstrating the `Bodu.Core` package. Each sample is a standalone
project; run one with:

```bash
dotnet run --project samples/Core/<SampleName>
```

Every sample is offline and deterministic: fixed inputs (and, for `TextEncoding`, a few small
committed byte fixtures), and every scenario prints the same output on every run.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Core.Samples.FunctionalRailway` | The `Bodu.Functional` seam — `Option<T>` (Map/Bind/Filter/Match) for absence, the `Result`/`Result<T>` validate→parse→transform railway that short-circuits on the first `ResultError`, `Either<,>` for a typed choice, `Memoizer` caching a pure function (counter advances once per distinct argument), and the Task-based async companions | `Bodu.Core` |
| `Bodu.Core.Samples.CoreToolbox` | `SequenceGenerator` bounded sequences, `PooledBufferBuilder<T>`, the `Bodu.Collections.Generic.Extensions` LINQ operators (Batch/Windowed/Pairwise/Scan/RunLengthEncode/…), the string/comparable/numeric extensions, `WeekPattern` presets and parse/format, and the deterministic single-flow use of `AsyncLazy`/`AsyncManualResetEvent`/`AsyncLock` | `Bodu.Core` |
| `Bodu.Core.Samples.TextEncoding` | The `Bodu.Text` utilities — `EncodingDetection` BOM sniffing across UTF-8/UTF-16 preambles with a UTF-8 fallback, `EncodingExtensions` transcoding with preamble handling and replacement/exception fallbacks, and `StringEncodingExtensions` byte-count probes and pooled encoding | `Bodu.Core` |

Each sample project has its own README with the four-part per-scenario breakdown (Intent /
What it does / What to expect / APIs demonstrated).
