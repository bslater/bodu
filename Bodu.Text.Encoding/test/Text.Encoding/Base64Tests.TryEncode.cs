// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.TryEncode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{
    /// <summary>
    /// Verifies that <see cref="Base64.TryEncode" /> succeeds when the destination is large enough.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationLargeEnough_ShouldReturnTrueAndExactCharCount()
    {
        byte[] bytes = Ascii("foobar");
        char[] destination = new char[8];

        bool ok = Base64.TryEncode(bytes.AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, charsWritten);
        Assert.AreEqual("Zm9vYmFy", new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncode" /> returns <see langword="false" /> when the destination is too
    /// small.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationTooSmall_ShouldReturnFalseAndZeroCharsWritten()
    {
        byte[] bytes = Ascii("foobar");
        char[] destination = new char[1];

        bool ok = Base64.TryEncode(bytes.AsSpan(), destination, out int charsWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncode" /> on empty input returns <see langword="true" /> with
    /// <c>charsWritten = 0</c>.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenInputIsEmpty_ShouldReturnTrueAndZeroCharsWritten()
    {
        char[] destination = new char[4];

        bool ok = Base64.TryEncode(ReadOnlySpan<byte>.Empty, destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncode" /> honours <see cref="BaseFormattingOptions.OmitPadding" />.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenOmitPadding_ShouldWriteFewerCharacters()
    {
        byte[] bytes = Ascii("foo");
        char[] destination = new char[4];

        bool ok = Base64.TryEncode(bytes.AsSpan(), destination, out int charsWritten, Base64Variant.Standard, BaseFormattingOptions.OmitPadding);

        Assert.IsTrue(ok);
        Assert.AreEqual(4, charsWritten);
        Assert.AreEqual("Zm9v", new string(destination, 0, charsWritten));
    }
}
