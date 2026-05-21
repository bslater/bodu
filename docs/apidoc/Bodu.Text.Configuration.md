---
uid: Bodu.Text.Configuration
---

![Bodu.Text.Configuration](~/images/hero-configuration.svg)

## Purpose

**Bodu.Text.Configuration** is the configuration-layering package of the Bodu suite. It parses INI / EditorConfig-style text, optionally collects diagnostics, layers a preamble plus glob-anchored sections in source order for a target path, and projects the result into a flat, colon-delimited <xref:Bodu.Text.Configuration.ConfigurationView>. The view exposes typed accessors (`GetString`, `GetInt32`, `GetBoolean`, `GetEnum<T>`, `GetValue<T>` for any <xref:System.ISpanParsable`1>) and integrates directly with `Microsoft.Extensions.Configuration` through the sibling <xref:Bodu.Extensions.Configuration.Text> package.

Reach for this library when you need EditorConfig-style file-targeted configuration layering, programmatic INI parsing with diagnostic collection, or a `Microsoft.Extensions.Configuration`-compatible flat key/value view without taking a dependency on `Microsoft.Extensions.*` from the parser itself. The underlying data model is <xref:Bodu.Text.Ini.IniDocument> from the <xref:Bodu.Text.Formats> package, so anything the INI codec can read is something `Bodu.Text.Configuration` can resolve.

## Static documentation

- **[Bodu.Text.Configuration introduction](~/docs/text-configuration/index.md)** — shape of the library, headline types, scenarios.
- **[Bodu.Text.Configuration core concepts](~/docs/text-configuration/concepts.md)** — vocabulary: document vs view, profile, parse/resolve/write options, key mapping, glob pattern, preamble, target path, diagnostic mode, unset.
- **[Bodu.Text.Configuration getting started](~/docs/text-configuration/getting-started.md)** — install and minimal samples for parse-resolve-read, profile presets, diagnostics, round-trip save.
- **[Bodu.Extensions.Configuration.Text](~/docs/extensions-configuration-text/index.md)** — `IConfigurationBuilder` bridge.

## Key types

**Document and view**

- <xref:Bodu.Text.Configuration.ConfigurationDocument> — static façade for parsing and saving. `Parse`, `ParseWithDiagnostics`, `Load(path | Stream | TextReader)`, `Save(document, path | Stream | TextWriter, options?)`. Backed by <xref:Bodu.Text.Ini.IniDocument>.
- <xref:Bodu.Text.Configuration.ConfigurationView> — read-only, one-shot resolved snapshot for a target path; implements `IEnumerable<KeyValuePair<string, string?>>`; exposes `Values`, `Keys`, `Count`, indexer, and the `GetXxx` / `TryGetXxx` / `GetValue<T>` / `TryGetValue<T>` family.
- <xref:Bodu.Text.Configuration.ConfigurationExtensions> — extension methods over the underlying INI primitives: `Resolve(targetPath)` on `IniDocument`, `IsMatch(relativePath)` on `IniSection`, `ConfigurationPath()` on `IniEntry`, `Preamble()` on `IniDocument`.
- <xref:Bodu.Text.Configuration.ConfigurationParseResult> — output of `ParseWithDiagnostics`: the document and an `ImmutableArray<ConfigurationDiagnostic>` of recoverable issues collected during the parse.
- <xref:Bodu.Text.Configuration.ConfigurationParseException> — thrown by the throwing parse / load overloads under `ConfigurationDiagnosticMode.Throw`.

**Profiles and options**

- <xref:Bodu.Text.Configuration.ConfigurationProfile> — `Bodu` (default), `EditorConfigCompatible`, `Strict`, `Relaxed`. Each option type has a static `For(profile)` factory and four named property presets.
- <xref:Bodu.Text.Configuration.ConfigurationParseOptions> — reader behaviour: `InlineCommentMode`, `DuplicateKeyMode`, `DuplicateSectionMode`, `DiagnosticMode`, `MaxLineLength`, `MaxKeyLength`, `TrimKeysAndValues`, `AllowKeyOnlyProperties`, `DefaultEncoding`, `KeyOptions`. Static presets `Bodu`, `EditorConfigCompatible`, `Strict`, `Relaxed`.
- <xref:Bodu.Text.Configuration.ConfigurationResolveOptions> — resolver behaviour: `PathRoot`, `MissingPathRootMode`, `ApplyPreambleProperties`, `PathComparison`, `UnsetValueMode`, `KeyOptions`. Static presets aligned with the same four profiles.
- <xref:Bodu.Text.Configuration.ConfigurationWriteOptions> — writer behaviour: encoding (default UTF-8 without BOM), newline style, blank-line policy, property formatting. Static presets aligned with the same four profiles.

**Keys**

- <xref:Bodu.Text.Configuration.ConfigurationKey> — read-only struct with `RawKey`, `Path` (the canonical colon-delimited form), `Segments`, `CaseSensitive`. Static `Parse(rawKey, options?)` / `TryParse(rawKey, options?, out result)` factories.
- <xref:Bodu.Text.Configuration.ConfigurationKeyOptions> — `SegmentSeparators` (default `{ '.', ':' }`), `Mapping` (`DotToColon` default / `Colon` / `Identity`), `CaseSensitive` (default `false`), `AllowEmptySegments` (default `false`).
- <xref:Bodu.Text.Configuration.ConfigurationKeyMapping> — `DotToColon` (default), `Colon`, `Identity`.

**Diagnostics**

- <xref:Bodu.Text.Configuration.ConfigurationDiagnostic> — immutable record carrying `Severity`, `Code`, `Message`, `Location`.
- <xref:Bodu.Text.Configuration.ConfigurationDiagnosticSeverity> — `Warning`, `Error`.
- <xref:Bodu.Text.Configuration.ConfigurationDiagnosticCode> — stable category identifier (duplicate key, invalid section header, invalid unset, line-length exceeded, …).
- <xref:Bodu.Text.Configuration.ConfigurationDiagnosticMode> — `Throw` (default), `Collect`, `Ignore`.
- <xref:Bodu.Text.Configuration.ConfigurationSourceLocation> — line / column / file metadata pointing into the source text.

**Modes**

- <xref:Bodu.Text.Configuration.ConfigurationInlineCommentMode> — `Disabled` (EditorConfig), `WhitespaceIntroduced` (default), `Always`.
- <xref:Bodu.Text.Configuration.ConfigurationUnsetValueMode> — `TreatAsLiteral` (default), `RemoveEffectiveValue` (EditorConfig sentinel).
- <xref:Bodu.Text.Configuration.ConfigurationMissingPathRootMode> — `UseEmptyRoot` (default), `Throw`, `IgnoreAnchoredPatterns`.

**Pattern engine**

- <xref:Bodu.Text.Configuration.ConfigurationPattern> — compiled EditorConfig-style glob pattern. `Compile(text)` + `IsMatch(path)`. Used internally by the resolver and exposed for callers that want to test pattern matching standalone.

## Example

```csharp
using Bodu.Text.Configuration;
using Bodu.Text.Formats;

const string source = """
# Bodu configuration sample
root = true

[*.cs]
format.indent.style = space
format.indent.size  = 4
logging.level.default = Information

[src/**/*.{cs,csproj}]
format.indent.size = 2
logging.level.default = Warning
""";

// Parse, optionally collect diagnostics, then resolve for a target path.
ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics(source);
IniDocument doc = result.Document;

ConfigurationView view = doc.Resolve("src/Bodu.Text.Configuration/src/Foo.cs");

string indentStyle = view.GetString("format:indent:style");      // "space"
int indentSize     = view.GetInt32("format:indent:size");        // 2 — last-wins from [src/**]
string logLevel    = view.GetString("logging:level:default");    // "Warning"
double threshold   = view.GetValue<double>("limits:cpu:threshold", fallback: 0.8);

// Profile presets — swap the parser into EditorConfig-strict mode.
IniDocument editorConfig = ConfigurationDocument.Parse(
    source,
    ConfigurationParseOptions.EditorConfigCompatible);

// Round-trip back to text.
using StringWriter sw = new();
ConfigurationDocument.Save(doc, sw);
string canonicalText = sw.ToString();
```

## Notes

- **Immutable views.** <xref:Bodu.Text.Configuration.ConfigurationView> is a one-shot snapshot. The underlying dictionary is exposed read-only through `Values`; subsequent mutations of the originating `IniDocument` do not propagate. Take a fresh view after every meaningful document edit.
- **Thread safety.** The view is safe to read from any number of threads concurrently. The parser, resolver, and save APIs are short-lived and stateless across calls; the document model itself is not thread-safe for concurrent mutation but is safe for concurrent read.
- **Determinism.** All parsing, resolving, and typed conversion uses <xref:System.Globalization.CultureInfo.InvariantCulture>. Output bytes from `Save` are byte-stable for documents the library produced; documents authored by hand may be reformatted to the canonical layout on first save.
- **Last-wins precedence.** The resolver walks the document once in source order; for any configuration key, the value from the later matching section wins. There is no override / inherit hierarchy beyond "later in the file wins" — this matches EditorConfig and is intentional.
- **EditorConfig conformance.** Setting the profile to <xref:Bodu.Text.Configuration.ConfigurationProfile.EditorConfigCompatible> aligns inline-comment, preamble, key-mapping, and unset behaviour with the EditorConfig 0.17.2 specification. Bodu-specific extensions (whitespace-introduced inline comments, dotted-to-colon key mapping, preamble layering) are opt-out under that profile.
- **Validation.** Public entry points validate inputs through `ThrowHelper` and the package-local `ConfigurationThrowHelper`. Null inputs surface as `ArgumentNullException`; empty or whitespace strings surface as `ArgumentException` with the parameter name set via `CallerArgumentExpression`.
- **See also:** the [introduction](~/docs/text-configuration/index.md), [core concepts](~/docs/text-configuration/concepts.md), and [getting-started](~/docs/text-configuration/getting-started.md); the underlying <xref:Bodu.Text.Ini.IniDocument> model in [Bodu.Text.Formats](~/docs/formats/index.md); and the `Microsoft.Extensions.Configuration` bridge in [Bodu.Extensions.Configuration.Text](~/docs/extensions-configuration-text/index.md).
