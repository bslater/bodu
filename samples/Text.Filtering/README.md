# Text.Filtering Samples

Console applications demonstrating the `Bodu.Text.Filtering` include/exclude text filtering
engine. Each sample is a standalone project; run one with:

```bash
dotnet run --project samples/Text.Filtering/<SampleName>
```

Every sample is pure computation over fixed in-code corpora — offline and deterministic, no data
files.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Text.Filtering.Samples.FilteringTour` | Include/exclude sets (`AnyMatch`) with the include-all default, gitignore-convention `TextFilter.Parse` and `LastMatchWins` ordered rules (re-inclusion, leading-`!*` allowlists), the glob grammar (character classes, `{a,b}` alternation, escapes) with `Evaluate`/`GetMatchingPatterns` deciding-pattern diagnostics, and the telemetry surface (`GetStatistics` counters, per-pattern hit counts, an `ITextFilterObserver` veto logger) | `Bodu.Text.Filtering` |
