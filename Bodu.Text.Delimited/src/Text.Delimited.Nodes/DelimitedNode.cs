// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

using Bodu.Text.Delimited.Reader;
using Bodu.Text.Delimited.Writer;

namespace Bodu.Text.Delimited.Nodes;

/// <summary>
/// Represents a node in a mutable delimited (CSV/TSV) document tree. A delimited document is an array of records, so
/// the tree has three concrete shapes: a <see cref="DelimitedArray" /> (the document, or a positional record), a
/// <see cref="DelimitedObject" /> (a header-keyed record), and a <see cref="DelimitedValue" /> string field.
/// </summary>
public abstract class DelimitedNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedNode" /> class.
    /// </summary>
    private protected DelimitedNode()
    {
    }

    /// <summary>
    /// Gets the value kind of this node.
    /// </summary>
    /// <value>The node's <see cref="DelimitedValueKind" />.</value>
    public abstract DelimitedValueKind ValueKind { get; }

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a mutable delimited document array.
    /// </summary>
    /// <param name="utf8Delimited">The delimited source bytes.</param>
    /// <returns>The parsed <see cref="DelimitedArray" /> document.</returns>
    /// <exception cref="DelimitedFormatException">Thrown when the bytes are not valid delimited text.</exception>
    public static DelimitedArray Parse(ReadOnlySpan<byte> utf8Delimited) =>
        Parse(utf8Delimited, DelimitedReaderOptions.Default);

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a mutable delimited document array using the supplied reader options.
    /// </summary>
    /// <param name="utf8Delimited">The delimited source bytes.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The parsed <see cref="DelimitedArray" /> document.</returns>
    /// <exception cref="DelimitedFormatException">Thrown when the bytes are not valid delimited text.</exception>
    public static DelimitedArray Parse(ReadOnlySpan<byte> utf8Delimited, DelimitedReaderOptions options)
    {
        var reader = new Utf8DelimitedReader(utf8Delimited, options);
        var stack = new Stack<DelimitedNode>();
        DelimitedArray? root = null;
        string? pendingName = null;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DelimitedTokenType.StartArray:
                    var array = new DelimitedArray();
                    AttachChild(stack, array, ref pendingName);
                    root ??= array;
                    stack.Push(array);
                    break;

                case DelimitedTokenType.StartObject:
                    var obj = new DelimitedObject();
                    AttachChild(stack, obj, ref pendingName);
                    stack.Push(obj);
                    break;

                case DelimitedTokenType.EndArray:
                case DelimitedTokenType.EndObject:
                    stack.Pop();
                    break;

                case DelimitedTokenType.PropertyName:
                    pendingName = reader.GetString();
                    break;

                case DelimitedTokenType.String:
                    AttachChild(stack, new DelimitedValue(reader.GetString()), ref pendingName);
                    break;

                default:
                    break;
            }
        }

        return root ?? new DelimitedArray();
    }

    /// <summary>
    /// Returns this node as a <see cref="DelimitedArray" />.
    /// </summary>
    /// <returns>This node cast to <see cref="DelimitedArray" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this node is not an array.</exception>
    public DelimitedArray AsArray() =>
        this as DelimitedArray ?? throw new InvalidOperationException();

    /// <summary>
    /// Returns this node as a <see cref="DelimitedObject" />.
    /// </summary>
    /// <returns>This node cast to <see cref="DelimitedObject" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this node is not an object.</exception>
    public DelimitedObject AsObject() =>
        this as DelimitedObject ?? throw new InvalidOperationException();

    /// <summary>
    /// Returns this node as a <see cref="DelimitedValue" />.
    /// </summary>
    /// <returns>This node cast to <see cref="DelimitedValue" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this node is not a value.</exception>
    public DelimitedValue AsValue() =>
        this as DelimitedValue ?? throw new InvalidOperationException();

    /// <summary>
    /// Creates a deep copy of this node.
    /// </summary>
    /// <returns>The cloned node.</returns>
    public abstract DelimitedNode DeepClone();

    /// <summary>
    /// Writes this node's delimited representation to the supplied writer.
    /// </summary>
    /// <param name="writer">The writer that receives the delimited bytes.</param>
    public abstract void WriteTo(ref Utf8DelimitedWriter writer);

    /// <summary>
    /// Serializes this node to a new UTF-8 byte array.
    /// </summary>
    /// <returns>The delimited bytes.</returns>
    public byte[] ToUtf8Bytes()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer);
        WriteTo(ref writer);
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Serializes this node to delimited text.
    /// </summary>
    /// <returns>The delimited text.</returns>
    public override string ToString() =>
        Encoding.UTF8.GetString(ToUtf8Bytes());

    /// <summary>
    /// Attaches a parsed child to the container currently on top of the stack.
    /// </summary>
    /// <param name="stack">The container stack.</param>
    /// <param name="child">The child node to attach.</param>
    /// <param name="pendingName">The pending property name (object context), cleared once consumed.</param>
    private static void AttachChild(Stack<DelimitedNode> stack, DelimitedNode child, ref string? pendingName)
    {
        if (stack.Count == 0)
            return;

        switch (stack.Peek())
        {
            case DelimitedArray array:
                array.Add(child);
                break;

            case DelimitedObject obj when pendingName is not null:
                obj[pendingName] = child;
                pendingName = null;
                break;

            default:
                break;
        }
    }
}
