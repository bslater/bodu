---
title: Reading .pst mail stores
---

# Reading `.pst` mail stores

<xref:Bodu.Formats.Outlook.OutlookMailStore> is the session type for a personal-folders file: open it over a path or a stream, walk the folder hierarchy down from `RootFolder`, read each message through the conveniences or the raw property surface, and dispose it when done. The session owns the <xref:Bodu.IO.Pst.PstFile> container beneath it and, unless you opt out with `leaveOpen`, the source stream. Every folder, message, recipient, and attachment you obtain is a view bound to the session's lifetime — members called after `Dispose` throw <xref:System.ObjectDisposedException>.

Reads are lazy throughout. Opening parses only the container header; folder enumerations stream the hierarchy and contents tables one row block at a time; each object's properties decode once, on first access. The session is single-threaded — do not call its members, or those of any view, concurrently.

The samples below run against the two fixtures the [runnable PST sample](../../samples/io-pst.md) ships — `sample1.pst` (Unicode format) copied as `archive.pst`, and `sample2.pst` (ANSI format) copied as `legacy.pst`. The quoted output is what they print.

## Open a store and read the store object

```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");

Console.WriteLine(store.DisplayName);                       // PidTagDisplayName of the store object
Console.WriteLine(store.RootFolder.HasSubfolders);

foreach (OutlookMailFolder folder in store.RootFolder.EnumerateSubfolders())
    Console.WriteLine($"{folder.DisplayName} [{folder.ContainerClass ?? "-"}]");

// sample1
// True
// Top of Outlook data file [-]
// Search Root [-]
```

Three factories open a session: `OpenRead(path)` owns the file it opens; `OpenRead(stream, leaveOpen)` reads from the stream's current position; `Open(stream, options, leaveOpen)` additionally takes an <xref:Bodu.Formats.Outlook.OutlookMailStoreReaderOptions> (see [Reader options and resource limits](reader-options-and-limits.md)). The stream must be readable **and** seekable — the container seeks on demand and never buffers the file whole — and a stream that is not throws <xref:System.ArgumentException>.

`Properties` is the store object's tag-addressed <xref:Bodu.Formats.Outlook.MapiPropertyCollection>, decoded on first access; `DisplayName` is its `PidTagDisplayName`. The store's own code page, resolved from those properties, is the encoding every folder inherits unless it declares one of its own.

## Sniff before opening

```csharp
using Bodu.Formats.Outlook;
using Bodu.IO.Pst;

using FileStream stream = File.OpenRead("maybe-a-store.bin");
if (!OutlookMailStore.IsPstFile(stream))
    return;

var options = new OutlookMailStoreReaderOptions { ValidationLevel = PstValidationLevel.Strict };
using var store = OutlookMailStore.Open(stream, options, leaveOpen: true);
Console.WriteLine(store.DisplayName);
```

`IsPstFile` checks only the `!BDN` magic and restores the stream position, so it is cheap to call ahead of a full open. It answers `true` for *any* PST variant: Unicode and ANSI files then open, while the 4 KiB-page OST variant is rejected by the container with <xref:Bodu.IO.Pst.PstUnsupportedFormatException>.

## Pattern 1 — inventory a store

`RootFolder` is structural: the folders a user sees hang beneath *Top of Outlook data file* (the IPM subtree), and the search root sits beside it. Walk `EnumerateSubfolders` recursively and print what each folder declares about itself.

