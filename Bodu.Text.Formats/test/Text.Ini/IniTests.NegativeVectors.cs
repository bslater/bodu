// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniTests.NegativeVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

public sealed partial class IniTests
{

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> throws <see cref="IniFormatException" /> when
    /// the source contains a section header missing the closing bracket.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSectionHeaderMissingClosingBracket_ShouldThrowExactly()
    {
        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Ini.Parse("[unclosed");
        });

        Assert.AreEqual(1, ex.LineNumber);
        StringAssert.Contains(ex.Message, FormatsResourceStrings.ResourceManager
            .GetString("Format_Invalid_IniMalformedSectionHeader",
                System.Globalization.CultureInfo.InvariantCulture)![..10]);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> throws <see cref="IniFormatException" /> when
    /// the section header contains an empty name.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSectionNameIsEmpty_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Ini.Parse("[]");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> throws <see cref="IniFormatException" /> when
    /// the section header contains a whitespace-only name.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSectionNameIsWhitespaceOnly_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Ini.Parse("[   ]");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> reports the correct 1-based line number in the
    /// thrown <see cref="IniFormatException" /> when the malformed section header is not on the first line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMalformedHeaderOnThirdLine_ShouldReportLineNumberThree()
    {
        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Ini.Parse("key=value\n# comment\n[bad");
        });

        Assert.AreEqual(3, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> throws <see cref="IniFormatException" /> when
    /// a key line contains only a separator character (empty key).
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyIsEmpty_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Ini.Parse("[s]\n=value");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> reports the correct line number for an
    /// empty-key error inside a section.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyIsEmptyOnSecondLine_ShouldReportCorrectLineNumber()
    {
        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Ini.Parse("[s]\n=value");
        });

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> throws <see cref="IniFormatException" /> when
    /// global entries are disallowed and a key appears before any section header.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGlobalKeyDisallowedAndKeyPresent_ShouldThrowExactly()
    {
        IniParseOptions options = new() { AllowGlobalSection = false };

        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Ini.Parse("key=value", options);
        });

        Assert.AreEqual(1, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> throws <see cref="IniFormatException" /> when
    /// duplicate keys are disallowed and the same key appears twice in a section.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyDisallowedAndKeyRepeated_ShouldThrowExactly()
    {
        IniParseOptions options = new() { DuplicateKeyBehavior = IniDuplicateKeyBehavior.Disallowed };

        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Ini.Parse("[s]\nkey=1\nkey=2", options);
        });

        Assert.AreEqual(3, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> throws <see cref="IniFormatException" /> when
    /// duplicate sections are disallowed and the same section header appears twice.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionDisallowedAndSectionRepeated_ShouldThrowExactly()
    {
        IniParseOptions options = new() { DuplicateSectionBehavior = IniDuplicateSectionBehavior.Disallowed };

        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Ini.Parse("[s]\nkey=1\n[s]\nkey=2", options);
        });

        Assert.AreEqual(3, ex.LineNumber);
    }

}
