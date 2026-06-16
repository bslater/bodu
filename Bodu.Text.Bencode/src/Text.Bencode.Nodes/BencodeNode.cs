// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Represents a single node in a mutable Bencode (BEP 3) document object model, serving as the base for the three
/// concrete node kinds — <see cref="BencodeObject" />, <see cref="BencodeArray" />, and <see cref="BencodeValue" />.
/// </summary>
/// <remarks>
/// <para>
/// A node tree is editable in place: containers expose the standard collection surfaces, scalar values can be replaced,
/// and any node can be re-serialized to canonical Bencode through <see cref="WriteTo" /> or <see cref="ToByteArray" />.
/// Bencode has no boolean, null, or floating-point values, so the model defines only the four value kinds the format
/// supports; in particular there is no null node, so a tree that still contains a <see langword="null" /> entry cannot
/// be written.
/// </para>
/// <para>
/// Each node has at most one parent. Adding a node that already belongs to another container throws an
/// <see cref="InvalidOperationException" />.
/// </para>
/// </remarks>
public abstract class BencodeNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeNode" /> class.
    /// </summary>
    /// <remarks>
    /// The constructor is accessible only within this assembly, which closes derivation to the three concrete node
    /// kinds defined alongside it.
    /// </remarks>
    private protected BencodeNode()
    {
    }

    /// <summary>
    /// Gets the node that contains this node, or <see langword="null" /> when this node is the root of its tree.
    /// </summary>
    /// <value>The containing node, or <see langword="null" /> for a root node.</value>
    /// <returns>The parent node, or <see langword="null" /> when this node has no parent.</returns>
    public BencodeNode? Parent { get; internal set; }

    /// <summary>
    /// Gets the topmost node of the tree this node belongs to.
    /// </summary>
    /// <value>The root node reached by following <see cref="Parent" /> until it is <see langword="null" />.</value>
    /// <returns>The root node, which is this node when it has no parent.</returns>
    public BencodeNode Root
    {
        get
        {
            BencodeNode current = this;
            while (current.Parent is { } parent)
                current = parent;

            return current;
        }
    }

    /// <summary>
    /// Gets or sets the element at the specified index, treating this node as a <see cref="BencodeArray" />.
    /// </summary>
    /// <param name="index">The zero-based index of the element.</param>
    /// <value>The element at <paramref name="index" />.</value>
    /// <returns>The element at <paramref name="index" />, which may be <see langword="null" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this node is not a <see cref="BencodeArray" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> is outside the bounds of the array.
    /// </exception>
    public BencodeNode? this[int index]
    {
        get => AsArray()[index];
        set => AsArray()[index] = value;
    }

    /// <summary>
    /// Gets or sets the value associated with the specified property name, treating this node as a
    /// <see cref="BencodeObject" />.
    /// </summary>
    /// <param name="propertyName">The property name to look up.</param>
    /// <value>The value associated with <paramref name="propertyName" />.</value>
    /// <returns>
    /// The value associated with <paramref name="propertyName" />, which may be <see langword="null" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this node is not a <see cref="BencodeObject" />.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="propertyName" /> is <see langword="null" />.
    /// </exception>
    public BencodeNode? this[string propertyName]
    {
        get => AsObject()[propertyName];
        set => AsObject()[propertyName] = value;
    }

    /// <summary>
    /// Returns this node as a <see cref="BencodeObject" />.
    /// </summary>
    /// <returns>This node cast to <see cref="BencodeObject" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this node is not a <see cref="BencodeObject" />.
    /// </exception>
    public BencodeObject AsObject() =>
        this as BencodeObject
            ?? throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_NodeNotObject, GetValueKind()));

    /// <summary>
    /// Returns this node as a <see cref="BencodeArray" />.
    /// </summary>
    /// <returns>This node cast to <see cref="BencodeArray" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this node is not a <see cref="BencodeArray" />.
    /// </exception>
    public BencodeArray AsArray() =>
        this as BencodeArray
            ?? throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_NodeNotArray, GetValueKind()));

    /// <summary>
    /// Returns this node as a <see cref="BencodeValue" />.
    /// </summary>
    /// <returns>This node cast to <see cref="BencodeValue" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this node is not a <see cref="BencodeValue" />.
    /// </exception>
    public BencodeValue AsValue() =>
        this as BencodeValue
            ?? throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_NodeNotValue, GetValueKind()));

    /// <summary>
    /// Gets the kind of value this node represents.
    /// </summary>
    /// <returns>The <see cref="BencodeValueKind" /> of this node.</returns>
    public abstract BencodeValueKind GetValueKind();

    /// <summary>
    /// Returns the scalar value of this node converted to the requested type, treating this node as a
    /// <see cref="BencodeValue" />.
    /// </summary>
    /// <typeparam name="T">The type to convert the scalar value to.</typeparam>
    /// <returns>The converted scalar value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this node is not a <see cref="BencodeValue" />, or its stored value cannot be converted to
    /// <typeparamref name="T" />.
    /// </exception>
    public T GetValue<T>() =>
        AsValue().GetValue<T>();

    /// <summary>
    /// Writes the canonical Bencode encoding of this node to the supplied writer.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when the subtree rooted at this node contains a <see langword="null" /> entry, which has no Bencode
    /// representation.
    /// </exception>
    public abstract void WriteTo(Utf8BencodeWriter writer);

    /// <summary>
    /// Serializes this node to a new canonical Bencode byte array.
    /// </summary>
    /// <returns>The Bencode encoding of this node.</returns>
    /// <exception cref="BencodeSerializationException">
    /// Thrown when the subtree rooted at this node contains a <see langword="null" /> entry, which has no Bencode
    /// representation.
    /// </exception>
    public byte[] ToByteArray()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        WriteTo(writer);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Parses a single Bencode document into a node tree.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <returns>The root node of the parsed tree.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="data" /> is empty or is not valid canonical Bencode.
    /// </exception>
    /// <remarks>
    /// The underlying reader enforces the canonical grammar — a single root value, ascending unique dictionary keys,
    /// and no trailing bytes — so a successful parse round-trips byte-for-byte through <see cref="ToByteArray" />.
    /// </remarks>
    public static BencodeNode? Parse(ReadOnlySpan<byte> data) =>
        Parse(data, default);

    /// <summary>
    /// Parses a single Bencode document into a node tree, using the supplied options for every object created while
    /// parsing.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <param name="options">The node options controlling property-name case sensitivity.</param>
    /// <returns>The root node of the parsed tree.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="data" /> is empty or is not valid canonical Bencode.
    /// </exception>
    /// <remarks>
    /// Every <see cref="BencodeObject" /> materialized while parsing adopts the comparison selected by
    /// <paramref name="options" />, so a case-insensitive parse yields a tree whose dictionary lookups ignore case.
    /// </remarks>
    public static BencodeNode? Parse(ReadOnlySpan<byte> data, BencodeNodeOptions options) =>
        Parse(data, options, default);

    /// <summary>
    /// Parses a single Bencode document into a node tree, using the supplied node options for every object created
    /// while parsing and the supplied document options for the parse itself.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <param name="options">The node options controlling property-name case sensitivity.</param>
    /// <param name="documentOptions">The document options controlling depth and dictionary-key leniency.</param>
    /// <returns>The root node of the parsed tree.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="data" /> is empty or is not a single Bencode value acceptable under
    /// <paramref name="documentOptions" />.
    /// </exception>
    /// <remarks>
    /// When <see cref="Document.BencodeDocumentOptions.AllowDuplicateKeys" /> is set, repeated keys collapse into the
    /// dictionary-backed <see cref="BencodeObject" /> with the last occurrence winning.
    /// </remarks>
    public static BencodeNode? Parse(ReadOnlySpan<byte> data, BencodeNodeOptions options, Document.BencodeDocumentOptions documentOptions)
    {
        var reader = new Utf8BencodeReader(data, new BencodeReaderOptions
        {
            MaxDepth = documentOptions.MaxDepth,
            AllowUnsortedKeys = documentOptions.AllowUnsortedKeys,
            AllowDuplicateKeys = documentOptions.AllowDuplicateKeys,
        });
        if (!reader.Read())
            throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeUnexpectedEndOfData, 0);

        BencodeNode node = ReadFrom(ref reader, options);

        // The reader rejects trailing bytes on the next Read, completing single-root validation.
        reader.Read();

        return node;
    }

    /// <summary>
    /// Reads a node from the reader, which must be positioned on the first token of the value to read.
    /// </summary>
    /// <param name="reader">The reader positioned on the value's first token.</param>
    /// <returns>The node read from the reader.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the reader is positioned on an unexpected token.
    /// </exception>
    /// <remarks>
    /// On return the reader is positioned on the value's last token (the scalar token itself, or the container's end
    /// token), matching the read-positioning contract the serializer's converters observe. Objects are created with the
    /// default, case-sensitive comparison.
    /// </remarks>
    internal static BencodeNode ReadFrom(ref Utf8BencodeReader reader) =>
        ReadFrom(ref reader, default);

    /// <summary>
    /// Reads a node from the reader, which must be positioned on the first token of the value to read, materializing
    /// every object with the comparison selected by the supplied options.
    /// </summary>
    /// <param name="reader">The reader positioned on the value's first token.</param>
    /// <param name="options">The node options controlling property-name case sensitivity.</param>
    /// <returns>The node read from the reader.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the reader is positioned on an unexpected token.
    /// </exception>
    /// <remarks>
    /// On return the reader is positioned on the value's last token (the scalar token itself, or the container's end
    /// token), matching the read-positioning contract the serializer's converters observe.
    /// </remarks>
    internal static BencodeNode ReadFrom(ref Utf8BencodeReader reader, BencodeNodeOptions options)
    {
        switch (reader.TokenType)
        {
            case BencodeTokenType.Integer:
                return reader.TryGetInt64(out long integer)
                    ? BencodeValue.Create(integer)
                    : BencodeValue.Create(reader.GetUInt64());

            case BencodeTokenType.ByteString:
                return BencodeValue.Create(reader.GetBytes());

            case BencodeTokenType.StartList:
            {
                var array = new BencodeArray();
                while (reader.Read() && reader.TokenType != BencodeTokenType.EndList)
                    array.Add(ReadFrom(ref reader, options));

                return array;
            }

            case BencodeTokenType.StartDictionary:
            {
                var obj = new BencodeObject(options);
                while (reader.Read() && reader.TokenType != BencodeTokenType.EndDictionary)
                {
                    string key = reader.GetString();
                    reader.Read();
                    obj[key] = ReadFrom(ref reader, options);
                }

                return obj;
            }

            default:
                throw new BencodeFormatException(
                    string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Format_Invalid_BencodeUnexpectedToken, reader.TokenType, reader.BytesConsumed),
                    reader.BytesConsumed);
        }
    }

    /// <summary>
    /// Creates a deep copy of this node and its entire subtree.
    /// </summary>
    /// <returns>An independent clone with no parent.</returns>
    public abstract BencodeNode DeepClone();

    /// <summary>
    /// Computes the path from the root of this node's tree to this node.
    /// </summary>
    /// <returns>
    /// The JSONPath-style path: <c>$</c> for a root node, with <c>[n]</c> segments for array indices and <c>.name</c>
    /// segments for object keys. A key containing characters other than ASCII letters, digits, and underscores is
    /// rendered as a quoted <c>['name']</c> segment.
    /// </returns>
    /// <remarks>
    /// When the same node instance is stored under more than one key of the same object, the path reports the first
    /// matching key in enumeration order.
    /// </remarks>
    public string GetPath()
    {
        if (Parent is null)
            return "$";

        List<string> segments = [];
        BencodeNode current = this;
        while (current.Parent is { } parent)
        {
            segments.Add(GetPathSegment(parent, current));
            current = parent;
        }

        segments.Reverse();
        return "$" + string.Concat(segments);
    }

    /// <summary>
    /// Replaces this node within its parent container with the supplied value, detaching this node. The implicit
    /// conversions from <see langword="string" />, integers, and byte arrays let scalars be passed directly.
    /// </summary>
    /// <param name="value">
    /// The replacement node, or <see langword="null" /> to clear the slot this node occupies.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="value" /> already belongs to another container.
    /// </exception>
    /// <remarks>
    /// When this node has no parent the call does nothing. After a successful replacement this node's
    /// <see cref="Parent" /> is <see langword="null" /> and it can be added to another container.
    /// </remarks>
    public void ReplaceWith(BencodeNode? value)
    {
        switch (Parent)
        {
            case BencodeObject obj:
                foreach (KeyValuePair<string, BencodeNode?> entry in obj)
                {
                    if (ReferenceEquals(entry.Value, this))
                    {
                        obj[entry.Key] = value;
                        return;
                    }
                }

                return;

            case BencodeArray array:
                array[array.IndexOf(this)] = value;
                return;

            default:
                return;
        }
    }

    /// <summary>
    /// Formats the path segment that addresses <paramref name="child" /> within <paramref name="parent" />.
    /// </summary>
    /// <param name="parent">The containing node.</param>
    /// <param name="child">The child whose segment is produced.</param>
    /// <returns>An index segment for an array parent, or a key segment for an object parent.</returns>
    private static string GetPathSegment(BencodeNode parent, BencodeNode child)
    {
        if (parent is BencodeArray array)
            return "[" + array.IndexOf(child).ToString(CultureInfo.InvariantCulture) + "]";

        var obj = (BencodeObject)parent;
        foreach (KeyValuePair<string, BencodeNode?> entry in obj)
        {
            if (ReferenceEquals(entry.Value, child))
                return FormatKeySegment(entry.Key);
        }

        // Unreachable while the single-parent invariant holds: a child always appears in its parent.
        return string.Empty;
    }

    /// <summary>
    /// Formats an object key as a path segment, quoting keys that contain characters outside ASCII letters, digits, and
    /// underscores.
    /// </summary>
    /// <param name="key">The object key.</param>
    /// <returns>A <c>.name</c> segment for simple keys; otherwise a quoted <c>['name']</c> segment.</returns>
    private static string FormatKeySegment(string key)
    {
        bool simple = key.Length > 0;
        foreach (char c in key)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '_')
            {
                simple = false;
                break;
            }
        }

        return simple
            ? "." + key
            : "['" + key.Replace("'", "\\'", StringComparison.Ordinal) + "']";
    }

    /// <summary>
    /// Determines whether two node trees are structurally equal.
    /// </summary>
    /// <param name="node1">The first node, which may be <see langword="null" />.</param>
    /// <param name="node2">The second node, which may be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when both nodes are <see langword="null" /> or represent the same value kind with equal
    /// content; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Integers compare by value and byte strings by byte sequence. Arrays compare element-wise in order, and objects
    /// compare by key set with equal values, independently of in-memory key order.
    /// </remarks>
    public static bool DeepEquals(BencodeNode? node1, BencodeNode? node2)
    {
        if (ReferenceEquals(node1, node2))
            return true;
        if (node1 is null || node2 is null)
            return false;

        BencodeValueKind kind = node1.GetValueKind();
        if (kind != node2.GetValueKind())
            return false;

        return kind switch
        {
            BencodeValueKind.Integer => node1.AsValue().IntegerEquals(node2.AsValue()),
            BencodeValueKind.ByteString => node1.AsValue().GetValue<byte[]>().AsSpan().SequenceEqual(node2.AsValue().GetValue<byte[]>()),
            BencodeValueKind.Array => ArrayEquals(node1.AsArray(), node2.AsArray()),
            _ => ObjectEquals(node1.AsObject(), node2.AsObject()),
        };
    }

    /// <summary>
    /// Returns a string representation of this node.
    /// </summary>
    /// <returns>A textual rendering of this node.</returns>
    public abstract override string ToString();

    /// <summary>
    /// Creates a <see cref="BencodeValue" /> that wraps the supplied string as a byte string.
    /// </summary>
    /// <param name="value">The string to wrap.</param>
    public static implicit operator BencodeNode(string value) =>
        BencodeValue.Create(value);

    /// <summary>
    /// Creates a <see cref="BencodeValue" /> that wraps the supplied 64-bit integer.
    /// </summary>
    /// <param name="value">The integer to wrap.</param>
    public static implicit operator BencodeNode(long value) =>
        BencodeValue.Create(value);

    /// <summary>
    /// Creates a <see cref="BencodeValue" /> that wraps the supplied 32-bit integer.
    /// </summary>
    /// <param name="value">The integer to wrap.</param>
    public static implicit operator BencodeNode(int value) =>
        BencodeValue.Create(value);

    /// <summary>
    /// Creates a <see cref="BencodeValue" /> that wraps the supplied unsigned 64-bit integer, permitting the full
    /// <see cref="ulong" /> range.
    /// </summary>
    /// <param name="value">The unsigned integer to wrap.</param>
    public static implicit operator BencodeNode(ulong value) =>
        BencodeValue.Create(value);

    /// <summary>
    /// Creates a <see cref="BencodeValue" /> that wraps the supplied byte array as a byte string.
    /// </summary>
    /// <param name="value">The byte array to wrap.</param>
    public static implicit operator BencodeNode(byte[] value) =>
        BencodeValue.Create(value);

    /// <summary>
    /// Reads the scalar value of the supplied node as a 64-bit integer.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not an integer-valued <see cref="BencodeValue" />.
    /// </exception>
    public static explicit operator long(BencodeNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<long>();
    }

    /// <summary>
    /// Reads the scalar value of the supplied node as a 32-bit integer.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not an integer-valued <see cref="BencodeValue" />.
    /// </exception>
    public static explicit operator int(BencodeNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<int>();
    }

    /// <summary>
    /// Reads the scalar value of the supplied node as an unsigned 64-bit integer.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not a non-negative integer-valued <see cref="BencodeValue" />.
    /// </exception>
    public static explicit operator ulong(BencodeNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<ulong>();
    }

    /// <summary>
    /// Reads the scalar value of the supplied node as a string, decoding the byte string as UTF-8.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not a byte-string <see cref="BencodeValue" />.
    /// </exception>
    public static explicit operator string(BencodeNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<string>();
    }

    /// <summary>
    /// Reads the scalar value of the supplied node as a byte array.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not a byte-string <see cref="BencodeValue" />.
    /// </exception>
    public static explicit operator byte[](BencodeNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<byte[]>();
    }

    /// <summary>
    /// Assigns this node's parent, enforcing the single-parent rule.
    /// </summary>
    /// <param name="parent">The container taking ownership of this node.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this node already belongs to a different container.
    /// </exception>
    internal void AssignParent(BencodeNode parent)
    {
        if (Parent is not null && !ReferenceEquals(Parent, parent))
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_NodeAlreadyHasParent);

        Parent = parent;
    }

    /// <summary>
    /// Compares two arrays element-wise in order.
    /// </summary>
    /// <param name="left">The first array.</param>
    /// <param name="right">The second array.</param>
    /// <returns><see langword="true" /> when the arrays have equal elements in the same order.</returns>
    private static bool ArrayEquals(BencodeArray left, BencodeArray right)
    {
        if (left.Count != right.Count)
            return false;

        for (int i = 0; i < left.Count; i++)
        {
            if (!DeepEquals(left[i], right[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Compares two objects by key set with deeply equal values, independently of key order.
    /// </summary>
    /// <param name="left">The first object.</param>
    /// <param name="right">The second object.</param>
    /// <returns><see langword="true" /> when the objects have the same keys mapped to deeply equal values.</returns>
    private static bool ObjectEquals(BencodeObject left, BencodeObject right)
    {
        if (left.Count != right.Count)
            return false;

        foreach (KeyValuePair<string, BencodeNode?> entry in left)
        {
            if (!right.TryGetValue(entry.Key, out BencodeNode? other) || !DeepEquals(entry.Value, other))
                return false;
        }

        return true;
    }
}
