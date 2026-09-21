---
title: Reading nodes and tables
---

# Reading nodes and tables

<xref:Bodu.IO.Pst.PstFile> opens a PST file as a read-only session over its node database. This guide covers the end-to-end recipe: open the file, address a node by identifier, walk the directory and a node's private subnodes, and read the two LTP views every object exposes — the property context (a property bag) and the table context (a table). Values keep their on-disk wire types throughout; nothing here knows what a "subject" is.

The samples run against `sample1.pst` from the [runnable PST sample](../../samples/io-pst.md), copied as `archive.pst`; the quoted output is what they print.

## Pattern 1 — open a file

```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

Console.WriteLine($"{file.Format} format, block data: {file.CryptMethod}");

// Unicode format, block data: Permute
```

`OpenRead(path)` owns the file it opens. `OpenRead(stream, leaveOpen)` and `Open(stream, options, leaveOpen)` read a stream that must be readable and seekable (<xref:System.ArgumentException> otherwise); the container starts at the stream's *current position*, so a PST embedded in a larger stream opens without copying. Opening parses the header only — a wrong magic, an undeclared version, or (under `Strict`) a bad header CRC throws <xref:Bodu.IO.Pst.PstFileFormatException> here, and the 4 KiB-page OST variant throws <xref:Bodu.IO.Pst.PstUnsupportedFormatException>.

<xref:Bodu.IO.Pst.PstFile.Format> reports the variant (`Unicode`, 64-bit structures, or `Ansi`, 32-bit); every public type behaves identically over either. <xref:Bodu.IO.Pst.PstFile.CryptMethod> is the header's content encoding — `None`, `Permute`, or `Cyclic` — which the reader undoes transparently on every block read; you never see encoded bytes.

```csharp
using Bodu.IO.Pst;

using FileStream stream = File.OpenRead("archive.pst");
if (!PstFile.IsPstFile(stream))
    return;

var options = new PstFileOptions { ValidationLevel = PstValidationLevel.Strict };
using PstFile file = PstFile.Open(stream, options, leaveOpen: true);
Console.WriteLine(file.Format);
```

`IsPstFile` checks the magic only and restores the position. The options are covered in [Streaming and validation](streaming-and-validation.md).

## Node identifiers

Every object in the file is addressed by a <xref:Bodu.IO.Pst.PstNodeId>: a 32-bit value whose five low bits carry the <xref:Bodu.IO.Pst.PstNodeType> and whose 27 high bits carry an index. The type bits make identifiers self-describing, and the format leans on it — a folder's hierarchy, contents, and associated-contents tables reuse the folder's *index* with the corresponding *table* type bits, so you can compute them without any lookup.

```csharp
using Bodu.IO.Pst;

PstNodeId root = PstNodeId.RootFolder;                                    // 0x00000122
var hierarchy = new PstNodeId(PstNodeType.HierarchyTable, root.Index);   // same index, table type bits
var contents = new PstNodeId(PstNodeType.ContentsTable, root.Index);
var raw = new PstNodeId(0x00002223u);                                     // from a value read out of a table

Console.WriteLine($"{root} type={root.Type} index={root.Index}");
Console.WriteLine($"{hierarchy} type={hierarchy.Type} index={hierarchy.Index}");
Console.WriteLine($"{contents} type={contents.Type}");
Console.WriteLine($"{raw} type={raw.Type} index={raw.Index}");
Console.WriteLine(PstNodeId.MessageStore == new PstNodeId(0x21));        // True

// 0x00000122 type=NormalFolder index=9
// 0x0000012D type=HierarchyTable index=9
// 0x0000012E type=ContentsTable
// 0x00002223 type=SearchFolder index=273
// True
```

`new PstNodeId(type, index)` rejects an index above `0x07FFFFFF` with <xref:System.ArgumentOutOfRangeException>; `new PstNodeId(uint)` wraps any raw value. `ToString` renders `0x` plus eight upper-case hexadecimal digits, and the struct is `IEquatable` with `==` / `!=`.

