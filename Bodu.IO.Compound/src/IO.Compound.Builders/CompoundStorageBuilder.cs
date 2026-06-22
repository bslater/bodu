// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.IO.Compound.Builders;

/// <summary>
/// Represents a storage entry in a mutable compound-file object model — a named container of child storages and
/// streams.
/// </summary>
/// <remarks>
/// <para>
/// This is the authoring counterpart of <see cref="CompoundStorage" /> and the compound-file analogue of a
/// <c>JsonObject</c>: children are keyed by name and a node belongs to at most one storage at a time. A storage with no
/// parent is the root of a document; serialize a tree with its <see cref="ToArray(CompoundBuildOptions)" /> /
/// <see cref="WriteTo(Stream, CompoundBuildOptions)" /> members.
/// </para>
/// <para>
/// Names are compared per <see cref="CompoundStorageBuilderOptions" /> (case-insensitive by default, matching the
/// compound-file format). The serialization order of children is determined by the builder, not by insertion order.
/// </para>
/// </remarks>
public sealed partial class CompoundStorageBuilder
    : CompoundEntryBuilder, IDictionary<string, CompoundEntryBuilder>
{
    /// <summary>The conventional name of the root storage entry.</summary>
    private const string RootEntryName = "Root Entry";

    /// <summary>The options controlling name comparison for this storage and its descendants.</summary>
    private readonly CompoundStorageBuilderOptions _options;

    /// <summary>The child entries keyed by name.</summary>
    private readonly Dictionary<string, CompoundEntryBuilder> _children;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStorageBuilder" /> class with default options.
    /// </summary>
    public CompoundStorageBuilder()
        : this(default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStorageBuilder" /> class with the specified options.
    /// </summary>
    /// <param name="options">The options controlling name comparison.</param>
    public CompoundStorageBuilder(CompoundStorageBuilderOptions options)
    {
        _options = options;
        _children = new Dictionary<string, CompoundEntryBuilder>(options.NameComparer);
    }

    /// <inheritdoc />
    public override CompoundEntryType EntryType =>
        Parent is null ? CompoundEntryType.RootStorage : CompoundEntryType.Storage;

    /// <inheritdoc />
    public ICollection<string> Keys => _children.Keys;

    /// <inheritdoc />
    public ICollection<CompoundEntryBuilder> Values => _children.Values;

    /// <inheritdoc />
    public int Count => _children.Count;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, CompoundEntryBuilder>>.IsReadOnly => false;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when the key or value is <see langword="null" />.</exception>
    /// <exception cref="CompoundFileSerializationException">Thrown when the name is invalid.</exception>
    public CompoundEntryBuilder this[string key]
    {
        get => _children[key];
        set
        {
            ThrowHelper.ThrowIfNull(value);
            ValidateName(key);

            if (_children.TryGetValue(key, out CompoundEntryBuilder? existing))
            {
                if (ReferenceEquals(existing, value))
                    return;

                existing.Parent = null;
            }

            value.AssignParent(this);
            value.Name = key;
            _children[key] = value;
        }
    }

    /// <summary>
    /// Creates a new, detached root storage node named <c>Root Entry</c>.
    /// </summary>
    /// <returns>A root <see cref="CompoundStorageBuilder" /> ready for authoring.</returns>
    public static CompoundStorageBuilder CreateRoot() =>
        new() { Name = RootEntryName };

    /// <summary>
    /// Creates a new, detached root storage node with the specified options.
    /// </summary>
    /// <param name="options">The options controlling name comparison.</param>
    /// <returns>A root <see cref="CompoundStorageBuilder" /> ready for authoring.</returns>
    public static CompoundStorageBuilder CreateRoot(CompoundStorageBuilderOptions options) =>
        new(options) { Name = RootEntryName };

    /// <summary>
    /// Adds a new child storage with the specified name.
    /// </summary>
    /// <param name="name">The storage name.</param>
    /// <returns>The created <see cref="CompoundStorageBuilder" />.</returns>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public CompoundStorageBuilder AddStorage(string name)
    {
        var storage = new CompoundStorageBuilder(_options);
        AddCore(name, storage);
        return storage;
    }

    /// <summary>
    /// Adds a new child stream with the specified name and payload.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="content">The payload bytes.</param>
    /// <returns>The created <see cref="CompoundStreamBuilder" />.</returns>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public CompoundStreamBuilder AddStream(string name, ReadOnlyMemory<byte> content)
    {
        CompoundStreamBuilder stream = CompoundStreamBuilder.Create(name, content);
        AddCore(name, stream);
        return stream;
    }

    /// <summary>
    /// Adds a new deferred child stream whose payload is read on demand from a re-openable source.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="openRead">A factory that opens a readable stream over the payload each time it is invoked.</param>
    /// <param name="length">The payload length, in bytes.</param>
    /// <returns>The created <see cref="CompoundStreamBuilder" />.</returns>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public CompoundStreamBuilder AddStream(string name, Func<Stream> openRead, long length)
    {
        CompoundStreamBuilder stream = CompoundStreamBuilder.Create(name, openRead, length);
        AddCore(name, stream);
        return stream;
    }

    /// <summary>
    /// Adds a new deferred child stream whose payload is read on demand from a file.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="path">The path of the file providing the payload.</param>
    /// <returns>The created <see cref="CompoundStreamBuilder" />.</returns>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public CompoundStreamBuilder AddStreamFromFile(string name, string path)
    {
        CompoundStreamBuilder stream = CompoundStreamBuilder.CreateFromFile(name, path);
        AddCore(name, stream);
        return stream;
    }

    /// <inheritdoc />
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public void Add(string key, CompoundEntryBuilder value)
    {
        ThrowHelper.ThrowIfNull(value);

        AddCore(key, value);
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, CompoundEntryBuilder>>.Add(KeyValuePair<string, CompoundEntryBuilder> item) =>
        Add(item.Key, item.Value);

    /// <inheritdoc />
    public bool ContainsKey(string key) =>
        _children.ContainsKey(key);

    /// <summary>
    /// Determines whether the storage contains a child with the specified name.
    /// </summary>
    /// <param name="name">The name to look up.</param>
    /// <returns>
    /// <see langword="true" /> when a child with the name exists; otherwise <see langword="false" />.
    /// </returns>
    public bool ContainsName(string name) =>
        _children.ContainsKey(name);

    /// <inheritdoc />
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out CompoundEntryBuilder value) =>
        _children.TryGetValue(key, out value);

    /// <summary>
    /// Attempts to get the child storage with the specified name.
    /// </summary>
    /// <param name="name">The storage name.</param>
    /// <param name="storage">
    /// When this method returns <see langword="true" />, the matching storage; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a child storage with the name exists; otherwise <see langword="false" />.
    /// </returns>
    public bool TryGetStorage(string name, [MaybeNullWhen(false)] out CompoundStorageBuilder storage)
    {
        if (_children.TryGetValue(name, out CompoundEntryBuilder? node) && node is CompoundStorageBuilder found)
        {
            storage = found;
            return true;
        }

        storage = null;
        return false;
    }

    /// <summary>
    /// Attempts to get the child stream with the specified name.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="stream">
    /// When this method returns <see langword="true" />, the matching stream; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a child stream with the name exists; otherwise <see langword="false" />.
    /// </returns>
    public bool TryGetStream(string name, [MaybeNullWhen(false)] out CompoundStreamBuilder stream)
    {
        if (_children.TryGetValue(name, out CompoundEntryBuilder? node) && node is CompoundStreamBuilder found)
        {
            stream = found;
            return true;
        }

        stream = null;
        return false;
    }

    /// <summary>
    /// Enumerates the direct child storages of this storage.
    /// </summary>
    /// <returns>The child storages.</returns>
    public IEnumerable<CompoundStorageBuilder> EnumerateStorages() =>
        _children.Values.OfType<CompoundStorageBuilder>();

    /// <summary>
    /// Enumerates the direct child streams of this storage.
    /// </summary>
    /// <returns>The child streams.</returns>
    public IEnumerable<CompoundStreamBuilder> EnumerateStreams() =>
        _children.Values.OfType<CompoundStreamBuilder>();

    /// <inheritdoc />
    public bool Remove(string key)
    {
        if (_children.TryGetValue(key, out CompoundEntryBuilder? node))
        {
            node.Parent = null;
            return _children.Remove(key);
        }

        return false;
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, CompoundEntryBuilder>>.Remove(KeyValuePair<string, CompoundEntryBuilder> item) =>
        ((ICollection<KeyValuePair<string, CompoundEntryBuilder>>)_children).Contains(item) && Remove(item.Key);

    /// <summary>
    /// Renames a child entry.
    /// </summary>
    /// <param name="oldName">The current name of the child.</param>
    /// <param name="newName">The new name.</param>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when <paramref name="newName" /> is invalid or already present, or when no child named
    /// <paramref name="oldName" /> exists.
    /// </exception>
    public void Rename(string oldName, string newName)
    {
        ValidateName(newName);
        if (!_children.TryGetValue(oldName, out CompoundEntryBuilder? node))
        {
            throw new CompoundFileSerializationException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.IO_KeyNotFound_CompoundStream, oldName));
        }

        if (_children.ContainsKey(newName) && !_options.NameComparer.Equals(oldName, newName))
        {
            throw new CompoundFileSerializationException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_Invalid_CompoundEntryBuilderDuplicateName, newName));
        }

        _ = _children.Remove(oldName);
        node.Name = newName;
        _children[newName] = node;
    }

    /// <inheritdoc />
    public void Clear()
    {
        foreach (CompoundEntryBuilder node in _children.Values)
            node.Parent = null;

        _children.Clear();
    }

    /// <summary>
    /// Replaces this storage's metadata and children with deep copies of another storage tree's, in place.
    /// </summary>
    /// <param name="source">The storage tree whose contents replace this storage's.</param>
    /// <remarks>
    /// Preserves this storage's object identity (so existing references remain valid) while restoring it to a snapshot
    /// of <paramref name="source" />. Used by <see cref="CompoundFile.Revert" /> to discard staged update-mode edits.
    /// </remarks>
    internal void ResetTo(CompoundStorageBuilder source)
    {
        Clear();
        ClassId = source.ClassId;
        CreationTime = source.CreationTime;
        ModifiedTime = source.ModifiedTime;
        StateBits = source.StateBits;

        var snapshot = (CompoundStorageBuilder)source.DeepClone();
        foreach (CompoundEntryBuilder child in snapshot._children.Values.ToList())
        {
            child.Parent = null;
            AddCore(child.Name, child);
        }
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, CompoundEntryBuilder>>.Contains(KeyValuePair<string, CompoundEntryBuilder> item) =>
        ((ICollection<KeyValuePair<string, CompoundEntryBuilder>>)_children).Contains(item);

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, CompoundEntryBuilder>>.CopyTo(KeyValuePair<string, CompoundEntryBuilder>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, CompoundEntryBuilder>>)_children).CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, CompoundEntryBuilder>> GetEnumerator() =>
        _children.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        _children.GetEnumerator();

    /// <inheritdoc />
    public override CompoundEntryBuilder DeepClone()
    {
        var clone = new CompoundStorageBuilder(_options)
        {
            Name = Name,
            ClassId = ClassId,
            CreationTime = CreationTime,
            ModifiedTime = ModifiedTime,
            StateBits = StateBits,
        };

        foreach (KeyValuePair<string, CompoundEntryBuilder> child in _children)
            clone.AddCore(child.Key, child.Value.DeepClone());

        return clone;
    }

    /// <summary>
    /// Adds a node under the specified name, validating the name and enforcing uniqueness and the single-parent rule.
    /// </summary>
    /// <param name="name">The child name.</param>
    /// <param name="node">The node to add.</param>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    private void AddCore(string name, CompoundEntryBuilder node)
    {
        ValidateName(name);
        if (_children.ContainsKey(name))
        {
            throw new CompoundFileSerializationException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_Invalid_CompoundEntryBuilderDuplicateName, name));
        }

        node.AssignParent(this);
        node.Name = name;
        _children[name] = node;
    }
}
