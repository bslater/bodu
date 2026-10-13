// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

/// <summary>
/// Provides span-friendly, allocation-aware, and pool-aware extension methods on <see cref="System.Text.Encoding" />,
/// <see cref="ReadOnlySpan{T}" /> of <see cref="char" /> and <see cref="byte" />, and on
/// <see cref="System.Text.Encoder" /> / <see cref="System.Text.Decoder" />.
/// </summary>
/// <remarks>
/// <para>
/// The extensions are split into focused partial files by concern (encode, decode, UTF-8 fast paths, transcoding,
/// fallback handling, preamble / BOM, pooled allocation, <see cref="System.Buffers.IBufferWriter{T}" /> writes,
/// classification, and chunked encoder/decoder operations). Two receiver shapes coexist:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>ReadOnlySpan&lt;char&gt;</c> / <c>ReadOnlySpan&lt;byte&gt;</c> receivers express the <em>data-first</em> form -
/// discoverable via IntelliSense from a span variable.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="System.Text.Encoding" /> receivers express the <em>encoding-first</em> form - useful when an encoding
/// instance is the obvious starting point (for example, encoder-fluent fallback configuration).
/// </description>
/// </item>
/// </list>
/// <para>
/// Pooled allocation surfaces come in three forms with different lifetimes. The <c>*Rented</c> methods return a bare
/// array rented from <see cref="System.Buffers.ArrayPool{T}.Shared" />, which the caller returns. The <c>*Pooled</c>
/// methods return a <see cref="Buffers.PooledBufferBuilder{T}" />, whose disposal returns its rental.
/// <c>ToOwnedBytes</c> and the <c>*Owner</c> methods return an <see cref="System.Buffers.IMemoryOwner{T}" /> rented
/// from a <see cref="System.Buffers.MemoryPool{T}" /> (<see cref="System.Buffers.MemoryPool{T}.Shared" /> by default)
/// and sliced to the encoded length, whose disposal returns it.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Encode a chunk of UTF-16 text to UTF-8 bytes via the span receiver.
/// ReadOnlySpan<char> chars = "héllo".AsSpan();
/// byte[] utf8 = chars.ToUtf8Bytes();
///
/// // Round-trip via the encoding receiver, using the pooled builder for zero unnecessary allocations.
/// using PooledBufferBuilder<byte> pooled = System.Text.Encoding.UTF8.GetBytesPooled(chars);
/// ReadOnlySpan<byte> encoded = pooled.WrittenSpan;
///
/// // Detect a UTF-8 byte-order-mark and decode the remainder.
/// if (System.Text.Encoding.UTF8.StartsWithPreamble(encoded))
///     encoded = System.Text.Encoding.UTF8.StripPreamble(encoded);
///
/// string decoded = encoded.DecodeToString(System.Text.Encoding.UTF8);
///]]>
/// </code>
/// </example>
public static partial class EncodingExtensions
{
}
