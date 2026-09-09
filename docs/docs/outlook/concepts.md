---
title: Bodu.Formats.Outlook — Core concepts
---

# Bodu.Formats.Outlook — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md), and refer back whenever a term feels imprecise.

`Bodu.Formats.Outlook` is part of the **[Binary Formats & I/O](../topics/binary-formats.md)** topic — the value model shared by the `.msg` reader (over [`Bodu.IO.Compound`](../io-compound/index.md)) and the `.pst` reader (over [`Bodu.IO.Pst`](../io-pst/index.md)). For the high-level shape of the family, start with the [introduction](index.md).

## Session and view

Both readers expose a **session** — <xref:Bodu.Formats.Outlook.OutlookMessage> for a `.msg` file, <xref:Bodu.Formats.Outlook.OutlookMailStore> for a `.pst` file — that owns the container and, unless `leaveOpen: true` was passed, the source stream. Everything obtained from a session is a **view** bound to its lifetime: a `.pst` folder, message, recipient, or attachment, and a `.msg` recipient, attachment, or nested message. Disposing the session invalidates them all; members called afterwards throw <xref:System.ObjectDisposedException>.

Reads are lazy. A `.msg` session decodes the root property stream eagerly at open (so `Properties` needs no further I/O) and materializes recipients, attachments, and the named-property mapping on first access; a `.pst` session parses only the container header at open, streams folder and message tables row block by row block, and decodes each object's properties once on first access. The `.pst` session is documented single-threaded — its members, and those of every view, must not be called concurrently. Treat a `.msg` session the same way.

## Property

A MAPI **property** is a 32-bit tag paired with a value. <xref:Bodu.Formats.Outlook.MapiProperty> is the decoded pair; <xref:Bodu.Formats.Outlook.MapiPropertyCollection> is the bag of them on every object — message, folder, store, recipient, attachment. The collection keeps properties in first-occurrence order, replaces a later property that carries an exact-duplicate tag, and answers lookups by full tag (`TryGetValue(tag, out property)`, `Contains(tag)`), by identifier alone (`TryGetValue(id, out property)` returns the first property carrying that identifier; `GetAll(id)` returns every one), and through typed accessors that probe the plausible wire types and return `null` when nothing matches: `GetString` (Unicode, then code-page), `GetInt32` (Int32, then a widened Int16), `GetInt64`, `GetBoolean`, `GetDouble` (Double, then a widened Float), `GetDateTime`, `GetGuid`, `GetBinary`, and `GetStringArray`. A typed accessor never throws for a missing property.

## Property tag anatomy

<xref:Bodu.Formats.Outlook.MapiPropertyTag> is the 32-bit tag laid out per MS-OXCDATA §2.11.4:

| Bits | Meaning | Exposed as |
|---|---|---|
| 31–16 | The 16-bit **property identifier** | `Id` |
| 12 (`0x1000` of the low word) | The **multi-valued flag** | `IsMultiValued` |
| 15–0 minus the flag | The base **wire type** | `Type` (a <xref:Bodu.Formats.Outlook.MapiPropertyType>) |

`Value` preserves the raw combination; `Type` always reports the base code with the flag stripped. `IsNamed` is `true` for identifiers at or above `0x8000` — the named-property range (below). Construct a tag from an identifier and type (`new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode)`), from a raw value (`new MapiPropertyTag(0x0037001F)`), or as a multi-valued tag with `MapiPropertyTag.ForMultiValue(id, elementType)`. `ToString` renders the canonical eight-digit hexadecimal form.

The wire-type codes:

| Code | `MapiPropertyType` | Decodes to |
|---|---|---|
| `0x0002` | `Int16` | `short` |
| `0x0003` | `Int32` | `int` |
| `0x0004` | `Float` | `float` |
| `0x0005` | `Double` | `double` |
| `0x0006` | `Currency` | fixed-point integer |
| `0x0007` | `AppTime` | application time |
| `0x000A` | `ErrorCode` | error code |
| `0x000B` | `Boolean` | `bool` |
| `0x000D` | `Object` | an embedded object (attachment storages) |
| `0x0014` | `Int64` | `long` |
| `0x001E` | `String8` | `string`, decoded through the object's code page |
| `0x001F` | `Unicode` | `string` (UTF-16) |
| `0x0040` | `SystemTime` | `DateTimeOffset` (FILETIME) |
| `0x0048` | `Guid` | `Guid` |
| `0x0102` | `Binary` | `byte[]` |
| `0x0000` / `0x0001` | `Unspecified` / `Null` | no value |

