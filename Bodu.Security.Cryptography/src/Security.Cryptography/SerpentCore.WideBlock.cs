// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCore.WideBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

// The rounds of the wide-block variants (Serpent-256, 512 and 1024). Each round adds its round key, applies its S-box to
// every four-word group, applies the linear transform to every group and rotates the words one position to the left;
// the last round replaces the linear transform and the rotation with a final round key. The tweak the construction
// injects every fourth round arrives folded into the round keys.
internal static partial class SerpentCore
{
    /// <summary>The number of words in the state the resident rounds hold in locals: Serpent-256's.</summary>
    internal const int ResidentWideBlockWords = 8;

    /// <summary>The number of words in the widest state the streamed rounds take: Serpent-1024's.</summary>
    internal const int MaximumWideBlockWords = 32;

    /// <summary>
    /// Encrypts one block of a wide-block Serpent variant, holding its words in locals when it has eight and streaming
    /// it a four-word group at a time otherwise.
    /// </summary>
    /// <param name="roundKeys">
    /// The round keys: <paramref name="rounds" /> + 1 keys of <paramref name="words" /> words each, with the tweak
    /// folded in.
    /// </param>
    /// <param name="words">The number of 32-bit words in a block.</param>
    /// <param name="rounds">The number of rounds.</param>
    /// <param name="input">The plaintext block.</param>
    /// <param name="output">
    /// Receives the ciphertext block; it may be the same memory as <paramref name="input" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="words" /> is not 8, 16 or 32; <paramref name="rounds" /> is not a positive multiple of 8; or
    /// <paramref name="roundKeys" />, <paramref name="input" /> or <paramref name="output" /> is too short.
    /// </exception>
    internal static void EncryptWideBlock(ReadOnlySpan<uint> roundKeys, int words, int rounds, ReadOnlySpan<byte> input, Span<byte> output)
    {
        if (words == ResidentWideBlockWords)
            EncryptResidentWideBlock(roundKeys, rounds, input, output);
        else
            EncryptStreamedWideBlock(roundKeys, words, rounds, input, output);
    }

    /// <summary>
    /// Decrypts one block of a wide-block Serpent variant, holding its words in locals when it has eight and streaming
    /// it a four-word group at a time otherwise.
    /// </summary>
    /// <param name="roundKeys">
    /// The round keys: <paramref name="rounds" /> + 1 keys of <paramref name="words" /> words each, with the tweak
    /// folded in.
    /// </param>
    /// <param name="words">The number of 32-bit words in a block.</param>
    /// <param name="rounds">The number of rounds.</param>
    /// <param name="input">The ciphertext block.</param>
    /// <param name="output">
    /// Receives the plaintext block; it may be the same memory as <paramref name="input" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="words" /> is not 8, 16 or 32; <paramref name="rounds" /> is not a positive multiple of 8; or
    /// <paramref name="roundKeys" />, <paramref name="input" /> or <paramref name="output" /> is too short.
    /// </exception>
    internal static void DecryptWideBlock(ReadOnlySpan<uint> roundKeys, int words, int rounds, ReadOnlySpan<byte> input, Span<byte> output)
    {
        if (words == ResidentWideBlockWords)
            DecryptResidentWideBlock(roundKeys, rounds, input, output);
        else
            DecryptStreamedWideBlock(roundKeys, words, rounds, input, output);
    }

