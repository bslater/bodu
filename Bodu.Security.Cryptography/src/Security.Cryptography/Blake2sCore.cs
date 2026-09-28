// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the BLAKE2s compression function <c>F</c> (RFC 7693, Section 3.2) for <see cref="Blake2s" />.
/// </summary>
/// <remarks>
/// <para>
/// Each kernel compresses one 64-byte block into the eight-word chaining state. Dispatch picks the widest kernel the
/// processor supports and the process allows; every kernel produces the same state.
/// </para>
/// <para>
/// The kernels read the message words straight from the block and keep the working vector in registers, so they leave
/// no copy of the message or of the working vector in memory of their own. Values the JIT spills to its own stack slots
/// are beyond the library's reach.
/// </para>
/// </remarks>
internal static partial class Blake2sCore
{
    /// <summary>The number of bytes in a BLAKE2s block.</summary>
    internal const int BlockBytes = 64;

    /// <summary>The number of 32-bit words in the BLAKE2s chaining state.</summary>
    internal const int StateWords = 8;

    /// <summary>The first word of the BLAKE2s initialization vector (the SHA-256 IV, RFC 7693, Section 2.6).</summary>
    private const uint Iv0 = 0x6A09E667U;

    /// <summary>The second word of the BLAKE2s initialization vector.</summary>
    private const uint Iv1 = 0xBB67AE85U;

    /// <summary>The third word of the BLAKE2s initialization vector.</summary>
    private const uint Iv2 = 0x3C6EF372U;

    /// <summary>The fourth word of the BLAKE2s initialization vector.</summary>
    private const uint Iv3 = 0xA54FF53AU;

    /// <summary>The fifth word of the BLAKE2s initialization vector.</summary>
    private const uint Iv4 = 0x510E527FU;

    /// <summary>The sixth word of the BLAKE2s initialization vector.</summary>
    private const uint Iv5 = 0x9B05688CU;

    /// <summary>The seventh word of the BLAKE2s initialization vector.</summary>
    private const uint Iv6 = 0x1F83D9ABU;

    /// <summary>The eighth word of the BLAKE2s initialization vector.</summary>
    private const uint Iv7 = 0x5BE0CD19U;

    /// <summary>
    /// Gets the BLAKE2s initialization vector: the eight words the chaining state starts from before the parameter
    /// block is folded in.
    /// </summary>
    internal static ReadOnlySpan<uint> InitializationVector => [Iv0, Iv1, Iv2, Iv3, Iv4, Iv5, Iv6, Iv7];

    /// <summary>
    /// Compresses one block into the chaining state with the kernel dispatch selects.
    /// </summary>
    /// <param name="state">The eight-word chaining state, updated in place.</param>
    /// <param name="block">The 64-byte block.</param>
    /// <param name="counter">The number of message bytes compressed so far, this block included.</param>
    /// <param name="last"><see langword="true" /> for the final block, which sets the finalization flag.</param>
    internal static void Compress(Span<uint> state, ReadOnlySpan<byte> block, ulong counter, bool last) =>
        Compress(SelectKernel(), state, block, counter, last);

    /// <summary>
    /// Compresses one block into the chaining state with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="state">The eight-word chaining state, updated in place.</param>
    /// <param name="block">The 64-byte block.</param>
    /// <param name="counter">The number of message bytes compressed so far, this block included.</param>
    /// <param name="last"><see langword="true" /> for the final block, which sets the finalization flag.</param>
    internal static void Compress(KernelKind kernel, Span<uint> state, ReadOnlySpan<byte> block, ulong counter, bool last)
    {
        ref uint h = ref MemoryMarshal.GetReference(state);
        ref byte m = ref MemoryMarshal.GetReference(block);
        uint finalization = last ? uint.MaxValue : 0;

        switch (kernel == KernelKind.Auto ? SelectKernel() : kernel)
        {
            default:
                CompressScalar(ref h, ref m, counter, finalization);
                break;
        }
    }

    /// <summary>
    /// Selects the widest kernel the processor supports and the process allows.
    /// </summary>
    /// <returns>The kernel dispatch runs; never <see cref="KernelKind.Auto" />.</returns>
    /// <remarks>
    /// Every gate honors the <see cref="SimdCapabilities.DisableSimdSwitchName" /> switch, which pins the scalar
    /// kernel.
    /// </remarks>
    internal static KernelKind SelectKernel() =>
        KernelKind.Scalar;

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
        _ => false,
    };
}
