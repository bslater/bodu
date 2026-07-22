---
title: Parsing and profiles
---

# Parsing and profiles

`Bodu.Text.Configuration` is a self-contained parser / view layer over its own trivia-preserving INI document model. The parser reads INI faithfully; the layer adds parse-time profiles (Bodu, EditorConfig-compatible, Strict, Relaxed), structured diagnostics, and a flattened view-projecting surface that resolves dotted paths and EditorConfig-style glob sections.

This guide covers the *parsing* half — `ConfigurationDocument.Parse`, `ConfigurationParseOptions`, and the four profiles. For the *resolve* half — `Resolve` → `ConfigurationView`, key projection, typed lookup, glob matching — see [Views and resolution](views-and-resolution.md). For the diagnostic catalogue — every `ConfigurationDiagnosticCode` value, what triggers it — see [Diagnostics](diagnostics.md).

## Pattern 1 — parse a string with default options

```csharp
using Bodu.Text.Configuration;

string source = """
appName = MyApp

[Logging]
Level.Default = Information
Level.Microsoft = Warning
""";

ConfigurationDocument document = ConfigurationDocument.Parse(source);
```

`Parse(string)` returns a `ConfigurationDocument` — a first-class type that inherits the library's own read-only `IniDocumentBase` document model. Use the document directly when you only need read access to sections and entries; use [`Resolve`](views-and-resolution.md) when you want path projection and typed lookup.

The default profile is `Bodu` — inline comments only after whitespace, lenient section headers, last-wins on duplicates, throw on the first error. Override the profile per call via `ConfigurationParseOptions`:

```csharp
ConfigurationDocument doc = ConfigurationDocument.Parse(
    source,
    ConfigurationParseOptions.EditorConfigCompatible);
```

## Pattern 2 — load from a file or stream

```csharp
using Bodu.Text.Configuration;

ConfigurationDocument fromFile   = ConfigurationDocument.Load("appsettings.ini");
ConfigurationDocument fromStream = ConfigurationDocument.Load(stream, leaveOpen: true);
ConfigurationDocument fromReader = ConfigurationDocument.Load(new StringReader(source));
```

`Load(string)` reads UTF-8 by default — pass an `Encoding` explicitly when the file is not UTF-8. `Load(Stream)` does not dispose the stream when `leaveOpen: true`. `Load(TextReader)` is for callers that already control the reader's lifetime.

## Pattern 3 — non-throwing parse

```csharp
using Bodu.Text.Configuration;

if (ConfigurationDocument.TryParse(source, out ConfigurationDocument? document))
{
    Configure(document);
}
else
{
    log.Warn("Failed to parse INI");
}
```

`TryParse` returns `false` and sets the document to `null` on the first parse error. Reach for the diagnostic-collecting variant below if you need to know *why*.

## Pattern 4 — collect diagnostics instead of throwing

```csharp
using Bodu.Text.Configuration;

ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics(
    source,
    ConfigurationParseOptions.Relaxed);

foreach (ConfigurationDiagnostic d in result.Diagnostics)
    log.Warn("{Severity} {Code} at {Location}: {Message}",
             d.Severity, d.Code, d.Location, d.Message);

ConfigurationDocument document = result.Document;
```

`ParseWithDiagnostics` always returns a `ConfigurationParseResult` carrying the document and the diagnostic list. With the `Relaxed` profile (which sets `DiagnosticMode = Collect`), the parser keeps going past recoverable errors and reports them all at once — useful for IDE-style validation. With other profiles, the diagnostic list is populated alongside the throw if you catch `ConfigurationParseException`.

## Pattern 5 — write a document back out

```csharp
using Bodu.Text.Configuration;

ConfigurationDocument.Save(document, "out.ini");
ConfigurationDocument.Save(document, stream, leaveOpen: false);
ConfigurationDocument.Save(document, new StreamWriter(path), options: writeOptions);
```

