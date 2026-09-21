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
--- Parse versus ParseWithDiagnostics ---
  What   : Loads the committed configuration file, then parses deliberately flawed text through the diagnostic entry
           point and prints every problem it collected alongside the document it still returned.
  Why    : These two entry points answer different questions about the same input. A generated or machine-written
           file that does not parse is a bug upstream, and the right response is to stop at the first error with a
           clear exception. A file a person typed is different: stopping at the first error means they fix one line,
           re-run, and discover the next - so the parser collects everything it can and still returns a usable
           document, which is what an editor needs to underline every problem at once. Each diagnostic carries a
           stable code and a line number for exactly that reason.
  Expect : The flawed text yields a usable document rather than nothing, with the unparseable line reported as a
           diagnostic naming its code and line number. The valid lines around it still parsed - partial recovery is
           the point, since a document that collapses on one bad line cannot drive an editor.

  sample.boduconfig: root section + 3 glob sections  (the committed file parses clean, so Load's throw-on-error behaviour is the right entry point for it)
  flawed text      : document usable = True, diagnostics = 1  (expected True - a document comes back despite the error, which is what lets an editor keep working on a file mid-edit)
    [Error] MissingEquals at line 5: Configuration property line is missing the '=' separator.
  (a stable code and a line number per diagnostic - enough for an editor to place a squiggle without re-parsing)
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
--- Resolve(targetPath) - the cascade ---
  What   : Resolves the same document against three target paths - a production source file, a test file, and a
           markdown file - and prints the effective values each one sees, then reads typed values through the enum,
           boolean and defaulted-string getters.
  Why    : This is the idea the whole library is built around, and it inverts how configuration usually works.
           Rather than a file per directory, one file holds defaults plus glob-targeted exceptions, and a value is
           only meaningful relative to the path you are asking about. Every section whose glob matches contributes,
           with later sections overriding earlier ones, so specificity is expressed by ordering rather than by a
           precedence algorithm nobody can predict. The typed getters exist because the file format has exactly one
           value type - text - and every consumer would otherwise reimplement the same invariant-culture parsing,
           differently.
  Expect : Three paths, three different effective configurations, from one document and no per-directory files. The
           test file picks up an override the source file does not, and the markdown file falls through to the
           defaults because no .cs glob matches it. The typed row shows a value absent from the file resolving to
           its supplied default rather than throwing.

  src/App/Program.cs               indent_size = 8, max_line_length = 100
  test/AppTests/ProgramTests.cs    indent_size = 4, max_line_length = (unset)
  README.md                        indent_size = 4, max_line_length = 120
  (one document, three effective configurations - the path is the input, and section order decides which override wins)
  typed: start_day = Monday, strict_nullability = True, theme = (default)  (every wire value is text; the getters parse with invariant culture, and 'theme' falls back because the file does not set it)
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
--- The 'unset' value and dialect presets ---
  What   : Resolves the same test-tree path twice with opposite unset-value modes, then resolves it once more
           through the EditorConfig-compatible preset.
  Why    : A cascade can only add values, which leaves no way to say a deeper section should stop inheriting one -
           so EditorConfig gives the literal text 'unset' that meaning. It is a genuine dialect decision rather than
           an obvious one: a file that legitimately wants the string "unset" as a value needs the other mode, so the
           library makes it explicit instead of guessing. Presets exist because a dialect is not one switch but
           several that have to agree; setting them individually is how a configuration reader ends up
           almost-compatible with the format it claims to read.
  Expect : The same key, the same path, two different answers - literal text under one mode, absent under the other.
           The preset then reports the key as absent without naming any individual switch, because it carries the
           whole EditorConfig dialect including this one.

  TreatAsLiteral      : max_line_length = 'unset'  (the word is just a value here - the mode a file that genuinely stores the text "unset" needs)
  RemoveEffectiveValue: max_line_length = '(absent)'  (EditorConfig semantics - the key is gone from the view, which is the only way a cascade can un-inherit)
  EditorConfig preset : max_line_length present = False  (expected False - the preset carries the whole dialect, so the switches cannot drift out of agreement)
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
--- Saving - mutate, append, write, and re-resolve ---
  What   : Edits a value in an existing section, composes a new document with an extra section appended, saves it,
           counts the comment lines that survived, and resolves a path from the written file.
  Why    : A configuration file a program writes back is usually a file a person also edits, and the failure mode is
           a tool that reformats or silently strips the comments explaining why a setting is there. This document
           model is trivia-preserving underneath, so an edit changes the value it was asked to change and leaves
           everything else - including comments and spacing - as the author wrote it. The read-only surface
           deliberately does not grow sections: adding one is a structural change, so it goes through composing a
           document rather than mutating in place, which keeps accidental structural edits out of the common path.
  Expect : The comment lines are still in the saved file - the edit touched one value, not the formatting around it.
           Re-loading the written file and resolving a path that only the appended section matches proves the round
           trip produced a file the parser accepts, not just text that looks right.

  saved 437 chars to bodu-sample.boduconfig; comment lines preserved: 3  (the comments the author wrote survive the edit - a config writer that strips them is a config writer people turn off)
  re-resolved docs/guide/intro.md: max_line_length = 80  (the value comes from the appended section, so the written file really parses - not just looks right)
```

**APIs demonstrated.** `IniSection.SetEntry` on a resolved section,
`IniSection(name, entries)` / `IniDocument(globalSection, sections)` composition,
`ConfigurationDocument.Save(document, path)`, comment preservation
(`ConfigurationWriteOptions.PreserveComments`, on by default).

## Layout

```text
Bodu.Text.Configuration.Samples.ConfigCascade/
  Program.cs                         # runs the scenarios in order
  SampleConsole.cs                   # the What / Why / Expect scenario banner
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
