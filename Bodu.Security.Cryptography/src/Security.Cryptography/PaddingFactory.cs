// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingFactory.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Creates <see cref="IPaddingStrategy" /> instances for the framework
/// <see cref="PaddingMode" /> values and for the extended <see cref="BoduPaddingMode" /> values.
/// </summary>
/// <remarks>
/// <para>
/// The factory dispatches a padding-scheme enumeration value to the matching <see cref="IPaddingStrategy"/>
/// implementation. Used internally by every <see cref="System.Security.Cryptography.SymmetricAlgorithm"/> in
/// this library when its <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding"/> is set.
/// </para>
/// <para>
/// Two overloads are provided. <see cref="Create(PaddingMode)"/> dispatches the BCL-standard
/// <see cref="PaddingMode"/> values (PKCS7, Zeros, None, ANSI X.923, ISO 10126).
/// <see cref="Create(BoduPaddingMode)"/> additionally supports <see cref="BoduPaddingMode.ISO7816_4"/>, the
/// "one-and-zeros" scheme used by smart cards and SHA-3 / Keccak — pick this overload when integrating with
/// code that uses the <see cref="BoduPaddingMode"/> superset.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/padding.html">Padding guide — PKCS7, Zeros, None, ANSI X.923, ISO 10126 and ISO/IEC 7816-4 with worked examples</seealso>
public static class PaddingFactory
{
    /// <summary>
    /// Creates a new <see cref="IPaddingStrategy" /> for the specified framework padding mode.
    /// </summary>
    /// <param name="mode">The padding scheme to apply. Supported values are
    /// <see cref="PaddingMode.PKCS7" />, <see cref="PaddingMode.Zeros" />,
    /// <see cref="PaddingMode.None" />, <see cref="PaddingMode.ANSIX923" /> and
    /// <see cref="PaddingMode.ISO10126" />.</param>
    /// <returns>An <see cref="IPaddingStrategy" /> that implements the requested <paramref name="mode" />.</returns>
    /// <exception cref="CryptographicException">Thrown if <paramref name="mode" /> is not a supported padding scheme.</exception>
    public static IPaddingStrategy Create(PaddingMode mode) => mode switch
    {
        PaddingMode.PKCS7 => new Pkcs7Padding(),
        PaddingMode.Zeros => new ZeroPadding(),
        PaddingMode.None => new NoPadding(),
        PaddingMode.ANSIX923 => new Ansix923Padding(),
        PaddingMode.ISO10126 => new Iso10126Padding(),
        _ => throw new CryptographicException($"Unsupported padding mode: {mode}")
    };

    /// <summary>
    /// Creates a new <see cref="IPaddingStrategy" /> for the specified extended padding mode.
    /// </summary>
    /// <param name="mode">The padding scheme to apply. In addition to the five framework-enum values
    /// this overload supports <see cref="BoduPaddingMode.ISO7816_4" />.</param>
    /// <returns>An <see cref="IPaddingStrategy" /> that implements the requested <paramref name="mode" />.</returns>
    /// <exception cref="CryptographicException">Thrown if <paramref name="mode" /> is not a supported padding scheme.</exception>
    public static IPaddingStrategy Create(BoduPaddingMode mode) => mode switch
    {
        BoduPaddingMode.PKCS7 => new Pkcs7Padding(),
        BoduPaddingMode.Zeros => new ZeroPadding(),
        BoduPaddingMode.None => new NoPadding(),
        BoduPaddingMode.ANSIX923 => new Ansix923Padding(),
        BoduPaddingMode.ISO10126 => new Iso10126Padding(),
        BoduPaddingMode.ISO7816_4 => new Iso7816_4Padding(),
        _ => throw new CryptographicException($"Unsupported padding mode: {mode}")
    };
}