`Save` writes the document through the library's own INI writer with configurable `ConfigurationWriteOptions`. Comment trivia is preserved when the input parse retained it (`PreserveComments: true`, the default).

## The four profiles

`ConfigurationProfile` is the headline behaviour switch. Each profile is a curated set of parse *and* resolve defaults. Materialise a clean starting point for a profile with the `For(profile)` factory that each option type exposes — `ConfigurationParseOptions.For(profile)`, `ConfigurationResolveOptions.For(profile)`, `ConfigurationWriteOptions.For(profile)` — then override individual fields for fine-grained tuning.

| Profile | When to use |
|---|---|
| `Bodu` *(default)* | Bodu's own convention — EditorConfig-like with safer defaults. Use for application configuration authored by your team. |
| `EditorConfigCompatible` | Strict EditorConfig 0.17.2 parity. Use when you are reading `.editorconfig` files that must round-trip through other tooling. |
| `Strict` | Generated-file semantics — duplicates rejected, errors thrown. Use for machine-emitted files where ambiguity is a defect. |
| `Relaxed` | User-authored file semantics — collect diagnostics, last-wins on duplicates. Use for end-user-facing configuration where forgiving parsing matters. |

The full per-profile table:

| Setting | Bodu | EditorConfigCompatible | Strict | Relaxed |
|---|---|---|---|---|
| `InlineCommentMode` | `WhitespaceIntroduced` | `Disabled` | `Disabled` | `WhitespaceIntroduced` |
| `DuplicateKeyMode` | `LastWins` | `LastWins` | `Disallowed` | `LastWins` |
| `DuplicateSectionMode` | `Preserve` | `Preserve` | `Disallowed` | `Preserve` |
| `SectionHeaderMode` | `Lenient` | `Strict` | `Strict` | `Lenient` |
| `DiagnosticMode` | `Throw` | `Throw` | `Throw` | `Collect` |

