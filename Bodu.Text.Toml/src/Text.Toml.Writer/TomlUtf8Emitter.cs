// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlUtf8Emitter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Toml.Writer;

/// <summary>
/// A minimal append-only UTF-8 emitter over an <see cref="IBufferWriter{T}" />, leasing spans from the destination and
/// advancing them as they fill so that canonical TOML bytes flow to the output without an intermediate document-sized
/// buffer.
/// </summary>
/// <remarks>
/// Callers must invoke <see cref="Complete" /> after the final append to commit the tail of the current lease to the
/// destination writer.
/// </remarks>
internal ref struct TomlUtf8Emitter
{
    /// <summary>The destination buffer writer receiving the emitted bytes.</summary>
    private readonly IBufferWriter<byte> _output;

    /// <summary>The current span leased from <see cref="_output" />.</summary>
    private Span<byte> _buffer;

    /// <summary>The number of bytes written into <see cref="_buffer" /> and not yet advanced.</summary>
    private int _pos;

    /// <summary>Whether any byte has been emitted.</summary>
    private bool _hasContent;

    /// <summary>The total number of bytes emitted.</summary>
    private long _bytesWritten;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlUtf8Emitter" /> struct over the destination writer.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    internal TomlUtf8Emitter(IBufferWriter<byte> output)
    {
        _output = output;
    }

    /// <summary>
    /// Gets a value indicating whether any byte has been emitted, used to decide blank-line separation before a section
    /// header.
    /// </summary>
    /// <returns><see langword="true" /> once any byte has been written.</returns>
    internal readonly bool HasContent => _hasContent;

    /// <summary>
    /// Gets the total number of bytes emitted, for the writer's committed/pending accounting.
    /// </summary>
    /// <returns>The byte count.</returns>
    internal readonly long BytesWritten => _bytesWritten;

    /// <summary>
    /// Appends a single byte.
    /// </summary>
    /// <param name="value">The byte to append.</param>
    internal void Append(byte value)
    {
        Reserve(1)[0] = value;
        _pos++;
        _bytesWritten++;
        _hasContent = true;
    }

    /// <summary>
    /// Appends raw UTF-8 bytes.
    /// </summary>
    /// <param name="bytes">The bytes to append.</param>
    internal void Append(scoped ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            return;

        bytes.CopyTo(Reserve(bytes.Length));
        _pos += bytes.Length;
        _bytesWritten += bytes.Length;
        _hasContent = true;
    }

    /// <summary>
    /// Appends characters known to be ASCII, narrowing each to one byte.
    /// </summary>
    /// <param name="chars">The ASCII characters to append.</param>
    internal void AppendAscii(scoped ReadOnlySpan<char> chars)
    {
        if (chars.IsEmpty)
            return;

        Span<byte> span = Reserve(chars.Length);
        for (int i = 0; i < chars.Length; i++)
            span[i] = (byte)chars[i];

        _pos += chars.Length;
        _bytesWritten += chars.Length;
        _hasContent = true;
    }

    /// <summary>
    /// Appends one Unicode scalar value in its UTF-8 encoding.
    /// </summary>
    /// <param name="rune">The scalar value to append.</param>
    internal void Append(Rune rune)
    {
        int written = rune.EncodeToUtf8(Reserve(4));
        _pos += written;
        _bytesWritten += written;
        _hasContent = true;
    }

    /// <summary>
    /// Commits the unadvanced tail of the current lease to the destination writer.
    /// </summary>
    internal void Complete()
    {
        if (_pos > 0)
        {
            _output.Advance(_pos);
            _pos = 0;
            _buffer = default;
        }
    }

    /// <summary>
    /// Returns a span with at least <paramref name="sizeHint" /> writable bytes, advancing and re-leasing from the
    /// destination when the current lease is full.
    /// </summary>
    /// <param name="sizeHint">The minimum number of bytes required.</param>
    /// <returns>The writable span.</returns>
    private Span<byte> Reserve(int sizeHint)
    {
        if (_buffer.Length - _pos < sizeHint)
        {
            if (_pos > 0)
                _output.Advance(_pos);

            _buffer = _output.GetSpan(Math.Max(sizeHint, 256));
            _pos = 0;
        }

        return _buffer[_pos..];
    }
}
