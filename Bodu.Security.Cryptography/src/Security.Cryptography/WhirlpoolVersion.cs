// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WhirlpoolVersion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Specifies the published revision of the <see cref="Whirlpool" /> hash algorithm selected by
/// <see cref="Whirlpool.Version" />.
/// </summary>
/// <remarks>
/// <para>
/// Whirlpool was proposed by Paulo S. L. M. Barreto and Vincent Rijmen in 2000 and revised twice before being
/// standardized in <c>ISO/IEC 10118-3</c>. The three published revisions use the same overall Merkle–Damgård
/// construction around the internal <c>W</c> block cipher but differ in the non-linear substitution layer and the
/// linear diffusion matrix:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="WhirlpoolInfo1" /> — the <c>Whirlpool-0</c> function as originally submitted to <c>NESSIE</c> in 2000. It
/// uses the original pseudo-random S-box and the original diffusion matrix.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="WhirlpoolInfo2" /> — the <c>Whirlpool-T</c> revision published in 2001. It replaces the S-box with the
/// structured mini-box construction while retaining the original diffusion matrix.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="WhirlpoolInfo3" /> — the final <c>Whirlpool</c> function adopted by <c>ISO/IEC 10118-3</c> in 2003. It
/// retains the mini-box S-box introduced in <c>Whirlpool-T</c> and uses the revised diffusion matrix with branch number
/// nine.
/// </description>
/// </item>
/// </list>
/// <note type="important">Only <see cref="WhirlpoolInfo3" /> is the standardized variant. The earlier revisions are
/// provided for interoperability with legacy data and academic study.</note>
/// </remarks>
public enum WhirlpoolVersion
{
    /// <summary>
    /// The original <c>Whirlpool-0</c> function submitted to <c>NESSIE</c> in 2000, combining the original
    /// pseudo-random S-box with the original diffusion matrix.
    /// </summary>
    WhirlpoolInfo1,

    /// <summary>
    /// The <c>Whirlpool-T</c> revision published in 2001, which adopts the structured mini-box S-box but retains the
    /// original diffusion matrix.
    /// </summary>
    WhirlpoolInfo2,

    /// <summary>
    /// The final <c>Whirlpool</c> function adopted by <c>ISO/IEC 10118-3</c> in 2003, pairing the mini-box S-box with
    /// the revised diffusion matrix.
    /// </summary>
    WhirlpoolInfo3,
}
