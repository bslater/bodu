// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackTransform.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Performs a cryptographic transformation of data using the <see cref="Skipjack" /> algorithm. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// Instances of this class are returned by <see cref="Skipjack.CreateEncryptor(byte[], byte[])" /> and
/// <see cref="Skipjack.CreateDecryptor(byte[], byte[])" />. Using this class directly is not recommended; prefer using
/// <see cref="Skipjack" /> with a <see cref="CryptoStream" />, which handles padding and block alignment automatically.
/// </para>
/// </remarks>
internal sealed class SkipjackTransform
    : BlockCipherTransform
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SkipjackTransform" /> class using the specified cipher, mode,
    /// padding, and initialization vector.
    /// </summary>
    /// <param name="cipher">
    /// The configured <see cref="IBlockCipher" /> engine to use. Must not be <see langword="null" />.
    /// </param>
    /// <param name="cipherMode">
    /// The block cipher mode of operation (for example, <see cref="CipherModeKind.CBC" />).
    /// </param>
    /// <param name="paddingMode">The padding scheme to apply to the final block.</param>
    /// <param name="iv">The initialization vector for the cipher mode. Must match the cipher block size.</param>
    /// <param name="encrypt">
    /// <see langword="true" /> to configure for encryption; <see langword="false" /> for decryption.
    /// </param>
    /// <exception cref="System.ArgumentNullException">
    /// <paramref name="cipher" /> is <see langword="null" />.
    /// </exception>
    public SkipjackTransform(IBlockCipher cipher, CipherModeKind cipherMode, PaddingModeKind paddingMode, byte[]? iv, bool encrypt)
        : base(cipher, cipherMode, paddingMode, iv, encrypt)
    {
    }
}
