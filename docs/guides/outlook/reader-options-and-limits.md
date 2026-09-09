---
title: Reader options and resource limits
---

# Reader options and resource limits

Both Outlook readers take an `init`-only options object on their `Open` overload — <xref:Bodu.Formats.Outlook.OutlookMessageReaderOptions> for a `.msg` file, <xref:Bodu.Formats.Outlook.OutlookMailStoreReaderOptions> for a `.pst` store. The two share a design: the validation level is the *container's* own enumeration rather than a reader-specific one, a `DecompressRtf` switch controls the RTF body convenience, and three hardening limits bound what a crafted file can make the reader allocate or recurse into. Each type then adds the knobs its container needs.

Every limit is validated at construction — a zero or negative value throws <xref:System.ArgumentOutOfRangeException> from the initializer — so a misconfigured options object never reaches `Open`. The defaults suit real-world files; this page is for when you need to tighten them (untrusted input), loosen them (a legitimately huge archive), or know exactly what happens when one trips.

## `OutlookMessageReaderOptions` (`.msg`)

```csharp
using Bodu.Formats.Outlook;
using Bodu.IO.Compound;

var options = new OutlookMessageReaderOptions
{
    ValidationLevel = CompoundValidationLevel.Strict,   // structural problems throw instead of being omitted
    ReadStrategy = CompoundReadStrategy.Streaming,      // sectors are read on demand from the seekable source
    DecompressRtf = true,
    MaxEmbeddedMessageDepth = 4,
    MaxDecompressedRtfBytes = 8 * 1024 * 1024,
    MaxInlineAttachmentBytes = 256 * 1024,              // larger payloads stay in the container until streamed
};

using FileStream source = File.OpenRead("invoice.msg");
using var message = OutlookMessage.Open(source, options);   // Streaming: the source must stay open
Console.WriteLine(message.Subject);
```

