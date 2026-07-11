# Text.Formats Samples

Console applications demonstrating the `Bodu.Text.Formats` package (RFC 4180 delimited text,
INI, DotEnv). Each sample is a standalone project; run one with:

```bash
dotnet run --project samples/Text.Formats/<SampleName>
```

Every sample is offline and deterministic: the inputs are small committed `Data/` files plus
inline snippets of deliberately dirty text.

> The sample root namespaces sit under `Bodu.Samples.*` rather than `Bodu.Text.*`: from inside
> a namespace under `Bodu.Text`, the simple names `Delimited`, `Ini`, and `DotEnv` resolve to
> the `Bodu.Text.Delimited`/`.Ini`/`.DotEnv` *namespaces* instead of the facade classes. Each
> project README documents the pitfall.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Text.Formats.Samples.DelimitedData` | `Delimited.Parse` with culture-safe `ISpanParsable` typed getters by header name, the `Strict`/`Ragged` field-count and `Throw`/`SkipRecord` malformed-record policies on dirty input, `Format` round trips with selective quoting and CSV→TSV dialect conversion, and the streaming `DelimitedReader`→`DelimitedWriter` filter pipeline | `Bodu.Text.Formats` |
| `Bodu.Text.Formats.Samples.ConfigFiles` | INI — global section, typed values, comment-preserving mutate + `Format` loop, write-side inline comments; DotEnv — literal no-interpolation values, quoting, `export` prefix, empty-vs-absent, and the streaming `DotEnvReader` with line numbers | `Bodu.Text.Formats` |
