# Bodu.IO.Pst

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A **low-level, read-only container library** for the Outlook personal-folders format
(PST, [MS-PST]). It reads the **node database (NDB)** layer of a Unicode-format file —
the header, the node and block B-trees, block data with the format's permute and
cyclic content encodings decoded and checksums verified, multi-block data trees, and
per-node subnode trees — and the **LTP (Lists, Tables, Properties)** layer over it:
each node's heap-on-node, BTree-on-heap, and the property-context and table-context
views with wire-typed values.

This is the substrate a message-level reader builds on — the same layering as
[`Bodu.IO.Compound`](../Bodu.IO.Compound) beneath the `.msg` reader. The library
speaks node identifiers, 16-bit property ids, and raw wire-typed payloads; folder,
message, recipient, and attachment semantics (the messaging layer of MS-PST) belong
to a future `Bodu.Formats.Outlook.Pst` package over the shared
[`Bodu.Formats.Outlook`](../Bodu.Formats.Outlook) value model.

```csharp
using Bodu.IO.Pst;

using var file = PstFile.OpenRead("archive.pst");

Console.WriteLine($"{file.Format}, content encoding: {file.CryptMethod}");

foreach (PstNodeInfo info in file.EnumerateNodes())
    Console.WriteLine($"{info.NodeId} ({info.NodeId.Type}): {info.DataLength} bytes");

// Raw node payloads (NDB) …
PstNode store = file.GetNode(PstNodeId.MessageStore);
byte[] payload = store.ReadAllBytes();

// … and typed LTP views: property bags and tables.
PstPropertyContext properties = store.ReadPropertyContext();
if (properties.TryGetValue(0x3001 /* display name */, out PstPropertyValue name))
    Console.WriteLine(name.GetString());
```

## What it reads

- **Header** — format discrimination (`wVer`), the content-encoding method
  (`bCryptMethod`), and the B-tree roots, with the header checksum verified.
- **Node B-tree (NBT)** — every node's identifier, parent, data-block and
  subnode-block references; enumerated in identifier order or looked up by id.
- **Block B-tree (BBT)** — block resolution with trailer validation and, under
  `PstValidationLevel.Strict`, per-block checksum and signature verification.
- **Content encodings** — the MS-PST §5.1 permute and §5.2 cyclic byte
  substitutions are decoded transparently; real encryption (Windows Information
  Protection) is rejected as unsupported.
- **Data trees** — XBLOCK/XXBLOCK multi-block payloads flattened to a single
  byte sequence (`ReadAllBytes` / `OpenDataStream`).
- **Subnode trees** — SLBLOCK/SIBLOCK walks exposing each node's private
  namespace (`EnumerateSubnodes` / `TryGetSubnode`).
- **Heap-on-node and BTree-on-heap** — the LTP allocator (HN) parsed over each
  node's ordered data blocks, and the keyed record store (BTH) built on it, with
  all heap geometry validated at every validation level.
- **Property contexts** — `PstNode.ReadPropertyContext()` returns the node's
  property bag (`PstPropertyContext`): 16-bit property ids with raw wire type
  codes and resolved payloads (`PstPropertyValue` typed accessors), whether the
  value is inline, heap-resident, or subnode-resident.
- **Table contexts** — `PstNode.ReadTableContext()` returns the node's table
  (`PstTableContext`): typed columns, the row count from the row index, keyed
  row lookup, and forward-only row enumeration that streams the row matrix one
  block at a time (`PstTableRow` cell access honors the existence bitmap).

Validation is tiered via `PstFileOptions.ValidationLevel`: `Compatible` (default,
structural checks plus the header checksum), `Strict` (every page and block checksum
and signature), and `Minimal` (salvage reads of damaged files).

## Out of scope

- The **ANSI** format (`wVer` 14/15) and the 4 KiB-page OST variant — recognized
  and rejected with `PstUnsupportedFormatException`, not read.
- **MAPI and messaging semantics** — folders, messages, recipients, attachments,
  named-property resolution, and multi-valued/object payload decoding (surfaced
  raw). These belong to the future `Bodu.Formats.Outlook.Pst` reader.
- Writing, repair, and password handling (the format's password is advisory only).

[MS-PST]: https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-pst/
