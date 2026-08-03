# Bodu.Formats.Outlook.Msg

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A **read-only reader for the Outlook message format** (`.msg` / MS-OXMSG), built on
[`Bodu.IO.Compound`](../Bodu.IO.Compound) — a `.msg` file *is* an OLE2 compound file —
and the shared [`Bodu.Formats.Outlook`](../Bodu.Formats.Outlook) MAPI value model.

A message is opened as a disposable session exposing every decoded MAPI property, the
recipient and attachment tables, nested attached messages, named-property resolution,
and the text, HTML, and compressed-RTF (MS-OXRTFCP) bodies. There is no MAPI session
emulation and no message authoring.

```csharp
using Bodu.Formats.Outlook;

using var message = OutlookMessage.OpenRead("invoice.msg");

Console.WriteLine(message.Subject);
Console.WriteLine(message.SenderEmailAddress);

foreach (OutlookRecipient recipient in message.Recipients)
    Console.WriteLine($"  {recipient.RecipientType}: {recipient.EmailAddress}");

foreach (OutlookAttachment attachment in message.Attachments)
    Console.WriteLine($"  attachment: {attachment.FileName} ({attachment.Size} bytes)");
```

## What is read

- The message property stream (`__properties_version1.0`) and every
  `__substg1.0_…` value stream — fixed, variable-length, and multi-valued
  properties, Unicode and code-page (ANSI) strings alike.
- Recipient and attachment storages, including attachment payload streams and
  nested attached messages (recursively).
- The named-property mapping (`__nameid_version1.0`), resolved bidirectionally.
- The message bodies: plain text, HTML, and RTF (decompressed per MS-OXRTFCP).

## Out of scope

- Writing or editing `.msg` files.
- MAPI session semantics (`IMessage` emulation, store behaviour).
- RTF→HTML/Text de-encapsulation (MS-OXRTFEX) — consumers receive the RTF verbatim.
- TNEF (`winmail.dat`), S/MIME decryption, and OLE-attachment rendering.
