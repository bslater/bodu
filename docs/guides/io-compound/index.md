---
title: Bodu.IO.Compound guides
---

# Bodu.IO.Compound guides

Recipe-style walk-throughs for **Bodu.IO.Compound**, the reader and writer for the OLE2 / Compound File Binary (CFB) container format — the structured-storage envelope used by legacy Microsoft Office files (`.xls`, `.doc`, `.ppt`, `.msg`) and other technologies.

The library has no application-format knowledge: it exposes the embedded storage hierarchy and the raw byte payload of each named stream, and leaves interpretation to the caller. The narrow BIFF8 `.xls` reader in [Bodu.Formats.Excel.Binary](../excel/index.md) is built directly on top of it.

If you are new to the library, start with the [introduction](../../docs/io-compound/index.md), the [Core concepts](../../docs/io-compound/concepts.md) glossary, and the [getting-started page](../../docs/io-compound/getting-started.md). The guides below assume you know the vocabulary (compound file, storage, stream, sector chain, property set).

## How the library works

A compound file is effectively a small file system embedded in a single file. <xref:Bodu.IO.Compound.CompoundFile> is the managed counterpart of the COM `StgOpenStorage` entry point: navigation begins at `RootStorage` and descends through nested <xref:Bodu.IO.Compound.CompoundStorage> containers (the COM `IStorage`) to <xref:Bodu.IO.Compound.CompoundStream> leaves (the COM `IStream`). A <xref:Bodu.IO.Compound.CompoundStream> is itself a seekable <xref:System.IO.Stream> cursor over the bytes — read-only when opened from a read-only file, read-write on a writable one.

![A compound file is a structured-storage envelope: a header, allocation tables, and a directory of sectors on the left, resolving via CompoundFile.Open into the logical RootStorage to CompoundStorage to CompoundStream hierarchy on the right.](../../images/diagrams/io-compound-structure.svg)

By default the whole source is buffered into memory at open time, so the file is read-only and safe to share across threads. Opening with `buffered: false` reads sectors on demand from a seekable stream instead, bounding memory for large files.

> Most of these guides cover the read path. For writing — building a container from scratch, editing one, or embedding property sets — see [Authoring compound files](authoring-compound-files.md).

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| <xref:Bodu.IO.Compound> | The `CompoundFile` reader and writer, the `CompoundStorage` / `CompoundStream` hierarchy, the `CompoundStream` cursor, `CompoundEntryInfo` metadata, and the `CompoundFileFormatException` / `CompoundStreamNotFoundException` / `CompoundFileSerializationException` errors. | [Reading compound files](reading-compound-files.md) · [Buffered vs streaming access](streaming-and-buffering.md) · [Authoring compound files](authoring-compound-files.md) |
| <xref:Bodu.IO.Compound.Builders> | The detached authoring object model — `CompoundStorageBuilder`, `CompoundStreamBuilder`, and the `CompoundBuildOptions` serialization options. | [Authoring compound files](authoring-compound-files.md) |
| <xref:Bodu.IO.Compound.PropertySets> | The OLE property-set readers and writers — `SummaryInformation`, `DocumentSummaryInformation`, their `…Builder` authors, and the underlying `OlePropertySet`. | [Reading property sets](property-sets.md) · [Authoring compound files](authoring-compound-files.md) |

## Guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="reading-compound-files.md">Reading compound files</a></h3>
  <p>Open a file, probe the signature, walk the storage hierarchy with the enumerate and <code>TryOpen</code> surfaces, and read a named stream's bytes — the end-to-end navigation recipe.</p>
</div>

<div class="bodu-card">
  <h3><a href="authoring-compound-files.md">Authoring compound files</a></h3>
  <p>Write a container from scratch with <code>CompoundStorageBuilder</code>, mutate one in place via <code>CompoundFile.Create</code> and <code>Commit</code>, edit an existing file, and embed summary-information property sets.</p>
</div>

<div class="bodu-card">
  <h3><a href="streaming-and-buffering.md">Buffered vs streaming access</a></h3>
  <p>The <code>buffered</code> flag, the <code>CompoundStream</code> cursor, <code>AsMemory</code> vs chunked <code>Read</code>, asynchronous commit and streaming reads, lifetime and threading contracts, and how to bound memory for large files.</p>
</div>

<div class="bodu-card">
  <h3><a href="property-sets.md">Reading property sets</a></h3>
  <p>The <code>\x05SummaryInformation</code> and <code>\x05DocumentSummaryInformation</code> metadata streams — typed accessors, the raw <code>OlePropertySet</code>, and the <code>TryGet*</code> convenience methods on <code>CompoundFile</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="office-format-nuances.md">Office format nuances</a></h3>
  <p>How the legacy Office documents (<code>.xls</code>, <code>.doc</code>, <code>.ppt</code>, <code>.msg</code>) lay out their named streams inside the CFB envelope, and the quirks to expect when reading them.</p>
</div>

</div>

## Suggested reading path

1. **[Reading compound files](reading-compound-files.md)** — the core open → navigate → read recipe that every other use builds on.
2. **[Buffered vs streaming access](streaming-and-buffering.md)** — once the file is too large to hold whole, or you need to control the source's lifetime.
3. **[Reading property sets](property-sets.md)** — when you want the authored document metadata (title, author, timestamps) rather than the format payload.
4. **[Authoring compound files](authoring-compound-files.md)** — when you need to *write* a container rather than read one.

## Where to go next

- [Runnable samples](../../samples/io-compound.md) — the offline CompoundBasics sample under `samples/IO.Compound/`: builder authoring + read-back, property sets, detection and the v3/v4 knob, a real `.doc`'s tree.
- [Bodu.IO.Compound API reference](xref:Bodu.IO.Compound) — every type and member, including the [Bodu.IO.Compound.Builders](xref:Bodu.IO.Compound.Builders) authoring types.
- [Bodu.Formats.Excel.Binary](../excel/index.md) — the BIFF8 `.xls` reader built on this package.
- [Package matrix](../../docs/package-matrix.md) — where Bodu.IO.Compound sits in the suite and its dependency stack.
