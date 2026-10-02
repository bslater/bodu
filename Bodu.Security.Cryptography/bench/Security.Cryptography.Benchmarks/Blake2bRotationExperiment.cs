// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bRotationExperiment.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

// TEMPORARY (issue #755): BLAKE2b's 256-bit kernel, copied from Blake2bCore.Vector256Kernel<TIsa>, over six sets of
// rotations, timed side by side on the hosted runners to find what AMD's Zen 4 rewards without losing Intel's AVX-512
// gain. Each set is held to the library's digest before it is timed. Remove before merging.
#pragma warning disable
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography.Benchmarks;

internal static class Blake2bRotationExperiment
{
    private const ulong Iv0 = 0x6A09E667F3BCC908UL, Iv1 = 0xBB67AE8584CAA73BUL, Iv2 = 0x3C6EF372FE94F82BUL;
    private const ulong Iv3 = 0xA54FF53A5F1D36F1UL, Iv4 = 0x510E527FADE682D1UL, Iv5 = 0x9B05688C2B3E6C1FUL;
    private const ulong Iv6 = 0x1F83D9ABFB41BD6BUL, Iv7 = 0x5BE0CD19137E2179UL;

    private static ReadOnlySpan<byte> Sigma =>
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

    // Each set of rotations names, for 32, 24, 16 and 63 bits, the instruction it rotates with:
    //   Ror       VPRORQ for every one: the library's Avx512Isa.
    //   Shuffle   VPSHUFD, VPSHUFB, VPSHUFB, and an add, a shift and an XOR: the library's Avx2Isa.
    //   Mixed     the shuffles for 32, 24 and 16, VPRORQ for 63: the set issue #755 proposes.
    //   Shuf32    VPSHUFD for 32, VPRORQ for the rest.
    //   Shuf2416  VPSHUFB for 24 and 16, VPRORQ for 32 and 63.
    //   Add63     VPRORQ for 32, 24 and 16, and the add, shift and XOR for 63.
    internal static IEnumerable<(string Name, Action Run)> Cases(byte[] data)
    {
        byte[] expected;
        using (var blake2b = new Blake2b())
            expected = blake2b.ComputeHash(data);

        var state = new ulong[8];
        var sets = new (string Name, bool IsSupported, Func<byte[]> Hash, Action Run)[]
        {
            ("Ror", Avx512F.VL.IsSupported, () => Hash<Ror>(data), () => CompressAll<Ror>(state, data)),
            ("Shuffle", Avx2.IsSupported, () => Hash<Shuffle>(data), () => CompressAll<Shuffle>(state, data)),
            ("Mixed", Avx512F.VL.IsSupported, () => Hash<Mixed>(data), () => CompressAll<Mixed>(state, data)),
            ("Shuf32", Avx512F.VL.IsSupported, () => Hash<Shuf32>(data), () => CompressAll<Shuf32>(state, data)),
            ("Shuf2416", Avx512F.VL.IsSupported, () => Hash<Shuf2416>(data), () => CompressAll<Shuf2416>(state, data)),
            ("Add63", Avx512F.VL.IsSupported, () => Hash<Add63>(data), () => CompressAll<Add63>(state, data)),
        };

        foreach ((string name, bool isSupported, Func<byte[]> hash, Action run) in sets)
        {
            if (!isSupported)
                continue;

            if (!hash().AsSpan().SequenceEqual(expected))
                throw new InvalidOperationException($"The BLAKE2b rotation set {name} does not match the library's digest.");

            yield return ($"BLAKE2b rotations {name} 1 MiB", run);
        }
    }

    private static byte[] Hash<TRotations>(ReadOnlySpan<byte> data)
        where TRotations : struct, IRotations
    {
        Span<ulong> h = stackalloc ulong[8];
        h[0] = Iv0 ^ 0x01010040UL;
        h[1] = Iv1;
        h[2] = Iv2;
        h[3] = Iv3;
        h[4] = Iv4;
        h[5] = Iv5;
        h[6] = Iv6;
        h[7] = Iv7;
        CompressAll<TRotations>(h, data);
        return MemoryMarshal.AsBytes(h).ToArray();
    }

    // Compresses every whole block, the last with the finalization flag; data must be one or more whole blocks.
    private static void CompressAll<TRotations>(Span<ulong> state, ReadOnlySpan<byte> data)
        where TRotations : struct, IRotations
    {
        ref ulong h = ref MemoryMarshal.GetReference(state);
        ref byte d = ref MemoryMarshal.GetReference(data);
        int blocks = data.Length / 128;
        for (int i = 0; i < blocks; i++)
            Kernel<TRotations>.Compress(ref h, ref Unsafe.Add(ref d, i * 128), (ulong)(i + 1) * 128, i == blocks - 1 ? ulong.MaxValue : 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong M(ref byte block, nuint index)
    {
        ulong word = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref block, index * sizeof(ulong)));
        return BitConverter.IsLittleEndian ? word : System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(word);
    }

    internal interface IRotations
    {
        static abstract Vector256<ulong> RotateRight32(Vector256<ulong> value);

        static abstract Vector256<ulong> RotateRight24(Vector256<ulong> value);

        static abstract Vector256<ulong> RotateRight16(Vector256<ulong> value);

        static abstract Vector256<ulong> RotateRight63(Vector256<ulong> value);
    }

