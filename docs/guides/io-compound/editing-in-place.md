---
title: Editing an existing container in place
---

# Editing an existing container in place

[Authoring compound files](authoring-compound-files.md) shows two ways to write: build a detached tree with <xref:Bodu.IO.Compound.Builders.CompoundStorageBuilder> and serialize it once, or create a fresh writable <xref:Bodu.IO.Compound.CompoundFile> and `Commit`. This guide covers the third: open a container that already exists — a `.doc`, an `.xls`, a `.msg` — for update, mutate it through the same storage and stream surface you read it with, and write it back to the same destination with a single `Commit`, or throw the edits away with `Revert`.

The samples run against `sample1.doc` from the [runnable compound-file sample](../../samples/io-compound.md), a Word 2000 document copied as `report.doc`; the quoted output is what they print.

## How update mode works

`CompoundFile.Open(path, FileMode.Open, FileAccess.ReadWrite)` — or the stream overload `Open(stream, FileMode.Open, FileAccess.ReadWrite, leaveOpen)` — mirrors `System.IO.Packaging.Package.Open`. It reads the whole existing container once, parses it, and loads the hierarchy into an in-memory **staging tree**: every storage's metadata and every stream's full payload. It also keeps an independent **baseline** clone of that tree. From then on:

- `RootStorage` and everything beneath it operate on the staging tree. Reads see your edits immediately; the destination sees nothing.
- `IsDirty` turns `true` on the first mutation — opening a writable cursor counts, even before anything is written — and `false` after `Commit` or `Revert`.
- `Commit` (or `CommitAsync`) truncates the destination and **rewrites the whole container** from the staging tree. There is no in-place patching of sectors — the layout is recomputed from scratch every time.
- `Revert` resets the staging tree to the baseline — the state loaded **at open**.
- `Dispose` without `Commit` leaves the destination exactly as it was.

The mode/access combinations `Open(stream, mode, access)` accepts:

| `FileMode` | `FileAccess` | Result |
|---|---|---|
| `Open` | `Read` | Read-only, as `CompoundFile.Open(stream)`; `buffered` applies. |
| `Open` | `ReadWrite` | **Update mode** — loads the existing content for editing. |
| `Create` / `CreateNew` | `Write` or `ReadWrite` | A new, empty writable file, as `CompoundFile.Create`. |
| `OpenOrCreate` | `ReadWrite` | Update mode when the stream holds content; create mode when it is empty. |
| `Open` | `Write` | <xref:System.NotSupportedException> — mutating an existing file requires read access, because update loads a snapshot first. |
| anything else | | <xref:System.NotSupportedException>. |

The path overload adds a `FileShare` (default `FileShare.Read`) and owns the `FileStream` it opens; `FileNotFoundException` surfaces when `Open` names a file that does not exist.

## Pattern 1 — add a stream to an existing `.doc` and commit

```csharp
using Bodu.IO.Compound;
using System.Text;

File.Copy("report.doc", "report-edited.doc", overwrite: true);

using (CompoundFile file = CompoundFile.Open("report-edited.doc", FileMode.Open, FileAccess.ReadWrite))
{
    Console.WriteLine($"opened: CanWrite={file.CanWrite} IsDirty={file.IsDirty}");

    file.RootStorage.CreateStream("Bodu.Notes", Encoding.UTF8.GetBytes("reviewed 2026-09-09"));
    Console.WriteLine($"staged:  IsDirty={file.IsDirty}");

    file.Commit();                                   // the container is rewritten now
    Console.WriteLine($"committed: IsDirty={file.IsDirty}");
}

using CompoundFile check = CompoundFile.OpenRead("report-edited.doc");
foreach (CompoundEntryInfo entry in check.RootStorage.EnumerateEntries())
    Console.WriteLine($"  {entry.EntryType} {entry.Name.Replace("\u0005", "\\x05").Replace("\u0001", "\\x01")} ({entry.Length} bytes)");

// opened: CanWrite=True IsDirty=False
// staged:  IsDirty=True
// committed: IsDirty=False
//   Stream 1Table (8375 bytes)
//   Stream \x01CompObj (106 bytes)
//   Stream Bodu.Notes (19 bytes)
//   Storage ObjectPool (0 bytes)
//   Stream WordDocument (9280 bytes)
//   Stream \x05SummaryInformation (4096 bytes)
//   Stream \x05DocumentSummaryInformation (4096 bytes)
```

`CreateStream(name, ReadOnlyMemory<byte>)` stages a complete payload in one call and throws <xref:System.ArgumentException> when a child of that name already exists; `CreateStream(name)` instead returns an empty writable cursor. `CreateStorage(name)` adds a nested storage. Word ignores streams it does not recognize, so a private stream beside `WordDocument` is the conventional way to carry application metadata in a document you do not otherwise change.

