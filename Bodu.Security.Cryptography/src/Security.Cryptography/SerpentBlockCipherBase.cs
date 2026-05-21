// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentBlockCipherBase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for managed Serpent block cipher engines, providing the shared S-boxes, bitsliced
/// linear transform, prekey recurrence, and resource-disposal plumbing used by the standard <c>Serpent-128</c> variant
/// and the non-standard wide-block tweakable variants (<c>Serpent-256</c>, <c>Serpent-512</c>, <c>Serpent-1024</c>).
/// </summary>
/// <remarks>
/// <para>
/// Derived classes supply the state width (in 32-bit words), the round count, and their own <see cref="Encrypt" /> and
/// <see cref="Decrypt" /> implementations. The base class exposes the Serpent S-boxes <c>S0..S7</c> and their inverses,
/// the bitsliced linear transform <see cref="LinearTransform" /> and its inverse <see cref="InverseLinearTransform" />,
/// and the round-key expansion helper <see cref="ExpandPrekeys" /> driven by the golden-ratio constant
/// <c>phi = 0x9E3779B9</c>.
/// </para>
/// <para>
/// Serpent operates on four 32-bit words in bitsliced form. Each bit position across those four words represents one
/// 4-bit S-box input. The helpers in this base class keep that representation explicit so the concrete ciphers can
/// share the standard S-box, inverse S-box, linear-transform, and key-schedule operations.
/// </para>
/// <para>
/// External callers cannot derive new variants: the constructor and protected members are scoped
/// <c>private protected</c>. Use <see cref="Serpent128Cipher" /> or one of the wide-block
/// <see cref="Serpent256Cipher" /> / <see cref="Serpent512Cipher" /> / <see cref="Serpent1024Cipher" /> types directly,
/// or compose with <see cref="IBlockCipherModeTransform" /> via <see cref="BlockCipherModeFactory" />.
/// </para>
/// </remarks>
public abstract partial class SerpentBlockCipherBase
    : IBlockCipher
{
    /// <summary>
    /// The golden-ratio fractional constant used in the Serpent prekey recurrence.
    /// </summary>
    /// <remarks>
    /// Serpent defines this value as <c>floor((sqrt(5) - 1) * 2^31)</c>, encoded as <c>0x9E3779B9</c>. It is mixed into
    /// every generated prekey word together with the word index before the 11-bit rotation.
    /// </remarks>
    private protected const uint Phi = 0x9E3779B9u;

    /// <summary>
    /// Indicates whether the instance has been disposed.
    /// </summary>
    private protected bool _disposed;

    /// <summary>
    /// The eight Serpent S-boxes, indexed <c>[sboxIndex * 16 + nibble]</c>.
    /// </summary>
    /// <remarks>
    /// These are the fixed 4-bit Serpent substitution boxes <c>S0..S7</c>. They are applied in bitsliced form to 32
    /// parallel 4-bit columns rather than to individual nibbles in memory order.
    /// </remarks>
    private static readonly byte[] s_sBoxes =
    [

        // S0
        3, 8, 15, 1, 10, 6, 5, 11, 14, 13, 4, 2, 7, 0, 9, 12,

        // S1
        15, 12, 2, 7, 9, 0, 5, 10, 1, 11, 14, 8, 6, 13, 3, 4,

        // S2
        8, 6, 7, 9, 3, 12, 10, 15, 13, 1, 14, 4, 0, 11, 5, 2,

        // S3
        0, 15, 11, 8, 12, 9, 6, 3, 13, 1, 2, 4, 10, 7, 5, 14,

        // S4
        1, 15, 8, 3, 12, 0, 11, 6, 2, 5, 4, 10, 9, 14, 7, 13,

        // S5
        15, 5, 2, 11, 4, 10, 9, 12, 0, 3, 14, 8, 13, 6, 7, 1,

        // S6
        7, 2, 12, 5, 8, 4, 6, 11, 14, 9, 1, 15, 13, 3, 10, 0,

        // S7
        1, 13, 15, 0, 14, 8, 2, 11, 7, 4, 12, 10, 9, 3, 5, 6,
    ];

    /// <summary>
    /// The eight Serpent inverse S-boxes, indexed <c>[sboxIndex * 16 + nibble]</c>.
    /// </summary>
    /// <remarks>
    /// These tables reverse <see cref="s_sBoxes" /> during decryption. For every S-box index <c>s</c> and nibble
    /// <c>x</c>, <c>InvS_s[S_s[x]] == x</c>.
    /// </remarks>
    private static readonly byte[] s_invSBoxes =
    [

        // InvS0
        13, 3, 11, 0, 10, 6, 5, 12, 1, 14, 4, 7, 15, 9, 8, 2,

        // InvS1
        5, 8, 2, 14, 15, 6, 12, 3, 11, 4, 7, 9, 1, 13, 10, 0,

        // InvS2
        12, 9, 15, 4, 11, 14, 1, 2, 0, 3, 6, 13, 5, 8, 10, 7,

        // InvS3
        0, 9, 10, 7, 11, 14, 6, 13, 3, 5, 12, 2, 4, 8, 15, 1,

        // InvS4
        5, 0, 8, 3, 10, 9, 7, 14, 2, 12, 11, 6, 4, 15, 13, 1,

        // InvS5
        8, 15, 2, 9, 4, 1, 13, 14, 11, 6, 5, 3, 7, 12, 10, 0,

        // InvS6
        15, 10, 1, 13, 5, 3, 6, 0, 4, 9, 14, 7, 2, 12, 8, 11,

        // InvS7
        3, 0, 6, 13, 9, 14, 15, 8, 5, 12, 11, 7, 10, 1, 4, 2,
    ];

    /// <summary>
    /// Finalizes an instance of the <see cref="SerpentBlockCipherBase" /> class.
    /// </summary>
    /// <remarks>
    /// The finalizer delegates to the standard dispose pattern so derived cipher implementations can clear round-key
    /// material even if callers fail to dispose the instance explicitly.
    /// </remarks>
    ~SerpentBlockCipherBase()
    {
        this.Dispose(false);
    }

    /// <inheritdoc />
    public abstract int BlockSize { get; }

    /// <inheritdoc />
    public abstract void Decrypt(ReadOnlySpan<byte> input, Span<byte> output);

    /// <inheritdoc />
    public abstract void Encrypt(ReadOnlySpan<byte> input, Span<byte> output);

    /// <inheritdoc />
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases all internal buffers and sensitive material.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> when invoked from <see cref="Dispose()" />; <see langword="false" /> when invoked from
    /// the finalizer.
    /// </param>
    /// <remarks>
    /// The base implementation records the disposed state. Derived Serpent implementations should override this method
    /// to clear expanded round keys, tweak material, and other sensitive buffers before calling
    /// <c>base.Dispose(disposing)</c>.
    /// </remarks>
    protected virtual void Dispose(bool disposing) => this._disposed = true;

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed() =>
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif


    /// <summary>
    /// Applies the Serpent S-box identified by <paramref name="sBoxIndex" /> to the four 32-bit words in bitsliced
    /// form.
    /// </summary>
    /// <param name="sBoxIndex">The S-box index in the range <c>0..7</c>.</param>
    /// <param name="x0">The first state word, modified in place.</param>
    /// <param name="x1">The second state word, modified in place.</param>
    /// <param name="x2">The third state word, modified in place.</param>
    /// <param name="x3">The fourth state word, modified in place.</param>
    /// <remarks>
    /// <para>
    /// Each of the 32 bit columns across the four words is treated as a 4-bit nibble: bit 0 from <paramref name="x0" />
    /// , bit 1 from <paramref name="x1" />, bit 2 from <paramref name="x2" />, and bit 3 from <paramref name="x3" />.
    /// The selected S-box maps that nibble to a replacement 4-bit value, whose output bits are written back into the
    /// same bit position of the four result words.
    /// </para>
    /// <para>
    /// This is the canonical Serpent bitslice representation. The implementation favors portability and readability
    /// over hand-minimized Boolean gate expressions; correctness is anchored against standard Serpent test vectors.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static void ApplySBox(int sBoxIndex, ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        ReadOnlySpan<byte> table = s_sBoxes.AsSpan(sBoxIndex * 16, 16);
        uint y0 = 0, y1 = 0, y2 = 0, y3 = 0;

        // Process the four-word state as 32 independent bitsliced S-box inputs.
        for (var i = 0; i < 32; i++)
        {
            // Gather one 4-bit input from bit position i across x0..x3.
            var nibble = (int)(((x0 >> i) & 1u)
                             | (((x1 >> i) & 1u) << 1)
                             | (((x2 >> i) & 1u) << 2)
                             | (((x3 >> i) & 1u) << 3));

            // Substitute through S{sBoxIndex} and scatter the resulting four output bits back into y0..y3.
            int s = table[nibble];
            y0 |= (uint)(s & 1) << i;
            y1 |= (uint)((s >> 1) & 1) << i;
            y2 |= (uint)((s >> 2) & 1) << i;
            y3 |= (uint)((s >> 3) & 1) << i;
        }

        x0 = y0;
        x1 = y1;
        x2 = y2;
        x3 = y3;
    }

    /// <summary>
    /// Applies the inverse of the Serpent S-box identified by <paramref name="sBoxIndex" /> to the four 32-bit words in
    /// bitsliced form.
    /// </summary>
    /// <param name="sBoxIndex">The inverse S-box index in the range <c>0..7</c>.</param>
    /// <param name="x0">The first state word, modified in place.</param>
    /// <param name="x1">The second state word, modified in place.</param>
    /// <param name="x2">The third state word, modified in place.</param>
    /// <param name="x3">The fourth state word, modified in place.</param>
    /// <remarks>
    /// Decryption uses the same bitsliced gather/scatter representation as <see cref="ApplySBox" />, but substitutes
    /// through <c>InvS0..InvS7</c> so each round can reverse the corresponding encryption S-box layer.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static void ApplyInverseSBox(int sBoxIndex, ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        ReadOnlySpan<byte> table = s_invSBoxes.AsSpan(sBoxIndex * 16, 16);
        uint y0 = 0, y1 = 0, y2 = 0, y3 = 0;

        // Process the four-word state as 32 independent inverse S-box inputs.
        for (var i = 0; i < 32; i++)
        {
            // Gather one 4-bit input from bit position i across x0..x3.
            var nibble = (int)(((x0 >> i) & 1u)
                             | (((x1 >> i) & 1u) << 1)
                             | (((x2 >> i) & 1u) << 2)
                             | (((x3 >> i) & 1u) << 3));

            // Substitute through InvS{sBoxIndex} and scatter the original four bits back into y0..y3.
            int s = table[nibble];
            y0 |= (uint)(s & 1) << i;
            y1 |= (uint)((s >> 1) & 1) << i;
            y2 |= (uint)((s >> 2) & 1) << i;
            y3 |= (uint)((s >> 3) & 1) << i;
        }

        x0 = y0;
        x1 = y1;
        x2 = y2;
        x3 = y3;
    }

    /// <summary>
    /// Applies the Serpent bitsliced linear transform <c>L</c> to a four-word sub-state.
    /// </summary>
    /// <param name="x0">The first state word, modified in place.</param>
    /// <param name="x1">The second state word, modified in place.</param>
    /// <param name="x2">The third state word, modified in place.</param>
    /// <param name="x3">The fourth state word, modified in place.</param>
    /// <remarks>
    /// Serpent applies this linear diffusion layer after the S-box layer in every encryption round except the final
    /// round. The sequence below follows the specification exactly: rotate <c>x0</c> and <c>x2</c>, mix by XOR and
    /// shifts, rotate <c>x1</c> and <c>x3</c>, mix again, then apply the final rotations to <c>x0</c> and <c>x2</c>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static void LinearTransform(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        // Initial word rotations: x0 <<< 13 and x2 <<< 3.
        x0 = BitOperations.RotateLeft(x0, 13);
        x2 = BitOperations.RotateLeft(x2, 3);

        // First diffusion mix.
        x1 ^= x0 ^ x2;
        x3 ^= x2 ^ (x0 << 3);

        // Middle rotations: x1 <<< 1 and x3 <<< 7.
        x1 = BitOperations.RotateLeft(x1, 1);
        x3 = BitOperations.RotateLeft(x3, 7);

        // Second diffusion mix.
        x0 ^= x1 ^ x3;
        x2 ^= x3 ^ (x1 << 7);

        // Final word rotations: x0 <<< 5 and x2 <<< 22.
        x0 = BitOperations.RotateLeft(x0, 5);
        x2 = BitOperations.RotateLeft(x2, 22);
    }

    /// <summary>
    /// Applies the inverse of the Serpent bitsliced linear transform to a four-word sub-state.
    /// </summary>
    /// <param name="x0">The first state word, modified in place.</param>
    /// <param name="x1">The second state word, modified in place.</param>
    /// <param name="x2">The third state word, modified in place.</param>
    /// <param name="x3">The fourth state word, modified in place.</param>
    /// <remarks>
    /// Decryption applies this inverse diffusion layer before the inverse S-box for all non-final rounds. The
    /// operations are the exact reverse of <see cref="LinearTransform" />, with rotations inverted and XOR mixes
    /// replayed in reverse order.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static void InverseLinearTransform(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        // Undo final word rotations from L: x2 >>> 22 and x0 >>> 5.
        x2 = BitOperations.RotateRight(x2, 22);
        x0 = BitOperations.RotateRight(x0, 5);

        // Undo the second diffusion mix.
        x2 ^= x3 ^ (x1 << 7);
        x0 ^= x1 ^ x3;

        // Undo middle rotations from L: x3 >>> 7 and x1 >>> 1.
        x3 = BitOperations.RotateRight(x3, 7);
        x1 = BitOperations.RotateRight(x1, 1);

        // Undo the first diffusion mix.
        x3 ^= x2 ^ (x0 << 3);
        x1 ^= x0 ^ x2;

        // Undo initial word rotations from L: x2 >>> 3 and x0 >>> 13.
        x2 = BitOperations.RotateRight(x2, 3);
        x0 = BitOperations.RotateRight(x0, 13);
    }

    /// <summary>
    /// Runs the Serpent prekey recurrence to populate the prekey buffer.
    /// </summary>
    /// <param name="seed">
    /// The first <paramref name="window" /> entries of <paramref name="prekeys" />, already seeded with the key
    /// material.
    /// </param>
    /// <param name="prekeys">
    /// The buffer receiving the expanded prekey sequence. Must start with <paramref name="seed" />.
    /// </param>
    /// <param name="window">
    /// The recurrence window in words. Must be at least 8 to satisfy the Serpent recurrence indices
    /// <c>i-8, i-5, i-3, i-1</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// The recurrence is the Serpent key schedule expansion
    /// <c>w[i] = ROL(w[i-window] ^ w[i-5] ^ w[i-3] ^ w[i-1] ^ phi ^ i, 11)</c> for
    /// <c>i = 0..prekeys.Length - window - 1</c>. The caller initializes <c>prekeys[0..window-1]</c> with the seed and
    /// this helper computes the remaining entries in place.
    /// </para>
    /// <para>
    /// Standard Serpent-128 uses <c>window = 8</c>. The wide-block variants use larger windows matched to their state
    /// width, but keep the same recurrence shape so the indices remain relative to the active state window.
    /// </para>
    /// </remarks>
    private protected static void ExpandPrekeys(ReadOnlySpan<uint> seed, Span<uint> prekeys, int window)
    {
        // Seed the recurrence with the padded key words prepared by the concrete cipher implementation.
        seed.CopyTo(prekeys);

        for (var i = 0; i + window < prekeys.Length; i++)
        {
            // Compute the next prekey word from the Serpent recurrence, then rotate left by 11 bits.
            // The use of i + window maps the specification's w[i] output onto the caller's seeded buffer layout.
            var value = prekeys[i] ^ prekeys[i + window - 5] ^ prekeys[i + window - 3] ^ prekeys[i + window - 1] ^ Phi ^ (uint)i;
            prekeys[i + window] = BitOperations.RotateLeft(value, 11);
        }
    }

    /// <summary>
    /// Returns the Serpent S-box index used by round-key <paramref name="roundIndex" /> in the key schedule.
    /// </summary>
    /// <param name="roundIndex">The round-key index, in the range <c>0..R</c>.</param>
    /// <returns>The S-box index, in the range <c>0..7</c>.</returns>
    /// <remarks>
    /// Serpent's key schedule applies the S-boxes to successive prekey words in descending cyclic order:
    /// <c>K_0 → S3, K_1 → S2, K_2 → S1, K_3 → S0, K_4 → S7, …</c>, following the standard formula
    /// <c>(3 − roundIndex) mod 8</c>. The same ordering is reused for the wide-block variants so that <c>K_0</c> always
    /// uses <c>S3</c>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static int KeyScheduleSBoxIndex(int roundIndex)
    {
        // Bitwise AND with 7 performs modulo 8 for the descending S-box schedule.
        var value = (3 - roundIndex) & 7;
        return value;
    }
}
