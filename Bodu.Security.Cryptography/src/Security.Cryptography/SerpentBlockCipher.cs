// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentBlockCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for the non-standard wide-block tweakable Serpent engines (
/// <see cref="Serpent256Cipher" />, <see cref="Serpent512Cipher" />, <see cref="Serpent1024Cipher" />).
/// </summary>
/// <remarks>
/// <para>
/// This type extends <see cref="SerpentBlockCipherBase" /> with a 128-bit tweak schedule expressed as five cycling
/// 32-bit entries <c>[T0, T1, T2, T3, T0 ^ T1 ^ T2 ^ T3]</c> (the 32-bit analogue of the Threefish
/// <c>[T0, T1, T0 ^ T1]</c> layout) and with a round-key schedule sized to match the variant's block width. Derived
/// classes specify the state width (in 32-bit words) and round count; this class builds the expanded round keys and
/// folds the tweak schedule into them.
/// </para>
/// <para>
/// The round function keeps the Serpent-style structure of key XOR, S-box layer, and linear diffusion, then extends it
/// across wider states by applying each Serpent operation to four-word groups and adding a word-rotation permutation
/// between groups. Tweak material is injected into the tail of the state every four rounds.
/// </para>
/// <note type="important"> The wide-block tweakable Serpent family is a <b>non-standard, experimental construction</b>
/// developed for this library. It is not interoperable with canonical Serpent implementations at any block size, and
/// its cryptographic properties have not been externally analyzed. Use the canonical <see cref="Serpent128Cipher" />
/// when Serpent compatibility is required. </note>
/// <para>
/// This implementation computes each S-box as a Boolean circuit (Osvik's) and the linear transform with rotations,
/// shifts and XOR, so it reads no tables and takes no branches that depend on the key or the data: its running time
/// does not depend on either.
/// </para>
/// <para>
/// The rounds run in <see cref="SerpentCore" />, eight at a time, one per S-box. Serpent-256's eight words stay in
/// locals throughout, so the word rotation is a renaming; the wider states pass through each round a four-word group at
/// a time. The tweak material is folded into the round keys when the key is set: each injection is followed at once by
/// the next round's key, so adding it to that key gives the same rounds.
/// </para>
/// </remarks>
public abstract partial class SerpentBlockCipher
    : SerpentBlockCipherBase
{
    /// <summary>The tweak size in bits.</summary>
    /// <remarks>
    /// The tweak is always four 32-bit words, independent of the selected wide-block state size.
    /// </remarks>
    private protected const int TweakSizeBits = 128;

    /// <summary>The expanded round-key schedule, laid out as <c>(Rounds + 1) * BlockWords</c> contiguous 32-bit words, with the tweak folded in.</summary>
    /// <remarks>
    /// Round key <c>r</c> starts at offset <c>r * BlockWords</c>. Encryption uses one key before each round and a final
    /// post-S-box key after the last round, mirroring canonical Serpent's <c>R + 1</c> key schedule shape. The tweak
    /// material injected after every fourth round is folded into the key of the round that follows it.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Scoped private protected so only the in-assembly wide-block Serpent variant classes can access the round-key schedule directly, avoiding property dispatch on the hot encrypt/decrypt path.")]
    private protected readonly uint[] _roundKeys;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerpentBlockCipher" /> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">
    /// The encryption key. Its byte length must equal <see cref="IBlockCipher.BlockSize" /> / 8.
    /// </param>
    /// <param name="tweak">The 16-byte (128-bit) tweak value.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> or <paramref name="tweak" /> does not have the expected length.
    /// </exception>
    private protected SerpentBlockCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
    {
        if (key.Length != BlockSize / 8)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_KeySize, key.Length * 8, BlockSize),
                nameof(key));
        }

        if (tweak.Length != TweakSizeBits / 8)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_TweakSize, tweak.Length * 8, TweakSizeBits),
                nameof(tweak));
        }

        // Build one full-width round key for every round plus the final post-S-box whitening key, then fold in the tweak
        // material injected after every fourth round.
        _roundKeys = new uint[(Rounds + 1) * BlockWords];
        BuildRoundKeys(key);
        FoldTweak(tweak);
    }

    /// <summary>
    /// Gets the number of 32-bit state words in a single block. Equal to <see cref="IBlockCipher.BlockSize" /> divided
    /// by 4.
    /// </summary>
    /// <remarks>
    /// This value is supplied by the concrete wide-block variant: 8, 16 or 32. The S-box and linear transform layers
    /// operate on four-word Serpent sub-states, and the word rotation between rounds is computed with a mask.
    /// </remarks>
    private protected abstract int BlockWords { get; }

    /// <summary>
    /// Gets the total number of cipher rounds executed by this variant.
    /// </summary>
    /// <remarks>
    /// The round count is supplied by the concrete wide-block variant. It must be a multiple of eight, because the
    /// rounds run eight at a time, one per S-box; tweak injection occurs after every fourth non-final round.
    /// </remarks>
    private protected abstract int Rounds { get; }

    /// <inheritdoc />
    public override void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        if (input.Length != BlockSize / 8 || output.Length != BlockSize / 8)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_BlockLength, BlockSize / 8));
        }

        SerpentCore.EncryptWideBlock(_roundKeys, BlockWords, Rounds, input, output);
    }

    /// <inheritdoc />
    public override void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        if (input.Length != BlockSize / 8 || output.Length != BlockSize / 8)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_BlockLength, BlockSize / 8));
        }

        SerpentCore.DecryptWideBlock(_roundKeys, BlockWords, Rounds, input, output);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        // The round keys, with the tweak folded in, are derived from secret inputs and are zeroed in both disposal
        // paths so they are not retained if the finalizer runs before an explicit Dispose call.
        CryptographyHelper.Clear(_roundKeys);

        base.Dispose(disposing);
    }

    /// <summary>
    /// Populates <paramref name="schedule" /> with the five-entry tweak schedule derived from the supplied 16-byte
    /// tweak.
    /// </summary>
    /// <param name="tweak">The 16-byte tweak.</param>
    /// <param name="schedule">The destination buffer, sized for five 32-bit entries.</param>
    /// <remarks>
    /// The schedule stores <c>[T0, T1, T2, T3, T0 ^ T1 ^ T2 ^ T3]</c> — the four little-endian tweak words followed by
    /// their parity. Entries cycle modulo 5 at each tweak-injection point, mirroring the Threefish
    /// <c>[T0, T1, T0 ^ T1]</c> layout scaled to 32-bit state words.
    /// </remarks>
    private static void BuildTweakSchedule(ReadOnlySpan<byte> tweak, Span<uint> schedule)
    {
        // Interpret the 128-bit tweak as four little-endian 32-bit words.
        uint t0 = BinaryPrimitives.ReadUInt32LittleEndian(tweak[..4]);
        uint t1 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(4, 4));
        uint t2 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(8, 4));
        uint t3 = BinaryPrimitives.ReadUInt32LittleEndian(tweak.Slice(12, 4));

        schedule[0] = t0;
        schedule[1] = t1;
        schedule[2] = t2;
        schedule[3] = t3;

        // The fifth entry is tweak parity. It allows the cyclic injection schedule to include all tweak words and their XOR.
        schedule[4] = t0 ^ t1 ^ t2 ^ t3;
    }

    /// <summary>
    /// Expands <paramref name="key" /> into the <see cref="_roundKeys" /> schedule, producing <c>Rounds + 1</c> round
    /// keys of <see cref="BlockWords" /> 32-bit words each.
    /// </summary>
    /// <param name="key">The raw key bytes; length must equal <c>BlockWords * 4</c>.</param>
    /// <remarks>
    /// Uses a widened Serpent prekey recurrence with a window of <see cref="BlockWords" /> words seeded directly from
    /// the key. After the recurrence, applies the rotating Serpent S-box schedule to each four-word group of successive
    /// round keys, matching the canonical <c>K_0 → S3, K_1 → S2, …</c> order.
    /// </remarks>
    private void BuildRoundKeys(ReadOnlySpan<byte> key)
    {
        int w = BlockWords;

        // Seed the widened prekey recurrence with the raw key words. Serpent-1024 has the widest state
        // (32 words = 128 bytes), which is still small enough for stack allocation.
        Span<uint> seed = stackalloc uint[w];
        for (int i = 0; i < w; i++)
            seed[i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 4, 4));

        // The recurrence emits enough words to cover every full-width round key after the initial seed window.
        int prekeyLength = w + _roundKeys.Length;
        uint[] prekeysArray = new uint[prekeyLength];
        try
        {
            Span<uint> prekeys = prekeysArray;
            ExpandPrekeys(seed, prekeys, w);

            // Each full-width round key is partitioned into canonical four-word Serpent groups. The key-schedule S-box
            // for round r is applied to every group in that round key.
            int groupsPerRoundKey = w / 4;

            for (int r = 0; r <= Rounds; r++)
            {
                int sboxIndex = KeyScheduleSBoxIndex(r);
                int roundStart = w + (r * w);

                for (int g = 0; g < groupsPerRoundKey; g++)
                {
                    int src = roundStart + (g * 4);
                    uint x0 = prekeys[src];
                    uint x1 = prekeys[src + 1];
                    uint x2 = prekeys[src + 2];
                    uint x3 = prekeys[src + 3];

                    ApplySBox(sboxIndex, ref x0, ref x1, ref x2, ref x3);

                    int dst = (r * w) + (g * 4);
                    _roundKeys[dst] = x0;
                    _roundKeys[dst + 1] = x1;
                    _roundKeys[dst + 2] = x2;
                    _roundKeys[dst + 3] = x3;
                }
            }
        }
        finally
        {
            CryptographyHelper.Clear(prekeysArray);
            CryptographyHelper.Clear(seed);
        }
    }

    /// <summary>
    /// Folds the tweak into the round keys: adds the material injected after every fourth round but the last to the key
    /// of the round that follows it.
    /// </summary>
    /// <param name="tweak">The 16-byte tweak.</param>
    /// <remarks>
    /// Injection <c>j</c>, after round <c>4j − 1</c>, adds <c>tw[j mod 5]</c>, <c>tw[(j + 1) mod 5]</c> and <c>j</c> to
    /// the state's last three words, where <c>tw</c> is the schedule <see cref="BuildTweakSchedule" /> builds. Round
    /// <c>4j</c> adds its key at once, so both directions compute the same rounds with the injection moved into round
    /// key <c>4j</c>.
    /// </remarks>
    private void FoldTweak(ReadOnlySpan<byte> tweak)
    {
        Span<uint> schedule = stackalloc uint[5];
        BuildTweakSchedule(tweak, schedule);

        int w = BlockWords;
        for (int injection = 1; injection < Rounds / 4; injection++)
        {
            // One past the last word of round key 4j.
            int end = (4 * injection * w) + w;
            _roundKeys[end - 3] ^= schedule[injection % 5];
            _roundKeys[end - 2] ^= schedule[(injection + 1) % 5];
            _roundKeys[end - 1] ^= (uint)injection;
        }

        CryptographyHelper.Clear(schedule);
    }
}