| Property | Type | Default | Effect | When it trips |
|---|---|---|---|---|
| `ValidationLevel` | <xref:Bodu.IO.Compound.CompoundValidationLevel> | `Compatible` | Forwarded to the container **and** used by the message decoder. `Compatible` omits a malformed property and continues; `Strict` makes structural problems throw; `Minimal` is passed to the container for best-effort recovery and behaves as `Compatible` in the decoder. | `Strict` only — see [What `Strict` changes](#what-strict-changes). |
| `ReadStrategy` | <xref:Bodu.IO.Compound.CompoundReadStrategy> | `Buffered` | Forwarded to the container. `Buffered` reads the whole file into memory at open; `Streaming` reads sectors on demand from a seekable source that must stay open; `Auto` picks by size against the container's default 64 MiB cutoff. | Not a limit. `Streaming` over a non-seekable stream throws <xref:System.ArgumentException> at open. |
| `DecompressRtf` | `bool` | `true` | Whether `BodyRtf` decompresses `PidTagRtfCompressed`. When `false`, `BodyRtf` returns `null` and the raw payload stays reachable through `Properties`. | Not a limit. |
| `MaxEmbeddedMessageDepth` | `int` | `16` | The deepest nesting `OpenMessage()` opens: the root message is depth 0, a message opened from one of its attachments is depth 1, and so on. | `OpenMessage()` on an attachment whose message would sit deeper throws <xref:Bodu.Formats.Outlook.OutlookMsgFormatException> at every validation level. |
| `MaxDecompressedRtfBytes` | `int` | 64 MiB | The largest decompressed RTF body the reader produces. The declared size sits outside the payload's CRC, so it is bounded rather than trusted. | `BodyRtf` on a payload whose declared or produced size exceeds the ceiling throws <xref:Bodu.Formats.Outlook.OutlookMsgFormatException> before any allocation, at every validation level. |
| `MaxInlineAttachmentBytes` | `int` | 1 MiB | The largest by-value payload decoded into an attachment's `Properties`. Above it the property is present with a `null` value and `OpenContentStream()` reads from the container. | **Never** — it is a decoding threshold, not a validation rule, and does not vary with the validation level. |

The reader forwards `ValidationLevel` and `ReadStrategy` to the underlying <xref:Bodu.IO.Compound.CompoundFileOptions>; the container's `MaxBufferedBytes` (the `Auto` cutoff) is not surfaced and keeps its 64 MiB default.

## `OutlookMailStoreReaderOptions` (`.pst`)

```csharp
using Bodu.Formats.Outlook;
using Bodu.IO.Pst;

var options = new OutlookMailStoreReaderOptions
{
    ValidationLevel = PstValidationLevel.Strict,        // every checksum, signature, and table row enforced
    BlockCacheSize = 512,                               // decoded-block LRU entries (0 disables the cache)
    DecompressRtf = true,
    MaxNodeDataLength = 64L * 1024 * 1024,              // largest node payload materialized at once
    MaxEmbeddedMessageDepth = 4,
    MaxDecompressedRtfBytes = 8 * 1024 * 1024,
    MaxInlineAttachmentBytes = 256 * 1024,
};

using var store = OutlookMailStore.Open(File.OpenRead("archive.pst"), options);
Console.WriteLine(store.DisplayName);
```

| Property | Type | Default | Effect | When it trips |
|---|---|---|---|---|
| `ValidationLevel` | <xref:Bodu.IO.Pst.PstValidationLevel> | `Compatible` | Forwarded to the container **and** used by the MAPI decoder. `Compatible` checks structure plus the header CRC; `Strict` additionally verifies every page and block checksum and signature and makes messaging-level problems throw; `Minimal` skips every checksum for salvage reads and behaves as `Compatible` in the decoder. | `Strict` only — see [What `Strict` changes](#what-strict-changes). |
| `BlockCacheSize` | `int` | `256` | The container's least-recently-used cache of decoded pages and block payloads, in entries (each at most 8 KiB, so roughly 2 MB by default). `0` disables caching. Negative values throw. | Not a limit. |
| `DecompressRtf` | `bool` | `true` | As for `.msg`: `false` makes `BodyRtf` return `null`; the compressed payload stays in `Properties`. | Not a limit. |
| `MaxNodeDataLength` | `long` | 256 MiB | Forwarded to the container as <xref:Bodu.IO.Pst.PstFileOptions.MaxNodeDataLength>: the largest node payload materialized at once — the ceiling on any single property value, attachment payload, or table the store decodes whole. Streaming reads (`OpenContentStream()` on a deferred payload) are unbounded by design. | The **container** refuses the read with <xref:Bodu.IO.Pst.PstFileFormatException> and `Error == PstFileError.LimitExceeded`, at every validation level. It propagates unwrapped. |
| `MaxEmbeddedMessageDepth` | `int` | `16` | As for `.msg`: a folder-level message is depth 0. | `OpenMessage()` past the limit throws <xref:Bodu.Formats.Outlook.OutlookPstFormatException> at every validation level. |
| `MaxDecompressedRtfBytes` | `int` | 64 MiB | As for `.msg`. | `BodyRtf` throws <xref:Bodu.Formats.Outlook.OutlookPstFormatException> at every validation level. |
| `MaxInlineAttachmentBytes` | `int` | 1 MiB | As for `.msg`: above it the payload is left in the store and served block by block by `OpenContentStream()`. | **Never**. |

### What the mail store forwards to the container

| <xref:Bodu.IO.Pst.PstFileOptions> member | Forwarded? | Value the container sees |
|---|---|---|
| `ValidationLevel` | Yes | `OutlookMailStoreReaderOptions.ValidationLevel` |
| `BlockCacheSize` | Yes | `OutlookMailStoreReaderOptions.BlockCacheSize` |
| `MaxNodeDataLength` | Yes | `OutlookMailStoreReaderOptions.MaxNodeDataLength` |
| `MaxDataTreeLeaves` | **No** | The container default of 65,536 leaf blocks per node. A data tree fanning out further is refused with `PstFileFormatException` / `LimitExceeded` exactly like an oversized payload. |

To change `MaxDataTreeLeaves` — or anything else on <xref:Bodu.IO.Pst.PstFileOptions> — open the file with <xref:Bodu.IO.Pst.PstFile> directly; see [Streaming and validation](../io-pst/streaming-and-validation.md).

## What `Strict` changes

Under the tolerant levels (`Compatible`, `Minimal`) each reader recovers from a malformed object by omitting the piece it cannot decode and continuing. Under `Strict` the same conditions throw the reader's format exception, and the container beneath enforces its own checksums and signatures as well.

**`.msg` — <xref:Bodu.Formats.Outlook.OutlookMsgFormatException> under `Strict`:**

- A variable-length property whose value stream (`__substg1.0_XXXXYYYY`) or multi-value element stream is missing.
- A `Unicode` (`0x001F`) payload with an odd byte length.
- A malformed multi-valued property.
- A recipient or attachment child storage whose name suffix is not eight hexadecimal digits, indices that are not contiguous from zero, or a count that disagrees with the property stream's declared count.
- A malformed `__nameid_version1.0` entry stream (wrong record size, a GUID index that does not resolve, a string offset that does not resolve).

At *every* level the reader still throws for a container that is not a compound file (the <xref:Bodu.IO.Compound.CompoundFileFormatException> becomes the `InnerException`), a root without a `__properties_version1.0` stream, a property stream that is not a whole number of 16-byte records, a missing by-value content stream or embedded-message storage, and the depth and RTF limits above.

**`.pst` — <xref:Bodu.Formats.Outlook.OutlookPstFormatException> under `Strict`:**

- A hierarchy, contents, or associated-contents table row that does not reference a node of the expected type (`NormalFolder`, `NormalMessage`, `AssociatedMessage`); search-folder rows are skipped silently at every level.
- An attachment-table row that does not reference an attachment subnode of the message.
- A property or recipient cell whose value cannot be decoded, or a malformed multi-valued property.
- An undefined `PidTagAttachMethod` value (the tolerant levels infer `ByValue` / `None`).
- A malformed name-to-id map.

At every level the reader throws for a missing by-value payload, a missing embedded-message subnode, and the depth and RTF limits. Container corruption is never wrapped: it surfaces as the <xref:Bodu.IO.Pst.PstFileException> family (see [Reading .pst mail stores](reading-pst-mail-stores.md#errors-two-families-not-one)), and under `Strict` the container also rejects any page or block whose CRC, trailer signature, or block identifier disagrees, an unknown wire type in a property context, and a table row matrix that holds more rows than its row index declares.

## The inline-attachment threshold in practice

`MaxInlineAttachmentBytes` is the one `Max*` value that never throws. Set it to 1 byte and every by-value attachment is deferred:

```csharp
using Bodu.Formats.Outlook;

var options = new OutlookMailStoreReaderOptions { MaxInlineAttachmentBytes = 1 };
using var store = OutlookMailStore.Open(File.OpenRead("archive.pst"), options);

foreach (OutlookMailMessage message in AllMessages(store.RootFolder))
{
    foreach (OutlookMailAttachment attachment in message.Attachments)
    {
        if (attachment.Method != OutlookAttachmentMethod.ByValue)
            continue;

        var tag = new MapiPropertyTag(MapiPropertyIds.AttachData, MapiPropertyType.Binary);
        bool present = attachment.Properties.Contains(tag);                                       // true
        ReadOnlyMemory<byte>? inline = attachment.Properties.GetBinary(MapiPropertyIds.AttachData);   // null above the threshold

        using Stream content = attachment.OpenContentStream();                                     // streams from the store
        Console.WriteLine($"{attachment.FileName}: present={present} inline={inline is not null} size={attachment.Size} streamed={content.Length}");
    }
}

// leah_thumper.jpg: present=True inline=False size=96808 streamed=93142
```

`Contains` still answers `true` and `Method` still reports `ByValue` — the property is present, its value is simply `null` — while `GetBinary` returns `null` and `OpenContentStream()` reads the 93,142-byte payload block by block. (The depth-first *AllMessages* helper is the one defined in [Reading .pst mail stores](reading-pst-mail-stores.md).) The `.msg` reader behaves the same way; combined with `ReadStrategy = Streaming` the payload is never held in memory in full:

```csharp
using Bodu.Formats.Outlook;
using Bodu.IO.Compound;

var options = new OutlookMessageReaderOptions
{
    ReadStrategy = CompoundReadStrategy.Streaming,
    MaxInlineAttachmentBytes = 1,
};

using FileStream source = File.OpenRead("invoice.msg");
using var message = OutlookMessage.Open(source, options);

foreach (OutlookAttachment attachment in message.Attachments)
{
    if (attachment.Method != OutlookAttachmentMethod.ByValue)
        continue;

    ReadOnlyMemory<byte>? inline = attachment.Properties.GetBinary(MapiPropertyIds.AttachData);
    using Stream content = attachment.OpenContentStream();
    Console.WriteLine($"{attachment.FileName}: inline={inline is not null} size={attachment.Size} streamed={content.Length}");
}

// test-unicode.doc: inline=False size=24064 streamed=24064
// pj1.txt: inline=False size=89 streamed=89
```

## When a limit trips

The node-payload ceiling is enforced by the container, so it surfaces as the container's exception — and because the `.pst` reader does not wrap container faults, that is what you catch:

```csharp
using Bodu.Formats.Outlook;
using Bodu.IO.Pst;

var options = new OutlookMailStoreReaderOptions { MaxNodeDataLength = 1 };

try
{
    using var store = OutlookMailStore.Open(File.OpenRead("archive.pst"), options);
    Console.WriteLine(store.DisplayName);
}
catch (PstFileFormatException ex) when (ex.Error == PstFileError.LimitExceeded)
{
    Console.WriteLine($"{ex.Error}: {ex.Message}");
}

// LimitExceeded: The data behind block 716 exceeds the session's configured resource limits.
```

The embedded-message depth is enforced by the reader and surfaces as the reader's exception, from `OpenMessage()`:

```csharp
using Bodu.Formats.Outlook;

var options = new OutlookMailStoreReaderOptions { MaxEmbeddedMessageDepth = 1 };
using var store = OutlookMailStore.Open(File.OpenRead("archive.pst"), options);

foreach (OutlookMailMessage message in AllMessages(store.RootFolder))
    Descend(message);

static void Descend(OutlookMailMessage message)
{
    foreach (OutlookMailAttachment attachment in message.Attachments)
    {
        if (attachment.Method != OutlookAttachmentMethod.EmbeddedMessage)
            continue;

        try
        {
            Descend(attachment.OpenMessage());
        }
        catch (OutlookPstFormatException ex)
        {
            Console.WriteLine($"depth {message.EmbeddedDepth}: {ex.Message}");
        }
    }
}
```

With `MaxEmbeddedMessageDepth = 1`, a folder-level message (depth 0) may open its embedded messages (depth 1), but each of those throws when asked to open its own. (The sample store nests nothing, so this prints nothing.) The `.msg` reader has the identical shape with <xref:Bodu.Formats.Outlook.OutlookMsgFormatException>.

| Limit | Enforced by | `.msg` surfaces as | `.pst` surfaces as | Member |
|---|---|---|---|---|
| `MaxEmbeddedMessageDepth` | Reader | <xref:Bodu.Formats.Outlook.OutlookMsgFormatException> | <xref:Bodu.Formats.Outlook.OutlookPstFormatException> | `OpenMessage()` |
| `MaxDecompressedRtfBytes` | Reader | <xref:Bodu.Formats.Outlook.OutlookMsgFormatException> | <xref:Bodu.Formats.Outlook.OutlookPstFormatException> | `BodyRtf` |
| `MaxInlineAttachmentBytes` | Reader | — (threshold) | — (threshold) | `Properties` / `OpenContentStream()` |
| `MaxNodeDataLength` | Container | — | <xref:Bodu.IO.Pst.PstFileFormatException>, `Error == LimitExceeded` | Any read that materializes a node |
| `MaxDataTreeLeaves` (not surfaced) | Container | — | <xref:Bodu.IO.Pst.PstFileFormatException>, `Error == LimitExceeded` | Any read that walks a data tree |

## Turning RTF decompression off

```csharp
using Bodu.Formats.Outlook;

var options = new OutlookMailStoreReaderOptions { DecompressRtf = false };
using var store = OutlookMailStore.Open(File.OpenRead("archive.pst"), options);

foreach (OutlookMailMessage message in AllMessages(store.RootFolder))
{
    string? rtf = message.BodyRtf;                                                       // always null
    ReadOnlyMemory<byte>? compressed = message.Properties.GetBinary(MapiPropertyIds.RtfCompressed);
    Console.WriteLine($"{message.Subject}: BodyRtf is null={rtf is null}, compressed payload={compressed?.Length ?? 0} bytes");
}

// Here is a sample message: BodyRtf is null=True, compressed payload=0 bytes
```

Disable it when you archive the compressed payload verbatim, hand it to another decoder, or simply never read RTF and want to skip the work. The sample message stores no RTF body at all, which is why the payload reads as zero bytes.

## Invalid values

```csharp
using Bodu.Formats.Outlook;

try
{
    _ = new OutlookMailStoreReaderOptions { MaxNodeDataLength = 0 };
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"{ex.ParamName}: {ex.Message}");
}

// value: The value must be a positive number. (Parameter 'value')
```

Every `Max*` limit rejects zero and negative values; `BlockCacheSize` rejects negative values only (`0` is the documented "no cache" setting).

## Where to go next

- [Reading .pst mail stores](reading-pst-mail-stores.md) — the session, folder walk, messages, attachments, and the two exception families.
- [Reading .msg files](reading-msg-files.md) — the single-message reader these `.msg` options configure.
- [Streaming and validation](../io-pst/streaming-and-validation.md) — the container-level `PstFileOptions`, including `MaxDataTreeLeaves`, and the complete `PstFileError` catalogue.
- [Buffered vs streaming access](../io-compound/streaming-and-buffering.md) — what `ReadStrategy` does inside the compound-file container.
- [Bodu.Formats.Outlook core concepts](../../docs/outlook/concepts.md#resource-limits) — the resource-limit rationale in the vocabulary page.
- [Bodu.Formats.Outlook guides](index.md) — every guide in this topic.
