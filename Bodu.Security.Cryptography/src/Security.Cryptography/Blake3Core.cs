// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Core.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the BLAKE3 compression function for <see cref="Blake3" />: one block at a time, and whole chunks, parents
/// and subtrees many at once.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Compress(Span{uint}, ReadOnlySpan{byte}, ulong, uint, uint)" /> compresses one 64-byte block into an
/// eight-word chaining value. <see cref="CompressChunks" /> and <see cref="CompressParents" /> compress many inputs at
/// once, one to a lane: sixteen on the 512-bit kernel over AVX-512F, eight on the 256-bit kernel over AVX2 or
/// AVX-512VL, and four on the 128-bit kernel over SSSE3 or AdvSimd.
/// <see cref="CompressSubtree(ReadOnlySpan{byte}, ReadOnlySpan{uint}, ulong, uint, Span{uint})" /> builds a complete
/// subtree from both. Dispatch picks the widest kernel the processor supports and the process allows; every kernel
/// produces the same chaining values.
/// </para>
/// <para>
/// The one-block kernels read the message words straight from the block and keep the working vector in registers, so
/// they leave no copy of the message or of the working vector in memory of their own. The many-input kernels hold a
/// transposed copy of the current blocks, and the subtree code the chaining values of up to 64 chunks, on the stack,
/// clearing both before they return. Values the JIT spills to its own stack slots are beyond the library's reach.
/// </para>
/// <para>
/// Every kernel forbids inlining, so it is compiled on its own with its own inlining budget. Under .NET 8's dynamic PGO
/// the dispatcher otherwise inlined whichever kernel it found hot, ran out of budget inside it, and left the kernel's
/// own helpers as calls.
/// </para>
/// </remarks>
[SkipLocalsInit]
internal static partial class Blake3Core
{
    /// <summary>The number of bytes in a BLAKE3 block.</summary>
    internal const int BlockBytes = 64;

    /// <summary>The number of bytes in a BLAKE3 chunk, the leaf of the hash tree.</summary>
    internal const int ChunkBytes = 1024;

    /// <summary>The number of 32-bit words in a chaining value, and in a key.</summary>
    internal const int ChainingValueWords = 8;

    /// <summary>The number of bytes in an encoded chaining value.</summary>
    internal const int ChainingValueBytes = ChainingValueWords * sizeof(uint);

    /// <summary>The flag on the first block of every chunk.</summary>
    internal const uint ChunkStart = 1u << 0;

    /// <summary>The flag on the last block of every chunk.</summary>
    internal const uint ChunkEnd = 1u << 1;

    /// <summary>The flag on every parent node.</summary>
    internal const uint Parent = 1u << 2;

    /// <summary>The flag on the root node, whose output is the hash.</summary>
    internal const uint Root = 1u << 3;

    /// <summary>The flag on every node of the keyed hash mode.</summary>
    internal const uint KeyedHash = 1u << 4;

    /// <summary>The flag on every node that hashes the context string in the key derivation mode.</summary>
    internal const uint DeriveKeyContext = 1u << 5;

    /// <summary>The flag on every node that hashes the key material in the key derivation mode.</summary>
    internal const uint DeriveKeyMaterial = 1u << 6;

    /// <summary>The first word of the BLAKE3 initialization vector (the SHA-256 IV).</summary>
    private const uint Iv0 = 0x6A09E667U;

    /// <summary>The second word of the BLAKE3 initialization vector.</summary>
    private const uint Iv1 = 0xBB67AE85U;

    /// <summary>The third word of the BLAKE3 initialization vector.</summary>
    private const uint Iv2 = 0x3C6EF372U;

    /// <summary>The fourth word of the BLAKE3 initialization vector.</summary>
    private const uint Iv3 = 0xA54FF53AU;

    /// <summary>The fifth word of the BLAKE3 initialization vector.</summary>
    private const uint Iv4 = 0x510E527FU;

    /// <summary>The sixth word of the BLAKE3 initialization vector.</summary>
    private const uint Iv5 = 0x9B05688CU;

    /// <summary>The seventh word of the BLAKE3 initialization vector.</summary>
    private const uint Iv6 = 0x1F83D9ABU;

    /// <summary>The eighth word of the BLAKE3 initialization vector.</summary>
    private const uint Iv7 = 0x5BE0CD19U;

