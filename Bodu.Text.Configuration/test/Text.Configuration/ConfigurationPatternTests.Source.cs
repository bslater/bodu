// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPatternTests.Source.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationPatternTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationPattern.Source" /> exposes the original pattern text.
    /// </summary>
    [TestMethod]
    public void Source_ShouldExposeOriginalPatternText()
    {
        ConfigurationPattern pattern = ConfigurationPattern.Compile("*.cs");

        Assert.AreEqual("*.cs", pattern.Source);
    }
}
