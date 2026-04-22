// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHash128.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 128-bit keyed hash using the <c>SipHash</c> algorithm by Aumasson and Bernstein. Produces a 16-byte authentication tag
/// from a 128-bit key, offering increased collision resistance over <see cref="SipHash64" />. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SipHash128" /> is parameterised as <c>SipHash-c-d</c>, where <c>c</c> is the number of compression rounds and
/// <c>d</c> is the number of finalisation rounds. The default configuration corresponds to <c>SipHash-2-4</c>.
/// </para>
/// <para>See <see cref="SipHash{T}" /> for a description of the round structure.</para>
/// </remarks>
public sealed class SipHash128
    : SipHash<SipHash128>
{
    /// <summary>
    /// Initialises a new instance of the <see cref="SipHash128" /> class with a fixed 128-bit output size, the default
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
    /// <description>128</description>
    /// </item>
    /// <item>
    /// <term><see cref="SipHash{T}.Key" /></term>
    /// <description>Cryptographically random 16-byte key containing no zero bytes.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public SipHash128()
        : base(128)
    {
    }
}
