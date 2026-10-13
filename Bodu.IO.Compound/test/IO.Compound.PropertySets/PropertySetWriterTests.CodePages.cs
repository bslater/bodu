// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetWriterTests.CodePages.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies that a property set round-trips its text at every code page the writer and reader resolve an encoding
/// for, including the multi-byte ones.
/// </summary>
public partial class PropertySetWriterTests
{
    /// <summary>
    /// Verifies that a section declaring a code page above the signed 16-bit range round-trips. The code page is
    /// emitted as a signed <c>VT_I2</c>, so 65001 is written as -535 and must be recovered as an unsigned value; any
    /// code page below 32768 would not exercise that narrowing.
    /// </summary>
    [TestMethod]
    public void Write_WhenCodePageExceedsSignedRange_ShouldRoundTripCodePageAndText()
    {
        OlePropertySection parsed = RoundTripSection(65001, OlePropertyType.AnsiString, "café ☕");

        Assert.AreEqual(65001, parsed.CodePage);
        Assert.AreEqual("café ☕", parsed[20]?.AsString());
    }

    /// <summary>
    /// Verifies that a big-endian UTF-16 section round-trips its text. The writer and reader resolve the same
    /// encoding from the code page, so the pairing is self-consistent even though this code page is rare in the wild.
    /// </summary>
    [TestMethod]
    public void Write_WhenCodePageIsBigEndianUnicode_ShouldRoundTripText()
    {
        OlePropertySection parsed = RoundTripSection(1201, OlePropertyType.AnsiString, "abc");

        Assert.AreEqual(1201, parsed.CodePage);
        Assert.AreEqual("abc", parsed[20]?.AsString());
    }

    /// <summary>
    /// Verifies that a little-endian UTF-16 section round-trips its text and its property-name dictionary, which
    /// uses character-count prefixes and four-byte alignment rather than the byte-count layout every other code page
    /// uses.
    /// </summary>
    [TestMethod]
    public void Write_WhenCodePageIsUnicode_ShouldRoundTripTextAndDictionary()
    {
        var section = new OlePropertySection(TestFormatId, 1200);
        section.Set(20, OlePropertyValue.Create("naïve", OlePropertyType.AnsiString));
        section.SetName(20, "Réviseur");

        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1200);
        set.AddSection(section);

        OlePropertySection parsed = OlePropertySet.Parse(set.ToArray()).Sections[0];

        Assert.AreEqual(1200, parsed.CodePage);
        Assert.AreEqual("naïve", parsed[20]?.AsString());
        Assert.AreEqual("Réviseur", parsed.GetNamedProperties().Keys.Single());
    }

    /// <summary>
    /// Verifies that a section declaring code page 1252 encodes the characters Windows-1252 has outside Latin-1 with
    /// their Windows-1252 bytes (U+2019 as 0x92) and reads them back, rather than replacing them with question marks.
    /// </summary>
    [TestMethod]
    public void Write_WhenCodePageIs1252_ShouldEncodeWindows1252Characters()
    {
        var section = new OlePropertySection(TestFormatId, 1252);
        section.Set(20, OlePropertyValue.Create("it\u2019s \u20AC5", OlePropertyType.AnsiString));

        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1252);
        set.AddSection(section);
        byte[] bytes = set.ToArray();
        ReadOnlySpan<byte> windows1252 = [0x69, 0x74, 0x92, 0x73, 0x20, 0x80, 0x35, 0x00];

        Assert.IsTrue(bytes.AsSpan().IndexOf(windows1252) >= 0);
        Assert.AreEqual("it\u2019s \u20AC5", OlePropertySet.Parse(bytes).Sections[0][20]?.AsString());
    }

    /// <summary>
    /// Verifies that a section declaring the double-byte code page 932 (Shift-JIS) round-trips its ANSI text and its
    /// property-name dictionary.
    /// </summary>
    [TestMethod]
    public void Write_WhenCodePageIsShiftJis_ShouldRoundTripTextAndDictionary()
    {
        var section = new OlePropertySection(TestFormatId, 932);
        section.Set(20, OlePropertyValue.Create("\u65E5\u672C\u8A9E", OlePropertyType.AnsiString));
        section.SetName(20, "\u540D\u524D");

        var set = new OlePropertySet(TestFormatId, Guid.Empty, 932);
        set.AddSection(section);

        OlePropertySection parsed = OlePropertySet.Parse(set.ToArray()).Sections[0];

        Assert.AreEqual(932, parsed.CodePage);
        Assert.AreEqual("\u65E5\u672C\u8A9E", parsed[20]?.AsString());
        Assert.AreEqual("\u540D\u524D", parsed.GetNamedProperties().Keys.Single());
    }

    /// <summary>
    /// Round-trips a single-property section declared at the supplied code page.
    /// </summary>
    /// <param name="codePage">The section code page.</param>
    /// <param name="type">The property type.</param>
    /// <param name="text">The property text.</param>
    /// <returns>The re-parsed section.</returns>
    private static OlePropertySection RoundTripSection(int codePage, OlePropertyType type, string text)
    {
        var section = new OlePropertySection(TestFormatId, codePage);
        section.Set(20, OlePropertyValue.Create(text, type));

        var set = new OlePropertySet(TestFormatId, Guid.Empty, codePage);
        set.AddSection(section);

        return OlePropertySet.Parse(set.ToArray()).Sections[0];
    }
}
