// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconCxof128.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a variable-length output using the <c>Ascon-CXOF128</c> customizable extendable output function (CXOF) as
/// defined in NIST SP 800-232. Supports an optional customization string that domain-separates outputs from
/// <see cref="AsconXof128" />. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Ascon-CXOF128 extends <see cref="AsconXof128" /> with a customization phase. The customization string <c>Z</c> is
/// absorbed before any message data, using a dedicated domain-separation constant to ensure that different
/// customization strings produce independent output functions. An empty customization string does <b>not</b> produce
/// the same output as <see cref="AsconXof128" />.
/// </para>
/// <para>
/// If no customization string is required, prefer <see cref="AsconXof128" /> directly. Use Ascon-CXOF128 when you need
/// distinct output functions for different application contexts (for example, key derivation vs. masking) from a single
/// primitive.
/// </para>
/// <para>
/// The lifecycle is:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// Optionally call <see cref="Customize" /> (before any <see cref="AsconXof{T}.Absorb" /> call).
/// </description>
/// </item>
/// <item>
/// <description>Call <see cref="AsconXof{T}.Absorb" /> zero or more times.</description>
/// </item>
/// <item>
/// <description>Call <see cref="AsconXof{T}.Squeeze" /> to produce output.</description>
/// </item>
/// <item>
/// <description>Call <see cref="AsconXof{T}.Initialize" /> to reset for reuse.</description>
/// </item>
/// </list>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Output size: variable, any positive multiple of 8 bits.</description>
/// </item>
/// <item>
/// <description>
/// Customization string: optional bytes absorbed before message data, domain-separates outputs.
/// </description>
/// </item>
/// <item>
/// <description>State: 320-bit sponge; rate: 8 bytes (64 bits).</description>
/// </item>
/// <item>
/// <description>Permutation: Ascon-p12 for every absorption, transition, and squeeze round.</description>
/// </item>
/// <item>
/// <description>Specification: NIST SP 800-232 (ASCON family).</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Ascon-CXOF128.</strong> Pick the customizable XOF when you need multiple independent output
/// streams from one primitive — KMAC-style domain separation per protocol layer, per-purpose KDFs (signing-key vs.
/// encryption-key vs. binding-tag), or hash-based DRBGs that must not collide across applications. For uncustomized XOF
/// output use <see cref="AsconXof128" />; for fixed-length 256-bit hashes use <see cref="AsconHash256" />; for the AEAD
/// member of the suite use <see cref="AsconAead128" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var cxof = new AsconCxof128();
/// cxof.Customize(Encoding.UTF8.GetBytes("my-app-v1"));
/// cxof.Absorb(message);
/// byte[] output = cxof.GetHash(32);
///]]>
/// </code>
/// </example>
/// <seealso cref="AsconXof128"/> <seealso href="https://doi.org/10.6028/NIST.SP.800-232">NIST SP 800-232 (ASCON)
/// </seealso>
public sealed class AsconCxof128
    : AsconXof<AsconCxof128>
{
    /// <summary>The pre-computed initial state word 0 for Ascon-CXOF128.</summary>
    /// <remarks>
    /// The five IV words are the result of applying Ascon-p12 to <c>[0x0000080000cc0004, 0, 0, 0, 0]</c> — the raw
    /// Ascon-CXOF128 IV defined in NIST SP 800-232. The values are verified against the ascon-c
    /// <c>LWC_CXOF_KAT_128_512</c> reference vectors (all 1089 rows).
    /// </remarks>
    private const ulong Iv0 = 0x675527c2a0e8de03UL;

    /// <summary>The pre-computed initial state word 1 for Ascon-CXOF128.</summary>
    private const ulong Iv1 = 0x43d12d7dc0377bbcUL;

    /// <summary>The pre-computed initial state word 2 for Ascon-CXOF128.</summary>
    private const ulong Iv2 = 0xe9901dec426e81b5UL;

    /// <summary>The pre-computed initial state word 3 for Ascon-CXOF128.</summary>
    private const ulong Iv3 = 0x2ab14907720780b6UL;

    /// <summary>The pre-computed initial state word 4 for Ascon-CXOF128.</summary>
    private const ulong Iv4 = 0x8f3f1d02d432bc46UL;

    /// <summary>Indicates whether a customization string has been absorbed via <see cref="Customize" />.</summary>
    private bool _customized;

    /// <summary>Indicates whether message data has been absorbed via <see cref="Absorb" />.</summary>
    private bool _absorbed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsconCxof128" /> class.
    /// </summary>
    public AsconCxof128()
        : base(Iv0, Iv1, Iv2, Iv3, Iv4, 12, "ASCON-CXOF128")
    { }

    /// <summary>
    /// Absorbs a customization string that domain-separates this instance from other uses of the same primitive. Must
    /// be called before any call to <see cref="AsconXof{T}.Absorb" />.
    /// </summary>
    /// <param name="customization">
    /// The customization string. May be empty to indicate the default (un-customized) domain. Calling this method with
    /// an empty span is distinct from not calling it at all.
    /// </param>
    /// <remarks>
    /// <para>
    /// Per NIST SP 800-232, the customization phase absorbs the bit length of <c>Z</c> encoded as a 64-bit
    /// little-endian integer, immediately followed by the bytes of <c>Z</c>, through the standard Ascon rate-8 sponge
    /// pipeline. The phase is then closed with Ascon padding and the full 12-round permutation before message
    /// absorption begins.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// <see cref="Customize" /> has already been called on this instance. Call <see cref="AsconXof{T}.Initialize" /> to
    /// reset.
    /// </exception>
    public void Customize(ReadOnlySpan<byte> customization)
    {
        ThrowIfDisposed();
        if (_customized || _absorbed)
            throw new InvalidOperationException(CryptoResourceStrings.Crypt_Invalid_XofCustomizationAfterAbsorb);

        // NIST SP 800-232 prefixes the customization string with its bit length as a 64-bit
        // little-endian integer, absorbed into the same sponge stream as Z. Absorbing the length
        // first is what domain-separates one customization from another.
        Span<byte> lengthPrefix = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(lengthPrefix, (ulong)customization.Length * 8);
        base.Absorb(lengthPrefix);
        base.Absorb(customization);

        // Close the customization phase with Ascon padding and the p12 transition so that message
        // absorption starts from a fully mixed state.
        FinalizeAbsorptionPhase();

        _customized = true;
    }

    /// <inheritdoc />
    public override void Absorb(ReadOnlySpan<byte> data)
    {
        _absorbed = true;
        base.Absorb(data);
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();
        _customized = false;
        _absorbed = false;
    }
}
