---
title: Bodu.IO.Pst — Core concepts
---

# Bodu.IO.Pst — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md), and refer back whenever a term feels imprecise.

`Bodu.IO.Pst` is part of the **[Binary Formats & I/O](../topics/binary-formats.md)** topic — a container tier beneath the format readers, like `Bodu.IO.Compound` beneath `Bodu.Formats.Excel.Binary`. For the high-level shape of the library, start with the [introduction](index.md).

## PST file

A **PST file** (personal storage table, MS-PST) is a single-file database Outlook uses for personal-folders archives. Unlike a compound file's folder-and-stream hierarchy, a PST is organized as a flat **node database** indexed by B-trees, with the object hierarchy expressed through node identifiers rather than a directory tree. The format has three layers — the node database (NDB), the lists/tables/properties layer (LTP), and the messaging layer — and this package implements the first two. The messaging layer (folders, messages, MAPI semantics) belongs to `Bodu.Formats.Outlook.Pst`.

The format comes in two variants: **Unicode** (`wVer` 23, the default since Outlook 2003, 64-bit internal offsets) and the legacy **ANSI** variant (`wVer` 14/15, 32-bit offsets). This package reads both: an internal layout descriptor selected from the header's version supplies the identifier widths, structure offsets, and trailer layout for the file, and every public type behaves identically over either. OST files are recognized and rejected with <xref:Bodu.IO.Pst.PstUnsupportedFormatException>. <xref:Bodu.IO.Pst.PstFileFormat> reports what the header declared.

## Header

The header is the preamble anchoring the file (564 bytes in the Unicode format, 512 in ANSI): the `!BDN` magic, a CRC over the following 471 bytes (the MS-PST §5.3 checksum), the client magic `SM`, the format version, the file size, the root references of the two B-trees, the sentinel byte, and the content-encoding method. Under <xref:Bodu.IO.Pst.PstValidationLevel.Strict> the header CRC is enforced; a wrong magic, an undeclared version, or a truncated header is rejected as malformed.

## Node identifier (NID)

Every object in the file is a **node**, addressed by a 32-bit <xref:Bodu.IO.Pst.PstNodeId>: the five low bits carry the <xref:Bodu.IO.Pst.PstNodeType> (normal folder, normal message, attachment, the table kinds, …) and the 27 bits above them carry the index. Three fixed identifiers anchor every file and are exposed as well-known values — `PstNodeId.MessageStore` (`0x21`), `PstNodeId.NameToIdMap` (`0x61`), and `PstNodeId.RootFolder` (`0x122`).

The type bits make identifiers self-describing: a folder's hierarchy table, contents table, and associated-contents table reuse the folder's *index* with the corresponding *table* type bits, which is how the messaging layer finds them without any directory lookup.

## Block, BID, and trailer

Raw bytes live in **blocks**, addressed by 64-bit **block identifiers (BIDs)** and aligned to 64-byte boundaries. Every block ends with a 16-byte trailer recording its payload length, a signature computed from its address and identifier, a CRC over the payload, and the BID itself. Bit 1 of a BID marks an **internal** block (tree metadata); internal blocks are never content-encoded. Under `Strict` validation every trailer field is enforced; `Compatible` checks structure but tolerates writer quirks.

## Content encoding

The format's optional "compressible encryption" (<xref:Bodu.IO.Pst.PstCryptMethod>) obfuscates external block data: **permute** applies a fixed byte substitution, **cyclic** applies a substitution keyed by the low bits of the BID. Neither is cryptography — both decode without a key, and the reader does so transparently. The header declares which method the file uses.

## Node and block B-trees (NBT / BBT)

Two page-based B-trees index the file: the **node B-tree (NBT)** maps a NID to its node record — the data-tree root BID, the subnode-tree root BID, and the parent NID — and the **block B-tree (BBT)** maps a BID to its block's address and length. Pages are 512 bytes with their own trailers; the reader descends from the header's root references, validating page types, signatures, and CRCs per the active validation level.

## Data tree

A node's payload larger than one block (~8 KB) is stored as a tree: an **XBLOCK** lists data-block BIDs, and an **XXBLOCK** lists XBLOCKs, with the logical length recorded in the tree header. <xref:Bodu.IO.Pst.PstNode.ReadAllBytes> flattens the tree into one array; <xref:Bodu.IO.Pst.PstNode.OpenDataStream> instead resolves only the ordered *leaf list* and serves a seekable read-only stream that keeps a single leaf block resident — the logical payload can exceed available memory without the reader materializing it. <xref:Bodu.IO.Pst.PstNode.DataLength> prices a payload by summing leaf lengths without reading any of them.