> [!NOTE]
> The same four profiles also drive *resolve* defaults through <xref:Bodu.Text.Configuration.ConfigurationResolveOptions> — `ApplyPreambleProperties`, `MissingPathRootMode`, and `UnsetValueMode`. Selecting a profile at parse time does not automatically apply its resolve defaults; pass the matching `ConfigurationResolveOptions` (or its `For(profile)` result) to `Resolve` so both halves of the pipeline agree. See [Views and resolution](views-and-resolution.md#pattern-5--resolve-options).

## `ConfigurationParseOptions` field-by-field

Construct a custom option set when none of the four profiles fits. `ConfigurationParseOptions` is a sealed class with `init`-only properties, so an object initialiser builds an immutable, thread-safe instance you can cache and reuse.

### Profile and presets

- `Profile` (`ConfigurationProfile`, default `Bodu`) — selects the cohort of defaults above. Setting individual fields below overrides the profile's choice.

### Comment handling

- `InlineCommentMode` (`ConfigurationInlineCommentMode`, default per profile) —
  - `Disabled` — `#` / `;` in value text is literal (EditorConfig parity).
  - `WhitespaceIntroduced` — `#` / `;` starts a comment only when preceded by whitespace (the safer default).
  - `Always` — `#` / `;` always starts a comment, even mid-token.

### Section headers

- `SectionHeaderMode` (`ConfigurationSectionHeaderMode`, default per profile) —
  - `Lenient` — trailing content after `]` is silently accepted.
  - `Strict` — trailing non-whitespace emits a `TrailingContentAfterSectionHeader` diagnostic.
  - `AllowTrailingInlineComment` — `#` / `;` after `]` is a comment; other trailing content still errors.

### Duplicate handling

- `DuplicateKeyMode` (<xref:Bodu.Text.Configuration.DuplicateKeyPolicy>, default per profile) — `LastWins`, `FirstWins`, or `Disallowed`. See [INI duplicate-key policies](../formats/ini.md#pattern-4--duplicate-policies).
- `DuplicateSectionMode` (<xref:Bodu.Text.Configuration.IniDuplicateSectionBehavior>, default per profile) — `Preserve`, `Merge`, `MergeAdjacent`, or `Disallowed` (`MergeAll` is an alias for `Merge`).

### Diagnostic handling

- `DiagnosticMode` (`ConfigurationDiagnosticMode`, default per profile) —
  - `Throw` — first recoverable error raises `ConfigurationParseException` with a single diagnostic.
  - `Collect` — parser continues past recoverable errors; diagnostics accumulate on the `ConfigurationParseResult`.
  - `Ignore` — diagnostics dropped silently. Non-recoverable errors still throw.

### Trimming and limits

- `TrimKeysAndValues` (`bool`, default `true`) — trim leading / trailing whitespace from parsed keys and values.
- `AllowKeyOnlyProperties` (`bool`, default `false`) — accept a line with no `=` as a key whose value is the empty string.
- `MaxLineLength` (`int`, default `8192`) — line-length cap; exceeding it emits `LineTooLong`.
- `MaxKeyLength` (`int`, default `1024`) — key-length cap; exceeding it emits `KeyTooLong`.

### Key shape and encoding

- `KeyOptions` (`ConfigurationKeyOptions`, default `Default`) — segment-separator characters, dotted-to-colon mapping, case sensitivity. See [Views and resolution](views-and-resolution.md#configurationkey-and-configurationkeyoptions).
- `DefaultEncoding` (`Encoding`, default `Encoding.UTF8`) — encoding used by `Load(string)` and `Load(Stream)` when the source has no BOM.

## Static presets

```csharp
ConfigurationParseOptions.Bodu                    // cached default
ConfigurationParseOptions.EditorConfigCompatible
ConfigurationParseOptions.Strict
ConfigurationParseOptions.Relaxed

ConfigurationParseOptions.For(ConfigurationProfile.Strict)   // data-driven factory
```

The static properties are cached; reach for them rather than constructing fresh option records.

## Worked example — EditorConfig-style parse

```csharp
using Bodu.Text.Configuration;

string editorConfig = """
root = true

[*.cs]
indent_size = 4
end_of_line = crlf

[*.{md,json}]
indent_size = 2
""";

ConfigurationDocument document = ConfigurationDocument.Parse(
    editorConfig,
    ConfigurationParseOptions.EditorConfigCompatible);

Console.WriteLine(document.Sections.Count);              // 2
Console.WriteLine(document.Sections[0].Name);            // "*.cs"
Console.WriteLine(document.Sections[1]["indent_size"]);  // "2"
```

The `EditorConfigCompatible` profile disables inline comments so a `#` inside a glob (`[file_with_#_in_name.cs]`) is not stripped, enforces strict section-header termination, and otherwise behaves identically to the EditorConfig 0.17.2 specification.

To go from the parsed `ConfigurationDocument` to a resolved typed view — including the EditorConfig glob behaviour where `[*.cs]` matches every `.cs` file — see [Views and resolution](views-and-resolution.md).

## Exceptions

- **`ConfigurationParseException`** (derives from `FormatException`) — raised when `DiagnosticMode = Throw` hits the first recoverable error, or any time a non-recoverable error occurs. The primary diagnostic is exposed on `Diagnostic`, every diagnostic gathered before the failure on the `Diagnostics` array, and `Location` forwards to the primary diagnostic's source location (or `ConfigurationSourceLocation.None`).
- **`ArgumentException`** / **`ArgumentNullException`** — input string null or stream not readable. Standard BCL contract.

## Switching profiles at runtime

A profile is not baked into the document — it is chosen per `Parse` call through
the `ConfigurationParseOptions` argument. That makes profile selection an
ordinary runtime decision: read the same source under whichever profile the
context calls for, or re-parse an already-loaded source under a different one.

**Per-environment selection.** Pick a profile from configuration or the host
environment, then pass the matching cached preset:

```csharp
using Bodu.Text.Configuration;

static ConfigurationParseOptions ProfileFor(string environment) => environment switch
{
    "Production"  => ConfigurationParseOptions.Strict,    // machine-emitted; ambiguity is a defect
    "Development" => ConfigurationParseOptions.Relaxed,   // hand-edited; collect diagnostics, keep going
    _             => ConfigurationParseOptions.Bodu,      // the default convention
};

ConfigurationDocument document = ConfigurationDocument.Parse(source, ProfileFor(environment));
```

Reach for the static presets (`ConfigurationParseOptions.Strict`, `.Relaxed`,
`.Bodu`, `.EditorConfigCompatible`) rather than constructing fresh records — they
are cached, and `ConfigurationParseOptions.For(profile)` is the data-driven
factory when the profile itself is a value rather than a literal.

**Sniffing the content.** When the source's dialect is not known up front, a
cheap probe can choose the profile before the real parse. A `root = true`
preamble, for instance, signals an EditorConfig file:

```csharp
bool looksLikeEditorConfig = source
    .AsSpan()
    .TrimStart()
    .StartsWith("root", StringComparison.OrdinalIgnoreCase);

ConfigurationParseOptions options = looksLikeEditorConfig
    ? ConfigurationParseOptions.EditorConfigCompatible
    : ConfigurationParseOptions.Bodu;

ConfigurationDocument document = ConfigurationDocument.Parse(source, options);
```

**Re-parsing under a stricter profile.** Profiles differ in *what they reject*,
not in the data model, so a document parsed leniently can be re-parsed strictly
to validate it without changing how you consume it. A common shape is to ingest
under `Relaxed` (collect every diagnostic, never throw), then re-run under
`Strict` only when you need a hard gate:

```csharp
ConfigurationParseResult lenient = ConfigurationDocument.ParseWithDiagnostics(
    source, ConfigurationParseOptions.Relaxed);

if (lenient.Diagnostics.Length > 0 && deployGate)
{
    // Surface the failures as a single throw under the stricter profile.
    ConfigurationDocument.Parse(source, ConfigurationParseOptions.Strict);
}

ConfigurationDocument document = lenient.Document;
```

**Trade-offs.** Re-parsing pays the parse cost twice and discards the first
document, so reserve it for a deliberate validation step rather than the hot
path. Switching profiles also switches *resolve* defaults bundled with each
profile (duplicate handling, section-header strictness), so a value that resolved
one way under `Bodu` can resolve differently under `EditorConfigCompatible` —
choose the profile once per source and keep the consuming code reading the same
`ConfigurationDocument` surface regardless. For fine-grained control without
swapping the whole cohort, override individual fields on a single options record
instead (see the field-by-field section above).

## When *not* to use `ConfigurationDocument`

- **Tabular data.** Reach for [`Delimited`](../formats/delimited.md).
- **`.env` files.** Reach for [`DotEnv`](../formats/dotenv.md).
- **Strict round-trip fidelity at the byte level.** The codec normalises whitespace; if you need to round-trip a file byte-for-byte, hold on to the original bytes.
- **A codec only, with no view layer.** Use [`Bodu.Text.Ini`](../formats/ini.md) directly — the standalone INI library covers plain reading and writing; `Bodu.Text.Configuration` exists for profiles and the resolve / view layer.

## See also

- [Views and resolution](views-and-resolution.md) — `ConfigurationView`, key projection, typed lookup, glob matching.
- [Diagnostics](diagnostics.md) — the full diagnostic-code catalogue.
- [`Bodu.Text.Configuration` API reference](xref:Bodu.Text.Configuration).
- [`Bodu.Text.Ini`](../formats/ini.md) — the standalone INI library.
- [`Bodu.Extensions.Configuration.Text`](../extensions-configuration-text/index.md) — bridge to `Microsoft.Extensions.Configuration`.
- **[Configuration guides](../topics/configuration.md)** — every guide in this topic, across Bodu.Text.Configuration and Bodu.Extensions.Configuration.Text.