Three fixed identifiers anchor every file and are exposed as well-known values:

| Well-known | Value | Holds |
|---|---|---|
| `PstNodeId.MessageStore` | `0x21` | The store object's property context (display name, record key, …). |
| `PstNodeId.NameToIdMap` | `0x61` | The named-property mapping the messaging layer resolves `0x8000`+ identifiers through. |
| `PstNodeId.RootFolder` | `0x122` | The root folder; user folders hang beneath it. |

The <xref:Bodu.IO.Pst.PstNodeType> values you will meet most: `NormalFolder` (`0x02`), `SearchFolder` (`0x03`), `NormalMessage` (`0x04`), `Attachment` (`0x05`), `AssociatedMessage` (`0x08`), the table kinds `HierarchyTable` (`0x0D`), `ContentsTable` (`0x0E`), `AssociatedContentsTable` (`0x0F`), `AttachmentTable` (`0x11`), `RecipientTable` (`0x12`), plus `Internal` (`0x01`) and `Ltp` (`0x1F`) for bookkeeping nodes. A file may carry type values MS-PST does not name; they surface as the raw number.

## Pattern 2 — walk the directory and look nodes up

```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

var census = new SortedDictionary<PstNodeType, int>();
long largest = 0;
foreach (PstNodeInfo info in file.EnumerateNodes())
{
    census[info.NodeId.Type] = census.GetValueOrDefault(info.NodeId.Type) + 1;
    largest = Math.Max(largest, info.DataLength);
}

foreach ((PstNodeType type, int count) in census)
    Console.WriteLine($"{type,-24}{count,5}");
Console.WriteLine($"largest payload: {largest} bytes");

PstNode store = file.GetNode(PstNodeId.MessageStore);             // throws PstNodeNotFoundException when absent
Console.WriteLine($"{store} parent={store.ParentId} subnodes={store.HasSubnodes} data={store.DataLength} bytes");

if (!file.TryGetNode(new PstNodeId(PstNodeType.NormalFolder, 0x7FFFFFF), out PstNode? missing))
    Console.WriteLine("no such node");

// Internal                   12
// NormalFolder                5
// SearchFolder                2
// NormalMessage               1
// SearchUpdateQueue           2
// SearchCriteria              2
// ReceiveFolderTable          1
// OutgoingQueueTable          1
// HierarchyTable              6
// ContentsTable               6
// AssociatedContentsTable     6
// SearchContentsTable         3
// AttachmentTable             1
// RecipientTable              1
// 22                          1
// 23                          1
// 24                          1
// largest payload: 4198 bytes
// 0x00000021 (Internal) parent=0x00000000 subnodes=False data=290 bytes
// no such node
```

`EnumerateNodes` walks the node B-tree in identifier order and yields a <xref:Bodu.IO.Pst.PstNodeInfo> snapshot per node — `NodeId`, `ParentNodeId`, `DataLength`, `HasSubnodes` — resolving the length from the block B-tree without reading any payload. `GetNode` throws <xref:Bodu.IO.Pst.PstNodeNotFoundException> (`Error == PstFileError.NodeNotFound`) for an absent identifier; `TryGetNode` returns `false`. Either returns a <xref:Bodu.IO.Pst.PstNode> that caches its data-tree leaf list and subnode directory for its own lifetime, so keep the instance around when you will read it more than once.

## Pattern 3 — dump the store node's property context

`ReadPropertyContext` parses the node's heap and returns a <xref:Bodu.IO.Pst.PstPropertyContext>: a read-only collection of <xref:Bodu.IO.Pst.PstPropertyValue> keyed by 16-bit property identifier, in ascending identifier order. The records are materialized when the context is read, but each value's payload resolves only when accessed, so listing identifiers and wire types costs nothing per value.

