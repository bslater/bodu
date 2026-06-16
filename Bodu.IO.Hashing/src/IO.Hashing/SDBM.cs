// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SDBM.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using the SDBM algorithm popularized by the public-domain NDBM database
/// library. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// For each input byte, the running hash is updated as
/// <c><![CDATA[hash = byte + (hash << 6) + (hash << 16) - hash]]></c>, producing good distribution for short and
/// medium-length keys at minimal cost.
/// </para>
/// <para>
/// <strong>When to choose SDBM.</strong> SDBM is the public-domain hash from the NDBM/SDBM database library and the
/// historical default in many Unix tools — pick it when interoperating with code that has standardized on the SDBM mix,
/// or when a small, dependency-free 32-bit hash is sufficient. Empirically SDBM gives slightly better distribution than
/// <see cref="Bernstein" /> and <see cref="ApHash" /> on short keys at the same per-byte cost. For modern hash-table
/// workloads prefer <see cref="Fnv1a32" /> (better avalanche, same cost) or <see cref="MurmurHash3_32" /> (markedly
/// better distribution on inputs longer than ~16 bytes).
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
/// var sdbm = new SDBM();
/// byte[] digest = sdbm.ComputeHash(System.Text.Encoding.UTF8.GetBytes("dbm-key"));
///]]>
/// </code>
/// </example>
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
