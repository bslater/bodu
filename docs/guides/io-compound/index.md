---
title: Bodu.IO.Compound guides
---

# Bodu.IO.Compound guides

Recipe-style walk-throughs for **Bodu.IO.Compound**, the read-only reader for the OLE2 / Compound File Binary (CFB) container format — the structured-storage envelope used by legacy Microsoft Office files (`.xls`, `.doc`, `.ppt`, `.msg`) and other technologies.

The library has no application-format knowledge: it exposes the embedded storage hierarchy and the raw byte payload of each named stream, and leaves interpretation to the caller. The narrow BIFF8 `.xls` reader in <xref:Bodu.Formats.Excel.Binary> is built directly on top of it.

If you are new to the library, start with the [introduction](../../docs/io-compound/index.md), the [Core concepts](../../docs/io-compound/concepts.md) glossary, and the [getting-started page](../../docs/io-compound/getting-started.md). The guides below assume you know the vocabulary (compound file, storage, stream, sector chain, property set).

## How the library works

A compound file is effectively a small file system embedded in a single file. <xref:Bodu.IO.Compound.CompoundFile> is the managed counterpart of the COM `StgOpenStorage` entry point: navigation begins at `RootStorage` and descends through nested <xref:Bodu.IO.Compound.CompoundStorage> containers (the COM `IStorage`) to <xref:Bodu.IO.Compound.CompoundStream> leaves (the COM `IStream`). A <xref:Bodu.IO.Compound.CompoundStream> is itself a read-only, seekable <xref:System.IO.Stream> cursor over the bytes.

![A compound file is a structured-storage envelope: a header, allocation tables, and a directory of sectors on the left, resolving via CompoundFile.Open into the logical RootStorage to CompoundStorage to CompoundStream hierarchy on the right.](../../images/diagrams/io-compound-structure.svg)

By default the whole source is buffered into memory at open time, so the file is read-only and safe to share across threads. Opening with `buffered: false` reads sectors on demand from a seekable stream instead, bounding memory for large files.

> These guides cover the read path — opening with `FileMode.Open` and `FileAccess.Read`.

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| <xref:Bodu.IO.Compound> | The `CompoundFile` reader, the `CompoundStorage` / `CompoundStream` hierarchy, the `CompoundStream` cursor, `CompoundEntryInfo` metadata, and the `CompoundFileFormatException` / `CompoundStreamNotFoundException` errors. | [Reading compound files](reading-compound-files.md) · [Buffered vs streaming access](streaming-and-buffering.md) |
| <xref:Bodu.IO.Compound.PropertySets> | The OLE property-set readers — `SummaryInformation`, `DocumentSummaryInformation`, and the underlying `OlePropertySet`. | [Reading property sets](property-sets.md) |

## Guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="reading-compound-files.md">Reading compound files</a></h3>
  <p>Open a file, probe the signature, walk the storage hierarchy with the enumerate and <code>TryOpen</code> surfaces, and read a named stream's bytes — the end-to-end navigation recipe.</p>
</div>

<div class="bodu-card">
  <h3><a href="streaming-and-buffering.md">Buffered vs streaming access</a></h3>
  <p>The <code>buffered</code> flag, the <code>CompoundStream</code> cursor, <code>AsMemory</code> vs chunked <code>Read</code>, lifetime and threading contracts, and how to bound memory for large files.</p>
</div>

<div class="bodu-card">
  <h3><a href="property-sets.md">Reading property sets</a></h3>
  <p>The <code>\x05SummaryInformation</code> and <code>\x05DocumentSummaryInformation</code> metadata streams — typed accessors, the raw <code>OlePropertySet</code>, and the <code>TryGet*</code> convenience methods on <code>CompoundFile</code>.</p>
</div>

</div>

## Suggested reading path

1. **[Reading compound files](reading-compound-files.md)** — the core open → navigate → read recipe that every other use builds on.
2. **[Buffered vs streaming access](streaming-and-buffering.md)** — once the file is too large to hold whole, or you need to control the source's lifetime.
3. **[Reading property sets](property-sets.md)** — when you want the authored document metadata (title, author, timestamps) rather than the format payload.

## Where to go next

- [Bodu.IO.Compound API reference](xref:Bodu.IO.Compound) — every type and member.
- [Bodu.Formats.Excel.Binary](xref:Bodu.Formats.Excel.Binary) — the BIFF8 `.xls` reader built on this package.
- [Package matrix](../../docs/package-matrix.md) — where Bodu.IO.Compound sits in the suite and its dependency stack.
