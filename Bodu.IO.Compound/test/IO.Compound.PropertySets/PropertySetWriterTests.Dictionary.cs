// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetWriterTests.Dictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

public partial class PropertySetWriterTests
{
    /// <summary>
    /// Round-trips a section carrying a name dictionary at the given code page and returns the parsed section.
    /// </summary>
    /// <param name="codePage">The section code page.</param>
    /// <returns>The parsed section.</returns>
    private static OlePropertySection RoundTripDictionary(int codePage)
    {
        var section = new OlePropertySection(TestFormatId, codePage);
        section.Set(2, OlePropertyValue.Create(7));
        section.Set(3, OlePropertyValue.Create(11));
        section.SetName(2, "First Custom");
        section.SetName(3, "Odd");
        var set = new OlePropertySet(TestFormatId, Guid.Empty, codePage);
        set.AddSection(section);

        return OlePropertySet.Parse(set.ToArray()).Sections[0];
    }

    /// <summary>
    /// Verifies that a name dictionary in an ANSI (code page 1252) section round-trips its property names.
    /// </summary>
    [TestMethod]
    public void Write_WhenAnsiDictionary_ShouldRoundTripNames()
    {
        OlePropertySection parsed = RoundTripDictionary(1252);

        Assert.AreEqual("First Custom", parsed.PropertyNames[2]);
        Assert.AreEqual("Odd", parsed.PropertyNames[3]);
    }

    /// <summary>
    /// Verifies that a name dictionary in a Unicode (code page 1200) section round-trips its property names using
    /// character-count prefixes and four-byte entry padding.
    /// </summary>
    [TestMethod]
    public void Write_WhenUnicodeCodePageDictionary_ShouldRoundTripNames()
    {
        OlePropertySection parsed = RoundTripDictionary(1200);

        Assert.AreEqual("First Custom", parsed.PropertyNames[2]);
        Assert.AreEqual("Odd", parsed.PropertyNames[3]);
    }

    /// <summary>
    /// Verifies that a name set on the first section of a set, which is not the user-defined section, is written as that
    /// section's dictionary, property 0, and reads back.
    /// </summary>
    [TestMethod]
    public void Write_WhenAnySectionHasNames_ShouldWriteItsDictionary()
    {
        var section = new OlePropertySection(TestFormatId, 1252);
        section.Set(2, OlePropertyValue.Create(7));
        section.SetName(2, "Seven");
        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1252);
        set.AddSection(section);

        byte[] bytes = set.ToArray();

        Assert.IsTrue(FindPropertyOffsets(bytes, 0).ContainsKey(0));
        Assert.AreEqual("Seven", OlePropertySet.Parse(bytes).Sections[0].PropertyNames[2]);
    }
}
