// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a variable-length output using the <c>Ascon-XOF128</c> extendable output function (XOF) as defined in
/// NIST SP 800-232. Uses the Ascon-p8 permutation during absorption and squeezing over a 320-bit sponge state. This class
/// cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Ascon-XOF128 is the NIST SP 800-232 standardised extendable output function. It uses an 8-byte (64-bit) rate, Ascon-p12
/// for the absorption-to-squeeze transition, and Ascon-p8 between absorption and squeeze blocks. This provides a security
/// strength of 128 bits against collision, preimage, and second-preimage attacks (for output lengths ≥ 32 bytes).
/// </para>
/// <para>
/// For a fixed-length 256-bit digest, consider <see cref="AsconHash256" />. For output with a customization string, use
/// <see cref="AsconCxof128" />.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Output size: variable, any positive multiple of 8 bits.</description></item>
///   <item><description>State: 320-bit sponge; rate: 8 bytes (64 bits).</description></item>
///   <item><description>Permutation: Ascon-p12 for absorption-to-squeeze transition; Ascon-p8 elsewhere.</description></item>
///   <item><description>Security level: 128 bits (for outputs ≥ 32 bytes).</description></item>
///   <item><description>Specification: NIST SP 800-232 (ASCON family).</description></item>
/// </list>
/// <para>
/// <strong>When to choose Ascon-XOF128.</strong> Pick this when an extendable-output function is required
/// inside an Ascon-based protocol — variable-length KDF outputs, hash-based DRBGs, or any context that needs
/// more than the 256-bit fixed output of <see cref="AsconHash256"/>. For non-Ascon settings <see cref="Shake"/>
/// (NIST FIPS 202) or <see cref="Blake3"/>'s XOF mode is faster on commodity hardware. For domain-separated
/// output families (KMAC-style or per-purpose XOF instances), use <see cref="AsconCxof128"/> to bind a
/// customization string into the initial state.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var xof = new AsconXof128();
/// xof.Absorb(message);
/// byte[] output = xof.GetHash(64); // Produce 64 bytes of output
/// </code>
/// </example>
/// <seealso cref="AsconCxof128" />
/// <seealso cref="AsconHash256" />
/// <seealso href="https://doi.org/10.6028/NIST.SP.800-232">NIST SP 800-232 (ASCON)</seealso>
public sealed class AsconXof128 : AsconXof<AsconXof128>
{
    // Pre-computed initial state for Ascon-XOF128 (NIST SP 800-232).
    // These five words are the result of applying Ascon-p12 to [raw_IV, 0, 0, 0, 0].
    // Source: NIST SP 800-232 / ascon-c opt64/constants.h (ASCON_XOF128_IV0..IV4).
    private const ulong Iv0 = 0xb57e273b814cd416UL;
    private const ulong Iv1 = 0x2b51042562ae2420UL;
    private const ulong Iv2 = 0x66a3a7768ddf2218UL;
    private const ulong Iv3 = 0x5aad0a7a8153650cUL;
    private const ulong Iv4 = 0x4f3e0e32539493b6UL;

    /// <summary>
    /// Initialises a new instance of the <see cref="AsconXof128" /> class.
    /// </summary>
    public AsconXof128()
        : base(Iv0, Iv1, Iv2, Iv3, Iv4, 8, "ASCON-XOF128")
    { }
}