    internal readonly struct Ror : IRotations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight32(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight24(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 24);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight16(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight63(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 63);
    }

    internal readonly struct Shuffle : IRotations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight32(Vector256<ulong> value) =>
            Avx2.Shuffle(value.AsUInt32(), 0b10_11_00_01).AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight24(Vector256<ulong> value) =>
            Avx2.Shuffle(value.AsByte(), Vector256.Create((byte)3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10, 3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10)).AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight16(Vector256<ulong> value) =>
            Avx2.Shuffle(value.AsByte(), Vector256.Create((byte)2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9, 2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9)).AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight63(Vector256<ulong> value) =>
            Avx2.Add(value, value) ^ Avx2.ShiftRightLogical(value, 63);
    }

    internal readonly struct Mixed : IRotations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight32(Vector256<ulong> value) =>
            Avx2.Shuffle(value.AsUInt32(), 0b10_11_00_01).AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight24(Vector256<ulong> value) =>
            Avx2.Shuffle(value.AsByte(), Vector256.Create((byte)3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10, 3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10)).AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight16(Vector256<ulong> value) =>
            Avx2.Shuffle(value.AsByte(), Vector256.Create((byte)2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9, 2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9)).AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight63(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 63);
    }

    internal readonly struct Shuf32 : IRotations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight32(Vector256<ulong> value) =>
            Avx2.Shuffle(value.AsUInt32(), 0b10_11_00_01).AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight24(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 24);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight16(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight63(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 63);
    }

    internal readonly struct Shuf2416 : IRotations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight32(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight24(Vector256<ulong> value) =>
            Avx2.Shuffle(value.AsByte(), Vector256.Create((byte)3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10, 3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10)).AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight16(Vector256<ulong> value) =>
            Avx2.Shuffle(value.AsByte(), Vector256.Create((byte)2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9, 2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9)).AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight63(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 63);
    }

    internal readonly struct Add63 : IRotations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight32(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight24(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 24);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight16(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight63(Vector256<ulong> value) =>
            Avx2.Add(value, value) ^ Avx2.ShiftRightLogical(value, 63);
    }

    // Blake2bCore.Vector256Kernel<TIsa> as #754 left it, over a set of rotations.
    internal readonly struct Kernel<TRotations>
        where TRotations : struct, IRotations
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void Compress(ref ulong h, ref byte block, ulong counter, ulong finalization)
        {
            Vector256<ulong> h0 = Vector256.LoadUnsafe(ref h);
            Vector256<ulong> h1 = Vector256.LoadUnsafe(ref h, 4);
            Vector256<ulong> a = h0;
            Vector256<ulong> b = h1;
            Vector256<ulong> c = Vector256.Create(Iv0, Iv1, Iv2, Iv3);
            Vector256<ulong> d = Vector256.Create(Iv4 ^ counter, Iv5, Iv6 ^ finalization, Iv7);

            ref byte sigma = ref MemoryMarshal.GetReference(Sigma);
            for (int round = 0; round < 12; round++)
            {
                ref byte s = ref Unsafe.Add(ref sigma, round * 16);

                Round(
                    ref a,
                    ref b,
                    ref c,
                    ref d,
                    Load(ref block, ref s, 0, 2, 4, 6),
                    Load(ref block, ref s, 1, 3, 5, 7),
                    Load(ref block, ref s, 14, 8, 10, 12),
                    Load(ref block, ref s, 15, 9, 11, 13));
            }

            (h0 ^ a ^ c).StoreUnsafe(ref h);
            (h1 ^ b ^ d).StoreUnsafe(ref h, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Round(
            ref Vector256<ulong> a,
            ref Vector256<ulong> b,
            ref Vector256<ulong> c,
            ref Vector256<ulong> d,
            Vector256<ulong> columnX,
            Vector256<ulong> columnY,
            Vector256<ulong> diagonalX,
            Vector256<ulong> diagonalY)
        {
            G(ref a, ref b, ref c, ref d, columnX, columnY);

            a = Avx2.Permute4x64(a, 0b10_01_00_11);
            c = Avx2.Permute4x64(c, 0b00_11_10_01);
            d = Avx2.Permute4x64(d, 0b01_00_11_10);

            G(ref a, ref b, ref c, ref d, diagonalX, diagonalY);

            a = Avx2.Permute4x64(a, 0b00_11_10_01);
            c = Avx2.Permute4x64(c, 0b10_01_00_11);
            d = Avx2.Permute4x64(d, 0b01_00_11_10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void G(ref Vector256<ulong> a, ref Vector256<ulong> b, ref Vector256<ulong> c, ref Vector256<ulong> d, Vector256<ulong> x, Vector256<ulong> y)
        {
            a = a + x + b;
            d = TRotations.RotateRight32(d ^ a);
            c += d;
            b = TRotations.RotateRight24(b ^ c);
            a = a + y + b;
            d = TRotations.RotateRight16(d ^ a);
            c += d;
            b = TRotations.RotateRight63(b ^ c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> Load(ref byte block, ref byte schedule, int k0, int k1, int k2, int k3) =>
            Vector256.Create(
                Vector128.CreateScalarUnsafe(M(ref block, Unsafe.Add(ref schedule, k0))).WithElement(1, M(ref block, Unsafe.Add(ref schedule, k1))),
                Vector128.CreateScalarUnsafe(M(ref block, Unsafe.Add(ref schedule, k2))).WithElement(1, M(ref block, Unsafe.Add(ref schedule, k3))));
    }
}