```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");

Console.WriteLine(store.DisplayName);
Inventory(store.RootFolder, depth: 0);

static void Inventory(OutlookMailFolder folder, int depth)
{
    string pad = new(' ', depth * 2);
    Console.WriteLine($"{pad}{folder.DisplayName ?? "(unnamed)"} [{folder.ContainerClass ?? "-"}] " +
        $"messages={folder.MessageCount?.ToString() ?? "?"} unread={folder.UnreadCount?.ToString() ?? "?"} " +
        $"subfolders={folder.HasSubfolders}");

    foreach (OutlookMailFolder child in folder.EnumerateSubfolders())
        Inventory(child, depth + 1);
}

// sample1
//  [-] messages=0 unread=0 subfolders=True
//   Top of Outlook data file [-] messages=0 unread=0 subfolders=True
//     Deleted Items [-] messages=0 unread=0 subfolders=False
//     Sample1 [IPF.Note] messages=1 unread=0 subfolders=False
//   Search Root [-] messages=0 unread=0 subfolders=False
```

`MessageCount`, `UnreadCount`, and `HasSubfolders` are the values the folder *declares* (`PidTagContentCount`, `PidTagContentUnreadCount`, `PidTagSubfolders`), not counts of enumerated rows — a writer that skipped the bookkeeping leaves them `null` (or `false`). `ContainerClass` is `PidTagContainerClass` (`IPF.Note` for a mail folder, `IPF.Contact`, `IPF.Appointment`, …) and is typically absent on the structural folders. A folder whose hierarchy or contents table node is missing enumerates empty rather than failing, matching real-world stores that omit empty tables.

> [!NOTE]
> Search folders are Outlook runtime state, not archive content: `EnumerateSubfolders` skips them at every validation level, so the *SPAM Search Folder* and *ItemProcSearch* entries that the container's raw hierarchy table lists (see [Reading nodes and tables](../io-pst/reading-nodes-and-tables.md)) never appear here.

## Pattern 2 — export every message's headers

The later patterns want every message in the store, so start with a depth-first helper. Enumeration is streaming: the helper yields each message as its table row is read, and nothing is buffered ahead.

```csharp
// Every message beneath a folder, depth first
static IEnumerable<OutlookMailMessage> AllMessages(OutlookMailFolder folder)
{
    foreach (OutlookMailMessage message in folder.EnumerateMessages())
        yield return message;

    foreach (OutlookMailFolder child in folder.EnumerateSubfolders())
    {
        foreach (OutlookMailMessage message in AllMessages(child))
            yield return message;
    }
}
```

`TransportMessageHeaders` is `PidTagTransportMessageHeaders` — the RFC 5322 header block exactly as the message was received. Messages that never crossed a transport (drafts, items created locally) do not carry it, so fall back to the MAPI scalars.

```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");
using var output = new StreamWriter("headers.txt");

foreach (OutlookMailMessage message in AllMessages(store.RootFolder))
{
    if (message.TransportMessageHeaders is string headers)
    {
        output.WriteLine(headers.TrimEnd());   // the RFC 5322 header block as received
    }
    else
    {
        // Messages that never crossed SMTP (drafts, items created locally) carry no
        // transport headers; synthesize the essentials from the MAPI properties.
        output.WriteLine($"Message-ID: {message.InternetMessageId ?? "(none)"}");
        output.WriteLine($"From: {message.SenderName} <{message.SenderEmailAddress}>");
        output.WriteLine($"Subject: {message.Subject}");
        output.WriteLine($"Date: {message.SentTime ?? message.ReceivedTime:R}");
    }

    output.WriteLine($"X-Message-Class: {message.MessageClass}");
    output.WriteLine();
}

// headers.txt (excerpt):
// Received: from TK5EX14MBXC114.redmond.corp.microsoft.com ([169.254.3.48]) by
//  TK5EX14MLTC103.redmond.corp.microsoft.com ([157.54.79.174]) with mapi; Mon,
//  15 Mar 2010 10:12:07 -0700
// From: Terry Mahaffey <terrymah@microsoft.com>
// Subject: Here is a sample message
// Date: Mon, 15 Mar 2010 10:12:05 -0700
// Message-ID: <B2FDDB8BE384C94794441DB4A7F3D8B804AE624B@TK5EX14MBXC114.redmond.corp.microsoft.com>
// ...
// X-Message-Class: IPM.Note
```

