// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtsModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Ciphertext Stealing (CTS) over CBC mode to an underlying <see cref="IBlockCipher" />, allowing encryption of
/// inputs whose length is not a multiple of the block size without requiring padding.
/// </summary>
/// <remarks>
/// <para>
/// CTS produces ciphertext of exactly the same length as the plaintext by "stealing" bytes from the penultimate
/// ciphertext block to complete the final partial block. For block-aligned inputs (where the total length is a multiple
/// of the block size), CTS behaves identically to CBC with no stealing.
/// </para>
/// <para>
/// The CTS steal algorithm (CS3 / IEEE 1619 variant):
/// <list type="bullet">
/// <item>
/// <description>
/// All complete blocks except the last two are processed normally in CBC mode.
/// </description>
/// </item>
/// <item>
/// <description>
/// Encrypt: the penultimate block is CBC-encrypted to E; the final partial block P_n is padded with E[m:] and
/// raw-encrypted to produce C_n (full block); C_{n-1} = E[0:m]. Output: C_n then C_{n-1}.
/// </description>
/// </item>
/// <item>
/// <description>
/// Decrypt: C_n is raw-decrypted to recover the padded P_n and E[m:]; C_{n-1} is reconstructed as E[0:m] ||
/// reconstructed; then the full C_{n-1} is CBC-decrypted to P_{n-1}.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// The input to <see cref="Transform" /> must be at least one full block long. For inputs of exactly one block, the
/// result is identical to standard CBC encryption or decryption.
/// </para>
/// <para>
/// <strong>When to use CTS.</strong> Pick CTS when ciphertext length must equal plaintext length and the input is not
/// block-aligned — typical in fixed-size record formats, network frames with strict size budgets, and on-disk layouts
/// where adding padding bytes is impossible. CTS shares CBC's lack of authentication and its sequential nature; for new
/// general-purpose encryption an AEAD mode (<see cref="GcmModeTransform" />, <see cref="EaxModeTransform" />) is
/// preferable. The variant implemented here is CS3 / IEEE 1619 (the order used by NIST SP 800-38A Addendum and most
/// modern interop).
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
///
/// // Most callers should set SymmetricAlgorithm.Mode = CipherBlockMode.CTS instead of using this directly.
/// using IBlockCipher cipher = new AesBlockCipher(key);
/// byte[] iv = RandomNumberGenerator.GetBytes(cipher.BlockSize / 8);
/// IBlockCipherModeTransform cts = new CtsModeTransform(cipher, iv);
///
/// // CTS preserves length: plaintext.Length == ciphertext.Length, no padding required.
/// byte[] ciphertext = new byte[plaintext.Length];
/// int written = cts.Transform(plaintext, ciphertext, encrypt: true);
///]]>
/// </code>
/// </example>
public sealed class CtsModeTransform
    : IBlockCipherModeTransform
{
    private readonly IBlockCipher _cipher;
    private readonly byte[] _iv;
    private readonly byte[] _currentIv; // running CBC chaining vector
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CtsModeTransform" /> class.
    /// </summary>
    /// <param name="cipher">The underlying block cipher.</param>
    /// <param name="iv">
    /// The initialization vector for the CBC chain. Must equal the cipher block size. A defensive copy is taken; the
    /// caller's array is not modified.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="iv" /> length does not equal the cipher block size.
    /// </exception>
    public CtsModeTransform(IBlockCipher cipher, byte[] iv)
    {
        ThrowHelper.ThrowIfNull(cipher);
        CryptoHelpers.ThrowIfIvLengthInvalid(iv, cipher.BlockSize);

        this._cipher = cipher;
        this._iv = (byte[])iv.Clone();
        this._currentIv = (byte[])iv.Clone();
    }

    /// <inheritdoc />
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        var blockSize = this._cipher.BlockSize / 8;

        if (input.Length < blockSize)
        {
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Arg_Invalid_CtsInputTooShort, blockSize),
                nameof(input));
        }

        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

        // If perfectly block-aligned (or exactly one block), use plain CBC — no stealing needed.
        if (input.Length % blockSize == 0)
            return encrypt ? EncryptCbc(input, output) : DecryptCbc(input, output);

        // Non-aligned input: number of complete blocks before the final partial.
        var fullBlocks = input.Length / blockSize;   // blocks 0..fullBlocks-1 are complete
        var tailBytes = input.Length % blockSize;   // 0 < tailBytes < blockSize

        return encrypt
            ? EncryptCts(input, output, fullBlocks, tailBytes, blockSize)
            : DecryptCts(input, output, fullBlocks, tailBytes, blockSize);
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Standard CBC encryption (no stealing), used when input is block-aligned.
    /// </summary>
    /// <param name="input">The plaintext bytes to encrypt.</param>
    /// <param name="output">The destination span for the ciphertext.</param>
    /// <returns>The number of bytes written to <paramref name="output" />.</returns>
    private int EncryptCbc(ReadOnlySpan<byte> input, Span<byte> output)
    {
        var blockSize = this._cipher.BlockSize / 8;
        Span<byte> block = stackalloc byte[blockSize];

        for (var offset = 0; offset < input.Length; offset += blockSize)
        {
            ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
            Span<byte> outBlock = output.Slice(offset, blockSize);

            for (var i = 0; i < blockSize; i++)
                block[i] = (byte)(inBlock[i] ^ this._currentIv[i]);

            this._cipher.Encrypt(block, outBlock);
            outBlock.CopyTo(this._currentIv);
        }

        return input.Length;
    }

    /// <summary>
    /// Standard CBC decryption (no stealing), used when input is block-aligned.
    /// </summary>
    /// <param name="input">The ciphertext bytes to decrypt.</param>
    /// <param name="output">The destination span for the plaintext.</param>
    /// <returns>The number of bytes written to <paramref name="output" />.</returns>
    private int DecryptCbc(ReadOnlySpan<byte> input, Span<byte> output)
    {
        var blockSize = this._cipher.BlockSize / 8;
        Span<byte> block = stackalloc byte[blockSize];

        for (var offset = 0; offset < input.Length; offset += blockSize)
        {
            ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
            Span<byte> outBlock = output.Slice(offset, blockSize);

            this._cipher.Decrypt(inBlock, block);
            for (var i = 0; i < blockSize; i++)
                outBlock[i] = (byte)(block[i] ^ this._currentIv[i]);

            inBlock.CopyTo(this._currentIv);
        }

        return input.Length;
    }

    /// <summary>
    /// CTS encryption for non-block-aligned input. Processes all full blocks except the last two with standard CBC,
    /// then applies the steal algorithm to the penultimate and final blocks.
    /// </summary>
    /// <param name="input">The plaintext input.</param>
    /// <param name="output">The destination span for the ciphertext.</param>
    /// <param name="fullBlocks">The number of complete blocks in <paramref name="input" />.</param>
    /// <param name="tailBytes">
    /// The number of bytes in the final partial block (1 to <paramref name="blockSize" /> − 1).
    /// </param>
    /// <param name="blockSize">The underlying cipher block size in bytes.</param>
    /// <returns>The number of bytes written to <paramref name="output" />.</returns>
    private int EncryptCts(ReadOnlySpan<byte> input, Span<byte> output, int fullBlocks, int tailBytes, int blockSize)
    {
        // Process all full blocks except the penultimate through standard CBC.
        var bodyBlocks = fullBlocks - 1; // blocks before the CTS pair
        var bodyLength = bodyBlocks * blockSize;

        if (bodyBlocks > 0)
            EncryptCbc(input[..bodyLength], output[..bodyLength]);

        // Penultimate block (full).
        ReadOnlySpan<byte> penultimate = input.Slice(bodyLength, blockSize);
        Span<byte> e = stackalloc byte[blockSize];

        // CBC-encrypt the penultimate block: E = CBC_E(P_{n-1}).
        Span<byte> xored = stackalloc byte[blockSize];
        for (var i = 0; i < blockSize; i++)
            xored[i] = (byte)(penultimate[i] ^ this._currentIv[i]);
        this._cipher.Encrypt(xored, e);

        // Final partial block: P_n (tailBytes bytes).
        ReadOnlySpan<byte> tail = input.Slice(bodyLength + blockSize, tailBytes);

        // Build the padded final block: P_n || E[tailBytes..blockSize].
        Span<byte> padded = stackalloc byte[blockSize];
        tail.CopyTo(padded);
        e[tailBytes..].CopyTo(padded[tailBytes..]);

        // C_n = raw_encrypt(padded) — no CBC chain.
        Span<byte> cn = stackalloc byte[blockSize];
        this._cipher.Encrypt(padded, cn);

        // Output: C_n (full block) then C_{n-1} = E[0..tailBytes].
        cn.CopyTo(output[bodyLength..]);
        e[..tailBytes].CopyTo(output[(bodyLength + blockSize)..]);

        return input.Length;
    }

    /// <summary>
    /// CTS decryption for non-block-aligned input. Reconstructs the penultimate and final plaintext blocks using the
    /// inverse steal, then decrypts all body blocks with CBC.
    /// </summary>
    /// <param name="input">The ciphertext input.</param>
    /// <param name="output">The destination span for the plaintext.</param>
    /// <param name="fullBlocks">The number of complete blocks in <paramref name="input" />.</param>
    /// <param name="tailBytes">
    /// The number of bytes in the final partial block (1 to <paramref name="blockSize" /> − 1).
    /// </param>
    /// <param name="blockSize">The underlying cipher block size in bytes.</param>
    /// <returns>The number of bytes written to <paramref name="output" />.</returns>
    private int DecryptCts(ReadOnlySpan<byte> input, Span<byte> output, int fullBlocks, int tailBytes, int blockSize)
    {
        // Body blocks (all before the CTS pair).
        var bodyBlocks = fullBlocks - 1;
        var bodyLength = bodyBlocks * blockSize;

        if (bodyBlocks > 0)
            DecryptCbc(input[..bodyLength], output[..bodyLength]);

        // In the encrypted stream: [C_n (blockSize)] [C_{n-1} truncated (tailBytes)]
        ReadOnlySpan<byte> cn = input.Slice(bodyLength, blockSize);
        ReadOnlySpan<byte> cnMinus1Trunc = input.Slice(bodyLength + blockSize, tailBytes);

        // Step 1: raw-decrypt C_n → gives us P_n || E[tailBytes..].
        Span<byte> rawDec = stackalloc byte[blockSize];
        this._cipher.Decrypt(cn, rawDec);

        // Step 2: reconstruct E (the full encrypted penultimate block).
        // E[0..tailBytes] = C_{n-1} truncated; E[tailBytes..] = rawDec[tailBytes..].
        Span<byte> e = stackalloc byte[blockSize];
        cnMinus1Trunc.CopyTo(e);
        rawDec[tailBytes..].CopyTo(e[tailBytes..]);

        // Step 3: recover P_n = rawDec[0..tailBytes] (the rest was padding from E).
        rawDec[..tailBytes].CopyTo(output[(bodyLength + blockSize)..]);

        // Step 4: CBC-decrypt e to get P_{n-1}.
        Span<byte> block = stackalloc byte[blockSize];
        this._cipher.Decrypt(e, block);
        Span<byte> penultimateOut = output.Slice(bodyLength, blockSize);
        for (var i = 0; i < blockSize; i++)
            penultimateOut[i] = (byte)(block[i] ^ this._currentIv[i]);

        return input.Length;
    }

    /// <summary>
    /// Releases the resources used by this instance and zeroes the seed and running CBC chaining vector so that
    /// key-equivalent state does not linger in memory after disposal. The underlying <see cref="IBlockCipher" /> is not
    /// disposed by this type — ownership remains with the caller.
    /// </summary>
    /// <remarks>
    /// Idempotent.
    /// </remarks>
    public void Dispose()
    {
        if (this._disposed)
            return;

        CryptoHelpers.Clear(this._iv);
        CryptoHelpers.Clear(this._currentIv);
        this._disposed = true;
        GC.SuppressFinalize(this);
    }
}
