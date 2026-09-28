// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3CoreTests.CompressParents.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Blake3CoreTests
{
    /// <summary>
    /// Verifies that each kernel compresses every parent of a run exactly as compressing the parents one at a time
    /// does, for every run length up to two groups of sixteen lanes and a remainder.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void CompressParents_ForEachKernel_ShouldMatchParentByParentCompression(string kernel)
    {
        Blake3Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x3D3D);

        for (int count = 0; count <= 40; count++)
        {
            byte[] children = new byte[count * Blake3Core.BlockBytes];
            random.NextBytes(children);
            uint[] key = RandomKey(random);
            uint flags = (uint)random.Next(0, 128);

            byte[] expected = ExpectedParents(key, flags, children);
            byte[] actual = new byte[expected.Length];
            Blake3Core.CompressParents(kind, children, key, flags, actual);

            CollectionAssert.AreEqual(expected, actual, $"{count} parents");
        }
    }

    /// <summary>
    /// Verifies that each kernel reduces a level of parents in place, its chaining values overlaying the blocks they
    /// came from, exactly as it does into a separate buffer.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void CompressParents_WhenChainingValuesOverlayChildren_ShouldMatchSeparateOutput(string kernel)
    {
        Blake3Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x3D3E);

        for (int count = 1; count <= 40; count++)
        {
            byte[] children = new byte[count * Blake3Core.BlockBytes];
            random.NextBytes(children);
            uint[] key = RandomKey(random);

            byte[] expected = ExpectedParents(key, 0, children);
            Blake3Core.CompressParents(kind, children, key, 0, children);

            CollectionAssert.AreEqual(expected, children[..expected.Length], $"{count} parents");
        }
    }

    /// <summary>
    /// Verifies that input holding a partial parent block is rejected with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void CompressParents_WhenChildrenIsNotWholeBlocks_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Blake3Core.CompressParents(Blake3Core.KernelKind.Auto, new byte[Blake3Core.BlockBytes - 1], Blake3Core.InitializationVector.ToArray(), 0, new byte[Blake3Core.ChainingValueBytes]);
        });

        Assert.AreEqual("children", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a key shorter than eight words is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void CompressParents_WhenKeyIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.CompressParents(Blake3Core.KernelKind.Auto, new byte[Blake3Core.BlockBytes], new uint[7], 0, new byte[Blake3Core.ChainingValueBytes]);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that room for fewer chaining values than parents is rejected with
    /// <see cref="ArgumentOutOfRangeException" /> rather than written past.
    /// </summary>
    [TestMethod]
    public void CompressParents_WhenChainingValuesIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.CompressParents(Blake3Core.KernelKind.Auto, new byte[2 * Blake3Core.BlockBytes], Blake3Core.InitializationVector.ToArray(), 0, new byte[(2 * Blake3Core.ChainingValueBytes) - 1]);
        });

        Assert.AreEqual("chainingValues", ex.ParamName);
    }

    /// <summary>
    /// Compresses parents one at a time with the scalar kernel.
    /// </summary>
    /// <param name="key">The eight-word key.</param>
    /// <param name="flags">The mode's flags.</param>
    /// <param name="children">The parents' 64-byte blocks.</param>
    /// <returns>The parents' encoded chaining values.</returns>
    private static byte[] ExpectedParents(uint[] key, uint flags, byte[] children)
    {
        int count = children.Length / Blake3Core.BlockBytes;
        byte[] expected = new byte[count * Blake3Core.ChainingValueBytes];
        for (int parent = 0; parent < count; parent++)
        {
            uint[] chainingValue = (uint[])key.Clone();
            Blake3Core.Compress(Blake3Core.KernelKind.Scalar, chainingValue, children.AsSpan(parent * Blake3Core.BlockBytes, Blake3Core.BlockBytes), 0, Blake3Core.BlockBytes, flags | Blake3Core.Parent);
            Encode(chainingValue).CopyTo(expected, parent * Blake3Core.ChainingValueBytes);
        }

        return expected;
    }
}