The same session over a stream you own:

```csharp
using Bodu.IO.Compound;

using FileStream stream = new("report-edited.doc", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
using CompoundFile file = CompoundFile.Open(stream, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);

Console.WriteLine(file.RootStorage.TryOpenStream("Bodu.Notes", out CompoundStream? notes));
notes?.Dispose();

// True
```

The stream must be seekable (update mode rewinds it to load the snapshot) and writable (for `Commit`). `Commit` on a non-seekable destination appends the container at the current position rather than truncating.

## Pattern 2 — replace a stream's bytes

A writable file hands out a **read-write cursor** from `OpenStream(name, FileMode, FileAccess)` — a real <xref:System.IO.Stream> whose `CanWrite` is `true` — and the `FileMode` decides what the cursor starts with:

| `FileMode` | Stream exists | Stream missing |
|---|---|---|
| `Open` | Cursor seeded with the current bytes, positioned at 0. | <xref:Bodu.IO.Compound.CompoundStreamNotFoundException> (`TryOpenStream` returns `false`). |
| `OpenOrCreate` | As `Open`. | Created empty. |
| `Create` | **Truncated** to empty. | Created empty. |
| `CreateNew` | <xref:System.IOException> (`TryOpenStream` returns `false`). | Created empty. |
| `Append` | Cursor seeded with the current bytes, positioned at the end. | Created empty. |
| `Truncate` | <xref:System.NotSupportedException>. | |

With `FileAccess.Write` the cursor is write-only (`CanRead` is `false`); with `ReadWrite` you can read what you seeded. Requesting write access on a read-only file throws <xref:System.NotSupportedException>. Four ways to replace a payload, all staged until `Commit`:

```csharp
using Bodu.IO.Compound;
using System.Text;

using CompoundFile file = CompoundFile.Open("report-edited.doc", FileMode.Open, FileAccess.ReadWrite);

// (a) Truncate and rewrite: FileMode.Create resets the payload to empty.
using (CompoundStream stream = file.RootStorage.OpenStream("Bodu.Notes", FileMode.Create, FileAccess.Write))
    stream.Write(Encoding.UTF8.GetBytes("reviewed 2026-09-09; approved 2026-09-10"));

// (b) Patch in place: FileMode.Open seeds the cursor with the current bytes.
using (CompoundStream stream = file.RootStorage.OpenStream("Bodu.Notes", FileMode.Open, FileAccess.ReadWrite))
{
    stream.Seek(-10, SeekOrigin.End);
    stream.Write(Encoding.UTF8.GetBytes("2026-09-11"));
}

// (c) Append: FileMode.Append positions the cursor at the end.
using (CompoundStream stream = file.RootStorage.OpenStream("Bodu.Notes", FileMode.Append, FileAccess.Write))
    stream.Write(Encoding.UTF8.GetBytes("; archived"));

// (d) Replace wholesale from a byte buffer: delete, then create.
file.RootStorage.Delete("Bodu.Notes");
file.RootStorage.CreateStream("Bodu.Notes", Encoding.UTF8.GetBytes("reviewed 2026-09-09; approved 2026-09-11; archived"));

file.Commit();

using CompoundStream read = file.RootStorage.OpenStream("Bodu.Notes");
Console.WriteLine(Encoding.UTF8.GetString(read.ReadAllBytes()));

// reviewed 2026-09-09; approved 2026-09-11; archived
```

> [!NOTE]
> A writable cursor buffers its payload in memory and flushes it into the staging tree when it is flushed or disposed — **dispose the cursor before you `Commit`**, or the bytes you wrote are not yet in the tree. `Flush()` stores a snapshot mid-life; `Dispose()` transfers the buffer. The cursor caps its payload at `int.MaxValue` bytes; larger payloads belong to the builder's deferred sources (see [Buffered vs streaming access](streaming-and-buffering.md)). `SetLength` shrinks or grows the payload.

The non-throwing form takes the same mode and access:

```csharp
using Bodu.IO.Compound;

using CompoundFile file = CompoundFile.Open("report-edited.doc", FileMode.Open, FileAccess.ReadWrite);

if (file.RootStorage.TryOpenStream("Bodu.Notes", FileMode.Open, FileAccess.ReadWrite, out CompoundStream? notes))
{
    using (notes)
        Console.WriteLine($"{notes.Length} bytes, CanWrite={notes.CanWrite}");
}

// CreateNew over an existing name reports false rather than throwing.
bool created = file.RootStorage.TryOpenStream("Bodu.Notes", FileMode.CreateNew, FileAccess.Write, out CompoundStream? duplicate);
duplicate?.Dispose();
Console.WriteLine($"CreateNew over an existing stream: {created}");

file.Revert();

// 50 bytes, CanWrite=True
// CreateNew over an existing stream: False
```

