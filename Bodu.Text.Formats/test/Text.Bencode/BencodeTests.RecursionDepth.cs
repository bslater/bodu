// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.RecursionDepth.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{
    /// <summary>
    /// Builds a bencoded byte sequence consisting of <paramref name="depth" /> nested empty lists, the innermost
    /// containing no items: <c>l…le…e</c> with <paramref name="depth" /> opening <c>l</c> bytes and <paramref name="depth" /> closing <c>e</c> bytes.
    /// </summary>
    /// <param name="depth">The desired nesting depth.</param>
    /// <returns>The encoded byte buffer.</returns>
    private static byte[] BuildNestedLists(int depth)
    {
        byte[] bytes = new byte[depth * 2];
        for (var i = 0; i < depth; i++)
            bytes[i] = (byte)'l';

        for (var i = 0; i < depth; i++)
            bytes[depth + i] = (byte)'e';

        return bytes;
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> throws
    /// <see cref="BencodeFormatException" /> rather than allowing a <see cref="StackOverflowException" /> when the
    /// nesting depth exceeds the configured maximum. The fixed test depth (1024) is well above the default
    /// maximum (512) but well below the platform recursion budget so the test is deterministic.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNestingExceedsDefaultMaxDepth_ShouldThrowBencodeFormatException()
    {
        byte[] payload = BuildNestedLists(1024);

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = Bencode.Decode(payload);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte}, BencodeParseOptions)" /> honours an explicit
    /// <see cref="BencodeParseOptions.MaxDepth" /> setting and throws a controlled exception once the limit is exceeded.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNestingExceedsConfiguredMaxDepth_ShouldThrowBencodeFormatException()
    {
        BencodeParseOptions options = new() { MaxDepth = 4 };
        byte[] payload = BuildNestedLists(5);

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = Bencode.Decode(payload, options);
        });
    }

    /// <summary>
    /// Verifies that decoding a payload at exactly the configured <see cref="BencodeParseOptions.MaxDepth" /> succeeds.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNestingEqualsConfiguredMaxDepth_ShouldSucceed()
    {
        BencodeParseOptions options = new() { MaxDepth = 4 };
        byte[] payload = BuildNestedLists(4);

        BencodedValue value = Bencode.Decode(payload, options);

        Assert.IsInstanceOfType<BencodedList>(value);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.TryDecode(ReadOnlySpan{byte}, out BencodedValue?, out int)" /> returns
    /// <see langword="false" /> rather than propagating the depth-limit exception, consistent with its lenient
    /// failure contract.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenNestingExceedsDefaultMaxDepth_ShouldReturnFalse()
    {
        byte[] payload = BuildNestedLists(1024);

        bool success = Bencode.TryDecode(payload, out BencodedValue? value, out int bytesConsumed);

        Assert.IsFalse(success);
        Assert.IsNull(value);
        Assert.AreEqual(0, bytesConsumed);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeParseOptions.MaxDepth" /> rejects non-positive values.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetToZero_ShouldThrow()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new BencodeParseOptions { MaxDepth = 0 };
        });
    }
}
