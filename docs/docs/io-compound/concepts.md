---
title: Bodu.IO.Compound — Core concepts
---

# Bodu.IO.Compound — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [guides](../../guides/io-compound/index.md), and refer back whenever a term feels imprecise.

`Bodu.IO.Compound` is part of the **[Binary Formats & I/O](../topics/binary-formats.md)** topic — the container tier beneath the format readers. For the high-level shape of the library, start with the [introduction](index.md).

## Compound file (CFB)

A **compound file** — also called OLE2 structured storage or Compound File Binary (CFB) — is a single physical file that embeds a small hierarchical file system. It begins with an eight-byte signature (`D0 CF 11 E0 A1 B1 1A E1`), followed by a 512-byte header, allocation tables, a directory, and a series of fixed-size sectors holding the actual bytes. Legacy Microsoft Office documents (`.xls`, `.doc`, `.ppt`, `.msg`) are compound files. <xref:Bodu.IO.Compound.CompoundFile> parses the container and exposes its hierarchy.

## Storage and stream

The container's directory is a tree of two node kinds:

- A **storage** (<xref:Bodu.IO.Compound.CompoundStorage>, the COM `IStorage`) is a named container of child storages and streams — a folder.
- A **stream** (<xref:Bodu.IO.Compound.CompoundStream>, the COM `IStream`) is a named, file-like leaf carrying an opaque byte payload — a file.

The single **root storage** (<xref:Bodu.IO.Compound.CompoundFile.RootStorage>) anchors the tree; it is distinguished by a <xref:Bodu.IO.Compound.CompoundEntryType.RootStorage> entry type and conventionally named `Root Entry`. Lookups are **scoped to direct children** and compared with **ordinal (case-sensitive)** equality, so two streams that share a name under different storages stay distinct.

## Sector and FAT

A stream's payload is not stored contiguously; it is split into fixed-size **sectors** (typically 512 bytes) linked into a **sector chain** by a file-allocation table (FAT). The reader follows the chain to assemble the payload. Streams smaller than a threshold (the **mini-stream cutoff**) live in a separate **mini-FAT** and **mini-stream** with smaller sectors, so a file full of tiny streams does not waste a full sector each. This split is an implementation detail the public API hides — you read bytes, not sectors — but it is why a streaming read advances sector by sector.

## Directory

The **directory** is the table of entries, one per node, recording each node's name, type, size, start sector, class id, timestamps, and its position in the red-black tree that orders siblings. <xref:Bodu.IO.Compound.CompoundEntryInfo> is the immutable, public snapshot of a directory entry returned by the enumerate APIs and by `Stat`.

## Buffered vs streaming

A compound file can be held two ways, chosen by the `buffered` flag on `CompoundFile.Open`:

- **Buffered** (the default) reads the whole source into memory at open time. Access never touches the original source afterward, the instance is read-only and safe to share across threads, and the source can be closed immediately.
- **Streaming** (`buffered: false`) reads sectors on demand from a seekable source, bounding memory for large files. The source must stay open and unmodified for the instance's lifetime, and reads are serialized against the shared source position — concurrent, but not parallel.

The choice is invisible to a <xref:Bodu.IO.Compound.CompoundStream> cursor's callers: the same `Read` / `Seek` behaviour applies either way, only the memory profile differs.

## Stream cursor

A <xref:Bodu.IO.Compound.CompoundStream> is a standard read-only, seekable <xref:System.IO.Stream> — `CanRead` and `CanSeek` are `true`, `CanWrite` is `false`, and `Write` / `SetLength` throw. Because it is a `Stream`, it composes with the BCL surfaces that consume one (`BinaryReader`, `StreamReader`, `CopyTo`). <xref:Bodu.IO.Compound.CompoundStream.AsMemory> returns a whole-payload view (no copy for a buffered stream; a full read for a streaming one); prefer chunked `Read` for large streaming payloads. A single cursor is not safe for concurrent use because its `Position` advances as it reads.

## Property set

A **property set** is an OLE metadata stream that maps integer property IDs to typed values (strings, integers, booleans, `FILETIME` timestamps), grouped into code-paged sections. The two well-known ones carry authored document metadata: `\x05SummaryInformation` (title, author, timestamps, page/word counts) and `\x05DocumentSummaryInformation` (category, slide/line counts, and more). <xref:Bodu.IO.Compound.PropertySets.SummaryInformation> and <xref:Bodu.IO.Compound.PropertySets.DocumentSummaryInformation> are typed views that translate the well-known IDs into named, nullable properties; <xref:Bodu.IO.Compound.PropertySets.OlePropertySet> is the raw map underneath.

## Errors

| Exception | Meaning |
|---|---|
| <xref:Bodu.IO.Compound.CompoundFileFormatException> | The content is not a well-formed compound file, or a stream's sector chain is malformed. |
| <xref:Bodu.IO.Compound.CompoundStreamNotFoundException> | A named stream or storage was requested via the throwing `OpenStream` / `OpenStorage` and does not exist; its `StreamName` names the missing entry. The `Try*` forms return `false` instead. |

## Where to go next

- **[Getting started](getting-started.md)** — install + minimal samples.
- **[Introduction](index.md)** — the headline types and scenarios.
- **[Bodu.IO.Compound guides](../../guides/io-compound/index.md)** — the recipe-style walk-throughs.
