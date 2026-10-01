// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2GatherExperiment.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

// TEMPORARY (issue #743): BLAKE2 message-gather variants, measured side by side on the hosted runners to find what AMD's
// Zen 4 rewards. Every variant is held to the library's digest before it is timed. Remove before merging.
#if !BODU_CRYPTO_BASELINE
#pragma warning disable
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography.Benchmarks.Blake2Experiment;

public interface IBlake2sKernel
{
    static abstract string Name { get; }
    static abstract bool IsSupported { get; }
    static abstract void Compress(ref uint h, ref byte block, ulong counter, uint finalization);
}

public static class B2s
{
    public const uint Iv0 = 0x6A09E667U, Iv1 = 0xBB67AE85U, Iv2 = 0x3C6EF372U, Iv3 = 0xA54FF53AU;
    public const uint Iv4 = 0x510E527FU, Iv5 = 0x9B05688CU, Iv6 = 0x1F83D9ABU, Iv7 = 0x5BE0CD19U;

    public static ReadOnlySpan<byte> Sigma =>
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
        14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3,
        11, 8, 12, 0, 5, 2, 15, 13, 10, 14, 3, 6, 7, 1, 9, 4,
        7, 9, 3, 1, 13, 12, 11, 14, 2, 6, 5, 10, 4, 0, 15, 8,
        9, 0, 5, 7, 2, 4, 10, 15, 14, 1, 11, 12, 6, 8, 3, 13,
        2, 12, 6, 10, 0, 11, 8, 3, 4, 13, 7, 5, 15, 14, 1, 9,
        12, 5, 1, 15, 14, 13, 4, 10, 0, 7, 6, 3, 9, 2, 8, 11,
        13, 11, 7, 14, 12, 1, 3, 9, 5, 0, 15, 4, 8, 6, 2, 10,
        6, 15, 14, 9, 11, 3, 0, 8, 12, 2, 13, 7, 1, 4, 10, 5,
        10, 2, 8, 4, 7, 6, 1, 5, 15, 11, 9, 14, 3, 12, 13, 0,
    ];

    public static readonly byte[][] Sigma100 = BuildJagged();

    private static byte[][] BuildJagged()
    {
        var rows = new byte[10][];
        for (int r = 0; r < 10; r++)
            rows[r] = Sigma.Slice(r * 16, 16).ToArray();
        return rows;
    }

    // Index vectors for vpermt2d over the message held as two Vector256 halves: per round, four vectors (column x,
    // column y, diagonal x, diagonal y), each eight lanes, the low four carrying the schedule's word indices.
    public static readonly uint[] PermuteIndices = BuildPermuteIndices();

    private static uint[] BuildPermuteIndices()
    {
        var table = new uint[10 * 4 * 8];
        int[][] picks = [[0, 2, 4, 6], [1, 3, 5, 7], [8, 10, 12, 14], [9, 11, 13, 15]];
        for (int r = 0; r < 10; r++)
            for (int v = 0; v < 4; v++)
                for (int lane = 0; lane < 4; lane++)
                    table[((r * 4) + v) * 8 + lane] = Sigma[(r * 16) + picks[v][lane]];
        return table;
    }

    public static byte[] Hash<TKernel>(ReadOnlySpan<byte> data)
        where TKernel : IBlake2sKernel
    {
        if (data.Length == 0 || data.Length % 64 != 0) throw new ArgumentException("whole blocks only");
        Span<uint> h = stackalloc uint[8];
        h[0] = Iv0 ^ 0x01010000U ^ 32U; h[1] = Iv1; h[2] = Iv2; h[3] = Iv3; h[4] = Iv4; h[5] = Iv5; h[6] = Iv6; h[7] = Iv7;
        ref uint hRef = ref MemoryMarshal.GetReference(h);
        ref byte d = ref MemoryMarshal.GetReference(data);
        int blocks = data.Length / 64;
        for (int i = 0; i < blocks - 1; i++)
            TKernel.Compress(ref hRef, ref Unsafe.Add(ref d, i * 64), (ulong)(i + 1) * 64, 0);
        TKernel.Compress(ref hRef, ref Unsafe.Add(ref d, (blocks - 1) * 64), (ulong)blocks * 64, uint.MaxValue);
        return MemoryMarshal.AsBytes(h).ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint M(ref byte block, int index) => Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref block, index * sizeof(uint)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> Ror16(Vector128<uint> v) => Avx512F.VL.RotateRight(v, 16);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> Ror12(Vector128<uint> v) => Avx512F.VL.RotateRight(v, 12);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> Ror8(Vector128<uint> v) => Avx512F.VL.RotateRight(v, 8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> Ror7(Vector128<uint> v) => Avx512F.VL.RotateRight(v, 7);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void G(ref Vector128<uint> a, ref Vector128<uint> b, ref Vector128<uint> c, ref Vector128<uint> d, Vector128<uint> x, Vector128<uint> y)
    {
        a += b + x;
        d = Ror16(d ^ a);
        c += d;
        b = Ror12(b ^ c);
        a += b + y;
        d = Ror8(d ^ a);
        c += d;
        b = Ror7(b ^ c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Diagonalize(ref Vector128<uint> b, ref Vector128<uint> c, ref Vector128<uint> d)
    {
        b = Sse2.Shuffle(b, 0b00_11_10_01);
        c = Sse2.Shuffle(c, 0b01_00_11_10);
        d = Sse2.Shuffle(d, 0b10_01_00_11);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Undiagonalize(ref Vector128<uint> b, ref Vector128<uint> c, ref Vector128<uint> d)
    {
        b = Sse2.Shuffle(b, 0b10_01_00_11);
        c = Sse2.Shuffle(c, 0b01_00_11_10);
        d = Sse2.Shuffle(d, 0b00_11_10_01);
    }

    // Gathers four message words with each load folded into its insert: the index scales by four in a native-sized
    // address, and every insert is its own statement, so each load sits immediately before the instruction that reads it.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> LoadFolded(ref uint words, ref byte s, int k0, int k1, int k2, int k3)
    {
        Vector128<uint> v = Vector128.CreateScalarUnsafe(Unsafe.Add(ref words, (nint)Unsafe.Add(ref s, k0)));
        v = Sse41.Insert(v, Unsafe.Add(ref words, (nint)Unsafe.Add(ref s, k1)), 1);
        v = Sse41.Insert(v, Unsafe.Add(ref words, (nint)Unsafe.Add(ref s, k2)), 2);
        return Sse41.Insert(v, Unsafe.Add(ref words, (nint)Unsafe.Add(ref s, k3)), 3);
    }
}

/// <summary>The library's kernel as it ships (Vector128Kernel over Avx512Isa).</summary>
public readonly struct S_Current : IBlake2sKernel
{
    public static string Name => "current";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref uint h, ref byte block, ulong counter, uint finalization)
    {
        Vector128<uint> h0 = Vector128.LoadUnsafe(ref h);
        Vector128<uint> h1 = Vector128.LoadUnsafe(ref h, 4);
        Vector128<uint> a = h0, b = h1;
        Vector128<uint> c = Vector128.Create(B2s.Iv0, B2s.Iv1, B2s.Iv2, B2s.Iv3);
        Vector128<uint> d = Vector128.Create(B2s.Iv4 ^ (uint)counter, B2s.Iv5 ^ (uint)(counter >> 32), B2s.Iv6 ^ finalization, B2s.Iv7);
        ref byte sigma = ref MemoryMarshal.GetReference(B2s.Sigma);
        for (int round = 0; round < 10; round++)
        {
            ref byte s = ref Unsafe.Add(ref sigma, round * 16);
            Round(ref a, ref b, ref c, ref d, Load(ref block, ref s, 0, 2, 4, 6), Load(ref block, ref s, 1, 3, 5, 7), Load(ref block, ref s, 8, 10, 12, 14), Load(ref block, ref s, 9, 11, 13, 15));
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Round(ref Vector128<uint> a, ref Vector128<uint> b, ref Vector128<uint> c, ref Vector128<uint> d, Vector128<uint> cx, Vector128<uint> cy, Vector128<uint> dx, Vector128<uint> dy)
    {
        B2s.G(ref a, ref b, ref c, ref d, cx, cy);
        B2s.Diagonalize(ref b, ref c, ref d);
        B2s.G(ref a, ref b, ref c, ref d, dx, dy);
        B2s.Undiagonalize(ref b, ref c, ref d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<uint> Load(ref byte block, ref byte s, int k0, int k1, int k2, int k3) =>
        Vector128.Create(B2s.M(ref block, Unsafe.Add(ref s, k0)), B2s.M(ref block, Unsafe.Add(ref s, k1)), B2s.M(ref block, Unsafe.Add(ref s, k2)), B2s.M(ref block, Unsafe.Add(ref s, k3)));
}

/// <summary>The current round, with every message load folded into its insert; the four gathers stay at the top.</summary>
public readonly struct S_Folded : IBlake2sKernel
{
    public static string Name => "folded";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref uint h, ref byte block, ulong counter, uint finalization)
    {
        Vector128<uint> h0 = Vector128.LoadUnsafe(ref h);
        Vector128<uint> h1 = Vector128.LoadUnsafe(ref h, 4);
        Vector128<uint> a = h0, b = h1;
        Vector128<uint> c = Vector128.Create(B2s.Iv0, B2s.Iv1, B2s.Iv2, B2s.Iv3);
        Vector128<uint> d = Vector128.Create(B2s.Iv4 ^ (uint)counter, B2s.Iv5 ^ (uint)(counter >> 32), B2s.Iv6 ^ finalization, B2s.Iv7);
        ref uint words = ref Unsafe.As<byte, uint>(ref block);
        ref byte sigma = ref MemoryMarshal.GetReference(B2s.Sigma);
        for (int round = 0; round < 10; round++)
        {
            ref byte s = ref Unsafe.Add(ref sigma, round * 16);
            Vector128<uint> cx = B2s.LoadFolded(ref words, ref s, 0, 2, 4, 6);
            Vector128<uint> cy = B2s.LoadFolded(ref words, ref s, 1, 3, 5, 7);
            Vector128<uint> dx = B2s.LoadFolded(ref words, ref s, 8, 10, 12, 14);
            Vector128<uint> dy = B2s.LoadFolded(ref words, ref s, 9, 11, 13, 15);
            B2s.G(ref a, ref b, ref c, ref d, cx, cy);
            B2s.Diagonalize(ref b, ref c, ref d);
            B2s.G(ref a, ref b, ref c, ref d, dx, dy);
            B2s.Undiagonalize(ref b, ref c, ref d);
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }
}

/// <summary>Folded loads, with the diagonal step's gathers after the column step, as 1.0.0 ordered them.</summary>
public readonly struct S_Interleaved : IBlake2sKernel
{
    public static string Name => "interleaved";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref uint h, ref byte block, ulong counter, uint finalization)
    {
        Vector128<uint> h0 = Vector128.LoadUnsafe(ref h);
        Vector128<uint> h1 = Vector128.LoadUnsafe(ref h, 4);
        Vector128<uint> a = h0, b = h1;
        Vector128<uint> c = Vector128.Create(B2s.Iv0, B2s.Iv1, B2s.Iv2, B2s.Iv3);
        Vector128<uint> d = Vector128.Create(B2s.Iv4 ^ (uint)counter, B2s.Iv5 ^ (uint)(counter >> 32), B2s.Iv6 ^ finalization, B2s.Iv7);
        ref uint words = ref Unsafe.As<byte, uint>(ref block);
        ref byte sigma = ref MemoryMarshal.GetReference(B2s.Sigma);
        for (int round = 0; round < 10; round++)
        {
            ref byte s = ref Unsafe.Add(ref sigma, round * 16);
            Vector128<uint> x = B2s.LoadFolded(ref words, ref s, 0, 2, 4, 6);
            Vector128<uint> y = B2s.LoadFolded(ref words, ref s, 1, 3, 5, 7);
            B2s.G(ref a, ref b, ref c, ref d, x, y);
            B2s.Diagonalize(ref b, ref c, ref d);
            x = B2s.LoadFolded(ref words, ref s, 8, 10, 12, 14);
            y = B2s.LoadFolded(ref words, ref s, 9, 11, 13, 15);
            B2s.G(ref a, ref b, ref c, ref d, x, y);
            B2s.Undiagonalize(ref b, ref c, ref d);
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }
}

/// <summary>Interleaved folded gathers with 1.0.0's variable rotates (vprorvd).</summary>
public readonly struct S_Variable : IBlake2sKernel
{
    private static readonly Vector128<uint> s_r16 = Vector128.Create(16U), s_r12 = Vector128.Create(12U), s_r8 = Vector128.Create(8U), s_r7 = Vector128.Create(7U);

    public static string Name => "variable";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref uint h, ref byte block, ulong counter, uint finalization)
    {
        Vector128<uint> h0 = Vector128.LoadUnsafe(ref h);
        Vector128<uint> h1 = Vector128.LoadUnsafe(ref h, 4);
        Vector128<uint> a = h0, b = h1;
        Vector128<uint> c = Vector128.Create(B2s.Iv0, B2s.Iv1, B2s.Iv2, B2s.Iv3);
        Vector128<uint> d = Vector128.Create(B2s.Iv4 ^ (uint)counter, B2s.Iv5 ^ (uint)(counter >> 32), B2s.Iv6 ^ finalization, B2s.Iv7);
        Vector128<uint> r16 = s_r16, r12 = s_r12, r8 = s_r8, r7 = s_r7;
        ref uint words = ref Unsafe.As<byte, uint>(ref block);
        ref byte sigma = ref MemoryMarshal.GetReference(B2s.Sigma);
        for (int round = 0; round < 10; round++)
        {
            ref byte s = ref Unsafe.Add(ref sigma, round * 16);
            Vector128<uint> x = B2s.LoadFolded(ref words, ref s, 0, 2, 4, 6);
            Vector128<uint> y = B2s.LoadFolded(ref words, ref s, 1, 3, 5, 7);
            G(ref a, ref b, ref c, ref d, x, y, r16, r12, r8, r7);
            B2s.Diagonalize(ref b, ref c, ref d);
            x = B2s.LoadFolded(ref words, ref s, 8, 10, 12, 14);
            y = B2s.LoadFolded(ref words, ref s, 9, 11, 13, 15);
            G(ref a, ref b, ref c, ref d, x, y, r16, r12, r8, r7);
            B2s.Undiagonalize(ref b, ref c, ref d);
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void G(ref Vector128<uint> a, ref Vector128<uint> b, ref Vector128<uint> c, ref Vector128<uint> d, Vector128<uint> x, Vector128<uint> y, Vector128<uint> r16, Vector128<uint> r12, Vector128<uint> r8, Vector128<uint> r7)
    {
        a += b + x;
        d = Avx512F.VL.RotateRightVariable(d ^ a, r16);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, r12);
        a += b + y;
        d = Avx512F.VL.RotateRightVariable(d ^ a, r8);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, r7);
    }
}

/// <summary>Interleaved folded gathers from a copy of the block on the stack, as 1.0.0 gathered them.</summary>
public readonly struct S_StackCopy : IBlake2sKernel
{
    public static string Name => "stackcopy";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [InlineArray(16)]
    private struct Words
    {
        private uint _element;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref uint h, ref byte block, ulong counter, uint finalization)
    {
        Unsafe.SkipInit(out Words m);
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<Words, byte>(ref m), ref block, 64);
        Vector128<uint> h0 = Vector128.LoadUnsafe(ref h);
        Vector128<uint> h1 = Vector128.LoadUnsafe(ref h, 4);
        Vector128<uint> a = h0, b = h1;
        Vector128<uint> c = Vector128.Create(B2s.Iv0, B2s.Iv1, B2s.Iv2, B2s.Iv3);
        Vector128<uint> d = Vector128.Create(B2s.Iv4 ^ (uint)counter, B2s.Iv5 ^ (uint)(counter >> 32), B2s.Iv6 ^ finalization, B2s.Iv7);
        ref uint words = ref Unsafe.As<Words, uint>(ref m);
        ref byte sigma = ref MemoryMarshal.GetReference(B2s.Sigma);
        for (int round = 0; round < 10; round++)
        {
            ref byte s = ref Unsafe.Add(ref sigma, round * 16);
            Vector128<uint> x = B2s.LoadFolded(ref words, ref s, 0, 2, 4, 6);
            Vector128<uint> y = B2s.LoadFolded(ref words, ref s, 1, 3, 5, 7);
            B2s.G(ref a, ref b, ref c, ref d, x, y);
            B2s.Diagonalize(ref b, ref c, ref d);
            x = B2s.LoadFolded(ref words, ref s, 8, 10, 12, 14);
            y = B2s.LoadFolded(ref words, ref s, 9, 11, 13, 15);
            B2s.G(ref a, ref b, ref c, ref d, x, y);
            B2s.Undiagonalize(ref b, ref c, ref d);
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }
}

/// <summary>The message in two Vector256 halves, each step's words gathered by one vpermt2d.</summary>
public readonly struct S_Permute : IBlake2sKernel
{
    public static string Name => "permute";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref uint h, ref byte block, ulong counter, uint finalization)
    {
        Vector256<uint> low = Vector256.LoadUnsafe(ref Unsafe.As<byte, uint>(ref block));
        Vector256<uint> high = Vector256.LoadUnsafe(ref Unsafe.As<byte, uint>(ref block), 8);
        Vector128<uint> h0 = Vector128.LoadUnsafe(ref h);
        Vector128<uint> h1 = Vector128.LoadUnsafe(ref h, 4);
        Vector128<uint> a = h0, b = h1;
        Vector128<uint> c = Vector128.Create(B2s.Iv0, B2s.Iv1, B2s.Iv2, B2s.Iv3);
        Vector128<uint> d = Vector128.Create(B2s.Iv4 ^ (uint)counter, B2s.Iv5 ^ (uint)(counter >> 32), B2s.Iv6 ^ finalization, B2s.Iv7);
        ref uint indices = ref MemoryMarshal.GetArrayDataReference(B2s.PermuteIndices);
        for (int round = 0; round < 10; round++)
        {
            ref uint r = ref Unsafe.Add(ref indices, round * 32);
            Vector128<uint> x = Avx512F.VL.PermuteVar8x32x2(low, Vector256.LoadUnsafe(ref r), high).GetLower();
            Vector128<uint> y = Avx512F.VL.PermuteVar8x32x2(low, Vector256.LoadUnsafe(ref r, 8), high).GetLower();
            B2s.G(ref a, ref b, ref c, ref d, x, y);
            B2s.Diagonalize(ref b, ref c, ref d);
            x = Avx512F.VL.PermuteVar8x32x2(low, Vector256.LoadUnsafe(ref r, 16), high).GetLower();
            y = Avx512F.VL.PermuteVar8x32x2(low, Vector256.LoadUnsafe(ref r, 24), high).GetLower();
            B2s.G(ref a, ref b, ref c, ref d, x, y);
            B2s.Undiagonalize(ref b, ref c, ref d);
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }
}

/// <summary>1.0.0's ProcessBlockAvx512, ported to the kernel signature: stack copy, jagged sigma, Create gathers, vprorvd.</summary>
public readonly struct S_V100 : IBlake2sKernel
{
    private static readonly Vector128<uint> s_ror12 = Vector128.Create(12U), s_ror16 = Vector128.Create(16U), s_ror7 = Vector128.Create(7U), s_ror8 = Vector128.Create(8U);
    private static readonly uint[] s_iv = [B2s.Iv0, B2s.Iv1, B2s.Iv2, B2s.Iv3, B2s.Iv4, B2s.Iv5, B2s.Iv6, B2s.Iv7];

    public static string Name => "v100";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GSimd(ref Vector128<uint> a, ref Vector128<uint> b, ref Vector128<uint> c, ref Vector128<uint> d, Vector128<uint> mx, Vector128<uint> my)
    {
        a += b + mx;
        d = Avx512F.VL.RotateRightVariable(d ^ a, s_ror16);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, s_ror12);
        a += b + my;
        d = Avx512F.VL.RotateRightVariable(d ^ a, s_ror8);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, s_ror7);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref uint hRef, ref byte blockRef, ulong totalBytesIncludingThisBlock, uint finalization)
    {
        Span<uint> m = stackalloc uint[16];
        ref uint wordRef = ref Unsafe.As<byte, uint>(ref blockRef);
        for (int i = 0; i < 16; i++)
            m[i] = Unsafe.Add(ref wordRef, i);

        ref uint mRef = ref MemoryMarshal.GetReference(m);
        var a = Vector128.Create(Unsafe.Add(ref hRef, 0), Unsafe.Add(ref hRef, 1), Unsafe.Add(ref hRef, 2), Unsafe.Add(ref hRef, 3));
        var b = Vector128.Create(Unsafe.Add(ref hRef, 4), Unsafe.Add(ref hRef, 5), Unsafe.Add(ref hRef, 6), Unsafe.Add(ref hRef, 7));
        var c = Vector128.Create(s_iv[0], s_iv[1], s_iv[2], s_iv[3]);
        var d = Vector128.Create(
            s_iv[4] ^ (uint)(totalBytesIncludingThisBlock & 0xFFFFFFFFUL),
            s_iv[5] ^ (uint)(totalBytesIncludingThisBlock >> 32),
            finalization != 0 ? ~s_iv[6] : s_iv[6],
            s_iv[7]);

        for (int r = 0; r < 10; r++)
        {
            byte[] s = B2s.Sigma100[r];
            var mx = Vector128.Create(Unsafe.Add(ref mRef, s[0]), Unsafe.Add(ref mRef, s[2]), Unsafe.Add(ref mRef, s[4]), Unsafe.Add(ref mRef, s[6]));
            var my = Vector128.Create(Unsafe.Add(ref mRef, s[1]), Unsafe.Add(ref mRef, s[3]), Unsafe.Add(ref mRef, s[5]), Unsafe.Add(ref mRef, s[7]));
            GSimd(ref a, ref b, ref c, ref d, mx, my);
            b = Sse2.Shuffle(b.AsInt32(), 0x39).AsUInt32();
            c = Sse2.Shuffle(c.AsInt32(), 0x4E).AsUInt32();
            d = Sse2.Shuffle(d.AsInt32(), 0x93).AsUInt32();
            mx = Vector128.Create(Unsafe.Add(ref mRef, s[8]), Unsafe.Add(ref mRef, s[10]), Unsafe.Add(ref mRef, s[12]), Unsafe.Add(ref mRef, s[14]));
            my = Vector128.Create(Unsafe.Add(ref mRef, s[9]), Unsafe.Add(ref mRef, s[11]), Unsafe.Add(ref mRef, s[13]), Unsafe.Add(ref mRef, s[15]));
            GSimd(ref a, ref b, ref c, ref d, mx, my);
            b = Sse2.Shuffle(b.AsInt32(), 0x93).AsUInt32();
            c = Sse2.Shuffle(c.AsInt32(), 0x4E).AsUInt32();
            d = Sse2.Shuffle(d.AsInt32(), 0x39).AsUInt32();
        }

        var hLo = Vector128.Create(Unsafe.Add(ref hRef, 0), Unsafe.Add(ref hRef, 1), Unsafe.Add(ref hRef, 2), Unsafe.Add(ref hRef, 3));
        var hHi = Vector128.Create(Unsafe.Add(ref hRef, 4), Unsafe.Add(ref hRef, 5), Unsafe.Add(ref hRef, 6), Unsafe.Add(ref hRef, 7));
        hLo ^= a ^ c;
        hHi ^= b ^ d;
        Unsafe.Add(ref hRef, 0) = hLo.GetElement(0);
        Unsafe.Add(ref hRef, 1) = hLo.GetElement(1);
        Unsafe.Add(ref hRef, 2) = hLo.GetElement(2);
        Unsafe.Add(ref hRef, 3) = hLo.GetElement(3);
        Unsafe.Add(ref hRef, 4) = hHi.GetElement(0);
        Unsafe.Add(ref hRef, 5) = hHi.GetElement(1);
        Unsafe.Add(ref hRef, 6) = hHi.GetElement(2);
        Unsafe.Add(ref hRef, 7) = hHi.GetElement(3);
    }
}

public interface IBlake2bKernel
{
    static abstract string Name { get; }
    static abstract bool IsSupported { get; }
    static abstract void Compress(ref ulong h, ref byte block, ulong counter, ulong finalization);
}

public static class B2b
{
    public const ulong Iv0 = 0x6A09E667F3BCC908UL, Iv1 = 0xBB67AE8584CAA73BUL, Iv2 = 0x3C6EF372FE94F82BUL, Iv3 = 0xA54FF53A5F1D36F1UL;
    public const ulong Iv4 = 0x510E527FADE682D1UL, Iv5 = 0x9B05688C2B3E6C1FUL, Iv6 = 0x1F83D9ABFB41BD6BUL, Iv7 = 0x5BE0CD19137E2179UL;

    // Rounds 10 and 11 repeat rounds 0 and 1.
    public static ReadOnlySpan<byte> Sigma =>
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
        14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3,
        11, 8, 12, 0, 5, 2, 15, 13, 10, 14, 3, 6, 7, 1, 9, 4,
        7, 9, 3, 1, 13, 12, 11, 14, 2, 6, 5, 10, 4, 0, 15, 8,
        9, 0, 5, 7, 2, 4, 10, 15, 14, 1, 11, 12, 6, 8, 3, 13,
        2, 12, 6, 10, 0, 11, 8, 3, 4, 13, 7, 5, 15, 14, 1, 9,
        12, 5, 1, 15, 14, 13, 4, 10, 0, 7, 6, 3, 9, 2, 8, 11,
        13, 11, 7, 14, 12, 1, 3, 9, 5, 0, 15, 4, 8, 6, 2, 10,
        6, 15, 14, 9, 11, 3, 0, 8, 12, 2, 13, 7, 1, 4, 10, 5,
        10, 2, 8, 4, 7, 6, 1, 5, 15, 11, 9, 14, 3, 12, 13, 0,
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
        14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3,
    ];

    // Index vectors for vpermt2q over the message held as two Vector512 halves: per round, four vectors of eight lanes,
    // the low four carrying the schedule's word indices.
    public static readonly ulong[] PermuteIndices = BuildPermuteIndices();

    private static ulong[] BuildPermuteIndices()
    {
        var table = new ulong[12 * 4 * 8];
        int[][] picks = [[0, 2, 4, 6], [1, 3, 5, 7], [8, 10, 12, 14], [9, 11, 13, 15]];
        for (int r = 0; r < 12; r++)
            for (int v = 0; v < 4; v++)
                for (int lane = 0; lane < 4; lane++)
                    table[((r * 4) + v) * 8 + lane] = Sigma[(r * 16) + picks[v][lane]];
        return table;
    }

    public static byte[] Hash<TKernel>(ReadOnlySpan<byte> data)
        where TKernel : IBlake2bKernel
    {
        if (data.Length == 0 || data.Length % 128 != 0) throw new ArgumentException("whole blocks only");
        Span<ulong> h = stackalloc ulong[8];
        h[0] = Iv0 ^ 0x01010000UL ^ 64UL; h[1] = Iv1; h[2] = Iv2; h[3] = Iv3; h[4] = Iv4; h[5] = Iv5; h[6] = Iv6; h[7] = Iv7;
        ref ulong hRef = ref MemoryMarshal.GetReference(h);
        ref byte d = ref MemoryMarshal.GetReference(data);
        int blocks = data.Length / 128;
        for (int i = 0; i < blocks - 1; i++)
            TKernel.Compress(ref hRef, ref Unsafe.Add(ref d, i * 128), (ulong)(i + 1) * 128, 0);
        TKernel.Compress(ref hRef, ref Unsafe.Add(ref d, (blocks - 1) * 128), (ulong)blocks * 128, ulong.MaxValue);
        return MemoryMarshal.AsBytes(h).ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong M(ref byte block, int index) => Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref block, index * sizeof(ulong)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void G(ref Vector256<ulong> a, ref Vector256<ulong> b, ref Vector256<ulong> c, ref Vector256<ulong> d, Vector256<ulong> x, Vector256<ulong> y)
    {
        a += b + x;
        d = Avx512F.VL.RotateRight(d ^ a, 32);
        c += d;
        b = Avx512F.VL.RotateRight(b ^ c, 24);
        a += b + y;
        d = Avx512F.VL.RotateRight(d ^ a, 16);
        c += d;
        b = Avx512F.VL.RotateRight(b ^ c, 63);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Diagonalize(ref Vector256<ulong> b, ref Vector256<ulong> c, ref Vector256<ulong> d)
    {
        b = Avx2.Permute4x64(b, 0b00_11_10_01);
        c = Avx2.Permute4x64(c, 0b01_00_11_10);
        d = Avx2.Permute4x64(d, 0b10_01_00_11);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Undiagonalize(ref Vector256<ulong> b, ref Vector256<ulong> c, ref Vector256<ulong> d)
    {
        b = Avx2.Permute4x64(b, 0b10_01_00_11);
        c = Avx2.Permute4x64(c, 0b01_00_11_10);
        d = Avx2.Permute4x64(d, 0b00_11_10_01);
    }

    // Gathers four message words with each load folded into its move or insert.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<ulong> LoadFolded(ref ulong words, ref byte s, int k0, int k1, int k2, int k3)
    {
        Vector128<ulong> low = Vector128.CreateScalarUnsafe(Unsafe.Add(ref words, (nint)Unsafe.Add(ref s, k0)));
        low = Sse41.X64.Insert(low, Unsafe.Add(ref words, (nint)Unsafe.Add(ref s, k1)), 1);
        Vector128<ulong> high = Vector128.CreateScalarUnsafe(Unsafe.Add(ref words, (nint)Unsafe.Add(ref s, k2)));
        high = Sse41.X64.Insert(high, Unsafe.Add(ref words, (nint)Unsafe.Add(ref s, k3)), 1);
        return Vector256.Create(low, high);
    }
}

/// <summary>The library's kernel as it ships (Vector256Kernel over Avx512Isa).</summary>
public readonly struct B_Current : IBlake2bKernel
{
    public static string Name => "current";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref ulong h, ref byte block, ulong counter, ulong finalization)
    {
        Vector256<ulong> h0 = Vector256.LoadUnsafe(ref h);
        Vector256<ulong> h1 = Vector256.LoadUnsafe(ref h, 4);
        Vector256<ulong> a = h0, b = h1;
        Vector256<ulong> c = Vector256.Create(B2b.Iv0, B2b.Iv1, B2b.Iv2, B2b.Iv3);
        Vector256<ulong> d = Vector256.Create(B2b.Iv4 ^ counter, B2b.Iv5, B2b.Iv6 ^ finalization, B2b.Iv7);
        ref byte sigma = ref MemoryMarshal.GetReference(B2b.Sigma);
        for (int round = 0; round < 12; round++)
        {
            ref byte s = ref Unsafe.Add(ref sigma, round * 16);
            Round(ref a, ref b, ref c, ref d, Load(ref block, ref s, 0, 2, 4, 6), Load(ref block, ref s, 1, 3, 5, 7), Load(ref block, ref s, 8, 10, 12, 14), Load(ref block, ref s, 9, 11, 13, 15));
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Round(ref Vector256<ulong> a, ref Vector256<ulong> b, ref Vector256<ulong> c, ref Vector256<ulong> d, Vector256<ulong> cx, Vector256<ulong> cy, Vector256<ulong> dx, Vector256<ulong> dy)
    {
        B2b.G(ref a, ref b, ref c, ref d, cx, cy);
        B2b.Diagonalize(ref b, ref c, ref d);
        B2b.G(ref a, ref b, ref c, ref d, dx, dy);
        B2b.Undiagonalize(ref b, ref c, ref d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<ulong> Load(ref byte block, ref byte s, int k0, int k1, int k2, int k3) =>
        Vector256.Create(B2b.M(ref block, Unsafe.Add(ref s, k0)), B2b.M(ref block, Unsafe.Add(ref s, k1)), B2b.M(ref block, Unsafe.Add(ref s, k2)), B2b.M(ref block, Unsafe.Add(ref s, k3)));
}

/// <summary>Folded loads, gathers at the top of the round.</summary>
public readonly struct B_Folded : IBlake2bKernel
{
    public static string Name => "folded";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref ulong h, ref byte block, ulong counter, ulong finalization)
    {
        Vector256<ulong> h0 = Vector256.LoadUnsafe(ref h);
        Vector256<ulong> h1 = Vector256.LoadUnsafe(ref h, 4);
        Vector256<ulong> a = h0, b = h1;
        Vector256<ulong> c = Vector256.Create(B2b.Iv0, B2b.Iv1, B2b.Iv2, B2b.Iv3);
        Vector256<ulong> d = Vector256.Create(B2b.Iv4 ^ counter, B2b.Iv5, B2b.Iv6 ^ finalization, B2b.Iv7);
        ref ulong words = ref Unsafe.As<byte, ulong>(ref block);
        ref byte sigma = ref MemoryMarshal.GetReference(B2b.Sigma);
        for (int round = 0; round < 12; round++)
        {
            ref byte s = ref Unsafe.Add(ref sigma, round * 16);
            Vector256<ulong> cx = B2b.LoadFolded(ref words, ref s, 0, 2, 4, 6);
            Vector256<ulong> cy = B2b.LoadFolded(ref words, ref s, 1, 3, 5, 7);
            Vector256<ulong> dx = B2b.LoadFolded(ref words, ref s, 8, 10, 12, 14);
            Vector256<ulong> dy = B2b.LoadFolded(ref words, ref s, 9, 11, 13, 15);
            B2b.G(ref a, ref b, ref c, ref d, cx, cy);
            B2b.Diagonalize(ref b, ref c, ref d);
            B2b.G(ref a, ref b, ref c, ref d, dx, dy);
            B2b.Undiagonalize(ref b, ref c, ref d);
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }
}

/// <summary>Folded loads, the diagonal gathers after the column step.</summary>
public readonly struct B_Interleaved : IBlake2bKernel
{
    public static string Name => "interleaved";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref ulong h, ref byte block, ulong counter, ulong finalization)
    {
        Vector256<ulong> h0 = Vector256.LoadUnsafe(ref h);
        Vector256<ulong> h1 = Vector256.LoadUnsafe(ref h, 4);
        Vector256<ulong> a = h0, b = h1;
        Vector256<ulong> c = Vector256.Create(B2b.Iv0, B2b.Iv1, B2b.Iv2, B2b.Iv3);
        Vector256<ulong> d = Vector256.Create(B2b.Iv4 ^ counter, B2b.Iv5, B2b.Iv6 ^ finalization, B2b.Iv7);
        ref ulong words = ref Unsafe.As<byte, ulong>(ref block);
        ref byte sigma = ref MemoryMarshal.GetReference(B2b.Sigma);
        for (int round = 0; round < 12; round++)
        {
            ref byte s = ref Unsafe.Add(ref sigma, round * 16);
            Vector256<ulong> x = B2b.LoadFolded(ref words, ref s, 0, 2, 4, 6);
            Vector256<ulong> y = B2b.LoadFolded(ref words, ref s, 1, 3, 5, 7);
            B2b.G(ref a, ref b, ref c, ref d, x, y);
            B2b.Diagonalize(ref b, ref c, ref d);
            x = B2b.LoadFolded(ref words, ref s, 8, 10, 12, 14);
            y = B2b.LoadFolded(ref words, ref s, 9, 11, 13, 15);
            B2b.G(ref a, ref b, ref c, ref d, x, y);
            B2b.Undiagonalize(ref b, ref c, ref d);
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }
}

/// <summary>Interleaved folded gathers with 1.0.0's variable rotates (vprorvq).</summary>
public readonly struct B_Variable : IBlake2bKernel
{
    private static readonly Vector256<ulong> s_r32 = Vector256.Create(32UL), s_r24 = Vector256.Create(24UL), s_r16 = Vector256.Create(16UL), s_r63 = Vector256.Create(63UL);

    public static string Name => "variable";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref ulong h, ref byte block, ulong counter, ulong finalization)
    {
        Vector256<ulong> h0 = Vector256.LoadUnsafe(ref h);
        Vector256<ulong> h1 = Vector256.LoadUnsafe(ref h, 4);
        Vector256<ulong> a = h0, b = h1;
        Vector256<ulong> c = Vector256.Create(B2b.Iv0, B2b.Iv1, B2b.Iv2, B2b.Iv3);
        Vector256<ulong> d = Vector256.Create(B2b.Iv4 ^ counter, B2b.Iv5, B2b.Iv6 ^ finalization, B2b.Iv7);
        Vector256<ulong> r32 = s_r32, r24 = s_r24, r16 = s_r16, r63 = s_r63;
        ref ulong words = ref Unsafe.As<byte, ulong>(ref block);
        ref byte sigma = ref MemoryMarshal.GetReference(B2b.Sigma);
        for (int round = 0; round < 12; round++)
        {
            ref byte s = ref Unsafe.Add(ref sigma, round * 16);
            Vector256<ulong> x = B2b.LoadFolded(ref words, ref s, 0, 2, 4, 6);
            Vector256<ulong> y = B2b.LoadFolded(ref words, ref s, 1, 3, 5, 7);
            G(ref a, ref b, ref c, ref d, x, y, r32, r24, r16, r63);
            B2b.Diagonalize(ref b, ref c, ref d);
            x = B2b.LoadFolded(ref words, ref s, 8, 10, 12, 14);
            y = B2b.LoadFolded(ref words, ref s, 9, 11, 13, 15);
            G(ref a, ref b, ref c, ref d, x, y, r32, r24, r16, r63);
            B2b.Undiagonalize(ref b, ref c, ref d);
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void G(ref Vector256<ulong> a, ref Vector256<ulong> b, ref Vector256<ulong> c, ref Vector256<ulong> d, Vector256<ulong> x, Vector256<ulong> y, Vector256<ulong> r32, Vector256<ulong> r24, Vector256<ulong> r16, Vector256<ulong> r63)
    {
        a += b + x;
        d = Avx512F.VL.RotateRightVariable(d ^ a, r32);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, r24);
        a += b + y;
        d = Avx512F.VL.RotateRightVariable(d ^ a, r16);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, r63);
    }
}

/// <summary>Interleaved folded gathers from a copy of the block on the stack.</summary>
public readonly struct B_StackCopy : IBlake2bKernel
{
    public static string Name => "stackcopy";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [InlineArray(16)]
    private struct Words
    {
        private ulong _element;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref ulong h, ref byte block, ulong counter, ulong finalization)
    {
        Unsafe.SkipInit(out Words m);
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<Words, byte>(ref m), ref block, 128);
        Vector256<ulong> h0 = Vector256.LoadUnsafe(ref h);
        Vector256<ulong> h1 = Vector256.LoadUnsafe(ref h, 4);
        Vector256<ulong> a = h0, b = h1;
        Vector256<ulong> c = Vector256.Create(B2b.Iv0, B2b.Iv1, B2b.Iv2, B2b.Iv3);
        Vector256<ulong> d = Vector256.Create(B2b.Iv4 ^ counter, B2b.Iv5, B2b.Iv6 ^ finalization, B2b.Iv7);
        ref ulong words = ref Unsafe.As<Words, ulong>(ref m);
        ref byte sigma = ref MemoryMarshal.GetReference(B2b.Sigma);
        for (int round = 0; round < 12; round++)
        {
            ref byte s = ref Unsafe.Add(ref sigma, round * 16);
            Vector256<ulong> x = B2b.LoadFolded(ref words, ref s, 0, 2, 4, 6);
            Vector256<ulong> y = B2b.LoadFolded(ref words, ref s, 1, 3, 5, 7);
            B2b.G(ref a, ref b, ref c, ref d, x, y);
            B2b.Diagonalize(ref b, ref c, ref d);
            x = B2b.LoadFolded(ref words, ref s, 8, 10, 12, 14);
            y = B2b.LoadFolded(ref words, ref s, 9, 11, 13, 15);
            B2b.G(ref a, ref b, ref c, ref d, x, y);
            B2b.Undiagonalize(ref b, ref c, ref d);
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }
}

/// <summary>The message in two Vector512 halves, each step's words gathered by one vpermt2q.</summary>
public readonly struct B_Permute : IBlake2bKernel
{
    public static string Name => "permute";
    public static bool IsSupported => Avx512F.IsSupported && Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref ulong h, ref byte block, ulong counter, ulong finalization)
    {
        Vector512<ulong> low = Vector512.LoadUnsafe(ref Unsafe.As<byte, ulong>(ref block));
        Vector512<ulong> high = Vector512.LoadUnsafe(ref Unsafe.As<byte, ulong>(ref block), 8);
        Vector256<ulong> h0 = Vector256.LoadUnsafe(ref h);
        Vector256<ulong> h1 = Vector256.LoadUnsafe(ref h, 4);
        Vector256<ulong> a = h0, b = h1;
        Vector256<ulong> c = Vector256.Create(B2b.Iv0, B2b.Iv1, B2b.Iv2, B2b.Iv3);
        Vector256<ulong> d = Vector256.Create(B2b.Iv4 ^ counter, B2b.Iv5, B2b.Iv6 ^ finalization, B2b.Iv7);
        ref ulong indices = ref MemoryMarshal.GetArrayDataReference(B2b.PermuteIndices);
        for (int round = 0; round < 12; round++)
        {
            ref ulong r = ref Unsafe.Add(ref indices, round * 32);
            Vector256<ulong> x = Avx512F.PermuteVar8x64x2(low, Vector512.LoadUnsafe(ref r), high).GetLower();
            Vector256<ulong> y = Avx512F.PermuteVar8x64x2(low, Vector512.LoadUnsafe(ref r, 8), high).GetLower();
            B2b.G(ref a, ref b, ref c, ref d, x, y);
            B2b.Diagonalize(ref b, ref c, ref d);
            x = Avx512F.PermuteVar8x64x2(low, Vector512.LoadUnsafe(ref r, 16), high).GetLower();
            y = Avx512F.PermuteVar8x64x2(low, Vector512.LoadUnsafe(ref r, 24), high).GetLower();
            B2b.G(ref a, ref b, ref c, ref d, x, y);
            B2b.Undiagonalize(ref b, ref c, ref d);
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }
}

/// <summary>1.0.0's ProcessBlockAvx512, ported to the kernel signature.</summary>
public readonly struct B_V100 : IBlake2bKernel
{
    private static readonly Vector256<ulong> s_ror16 = Vector256.Create(16UL), s_ror24 = Vector256.Create(24UL), s_ror32 = Vector256.Create(32UL), s_ror63 = Vector256.Create(63UL);
    private static readonly ulong[] s_iv = [B2b.Iv0, B2b.Iv1, B2b.Iv2, B2b.Iv3, B2b.Iv4, B2b.Iv5, B2b.Iv6, B2b.Iv7];

    public static string Name => "v100";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GSimd(ref Vector256<ulong> a, ref Vector256<ulong> b, ref Vector256<ulong> c, ref Vector256<ulong> d, Vector256<ulong> mx, Vector256<ulong> my)
    {
        a += b + mx;
        d = Avx512F.VL.RotateRightVariable(d ^ a, s_ror32);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, s_ror24);
        a += b + my;
        d = Avx512F.VL.RotateRightVariable(d ^ a, s_ror16);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, s_ror63);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref ulong hRef, ref byte blockRef, ulong totalBytesIncludingThisBlock, ulong finalization)
    {
        Span<ulong> m = stackalloc ulong[16];
        ref ulong wordRef = ref Unsafe.As<byte, ulong>(ref blockRef);
        for (int i = 0; i < 16; i++)
            m[i] = Unsafe.Add(ref wordRef, i);

        ref ulong mRef = ref MemoryMarshal.GetReference(m);
        var a = Vector256.Create(Unsafe.Add(ref hRef, 0), Unsafe.Add(ref hRef, 1), Unsafe.Add(ref hRef, 2), Unsafe.Add(ref hRef, 3));
        var b = Vector256.Create(Unsafe.Add(ref hRef, 4), Unsafe.Add(ref hRef, 5), Unsafe.Add(ref hRef, 6), Unsafe.Add(ref hRef, 7));
        var c = Vector256.Create(s_iv[0], s_iv[1], s_iv[2], s_iv[3]);
        var d = Vector256.Create(s_iv[4] ^ totalBytesIncludingThisBlock, s_iv[5], finalization != 0 ? ~s_iv[6] : s_iv[6], s_iv[7]);

        for (int r = 0; r < 12; r++)
        {
            byte[] s = B2s.Sigma100[r % 10];
            var mx = Vector256.Create(Unsafe.Add(ref mRef, s[0]), Unsafe.Add(ref mRef, s[2]), Unsafe.Add(ref mRef, s[4]), Unsafe.Add(ref mRef, s[6]));
            var my = Vector256.Create(Unsafe.Add(ref mRef, s[1]), Unsafe.Add(ref mRef, s[3]), Unsafe.Add(ref mRef, s[5]), Unsafe.Add(ref mRef, s[7]));
            GSimd(ref a, ref b, ref c, ref d, mx, my);
            b = Avx2.Permute4x64(b, 0x39);
            c = Avx2.Permute4x64(c, 0x4E);
            d = Avx2.Permute4x64(d, 0x93);
            mx = Vector256.Create(Unsafe.Add(ref mRef, s[8]), Unsafe.Add(ref mRef, s[10]), Unsafe.Add(ref mRef, s[12]), Unsafe.Add(ref mRef, s[14]));
            my = Vector256.Create(Unsafe.Add(ref mRef, s[9]), Unsafe.Add(ref mRef, s[11]), Unsafe.Add(ref mRef, s[13]), Unsafe.Add(ref mRef, s[15]));
            GSimd(ref a, ref b, ref c, ref d, mx, my);
            b = Avx2.Permute4x64(b, 0x93);
            c = Avx2.Permute4x64(c, 0x4E);
            d = Avx2.Permute4x64(d, 0x39);
        }

        var hLo = Vector256.Create(Unsafe.Add(ref hRef, 0), Unsafe.Add(ref hRef, 1), Unsafe.Add(ref hRef, 2), Unsafe.Add(ref hRef, 3));
        var hHi = Vector256.Create(Unsafe.Add(ref hRef, 4), Unsafe.Add(ref hRef, 5), Unsafe.Add(ref hRef, 6), Unsafe.Add(ref hRef, 7));
        hLo ^= a ^ c;
        hHi ^= b ^ d;
        Unsafe.Add(ref hRef, 0) = hLo.GetElement(0);
        Unsafe.Add(ref hRef, 1) = hLo.GetElement(1);
        Unsafe.Add(ref hRef, 2) = hLo.GetElement(2);
        Unsafe.Add(ref hRef, 3) = hLo.GetElement(3);
        Unsafe.Add(ref hRef, 4) = hHi.GetElement(0);
        Unsafe.Add(ref hRef, 5) = hHi.GetElement(1);
        Unsafe.Add(ref hRef, 6) = hHi.GetElement(2);
        Unsafe.Add(ref hRef, 7) = hHi.GetElement(3);
    }
}

/// <summary>The library's kernel after the change: the native-sized index and the word-at-a-time gather.</summary>
public readonly struct S_Fixed : IBlake2sKernel
{
    public static string Name => "fixed";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint M(ref byte block, nuint index) => Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref block, index * sizeof(uint)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<uint> Load(ref byte block, ref byte s, int k0, int k1, int k2, int k3) =>
        Vector128.CreateScalarUnsafe(M(ref block, Unsafe.Add(ref s, k0)))
            .WithElement(1, M(ref block, Unsafe.Add(ref s, k1)))
            .WithElement(2, M(ref block, Unsafe.Add(ref s, k2)))
            .WithElement(3, M(ref block, Unsafe.Add(ref s, k3)));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref uint h, ref byte block, ulong counter, uint finalization)
    {
        Vector128<uint> h0 = Vector128.LoadUnsafe(ref h);
        Vector128<uint> h1 = Vector128.LoadUnsafe(ref h, 4);
        Vector128<uint> a = h0, b = h1;
        Vector128<uint> c = Vector128.Create(B2s.Iv0, B2s.Iv1, B2s.Iv2, B2s.Iv3);
        Vector128<uint> d = Vector128.Create(B2s.Iv4 ^ (uint)counter, B2s.Iv5 ^ (uint)(counter >> 32), B2s.Iv6 ^ finalization, B2s.Iv7);
        ref byte sigma = ref MemoryMarshal.GetReference(B2s.Sigma);
        for (int round = 0; round < 10; round++)
        {
            ref byte s = ref Unsafe.Add(ref sigma, round * 16);
            Vector128<uint> cx = Load(ref block, ref s, 0, 2, 4, 6), cy = Load(ref block, ref s, 1, 3, 5, 7), dx = Load(ref block, ref s, 8, 10, 12, 14), dy = Load(ref block, ref s, 9, 11, 13, 15);
            B2s.G(ref a, ref b, ref c, ref d, cx, cy);
            B2s.Diagonalize(ref b, ref c, ref d);
            B2s.G(ref a, ref b, ref c, ref d, dx, dy);
            B2s.Undiagonalize(ref b, ref c, ref d);
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }
}

/// <summary>The library's kernel after the change: the native-sized index and the half-at-a-time gather.</summary>
public readonly struct B_Fixed : IBlake2bKernel
{
    public static string Name => "fixed";
    public static bool IsSupported => Avx512F.VL.IsSupported;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong M(ref byte block, nuint index) => Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref block, index * sizeof(ulong)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<ulong> Load(ref byte block, ref byte s, int k0, int k1, int k2, int k3) =>
        Vector256.Create(
            Vector128.CreateScalarUnsafe(M(ref block, Unsafe.Add(ref s, k0))).WithElement(1, M(ref block, Unsafe.Add(ref s, k1))),
            Vector128.CreateScalarUnsafe(M(ref block, Unsafe.Add(ref s, k2))).WithElement(1, M(ref block, Unsafe.Add(ref s, k3))));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Compress(ref ulong h, ref byte block, ulong counter, ulong finalization)
    {
        Vector256<ulong> h0 = Vector256.LoadUnsafe(ref h);
        Vector256<ulong> h1 = Vector256.LoadUnsafe(ref h, 4);
        Vector256<ulong> a = h0, b = h1;
        Vector256<ulong> c = Vector256.Create(B2b.Iv0, B2b.Iv1, B2b.Iv2, B2b.Iv3);
        Vector256<ulong> d = Vector256.Create(B2b.Iv4 ^ counter, B2b.Iv5, B2b.Iv6 ^ finalization, B2b.Iv7);
        ref byte sigma = ref MemoryMarshal.GetReference(B2b.Sigma);
        for (int round = 0; round < 12; round++)
        {
            ref byte s = ref Unsafe.Add(ref sigma, round * 16);
            Vector256<ulong> cx = Load(ref block, ref s, 0, 2, 4, 6), cy = Load(ref block, ref s, 1, 3, 5, 7), dx = Load(ref block, ref s, 8, 10, 12, 14), dy = Load(ref block, ref s, 9, 11, 13, 15);
            B2b.G(ref a, ref b, ref c, ref d, cx, cy);
            B2b.Diagonalize(ref b, ref c, ref d);
            B2b.G(ref a, ref b, ref c, ref d, dx, dy);
            B2b.Undiagonalize(ref b, ref c, ref d);
        }

        (h0 ^ a ^ c).StoreUnsafe(ref h);
        (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
    }
}

/// <summary>Verifies each variant against the library and yields the cases the harness measures.</summary>
public static class Experiment
{
    public static IEnumerable<(string Name, Action Run)> Cases(byte[] data)
    {
        byte[] blake2s, blake2b;
        using (var algorithm = new Blake2s()) blake2s = algorithm.ComputeHash(data);
        using (var algorithm = new Blake2b()) blake2b = algorithm.ComputeHash(data);

        var cases = new List<(string, Action)>();
        void S<T>() where T : IBlake2sKernel
        {
            if (!T.IsSupported) return;
            if (!B2s.Hash<T>(data).AsSpan().SequenceEqual(blake2s)) { Console.WriteLine($"kernel  BLAKE2s {T.Name} MISMATCH"); return; }
            cases.Add(($"BLAKE2s {T.Name}", () => B2s.Hash<T>(data)));
        }

        void B<T>() where T : IBlake2bKernel
        {
            if (!T.IsSupported) return;
            if (!B2b.Hash<T>(data).AsSpan().SequenceEqual(blake2b)) { Console.WriteLine($"kernel  BLAKE2b {T.Name} MISMATCH"); return; }
            cases.Add(($"BLAKE2b {T.Name}", () => B2b.Hash<T>(data)));
        }

        S<S_Current>(); S<S_Fixed>(); S<S_Interleaved>(); S<S_Variable>(); S<S_StackCopy>(); S<S_Permute>(); S<S_V100>();
        B<B_Current>(); B<B_Fixed>(); B<B_Interleaved>(); B<B_Variable>(); B<B_StackCopy>(); B<B_Permute>(); B<B_V100>();
        return cases;
    }
}
#endif
