// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies XEX-based Tweaked CodeBook mode with ciphertext Stealing (XTS) to an underlying
/// pair of <see cref="IBlockCipher" /> instances, per IEEE Std 1619-2007 / NIST SP 800-38E.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/xts-mode.svg" alt="XTS data flow — the tweak cipher encrypts the sector number, successive α multiplications in GF(2^128) derive per-block tweaks T_j, and each block is XORed with T_j before and after the data cipher." />
/// </para>
/// <para>
/// XTS requires two independent ciphers keyed with different material:
/// <list type="bullet">
/// <item><description><paramref name="dataCipher" /> (Key₁) — encrypts or decrypts the data. Shown as <b>E_K₁</b> in the central column of the diagram.</description></item>
/// <item><description><paramref name="tweakCipher" /> (Key₂) — encrypts the sector number (tweak). Shown as <b>E_K₂</b> on the left.</description></item>
/// </list>
/// Using the same key for both reduces XTS to a single-key construction and weakens security.
/// </para>
/// <para>
/// For each 128-bit block j in a sector, the XEX construction is:
/// <code>
/// T_j  = α^j ⊗ tweakCipher.Encrypt(tweak)     // Galois field multiplication
/// C_j  = dataCipher.Encrypt(P_j ⊕ T_j) ⊕ T_j  // encrypt
/// P_j  = dataCipher.Decrypt(C_j ⊕ T_j) ⊕ T_j  // decrypt
/// </code>
/// The horizontal tweak bus in the diagram corresponds to this successive <c>·α</c> multiplication:
/// each <b>·α</b> box doubles the tweak in GF(2¹²⁸) so the Tⱼ arriving at cell <em>j</em> is αʲ times the
/// base tweak. The two XOR nodes inside each cell — before and after the data cipher — realise the
/// <c>⊕ T_j</c> pairs in the equation above.
/// </para>
/// <para>
/// GF(2^128) multiplication uses the primitive polynomial x^128 + x^7 + x^2 + x + 1 with
/// little-endian bit representation (byte 0, bit 0 = coefficient of x^0), identical to IEEE 1619.
/// </para>
/// </remarks>
public sealed class XtsModeTransform : IBlockCipherModeTransform
{
    private readonly IBlockCipher _dataCipher;
    private readonly IBlockCipher _tweakCipher;
    private readonly byte[] _tweak; // sector number / tweak value

    /// <summary>
    /// Initializes a new instance of the <see cref="XtsModeTransform" /> class.
    /// </summary>
    /// <param name="dataCipher">
    /// The cipher keyed with Key₁, used to encrypt or decrypt data blocks.
    /// </param>
    /// <param name="tweakCipher">
    /// The cipher keyed with Key₂, used to encrypt the sector number. Must have the same
    /// block size as <paramref name="dataCipher" />.
    /// </param>
    /// <param name="tweak">
    /// The sector number encoded as a block-size byte array in little-endian order.
    /// A defensive copy is taken.
    /// </param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// Block sizes differ, or <paramref name="tweak" /> length does not equal the block size.
    /// </exception>
    public XtsModeTransform(IBlockCipher dataCipher, IBlockCipher tweakCipher, byte[] tweak)
    {
        this._dataCipher = dataCipher ?? throw new ArgumentNullException(nameof(dataCipher));
        this._tweakCipher = tweakCipher ?? throw new ArgumentNullException(nameof(tweakCipher));
        if (tweak is null) throw new ArgumentNullException(nameof(tweak));
        if (tweakCipher.BlockSize != dataCipher.BlockSize)
            throw new ArgumentException(
                $"tweakCipher block size ({tweakCipher.BlockSize}) must equal dataCipher block size ({dataCipher.BlockSize}).",
                nameof(tweakCipher));
        if (tweak.Length != dataCipher.BlockSize)
            throw new ArgumentException(
                $"Tweak length ({tweak.Length}) must equal the cipher block size ({dataCipher.BlockSize}).",
                nameof(tweak));

        this._tweak = (byte[])tweak.Clone();
    }

    /// <inheritdoc />
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        int blockSize = this._dataCipher.BlockSize;
        if (input.Length % blockSize != 0)
            throw new ArgumentException(
                $"XTS requires block-aligned input; {input.Length} is not a multiple of {blockSize}.", nameof(input));
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

        // T_0 = tweakCipher.Encrypt(sector_number)
        Span<byte> T = stackalloc byte[blockSize];
        this._tweakCipher.Encrypt(this._tweak, T);

        Span<byte> buf = stackalloc byte[blockSize];

        for (int offset = 0; offset < input.Length; offset += blockSize)
        {
            ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
            Span<byte> outBlock = output.Slice(offset, blockSize);

            // XEX: out = cipher(in XOR T) XOR T
            for (int i = 0; i < blockSize; i++) buf[i] = (byte)(inBlock[i] ^ T[i]);
            if (encrypt)
                this._dataCipher.Encrypt(buf, outBlock);
            else
                this._dataCipher.Decrypt(buf, outBlock);
            for (int i = 0; i < blockSize; i++) outBlock[i] ^= T[i];

            // Advance tweak: T = α ⊗ T in GF(2^128), little-endian, poly 0x87 reduction.
            GfDouble(T);
        }

        return input.Length;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Multiplies <paramref name="T" /> by α in GF(2^128) using the IEEE 1619 polynomial
    /// x^128 + x^7 + x^2 + x + 1, with little-endian bit ordering (byte 0 = x^0..x^7).
    /// </summary>
    /// <param name="T">The 16-byte tweak block to double in GF(2<sup>128</sup>); updated in place.</param>
    private static void GfDouble(Span<byte> T)
    {
        // Left-shift the 128-bit little-endian value. Carry propagates from byte[0] upward.
        int carry = 0;
        for (int i = 0; i < T.Length; i++)
        {
            int t = T[i];
            T[i] = (byte)((t << 1) | carry);
            carry = t >> 7;
        }
        // If the MSB of byte[15] was set (= x^127 coefficient), reduce by 0x87 (= x^7+x^2+x+1).
        if (carry != 0) T[0] ^= 0x87;
    }
}
