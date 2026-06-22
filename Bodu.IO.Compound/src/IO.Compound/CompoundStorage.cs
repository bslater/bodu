// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.Internal;
using Bodu.IO.Compound.PropertySets;

namespace Bodu.IO.Compound;

/// <summary>
/// Represents a storage within a compound file — a named container of child storages and streams — and provides
/// navigation over its immediate children.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/io-compound-structure.svg" alt="A CompoundStorage is a named container of child storages and streams within a compound file, the managed counterpart of the COM IStorage interface. Navigation starts at RootStorage and descends through nested CompoundStorage containers to CompoundStream leaves. Lookups are scoped to a storage's direct children and matched case-insensitively, as the compound-file format defines."/>
/// </para>
/// <para>
/// This type is the managed counterpart of the COM <c>IStorage</c> interface. The root storage and every nested storage
/// are represented by the same type; the root is distinguished by an <see cref="CompoundEntryType.RootStorage" /> value
/// on its <see cref="Stat" />. All lookups are scoped to a storage's direct children and compared case-insensitively
/// using the compound-file name relationship, so streams that share a name under different storages remain distinct.
/// </para>
/// <para>
/// When the owning file is opened for writing (see
/// <see cref="CompoundFile.Create(System.IO.Stream, bool, Bodu.IO.Compound.Builders.CompoundBuildOptions)" />), the
/// mutation members (<see cref="CreateStorage(string)" />, <see cref="CreateStream(string)" />,
/// <see cref="Delete(string)" />, and <see cref="Rename(string, string)" />) stage edits in memory; the destination is
/// written only when <see cref="CompoundFile.Commit" /> is called. On a read-only storage those members throw
/// <see cref="InvalidOperationException" />.
/// </para>
/// </remarks>
public sealed class CompoundStorage
{
    /// <summary>The owning compound file used to resolve children and materialize streams.</summary>
    private readonly CompoundFile _file;

    /// <summary>The directory entry this storage wraps for a read-only file, or <see langword="null" /> when writable.</summary>
    private readonly CfbDirectoryEntry? _entry;

    /// <summary>The staging node this storage wraps for a writable file, or <see langword="null" /> when read-only.</summary>
    private readonly CompoundStorageBuilder? _node;

