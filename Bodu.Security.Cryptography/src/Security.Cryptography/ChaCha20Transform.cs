// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Transform.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Performs a cryptographic transformation of data using the ChaCha20 keystream. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Instances of this class are returned by <see cref="ChaCha20.CreateEncryptor(byte[], byte[])" /> /
/// <see cref="ChaCha20.CreateDecryptor(byte[], byte[])" /> and by the corresponding <see cref="XChaCha20" /> members
/// (after HChaCha20 subkey derivation). Because an additive stream cipher is self-inverse, the same transform serves
/// both directions. Using this class directly is not recommended; prefer <see cref="ChaCha20" /> or
/// <see cref="XChaCha20" /> with a <see cref="CryptoStream" />.
/// </para>
/// </remarks>
internal sealed class ChaCha20Transform
    : StreamCipherTransform
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChaCha20Transform" /> class.
    /// </summary>
    /// <param name="cipher">The configured <see cref="IStreamCipher" /> engine, with key and nonce bound.</param>
    /// <param name="initialCounter">The block counter for the first keystream block.</param>
    internal ChaCha20Transform(IStreamCipher cipher, uint initialCounter)
        : base(cipher, initialCounter)
    {
    }
}
