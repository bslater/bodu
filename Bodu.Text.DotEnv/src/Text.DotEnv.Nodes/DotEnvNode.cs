// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

using Bodu.Text.DotEnv.Reader;
using Bodu.Text.DotEnv.Writer;

namespace Bodu.Text.DotEnv.Nodes;

/// <summary>
/// Represents a node in a mutable DotEnv document tree. A DotEnv document is a flat, ordered object of string-valued
/// keys, so the tree has exactly two concrete shapes: a <see cref="DotEnvObject" /> root and the
/// <see cref="DotEnvValue" /> string leaves it contains.
/// </summary>
public abstract class DotEnvNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvNode" /> class.
    /// </summary>
    private protected DotEnvNode()
    {
    }

    /// <summary>
    /// Gets the value kind of this node.
    /// </summary>
    /// <value>The node's <see cref="DotEnvValueKind" />.</value>
    public abstract DotEnvValueKind ValueKind { get; }

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a mutable DotEnv object tree.
    /// </summary>
    /// <param name="utf8DotEnv">The DotEnv source bytes.</param>
    /// <returns>The parsed <see cref="DotEnvObject" /> root.</returns>
    /// <exception cref="DotEnvFormatException">Thrown when the bytes are not valid DotEnv.</exception>
    public static DotEnvObject Parse(ReadOnlySpan<byte> utf8DotEnv) =>
        Parse(utf8DotEnv, DotEnvReaderOptions.Default);

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a mutable DotEnv object tree using the supplied reader options.
    /// </summary>
    /// <param name="utf8DotEnv">The DotEnv source bytes.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The parsed <see cref="DotEnvObject" /> root.</returns>
    /// <exception cref="DotEnvFormatException">Thrown when the bytes are not valid DotEnv.</exception>
    public static DotEnvObject Parse(ReadOnlySpan<byte> utf8DotEnv, DotEnvReaderOptions options)
    {
        var reader = new Utf8DotEnvReader(utf8DotEnv, options);
        var root = new DotEnvObject();
        string? pendingKey = null;
        bool pendingExport = false;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DotEnvTokenType.PropertyName:
                    pendingKey = reader.GetString();
                    pendingExport = reader.CurrentIsExport;
                    break;

                case DotEnvTokenType.String:
                    root.Set(pendingKey!, new DotEnvValue(reader.GetString()), pendingExport);
                    pendingKey = null;
                    pendingExport = false;
                    break;

                default:
                    break;
            }
        }

        return root;
    }

    /// <summary>
    /// Parses the supplied DotEnv text into a mutable DotEnv object tree.
    /// </summary>
    /// <param name="dotEnv">The DotEnv source text.</param>
    /// <returns>The parsed <see cref="DotEnvObject" /> root.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dotEnv" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DotEnvFormatException">Thrown when the text is not valid DotEnv.</exception>
    public static DotEnvObject Parse(string dotEnv)
    {
        ThrowHelper.ThrowIfNull(dotEnv);

        return Parse(Encoding.UTF8.GetBytes(dotEnv));
    }

    /// <summary>
    /// Returns this node as a <see cref="DotEnvObject" />.
    /// </summary>
    /// <returns>This node cast to <see cref="DotEnvObject" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this node is not an object.</exception>
    public DotEnvObject AsObject() =>
        this as DotEnvObject ?? throw new InvalidOperationException();

    /// <summary>
    /// Returns this node as a <see cref="DotEnvValue" />.
    /// </summary>
    /// <returns>This node cast to <see cref="DotEnvValue" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this node is not a value.</exception>
    public DotEnvValue AsValue() =>
        this as DotEnvValue ?? throw new InvalidOperationException();

    /// <summary>
    /// Creates a deep copy of this node.
    /// </summary>
    /// <returns>The cloned node.</returns>
    public abstract DotEnvNode DeepClone();

    /// <summary>
    /// Writes this node's DotEnv representation to the supplied writer.
    /// </summary>
    /// <param name="writer">The writer that receives the DotEnv bytes.</param>
    public abstract void WriteTo(ref Utf8DotEnvWriter writer);

    /// <summary>
    /// Serializes this node to a new UTF-8 byte array.
    /// </summary>
    /// <returns>The DotEnv bytes.</returns>
    public byte[] ToUtf8Bytes()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DotEnvWriter(buffer);
        WriteTo(ref writer);
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Serializes this node to DotEnv text.
    /// </summary>
    /// <returns>The DotEnv text.</returns>
    public override string ToString() =>
        Encoding.UTF8.GetString(ToUtf8Bytes());

    /// <summary>
    /// Converts a string to a <see cref="DotEnvValue" />.
    /// </summary>
    /// <param name="value">The string value.</param>
    public static implicit operator DotEnvNode(string value) =>
        new DotEnvValue(value);
}