The scalar conveniences on <xref:Bodu.Formats.Outlook.OutlookMailMessage> are all nullable views over `Properties`:

| Convenience | Property | Notes |
|---|---|---|
| `Subject` | `PidTagSubject` | The MS-PST subject-prefix marker (U+0001 plus a length byte) is stripped; the raw value stays in `Properties`. |
| `SenderName` / `SenderEmailAddress` | `PidTagSenderName` / `PidTagSenderEmailAddress` | The address is often an X.500 DN for Exchange-originated mail. |
| `MessageClass` | `PidTagMessageClass` | `IPM.Note`, `IPM.Contact`, `IPM.Appointment`, … |
| `InternetMessageId` | `PidTagInternetMessageId` | The RFC 5322 `Message-ID`. |
| `TransportMessageHeaders` | `PidTagTransportMessageHeaders` | Absent on messages that never crossed a transport. |
| `SentTime` | `PidTagClientSubmitTime` | |
| `ReceivedTime` | `PidTagMessageDeliveryTime` | |
| `EmbeddedDepth` | — | `0` for a folder-level message, one more per `OpenMessage()` level. |

## Bodies

```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");

foreach (OutlookMailMessage message in AllMessages(store.RootFolder))
{
    string? text = message.BodyText;                  // PidTagBody
    string? html = message.BodyHtml;                  // PidTagHtml, decoded through the internet code page
    string? rtf = message.BodyRtf;                    // PidTagRtfCompressed, decompressed per MS-OXRTFCP

    Console.WriteLine($"{message.Subject}: text={text?.Length.ToString() ?? "-"} html={html?.Length.ToString() ?? "-"} rtf={rtf?.Length.ToString() ?? "-"}");
    Console.WriteLine((text ?? html ?? rtf ?? "(no body)").ReplaceLineEndings(" ").Trim());
}

// Here is a sample message: text=79 html=1701 rtf=-
// With a sample attachment. It’s my daughter and our puppy. Aren’t they cute?
```

`BodyHtml` and `BodyRtf` are decoded once and cached on the view. `BodyRtf` returns `null` when the property is absent *or* when <xref:Bodu.Formats.Outlook.OutlookMailStoreReaderOptions.DecompressRtf> is `false`; a compressed payload that is malformed, fails its checksum, or would decompress past `MaxDecompressedRtfBytes` throws <xref:Bodu.Formats.Outlook.OutlookPstFormatException>.

## Recipients

Recipients are row-resident: each recipient-table row decodes directly into an <xref:Bodu.Formats.Outlook.OutlookRecipient> — the same type the `.msg` reader hands out — in table order.

```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");

foreach (OutlookMailMessage message in AllMessages(store.RootFolder))
{
    Console.WriteLine($"{message.Subject} from {message.SenderName} <{message.SenderEmailAddress}>");
    foreach (OutlookRecipient recipient in message.Recipients)
        Console.WriteLine($"  {recipient.RecipientType}: {recipient.DisplayName} <{recipient.EmailAddress}> ({recipient.AddressType})");
}

// Here is a sample message from Terry Mahaffey </O=MICROSOFT/OU=NORTHAMERICA/CN=RECIPIENTS/CN=TERRYMAH1>
//   To: Terry Mahaffey </O=MICROSOFT/OU=Northamerica/cn=Recipients/cn=terrymah1> (EX)
```

## Pattern 3 — extract attachments recursively

<xref:Bodu.Formats.Outlook.OutlookMailAttachment> exposes `Method`, `FileName` (the long form, falling back to the short), `ContentId`, `MimeTag`, and `Size`, plus two content accessors that are method-specific: `OpenContentStream()` serves a by-value payload and `OpenMessage()` serves an embedded message. Each throws <xref:System.NotSupportedException> for the other method kinds, so branch on `Method` first.

