// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the Argon2 memory-hard function defined by RFC 9106 - the pre-hashing digest, the slicewise memory fill
/// with per-variant reference indexing, the compression function <c>G</c>, and the finalization - shared by the
/// <see cref="Argon2d" />, <see cref="Argon2i" />, and <see cref="Argon2id" /> public types.
/// </summary>
/// <remarks>
/// <para>
/// The fill is generic over an <see cref="IArgon2Kernel" />, the compression function for one instruction set, so each
/// kernel runs in a loop the JIT specializes for it. Every kernel carries the previous block across a segment, so a
/// block reads only its reference block and, on passes that XOR, its own previous contents.
/// </para>
/// <para>
/// Every buffer that holds a password-derived word - the matrix, H0, the per-segment scratch, and the buffers used to
/// build the first and last blocks - is cleared before it is released. Values the JIT keeps in registers or spills to
/// its own stack slots are beyond the library's reach.
/// </para>
/// </remarks>
[SkipLocalsInit]
internal static partial class Argon2Core
{
    /// <summary>The number of 64-bit words in a 1024-byte memory block.</summary>
    private const int WordsPerBlock = Argon2Matrix.WordsPerBlock;

    /// <summary>The number of bytes in a memory block.</summary>
    private const int BlockSizeBytes = WordsPerBlock * sizeof(ulong);

    /// <summary>The number of vertical slices (synchronization points) each lane is divided into.</summary>
    private const int SyncPoints = 4;

    /// <summary>The number of (J1, J2) address pairs produced by a single Argon2i address block.</summary>
    private const int AddressesPerBlock = WordsPerBlock;

    /// <summary>
    /// Derives an Argon2 tag into <paramref name="tag" /> from the supplied inputs and parameters.
    /// </summary>
    /// <param name="type">The Argon2 variant selecting the reference-indexing strategy.</param>
    /// <param name="parameters">The validated cost and auxiliary parameters.</param>
    /// <param name="password">The password / message <c>P</c>.</param>
    /// <param name="salt">The salt / nonce <c>S</c>.</param>
    /// <param name="tag">The destination buffer; its length must equal <c>parameters.TagLength</c>.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of threads the derivation may use, the calling thread included; <c>-1</c> lets the library
    /// choose.
    /// </param>
    /// <exception cref="OutOfMemoryException">The memory matrix cannot be allocated.</exception>
    internal static void DeriveTag(
        Argon2Type type,
        Argon2Parameters parameters,
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        Span<byte> tag,
        int maxDegreeOfParallelism) =>
        DeriveTag(type, parameters, password, salt, tag, new FillOptions(maxDegreeOfParallelism));

    /// <summary>
    /// Derives an Argon2 tag into <paramref name="tag" />, filling the matrix as <paramref name="options" /> describe.
    /// </summary>
    /// <param name="type">The Argon2 variant selecting the reference-indexing strategy.</param>
    /// <param name="parameters">The validated cost and auxiliary parameters.</param>
    /// <param name="password">The password / message <c>P</c>.</param>
    /// <param name="salt">The salt / nonce <c>S</c>.</param>
    /// <param name="tag">The destination buffer; its length must equal <c>parameters.TagLength</c>.</param>
    /// <param name="options">How the matrix is filled.</param>
    /// <exception cref="OutOfMemoryException">The memory matrix cannot be allocated.</exception>
    internal static void DeriveTag(
        Argon2Type type,
        Argon2Parameters parameters,
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        Span<byte> tag,
        FillOptions options)
    {
        var geometry = new Geometry(type, parameters);

        using Argon2Matrix matrix = Argon2Matrix.Rent(geometry.MemoryBlocks);
        DeriveTag(geometry, parameters, password, salt, tag, matrix, options);
    }

