// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Represents a single node in a mutable TOML document object model, serving as the base for the three concrete node
/// kinds — <see cref="TomlObject" />, <see cref="TomlArray" />, and <see cref="TomlValue" />. Mirrors the role of
/// <see cref="System.Text.Json.Nodes.JsonNode" /> for TOML.
/// </summary>
/// <remarks>
/// <para>
/// A node tree is editable in place: containers expose the standard collection surfaces, scalar values can be replaced,
/// and any node can be re-serialized to canonical TOML through <see cref="WriteTo" /> or <see cref="ToUtf8Bytes" />.
/// TOML defines eight scalar value kinds — a string, a 64-bit integer, a float, a Boolean, and the four date-time kinds
/// — so the model omits the null node that <see cref="System.Text.Json.Nodes.JsonNode" /> carries; a tree that still
/// contains a <see langword="null" /> entry cannot be written.
/// </para>
/// <para>
/// A TOML document's root is always a table, so a document cannot be a bare scalar or array.
/// <see cref="Parse(ReadOnlySpan{byte})" /> therefore yields a <see cref="TomlObject" /> root, and serializing a node
/// whose root is not a table throws.
/// </para>
/// <para>
/// Each node has at most one parent. Adding a node that already belongs to another container throws an
/// <see cref="InvalidOperationException" />, matching the single-parent rule of
/// <see cref="System.Text.Json.Nodes.JsonNode" />.
/// </para>
/// </remarks>
public abstract class TomlNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlNode" /> class.
    /// </summary>
    /// <remarks>
    /// The constructor is accessible only within this assembly, which closes derivation to the three concrete node
    /// kinds defined alongside it.
    /// </remarks>
    private protected TomlNode()
    {
    }

    /// <summary>
    /// Gets the node that contains this node, or <see langword="null" /> when this node is the root of its tree.
    /// </summary>
    /// <value>The containing node, or <see langword="null" /> for a root node.</value>
    /// <returns>The parent node, or <see langword="null" /> when this node has no parent.</returns>
    public TomlNode? Parent { get; internal set; }

    /// <summary>
    /// Gets the topmost node of the tree this node belongs to.
    /// </summary>
    /// <value>The root node reached by following <see cref="Parent" /> until it is <see langword="null" />.</value>
    /// <returns>The root node, which is this node when it has no parent.</returns>
    public TomlNode Root
    {
        get
        {
            TomlNode current = this;
            while (current.Parent is { } parent)
                current = parent;

            return current;
        }
    }

    /// <summary>
    /// Gets or sets the element at the specified index, treating this node as a <see cref="TomlArray" />.
    /// </summary>
    /// <param name="index">The zero-based index of the element.</param>
    /// <value>The element at <paramref name="index" />.</value>
    /// <returns>The element at <paramref name="index" />, which may be <see langword="null" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this node is not a <see cref="TomlArray" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> is outside the bounds of the array.
    /// </exception>
    public TomlNode? this[int index]
    {
        get => AsArray()[index];
        set => AsArray()[index] = value;
    }

    /// <summary>
    /// Gets or sets the value associated with the specified property name, treating this node as a
    /// <see cref="TomlObject" />.
    /// </summary>
    /// <param name="propertyName">The property name to look up.</param>
    /// <value>The value associated with <paramref name="propertyName" />.</value>
    /// <returns>
    /// The value associated with <paramref name="propertyName" />, which may be <see langword="null" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this node is not a <see cref="TomlObject" />.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="propertyName" /> is <see langword="null" />.
    /// </exception>
    public TomlNode? this[string propertyName]
    {
        get => AsObject()[propertyName];
        set => AsObject()[propertyName] = value;
    }

    /// <summary>
    /// Returns this node as a <see cref="TomlObject" />.
    /// </summary>
    /// <returns>This node cast to <see cref="TomlObject" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this node is not a <see cref="TomlObject" />.
    /// </exception>
    public TomlObject AsObject() =>
        this as TomlObject
            ?? throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_NodeNotObject, GetValueKind()));

    /// <summary>
    /// Returns this node as a <see cref="TomlArray" />.
    /// </summary>
    /// <returns>This node cast to <see cref="TomlArray" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this node is not a <see cref="TomlArray" />.</exception>
    public TomlArray AsArray() =>
        this as TomlArray
            ?? throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_NodeNotArray, GetValueKind()));

    /// <summary>
    /// Returns this node as a <see cref="TomlValue" />.
    /// </summary>
    /// <returns>This node cast to <see cref="TomlValue" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this node is not a <see cref="TomlValue" />.</exception>
    public TomlValue AsValue() =>
        this as TomlValue
            ?? throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_NodeNotValue, GetValueKind()));

    /// <summary>
    /// Gets the kind of value this node represents.
    /// </summary>
    /// <returns>The <see cref="TomlValueKind" /> of this node.</returns>
    public abstract TomlValueKind GetValueKind();

    /// <summary>
    /// Returns the scalar value of this node converted to the requested type, treating this node as a
    /// <see cref="TomlValue" />.
    /// </summary>
    /// <typeparam name="T">The type to convert the scalar value to.</typeparam>
    /// <returns>The converted scalar value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this node is not a <see cref="TomlValue" />, or its stored value cannot be converted to
    /// <typeparamref name="T" />.
    /// </exception>
    public T GetValue<T>() =>
        AsValue().GetValue<T>();

    /// <summary>
    /// Writes the canonical TOML encoding of this node to the supplied writer.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the subtree rooted at this node contains a <see langword="null" /> entry, which has no TOML
    /// representation.
    /// </exception>
    /// <remarks>
    /// Because a TOML document's root must be a table, the writer produces a document only when the outermost node is a
    /// <see cref="TomlObject" />. Writing a node whose root is a scalar or array yields no document; use
    /// <see cref="ToUtf8Bytes" /> to obtain the canonical bytes, which validates the table-root rule.
    /// </remarks>
    public abstract void WriteTo(Utf8TomlWriter writer);

    /// <summary>
    /// Serializes this node to a new canonical TOML byte array.
    /// </summary>
    /// <returns>The UTF-8 TOML encoding of this node.</returns>
    /// <exception cref="TomlSerializationException">
    /// Thrown when this node's root is not a <see cref="TomlObject" />, or the subtree rooted at this node contains a
    /// <see langword="null" /> entry, which has no TOML representation.
    /// </exception>
    public byte[] ToUtf8Bytes()
    {
        if (GetValueKind() != TomlValueKind.Table)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_RootNotTable, GetType()));
        }

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer);
        WriteTo(writer);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Parses a single TOML document into a node tree.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML source bytes.</param>
    /// <returns>The root node of the parsed tree, which is always a <see cref="TomlObject" />.</returns>
    /// <exception cref="TomlFormatException">
    /// Thrown when <paramref name="utf8Toml" /> is not a valid TOML document.
    /// </exception>
    public static TomlNode? Parse(ReadOnlySpan<byte> utf8Toml) =>
        Parse(utf8Toml, default);

    /// <summary>
    /// Parses a single TOML document into a node tree, using the supplied options for every table created while
    /// parsing.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML source bytes.</param>
    /// <param name="options">The node options controlling property-name case sensitivity.</param>
    /// <returns>The root node of the parsed tree, which is always a <see cref="TomlObject" />.</returns>
    /// <exception cref="TomlFormatException">
    /// Thrown when <paramref name="utf8Toml" /> is not a valid TOML document.
    /// </exception>
    /// <remarks>
    /// Every <see cref="TomlObject" /> materialized while parsing adopts the comparison selected by
    /// <paramref name="options" />, so a case-insensitive parse yields a tree whose table lookups ignore case.
    /// </remarks>
    public static TomlNode? Parse(ReadOnlySpan<byte> utf8Toml, TomlNodeOptions options)
    {
        var reader = new TomlDocumentReader(utf8Toml);
        if (!reader.Read())
            throw new TomlFormatException(TomlResourceStrings.Format_Invalid_TomlExpectedValue);

        return ReadFrom(ref reader, options);
    }

    /// <summary>
    /// Reads a node from the reader, which must be positioned on the first token of the value to read.
    /// </summary>
    /// <param name="reader">The reader positioned on the value's first token.</param>
    /// <returns>The node read from the reader.</returns>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the reader is positioned on an unexpected token.
    /// </exception>
    /// <remarks>
    /// On return the reader is positioned on the value's last token (the scalar token itself, or the container's end
    /// token), matching the read-positioning contract the serializer's converters observe. Tables are created with the
    /// default, case-sensitive comparison.
    /// </remarks>
    internal static TomlNode ReadFrom(ref TomlDocumentReader reader) =>
        ReadFrom(ref reader, default);

    /// <summary>
    /// Reads a node from the reader, which must be positioned on the first token of the value to read, materializing
    /// every table with the comparison selected by the supplied options.
    /// </summary>
    /// <param name="reader">The reader positioned on the value's first token.</param>
    /// <param name="options">The node options controlling property-name case sensitivity.</param>
    /// <returns>The node read from the reader.</returns>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the reader is positioned on an unexpected token.
    /// </exception>
    /// <remarks>
    /// On return the reader is positioned on the value's last token (the scalar token itself, or the container's end
    /// token), matching the read-positioning contract the serializer's converters observe.
    /// </remarks>
    internal static TomlNode ReadFrom(ref TomlDocumentReader reader, TomlNodeOptions options)
    {
        switch (reader.TokenType)
        {
            case TomlTokenType.String:
                return TomlValue.Create(reader.GetString());

            case TomlTokenType.Integer:
                return TomlValue.Create(reader.GetInt64());

            case TomlTokenType.Float:
                return TomlValue.Create(reader.GetDouble());

            case TomlTokenType.Boolean:
                return TomlValue.Create(reader.GetBoolean());

            case TomlTokenType.OffsetDateTime:
                return TomlValue.Create(reader.GetDateTimeOffset());

            case TomlTokenType.LocalDateTime:
                return TomlValue.Create(reader.GetDateTime());

            case TomlTokenType.LocalDate:
                return TomlValue.Create(reader.GetDateOnly());

            case TomlTokenType.LocalTime:
                return TomlValue.Create(reader.GetTimeOnly());

            case TomlTokenType.StartArray:
                {
                    var array = new TomlArray();
                    while (reader.Read() && reader.TokenType != TomlTokenType.EndArray)
                        array.Add(ReadFrom(ref reader, options));

                    return array;
                }

            case TomlTokenType.StartTable:
                {
                    var obj = new TomlObject(options);
                    while (reader.Read() && reader.TokenType != TomlTokenType.EndTable)
                    {
                        string key = reader.GetString();
                        reader.Read();
                        obj[key] = ReadFrom(ref reader, options);
                    }

                    return obj;
                }

            default:
                throw new TomlSerializationException(
                    string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedValue, reader.TokenType));
        }
    }

    /// <summary>
    /// Creates a deep copy of this node and its entire subtree.
    /// </summary>
    /// <returns>An independent clone with no parent.</returns>
    public abstract TomlNode DeepClone();

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
    /// Scalars compare by kind and value. Arrays compare element-wise in order, and tables compare by key set with
    /// equal values, independently of in-memory key order.
    /// </remarks>
    public static bool DeepEquals(TomlNode? node1, TomlNode? node2)
    {
        if (ReferenceEquals(node1, node2))
            return true;
        if (node1 is null || node2 is null)
            return false;

        TomlValueKind kind = node1.GetValueKind();
        if (kind != node2.GetValueKind())
            return false;

        return kind switch
        {
            TomlValueKind.Array => ArrayEquals(node1.AsArray(), node2.AsArray()),
            TomlValueKind.Table => ObjectEquals(node1.AsObject(), node2.AsObject()),
            _ => node1.AsValue().ValueEquals(node2.AsValue()),
        };
    }

    /// <summary>
    /// Returns a string representation of this node.
    /// </summary>
    /// <returns>A textual rendering of this node.</returns>
    public abstract override string ToString();

    /// <summary>
    /// Creates a <see cref="TomlValue" /> that wraps the supplied string.
    /// </summary>
    /// <param name="value">The string to wrap.</param>
    public static implicit operator TomlNode(string value) =>
        TomlValue.Create(value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> that wraps the supplied 64-bit integer.
    /// </summary>
    /// <param name="value">The integer to wrap.</param>
    public static implicit operator TomlNode(long value) =>
        TomlValue.Create(value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> that wraps the supplied 32-bit integer.
    /// </summary>
    /// <param name="value">The integer to wrap.</param>
    public static implicit operator TomlNode(int value) =>
        TomlValue.Create(value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> that wraps the supplied floating-point value.
    /// </summary>
    /// <param name="value">The floating-point value to wrap.</param>
    public static implicit operator TomlNode(double value) =>
        TomlValue.Create(value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> that wraps the supplied Boolean value.
    /// </summary>
    /// <param name="value">The Boolean value to wrap.</param>
    public static implicit operator TomlNode(bool value) =>
        TomlValue.Create(value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> that wraps the supplied offset date-time value.
    /// </summary>
    /// <param name="value">The offset date-time value to wrap.</param>
    public static implicit operator TomlNode(DateTimeOffset value) =>
        TomlValue.Create(value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> that wraps the supplied local date-time value.
    /// </summary>
    /// <param name="value">The local date-time value to wrap.</param>
    public static implicit operator TomlNode(DateTime value) =>
        TomlValue.Create(value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> that wraps the supplied local date value.
    /// </summary>
    /// <param name="value">The local date value to wrap.</param>
    public static implicit operator TomlNode(DateOnly value) =>
        TomlValue.Create(value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> that wraps the supplied local time value.
    /// </summary>
    /// <param name="value">The local time value to wrap.</param>
    public static implicit operator TomlNode(TimeOnly value) =>
        TomlValue.Create(value);

    /// <summary>
    /// Reads the scalar value of the supplied node as a string.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not a string-valued <see cref="TomlValue" />.
    /// </exception>
    public static explicit operator string(TomlNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<string>();
    }

    /// <summary>
    /// Reads the scalar value of the supplied node as a 64-bit integer.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not an integer-valued <see cref="TomlValue" />.
    /// </exception>
    public static explicit operator long(TomlNode node)
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
    /// Thrown when <paramref name="node" /> is not an integer-valued <see cref="TomlValue" /> whose value fits a 32-bit
    /// integer.
    /// </exception>
    public static explicit operator int(TomlNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<int>();
    }

    /// <summary>
    /// Reads the scalar value of the supplied node as a floating-point value.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not a float-valued <see cref="TomlValue" />.
    /// </exception>
    public static explicit operator double(TomlNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<double>();
    }

    /// <summary>
    /// Reads the scalar value of the supplied node as a Boolean.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not a Boolean-valued <see cref="TomlValue" />.
    /// </exception>
    public static explicit operator bool(TomlNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<bool>();
    }

    /// <summary>
    /// Reads the scalar value of the supplied node as an offset date-time.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not an offset-date-time-valued <see cref="TomlValue" />.
    /// </exception>
    public static explicit operator DateTimeOffset(TomlNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<DateTimeOffset>();
    }

    /// <summary>
    /// Reads the scalar value of the supplied node as a local date-time.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not a local-date-time-valued <see cref="TomlValue" />.
    /// </exception>
    public static explicit operator DateTime(TomlNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<DateTime>();
    }

    /// <summary>
    /// Reads the scalar value of the supplied node as a local date.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not a local-date-valued <see cref="TomlValue" />.
    /// </exception>
    public static explicit operator DateOnly(TomlNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<DateOnly>();
    }

    /// <summary>
    /// Reads the scalar value of the supplied node as a local time.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node" /> is not a local-time-valued <see cref="TomlValue" />.
    /// </exception>
    public static explicit operator TimeOnly(TomlNode node)
    {
        ThrowHelper.ThrowIfNull(node);
        return node.GetValue<TimeOnly>();
    }

    /// <summary>
    /// Assigns this node's parent, enforcing the single-parent rule.
    /// </summary>
    /// <param name="parent">The container taking ownership of this node.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this node already belongs to a different container.
    /// </exception>
    internal void AssignParent(TomlNode parent)
    {
        if (Parent is not null && !ReferenceEquals(Parent, parent))
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_NodeAlreadyHasParent);

        Parent = parent;
    }

    /// <summary>
    /// Compares two arrays element-wise in order.
    /// </summary>
    /// <param name="left">The first array.</param>
    /// <param name="right">The second array.</param>
    /// <returns><see langword="true" /> when the arrays have equal elements in the same order.</returns>
    private static bool ArrayEquals(TomlArray left, TomlArray right)
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
    /// Compares two tables by key set with deeply equal values, independently of key order.
    /// </summary>
    /// <param name="left">The first table.</param>
    /// <param name="right">The second table.</param>
    /// <returns><see langword="true" /> when the tables have the same keys mapped to deeply equal values.</returns>
    private static bool ObjectEquals(TomlObject left, TomlObject right)
    {
        if (left.Count != right.Count)
            return false;

        foreach (KeyValuePair<string, TomlNode?> entry in left)
        {
            if (!right.TryGetValue(entry.Key, out TomlNode? other) || !DeepEquals(entry.Value, other))
                return false;
        }

        return true;
    }
}
