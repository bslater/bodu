// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JSHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using Justin Sobel's JSHash bitwise mixing function. This class
/// cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// For each input byte, JSHash updates the running hash as
/// <c><![CDATA[hash ^= (hash << 5) + (hash >> 2) + byte]]></c>, seeded with <c>0x4E67C6A7</c>. The finalised
/// hash is written to the output buffer in little-endian byte order.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class JSHash
    : NonCryptographicHashAlgorithm
{
    private const int HashLength = 4;
    private const uint Seed = 0x4E67C6A7;

    private uint _workingHash = Seed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JSHash" /> class seeded with the canonical
    /// <c>0x4E67C6A7</c> initial value.
    /// </summary>
    public JSHash()
        : base(HashLength)
    {
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        uint v = _workingHash;
        foreach (byte b in source)
        {
            v ^= (v << 5) + (v >> 2) + b;
        }

        _workingHash = v;
    }

    /// <inheritdoc />
    public override void Reset() => _workingHash = Seed;

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32LittleEndian(destination, _workingHash);
}
