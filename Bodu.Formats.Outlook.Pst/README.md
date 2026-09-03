# Bodu.Formats.Outlook.Pst

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A **read-only reader for the Outlook personal-folders format** (`.pst` / MS-PST,
Unicode format), built on [`Bodu.IO.Pst`](../Bodu.IO.Pst) — the node-database
container reader — and the shared [`Bodu.Formats.Outlook`](../Bodu.Formats.Outlook)
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
        Console.WriteLine($"  {message.Subject} — {message.SenderName}");

        foreach (OutlookRecipient recipient in message.Recipients)
            Console.WriteLine($"    {recipient.RecipientType}: {recipient.EmailAddress}");

        foreach (OutlookMailAttachment attachment in message.Attachments)
            Console.WriteLine($"    attachment: {attachment.FileName} ({attachment.Size} bytes)");
    }
}
```

## What is read

- The folder hierarchy from the root folder down — hierarchy, contents, and
  associated-contents tables, streamed row by row (search folders, which are
  Outlook runtime state, are excluded).
- Every object's property context — fixed, variable-length, and multi-valued
  properties, Unicode and code-page (ANSI) strings alike, with per-object
  code-page resolution inherited down the store → message → attachment chain.
- Recipient tables (row-resident properties) and attachment objects, including
  by-value content payloads and nested embedded messages (recursively).
- The store-wide name-to-id map (node `0x61`), resolved bidirectionally.
- The message bodies: plain text, HTML, and RTF (decompressed per MS-OXRTFCP).

## Out of scope

- Writing or editing `.pst` files.
- The legacy ANSI variant and OST files (recognized and rejected).
- MAPI session semantics (`IMsgStore` emulation, search-folder population).
- RTF→HTML/Text de-encapsulation (MS-OXRTFEX) — consumers receive the RTF verbatim.
- TNEF (`winmail.dat`), S/MIME decryption, and OLE-attachment rendering.
