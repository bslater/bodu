// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Synthetic Initialization Vector (SIV) mode to two underlying <see cref="IBlockCipher" /> instances,
/// providing deterministic authenticated encryption per RFC 5297 (AES-SIV).
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow - SIV inverts the usual order by running the MAC pipeline first (S2V) to derive a synthetic IV, which is then used as the CTR counter."/>
/// </para>
/// <para>
/// SIV <em>inverts</em> the order shown in the generic AEAD diagram: the bottom pipeline runs <b>first</b> - S2V/CMAC
/// over the associated data and plaintext produces the synthetic IV, which is both the tag and the CTR counter base -
/// and only then does the top pipeline encrypt the plaintext under that derived counter. That reversal is what makes
/// SIV misuse-resistant: re-encrypting the same message yields the same ciphertext, but confidentiality is not lost
/// beyond confirming message equality.
/// </para>
/// <para>
/// SIV requires two independent ciphers keyed with different material:
/// <list type="bullet">
/// <item>
/// <description><c>s2vCipher</c> (K₁) - used by CMAC and S2V to derive the synthetic IV.</description>
/// </item>
/// <item>
/// <description><c>ctrCipher</c> (K₂) - used by AES-CTR to encrypt the plaintext.</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// The S2V algorithm (RFC 5297 Section 2.4) accumulates all associated data blocks and the plaintext into a single
/// 128-bit tag using CMAC:
/// <code>
///<![CDATA[
/// D ← CMAC(K₁, 0^128)
/// for each AD block Sᵢ (i < n): D ← dbl(D) ⊕ CMAC(K₁, Sᵢ)
/// if |Sₙ| ≥ 128: T ← CMAC(K₁, xorend(Sₙ, D))
/// else:            T ← CMAC(K₁, dbl(D) ⊕ pad(Sₙ))
/// SIV ← T
///]]>
/// </code>
/// </para>
/// <para>
/// Ciphertext is output as <c>C || SIV</c> (ciphertext then tag), consistent with the
/// <see cref="IAeadBlockCipherModeTransform" /> convention.
/// </para>
/// <para>
/// <strong>When to use SIV.</strong> Pick AES-SIV when deterministic authenticated encryption is wanted - key wrapping
/// (RFC 5297 §6 / RFC 5649), envelope encryption schemes that need stable ciphertext for deduplication, or any context
/// that cannot maintain a per-message nonce. SIV is two-pass and slower than <see cref="GcmModeTransform" /> on
/// commodity hardware, but it has the strongest misuse-resistance profile in this library: re-encrypting the same
/// <c>(plaintext, AAD)</c> tuple produces the same ciphertext, but distinct messages remain confidential and authentic.
/// <see cref="GcmSivModeTransform" /> is the RFC 8452 alternative - same misuse-resistance category, different MAC
/// (POLYVAL) and key schedule, typically faster on AES-NI/PCLMULQDQ hardware.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// // SIV uses a doubled key: first half drives the MAC, second half drives CTR encryption.
/// using IBlockCipher s2v = new AesBlockCipher(macKey);
/// using IBlockCipher ctr = new AesBlockCipher(encKey);
/// byte[] iv = new byte[s2v.BlockSize / 8]; // ignored by SIV - present for interface compatibility
/// using IAeadBlockCipherModeTransform siv = new SivModeTransform(s2v, ctr, iv);
///
/// byte[] sealed_ = siv.Encrypt(plaintext, associatedData: header);
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/aead-modes.html#siv---misuse-resistant">SIV walk-through in the AEAD-modes
/// guide</seealso> <seealso cref="AesBlockCipher"/>
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/>
public sealed class SivModeTransform
    : IAeadBlockCipherModeTransform, IDisposable
{
    /// <summary>Length of the SIV cipher block is 128 bits (16 bytes). Byte length derived inline via <see cref="BlockSizeBits" /> / 8.</summary>
    private const int BlockSizeBits = 128;

    /// <summary>Length of the SIV authentication tag is 128 bits (16 bytes). Byte length derived inline via <see cref="TagSizeBits" /> / 8.</summary>
    private const int TagSizeBits = 128;

    /// <summary>The block size, in bytes, of both ciphers, of CMAC, and of a counter block.</summary>
    private const int BlockBytes = BlockSizeBits / 8;

    /// <summary>The cipher keyed with K₁, used by CMAC and S2V to derive the synthetic IV.</summary>
    private readonly IBlockCipher _s2vCipher;

    /// <summary>The cipher keyed with K₂, used by AES-CTR to encrypt the plaintext.</summary>
    private readonly IBlockCipher _ctrCipher;

    /// <summary>The retained associated-data bytes contributed to the S2V computation, or <see langword="null" /> until set.</summary>
    private byte[]? _aad;

    /// <summary>Indicates whether the associated data has been supplied for this transform.</summary>
    private bool _aadProcessed;

    /// <summary>Indicates whether this single-use transform has already encrypted or decrypted a message.</summary>
    private bool _completed;

    /// <summary>Indicates whether the instance has been disposed and its retained state cleared.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SivModeTransform" /> class.
    /// </summary>
    /// <param name="s2vCipher">The cipher keyed with K₁, used for CMAC and S2V computation.</param>
    /// <param name="ctrCipher">The cipher keyed with K₂, used for CTR encryption.</param>
    /// <param name="iv">
    /// Accepted for interface compatibility; not used by SIV because the synthetic IV is derived from the data.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="s2vCipher" />, <paramref name="ctrCipher" />, or <paramref name="iv" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Either cipher does not have a 16-byte block size, or <paramref name="iv" /> length does not equal the S2V cipher
    /// block size.
    /// </exception>
    public SivModeTransform(IBlockCipher s2vCipher, IBlockCipher ctrCipher, byte[] iv)
    {
        ThrowHelper.ThrowIfNull(s2vCipher);
        ThrowHelper.ThrowIfNull(ctrCipher);
        CryptographyThrowHelper.ThrowIfIvLengthInvalid(iv, s2vCipher.BlockSize);

        CryptographyThrowHelper.ThrowIfBlockSizeNotEqualTo(s2vCipher, BlockSizeBits, "SIV S2V", nameof(s2vCipher));

        CryptographyThrowHelper.ThrowIfBlockSizeNotEqualTo(ctrCipher, BlockSizeBits, "SIV CTR", nameof(ctrCipher));

        _s2vCipher = s2vCipher;
        _ctrCipher = ctrCipher;

        // iv is intentionally unused - SIV derives its own synthetic IV.
    }

    /// <inheritdoc />
    /// <value>Length of the SIV authentication tag is 128 bits (16 bytes).</value>
    public int TagSize => TagSizeBits;

    /// <inheritdoc />
    public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
    {
        ThrowIfDisposed();

        CryptographyThrowHelper.ThrowIfAssociatedDataAlreadyProcessed(_aadProcessed);

        _aad = associatedData.ToArray();
        _aadProcessed = true;
    }

    /// <inheritdoc />
    public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        int required = plaintext.Length + (TagSizeBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, required);

        EnsureAadProcessed();

        // The synthetic IV, then the counter derived from it.
        Span<byte> scratch = stackalloc byte[2 * BlockBytes];

        try
        {
            Span<byte> siv = scratch[..BlockBytes];
            Span<byte> counter = scratch.Slice(BlockBytes, BlockBytes);

            // SIV = S2V(K1, AAD, plaintext).
            S2V(_aad!, plaintext, siv);

            // Encrypt plaintext with CTR (K2) seeded from SIV with bits 31 and 63 cleared.
            siv.CopyTo(counter);
            counter[8] &= 0x7F;
            counter[12] &= 0x7F;
            CounterKeystream.TransformBigEndian128(_ctrCipher, counter, plaintext, output[..plaintext.Length]);

            // Output: ciphertext || SIV tag.
            siv.CopyTo(output[plaintext.Length..]);

            return required;
        }
        finally
        {
            CryptographyHelper.Clear(scratch);
            _completed = true;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <strong>Authentication pattern: write-then-clear.</strong> The synthetic IV is recomputed by S2V over the
    /// decrypted plaintext, so the CTR decryption is written into <paramref name="output" /> first and the SIV is
    /// compared in constant time afterwards. On any failure - an authentication mismatch or an exception from either
    /// underlying cipher mid-transform - the plaintext region of <paramref name="output" /> is zeroed before the
    /// exception propagates, so unverified plaintext never escapes. See
    /// <see cref="IAeadBlockCipherModeTransform.Decrypt" /> for the library-wide failure contract.
    /// </remarks>
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        CryptographyThrowHelper.ThrowIfCiphertextTooShort(ciphertextWithTag, TagSizeBits / 8);

        int plaintextLength = ciphertextWithTag.Length - (TagSizeBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, plaintextLength);

        EnsureAadProcessed();

        ReadOnlySpan<byte> ciphertext = ciphertextWithTag[..plaintextLength];
        ReadOnlySpan<byte> receivedSiv = ciphertextWithTag[plaintextLength..];

        // The received SIV, the counter derived from it, and the expected SIV.
        Span<byte> scratch = stackalloc byte[3 * BlockBytes];

        try
        {
            Span<byte> sivCopy = scratch[..BlockBytes];
            Span<byte> counter = scratch.Slice(BlockBytes, BlockBytes);
            Span<byte> expectedSiv = scratch.Slice(2 * BlockBytes, BlockBytes);

            // Copy the SIV first: it seeds the counter, and the plaintext may be written over the buffer holding it.
            receivedSiv.CopyTo(sivCopy);

            // Decrypt with CTR seeded from the received SIV.
            sivCopy.CopyTo(counter);
            counter[8] &= 0x7F;
            counter[12] &= 0x7F;
            CounterKeystream.TransformBigEndian128(_ctrCipher, counter, ciphertext, output[..plaintextLength]);

            // Verify SIV.
            S2V(_aad!, output[..plaintextLength], expectedSiv);
            if (!CryptographicOperations.FixedTimeEquals(expectedSiv, sivCopy))
            {
                CryptographyHelper.Clear(output[..plaintextLength]);
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);
            }

            return plaintextLength;
        }
        catch
        {
            // Zero the plaintext region on any failure - a SIV mismatch or a fault from either underlying
            // cipher mid-transform - so the unverified plaintext this write-then-clear mode has already
            // written never leaks.
            CryptographyHelper.Clear(output[..plaintextLength]);
            throw;
        }
        finally
        {
            CryptographyHelper.Clear(scratch);
            _completed = true;
        }
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if this transform has already encrypted or decrypted a message.
    /// SIV transforms are single-use; create a fresh instance per message.
    /// </summary>
    private void ThrowIfCompleted() =>
        CryptographyThrowHelper.ThrowIfAlreadyCompleted(_completed);

    /// <summary>
    /// Releases the resources used by this instance and clears retained associated-data state from memory.
    /// </summary>
    /// <remarks>
    /// The supplied <see cref="IBlockCipher" /> instances are not disposed by this type. Ownership remains with the
    /// caller.
    /// </remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release managed resources; <see langword="false" /> to release unmanaged resources
    /// only.
    /// </param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            CryptographyHelper.ClearAndNullify(ref _aad);
            _aadProcessed = false;
        }

        _disposed = true;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the associated-data S2V contribution has been initialized before payload processing.
    /// </summary>
    private void EnsureAadProcessed()
    {
        if (!_aadProcessed)
        {
            _aad = [];
            _aadProcessed = true;
        }
    }

    /// <summary>
    /// Computes the Synthetic IV using S2V per RFC 5297 Section 2.4. S2V(K, S₁, …, Sₙ) where S₁ = AAD and Sₙ =
    /// plaintext; empty associated data contributes no string, so the plaintext is then the only one.
    /// </summary>
    /// <param name="aad">The associated authenticated data.</param>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="v">Receives the 16-byte synthetic initialization vector.</param>
    /// <remarks>
    /// Every CMAC runs through <see cref="Cmac" />, so the plaintext is never copied: for a plaintext of a block or
    /// more, <c>Sₙ xorend D</c> is its bytes up to the last block as they are, then that last block XORed with <c>D</c>.
    /// </remarks>
    private void S2V(ReadOnlySpan<byte> aad, ReadOnlySpan<byte> plaintext, Span<byte> v)
    {
        // Scratch: the CMAC subkeys K1 and K2, the CMAC state and held-back block, D, and a block for T.
        Span<byte> scratch = stackalloc byte[6 * BlockBytes];

        try
        {
            Span<byte> d = scratch.Slice(4 * BlockBytes, BlockBytes);
            Span<byte> block = scratch.Slice(5 * BlockBytes, BlockBytes);
            Cmac.DeriveSubkeys(_s2vCipher, scratch[..BlockBytes], scratch.Slice(BlockBytes, BlockBytes));

            // D = CMAC(K1, 0^128).
            block.Clear();
            ComputeCmac(scratch, block, d);

            // For each component before the last: D = dbl(D) XOR CMAC(K1, component).
            if (aad.Length > 0)
            {
                GaloisField128.Double(d, d);
                ComputeCmac(scratch, aad, block);
                CryptographyHelper.Xor(d, block, d);
            }

            // Last component = plaintext. A plaintext shorter than a block, the empty one included, is padded with
            // 10* - <one> is only for a call with no strings, which cannot happen here.
            if (plaintext.Length >= BlockBytes)
            {
                var cmac = NewCmac(scratch);
                cmac.Append(plaintext[..^BlockBytes]);
                CryptographyHelper.Xor(plaintext[^BlockBytes..], d, block);
                cmac.Append(block);
                cmac.Finish(v);
                return;
            }

            GaloisField128.Double(d, d);
            block.Clear();
            plaintext.CopyTo(block);
            block[plaintext.Length] = 0x80;
            CryptographyHelper.Xor(d, block, block);
            ComputeCmac(scratch, block, v);
        }
        finally
        {
            CryptographyHelper.Clear(scratch);
        }
    }

    /// <summary>
    /// Computes the CMAC of <paramref name="message" /> under K1.
    /// </summary>
    /// <param name="scratch">
    /// Scratch whose first two blocks hold the subkeys and whose next two receive the CMAC state.
    /// </param>
    /// <param name="message">The message.</param>
    /// <param name="mac">Receives the 16-byte MAC.</param>
    private void ComputeCmac(Span<byte> scratch, ReadOnlySpan<byte> message, Span<byte> mac)
    {
        var cmac = NewCmac(scratch);
        cmac.Append(message);
        cmac.Finish(mac);
    }

    /// <summary>
    /// Starts a CMAC under K1 over the scratch layout <see cref="S2V" /> uses.
    /// </summary>
    /// <param name="scratch">
    /// Scratch whose first two blocks hold the subkeys and whose next two receive the CMAC state.
    /// </param>
    /// <returns>The computation.</returns>
    private Cmac NewCmac(Span<byte> scratch) =>
        new(
            _s2vCipher,
            scratch[..BlockBytes],
            scratch.Slice(BlockBytes, BlockBytes),
            scratch.Slice(2 * BlockBytes, BlockBytes),
            scratch.Slice(3 * BlockBytes, BlockBytes));

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
