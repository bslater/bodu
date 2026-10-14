# Bodu.Formats.Outlook

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

The shared **MAPI value model** for the Bodu Outlook format readers: property tags and
types, decoded property values with a tag-addressed collection, named-property
identities, recipient and attachment enumerations, and the shared exception hierarchy.

This package is deliberately **container-free** - it knows nothing about `.msg` or
`.pst` files. The [`Bodu.Formats.Outlook.Msg`](../Bodu.Formats.Outlook.Msg) reader and
the [`Bodu.Formats.Outlook.Pst`](../Bodu.Formats.Outlook.Pst) reader over `Bodu.IO.Pst`
build on this model rather than owning it, the same shared-value-model convention as
`Bodu.Formats.Excel` and `Bodu.Financial.ExchangeRates`.

```csharp
using Bodu.Formats.Outlook;

var subject = new MapiProperty(
    new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode),
    "Quarterly report");

var properties = new MapiPropertyCollection(new[] { subject });

Console.WriteLine(properties.GetString(MapiPropertyIds.Subject)); // Quarterly report
```

## What's in the model

- `MapiPropertyTag` / `MapiPropertyType` - the 32-bit property tag (16-bit id +
  16-bit type) and the `PT_*` type codes, including the multi-valued flag and the
  named-property id range.
- `MapiProperty` / `MapiPropertyCollection` - one decoded property and the
  tag-addressed, read-only collection with typed accessors (`GetString`,
  `GetInt32`, `GetDateTime`, `GetBinary`, …).
- `MapiNamedProperty` - a named-property identity (property-set GUID plus a numeric
  id or a string name).
- `MapiPropertyIds` - curated well-known property ids (`PidTag*`).
- `OutlookRecipientType` / `OutlookAttachmentMethod` - the recipient and attachment
  enumerations shared by the format readers.
- `OutlookFormatException` - the base exception for Outlook format failures.

## Out of scope

- Any container or file-format knowledge - parsing lives in the format packages.
- The full MS-OXPROPS property catalogue - `MapiPropertyIds` is a curated subset;
  every property remains reachable by raw id through the collection.
- MAPI session semantics (`IMAPIProp`, property flag enforcement, stores).
