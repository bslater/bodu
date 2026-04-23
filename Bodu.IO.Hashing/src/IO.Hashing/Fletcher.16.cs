// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher.16.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes the hash for the input data using the <c>Fletcher-16</c> hash algorithm. This variant performs a
/// non-cryptographic checksum calculation using two 8-bit accumulators to detect errors in small byte sequences.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Fletcher16" /> is a lightweight checksum algorithm that produces a 16-bit output by iteratively summing
/// input bytes using two rolling accumulators. It was introduced by Brian Kernighan and Dennis Ritchie and is commonly
/// used in network protocols, embedded systems, and file integrity checks where performance and simplicity are preferred
/// over cryptographic strength.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password
/// hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Fletcher16
    : Fletcher<Fletcher16>
{
    private const int FletcherHashSize = 16;

    /// <summary>
    /// Initialises a new instance of the <see cref="Fletcher16" /> class with a 16-bit hash size.
    /// </summary>
    public Fletcher16()
        : base(FletcherHashSize)
    {
    }
}
