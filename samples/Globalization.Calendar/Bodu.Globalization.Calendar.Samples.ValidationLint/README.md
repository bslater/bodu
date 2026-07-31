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

## Scenarios

| Scenario | Shows |
|---|---|
| `LintingAuthoredDocuments` | `Validate()` on clean, semantically invalid, and structurally incomplete builders; `TryBuild` as the non-throwing `Build()` |
| `LintingRulePackText` | `TryLoad` turning malformed XML into `BODU-CAL-SYNTAX`, reporting semantic codes, and loading a valid pack |

## NuGet equivalents

```bash
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Globalization.Calendar.Builder
```
