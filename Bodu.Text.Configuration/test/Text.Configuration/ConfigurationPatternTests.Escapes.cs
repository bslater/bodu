// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPatternTests.Escapes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationPatternTests
{
    /// <summary>
    /// Verifies that a pattern ending with a lone backslash compiles, escaping the trailing backslash literally.
    /// </summary>
    [TestMethod]
    public void Compile_WhenPatternEndsWithBackslash_ShouldEscapeIt()
    {
        var pattern = ConfigurationPattern.Compile("abc\\");

        Assert.AreEqual("abc\\", pattern.Source);
    }

    /// <summary>
    /// Verifies that an escaped character inside a character class is matched literally.
    /// </summary>
    [TestMethod]
    public void Compile_WhenCharClassContainsEscape_ShouldMatchLiteral()
    {
        var pattern = ConfigurationPattern.Compile("file[\\*].cs");

        Assert.IsTrue(pattern.IsMatch("file*.cs"));
    }

    /// <summary>
    /// Verifies that a caret inside (but not at the start of) a character class is matched literally.
    /// </summary>
    [TestMethod]
    public void Compile_WhenCharClassContainsCaret_ShouldMatchLiteral()
    {
        var pattern = ConfigurationPattern.Compile("v[1^].cs");

        Assert.IsTrue(pattern.IsMatch("v^.cs"));
    }
}
