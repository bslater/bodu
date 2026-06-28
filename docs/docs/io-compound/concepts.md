---
title: Bodu.IO.Compound — Core concepts
---

# Bodu.IO.Compound — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/io-compound/index.md), and refer back whenever a term feels imprecise.

`Bodu.IO.Compound` is part of the **[Binary Formats & I/O](../topics/binary-formats.md)** topic — the container tier beneath the format readers. For the high-level shape of the library, start with the [introduction](index.md).

## Compound file (CFB)

A **compound file** — also called OLE2 structured storage or Compound File Binary (CFB) — is a single physical file that embeds a small hierarchical file system. It begins with an eight-byte signature (`D0 CF 11 E0 A1 B1 1A E1`), followed by a 512-byte header, allocation tables, a directory, and a series of fixed-size sectors holding the actual bytes. Legacy Microsoft Office documents (`.xls`, `.doc`, `.ppt`, `.msg`) are compound files. <xref:Bodu.IO.Compound.CompoundFile> parses the container and exposes its hierarchy.

The format comes in two versions, distinguished by sector size: **version 3** uses 512-byte sectors, **version 4** uses 4096-byte sectors. The mini sector size is fixed at 64 bytes in both. <xref:Bodu.IO.Compound.CompoundFileVersion> selects the version when *writing*; the reader accepts either and reads the size from the header.

## Header

The header is the 512-byte preamble that fixes the layout constants the rest of the reader depends on. It carries the signature, a little-endian byte-order marker (`FFFE`), the sector and mini-sector shifts (`9` ⇒ 512, `12` ⇒ 4096; the mini-sector shift is always `6` ⇒ 64), the **mini-stream cutoff** (the size below which a stream lives in the mini-stream), the first sector of the directory chain, the first mini-FAT sector, and the start of the **DIFAT** (double-indirect FAT) — including its first 109 entries stored inline. Every field is read at its specification offset; a wrong byte-order marker, an unsupported sector shift, or a file shorter than 512 bytes is rejected as a malformed header.

## Storage and stream

The container's directory is a tree of two node kinds, distinguished by the <xref:Bodu.IO.Compound.CompoundEntryType> recorded in each entry:

- A **storage** (<xref:Bodu.IO.Compound.CompoundStorage>, the COM `IStorage`, <xref:Bodu.IO.Compound.CompoundEntryType.Storage>) is a named container of child storages and streams — a folder.
- A **stream** (<xref:Bodu.IO.Compound.CompoundStream>, the COM `IStream`, <xref:Bodu.IO.Compound.CompoundEntryType.Stream>) is a named, file-like leaf carrying an opaque byte payload — a file.

The single **root storage** (<xref:Bodu.IO.Compound.CompoundFile.RootStorage>) anchors the tree; it is distinguished by a <xref:Bodu.IO.Compound.CompoundEntryType.RootStorage> entry type and conventionally named `Root Entry`. It is special in one further way: it owns the mini-stream (see below), so its own start sector and size describe that mini-stream rather than a payload of its own.

Lookups are **scoped to a storage's direct children** — there is no path syntax; you descend one storage at a time. Names are matched with the **case-insensitive** compound-file relationship: two names are equal when their UTF-16 code units match after an invariant uppercase conversion, so `Workbook` and `WORKBOOK` denote the same entry. Two streams that share a name under *different* storages remain distinct, because the comparison only ever runs within one parent's children.

> [!IMPORTANT]
> The comparison is case-*insensitive*, not ordinal. Office stream names happen to have a fixed canonical casing, so an ordinal `HashSet<string>` over enumerated names works in practice — but the container itself will resolve `Workbook` from a request for `workbook`.

## Entry names

A directory entry stores its name in a fixed 64-byte UTF-16 field preceded by a two-byte length. The length counts *bytes including the terminating null*, so the maximum name is **31 characters** (`64 / 2 − 1`). Names are surfaced verbatim, including the control prefixes some Office streams carry — the `\x05` (`U+0005`) prefix on the `\x05SummaryInformation` and `\x05DocumentSummaryInformation` streams, and the `__substg1.0_` / `__attach_` prefixes used by `.msg` files — so a lookup must pass the exact prefixed name.

## Sector, FAT, and DIFAT

