# Text.Toml Samples

Console applications demonstrating the `Bodu.Text.Toml` package. Each sample is a standalone
project; run one with:

```bash
dotnet run --project samples/Text.Toml/<SampleName>
```

Every sample is offline and deterministic: the inputs are small committed `Data/*.toml` files,
and every scenario prints the same output on every run.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Text.Toml.Samples.TomlBasics` | The `TomlSerializer` POCO surface — file→POCO→TOML round trips over nested tables and arrays of tables, TOML's four native temporal kinds mapping to `DateOnly`/`TimeOnly`/`DateTimeOffset`, naming policies layered under `[TomlPropertyName]`/`[TomlIgnore]`/`[TomlRequired]` with `TomlStringEnumConverter`, and the `SpecVersion` / `ByteArrayHandling` wire knobs | `Bodu.Text.Toml` |
| `Bodu.Text.Toml.Samples.TomlDocuments` | The layers beneath the serializer — the mutable `TomlNode` DOM (edit in place, graft tables, re-emit), the read-only `TomlDocument`/`TomlElement` DOM (typed getters, enumeration, probing), the `Utf8TomlWriter`/`Utf8TomlReader` token surface, and streaming reads across buffer boundaries via `TomlReaderState` | `Bodu.Text.Toml` |
