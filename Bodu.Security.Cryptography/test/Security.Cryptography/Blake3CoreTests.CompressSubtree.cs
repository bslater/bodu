// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3CoreTests.CompressSubtree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Blake3CoreTests
{
    /// <summary>
    /// Verifies that each kernel computes the chaining value the specification's tree gives every complete subtree
    /// from one chunk to 256, across the 64-chunk batch the subtree code splits larger subtrees into.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void CompressSubtree_WhenSubtreeSpansOneTo256Chunks_ForEachKernel_ShouldMatchSpecificationTree(string kernel)
    {
        Blake3Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x3E3E);

        for (int chunks = 1; chunks <= 256; chunks *= 2)
        {
            byte[] input = new byte[chunks * Blake3Core.ChunkBytes];
            random.NextBytes(input);
            uint[] key = RandomKey(random);
            uint flags = (uint)random.Next(0, 128) & (Blake3Core.KeyedHash | Blake3Core.DeriveKeyContext | Blake3Core.DeriveKeyMaterial);
            ulong counter = (ulong)chunks * (ulong)random.Next(0, 1 << 20);

            uint[] expected = ReferenceSubtree(key, flags, input, counter);
            uint[] actual = new uint[Blake3Core.ChainingValueWords];
            Blake3Core.CompressSubtree(kind, input, key, counter, flags, actual);

            CollectionAssert.AreEqual(expected, actual, $"{chunks} chunks from counter {counter}");
        }
    }

    /// <summary>
    /// Verifies that dispatch computes a subtree's chaining value exactly as the kernel it selects does.
    /// </summary>
    [TestMethod]
    public void CompressSubtree_WhenKernelIsAuto_ShouldMatchSelectedKernel()
    {
        byte[] input = new byte[128 * Blake3Core.ChunkBytes];
        new Random(0x3E3F).NextBytes(input);
        uint[] expected = new uint[Blake3Core.ChainingValueWords];
        uint[] actual = new uint[Blake3Core.ChainingValueWords];

        Blake3Core.CompressSubtree(Blake3Core.SelectKernel(), input, Blake3Core.InitializationVector, 256, 0, expected);
        Blake3Core.CompressSubtree(input, Blake3Core.InitializationVector, 256, 0, actual);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that input that is not a power of two of whole chunks - empty, a partial chunk, or three chunks - is
    /// rejected with <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="length">The input's length, in bytes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1023)]
    [DataRow(1025)]
    [DataRow(3 * 1024)]
    public void CompressSubtree_WhenInputIsNotAPowerOfTwoOfChunks_ShouldThrowArgumentException(int length)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Blake3Core.CompressSubtree(new byte[length], Blake3Core.InitializationVector.ToArray(), 0, 0, new uint[Blake3Core.ChainingValueWords]);
        });

        Assert.AreEqual("input", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a key shorter than eight words is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void CompressSubtree_WhenKeyIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.CompressSubtree(new byte[Blake3Core.ChunkBytes], new uint[7], 0, 0, new uint[Blake3Core.ChainingValueWords]);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a chaining value shorter than eight words is rejected with
    /// <see cref="ArgumentOutOfRangeException" /> rather than written past.
    /// </summary>
    [TestMethod]
    public void CompressSubtree_WhenChainingValueIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.CompressSubtree(new byte[Blake3Core.ChunkBytes], Blake3Core.InitializationVector.ToArray(), 0, 0, new uint[7]);
        });

        Assert.AreEqual("chainingValue", ex.ParamName);
    }
}
