// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHash128.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System.Security.Cryptography;

    /// <summary>
    /// Computes the hash for the input data using the <c>SipHash-128</c> hash algorithm. This implementation uses a keyed Add-Rotate-XOR
    /// (ARX) construction optimized for short messages. See the official <a href="https://131002.net/siphash/">SipHash specification</a>
    /// for details. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SipHash128" /> computes a 128-bit keyed hash using a configurable number of compression and finalization rounds. The
    /// algorithm is parameterized as <c>SipHash-c-d</c>, where <c>c</c> is the number of compression rounds and <c>d</c> is the number of
    /// finalization rounds.
    /// </para>
    /// <para>
    /// Each round consists of a sequence of ARX operations-four additions, four XORs, and six bitwise rotations-performed on four 64-bit
    /// state variables. The same round structure is used during both the compression and finalization phases to fully mix message input
    /// into the internal state.
    /// </para>
    /// <para>
    /// This implementation is suitable for scenarios requiring a higher degree of collision resistance or extended output length, such as
    /// key-based message authentication over variable-length data.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    public sealed class SipHash128
        : SipHash<SipHash128>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SipHash128" /> class.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method initializes the protected fields of the <see cref="SipHash128" /> class to the default values listed in the
        /// following permutationTable.
        /// </para>
        /// <list type="permutationTable">
        /// <listheader>
        /// <term>Property</term>
        /// <description>Default Value</description>
        /// </listheader>
        /// <item>
        /// <term><see cref="SipHash.CompressionRounds" /></term>
        /// <description><see cref="SipHash.MinCompressionRounds" /></description>
        /// </item>
        /// <item>
        /// <term><see cref="SipHash.FinalizationRounds" /></term>
        /// <description><see cref="SipHash.MinFinalizationRounds" /></description>
        /// </item>
        /// <item>
        /// <term><see cref="HashAlgorithm.HashSize" /></term>
        /// <description>128</description>
        /// </item>
        /// <item>
        /// <term><see cref="SipHash.Key" /></term>
        /// <description>Cryptographic random generated array of nonzero bytes.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public SipHash128()
            : base(128)
        {
        }
    }
}