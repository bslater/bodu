// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniTests.TryParse.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

public sealed partial class IniTests
{

    /// <summary>
    /// Verifies that <see cref="Ini.TryParse(ReadOnlySpan{char}, out IniDocument)" /> returns
    /// <see langword="true" /> and a non-null document for valid input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsValid_ShouldReturnTrueAndDocument()
    {
        var result = Ini.TryParse("[section]\nkey=value", out IniDocument? document);

        Assert.IsTrue(result);
        Assert.IsNotNull(document);
        Assert.IsNotNull(document.GetSection("section"));
    }

    /// <summary>
    /// Verifies that <see cref="Ini.TryParse(ReadOnlySpan{char}, out IniDocument)" /> returns
    /// <see langword="true" /> and an empty document for empty input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsEmpty_ShouldReturnTrueWithEmptyDocument()
    {
        var result = Ini.TryParse(string.Empty, out IniDocument? document);

        Assert.IsTrue(result);
        Assert.IsNotNull(document);
        Assert.AreEqual(0, document.Sections.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.TryParse(ReadOnlySpan{char}, out IniDocument)" /> returns
    /// <see langword="false" /> and a <see langword="null" /> document for a malformed section header.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenSectionHeaderIsMalformed_ShouldReturnFalseWithNull()
    {
        var result = Ini.TryParse("[unclosed", out IniDocument? document);

        Assert.IsFalse(result);
        Assert.IsNull(document);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.TryParse(ReadOnlySpan{char}, IniParseOptions, out IniDocument)" /> respects
    /// the supplied options.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenGlobalKeyDisallowed_ShouldReturnFalseOnGlobalEntry()
    {
        IniParseOptions options = new() { AllowGlobalSection = false };

        var result = Ini.TryParse("key=value", options, out IniDocument? document);

        Assert.IsFalse(result);
        Assert.IsNull(document);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.TryParse(ReadOnlySpan{char}, out IniDocument)" /> does not throw even when
    /// the input is malformed.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsMalformed_ShouldNotThrow()
    {
        var result = false;
        IniDocument? document = null;

        Exception? caughtException = null;
        try
        {
            result = Ini.TryParse("[ ]", out document);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        Assert.IsNull(caughtException, $"TryParse threw {caughtException?.GetType().Name}.");
        Assert.IsFalse(result);
        Assert.IsNull(document);
    }

}
