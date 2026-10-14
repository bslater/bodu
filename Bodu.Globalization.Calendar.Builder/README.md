# Bodu.Globalization.Calendar.Builder

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

A fluent authoring API for `Bodu.Globalization.Calendar` notable-date documents. `NotableDateDocumentBuilder` constructs a calendar document in code - concepts, rules, adjustment and resolution policies, imports, and overrides - then serializes it to XML or a JSON subset, or materializes a live `NotableDateResource` for immediate use.

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.Builder
```

Targets `net8.0` and `net10.0`. All types live in the `Bodu.Globalization.Calendar.Builder` namespace.

## Authoring

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;
using Bodu.Globalization.Calendar.RangeResolution;

NotableDateResource resource = NotableDateDocumentBuilder
    .Create("au-national", schemaVersion: "1.0")
    .WithMetadata("Australia - National", description: "Federal public holidays")
    .WithResolutionPolicy(p => p.WithSameDayCollisionPolicy(CollisionPolicy.KeepAll))
    .AddAdjustmentPolicy("weekend-to-monday", a => a
        .When(AdjustmentTrigger.IfWeekend)
        .Then(AdjustmentAction.MoveToNextWorkingDay)
        .Emit(EmissionMode.ObservedOnly))
    .AddImport("christian-western", i => i.Use("easter-sunday").Use("good-friday"))
    .AddNotableDate("australia-day", "Australia Day", NotableDateCategory.PublicHoliday, d => d
        .AsNonWorkingByDefault()
        .AddRule("default", r => r
            .ForTerritory("AU")
            .Fixed(1, 26)
            .WithAdjustment("weekend-to-monday")))
    .Build(CommonNotableDateResources.Resolver);   // the resolver supplies the imported catalogue
```

A document that imports a catalogue must be built with a resolver; `Build()` resolves no imports, so the import above would fail with `BODU-CAL-IMPORT-MISSING`. Good Friday is defined relative to Easter Sunday, so both are imported.

The fluent entry points - `WithResourceId`, `WithSchemaVersion`, `WithMetadata`, `WithResolutionPolicy`, `AddAdjustmentPolicy`, `AddImport`, `AddNotableDate`, and `AddOverride` - are backed by the section builders `NotableDateDefinitionBuilder`, `NotableDateRuleBuilder`, `AdjustmentPolicyBuilder`, `ResolutionPolicyBuilder`, `ImportBuilder`, and `OverrideBuilder`.

## Serialization and loading

| Member | Purpose |
|---|---|
| `Build()` / `Build(importResolver)` | Materialize and validate a `NotableDateResource` |
| `ToXml()` / `ToXDocument()` | Render full-fidelity XML (`urn:bodu:globalization:calendar`) |
| `ToJson()` | Render the JSON subset (Gregorian-only, reduced trigger/action surface) |
| `Save(path)` / `Save(path, format)` | Persist in the format the extension names (`.xml`, `.json`, or `.bcal` for a binary rule pack; any other extension throws), or the `NotableDateDocumentFormat` passed |
| `Load(path)` | Reconstruct a builder from a saved `.xml` or `.json` document (a `.bcal` pack cannot be reopened) |
| `ToProvider()` | Wrap the result as an `INotableDateResourceProvider` |

## Building and testing

To build this package from source and run its tests, see [CONTRIBUTING.md](https://github.com/bslater/bodu/blob/master/CONTRIBUTING.md).

## License

MIT. © Bodu Pty. Ltd.
