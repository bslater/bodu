---
uid: Bodu.IO.Pst
---

![Bodu.IO.Pst](~/images/hero-io-pst.svg)

## Purpose

**Bodu.IO.Pst** is a low-level, read-only container reader for the Outlook personal-folders format (PST / MS-PST, Unicode and ANSI formats). It reads the node database (NDB) — the header, the node and block B-trees, block data with the format's permute and cyclic content encodings decoded and checksums verified, multi-block data trees, and per-node subnode trees — and the LTP layer over it: heap-on-node, BTree-on-heap, and per-node property-context and table-context views with wire-typed values. It carries no MAPI semantics and no write support: interpreting a property as a subject or a sender is the job of the `Bodu.Formats.Outlook.Pst` mail-store reader layered on top, which shares the <xref:Bodu.Formats.Outlook> MAPI value model with the `.msg` reader.

A PST file is a node-oriented database in a single file. <xref:Bodu.IO.Pst.PstFile> opens it as a disposable session; every object — folders, messages, tables, internal maps — is a <xref:Bodu.IO.Pst.PstNode> addressed by a 32-bit <xref:Bodu.IO.Pst.PstNodeId> whose five low bits carry its <xref:Bodu.IO.Pst.PstNodeType>. Payloads assemble transparently from multi-block data trees, stream through a decoded-block LRU cache, and a node's private subnode tree carries its children (a message's recipient and attachment tables, for example).

## Static documentation

- **[Introduction](~/docs/io-pst/index.md)** — the headline types, the layering beneath the mail-store reader, and the scenarios the library covers.
- **[Core concepts](~/docs/io-pst/concepts.md)** — node database, NID/BID, data and subnode trees, heap-on-node, property and table contexts, validation levels.
- **[Getting started](~/docs/io-pst/getting-started.md)** — install and minimal samples for opening, enumerating, and reading.
- **[Binary Formats & I/O topic overview](~/docs/topics/binary-formats.md)** — where the container sits beneath the format readers.

## Key types

**Session and nodes**

- <xref:Bodu.IO.Pst.PstFile> — the disposable read session. Factories `OpenRead` (path / `Stream`) and `Open(Stream, options, leaveOpen)`, the cheap `IsPstFile` probe; `Format` / `CryptMethod`, the node directory via `EnumerateNodes`, and lookup via `GetNode` / `TryGetNode`.
- <xref:Bodu.IO.Pst.PstNode> — one node. `ReadAllBytes` (buffered), `OpenDataStream` (a seekable read-only stream keeping one leaf block resident, bound to the session — it throws once the session is disposed), `DataLength` (priced without payload reads); `EnumerateSubnodes` / `TryGetSubnode`; and the LTP views `ReadPropertyContext` / `ReadTableContext`.
- <xref:Bodu.IO.Pst.PstNodeId> / <xref:Bodu.IO.Pst.PstNodeType> — the 32-bit identifier (five type bits + 27-bit index) with the well-known anchors `MessageStore` (`0x21`), `NameToIdMap` (`0x61`), and `RootFolder` (`0x122`).
- <xref:Bodu.IO.Pst.PstNodeInfo> — an immutable directory snapshot: identifier, parent, payload length, and whether subnodes are present.

**LTP views**

- <xref:Bodu.IO.Pst.PstPropertyContext> — the node's property bag: a tag-ordered collection of <xref:Bodu.IO.Pst.PstPropertyValue> entries (16-bit property identifier, raw wire type, payload resolved on access, typed accessors) with `TryGetValue` / `GetValue`, plus the streaming pair `TryGetValueLength` / `TryOpenValueStream` that prices or reads a value without materializing it.
- <xref:Bodu.IO.Pst.PstTableContext> — the node's table: <xref:Bodu.IO.Pst.PstTableColumn> metadata, `RowCount`, streaming `EnumerateRows` (one matrix block resident at a time), and keyed `TryGetRow`. <xref:Bodu.IO.Pst.PstTableRow> exposes `RowId`, `TryGetCell`, `EnumerateCells`, and the streaming pair `TryGetCellLength` / `TryOpenCellStream`.

**Options and formats**

- <xref:Bodu.IO.Pst.PstFileOptions> — `ValidationLevel` (<xref:Bodu.IO.Pst.PstValidationLevel>: `Compatible` / `Strict` / `Minimal`) and the decoded-block LRU `BlockCacheSize` (default 256 entries; `0` disables).
- <xref:Bodu.IO.Pst.PstFileFormat> / <xref:Bodu.IO.Pst.PstCryptMethod> — the header's declared variant (both `Unicode` and `Ansi` are read; the OST variant is rejected) and content encoding.

**Errors**

- <xref:Bodu.IO.Pst.PstFileException> (base, with a <xref:Bodu.IO.Pst.PstFileError> category), <xref:Bodu.IO.Pst.PstFileFormatException> (malformed content at the active validation level), <xref:Bodu.IO.Pst.PstUnsupportedFormatException> (recognized but unsupported variant — the 4 KiB-page OST), <xref:Bodu.IO.Pst.PstNodeNotFoundException> (a `GetNode` miss).

## Example

```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

// The store object's raw property bag.
PstNode store = file.GetNode(PstNodeId.MessageStore);
foreach (PstPropertyValue value in store.ReadPropertyContext())
    Console.WriteLine($"0x{value.PropertyId:X4} (wire 0x{value.WireType:X4}): {value.RawData.Length} bytes");

// The root folder's hierarchy table: each row identifier is a child folder's node identifier.
var hierarchyId = new PstNodeId(PstNodeType.HierarchyTable, PstNodeId.RootFolder.Index);
if (file.TryGetNode(hierarchyId, out PstNode? table))
{
    foreach (PstTableRow row in table.ReadTableContext().EnumerateRows())
        Console.WriteLine($"child folder 0x{row.RowId:X8}");
}
```
