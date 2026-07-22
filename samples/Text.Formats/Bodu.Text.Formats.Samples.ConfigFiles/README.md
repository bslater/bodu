# Bodu.Text.Formats.Samples.ConfigFiles

The two config-file formats — **`Bodu.Text.Ini`** and **`Bodu.Text.DotEnv`** (referenced
through the `Bodu.Text.Formats` umbrella). Everything runs offline against the committed
`Data/app.ini` and `Data/env.sample`.

```bash
dotnet run --project samples/Text.Formats/Bodu.Text.Formats.Samples.ConfigFiles
```

## Scenarios

### `IniReadTypedValues`

Reads `app.ini` through the read-only `IniDocument`: global keys (before any `[section]`)
hoist onto the root object; sections follow as nested objects. Then binds the whole file onto
a typed `AppConfig` class with `IniSerializer` and `NamingPolicy.SnakeCaseLower` — a section
POCO for `[server]` (with `int` conversion for `port` / `request_timeout`) and a
`Dictionary<string, string>` for `[logging]`.

### `IniMutateAndFormat`

The edit loop on the mutable, comment-preserving `IniNode` DOM: change `port` in place
(trivia kept), add `retention_days`, add a whole `[metrics]` section with an authored leading
comment, and write back — every comment from the source file survives. Layout is
canonicalized (`key=value`), and inline comments are deliberately not modeled: the dialect
keeps everything after `=` as value content.

### `DotEnvBasics`

`env.sample` through the read-only `DotEnvDocument`: the `export` prefix binds as the bare
key, double/single quotes delimit without becoming content, the inline `#` comment is dropped
from an unquoted value, and an empty value is distinct from an absent key. Values are
literal — no `${VAR}` interpolation. Typed settings come from
`DotEnvSerializer.Deserialize<Settings>` with the `Web` (SCREAMING_SNAKE_CASE) defaults.

### `DotEnvStreamingReader`

The forward-only `Utf8DotEnvReader` as a miniature lint pass: one token at a time with the
line number attached, flagging keys that look like they carry embedded credentials.