    /// <summary>
    /// Derives an Argon2 tag into <paramref name="tag" /> using a caller-supplied memory matrix.
    /// </summary>
    /// <param name="type">The Argon2 variant selecting the reference-indexing strategy.</param>
    /// <param name="parameters">The validated cost and auxiliary parameters.</param>
    /// <param name="password">The password / message <c>P</c>.</param>
    /// <param name="salt">The salt / nonce <c>S</c>.</param>
    /// <param name="tag">The destination buffer; its length must equal <c>parameters.TagLength</c>.</param>
    /// <param name="matrix">The matrix to fill; at least <c>m'</c> blocks, whatever their contents.</param>
    /// <param name="options">How the matrix is filled.</param>
    /// <remarks>
    /// The derivation writes every block it reads, so the matrix's prior contents cannot affect the tag; the tests
    /// prove that by lending a matrix filled with garbage. The caller keeps ownership and disposes the matrix.
    /// </remarks>
    internal static void DeriveTag(
        Argon2Type type,
        Argon2Parameters parameters,
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        Span<byte> tag,
        Argon2Matrix matrix,
        FillOptions options) =>
        DeriveTag(new Geometry(type, parameters), parameters, password, salt, tag, matrix, options);

    /// <summary>
    /// Derives an Argon2 tag into <paramref name="tag" />: H0, the first two columns, the fill, and the finalization.
    /// </summary>
    /// <param name="geometry">The shape of the matrix, including the variant.</param>
    /// <param name="parameters">The validated cost and auxiliary parameters.</param>
    /// <param name="password">The password / message <c>P</c>.</param>
    /// <param name="salt">The salt / nonce <c>S</c>.</param>
    /// <param name="tag">The destination buffer; its length must equal <c>parameters.TagLength</c>.</param>
    /// <param name="matrix">The matrix to fill; at least <c>m'</c> blocks.</param>
    /// <param name="options">How the matrix is filled.</param>
    private static void DeriveTag(
        in Geometry geometry,
        Argon2Parameters parameters,
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        Span<byte> tag,
        Argon2Matrix matrix,
        FillOptions options)
    {
        Span<byte> h0 = stackalloc byte[Argon2Blake2b.MaxDigestBytes];

        try
        {
            ComputeH0(geometry.Type, parameters, password, salt, h0);
            InitializeBlocks(matrix, h0, geometry);
            FillMemory(matrix, geometry, options.ResolveKernel(), options.ResolveWorkers(geometry.Lanes, geometry.SegmentLength));
            Finalize(matrix, geometry, tag);
        }
        finally
        {
            CryptographyHelper.Clear(h0);
        }
    }

    /// <summary>
    /// Selects the compression kernel dispatch runs: on x64 the widest the processor supports and the process allows,
    /// AVX2, then SSSE3; on ARM64 the AdvSimd kernel on Apple's cores under .NET 8, and the scalar kernel on other
    /// ARM64 processors and under .NET 10; and the scalar kernel everywhere else.
    /// </summary>
    /// <returns>The kernel dispatch runs; never <see cref="KernelKind.Auto" />.</returns>
    /// <remarks>
    /// Every gate honors the <see cref="SimdCapabilities.DisableSimdSwitchName" /> switch, which pins the scalar
    /// kernel.
    /// </remarks>
    internal static KernelKind SelectKernel() =>
        SelectKernel(SimdCapabilities.AppleSilicon);

    /// <summary>
    /// Selects the compression kernel as <see cref="SelectKernel()" /> does, on the specified platform.
    /// </summary>
    /// <param name="appleSilicon">
    /// Whether to make the choices for Apple's cores, as <see cref="SimdCapabilities.AppleSilicon" /> reports them.
    /// </param>
    /// <returns>The kernel dispatch runs; never <see cref="KernelKind.Auto" />.</returns>
    internal static KernelKind SelectKernel(bool appleSilicon)
    {
        if (SimdCapabilities.Avx2)
            return KernelKind.Avx2;

        if (IsAdvSimdSelected(appleSilicon))
            return KernelKind.AdvSimd;

        return SimdCapabilities.Ssse3 ? KernelKind.Ssse3 : KernelKind.Scalar;
    }

