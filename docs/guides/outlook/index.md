---
title: Bodu.Formats.Outlook guides
---

# Bodu.Formats.Outlook guides

Recipe-style walk-throughs for the Outlook format readers: **Bodu.Formats.Outlook**, the shared MAPI value model, and **Bodu.Formats.Outlook.Msg**, the read-only reader for the Outlook message format (`.msg` / MS-OXMSG). A message is opened as a disposable session exposing every decoded MAPI property, the recipient and attachment tables, nested attached messages, named-property resolution, and the text, HTML, and compressed-RTF bodies.

A `.msg` file *is* an OLE2 compound file: the message's properties live in a `__properties_version1.0` stream of fixed 16-byte records, variable-length values live in `__substg1.0_` streams named by their property tag, and recipients and attachments are indexed child storages. The container is read by <xref:Bodu.IO.Compound.CompoundFile>, on which the reader is built.

## How the library works

<xref:Bodu.Formats.Outlook.OutlookMessage> opens the container and eagerly decodes the root property stream, so the property surface is available without further I/O. Recipients, attachments, and the named-property mapping are materialized lazily on first access. Attachment payloads stream directly from the container, and an attached message opens recursively as a nested session sharing the root's container.

Both packages share the flattened `Bodu.Formats.Outlook` namespace: the value model — <xref:Bodu.Formats.Outlook.MapiPropertyTag>, <xref:Bodu.Formats.Outlook.MapiProperty>, the tag-addressed <xref:Bodu.Formats.Outlook.MapiPropertyCollection>, and <xref:Bodu.Formats.Outlook.MapiNamedProperty> — carries no container knowledge, so the `.pst` mail-store reader, `Bodu.Formats.Outlook.Pst` (over `Bodu.IO.Pst`; see the [Bodu.IO.Pst introduction](../../docs/io-pst/index.md)), shares it unchanged.

> These guides cover the read path only — the reader never writes `.msg` files, emulates a MAPI session, or de-encapsulates RTF into HTML.

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| <xref:Bodu.Formats.Outlook> | The `OutlookMessage` and `OutlookMailStore` sessions with their folder/recipient/attachment/body surfaces, the two reader-options types, the shared MAPI value model, and the `OutlookFormatException` family. | [Reading .msg files](reading-msg-files.md) · [Reading .pst mail stores](reading-pst-mail-stores.md) · [Properties and named properties](properties-and-named-properties.md) · [Reader options and resource limits](reader-options-and-limits.md) |

## Guides

- **[Reading .msg files](reading-msg-files.md)** — opening a message from a path or stream, the scalar and body conveniences, recipients, attachments, and nested messages.
- **[Reading .pst mail stores](reading-pst-mail-stores.md)** — opening a mail store, walking the folder hierarchy, message conveniences and bodies, recipients, attachments and embedded messages, store-wide named properties, and the two exception families.
- **[Properties and named properties](properties-and-named-properties.md)** — the raw property surface, typed accessors, tags and wire types, and resolving named properties.
- **[Reader options and resource limits](reader-options-and-limits.md)** — every option on both readers, what `Strict` changes, and what surfaces when a limit trips.
