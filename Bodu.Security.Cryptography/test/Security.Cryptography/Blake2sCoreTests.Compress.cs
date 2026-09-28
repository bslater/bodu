// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCoreTests.Compress.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Text;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public sealed partial class Blake2sCoreTests
{
    /// <summary>
    /// Verifies that each kernel reproduces RFC 7693, Appendix B's worked example, BLAKE2s-256("abc").
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx512")]
    public void Compress_ForEachKernel_ShouldMatchRfc7693Example(string kernel)
    {
        byte[] digest = Hash(ParseSupportedKernel(kernel), [], Encoding.ASCII.GetBytes("abc"), 32);

        Assert.AreEqual("508c5e8c327c14e2e1a72ba34eeb452f37458b209ed63a294d999b4c86675982", Convert.ToHexString(digest).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that each kernel reproduces every BLAKE2s entry of the official blake2-kat.json, unkeyed and keyed,
    /// over messages of 0 to 255 bytes.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx512")]
    public void Compress_ForEachKernel_ShouldMatchReferenceVectors(string kernel)
    {
        Blake2sCore.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (MessageDigestKnownAnswer vector in ReadReferenceVectors())
        {
            byte[] digest = Hash(kind, vector.Key ?? [], vector.Message, vector.Digest.Length);

            CollectionAssert.AreEqual(vector.Digest, digest, vector.Name);
        }
    }

    /// <summary>
    /// Verifies that each vector kernel leaves the same chaining state as the scalar kernel on seeded random states,
    /// blocks and counters, with and without the finalization flag.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx512")]
    public void Compress_ForEachKernel_ShouldMatchScalarKernel(string kernel)
    {
        Blake2sCore.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x2525);
        byte[] block = new byte[Blake2sCore.BlockBytes];

        for (int sample = 0; sample < 1000; sample++)
        {
            uint[] expected = new uint[Blake2sCore.StateWords];
            for (int i = 0; i < expected.Length; i++)
                expected[i] = (uint)random.Next() ^ ((uint)random.Next() << 31);

            uint[] actual = (uint[])expected.Clone();
            random.NextBytes(block);
            ulong counter = sample % 3 == 0 ? ulong.MaxValue - (ulong)sample : (ulong)random.NextInt64();
            bool last = sample % 2 == 0;

            Blake2sCore.Compress(Blake2sCore.KernelKind.Scalar, expected, block, counter, last);
            Blake2sCore.Compress(kind, actual, block, counter, last);

            CollectionAssert.AreEqual(expected, actual, $"sample {sample}");
        }
    }

    /// <summary>
    /// Verifies that every kernel forbids inlining, so it is compiled on its own rather than into the dispatcher.
    /// </summary>
    /// <remarks>
    /// Under .NET 8's dynamic PGO the dispatcher inlined whichever kernel it found hot, ran out of inlining budget
    /// inside it, and left the kernel's own <c>G</c>, message loads and lane rotations as calls: BLAKE2b's 128-bit
    /// kernel ran at a third of its speed, below the scalar kernel. A kernel that cannot be inlined is compiled on its
    /// own, with its own budget, whatever the profile says.
    /// </remarks>
    [TestMethod]
    public void Compress_ForEachKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        MethodInfo[] kernels =
        [
            typeof(Blake2sCore).GetMethod("CompressScalar", BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(Blake2sCore.Vector128Kernel<>).GetMethod("Compress", BindingFlags.NonPublic | BindingFlags.Static)!,
        ];

        foreach (MethodInfo kernel in kernels)
            Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining), $"{kernel.DeclaringType!.Name}.{kernel.Name}");
    }
}
