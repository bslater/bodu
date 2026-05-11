// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Snefru.256.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 256-bit (32-byte) hash using the <c>Snefru</c> hash algorithm by Ralph Merkle. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Snefru256"/> maintains an 8-word internal state and absorbs input in 32-byte blocks into a 512-bit working buffer,
/// applying 8 rounds of S-box substitution and word rotation per block. On finalisation the state is XOR-folded from the permuted
/// buffer and serialised in big-endian byte order. See <see cref="Snefru{T}"/> for shared background.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Output size: 256 bits (32 bytes).</description></item>
///   <item><description>Block size: 32 bytes; 8-word internal state; 8 rounds per block.</description></item>
///   <item><description>Status: <strong>broken</strong> — practical collision attacks are known.</description></item>
/// </list>
/// <para>
/// <strong>When to choose Snefru256.</strong> Academic study and legacy interop only. For new security-sensitive
/// 256-bit hashing use SHA-256 or <see cref="Blake2b"/>; for fingerprinting use a 256-bit non-cryptographic hash
/// from <c>Bodu.IO.Hashing</c>.
/// </para>
/// <note type="important">Snefru is considered broken and <b>not</b> suitable for password hashing, digital signatures, or secure
/// data integrity checks.</note>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using Bodu.Security.Cryptography;
///
/// using var snefru = new Snefru256();
/// byte[] digest = snefru.ComputeHash(legacyMessage);
/// </code>
/// </example>
public sealed class Snefru256
    : Snefru<Snefru256>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Snefru256"/> class using a fixed 256-bit output size.
    /// </summary>
    public Snefru256()
        : base(256)
    {
    }
}
