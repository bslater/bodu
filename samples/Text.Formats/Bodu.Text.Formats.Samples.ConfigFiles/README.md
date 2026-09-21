# Bodu.Text.Formats.Samples.ConfigFiles

The two config-file formats — **`Bodu.Text.Ini`** and **`Bodu.Text.DotEnv`** (referenced
through the `Bodu.Text.Formats` umbrella). Everything runs offline against the committed
`Data/app.ini` and `Data/env.sample`.

```bash
dotnet run --project samples/Text.Formats/Bodu.Text.Formats.Samples.ConfigFiles
```

## Scenario 1 — IniReadTypedValues

**Intent.** Show both INI read layers and the dialect decisions between them: the read-only
`IniDocument` for walking a file whose shape you discover, and `IniSerializer` for binding a
file whose shape you know.

**What it does.** Reads a key declared before any `[section]` header (hoisted onto the root),
lists the named sections, reaches into `[server]` by element access, then binds the whole file
onto a typed `AppConfig` — a POCO for `[server]` with `int` conversion, and a
`Dictionary<string, string>` for `[logging]`.

**What to expect.**

```text
--- INI - global keys, named sections, and typed binding ---
  What   : Parses the committed INI file, reads a key that appears before any section header, lists the named
           sections, reaches into one by element access, then binds the whole file onto a settings class - one
           section as a POCO, another as a dictionary.
  Why    : INI has no specification, so a reader's job is largely choosing which of the conflicting conventions to
           honour and saying so. Two choices show up here. Keys that appear before the first header are hoisted onto
           the root rather than dropped or given a synthetic section, which matches how most INI files are actually
           written. And a ';' after a value is treated as value content rather than a comment, because connection
           strings and paths legitimately contain semicolons and silently truncating them is the worse failure. The
           typed layer matters for a different reason: INI values are all strings, so binding is where a port
           becomes an int once rather than at every call site.
  Expect : The global key resolves without naming a section. A section binds to a POCO where its shape is known and
           to a dictionary where its keys are open-ended - both are ordinary properties on the same class, so a file
           can mix the two. The typed port arithmetic works because the value arrived as an int.

  global      : environment = production  (declared before any [section] header, so it hoists onto the root rather than needing a synthetic section name)
  sections    : server, logging
  [server]    : 0.0.0.0:8080  (both values arrive as strings - INI has no types, which is what the typed layer below exists to fix)
  typed       : port = 8080, timeout = 30s  (parsed once at bind time; request_timeout mapped to RequestTimeout by the snake_case naming policy)
  dictionary  : [logging] level = information, keys = 3  (a section whose keys are open-ended binds to a dictionary - the same class can mix POCO and dictionary sections)
```

**APIs demonstrated.** `IniDocument.Parse`, `IniElement.GetProperty` / `EnumerateObject` /
`ValueKind`, `IniSerializer.Deserialize<T>`, `IniSerializerOptions.PropertyNamingPolicy`.

## Scenario 2 — IniMutateAndFormat

**Intent.** Show the edit loop on the mutable, trivia-bearing `IniNode` DOM — the workflow for
tooling that rewrites a config file a human still owns.

**What it does.** Changes `port` in place, adds `retention_days` to an existing section,
appends a `[metrics]` section carrying an authored leading comment, emits the document, and
counts the comment lines that survived.

**What to expect.**

```text
--- INI - editing a file a human still owns ---
  What   : Parses the INI file into the mutable DOM, changes an existing value, adds a key to an existing section,
           appends a new section carrying an authored comment, prints the emitted text, and counts the comment lines
           in the result.
  Why    : Config files that tooling rewrites are usually files a person also edits, and the comments in them are
           the only record of why a setting has the value it does. A DOM that discards trivia makes every automated
           edit destructive, so the fix on Monday erases the explanation written on Friday. This DOM is deliberately
           trivia-bearing for that reason - the one sanctioned deviation from the trivia-free document models its
           sibling formats use - and comments are addressable, so a tool can also explain the entry it just added
           rather than leaving an unexplained value.
  Expect : Every comment from the source file is still in the emitted text, plus the one authored here above the new
           entry. The edited value and the added section appear in place, with the untouched parts of the file
           unchanged - an edit should be a diff of what was asked for, not a reformat.

  edited document:
  | ; Application configuration (sample data).
  | ; The global section holds top-level keys; named sections group the rest.
  | environment=production
  | [server]
  | ; Bind address and port for the listener.
  | host=0.0.0.0
  | port=9090
  | request_timeout=30
  | [logging]
  | level=information
  | ; Rolling file sink settings.
  | path=logs/app.log
  | max_size_mb=64
  | retention_days=14
  | [metrics]
  | ; scrape target
  | enabled=true
  | endpoint=/metrics
  comment lines emitted: 5  (the source file's comments plus the one authored above [metrics].enabled - an automated edit that erases them is a destructive edit)
```

**APIs demonstrated.** `IniNode.Parse`, `AsObject` / `AsValue`, `IniValue.LeadingComments`,
`IniNode.ToUtf8Bytes`.

## Scenario 3 — DotEnvBasics

