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
/// <para>
/// This is the authoring counterpart of <see cref="CompoundStreamEntry" /> and the compound-file analogue of a
/// <c>JsonValue</c> leaf.
/// </para>
/// <para>
/// A stream node is either <em>in-memory</em>, holding its bytes directly, or <em>deferred</em>, holding a re-openable
/// source (a file or a stream factory) of a known length. A deferred node lets large payloads be streamed straight to
/// the output during serialization without ever being held in memory; reading <see cref="Content" /> on such a node
/// materializes it.
/// </para>
/// </remarks>
public sealed class CompoundStreamNode
    : CompoundNode
{
    /// <summary>The in-memory payload, used when the node is not deferred.</summary>
    private ReadOnlyMemory<byte> _content;

    /// <summary>The deferred source factory, or <see langword="null" /> when the node is in-memory.</summary>
    private Func<Stream>? _open;

    /// <summary>The declared length of the deferred source.</summary>
    private long _length;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStreamNode" /> class holding an in-memory payload.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <param name="content">The initial payload.</param>
    private CompoundStreamNode(string name, ReadOnlyMemory<byte> content)
    {
        Name = name;
        _content = content;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStreamNode" /> class backed by a deferred source.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <param name="open">The factory that opens the payload stream.</param>
    /// <param name="length">The declared payload length, in bytes.</param>
    private CompoundStreamNode(string name, Func<Stream> open, long length)
    {
        Name = name;
        _open = open;
        _length = length;
    }

    /// <summary>
    /// Gets or sets the byte payload of the stream.
    /// </summary>
    /// <returns>The stream's content.</returns>
    /// <remarks>
    /// Reading this property on a deferred node opens and reads its source fully into memory; thereafter the node is
    /// in-memory. Setting the property replaces the payload and makes the node in-memory.
    /// </remarks>
    public ReadOnlyMemory<byte> Content
    {
        get
        {
            if (_open is not null)
            {
                _content = ReadFully(_open, _length);
                _open = null;
            }

            return _content;
        }

        set
        {
            _content = value;
            _open = null;
        }
    }

    /// <summary>
    /// Gets the length, in bytes, of the stream payload.
    /// </summary>
    /// <returns>The payload length; reported without opening a deferred source.</returns>
    public long Length => _open is null ? _content.Length : _length;

    /// <inheritdoc />
    public override CompoundEntryType EntryType => CompoundEntryType.Stream;

    /// <summary>
    /// Gets a value indicating whether the node is backed by a deferred source rather than in-memory bytes.
    /// </summary>
    /// <returns><see langword="true" /> when the node is deferred; otherwise <see langword="false" />.</returns>
    internal bool IsDeferred => _open is not null;

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
    /// Creates a deferred stream node whose payload is read on demand from a re-openable source.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <param name="openRead">A factory that opens a readable stream over the payload each time it is invoked.</param>
    /// <param name="length">The payload length, in bytes.</param>
    /// <returns>A new deferred <see cref="CompoundStreamNode" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="openRead" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length" /> is negative.</exception>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when <paramref name="name" /> is invalid.
    /// </exception>
    public static CompoundStreamNode Create(string name, Func<Stream> openRead, long length)
    {
        ValidateName(name);
        ThrowHelper.ThrowIfNull(openRead);
        ThrowHelper.ThrowIfNegative(length);

        return new CompoundStreamNode(name, openRead, length);
    }

    /// <summary>
    /// Creates a deferred stream node whose payload is read on demand from a file.
    /// </summary>
    /// <param name="name">The entry name.</param>
    /// <param name="path">The path of the file providing the payload.</param>
    /// <returns>A new deferred <see cref="CompoundStreamNode" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="path" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when <paramref name="name" /> is invalid.
    /// </exception>
    public static CompoundStreamNode CreateFromFile(string name, string path)
    {
        ValidateName(name);
        ThrowHelper.ThrowIfNull(path);

        long length = new FileInfo(path).Length;
        return new CompoundStreamNode(name, () => File.OpenRead(path), length);
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
    public override CompoundNode DeepClone()
    {
        CompoundStreamNode clone = _open is not null
            ? new CompoundStreamNode(Name, _open, _length)
            : new CompoundStreamNode(Name, _content.ToArray());

        clone.ClassId = ClassId;
        clone.CreationTime = CreationTime;
        clone.ModifiedTime = ModifiedTime;
        clone.StateBits = StateBits;
        return clone;
    }

    /// <summary>
    /// Opens a readable stream over a deferred node's payload.
    /// </summary>
    /// <returns>A readable stream over the payload.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the node is in-memory.</exception>
    internal Stream OpenContentStream() =>
        _open?.Invoke() ?? throw new InvalidOperationException(CompoundResourceStrings.Op_Invalid_CompoundNodeNotStream);

    /// <summary>
    /// Reads a stream fully into memory, up to the declared length.
    /// </summary>
    /// <param name="open">The factory that opens the payload stream.</param>
    /// <param name="length">The declared payload length.</param>
    /// <returns>The payload bytes.</returns>
    private static byte[] ReadFully(Func<Stream> open, long length)
    {
        using Stream source = open();
        using MemoryStream buffer = new(length > 0 && length <= int.MaxValue ? (int)length : 0);
        source.CopyTo(buffer);
        return buffer.ToArray();
    }
}
