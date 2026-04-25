// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHash256.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>ASCON-HASH256</c> cryptographic hash algorithm as defined in NIST SP 800-232. Produces a 256-bit
/// (32-byte) digest using the Ascon-p12 permutation over a 320-bit sponge state. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// ASCON-HASH256 uses the full 12-round Ascon-p permutation (Ascon-p12) at every phase: initialisation, per-block absorption, and
/// output squeezing. This makes it the most conservative ASCON hash variant with respect to security margin.
/// </para>
/// <para>
/// For applications where throughput is a higher priority than maximum security margin, consider
/// <see cref="AsconHashA256" />, which uses an 8-round permutation during absorption (Ascon-p8) and is otherwise identical.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var hash = new AsconHash256();
/// byte[] digest = hash.ComputeHash(message);
/// </code>
/// </example>
/// <seealso cref="AsconHashA256" />
/// <seealso href="https://doi.org/10.6028/NIST.SP.800-232">NIST SP 800-232 (ASCON)</seealso>
public sealed class AsconHash256
    : AsconHash<AsconHash256>
{
    private const ulong Iv = 0x00400c0000000100UL;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsconHash256" /> class.
    /// </summary>
    public AsconHash256()
        : base(Iv, 12, "ASCON-HASH256")
    { }
}
