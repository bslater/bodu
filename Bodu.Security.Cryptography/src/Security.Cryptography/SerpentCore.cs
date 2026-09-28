// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the Serpent round function for <see cref="Serpent128Cipher" /> and the wide-block Serpent variants: the
/// S-boxes as Boolean circuits, the linear transform, and the 32 rounds of Serpent-128.
/// </summary>
/// <remarks>
/// <para>
/// Serpent works on four 32-bit words in bitsliced form: bit <c>i</c> of the four words is the input of the <c>i</c>-th
/// 4-bit S-box. The S-boxes here are Osvik's circuits of 14 to 19 Boolean operations each, which take the same time
/// whatever the data; they replace a 32-step loop that gathered each 4-bit input and read it through a 16-entry table.
/// </para>
/// <para>
/// The circuits and the rounds work on plain words, so every operation compiles to one instruction and the four words
/// stay in registers through all 32 rounds. The vector kernels repeat them over vectors that hold the same word of
/// several blocks.
/// </para>
/// <para>
/// Every entry point forbids inlining. Dynamic PGO otherwise inlines a hot one into its caller, runs out of inlining
/// budget inside it, and leaves the circuits as calls that pass the words through memory, which more than halved the
/// cipher's speed; compiled on its own, each keeps its own budget whatever the profile.
/// </para>
/// </remarks>
[SkipLocalsInit]
internal static partial class SerpentCore
{
    /// <summary>The number of Serpent-128 rounds.</summary>
    internal const int RoundCount = 32;

    /// <summary>The number of round-key words: four for each round, and four for the key added after the last.</summary>
    internal const int RoundKeyWords = (RoundCount + 1) * 4;

    /// <summary>The number of bytes in a Serpent-128 block.</summary>
    internal const int BlockBytes = 16;

    /// <summary>
    /// Encrypts one 16-byte block under an expanded Serpent-128 key.
    /// </summary>
    /// <param name="roundKeys">The 132 round-key words.</param>
    /// <param name="input">The plaintext block.</param>
    /// <param name="output">
    /// Receives the ciphertext block; it may be the same memory as <paramref name="input" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="roundKeys" /> holds fewer than 132 words, or <paramref name="input" /> or
    /// <paramref name="output" /> is shorter than 16 bytes.
    /// </exception>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void EncryptBlock(ReadOnlySpan<uint> roundKeys, ReadOnlySpan<byte> input, Span<byte> output)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roundKeys.Length, RoundKeyWords, nameof(roundKeys));
        ArgumentOutOfRangeException.ThrowIfLessThan(input.Length, BlockBytes, nameof(input));
        ArgumentOutOfRangeException.ThrowIfLessThan(output.Length, BlockBytes, nameof(output));

        uint x0 = BinaryPrimitives.ReadUInt32LittleEndian(input);
        uint x1 = BinaryPrimitives.ReadUInt32LittleEndian(input[4..]);
        uint x2 = BinaryPrimitives.ReadUInt32LittleEndian(input[8..]);
        uint x3 = BinaryPrimitives.ReadUInt32LittleEndian(input[12..]);

        EncryptRounds(ref x0, ref x1, ref x2, ref x3, ref MemoryMarshal.GetReference(roundKeys));