    /// <summary>
    /// Encrypts one block of a wide-block Serpent variant a four-word group at a time, between two copies of the state.
    /// </summary>
    /// <param name="roundKeys">
    /// The round keys: <paramref name="rounds" /> + 1 keys of <paramref name="words" /> words each, with the tweak
    /// folded in.
    /// </param>
    /// <param name="words">The number of 32-bit words in a block.</param>
    /// <param name="rounds">The number of rounds.</param>
    /// <param name="input">The plaintext block.</param>
    /// <param name="output">
    /// Receives the ciphertext block; it may be the same memory as <paramref name="input" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="words" /> is not 8, 16 or 32; <paramref name="rounds" /> is not a positive multiple of 8; or
    /// <paramref name="roundKeys" />, <paramref name="input" /> or <paramref name="output" /> is too short.
    /// </exception>
    /// <remarks>
    /// Each round reads one copy a group at a time into locals and writes the other, placing every group one word to
    /// the left, which is the construction's word rotation. The rounds run eight at a time, one per S-box. The block is
    /// read in full before the first round and written only by the last, so the output may overlap the input.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void EncryptStreamedWideBlock(ReadOnlySpan<uint> roundKeys, int words, int rounds, ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfWideBlockIsInvalid(roundKeys, words, rounds, input, output);

        Span<uint> copies = stackalloc uint[2 * MaximumWideBlockWords];
        ref uint a = ref MemoryMarshal.GetReference(copies);
        ref uint b = ref Unsafe.Add(ref a, words);
        for (int i = 0; i < words; i++)
            Unsafe.Add(ref a, i) = BinaryPrimitives.ReadUInt32LittleEndian(input[(i * 4)..]);

        ref uint keys = ref MemoryMarshal.GetReference(roundKeys);
        int round = 0;
        while (true)
        {
            StreamedRound<SBox0>(ref a, ref b, ref Unsafe.Add(ref keys, round * words), words);
            StreamedRound<SBox1>(ref b, ref a, ref Unsafe.Add(ref keys, (round + 1) * words), words);
            StreamedRound<SBox2>(ref a, ref b, ref Unsafe.Add(ref keys, (round + 2) * words), words);
            StreamedRound<SBox3>(ref b, ref a, ref Unsafe.Add(ref keys, (round + 3) * words), words);
            StreamedRound<SBox4>(ref a, ref b, ref Unsafe.Add(ref keys, (round + 4) * words), words);
            StreamedRound<SBox5>(ref b, ref a, ref Unsafe.Add(ref keys, (round + 5) * words), words);
            StreamedRound<SBox6>(ref a, ref b, ref Unsafe.Add(ref keys, (round + 6) * words), words);
            if (round + 8 == rounds)
                break;

            StreamedRound<SBox7>(ref b, ref a, ref Unsafe.Add(ref keys, (round + 7) * words), words);
            round += 8;
        }

        // The last round: its key, S7 and the final key, with no linear transform or rotation.
        ref uint key = ref Unsafe.Add(ref keys, (round + 7) * words);
        ref uint last = ref Unsafe.Add(ref key, words);
        for (int g = 0; g < words; g += 4)
        {
            uint x0 = Unsafe.Add(ref b, g) ^ Unsafe.Add(ref key, g);
            uint x1 = Unsafe.Add(ref b, g + 1) ^ Unsafe.Add(ref key, g + 1);
            uint x2 = Unsafe.Add(ref b, g + 2) ^ Unsafe.Add(ref key, g + 2);
            uint x3 = Unsafe.Add(ref b, g + 3) ^ Unsafe.Add(ref key, g + 3);

            S7(ref x0, ref x1, ref x2, ref x3);

            BinaryPrimitives.WriteUInt32LittleEndian(output[(g * 4)..], x0 ^ Unsafe.Add(ref last, g));
            BinaryPrimitives.WriteUInt32LittleEndian(output[((g + 1) * 4)..], x1 ^ Unsafe.Add(ref last, g + 1));
            BinaryPrimitives.WriteUInt32LittleEndian(output[((g + 2) * 4)..], x2 ^ Unsafe.Add(ref last, g + 2));
            BinaryPrimitives.WriteUInt32LittleEndian(output[((g + 3) * 4)..], x3 ^ Unsafe.Add(ref last, g + 3));
        }