## Subnode tree

A node may carry a private namespace of child nodes — its **subnode tree** — stored as **SLBLOCK** leaves (NID → data/subnode BIDs) under an optional **SIBLOCK** index. A message node keeps its recipient table, attachment table, and attachment objects there; the subnodes are invisible to the NBT. <xref:Bodu.IO.Pst.PstNode.EnumerateSubnodes> and <xref:Bodu.IO.Pst.PstNode.TryGetSubnode*> walk it.

## Heap-on-node (HN) and HNID

The LTP layer allocates many small items inside one node's data payload: the **heap-on-node** subdivides each data block into indexed items behind a page map, identified by 32-bit **HIDs**. Where a value can be either heap-resident or too large for the heap, the format stores an **HNID** — a value that is an HID when its five low bits are zero and a subnode NID otherwise. The reader resolves both transparently when a property or cell value is accessed.

## Property context (PC)

A **property context** (heap client signature `0xBC`) is a node's property bag: a BTree-on-heap keyed by 16-bit property identifiers, each record carrying a wire type and either an inline value or an HNID reference. <xref:Bodu.IO.Pst.PstNode.ReadPropertyContext> returns the <xref:Bodu.IO.Pst.PstPropertyContext> view — tag-ordered <xref:Bodu.IO.Pst.PstPropertyValue> entries whose payloads resolve on access. Values keep their **wire types** (`0x001F` UTF-16 string, `0x0102` binary, `0x0003` Int32, …); interpreting them as MAPI properties is the format reader's job.

## Table context (TC)

A **table context** (heap client signature `0x7C`) is a node's table: a `TCINFO` header describing typed columns and cell-region offsets, a row index (a BTree-on-heap mapping row identifier → row number), and a row matrix of fixed-width rows whose variable-size cells hold HNIDs. <xref:Bodu.IO.Pst.PstNode.ReadTableContext> returns the <xref:Bodu.IO.Pst.PstTableContext> view: <xref:Bodu.IO.Pst.PstTableContext.Columns>, <xref:Bodu.IO.Pst.PstTableContext.RowCount>, streaming <xref:Bodu.IO.Pst.PstTableContext.EnumerateRows> (one matrix block resident at a time), and keyed <xref:Bodu.IO.Pst.PstTableContext.TryGetRow*>. Folder hierarchies and contents listings are table contexts whose row identifiers are the referenced nodes' NIDs.

## Validation levels

<xref:Bodu.IO.Pst.PstValidationLevel> selects how much cross-checking each read performs:

- **`Compatible`** (default) — structural validation with tolerance for real-world writer quirks.
- **`Strict`** — every CRC, trailer signature, and page invariant enforced; malformed content that a tolerant level would skip is rejected.
- **`Minimal`** — bounds and type checks only, for maximum tolerance.

## Decoded-block cache

<xref:Bodu.IO.Pst.PstFileOptions.BlockCacheSize> bounds a least-recently-used cache of decoded blocks and pages (default 256 entries; `0` disables it). Repeated reads through the same structures — a table walked twice, a property bag revisited — are then served without re-reading or re-decoding from the source. The cache trades memory for I/O; the entries are the decoded payloads themselves.

## Exceptions

Every failure surfaces through the <xref:Bodu.IO.Pst.PstFileException> family, carrying a <xref:Bodu.IO.Pst.PstFileError> category (invalid header, invalid block, invalid heap, node not found, …):

- <xref:Bodu.IO.Pst.PstFileFormatException> — the file violates the format at the active validation level.
- <xref:Bodu.IO.Pst.PstUnsupportedFormatException> — a recognized but unsupported variant (the 4 KiB-page OST).
- <xref:Bodu.IO.Pst.PstNodeNotFoundException> — a `GetNode` lookup for an absent identifier.

Corruption never escapes as any other exception type — the corruption sweeps in the test suite enforce exactly that contract.

## Where to go next

- **[Getting started](getting-started.md)** — install + minimal samples.
- **[Introduction](index.md)** — the high-level shape and headline types.
- **API reference** — [Bodu.IO.Pst](xref:Bodu.IO.Pst).
