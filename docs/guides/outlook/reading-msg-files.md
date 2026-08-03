---
title: Reading .msg files
---

# Reading `.msg` files

<xref:Bodu.Formats.Outlook.OutlookMessage> is the session type: open it over a file path or a stream, read what you need through the conveniences or the raw property surface, and dispose it when done. The session owns the compound-file container and, unless you opt out with `leaveOpen`, the source stream.

## Open a message and read the common fields

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using var message = OutlookMessage.OpenRead("invoice.msg");

Console.WriteLine(message.Subject);
Console.WriteLine(message.SenderName);
Console.WriteLine(message.SenderEmailAddress);
Console.WriteLine(message.SentTime);
Console.WriteLine(message.BodyText);
```

Every convenience is nullable and returns <see langword="null" /> when the message does not carry the underlying property — real-world messages omit fields freely.

## Sniff before opening

`IsMsgFile` checks that a stream is an OLE2 compound file whose root carries the message property stream, restoring the stream position afterwards. The conventional root class identifier is *not* required — real-world writers frequently omit it.

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using FileStream stream = File.OpenRead("maybe-a-message.bin");
if (OutlookMessage.IsMsgFile(stream))
{
    using var message = OutlookMessage.OpenRead(stream, leaveOpen: true);
    Console.WriteLine(message.Subject);
}
```

## Recipients and attachments

Recipients and attachments are materialized lazily from their indexed child storages, in index order.

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using var message = OutlookMessage.OpenRead("invoice.msg");

foreach (OutlookRecipient recipient in message.Recipients)
    Console.WriteLine($"{recipient.RecipientType}: {recipient.DisplayName} <{recipient.EmailAddress}>");

foreach (OutlookAttachment attachment in message.Attachments)
{
    if (attachment.Method == OutlookAttachmentMethod.ByValue)
    {
        using Stream content = attachment.OpenContentStream();
        using FileStream target = File.Create(attachment.FileName ?? "attachment.bin");
        content.CopyTo(target);
    }
    else if (attachment.Method == OutlookAttachmentMethod.EmbeddedMessage)
    {
        OutlookMessage nested = attachment.OpenMessage();
        Console.WriteLine($"attached message: {nested.Subject}");
    }
}
```

A nested message shares the root session's container and named-property mapping: disposing it is a no-op, and it becomes unusable for container-backed reads once the root session is disposed.

## Bodies

Three body conveniences cover the forms a message stores:

- `BodyText` — the plain-text `PidTagBody`.
- `BodyHtml` — the `PidTagHtml` payload, decoded through the message's internet code page.
- `BodyRtf` — the `PidTagRtfCompressed` payload, decompressed per MS-OXRTFCP. Set <xref:Bodu.Formats.Outlook.OutlookMessageReaderOptions.DecompressRtf> to `false` to suppress the decompression; the raw payload stays reachable through the property collection.

## Validation levels

`Open` accepts <xref:Bodu.Formats.Outlook.OutlookMessageReaderOptions>, whose `ValidationLevel` reuses the container's <xref:Bodu.IO.Compound.CompoundValidationLevel>:

- **Compatible** (default) — malformed properties are omitted and decoding continues; the tolerance real-world messages need.
- **Strict** — structural problems (a missing value stream, a misaligned property stream, inconsistent recipient/attachment counts) throw <xref:Bodu.Formats.Outlook.OutlookMsgFormatException>.
