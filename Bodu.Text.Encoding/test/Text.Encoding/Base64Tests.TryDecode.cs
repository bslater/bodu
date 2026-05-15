// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.TryDecode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{
    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> recovers the original bytes for a valid Standard input.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenStandardValidInput_ShouldReturnTrueAndExpectedBytes()
    {
        byte[] destination = new byte[6];

        bool ok = Base64.TryDecode("Zm9vYmFy".AsSpan(), destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(6, bytesWritten);
        CollectionAssert.AreEqual(Ascii("foobar"), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> returns <see langword="false" /> on invalid input rather than
    /// throwing.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenInvalidCharacters_ShouldReturnFalseAndZeroBytesWritten()
    {
        byte[] destination = new byte[6];

        bool ok = Base64.TryDecode("Zm9vYm!y".AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> returns <see langword="false" /> on a truncated input in strict
    /// mode.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenStrictAndPaddingOmitted_ShouldReturnFalseAndZeroBytesWritten()
    {
        byte[] destination = new byte[5];

        bool ok = Base64.TryDecode("Zm9vYmE".AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> with <see cref="BaseFormatStyles.AllowMissingPadding" /> accepts
    /// unpadded input.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenAllowMissingPadding_ShouldReturnTrueAndExpectedBytes()
    {
        byte[] destination = new byte[5];

        bool ok = Base64.TryDecode(
            "Zm9vYmE".AsSpan(),
            destination,
            out int bytesWritten,
            Base64Variant.Standard,
            BaseFormatStyles.AllowMissingPadding);

        Assert.IsTrue(ok);
        Assert.AreEqual(5, bytesWritten);
        CollectionAssert.AreEqual(Ascii("fooba"), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> for empty input returns <see langword="true" /> with
    /// <c>bytesWritten = 0</c>.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenInputIsEmpty_ShouldReturnTrueAndZeroBytesWritten()
    {
        byte[] destination = new byte[6];

        bool ok = Base64.TryDecode(ReadOnlySpan<char>.Empty, destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, bytesWritten);
    }
}
