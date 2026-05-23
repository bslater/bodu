// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfbModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies the Output Feedback (OFB) mode transformation to an underlying <see cref="IBlockCipher" />, turning it into
/// a synchronous stream cipher in which encryption and decryption are identical operations.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/classic-modes.svg" alt="OFB panel — the cipher's output keystream feeds forward into the next cipher input, independent of plaintext."/>
/// </para>
/// <para>
/// The keystream is produced by repeatedly encrypting the feedback register: <c>Oᵢ = E(Oᵢ₋₁)</c> with <c>O₀ = IV</c>,
/// and the output is <c>Pᵢ ⊕ Oᵢ</c>. See <b>panel 4</b> of the diagram above: the dashed feedback arrows run between
/// cipher <em>output</em> and the next cipher <em>input</em> — in contrast to CFB (panel 3), where feedback runs from
/// ciphertext. That structural difference is what makes OFB a synchronous stream cipher — the keystream is independent
/// of the plaintext — and immune to bit-flip propagation.
/// </para>
/// <para>
/// The initialization vector must equal the cipher block size in length and must never be reused under the same key,
/// otherwise keystreams collide and confidentiality is lost.
/// </para>
/// <para>
/// <strong>When to use OFB.</strong> Pick OFB only for legacy interop. For stream-cipher behavior
/// <see cref="CtrModeTransform" /> is the modern default — it parallelizes, supports random access, and is the mode
/// used by every major AEAD construction. OFB's main historical advantage was that bit errors in transmission do not
/// propagate, but unauthenticated stream ciphers cannot detect those errors at all, so the guarantee is rarely useful
/// in practice. As with CFB, OFB has no built-in authentication; reach for an AEAD mode (
/// <see cref="GcmModeTransform" />, <see cref="EaxModeTransform" />) when integrity matters.
/// </para>
/// <para>
/// OFB's keystream depends only on the IV and the key, so it is sequential at generation but the resulting keystream
/// can be precomputed and cached if needed.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
///
/// // Most callers should set SymmetricAlgorithm.Mode = CipherBlockMode.OFB instead of using this directly.
/// using IBlockCipher cipher = new AesBlockCipher(key);
/// byte[] iv = RandomNumberGenerator.GetBytes(cipher.BlockSize / 8); // unique per message
/// IBlockCipherModeTransform ofb = new OfbModeTransform(cipher, iv);
/// byte[] ciphertext = new byte[plaintext.Length];
/// int written = ofb.Transform(plaintext, ciphertext, encrypt: true);
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/cipher-modes.html#ofb--synchronous-stream-cipher">OFB walk-through in the
/// cipher-modes guide</seealso>
public sealed class OfbModeTransform
    : IBlockCipherModeTransform
{
    private readonly IBlockCipher _cipher;
    private readonly byte[] _currentIv;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OfbModeTransform" /> class with the specified cipher and
    /// initialization vector.
    /// </summary>
    /// <param name="cipher">The block cipher used to generate the keystream.</param>
    /// <param name="iv">
    /// The initialization vector used to seed the feedback register. A defensive copy is taken.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.
    /// </exception>
    public OfbModeTransform(IBlockCipher cipher, byte[] iv)
    {
        ThrowHelper.ThrowIfNull(cipher);
        CryptoHelpers.ThrowIfIvLengthInvalid(iv, cipher.BlockSize);

        _cipher = cipher;
        _currentIv = (byte[])iv.Clone();
    }

    /// <inheritdoc />
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        var blockSize = _cipher.BlockSize / 8;

        // Empty input is a no-op, consistent with CbcModeTransform.
        CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize, throwIfZero: false);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

        Span<byte> keystream = stackalloc byte[blockSize];

        for (var offset = 0; offset < input.Length; offset += blockSize)
        {
            ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
            Span<byte> outBlock = output.Slice(offset, blockSize);

            // Encrypt the feedback register to generate keystream
            _cipher.Encrypt(_currentIv, keystream);

            // XOR keystream with plaintext or ciphertext
            for (var i = 0; i < blockSize; i++)
                outBlock[i] = (byte)(inBlock[i] ^ keystream[i]);

            // Update feedback register with generated keystream
            keystream.CopyTo(_currentIv);
        }

        return input.Length;
    }

    /// <summary>
    /// Releases the resources used by this instance and zeroes the running feedback register so that key-equivalent
    /// keystream state does not linger in memory after disposal. The underlying <see cref="IBlockCipher" /> is not
    /// disposed by this type — ownership remains with the caller.
    /// </summary>
    /// <remarks>
    /// Idempotent.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        CryptoHelpers.Clear(_currentIv);
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