A stream's payload is not stored contiguously; it is split into fixed-size **sectors** and linked into a **sector chain** by the **file-allocation table (FAT)**. The FAT is itself an array of 32-bit sector identifiers — `FAT[n]` gives the next sector after sector `n`, and the chain terminates at the end-of-chain sentinel (`0xFFFFFFFE`). Other reserved sentinels mark free sectors (`0xFFFFFFFF`), FAT sectors, and DIFAT sectors. The reader walks the chain to assemble the payload, guarding against cycles (a chain longer than the FAT) and out-of-range identifiers.

The FAT can outgrow what the header holds inline. The **DIFAT** (double-indirect FAT) is the index of FAT sectors: the header stores the first 109 entries, and any overflow is chained through dedicated DIFAT sectors. The reader assembles the complete FAT from the inline entries plus the DIFAT chain when it opens the file.

Streams smaller than the **mini-stream cutoff** (typically 4096 bytes) are not given full sectors. Instead they live inside a single **mini-stream** — a regular-sector chain owned by the root storage — subdivided into 64-byte **mini sectors** and chained by a separate **mini-FAT**. This keeps a file full of tiny streams from wasting a full sector each. The split is an implementation detail the public API hides — you read bytes, not sectors — but it is why <xref:Bodu.IO.Compound.CompoundStream> materialises a small stream whole even under a streaming file, while a large stream is walked sector by sector.

## Directory

The **directory** is a chain of fixed **128-byte entries**, one per node, indexed by a zero-based **stream identifier (SID)** — entry 0 is always the root storage. Each entry records the node's name, type, red-black colour, the SIDs of its left sibling, right sibling, and child-tree root, its class id, state bits, creation and modified `FILETIME` timestamps, its start sector, and its payload size. A zero name length marks an unused (free) slot.

The children of a storage are not a flat list: they are arranged as a **red-black tree** keyed by the compound-file name relationship (shorter names sort first, then by uppercased code unit). The reader performs an in-order traversal of that tree to recover the children in **canonical (directory) order** — which is the order every enumerate API yields. The red-black colour is surfaced on <xref:Bodu.IO.Compound.CompoundEntryInfo.Color> (a <xref:Bodu.IO.Compound.CompoundEntryColor>) for diagnostic completeness; it is not needed to navigate.

<xref:Bodu.IO.Compound.CompoundEntryInfo> is the immutable, public snapshot of a directory entry returned by the enumerate APIs and by `Stat`. It is the managed counterpart of the COM `STATSTG` structure. The two timestamps are surfaced as nullable <xref:System.DateTimeOffset> values: a zero or out-of-range `FILETIME` (which stream entries conventionally record) becomes `null`.

## Buffered vs streaming

A compound file can be held two ways, chosen by the `buffered` flag on `CompoundFile.Open` or, equivalently, by a <xref:Bodu.IO.Compound.CompoundReadStrategy> on <xref:Bodu.IO.Compound.CompoundFileOptions.ReadStrategy>:

- **Buffered** (<xref:Bodu.IO.Compound.CompoundReadStrategy.Buffered>, the default) reads the whole source into memory at open time. Access never touches the original source afterward, the instance is read-only and safe to share across threads, and the source can be closed immediately.
- **Streaming** (<xref:Bodu.IO.Compound.CompoundReadStrategy.Streaming>, `buffered: false`) reads sectors on demand from a seekable source, bounding memory for large files. The source must stay open and unmodified for the instance's lifetime, and reads are serialised against the shared source position — concurrent, but not parallel.
- **Auto** (<xref:Bodu.IO.Compound.CompoundReadStrategy.Auto>) buffers a small seekable source and streams a large one, comparing the source length against <xref:Bodu.IO.Compound.CompoundFileOptions.MaxBufferedBytes> (64 MiB by default). It is only reachable through the options overload.

The choice is invisible to a <xref:Bodu.IO.Compound.CompoundStream> cursor's callers: the same `Read` / `Seek` behaviour applies either way, only the memory profile differs. Note that even under a streaming file, a stream *below the mini-stream cutoff* is materialised whole rather than walked, because its bytes live in the in-memory mini-stream.

## Stream cursor

A <xref:Bodu.IO.Compound.CompoundStream> obtained from a read-only file is a standard read-only, seekable <xref:System.IO.Stream> — `CanRead` and `CanSeek` are `true`, `CanWrite` is `false`, and `Write` / `SetLength` throw <xref:System.NotSupportedException>. Because it is a `Stream`, it composes with the BCL surfaces that consume one (`BinaryReader`, `StreamReader`, `CopyTo`, `ReadExactly`). Seeking past the end is permitted (the `Stream` contract); a subsequent read simply returns zero. <xref:Bodu.IO.Compound.CompoundStream.AsMemory> returns a whole-payload view independent of `Position` (no copy for a buffered stream; a full read for a streaming one), and `ReadAllBytes` copies the whole payload into a fresh array; prefer chunked `Read` for large streaming payloads. A single cursor is not safe for concurrent use because its `Position` advances as it reads — open one cursor per reader.