```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");
Directory.CreateDirectory("attachments");

foreach (OutlookMailMessage message in AllMessages(store.RootFolder))
    Extract(message);

static void Extract(OutlookMailMessage message)
{
    foreach (OutlookMailAttachment attachment in message.Attachments)
    {
        switch (attachment.Method)
        {
            case OutlookAttachmentMethod.ByValue:
                string name = Path.GetFileName(attachment.FileName ?? $"attachment-{Guid.NewGuid():N}.bin");
                using (Stream content = attachment.OpenContentStream())
                using (FileStream target = File.Create(Path.Combine("attachments", name)))
                    content.CopyTo(target);
                Console.WriteLine($"{message.Subject}: saved {name} ({attachment.Size} bytes, {attachment.MimeTag ?? "no MIME tag"})");
                break;

            case OutlookAttachmentMethod.EmbeddedMessage:
                OutlookMailMessage nested = attachment.OpenMessage();
                Console.WriteLine($"{message.Subject}: embedded '{nested.Subject}' at depth {nested.EmbeddedDepth}");
                Extract(nested);   // MaxEmbeddedMessageDepth bounds the recursion
                break;

            default:
                Console.WriteLine($"{message.Subject}: {attachment.Method} attachment {attachment.FileName} has no payload in the store");
                break;
        }
    }
}

// Here is a sample message: saved leah_thumper.jpg (96808 bytes, no MIME tag)
```

A few contracts worth knowing:

- **`Size` is what the writer recorded.** It reports `PidTagAttachSize` when present and only falls back to the payload length otherwise — and `PidTagAttachSize` conventionally counts the whole attachment object, not just the bytes. The sample's `leah_thumper.jpg` declares 96,808 bytes while `OpenContentStream()` yields 93,142. Measure the stream (`Length`) when the exact payload size matters; the fallback reads the store's index structures, so a deferred payload is never materialized to price it.
- **Large payloads never sit in memory whole.** A by-value payload at or below <xref:Bodu.Formats.Outlook.OutlookMailStoreReaderOptions.MaxInlineAttachmentBytes> (1 MiB by default) is decoded into `Properties` and served from those bytes; a larger one stays in the store and `OpenContentStream()` reads it block by block. The stream is bound to the session — dispose it before the session.
- **`Method` is inferred when the writer omitted it.** An attachment without `PidTagAttachMethod` reports `ByValue` when it carries a by-value payload and `None` otherwise; an undefined value throws <xref:Bodu.Formats.Outlook.OutlookPstFormatException> under strict validation.
- **Embedded messages inherit their code page.** `OpenMessage()` returns a view over the attachment's message-typed subnode whose `String8` properties decode with the attachment's encoding — which in turn inherited from the message, the folder, and the store, each level overriding only when it declares a code page of its own. `EmbeddedDepth` is one more than the owner's; opening past `MaxEmbeddedMessageDepth` throws <xref:Bodu.Formats.Outlook.OutlookPstFormatException> at every validation level.

## Folder-associated messages

Folders also hold *associated* items — hidden folder-associated-information (FAI) messages such as view settings, rules, and category lists. They are excluded from `EnumerateMessages` and reachable through `EnumerateAssociatedMessages`, which streams the folder's associated-contents table the same way.

```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");

foreach (OutlookMailFolder folder in AllFolders(store.RootFolder))
{
    foreach (OutlookMailMessage item in folder.EnumerateAssociatedMessages())
        Console.WriteLine($"{folder.DisplayName}: {item.MessageClass} — {item.Subject ?? "(no subject)"}");
}

static IEnumerable<OutlookMailFolder> AllFolders(OutlookMailFolder folder)
{
    yield return folder;
    foreach (OutlookMailFolder child in folder.EnumerateSubfolders())
    {
        foreach (OutlookMailFolder descendant in AllFolders(child))
            yield return descendant;
    }
}
```

(The sample store carries none, so this prints nothing.)

## Pattern 4 — find messages by named property

