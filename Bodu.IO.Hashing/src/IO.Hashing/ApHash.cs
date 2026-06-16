// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ApHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using Arash Partow's APHash algorithm, which alternates XOR mixing patterns
/// based on input byte-index parity. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// APHash seeds its state with the constant <c>0xAAAAAAAA</c> and combines each input byte using one of two XOR/shift
/// mixes depending on whether the byte's position is even or odd. It is intended for hash-table lookups and similar
/// non-cryptographic use cases.
/// </para>
/// <para>
/// <strong>When to choose APHash.</strong> APHash is one of the simplest position-aware mixes in the lib — pick it when
/// a small, dependency-free 32-bit hash is enough and the performance budget per byte is tight. For better avalanche on
/// hash-table keys prefer <see cref="Fnv1a32" /> or <see cref="MurmurHash3_32" />; for content fingerprinting prefer
/// <see cref="CityHash{T}" />. APHash, <see cref="JSHash" />, <see cref="SDBM" />, and <see cref="Pjw32" /> are
/// siblings in the same low-cost niche and produce comparable distribution; the choice between them is usually driven
/// by interop with an existing system that standardizes on one.
/// </para>
/// <para>
/// <strong>Output and lifecycle.</strong> Produces a 32-bit (4-byte) digest in little-endian byte order.
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> is non-destructive; instances are
/// not thread-safe and should be allocated per consumer.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// var ap = new ApHash();
/// byte[] digest = ap.ComputeHash(System.Text.Encoding.UTF8.GetBytes("dictionary-key"));
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class ApHash
    : NonCryptographicHashAlgorithm
{
    private const int HashLength = 4;
    private const uint Seed = 0xAAAAAAAAu;
    private ulong _size;

    private uint _workingHash = Seed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApHash" /> class with a 32-bit hash size.
    /// </summary>
    public ApHash()
        : base(HashLength)
    {
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        uint v = _workingHash;
        ulong size = _size;
        foreach (byte b in source)
        {
            if ((size & 1UL) == 0UL)
                v ^= (v << 7) ^ b ^ (v >> 3);
            else
                v ^= ~((v << 11) ^ b ^ (v >> 5));

            size++;
        }

        _workingHash = v;
        _size = size;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        _workingHash = Seed;
        _size = 0UL;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32LittleEndian(destination, _workingHash);
}
