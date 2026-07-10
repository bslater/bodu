# Bodu.Text.Configuration.Samples.ConfigCascade

The `Bodu.Text.Configuration` pipeline end to end: an EditorConfig-inspired, INI-backed
configuration model with three separable phases — **parse** (`ConfigurationDocument`),
**resolve** (`.Resolve(targetPath)` → `ConfigurationView`), and **write** (`Save`). Four
scenarios cover diagnostics-collecting parses, the path-targeted cascade, the `unset`
semantics and dialect presets, and rewriting a file without destroying its comments.
Everything runs offline against the committed `Data/sample.boduconfig`.

```bash
dotnet run --project samples/Text.Configuration/Bodu.Text.Configuration.Samples.ConfigCascade
```

## Scenario 1 — ParseAndDiagnostics

**Intent.** Contrast the two parse entry points and when each is right: `Parse`/`Load` throw
on the first structural error (generated files should never be half-read), while
`ParseWithDiagnostics` under the `Relaxed` profile collects every problem as a
`ConfigurationDiagnostic` and still returns a usable document — what an editor or linter
needs to show all errors at once.

**What it does.** Loads the clean committed file, then parses deliberately flawed text (a
property line with no `=`) with `ConfigurationParseOptions.Relaxed`, printing each collected
diagnostic's severity, code, and line number. Note the default (`Bodu`) profile parses with
`DiagnosticMode.Throw`, so the same text under `Parse` would raise
`ConfigurationParseException`.

**What to expect.**

```text
sample.boduconfig: root section + 3 glob sections
flawed text      : document usable = True, diagnostics = 1
  [Error] MissingEquals at line 5: Configuration property line is missing the '=' separator.
```

**APIs demonstrated.** `ConfigurationDocument.Load` / `.ParseWithDiagnostics`,
`ConfigurationParseOptions.Relaxed` (profile presets), `ConfigurationParseResult.Document` /
`.Diagnostics`, `ConfigurationDiagnostic.Severity` / `.Code` / `.Location.LineNumber`.

## Scenario 2 — ResolveCascade

**Intent.** Show the heart of the library: resolution is *per target path*. Every section
whose glob matches the path contributes its keys, later sections overriding earlier ones —
so one file expresses org-wide defaults plus per-tree exceptions, exactly like
`.editorconfig`.

**What it does.** Resolves the same document for three targets: a production source file
(matches `[*]` then `[src/**.cs]`, so it gets the tightened `indent_size = 8` /
`max_line_length = 100`), a test file (matches `[*]` then `[test/**.cs]`, whose
`max_line_length = unset` leaves a value that no longer parses as an int), and a non-`.cs`
file (only `[*]` applies). It then shows the typed getters on one view: `GetEnum<DayOfWeek>`,
`GetBoolean`, and `GetString` with a fallback for an absent key.

**What to expect.**

```text
src/App/Program.cs               indent_size = 8, max_line_length = 100
test/AppTests/ProgramTests.cs    indent_size = 4, max_line_length = (unset)
README.md                        indent_size = 4, max_line_length = 120
typed: start_day = Monday, strict_nullability = True, theme = (default)
```

**APIs demonstrated.** `document.Resolve(targetPath)`, glob-section matching and override
order, `ConfigurationView.GetInt32` / `TryGetInt32` / `GetEnum<T>` / `GetBoolean` /
`GetString(key, fallback)`.

## Scenario 3 — UnsetAndPresets

**Intent.** Explain what a literal `unset` value means — and that it is a *dialect decision*.
Under `TreatAsLiteral` (the default `Bodu` profile) the string `"unset"` is just a value;
under `RemoveEffectiveValue` (EditorConfig semantics) it erases the value inherited from
earlier sections. The canonical option sets (`ConfigurationResolveOptions.Bodu` /
`.EditorConfigCompatible` / `.For(profile)`) switch the whole pipeline's dialect coherently
instead of toggling five options by hand.

**What it does.** Resolves the test-file target under both `UnsetValueMode`s and prints the
effective `max_line_length`, then repeats with the `EditorConfigCompatible` canonical option
set to show it bundles `RemoveEffectiveValue` (the key is simply absent from the view).

**What to expect.**

```text
TreatAsLiteral      : max_line_length = 'unset'
RemoveEffectiveValue: max_line_length = '(absent)'
EditorConfig preset : max_line_length present = False
```

**APIs demonstrated.** `ConfigurationResolveOptions.UnsetValueMode`
(`ConfigurationUnsetValueMode.TreatAsLiteral` / `.RemoveEffectiveValue`),
`ConfigurationResolveOptions.EditorConfigCompatible` / `.For(ConfigurationProfile...)`.

## Scenario 4 — SaveRoundTrip

**Intent.** Show the write phase for tooling that edits config files humans own: the parsed
document *is* the INI model underneath, so existing sections mutate in place; new sections
are appended by composing an `IniDocument` from the existing parts; and `Save` writes the
result with the original comments preserved.

**What it does.** Edits `[src/**.cs]` in place with `SetEntry`, builds a new `[docs/**.md]`
`IniSection`, composes `new IniDocument(document.GlobalSection, document.Sections.Append(...))`,
saves it to a temp file with `ConfigurationDocument.Save`, verifies all three `#` comment
lines survived, and re-loads + re-resolves the saved file to prove the new section is live.

**What to expect.**

```text
saved 437 chars to bodu-sample.boduconfig; comment lines preserved: 3
re-resolved docs/guide/intro.md: max_line_length = 80
```

**APIs demonstrated.** `IniSection.SetEntry` on a resolved section,
`IniSection(name, entries)` / `IniDocument(globalSection, sections)` composition,
`ConfigurationDocument.Save(document, path)`, comment preservation
(`ConfigurationWriteOptions.PreserveComments`, on by default).

## Layout

```text
Bodu.Text.Configuration.Samples.ConfigCascade/
  Program.cs                         # runs the scenarios in order
  Data/sample.boduconfig             # committed input (root + [*] + two glob sections)
  Scenarios/ParseAndDiagnostics.cs
  Scenarios/ResolveCascade.cs
  Scenarios/UnsetAndPresets.cs
  Scenarios/SaveRoundTrip.cs
```

## Related

- `Bodu.Extensions.Configuration.Text.Samples.BridgeHosting` — flowing the same file format
  into `Microsoft.Extensions.Configuration` / `IOptions<T>`.
- `Bodu.Text.Formats.Samples.ConfigFiles` — the plain INI and DotEnv formats, for when you
  don't need path-targeted cascades.
- Guides: `docs/guides/text-configuration/`.
