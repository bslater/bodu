// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Buffers.Binary;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for the non-standard wide-block tweakable Serpent engines
/// (<see cref="Serpent256Cipher" />, <see cref="Serpent512Cipher" />, <see cref="Serpent1024Cipher" />).
/// </summary>
/// <remarks>
/// <para>
/// This type extends <see cref="SerpentBlockCipherBase" /> with a 128-bit tweak schedule expressed as five cycling 32-bit
/// entries <c>[T0, T1, T2, T3, T0 ^ T1 ^ T2 ^ T3]</c> (the 32-bit analogue of the Threefish <c>[T0, T1, T0 ^ T1]</c> layout)
/// and with a round-key schedule sized to match the variant's block width. Derived classes specify the state width (in 32-bit
/// words) and round count; this class builds the expanded round keys and tweak schedule used at every round-key injection
/// point.
/// </para>
/// <note type="important">
/// The wide-block tweakable Serpent family is a **non-standard, experimental construction** developed for this library. It is
/// not interoperable with canonical Serpent implementations at any block size, and its cryptographic properties have not been
/// externally analysed. Use the canonical <see cref="Serpent128Cipher" /> when Serpent compatibility is required.
/// </note>
/// </remarks>
public abstract partial class SerpentBlockCipher
    : SerpentBlockCipherBase
{
    /// <summary>
    /// The 128-bit tweak length in bytes.
    /// </summary>
    private protected const int TweakSizeBytes = 16;

    /// <summary>
    /// The expanded tweak schedule — five cycling 32-bit entries
    /// <c>[T0, T1, T2, T3, T0 ^ T1 ^ T2 ^ T3]</c> — XOR-injected at the tail of the state every four rounds.
    /// </summary>
    private protected readonly uint[] TweakSchedule;

    /// <summary>
    /// The expanded round-key schedule, laid out as <c>(Rounds + 1) * BlockWords</c> contiguous 32-bit words.
    /// </summary>
    private protected readonly uint[] RoundKeys;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerpentBlockCipher" /> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">
    /// The encryption key. Its length in bytes must equal the variant block size (<see cref="BlockSize" />).
    /// </param>
    /// <param name="tweak">The 16-byte (128-bit) tweak value.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> or <paramref name="tweak" /> does not have the expected length.
    /// </exception>
    private protected SerpentBlockCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
    {
        if (key.Length != this.BlockSize)
            throw new ArgumentException(
                string.Format(ResourceStrings.CryptographicException_InvalidKeySize, key.Length * 8, this.BlockSize * 8),
                nameof(key));

        if (tweak.Length != TweakSizeBytes)
            throw new ArgumentException(
                string.Format(ResourceStrings.CryptographicException_InvalidTweakSize, tweak.Length * 8, TweakSizeBytes * 8),
                nameof(tweak));

        this.TweakSchedule = new uint[5];
        BuildTweakSchedule(tweak, this.TweakSchedule);

        this.RoundKeys = new uint[(this.Rounds + 1) * this.BlockWords];
        this.BuildRoundKeys(key);
    }

    /// <summary>
    /// Gets the number of 32-bit state words in a single block. Equal to <see cref="IBlockCipher.BlockSize" /> divided by 4.
    /// </summary>
    private protected abstract int BlockWords { get; }

    /// <summary>
    /// Gets the total number of cipher rounds executed by this variant.
    /// </summary>
    private protected abstract int Rounds { get; }

    /// <inheritdoc />
    public override void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        if (input.Length != this.BlockSize || output.Length != this.BlockSize)
            throw new ArgumentException(
                string.Format(ResourceStrings.CryptographicException_InvalidBlockLength, this.BlockSize));

        int w = this.BlockWords;
        int rounds = this.Rounds;

        Span<uint> state = stackalloc uint[32].Slice(0, w);
        for (int i = 0; i < w; i++)
            state[i] = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(i * 4, 4));

        uint[] rk = this.RoundKeys;
        uint[] tw = this.TweakSchedule;
        int injection = 0;

        for (int r = 0; r < rounds - 1; r++)
        {
            XorRoundKey(state, rk, r * w);
            ApplySBoxLayer(state, r & 7);
            ApplyLinearLayer(state);

            if (((r + 1) & 3) == 0)
            {
                injection++;
                state[w - 3] ^= tw[injection % 5];
                state[w - 2] ^= tw[(injection + 1) % 5];
                state[w - 1] ^= (uint)injection;
            }
        }

        // Final round — no linear transform, but a post-round key XOR.
        XorRoundKey(state, rk, (rounds - 1) * w);
        ApplySBoxLayer(state, (rounds - 1) & 7);
        XorRoundKey(state, rk, rounds * w);

        for (int i = 0; i < w; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(i * 4, 4), state[i]);

        CryptoHelpers.Clear(state);
    }

    /// <inheritdoc />
    public override void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        if (input.Length != this.BlockSize || output.Length != this.BlockSize)
            throw new ArgumentException(
                string.Format(ResourceStrings.CryptographicException_InvalidBlockLength, this.BlockSize));

        int w = this.BlockWords;
        int rounds = this.Rounds;

        Span<uint> state = stackalloc uint[32].Slice(0, w);
        for (int i = 0; i < w; i++)
            state[i] = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(i * 4, 4));

        uint[] rk = this.RoundKeys;
        uint[] tw = this.TweakSchedule;

        // The last Encrypt injection used counter value (rounds / 4 - 1); Decrypt unwinds in reverse order starting here.
        int injection = (rounds / 4) - 1;

        // Reverse of final round.
        XorRoundKey(state, rk, rounds * w);
        ApplyInverseSBoxLayer(state, (rounds - 1) & 7);
        XorRoundKey(state, rk, (rounds - 1) * w);

        for (int r = rounds - 2; r >= 0; r--)
        {
            if (((r + 1) & 3) == 0)
            {
                state[w - 1] ^= (uint)injection;
                state[w - 2] ^= tw[(injection + 1) % 5];
                state[w - 3] ^= tw[injection % 5];
                injection--;
            }

            ApplyInverseLinearLayer(state);
            ApplyInverseSBoxLayer(state, r & 7);
            XorRoundKey(state, rk, r * w);
        }

        for (int i = 0; i < w; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(i * 4, 4), state[i]);

        CryptoHelpers.Clear(state);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (this.disposed) return;

        if (disposing)
        {
            CryptoHelpers.Clear(this.RoundKeys);
            CryptoHelpers.Clear(this.TweakSchedule);
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// XORs the <paramref name="source" /> sub-range (starting at <paramref name="offset" />, length
    /// <c><see cref="BlockWords" /></c>) into <paramref name="state" />.
    /// </summary>
    /// <param name="state">The cipher state, modified in place.</param>
    /// <param name="source">The round-key schedule.</param>
    /// <param name="offset">The starting offset within <paramref name="source" />.</param>
    private static void XorRoundKey(Span<uint> state, uint[] source, int offset)
    {
        for (int i = 0; i < state.Length; i++)
            state[i] ^= source[offset + i];
    }

    /// <summary>
    /// Applies the Serpent S-box identified by <paramref name="sBoxIndex" /> to every four-word group of <paramref name="state" />.
    /// </summary>
    /// <param name="state">The cipher state, modified in place.</param>
    /// <param name="sBoxIndex">The S-box index in the range <c>0..7</c>.</param>
    private static void ApplySBoxLayer(Span<uint> state, int sBoxIndex)
    {
        for (int g = 0; g < state.Length; g += 4)
        {
            uint x0 = state[g];
            uint x1 = state[g + 1];
            uint x2 = state[g + 2];
            uint x3 = state[g + 3];

            ApplySBox(sBoxIndex, ref x0, ref x1, ref x2, ref x3);

            state[g] = x0;
            state[g + 1] = x1;
            state[g + 2] = x2;
            state[g + 3] = x3;
        }
    }

    /// <summary>
    /// Applies the inverse Serpent S-box identified by <paramref name="sBoxIndex" /> to every four-word group of
    /// <paramref name="state" />.
    /// </summary>
    /// <param name="state">The cipher state, modified in place.</param>
    /// <param name="sBoxIndex">The S-box index in the range <c>0..7</c>.</param>
    private static void ApplyInverseSBoxLayer(Span<uint> state, int sBoxIndex)
    {
        for (int g = 0; g < state.Length; g += 4)
        {
            uint x0 = state[g];
            uint x1 = state[g + 1];
            uint x2 = state[g + 2];
            uint x3 = state[g + 3];

            ApplyInverseSBox(sBoxIndex, ref x0, ref x1, ref x2, ref x3);

            state[g] = x0;
            state[g + 1] = x1;
            state[g + 2] = x2;
            state[g + 3] = x3;
        }
    }

    /// <summary>
    /// Applies the Serpent linear transform to each four-word group of <paramref name="state" /> and rotates the word positions
    /// by one modulo <c><see cref="BlockWords" /></c> for cross-lane diffusion.
    /// </summary>
    /// <param name="state">The cipher state, modified in place.</param>
    /// <remarks>
    /// The rotation permutation <c>newState[j] = oldState[(j + 1) mod W]</c> is applied in-place via an end-of-state shift and
    /// is constant-time. It ensures that word positions circulate through every lane within <c>W</c> rounds so each lane
    /// receives diffusion contributions from every other lane.
    /// </remarks>
    private static void ApplyLinearLayer(Span<uint> state)
    {
        for (int g = 0; g < state.Length; g += 4)
        {
            uint x0 = state[g];
            uint x1 = state[g + 1];
            uint x2 = state[g + 2];
            uint x3 = state[g + 3];

            LinearTransform(ref x0, ref x1, ref x2, ref x3);

            state[g] = x0;
            state[g + 1] = x1;
            state[g + 2] = x2;
            state[g + 3] = x3;
        }

        // Cross-lane permutation: rotate word positions by one to the left.
        uint first = state[0];
        for (int i = 0; i < state.Length - 1; i++)
            state[i] = state[i + 1];
        state[state.Length - 1] = first;
    }

    /// <summary>
    /// Inverts <see cref="ApplyLinearLayer" /> — reverses the cross-lane rotation and applies the inverse linear transform to
    /// each four-word group.
    /// </summary>
    /// <param name="state">The cipher state, modified in place.</param>
    private static void ApplyInverseLinearLayer(Span<uint> state)
    {
        // Inverse cross-lane permutation: rotate word positions by one to the right.
        uint last = state[state.Length - 1];
        for (int i = state.Length - 1; i > 0; i--)
            state[i] = state[i - 1];
        state[0] = last;

        for (int g = 0; g < state.Length; g += 4)
        {
            uint x0 = state[g];
            uint x1 = state[g + 1];
            uint x2 = state[g + 2];
            uint x3 = state[g + 3];

            InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);

            state[g] = x0;
            state[g + 1] = x1;
            state[g + 2] = x2;
            state[g + 3] = x3;
        }
    }

    /// <summary>
    /// Populates <paramref name="schedule" /> with the five-entry tweak schedule derived from the supplied 16-byte tweak.
    /// </summary>
    /// <param name="tweak">The 16-byte tweak.</param>
    /// <param name="schedule">The destination buffer, sized for five 32-bit entries.</param>
    /// <remarks>
    /// The schedule stores <c>[T0, T1, T2, T3, T0 ^ T1 ^ T2 ^ T3]</c> — the four little-endian tweak words followed by their
    /// parity. Entries cycle modulo 5 at each tweak-injection point, mirroring the Threefish <c>[T0, T1, T0 ^ T1]</c> layout
    /// scaled to 32-bit state words.
    /// </remarks>
    private static void BuildTweakSchedule(ReadOnlySpan<byte> tweak, uint[] schedule)
    {
        uint t0 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(0, 4));
        uint t1 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(4, 4));
        uint t2 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(8, 4));
        uint t3 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(12, 4));

        schedule[0] = t0;
        schedule[1] = t1;
        schedule[2] = t2;
        schedule[3] = t3;
        schedule[4] = t0 ^ t1 ^ t2 ^ t3;
    }

    /// <summary>
    /// Expands <paramref name="key" /> into the <see cref="RoundKeys" /> schedule, producing <c>Rounds + 1</c> round keys of
    /// <see cref="BlockWords" /> 32-bit words each.
    /// </summary>
    /// <param name="key">The raw key bytes; length must equal <c>BlockWords * 4</c>.</param>
    /// <remarks>
    /// Uses a widened Serpent prekey recurrence with a window of <see cref="BlockWords" /> words seeded directly from the key.
    /// After the recurrence, applies the rotating Serpent S-box schedule to each four-word group of successive round keys,
    /// matching the canonical <c>K_0 → S3, K_1 → S2, …</c> order.
    /// </remarks>
    private void BuildRoundKeys(ReadOnlySpan<byte> key)
    {
        int w = this.BlockWords;

        // Seed the prekey recurrence with the key material. Serpent-1024 has the widest state (32 words = 128 bytes),
        // well within stackalloc territory.
        Span<uint> seed = stackalloc uint[32].Slice(0, w);
        for (int i = 0; i < w; i++)
            seed[i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 4, 4));

        int prekeyLength = w + this.RoundKeys.Length;
        uint[] prekeysArray = new uint[prekeyLength];
        try
        {
            Span<uint> prekeys = prekeysArray;
            ExpandPrekeys(seed, prekeys, w);

            // Apply the rotating S-box schedule to each 4-word sub-group of each round key.
            int groupsPerRoundKey = w / 4;

            for (int r = 0; r <= this.Rounds; r++)
            {
                int sboxIndex = KeyScheduleSBoxIndex(r);
                int roundStart = w + r * w;

                for (int g = 0; g < groupsPerRoundKey; g++)
                {
                    int src = roundStart + g * 4;
                    uint x0 = prekeys[src];
                    uint x1 = prekeys[src + 1];
                    uint x2 = prekeys[src + 2];
                    uint x3 = prekeys[src + 3];

                    ApplySBox(sboxIndex, ref x0, ref x1, ref x2, ref x3);

                    int dst = r * w + g * 4;
                    this.RoundKeys[dst] = x0;
                    this.RoundKeys[dst + 1] = x1;
                    this.RoundKeys[dst + 2] = x2;
                    this.RoundKeys[dst + 3] = x3;
                }
            }
        }
        finally
        {
            CryptoHelpers.Clear(prekeysArray);
            CryptoHelpers.Clear(seed);
        }
    }
}
