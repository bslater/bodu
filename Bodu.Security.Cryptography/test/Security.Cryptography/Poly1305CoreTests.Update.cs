// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305CoreTests.Update.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    public void Update_WithRfc8439AppendixA3Vector_ShouldProduceTag(MessageDigestKnownAnswer vector)
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
}
