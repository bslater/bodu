// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3CoreTests.CompressChunks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

public sealed partial class Blake3CoreTests
{
    /// <summary>
    /// Verifies that each kernel compresses every chunk of a run exactly as compressing the chunks one block at a time
    /// does, for every run length up to two groups of sixteen lanes and a remainder and a counter whose low word wraps inside the run.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void CompressChunks_WhenRunLengthVaries_ForEachKernel_ShouldMatchChunkByChunkCompression(string kernel)
    {
        Blake3Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x3C3C);

        for (int count = 0; count <= 40; count++)
        {
            byte[] chunks = new byte[count * Blake3Core.ChunkBytes];
            random.NextBytes(chunks);
            uint[] key = RandomKey(random);
            uint flags = (uint)random.Next(0, 128) & ~(Blake3Core.ChunkStart | Blake3Core.ChunkEnd);
            ulong counter = count % 2 == 0 ? uint.MaxValue - (ulong)(count / 2) : (ulong)random.NextInt64();

            byte[] expected = new byte[count * Blake3Core.ChainingValueBytes];
            for (int chunk = 0; chunk < count; chunk++)
            {
                uint[] chainingValue = CompressChunk(Blake3Core.KernelKind.Scalar, key, flags, chunks.AsSpan(chunk * Blake3Core.ChunkBytes, Blake3Core.ChunkBytes), counter + (ulong)chunk, isRoot: false);
                Encode(chainingValue).CopyTo(expected, chunk * Blake3Core.ChainingValueBytes);
            }

            byte[] actual = new byte[expected.Length];
            Blake3Core.CompressChunks(kind, chunks, key, counter, flags, actual);

            CollectionAssert.AreEqual(expected, actual, $"{count} chunks from counter {counter:X}");
        }
    }

    /// <summary>
    /// Verifies that dispatch compresses a run of chunks exactly as the kernel it selects does.
    /// </summary>
    [TestMethod]
    public void CompressChunks_WhenKernelIsAuto_ShouldMatchSelectedKernel()
    {
        byte[] chunks = new byte[11 * Blake3Core.ChunkBytes];
        new Random(0x3C3D).NextBytes(chunks);
        byte[] expected = new byte[11 * Blake3Core.ChainingValueBytes];
        byte[] actual = new byte[expected.Length];

        Blake3Core.CompressChunks(Blake3Core.SelectKernel(), chunks, Blake3Core.InitializationVector, 5, 0, expected);
        Blake3Core.CompressChunks(Blake3Core.KernelKind.Auto, chunks, Blake3Core.InitializationVector, 5, 0, actual);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that input holding a partial chunk is rejected with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void CompressChunks_WhenChunksIsNotWholeChunks_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Blake3Core.CompressChunks(Blake3Core.KernelKind.Auto, new byte[Blake3Core.ChunkBytes + 1], Blake3Core.InitializationVector.ToArray(), 0, 0, new byte[64]);
        });

        Assert.AreEqual("chunks", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a key shorter than eight words is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void CompressChunks_WhenKeyIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.CompressChunks(Blake3Core.KernelKind.Auto, new byte[Blake3Core.ChunkBytes], new uint[7], 0, 0, new byte[Blake3Core.ChainingValueBytes]);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that room for fewer chaining values than chunks is rejected with
    /// <see cref="ArgumentOutOfRangeException" /> rather than written past.
    /// </summary>
    [TestMethod]
    public void CompressChunks_WhenChainingValuesIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.CompressChunks(Blake3Core.KernelKind.Auto, new byte[2 * Blake3Core.ChunkBytes], Blake3Core.InitializationVector.ToArray(), 0, 0, new byte[(2 * Blake3Core.ChainingValueBytes) - 1]);
        });

        Assert.AreEqual("chainingValues", ex.ParamName);
    }

    /// <summary>
    /// Verifies that every many-input kernel forbids inlining, so it is compiled on its own rather than into the
    /// dispatcher.
    /// </summary>
    /// <remarks>
    /// Under .NET 8's dynamic PGO the dispatcher inlined whichever kernel it found hot, ran out of inlining budget
    /// inside it, and left the kernel's own <c>G</c>, message loads and lane rotations as calls: BLAKE2b's 128-bit
    /// kernel ran at a third of its speed, below the scalar kernel. A kernel that cannot be inlined is compiled on its
    /// own, with its own budget, whatever the profile says.
    /// </remarks>
    [TestMethod]
    public void CompressChunks_WhenDeclared_ForEachKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        MethodInfo[] kernels =
        [
            typeof(Blake3Core.Vector128Kernel<>).GetMethod("HashMany", BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(Blake3Core.Vector256Kernel<>).GetMethod("HashMany", BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(Blake3Core.Vector512Kernel).GetMethod("HashMany", BindingFlags.NonPublic | BindingFlags.Static)!,
        ];

        foreach (MethodInfo kernel in kernels)
            Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining), $"{kernel.DeclaringType!.Name}.{kernel.Name}");
    }
}