        BinaryPrimitives.WriteUInt32LittleEndian(output, x0);
        BinaryPrimitives.WriteUInt32LittleEndian(output[4..], x1);
        BinaryPrimitives.WriteUInt32LittleEndian(output[8..], x2);
        BinaryPrimitives.WriteUInt32LittleEndian(output[12..], x3);
    }

    /// <summary>
    /// Decrypts one 16-byte block under an expanded Serpent-128 key.
    /// </summary>
    /// <param name="roundKeys">The 132 round-key words.</param>
    /// <param name="input">The ciphertext block.</param>
    /// <param name="output">
    /// Receives the plaintext block; it may be the same memory as <paramref name="input" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="roundKeys" /> holds fewer than 132 words, or <paramref name="input" /> or
    /// <paramref name="output" /> is shorter than 16 bytes.
    /// </exception>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void DecryptBlock(ReadOnlySpan<uint> roundKeys, ReadOnlySpan<byte> input, Span<byte> output)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roundKeys.Length, RoundKeyWords, nameof(roundKeys));
        ArgumentOutOfRangeException.ThrowIfLessThan(input.Length, BlockBytes, nameof(input));
        ArgumentOutOfRangeException.ThrowIfLessThan(output.Length, BlockBytes, nameof(output));

        uint x0 = BinaryPrimitives.ReadUInt32LittleEndian(input);
        uint x1 = BinaryPrimitives.ReadUInt32LittleEndian(input[4..]);
        uint x2 = BinaryPrimitives.ReadUInt32LittleEndian(input[8..]);
        uint x3 = BinaryPrimitives.ReadUInt32LittleEndian(input[12..]);

        DecryptRounds(ref x0, ref x1, ref x2, ref x3, ref MemoryMarshal.GetReference(roundKeys));

        BinaryPrimitives.WriteUInt32LittleEndian(output, x0);
        BinaryPrimitives.WriteUInt32LittleEndian(output[4..], x1);
        BinaryPrimitives.WriteUInt32LittleEndian(output[8..], x2);
        BinaryPrimitives.WriteUInt32LittleEndian(output[12..], x3);
    }

    /// <summary>
    /// Applies the S-box with the specified index to four words: the form the key schedule and the wide-block variants
    /// use, whose S-box follows from a round number.
    /// </summary>
    /// <param name="index">The S-box index, 0 to 7.</param>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is not between 0 and 7.</exception>
    /// <remarks>
    /// The index is a round number, never data, so selecting the circuit by it leaks nothing.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void SBox(int index, ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        switch (index)
        {
            case 0: S0(ref x0, ref x1, ref x2, ref x3); break;
            case 1: S1(ref x0, ref x1, ref x2, ref x3); break;
            case 2: S2(ref x0, ref x1, ref x2, ref x3); break;
            case 3: S3(ref x0, ref x1, ref x2, ref x3); break;
            case 4: S4(ref x0, ref x1, ref x2, ref x3); break;
            case 5: S5(ref x0, ref x1, ref x2, ref x3); break;
            case 6: S6(ref x0, ref x1, ref x2, ref x3); break;
            case 7: S7(ref x0, ref x1, ref x2, ref x3); break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <summary>
    /// Applies the inverse of the S-box with the specified index to four words: the form the wide-block variants use.
    /// </summary>
    /// <param name="index">The index of the S-box whose inverse applies, 0 to 7.</param>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is not between 0 and 7.</exception>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void InverseSBox(int index, ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        switch (index)
        {
            case 0: I0(ref x0, ref x1, ref x2, ref x3); break;
            case 1: I1(ref x0, ref x1, ref x2, ref x3); break;
            case 2: I2(ref x0, ref x1, ref x2, ref x3); break;
            case 3: I3(ref x0, ref x1, ref x2, ref x3); break;
            case 4: I4(ref x0, ref x1, ref x2, ref x3); break;
            case 5: I5(ref x0, ref x1, ref x2, ref x3); break;
            case 6: I6(ref x0, ref x1, ref x2, ref x3); break;
            case 7: I7(ref x0, ref x1, ref x2, ref x3); break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <summary>
    /// Runs the 32 Serpent-128 encryption rounds over four words: in each, the round key, the round's S-box, and the
    /// linear transform, which the last round replaces with a final round key.
    /// </summary>
    /// <param name="x0">The first word.</param>
    /// <param name="x1">The second word.</param>
    /// <param name="x2">The third word.</param>
    /// <param name="x3">The fourth word.</param>
    /// <param name="roundKeys">The first of the 132 round-key words.</param>
    /// <remarks>
    /// The rounds run eight at a time, one per S-box, so every circuit is chosen at compile time rather than by the
    /// round number.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void EncryptRounds(ref uint x0, ref uint x1, ref uint x2, ref uint x3, ref uint roundKeys)
    {
        for (int r = 0; r < RoundCount; r += 8)
        {
            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r);
            S0(ref x0, ref x1, ref x2, ref x3);
            LinearTransform(ref x0, ref x1, ref x2, ref x3);

            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 1);
            S1(ref x0, ref x1, ref x2, ref x3);
            LinearTransform(ref x0, ref x1, ref x2, ref x3);

            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 2);
            S2(ref x0, ref x1, ref x2, ref x3);
            LinearTransform(ref x0, ref x1, ref x2, ref x3);

            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 3);
            S3(ref x0, ref x1, ref x2, ref x3);
            LinearTransform(ref x0, ref x1, ref x2, ref x3);

            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 4);
            S4(ref x0, ref x1, ref x2, ref x3);
            LinearTransform(ref x0, ref x1, ref x2, ref x3);

            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 5);
            S5(ref x0, ref x1, ref x2, ref x3);
            LinearTransform(ref x0, ref x1, ref x2, ref x3);

            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 6);
            S6(ref x0, ref x1, ref x2, ref x3);
            LinearTransform(ref x0, ref x1, ref x2, ref x3);

            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 7);
            S7(ref x0, ref x1, ref x2, ref x3);

            // The last round has no linear transform; the key after it takes its place.
            if (r + 8 < RoundCount)
                LinearTransform(ref x0, ref x1, ref x2, ref x3);
        }

        AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, RoundCount);
    }

    /// <summary>
    /// Runs the 32 Serpent-128 rounds backwards over four words, undoing <see cref="EncryptRounds" />.
    /// </summary>
    /// <param name="x0">The first word.</param>
    /// <param name="x1">The second word.</param>
    /// <param name="x2">The third word.</param>
    /// <param name="x3">The fourth word.</param>
    /// <param name="roundKeys">The first of the 132 round-key words.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DecryptRounds(ref uint x0, ref uint x1, ref uint x2, ref uint x3, ref uint roundKeys)
    {
        AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, RoundCount);

        for (int r = RoundCount - 8; r >= 0; r -= 8)
        {
            // The last encryption round had no linear transform to undo.
            if (r + 8 < RoundCount)
                InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);

            I7(ref x0, ref x1, ref x2, ref x3);
            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 7);

            InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
            I6(ref x0, ref x1, ref x2, ref x3);
            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 6);

            InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
            I5(ref x0, ref x1, ref x2, ref x3);
            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 5);

            InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
            I4(ref x0, ref x1, ref x2, ref x3);
            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 4);

            InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
            I3(ref x0, ref x1, ref x2, ref x3);
            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 3);

            InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
            I2(ref x0, ref x1, ref x2, ref x3);
            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 2);

            InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
            I1(ref x0, ref x1, ref x2, ref x3);
            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r + 1);

            InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
            I0(ref x0, ref x1, ref x2, ref x3);
            AddRoundKey(ref x0, ref x1, ref x2, ref x3, ref roundKeys, r);
        }
    }

    /// <summary>
    /// Applies the Serpent linear transform <c>L</c> to four words.
    /// </summary>
    /// <param name="x0">The first word.</param>
    /// <param name="x1">The second word.</param>
    /// <param name="x2">The third word.</param>
    /// <param name="x3">The fourth word.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LinearTransform(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        x0 = x0.RotateBitsLeftUnchecked(13);
        x2 = x2.RotateBitsLeftUnchecked(3);
        x1 ^= x0 ^ x2;
        x3 ^= x2 ^ (x0 << 3);
        x1 = x1.RotateBitsLeftUnchecked(1);
        x3 = x3.RotateBitsLeftUnchecked(7);
        x0 ^= x1 ^ x3;
        x2 ^= x3 ^ (x1 << 7);
        x0 = x0.RotateBitsLeftUnchecked(5);
        x2 = x2.RotateBitsLeftUnchecked(22);
    }

    /// <summary>
    /// Applies the inverse of the Serpent linear transform to four words.
    /// </summary>
    /// <param name="x0">The first word.</param>
    /// <param name="x1">The second word.</param>
    /// <param name="x2">The third word.</param>
    /// <param name="x3">The fourth word.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void InverseLinearTransform(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        x2 = x2.RotateBitsRightUnchecked(22);
        x0 = x0.RotateBitsRightUnchecked(5);
        x2 ^= x3 ^ (x1 << 7);
        x0 ^= x1 ^ x3;
        x3 = x3.RotateBitsRightUnchecked(7);
        x1 = x1.RotateBitsRightUnchecked(1);
        x3 ^= x2 ^ (x0 << 3);
        x1 ^= x0 ^ x2;
        x2 = x2.RotateBitsRightUnchecked(3);
        x0 = x0.RotateBitsRightUnchecked(13);
    }

    /// <summary>
    /// Adds the four words of a round key to four words by XOR.
    /// </summary>
    /// <param name="x0">The first word.</param>
    /// <param name="x1">The second word.</param>
    /// <param name="x2">The third word.</param>
    /// <param name="x3">The fourth word.</param>
    /// <param name="roundKeys">The first of the round-key words.</param>
    /// <param name="round">The round whose key is added: 0 to 32.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddRoundKey(ref uint x0, ref uint x1, ref uint x2, ref uint x3, ref uint roundKeys, int round)
    {
        ref uint key = ref Unsafe.Add(ref roundKeys, round * 4);
        x0 ^= key;
        x1 ^= Unsafe.Add(ref key, 1);
        x2 ^= Unsafe.Add(ref key, 2);
        x3 ^= Unsafe.Add(ref key, 3);
    }
}
