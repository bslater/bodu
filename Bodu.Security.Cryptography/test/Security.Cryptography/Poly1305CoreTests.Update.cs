// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305CoreTests.Update.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

public sealed partial class Poly1305CoreTests
{
    /// <summary>
    /// Verifies that the core produces the published tag for every RFC 8439 Appendix A.3 vector.
    /// </summary>
    /// <param name="vector">The Poly1305 reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Poly1305Tests.Poly1305Rfc8439KatData),
        typeof(Poly1305Tests),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Update_WhenGivenRfc8439AppendixA3Vector_ShouldProduceTag(MessageDigestKnownAnswer vector)
    {
        CollectionAssert.AreEqual(vector.Digest, ComputeTag(vector.Key!, vector.Message), vector.Name);
    }

    /// <summary>
    /// Verifies that carries are propagated and the top limb folded back in full, with the RFC 8439 Appendix A.3
    /// vectors that aim at them: a data limb of all ones taking a carry from below (#7), and a <c>5·H + L</c> reduction
    /// producing a 131-bit intermediate (#10) and final (#11) result.
    /// </summary>
    /// <param name="testName">The vector's number in Appendix A.3.</param>
    /// <param name="r">The key half <c>r</c>, in hexadecimal.</param>
    /// <param name="data">The message, in hexadecimal.</param>
    /// <param name="tag">The published tag, in hexadecimal.</param>
    [TestMethod]
    [DataRow(
        "#7",
        "01000000000000000000000000000000",
        "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFF11000000000000000000000000000000",
        "05000000000000000000000000000000")]
    [DataRow(
        "#10",
        "01000000000000000400000000000000",
        "E33594D7505E43B900000000000000003394D7505E4379CD01000000000000000000000000000000000000000000000001000000000000000000000000000000",
        "14000000000000005500000000000000")]
    [DataRow(
        "#11",
        "01000000000000000400000000000000",
        "E33594D7505E43B900000000000000003394D7505E4379CD010000000000000000000000000000000000000000000000",
        "13000000000000000000000000000000")]
    public void Update_WhenCarriesRunLongest_ShouldProduceTag(string testName, string r, string data, string tag)
    {
        byte[] key = [.. Convert.FromHexString(r), .. new byte[16]];

        CollectionAssert.AreEqual(Convert.FromHexString(tag), ComputeTag(key, Convert.FromHexString(data)), testName);
    }

