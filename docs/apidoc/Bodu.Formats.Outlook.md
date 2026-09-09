---
uid: Bodu.Formats.Outlook
---

![Bodu.Formats.Outlook](~/images/hero-outlook.svg)

## Purpose

**Bodu.Formats.Outlook** is the single, flattened namespace shared by three packages: the container-free MAPI value model (`Bodu.Formats.Outlook`) and the two read-only readers built on it — `Bodu.Formats.Outlook.Msg`, which opens one message (`.msg` / MS-OXMSG) over the <xref:Bodu.IO.Compound> OLE2 container, and `Bodu.Formats.Outlook.Pst`, which opens a personal-folders mail store (`.pst` / MS-PST, Unicode and ANSI formats) over the <xref:Bodu.IO.Pst> node database. The package names keep their suffixes; the public types do not — a `.msg` recipient and a `.pst` recipient are the same <xref:Bodu.Formats.Outlook.OutlookRecipient> over the same <xref:Bodu.Formats.Outlook.MapiPropertyCollection>, and code written against one reader's property surface carries over to the other. The container-specific decoders stay `internal` under `Bodu.Formats.Outlook.Msg` and `Bodu.Formats.Outlook.Pst`.

Both readers open a disposable **session** that owns the container, decode every MAPI property into the shared model, and layer curated conveniences (subject, sender, time stamps, recipients, attachments, the text / HTML / compressed-RTF bodies, named-property resolution) over the raw tag-addressed surface. Neither writes, emulates a MAPI session, or de-encapsulates RTF.

## Static documentation

- **[Introduction](~/docs/outlook/index.md)** — the three packages, the "which package do I need" table, and the headline types.
- **[Core concepts](~/docs/outlook/concepts.md)** — property-tag anatomy, named-property resolution, recipients and attachment methods, code pages, compressed RTF, validation levels, and the resource limits.
- **[Getting started](~/docs/outlook/getting-started.md)** — install and minimal samples for both readers.
- **[Reading `.msg` files](~/guides/outlook/reading-msg-files.md)** — opening a message, the conveniences, recipients, attachments, nested messages.
- **[Properties and named properties](~/guides/outlook/properties-and-named-properties.md)** — the raw property surface, tags and wire types, resolving named properties.
- **[Bodu.IO.Pst introduction](~/docs/io-pst/index.md)** and the **[PST runnable sample](~/samples/io-pst.md)** — the container beneath the mail-store reader.
- **[Binary Formats & I/O topic overview](~/docs/topics/binary-formats.md)** — the readers alongside their containers.

## Key types

**Value model — `Bodu.Formats.Outlook`**

- <xref:Bodu.Formats.Outlook.MapiPropertyTag> — the 32-bit tag (MS-OXCDATA §2.11.4): `Id` in the high word, base `Type` in the low word with the `0x1000` multi-valued flag stripped, `IsMultiValued`, and `IsNamed` (identifier ≥ `0x8000`); constructed from `(id, type)` or a raw `uint`, or via `ForMultiValue(id, elementType)`.
- <xref:Bodu.Formats.Outlook.MapiPropertyType> — the 16-bit wire-type codes: `Int16` / `Int32` / `Int64`, `Float` / `Double`, `Currency`, `AppTime`, `ErrorCode`, `Boolean`, `Object`, `String8` (code-paged) / `Unicode`, `SystemTime`, `Guid`, `Binary`, plus `Unspecified` / `Null`.
- <xref:Bodu.Formats.Outlook.MapiProperty> — one decoded property: `Tag` and a CLR `Value` (arrays for multi-valued properties).
- <xref:Bodu.Formats.Outlook.MapiPropertyCollection> — the tag-addressed, first-occurrence-ordered bag on every object. `TryGetValue` by tag or by identifier, `GetAll(id)`, `Contains(tag)`, `Count`, the shared `Empty`, and the typed accessors that probe the plausible wire types and return `null` when absent: `GetString`, `GetInt32`, `GetInt64`, `GetBoolean`, `GetDouble`, `GetDateTime`, `GetGuid`, `GetBinary`, `GetStringArray`.
- <xref:Bodu.Formats.Outlook.MapiNamedProperty> — the durable identity behind a named identifier: a property-set GUID with either a numeric `Id` or a string `Name`.
- <xref:Bodu.Formats.Outlook.MapiPropertyIds> — the curated `PidTag*` identifiers the conveniences use (`Subject`, `SenderName`, `ClientSubmitTime`, `Body`, `Html`, `RtfCompressed`, `AttachMethod`, `AttachLongFilename`, `MessageCodepage`, `ContainerClass`, …); any property is reachable by raw identifier regardless.
- <xref:Bodu.Formats.Outlook.OutlookRecipient> — one recipient: `RecipientType`, `DisplayName`, `EmailAddress`, `AddressType`, and its `Properties`. <xref:Bodu.Formats.Outlook.OutlookRecipientType> — `Originator` / `To` / `Cc` / `Bcc`.
- <xref:Bodu.Formats.Outlook.OutlookAttachmentMethod> — `None` / `ByValue` / `ByReference` / `ByReferenceResolve` / `ByReferenceOnly` / `EmbeddedMessage` / `Ole`.