    /// <summary>The parent storage, or <see langword="null" /> when this is the root storage.</summary>
    private readonly CompoundStorage? _parent;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStorage" /> class over a parsed directory entry.
    /// </summary>
    /// <param name="file">The owning compound file.</param>
    /// <param name="entry">The directory entry the storage wraps.</param>
    /// <param name="parent">The parent storage, or <see langword="null" /> for the root storage.</param>
    internal CompoundStorage(CompoundFile file, CfbDirectoryEntry entry, CompoundStorage? parent)
    {
        _file = file;
        _entry = entry;
        _parent = parent;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStorage" /> class over a writable staging node.
    /// </summary>
    /// <param name="file">The owning writable compound file.</param>
    /// <param name="node">The staging storage node the storage wraps.</param>
    /// <param name="parent">The parent storage, or <see langword="null" /> for the root storage.</param>
    internal CompoundStorage(CompoundFile file, CompoundStorageBuilder node, CompoundStorage? parent)
    {
        _file = file;
        _node = node;
        _parent = parent;
    }

    /// <summary>
    /// Gets a value indicating whether this storage stages edits for a writable compound file.
    /// </summary>
    /// <returns><see langword="true" /> when the owning file is writable; otherwise <see langword="false" />.</returns>
    public bool CanWrite => _node is not null;

    /// <summary>
    /// Gets the parent storage that contains this storage.
    /// </summary>
    /// <returns>
    /// The parent <see cref="CompoundStorage" />, or <see langword="null" /> when this is the root storage.
    /// </returns>
    public CompoundStorage? Parent => _parent;

    /// <summary>
    /// Gets the name of the storage as stored in the directory.
    /// </summary>
    /// <returns>The storage name; the root storage carries the conventional name <c>Root Entry</c>.</returns>
    public string Name => _node?.Name ?? _entry!.Name;

    /// <summary>
    /// Gets the metadata snapshot for this storage.
    /// </summary>
    /// <returns>A <see cref="CompoundEntryInfo" /> describing the storage.</returns>
    public CompoundEntryInfo Stat => _node is not null ? ToEntryInfo(_node) : _entry!.ToEntryInfo();

    /// <summary>
    /// Enumerates the metadata of every direct child of this storage, in directory order.
    /// </summary>
    /// <returns>A sequence of <see cref="CompoundEntryInfo" /> for the child storages and streams.</returns>
    public IEnumerable<CompoundEntryInfo> EnumerateEntries()
    {
        if (_node is not null)
        {
            foreach (CompoundEntryBuilder child in _node.Values)
                yield return ToEntryInfo(child);

            yield break;
        }

        foreach (CfbDirectoryEntry child in Children())
            yield return child.ToEntryInfo();
    }

    /// <summary>
    /// Enumerates the direct child storages of this storage, in directory order.
    /// </summary>
    /// <returns>A sequence of child <see cref="CompoundStorage" /> objects.</returns>
    public IEnumerable<CompoundStorage> EnumerateStorages()
    {
        if (_node is not null)
        {
            foreach (CompoundStorageBuilder child in _node.EnumerateStorages())
                yield return new CompoundStorage(_file, child, this);

            yield break;
        }

        foreach (CfbDirectoryEntry child in Children())
        {
            if (child.Type is CompoundEntryType.Storage or CompoundEntryType.RootStorage)
                yield return new CompoundStorage(_file, child, this);
        }
    }

    /// <summary>
    /// Enumerates the metadata of the direct child streams of this storage, in directory order.
    /// </summary>
    /// <returns>A sequence of <see cref="CompoundEntryInfo" /> for the child streams.</returns>
    public IEnumerable<CompoundEntryInfo> EnumerateStreams()
    {
        if (_node is not null)
        {
            foreach (CompoundStreamBuilder child in _node.EnumerateStreams())
                yield return ToEntryInfo(child);

            yield break;
        }

        foreach (CfbDirectoryEntry child in Children())
        {
            if (child.Type == CompoundEntryType.Stream)
                yield return child.ToEntryInfo();
        }
    }

    /// <summary>
    /// Opens the child storage with the specified name.
    /// </summary>
    /// <param name="name">The storage name, compared using the case-insensitive compound-file relationship.</param>
    /// <returns>The matching child <see cref="CompoundStorage" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundStreamNotFoundException">
    /// Thrown when no child storage with the given name exists.
    /// </exception>
    public CompoundStorage OpenStorage(string name) =>
        TryOpenStorage(name, out CompoundStorage? storage)
            ? storage
            : throw CompoundStreamNotFoundException.ForName(name);

    /// <summary>
    /// Opens a read-only cursor over the child stream with the specified name.
    /// </summary>
    /// <param name="name">The stream name, compared using the case-insensitive compound-file relationship.</param>
    /// <returns>
    /// A <see cref="CompoundStream" /> positioned at the start of the payload; dispose it when finished.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundStreamNotFoundException">
    /// Thrown when no child stream with the given name exists.
    /// </exception>
    public CompoundStream OpenStream(string name) =>
        TryOpenStream(name, out CompoundStream? stream)
            ? stream
            : throw CompoundStreamNotFoundException.ForName(name);

    /// <summary>
    /// Opens the child stream with the specified name using BCL-style <see cref="FileMode" /> and
    /// <see cref="FileAccess" /> semantics, mirroring <c>System.IO.Packaging.PackagePart.GetStream</c>.
    /// </summary>
    /// <param name="name">The stream name, compared using the case-insensitive compound-file relationship.</param>
    /// <param name="mode">The file mode applied to the named stream.</param>
    /// <param name="access">The access level; write access requires a writable compound file.</param>
    /// <returns>
    /// A <see cref="CompoundStream" /> positioned per <paramref name="mode" />; dispose it when finished.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="access" /> requests write access on a read-only compound file.
    /// </exception>
    /// <exception cref="CompoundStreamNotFoundException">
    /// Thrown when no child stream with the given name exists and <paramref name="mode" /> does not create one.
    /// </exception>
    public CompoundStream OpenStream(string name, FileMode mode, FileAccess access)
    {
        ThrowHelper.ThrowIfNull(name);

        if (_node is not null)
            return OpenStagedStream(name, mode, access);

        RequireReadOnly(mode, access);
        return OpenStream(name);
    }

    /// <summary>
    /// Attempts to open the child storage with the specified name.
    /// </summary>
    /// <param name="name">The storage name, compared using the case-insensitive compound-file relationship.</param>
    /// <param name="storage">
    /// When this method returns <see langword="true" />, the matching child storage; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a matching child storage exists; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public bool TryOpenStorage(string name, [MaybeNullWhen(false)] out CompoundStorage storage)
    {
        ThrowHelper.ThrowIfNull(name);

        if (_node is not null)
        {
            if (_node.TryGetStorage(name, out CompoundStorageBuilder? child))
            {
                storage = new CompoundStorage(_file, child, this);
                return true;
            }

            storage = null;
            return false;
        }

        CfbDirectoryEntry? entry = FindChild(name, CompoundEntryType.Storage);
        if (entry is not null)
        {
            storage = new CompoundStorage(_file, entry, this);
            return true;
        }

        storage = null;
        return false;
    }

    /// <summary>
    /// Attempts to open a read-only cursor over the child stream with the specified name.
    /// </summary>
    /// <param name="name">The stream name, compared using the case-insensitive compound-file relationship.</param>
    /// <param name="stream">
    /// When this method returns <see langword="true" />, a <see cref="CompoundStream" /> over the matching child
    /// stream; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a matching child stream exists; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public bool TryOpenStream(string name, [MaybeNullWhen(false)] out CompoundStream stream)
    {
        ThrowHelper.ThrowIfNull(name);

        if (_node is not null)
        {
            if (_node.TryGetStream(name, out CompoundStreamBuilder? child))
            {
                stream = new CompoundStream(ToEntryInfo(child), child.Content.ToArray(), this);
                return true;
            }

            stream = null;
            return false;
        }

        CfbDirectoryEntry? entry = FindChild(name, CompoundEntryType.Stream);
        if (entry is not null)
        {
            stream = _file.OpenStream(entry, this);
            return true;
        }

        stream = null;
        return false;
    }

    /// <summary>
    /// Attempts to open the child stream with the specified name using BCL-style <see cref="FileMode" /> and
    /// <see cref="FileAccess" /> semantics.
    /// </summary>
    /// <param name="name">The stream name, compared using the case-insensitive compound-file relationship.</param>
    /// <param name="mode">The file mode applied to the named stream.</param>
    /// <param name="access">The access level; write access requires a writable compound file.</param>
    /// <param name="stream">
    /// When this method returns <see langword="true" />, a <see cref="CompoundStream" /> over the matching child
    /// stream; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a matching child stream exists or is created; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="access" /> requests write access on a read-only compound file.
    /// </exception>
    public bool TryOpenStream(string name, FileMode mode, FileAccess access, [MaybeNullWhen(false)] out CompoundStream stream)
    {
        ThrowHelper.ThrowIfNull(name);

        if (_node is not null)
            return TryOpenStagedStream(name, mode, access, out stream);

        RequireReadOnly(mode, access);
        return TryOpenStream(name, out stream);
    }

    /// <summary>
    /// Creates a new child storage with the specified name and stages it for the next commit.
    /// </summary>
    /// <param name="name">The name of the storage to create.</param>
    /// <returns>The newly created child <see cref="CompoundStorage" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the compound file is read-only.</exception>
    /// <exception cref="ArgumentException">Thrown when a child with the same name already exists.</exception>
    public CompoundStorage CreateStorage(string name)
    {
        ThrowHelper.ThrowIfNull(name);
        RequireWritable();

        CompoundStorageBuilder child = _node!.AddStorage(name);
        _file.MarkDirty();
        return new CompoundStorage(_file, child, this);
    }

    /// <summary>
    /// Creates a new, empty child stream with the specified name and returns a writable cursor over it.
    /// </summary>
    /// <param name="name">The name of the stream to create.</param>
    /// <returns>A writable <see cref="CompoundStream" /> positioned at the start; dispose it when finished.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the compound file is read-only.</exception>
    public CompoundStream CreateStream(string name)
    {
        ThrowHelper.ThrowIfNull(name);
        RequireWritable();

        return OpenStream(name, FileMode.Create, FileAccess.ReadWrite);
    }

    /// <summary>
    /// Creates a new child stream with the specified name and content and stages it for the next commit.
    /// </summary>
    /// <param name="name">The name of the stream to create.</param>
    /// <param name="content">The payload to store in the stream.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the compound file is read-only.</exception>
    /// <exception cref="ArgumentException">Thrown when a child with the same name already exists.</exception>
    public void CreateStream(string name, ReadOnlyMemory<byte> content)
    {
        ThrowHelper.ThrowIfNull(name);
        RequireWritable();

        _node!.AddStream(name, content);
        _file.MarkDirty();
    }

    /// <summary>
    /// Removes the child storage or stream with the specified name from the staging tree.
    /// </summary>
    /// <param name="name">The name of the child to remove.</param>
    /// <returns>
    /// <see langword="true" /> when a matching child was removed; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the compound file is read-only.</exception>
    public bool Delete(string name)
    {
        ThrowHelper.ThrowIfNull(name);
        RequireWritable();

        bool removed = _node!.Remove(name);
        if (removed)
            _file.MarkDirty();

        return removed;
    }

    /// <summary>
    /// Renames the child storage or stream with the specified name.
    /// </summary>
    /// <param name="oldName">The current name of the child.</param>
    /// <param name="newName">The new name for the child.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="oldName" /> or <paramref name="newName" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the compound file is read-only.</exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no child named <paramref name="oldName" /> exists.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a child named <paramref name="newName" /> already exists.
    /// </exception>
    public void Rename(string oldName, string newName)
    {
        ThrowHelper.ThrowIfNull(oldName);
        ThrowHelper.ThrowIfNull(newName);
        RequireWritable();

        _node!.Rename(oldName, newName);
        _file.MarkDirty();
    }

    /// <summary>
    /// Attempts to open and parse the named child stream as an OLE property set.
    /// </summary>
    /// <param name="name">The property-set stream name (for example, <c>\x05SummaryInformation</c>).</param>
    /// <param name="propertySet">
    /// When this method returns <see langword="true" />, the parsed <see cref="OlePropertySet" />; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a matching child stream exists; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the stream is not a well-formed property set.
    /// </exception>
    public bool TryOpenPropertySet(string name, [MaybeNullWhen(false)] out OlePropertySet propertySet)
    {
        if (TryOpenStream(name, out CompoundStream? stream))
        {
            using (stream)
                propertySet = OlePropertySet.Parse(stream.ReadAllBytes());
            return true;
        }

        propertySet = null;
        return false;
    }

    /// <summary>
    /// Validates that the requested mode and access describe a read-only open, the only capability the current release
    /// supports.
    /// </summary>
    /// <param name="mode">The requested file mode.</param>
    /// <param name="access">The requested access level.</param>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="mode" /> is not <see cref="FileMode.Open" /> or <paramref name="access" /> is not
    /// <see cref="FileAccess.Read" />.
    /// </exception>
    private static void RequireReadOnly(FileMode mode, FileAccess access)
    {
        if (mode != FileMode.Open || access != FileAccess.Read)
        {
            throw new NotSupportedException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_NotSupported_CompoundFileWriteMode, $"{mode}/{access}"));
        }
    }

    /// <summary>
    /// Projects a staging entry node into an immutable <see cref="CompoundEntryInfo" /> snapshot.
    /// </summary>
    /// <param name="node">The staging node to project.</param>
    /// <returns>A <see cref="CompoundEntryInfo" /> describing the node.</returns>
    private static CompoundEntryInfo ToEntryInfo(CompoundEntryBuilder node) =>
        new()
        {
            Name = node.Name,
            EntryType = node.EntryType,
            Length = node is CompoundStreamBuilder stream ? stream.Length : 0L,
            ClassId = node.ClassId,
            StateBits = node.StateBits,
            CreationTime = node.CreationTime,
            LastModifiedTime = node.ModifiedTime,
        };

    /// <summary>
    /// Opens a cursor over the named staging stream node, applying the requested mode and access.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="mode">The file mode applied to the stream.</param>
    /// <param name="access">The requested access level.</param>
    /// <returns>A <see cref="CompoundStream" /> over the staged stream.</returns>
    /// <exception cref="CompoundStreamNotFoundException">
    /// Thrown when the stream does not exist and <paramref name="mode" /> does not create one.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when <paramref name="mode" /> is <see cref="FileMode.CreateNew" /> and the stream already exists.
    /// </exception>
    private CompoundStream OpenStagedStream(string name, FileMode mode, FileAccess access)
    {
        if (TryOpenStagedStream(name, mode, access, out CompoundStream? stream))
            return stream;

        // Translate the recoverable-absence cases the Try-core reports as false into the documented exceptions.
        _node!.TryGetStream(name, out CompoundStreamBuilder? existing);
        if (mode == FileMode.CreateNew && existing is not null)
        {
            throw new IOException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_NotSupported_CompoundFileWriteMode, $"{mode}/{access}"));
        }

        throw CompoundStreamNotFoundException.ForName(name);
    }

    /// <summary>
    /// Attempts to open a cursor over the named staging stream node, returning <see langword="false" /> for the
    /// recoverable-absence cases (open-missing, create-new-exists) instead of throwing.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="mode">The file mode applied to the stream.</param>
    /// <param name="access">The requested access level.</param>
    /// <param name="stream">
    /// When this method returns <see langword="true" />, the opened cursor; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a cursor was opened or created; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="mode" /> is not a supported file mode.
    /// </exception>
    private bool TryOpenStagedStream(string name, FileMode mode, FileAccess access, [MaybeNullWhen(false)] out CompoundStream stream)
    {
        stream = null;
        bool wantWrite = (access & FileAccess.Write) != 0;
        bool wantRead = (access & FileAccess.Read) != 0;
        _node!.TryGetStream(name, out CompoundStreamBuilder? existing);

        if (!wantWrite)
        {
            if (existing is null)
                return false;

            stream = new CompoundStream(ToEntryInfo(existing), existing.Content.ToArray(), this);
            return true;
        }

        CompoundStreamBuilder target;
        ReadOnlyMemory<byte> seed;
        bool append = false;
        switch (mode)
        {
            case FileMode.CreateNew when existing is not null:
                return false;

            case FileMode.CreateNew:
            case FileMode.Create:
                target = existing ?? _node.AddStream(name, ReadOnlyMemory<byte>.Empty);
                seed = ReadOnlyMemory<byte>.Empty;
                break;

            case FileMode.Open when existing is null:
                return false;

            case FileMode.Open:
                target = existing;
                seed = existing.Content;
                break;

            case FileMode.OpenOrCreate:
                target = existing ?? _node.AddStream(name, ReadOnlyMemory<byte>.Empty);
                seed = existing is null ? ReadOnlyMemory<byte>.Empty : existing.Content;
                break;

            case FileMode.Append:
                target = existing ?? _node.AddStream(name, ReadOnlyMemory<byte>.Empty);
                seed = existing is null ? ReadOnlyMemory<byte>.Empty : existing.Content;
                append = true;
                break;

            default:
                throw new NotSupportedException(
                    string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_NotSupported_CompoundFileWriteMode, $"{mode}/{access}"));
        }

        _file.MarkDirty();
        var cursor = new CompoundStream(_file, target, seed, wantRead, this);
        if (append)
            cursor.Seek(0, SeekOrigin.End);

        stream = cursor;
        return true;
    }

    /// <summary>
    /// Throws when this storage belongs to a read-only compound file.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the compound file is read-only.</exception>
    private void RequireWritable()
    {
        if (_node is null)
            throw new InvalidOperationException(CompoundResourceStrings.Op_Invalid_CompoundFileReadOnly);
    }

    /// <summary>
    /// Enumerates the resolved directory entries of this storage's direct children.
    /// </summary>
    /// <returns>A sequence of child directory entries, in directory order.</returns>
    private IEnumerable<CfbDirectoryEntry> Children()
    {
        foreach (int sid in _entry!.Children)
        {
            CfbDirectoryEntry? child = _file.GetEntry(sid);
            if (child is not null)
                yield return child;
        }
    }

    /// <summary>
    /// Finds the direct child with the specified name and type.
    /// </summary>
    /// <param name="name">The child name, compared using the case-insensitive compound-file relationship.</param>
    /// <param name="type">The required entry type.</param>
    /// <returns>The matching child entry, or <see langword="null" /> when none matches.</returns>
    private CfbDirectoryEntry? FindChild(string name, CompoundEntryType type)
    {
        foreach (CfbDirectoryEntry child in Children())
        {
            if (child.Type == type && CompoundNameComparer.Instance.Equals(child.Name, name))
                return child;
        }

        return null;
    }
}
