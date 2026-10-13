---
title: Bodu.Formats.Outlook guides
---

# Bodu.Formats.Outlook guides

Recipe-style walk-throughs for the Outlook format readers: **Bodu.Formats.Outlook**, the shared MAPI value model; **Bodu.Formats.Outlook.Msg**, the read-only reader for the Outlook message format (`.msg` / MS-OXMSG); and **Bodu.Formats.Outlook.Pst**, the read-only reader for the Outlook personal-folders format (`.pst` / MS-PST). A message is opened as a disposable session exposing every decoded MAPI property, the recipient and attachment tables, nested attached messages, named-property resolution, and the text, HTML, and compressed-RTF bodies. A mail store is opened as a disposable session exposing the store properties, the folder hierarchy, and every message in it with the same surface, embedded messages and store-wide named-property resolution included.

A `.msg` file *is* an OLE2 compound file: the message's properties live in a `__properties_version1.0` stream of fixed 16-byte records, variable-length values live in `__substg1.0_` streams named by their property tag, and recipients and attachments are indexed child storages. The container is read by <xref:Bodu.IO.Compound.CompoundFile>, on which the reader is built. A `.pst` file is a node database: each folder, message, and attachment is a node whose properties live in a property context and whose children are listed in table contexts. The container is read by <xref:Bodu.IO.Pst.PstFile>, on which the mail-store reader is built.

## How the library works

<xref:Bodu.Formats.Outlook.OutlookMessage> opens the container and eagerly decodes the root property stream, so the property surface is available without further I/O. Recipients, attachments, and the named-property mapping are materialized lazily on first access. Attachment payloads stream directly from the container, and an attached message opens recursively as a nested session sharing the root's container.

<xref:Bodu.Formats.Outlook.OutlookMailStore> opens the container and parses only its header. Folder and message enumerations stream table rows, each store, folder, message, and attachment decodes its properties once on first access, attachment payloads stream from the container, and an embedded message opens as a view bound to the same session.

The three packages share the flattened `Bodu.Formats.Outlook` namespace: the value model - <xref:Bodu.Formats.Outlook.MapiPropertyTag>, <xref:Bodu.Formats.Outlook.MapiProperty>, the tag-addressed <xref:Bodu.Formats.Outlook.MapiPropertyCollection>, and <xref:Bodu.Formats.Outlook.MapiNamedProperty> - carries no container knowledge, so the `.msg` reader and the `.pst` mail-store reader (over `Bodu.IO.Pst`; see the [Bodu.IO.Pst introduction](../../docs/io-pst/index.md)) share it unchanged.

> These guides cover the read path only - the readers never write `.msg` or `.pst` files, emulate a MAPI session, or de-encapsulate RTF into HTML.

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| <xref:Bodu.Formats.Outlook> | The `OutlookMessage` and `OutlookMailStore` sessions with their folder/recipient/attachment/body surfaces, the two reader-options types, the shared MAPI value model, and the `OutlookFormatException` family. | [Reading .msg files](reading-msg-files.md) · [Reading .pst mail stores](reading-pst-mail-stores.md) · [Properties and named properties](properties-and-named-properties.md) · [Reader options and resource limits](reader-options-and-limits.md) |

## Guides

- **[Reading .msg files](reading-msg-files.md)** - opening a message from a path or stream, the scalar and body conveniences, recipients, attachments, and nested messages.
- **[Reading .pst mail stores](reading-pst-mail-stores.md)** - opening a mail store, walking the folder hierarchy, message conveniences and bodies, recipients, attachments and embedded messages, store-wide named properties, and the two exception families.
- **[Properties and named properties](properties-and-named-properties.md)** - the raw property surface, typed accessors, tags and wire types, and resolving named properties.
- **[Reader options and resource limits](reader-options-and-limits.md)** - every option on both readers, what `Strict` changes, and what surfaces when a limit trips.
