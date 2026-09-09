---
title: Bodu.Formats.Outlook — Getting started
---

# Bodu.Formats.Outlook — Getting started

Unfamiliar with terms like *property tag*, *named property*, *attachment method*, *code page*, or *validation level*? Read [Core concepts](concepts.md) first.

## Install

Install the reader for the format you have; each pulls the shared `Bodu.Formats.Outlook` value model transitively.

```bash
dotnet add package Bodu.Formats.Outlook.Msg
```

```bash
dotnet add package Bodu.Formats.Outlook.Pst
```

Both target `net8.0` and share the single `Bodu.Formats.Outlook` namespace. Dependencies, from the project files:

- `Bodu.Formats.Outlook.Msg` — `Bodu.Formats.Outlook`, `Bodu.IO.Compound` (the OLE2 container), `Bodu.Core`, and `System.Text.Encoding.CodePages` (Windows code pages for `String8` properties).
- `Bodu.Formats.Outlook.Pst` — `Bodu.Formats.Outlook`, `Bodu.IO.Pst` (the node-database container, which itself references `Bodu.Collections`), `Bodu.Core`, and `System.Text.Encoding.CodePages`.
- `Bodu.Formats.Outlook` alone — `Bodu.Core` only. Reference it directly from a library that models MAPI properties without opening files.

## Sniff before opening

Both sessions expose a cheap probe that restores the stream position afterwards. `IsMsgFile` checks for an OLE2 compound file whose root holds the `__properties_version1.0` stream (the root class identifier is not required — real-world writers omit it); `IsPstFile` checks the PST magics and answers `true` for any variant.

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using FileStream stream = File.OpenRead("unknown.bin");

if (OutlookMessage.IsMsgFile(stream))
{
    using var message = OutlookMessage.OpenRead(stream, leaveOpen: true);
    Console.WriteLine($"message: {message.Subject}");
}
else if (OutlookMailStore.IsPstFile(stream))
{
    using var store = OutlookMailStore.OpenRead(stream, leaveOpen: true);
    Console.WriteLine($"mail store: {store.DisplayName}");
}
```

## Open a `.msg` file

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using var message = OutlookMessage.OpenRead("invoice.msg");

Console.WriteLine(message.Subject);
Console.WriteLine($"{message.SenderName} <{message.SenderEmailAddress}>");
Console.WriteLine(message.SentTime);

foreach (OutlookRecipient recipient in message.Recipients)
    Console.WriteLine($"{recipient.RecipientType}: {recipient.DisplayName} <{recipient.EmailAddress}>");

foreach (OutlookAttachment attachment in message.Attachments)
    Console.WriteLine($"{attachment.Method}: {attachment.FileName} ({attachment.Size} bytes)");
```

Every convenience is nullable and returns `null` when the message does not carry the underlying property. The session is <xref:System.IDisposable>: the `using` declaration closes the container and the source stream unless `leaveOpen: true` was passed to a stream overload.

## Open a `.pst` file and walk the folders

`RootFolder` is structural — user folders (the IPM subtree) hang beneath it — so walk `EnumerateSubfolders` recursively. Enumerations stream the folder's tables one row block at a time; nothing is materialized ahead of iteration.

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");

Console.WriteLine(store.DisplayName);
Walk(store.RootFolder, indent: 0);

static void Walk(OutlookMailFolder folder, int indent)
{
    var pad = new string(' ', indent * 2);
    Console.WriteLine($"{pad}{folder.DisplayName} [{folder.ContainerClass}] " +
        $"{folder.MessageCount?.ToString() ?? "?"} messages, {folder.UnreadCount?.ToString() ?? "?"} unread");

    foreach (OutlookMailMessage message in folder.EnumerateMessages())
        Console.WriteLine($"{pad}  {message.SentTime:u}  {message.Subject} — {message.SenderName}");

    foreach (OutlookMailFolder child in folder.EnumerateSubfolders())
        Walk(child, indent + 1);
}
```

`MessageCount` and `UnreadCount` are the counts the folder *declares* (`PidTagContentCount` / `PidTagContentUnreadCount`), not a count of enumerated rows. Search folders are excluded from `EnumerateSubfolders` at every validation level; folder-associated (hidden) items are reachable through `EnumerateAssociatedMessages()`.

## Read a body

The three body conveniences have the same names on both message types. `BodyRtf` decompresses `PidTagRtfCompressed` on first access and caches the result.

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using var message = OutlookMessage.OpenRead("invoice.msg");

string body = message.BodyText
    ?? message.BodyHtml
    ?? message.BodyRtf
    ?? "(no body)";

Console.WriteLine(body);
```

