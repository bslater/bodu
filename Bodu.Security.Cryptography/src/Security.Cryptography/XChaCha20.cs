// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XChaCha20.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the extended-nonce XChaCha20 stream cipher. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// XChaCha20 extends RFC 8439 ChaCha20 to a 192-bit nonce, following <c>draft-irtf-cfrg-xchacha</c>. The longer nonce
/// is large enough to choose at random per message without meaningful collision risk, which makes XChaCha20 the safer
/// default for protocols that cannot guarantee a unique 96-bit counter — the use case behind libsodium's
/// <c>crypto_stream_xchacha20</c>.
/// </para>
/// <para>
/// The construction is a thin shell over ChaCha20: the first 128 bits of the 192-bit nonce are combined with the key
/// via HChaCha20 to derive a 256-bit subkey, and the cipher then runs as ordinary ChaCha20 under that subkey with a
/// 96-bit nonce formed as four zero bytes followed by the remaining 64 bits of the original nonce. All keystream
/// generation, partial-block carry, and counter-overflow protection are inherited from the shared
/// <see cref="StreamCipherTransform" />
/// / <see cref="ChaCha20StreamCipher" /> stack, so XChaCha20 contains no duplicate cipher logic.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Key size: 256 bits (32 bytes).</description>
/// </item>
/// <item>
/// <description>Nonce (IV) size: 192 bits (24 bytes).</description>
/// </item>
/// <item>
/// <description>Block counter: 32-bit, starting at <see cref="InitialCounter" /> (default 0).</description>
/// </item>
/// </list>
/// <para>
/// Like ChaCha20 this is the <em>raw</em>, confidentiality-only cipher and is self-inverse — encryptor and decryptor
/// are interchangeable. For authenticated encryption, pair it with a MAC such as <see cref="Poly1305" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var xchacha = new XChaCha20();
/// xchacha.GenerateKey(); // 256-bit
/// xchacha.GenerateIV();  // 192-bit nonce — safe to choose at random
/// byte[] ciphertext = xchacha.Encrypt(plaintext);
/// byte[] roundTrip  = xchacha.Decrypt(ciphertext);
///]]>
/// </code>
/// </example>
/// <seealso href="https://datatracker.ietf.org/doc/html/draft-irtf-cfrg-xchacha">draft-irtf-cfrg-xchacha — XChaCha:
/// eXtended-nonce ChaCha and AEAD_XChaCha20_Poly1305</seealso>
/// <seealso cref="ChaCha20" />
public sealed class XChaCha20
    : SymmetricAlgorithm, IStreamCipherAlgorithm
{
    /// <summary>
    /// The required XChaCha20 key size, in bits (256).
    /// </summary>
    internal const int KeySizeBits = ChaCha20StreamCipher.KeySizeBytes * 8;

    /// <summary>
    /// The XChaCha20 extended nonce size, in bits (192).
    /// </summary>
    internal const int NonceSizeBits = NonceSizeBytes * 8;

    /// <summary>
    /// The XChaCha20 extended nonce size, in bytes (24).
    /// </summary>
    internal const int NonceSizeBytes = 24;

    private static readonly KeySizes[] s_keySizes = [new KeySizes(KeySizeBits, KeySizeBits, 0)];
    private static readonly KeySizes[] s_nonceSizes = [new KeySizes(NonceSizeBits, NonceSizeBits, 0)];

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="XChaCha20" /> class with default parameters.
    /// </summary>
    /// <remarks>
    /// The default configuration uses a 256-bit key and a 192-bit nonce, with the block counter starting at 0.
    /// </remarks>
    public XChaCha20()
    {
        KeySizeValue = KeySizeBits;
        LegalKeySizesValue = s_keySizes;

        BlockSizeValue = NonceSizeBits;
        LegalBlockSizesValue = s_nonceSizes;

        ModeValue = CipherMode.CBC;
        PaddingValue = PaddingMode.None;
    }

    /// <summary>
    /// Gets or sets the initial 32-bit block counter used when generating the keystream.
    /// </summary>
    /// <value>The starting block-counter value. The default is 0.</value>
    /// <returns>The block-counter value applied to the first keystream block.</returns>
    /// <remarks>
    /// libsodium's <c>crypto_stream_xchacha20</c> starts the counter at 0. Set this before creating an encryptor or
    /// decryptor when matching an external counter convention.
    /// </remarks>
    public uint InitialCounter { get; set; }

    /// <summary>
    /// Creates a new <see cref="XChaCha20" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="XChaCha20" /> instance.</returns>
    public static new XChaCha20 Create() =>
        new();

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) =>
        CreateTransform(rgbKey, rgbIV);

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) =>
        CreateTransform(rgbKey, rgbIV);

    /// <inheritdoc />
    public override void GenerateKey()
    {
        ThrowIfDisposed();
        KeyValue = CryptoHelpers.GetRandomNonZeroBytes(KeySizeValue / 8);
    }

    /// <inheritdoc />
    public override void GenerateIV()
    {
        ThrowIfDisposed();
        IVValue = CryptoHelpers.GetRandomNonZeroBytes(BlockSizeValue / 8);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                CryptoHelpers.Clear(KeyValue);
                CryptoHelpers.Clear(IVValue);
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Validates the key and nonce, derives the HChaCha20 subkey, builds the ChaCha20 engine, and returns a
    /// self-inverse stream transform.
    /// </summary>
    /// <param name="rgbKey">The 256-bit key.</param>
    /// <param name="rgbIV">The 192-bit extended nonce.</param>
    /// <returns>An <see cref="ICryptoTransform" /> that XORs data with the XChaCha20 keystream.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The key or nonce length is invalid.</exception>
    private ICryptoTransform CreateTransform(byte[] rgbKey, byte[]? rgbIV)
    {
        ThrowIfDisposed();
        CryptoHelpers.ThrowIfInvalidKeySize(rgbKey, KeySize, LegalKeySizes);
        ChaCha20Helpers.ThrowIfInvalidNonceSize(rgbIV, NonceSizeBytes);

        // Derive the 256-bit subkey from the key and the first 128 bits of the nonce.
        Span<byte> subkey = stackalloc byte[ChaCha20StreamCipher.KeySizeBytes];

        // The ChaCha20 nonce is 4 zero bytes followed by the trailing 64 bits of the XChaCha20 nonce.
        Span<byte> chachaNonce = stackalloc byte[ChaCha20StreamCipher.NonceSizeBytes];

        try
        {
            ChaCha20StreamCipher.HChaCha20(rgbKey, rgbIV.AsSpan(0, ChaCha20StreamCipher.HChaChaNonceSizeBytes), subkey);
            rgbIV.AsSpan(ChaCha20StreamCipher.HChaChaNonceSizeBytes, 8).CopyTo(chachaNonce.Slice(4, 8));

            var engine = new ChaCha20StreamCipher(subkey, chachaNonce);
            return new ChaCha20Transform(engine, InitialCounter);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(subkey);
        }
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if this instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
#endif
}
