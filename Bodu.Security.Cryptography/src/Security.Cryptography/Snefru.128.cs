// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Snefru.128.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 128-bit (16-byte) hash using the <c>Snefru</c> hash algorithm by Ralph Merkle. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Snefru128" /> maintains a 4-word internal state and absorbs input in 48-byte blocks into a 512-bit
/// working buffer, applying 8 rounds of S-box substitution and word rotation per block. On finalization the state is
/// XOR-folded from the permuted buffer and serialized in big-endian byte order. See <see cref="Snefru{T}" /> for shared
/// background.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Output size: 128 bits (16 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Block size: 48 bytes; 4-word internal state; 8 rounds per block.
/// </description>
/// </item>
/// <item>
/// <description>
/// Status: <strong>broken</strong> — practical collision attacks are known.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Snefru128.</strong> For academic study and interop with archival data only. For new
/// security-sensitive work use SHA-2 or <see cref="Blake2b" />; for 128-bit non-cryptographic fingerprinting use a
/// <c>Bodu.IO.Hashing</c> algorithm such as <c>MurmurHash3_128</c>.
/// </para>
/// <note type="important">Snefru is considered broken and <b>not</b> suitable for password hashing, digital signatures,
/// or secure data integrity checks.</note>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
///
/// using var snefru = new Snefru128();
/// byte[] digest = snefru.ComputeHash(legacyMessage);
///]]>
/// </code>
/// </example>
/// <seealso cref="Snefru{T}"/> <seealso cref="Snefru256"/>
public sealed class Snefru128
    : Snefru<Snefru128>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Snefru128" /> class using a fixed 128-bit output size.
    /// </summary>
    public Snefru128()
        : base(128)
    {
    }
}
