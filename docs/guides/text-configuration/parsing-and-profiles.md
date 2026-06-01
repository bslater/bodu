---
title: Parsing and profiles
---

# Parsing and profiles

`Bodu.Text.Configuration` is the parser / view layer that sits on top of [`Bodu.Text.Formats.Ini`](../formats/ini.md). The codec parses INI faithfully; this layer adds parse-time profiles (Bodu, EditorConfig-compatible, Strict, Relaxed), structured diagnostics, and a flattened view-projecting surface that resolves dotted paths and EditorConfig-style glob sections.

This guide covers the *parsing* half — `ConfigurationDocument.Parse`, `ConfigurationParseOptions`, and the four profiles. For the *resolve* half — `Resolve` → `ConfigurationView`, key projection, typed lookup, glob matching — see [Views and resolution](views-and-resolution.md). For the diagnostic catalogue — every `ConfigurationDiagnosticCode` value, what triggers it — see [Diagnostics](diagnostics.md).

## Pattern 1 — parse a string with default options

```csharp
using Bodu.Text.Configuration;
using Bodu.Text.Ini;

string source = """
appName = MyApp

[Logging]
Level.Default = Information
Level.Microsoft = Warning
""";

IniDocument document = ConfigurationDocument.Parse(source);
```

`Parse(string)` returns the underlying `IniDocument` from `Bodu.Text.Formats.Ini` — the codec's faithful document model. Use the document directly when you only need codec-level access; use [`Resolve`](views-and-resolution.md) when you want path projection and typed lookup.

The default profile is `Bodu` — inline comments only after whitespace, lenient section headers, last-wins on duplicates, throw on the first error. Override the profile per call via `ConfigurationParseOptions`:

```csharp
IniDocument doc = ConfigurationDocument.Parse(
    source,
    ConfigurationParseOptions.EditorConfigCompatible);
```

## Pattern 2 — load from a file or stream

```csharp
using Bodu.Text.Configuration;

IniDocument fromFile   = ConfigurationDocument.Load("appsettings.ini");
IniDocument fromStream = ConfigurationDocument.Load(stream, leaveOpen: true);
IniDocument fromReader = ConfigurationDocument.Load(new StringReader(source));
```

`Load(string)` reads UTF-8 by default — pass an `Encoding` explicitly when the file is not UTF-8. `Load(Stream)` does not dispose the stream when `leaveOpen: true`. `Load(TextReader)` is for callers that already control the reader's lifetime.

## Pattern 3 — non-throwing parse

```csharp
using Bodu.Text.Configuration;

if (ConfigurationDocument.TryParse(source, out IniDocument? document))
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

IniDocument document = result.Document;
```

`ParseWithDiagnostics` always returns a `ConfigurationParseResult` carrying the document and the diagnostic list. With the `Relaxed` profile (which sets `DiagnosticMode = Collect`), the parser keeps going past recoverable errors and reports them all at once — useful for IDE-style validation. With other profiles, the diagnostic list is populated alongside the throw if you catch `ConfigurationParseException`.

## Pattern 5 — write a document back out

```csharp
using Bodu.Text.Configuration;

ConfigurationDocument.Save(document, "out.ini");
ConfigurationDocument.Save(document, stream, leaveOpen: false);
ConfigurationDocument.Save(document, new StreamWriter(path), options: writeOptions);
```

`Save` writes the document through `Bodu.Text.Formats.Ini` with configurable `ConfigurationWriteOptions`. Comment trivia is preserved when the input parse retained it (`PreserveComments: true`, the default).

## The four profiles

`ConfigurationProfile` is the headline behaviour switch. Each profile is a curated set of parse and resolve defaults — there is also a `ConfigurationProfile.For(profile)` factory if you want a clean starting point for fine-grained tuning.

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

## `ConfigurationParseOptions` field-by-field

Construct a custom option set when none of the four profiles fits. Every field is `init`-only on a readonly struct.

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

- `DuplicateKeyMode` (`IniDuplicateKeyBehavior`, default per profile) — `LastWins`, `FirstWins`, or `Disallowed`. See [INI duplicate-key policies](../formats/ini.md#pattern-5--duplicate-section-policies).
- `DuplicateSectionMode` (`IniDuplicateSectionBehavior`, default per profile) — `Preserve`, `Merge`, `MergeAdjacent`, or `Disallowed`.

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

- `KeyOptions` (`ConfigurationKeyOptions`, default `Default`) — segment-separator characters, dotted-to-colon mapping, case sensitivity. See [Views and resolution](views-and-resolution.md#configurationkeyoptions).
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
using Bodu.Text.Ini;

string editorConfig = """
root = true

[*.cs]
indent_size = 4
end_of_line = crlf

[*.{md,json}]
indent_size = 2
""";

IniDocument document = ConfigurationDocument.Parse(
    editorConfig,
    ConfigurationParseOptions.EditorConfigCompatible);

Console.WriteLine(document.Sections.Count);              // 2
Console.WriteLine(document.Sections[0].Name);            // "*.cs"
Console.WriteLine(document.Sections[1]["indent_size"]);  // "2"
```

The `EditorConfigCompatible` profile disables inline comments so a `#` inside a glob (`[file_with_#_in_name.cs]`) is not stripped, enforces strict section-header termination, and otherwise behaves identically to the EditorConfig 0.17.2 specification.

To go from the parsed `IniDocument` to a resolved typed view — including the EditorConfig glob behaviour where `[*.cs]` matches every `.cs` file — see [Views and resolution](views-and-resolution.md).

## Exceptions

- **`ConfigurationParseException`** — raised when `DiagnosticMode = Throw` hits the first recoverable error, or any time a non-recoverable error occurs. Carries the offending `ConfigurationDiagnostic` and source location.
- **`ArgumentException`** / **`ArgumentNullException`** — input string null or stream not readable. Standard BCL contract.

## When *not* to use `ConfigurationDocument`

- **Tabular data.** Reach for [`Delimited`](../formats/delimited.md).
- **`.env` files.** Reach for [`DotEnv`](../formats/dotenv.md).
- **Strict round-trip fidelity at the byte level.** The codec normalises whitespace; if you need to round-trip a file byte-for-byte, hold on to the original bytes.
- **A codec only, with no view layer.** Use [`Bodu.Text.Formats.Ini`](../formats/ini.md) directly — `ConfigurationDocument` wraps the codec to add profiles and the resolve / view layer; you do not need this surface if you are only reading or writing INI.

## See also

- [Views and resolution](views-and-resolution.md) — `ConfigurationView`, key projection, typed lookup, glob matching.
- [Diagnostics](diagnostics.md) — the full diagnostic-code catalogue.
- [`Bodu.Text.Configuration` API reference](~/apidoc/Bodu.Text.Configuration.md).
- [`Bodu.Text.Formats.Ini`](../formats/ini.md) — the underlying codec.
- [`Bodu.Extensions.Configuration.Text`](../extensions-configuration-text/index.md) — bridge to `Microsoft.Extensions.Configuration`.