```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

PstNode store = file.GetNode(PstNodeId.MessageStore);
PstPropertyContext properties = store.ReadPropertyContext();

Console.WriteLine($"{properties.Count} properties");
foreach (ushort id in properties.EnumeratePropertyIds())
{
    properties.TryGetWireType(id, out ushort wireType);
    Console.WriteLine($"  0x{id:X4} wire 0x{wireType:X4}");
}

// PidTagDisplayName (0x3001): a UTF-16 string in a Unicode-format file.
if (properties.TryGetValue(0x3001, out PstPropertyValue displayName) && displayName.WireType == 0x001F)
    Console.WriteLine($"display name: {displayName.GetString()}");

// PidTagRecordKey (0x0FF9): opaque bytes.
if (properties.Contains(0x0FF9))
    Console.WriteLine($"record key: {Convert.ToHexString(properties.GetValue(0x0FF9).GetBytes())}");

// Every value at once, with its diagnostic form.
foreach (PstPropertyValue value in properties)
    Console.WriteLine($"  {value}");

// 12 properties
//   0x0E34 wire 0x0102
//   0x0E38 wire 0x0003
//   0x0FF9 wire 0x0102
//   0x3001 wire 0x001F
//   ...
// display name: sample1
// record key: 6A552B813C43F94384F18B7DA2393E95
//   0x0E34 (0x0102, 24 bytes)
//   0x0E38 (0x0003, 4 bytes)
//   0x3001 (0x001F, 14 bytes)
//   ...
```

The lookup surface: `Count`, `Contains(id)`, `EnumeratePropertyIds()`, `TryGetWireType(id, out wireType)` (no payload read), `TryGetValue(id, out value)`, and `GetValue(id)`, which throws <xref:Bodu.IO.Pst.PstFileException> with `Error == PstFileError.PropertyNotFound` for an absent identifier. Enumerating the context resolves every payload in turn. `TryGetValueLength` and `TryOpenValueStream` are the streaming pair, covered in [Streaming and validation](streaming-and-validation.md).

> [!NOTE]
> `ReadPropertyContext` and `ReadTableContext` re-read the heap from the source on every call. Retain the returned instance for the duration of your reads rather than calling them per property or per row.

### Typed accessors

A <xref:Bodu.IO.Pst.PstPropertyValue> carries `PropertyId`, the raw `WireType` (the MS-OXCDATA code, unchanged from the file), and `RawData` — the resolved little-endian payload (empty for a null value). The typed accessors require the matching wire type and at least the type's width; a mismatch throws <xref:System.InvalidOperationException>.

| Accessor | Accepts wire type | Returns |
|---|---|---|
| `GetInt16()` | `0x0002` | `short` |
| `GetInt32()` | `0x0003`, or the 32-bit error code `0x000A` | `int` |
| `GetInt64()` | `0x0014`, the currency `0x0006`, or the FILETIME `0x0040` | `long` — for `0x0040`, pass to `DateTime.FromFileTimeUtc` |
| `GetBoolean()` | `0x000B` | `bool` (any nonzero byte) |
| `GetSingle()` | `0x0004` | `float` |
| `GetDouble()` | `0x0005`, or the floating time `0x0007` | `double` |
| `GetGuid()` | `0x0048` | `Guid` |
| `GetString()` | `0x001F` only | `string` (UTF-16LE) |
| `GetBytes()` | any | a copy of `RawData` |

`GetString` deliberately handles only the UTF-16 type: a code-page string (`0x001E`) stays bytes because choosing its encoding is a format-layer decision, as are multi-valued (`0x1000` flag) and object-typed payloads, which surface raw. The mail-store reader is where those decode.

