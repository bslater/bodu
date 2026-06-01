// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the extended-nonce XSalsa20 stream cipher specified by Daniel J. Bernstein.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// XSalsa20 extends Salsa20 from a 64-bit nonce to a 192-bit nonce. The longer nonce is large enough to choose at
/// random per message without meaningful collision risk, which makes XSalsa20 the safer default for protocols that
/// cannot guarantee a unique 64-bit counter — the construction underlying NaCl / libsodium's
/// <c>crypto_stream_xsalsa20</c>.
/// </para>
/// <para>
/// The construction is a thin shell over Salsa20: the first 128 bits of the 192-bit nonce are combined with the key via
/// HSalsa20 to derive a 256-bit subkey, and the cipher then runs as ordinary Salsa20 under that subkey with the
/// remaining 64 bits of the original nonce. All keystream generation, partial-block carry, and counter-overflow
/// protection are inherited from the shared <see cref="StreamCipherTransform" /> / <see cref="Salsa20StreamCipher" />
/// stack, so XSalsa20 contains no duplicate cipher logic.
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
/// Block counter: 64-bit, starting at <see cref="InitialCounter" /> (default 0).
/// </description>
/// </item>
/// </list>
/// <para>
/// Like Salsa20 this is the <em>raw</em>, confidentiality-only cipher and is self-inverse. For authenticated
/// encryption, pair it with a MAC such as <see cref="Poly1305" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var xsalsa = new XSalsa20();
/// xsalsa.GenerateKey(); // 256-bit
/// xsalsa.GenerateNonce(); // 192-bit nonce — safe to choose at random
/// byte[] ciphertext = xsalsa.Encrypt(plaintext);
/// byte[] roundTrip  = xsalsa.Decrypt(ciphertext);
///]]>
/// </code>
/// </example>
/// <seealso href="https://cr.yp.to/snuffle/xsalsa-20081128.pdf">Extending the Salsa20 nonce (Bernstein, 2008)</seealso>
/// <seealso cref="Salsa20" />
public sealed class XSalsa20
    : SymmetricStreamAlgorithm
{
    /// <summary>
    /// The required XSalsa20 key size, in bits (256).
    /// </summary>
    internal const int KeySizeBits = Salsa20StreamCipher.KeySize256Bytes * 8;

    /// <summary>
    /// The XSalsa20 extended nonce size, in bytes (24).
    /// </summary>
    internal const int NonceSizeBytes = 24;

    /// <summary>
    /// The XSalsa20 extended nonce size, in bits (192).
    /// </summary>
    internal const int NonceSizeBits = NonceSizeBytes * 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="XSalsa20" /> class with default parameters.
    /// </summary>
    /// <remarks>
    /// The default configuration uses a 256-bit key and a 192-bit nonce, with the block counter starting at 0.
    /// </remarks>
    public XSalsa20()
        : base(KeySizeBits, NonceSizeBits)
    {
    }

    /// <summary>
    /// Gets or sets the initial 64-bit block counter used when generating the keystream.
    /// </summary>
    /// <value>The starting block-counter value. The default is 0.</value>
    /// <returns>The block-counter value applied to the first keystream block.</returns>
    /// <remarks>
    /// libsodium's <c>crypto_stream_xsalsa20</c> starts the counter at 0. Set this before creating an encryptor or
    /// decryptor when matching an external counter convention.
    /// </remarks>
    public ulong InitialCounter { get; set; }

    /// <summary>
    /// Creates a new <see cref="XSalsa20" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="XSalsa20" /> instance.</returns>
    public static XSalsa20 Create() =>
        new();

    /// <inheritdoc />
    /// <remarks>
    /// Derives a 256-bit subkey from the key and the first 128 bits of the nonce via HSalsa20, then returns an ordinary
    /// Salsa20 engine under that subkey with the trailing 64 bits of the extended nonce.
    /// </remarks>
    protected override IStreamCipher CreateStreamCipher(byte[] key, byte[] nonce)
    {
        Span<byte> subkey = stackalloc byte[Salsa20StreamCipher.KeySize256Bytes];

        // The derived Salsa20 nonce is not secret; it lives on the stack only to avoid a heap allocation. The engine
        // constructor copies it synchronously, so the span need not outlive this call.
        Span<byte> salsaNonce = stackalloc byte[Salsa20StreamCipher.NonceSizeBytes];

        try
        {
            Salsa20StreamCipher.HSalsa20(key, nonce.AsSpan(0, Salsa20StreamCipher.HSalsaNonceSizeBytes), subkey);
            nonce.AsSpan(Salsa20StreamCipher.HSalsaNonceSizeBytes, 8).CopyTo(salsaNonce);

            return new Salsa20StreamCipher(subkey, salsaNonce, InitialCounter);
        }
        finally
        {
            // The derived subkey is secret key material and must be wiped once the engine has consumed it.
            CryptographicOperations.ZeroMemory(subkey);
        }
    }
}
