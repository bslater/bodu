// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Pjw32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using Peter J. Weinberger's PJW shift-and-fold algorithm (as
/// described in the "Dragon Book"). This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// For each input byte, the hash is shifted left by 4 bits and the byte added; any overflow into the top 4
/// bits is then XOR-folded back into the low-order bits, producing a well-distributed hash for identifier
/// and symbol-table use.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Pjw32
    : NonCryptographicHashAlgorithm
{
    private const int HashLength = 4;
    private const uint HighBitsMask = 0xF0000000u;
    private const uint LowBitsMask = 0x0FFFFFFFu;
    private const int Shift = 28;

    private uint _workingHash;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pjw32" /> class.
    /// </summary>
    public Pjw32()
        : base(HashLength)
    {
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Append(ReadOnlySpan<byte> source)
    {
        uint v = this._workingHash;
        foreach (byte b in source)
        {
            v = (v << 4) + b;
            uint high = v & HighBitsMask;
            v ^= high >> Shift;
            v &= LowBitsMask;
        }

        this._workingHash = v;
    }

    /// <inheritdoc />
    public override void Reset() => this._workingHash = 0;

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32BigEndian(destination, this._workingHash);
}
