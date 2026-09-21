# Bodu.Globalization.Calendar.Samples.ValidationLint

Demonstrates the collect-mode validation lint: `NotableDateDocumentBuilder.Validate()` /
`TryBuild(...)` for fluently authored documents, and `NotableDateResourceLoader.TryLoad` for
arbitrary rule-pack text — every problem surfaces as a `NotableDateValidationDiagnostic` with a
stable `BODU-CAL-*` code instead of an exception, the shape build tasks and editor integrations
want. The complete code catalogue lives in the
[validation diagnostics guide](../../../docs/guides/calendar/validation-diagnostics.md).

```bash
dotnet run --project samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.ValidationLint
```

## Scenario 1 — LintingAuthoredDocuments

**Intent.** Show the linting surface for a document you are building in code, where the likely
mistakes are semantic rather than syntactic — a misspelled algorithm key is perfectly
well-formed.

**What it does.** Validates a clean document, one whose rule names an algorithm that does not
exist, and one whose concept has no rules at all, then builds the clean one through the
non-throwing `TryBuild`.

**What to expect.**

```text
  Clean document diagnostics: 0  (expected 0 - and because Validate runs the loader's own pipeline, an empty result guarantees Build will succeed)
    [Error] BODU-CAL-ALGORITHM: Notable date 'mystery-day', rule 'default': algorithm key 'no-such-algorithm' is not recognized.
  (a stable code, not a message to pattern-match - the document is well-formed XML, so only validation catches this)
    [Error] BODU-CAL-BUILDER-INCOMPLETE: The notable-date concept 'empty-concept' has no rules. Add at least one rule before building or serializing the document.
  (Build would have thrown on this one - a linter has to survive input worse than the loader accepts)
  TryBuild succeeded: corp.holidays resolves 1 occurrence(s) in 2026  (a true return hands back a ready resource; a false one hands back the diagnostics instead)
```

`Validate()` runs the loader's own pipeline, which is what makes an empty result a guarantee
that `Build()` will succeed rather than a weaker check. The structurally incomplete document is
the interesting row: `Build()` would have thrown an `InvalidOperationException` on it, and the
linter still reports it as a diagnostic — a linter has to survive input worse than the loader
accepts.

**APIs demonstrated.** `NotableDateDocumentBuilder.Validate` / `TryBuild`,
`NotableDateValidationDiagnostic` (severity, `BODU-CAL-*` code, message).

## Scenario 2 — LintingRulePackText

**Intent.** Show the surface a build task or editor integration needs, where the input is
arbitrary text — including text that is not well-formed XML.

**What it does.** Lints three inputs through `TryLoad`: text that is not XML, a well-formed pack
naming an algorithm that does not exist, and a valid pack.

**What to expect.**

```text
  malformed input: loaded=False, resource=none
    [Error] BODU-CAL-SYNTAX: The notable-date document XML is not well-formed: 'not' is an unexpected token. The expected token is '='. Line 1, position 26.
  unknown algorithm key: loaded=False, resource=none
    [Error] BODU-CAL-ALGORITHM: Notable date 'mystery-day', rule 'x': algorithm key 'no-such-algorithm' is not recognized.
  valid pack: loaded=True, resource=pack.valid
```

Nothing throws, including the input that is not XML — which matters because a file being edited
is malformed for most of the time it is being edited, so a loader that throws on bad XML cannot
drive a linter at all. Syntax errors carry codes in the same scheme as semantic ones, so a
consumer renders one kind of thing rather than two, and the codes match those the throwing
overloads carry inside `NotableDateValidationException`.

**APIs demonstrated.** `NotableDateResourceLoader.TryLoad`, `BODU-CAL-SYNTAX` /
`BODU-CAL-ALGORITHM` diagnostic codes.

## Layout

```text
Bodu.Globalization.Calendar.Samples.ValidationLint/
  Program.cs                             # runs the scenarios in order
  SampleConsole.cs                       # the What / Why / Expect scenario banner
  Scenarios/LintingAuthoredDocuments.cs
  Scenarios/LintingRulePackText.cs
```

## NuGet equivalents

```bash
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Globalization.Calendar.Builder
```