    /// <summary>
    /// Gets the message-word indices each of the seven rounds reads, sixteen per round: round <c>r</c> reads the block
    /// as permuted <c>r</c> times by the BLAKE3 message permutation.
    /// </summary>
    /// <remarks>
    /// The scalar kernel writes its rounds out with these indices as constants; the 128-bit kernel reads them from
    /// here.
    /// </remarks>
    internal static ReadOnlySpan<byte> MessageSchedule =>
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
        2, 6, 3, 10, 7, 0, 4, 13, 1, 11, 12, 5, 9, 14, 15, 8,
        3, 4, 10, 12, 13, 2, 7, 14, 6, 5, 9, 0, 11, 15, 8, 1,
        10, 7, 12, 9, 14, 3, 13, 15, 4, 0, 11, 2, 5, 8, 1, 6,
        12, 13, 9, 11, 15, 10, 14, 8, 7, 2, 5, 3, 0, 1, 6, 4,
        9, 14, 11, 5, 8, 12, 15, 1, 13, 3, 0, 10, 2, 6, 4, 7,
        11, 15, 5, 0, 1, 9, 8, 6, 14, 10, 2, 12, 3, 4, 7, 13,
    ];

    /// <summary>
    /// Gets the BLAKE3 initialization vector: the key of the unkeyed hash, and the third row of every compression's
    /// working vector.
    /// </summary>
    internal static ReadOnlySpan<uint> InitializationVector => [Iv0, Iv1, Iv2, Iv3, Iv4, Iv5, Iv6, Iv7];

    /// <summary>
    /// Compresses one block into a chaining value with the kernel dispatch selects.
    /// </summary>
    /// <param name="chainingValue">
    /// The eight-word chaining value, replaced in place by the compression's output.
    /// </param>
    /// <param name="block">The 64-byte block, zero-padded past <paramref name="blockLength" />.</param>
    /// <param name="counter">The chunk counter; zero for a parent node.</param>
    /// <param name="blockLength">The number of message bytes in the block.</param>
    /// <param name="flags">The domain-separation flags.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="chainingValue" /> holds fewer than eight words, or <paramref name="block" /> fewer than 64
    /// bytes.
    /// </exception>
    internal static void Compress(Span<uint> chainingValue, ReadOnlySpan<byte> block, ulong counter, uint blockLength, uint flags) =>
        Compress(SelectKernel(), chainingValue, block, counter, blockLength, flags);

    /// <summary>
    /// Compresses one block into a chaining value with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="chainingValue">
    /// The eight-word chaining value, replaced in place by the compression's output.
    /// </param>
    /// <param name="block">The 64-byte block, zero-padded past <paramref name="blockLength" />.</param>
    /// <param name="counter">The chunk counter; zero for a parent node.</param>
    /// <param name="blockLength">The number of message bytes in the block.</param>
    /// <param name="flags">The domain-separation flags.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="chainingValue" /> holds fewer than eight words, or <paramref name="block" /> fewer than 64
    /// bytes.
    /// </exception>
    /// <remarks>
    /// The output is the first half of the compression's output, the only half a 256-bit hash reads: the next chaining
    /// value, or at the root the digest.
    /// </remarks>
    internal static void Compress(KernelKind kernel, Span<uint> chainingValue, ReadOnlySpan<byte> block, ulong counter, uint blockLength, uint flags)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(chainingValue.Length, ChainingValueWords, nameof(chainingValue));
        ArgumentOutOfRangeException.ThrowIfLessThan(block.Length, BlockBytes, nameof(block));

        ref uint cv = ref MemoryMarshal.GetReference(chainingValue);
        ref byte m = ref MemoryMarshal.GetReference(block);

        switch (kernel == KernelKind.Auto ? SelectKernel() : kernel)
        {
            case KernelKind.Avx512:
            case KernelKind.Avx512Wide:
                Vector128Kernel<Blake2sCore.Avx512Isa>.Compress(ref cv, ref m, counter, blockLength, flags);
                break;

            case KernelKind.AdvSimd:
                Vector128Kernel<Blake2sCore.AdvSimdIsa>.Compress(ref cv, ref m, counter, blockLength, flags);
                break;

            case KernelKind.Avx2:
            case KernelKind.Ssse3:
                Vector128Kernel<Blake2sCore.Ssse3Isa>.Compress(ref cv, ref m, counter, blockLength, flags);
                break;

            default:
                CompressScalar(ref cv, ref m, counter, blockLength, flags);
                break;
        }
    }

    /// <summary>
    /// Selects the widest kernel the processor supports and the process allows: AVX-512, then AVX2, then AdvSimd on
    /// ARM64, then SSSE3, then the scalar kernel.
    /// </summary>
    /// <returns>The kernel dispatch runs; never <see cref="KernelKind.Auto" />.</returns>
    /// <remarks>
    /// <para>
    /// Every gate honors the <see cref="SimdCapabilities.DisableSimdSwitchName" /> switch, which pins the scalar
    /// kernel.
    /// </para>
    /// <para>
    /// AVX-512 takes the sixteen-way kernel only where <see cref="Vector512.IsHardwareAccelerated" /> holds. The
    /// runtime clears it on processors whose clock drops under sustained 512-bit work, so there the eight-way kernel
    /// runs instead; <c>DOTNET_PreferredVectorBitWidth=512</c> opts such a processor in.
    /// </para>
    /// </remarks>
    internal static KernelKind SelectKernel()
    {
        if (SimdCapabilities.Avx512FVL)
            return Vector512.IsHardwareAccelerated ? KernelKind.Avx512Wide : KernelKind.Avx512;

        if (SimdCapabilities.Avx2)
            return KernelKind.Avx2;

        if (SimdCapabilities.AdvSimd)
            return KernelKind.AdvSimd;

        return SimdCapabilities.Ssse3 ? KernelKind.Ssse3 : KernelKind.Scalar;
    }

    /// <summary>
    /// Determines whether the processor can run the specified kernel, whether or not the process allows vector code.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <returns>
    /// <see langword="true" /> if the processor supports every instruction the kernel uses; otherwise,
    /// <see langword="false" />.
    /// </returns>
    internal static bool IsSupported(KernelKind kernel) => kernel switch
    {
        KernelKind.Auto or KernelKind.Scalar => true,
        KernelKind.Ssse3 => System.Runtime.Intrinsics.X86.Ssse3.IsSupported,
        KernelKind.AdvSimd => System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported,
        KernelKind.Avx2 => System.Runtime.Intrinsics.X86.Avx2.IsSupported,
        KernelKind.Avx512 or KernelKind.Avx512Wide => System.Runtime.Intrinsics.X86.Avx2.IsSupported && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported,
        _ => false,
    };

    /// <summary>
    /// Encodes a chaining value as the 32 little-endian bytes BLAKE3 defines, the form a parent node's block and the
    /// digest take.
    /// </summary>
    /// <param name="chainingValue">The eight-word chaining value.</param>
    /// <param name="destination">The 32 bytes that receive the encoding.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="chainingValue" /> holds fewer than eight words, or <paramref name="destination" /> fewer than 32
    /// bytes.
    /// </exception>
    internal static void StoreChainingValue(ReadOnlySpan<uint> chainingValue, Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(chainingValue.Length, ChainingValueWords, nameof(chainingValue));
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, ChainingValueBytes, nameof(destination));

        if (BitConverter.IsLittleEndian)
        {
            MemoryMarshal.AsBytes(chainingValue[..ChainingValueWords]).CopyTo(destination);
            return;
        }

        for (int i = 0; i < ChainingValueWords; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(destination[(i * sizeof(uint))..], chainingValue[i]);
    }

    /// <summary>
    /// Decodes a chaining value from the 32 little-endian bytes BLAKE3 defines.
    /// </summary>
    /// <param name="source">The 32-byte encoding.</param>
    /// <param name="chainingValue">The eight words that receive the chaining value.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="source" /> holds fewer than 32 bytes, or <paramref name="chainingValue" /> fewer than eight
    /// words.
    /// </exception>
    internal static void LoadChainingValue(ReadOnlySpan<byte> source, Span<uint> chainingValue)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(source.Length, ChainingValueBytes, nameof(source));
        ArgumentOutOfRangeException.ThrowIfLessThan(chainingValue.Length, ChainingValueWords, nameof(chainingValue));

        for (int i = 0; i < ChainingValueWords; i++)
            chainingValue[i] = BinaryPrimitives.ReadUInt32LittleEndian(source[(i * sizeof(uint))..]);
    }
}
