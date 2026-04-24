// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SDBM.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using the SDBM algorithm popularised by the public-domain NDBM
/// database library. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// For each input byte, the running hash is updated as
/// <c><![CDATA[hash = byte + (hash << 6) + (hash << 16) - hash]]></c>, producing good distribution for short
/// and medium-length keys at minimal cost.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class SDBM
    : NonCryptographicHashAlgorithm
{
    private const int HashLength = 4;

    private uint _workingHash;

    /// <summary>
    /// Initializes a new instance of the <see cref="SDBM" /> class.
    /// </summary>
    public SDBM()
        : base(HashLength)
    {
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        uint v = _workingHash;
        foreach (byte b in source)
        {
            v = b + (v << 6) + (v << 16) - v;
        }

        _workingHash = v;
    }

    /// <inheritdoc />
    public override void Reset() => _workingHash = 0;

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32BigEndian(destination, _workingHash);
}
