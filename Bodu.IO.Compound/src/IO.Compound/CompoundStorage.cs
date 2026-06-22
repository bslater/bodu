// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
/// Creation and mutation members (<c>CreateStorage</c>, <c>CreateStream</c>, <c>Delete</c>, <c>Rename</c>,
/// <c>Commit</c>, and <c>Revert</c>) are reserved for a future read-write implementation and are not yet declared.
/// </para>
/// </remarks>
public sealed class CompoundStorage
{
    /// <summary>The owning compound file used to resolve children and materialize streams.</summary>
    private readonly CompoundFile _file;

    /// <summary>The directory entry this storage wraps.</summary>
    private readonly CfbDirectoryEntry _entry;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStorage" /> class.
    /// </summary>
    /// <param name="file">The owning compound file.</param>
    /// <param name="entry">The directory entry the storage wraps.</param>
    internal CompoundStorage(CompoundFile file, CfbDirectoryEntry entry)
    {
        _file = file;
        _entry = entry;
    }

    /// <summary>
    /// Gets the name of the storage as stored in the directory.
    /// </summary>
    /// <returns>The storage name; the root storage carries the conventional name <c>Root Entry</c>.</returns>
    public string Name => _entry.Name;

    /// <summary>
    /// Gets the metadata snapshot for this storage.
    /// </summary>
    /// <returns>A <see cref="CompoundEntryInfo" /> describing the storage.</returns>
    public CompoundEntryInfo Stat => _entry.ToEntryInfo();

    /// <summary>
    /// Enumerates the metadata of every direct child of this storage, in directory order.
    /// </summary>
    /// <returns>A sequence of <see cref="CompoundEntryInfo" /> for the child storages and streams.</returns>
    public IEnumerable<CompoundEntryInfo> EnumerateEntries()
    {
        foreach (CfbDirectoryEntry child in Children())
            yield return child.ToEntryInfo();
    }

    /// <summary>
    /// Enumerates the direct child storages of this storage, in directory order.
    /// </summary>
    /// <returns>A sequence of child <see cref="CompoundStorage" /> objects.</returns>
    public IEnumerable<CompoundStorage> EnumerateStorages()
    {
        foreach (CfbDirectoryEntry child in Children())
        {
            if (child.Type is CompoundEntryType.Storage or CompoundEntryType.RootStorage)
                yield return new CompoundStorage(_file, child);
        }
    }

    /// <summary>
    /// Enumerates the metadata of the direct child streams of this storage, in directory order.
    /// </summary>
    /// <returns>A sequence of <see cref="CompoundEntryInfo" /> for the child streams.</returns>
    public IEnumerable<CompoundEntryInfo> EnumerateStreams()
    {
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
    /// <param name="mode">The file mode; the current release supports <see cref="FileMode.Open" /> only.</param>
    /// <param name="access">The access level; the current release supports <see cref="FileAccess.Read" /> only.</param>
    /// <returns>
    /// A <see cref="CompoundStream" /> positioned at the start of the payload; dispose it when finished.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="mode" /> or <paramref name="access" /> requests a write capability, which is not yet
    /// supported.
    /// </exception>
    /// <exception cref="CompoundStreamNotFoundException">
    /// Thrown when no child stream with the given name exists.
    /// </exception>
    public CompoundStream OpenStream(string name, FileMode mode, FileAccess access)
    {
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

        CfbDirectoryEntry? entry = FindChild(name, CompoundEntryType.Storage);
        if (entry is not null)
        {
            storage = new CompoundStorage(_file, entry);
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

        CfbDirectoryEntry? entry = FindChild(name, CompoundEntryType.Stream);
        if (entry is not null)
        {
            stream = _file.OpenStream(entry);
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
    /// <param name="mode">The file mode; the current release supports <see cref="FileMode.Open" /> only.</param>
    /// <param name="access">The access level; the current release supports <see cref="FileAccess.Read" /> only.</param>
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
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="mode" /> or <paramref name="access" /> requests a write capability, which is not yet
    /// supported.
    /// </exception>
    public bool TryOpenStream(string name, FileMode mode, FileAccess access, [MaybeNullWhen(false)] out CompoundStream stream)
    {
        RequireReadOnly(mode, access);

        return TryOpenStream(name, out stream);
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
    /// Enumerates the resolved directory entries of this storage's direct children.
    /// </summary>
    /// <returns>A sequence of child directory entries, in directory order.</returns>
    private IEnumerable<CfbDirectoryEntry> Children()
    {
        foreach (int sid in _entry.Children)
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