```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");
PstPropertyContext properties = file.GetNode(PstNodeId.MessageStore).ReadPropertyContext();

try
{
    _ = properties.GetValue(0x3001).GetInt32();   // the display name is a string
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
}

try
{
    _ = properties.GetValue(0x0001);              // not present
}
catch (PstFileException ex) when (ex.Error == PstFileError.PropertyNotFound)
{
    Console.WriteLine(ex.Message);
}

// The property value has wire type 0x001F and cannot be read as 0003.
// No property 0x0001 exists in the property context of node 0x00000021.
```

## Pattern 4 — list the root hierarchy table

`ReadTableContext` returns a <xref:Bodu.IO.Pst.PstTableContext>: `Columns` (a <xref:Bodu.IO.Pst.PstTableColumn> per column — property identifier, wire type, cell width), `RowCount` from the table's row index, and rows whose identifier names the object the row stands for. A folder's hierarchy table lists its child folders, so each `RowId` is a child folder's node identifier.

```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

var hierarchyId = new PstNodeId(PstNodeType.HierarchyTable, PstNodeId.RootFolder.Index);
PstNode tableNode = file.GetNode(hierarchyId);
PstTableContext table = tableNode.ReadTableContext();

Console.WriteLine($"{table.RowCount} rows x {table.Columns.Count} columns");
foreach (PstTableColumn column in table.Columns)
    Console.WriteLine($"  {column}");

foreach (PstTableRow row in table.EnumerateRows())
{
    var childId = new PstNodeId(row.RowId);                  // each row stands for a child folder node

    string name = row.TryGetCell(0x3001, out PstPropertyValue display) && display.WireType == 0x001F
        ? display.GetString()
        : "(unnamed)";
    int messages = row.TryGetCell(0x3602, out PstPropertyValue count) ? count.GetInt32() : 0;   // PidTagContentCount
    bool hasChildren = row.TryGetCell(0x360A, out PstPropertyValue sub) && sub.GetBoolean();     // PidTagSubfolders

    Console.WriteLine($"  {childId} ({childId.Type}) {name}: {messages} messages, subfolders={hasChildren}");
}

// 4 rows x 13 columns
//   0x0E30 (0x0102, 4 bytes)
//   0x0E33 (0x0014, 8 bytes)
//   ...
//   0x3001 (0x001F, 4 bytes)
//   0x3602 (0x0003, 4 bytes)
//   0x360A (0x000B, 1 bytes)
//   ...
//   0x00008022 (NormalFolder) Top of Outlook data file: 0 messages, subfolders=True
//   0x00008042 (NormalFolder) Search Root: 0 messages, subfolders=False
//   0x00002223 (SearchFolder) SPAM Search Folder 2: 0 messages, subfolders=False
//   0x00080023 (SearchFolder) ItemProcSearch: 0 messages, subfolders=False
```

Note the widths: a variable-size column such as the `0x001F` display name occupies four bytes in the row — a value reference into the heap or a subnode — and resolves when the cell is read, exactly like a property-context value. Fixed-width cells (`Int32`, `Boolean`, the eight-byte `Int64`) sit inline. The container lists the two search folders the mail-store reader deliberately hides.

`EnumerateRows` streams the row matrix one block at a time and never materializes the whole table; each yielded <xref:Bodu.IO.Pst.PstTableRow> copies its own bytes, so rows stay valid after the enumeration advances. The row surface is `RowId`, `TryGetCell(id, out value)` — `true` only when the table declares the column *and* the row's existence bitmap marks the cell present — `EnumerateCells()` over the present cells, and the streaming pair `TryGetCellLength` / `TryOpenCellStream`.

```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

var hierarchyId = new PstNodeId(PstNodeType.HierarchyTable, PstNodeId.RootFolder.Index);
PstTableContext table = file.GetNode(hierarchyId).ReadTableContext();

uint[] rowIds = table.EnumerateRowIds().ToArray();          // no row materialized
Console.WriteLine(string.Join(", ", rowIds.Select(id => new PstNodeId(id))));

if (table.TryGetRow(rowIds[0], out PstTableRow? row))
{
    foreach (PstPropertyValue cell in row.EnumerateCells())
        Console.WriteLine($"  {cell}");
}

// 0x00008022, 0x00008042, 0x00002223, 0x00080023
//   0x3001 (0x001F, 48 bytes)
//   0x3602 (0x0003, 4 bytes)
//   0x3603 (0x0003, 4 bytes)
//   0x360A (0x000B, 1 bytes)
//   0x67F2 (0x0003, 4 bytes)
//   0x67F3 (0x0003, 4 bytes)
```