`OpenStream(name)` and `TryOpenStream(name, out stream)` without a mode still work on a writable file: they return a read-only *snapshot* cursor over the staged bytes, so a plain read never marks the file dirty.

## Rename and delete

```csharp
using Bodu.IO.Compound;

using CompoundFile file = CompoundFile.Open("report-edited.doc", FileMode.Open, FileAccess.ReadWrite);

file.RootStorage.Rename("Bodu.Notes", "Bodu.Archive");            // in place; the payload is untouched
bool removed = file.RootStorage.Delete("\u0001CompObj");          // true when a child was removed
bool missing = file.RootStorage.Delete("NoSuchStream");           // false, not an exception

Console.WriteLine($"removed={removed} missing={missing} dirty={file.IsDirty}");
file.Revert();                                                    // keep the file as it was for the next pattern

// removed=True missing=False dirty=True
```

`Rename` applies to a storage or a stream, keeps its payload, metadata, and position in the tree, and throws <xref:System.Collections.Generic.KeyNotFoundException> for a missing source or <xref:System.ArgumentException> for a taken target name. `Delete` removes a stream or a whole storage subtree and reports whether anything was removed. Names follow the container's rules — at most 31 UTF-16 code units, no `/` or null characters, compared case-insensitively — and the `\x05` / `\x01` control prefixes are part of the name.

A writable storage also exposes its directory metadata as settable properties — `ClassId`, `CreationTime`, `ModifiedTime`, `StateBits` — which `Commit` writes exactly as set and never stamps for you.

## Pattern 3 — revert after a failed edit

Because nothing reaches the destination until `Commit`, a multi-step edit that fails part-way is undone by a single `Revert`:

```csharp
using Bodu.IO.Compound;

using CompoundFile file = CompoundFile.Open("report-edited.doc", FileMode.Open, FileAccess.ReadWrite);

try
{
    file.RootStorage.Delete("WordDocument");
    file.RootStorage.CreateStream("WordDocument", Transform());   // throws before the container is touched
    file.Commit();
}
catch (InvalidDataException ex)
{
    Console.WriteLine($"edit failed: {ex.Message}; IsDirty={file.IsDirty}");
    file.Revert();
    Console.WriteLine($"reverted: IsDirty={file.IsDirty}, WordDocument present={file.RootStorage.TryOpenStream("WordDocument", out CompoundStream? _)}");
}

static byte[] Transform() =>
    throw new InvalidDataException("unsupported document version");

// edit failed: unsupported document version; IsDirty=True
// reverted: IsDirty=False, WordDocument present=True
```

Disposing instead of reverting has the same effect on the destination — the staged edits are simply dropped — so `Revert` matters when you intend to keep using the session.

> [!WARNING]
> `Revert` restores the baseline captured **at open**, not the state of the last `Commit`. After a `Commit`, a `Revert` silently stages the open-time content again and reports `IsDirty == false` even though the destination now holds the committed edits; a second `Commit` would write the original content back out. If you need a "commit, then keep editing, then undo just the recent edits" flow, close and reopen the file after committing.

```csharp
using Bodu.IO.Compound;

using CompoundFile file = CompoundFile.Open("report-edited.doc", FileMode.Open, FileAccess.ReadWrite);

file.RootStorage.CreateStream("Bodu.Second", new byte[] { 1, 2, 3 });
file.Commit();

file.Revert();                                                    // back to the open-time snapshot...
Console.WriteLine($"IsDirty={file.IsDirty}, Bodu.Second staged={file.RootStorage.TryOpenStream("Bodu.Second", out CompoundStream? _)}");

file.Commit();                                                    // ...which this writes back out

// IsDirty=False, Bodu.Second staged=False
```

## What is guaranteed, and what is not

| Guarantee | Holds? |
|---|---|
| Edits are invisible to the destination until `Commit`. | Yes — the destination is not touched by any mutation, cursor, or `Revert`. |
| Disposing without `Commit` leaves the destination unchanged. | Yes. |
| A `Commit` writes every staged edit or none of them *as far as the staging tree goes*. | Yes — the layout is computed from the whole tree before the first byte is written. |
| A `Commit` that faults or is cancelled mid-write leaves the destination intact. | **No.** The destination has been truncated and partially written; the file stays dirty with the staging tree intact, so `Commit` again or `Revert`. |
| Unchanged streams keep their bytes; storages keep their `ClassId`, timestamps, and state bits. | Yes — the rewrite preserves entry metadata and payloads; only the sector layout is recomputed. |
| The output is byte-identical to the input when nothing changed. | No — a rewrite may lay sectors out differently from the original writer. |
| Concurrent use of one `CompoundFile`. | Not supported. |

Because a faulting `Commit` is not crash-safe, commit to a temporary copy and swap it into place when the original must survive a power failure or a full disk:

