---
title: Bodu.IO.Pst guides
---

# Bodu.IO.Pst guides

Recipe-style walk-throughs for **Bodu.IO.Pst**, the low-level, read-only container reader for the Outlook personal-folders format (`.pst` / MS-PST, Unicode and ANSI formats). The package reads the node database (NDB) — header, node and block B-trees, block data with the format's content encodings decoded and checksums verified, multi-block data trees, per-node subnode trees — and the LTP layer over it: heap-on-node, BTree-on-heap, and the property-context and table-context views with wire-typed values.

The library has no MAPI knowledge: it exposes nodes, raw payloads, and property bags whose values keep their on-disk wire types, and leaves *meaning* (subjects, senders, folders) to the caller. The mail-store reader in [Bodu.Formats.Outlook.Pst](../outlook/index.md) is built directly on top of it — the same container/format split as `Bodu.IO.Compound` beneath `Bodu.Formats.Excel.Binary`.

If you are new to the library, start with the [introduction](../../docs/io-pst/index.md), the [Core concepts](../../docs/io-pst/concepts.md) glossary, and the [getting-started page](../../docs/io-pst/getting-started.md). The guides below assume you know the vocabulary (node, NID, data tree, subnode tree, heap-on-node, property context, table context).

## How the library works

A PST file is a flat, B-tree-indexed database of **nodes** in a single file. <xref:Bodu.IO.Pst.PstFile> opens it as a disposable session; every object — folders, messages, tables, internal maps — is a <xref:Bodu.IO.Pst.PstNode> addressed by a 32-bit <xref:Bodu.IO.Pst.PstNodeId> whose five low bits carry the node's <xref:Bodu.IO.Pst.PstNodeType>. A node has a data payload (assembled transparently from its data tree), a private subnode tree, and two LTP views over its heap: <xref:Bodu.IO.Pst.PstPropertyContext>, the property bag, and <xref:Bodu.IO.Pst.PstTableContext>, the table.

Reads are lazy and seek the source on demand — the file is never buffered whole. Opening parses only the header; node lookups walk the node B-tree; payloads resolve when asked for, through a bounded least-recently-used cache of decoded blocks; and every payload has a streaming twin (`OpenDataStream`, `TryOpenValueStream`, `TryOpenCellStream`) that keeps one block resident regardless of the logical size. The session is single-threaded.

> These guides cover the container. For folders, messages, recipients, attachments, and bodies with MAPI semantics, see [Reading .pst mail stores](../outlook/reading-pst-mail-stores.md).

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| <xref:Bodu.IO.Pst> | The `PstFile` session and `PstFileOptions`; `PstNode` with `PstNodeId` / `PstNodeType` / `PstNodeInfo`; the LTP views `PstPropertyContext` / `PstPropertyValue` and `PstTableContext` / `PstTableColumn` / `PstTableRow`; `PstFileFormat`, `PstCryptMethod`, `PstValidationLevel`; and the `PstFileException` family with its `PstFileError` category. | [Reading nodes and tables](reading-nodes-and-tables.md) · [Streaming and validation](streaming-and-validation.md) |

The NDB and LTP record layers (`Bodu.IO.Pst.Internal`) are internal and not part of the public surface.

## Guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="reading-nodes-and-tables.md">Reading nodes and tables</a></h3>
  <p>Open a file, construct and decode node identifiers, walk the node directory and a node's subnodes, and read the two LTP views — the property context with its typed accessors and the table context with streaming rows and keyed lookup.</p>
</div>

<div class="bodu-card">
  <h3><a href="streaming-and-validation.md">Streaming and validation</a></h3>
  <p>Price and stream large payloads with the length/stream pairs, tune <code>PstFileOptions</code> (validation level, block cache, node-data and data-tree limits), and classify every failure with the complete <code>PstFileError</code> catalogue.</p>
</div>

<div class="bodu-card">
  <h3><a href="../outlook/reading-pst-mail-stores.md">Reading .pst mail stores</a></h3>
  <p>The mail-store reader built on this package: folders, messages, recipients, attachments, embedded messages, bodies, and store-wide named properties, with the two exception families kept apart.</p>
</div>

</div>

## Suggested reading path

1. **[Reading nodes and tables](reading-nodes-and-tables.md)** — the open → look up → read recipe every other use builds on.
2. **[Streaming and validation](streaming-and-validation.md)** — once payloads are large, input is untrusted, or you need to say *why* a file was rejected.
3. **[Reading .pst mail stores](../outlook/reading-pst-mail-stores.md)** — when you want messages rather than nodes.

## Where to go next

- [Runnable samples](../../samples/io-pst.md) — the offline PstBasics sample under `samples/IO.Pst/`: detection and open, the node and table views, streaming under `Strict`, and the mail-store view over the same Unicode and ANSI fixtures.
- [Bodu.IO.Pst API reference](xref:Bodu.IO.Pst) — every type and member.
- [Bodu.Formats.Outlook guides](../outlook/index.md) — the readers that share the MAPI value model.
- [Package matrix](../../docs/package-matrix.md) — where Bodu.IO.Pst sits in the suite and its dependency stack.