    /// <summary>
    /// Determines whether dispatch selects the AdvSimd kernel: under .NET 8 on Apple's cores, wherever
    /// <see cref="SimdCapabilities.AdvSimd" /> allows it, and under .NET 10 only while
    /// <see cref="SimdCapabilities.AdvSimdSingleState" />, which is closed, allows it.
    /// </summary>
    /// <param name="appleSilicon">
    /// Whether to make the choice for Apple's cores, as <see cref="SimdCapabilities.AppleSilicon" /> reports them.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if dispatch selects the AdvSimd kernel; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Under .NET 8 the AdvSimd kernel ran 1.3 to 1.6 times as fast as the scalar kernel on an Apple M1, and took 36 to
    /// 48 percent of 1.0.0's CPU where the scalar kernel took 58 to 67 percent; on a Neoverse N2 it ran at 0.77 to 0.80
    /// of the scalar kernel's speed. Under .NET 10, whose scalar kernel is faster, the scalar kernel ran 1.25 times as
    /// fast as the AdvSimd kernel on the N2 and about as fast on the M1.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAdvSimdSelected(bool appleSilicon)
    {
#if NET10_0_OR_GREATER
        return SimdCapabilities.AdvSimdSingleState;
#else
        return SimdCapabilities.AdvSimd && appleSilicon;
#endif
    }

