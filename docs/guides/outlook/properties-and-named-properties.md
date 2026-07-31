---
title: Properties and named properties
---

# Properties and named properties

Everything a message carries is a MAPI property: a 32-bit tag — a 16-bit identifier plus a 16-bit wire type — paired with a value. The conveniences on <xref:Bodu.Formats.Outlook.OutlookMessage> are curated views; the full surface is the tag-addressed <xref:Bodu.Formats.Outlook.MapiPropertyCollection> on `Properties`, and the same collection shape hangs off every recipient and attachment.

## The raw property surface

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using var message = OutlookMessage.OpenRead("invoice.msg");

// Typed accessors probe the plausible wire types for an identifier and
// return null when absent — they never throw for a missing property.
string? subject = message.Properties.GetString(MapiPropertyIds.Subject);
int? codePage = message.Properties.GetInt32(MapiPropertyIds.MessageCodepage);
DateTimeOffset? sent = message.Properties.GetDateTime(MapiPropertyIds.ClientSubmitTime);

// Or address a property by its full tag and inspect the decoded value.
var tag = new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode);
if (message.Properties.TryGetValue(tag, out MapiProperty? property))
    Console.WriteLine($"{property.Tag} = {property.Value}");

// Enumerate everything the message stores.
foreach (MapiProperty item in message.Properties)
    Console.WriteLine(item);
```

`MapiPropertyIds` carries the curated well-known identifiers (`PidTag*`); any property is reachable by raw identifier whether or not it is listed there.

## Tags and wire types

<xref:Bodu.Formats.Outlook.MapiPropertyTag> decomposes the 32-bit tag: `Id`, the base `Type` (with the multi-valued flag stripped), `IsMultiValued`, and `IsNamed` for identifiers at or above `0x8000`. `ToString` renders the canonical hexadecimal form, for example `0x0037001F` — the subject as a Unicode string.

Multi-valued properties decode to arrays (`string[]`, `int[]`, `byte[][]`, …) and are addressed through the multi-valued accessors such as `GetStringArray`.

## Named properties

Identifiers at or above `0x8000` are file-specific: their meaning comes from the message's named-property mapping, which pairs each identifier with a durable <xref:Bodu.Formats.Outlook.MapiNamedProperty> — a property-set GUID plus a numeric identifier or a string name. Resolve in either direction:

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using var message = OutlookMessage.OpenRead("invoice.msg");

// PS_PUBLIC_STRINGS "Keywords" — the category list.
var keywords = new MapiNamedProperty(
    new Guid("00020329-0000-0000-C000-000000000046"), "Keywords");

if (message.TryGetNamedPropertyId(keywords, out ushort id))
{
    string[]? categories = message.Properties.GetStringArray(id);
    Console.WriteLine(string.Join(", ", categories ?? Array.Empty<string>()));
}

// The reverse direction: what does a named identifier mean in this file?
if (message.TryGetPropertyName(new MapiPropertyTag(0x8000101F), out MapiNamedProperty name))
    Console.WriteLine(name);
```

The mapping is parsed lazily from the root's `__nameid_version1.0` storage and, per MS-OXMSG, is shared by every nested attached message.
