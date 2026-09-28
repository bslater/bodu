// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EngineBackedXChaCha20Poly1305.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides XChaCha20-Poly1305 the way a type derived from <see cref="Poly1305AeadTransform" /> in another assembly
/// must: by overriding <see cref="Poly1305AeadTransform.CreateEngine" /> alone, with an engine that has no bulk entry
/// point. Its messages take the base class's engine path, one keystream block at a time.
/// </summary>
public sealed class EngineBackedXChaCha20Poly1305
    : Poly1305AeadTransform
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EngineBackedXChaCha20Poly1305" /> class with the specified key and
    /// nonce.
    /// </summary>
    /// <param name="key">The 32-byte key.</param>
    /// <param name="nonce">The 24-byte nonce.</param>
    public EngineBackedXChaCha20Poly1305(byte[] key, byte[] nonce)
        : base(
            key is null ? throw new ArgumentNullException(nameof(key)) : key.AsSpan(),
            nonce is null ? throw new ArgumentNullException(nameof(nonce)) : nonce.AsSpan())
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Derives the subkey and nonce as <see cref="XChaCha20Poly1305" /> does, and hides the engine's bulk entry point
    /// behind <see cref="SingleBlockStreamCipher" />.
    /// </remarks>
    protected override IStreamCipher CreateEngine()
    {
        Span<byte> subkey = stackalloc byte[ChaCha20StreamCipher.KeySizeBytes];
        Span<byte> chachaNonce = stackalloc byte[ChaCha20StreamCipher.NonceSizeBytes];

        try
        {
            ChaCha20StreamCipher.HChaCha20(Key, Nonce[..ChaCha20StreamCipher.HChaChaNonceSizeBytes], subkey);
            Nonce.Slice(ChaCha20StreamCipher.HChaChaNonceSizeBytes, 8).CopyTo(chachaNonce[4..]);

            return new SingleBlockStreamCipher(new ChaCha20StreamCipher(subkey, chachaNonce, initialCounter: 0));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(subkey);
        }
    }
}
