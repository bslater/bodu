// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedStringTests.GetUtf8String.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

public sealed partial class BencodedStringTests
{

    /// <summary>
    /// Verifies that <see cref="BencodedString.GetUtf8String" /> round-trips accented characters.
    /// </summary>
    [TestMethod]
    public void GetUtf8String_WhenAccentedCharacter_ShouldReturnOriginalText()
    {
        BencodedString value = BencodedString.FromUtf8("héllo");

        Assert.AreEqual("héllo", value.GetUtf8String());
    }
    /// <summary>
    /// Verifies that <see cref="BencodedString.GetUtf8String" /> round-trips ASCII content authored via
    /// <see cref="BencodedString.FromUtf8(string)" />.
    /// </summary>
    [TestMethod]
    public void GetUtf8String_WhenAsciiContent_ShouldReturnOriginalText()
    {
        BencodedString value = BencodedString.FromUtf8("spam");

        Assert.AreEqual("spam", value.GetUtf8String());
    }

    /// <summary>
    /// Verifies that <see cref="BencodedString.GetUtf8String" /> round-trips empty content.
    /// </summary>
    [TestMethod]
    public void GetUtf8String_WhenEmptyContent_ShouldReturnEmptyString()
    {
        BencodedString value = BencodedString.FromUtf8(string.Empty);

        Assert.AreEqual(string.Empty, value.GetUtf8String());
    }

    /// <summary>
    /// Verifies that <see cref="BencodedString.GetUtf8String" /> round-trips a surrogate-pair (astral-plane)
    /// character.
    /// </summary>
    [TestMethod]
    public void GetUtf8String_WhenSurrogatePair_ShouldReturnOriginalText()
    {
        BencodedString value = BencodedString.FromUtf8("😀");

        Assert.AreEqual("😀", value.GetUtf8String());
    }

}