Identifiers at or above `0x8000` are assigned per file, so the durable identity — a property-set GUID plus a number or a name — has to be resolved to *this* store's identifier first. The mapping lives in one name-to-id map node per store, so <xref:Bodu.Formats.Outlook.OutlookMailStore> resolves it store-wide and one lookup serves every message and attachment of the session.

```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");

// PS_PUBLIC_STRINGS "Keywords" — the category list Outlook shows as color categories.
var keywords = new MapiNamedProperty(new Guid("00020329-0000-0000-C000-000000000046"), "Keywords");

if (!store.TryGetNamedPropertyId(keywords, out ushort id))
{
    Console.WriteLine("This store never assigned an identifier to Keywords.");
    return;
}

Console.WriteLine($"Keywords is 0x{id:X4} in this store");

foreach (OutlookMailMessage message in AllMessages(store.RootFolder))
{
    string[]? categories = message.Properties.GetStringArray(id);
    Console.WriteLine($"{message.Subject}: {(categories is { Length: > 0 } ? string.Join(", ", categories) : "(no categories)")}");
}

// Keywords is 0x8012 in this store
// Here is a sample message: Green Category, Blue Category
```

The reverse direction answers "what does this named tag mean in this file?":

```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");

var tag = new MapiPropertyTag(0x8000, MapiPropertyType.Unicode);
if (store.TryGetPropertyName(tag, out MapiNamedProperty name))
    Console.WriteLine($"{tag} is {name}");

// 0x8000001F is {00062002-0000-0000-c000-000000000046}:0x00008205
```

The whole property surface — typed accessors, tags, wire types, multi-valued properties — is covered in [Properties and named properties](properties-and-named-properties.md); it is identical on both readers.

## ANSI and Unicode stores

The mail store opens both PST formats through the same call, and nothing on its surface changes between them. What differs is beneath: an ANSI file (`wVer` 14/15) uses 32-bit structures and typically stores strings as code-page (`String8`) values rather than UTF-16, so the reader decodes them through the object's resolved code page — `PidTagMessageCodepage`, then `PidTagInternetCodepage`, then the inherited encoding, then Windows-1252. The session does not expose the format itself; when you need it, ask the container.

```csharp
using Bodu.Formats.Outlook;
using Bodu.IO.Pst;

using FileStream stream = File.OpenRead("legacy.pst");

PstFileFormat format;
using (PstFile container = PstFile.OpenRead(stream, leaveOpen: true))
    format = container.Format;                                  // Unicode or Ansi
stream.Position = 0;

using var store = OutlookMailStore.OpenRead(stream);
Console.WriteLine($"{format}: {store.DisplayName}");

foreach (OutlookMailMessage message in AllMessages(store.RootFolder))
{
    int? codePage = message.Properties.GetInt32(MapiPropertyIds.MessageCodepage);
    Console.WriteLine($"  {message.Subject} — {message.SenderName} (code page {codePage?.ToString() ?? "inherited"})");
}

// Ansi: sample2
//   Here is a sample message — Terry Mahaffey (code page 1252)
```

The Windows code pages that dominate real-world mail are available on every platform because the reader registers `CodePagesEncodingProvider` — that is what the `System.Text.Encoding.CodePages` dependency is for.

## Errors: two families, not one

The `.pst` reader reports failures through two unrelated exception hierarchies, and — unlike the `.msg` reader, which wraps container faults — it lets the container's exceptions propagate **unwrapped**:

| Exception | Raised for | Where |
|---|---|---|
| <xref:Bodu.IO.Pst.PstFileFormatException> | The stream is not structurally a PST file, or a page, block, tree, heap, or context is malformed; `Error` carries the <xref:Bodu.IO.Pst.PstFileError> category. Also `LimitExceeded` when a node payload exceeds `MaxNodeDataLength`. | `Open`, and any property, folder, message, or attachment read. |
| <xref:Bodu.IO.Pst.PstUnsupportedFormatException> | The 4 KiB-page OST variant, or Windows Information Protection encryption. | `Open`. |
| <xref:Bodu.IO.Pst.PstFileException> (base) | Catch-all for the container family. | Everywhere the two above apply. |
| <xref:Bodu.Formats.Outlook.OutlookPstFormatException> | A messaging-level violation of MS-PST: under strict validation a hierarchy, contents, recipient, or attachment row that references no valid node, an undecodable property value, an undefined attachment method, or a malformed name-to-id map; at every level a missing by-value payload or embedded-message subnode, a tripped embedded-message depth, or a malformed / oversized compressed-RTF body. | Folder and message enumeration, `Properties`, `Method`, `OpenContentStream`, `OpenMessage`, `BodyRtf`. |
| <xref:System.NotSupportedException> | A content accessor that does not match the attachment's `Method`. | `OpenContentStream`, `OpenMessage`. |
| <xref:System.ObjectDisposedException> | Any member after the session is disposed. | Everywhere. |
| <xref:System.ArgumentException> | The stream is not readable and seekable. | `Open` / `OpenRead(stream)`. |

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

// With a Word document renamed to suspect.pst:
// Container corruption: InvalidHeader — The stream does not begin with a valid PST header.
```

> [!TIP]
> Under the default `Compatible` level the reader is forgiving: a malformed property is omitted, a table row that references no valid node is skipped, and an undefined attachment method is inferred. Reach for `Strict` when you are validating a corpus and want those conditions to surface as `OutlookPstFormatException` instead — see [Reader options and resource limits](reader-options-and-limits.md).

## API summary

| Type | Members |
|---|---|
| <xref:Bodu.Formats.Outlook.OutlookMailStore> | `OpenRead(path)` · `OpenRead(stream, leaveOpen)` · `Open(stream, options, leaveOpen)` · `IsPstFile(stream)` · `Properties` · `DisplayName` · `RootFolder` · `TryGetNamedPropertyId` · `TryGetPropertyName` · `Dispose` |
| <xref:Bodu.Formats.Outlook.OutlookMailFolder> | `Properties` · `DisplayName` · `ContainerClass` · `MessageCount` · `UnreadCount` · `HasSubfolders` · `EnumerateSubfolders()` · `EnumerateMessages()` · `EnumerateAssociatedMessages()` |
| <xref:Bodu.Formats.Outlook.OutlookMailMessage> | `Properties` · `Subject` · `SenderName` · `SenderEmailAddress` · `MessageClass` · `InternetMessageId` · `TransportMessageHeaders` · `SentTime` · `ReceivedTime` · `BodyText` · `BodyHtml` · `BodyRtf` · `Recipients` · `Attachments` · `EmbeddedDepth` |
| <xref:Bodu.Formats.Outlook.OutlookMailAttachment> | `Properties` · `Method` · `FileName` · `ContentId` · `MimeTag` · `Size` · `OpenContentStream()` · `OpenMessage()` |
| <xref:Bodu.Formats.Outlook.OutlookRecipient> | `RecipientType` · `DisplayName` · `EmailAddress` · `AddressType` · `Properties` |

## Where to go next

- [Reader options and resource limits](reader-options-and-limits.md) — every option on both readers, what `Strict` changes, and what surfaces when a limit trips.
- [Properties and named properties](properties-and-named-properties.md) — the raw property surface, typed accessors, and tags; identical on both readers.
- [Reading .msg files](reading-msg-files.md) — the single-message reader that shares this value model.
- [Bodu.Formats.Outlook core concepts](../../docs/outlook/concepts.md) — sessions and views, code pages, compressed RTF, and the resource limits.
- [Bodu.IO.Pst guides](../io-pst/index.md) — the node database beneath the mail store, for when you need raw nodes and tables.
- [Runnable PST sample](../../samples/io-pst.md) — the fixtures these samples ran against.
- [Bodu.Formats.Outlook guides](index.md) — every guide in this topic.