```csharp
using Bodu.IO.Compound;

string path = "report-edited.doc";
string temp = path + ".tmp";

using (FileStream source = File.OpenRead(path))
using (FileStream destination = File.Create(temp))
{
    source.CopyTo(destination);
    destination.Position = 0;

    using CompoundFile file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
    file.RootStorage.CreateStream("Bodu.Safe", new byte[] { 7 });
    file.Commit();
}

File.Move(temp, path, overwrite: true);   // atomic on the same volume
Console.WriteLine("replaced");
```

`CommitAsync` / `FlushAsync` write the same bytes with asynchronous I/O and observe the cancellation token before the destination is touched and again between chunks; `Flush` is an alias for `Commit` for stream-style consumers.

```csharp
using Bodu.IO.Compound;

using CompoundFile file = CompoundFile.Open("report-edited.doc", FileMode.Open, FileAccess.ReadWrite);

file.RootStorage.CreateStream("Bodu.Async", new byte[] { 4, 5, 6 });
await file.CommitAsync(cancellationToken);
Console.WriteLine($"IsDirty={file.IsDirty}");

// IsDirty=False
```

The rewrite preserves what the original writer recorded — the root storage's class identifier and timestamps, and every untouched payload byte for byte:

```csharp
using Bodu.IO.Compound;

using CompoundFile before = CompoundFile.OpenRead("report.doc");
using CompoundFile after = CompoundFile.OpenRead("report-edited.doc");

Console.WriteLine($"root class id: {before.RootStorage.ClassId} -> {after.RootStorage.ClassId}");
Console.WriteLine($"root modified: {before.RootStorage.ModifiedTime} -> {after.RootStorage.ModifiedTime}");

using CompoundStream a = before.RootStorage.OpenStream("WordDocument");
using CompoundStream b = after.RootStorage.OpenStream("WordDocument");
Console.WriteLine($"WordDocument identical: {a.ReadAllBytes().AsSpan().SequenceEqual(b.ReadAllBytes())}");

// root class id: 00020906-0000-0000-c000-000000000046 -> 00020906-0000-0000-c000-000000000046
// root modified: 03/02/2001 18:14:04 +00:00 -> 03/02/2001 18:14:04 +00:00
// WordDocument identical: True
```

## In-place editing versus the builder round-trip

The builder path in [Authoring compound files](authoring-compound-files.md) reaches the same outcome — load, mutate, write — with a different shape:

```csharp
using Bodu.IO.Compound;
using Bodu.IO.Compound.Builders;
using System.Text;

CompoundStorageBuilder root;
using (CompoundFile source = CompoundFile.OpenRead("report.doc"))
    root = CompoundStorageBuilder.FromFile(source);

root.AddStream("Bodu.Notes", Encoding.UTF8.GetBytes("reviewed"));
root.Save("report-rebuilt.doc");
Console.WriteLine(new FileInfo("report-rebuilt.doc").Length);

// 29696
```

| | In-place `CompoundFile` | Builder round-trip |
|---|---|---|
| Entry point | `Open(…, FileMode.Open, FileAccess.ReadWrite)` | `CompoundStorageBuilder.FromFile(file)` / `Load(stream)` |
| Mutation surface | The `CompoundStorage` / `CompoundStream` read surface plus `CreateStream`, `Delete`, `Rename`, writable cursors | The builder's dictionary-style `AddStream` / `AddStorage` / `Remove` / `Rename` |
| Writing bytes | Through a `Stream` cursor — seek, patch, append | Whole payloads: `ReadOnlyMemory<byte>`, a deferred opener, or a file path |
| Destination | The same file or stream, rewritten by `Commit` | Wherever `Save` / `WriteTo` / `ToArray` points |
| Memory | The whole file is loaded at open | `FromFile(file, lazy: true)` defers payloads until serialization; deferred sources never load |
| Undo | `Revert` to the open-time baseline | Discard the builder |
| Sector version | The `CompoundBuildOptions` default (V3) | Any `CompoundBuildOptions` per call |

Reach for in-place editing when you want `Stream` semantics over the bytes or a single file handle from open to commit. Reach for the builder when the output goes somewhere else, payloads are too large to buffer, or you want to choose the sector version.

## Where to go next

- [Authoring compound files](authoring-compound-files.md) — the builder and create-mode paths, options, and naming rules.
- [Authoring custom property sets](custom-property-sets.md) — `WritePropertySet` and the OLE property-set model, the commonest reason to edit a document in place.
- [Buffered vs streaming access](streaming-and-buffering.md) — the writable cursor's memory profile and the asynchronous commit.
- [Reading compound files](reading-compound-files.md) — the navigation surface the write path reuses.
- [Bodu.IO.Compound guides](index.md) — every guide in this topic.
