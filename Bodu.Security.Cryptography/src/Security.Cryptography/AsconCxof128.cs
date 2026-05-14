// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconCxof128.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a variable-length output using the <c>Ascon-CXOF128</c> customizable extendable output function (CXOF) as
/// defined in NIST SP 800-232. Supports an optional customization string that domain-separates outputs from
/// <see cref="AsconXof128"/>. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Ascon-CXOF128 extends <see cref="AsconXof128"/> with a customization phase. The customization string <c>Z</c> is
/// absorbed before any message data, using a dedicated domain-separation constant to ensure that different customization
/// strings produce independent output functions. An empty customization string does <b>not</b> produce the same output as
/// <see cref="AsconXof128"/>.
/// </para>
/// <para>
/// If no customization string is required, prefer <see cref="AsconXof128"/> directly. Use Ascon-CXOF128 when you need
/// distinct output functions for different application contexts (for example, key derivation vs. masking) from a single
/// primitive.
/// </para>
/// <para>
/// The lifecycle is:
/// </para>
/// <list type="number">
/// <item><description>Optionally call <see cref="Customize"/> (before any <see cref="AsconXof{T}.Absorb"/> call).</description></item>
/// <item><description>Call <see cref="AsconXof{T}.Absorb"/> zero or more times.</description></item>
/// <item><description>Call <see cref="AsconXof{T}.Squeeze"/> to produce output.</description></item>
/// <item><description>Call <see cref="AsconXof{T}.Initialize"/> to reset for reuse.</description></item>
/// </list>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Output size: variable, any positive multiple of 8 bits.</description></item>
///   <item><description>Customization string: optional bytes absorbed before message data, domain-separates outputs.</description></item>
///   <item><description>State: 320-bit sponge; rate: 8 bytes (64 bits).</description></item>
///   <item><description>Permutation: Ascon-p12 for transitions; Ascon-p8 between absorption rounds.</description></item>
///   <item><description>Specification: NIST SP 800-232 (ASCON family).</description></item>
/// </list>
/// <para>
/// <strong>When to choose Ascon-CXOF128.</strong> Pick the customizable XOF when you need multiple
/// independent output streams from one primitive — KMAC-style domain separation per protocol layer,
/// per-purpose KDFs (signing-key vs. encryption-key vs. binding-tag), or hash-based DRBGs that must not
/// collide across applications. For uncustomized XOF output use <see cref="AsconXof128"/>; for fixed-length
/// 256-bit hashes use <see cref="AsconHash256"/>; for the AEAD member of the suite use
/// <see cref="AsconAead128"/>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var cxof = new AsconCxof128();
/// cxof.Customize(Encoding.UTF8.GetBytes("my-app-v1"));
/// cxof.Absorb(message);
/// byte[] output = cxof.GetHash(32);
/// </code>
/// </example>
/// <seealso cref="AsconXof128"/>
/// <seealso href="https://doi.org/10.6028/NIST.SP.800-232">NIST SP 800-232 (ASCON)</seealso>
public sealed class AsconCxof128
    : AsconXof<AsconCxof128>
{
    // Pre-computed initial state for Ascon-CXOF128 (NIST SP 800-232).
    // These five words are the result of applying Ascon-p12 to [raw_IV, 0, 0, 0, 0].
    // Source: NIST SP 800-232 / ascon-c opt64/constants.h (ASCON_CXOF128_IV0..IV4).
    private const ulong Iv0 = 0x3e228512a6849c43UL;
    private const ulong Iv1 = 0x3b0e9f7a5e1f9a92UL;
    private const ulong Iv2 = 0x77be5ee5826c2fc0UL;
    private const ulong Iv3 = 0x1eca27ad2e7e3636UL;
    private const ulong Iv4 = 0x7d0765b2c5a6d428UL;

    private bool _customized;
    private bool _absorbed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsconCxof128"/> class.
    /// </summary>
    public AsconCxof128()
        : base(Iv0, Iv1, Iv2, Iv3, Iv4, 8, "ASCON-CXOF128")
    { }

    /// <summary>
    /// Absorbs a customization string that domain-separates this instance from other uses of the same primitive. Must be
    /// called before any call to <see cref="AsconXof{T}.Absorb"/>.
    /// </summary>
    /// <param name="customization">
    /// The customization string. May be empty to indicate the default (un-customized) domain. Calling this method with an
    /// empty span is distinct from not calling it at all.
    /// </param>
    /// <remarks>
    /// <para>
    /// Per NIST SP 800-232, the customization string is absorbed into the sponge using the standard Ascon padding rule,
    /// followed by a domain-separation constant (XOR of 1 into state word S4) that distinguishes the customized initial
    /// state from the un-customized one.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// <see cref="Customize"/> has already been called on this instance. Call <see cref="AsconXof{T}.Initialize"/> to reset.
    /// </exception>
    public void Customize(ReadOnlySpan<byte> customization)
    {
        this.ThrowIfDisposed();
        if (this._customized || this._absorbed)
            throw new InvalidOperationException(CryptoResourceStrings.CryptographicException_XofCustomizationAfterAbsorb);

        // Absorb Z through the standard sponge pipeline, then finalize the customization
        // phase with Ascon padding and pb rounds to close the customization domain.
        base.Absorb(customization);
        this.FinalizeAbsorptionPhase();

        // Domain separation: XOR 1 into S4 to distinguish the customized initial state
        // from the message-absorption state (per NIST SP 800-232 Section 2.3).
        this.XorS4(1UL);

        this._customized = true;
    }

    /// <inheritdoc />
    public override void Absorb(ReadOnlySpan<byte> data)
    {
        this._absorbed = true;
        base.Absorb(data);
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();
        this._customized = false;
        this._absorbed = false;
    }
}
