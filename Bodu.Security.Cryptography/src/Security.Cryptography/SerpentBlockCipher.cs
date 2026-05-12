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
/// (<see cref="Serpent256Cipher"/>, <see cref="Serpent512Cipher"/>, <see cref="Serpent1024Cipher"/>).
/// </summary>
/// <remarks>
/// <para>
/// This type extends <see cref="SerpentBlockCipherBase"/> with a 128-bit tweak schedule expressed as five cycling 32-bit
/// entries <c>[T0, T1, T2, T3, T0 ^ T1 ^ T2 ^ T3]</c> (the 32-bit analogue of the Threefish <c>[T0, T1, T0 ^ T1]</c> layout)
/// and with a round-key schedule sized to match the variant's block width. Derived classes specify the state width (in 32-bit
/// words) and round count; this class builds the expanded round keys and tweak schedule used at every round-key injection
/// point.
/// </para>
/// <note type="important">
/// The wide-block tweakable Serpent family is a **non-standard, experimental construction** developed for this library. It is
/// not interoperable with canonical Serpent implementations at any block size, and its cryptographic properties have not been
/// externally analysed. Use the canonical <see cref="Serpent128Cipher"/> when Serpent compatibility is required.
/// </note>
/// </remarks>
public abstract partial class SerpentBlockCipher
    : SerpentBlockCipherBase
{
    /// <summary>
    /// The tweak size in bits.
    /// </summary>
    private protected const int TweakSizeBits = 128;

    /// <summary>
    /// The expanded tweak schedule — five cycling 32-bit entries
    /// <c>[T0, T1, T2, T3, T0 ^ T1 ^ T2 ^ T3]</c> — XOR-injected at the tail of the state every four rounds.
    /// </summary>
    private protected readonly uint[] _tweakSchedule;

    /// <summary>
    /// The expanded round-key schedule, laid out as <c>(Rounds + 1) * BlockWords</c> contiguous 32-bit words.
    /// </summary>
    private protected readonly uint[] _roundKeys;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerpentBlockCipher"/> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">
    /// The encryption key. Its byte length must equal <see cref="IBlockCipher.BlockSize"/> / 8.
    /// </param>
    /// <param name="tweak">The 16-byte (128-bit) tweak value.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> or <paramref name="tweak"/> does not have the expected length.
    /// </exception>
    private protected SerpentBlockCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
    {
        if (key.Length != this.BlockSize / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidKeySize, key.Length * 8, this.BlockSize),
                nameof(key));

        if (tweak.Length != TweakSizeBits / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidTweakSize, tweak.Length * 8, TweakSizeBits),
                nameof(tweak));

        this._tweakSchedule = new uint[5];
        BuildTweakSchedule(tweak, this._tweakSchedule);

        this._roundKeys = new uint[(this.Rounds + 1) * this.BlockWords];
        this.BuildRoundKeys(key);
    }

    /// <summary>
    /// Gets the number of 32-bit state words in a single block. Equal to <see cref="IBlockCipher.BlockSize"/> divided by 4.
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
        if (input.Length != this.BlockSize / 8 || output.Length != this.BlockSize / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidBlockLength, this.BlockSize / 8));

        var w = this.BlockWords;
        var rounds = this.Rounds;

        Span<uint> state = (stackalloc uint[32])[..w];
        for (var i = 0; i < w; i++)
            state[i] = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(i * 4, 4));

        var rk = this._roundKeys;
        var tw = this._tweakSchedule;
        var injection = 0;

        for (var r = 0; r < rounds - 1; r++)
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

        for (var i = 0; i < w; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(i * 4, 4), state[i]);

        CryptoHelpers.Clear(state);
    }

    /// <inheritdoc />
    public override void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        if (input.Length != this.BlockSize / 8 || output.Length != this.BlockSize / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidBlockLength, this.BlockSize / 8));

        var w = this.BlockWords;
        var rounds = this.Rounds;

        Span<uint> state = (stackalloc uint[32])[..w];
        for (var i = 0; i < w; i++)
            state[i] = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(i * 4, 4));

        var rk = this._roundKeys;
        var tw = this._tweakSchedule;

        // The last Encrypt injection used counter value (rounds / 4 - 1); Decrypt unwinds in reverse order starting here.
        var injection = (rounds / 4) - 1;

        // Reverse of final round.
        XorRoundKey(state, rk, rounds * w);
        ApplyInverseSBoxLayer(state, (rounds - 1) & 7);
        XorRoundKey(state, rk, (rounds - 1) * w);

        for (var r = rounds - 2; r >= 0; r--)
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

        for (var i = 0; i < w; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(i * 4, 4), state[i]);

        CryptoHelpers.Clear(state);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (this._disposed) return;

        if (disposing)
        {
            CryptoHelpers.Clear(this._roundKeys);
            CryptoHelpers.Clear(this._tweakSchedule);
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// XORs the <paramref name="source"/> sub-range (starting at <paramref name="offset"/>, length
    /// <c><see cref="BlockWords"/></c>) into <paramref name="state"/>.
    /// </summary>
    /// <param name="state">The cipher state, modified in place.</param>
    /// <param name="source">The round-key schedule.</param>
    /// <param name="offset">The starting offset within <paramref name="source"/>.</param>
    private static void XorRoundKey(Span<uint> state, uint[] source, int offset)
    {
        for (var i = 0; i < state.Length; i++)
            state[i] ^= source[offset + i];
    }

    /// <summary>
    /// Applies the Serpent S-box identified by <paramref name="sBoxIndex"/> to every four-word group of <paramref name="state"/>.
    /// </summary>
    /// <param name="state">The cipher state, modified in place.</param>
    /// <param name="sBoxIndex">The S-box index in the range <c>0..7</c>.</param>
    private static void ApplySBoxLayer(Span<uint> state, int sBoxIndex)
    {
        for (var g = 0; g < state.Length; g += 4)
        {
            var x0 = state[g];
            var x1 = state[g + 1];
            var x2 = state[g + 2];
            var x3 = state[g + 3];

            ApplySBox(sBoxIndex, ref x0, ref x1, ref x2, ref x3);

            state[g] = x0;
            state[g + 1] = x1;
            state[g + 2] = x2;
            state[g + 3] = x3;
        }
    }

    /// <summary>
    /// Applies the inverse Serpent S-box identified by <paramref name="sBoxIndex"/> to every four-word group of
    /// <paramref name="state"/>.
    /// </summary>
    /// <param name="state">The cipher state, modified in place.</param>
    /// <param name="sBoxIndex">The S-box index in the range <c>0..7</c>.</param>
    private static void ApplyInverseSBoxLayer(Span<uint> state, int sBoxIndex)
    {
        for (var g = 0; g < state.Length; g += 4)
        {
            var x0 = state[g];
            var x1 = state[g + 1];
            var x2 = state[g + 2];
            var x3 = state[g + 3];

            ApplyInverseSBox(sBoxIndex, ref x0, ref x1, ref x2, ref x3);

            state[g] = x0;
            state[g + 1] = x1;
            state[g + 2] = x2;
            state[g + 3] = x3;
        }
    }

    /// <summary>
    /// Applies the Serpent linear transform to each four-word group of <paramref name="state"/> and rotates the word positions
    /// by one modulo <c><see cref="BlockWords"/></c> for cross-lane diffusion.
    /// </summary>
    /// <param name="state">The cipher state, modified in place.</param>
    /// <remarks>
    /// The rotation permutation <c>newState[j] = oldState[(j + 1) mod W]</c> is applied in-place via an end-of-state shift and
    /// is constant-time. It ensures that word positions circulate through every lane within <c>W</c> rounds so each lane
    /// receives diffusion contributions from every other lane.
    /// </remarks>
    private static void ApplyLinearLayer(Span<uint> state)
    {
        for (var g = 0; g < state.Length; g += 4)
        {
            var x0 = state[g];
            var x1 = state[g + 1];
            var x2 = state[g + 2];
            var x3 = state[g + 3];

            LinearTransform(ref x0, ref x1, ref x2, ref x3);

            state[g] = x0;
            state[g + 1] = x1;
            state[g + 2] = x2;
            state[g + 3] = x3;
        }

        // Cross-lane permutation: rotate word positions by one to the left.
        var first = state[0];
        for (var i = 0; i < state.Length - 1; i++)
            state[i] = state[i + 1];
        state[^1] = first;
    }

    /// <summary>
    /// Inverts <see cref="ApplyLinearLayer"/> — reverses the cross-lane rotation and applies the inverse linear transform to
    /// each four-word group.
    /// </summary>
    /// <param name="state">The cipher state, modified in place.</param>
    private static void ApplyInverseLinearLayer(Span<uint> state)
    {
        // Inverse cross-lane permutation: rotate word positions by one to the right.
        var last = state[^1];
        for (var i = state.Length - 1; i > 0; i--)
            state[i] = state[i - 1];
        state[0] = last;

        for (var g = 0; g < state.Length; g += 4)
        {
            var x0 = state[g];
            var x1 = state[g + 1];
            var x2 = state[g + 2];
            var x3 = state[g + 3];

            InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);

            state[g] = x0;
            state[g + 1] = x1;
            state[g + 2] = x2;
            state[g + 3] = x3;
        }
    }

    /// <summary>
    /// Populates <paramref name="schedule"/> with the five-entry tweak schedule derived from the supplied 16-byte tweak.
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
        var t0 = BinaryPrimitives.ReadUInt32LittleEndian(tweak[..4]);
        var t1 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(4, 4));
        var t2 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(8, 4));
        var t3 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(12, 4));

        schedule[0] = t0;
        schedule[1] = t1;
        schedule[2] = t2;
        schedule[3] = t3;
        schedule[4] = t0 ^ t1 ^ t2 ^ t3;
    }

    /// <summary>
    /// Expands <paramref name="key"/> into the <see cref="_roundKeys"/> schedule, producing <c>Rounds + 1</c> round keys of
    /// <see cref="BlockWords"/> 32-bit words each.
    /// </summary>
    /// <param name="key">The raw key bytes; length must equal <c>BlockWords * 4</c>.</param>
    /// <remarks>
    /// Uses a widened Serpent prekey recurrence with a window of <see cref="BlockWords"/> words seeded directly from the key.
    /// After the recurrence, applies the rotating Serpent S-box schedule to each four-word group of successive round keys,
    /// matching the canonical <c>K_0 → S3, K_1 → S2, …</c> order.
    /// </remarks>
    private void BuildRoundKeys(ReadOnlySpan<byte> key)
    {
        var w = this.BlockWords;

        // Seed the prekey recurrence with the key material. Serpent-1024 has the widest state (32 words = 128 bytes),
        // well within stackalloc territory.
        Span<uint> seed = (stackalloc uint[32])[..w];
        for (var i = 0; i < w; i++)
            seed[i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 4, 4));

        var prekeyLength = w + this._roundKeys.Length;
        var prekeysArray = new uint[prekeyLength];
        try
        {
            Span<uint> prekeys = prekeysArray;
            ExpandPrekeys(seed, prekeys, w);

            // Apply the rotating S-box schedule to each 4-word sub-group of each round key.
            var groupsPerRoundKey = w / 4;

            for (var r = 0; r <= this.Rounds; r++)
            {
                var sboxIndex = KeyScheduleSBoxIndex(r);
                var roundStart = w + r * w;

                for (var g = 0; g < groupsPerRoundKey; g++)
                {
                    var src = roundStart + g * 4;
                    var x0 = prekeys[src];
                    var x1 = prekeys[src + 1];
                    var x2 = prekeys[src + 2];
                    var x3 = prekeys[src + 3];

                    ApplySBox(sboxIndex, ref x0, ref x1, ref x2, ref x3);

                    var dst = r * w + g * 4;
                    this._roundKeys[dst] = x0;
                    this._roundKeys[dst + 1] = x1;
                    this._roundKeys[dst + 2] = x2;
                    this._roundKeys[dst + 3] = x3;
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
