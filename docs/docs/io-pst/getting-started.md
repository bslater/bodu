---
title: Bodu.IO.Pst — Getting started
---

# Bodu.IO.Pst — Getting started

Unfamiliar with terms like *node database*, *NID*, *data tree*, *heap-on-node*, or *table context*? Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.IO.Pst
```

Targets `net8.0`. Depends only on `Bodu.Core` for shared throw-helpers; no other NuGet references. For the message-level view — folders, subjects, senders, attachments — install `Bodu.Formats.Outlook.Pst` instead (it references this package) and see [the mail-store sample below](#read-the-mail-store-instead).

## Open a file and read the store node

<!-- compile -->
```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

PstNode store = file.GetNode(PstNodeId.MessageStore);
foreach (PstPropertyValue value in store.ReadPropertyContext())
    Console.WriteLine($"0x{value.PropertyId:X4} (wire 0x{value.WireType:X4}): {value.RawData.Length} bytes");
```

The returned session is <xref:System.IDisposable> — the `using` declaration disposes it and closes the source unless `leaveOpen: true` was passed to `Open`. Reads are lazy: opening parses only the header, and each node's payload is read when asked for.

## Probe before opening

```csharp
using Bodu.IO.Pst;

using FileStream source = File.OpenRead(path);

if (PstFile.IsPstFile(source))
{
    using PstFile file = PstFile.OpenRead(source, leaveOpen: true);
    // ...
}
```

`IsPstFile` checks only the `!BDN` magic and restores the stream position, so it is cheap to call ahead of a full open. It answers `true` for *any* PST variant — a subsequent open of an ANSI or OST file throws <xref:Bodu.IO.Pst.PstUnsupportedFormatException>.

## Enumerate the node directory

<!-- compile -->
```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

foreach (PstNodeInfo info in file.EnumerateNodes())
    Console.WriteLine($"{info.NodeId} ({info.NodeId.Type}): {info.DataLength} bytes" +
        (info.HasSubnodes ? " + subnodes" : string.Empty));
```

`EnumerateNodes` walks the node B-tree in identifier order and yields metadata snapshots without reading any payload.

## Read a table context

<!-- compile -->
```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

// A folder's hierarchy table reuses the folder's index with the hierarchy-table type bits.
PstNodeId root = PstNodeId.RootFolder;
var hierarchyId = new PstNodeId(PstNodeType.HierarchyTable, root.Index);

if (file.TryGetNode(hierarchyId, out PstNode? table))
{
    PstTableContext context = table.ReadTableContext();
    Console.WriteLine($"{context.RowCount} rows x {context.Columns.Count} columns");

    foreach (PstTableRow row in context.EnumerateRows())
        Console.WriteLine($"row 0x{row.RowId:X8}");   // each RowId is a child folder's NID
}
```

Row enumeration streams the row matrix one block at a time; each yielded row owns its bytes and stays valid after the enumeration advances.

## Stream a large payload

```csharp
using Bodu.IO.Pst;

using PstFile file = PstFile.OpenRead("archive.pst");

PstNode node = file.GetNode(someId);
Console.WriteLine($"{node.DataLength} bytes");        // priced without reading the payload

using Stream data = node.OpenDataStream();            // one leaf block resident at a time
data.CopyTo(destination);
```

`ReadAllBytes()` is the buffered convenience; `OpenDataStream()` never materializes the whole payload, so it is the right call for multi-megabyte attachments.

## Tune validation and caching

<!-- compile -->
```csharp
using Bodu.IO.Pst;

var options = new PstFileOptions
{
    ValidationLevel = PstValidationLevel.Strict,   // every CRC and signature enforced
    BlockCacheSize = 512,                          // decoded-block LRU entries (0 disables)
};

using PstFile file = PstFile.Open(File.OpenRead("archive.pst"), options);
```

## Handle malformed input

```csharp
using Bodu.IO.Pst;

try
{
    using PstFile file = PstFile.OpenRead(path);
    // ...
}
catch (PstUnsupportedFormatException)
{
    Console.WriteLine("A recognized but unsupported PST variant (ANSI or OST).");
}
catch (PstFileFormatException ex)
{
    Console.WriteLine($"Malformed: {ex.Error} — {ex.Message}");
}
```

Every failure surfaces through the <xref:Bodu.IO.Pst.PstFileException> family with a <xref:Bodu.IO.Pst.PstFileError> category — corruption never escapes as any other exception type.

## Read the mail store instead

When you want messages rather than nodes, layer `Bodu.Formats.Outlook.Pst` on top:

```bash
dotnet add package Bodu.Formats.Outlook.Pst
```

<!-- compile -->
```csharp
using Bodu.Formats.Outlook;

using var store = OutlookMailStore.OpenRead("archive.pst");

foreach (OutlookMailFolder folder in store.RootFolder.EnumerateSubfolders())
{
    Console.WriteLine($"{folder.DisplayName} ({folder.MessageCount?.ToString() ?? "?"} messages)");

    foreach (OutlookMailMessage message in folder.EnumerateMessages())
    {
        Console.WriteLine($"  {message.Subject} — {message.SenderName} at {message.SentTime}");

        foreach (OutlookRecipient recipient in message.Recipients)
            Console.WriteLine($"    to {recipient.DisplayName} <{recipient.EmailAddress}>");

        foreach (OutlookMailAttachment attachment in message.Attachments)
            Console.WriteLine($"    attachment {attachment.FileName} ({attachment.Size} bytes)");
    }
}
```

## Where to go next

- **[Core concepts](concepts.md)** — the vocabulary in depth.
- **[Introduction](index.md)** — scope, scenarios, and headline types.
- **API reference** — [Bodu.IO.Pst](xref:Bodu.IO.Pst) · [Bodu.Formats.Outlook](xref:Bodu.Formats.Outlook).
