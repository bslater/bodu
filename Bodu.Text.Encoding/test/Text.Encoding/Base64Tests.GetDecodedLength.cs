// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.GetDecodedLength.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{

    /// <summary>
    /// Verifies that <see cref="Base64.GetDecodedLength" /> rejects invalid characters with
    /// <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenInvalidCharacter_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.GetDecodedLength("Zm@v".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.GetDecodedLength" /> with the MIME variant implicitly strips whitespace.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenMimeVariant_ShouldImplicitlyStripWhitespace()
    {
        int actual = Base64.GetDecodedLength("Zm9v\r\nYmFy".AsSpan(), Base64Variant.Mime);

        Assert.AreEqual(6, actual);
    }
    /// <summary>
    /// Verifies that <see cref="Base64.GetDecodedLength" /> returns the exact byte count for a padded input.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenPaddedInput_ShouldReturnExactByteCount()
    {
        int actual = Base64.GetDecodedLength("Zm9vYg==".AsSpan());

        Assert.AreEqual(4, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.GetDecodedLength" /> handles unpadded input.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenUnpaddedInput_ShouldExcludePaddingFromCount()
    {
        int actual = Base64.GetDecodedLength("Zm9vYmFy".AsSpan());

        Assert.AreEqual(6, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.GetEncodedLength(int)" /> (no-options overload) returns the Standard variant
    /// length.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WithoutOptionsOverload_ShouldReturnStandardLength()
    {
        Assert.AreEqual(8, Base64.GetEncodedLength(6));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.GetEncodedLength(int, Base64Variant)" /> reflects the variant default padding
    /// convention.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WithVariantOverload_ShouldReflectVariantDefaults()
    {
        // 4 bytes -> 6 data chars, Standard pads to 8; UrlSafe omits padding.
        int standard = Base64.GetEncodedLength(4, Base64Variant.Standard);
        int urlSafe = Base64.GetEncodedLength(4, Base64Variant.UrlSafe);

        Assert.AreEqual(8, standard);
        Assert.AreEqual(6, urlSafe);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryGetDecodedLength" /> returns <see langword="false" /> for invalid input.
    /// </summary>
    [TestMethod]
    public void TryGetDecodedLength_WhenInvalid_ShouldReturnFalseAndZero()
    {
        bool ok = Base64.TryGetDecodedLength("Zm@v".AsSpan(), out int byteCount);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, byteCount);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryGetDecodedLength" /> returns <see langword="true" /> for a valid input.
    /// </summary>
    [TestMethod]
    public void TryGetDecodedLength_WhenValid_ShouldReturnTrueAndCount()
    {
        bool ok = Base64.TryGetDecodedLength("Zm9vYmFy".AsSpan(), out int byteCount);

        Assert.IsTrue(ok);
        Assert.AreEqual(6, byteCount);
    }

}
