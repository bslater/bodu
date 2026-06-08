// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationViewTests.BooleanRejection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class ConfigurationViewTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetBoolean(string)" /> rejects the common
    /// non-EditorConfig boolean spellings — <c>yes</c>, <c>no</c>, <c>on</c>, <c>off</c>, <c>1</c>, <c>0</c>.
    /// Pinning this contract protects against accidental relaxation that would silently make documents that
    /// rely on EditorConfig-strict semantics behave differently.
    /// </summary>
    [TestMethod]
    [DataRow("yes")]
    [DataRow("no")]
    [DataRow("on")]
    [DataRow("off")]
    [DataRow("1")]
    [DataRow("0")]
    [DataRow("YES")]
    [DataRow("On")]
    public void GetBoolean_WhenValueIsRelaxedSpelling_ShouldThrowFormatException(string value)
    {
        ConfigurationDocument doc = ConfigurationDocument.Parse($"[*]\nflag = {value}\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetBoolean("flag"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.TryGetBoolean(string, out bool)" /> also rejects the
    /// relaxed spellings — TryGet contract must agree with GetBoolean on what is and is not a boolean.
    /// </summary>
    [TestMethod]
    public void TryGetBoolean_WhenValueIsRelaxedSpelling_ShouldReturnFalse()
    {
        ConfigurationDocument doc = ConfigurationDocument.Parse("[*]\nflag = yes\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.IsFalse(view.TryGetBoolean("flag", out _));
    }

    /// <summary>
    /// Verifies that surrounding whitespace is tolerated by <see cref="bool.TryParse(string, out bool)" />,
    /// matching the underlying BCL contract. Pinning this so a future trim policy change does not regress
    /// it.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WhenValueHasSurroundingWhitespace_ShouldStillParse()
    {
        // The parser trims values by default (TrimKeysAndValues = true), but bool.TryParse also tolerates
        // surrounding whitespace internally. Either layer suffices.
        ConfigurationDocument doc = ConfigurationDocument.Parse("[*]\nflag =   true   \n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.IsTrue(view.GetBoolean("flag"));
    }
}