    /// <summary>
    /// Determines whether the processor can run the specified compression kernel, whether or not the process allows
    /// vector code.
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
        _ => false,
    };

    /// <summary>
    /// Fills every block after the first two columns with the specified compression kernel.
    /// </summary>
    /// <param name="matrix">The memory matrix, its first two columns already filled.</param>
    /// <param name="geometry">The shape of the matrix.</param>
    /// <param name="kernel">The compression kernel; not <see cref="KernelKind.Auto" />.</param>
    /// <param name="workers">The number of threads that fill each slice, the calling thread included.</param>
    /// <remarks>
    /// The kernel is chosen once per derivation; each branch runs a fill the JIT specializes for its kernel, so no
    /// block pays for the choice.
    /// </remarks>
    private static void FillMemory(Argon2Matrix matrix, in Geometry geometry, KernelKind kernel, int workers)
    {
        switch (kernel)
        {
            case KernelKind.Avx2:
                FillMemory<Avx2Kernel>(matrix, geometry, workers);
                break;

            case KernelKind.AdvSimd:
                FillMemory<Vector128Kernel<AdvSimdIsa>>(matrix, geometry, workers);
                break;

            case KernelKind.Ssse3:
                FillMemory<Vector128Kernel<Ssse3Isa>>(matrix, geometry, workers);
                break;

            default:
                FillMemory<ScalarKernel>(matrix, geometry, workers);
                break;
        }
    }

    /// <summary>
    /// Fills every block after the first two columns: pass by pass, and slice by slice (RFC 9106, Section 3.4).
    /// </summary>
    /// <typeparam name="TKernel">The compression kernel.</typeparam>
    /// <param name="matrix">The memory matrix, its first two columns already filled.</param>
    /// <param name="geometry">The shape of the matrix.</param>
    /// <param name="workers">The number of threads that fill each slice, the calling thread included.</param>
    private static void FillMemory<TKernel>(Argon2Matrix matrix, in Geometry geometry, int workers)
        where TKernel : struct, IArgon2Kernel
    {
        if (workers > 1)
        {
            FillMemoryInParallel<TKernel>(matrix, geometry, workers);
            return;
        }

        for (int pass = 0; pass < geometry.Passes; pass++)
        {
            for (int slice = 0; slice < SyncPoints; slice++)
            {
                for (int lane = 0; lane < geometry.Lanes; lane++)
                    FillSegment<TKernel>(matrix, geometry, pass, slice, lane);
            }
        }
    }

    /// <summary>
    /// Fills every block after the first two columns, dividing the segments of each slice among threads.
    /// </summary>
    /// <typeparam name="TKernel">The compression kernel.</typeparam>
    /// <param name="matrix">The memory matrix, its first two columns already filled.</param>
    /// <param name="geometry">The shape of the matrix.</param>
    /// <param name="workers">The number of threads that fill each slice, the calling thread included.</param>
    /// <remarks>
    /// <para>
    /// Each slice is a fork over its lanes and a join. The join is RFC 9106's synchronization point: no segment reads
    /// another lane's block from an unfinished slice, and completing the loop publishes the slice's blocks to the next
    /// slice's readers on every memory model.
    /// </para>
    /// <para>
    /// The calling thread fills lanes too, and fills them all if no worker arrives, so a starved thread pool slows a
    /// derivation down but never stalls it. The default scheduler is named explicitly so a scheduler the caller runs
    /// under is never used. A fault in a segment surfaces as itself rather than wrapped in an
    /// <see cref="AggregateException" />, and only after every worker has stopped, so the matrix is never released
    /// while a worker could still write to it.
    /// </para>
    /// </remarks>
    private static void FillMemoryInParallel<TKernel>(Argon2Matrix matrix, Geometry geometry, int workers)
        where TKernel : struct, IArgon2Kernel
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = workers, TaskScheduler = TaskScheduler.Default };

        for (int pass = 0; pass < geometry.Passes; pass++)
        {
            for (int slice = 0; slice < SyncPoints; slice++)
            {
                int currentPass = pass;
                int currentSlice = slice;

                try
                {
                    Parallel.For(0, geometry.Lanes, options, lane => FillSegment<TKernel>(matrix, geometry, currentPass, currentSlice, lane));
                }
                catch (AggregateException ex) when (ex.InnerExceptions.Count > 0)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerExceptions[0]).Throw();
                }
            }
        }
    }

    /// <summary>
    /// Computes the 64-byte pre-hashing digest <c>H0</c> (RFC 9106, Figure 1).
    /// </summary>
    /// <param name="type">The Argon2 variant being computed.</param>
    /// <param name="p">The Argon2 parameters describing cost and auxiliary inputs.</param>
    /// <param name="password">The password bytes.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <param name="h0">The destination span that receives the 64-byte <c>H0</c> digest.</param>
    /// <remarks>
    /// The fields are hashed as they are appended, so the password is never copied into a buffer of its own.
    /// </remarks>
    private static void ComputeH0(
        Argon2Type type,
        Argon2Parameters p,
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        Span<byte> h0)
    {
        ReadOnlySpan<byte> secret = p.Secret ?? ReadOnlySpan<byte>.Empty;
        ReadOnlySpan<byte> associatedData = p.AssociatedData ?? ReadOnlySpan<byte>.Empty;

        Span<ulong> state = stackalloc ulong[Argon2Blake2b.StateWords];
        Span<byte> block = stackalloc byte[Argon2Blake2b.BlockSizeBytes];

        var hasher = new Argon2Blake2b.Hasher(state, block, Argon2Blake2b.MaxDigestBytes);
        hasher.AppendLittleEndian(p.Parallelism);
        hasher.AppendLittleEndian(p.TagLength);
        hasher.AppendLittleEndian(p.MemoryKiB);
        hasher.AppendLittleEndian(p.Iterations);
        hasher.AppendLittleEndian(p.Version);
        hasher.AppendLittleEndian((int)type);
        AppendLengthPrefixed(ref hasher, password);
        AppendLengthPrefixed(ref hasher, salt);
        AppendLengthPrefixed(ref hasher, secret);
        AppendLengthPrefixed(ref hasher, associatedData);
        hasher.Finish(h0);
    }

    /// <summary>
    /// Appends a 32-bit little-endian length prefix followed by the bytes, as RFC 9106 encodes H0's variable fields.
    /// </summary>
    /// <param name="hasher">The hasher computing H0.</param>
    /// <param name="value">The bytes to append after their length.</param>
    private static void AppendLengthPrefixed(ref Argon2Blake2b.Hasher hasher, ReadOnlySpan<byte> value)
    {
        hasher.AppendLittleEndian(value.Length);
        hasher.Append(value);
    }

    /// <summary>
    /// Fills the first two columns of every lane from <c>H0</c> via the variable-length hash <c>H'</c> (RFC 9106,
    /// Figures 3 and 4).
    /// </summary>
    /// <param name="matrix">The memory matrix.</param>
    /// <param name="h0">The 64-byte pre-hashing digest <c>H0</c>.</param>
    /// <param name="geometry">The shape of the matrix.</param>
    private static void InitializeBlocks(Argon2Matrix matrix, ReadOnlySpan<byte> h0, in Geometry geometry)
    {
        Span<byte> input = stackalloc byte[Argon2Blake2b.MaxDigestBytes + 8];
        Span<byte> block = stackalloc byte[BlockSizeBytes];

        try
        {
            h0.CopyTo(input);

            for (int lane = 0; lane < geometry.Lanes; lane++)
            {
                for (int column = 0; column < 2; column++)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(input.Slice(Argon2Blake2b.MaxDigestBytes, 4), column);
                    BinaryPrimitives.WriteInt32LittleEndian(input.Slice(Argon2Blake2b.MaxDigestBytes + 4, 4), lane);

                    Argon2Blake2b.HashVariableLength(input, block);
                    LoadBlockLE(block, matrix.BlockSpan((lane * geometry.LaneLength) + column));
                }
            }
        }
        finally
        {
            CryptographyHelper.Clear(input);
            CryptographyHelper.Clear(block);
        }
    }

    /// <summary>
    /// Computes the final block as the XOR of the last column across lanes, then emits the tag via <c>H'</c> (RFC 9106,
    /// Figure 7).
    /// </summary>
    /// <param name="matrix">The memory matrix.</param>
    /// <param name="geometry">The shape of the matrix.</param>
    /// <param name="tag">The destination span that receives the final tag.</param>
    private static void Finalize(Argon2Matrix matrix, in Geometry geometry, Span<byte> tag)
    {
        Span<ulong> c = stackalloc ulong[WordsPerBlock];
        Span<byte> cBytes = stackalloc byte[BlockSizeBytes];

        try
        {
            matrix.BlockSpan(geometry.LaneLength - 1).CopyTo(c);

            for (int lane = 1; lane < geometry.Lanes; lane++)
            {
                ReadOnlySpan<ulong> last = matrix.BlockSpan((lane * geometry.LaneLength) + geometry.LaneLength - 1);
                for (int k = 0; k < WordsPerBlock; k++)
                    c[k] ^= last[k];
            }

            StoreBlockLE(c, cBytes);
            Argon2Blake2b.HashVariableLength(cBytes, tag);
        }
        finally
        {
            CryptographyHelper.Clear(c);
            CryptographyHelper.Clear(cBytes);
        }
    }

    /// <summary>
    /// Loads a 1024-byte block into 128 little-endian 64-bit words.
    /// </summary>
    /// <param name="source">The 1024-byte source buffer.</param>
    /// <param name="block">The destination span of 128 64-bit words.</param>
    private static void LoadBlockLE(ReadOnlySpan<byte> source, Span<ulong> block)
    {
        for (int i = 0; i < WordsPerBlock; i++)
            block[i] = BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(i * 8, 8));
    }

    /// <summary>
    /// Serializes 128 64-bit words into a 1024-byte little-endian block.
    /// </summary>
    /// <param name="block">The source span of 128 64-bit words.</param>
    /// <param name="destination">The 1024-byte destination buffer.</param>
    private static void StoreBlockLE(ReadOnlySpan<ulong> block, Span<byte> destination)
    {
        for (int i = 0; i < WordsPerBlock; i++)
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(i * 8, 8), block[i]);
    }
}
