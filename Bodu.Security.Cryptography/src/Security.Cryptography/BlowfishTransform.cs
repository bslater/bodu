// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Performs a cryptographic transformation of data using the <see cref="Blowfish"/> algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Instances of this class are returned by <see cref="Blowfish.CreateEncryptor(byte[], byte[])"/> and
/// <see cref="Blowfish.CreateDecryptor(byte[], byte[])"/>. Using this class directly is not recommended; prefer using
/// <see cref="Blowfish"/> with a <see cref="System.Security.Cryptography.CryptoStream"/>, which handles padding and block
/// alignment automatically.
/// </para>
/// </remarks>
internal sealed class BlowfishTransform
    : BlockCipherTransform
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlowfishTransform"/> class.
    /// </summary>
    /// <param name="cipher">The configured <see cref="IBlockCipher"/> engine to use. Must not be <see langword="null"/>.</param>
    /// <param name="cipherMode">The block cipher mode of operation (e.g., CBC, ECB, CFB).</param>
    /// <param name="paddingMode">The padding scheme to apply to the final block.</param>
    /// <param name="iv">The initialization vector for the cipher mode. Must match the block size.</param>
    /// <param name="encrypt"><see langword="true"/> to configure for encryption; <see langword="false"/> for decryption.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="cipher"/> is <see langword="null"/>.</exception>
    internal BlowfishTransform(IBlockCipher cipher, CipherModeKind cipherMode, PaddingModeKind paddingMode, byte[] iv, bool encrypt)
        : base(cipher, cipherMode, paddingMode, iv, encrypt)
    {
    }
}
