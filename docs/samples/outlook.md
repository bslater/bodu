---
title: Runnable samples
---

# Runnable samples

The repository ships a runnable, self-contained sample project for the Outlook message reader
under
[`samples/Formats.Outlook/`](https://github.com/bslater/bodu/tree/master/samples/Formats.Outlook).
It is **offline and deterministic**, and it ships no binary fixture: because the reader is
read-only by design, the sample authors its own `.msg` with `Bodu.IO.Compound` and then reads it
back. It is a member of `bodu.slnx`, built and executed by CI. The README documents every
scenario individually: its intent, what the code does, the output to expect, and the APIs
demonstrated.

Run it from the repository root:

```bash
dotnet run --project samples/Formats.Outlook/Bodu.Formats.Outlook.Msg.Samples.MessageReading
```

## The samples

### Bodu.Formats.Outlook.Msg.Samples.MessageReading

Opening a `.msg` with <xref:Bodu.Formats.Outlook.OutlookMessage> and reading what a consumer
extracting mail actually wants: `IsMsgFile` sniffing the container rather than trusting the
extension, the subject / sender / body conveniences, the recipient table with
<xref:Bodu.Formats.Outlook.OutlookRecipientType> resolved to `To` and `Cc` rather than the raw
integers stored on the wire, and each attachment's metadata plus its bytes through
`OpenContentStream`. A second scenario drops to the layer beneath: the tag-addressed
<xref:Bodu.Formats.Outlook.MapiPropertyCollection> that the conveniences are shorthand for,
reading the subject both ways and showing that a tag the message does not carry reports absence
through `TryGetValue` rather than throwing.

The sample's `MsgAuthor.cs` is worth reading on its own. To avoid committing a binary fixture it
writes a small MS-OXMSG container with the compound-file writer, and in doing so spells out what a
`.msg` is: a `__properties_version1.0` stream of 16-byte tagged records, a
`__substg1.0_XXXXXXXX` value stream per variable-length property, and one child storage per
recipient and attachment. That layout is the thing the reader exists to hide, so seeing it once
makes the rest of the API obvious. *Packages: `Bodu.Formats.Outlook.Msg`, `Bodu.IO.Compound`.*

## Related

- [Outlook guides](../guides/outlook/index.md) — the shared MAPI value model, the `.msg` reader,
  and the `.pst` mail-store reader that shares it.
- [Bodu.Formats.Outlook introduction](../docs/outlook/index.md) — the value model and how the two
  readers sit on it.
- [IO.Pst samples](io-pst.md) — the `.pst` mail store, where the same value model is reached
  through `OutlookMailStore` over a far larger container.
- [IO.Compound samples](io-compound.md) — the OLE2 container reader, editor, and writer that
  backs `.msg`, `.xls`, and `.doc` files.
