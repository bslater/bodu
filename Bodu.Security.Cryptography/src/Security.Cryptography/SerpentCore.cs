// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
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
    /// Encrypts a run of 16-byte blocks independently under an expanded Serpent-128 key, with the kernel dispatch
    /// selects.
    /// </summary>
    /// <param name="roundKeys">The 132 round-key words.</param>
    /// <param name="input">The plaintext, a whole number of blocks.</param>
    /// <param name="output">
    /// The destination, at least as long as <paramref name="input" />; it may be the same memory, but must not
    /// partially overlap it.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="roundKeys" /> holds fewer than 132 words, or <paramref name="output" /> is shorter than
    /// <paramref name="input" />.
    /// </exception>
    /// <exception cref="ArgumentException">The length of <paramref name="input" /> is not a multiple of 16.</exception>
    internal static void EncryptBlocks(ReadOnlySpan<uint> roundKeys, ReadOnlySpan<byte> input, Span<byte> output) =>
        TransformBlocks(KernelKind.Auto, encrypt: true, roundKeys, input, output);

    /// <summary>
    /// Encrypts a run of 16-byte blocks independently under an expanded Serpent-128 key, with the specified kernel.
    /// </summary>
    /// <param name="kernel">The kernel, or <see cref="KernelKind.Auto" /> for the one dispatch selects.</param>
    /// <param name="roundKeys">The 132 round-key words.</param>
    /// <param name="input">The plaintext, a whole number of blocks.</param>
    /// <param name="output">
    /// The destination, at least as long as <paramref name="input" />; it may be the same memory, but must not
    /// partially overlap it.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="roundKeys" /> holds fewer than 132 words, or <paramref name="output" /> is shorter than
    /// <paramref name="input" />.
    /// </exception>
    /// <exception cref="ArgumentException">The length of <paramref name="input" /> is not a multiple of 16.</exception>
    /// <remarks>
    /// Every kernel produces the output of <see cref="EncryptBlock" />; a kernel the processor does not support throws
    /// <see cref="PlatformNotSupportedException" />.
    /// </remarks>
    internal static void EncryptBlocks(KernelKind kernel, ReadOnlySpan<uint> roundKeys, ReadOnlySpan<byte> input, Span<byte> output) =>
        TransformBlocks(kernel, encrypt: true, roundKeys, input, output);

    /// <summary>
    /// Decrypts a run of 16-byte blocks independently under an expanded Serpent-128 key, with the kernel dispatch
    /// selects.
    /// </summary>
    /// <param name="roundKeys">The 132 round-key words.</param>
    /// <param name="input">The ciphertext, a whole number of blocks.</param>
    /// <param name="output">
    /// The destination, at least as long as <paramref name="input" />; it may be the same memory, but must not
    /// partially overlap it.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="roundKeys" /> holds fewer than 132 words, or <paramref name="output" /> is shorter than
    /// <paramref name="input" />.
    /// </exception>
    /// <exception cref="ArgumentException">The length of <paramref name="input" /> is not a multiple of 16.</exception>
    internal static void DecryptBlocks(ReadOnlySpan<uint> roundKeys, ReadOnlySpan<byte> input, Span<byte> output) =>
        TransformBlocks(KernelKind.Auto, encrypt: false, roundKeys, input, output);

    /// <summary>
    /// Decrypts a run of 16-byte blocks independently under an expanded Serpent-128 key, with the specified kernel.
    /// </summary>
    /// <param name="kernel">The kernel, or <see cref="KernelKind.Auto" /> for the one dispatch selects.</param>
    /// <param name="roundKeys">The 132 round-key words.</param>
    /// <param name="input">The ciphertext, a whole number of blocks.</param>
    /// <param name="output">
    /// The destination, at least as long as <paramref name="input" />; it may be the same memory, but must not
    /// partially overlap it.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="roundKeys" /> holds fewer than 132 words, or <paramref name="output" /> is shorter than
    /// <paramref name="input" />.
    /// </exception>
    /// <exception cref="ArgumentException">The length of <paramref name="input" /> is not a multiple of 16.</exception>
    /// <remarks>
    /// Every kernel produces the output of <see cref="DecryptBlock" />; a kernel the processor does not support throws
    /// <see cref="PlatformNotSupportedException" />.
    /// </remarks>
    internal static void DecryptBlocks(KernelKind kernel, ReadOnlySpan<uint> roundKeys, ReadOnlySpan<byte> input, Span<byte> output) =>
        TransformBlocks(kernel, encrypt: false, roundKeys, input, output);

    /// <summary>
    /// Selects the widest kernel the processor supports and the process allows: AVX-512, then AVX2, then AdvSimd on
    /// ARM64, then SSSE3, then the scalar rounds.
    /// </summary>
    /// <returns>The kernel dispatch runs; never <see cref="KernelKind.Auto" />.</returns>
    /// <remarks>
    /// Every gate honors the <see cref="SimdCapabilities.DisableSimdSwitchName" /> switch, which pins the scalar
    /// rounds.
    /// </remarks>
    internal static KernelKind SelectKernel()
    {
        if (SimdCapabilities.Avx512FVL)
            return KernelKind.Avx512;

        if (SimdCapabilities.Avx2)
            return KernelKind.Avx2;

        if (SimdCapabilities.AdvSimd)
            return KernelKind.AdvSimd;

        return SimdCapabilities.Ssse3 ? KernelKind.Ssse3 : KernelKind.Scalar;
    }

    /// <summary>
    /// Returns a value indicating whether the processor can run the specified kernel, whatever the switch allows.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <returns><see langword="true" /> if the kernel can run on this processor.</returns>
    internal static bool IsSupported(KernelKind kernel) => kernel switch
    {
        KernelKind.Auto or KernelKind.Scalar => true,
        KernelKind.Ssse3 => System.Runtime.Intrinsics.X86.Ssse3.IsSupported,
        KernelKind.AdvSimd => System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported,
        KernelKind.Avx2 => System.Runtime.Intrinsics.X86.Avx2.IsSupported,
        KernelKind.Avx512 => System.Runtime.Intrinsics.X86.Avx2.IsSupported && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported,
        _ => false,
    };

    /// <summary>
    /// Returns the number of blocks a run of the specified kernel takes at once, given how many remain: the widest it
    /// offers that fits, down to one for the scalar rounds.
    /// </summary>
    /// <param name="kernel">The kernel; not <see cref="KernelKind.Auto" />.</param>
    /// <param name="remaining">The number of blocks left.</param>
    /// <returns>8, 4 or 1.</returns>
    internal static int LanesFor(KernelKind kernel, int remaining) => kernel switch
    {
        KernelKind.Avx512 or KernelKind.Avx2 when remaining >= 8 => 8,
        KernelKind.Avx512 or KernelKind.Avx2 or KernelKind.Ssse3 or KernelKind.AdvSimd when remaining >= 4 => 4,
        _ => 1,
    };

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
    /// Encrypts or decrypts a run of blocks with the specified kernel: runs of the widest vectors it offers first, then
    /// narrower runs, then single blocks.
    /// </summary>
    /// <param name="kernel">The kernel, or <see cref="KernelKind.Auto" /> for the one dispatch selects.</param>
    /// <param name="encrypt"><see langword="true" /> to encrypt; <see langword="false" /> to decrypt.</param>
    /// <param name="roundKeys">The 132 round-key words.</param>
    /// <param name="input">The input, a whole number of blocks.</param>
    /// <param name="output">The destination, at least as long as <paramref name="input" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="roundKeys" /> holds fewer than 132 words, or <paramref name="output" /> is shorter than
    /// <paramref name="input" />.
    /// </exception>
    /// <exception cref="ArgumentException">The length of <paramref name="input" /> is not a multiple of 16.</exception>
    private static void TransformBlocks(KernelKind kernel, bool encrypt, ReadOnlySpan<uint> roundKeys, ReadOnlySpan<byte> input, Span<byte> output)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roundKeys.Length, RoundKeyWords, nameof(roundKeys));
        if (input.Length % BlockBytes != 0) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_InputLengthBlockMultiple, BlockBytes), nameof(input));
        ArgumentOutOfRangeException.ThrowIfLessThan(output.Length, input.Length, nameof(output));

        if (kernel == KernelKind.Auto)
            kernel = SelectKernel();

        ref uint keys = ref MemoryMarshal.GetReference(roundKeys);
        int blocks = input.Length / BlockBytes;
        int done = 0;

        while (done < blocks)
        {
            int remaining = blocks - done;
            int lanes = LanesFor(kernel, remaining);
            int groups = remaining / lanes;
            int offset = done * BlockBytes;
            ref byte source = ref Unsafe.Add(ref MemoryMarshal.GetReference(input), offset);
            ref byte destination = ref Unsafe.Add(ref MemoryMarshal.GetReference(output), offset);

            switch (kernel)
            {
                case KernelKind.Avx512 when lanes == 8 && encrypt:
                    Vector256Kernel<VectorRotation.Avx512>.EncryptBlocks(ref keys, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx512 when lanes == 8:
                    Vector256Kernel<VectorRotation.Avx512>.DecryptBlocks(ref keys, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx2 when lanes == 8 && encrypt:
                    Vector256Kernel<VectorRotation.Avx2>.EncryptBlocks(ref keys, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx2 when lanes == 8:
                    Vector256Kernel<VectorRotation.Avx2>.DecryptBlocks(ref keys, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx512 when lanes == 4 && encrypt:
                    Vector128Kernel<VectorRotation.Avx512>.EncryptBlocks(ref keys, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx512 when lanes == 4:
                    Vector128Kernel<VectorRotation.Avx512>.DecryptBlocks(ref keys, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx2 or KernelKind.Ssse3 when lanes == 4 && encrypt:
                    Vector128Kernel<VectorRotation.Ssse3>.EncryptBlocks(ref keys, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx2 or KernelKind.Ssse3 when lanes == 4:
                    Vector128Kernel<VectorRotation.Ssse3>.DecryptBlocks(ref keys, ref source, ref destination, groups);
                    break;

                case KernelKind.AdvSimd when lanes == 4 && encrypt:
                    Vector128Kernel<VectorRotation.AdvSimd>.EncryptBlocks(ref keys, ref source, ref destination, groups);
                    break;

                case KernelKind.AdvSimd when lanes == 4:
                    Vector128Kernel<VectorRotation.AdvSimd>.DecryptBlocks(ref keys, ref source, ref destination, groups);
                    break;

                default:
                    for (int block = 0; block < groups; block++)
                    {
                        int at = offset + (block * BlockBytes);
                        if (encrypt)
                            EncryptBlock(roundKeys, input.Slice(at, BlockBytes), output.Slice(at, BlockBytes));
                        else
                            DecryptBlock(roundKeys, input.Slice(at, BlockBytes), output.Slice(at, BlockBytes));
                    }

                    break;
            }

            done += groups * lanes;
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