        CryptographyHelper.Clear(copies[..(2 * words)]);
    }

    /// <summary>
    /// Decrypts one block of a wide-block Serpent variant a four-word group at a time, between two copies of the state.
    /// </summary>
    /// <param name="roundKeys">
    /// The round keys: <paramref name="rounds" /> + 1 keys of <paramref name="words" /> words each, with the tweak
    /// folded in.
    /// </param>
    /// <param name="words">The number of 32-bit words in a block.</param>
    /// <param name="rounds">The number of rounds.</param>
    /// <param name="input">The ciphertext block.</param>
    /// <param name="output">
    /// Receives the plaintext block; it may be the same memory as <paramref name="input" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="words" /> is not 8, 16 or 32; <paramref name="rounds" /> is not a positive multiple of 8; or
    /// <paramref name="roundKeys" />, <paramref name="input" /> or <paramref name="output" /> is too short.
    /// </exception>
    /// <remarks>
    /// The rounds run backwards, undoing <see cref="EncryptStreamedWideBlock" />'s. The block is read in full by the
    /// first and written only after the last, so the output may overlap the input.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void DecryptStreamedWideBlock(ReadOnlySpan<uint> roundKeys, int words, int rounds, ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfWideBlockIsInvalid(roundKeys, words, rounds, input, output);

        Span<uint> copies = stackalloc uint[2 * MaximumWideBlockWords];
        ref uint a = ref MemoryMarshal.GetReference(copies);
        ref uint b = ref Unsafe.Add(ref a, words);
        ref uint keys = ref MemoryMarshal.GetReference(roundKeys);
        int round = rounds - 8;

        // Undo the last round: the final key, S7 and the round's key.
        ref uint key = ref Unsafe.Add(ref keys, (round + 7) * words);
        ref uint last = ref Unsafe.Add(ref key, words);
        for (int g = 0; g < words; g += 4)
        {
            uint x0 = BinaryPrimitives.ReadUInt32LittleEndian(input[(g * 4)..]) ^ Unsafe.Add(ref last, g);
            uint x1 = BinaryPrimitives.ReadUInt32LittleEndian(input[((g + 1) * 4)..]) ^ Unsafe.Add(ref last, g + 1);
            uint x2 = BinaryPrimitives.ReadUInt32LittleEndian(input[((g + 2) * 4)..]) ^ Unsafe.Add(ref last, g + 2);
            uint x3 = BinaryPrimitives.ReadUInt32LittleEndian(input[((g + 3) * 4)..]) ^ Unsafe.Add(ref last, g + 3);

            I7(ref x0, ref x1, ref x2, ref x3);

            Unsafe.Add(ref a, g) = x0 ^ Unsafe.Add(ref key, g);
            Unsafe.Add(ref a, g + 1) = x1 ^ Unsafe.Add(ref key, g + 1);
            Unsafe.Add(ref a, g + 2) = x2 ^ Unsafe.Add(ref key, g + 2);
            Unsafe.Add(ref a, g + 3) = x3 ^ Unsafe.Add(ref key, g + 3);
        }

        while (true)
        {
            InverseStreamedRound<SBox6>(ref a, ref b, ref Unsafe.Add(ref keys, (round + 6) * words), words);
            InverseStreamedRound<SBox5>(ref b, ref a, ref Unsafe.Add(ref keys, (round + 5) * words), words);
            InverseStreamedRound<SBox4>(ref a, ref b, ref Unsafe.Add(ref keys, (round + 4) * words), words);
            InverseStreamedRound<SBox3>(ref b, ref a, ref Unsafe.Add(ref keys, (round + 3) * words), words);
            InverseStreamedRound<SBox2>(ref a, ref b, ref Unsafe.Add(ref keys, (round + 2) * words), words);
            InverseStreamedRound<SBox1>(ref b, ref a, ref Unsafe.Add(ref keys, (round + 1) * words), words);
            InverseStreamedRound<SBox0>(ref a, ref b, ref Unsafe.Add(ref keys, round * words), words);
            if (round == 0)
                break;

            round -= 8;
            InverseStreamedRound<SBox7>(ref b, ref a, ref Unsafe.Add(ref keys, (round + 7) * words), words);
        }

        for (int i = 0; i < words; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(output[(i * 4)..], Unsafe.Add(ref b, i));

        CryptographyHelper.Clear(copies[..(2 * words)]);
    }

    /// <summary>
    /// Encrypts one block of an eight-word wide-block Serpent variant with its words held in locals, so that the word
    /// rotation is a renaming of the locals.
    /// </summary>
    /// <param name="roundKeys">
    /// The round keys: <paramref name="rounds" /> + 1 keys of eight words each, with the tweak folded in.
    /// </param>
    /// <param name="rounds">The number of rounds.</param>
    /// <param name="input">The plaintext block.</param>
    /// <param name="output">
    /// Receives the ciphertext block; it may be the same memory as <paramref name="input" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="rounds" /> is not a positive multiple of 8, or <paramref name="roundKeys" />,
    /// <paramref name="input" /> or <paramref name="output" /> is too short.
    /// </exception>
    /// <remarks>
    /// With the locals <c>s0</c> to <c>s7</c>, word <c>i</c> of the state after round <c>k</c> of each eight is
    /// <c>s[(i + k + 1) mod 8]</c>: round <c>k</c> works on the groups <c>s[k..k+3]</c> and <c>s[k+4..k+7]</c>, indices
    /// taken mod 8, and every eight rounds the words are back in their places.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void EncryptResidentWideBlock(ReadOnlySpan<uint> roundKeys, int rounds, ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfWideBlockIsInvalid(roundKeys, ResidentWideBlockWords, rounds, input, output);

        uint s0 = BinaryPrimitives.ReadUInt32LittleEndian(input);
        uint s1 = BinaryPrimitives.ReadUInt32LittleEndian(input[4..]);
        uint s2 = BinaryPrimitives.ReadUInt32LittleEndian(input[8..]);
        uint s3 = BinaryPrimitives.ReadUInt32LittleEndian(input[12..]);
        uint s4 = BinaryPrimitives.ReadUInt32LittleEndian(input[16..]);
        uint s5 = BinaryPrimitives.ReadUInt32LittleEndian(input[20..]);
        uint s6 = BinaryPrimitives.ReadUInt32LittleEndian(input[24..]);
        uint s7 = BinaryPrimitives.ReadUInt32LittleEndian(input[28..]);

        ref uint key = ref MemoryMarshal.GetReference(roundKeys);
        int round = 0;
        while (true)
        {
            ResidentGroup<SBox0>(ref s0, ref s1, ref s2, ref s3, ref key);
            ResidentGroup<SBox0>(ref s4, ref s5, ref s6, ref s7, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Add(ref key, ResidentWideBlockWords);
            ResidentGroup<SBox1>(ref s1, ref s2, ref s3, ref s4, ref key);
            ResidentGroup<SBox1>(ref s5, ref s6, ref s7, ref s0, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Add(ref key, ResidentWideBlockWords);
            ResidentGroup<SBox2>(ref s2, ref s3, ref s4, ref s5, ref key);
            ResidentGroup<SBox2>(ref s6, ref s7, ref s0, ref s1, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Add(ref key, ResidentWideBlockWords);
            ResidentGroup<SBox3>(ref s3, ref s4, ref s5, ref s6, ref key);
            ResidentGroup<SBox3>(ref s7, ref s0, ref s1, ref s2, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Add(ref key, ResidentWideBlockWords);
            ResidentGroup<SBox4>(ref s4, ref s5, ref s6, ref s7, ref key);
            ResidentGroup<SBox4>(ref s0, ref s1, ref s2, ref s3, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Add(ref key, ResidentWideBlockWords);
            ResidentGroup<SBox5>(ref s5, ref s6, ref s7, ref s0, ref key);
            ResidentGroup<SBox5>(ref s1, ref s2, ref s3, ref s4, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Add(ref key, ResidentWideBlockWords);
            ResidentGroup<SBox6>(ref s6, ref s7, ref s0, ref s1, ref key);
            ResidentGroup<SBox6>(ref s2, ref s3, ref s4, ref s5, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Add(ref key, ResidentWideBlockWords);
            if (round + 8 == rounds)
                break;

            ResidentGroup<SBox7>(ref s7, ref s0, ref s1, ref s2, ref key);
            ResidentGroup<SBox7>(ref s3, ref s4, ref s5, ref s6, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Add(ref key, ResidentWideBlockWords);
            round += 8;
        }

        // The last round: its key, S7 and the final key, with no linear transform or rotation. Word i of the state is
        // s[(i + 7) mod 8].
        s7 ^= key;
        s0 ^= Unsafe.Add(ref key, 1);
        s1 ^= Unsafe.Add(ref key, 2);
        s2 ^= Unsafe.Add(ref key, 3);
        s3 ^= Unsafe.Add(ref key, 4);
        s4 ^= Unsafe.Add(ref key, 5);
        s5 ^= Unsafe.Add(ref key, 6);
        s6 ^= Unsafe.Add(ref key, 7);

        S7(ref s7, ref s0, ref s1, ref s2);
        S7(ref s3, ref s4, ref s5, ref s6);

        ref uint last = ref Unsafe.Add(ref key, ResidentWideBlockWords);
        BinaryPrimitives.WriteUInt32LittleEndian(output, s7 ^ last);
        BinaryPrimitives.WriteUInt32LittleEndian(output[4..], s0 ^ Unsafe.Add(ref last, 1));
        BinaryPrimitives.WriteUInt32LittleEndian(output[8..], s1 ^ Unsafe.Add(ref last, 2));
        BinaryPrimitives.WriteUInt32LittleEndian(output[12..], s2 ^ Unsafe.Add(ref last, 3));
        BinaryPrimitives.WriteUInt32LittleEndian(output[16..], s3 ^ Unsafe.Add(ref last, 4));
        BinaryPrimitives.WriteUInt32LittleEndian(output[20..], s4 ^ Unsafe.Add(ref last, 5));
        BinaryPrimitives.WriteUInt32LittleEndian(output[24..], s5 ^ Unsafe.Add(ref last, 6));
        BinaryPrimitives.WriteUInt32LittleEndian(output[28..], s6 ^ Unsafe.Add(ref last, 7));
    }

    /// <summary>
    /// Decrypts one block of an eight-word wide-block Serpent variant with its words held in locals, undoing
    /// <see cref="EncryptResidentWideBlock" />.
    /// </summary>
    /// <param name="roundKeys">
    /// The round keys: <paramref name="rounds" /> + 1 keys of eight words each, with the tweak folded in.
    /// </param>
    /// <param name="rounds">The number of rounds.</param>
    /// <param name="input">The ciphertext block.</param>
    /// <param name="output">
    /// Receives the plaintext block; it may be the same memory as <paramref name="input" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="rounds" /> is not a positive multiple of 8, or <paramref name="roundKeys" />,
    /// <paramref name="input" /> or <paramref name="output" /> is too short.
    /// </exception>
    /// <remarks>
    /// Undoing round <c>k</c> of each eight works on the same groups as the round itself, <c>s[k..k+3]</c> and
    /// <c>s[k+4..k+7]</c>, indices taken mod 8.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void DecryptResidentWideBlock(ReadOnlySpan<uint> roundKeys, int rounds, ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfWideBlockIsInvalid(roundKeys, ResidentWideBlockWords, rounds, input, output);

        // The last round left word i of the state in s[(i + 7) mod 8].
        uint s7 = BinaryPrimitives.ReadUInt32LittleEndian(input);
        uint s0 = BinaryPrimitives.ReadUInt32LittleEndian(input[4..]);
        uint s1 = BinaryPrimitives.ReadUInt32LittleEndian(input[8..]);
        uint s2 = BinaryPrimitives.ReadUInt32LittleEndian(input[12..]);
        uint s3 = BinaryPrimitives.ReadUInt32LittleEndian(input[16..]);
        uint s4 = BinaryPrimitives.ReadUInt32LittleEndian(input[20..]);
        uint s5 = BinaryPrimitives.ReadUInt32LittleEndian(input[24..]);
        uint s6 = BinaryPrimitives.ReadUInt32LittleEndian(input[28..]);

        // Undo the last round: the final key, S7 and the round's key.
        ref uint key = ref Unsafe.Add(ref MemoryMarshal.GetReference(roundKeys), (rounds - 1) * ResidentWideBlockWords);
        ref uint last = ref Unsafe.Add(ref key, ResidentWideBlockWords);
        s7 ^= last;
        s0 ^= Unsafe.Add(ref last, 1);
        s1 ^= Unsafe.Add(ref last, 2);
        s2 ^= Unsafe.Add(ref last, 3);
        s3 ^= Unsafe.Add(ref last, 4);
        s4 ^= Unsafe.Add(ref last, 5);
        s5 ^= Unsafe.Add(ref last, 6);
        s6 ^= Unsafe.Add(ref last, 7);

        I7(ref s7, ref s0, ref s1, ref s2);
        I7(ref s3, ref s4, ref s5, ref s6);

        s7 ^= key;
        s0 ^= Unsafe.Add(ref key, 1);
        s1 ^= Unsafe.Add(ref key, 2);
        s2 ^= Unsafe.Add(ref key, 3);
        s3 ^= Unsafe.Add(ref key, 4);
        s4 ^= Unsafe.Add(ref key, 5);
        s5 ^= Unsafe.Add(ref key, 6);
        s6 ^= Unsafe.Add(ref key, 7);

        int round = rounds - 8;
        while (true)
        {
            key = ref Unsafe.Subtract(ref key, ResidentWideBlockWords);
            InverseResidentGroup<SBox6>(ref s6, ref s7, ref s0, ref s1, ref key);
            InverseResidentGroup<SBox6>(ref s2, ref s3, ref s4, ref s5, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Subtract(ref key, ResidentWideBlockWords);
            InverseResidentGroup<SBox5>(ref s5, ref s6, ref s7, ref s0, ref key);
            InverseResidentGroup<SBox5>(ref s1, ref s2, ref s3, ref s4, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Subtract(ref key, ResidentWideBlockWords);
            InverseResidentGroup<SBox4>(ref s4, ref s5, ref s6, ref s7, ref key);
            InverseResidentGroup<SBox4>(ref s0, ref s1, ref s2, ref s3, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Subtract(ref key, ResidentWideBlockWords);
            InverseResidentGroup<SBox3>(ref s3, ref s4, ref s5, ref s6, ref key);
            InverseResidentGroup<SBox3>(ref s7, ref s0, ref s1, ref s2, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Subtract(ref key, ResidentWideBlockWords);
            InverseResidentGroup<SBox2>(ref s2, ref s3, ref s4, ref s5, ref key);
            InverseResidentGroup<SBox2>(ref s6, ref s7, ref s0, ref s1, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Subtract(ref key, ResidentWideBlockWords);
            InverseResidentGroup<SBox1>(ref s1, ref s2, ref s3, ref s4, ref key);
            InverseResidentGroup<SBox1>(ref s5, ref s6, ref s7, ref s0, ref Unsafe.Add(ref key, 4));
            key = ref Unsafe.Subtract(ref key, ResidentWideBlockWords);
            InverseResidentGroup<SBox0>(ref s0, ref s1, ref s2, ref s3, ref key);
            InverseResidentGroup<SBox0>(ref s4, ref s5, ref s6, ref s7, ref Unsafe.Add(ref key, 4));
            if (round == 0)
                break;

            round -= 8;
            key = ref Unsafe.Subtract(ref key, ResidentWideBlockWords);
            InverseResidentGroup<SBox7>(ref s7, ref s0, ref s1, ref s2, ref key);
            InverseResidentGroup<SBox7>(ref s3, ref s4, ref s5, ref s6, ref Unsafe.Add(ref key, 4));
        }

        BinaryPrimitives.WriteUInt32LittleEndian(output, s0);
        BinaryPrimitives.WriteUInt32LittleEndian(output[4..], s1);
        BinaryPrimitives.WriteUInt32LittleEndian(output[8..], s2);
        BinaryPrimitives.WriteUInt32LittleEndian(output[12..], s3);
        BinaryPrimitives.WriteUInt32LittleEndian(output[16..], s4);
        BinaryPrimitives.WriteUInt32LittleEndian(output[20..], s5);
        BinaryPrimitives.WriteUInt32LittleEndian(output[24..], s6);
        BinaryPrimitives.WriteUInt32LittleEndian(output[28..], s7);
    }

    /// <summary>
    /// Runs one round of a streamed wide block: adds the round key to each four-word group of
    /// <paramref name="source" />, applies the S-box and the linear transform, and writes the group to
    /// <paramref name="destination" /> one word to the left.
    /// </summary>
    /// <typeparam name="TSBox">The round's S-box.</typeparam>
    /// <param name="source">The first word of the state the round reads.</param>
    /// <param name="destination">
    /// The first word of the state the round writes, apart from <paramref name="source" />.
    /// </param>
    /// <param name="key">The first word of the round key.</param>
    /// <param name="words">The number of words in the state: 8, 16 or 32.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StreamedRound<TSBox>(ref uint source, ref uint destination, ref uint key, int words)
        where TSBox : struct, IRoundSBox
    {
        int mask = words - 1;
        for (int g = 0; g < words; g += 4)
        {
            uint x0 = Unsafe.Add(ref source, g) ^ Unsafe.Add(ref key, g);
            uint x1 = Unsafe.Add(ref source, g + 1) ^ Unsafe.Add(ref key, g + 1);
            uint x2 = Unsafe.Add(ref source, g + 2) ^ Unsafe.Add(ref key, g + 2);
            uint x3 = Unsafe.Add(ref source, g + 3) ^ Unsafe.Add(ref key, g + 3);

            TSBox.Forward(ref x0, ref x1, ref x2, ref x3);
            LinearTransform(ref x0, ref x1, ref x2, ref x3);

            // Word j of the rotated state is word j + 1 of the old one, so the group's first word ends the group before
            // it, or the state.
            Unsafe.Add(ref destination, (g - 1) & mask) = x0;
            Unsafe.Add(ref destination, g) = x1;
            Unsafe.Add(ref destination, g + 1) = x2;
            Unsafe.Add(ref destination, g + 2) = x3;
        }
    }

    /// <summary>
    /// Undoes one round of a streamed wide block: reads each four-word group of <paramref name="source" /> from one
    /// word to the left, undoing the rotation, applies the inverse linear transform and the inverse S-box, and writes
    /// the group to <paramref name="destination" /> with the round key added.
    /// </summary>
    /// <typeparam name="TSBox">The round's S-box.</typeparam>
    /// <param name="source">The first word of the state the round reads.</param>
    /// <param name="destination">
    /// The first word of the state the round writes, apart from <paramref name="source" />.
    /// </param>
    /// <param name="key">The first word of the round key.</param>
    /// <param name="words">The number of words in the state: 8, 16 or 32.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InverseStreamedRound<TSBox>(ref uint source, ref uint destination, ref uint key, int words)
        where TSBox : struct, IRoundSBox
    {
        int mask = words - 1;
        for (int g = 0; g < words; g += 4)
        {
            uint x0 = Unsafe.Add(ref source, (g - 1) & mask);
            uint x1 = Unsafe.Add(ref source, g);
            uint x2 = Unsafe.Add(ref source, g + 1);
            uint x3 = Unsafe.Add(ref source, g + 2);

            InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
            TSBox.Inverse(ref x0, ref x1, ref x2, ref x3);

            Unsafe.Add(ref destination, g) = x0 ^ Unsafe.Add(ref key, g);
            Unsafe.Add(ref destination, g + 1) = x1 ^ Unsafe.Add(ref key, g + 1);
            Unsafe.Add(ref destination, g + 2) = x2 ^ Unsafe.Add(ref key, g + 2);
            Unsafe.Add(ref destination, g + 3) = x3 ^ Unsafe.Add(ref key, g + 3);
        }
    }

    /// <summary>
    /// Runs one group of a resident round: adds four words of the round key, then applies the S-box and the linear
    /// transform.
    /// </summary>
    /// <typeparam name="TSBox">The round's S-box.</typeparam>
    /// <param name="x0">The group's first word.</param>
    /// <param name="x1">The group's second word.</param>
    /// <param name="x2">The group's third word.</param>
    /// <param name="x3">The group's fourth word.</param>
    /// <param name="key">The first of the four round-key words.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ResidentGroup<TSBox>(ref uint x0, ref uint x1, ref uint x2, ref uint x3, ref uint key)
        where TSBox : struct, IRoundSBox
    {
        x0 ^= key;
        x1 ^= Unsafe.Add(ref key, 1);
        x2 ^= Unsafe.Add(ref key, 2);
        x3 ^= Unsafe.Add(ref key, 3);

        TSBox.Forward(ref x0, ref x1, ref x2, ref x3);
        LinearTransform(ref x0, ref x1, ref x2, ref x3);
    }

    /// <summary>
    /// Undoes one group of a resident round: applies the inverse linear transform and the inverse S-box, then adds four
    /// words of the round key.
    /// </summary>
    /// <typeparam name="TSBox">The round's S-box.</typeparam>
    /// <param name="x0">The group's first word.</param>
    /// <param name="x1">The group's second word.</param>
    /// <param name="x2">The group's third word.</param>
    /// <param name="x3">The group's fourth word.</param>
    /// <param name="key">The first of the four round-key words.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InverseResidentGroup<TSBox>(ref uint x0, ref uint x1, ref uint x2, ref uint x3, ref uint key)
        where TSBox : struct, IRoundSBox
    {
        InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
        TSBox.Inverse(ref x0, ref x1, ref x2, ref x3);

        x0 ^= key;
        x1 ^= Unsafe.Add(ref key, 1);
        x2 ^= Unsafe.Add(ref key, 2);
        x3 ^= Unsafe.Add(ref key, 3);
    }

    /// <summary>
    /// Validates the arguments of a wide-block encryption or decryption.
    /// </summary>
    /// <param name="roundKeys">The round keys.</param>
    /// <param name="words">The number of 32-bit words in a block.</param>
    /// <param name="rounds">The number of rounds.</param>
    /// <param name="input">The input block.</param>
    /// <param name="output">The output block.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="words" /> is not 8, 16 or 32; <paramref name="rounds" /> is not a positive multiple of 8; or
    /// <paramref name="roundKeys" />, <paramref name="input" /> or <paramref name="output" /> is too short.
    /// </exception>
    private static void ThrowIfWideBlockIsInvalid(ReadOnlySpan<uint> roundKeys, int words, int rounds, ReadOnlySpan<byte> input, ReadOnlySpan<byte> output)
    {
        if (words is not (8 or 16 or 32)) throw new ArgumentOutOfRangeException(nameof(words));
        if (rounds <= 0 || (rounds & 7) != 0) throw new ArgumentOutOfRangeException(nameof(rounds));
        ArgumentOutOfRangeException.ThrowIfLessThan(roundKeys.Length, (rounds + 1) * words, nameof(roundKeys));
        ArgumentOutOfRangeException.ThrowIfLessThan(input.Length, words * 4, nameof(input));
        ArgumentOutOfRangeException.ThrowIfLessThan(output.Length, words * 4, nameof(output));
    }
}
