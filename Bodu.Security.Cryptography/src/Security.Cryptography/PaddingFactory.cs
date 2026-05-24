// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingFactory.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Creates <see cref="IPaddingStrategy" /> instances for the framework <see cref="PaddingMode" /> values and for the
/// extended <see cref="PaddingModeKind" /> values.
/// </summary>
/// <remarks>
/// <para>
/// The factory dispatches a padding-scheme enumeration value to the matching <see cref="IPaddingStrategy" />
/// implementation. Used internally by every <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> in this
/// library when its padding is configured.
/// </para>
/// <para>
/// Two overloads are provided. <see cref="Create(PaddingMode)" /> dispatches the BCL-standard
/// <see cref="PaddingMode" /> values (PKCS7, Zeros, None, ANSI X.923, ISO 10126).
/// <see cref="Create(PaddingModeKind)" /> additionally supports <see cref="PaddingModeKind.ISO7816_4" />, the
/// "one-and-zeros" scheme used by smart cards and SHA-3 / Keccak — pick this overload when integrating with the
/// extended <see cref="PaddingModeKind" /> superset.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // BCL-standard padding mode — covers PKCS7, Zeros, None, ANSI X.923, ISO 10126.
/// IPaddingStrategy pkcs7 = PaddingFactory.Create(PaddingMode.PKCS7);
///
/// // Extended padding mode — adds ISO/IEC 7816-4 ("one-and-zeros") used by smart cards and SHA-3.
/// IPaddingStrategy iso7816 = PaddingFactory.Create(PaddingModeKind.ISO7816_4);
///
/// // Apply on encryption.
/// byte[] padded = pkcs7.Pad(plaintext, blockSizeBits: 128);
///
/// // Remove on decryption — Unpad validates the trailer and throws CryptographicException
/// // if the padding is malformed.
/// byte[] stripped = pkcs7.Unpad(decrypted, blockSizeBits: 128);
///]]>
/// </example>
/// <seealso href="../guides/cryptography/padding.html">Padding guide — PKCS7, Zeros, None, ANSI X.923, ISO 10126 and
/// ISO/IEC 7816-4 with worked examples</seealso>
public static class PaddingFactory
{
    /// <summary>
    /// Creates a new <see cref="IPaddingStrategy" /> for the specified framework padding mode.
    /// </summary>
    /// <param name="mode">
    /// The padding scheme to apply. Supported values are <see cref="PaddingMode.PKCS7" />,
    /// <see cref="PaddingMode.Zeros" />, <see cref="PaddingMode.None" />, <see cref="PaddingMode.ANSIX923" /> and
    /// <see cref="PaddingMode.ISO10126" />.
    /// </param>
    /// <returns>An <see cref="IPaddingStrategy" /> that implements the requested <paramref name="mode" />.</returns>
    /// <exception cref="CryptographicException">
    /// Thrown if <paramref name="mode" /> is not a supported padding scheme.
    /// </exception>
    public static IPaddingStrategy Create(PaddingMode mode) => mode switch
    {
        PaddingMode.PKCS7 => new Pkcs7Padding(),
        PaddingMode.Zeros => new ZeroPadding(),
        PaddingMode.None => new NoPadding(),
        PaddingMode.ANSIX923 => new Ansix923Padding(),
        PaddingMode.ISO10126 => new Iso10126Padding(),
        _ => throw new CryptographicException(
            string.Format(CryptoResourceStrings.Crypt_Invalid_UnsupportedPaddingMode, mode))
    };

    /// <summary>
    /// Creates a new <see cref="IPaddingStrategy" /> for the specified extended padding mode.
    /// </summary>
    /// <param name="mode">
    /// The padding scheme to apply. In addition to the five framework-enum values this overload supports
    /// <see cref="PaddingModeKind.ISO7816_4" />.
    /// </param>
    /// <returns>An <see cref="IPaddingStrategy" /> that implements the requested <paramref name="mode" />.</returns>
    /// <exception cref="CryptographicException">
    /// Thrown if <paramref name="mode" /> is not a supported padding scheme.
    /// </exception>
    public static IPaddingStrategy Create(PaddingModeKind mode) => mode switch
    {
        PaddingModeKind.PKCS7 => new Pkcs7Padding(),
        PaddingModeKind.Zeros => new ZeroPadding(),
        PaddingModeKind.None => new NoPadding(),
        PaddingModeKind.ANSIX923 => new Ansix923Padding(),
        PaddingModeKind.ISO10126 => new Iso10126Padding(),
        PaddingModeKind.ISO7816_4 => new Iso7816_4Padding(),
        _ => throw new CryptographicException(
            string.Format(System.Globalization.CultureInfo.InvariantCulture, CryptoResourceStrings.Crypt_Invalid_UnsupportedPaddingMode, mode))
    };
}
