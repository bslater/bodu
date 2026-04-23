// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher.64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes the hash for the input data using the <c>Fletcher-64</c> hash algorithm. This variant performs a
/// non-cryptographic checksum calculation using two 32-bit accumulators to efficiently detect errors in byte sequences.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Fletcher64" /> is a non-cryptographic hash algorithm that computes a 64-bit checksum by iteratively
/// processing input bytes using two 32-bit rolling sums. It was introduced by Brian Kernighan and Dennis Ritchie and is
/// commonly used in applications such as network protocols, data validation, and embedded systems where simple and
/// efficient error detection is sufficient.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password
/// hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Fletcher64
    : Fletcher<Fletcher64>
{
    private const int FletcherHashSize = 64;

    /// <summary>
    /// Initialises a new instance of the <see cref="Fletcher64" /> class with a 64-bit hash size.
    /// </summary>
    public Fletcher64()
        : base(FletcherHashSize)
    {
    }
}