## Save an attachment and open an attached message

Check `Method` before choosing the accessor — `OpenContentStream()` serves a by-value payload, `OpenMessage()` an embedded message, and each throws <xref:System.NotSupportedException> for the other kinds.

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using var message = OutlookMessage.OpenRead("invoice.msg");

foreach (OutlookAttachment attachment in message.Attachments)
{
    switch (attachment.Method)
    {
        case OutlookAttachmentMethod.ByValue:
            using (Stream content = attachment.OpenContentStream())
            using (FileStream target = File.Create(attachment.FileName ?? "attachment.bin"))
                content.CopyTo(target);
            break;

        case OutlookAttachmentMethod.EmbeddedMessage:
            OutlookMessage nested = attachment.OpenMessage();
            Console.WriteLine($"attached message (depth {nested.EmbeddedDepth}): {nested.Subject}");
            break;
    }
}
```

In the `.msg` reader a nested message shares the root session's container; disposing it is a no-op. The `.pst` reader has the same shape over <xref:Bodu.Formats.Outlook.OutlookMailAttachment>:

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");

foreach (OutlookMailFolder folder in store.RootFolder.EnumerateSubfolders())
{
    foreach (OutlookMailMessage message in folder.EnumerateMessages())
    {
        foreach (OutlookMailAttachment attachment in message.Attachments)
        {
            if (attachment.Method == OutlookAttachmentMethod.EmbeddedMessage)
            {
                OutlookMailMessage nested = attachment.OpenMessage();
                Console.WriteLine($"{message.Subject} carries: {nested.Subject}");
            }
        }
    }
}
```

## Look up a named property

Named identifiers (at or above `0x8000`) are assigned per file. Resolve the durable identity — a property-set GUID plus a number or a name — to this file's identifier, then address the property collection with the expected wire type. The two members have the same names on both sessions; the `.pst` mapping is store-wide.

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

// PS_PUBLIC_STRINGS "Keywords" — the category list.
var keywords = new MapiNamedProperty(
    new Guid("00020329-0000-0000-C000-000000000046"), "Keywords");

using var message = OutlookMessage.OpenRead("invoice.msg");

if (message.TryGetNamedPropertyId(keywords, out ushort id))
    Console.WriteLine(string.Join(", ", message.Properties.GetStringArray(id) ?? Array.Empty<string>()));

using var store = OutlookMailStore.OpenRead("archive.pst");

if (store.TryGetNamedPropertyId(keywords, out ushort storeId))
{
    foreach (OutlookMailFolder folder in store.RootFolder.EnumerateSubfolders())
        foreach (OutlookMailMessage item in folder.EnumerateMessages())
            Console.WriteLine($"{item.Subject}: {string.Join(", ", item.Properties.GetStringArray(storeId) ?? Array.Empty<string>())}");
}

// The reverse direction: what does a named tag mean in this file?
if (message.TryGetPropertyName(new MapiPropertyTag(0x8000101F), out MapiNamedProperty name))
    Console.WriteLine(name);
```

## Read any property by identifier

The conveniences are curated views; the full surface is the tag-addressed <xref:Bodu.Formats.Outlook.MapiPropertyCollection> on `Properties`, with the same shape on every folder, recipient, and attachment.

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using var message = OutlookMessage.OpenRead("invoice.msg");

string? messageClass = message.Properties.GetString(MapiPropertyIds.MessageClass);   // "IPM.Note"
int? codePage = message.Properties.GetInt32(MapiPropertyIds.MessageCodepage);
DateTimeOffset? delivered = message.Properties.GetDateTime(MapiPropertyIds.MessageDeliveryTime);

foreach (MapiProperty property in message.Properties)
    Console.WriteLine($"{property.Tag} = {property.Value}");
```

