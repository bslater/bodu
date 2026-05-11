// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockPaddingMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the padding schemes applied to the final block of plaintext during encryption and removed during decryption.
/// </summary>
/// <remarks>
/// <para>
/// This enumeration extends the framework <see cref="System.Security.Cryptography.PaddingMode"/> with additional
/// schemes that have no BCL counterpart. The member names are intentionally identical to those of
/// <see cref="System.Security.Cryptography.PaddingMode"/> so that
/// <see cref="System.Enum.TryParse{TEnum}(string?, out TEnum)"/> can synchronise values between the two
/// enumerations by name.
/// </para>
/// <para>
/// The <see cref="BlockPadding"/> property on each symmetric algorithm in this library accepts a
/// <see cref="BlockPaddingMode"/> value, automatically updating the underlying
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding"/> property when the value maps to a
/// recognised <see cref="System.Security.Cryptography.PaddingMode"/> member. Setting
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding"/> on those algorithms performs the
/// reverse synchronisation.
/// </para>
/// <para>
/// Use <see cref="PaddingFactory.Create(BlockPaddingMode)"/> to obtain the <see cref="IPaddingStrategy"/>
/// implementation for a given value.
/// </para>
/// </remarks>
/// <seealso cref="System.Security.Cryptography.PaddingMode"/>
/// <seealso href="../guides/cryptography/padding.html">Padding guide — PKCS7, Zeros, None, ANSI X.923, ISO 10126 and ISO/IEC 7816-4 with worked examples</seealso>
public enum BlockPaddingMode
{
    /// <summary>
    /// No padding is applied. The plaintext must be an exact multiple of the cipher block size.
    /// </summary>
    None,

    /// <summary>
    /// PKCS#7 padding (RFC 5652). Each padding byte contains the count of bytes added. The minimum pad
    /// is 1 byte; the maximum is one full block.
    /// </summary>
    PKCS7,

    /// <summary>
    /// Zero-byte padding. Trailing zero bytes are added to fill the final block to the block boundary.
    /// </summary>
    /// <remarks>
    /// This scheme is non-self-describing: a trailing run of legitimate zero plaintext bytes is
    /// indistinguishable from the padding, so the original plaintext length cannot be recovered on
    /// decryption unless it is communicated by another means.
    /// </remarks>
    Zeros,

    /// <summary>
    /// ANSI X.923 padding. Padding bytes are zero except for the final byte, which records the total
    /// number of padding bytes appended.
    /// </summary>
    ANSIX923,

    /// <summary>
    /// ISO 10126 padding. Padding bytes are random except for the final byte, which records the total
    /// number of padding bytes appended.
    /// </summary>
    ISO10126,

    /// <summary>
    /// ISO/IEC 7816-4 padding. A mandatory <c>0x80</c> terminator byte is appended immediately after
    /// the last plaintext byte, followed by zero bytes to fill the block. An additional all-zero-pad
    /// block is always appended when the input is already block-aligned or empty.
    /// </summary>
    /// <remarks>
    /// ISO/IEC 7816-4 is a length-recoverable, self-describing padding scheme used in smart-card
    /// applications and as the sponge-function domain separator in SHA-3 / Keccak.
    /// </remarks>
    ISO7816_4,
}
