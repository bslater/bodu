// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Performs a cryptographic transformation of data using a <see cref="Threefish"/> block cipher engine. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// Instances of this class are returned by <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])"/> and
/// <see cref="Threefish.CreateDecryptor(byte[], byte[], byte[])"/>. Using this class directly is not recommended; prefer using a
/// concrete <see cref="Threefish"/> algorithm with a <see cref="CryptoStream"/>.
/// </para>
/// </remarks>
internal sealed class ThreefishTransform
    : BlockCipherTransform
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThreefishTransform"/> class using the specified cipher, mode, padding, and
    /// initialisation vector.
    /// </summary>
    /// <param name="cipher">The configured <see cref="IBlockCipher"/> engine to use. Must not be <see langword="null"/>.</param>
    /// <param name="cipherMode">The block cipher mode of operation (for example, <see cref="CipherBlockMode.CBC"/>).</param>
    /// <param name="paddingMode">The padding scheme to apply to the final block.</param>
    /// <param name="iv">The initialisation vector for the cipher mode. Must match the cipher block size.</param>
    /// <param name="encrypt"><see langword="true"/> to configure for encryption; <see langword="false"/> for decryption.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="cipher"/> is <see langword="null"/>.</exception>
    public ThreefishTransform(IBlockCipher cipher, CipherBlockMode cipherMode, PaddingMode paddingMode, byte[] iv, bool encrypt)
        : base(cipher, cipherMode, paddingMode, iv, encrypt)
    {
    }
}
