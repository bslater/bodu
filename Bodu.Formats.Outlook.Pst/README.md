# Bodu.Formats.Outlook.Pst

> **API stability - Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

A **read-only reader for the Outlook personal-folders format** (`.pst` / MS-PST,
Unicode and ANSI formats), built on [`Bodu.IO.Pst`](../Bodu.IO.Pst) - the node-database
container reader - and the shared [`Bodu.Formats.Outlook`](../Bodu.Formats.Outlook)
MAPI value model it has in common with the `.msg` reader.

A mail store is opened as a disposable session exposing the store properties, the
folder hierarchy, and every message with its decoded MAPI properties, recipients,
attachments (including nested embedded messages), store-wide named-property
resolution, and the text, HTML, and compressed-RTF (MS-OXRTFCP) bodies. There is no
MAPI session emulation and no store authoring.

```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");

foreach (OutlookMailFolder folder in store.RootFolder.EnumerateSubfolders())
{
    Console.WriteLine(folder.DisplayName);

    foreach (OutlookMailMessage message in folder.EnumerateMessages())
    {
        Console.WriteLine($"  {message.Subject} - {message.SenderName}");

        foreach (OutlookRecipient recipient in message.Recipients)
            Console.WriteLine($"    {recipient.RecipientType}: {recipient.EmailAddress}");

        foreach (OutlookMailAttachment attachment in message.Attachments)
            Console.WriteLine($"    attachment: {attachment.FileName} ({attachment.Size} bytes)");
    }
}
```

## What is read

- The folder hierarchy from the root folder down - hierarchy, contents, and
  associated-contents tables, streamed row by row (search folders, which are
  Outlook runtime state, are excluded).
- Every object's property context - fixed, variable-length, and multi-valued
  properties, Unicode and code-page (ANSI) strings alike, with per-object
  code-page resolution inherited down the store → message → attachment chain.
- Recipient tables (row-resident properties) and attachment objects, including
  by-value content payloads and nested embedded messages (recursively). A payload
  larger than `OutlookMailStoreReaderOptions.MaxInlineAttachmentBytes` (1 MiB by
  default) is not decoded into the property collection; `OpenContentStream`
  streams it from the store block by block.
- The store-wide name-to-id map (node `0x61`), resolved bidirectionally.
- The message bodies: plain text, HTML, and RTF (decompressed per MS-OXRTFCP).
- Damaged stores, on request: with `OutlookMailStoreReaderOptions.OmitUnresolvableValues`
  a property value or recipient cell whose reference does not resolve is left out
  rather than failing its folder, message or attachment, and
  `ValueOmittedCallback` reports each value the reader omits, so a salvaged object
  can be told from a clean one.

## Errors

Every failure to read a store surfaces as `OutlookPstFormatException`, part of the
`OutlookFormatException` family it shares with the `.msg` reader, so one handler
catches both formats. A container fault - a source that is not a PST file, an
unsupported variant, or a corrupt page, block, heap, or table met by any read,
a content stream's included - keeps the `Bodu.IO.Pst` exception as
`InnerException`: a `PstFileFormatException`, `PstUnsupportedFormatException`, or
`PstNodeNotFoundException`, whose `Error` names the `PstFileError` category. A
messaging-level violation (a row that names no valid node under strict validation,
a malformed name-to-id map, a tripped limit) has no inner exception.

## Out of scope

- Writing or editing `.pst` files.
- OST files (recognized and rejected, with `PstUnsupportedFormatException` inside
  the `OutlookPstFormatException`).
- MAPI session semantics (`IMsgStore` emulation, search-folder population).
- RTF→HTML/Text de-encapsulation (MS-OXRTFEX) - consumers receive the RTF verbatim.
- TNEF (`winmail.dat`), S/MIME decryption, and OLE-attachment rendering.
