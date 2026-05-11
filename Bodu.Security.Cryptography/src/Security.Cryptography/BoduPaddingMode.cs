// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduPaddingMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Specifies a padding scheme for block-cipher operations. Mirrors the values of the
/// framework <see cref="PaddingMode"/> enum and adds schemes that are not part of the
/// framework surface.
/// </summary>
/// <remarks>
/// <para>
/// Numeric values <c>0</c> through <c>4</c> are deliberately aligned with
/// <see cref="PaddingMode"/> so that a <see cref="BoduPaddingMode"/> value may be
/// converted to the framework enum by cast for the five values the framework defines.
/// The <see cref="ISO7816_4"/> value has no framework counterpart.
/// </para>
/// <para>
/// Pass a <see cref="BoduPaddingMode"/> to
/// <see cref="PaddingFactory.Create(BoduPaddingMode)"/> to obtain the matching
/// <see cref="IPaddingStrategy"/> implementation.
/// </para>
/// <para>
/// <strong>When to use this enum vs the framework <see cref="PaddingMode"/>.</strong> Use
/// <see cref="PaddingMode"/> for any code path that interoperates with
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding"/>; use
/// <see cref="BoduPaddingMode"/> only when <see cref="ISO7816_4"/> is required. The two are interchangeable
/// for the first five values — a <see cref="PaddingMode"/> value can be assigned to a
/// <see cref="BoduPaddingMode"/> by cast and will behave identically.
/// </para>
/// </remarks>
/// <seealso cref="PaddingFactory"/>
/// <seealso cref="IPaddingStrategy"/>
public enum BoduPaddingMode
{
    /// <summary>
    /// No padding is applied. The input must already be a multiple of the block size.
    /// Equivalent to <see cref="PaddingMode.None"/>.
    /// </summary>
    None = 0,

    /// <summary>
    /// PKCS#7 padding (RFC 5652). Each pad byte holds the padding length.
    /// Equivalent to <see cref="PaddingMode.PKCS7"/>.
    /// </summary>
    PKCS7 = 1,

    /// <summary>
    /// Zero padding. Appends <c>0x00</c> bytes until the input aligns with the block
    /// size. Not self-describing. Equivalent to <see cref="PaddingMode.Zeros"/>.
    /// </summary>
    Zeros = 2,

    /// <summary>
    /// ANSI X.923 padding. Pad bytes are <c>0x00</c> except the final byte, which
    /// holds the padding length. Equivalent to <see cref="PaddingMode.ANSIX923"/>.
    /// </summary>
    ANSIX923 = 3,

    /// <summary>
    /// ISO 10126 padding. Pad bytes are random except the final byte, which holds the
    /// padding length. Equivalent to <see cref="PaddingMode.ISO10126"/>.
    /// </summary>
    ISO10126 = 4,

    /// <summary>
    /// ISO/IEC 7816-4 padding (also known as "one-and-zeros" or bit padding). The first
    /// pad byte is <c>0x80</c> and remaining pad bytes are <c>0x00</c>. A full block of
    /// padding is appended when the input is already block-aligned so that unpadding is
    /// unambiguous.
    /// </summary>
    /// <remarks>
    /// This value has no counterpart in the framework <see cref="PaddingMode"/> enum.
    /// It is widely used in smart-card standards, SHA-3/Keccak and CMAC.
    /// </remarks>
    ISO7816_4 = 5,
}
