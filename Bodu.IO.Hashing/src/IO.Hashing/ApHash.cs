// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ApHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using Arash Partow's APHash algorithm, which alternates XOR
/// mixing patterns based on input byte-index parity. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// APHash seeds its state with the constant <c>0xAAAAAAAA</c> and combines each input byte using one of two
/// XOR/shift mixes depending on whether the byte's position is even or odd. It is intended for hash-table
/// lookups and similar non-cryptographic use cases.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class ApHash
    : NonCryptographicHashAlgorithm
{
    private const int HashLength = 4;
    private const uint Seed = 0xAAAAAAAAu;

    private uint _workingHash = Seed;
    private ulong _size;

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
        uint v = this._workingHash;
        ulong size = this._size;
        foreach (byte b in source)
        {
            if ((size & 1UL) == 0UL)
                v ^= (v << 7) ^ b ^ (v >> 3);
            else
                v ^= ~((v << 11) ^ b ^ (v >> 5));

            size++;
        }

        this._workingHash = v;
        this._size = size;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        this._workingHash = Seed;
        this._size = 0UL;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32LittleEndian(destination, this._workingHash);
}
