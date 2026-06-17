// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SuperFastHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using Paul Hsieh's <c>SuperFastHash</c> algorithm, intended for hash-table
/// keying. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The algorithm seeds the running hash with the total length of the input and therefore requires the entire payload to
/// be visible before finalization. Appended bytes are accumulated in an internal buffer and the full compute is
/// executed each time the current hash is requested. The finalized 32-bit value is written to the output buffer in
/// little-endian byte order.
/// </para>
/// <para>
/// Because the implementation buffers its input, memory use grows linearly with the total number of bytes appended
/// between calls to <see cref="Reset" />. The algorithm is well suited to short keys — its intended use case — but
/// should be avoided for very large streams where a block-oriented, fixed-memory hash would be more appropriate.
/// </para>
/// <para>
/// <strong>When to choose SuperFastHash.</strong> SuperFastHash predates MurmurHash and was designed by Paul Hsieh for
/// in-memory hash tables of short keys (32-byte block sums and similar). It has been superseded by the MurmurHash3 /
/// CityHash / xxHash generation, which beat it on every measurable axis — pick it only when reproducing a digest from
/// existing SuperFastHash-based code. For new work on short keys prefer <see cref="MurmurHash3_32" /> or
/// <see cref="Fnv1a32" />; for streaming and very large inputs prefer <see cref="CityHash64" /> or a CRC variant via
/// <see cref="Bodu.IO.Hashing.Checksums.Crc" />.
/// </para>
/// <para>
/// Instances are not thread-safe; share behind explicit synchronization.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// // Suited to short keys; avoid for multi-megabyte streams (the input is fully buffered).
/// var sfh = new SuperFastHash();
/// byte[] digest = sfh.ComputeHash(System.Text.Encoding.UTF8.GetBytes("short-key"));
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class SuperFastHash
    : NonCryptographicHashAlgorithm, IDisposable
{
    /// <summary>
    /// The fixed digest length, in bytes, produced by this algorithm.
    /// </summary>
    private const int HashLength = 4;

    /// <summary>
    /// The buffer accumulating all appended input, since the algorithm requires the full payload before finalization.
    /// </summary>
    private readonly MemoryStream _buffer = new();

    /// <summary>
    /// Indicates whether the instance has been disposed and its buffered input released.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuperFastHash" /> class.
    /// </summary>
    public SuperFastHash()
        : base(HashLength)
    {
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _buffer.Write(source);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="SuperFastHash" /> instance and clears its buffered input
    /// state.
    /// </summary>
    /// <remarks>
    /// Subsequent calls to <see cref="Append(ReadOnlySpan{byte})" />, <see cref="Reset" />, or
    /// <see cref="GetCurrentHashCore(Span{byte})" /> throw <see cref="ObjectDisposedException" />. Calling
    /// <see cref="Dispose" /> multiple times is safe and has no effect after the first invocation.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        _buffer.Dispose();
        _disposed = true;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _buffer.SetLength(0);
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ReadOnlySpan<byte> data = _buffer.GetBuffer().AsSpan(0, (int)_buffer.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(destination, Compute(data));
    }

    /// <summary>
    /// Computes Paul Hsieh's SuperFastHash over the supplied payload and returns the finalized 32-bit value.
    /// </summary>
    /// <param name="data">The input bytes to hash.</param>
    /// <returns>The 32-bit SuperFastHash value. Returns <c>0</c> when <paramref name="data" /> is empty.</returns>
    /// <remarks>
    /// The pre-avalanche loop consumes the payload in 4-byte blocks, reading each half as a little-endian 16-bit word.
    /// Any residual 1, 2, or 3 trailing bytes are folded in by a dedicated tail branch, and the final avalanche mixes
    /// the result prior to return. The odd-byte tail branches sign-extend the trailing byte via <see cref="sbyte" /> to
    /// match Hsieh's reference C <c>(signed char)</c> behavior.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Compute(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return 0;

        uint hash = (uint)data.Length;
        int rem = data.Length & 3;
        int blocks = data.Length >> 2;
        int i = 0;

        for (int b = 0; b < blocks; b++)
        {
            ushort w0 = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(i, 2));
            ushort w1 = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(i + 2, 2));

            hash += w0;
            uint tmp = ((uint)w1 << 11) ^ hash;
            hash = (hash << 16) ^ tmp;
            hash += hash >> 11;

            i += 4;
        }

        switch (rem)
        {
            case 3:
                hash += BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(i, 2));
                hash ^= hash << 16;
                hash ^= (uint)((sbyte)data[i + 2]) << 18;
                hash += hash >> 11;
                break;

            case 2:
                hash += BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(i, 2));
                hash ^= hash << 11;
                hash += hash >> 17;
                break;

            case 1:
                hash += (uint)(sbyte)data[i];
                hash ^= hash << 10;
                hash += hash >> 1;
                break;
        }

        hash ^= hash << 3;
        hash += hash >> 5;
        hash ^= hash << 4;
        hash += hash >> 17;
        hash ^= hash << 25;
        hash += hash >> 6;

        return hash;
    }
}
