// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305CoreTests.Finish.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Poly1305CoreTests
{
    /// <summary>
    /// Verifies that the final reduction and the addition of <c>s</c> are exact at their edges, with the RFC 8439
    /// Appendix A.3 vectors that aim at them: a partially reduced result that is not fully reduced (#5), <c>s</c>
    /// overflowing modulo 2^128 (#6), and a polynomial part of exactly 2^130 − 5 (#8) and 2^130 − 6 (#9).
    /// </summary>
    /// <param name="testName">The vector's number in Appendix A.3.</param>
    /// <param name="key">The key, <c>r</c> then <c>s</c>, in hexadecimal.</param>
    /// <param name="data">The message, in hexadecimal.</param>
    /// <param name="tag">The published tag, in hexadecimal.</param>
    [TestMethod]
    [DataRow(
        "#5",
        "0200000000000000000000000000000000000000000000000000000000000000",
        "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",
        "03000000000000000000000000000000")]
    [DataRow(
        "#6",
        "02000000000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",
        "02000000000000000000000000000000",
        "03000000000000000000000000000000")]
    [DataRow(
        "#8",
        "0100000000000000000000000000000000000000000000000000000000000000",
        "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFBFEFEFEFEFEFEFEFEFEFEFEFEFEFEFE01010101010101010101010101010101",
        "00000000000000000000000000000000")]
    [DataRow(
        "#9",
        "0200000000000000000000000000000000000000000000000000000000000000",
        "FDFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",
        "FAFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF")]
    public void Finish_WhenReductionIsAtItsEdge_ShouldProduceTag(string testName, string key, string data, string tag)
    {
        CollectionAssert.AreEqual(Convert.FromHexString(tag), ComputeTag(Convert.FromHexString(key), Convert.FromHexString(data)), testName);
    }

    /// <summary>
    /// Verifies that a partial last block of every length from 1 to 15 bytes is padded with a single 1 byte and zeros
    /// and absorbed without the 2^128 bit, as the reference implementation does.
    /// </summary>
    [TestMethod]
    public void Finish_WhenLastBlockIsPartial_ShouldMatchReferenceImplementation()
    {
        var random = new Random(0x1305_0006);

        for (int length = 1; length < Poly1305Core.BlockBytes; length++)
        {
            byte[] key = NextBytes(random, Poly1305Core.KeyBytes);
            byte[] message = NextBytes(random, (2 * Poly1305Core.BlockBytes) + length);

            CollectionAssert.AreEqual(Poly1305Reference.ComputeTag(key, message), ComputeTag(key, message), $"partial block of {length} bytes");
        }
    }

    /// <summary>
    /// Verifies that a tag span shorter than 16 bytes is rejected with <see cref="ArgumentOutOfRangeException" /> naming
    /// the parameter.
    /// </summary>
    /// <param name="length">The rejected tag length.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(15)]
    public void Finish_WhenTagIsShorterThan16Bytes_ShouldThrowArgumentOutOfRangeException(int length)
    {
        byte[] tag = new byte[length];
        Poly1305Core core = default;
        core.Initialize(new byte[Poly1305Core.KeyBytes]);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            core.Finish(tag);
        });

        Assert.AreEqual("tag", ex.ParamName);
    }

    /// <summary>
    /// Verifies that finishing into a span longer than 16 bytes writes the tag to its first 16 bytes and leaves the
    /// rest untouched.
    /// </summary>
    [TestMethod]
    public void Finish_WhenTagSpanIsLonger_ShouldWriteOnlyTheFirst16Bytes()
    {
        var random = new Random(0x1305_0007);
        byte[] key = NextBytes(random, Poly1305Core.KeyBytes);
        byte[] message = NextBytes(random, 50);
        byte[] output = new byte[Poly1305Core.TagBytes + 8];
        Array.Fill(output, (byte)0xA5);

        Poly1305Core core = default;
        core.Initialize(key);
        core.Update(message);
        core.Finish(output);

        CollectionAssert.AreEqual(Poly1305Reference.ComputeTag(key, message), output[..Poly1305Core.TagBytes]);
        Assert.IsTrue(output.AsSpan(Poly1305Core.TagBytes).IndexOfAnyExcept((byte)0xA5) < 0, "Finish must not write past the tag.");
    }

    /// <summary>
    /// Verifies that finishing clears the whole core - key schedule, <c>s</c>, accumulator and a held partial block -
    /// once the tag is written.
    /// </summary>
    [TestMethod]
    public void Finish_WhenTagIsWritten_ShouldClearTheCore()
    {
        var random = new Random(0x1305_0008);
        Poly1305Core core = default;
        core.Initialize(NextBytes(random, Poly1305Core.KeyBytes));
        core.Update(NextBytes(random, 23));
        Assert.IsFalse(IsCleared(ref core), "Precondition: the core should hold key and message state.");

        core.Finish(new byte[Poly1305Core.TagBytes]);

        Assert.IsTrue(IsCleared(ref core), "Finish must clear the core.");
    }
}
