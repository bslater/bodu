// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128Cipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the canonical <c>Serpent</c> block cipher, which operates on 128-bit (16-byte) blocks using a 128, 192,
/// or 256-bit key.
/// </summary>
/// <remarks>
/// <para>
/// Serpent is a 32-round substitution–permutation network designed by Ross Anderson, Eli Biham, and Lars Knudsen as an
/// Advanced Encryption Standard (AES) candidate. Each round applies a round-key XOR, one of the eight 4-bit S-boxes
/// <c>S0..S7</c>, and the bitsliced linear transformation <c>L</c>. The final round replaces <c>L</c> with a post-round
/// key XOR. Shorter keys are padded to 256 bits by appending a <c>1</c> bit followed by zeros, per the Serpent
/// specification.
/// </para>
/// <para>
/// This class implements the standard Serpent-128 block primitive only. It is interoperable with canonical Serpent test
/// vectors and does not use the tweak schedule or widened state used by the non-standard wide-block variants.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Serpent128" /> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> contract. Use <see cref="Serpent128Cipher" />
/// directly only when composing the raw block primitive with an <see cref="IBlockCipherModeTransform" /> or
/// <see cref="IPaddingStrategy" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Direct single-block use. For most workloads prefer the Serpent128 SymmetricAlgorithm wrapper.
/// byte[] key = new byte[32];   // 128, 192, or 256 bits — Serpent pads shorter keys to 256
/// RandomNumberGenerator.Fill(key);
///
/// using var cipher = new Serpent128Cipher(key);
///
/// byte[] plaintext  = new byte[16];   // one 128-bit block
/// byte[] ciphertext = new byte[16];
/// cipher.Encrypt(plaintext, ciphertext);
///
/// byte[] roundtrip = new byte[16];
/// cipher.Decrypt(ciphertext, roundtrip);
/// // roundtrip equals plaintext
///]]>
/// </example>
/// <seealso cref="Serpent128"/>
public sealed class Serpent128Cipher
    : SerpentBlockCipherBase
{
    /// <summary>
    /// Length of the Serpent block is 128 bits (16 bytes). Internal constant kept for span-length validation; callers
    /// should read <see cref="BlockSize" /> instead.
    /// </summary>
    private const int BlockSizeBits = 128;

    /// <summary>
    /// The number of cipher rounds executed by Serpent.
    /// </summary>
    /// <remarks>
    /// Canonical Serpent always uses 32 rounds regardless of whether the input key is 128, 192, or 256 bits.
    /// </remarks>
    private const int RoundCount = 32;

    /// <summary>
    /// The number of 32-bit round-key words (<c>(RoundCount + 1) * 4 = 132</c>).
    /// </summary>
    /// <remarks>
    /// Serpent requires one 128-bit subkey for each of the 32 rounds plus one final post-S-box whitening subkey.
    /// </remarks>
    private const int RoundKeyWordCount = (RoundCount + 1) * 4;

    /// <summary>
    /// The expanded round keys (<c>K_0..K_32</c>), each four 32-bit words, laid out contiguously as 132 words.
    /// </summary>
    /// <remarks>
    /// Round key <c>K_r</c> starts at offset <c>r * 4</c>. Encryption consumes <c>K_0..K_31</c> before each S-box layer
    /// and <c>K_32</c> after the final S-box layer.
    /// </remarks>
    private readonly uint[] _roundKeys;

    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent128Cipher" /> class using the specified key.
    /// </summary>
    /// <param name="key">
    /// The Serpent key. Length must be 16, 24, or 32 bytes (128, 192, or 256 bits). Shorter keys are padded to 256 bits
    /// per the Serpent specification.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> does not have a length of 16, 24, or 32 bytes.
    /// </exception>
    public Serpent128Cipher(ReadOnlySpan<byte> key)
    {
        if (key.Length is not 16 and not 24 and not 32)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_KeySize, key.Length * 8, "128, 192, 256"),
                nameof(key));
        }

        // Expand the supplied 128-, 192-, or 256-bit key into the 33 canonical Serpent round keys.
        _roundKeys = new uint[RoundKeyWordCount];
        BuildRoundKeys(key, _roundKeys);
    }

    /// <inheritdoc />
    /// <value>Length of the Serpent block is 128 bits (16 bytes).</value>
    public override int BlockSize => BlockSizeBits;

    /// <inheritdoc />
    public override void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        if (input.Length != BlockSizeBits / 8 || output.Length != BlockSizeBits / 8)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_BlockLength, BlockSizeBits / 8));
        }

        // Load the 128-bit plaintext block as four little-endian 32-bit words. This is the canonical Serpent bitslice
        // representation used by the shared S-box and linear-transform helpers.
        uint x0 = BinaryReadUInt32LE(input, 0);
        uint x1 = BinaryReadUInt32LE(input, 4);
        uint x2 = BinaryReadUInt32LE(input, 8);
        uint x3 = BinaryReadUInt32LE(input, 12);

        uint[] rk = _roundKeys;

        // Rounds 0..30: XOR the round key, apply S_r where r cycles modulo 8, then apply the Serpent linear transform.
        // The linear transform is omitted only from the final round.
        for (int r = 0; r < RoundCount - 1; r++)
        {
            int k = r * 4;
            x0 ^= rk[k];
            x1 ^= rk[k + 1];
            x2 ^= rk[k + 2];
            x3 ^= rk[k + 3];

            ApplySBox(r & 7, ref x0, ref x1, ref x2, ref x3);
            LinearTransform(ref x0, ref x1, ref x2, ref x3);
        }

        // Final round: Serpent performs the final key XOR and S-box layer without the linear transform, then applies
        // the extra post-round key K_32 as output whitening.
        int kFinal = (RoundCount - 1) * 4;
        x0 ^= rk[kFinal];
        x1 ^= rk[kFinal + 1];
        x2 ^= rk[kFinal + 2];
        x3 ^= rk[kFinal + 3];

        ApplySBox((RoundCount - 1) & 7, ref x0, ref x1, ref x2, ref x3);

        int kPost = RoundCount * 4;
        x0 ^= rk[kPost];
        x1 ^= rk[kPost + 1];
        x2 ^= rk[kPost + 2];
        x3 ^= rk[kPost + 3];

        // Store the ciphertext using the same little-endian 32-bit word layout used for input.
        BinaryWriteUInt32LE(output, 0, x0);
        BinaryWriteUInt32LE(output, 4, x1);
        BinaryWriteUInt32LE(output, 8, x2);
        BinaryWriteUInt32LE(output, 12, x3);
    }

    /// <inheritdoc />
    public override void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        if (input.Length != BlockSizeBits / 8 || output.Length != BlockSizeBits / 8)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_BlockLength, BlockSizeBits / 8));
        }

        // Load the 128-bit ciphertext block as four little-endian 32-bit words.
        uint x0 = BinaryReadUInt32LE(input, 0);
        uint x1 = BinaryReadUInt32LE(input, 4);
        uint x2 = BinaryReadUInt32LE(input, 8);
        uint x3 = BinaryReadUInt32LE(input, 12);

        uint[] rk = _roundKeys;

        // Reverse the final encryption round: remove post-round key K_32, apply inverse S_31, then remove K_31.
        int kPost = RoundCount * 4;
        x0 ^= rk[kPost];
        x1 ^= rk[kPost + 1];
        x2 ^= rk[kPost + 2];
        x3 ^= rk[kPost + 3];

        ApplyInverseSBox((RoundCount - 1) & 7, ref x0, ref x1, ref x2, ref x3);

        int kFinal = (RoundCount - 1) * 4;
        x0 ^= rk[kFinal];
        x1 ^= rk[kFinal + 1];
        x2 ^= rk[kFinal + 2];
        x3 ^= rk[kFinal + 3];

        // Reverse rounds 30..0. The inverse order is linear transform, inverse S-box, then round-key XOR because
        // encryption applied key XOR, S-box, then linear transform.
        for (int r = RoundCount - 2; r >= 0; r--)
        {
            InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
            ApplyInverseSBox(r & 7, ref x0, ref x1, ref x2, ref x3);

            int k = r * 4;
            x0 ^= rk[k];
            x1 ^= rk[k + 1];
            x2 ^= rk[k + 2];
            x3 ^= rk[k + 3];
        }

        // Store the recovered plaintext using the same little-endian 32-bit word layout.
        BinaryWriteUInt32LE(output, 0, x0);
        BinaryWriteUInt32LE(output, 4, x1);
        BinaryWriteUInt32LE(output, 8, x2);
        BinaryWriteUInt32LE(output, 12, x3);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        // Round keys are derived from secret key material and are zeroed in both disposal paths
        // so they are not retained if the finalizer runs before an explicit Dispose call.
        CryptographyHelper.Clear(_roundKeys);

        base.Dispose(disposing);
    }

    /// <summary>
    /// Reads a little-endian <see cref="uint" /> from <paramref name="buffer" /> at the specified
    /// <paramref name="offset" />.
    /// </summary>
    /// <param name="buffer">The source byte span.</param>
    /// <param name="offset">The byte offset at which to read.</param>
    /// <returns>The little-endian <see cref="uint" /> value read from <paramref name="buffer" />.</returns>
    /// <remarks>
    /// Serpent blocks are represented internally as four little-endian 32-bit words. Keeping this helper local makes
    /// the block layout explicit at each load site.
    /// </remarks>
    private static uint BinaryReadUInt32LE(ReadOnlySpan<byte> buffer, int offset) =>
        System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4));

    /// <summary>
    /// Writes <paramref name="value" /> to <paramref name="buffer" /> at the specified <paramref name="offset" /> in
    /// little-endian byte order.
    /// </summary>
    /// <param name="buffer">The destination byte span.</param>
    /// <param name="offset">The byte offset at which to write.</param>
    /// <param name="value">The value to write.</param>
    /// <remarks>
    /// Serpent blocks are emitted as four little-endian 32-bit words, matching the load format and canonical
    /// test-vector representation used by this implementation.
    /// </remarks>
    private static void BinaryWriteUInt32LE(Span<byte> buffer, int offset, uint value) =>
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(offset, 4), value);

    /// <summary>
    /// Expands <paramref name="key" /> into the 132-word round-key schedule.
    /// </summary>
    /// <param name="key">
    /// The Serpent key (16, 24, or 32 bytes). Keys shorter than 32 bytes are padded per the Serpent specification.
    /// </param>
    /// <param name="roundKeys">
    /// The destination buffer, which must have exactly <see cref="RoundKeyWordCount" /> entries.
    /// </param>
    /// <remarks>
    /// <para>
    /// Serpent first pads any key shorter than 256 bits by appending a single <c>1</c> bit followed by zeros. Because
    /// valid key sizes are byte-aligned here, this implementation writes <c>0x01</c> immediately after the supplied key
    /// bytes and clears the rest of the 32-byte key buffer.
    /// </para>
    /// <para>
    /// The eight padded words seed the Serpent prekey recurrence. The generated prekey tail is then transformed in
    /// groups of four words using the key-schedule S-box order
    /// <c>K_0 → S3, K_1 → S2, K_2 → S1, K_3 → S0, K_4 → S7, ...</c> to produce <c>K_0..K_32</c>.
    /// </para>
    /// </remarks>
    private static void BuildRoundKeys(ReadOnlySpan<byte> key, uint[] roundKeys)
    {
        // Build the 256-bit padded key buffer. A 256-bit key is used unchanged; 128- and 192-bit keys receive the Serpent
        // 1-bit terminator followed by zeros.
        Span<byte> paddedKey = stackalloc byte[32];
        paddedKey.Clear();
        key.CopyTo(paddedKey);

        if (key.Length < 32)
            paddedKey[key.Length] = 0x01;

        // Interpret the padded key as eight little-endian 32-bit seed words w[-8]..w[-1].
        Span<uint> seed = stackalloc uint[8];
        for (int i = 0; i < 8; i++)
            seed[i] = BinaryReadUInt32LE(paddedKey, i * 4);

        // Prekey layout: w[-8..-1] followed by w[0..131], laid out contiguously as 140 words.
        // ExpandPrekeys copies the seed and fills the generated tail using the Serpent recurrence.
        Span<uint> prekeys = stackalloc uint[8 + RoundKeyWordCount];
        ExpandPrekeys(seed, prekeys, 8);

        // Apply the rotating S-box schedule to successive 4-word groups of the generated prekey tail, producing K_0..K_32.
        for (int r = 0; r <= RoundCount; r++)
        {
            int src = 8 + (r * 4);
            uint x0 = prekeys[src];
            uint x1 = prekeys[src + 1];
            uint x2 = prekeys[src + 2];
            uint x3 = prekeys[src + 3];

            ApplySBox(KeyScheduleSBoxIndex(r), ref x0, ref x1, ref x2, ref x3);

            int dst = r * 4;
            roundKeys[dst] = x0;
            roundKeys[dst + 1] = x1;
            roundKeys[dst + 2] = x2;
            roundKeys[dst + 3] = x3;
        }

        CryptographyHelper.Clear(prekeys);
        CryptographyHelper.Clear(seed);
        CryptographyHelper.Clear(paddedKey);
    }
}
