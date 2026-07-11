# Bodu.Text.Formats.Samples.ConfigFiles

The two config-file document formats in `Bodu.Text.Formats`: INI (`Bodu.Text.Ini`) and DotEnv
(`Bodu.Text.DotEnv`). Four scenarios cover reading typed values out of both formats, the INI
edit loop where a tool rewrites a file a human still owns (comments intact), DotEnv's
deliberately literal value contract, and the streaming DotEnv reader. Everything runs offline
against the committed `Data/app.ini` and `Data/env.sample`.

> Note on namespaces: as with the DelimitedData sample, the root namespace is
> `Bodu.Samples.Text.Formats.ConfigFiles` — under `Bodu.Text`, the simple names `Ini` and
> `DotEnv` would resolve to the `Bodu.Text.Ini` / `Bodu.Text.DotEnv` *namespaces* instead of
> the facade classes.

```bash
dotnet run --project samples/Text.Formats/Bodu.Text.Formats.Samples.ConfigFiles
```

## Scenario 1 — IniReadTypedValues

**Intent.** Show the INI read model: keys before the first `[section]` live in a global
section, named sections carry the rest, values come back typed via the same
`ISpanParsable`/`InvariantCulture` getters as the other formats, and comment lines are
preserved on the entry they precede — the document is a faithful model of the file, not just
a key/value bag.

**What it does.** Parses `Data/app.ini`, reads the global `environment` key, lists the
sections, pulls `[server]` values with `GetValue<int>`, probes an absent key with
`TryGetValue`, and reads the leading comments attached to two entries. (The parser
deliberately treats `;` after a value as value content — values may contain semicolons —
so inline comments are a *write-side* feature; see Scenario 2.)

**What to expect.**

```text
global      : environment = production
sections    : server, logging
[server]    : 0.0.0.0:8080 (timeout 30s)
[logging]   : level = information, retention_days present = False
comments    : 'host' leading comment = 'Bind address and port for the listener.'
            : 'path' leading comment = 'Rolling file sink settings.'
```

**APIs demonstrated.** `Ini.Parse`, `IniDocument.GlobalSection` / `.Sections` /
`.GetSection`, section indexer, `IniSection.GetValue<T>` / `TryGetValue<T>`,
`IniEntry.LeadingComments`.

## Scenario 2 — IniMutateAndFormat

**Intent.** Show the edit loop for tooling that rewrites config files humans maintain: parse,
change values, add entries and sections, format back — with every comment from the original
file surviving, so the tool's write doesn't destroy the file's documentation.

**What it does.** Edits the parsed document three ways — `SetEntry` overwrites
`[server] port`, adds `retention_days` to `[logging]`, and `GetOrAddSection` creates a whole
new `[metrics]` section — then attaches an `IniComment` to a new entry's `InlineComment`
(the write-side inline-comment surface) and emits the document with `Ini.Format`.

**What to expect.** The full edited file with all four original comment lines still in place,
the changed port, the appended entries, the new section, and the emitted inline comment:

```text
  | [metrics]
  | enabled = true ; scrape target
  | endpoint = /metrics
comment lines preserved: 4
```

**APIs demonstrated.** `IniSection.SetEntry`, `IniDocument.GetOrAddSection`,
`IniEntry.InlineComment` + `IniComment(prefix, text)`, `Ini.Format`, comment round-tripping
(`IniParseOptions.PreserveComments`, on by default).

## Scenario 3 — DotEnvBasics

**Intent.** Show DotEnv's parse model and its key design constraint: values are returned
*literally*. No `${VAR}` interpolation happens at parse time — what the file says is exactly
what your process gets, and any expansion is the consumer's explicit, auditable decision.

**What it does.** Parses `Data/env.sample` and walks the notable shapes: an
`export`-prefixed key (the prefix is shell syntax, stripped from the key), a double-quoted
URL and single-quoted string (quotes delimit, they are not content), an inline `#` comment
dropped from the value, an empty value that is distinct from an absent key (`null` from the
indexer), and a typed `GetValue<int>` read.

**What to expect.**

```text
entries      : 6
export prefix: APP_ENV = production
double-quoted: DATABASE_URL = postgres://app:placeholder@localhost:5432/app_db
single-quoted: GREETING = Hello, operator
inline commt : FEATURE_FLAGS = 'search,exports'
empty vs null: EMPTY_VALUE = '', MISSING = null
typed        : APP_PORT + 1 = 8081
```

**APIs demonstrated.** `DotEnv.Parse`, `DotEnvDocument` indexer (null for absent),
`GetValue<T>`, `DotEnvParseOptions.AllowExportPrefix` / `.AllowInlineComments` (both on by
default), the no-interpolation contract.

## Scenario 4 — DotEnvStreamingReader

**Intent.** Show the forward-only `DotEnvReader` for scanning env files without building a
document — one entry in memory at a time, each carrying its source line number, which is
exactly what a linter or secrets-scanner wants to report.

**What it does.** Streams `Data/env.sample` through `DotEnvReader.Read()`, printing each
`Key` / `Value` with its `LineNumber`, and flags URL-shaped keys as a miniature lint pass.

**What to expect.**

```text
  line  3: APP_ENV = 'production'
  line  4: APP_PORT = '8080'
  line  5: DATABASE_URL = '...'  <- check for embedded credentials
  ...
```

**APIs demonstrated.** `DotEnvReader(TextReader)`, `.Read()` / `.Key` / `.Value` /
`.LineNumber`, `IDisposable` streaming.

## Layout

```text
Bodu.Text.Formats.Samples.ConfigFiles/
  Program.cs                            # runs the scenarios in order
  Data/app.ini                          # committed INI input (global + 2 sections + comments)
  Data/env.sample                       # committed .env input (placeholders only, no secrets)
  Scenarios/IniReadTypedValues.cs
  Scenarios/IniMutateAndFormat.cs
  Scenarios/DotEnvBasics.cs
  Scenarios/DotEnvStreamingReader.cs
```

## Related

- `Bodu.Text.Formats.Samples.DelimitedData` — the RFC 4180 CSV/TSV surface from the same
  package.
- `Bodu.Text.Configuration` samples (`samples/Text.Configuration/`) — the richer
  `.boduconfig` cascade format, for when INI-style files need profiles and path-targeted
  resolution.
- Guides: `docs/guides/formats/`.