**Intent.** Show what a `.env` reader must decide, given that the format looks like shell but
is not shell.

**What it does.** Reads an exported key, a double-quoted value, a single-quoted one, a value
followed by an inline comment, and an empty value — then binds the file onto a typed settings
class with the `Web` (SCREAMING_SNAKE_CASE) defaults.

**What to expect.**

```text
--- DotEnv - literal values, quoting, and the export prefix ---
  What   : Parses the committed env file and reads an exported key, a double-quoted value, a single-quoted one, a
           value followed by an inline comment, and an empty value - then binds the same file onto a typed settings
           class.
  Why    : A .env file looks like shell but is not shell, and the gap is where the bugs live. This reader returns
           values literally: a ${VAR} reference stays the six characters it is, because interpolating at parse time
           would mean a config file could read the process environment, and which variables were in scope would
           decide what the file meant. Quotes are delimiters rather than content, an 'export' prefix is shell syntax
           rather than part of the key, and a trailing comment is not part of the value. The empty-versus-absent
           distinction is the other one worth knowing: an empty value is a value, and a consumer that treats it as
           unset will silently substitute a default the author explicitly overrode.
  Expect : The exported key binds without its prefix, quotes do not appear in the values, and the inline comment is
           gone. The empty value reads as an empty string while a genuinely missing key reports absent - two
           different answers that a naive reader collapses into one. The typed row shows the SCREAMING_SNAKE_CASE
           policy mapping APP_PORT onto an int property.

  entries      : 6
  export prefix: APP_ENV = production  (written as 'export APP_ENV=...' - the prefix is shell syntax and is not part of the key)
  double-quoted: DATABASE_URL = postgres://app:placeholder@localhost:5432/app_db
  single-quoted: GREETING = Hello, operator  (the quotes delimit the value, they are not content)
  inline commt : FEATURE_FLAGS = 'search,exports'  (the trailing comment is stripped, and the value is not trimmed of meaning beyond it)
  empty vs null: EMPTY_VALUE = '', MISSING present = False  (an empty value is a value - collapsing it into 'absent' would silently restore a default the author overrode)
  typed        : APP_PORT + 1 = 8081  (arithmetic works because the Web preset's SCREAMING_SNAKE_CASE policy bound APP_PORT onto an int property)
```

**APIs demonstrated.** `DotEnvDocument.Parse`, `DotEnvElement.GetProperty` /
`TryGetProperty` / `EnumerateObject`, `DotEnvSerializer.Deserialize<T>`,
`DotEnvSerializerDefaults.Web`.

## Scenario 4 — DotEnvStreamingReader

**Intent.** Show the forward-only reader in its natural role: saying something *about* a file
rather than reading values *out of* it.

**What it does.** Streams the file one token at a time, holding each key until its value token
arrives, and runs a miniature lint pass flagging keys whose names suggest embedded
credentials — reporting the source line for each entry.

**What to expect.**

```text
--- DotEnv - the forward-only reader with line numbers ---
  What   : Streams the same env file one token at a time, holding each key until its value token arrives, and runs a
           small lint pass that flags keys whose names suggest they may carry credentials.
  Why    : The document DOM is the right surface when you want the values; this one is right when you want to say
           something about the file itself. A linter, a secret scanner or a migration tool needs the source line to
           report against, and that is exactly what a materialized document throws away - by the time you have a
           dictionary, the file is gone. Reading forward-only also means nothing larger than one token is held,
           which is what makes scanning a directory of env files cheap rather than proportional to their combined
           size.
  Expect : Each entry is reported with the line it came from, which is what a diagnostic needs to be actionable. The
           key and value arrive as separate tokens, so the loop holds the key across the read - that is the shape of
           every forward-only reader, and why the line number is captured with the key rather than with the value.

  line  4: APP_ENV = 'production'
  line  5: APP_PORT = '8080'
  line  6: DATABASE_URL = 'postgres://app:placeholder@localhost:5432/app_db'  <- check for embedded credentials
  line  7: FEATURE_FLAGS = 'search,exports'
  line  8: GREETING = 'Hello, operator'
  line  9: EMPTY_VALUE = ''
  (the line number comes from the reader, not reconstructed - a materialized document would have discarded it)
```

**APIs demonstrated.** `Utf8DotEnvReader.Read` / `TokenType` / `GetString` / `LineNumber`,
`DotEnvTokenType`.

## Layout

```text
Bodu.Text.Formats.Samples.ConfigFiles/
  Program.cs                        # runs the scenarios in order
  SampleConsole.cs                  # the What / Why / Expect scenario banner
  Data/app.ini                      # committed INI input (global keys + two sections)
  Data/env.sample                   # committed .env input (export, quoting, empty value)
  Scenarios/IniReadTypedValues.cs
  Scenarios/IniMutateAndFormat.cs
  Scenarios/DotEnvBasics.cs
  Scenarios/DotEnvStreamingReader.cs
```

## Related

- `Bodu.Text.Formats.Samples.DelimitedData` — the delimited half of the umbrella package.
- Guides: `docs/guides/text-formats/`.