A multi-valued property decodes to an array of the element type (`string[]`, `int[]`, `byte[][]`, …).

## Well-known identifiers

<xref:Bodu.Formats.Outlook.MapiPropertyIds> is a deliberately small catalogue of `PidTag*` identifiers — the ones the convenience surfaces and common consumer scenarios need (`Subject`, `SenderName`, `ClientSubmitTime`, `Body`, `Html`, `RtfCompressed`, `AttachMethod`, `AttachLongFilename`, `MessageCodepage`, `ContainerClass`, …). Every property remains reachable by raw identifier whether or not it is listed there.

## Named property

Identifiers at or above `0x8000` are **named properties**: the file assigns them, so `0x8004` means one thing in this message and something else in the next. The durable identity is a <xref:Bodu.Formats.Outlook.MapiNamedProperty> — a **property-set GUID** scoping either a numeric `Id` or a string `Name` (exactly one is set, per MS-OXCDATA §2.6.1). Well-known property sets include `PS_MAPI`, `PS_PUBLIC_STRINGS` (`00020329-0000-0000-C000-000000000046`, the home of `Keywords`), and the application-defined GUIDs of Outlook's own item classes.

Each file carries a mapping from identifier to identity, and each session resolves it in both directions with the same two members:

- `TryGetNamedPropertyId(MapiNamedProperty name, out ushort id)` — the file-specific identifier a name maps to; combine it with the expected `MapiPropertyType` to address a property collection.
- `TryGetPropertyName(MapiPropertyTag tag, out MapiNamedProperty name)` — the identity behind a named tag.

Where the mapping lives differs by format. A `.msg` file keeps it in the root's `__nameid_version1.0` storage, parsed lazily and shared by every nested attached message (per MS-OXMSG). A `.pst` file keeps one **name-to-id map node** per store (MS-PST §2.4.7), so <xref:Bodu.Formats.Outlook.OutlookMailStore> resolves store-wide and one resolution applies to every message and attachment of the session. In both, entry *i* of the entry stream defines identifier `0x8000 + i`; a malformed entry throws the format's exception under strict validation and is skipped otherwise.

## Recipient

<xref:Bodu.Formats.Outlook.OutlookRecipient> is a container-free view over one recipient's property collection: `RecipientType` (an <xref:Bodu.Formats.Outlook.OutlookRecipientType> — `Originator`, `To`, `Cc`, or `Bcc`; `null` when the property is absent), `DisplayName`, `EmailAddress`, and `AddressType`. A `.msg` message lists recipients in storage-index order (its `__recip_version1.0_#` child storages); a `.pst` message lists them in recipient-table order, each row decoding directly into the recipient's collection. Both readers hand out the same type, so recipient-handling code is shared.

## Attachment and attachment method

<xref:Bodu.Formats.Outlook.OutlookAttachment> (`.msg`) and <xref:Bodu.Formats.Outlook.OutlookMailAttachment> (`.pst`) expose the same surface: `Method`, `FileName` (the long form, falling back to the short), `ContentId`, `MimeTag`, `Size`, and the two content accessors. `Method` is an <xref:Bodu.Formats.Outlook.OutlookAttachmentMethod>:

| Method | Meaning | Content access |
|---|---|---|
| `ByValue` | The bytes are stored in the file (`PidTagAttachDataBinary`). | `OpenContentStream()` |
| `EmbeddedMessage` | The attachment is another message. | `OpenMessage()` |
| `ByReference` / `ByReferenceResolve` / `ByReferenceOnly` | The file holds a path to the content, not the content. | Neither — no payload in the file |
| `Ole` | An OLE object. | Neither |
| `None` | No content declared. | Neither |