**Message session — `Bodu.Formats.Outlook.Msg`**

- <xref:Bodu.Formats.Outlook.OutlookMessage> — the disposable `.msg` session. Factories `OpenRead` (path / `Stream`) and `Open(Stream, options, leaveOpen)`, the cheap `IsMsgFile` probe; `Properties`; the conveniences `Subject` (prefix marker stripped), `SenderName`, `SenderEmailAddress`, `MessageClass`, `InternetMessageId`, `TransportMessageHeaders`, `SentTime`, `ReceivedTime`; `Recipients` and `Attachments` (lazy, storage-index order); `BodyText` / `BodyHtml` / `BodyRtf`; `TryGetNamedPropertyId` / `TryGetPropertyName` over the `__nameid_version1.0` mapping shared with nested messages; `EmbeddedDepth`.
- <xref:Bodu.Formats.Outlook.OutlookAttachment> — one attachment: `Method` (inferred as `ByValue` when a payload exists but the property is absent), `FileName`, `ContentId`, `MimeTag`, `Size`; `OpenContentStream()` for a by-value payload, `OpenMessage()` for an embedded message (a nested session sharing the root's container — disposing it is a no-op).

**Mail-store session — `Bodu.Formats.Outlook.Pst`**

- <xref:Bodu.Formats.Outlook.OutlookMailStore> — the disposable `.pst` session. Factories `OpenRead` (path / `Stream`) and `Open(Stream, options, leaveOpen)`, the `IsPstFile` probe; `Properties`, `DisplayName`, `RootFolder`; the store-wide `TryGetNamedPropertyId` / `TryGetPropertyName` over the name-to-id map node. Single-threaded; every view is bound to the session's lifetime.
- <xref:Bodu.Formats.Outlook.OutlookMailFolder> — one folder: `DisplayName`, `ContainerClass`, the declared `MessageCount` / `UnreadCount`, `HasSubfolders`; the streaming `EnumerateSubfolders` (search folders excluded), `EnumerateMessages`, and `EnumerateAssociatedMessages`.
- <xref:Bodu.Formats.Outlook.OutlookMailMessage> — one message: the same convenience surface as `OutlookMessage` (`Subject`, `SenderName`, `MessageClass`, `InternetMessageId`, `TransportMessageHeaders`, the time stamps), `Recipients` (recipient-table order), `Attachments`, the three bodies, `EmbeddedDepth`.
- <xref:Bodu.Formats.Outlook.OutlookMailAttachment> — one attachment with the same surface as `OutlookAttachment`; `OpenContentStream()` streams a deferred payload block by block, `OpenMessage()` returns the embedded `OutlookMailMessage` with code-page inheritance.

**Options**

- <xref:Bodu.Formats.Outlook.OutlookMessageReaderOptions> — `ValidationLevel` (<xref:Bodu.IO.Compound.CompoundValidationLevel>, `Compatible`), `ReadStrategy` (<xref:Bodu.IO.Compound.CompoundReadStrategy>, `Buffered`), `DecompressRtf` (`true`), `MaxEmbeddedMessageDepth` (16), `MaxDecompressedRtfBytes` (64 MiB), `MaxInlineAttachmentBytes` (1 MiB).
- <xref:Bodu.Formats.Outlook.OutlookMailStoreReaderOptions> — `ValidationLevel` (<xref:Bodu.IO.Pst.PstValidationLevel>, `Compatible`), `BlockCacheSize` (256; `0` disables), `DecompressRtf` (`true`), `MaxNodeDataLength` (256 MiB), `MaxEmbeddedMessageDepth` (16), `MaxDecompressedRtfBytes` (64 MiB), `MaxInlineAttachmentBytes` (1 MiB).

**Errors**

- <xref:Bodu.Formats.Outlook.OutlookFormatException> — the family base; catch it to handle both formats.
- <xref:Bodu.Formats.Outlook.OutlookMsgFormatException> — not a compound file, a malformed container (the <xref:Bodu.IO.Compound.CompoundFileFormatException> is wrapped as `InnerException`), an MS-OXMSG violation under strict validation, or a tripped depth / RTF limit.
- <xref:Bodu.Formats.Outlook.OutlookPstFormatException> — a messaging-level MS-PST violation (an invalid table row under strict validation, a malformed name-to-id map, a tripped limit). Container corruption is not wrapped: it propagates as the <xref:Bodu.IO.Pst.PstFileException> family, and a failed open throws <xref:Bodu.IO.Pst.PstFileFormatException> or <xref:Bodu.IO.Pst.PstUnsupportedFormatException> directly.

## Example

```csharp
using Bodu.Formats.Outlook;

// Value model: address a property by tag, or by identifier through a typed accessor.
var subjectTag = new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode);
Console.WriteLine($"{subjectTag} named={subjectTag.IsNamed} multi={subjectTag.IsMultiValued}");   // 0x0037001F named=False multi=False
```

```csharp
using Bodu.Formats.Outlook;

// .msg: open, read the conveniences, save the by-value attachments.
using var message = OutlookMessage.OpenRead("invoice.msg");
Console.WriteLine($"{message.Subject} — {message.SenderName} ({message.SentTime:u})");

foreach (OutlookAttachment attachment in message.Attachments)
{
    if (attachment.Method != OutlookAttachmentMethod.ByValue)
        continue;

    using Stream content = attachment.OpenContentStream();
    using FileStream target = File.Create(attachment.FileName ?? "attachment.bin");
    content.CopyTo(target);
}
```

```csharp
using Bodu.Formats.Outlook;

// .pst: walk the folder hierarchy and resolve a named property store-wide.
using var store = OutlookMailStore.OpenRead("archive.pst");

var keywords = new MapiNamedProperty(new Guid("00020329-0000-0000-C000-000000000046"), "Keywords");
store.TryGetNamedPropertyId(keywords, out ushort keywordsId);

foreach (OutlookMailFolder folder in store.RootFolder.EnumerateSubfolders())
{
    foreach (OutlookMailMessage item in folder.EnumerateMessages())
    {
        string[] categories = item.Properties.GetStringArray(keywordsId) ?? Array.Empty<string>();
        Console.WriteLine($"{folder.DisplayName}: {item.Subject} [{string.Join(", ", categories)}]");
    }
}
```

```csharp
using Bodu.Formats.Outlook;
using Bodu.IO.Pst;

// Options and errors: strict validation, tighter limits, and the two failure surfaces.
var options = new OutlookMailStoreReaderOptions
{
    ValidationLevel = PstValidationLevel.Strict,
    MaxEmbeddedMessageDepth = 4,
    MaxInlineAttachmentBytes = 256 * 1024,
};

try
{
    using var store = OutlookMailStore.Open(File.OpenRead("suspect.pst"), options);
    Console.WriteLine(store.DisplayName);
}
catch (PstFileException ex)          { Console.WriteLine($"container: {ex.Error}"); }
catch (OutlookPstFormatException ex) { Console.WriteLine($"messaging: {ex.Message}"); }
```

## Notes

- **One namespace, three packages.** Install `Bodu.Formats.Outlook.Msg` or `Bodu.Formats.Outlook.Pst` for a format; both reference `Bodu.Formats.Outlook` transitively. Reference the model package alone from a library that only handles properties.
- **Conveniences never throw for absence.** `Subject`, `SenderName`, the time stamps, `FileName`, and the rest return `null` when the property is missing; the typed `Get*` accessors do the same. Only structural violations and tripped limits raise the format exceptions.
- **Code pages.** `String8` properties decode through the object's message code page, then its internet code page, then Windows-1252, with child objects inheriting; the readers register `CodePagesEncodingProvider` so Windows code pages resolve on every platform.
- **Compressed RTF is bounded.** `BodyRtf` decompresses `PidTagRtfCompressed` (MS-OXRTFCP) on first access; the declared size lies outside the payload's checksum, so `MaxDecompressedRtfBytes` is enforced before allocation at every validation level. Set `DecompressRtf = false` to keep the raw payload only.
- **Large attachments stream.** A by-value payload above `MaxInlineAttachmentBytes` is left in the container and served only through `OpenContentStream()` — in the `.msg` reader sector by sector under `CompoundReadStrategy.Streaming`, in the `.pst` reader block by block always.
- **Two error styles.** The `.msg` reader wraps container failures in <xref:Bodu.Formats.Outlook.OutlookMsgFormatException>; the `.pst` reader lets <xref:Bodu.IO.Pst.PstFileException> propagate and reserves <xref:Bodu.Formats.Outlook.OutlookPstFormatException> for messaging-level violations. Catch <xref:Bodu.Formats.Outlook.OutlookFormatException> for the family and the container family beneath it.
- **See also:** the [introduction](~/docs/outlook/index.md), [core concepts](~/docs/outlook/concepts.md), and [getting started](~/docs/outlook/getting-started.md); the [Bodu.Formats.Outlook guides](~/guides/outlook/index.md); and the containers <xref:Bodu.IO.Compound> and <xref:Bodu.IO.Pst>.
