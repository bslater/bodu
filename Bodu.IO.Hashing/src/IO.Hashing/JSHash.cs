// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JSHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using Justin Sobel's JSHash bitwise mixing function. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// For each input byte, JSHash updates the running hash as <c><![CDATA[hash ^= (hash << 5) + (hash >> 2) + byte]]></c>,
/// seeded with <c>0x4E67C6A7</c>. The finalized hash is written to the output buffer in little-endian byte order.
/// </para>
/// <para>
/// <strong>When to choose JSHash.</strong> JSHash is one of the small bitwise-mix hashes — pick it when a short,
/// dependency-free 32-bit hash is sufficient and the per-byte cost matters. Its distribution is roughly comparable to
/// <see cref="ApHash" />, <see cref="SDBM" />, and <see cref="Pjw32" />; the choice between them is usually driven by
/// interop with an existing system. For modern hash-table workloads prefer <see cref="Fnv1a32" /> (better avalanche,
/// same cost) or <see cref="MurmurHash3_32" /> (markedly better distribution on inputs longer than ~16 bytes).
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
/// using Bodu.IO.Hashing; using Bodu.IO.Hashing.Extensions; var js = new JSHash(); byte[]
/// digest = js.ComputeHash(System.Text.Encoding.UTF8.GetBytes("input-key"));
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class JSHash
    : NonCryptographicHashAlgorithm
{
    private const int HashLength = 4;
    private const uint Seed = 0x4E67C6A7;

    private uint _workingHash = Seed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JSHash" /> class seeded with the canonical <c>0x4E67C6A7</c>
    /// initial value.
    /// </summary>
    public JSHash()
        : base(HashLength)
    {
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        var v = _workingHash;
        foreach (var b in source)
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