Real-world writers frequently omit `PidTagAttachMethod`: an attachment with no declared method that carries a by-value payload reports `ByValue`, otherwise `None`. Calling the wrong accessor for the method throws <xref:System.NotSupportedException>. `Size` reports the recorded `PidTagAttachSize`, falling back to the payload length taken from the container's index — a deferred payload is never read to measure it.

A nested message opened through `OpenMessage()` reports `EmbeddedDepth` one greater than its parent (a file-level message is depth zero). In the `.msg` reader it shares the root session's container and named-property mapping, and disposing it is a no-op; in the `.pst` reader it is a view like any other, inheriting the attachment's code page.

## Subject normalization

Outlook stores a subject that carries a prefix (`RE:`, `FW:`, …) as U+0001, then a character whose code is the prefix length plus one, then the full subject text (MS-PST §2.4.5.1.2; the same encoding appears in `.msg` files). The `Subject` convenience on both message types strips the marker pair and returns the full text, prefix included; `Properties.GetString(MapiPropertyIds.Subject)` always surfaces the value as stored.

## Code page

A `String8` property is bytes in some character set; the readers resolve which one from the object's own properties — `PidTagMessageCodepage` first, then `PidTagInternetCodepage`, then Windows-1252, the historical default for objects that declare nothing. Child objects inherit: in a `.msg` file, recipients and attachments inherit the message's encoding; in a `.pst` file, a folder inherits from its parent (the root from the store), a message from its folder, an attachment from its message, and an embedded message from its attachment — each level overriding only when it declares a code page of its own. The `BodyHtml` convenience decodes `PidTagHtml` with the internet code page first, falling back to the message code page, or returns the value verbatim when the writer stored it as a string. The Windows code pages that dominate real-world mail (Windows-1252, Shift-JIS, …) are available on every platform because the readers register `CodePagesEncodingProvider`; the `System.Text.Encoding.CodePages` package dependency exists for this.

## Bodies and compressed RTF

Three conveniences cover the body forms a message stores, on both message types:

- `BodyText` — the plain-text `PidTagBody`.
- `BodyHtml` — the `PidTagHtml` payload, decoded as above; decoded once and cached.
- `BodyRtf` — the `PidTagRtfCompressed` payload decompressed per MS-OXRTFCP, decompressed once and cached.

Compressed RTF (LZFu) is an LZ77 variant over a 4096-byte dictionary preseeded with a standard RTF prologue, behind a 16-byte header that declares the compressed and uncompressed sizes, a magic (LZFu compressed, MELA raw), and a CRC. The declared uncompressed size sits *outside* the checksum, so it is bounded rather than trusted: a payload whose declared or produced size exceeds `MaxDecompressedRtfBytes` is rejected before allocation. Set `DecompressRtf` to `false` on either options type to skip decompression entirely — `BodyRtf` then returns `null` and the raw payload stays reachable through `Properties.GetBinary(MapiPropertyIds.RtfCompressed)`.

## Validation level

Each reader reuses its container's level rather than defining its own:

| Reader | Option | Values |
|---|---|---|
| `.msg` | <xref:Bodu.Formats.Outlook.OutlookMessageReaderOptions.ValidationLevel> | <xref:Bodu.IO.Compound.CompoundValidationLevel> — `Strict` / `Compatible` (default) / `Minimal` |
| `.pst` | <xref:Bodu.Formats.Outlook.OutlookMailStoreReaderOptions.ValidationLevel> | <xref:Bodu.IO.Pst.PstValidationLevel> — `Compatible` (default) / `Strict` / `Minimal` |

Under the tolerant levels a malformed property is omitted and decoding continues, a table row that references no valid node is skipped, and an undefined attachment method is inferred — the tolerance real-world files need. Under `Strict`, structural problems throw the format's exception: a missing `.msg` value stream or recipient/attachment count mismatch raises <xref:Bodu.Formats.Outlook.OutlookMsgFormatException>; a `.pst` hierarchy, contents, recipient, or attachment row that does not reference a valid node — or a malformed name-to-id map — raises <xref:Bodu.Formats.Outlook.OutlookPstFormatException>. The level also flows down to the container, which enforces its own checksums and signatures under `Strict`.

## Resource limits

