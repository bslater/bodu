# Text.Formats Samples

Console applications demonstrating the Bodu line-format libraries — `Bodu.Text.Delimited`,
`Bodu.Text.DotEnv`, and `Bodu.Text.Ini` — referenced through the `Bodu.Text.Formats` umbrella
package. Each sample is a standalone project; run one with:

```bash
dotnet run --project samples/Text.Formats/<SampleName>
```

Every sample is offline and deterministic: the inputs are small committed `Data/` files plus
inline snippets of deliberately dirty text.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Text.Formats.Samples.DelimitedData` | `DelimitedDocument` header-keyed access, typed records via `DelimitedSerializer` with the snake_case naming policy, the `Strict`/`Ragged` field-count and `Throw`/`SkipRecord` malformed-record policies on dirty input, `DelimitedNode` DOM round trips with CSV→TSV dialect conversion, and a `Utf8DelimitedReader`→`Utf8DelimitedWriter` token filter pipeline | `Bodu.Text.Formats` (umbrella) |
| `Bodu.Text.Formats.Samples.ConfigFiles` | INI — hoisted globals and section objects via `IniDocument`, typed binding via `IniSerializer`, and the comment-preserving `IniNode` mutate + write loop; DotEnv — literal no-interpolation values, quoting, `export` prefix, empty-vs-absent, typed settings via `DotEnvSerializer`, and the streaming `Utf8DotEnvReader` with line numbers | `Bodu.Text.Formats` (umbrella) |
