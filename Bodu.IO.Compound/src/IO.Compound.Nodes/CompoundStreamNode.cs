// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.IO.Compound.Nodes;

/// <summary>
/// Represents a stream entry in a mutable compound-file object model — a named, file-like node carrying an opaque byte
/// payload.
/// </summary>
/// <remarks>
/// This is the authoring counterpart of <see cref="CompoundStreamEntry" /> and the compound-file analogue of a
/// <c>JsonValue</c> leaf. The payload is held in memory and may be replaced at any time before serialization.
/// </remarks>
public sealed class CompoundStreamNode
    : CompoundNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStreamNode" /> class.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <param name="content">The initial payload.</param>
    private CompoundStreamNode(string name, ReadOnlyMemory<byte> content)
    {
        Name = name;
        Content = content;
    }

    /// <summary>
    /// Gets or sets the byte payload of the stream.
    /// </summary>
    /// <returns>The stream's content.</returns>
    public ReadOnlyMemory<byte> Content { get; set; }

    /// <summary>
    /// Gets the length, in bytes, of the stream payload.
    /// </summary>
    /// <returns>The payload length.</returns>
    public long Length => Content.Length;

    /// <inheritdoc />
    public override CompoundEntryType EntryType => CompoundEntryType.Stream;

    /// <summary>
    /// Creates a stream node with the specified name and payload.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <param name="content">The payload bytes.</param>
    /// <returns>A new <see cref="CompoundStreamNode" />.</returns>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when <paramref name="name" /> is invalid.
    /// </exception>
    public static CompoundStreamNode Create(string name, ReadOnlyMemory<byte> content)
    {
        ValidateName(name);

        return new CompoundStreamNode(name, content);
    }

    /// <summary>
    /// Creates a stream node whose payload is the encoded form of the supplied text.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <param name="text">The text to encode.</param>
    /// <param name="encoding">The encoding to apply; defaults to UTF-8.</param>
    /// <returns>A new <see cref="CompoundStreamNode" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when <paramref name="name" /> is invalid.
    /// </exception>
    public static CompoundStreamNode Create(string name, string text, Encoding? encoding = null)
    {
        ThrowHelper.ThrowIfNull(text);

        return Create(name, (ReadOnlyMemory<byte>)(encoding ?? Encoding.UTF8).GetBytes(text));
    }

    /// <summary>
    /// Creates a stream node whose payload is read from the supplied stream.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <param name="source">The stream to read to its end.</param>
    /// <returns>A new <see cref="CompoundStreamNode" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when <paramref name="name" /> is invalid.
    /// </exception>
    public static CompoundStreamNode Create(string name, Stream source)
    {
        ThrowHelper.ThrowIfNull(source);

        using MemoryStream buffer = new();
        source.CopyTo(buffer);
        return Create(name, (ReadOnlyMemory<byte>)buffer.ToArray());
    }

    /// <summary>
    /// Replaces the stream payload with the supplied bytes.
    /// </summary>
    /// <param name="content">The new payload, copied into the node.</param>
    public void SetContent(ReadOnlySpan<byte> content) =>
        Content = content.ToArray();

    /// <summary>
    /// Replaces the stream payload with the encoded form of the supplied text.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <param name="encoding">The encoding to apply; defaults to UTF-8.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    public void SetContent(string text, Encoding? encoding = null)
    {
        ThrowHelper.ThrowIfNull(text);

        Content = (encoding ?? Encoding.UTF8).GetBytes(text);
    }

    /// <inheritdoc />
    public override CompoundNode DeepClone() =>
        new CompoundStreamNode(Name, Content.ToArray())
        {
            ClassId = ClassId,
            CreationTime = CreationTime,
            ModifiedTime = ModifiedTime,
            StateBits = StateBits,
        };
}
