// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExtendedSymmetricAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the base class for the library's <see cref="SymmetricAlgorithm" /> implementations that support the
/// extended cipher-mode and padding catalogues (<see cref="CipherModeKind" /> / <see cref="PaddingModeKind" />) beyond
/// the framework <see cref="CipherMode" /> / <see cref="PaddingMode" /> enumerations.
/// </summary>
/// <remarks>
/// <para>
/// The framework <see cref="SymmetricAlgorithm.Mode" /> and <see cref="SymmetricAlgorithm.Padding" /> properties can
/// only express the members of <see cref="CipherMode" /> and <see cref="PaddingMode" />. This base adds the
/// <see cref="BlockMode" /> and <see cref="BlockPadding" /> counterparts over the extended
/// <see cref="CipherModeKind" /> / <see cref="PaddingModeKind" /> catalogues (which include, for example,
/// <see cref="CipherModeKind.CTR" /> and <see cref="PaddingModeKind.ISO7816_4" />) and keeps each pair bidirectionally
/// synchronized: assigning either member of a pair updates the other whenever an equivalent value exists, so consumers
/// inspecting the framework property always observe a consistent configuration.
/// </para>
/// <para>
/// Transform creation in derived classes reads <see cref="BlockMode" /> and <see cref="BlockPadding" />, so both the
/// framework properties and the extended properties are honoured regardless of which surface the caller configures.
/// </para>
/// </remarks>
/// <seealso cref="CipherModeKind"/> <seealso cref="PaddingModeKind"/> <seealso cref="BlockCipherTransform"/>
public abstract class ExtendedSymmetricAlgorithm
    : SymmetricAlgorithm
{
    /// <summary>The extended block cipher mode used when creating encryptors and decryptors.</summary>
    private CipherModeKind _blockMode = CipherModeKind.CBC;

    /// <summary>The extended padding mode used when creating encryptors and decryptors.</summary>
    private PaddingModeKind _blockPadding = PaddingModeKind.PKCS7;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedSymmetricAlgorithm" /> class.
    /// </summary>
    protected ExtendedSymmetricAlgorithm()
    {
    }

    /// <summary>
    /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="CipherModeKind" /> values. The default is <see cref="CipherModeKind.CBC" />.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode" /> property for use with
    /// <see cref="BlockCipherModeFactory" /> and the extended set of modes it supports, including
    /// <see cref="CipherModeKind.CTR" /> and <see cref="CipherModeKind.OFB" />, which are not available via the
    /// standard <see cref="CipherMode" /> enumeration.
    /// </para>
    /// <para>
    /// When the assigned value has a matching member in <see cref="CipherMode" /> (for example, CBC, ECB, CFB), the
    /// inherited <see cref="SymmetricAlgorithm.Mode" /> is kept in sync so that consumers inspecting the base property
    /// observe a consistent mode. Extended modes with no <see cref="CipherMode" /> equivalent leave the base property
    /// unchanged.
    /// </para>
    /// </remarks>
    public CipherModeKind BlockMode
    {
        get => _blockMode;
        set
        {
            _blockMode = value;

            // Keep the inherited Mode in sync where a direct CipherMode equivalent exists. Extended modes without a
            // mapping (e.g. CTR, OFB) intentionally leave ModeValue untouched.
            if (Enum.TryParse<CipherMode>(value.ToString(), out CipherMode mode) &&
                Enum.IsDefined(mode))
            {
                ModeValue = mode;
            }
        }
    }

    /// <summary>
    /// Gets or sets the extended padding mode used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="PaddingModeKind" /> values. The default is <see cref="PaddingModeKind.PKCS7" />.
    /// </value>
    /// <remarks>
    /// When the assigned value has a matching member in <see cref="PaddingMode" /> (for example, PKCS7, Zeros, None),
    /// the inherited <see cref="SymmetricAlgorithm.Padding" /> is kept in sync. Extended modes with no
    /// <see cref="PaddingMode" /> equivalent (such as <see cref="PaddingModeKind.ISO7816_4" />) leave the base property
    /// unchanged.
    /// </remarks>
    public PaddingModeKind BlockPadding
    {
        get => _blockPadding;
        set
        {
            _blockPadding = value;
            if (Enum.TryParse<PaddingMode>(value.ToString(), out PaddingMode mode) && Enum.IsDefined(mode))
                PaddingValue = mode;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Also synchronizes <see cref="BlockPadding" /> when the assigned value has a matching member in
    /// <see cref="PaddingModeKind" />.
    /// </remarks>
    public override PaddingMode Padding
    {
        get => base.Padding;
        set
        {
            base.Padding = value;
            if (Enum.TryParse<PaddingModeKind>(value.ToString(), out PaddingModeKind bpm) && Enum.IsDefined(bpm))
                _blockPadding = bpm;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Also synchronizes <see cref="BlockMode" /> so that encryptor and decryptor creation, which read
    /// <see cref="BlockMode" />, honour the assigned value. Every <see cref="CipherMode" /> member has a matching
    /// <see cref="CipherModeKind" /> member, so the synchronization always succeeds.
    /// </remarks>
    public override CipherMode Mode
    {
        get => base.Mode;
        set
        {
            base.Mode = value;
            if (Enum.TryParse<CipherModeKind>(value.ToString(), out CipherModeKind bm) && Enum.IsDefined(bm))
                _blockMode = bm;
        }
    }
}