`EnumerateRowIds` reads the leading dword of each row slot in place and allocates nothing per row — the right call when, as with hierarchy, contents, and attachment tables, you only want the referenced node identifiers. `TryGetRow(rowId, out row)` goes through the row index for a keyed lookup; it returns `false` for an unknown identifier and throws <xref:Bodu.IO.Pst.PstFileFormatException> when the index names a row the matrix does not hold.

## Pattern 5 — read a message node without MAPI semantics

A message is a `NormalMessage` node whose property context holds the message's properties and whose private **subnode tree** holds its recipient table, attachment table, and attachment objects — invisible to the node B-tree, reachable only through the owning node.

```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

PstNodeInfo first = file.EnumerateNodes().First(n => n.NodeId.Type == PstNodeType.NormalMessage);
PstNode message = file.GetNode(first.NodeId);
PstPropertyContext properties = message.ReadPropertyContext();

Console.WriteLine($"{message}: {properties.Count} properties, parent folder {message.ParentId}");

// 0x0037 PidTagSubject, 0x001A PidTagMessageClass — UTF-16 (0x001F) in a Unicode-format file.
if (properties.TryGetValue(0x0037, out PstPropertyValue subject) && subject.WireType == 0x001F)
    Console.WriteLine($"subject: {subject.GetString()}");
if (properties.TryGetValue(0x001A, out PstPropertyValue messageClass) && messageClass.WireType == 0x001F)
    Console.WriteLine($"class: {messageClass.GetString()}");

// 0x0E06 PidTagMessageDeliveryTime — a FILETIME (0x0040), read as Int64 ticks.
if (properties.TryGetValue(0x0E06, out PstPropertyValue delivered) && delivered.WireType == 0x0040)
    Console.WriteLine($"delivered: {DateTime.FromFileTimeUtc(delivered.GetInt64()):u}");

// 0x0E07 PidTagMessageFlags — Int32; bit 0 is MSGFLAG_READ.
if (properties.TryGetValue(0x0E07, out PstPropertyValue flags))
    Console.WriteLine($"flags: 0x{flags.GetInt32():X8}");

// The message's private subnodes: recipient table, attachment table, attachment objects.
foreach (PstNodeInfo subnode in message.EnumerateSubnodes())
    Console.WriteLine($"  subnode {subnode.NodeId} ({subnode.NodeId.Type}), {subnode.DataLength} bytes");

if (message.TryGetSubnodeOfType(PstNodeType.RecipientTable, out PstNode? recipientTable))
{
    foreach (PstTableRow row in recipientTable.ReadTableContext().EnumerateRows())
    {
        string name = row.TryGetCell(0x3001, out PstPropertyValue display) && display.WireType == 0x001F ? display.GetString() : "?";
        int type = row.TryGetCell(0x0C15, out PstPropertyValue recipientType) ? recipientType.GetInt32() : 0;   // 1 = To, 2 = Cc, 3 = Bcc
        Console.WriteLine($"  recipient type {type}: {name}");
    }
}

// 0x00200024 (NormalMessage): 110 properties, parent folder 0x00008082
// subject: Here is a sample message
// class: IPM.Note
// delivered: 2010-03-15 17:12:07Z
// flags: 0x00000031
//   subnode 0x00000671 (AttachmentTable), 514 bytes
//   subnode 0x00000692 (RecipientTable), 1000 bytes
//   subnode 0x00008025 (Attachment), 326 bytes
//   subnode 0x0000807F (Ltp), 1701 bytes
//   subnode 0x0000809F (Ltp), 2196 bytes
//   recipient type 1: Terry Mahaffey
```

