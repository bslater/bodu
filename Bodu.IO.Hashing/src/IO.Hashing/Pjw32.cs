// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Pjw32.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using Peter J. Weinberger's PJW shift-and-fold algorithm (as described in
/// the "Dragon Book"). This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// For each input byte, the hash is shifted left by 4 bits and the byte added; any overflow into the top 4 bits is then
/// XOR-folded back into the low-order bits, producing a well-distributed hash for identifier and symbol-table use.
/// </para>
/// <para>
/// <strong>When to choose PJW32.</strong> PJW is the symbol-table hash from Aho/Sethi/Ullman's "Compilers: Principles,
/// Techniques, and Tools" (the "Dragon Book") and the precursor to the ELF hash family (<see cref="Elf64" />) — pick it
/// when interoperating with compiler-generated symbol tables or reproducing a digest from an algorithm based on the
/// Dragon Book formulation. For general-purpose hash-table keying prefer <see cref="Fnv1a32" /> (closely related but
/// better-distributing) or <see cref="MurmurHash3_32" /> (much better distribution on inputs longer than ~16 bytes).
/// </para>
/// <para>
/// <strong>Output and lifecycle.</strong> Produces a 32-bit (4-byte) digest in little-endian byte order.
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> is non-destructive; instances are
/// not thread-safe.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// var pjw = new Pjw32();
/// byte[] digest = pjw.ComputeHash(System.Text.Encoding.ASCII.GetBytes("symbol_name"));
///]]>
/// </code>
/// </example>
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
        var v = _workingHash;
        foreach (var b in source)
        {
            v = (v << 4) + b;
            var high = v & HighBitsMask;
            v ^= high >> Shift;
            v &= LowBitsMask;
        }

        _workingHash = v;
    }

    /// <inheritdoc />
    public override void Reset() => _workingHash = 0;

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32BigEndian(destination, _workingHash);
}