A cursor opened from a *writable* file is the inverse: it is a growable in-memory buffer whose `CanWrite` is `true`, and its edits are staged until <xref:Bodu.IO.Compound.CompoundFile.Commit> is called. Writing is covered in the [authoring guide](../../guides/io-compound/authoring-compound-files.md); the rest of this documentation set is read-focused.

## Property set

A **property set** is an OLE metadata stream that maps integer property IDs to typed values (strings, integers, booleans, `FILETIME` timestamps), grouped into code-paged sections. The two well-known ones carry authored document metadata: `\x05SummaryInformation` (title, author, timestamps, page/word counts) and `\x05DocumentSummaryInformation` (category, slide/line counts, and more). <xref:Bodu.IO.Compound.PropertySets.SummaryInformation> and <xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformation> are typed views that translate the well-known IDs into named, nullable properties; <xref:Bodu.IO.Compound.PropertySets.OlePropertySet> is the raw map underneath.

## Validation level

How strictly a malformed container is rejected is governed by <xref:Bodu.IO.Compound.CompoundValidationLevel>, set through <xref:Bodu.IO.Compound.CompoundFileOptions.ValidationLevel>. Every level enforces the memory-safety invariants needed to parse without faulting — the signature, byte-order marker, sector sizes, file length, allocation-table bounds, and the presence of a root storage are *always* validated. The level controls only how the reader responds to the remaining, recoverable inconsistencies:

| Level | Behaviour |
|---|---|
| <xref:Bodu.IO.Compound.CompoundValidationLevel.Strict> | Rejects every non-conformant condition, including individually malformed directory entries (bad name length or entry type), an out-of-range colour byte, a non-zero size on a storage, and unsorted or duplicated siblings. |
| <xref:Bodu.IO.Compound.CompoundValidationLevel.Compatible> | The default. Rejects structural corruption — allocation-table cycles, out-of-range or short sector chains, cyclic directory links — but tolerates an individually malformed directory entry by skipping it. |
| <xref:Bodu.IO.Compound.CompoundValidationLevel.Minimal> | Recovers where it can: a cyclic, out-of-range, or short chain stops and yields the data collected so far; a cyclic directory link prunes the offending subtree; a short chain is zero-padded to the declared size — none of these throw. |

The parameterless `Open` / `OpenRead` overloads use `Compatible`. Pass a <xref:Bodu.IO.Compound.CompoundFileOptions> to choose another level.

## Errors

A malformed container raises <xref:Bodu.IO.Compound.CompoundFileFormatException> at the `Compatible` and `Strict` levels. Beyond its message, the exception carries a message-independent <xref:Bodu.IO.Compound.CompoundFileError> on its <xref:Bodu.IO.Compound.CompoundFileFormatException.Category> property — `InvalidSignature`, `TruncatedFile`, `SectorOutOfRange`, `FatCycle`, `InvalidMiniFat`, `StreamChainTooShort`, `DirectoryCycle`, `InvalidRootStorage`, `InvalidPropertySet`, and more — which is the stable way to reason about *why* a file was rejected (useful when testing a reader against a corpus of deliberately broken files). All compound-file exceptions derive from the common base <xref:Bodu.IO.Compound.CompoundFileException>.

| Exception | Meaning |
|---|---|
| <xref:Bodu.IO.Compound.CompoundFileException> | The base type for every compound-file failure; catch it to handle them uniformly. |
| <xref:Bodu.IO.Compound.CompoundFileFormatException> | The content is not a well-formed compound file, or a stream's sector chain is malformed. Its `Category` classifies the failure. |
| <xref:Bodu.IO.Compound.CompoundStreamNotFoundException> | A named stream or storage was requested via the throwing `OpenStream` / `OpenStorage` and does not exist; its `StreamName` names the missing entry. The `Try*` forms return `false` instead. |

## Where to go next

- **[Getting started](getting-started.md)** — install + minimal samples.
- **[Introduction](index.md)** — the headline types and scenarios.
- **[Bodu.IO.Compound guides](../../guides/io-compound/index.md)** — the recipe-style walk-throughs.
