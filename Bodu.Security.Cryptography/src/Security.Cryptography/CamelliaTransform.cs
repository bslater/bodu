// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Performs a cryptographic transformation of data using the <see cref="Camellia"/> algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Instances of this class are returned by <see cref="Camellia.CreateEncryptor(byte[], byte[])"/> and
/// <see cref="Camellia.CreateDecryptor(byte[], byte[])"/>. Using this class directly is not recommended; prefer using
/// <see cref="Camellia"/> with a <see cref="CryptoStream"/>.
/// </para>
/// </remarks>
internal sealed class CamelliaTransform
    : BlockCipherTransform
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CamelliaTransform"/> class.
    /// </summary>
    /// <param name="cipher">The configured <see cref="IBlockCipher"/> engine to use.</param>
    /// <param name="cipherMode">The block cipher mode of operation.</param>
    /// <param name="paddingMode">The padding scheme to apply to the final block.</param>
    /// <param name="iv">The initialisation vector for the cipher mode.</param>
    /// <param name="encrypt"><see langword="true"/> to configure for encryption; <see langword="false"/> for decryption.</param>
    internal CamelliaTransform(IBlockCipher cipher, CipherBlockMode cipherMode, PaddingMode paddingMode, byte[] iv, bool encrypt)
        : base(cipher, cipherMode, paddingMode, iv, encrypt)
    {
    }
}