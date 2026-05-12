// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingModeKind.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Specifies a padding scheme for block-cipher operations. Mirrors the standard framework
/// <see cref="PaddingMode" /> values and extends that surface with additional padding schemes that are not
/// part of the framework enum.
/// </summary>
/// <remarks>
/// <para>
/// Values <c>0</c> through <c>4</c> are deliberately aligned with <see cref="PaddingMode" /> so that the
/// mirrored values can be converted between <see cref="PaddingModeKind" /> and <see cref="PaddingMode" />
/// by cast. The <see cref="ISO7816_4" /> value is a Bodu extension and has no framework counterpart.
/// </para>
/// <para>
/// Bodu-specific values start at <c>1 &lt;&lt; 10</c> to avoid colliding with framework-defined values.
/// </para>
/// <para>
/// Pass a <see cref="PaddingModeKind" /> to <see cref="PaddingFactory" /> to obtain the matching
/// <see cref="IPaddingStrategy" /> implementation.
/// </para>
/// <para>
/// <strong>When to use this enum vs the framework <see cref="PaddingMode" />.</strong> Use
/// <see cref="PaddingMode" /> for code that must directly interoperate with
/// <see cref="SymmetricAlgorithm.Padding" />. Use <see cref="PaddingModeKind" /> when code should support
/// the framework padding schemes plus Bodu-specific extensions such as <see cref="ISO7816_4" />.
/// </para>
/// </remarks>
/// <seealso cref="PaddingMode" />
/// <seealso cref="PaddingFactory" />
/// <seealso cref="IPaddingStrategy" />
public enum PaddingModeKind
{
    /// <summary>
    /// Specifies that no padding is applied. The input must already be an exact multiple of the block size.
    /// Mirrors <see cref="PaddingMode.None" />.
    /// </summary>
    /// <remarks>
    /// Use this mode only when the caller independently guarantees block alignment. Padding and depadding
    /// operations do not add or remove bytes for this mode.
    /// </remarks>
    None = PaddingMode.None,

    /// <summary>
    /// Specifies PKCS#7 padding. Each padding byte contains the number of padding bytes added.
    /// Mirrors <see cref="PaddingMode.PKCS7" />.
    /// </summary>
    /// <remarks>
    /// PKCS#7 padding is self-describing and always appends at least one padding byte. If the input is already
    /// block-aligned, a full block of padding is appended.
    /// </remarks>
    PKCS7 = PaddingMode.PKCS7,

    /// <summary>
    /// Specifies zero padding. Zero-value bytes are appended until the input aligns with the block size.
    /// Mirrors <see cref="PaddingMode.Zeros" />.
    /// </summary>
    /// <remarks>
    /// Zero padding is not self-describing. It is only unambiguous when the original plaintext cannot end with
    /// zero bytes or when the original length is known by other means.
    /// </remarks>
    Zeros = PaddingMode.Zeros,

    /// <summary>
    /// Specifies ANSI X.923 padding. Padding bytes are zero except for the final byte, which contains the
    /// number of padding bytes added. Mirrors <see cref="PaddingMode.ANSIX923" />.
    /// </summary>
    /// <remarks>
    /// ANSI X.923 padding is self-describing and always appends at least one padding byte. If the input is
    /// already block-aligned, a full block of padding is appended.
    /// </remarks>
    ANSIX923 = PaddingMode.ANSIX923,

    /// <summary>
    /// Specifies ISO 10126 padding. Padding bytes before the final byte are random, and the final byte
    /// contains the number of padding bytes added. Mirrors <see cref="PaddingMode.ISO10126" />.
    /// </summary>
    /// <remarks>
    /// ISO 10126 padding is self-describing. The random filler bytes are ignored during depadding; only the
    /// final length byte is significant.
    /// </remarks>
    ISO10126 = PaddingMode.ISO10126,

    /// <summary>
    /// Specifies ISO/IEC 7816-4 padding, also known as one-and-zeros or bit padding. The first padding byte
    /// is <c>0x80</c>, followed by zero or more <c>0x00</c> bytes. This mode extends
    /// <see cref="PaddingMode" /> and has no framework counterpart.
    /// </summary>
    /// <remarks>
    /// ISO/IEC 7816-4 padding is self-describing and appends a full block of padding when the input is already
    /// block-aligned, ensuring depadding remains unambiguous. It is commonly used in smart-card standards and
    /// related cryptographic constructions that use one-and-zeros padding.
    /// </remarks>
    ISO7816_4 = 1 << 10,
}