`EnumerateSubnodes` yields <xref:Bodu.IO.Pst.PstNodeInfo> snapshots in stored order, with `ParentNodeId` set to the owning node; `TryGetSubnode(id, out node)` resolves one by identifier and `TryGetSubnodeOfType(type, out node)` the first of a type in a single pass — the natural way to reach a message's recipient and attachment tables. A subnode is a full <xref:Bodu.IO.Pst.PstNode>: it has its own payload, its own LTP views, and (for an attachment object) may carry a subnode tree of its own holding an embedded message. The two `Ltp` subnodes above hold large property values that overflowed the message's heap-on-node; the property context resolves them transparently when their property is read.

Which of these identifiers *means* subject, delivery time, or recipient type is MS-OXPROPS knowledge the container does not carry — that is the [mail-store reader's](../outlook/reading-pst-mail-stores.md) job, and the reason this layer exists separately.

## API summary

| Type | Members |
|---|---|
| <xref:Bodu.IO.Pst.PstFile> | `OpenRead(path)` · `OpenRead(stream, leaveOpen)` · `Open(stream, options, leaveOpen)` · `IsPstFile(stream)` · `Format` · `CryptMethod` · `EnumerateNodes()` · `GetNode(id)` · `TryGetNode(id, out node)` · `Dispose` |
| <xref:Bodu.IO.Pst.PstNodeId> | `.ctor(uint)` · `.ctor(type, index)` · `MessageStore` · `NameToIdMap` · `RootFolder` · `Value` · `Type` · `Index` |
| <xref:Bodu.IO.Pst.PstNode> | `Id` · `ParentId` · `HasSubnodes` · `DataLength` · `ReadAllBytes()` · `OpenDataStream()` · `EnumerateSubnodes()` · `TryGetSubnode(id, out node)` · `TryGetSubnodeOfType(type, out node)` · `ReadPropertyContext()` · `ReadTableContext()` |
| <xref:Bodu.IO.Pst.PstPropertyContext> | `Count` · `Contains(id)` · `EnumeratePropertyIds()` · `TryGetWireType(id, out wireType)` · `TryGetValue(id, out value)` · `GetValue(id)` · `TryGetValueLength(id, out length)` · `TryOpenValueStream(id, out stream)` · enumeration |
| <xref:Bodu.IO.Pst.PstPropertyValue> | `PropertyId` · `WireType` · `RawData` · `GetInt16()` · `GetInt32()` · `GetInt64()` · `GetBoolean()` · `GetSingle()` · `GetDouble()` · `GetGuid()` · `GetString()` · `GetBytes()` |
| <xref:Bodu.IO.Pst.PstTableContext> | `Columns` · `RowCount` · `EnumerateRows()` · `EnumerateRowIds()` · `TryGetRow(rowId, out row)` |
| <xref:Bodu.IO.Pst.PstTableRow> | `RowId` · `TryGetCell(id, out value)` · `EnumerateCells()` · `TryGetCellLength(id, out length)` · `TryOpenCellStream(id, out stream)` |

## Where to go next

- [Streaming and validation](streaming-and-validation.md) — the length/stream pairs for large payloads, `PstFileOptions`, and the full `PstFileError` catalogue.
- [Reading .pst mail stores](../outlook/reading-pst-mail-stores.md) — the same nodes with MAPI meaning attached.
- [Bodu.IO.Pst core concepts](../../docs/io-pst/concepts.md) — NDB, NID/BID, data and subnode trees, heap-on-node, and the two contexts in depth.
- [Runnable PST sample](../../samples/io-pst.md) — the fixtures these samples ran against.
- [Bodu.IO.Pst guides](index.md) — every guide in this topic.