    /// <summary>
    /// Verifies that the core matches the reference implementation on seeded random keys and messages of every length
    /// from empty to ten blocks, and on longer messages either side of a block boundary, each fed in a single call.
    /// </summary>
    [TestMethod]
    public void Update_WhenMessageIsSeededRandom_ShouldMatchReferenceImplementation()
    {
        var random = new Random(0x1305_0003);
        int[] lengths = [.. Enumerable.Range(0, 161), 255, 256, 257, 1000, 4099];

        foreach (int length in lengths)
        {
            byte[] key = NextBytes(random, Poly1305Core.KeyBytes);
            byte[] message = NextBytes(random, length);

            CollectionAssert.AreEqual(Poly1305Reference.ComputeTag(key, message), ComputeTag(key, message), $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that a message fed in seeded random pieces of up to 40 bytes, empty pieces included, produces the tag
    /// the reference implementation computes over the whole message, whichever block boundaries the pieces straddle.
    /// </summary>
    [TestMethod]
    public void Update_WhenMessageArrivesInPieces_ShouldMatchReferenceImplementation()
    {
        var random = new Random(0x1305_0004);

        for (int trial = 0; trial < 500; trial++)
        {
            byte[] key = NextBytes(random, Poly1305Core.KeyBytes);
            byte[] message = NextBytes(random, random.Next(300));

            CollectionAssert.AreEqual(
                Poly1305Reference.ComputeTag(key, message),
                ComputeTagInPieces(key, message, random, 40),
                $"trial {trial}, length {message.Length}");
        }
    }

    /// <summary>
    /// Verifies that the core matches the reference where the limbs and their products run largest: the key half
    /// <c>r</c> at its largest clamped value, with messages and the key half <c>s</c> of all ones or all zeros.
    /// </summary>
    /// <param name="messageFill">The value of every message byte.</param>
    /// <param name="sFill">The value of every byte of <c>s</c>.</param>
    [TestMethod]
    [DataRow((byte)0xFF, (byte)0xFF)]
    [DataRow((byte)0xFF, (byte)0x00)]
    [DataRow((byte)0x00, (byte)0xFF)]
    public void Update_WhenLimbsRunLargest_ShouldMatchReferenceImplementation(byte messageFill, byte sFill)
    {
        byte[] key = new byte[Poly1305Core.KeyBytes];
        Array.Fill(key, (byte)0xFF, 0, 16);
        Array.Fill(key, sFill, 16, 16);

        foreach (int length in Enumerable.Range(0, 97).Append(1024).Append(1031))
        {
            byte[] message = new byte[length];
            Array.Fill(message, messageFill);

            CollectionAssert.AreEqual(Poly1305Reference.ComputeTag(key, message), ComputeTag(key, message), $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that every kernel the processor supports produces the published tag for every RFC 8439 Appendix A.3
    /// vector; the vector kernels take the whole groups of blocks of the longest, the 375-byte vectors #2 and #3.
    /// </summary>
    /// <param name="vector">The Poly1305 reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Poly1305Tests.Poly1305Rfc8439KatData),
        typeof(Poly1305Tests),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Update_WhenGivenRfc8439AppendixA3Vector_ForEachKernel_ShouldProduceTag(MessageDigestKnownAnswer vector)
    {
        foreach (Poly1305Core.KernelKind kernel in Enum.GetValues<Poly1305Core.KernelKind>().Where(Poly1305Core.IsSupported))
            CollectionAssert.AreEqual(vector.Digest, ComputeTag(kernel, vector.Key!, vector.Message), $"{vector.Name}, {kernel}");
    }

    /// <summary>
    /// Verifies that each block loop, driven explicitly, matches the reference implementation on seeded random keys and
    /// messages of every length from empty to 1,100 bytes, fed in a single call: every number of whole groups of each
    /// kernel's lanes, with every number of blocks and bytes after them.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    [DataRow("Avx2Paired")]
    [DataRow("Avx512")]
    public void Update_WhenMessageIsSeededRandom_ForEachKernel_ShouldMatchReferenceImplementation(string kernel)
    {
        Poly1305Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x1305_0006);

        for (int length = 0; length <= 1100; length++)
        {
            byte[] key = NextBytes(random, Poly1305Core.KeyBytes);
            byte[] message = NextBytes(random, length);

            CollectionAssert.AreEqual(Poly1305Reference.ComputeTag(key, message), ComputeTag(kind, key, message), $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that each block loop, driven explicitly, matches the reference implementation on messages fed in seeded
    /// random pieces of up to 300 bytes, so that a kernel's runs start at every alignment the pieces leave, with the
    /// accumulator already holding the blocks before them.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    [DataRow("Avx2Paired")]
    [DataRow("Avx512")]
    public void Update_WhenMessageArrivesInPieces_ForEachKernel_ShouldMatchReferenceImplementation(string kernel)
    {
        Poly1305Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x1305_0007);

        for (int trial = 0; trial < 300; trial++)
        {
            byte[] key = NextBytes(random, Poly1305Core.KeyBytes);
            byte[] message = NextBytes(random, random.Next(1101));

            CollectionAssert.AreEqual(
                Poly1305Reference.ComputeTag(key, message),
                ComputeTagInPieces(kind, key, message, random, 300),
                $"trial {trial}, length {message.Length}");
        }
    }

    /// <summary>
    /// Verifies that each block loop, driven explicitly, matches the reference implementation where the limbs and their
    /// products run largest: the key half <c>r</c> at its largest clamped value, with messages and the key half
    /// <c>s</c> of all ones or all zeros, over lengths that fill many groups of every kernel's lanes.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="messageFill">The value of every message byte.</param>
    /// <param name="sFill">The value of every byte of <c>s</c>.</param>
    [TestMethod]
    [DataRow("Scalar", (byte)0xFF, (byte)0xFF)]
    [DataRow("Scalar", (byte)0xFF, (byte)0x00)]
    [DataRow("Scalar", (byte)0x00, (byte)0xFF)]
    [DataRow("Avx2", (byte)0xFF, (byte)0xFF)]
    [DataRow("Avx2", (byte)0xFF, (byte)0x00)]
    [DataRow("Avx2", (byte)0x00, (byte)0xFF)]
    [DataRow("Avx2Paired", (byte)0xFF, (byte)0xFF)]
    [DataRow("Avx2Paired", (byte)0xFF, (byte)0x00)]
    [DataRow("Avx2Paired", (byte)0x00, (byte)0xFF)]
    [DataRow("Avx512", (byte)0xFF, (byte)0xFF)]
    [DataRow("Avx512", (byte)0xFF, (byte)0x00)]
    [DataRow("Avx512", (byte)0x00, (byte)0xFF)]
    public void Update_WhenLimbsRunLargest_ForEachKernel_ShouldMatchReferenceImplementation(string kernel, byte messageFill, byte sFill)
    {
        Poly1305Core.KernelKind kind = ParseSupportedKernel(kernel);
        byte[] key = new byte[Poly1305Core.KeyBytes];
        Array.Fill(key, (byte)0xFF, 0, 16);
        Array.Fill(key, sFill, 16, 16);

        foreach (int length in Enumerable.Range(0, 301).Concat([1024, 1031, 4096, 4111, 65584]))
        {
            byte[] message = new byte[length];
            Array.Fill(message, messageFill);

            CollectionAssert.AreEqual(Poly1305Reference.ComputeTag(key, message), ComputeTag(kind, key, message), $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that each vector kernel matches the reference implementation when it starts from an accumulator the
    /// scalar loop has already taken to large limbs: one to eight blocks of all ones under the largest clamped
    /// <c>r</c>, followed by a run of whole groups of all ones, all zeros, or seeded random bytes.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Avx2")]
    [DataRow("Avx2Paired")]
    [DataRow("Avx512")]
    public void Update_WhenKernelFollowsScalarBlocks_ForEachKernel_ShouldMatchReferenceImplementation(string kernel)
    {
        Poly1305Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x1305_0008);
        byte[] key = new byte[Poly1305Core.KeyBytes];
        Array.Fill(key, (byte)0xFF, 0, 16);
        random.NextBytes(key.AsSpan(16));
        int groupBytes = Poly1305Core.LanesFor(kind) * Poly1305Core.BlockBytes;

        for (int prefixBlocks = 1; prefixBlocks <= 8; prefixBlocks++)
        {
            foreach (int fill in new[] { 0xFF, 0x00, -1 })
            {
                byte[] prefix = new byte[prefixBlocks * Poly1305Core.BlockBytes];
                Array.Fill(prefix, (byte)0xFF);
                byte[] body = new byte[5 * groupBytes];
                if (fill < 0)
                    random.NextBytes(body);
                else
                    Array.Fill(body, (byte)fill);

                Poly1305Core core = default;
                core.Initialize(key);
                core.Update(Poly1305Core.KernelKind.Scalar, prefix);
                core.Update(kind, body);

                byte[] actual = new byte[Poly1305Core.TagBytes];
                core.Finish(actual);

                CollectionAssert.AreEqual(Poly1305Reference.ComputeTag(key, [.. prefix, .. body]), actual, $"{prefixBlocks} prefix blocks, fill {fill}");
            }
        }
    }

    /// <summary>
    /// Verifies that dispatch produces the reference tag for runs of whole blocks either side of the lengths at which
    /// it moves from the scalar loop to AVX2, from AVX2's one-group loop to its paired loop, and from AVX2 to AVX-512,
    /// with and without a partial block after them.
    /// </summary>
    [TestMethod]
    public void Update_WhenRunLengthIsNearADispatchThreshold_ShouldMatchReferenceImplementation()
    {
        var random = new Random(0x1305_0009);

        foreach (int threshold in new[] { Poly1305Core.Avx2MinimumBytes, Poly1305Core.Avx2PairedMinimumBytes, Poly1305Core.Avx512MinimumBytes })
        {
            foreach (int delta in new[] { -129, -128, -64, -17, -16, -1, 0, 1, 15, 16, 17, 64, 127, 128, 129 })
            {
                byte[] key = NextBytes(random, Poly1305Core.KeyBytes);
                byte[] message = NextBytes(random, threshold + delta);

                CollectionAssert.AreEqual(Poly1305Reference.ComputeTag(key, message), ComputeTag(key, message), $"length {message.Length}");
            }
        }
    }

    /// <summary>
    /// Verifies that the scalar loop, the dispatch to the vector kernels and every kernel forbid inlining, so that each
    /// is compiled on its own rather than into the methods that absorb a message.
    /// </summary>
    /// <remarks>
    /// Under .NET 10's dynamic PGO, <c>Poly1305.HashCore</c> inlined the scalar loop, ran out of inlining budget inside
    /// it, and left the limb multiplications as calls: the scalar path lost about a tenth of its speed. The AEADs'
    /// framing methods, inlining the dispatch with its three kernel calls, ran out of budget the same way and left small
    /// helpers as calls on every message. A method that cannot be inlined is compiled on its own, with its own budget,
    /// whatever the profile says.
    /// </remarks>
    [TestMethod]
    public void Update_WhenDeclared_ForEachLoop_ShouldForbidInliningIntoItsCallers()
    {
        const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;
        const BindingFlags Static = BindingFlags.NonPublic | BindingFlags.Static;
        Type core = typeof(Poly1305Core);
        Type? vector256 = core.GetNestedType("Vector256Kernel", BindingFlags.NonPublic);
        Type? vector512 = core.GetNestedType("Vector512Kernel", BindingFlags.NonPublic);

        (string Name, MethodInfo? Method)[] loops =
        [
            ("Poly1305Core.Blocks", core.GetMethod("Blocks", Instance)),
            ("Poly1305Core.KernelBlocks", core.GetMethod("KernelBlocks", Instance)),
            ("Vector256Kernel.Blocks", vector256?.GetMethod("Blocks", Static)),
            ("Vector256Kernel.BlocksPaired", vector256?.GetMethod("BlocksPaired", Static)),
            ("Vector512Kernel.Blocks", vector512?.GetMethod("Blocks", Static)),
        ];

        foreach ((string name, MethodInfo? method) in loops)
        {
            Assert.IsNotNull(method, $"{name} is not declared.");
            Assert.IsTrue(method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining), name);
        }
    }
}
