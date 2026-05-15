// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.Decode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85Tests
{
    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> with Ascii85 expands the
    /// <c>z</c> shortcut into four zero bytes.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAscii85ZShortcut_ShouldExpandToFourZeroBytes()
    {
        byte[] actual = Base85.Decode("z");

        CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0 }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> rejects characters outside
    /// the Ascii85 alphabet (a control character below ASCII <c>'!'</c>).
    /// </summary>
    [TestMethod]
    public void Decode_WhenAscii85InvalidCharacter_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("\x01\x02\x03\x04\x05");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> rejects characters outside
    /// the variant alphabet (using a definitely-invalid character).
    /// </summary>
    [TestMethod]
    public void Decode_WhenCharBelowAlphabetRange_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("\x01\x02\x03\x04\x05");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> rejects misplaced
    /// <c>z</c> shortcut characters.
    /// </summary>
    [TestMethod]
    public void Decode_WhenZShortcutInMiddleOfGroup_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("9jz"); // 'z' appearing after non-zero data in a group
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> with Z85 rejects
    /// non-aligned input length.
    /// </summary>
    [TestMethod]
    public void Decode_WhenZ85NonAlignedInput_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("HelloW", Base85Variant.Z85);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> tolerates whitespace.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespace_ShouldStripAndDecode()
    {
        // First encode some bytes
        byte[] original = Ascii("Hello world!");
        string encoded = Base85.Encode(original);

        // Inject whitespace
        string decorated = string.Concat(encoded.Select((c, i) => i % 2 == 0 ? c.ToString() + " " : c.ToString()));

        byte[] decoded = Base85.Decode(decorated, Base85Variant.Ascii85, BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(original, decoded);
    }
}
