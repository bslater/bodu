---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for `Bodu.Core` under
[`samples/Core/`](https://github.com/bslater/bodu/tree/master/samples/Core). All three samples
are **offline and deterministic** — they run against fixed inputs and small committed byte
fixtures — and are members of `bodu.slnx`, built and executed by CI, so the code they show
cannot drift from the current API. Each sample's README documents every scenario individually:
its intent, what the code does, the output to expect, and the APIs demonstrated.

Run any sample from the repository root:

```bash
dotnet run --project samples/Core/<SampleName>
```

## The samples

### Bodu.Core.Samples.FunctionalRailway

The `Bodu.Functional` seam — railway-oriented primitives that replace `null`, out-parameters,
and exception-driven control flow with composable values: <xref:Bodu.Functional.Option`1> for
absence (`Map`/`Bind`/`Filter`/`Match`), <xref:Bodu.Functional.Result> /
<xref:Bodu.Functional.Result`1> for a validate → parse → transform railway that short-circuits
on the first <xref:Bodu.Functional.ResultError>, <xref:Bodu.Functional.Either`2> for a typed
either/or, <xref:Bodu.Functional.Memoizer> caching a pure function (the invocation counter
advances once per distinct argument), and the Task-based async companions
(`MapAsync`/`BindAsync`). *Package: `Bodu.Core`.*

### Bodu.Core.Samples.CoreToolbox

The everyday utility surface: `SequenceGenerator` (Fibonacci, Range, Thue-Morse, Farey, and
more, all bounded), the pooled `PooledBufferBuilder<T>` output builder, the
`Bodu.Collections.Generic.Extensions` LINQ operators (Batch, Windowed, Pairwise, Scan,
RunLengthEncode, Interleave, ZipLongest), the string transforms (slug/kebab/snake/Pascal casing,
diacritic removal) with the comparable and numeric extensions, `WeekPattern` presets and
parse/format, and the deterministic single-flow use of the `Bodu.Threading` async primitives
(`AsyncLazy`, `AsyncManualResetEvent`, `AsyncLock`). *Package: `Bodu.Core`.*

### Bodu.Core.Samples.TextEncoding

The `Bodu.Text` encoding utilities over committed byte fixtures: `EncodingDetection` BOM
sniffing across UTF-8/UTF-16LE/UTF-16BE preambles with an explicit UTF-8 fallback for BOM-less
input, `EncodingExtensions` transcoding between UTF-16 and UTF-8 with preamble handling and
replacement/exception fallbacks, and `StringEncodingExtensions` byte-count probes and pooled
encoding (`ToUtf8Bytes`, `GetUtf8BytesPooled`, `TryEncodeUtf8To`). *Package: `Bodu.Core`.*

## Related

- [Collections samples](collections.md) — the specialized collection catalogue that builds on
  `Bodu.Core`.
- [Numerics samples](numerics.md) — `Fraction<T>`, the interval algebra, and streaming
  statistics.
