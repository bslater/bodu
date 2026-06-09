# Bodu.Globalization.Calendar.Builder

A fluent authoring API for `Bodu.Globalization.Calendar` notable-date documents. `NotableDateDocumentBuilder` constructs a calendar document in code — concepts, rules, adjustment and resolution policies, imports, and overrides — then serializes it to XML or a JSON subset, or materializes a live `NotableDateResource` for immediate use.

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.Builder
```

Targets `net8.0`. All types live in the `Bodu.Globalization.Calendar.Builder` namespace.

## Authoring

```csharp
using Bodu.Globalization.Calendar.Builder;

NotableDateResource resource = NotableDateDocumentBuilder
    .Create("au-national", schemaVersion: "1.0")
    .WithMetadata("Australia — National", description: "Federal public holidays")
    .WithResolutionPolicy(p => /* duplicate, collision, observed-range rules */)
    .AddImport("christian-western")
    .AddNotableDate("australia-day", "Australia Day", category: "public", b => /* rules */)
    .Build();
```

The fluent entry points — `WithResourceId`, `WithSchemaVersion`, `WithMetadata`, `WithResolutionPolicy`, `AddAdjustmentPolicy`, `AddImport`, `AddNotableDate`, and `AddOverride` — are backed by the section builders `NotableDateDefinitionBuilder`, `NotableDateRuleBuilder`, `AdjustmentPolicyBuilder`, `ResolutionPolicyBuilder`, `ImportBuilder`, and `OverrideBuilder`.

## Serialization and loading

| Member | Purpose |
|---|---|
| `Build()` / `Build(importResolver)` | Materialize and validate a `NotableDateResource` |
| `ToXml()` / `ToXDocument()` | Render full-fidelity XML (`urn:bodu:globalization:calendar`) |
| `ToJson()` | Render the JSON subset (Gregorian-only, reduced trigger/action surface) |
| `Save(path)` / `Save(path, format)` | Persist as XML (default) or JSON (`NotableDateDocumentFormat`) |
| `Load(path)` | Reconstruct a builder from a saved document |
| `ToProvider()` | Wrap the result as an `INotableDateResourceProvider` |

## Testing

```bash
dotnet test Bodu.Globalization.Calendar.Builder/test/Bodu.Globalization.Calendar.Builder.Test.csproj --settings bvt.runsettings
```

Tests build documents end-to-end, serialize to XML/JSON, and assert against the real `NotableDateResourceLoader` / `NotableDateService` to confirm round-trip fidelity.

## License

MIT. © Bodu Pty. Ltd.