Both option types carry hardening limits that a crafted file cannot talk its way past. They are `init`-only, validated at construction (zero or negative values throw <xref:System.ArgumentOutOfRangeException>), and — except where noted — enforced at every validation level.

| Option | `.msg` default | `.pst` default | When it trips |
|---|---|---|---|
| `MaxEmbeddedMessageDepth` | 16 | 16 | `OpenMessage()` on an attachment that would sit deeper than the limit throws <xref:Bodu.Formats.Outlook.OutlookMsgFormatException> / <xref:Bodu.Formats.Outlook.OutlookPstFormatException>. A few kilobytes can nest attachments thousands deep; the limit keeps a recursive walk on its stack. |
| `MaxDecompressedRtfBytes` | 64 MiB | 64 MiB | `BodyRtf` on a payload whose declared or produced size exceeds the ceiling throws the same format exception, before any allocation. |
| `MaxInlineAttachmentBytes` | 1 MiB | 1 MiB | **Not an error.** A by-value payload above the threshold is not decoded into `Properties`: the property is present with a `null` value (`Contains` and `Method` are unaffected, `GetBinary` returns `null`), and `OpenContentStream()` serves it from the container. Combined with a streaming read the payload is never held in memory in full. This threshold does not vary with the validation level. |
| `MaxNodeDataLength` | — | 256 MiB | A `.pst` node payload that any single property value, attachment, or table would materialize at once above the ceiling is refused by the container with <xref:Bodu.IO.Pst.PstFileFormatException> and <xref:Bodu.IO.Pst.PstFileError.LimitExceeded>. Streaming reads (`OpenContentStream` on a deferred payload) are unbounded by design. |
| `BlockCacheSize` | — | 256 entries | Not a limit — the decoded-block LRU budget of the container; `0` disables the cache. |
| `ReadStrategy` | `Buffered` | — | Not a limit — `CompoundReadStrategy.Streaming` reads the `.msg` container sector by sector on demand instead of buffering it whole. |

The `.pst` options forward `ValidationLevel`, `BlockCacheSize`, and `MaxNodeDataLength` to the underlying <xref:Bodu.IO.Pst.PstFileOptions>; its remaining limit, `MaxDataTreeLeaves`, is not surfaced and keeps the container default of 65,536 leaf blocks per node.

## Exceptions

The readers raise a small family rooted at <xref:Bodu.Formats.Outlook.OutlookFormatException>, so a single handler catches both formats:

- <xref:Bodu.Formats.Outlook.OutlookMsgFormatException> — the `.msg` file is not a compound file, the container is malformed, or the message violates MS-OXMSG (including the depth and RTF limits above). Container corruption is wrapped: a failed open carries the <xref:Bodu.IO.Compound.CompoundFileFormatException> as `InnerException`, and a container fault while streaming an attachment surfaces as this exception too.
- <xref:Bodu.Formats.Outlook.OutlookPstFormatException> — a messaging-level violation of MS-PST: a folder or message object that breaks the format's conventions, a table row that references no valid node under strict validation, a malformed name-to-id map, or a tripped limit. Container corruption is **not** wrapped — it propagates as the <xref:Bodu.IO.Pst.PstFileException> family, and opening a `.pst` throws <xref:Bodu.IO.Pst.PstFileFormatException> (not structurally a PST) or <xref:Bodu.IO.Pst.PstUnsupportedFormatException> (the OST variant) directly.

Beyond the family: <xref:System.NotSupportedException> for a content accessor that does not match the attachment method, <xref:System.ObjectDisposedException> after the session is disposed, and <xref:System.ArgumentException> when a `.pst` stream is not readable and seekable.

## Where to go next

- **[Getting started](getting-started.md)** — install + minimal samples for both readers.
- **[Introduction](index.md)** — the high-level shape, the package table, and headline types.
- **[Properties and named properties](../../guides/outlook/properties-and-named-properties.md)** — the raw property surface in practice.
- **API reference** — [Bodu.Formats.Outlook](xref:Bodu.Formats.Outlook) · [Bodu.IO.Compound](xref:Bodu.IO.Compound) · [Bodu.IO.Pst](xref:Bodu.IO.Pst).
