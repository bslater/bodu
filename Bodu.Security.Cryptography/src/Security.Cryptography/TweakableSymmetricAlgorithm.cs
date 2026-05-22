// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for tweakable symmetric algorithms, which accept an additional tweak value in
/// addition to the key and initialization vector.
/// </summary>
/// <remarks>
/// <para>
/// This class extends <see cref="SymmetricAlgorithm" /> with a <see cref="Tweak" /> property, a
/// <see cref="LegalTweakSizes" /> enumeration, and tweak-aware <c>CreateEncryptor</c> / <c>CreateDecryptor</c>
/// overloads. It is intended for ciphers such as Threefish that take a tweak input as part of their cryptographic
/// contract.
/// </para>
/// <para>
/// Derived classes must override <see cref="CreateEncryptor(byte[], byte[], byte[])" />,
/// <see cref="CreateDecryptor(byte[], byte[], byte[])" />, and <see cref="GenerateTweak" />.
/// </para>
/// <para>
/// <strong>What is a tweak?</strong> A tweak is a third keying input alongside key and IV — a per-message (or
/// per-position) value that varies the cipher's behavior without renegotiating the key. Tweaks are what make tweakable
/// block ciphers a natural fit for disk encryption (the sector number is the tweak), authenticated modes built from the
/// cipher (the message position is the tweak), and protocols that need to bind ciphertext to a position or message
/// identifier.
/// </para>
/// <para>
/// <strong>Concrete implementations.</strong> The library ships <see cref="Threefish256" />,
/// <see cref="Threefish512" />, and <see cref="Threefish1024" /> as the production tweakable ciphers (Threefish is the
/// cipher under the hood of Skein). The <see cref="Serpent256" />/<see cref="Serpent512" />/<see cref="Serpent1024" />
/// wide-block variants are also tweakable but non-standard — see their headers for the experimental-only caveat.
/// </para>
/// <para>
/// <strong>Companion try-pattern helpers.</strong> The
/// <see cref="Bodu.Security.Cryptography.Extensions.TweakableSymmetricAlgorithmExtensions" /> class adds
/// <c>TryCreateEncryptor</c> and <c>TryCreateDecryptor</c> wrappers that return <see langword="false" /> when the
/// supplied key/IV/tweak combination is invalid — useful when keying material is user-supplied.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Construct via a concrete derivative — Threefish is the production tweakable cipher family.
/// using TweakableSymmetricAlgorithm alg = new Threefish256();
/// alg.GenerateKey();
/// alg.GenerateIV();
/// alg.GenerateTweak();      // unique to TweakableSymmetricAlgorithm — supplies the per-message tweak
///
/// // CreateEncryptor / CreateDecryptor accept the additional tweak argument.
/// using ICryptoTransform encryptor = alg.CreateEncryptor(alg.Key, alg.IV, alg.Tweak);
/// using ICryptoTransform decryptor = alg.CreateDecryptor(alg.Key, alg.IV, alg.Tweak);
///
/// // The companion try-pattern wrappers gate over user-supplied keying material.
/// if (alg.TryCreateEncryptor(userKey, userIv, userTweak, out ICryptoTransform? safe))
/// {
///     using (safe) { /* encrypt */ }
/// }
///]]>
/// </example>
/// <seealso cref="Threefish256"/> <seealso cref="Threefish512"/> <seealso cref="Threefish1024"/>
/// <seealso cref="Bodu.Security.Cryptography.Extensions.TweakableSymmetricAlgorithmExtensions"/>
public abstract class TweakableSymmetricAlgorithm
    : System.Security.Cryptography.SymmetricAlgorithm
{
    /// <summary>
    /// Specifies the tweak sizes, in bits, that are supported by the algorithm.
    /// </summary>
    /// <remarks>
    /// Each <see cref="KeySizes" /> entry expresses its minimum, maximum, and skip values in bits. This backing field
    /// defines the range of acceptable tweak sizes for a given algorithm and is used internally by the
    /// <see cref="LegalTweakSizes" /> property.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.NamingRules",
        "SA1306:Field names should begin with lower-case letter",
        Justification = "The field intentionally follows the protected field naming pattern used by HashAlgorithm, such as HashSizeValue, because it forms part of the inherited algorithm-state surface for derived cryptographic types.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Follows the BCL protected field naming convention (like LegalKeySizesValue on SymmetricAlgorithm) so that derived tweakable cipher types can read the legal tweak sizes without virtual dispatch.")]
    [MaybeNull]
    protected KeySizes[] LegalTweakSizesValue = null!;

    /// <summary>
    /// Stores the currently configured tweak size, in bits, for the algorithm instance.
    /// </summary>
    /// <remarks>
    /// This backing field represents the effective size of the tweak currently configured via <see cref="TweakSize" />
    /// or <see cref="Tweak" />.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
       "StyleCop.CSharp.NamingRules",
       "SA1306:Field names should begin with lower-case letter",
       Justification = "The field intentionally follows the protected field naming pattern used by HashAlgorithm, such as HashSizeValue, because it forms part of the inherited algorithm-state surface for derived cryptographic types.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Follows the BCL protected field naming convention (like KeySizeValue on SymmetricAlgorithm) so that derived tweakable cipher types can read the tweak size without virtual dispatch.")]
    protected int TweakSizeValue = 0;

    /// <summary>
    /// Stores the current tweak value used by the algorithm.
    /// </summary>
    /// <remarks>
    /// The value stored here is used internally and may be cleared or regenerated via <see cref="Dispose" />,
    /// <see cref="GenerateTweak" />, or changes to <see cref="TweakSize" />. Defensive copies are used when accessing
    /// or assigning through the <see cref="Tweak" /> property.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.NamingRules",
        "SA1306:Field names should begin with lower-case letter",
        Justification = "The field intentionally follows the protected field naming pattern used by HashAlgorithm, such as HashSizeValue, because it forms part of the inherited algorithm-state surface for derived cryptographic types.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Follows the BCL protected field naming convention (like IVValue on SymmetricAlgorithm) so that derived tweakable cipher types can read and clear tweak material directly without virtual dispatch.")]
    protected byte[]? TweakValue = null;

    private bool _disposed = false;

    /// <summary>
    /// Gets the tweak sizes, in bits, that are supported by the symmetric algorithm.
    /// </summary>
    /// <value>
    /// An array of <see cref="KeySizes" /> instances indicating the valid minimum, maximum, and step sizes supported
    /// for tweak values, all expressed in bits. Divide by 8 to obtain the equivalent byte lengths.
    /// </value>
    /// <returns>The legal tweak sizes, in bits.</returns>
    /// <remarks>
    /// This property returns a cloned copy of the internal tweak size definitions to prevent external modification.
    /// Override this property in derived types to define custom tweak size support for a specific algorithm.
    /// </remarks>
    public virtual KeySizes[] LegalTweakSizes =>
        this.LegalTweakSizesValue is null
            ? []
            : (KeySizes[])this.LegalTweakSizesValue.Clone();

    /// <summary>
    /// Gets or sets the tweak value for the symmetric algorithm.
    /// </summary>
    /// <value>
    /// A byte array representing the tweak to be used. Must conform to one of the valid lengths specified by
    /// <see cref="LegalTweakSizes" />.
    /// </value>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">
    /// Thrown when the tweak size is invalid or the tweak has not been set and <see cref="TweakSize" /> is missing or
    /// invalid.
    /// </exception>
    /// <remarks>
    /// If the tweak is not explicitly set, it will be lazily generated via <see cref="GenerateTweak" />. Defensive
    /// copies are made during assignment and retrieval to prevent external mutation.
    /// </remarks>
    public virtual byte[] Tweak
    {
        get
        {
            this.ThrowIfDisposed();

            if (this.TweakValue is null)
                this.GenerateTweak();

            return (byte[])this.TweakValue!.Clone();
        }

        set
        {
            this.ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(value);

            this.ThrowIfInvalidTweakSize(value.Length * 8);

            this.TweakValue = (byte[])value.Clone();
            this.TweakSizeValue = value.Length * 8;
        }
    }

    /// <summary>
    /// Gets or sets the tweak size, in bits, of the cryptographic operation for the tweakable cipher (for example, 128
    /// bits / 16 bytes for the Threefish family).
    /// </summary>
    /// <value>
    /// The tweak size, in bits. Must match one of the values defined in <see cref="LegalTweakSizes" />. Divide by 8 to
    /// obtain the equivalent byte length used when allocating the <see cref="Tweak" /> buffer.
    /// </value>
    /// <returns>The tweak size in bits.</returns>
    /// <exception cref="CryptographicException">
    /// Thrown when the value does not match a valid tweak size defined in <see cref="LegalTweakSizes" />.
    /// </exception>
    /// <remarks>
    /// Changing this value clears the current tweak. A new one can be generated via <see cref="GenerateTweak" />.
    /// </remarks>
    public virtual int TweakSize
    {
        get
        {
            this.ThrowIfDisposed();
            return this.TweakSizeValue;
        }

        set
        {
            this.ThrowIfDisposed();
            this.ThrowIfInvalidTweakSize(value);

            this.TweakSizeValue = value;
            this.TweakValue = null; // Clear previous tweak
        }
    }

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor() =>
        this.CreateDecryptor(this.Key, this.IV, this.Tweak);

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) =>
        this.CreateDecryptor(rgbKey, rgbIV, this.Tweak);

    /// <summary>
    /// Creates a symmetric decryptor using the specified key, initialization vector (IV), and tweak value.
    /// </summary>
    /// <param name="rgbKey">The secret key to use for decryption.</param>
    /// <param name="rgbIV">The initialization vector to use for the decryption operation.</param>
    /// <param name="tweak">The tweak value that modifies the decryption process.</param>
    /// <returns>An <see cref="ICryptoTransform" /> instance that can be used to perform the decryption.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="rgbKey" />, <paramref name="rgbIV" />, or <paramref name="tweak" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Thrown if any input does not conform to the expected size, format, or algorithm-specific constraints.
    /// </exception>
    /// <remarks>
    /// This method must be implemented by derived types to support decryption with a tweak, as required by tweakable
    /// block ciphers such as Threefish.
    /// </remarks>
    public abstract ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV, byte[] tweak);

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) =>
        this.CreateEncryptor(rgbKey, rgbIV, this.Tweak);

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor() =>
        this.CreateEncryptor(this.Key, this.IV, this.Tweak);

    /// <summary>
    /// Creates a symmetric encryptor using the specified key, initialization vector (IV), and tweak value.
    /// </summary>
    /// <param name="rgbKey">The secret key to use for encryption.</param>
    /// <param name="rgbIV">The initialization vector to use for the encryption operation.</param>
    /// <param name="tweak">The tweak value that modifies the encryption process.</param>
    /// <returns>An <see cref="ICryptoTransform" /> instance that can be used to perform the encryption.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="rgbKey" />, <paramref name="rgbIV" />, or <paramref name="tweak" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Thrown if any input does not conform to the expected size, format, or algorithm-specific constraints.
    /// </exception>
    /// <remarks>
    /// This method must be implemented by derived types to support encryption with a tweak, as required by tweakable
    /// block ciphers such as Threefish.
    /// </remarks>
    public abstract ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV, byte[] tweak);

    /// <summary>
    /// Generates a new tweak value for the algorithm based on the current <see cref="TweakSize" />.
    /// </summary>
    /// <exception cref="CryptographicException">
    /// Thrown if <see cref="TweakSize" /> is not configured to a valid size.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method initializes the tweak with random or algorithm-specific data. The generated size will match the
    /// current <see cref="TweakSize" />. If no size has been set, an exception will be thrown.
    /// </para>
    /// </remarks>
    public abstract void GenerateTweak();

    /// <summary>
    /// Determines whether the specified tweak size, in bits, is supported by the algorithm.
    /// </summary>
    /// <param name="length">The tweak size to validate, in bits.</param>
    /// <returns>
    /// <see langword="true" /> if the specified size matches any of the valid configurations in
    /// <see cref="LegalTweakSizes" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method checks the specified bit length against all entries in <see cref="LegalTweakSizes" />. A size is
    /// considered valid if it falls within the defined range and aligns with the specified skip size (if any).
    /// </para>
    /// </remarks>
    public bool ValidTweakSize(int length)
    {
        foreach (KeySizes size in this.LegalTweakSizes)
        {
            if (length < size.MinSize || length > size.MaxSize)
                continue;

            if (size.SkipSize == 0)
                return length == size.MinSize;

            if ((length - size.MinSize) % size.SkipSize == 0)
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!this._disposed)
        {
            if (disposing)
            {
                if (this.TweakValue?.Length > 0)
                {
                    CryptoHelpers.Clear(this.TweakValue);
                    this.TweakValue = [];
                }

                this.TweakSizeValue = 0;
            }

            this._disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Throws if the specified <paramref name="tweak" /> is <see langword="null" /> or its size is not valid.
    /// </summary>
    /// <param name="tweak">The tweak value to validate.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tweak" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="tweak" /> is not supported by <see cref="LegalTweakSizes" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfInvalidTweakSize(byte[] tweak)
    {
        ArgumentNullException.ThrowIfNull(tweak);
        this.ThrowIfInvalidTweakSize(tweak.Length * 8);
    }

    /// <summary>
    /// Throws an exception if the specified bit length is not a valid tweak size for this algorithm.
    /// </summary>
    /// <param name="bitLength">The length of the tweak in bits.</param>
    /// <exception cref="CryptographicException">
    /// Thrown if the specified bit length is not among the legal sizes defined by <see cref="LegalTweakSizes" />.
    /// </exception>
    /// <remarks>
    /// This method should be used internally to validate programmatic assignment to <see cref="TweakSize" /> or
    /// <see cref="Tweak" />.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfInvalidTweakSize(int bitLength)
    {
        if (!this.ValidTweakSize(bitLength))
            throw new CryptographicException(
                string.Format(
                    CryptoResourceStrings.Crypt_Invalid_TweakSize,
                    bitLength,
                    CryptoHelpers.FormatLegalSizes(this.LegalTweakSizes)));
    }

    /// <summary>
    /// Throws if the tweak has not been set or generated.
    /// </summary>
    /// <exception cref="CryptographicException">
    /// Thrown if the internal tweak value is <see langword="null" /> or empty.
    /// </exception>
    /// <remarks>
    /// Call this method before using the <see cref="Tweak" /> property to ensure that the tweak has been initialized.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfTweakNotSet()
    {
        if (this.TweakValue is null || this.TweakValue.Length == 0)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_TweakNotSet);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif

}
