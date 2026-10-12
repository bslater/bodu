// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHashA256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>ASCON-HASHA256</c> cryptographic hash algorithm, the round-reduced Hasha variant of the
/// Ascon v1.2 submission. Produces a 256-bit (32-byte) digest using a reduced-round Ascon-p permutation over a 320-bit
/// sponge state. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// ASCON-HASHA256 uses an 8-round Ascon-p permutation (Ascon-p8) during message block absorption and the full 12-round
/// permutation (Ascon-p12) during the initial squeeze phase. Subsequent squeeze blocks also use Ascon-p8. The reduced
/// absorption round count improves throughput for long messages relative to <see cref="AsconHash256" /> at a reduced -
/// though still substantial - security margin.
/// </para>
/// <para>
/// For the highest security margin, use <see cref="AsconHash256" />, which applies Ascon-p12 at every phase.
/// </para>
/// <para>
/// <strong>Standard status.</strong> NIST SP 800-232 standardizes ASCON-HASH256, not this variant: ASCON-HASHA256 comes
/// from the Ascon v1.2 lightweight-cryptography submission, which SP 800-232 did not adopt. Its initial state and
/// digests match the ascon-c reference implementation. Where a standardized algorithm is required, use
/// <see cref="AsconHash256" />.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Output size: 256 bits (32 bytes), fixed.</description>
/// </item>
/// <item>
/// <description>State: 320 bits sponge; rate: 64 bits (8 bytes).</description>
/// </item>
/// <item>
/// <description>
/// Permutation: Ascon-p12 for initialization and the first squeeze; Ascon-p8 for absorption and subsequent squeezes.
/// </description>
/// </item>
/// <item>
/// <description>Specification: the Ascon v1.2 submission (Hasha); not part of NIST SP 800-232.</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose ASCON-HASHA256.</strong> Pick this when interop with the Ascon v1.2 Hasha variant is
/// required and message throughput matters more than the extra round-count margin of <see cref="AsconHash256" /> - typical for IoT gateways
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
/// <seealso cref="AsconHash256"/>
public sealed class AsconHashA256
    : AsconHash
{
    /// <summary>The first word of the pre-computed initial state for ASCON-HASHA256 (Ascon v1.2).</summary>
    /// <remarks>
    /// The five IV words are the result of applying Ascon-p12 to <c>[raw_IV, 0, 0, 0, 0]</c>, sourced from ascon-c
    /// <c>opt64/constants.h</c> (<c>ASCON_HASHA_IV0..IV4</c>).
    /// </remarks>
    private const ulong Iv0 = 0xe2ffb4d17ffcadc5UL;

    /// <summary>The second word of the pre-computed initial state for ASCON-HASHA256.</summary>
    private const ulong Iv1 = 0xdd364b655fa88cebUL;

    /// <summary>The third word of the pre-computed initial state for ASCON-HASHA256.</summary>
    private const ulong Iv2 = 0xdcaabe85a70319d2UL;

    /// <summary>The fourth word of the pre-computed initial state for ASCON-HASHA256.</summary>
    private const ulong Iv3 = 0xd98f049404be3214UL;

    /// <summary>The fifth word of the pre-computed initial state for ASCON-HASHA256.</summary>
    private const ulong Iv4 = 0xca8c9d516e8a2221UL;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsconHashA256" /> class.
    /// </summary>
    public AsconHashA256()
        : base(Iv0, Iv1, Iv2, Iv3, Iv4, 8, "ASCON-HASHA256")
    { }
}
