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
/// <para>
/// The round function keeps the Serpent-style structure of key XOR, S-box layer, and linear diffusion, then extends it across
/// wider states by applying each Serpent operation to four-word groups and adding a word-rotation permutation between groups.
/// Tweak material is injected into the tail of the state every four rounds.
/// </para>
/// <note type="important">
/// The wide-block tweakable Serpent family is a <b>non-standard, experimental construction</b> developed for this library. It is
/// not interoperable with canonical Serpent implementations at any block size, and its cryptographic properties have not been
/// externally analyzed. Use the canonical <see cref="Serpent128Cipher"/> when Serpent compatibility is required.
/// </note>
/// </remarks>
public abstract partial class SerpentBlockCipher
    : SerpentBlockCipherBase
{
    /// <summary>
    /// The tweak size in bits.
    /// </summary>
    /// <remarks>
    /// The tweak is always four 32-bit words, independent of the selected wide-block state size.
    /// </remarks>
    private protected const int TweakSizeBits = 128;

    /// <summary>
    /// The expanded tweak schedule — five cycling 32-bit entries
    /// <c>[T0, T1, T2, T3, T0 ^ T1 ^ T2 ^ T3]</c> — XOR-injected at the tail of the state every four rounds.
    /// </summary>
    /// <remarks>
    /// At injection point <c>j</c>, this class injects <c>tw[j mod 5]</c>, <c>tw[(j + 1) mod 5]</c>, and the injection counter
    /// into the final three state words. The parity entry prevents the cycle from being a simple repetition of the four raw
    /// tweak words.
    /// </remarks>
    private protected readonly uint[] _tweakSchedule;

    /// <summary>
    /// The expanded round-key schedule, laid out as <c>(Rounds + 1) * BlockWords</c> contiguous 32-bit words.
    /// </summary>
    /// <remarks>
    /// Round key <c>r</c> starts at offset <c>r * BlockWords</c>. Encryption uses one key before each round and a final
    /// post-S-box key after the last round, mirroring canonical Serpent's <c>R + 1</c> key schedule shape.
    /// </remarks>
    private protected readonly uint[] _roundKeys;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerpentBlockCipher"/> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">
    /// The encryption key. Its byte length must equal <see cref="IBlockCipher.BlockSize"/> / 8.
    /// </param>
    /// <param name="tweak">
    /// The 16-byte (128-bit) tweak value.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> or <paramref name="tweak"/> does not have the expected length.
    /// </exception>
    private protected SerpentBlockCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
    {
        if (key.Length != this.BlockSize / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_KeySize, key.Length * 8, this.BlockSize),
                nameof(key));

        if (tweak.Length != TweakSizeBits / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_TweakSize, tweak.Length * 8, TweakSizeBits),
                nameof(tweak));

        // Build the five-word tweak cycle used by the per-four-round injection points.
        this._tweakSchedule = new uint[5];
        BuildTweakSchedule(tweak, this._tweakSchedule);

        // Build one full-width round key for every round plus the final post-S-box whitening key.
        this._roundKeys = new uint[(this.Rounds + 1) * this.BlockWords];
        this.BuildRoundKeys(key);
    }

    /// <summary>
    /// Gets the number of 32-bit state words in a single block. Equal to <see cref="IBlockCipher.BlockSize"/> divided by 4.
    /// </summary>
    /// <remarks>
    /// This value is supplied by the concrete wide-block variant. It must be a multiple of four because S-box and linear
    /// transform layers operate on four-word Serpent sub-states.
    /// </remarks>
    private protected abstract int BlockWords { get; }

    /// <summary>
    /// Gets the total number of cipher rounds executed by this variant.
    /// </summary>
    /// <remarks>
    /// The round count is supplied by the concrete wide-block variant. Tweak injection occurs after every fourth non-final
    /// round, and decryption assumes the same round grouping when reversing the injection counter.
    /// </remarks>
    private protected abstract int Rounds { get; }

    /// <inheritdoc />
    public override void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        if (input.Length != this.BlockSize / 8 || output.Length != this.BlockSize / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_BlockLength, this.BlockSize / 8));

        var w = this.BlockWords;
        var rounds = this.Rounds;

        // Load the wide block as little-endian 32-bit words. Canonical Serpent is word-oriented, and the wide-block variants
        // preserve that representation across the expanded state width.
        Span<uint> state = (stackalloc uint[32])[..w];
        for (var i = 0; i < w; i++)
            state[i] = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(i * 4, 4));

        var rk = this._roundKeys;
        var tw = this._tweakSchedule;
        var injection = 0;

        // All rounds except the final round use the Serpent-style sequence:
        //   state ^= K_r; state = S_r(state); state = L(state);
        // followed by this construction's additional tweak injection every four rounds.
        for (var r = 0; r < rounds - 1; r++)
        {
            XorRoundKey(state, rk, r * w);
            ApplySBoxLayer(state, r & 7);
            ApplyLinearLayer(state);

            // Inject tweak material after every fourth completed round. The counter increments before use so the first
            // injection uses schedule entries tw[1] and tw[2], matching the current decryption counter convention.
            if (((r + 1) & 3) == 0)
            {
                injection++;
                state[w - 3] ^= tw[injection % 5];
                state[w - 2] ^= tw[(injection + 1) % 5];
                state[w - 1] ^= (uint)injection;
            }
        }

        // Final round follows canonical Serpent shape: key XOR, S-box, then final key XOR. There is no linear transform
        // after the last S-box layer.
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
                string.Format(CryptoResourceStrings.Crypt_Invalid_BlockLength, this.BlockSize / 8));

        var w = this.BlockWords;
        var rounds = this.Rounds;

        // Load the ciphertext using the same little-endian word layout used by encryption.
        Span<uint> state = (stackalloc uint[32])[..w];
        for (var i = 0; i < w; i++)
            state[i] = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(i * 4, 4));

        var rk = this._roundKeys;
        var tw = this._tweakSchedule;

        // The last encryption tweak injection occurs after the final completed four-round group before the final round.
        // Decryption starts from that counter value and decrements as each injection is removed.
        var injection = (rounds / 4) - 1;

        // Reverse the final Serpent-shaped round: undo final key XOR, inverse S-box, then undo the previous round key.
        XorRoundKey(state, rk, rounds * w);
        ApplyInverseSBoxLayer(state, (rounds - 1) & 7);
        XorRoundKey(state, rk, (rounds - 1) * w);

        // Walk the remaining rounds backwards. Each step removes any tweak injection that followed the corresponding
        // encryption round, then applies inverse linear transform, inverse S-box, and round-key XOR.
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

        // Round keys and tweak material are derived from secret inputs and are zeroed in both disposal
        // paths so they are not retained if the finalizer runs before an explicit Dispose call.
        CryptoHelpers.Clear(this._roundKeys);
        CryptoHelpers.Clear(this._tweakSchedule);

        base.Dispose(disposing);
    }

    /// <summary>
    /// XORs a full-width round key into the current cipher state.
    /// </summary>
    /// <param name="state">
    /// The cipher state, modified in place.
    /// </param>
    /// <param name="source">
    /// The expanded round-key schedule.
    /// </param>
    /// <param name="offset">
    /// The starting word offset of the round key within <paramref name="source"/>.
    /// </param>
    /// <remarks>
    /// Round keys are stored contiguously as <c>K_r[0..BlockWords-1]</c>. XORing is its own inverse, so the same helper is
    /// used by encryption and decryption.
    /// </remarks>
    private static void XorRoundKey(Span<uint> state, uint[] source, int offset)
    {
        for (var i = 0; i < state.Length; i++)
            state[i] ^= source[offset + i];
    }

    /// <summary>
    /// Applies the Serpent S-box identified by <paramref name="sBoxIndex"/> to every four-word group of <paramref name="state"/>.
    /// </summary>
    /// <param name="state">
    /// The cipher state, modified in place.
    /// </param>
    /// <param name="sBoxIndex">
    /// The S-box index in the range <c>0..7</c>.
    /// </param>
    /// <remarks>
    /// Canonical Serpent applies one bitsliced S-box to a four-word state. The wide-block variants repeat that operation across
    /// each independent four-word lane group before the linear layer performs local diffusion and cross-lane rotation.
    /// </remarks>
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
    /// <param name="state">
    /// The cipher state, modified in place.
    /// </param>
    /// <param name="sBoxIndex">
    /// The inverse S-box index in the range <c>0..7</c>.
    /// </param>
    /// <remarks>
    /// This reverses <see cref="ApplySBoxLayer"/> during decryption by applying the matching inverse S-box to each four-word
    /// bitsliced lane group.
    /// </remarks>
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
    /// <param name="state">
    /// The cipher state, modified in place.
    /// </param>
    /// <remarks>
    /// <para>
    /// The first phase applies the canonical Serpent linear transform independently to each four-word group. The second phase
    /// is specific to this wide-block construction: it rotates every 32-bit word one position to the left so words migrate
    /// across four-word groups over successive rounds.
    /// </para>
    /// <para>
    /// The rotation permutation <c>newState[j] = oldState[(j + 1) mod W]</c> is applied in-place via an end-of-state shift.
    /// It ensures that word positions circulate through every lane within <c>W</c> rounds so each lane receives diffusion
    /// contributions from every other lane.
    /// </para>
    /// </remarks>
    private static void ApplyLinearLayer(Span<uint> state)
    {
        // Apply canonical Serpent local diffusion to each four-word sub-state.
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
    /// Inverts <see cref="ApplyLinearLayer"/> by reversing the cross-lane rotation and applying the inverse linear transform to
    /// each four-word group.
    /// </summary>
    /// <param name="state">
    /// The cipher state, modified in place.
    /// </param>
    /// <remarks>
    /// Because encryption performs local linear transforms first and cross-lane rotation second, decryption must undo the
    /// rotation first and then apply the inverse Serpent linear transform to each four-word group.
    /// </remarks>
    private static void ApplyInverseLinearLayer(Span<uint> state)
    {
        // Inverse cross-lane permutation: rotate word positions by one to the right.
        var last = state[^1];
        for (var i = state.Length - 1; i > 0; i--)
            state[i] = state[i - 1];
        state[0] = last;

        // Undo canonical Serpent local diffusion for each four-word sub-state.
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
    /// <param name="tweak">
    /// The 16-byte tweak.
    /// </param>
    /// <param name="schedule">
    /// The destination buffer, sized for five 32-bit entries.
    /// </param>
    /// <remarks>
    /// The schedule stores <c>[T0, T1, T2, T3, T0 ^ T1 ^ T2 ^ T3]</c> — the four little-endian tweak words followed by their
    /// parity. Entries cycle modulo 5 at each tweak-injection point, mirroring the Threefish <c>[T0, T1, T0 ^ T1]</c> layout
    /// scaled to 32-bit state words.
    /// </remarks>
    private static void BuildTweakSchedule(ReadOnlySpan<byte> tweak, uint[] schedule)
    {
        // Interpret the 128-bit tweak as four little-endian 32-bit words.
        var t0 = BinaryPrimitives.ReadUInt32LittleEndian(tweak[..4]);
        var t1 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(4, 4));
        var t2 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(8, 4));
        var t3 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(12, 4));

        schedule[0] = t0;
        schedule[1] = t1;
        schedule[2] = t2;
        schedule[3] = t3;

        // The fifth entry is tweak parity. It allows the cyclic injection schedule to include all tweak words and their XOR.
        schedule[4] = t0 ^ t1 ^ t2 ^ t3;
    }

    /// <summary>
    /// Expands <paramref name="key"/> into the <see cref="_roundKeys"/> schedule, producing <c>Rounds + 1</c> round keys of
    /// <see cref="BlockWords"/> 32-bit words each.
    /// </summary>
    /// <param name="key">
    /// The raw key bytes; length must equal <c>BlockWords * 4</c>.
    /// </param>
    /// <remarks>
    /// Uses a widened Serpent prekey recurrence with a window of <see cref="BlockWords"/> words seeded directly from the key.
    /// After the recurrence, applies the rotating Serpent S-box schedule to each four-word group of successive round keys,
    /// matching the canonical <c>K_0 → S3, K_1 → S2, …</c> order.
    /// </remarks>
    private void BuildRoundKeys(ReadOnlySpan<byte> key)
    {
        var w = this.BlockWords;

        // Seed the widened prekey recurrence with the raw key words. Serpent-1024 has the widest state
        // (32 words = 128 bytes), which is still small enough for stack allocation.
        Span<uint> seed = (stackalloc uint[32])[..w];
        for (var i = 0; i < w; i++)
            seed[i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 4, 4));

        // The recurrence emits enough words to cover every full-width round key after the initial seed window.
        var prekeyLength = w + this._roundKeys.Length;
        var prekeysArray = new uint[prekeyLength];
        try
        {
            Span<uint> prekeys = prekeysArray;
            ExpandPrekeys(seed, prekeys, w);

            // Each full-width round key is partitioned into canonical four-word Serpent groups. The key-schedule S-box
            // for round r is applied to every group in that round key.
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
