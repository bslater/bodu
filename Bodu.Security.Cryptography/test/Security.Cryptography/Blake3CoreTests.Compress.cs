// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3CoreTests.Compress.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public sealed partial class Blake3CoreTests
{
    /// <summary>
    /// Verifies that each kernel reproduces every unkeyed hash of the official test_vectors.json, over inputs from
    /// empty to 100 chunks.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void Compress_WhenHashingReferenceInputs_ForEachKernel_ShouldMatchReferenceHashVectors(string kernel)
    {
        Blake3Core.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (MessageDigestKnownAnswer vector in ReadReferenceVectors(Blake3KatReader.Read))
            CollectionAssert.AreEqual(vector.Digest, Hash(kind, Blake3Core.InitializationVector, 0, vector.Message), vector.Name);
    }

    /// <summary>
    /// Verifies that each kernel reproduces every keyed hash of the official test_vectors.json, which starts every
    /// chunk and parent from the key rather than the IV.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void Compress_WhenHashingReferenceInputsUnderKey_ForEachKernel_ShouldMatchReferenceKeyedHashVectors(string kernel)
    {
        Blake3Core.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (MessageDigestKnownAnswer vector in ReadReferenceVectors(Blake3KatReader.ReadKeyedHash))
            CollectionAssert.AreEqual(vector.Digest, KeyedHash(kind, vector.Key, vector.Message), vector.Name);
    }

    /// <summary>
    /// Verifies that each kernel reproduces every derived key of the official test_vectors.json, which hashes the
    /// context string and the key material under their own flags.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void Compress_WhenDerivingReferenceKeys_ForEachKernel_ShouldMatchReferenceDeriveKeyVectors(string kernel)
    {
        Blake3Core.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (MessageDigestKnownAnswer vector in ReadReferenceVectors(Blake3KatReader.ReadDeriveKey))
            CollectionAssert.AreEqual(vector.Digest, DeriveKey(kind, vector.Key, vector.Message), vector.Name);
    }

    /// <summary>
    /// Verifies that each vector kernel leaves the same chaining value as the scalar kernel on seeded random chaining
    /// values, blocks, counters, block lengths and flags.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void Compress_WhenInputsAreRandom_ForEachKernel_ShouldMatchScalarKernel(string kernel)
    {
        Blake3Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x3333);
        byte[] block = new byte[Blake3Core.BlockBytes];

        for (int sample = 0; sample < 1000; sample++)
        {
            uint[] expected = new uint[Blake3Core.ChainingValueWords];
            for (int i = 0; i < expected.Length; i++)
                expected[i] = (uint)random.Next() ^ ((uint)random.Next() << 31);

            uint[] actual = (uint[])expected.Clone();
            random.NextBytes(block);
            ulong counter = sample % 3 == 0 ? ulong.MaxValue - (ulong)sample : (ulong)random.NextInt64();
            uint blockLength = (uint)random.Next(0, Blake3Core.BlockBytes + 1);
            uint flags = (uint)random.Next(0, 128);

            Blake3Core.Compress(Blake3Core.KernelKind.Scalar, expected, block, counter, blockLength, flags);
            Blake3Core.Compress(kind, actual, block, counter, blockLength, flags);

            CollectionAssert.AreEqual(expected, actual, $"sample {sample}");
        }
    }

    /// <summary>
    /// Verifies that dispatch compresses a block exactly as the kernel it selects for a single block does.
    /// </summary>
    [TestMethod]
    public void Compress_WhenKernelIsAuto_ShouldMatchSelectedKernel()
    {
        byte[] block = new byte[Blake3Core.BlockBytes];
        new Random(0x3334).NextBytes(block);
        uint[] expected = Blake3Core.InitializationVector.ToArray();
        uint[] actual = Blake3Core.InitializationVector.ToArray();

        Blake3Core.Compress(Blake3Core.SelectSingleBlockKernel(), expected, block, 7, 64, Blake3Core.ChunkStart);
        Blake3Core.Compress(actual, block, 7, 64, Blake3Core.ChunkStart);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a chaining value shorter than eight words is rejected with
    /// <see cref="ArgumentOutOfRangeException" /> rather than read past its end.
    /// </summary>
    [TestMethod]
    public void Compress_WhenChainingValueIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.Compress(new uint[Blake3Core.ChainingValueWords - 1], new byte[Blake3Core.BlockBytes], 0, 0, 0);
        });

        Assert.AreEqual("chainingValue", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a block shorter than 64 bytes is rejected with <see cref="ArgumentOutOfRangeException" /> rather
    /// than read past its end.
    /// </summary>
    [TestMethod]
    public void Compress_WhenBlockIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.Compress(new uint[Blake3Core.ChainingValueWords], new byte[Blake3Core.BlockBytes - 1], 0, 0, 0);
        });

        Assert.AreEqual("block", ex.ParamName);
    }

    /// <summary>
    /// Verifies that every one-block kernel forbids inlining, so it is compiled on its own rather than into the
    /// dispatcher.
    /// </summary>
    /// <remarks>
    /// Under .NET 8's dynamic PGO the dispatcher inlined whichever kernel it found hot, ran out of inlining budget
    /// inside it, and left the kernel's own <c>G</c>, message loads and lane rotations as calls: BLAKE2b's 128-bit
    /// kernel ran at a third of its speed, below the scalar kernel. A kernel that cannot be inlined is compiled on its
    /// own, with its own budget, whatever the profile says.
    /// </remarks>
    [TestMethod]
    public void Compress_WhenDeclared_ForEachKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        MethodInfo[] kernels =
        [
            typeof(Blake3Core).GetMethod("CompressScalar", BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(Blake3Core.Vector128Kernel<>).GetMethod("Compress", BindingFlags.NonPublic | BindingFlags.Static)!,
        ];

        foreach (MethodInfo kernel in kernels)
            Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining), $"{kernel.DeclaringType!.Name}.{kernel.Name}");
    }
}
