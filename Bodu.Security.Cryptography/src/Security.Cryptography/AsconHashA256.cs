// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHashA256.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>ASCON-HASHA256</c> cryptographic hash algorithm as defined in NIST SP 800-232. Produces a 256-bit
/// (32-byte) digest using a reduced-round Ascon-p permutation during absorption over a 320-bit sponge state. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// ASCON-HASHA256 uses an 8-round Ascon-p permutation (Ascon-p8) during message block absorption and the full 12-round permutation
/// (Ascon-p12) during initialisation and output squeezing. The reduced absorption round count improves throughput for long messages
/// relative to <see cref="AsconHash256" /> at a reduced — though still substantial — security margin.
/// </para>
/// <para>
/// For the highest security margin, use <see cref="AsconHash256" />, which applies Ascon-p12 at every phase.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var hash = new AsconHashA256();
/// byte[] digest = hash.ComputeHash(message);
/// </code>
/// </example>
/// <seealso cref="AsconHash256" />
/// <seealso href="https://doi.org/10.6028/NIST.SP.800-232">NIST SP 800-232 (ASCON)</seealso>
public sealed class AsconHashA256
    : AsconHash<AsconHashA256>
{
    private const ulong Iv = 0x00400c0400000100UL;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsconHashA256" /> class.
    /// </summary>
    public AsconHashA256()
        : base(Iv, 8, "ASCON-HASHA256")
    { }
}