## Tune validation and limits

Pass an options instance to `Open` to change the validation level or the resource limits. Each reader reuses its container's validation enumeration.

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;
using Bodu.IO.Compound;

var options = new OutlookMessageReaderOptions
{
    ValidationLevel = CompoundValidationLevel.Strict,   // structural problems throw instead of being omitted
    ReadStrategy = CompoundReadStrategy.Streaming,      // read the container sector by sector
    MaxEmbeddedMessageDepth = 4,
    MaxDecompressedRtfBytes = 8 * 1024 * 1024,
    MaxInlineAttachmentBytes = 256 * 1024,              // larger payloads stay in the container until streamed
};

using var message = OutlookMessage.Open(File.OpenRead("invoice.msg"), options);
```

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;
using Bodu.IO.Pst;

var options = new OutlookMailStoreReaderOptions
{
    ValidationLevel = PstValidationLevel.Strict,        // every checksum and table row enforced
    BlockCacheSize = 512,                               // decoded-block LRU entries (0 disables)
    MaxNodeDataLength = 64L * 1024 * 1024,              // largest node payload materialized at once
    DecompressRtf = false,                              // BodyRtf returns null; raw payload stays in Properties
};

using var store = OutlookMailStore.Open(File.OpenRead("archive.pst"), options);
```

The defaults — `Compatible` validation, 16 levels of nesting, 64 MiB of decompressed RTF, 1 MiB of inline attachment, and (for `.pst`) 256 MiB of node data — suit real-world files; see [resource limits](concepts.md#resource-limits) for what each one guards.

## Handle malformed input

The two readers report failures differently beneath the shared <xref:Bodu.Formats.Outlook.OutlookFormatException> base. The `.msg` reader wraps container corruption — the <xref:Bodu.IO.Compound.CompoundFileFormatException> is the `InnerException` — while the `.pst` reader lets the <xref:Bodu.IO.Pst.PstFileException> family propagate unwrapped and reserves <xref:Bodu.Formats.Outlook.OutlookPstFormatException> for messaging-level violations.

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

try
{
    using var message = OutlookMessage.OpenRead("suspect.msg");
    Console.WriteLine(message.BodyRtf);
}
catch (OutlookMsgFormatException ex)
{
    // Not a compound file, a malformed container (see ex.InnerException), an MS-OXMSG
    // violation under strict validation, or a tripped nesting / RTF limit.
    Console.WriteLine($"Rejected: {ex.Message}");
}
```

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;
using Bodu.IO.Pst;

try
{
    using var store = OutlookMailStore.OpenRead("suspect.pst");
    foreach (OutlookMailFolder folder in store.RootFolder.EnumerateSubfolders())
        Console.WriteLine(folder.DisplayName);
}
catch (PstUnsupportedFormatException)
{
    Console.WriteLine("A recognized but unsupported PST variant (the 4 KiB-page OST).");
}
catch (PstFileException ex)
{
    Console.WriteLine($"Container corruption: {ex.Error} — {ex.Message}");
}
catch (OutlookPstFormatException ex)
{
    Console.WriteLine($"Messaging-level violation: {ex.Message}");
}
```

## Where to go next

- **[Core concepts](concepts.md)** — the vocabulary in depth, including the code-page and resource-limit rules.
- **[Introduction](index.md)** — the package table, scenarios, and headline types.
- **[Reading `.msg` files](../../guides/outlook/reading-msg-files.md)** and **[Properties and named properties](../../guides/outlook/properties-and-named-properties.md)** — the recipe-style guides.
- **[Bodu.IO.Pst getting started](../io-pst/getting-started.md)** and the **[PST runnable sample](../../samples/io-pst.md)** — the container beneath the mail-store reader.
- **API reference** — [Bodu.Formats.Outlook](xref:Bodu.Formats.Outlook) · [Bodu.IO.Compound](xref:Bodu.IO.Compound) · [Bodu.IO.Pst](xref:Bodu.IO.Pst).
