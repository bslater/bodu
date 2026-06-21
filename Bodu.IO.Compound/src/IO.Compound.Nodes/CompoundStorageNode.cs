// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.IO.Compound.Nodes;

/// <summary>
/// Represents a storage entry in a mutable compound-file object model — a named container of child storages and
/// streams.
/// </summary>
/// <remarks>
/// <para>
/// This is the authoring counterpart of <see cref="CompoundStorage" /> and the compound-file analogue of a
/// <c>JsonObject</c>: children are keyed by name and a node belongs to at most one storage at a time. A storage with no
/// parent is the root of a document and is the only node that can be serialized.
/// </para>
/// <para>
/// Names are compared per <see cref="CompoundNodeOptions" /> (case-insensitive by default, matching the compound-file
/// format). The serialization order of children is determined by the writer, not by insertion order.
/// </para>
/// </remarks>
public sealed partial class CompoundStorageNode
    : CompoundNode, IDictionary<string, CompoundNode>
{
    /// <summary>The conventional name of the root storage entry.</summary>
    private const string RootEntryName = "Root Entry";

    /// <summary>The options controlling name comparison for this storage and its descendants.</summary>
    private readonly CompoundNodeOptions _options;

    /// <summary>The child entries keyed by name.</summary>
    private readonly Dictionary<string, CompoundNode> _children;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStorageNode" /> class with default options.
    /// </summary>
    public CompoundStorageNode()
        : this(default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStorageNode" /> class with the specified options.
    /// </summary>
    /// <param name="options">The options controlling name comparison.</param>
    public CompoundStorageNode(CompoundNodeOptions options)
    {
        _options = options;
        _children = new Dictionary<string, CompoundNode>(options.NameComparer);
    }

    /// <inheritdoc />
    public override CompoundEntryType EntryType =>
        Parent is null ? CompoundEntryType.RootStorage : CompoundEntryType.Storage;

    /// <inheritdoc />
    public ICollection<string> Keys => _children.Keys;

    /// <inheritdoc />
    public ICollection<CompoundNode> Values => _children.Values;

    /// <inheritdoc />
    public int Count => _children.Count;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, CompoundNode>>.IsReadOnly => false;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when the key or value is <see langword="null" />.</exception>
    /// <exception cref="CompoundFileSerializationException">Thrown when the name is invalid.</exception>
    public CompoundNode this[string key]
    {
        get => _children[key];
        set
        {
            ThrowHelper.ThrowIfNull(value);
            ValidateName(key);

            if (_children.TryGetValue(key, out CompoundNode? existing))
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
    /// <returns>A root <see cref="CompoundStorageNode" /> ready for authoring.</returns>
    public static CompoundStorageNode CreateRoot() =>
        new() { Name = RootEntryName };

    /// <summary>
    /// Creates a new, detached root storage node with the specified options.
    /// </summary>
    /// <param name="options">The options controlling name comparison.</param>
    /// <returns>A root <see cref="CompoundStorageNode" /> ready for authoring.</returns>
    public static CompoundStorageNode CreateRoot(CompoundNodeOptions options) =>
        new(options) { Name = RootEntryName };

    /// <summary>
    /// Adds a new child storage with the specified name.
    /// </summary>
    /// <param name="name">The storage name.</param>
    /// <returns>The created <see cref="CompoundStorageNode" />.</returns>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public CompoundStorageNode AddStorage(string name)
    {
        var storage = new CompoundStorageNode(_options);
        AddCore(name, storage);
        return storage;
    }

    /// <summary>
    /// Adds a new child stream with the specified name and payload.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="content">The payload bytes.</param>
    /// <returns>The created <see cref="CompoundStreamNode" />.</returns>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public CompoundStreamNode AddStream(string name, ReadOnlyMemory<byte> content)
    {
        CompoundStreamNode stream = CompoundStreamNode.Create(name, content);
        AddCore(name, stream);
        return stream;
    }

    /// <summary>
    /// Adds a new child stream with the specified name and payload.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="content">The payload bytes, copied into the node.</param>
    /// <returns>The created <see cref="CompoundStreamNode" />.</returns>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public CompoundStreamNode AddStream(string name, ReadOnlySpan<byte> content) =>
        AddStream(name, (ReadOnlyMemory<byte>)content.ToArray());

    /// <inheritdoc />
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is invalid or already present.
    /// </exception>
    public void Add(string key, CompoundNode value)
    {
        ThrowHelper.ThrowIfNull(value);

        AddCore(key, value);
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, CompoundNode>>.Add(KeyValuePair<string, CompoundNode> item) =>
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
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out CompoundNode value) =>
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
    public bool TryGetStorage(string name, [MaybeNullWhen(false)] out CompoundStorageNode storage)
    {
        if (_children.TryGetValue(name, out CompoundNode? node) && node is CompoundStorageNode found)
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
    public bool TryGetStream(string name, [MaybeNullWhen(false)] out CompoundStreamNode stream)
    {
        if (_children.TryGetValue(name, out CompoundNode? node) && node is CompoundStreamNode found)
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
    public IEnumerable<CompoundStorageNode> EnumerateStorages() =>
        _children.Values.OfType<CompoundStorageNode>();

    /// <summary>
    /// Enumerates the direct child streams of this storage.
    /// </summary>
    /// <returns>The child streams.</returns>
    public IEnumerable<CompoundStreamNode> EnumerateStreams() =>
        _children.Values.OfType<CompoundStreamNode>();

    /// <inheritdoc />
    public bool Remove(string key)
    {
        if (_children.TryGetValue(key, out CompoundNode? node))
        {
            node.Parent = null;
            return _children.Remove(key);
        }

        return false;
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, CompoundNode>>.Remove(KeyValuePair<string, CompoundNode> item) =>
        ((ICollection<KeyValuePair<string, CompoundNode>>)_children).Contains(item) && Remove(item.Key);

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
        if (!_children.TryGetValue(oldName, out CompoundNode? node))
        {
            throw new CompoundFileSerializationException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.IO_KeyNotFound_CompoundStream, oldName));
        }

        if (_children.ContainsKey(newName) && !_options.NameComparer.Equals(oldName, newName))
        {
            throw new CompoundFileSerializationException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_Invalid_CompoundWriterDuplicateName, newName));
        }

        _ = _children.Remove(oldName);
        node.Name = newName;
        _children[newName] = node;
    }

    /// <inheritdoc />
    public void Clear()
    {
        foreach (CompoundNode node in _children.Values)
            node.Parent = null;

        _children.Clear();
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, CompoundNode>>.Contains(KeyValuePair<string, CompoundNode> item) =>
        ((ICollection<KeyValuePair<string, CompoundNode>>)_children).Contains(item);

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, CompoundNode>>.CopyTo(KeyValuePair<string, CompoundNode>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, CompoundNode>>)_children).CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, CompoundNode>> GetEnumerator() =>
        _children.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        _children.GetEnumerator();

    /// <inheritdoc />
    public override CompoundNode DeepClone()
    {
        var clone = new CompoundStorageNode(_options)
        {
            Name = Name,
            ClassId = ClassId,
            CreationTime = CreationTime,
            ModifiedTime = ModifiedTime,
            StateBits = StateBits,
        };

        foreach (KeyValuePair<string, CompoundNode> child in _children)
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
    private void AddCore(string name, CompoundNode node)
    {
        ValidateName(name);
        if (_children.ContainsKey(name))
        {
            throw new CompoundFileSerializationException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Op_Invalid_CompoundWriterDuplicateName, name));
        }

        node.AssignParent(this);
        node.Name = name;
        _children[name] = node;
    }
}
