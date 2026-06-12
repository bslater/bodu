---
title: Diagnostics
---

# Diagnostics

When `Bodu.Text.Configuration` encounters a problem it cannot silently resolve, it produces a `ConfigurationDiagnostic` — a structured record naming the diagnostic code, severity, source location, and a human-readable message. This guide covers the diagnostic surface end-to-end: how the parser surfaces diagnostics, how the modes interact, the severity scale, and the full catalogue of `ConfigurationDiagnosticCode` values.

For the parse-time options that control diagnostic *behaviour* — `DiagnosticMode`, the profile presets — see [Parsing and profiles](parsing-and-profiles.md). For the view-time surface, see [Views and resolution](views-and-resolution.md).

## Diagnostic mode

`ConfigurationDiagnosticMode` determines what the parser does when it encounters a recoverable error:

| Mode | Effect |
|---|---|
| `Throw` *(default)* | The first recoverable error raises `ConfigurationParseException` carrying a single `ConfigurationDiagnostic`. Parsing stops. |
| `Collect` | The parser continues past recoverable errors. Diagnostics accumulate on the `ConfigurationParseResult.Diagnostics` list. |
| `Ignore` | Recoverable diagnostics are dropped silently. Non-recoverable errors still throw. |

The `Bodu`, `EditorConfigCompatible`, and `Strict` profiles use `Throw`. The `Relaxed` profile uses `Collect` — handy for IDE-style validation where you want to surface every issue at once.

```csharp
using Bodu.Text.Configuration;

ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics(
    source,
    new ConfigurationParseOptions { DiagnosticMode = ConfigurationDiagnosticMode.Collect });

foreach (ConfigurationDiagnostic d in result.Diagnostics)
{
    Console.WriteLine($"{d.Severity} {d.Code} at {d.Location}: {d.Message}");
}

ConfigurationDocument document = result.Document;  // still populated, on a best-effort basis
```

## Diagnostic severity

`ConfigurationDiagnosticSeverity` classifies each diagnostic:

| Severity | Meaning |
|---|---|
| `Info` | Informational observation; the parse continues normally. |
| `Warning` | Non-fatal warning; the parse continues, the document is still produced. |
| `Error` | Recoverable error; under `Collect` mode the parse continues, under `Throw` mode it stops. |

The severity is fixed per diagnostic code — there is no "promote warnings to errors" knob.

## The `ConfigurationDiagnostic` record

```csharp
public sealed record ConfigurationDiagnostic(
    ConfigurationDiagnosticSeverity Severity,
    ConfigurationDiagnosticCode     Code,
    string                          Message,
    ConfigurationSourceLocation     Location);
```

Source locations carry the 1-based line number and column for the offending token. The default `ToString()` returns `"{Severity} {Code} at {Location}: {Message}"`, suitable for log lines and IDE diagnostics.

## The `ConfigurationDiagnosticCode` catalogue

Stable codes — the values do not change across versions, so consumers can build IDE squiggle rules or build-time enforcement around them.

### Structural errors

| Code | Trigger |
|---|---|
| `MissingEquals` *(1)* | A property line had no `=` separator (e.g. `key value` instead of `key = value`). |
| `EmptyKey` *(2)* | A property line declared an empty key (`= value`). |
| `UnterminatedSectionHeader` *(4)* | A section opened with `[` but no closing `]` appeared. |
| `EmptySectionHeader` *(5)* | A section header had no name between `[` and `]`. |
| `TrailingContentAfterSectionHeader` *(14)* | Text appeared after `]` when `SectionHeaderMode` did not allow it. |

### Duplicate handling

| Code | Trigger |
|---|---|
| `DuplicateKey` *(3)* | A key appeared more than once in a section when `DuplicateKeyMode = Disallowed`. |
| `DuplicateSection` *(12)* | A section name appeared more than once when `DuplicateSectionMode = Disallowed`. |

### Key validation

| Code | Trigger |
|---|---|
| `InvalidKeyCharacter` *(8)* | A key contained an illegal character (depends on `KeyOptions`). |
| `InvalidEscape` *(9)* | A malformed escape sequence (e.g. `\x` followed by non-hex) appeared in a value. |
| `KeyTooLong` *(11)* | The key exceeded `MaxKeyLength` (default 1024 chars). |
| `LineTooLong` *(10)* | The line exceeded `MaxLineLength` (default 8192 chars). |

### Glob-pattern compilation

These fire when a section header is compiled as a `ConfigurationPattern`:

| Code | Trigger |
|---|---|
| `UnbalancedBrace` *(6)* | A `{` in a section glob had no matching `}`. |
| `UnbalancedBracket` *(7)* | A `[` in a section glob had no matching `]`. |
| `NumericRangeTooLarge` *(13)* | A `{n1..n2}` range expanded to more than the parser's expansion cap. |
| `BraceNestingTooDeep` *(15)* | Brace nesting exceeded the parser's nesting cap. |
| `PatternTooLong` *(16)* | The compiled glob pattern exceeded the max compilable length. |

### Default code

`None` *(0)* — no specific code. Reserved for diagnostics produced from custom validators that do not have a catalogue entry; not used by the shipped parser.

## Diagnostics vs exceptions

The parser raises three kinds of exception:

1. **`ConfigurationParseException`** — raised when `DiagnosticMode = Throw` hits the first recoverable error, or for any non-recoverable error. Carries a single `ConfigurationDiagnostic`.
2. **`ArgumentException` / `ArgumentNullException`** — for invalid inputs (null source, unreadable stream). Standard BCL contract.
3. **`InvalidOperationException`** — for state violations that are not recoverable diagnostic codes (e.g. `Resolve` against a null view target).

Diagnostics under `Collect` and `Ignore` modes never throw — the parser carries on, populates the result document on a best-effort basis, and emits the diagnostic list for the caller to inspect.

## Working example — IDE-style validation

```csharp
using Bodu.Text.Configuration;

// Parse with diagnostic collection so every issue is surfaced, not just the first.
ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics(
    source,
    ConfigurationParseOptions.Relaxed);

foreach (ConfigurationDiagnostic d in result.Diagnostics)
{
    switch (d.Severity)
    {
        case ConfigurationDiagnosticSeverity.Info:
            log.Info($"{d.Code} at line {d.Location.Line}: {d.Message}");
            break;
        case ConfigurationDiagnosticSeverity.Warning:
            log.Warn($"{d.Code} at line {d.Location.Line}: {d.Message}");
            break;
        case ConfigurationDiagnosticSeverity.Error:
            log.Error($"{d.Code} at line {d.Location.Line}: {d.Message}");
            break;
    }
}

if (result.Diagnostics.All(d => d.Severity != ConfigurationDiagnosticSeverity.Error))
{
    Configure(result.Document);
}
```

The `Relaxed` profile is the right starting point for code-style validators and IDE plug-ins: it surfaces every recoverable issue without stopping the parse, and the resulting document is still usable for downstream analysis.

## See also

- [Parsing and profiles](parsing-and-profiles.md) — how `DiagnosticMode` interacts with the profile defaults.
- [Views and resolution](views-and-resolution.md) — `ConfigurationView`, key projection, typed lookup.
- [`Bodu.Text.Configuration` API reference](xref:Bodu.Text.Configuration).
- **[Configuration guides](../topics/configuration.md)** — every guide in this topic, across Bodu.Text.Configuration and Bodu.Extensions.Configuration.Text.
