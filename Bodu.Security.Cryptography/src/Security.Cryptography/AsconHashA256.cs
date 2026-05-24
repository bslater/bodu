// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHashA256.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>ASCON-HASHA256</c> cryptographic hash algorithm as defined in NIST SP 800-232. Produces
/// a 256-bit (32-byte) digest using a reduced-round Ascon-p permutation during absorption over a 320-bit sponge state.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// ASCON-HASHA256 uses an 8-round Ascon-p permutation (Ascon-p8) during message block absorption and the full 12-round
/// permutation (Ascon-p12) during the initial squeeze phase. Subsequent squeeze blocks also use Ascon-p8. The reduced
/// absorption round count improves throughput for long messages relative to <see cref="AsconHash256" /> at a reduced —
/// though still substantial — security margin.
/// </para>
/// <para>
/// For the highest security margin, use <see cref="AsconHash256" />, which applies Ascon-p12 at every phase.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Output size: 256 bits (32 bytes), fixed.
/// </description>
/// </item>
/// <item>
/// <description>
/// State: 320 bits sponge; rate: 64 bits (8 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Permutation: Ascon-p12 for initialization and the first squeeze; Ascon-p8 for absorption and subsequent squeezes.
/// </description>
/// </item>
/// <item>
/// <description>
/// Specification: NIST SP 800-232 (ASCON family).
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose ASCON-HASHA256.</strong> Pick this when ASCON-family interop is required and message
/// throughput matters more than the extra round-count margin of <see cref="AsconHash256" /> — typical for IoT gateways
/// and lightweight protocols that hash sustained streams of telemetry. For maximum margin use
/// <see cref="AsconHash256" />; for non-ASCON throughput-sensitive hashing on commodity hardware <see cref="Blake3" />
/// is faster still.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var hash = new AsconHashA256();
/// byte[] digest = hash.ComputeHash(message);
///]]>
/// </code>
/// </example>
/// <seealso cref="AsconHash256"/> <seealso href="https://doi.org/10.6028/NIST.SP.800-232">NIST SP 800-232 (ASCON)
/// </seealso>
public sealed class AsconHashA256
    : AsconHash<AsconHashA256>
{
    // Pre-computed initial state for ASCON-HASHA256 (NIST SP 800-232).
    // These five words are the result of applying Ascon-p12 to [raw_IV, 0, 0, 0, 0].
    // Source: ascon-c opt64/constants.h, ASCON_HASHA_IV0..IV4.
    private const ulong Iv0 = 0xe2ffb4d17ffcadc5UL;
    private const ulong Iv1 = 0xdd364b655fa88cebUL;
    private const ulong Iv2 = 0xdcaabe85a70319d2UL;
    private const ulong Iv3 = 0xd98f049404be3214UL;
    private const ulong Iv4 = 0xca8c9d516e8a2221UL;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsconHashA256" /> class.
    /// </summary>
    public AsconHashA256()
        : base(Iv0, Iv1, Iv2, Iv3, Iv4, 8, "ASCON-HASHA256")
    { }
}
