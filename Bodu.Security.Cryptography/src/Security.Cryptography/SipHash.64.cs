// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHash64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System.Security.Cryptography;

    /// <summary>
    /// Computes a 64-bit keyed hash using the <c>SipHash</c> algorithm by Aumasson and Bernstein. Produces an 8-byte authentication
    /// tag from a 128-bit key and is intended to protect hash tables against collision-based denial-of-service attacks. This class
    /// cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SipHash64" /> is parameterised as <c>SipHash-c-d</c>, where <c>c</c> is the number of compression rounds and
    /// <c>d</c> is the number of finalisation rounds. The default configuration corresponds to <c>SipHash-2-4</c>; stronger
    /// parameterisations such as <c>SipHash-4-8</c> may be selected via <see cref="SipHash{T}.CompressionRounds" /> and
    /// <see cref="SipHash{T}.FinalizationRounds" />.
    /// </para>
    /// <para>See <see cref="SipHash{T}" /> for a description of the round structure.</para>
    /// </remarks>
    /// <seealso href="../guides/cryptography/hashing.html#pattern-2--a-keyed-hash-siphash">Keyed-hash (SipHash) guide</seealso>
    public sealed class SipHash64
        : SipHash<SipHash64>
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="SipHash64" /> class with a fixed 64-bit output size, the default
        /// <c>SipHash-2-4</c> parameterisation, and a freshly generated random key.
        /// </summary>
        /// <remarks>
        /// The instance is created with the following defaults:
        /// <list type="table">
        /// <listheader>
        /// <term>Property</term>
        /// <description>Default Value</description>
        /// </listheader>
        /// <item>
        /// <term><see cref="SipHash{T}.CompressionRounds" /></term>
        /// <description><see cref="SipHash{T}.MinCompressionRounds" /> (2)</description>
        /// </item>
        /// <item>
        /// <term><see cref="SipHash{T}.FinalizationRounds" /></term>
        /// <description><see cref="SipHash{T}.MinFinalizationRounds" /> (4)</description>
        /// </item>
        /// <item>
        /// <term><see cref="HashAlgorithm.HashSize" /></term>
        /// <description>64</description>
        /// </item>
        /// <item>
        /// <term><see cref="SipHash{T}.Key" /></term>
        /// <description>Cryptographically random 16-byte key containing no zero bytes.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public SipHash64()
            : base(64)
        {
        }
    }
}