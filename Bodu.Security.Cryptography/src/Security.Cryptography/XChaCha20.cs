// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XChaCha20.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
/// <see cref="StreamCipherTransform" /> / <see cref="ChaCha20StreamCipher" /> stack, so XChaCha20 contains no duplicate
/// cipher logic.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Key size: 256 bits (32 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Nonce (IV) size: 192 bits (24 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Block counter: 32-bit, starting at <see cref="InitialCounter" /> (default 0).
/// </description>
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
/// xchacha.GenerateNonce(); // 192-bit nonce — safe to choose at random
/// byte[] ciphertext = xchacha.Encrypt(plaintext);
/// byte[] roundTrip  = xchacha.Decrypt(ciphertext);
///]]>
/// </code>
/// </example>
/// <seealso href="https://datatracker.ietf.org/doc/html/draft-irtf-cfrg-xchacha">draft-irtf-cfrg-xchacha — XChaCha:
/// eXtended-nonce ChaCha and AEAD_XChaCha20_Poly1305</seealso> <seealso cref="ChaCha20" />
public sealed class XChaCha20
    : SymmetricStreamAlgorithm
{
    /// <summary>
    /// The required XChaCha20 key size, in bits (256).
    /// </summary>
    internal const int KeySizeBits = ChaCha20StreamCipher.KeySizeBytes * 8;

    /// <summary>
    /// The XChaCha20 extended nonce size, in bytes (24).
    /// </summary>
    internal const int NonceSizeBytes = 24;

    /// <summary>
    /// The XChaCha20 extended nonce size, in bits (192).
    /// </summary>
    internal const int NonceSizeBits = NonceSizeBytes * 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="XChaCha20" /> class with default parameters.
    /// </summary>
    /// <remarks>
    /// The default configuration uses a 256-bit key and a 192-bit nonce, with the block counter starting at 0.
    /// </remarks>
    public XChaCha20()
        : base(KeySizeBits, NonceSizeBits)
    {
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
    public static XChaCha20 Create() =>
        new();

    /// <inheritdoc />
    /// <remarks>
    /// Derives a 256-bit subkey from the key and the first 128 bits of the nonce via HChaCha20, then returns an
    /// ordinary ChaCha20 engine under that subkey with a 96-bit nonce formed as four zero bytes followed by the
    /// trailing 64 bits of the extended nonce.
    /// </remarks>
    protected override IStreamCipher CreateStreamCipher(byte[] key, byte[] nonce)
    {
        Span<byte> subkey = stackalloc byte[ChaCha20StreamCipher.KeySizeBytes];
        Span<byte> chachaNonce = stackalloc byte[ChaCha20StreamCipher.NonceSizeBytes];

        try
        {
            ChaCha20StreamCipher.HChaCha20(key, nonce.AsSpan(0, ChaCha20StreamCipher.HChaChaNonceSizeBytes), subkey);
            nonce.AsSpan(ChaCha20StreamCipher.HChaChaNonceSizeBytes, 8).CopyTo(chachaNonce.Slice(4, 8));

            return new ChaCha20StreamCipher(subkey, chachaNonce, InitialCounter);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(subkey);
        }
    }
}
