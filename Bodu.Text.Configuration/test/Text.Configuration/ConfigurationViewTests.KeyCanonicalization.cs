// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationViewTests.KeyCanonicalization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationViewTests
{
    private static ConfigurationView ResolveWithMapping(string source, ConfigurationKeyMapping mapping)
    {
        var doc = ConfigurationDocument.Parse(source);
        ConfigurationResolveOptions options = new()
        {
            KeyOptions = new ConfigurationKeyOptions { Mapping = mapping },
        };

        return doc.Resolve("any.cs", options);
    }

    /// <summary>
    /// Verifies that under the default dot-to-colon mapping a value is reachable through the dotted, the colon, and a
    /// mixed-separator lookup form.
    /// </summary>
    [TestMethod]
    public void Lookup_WhenDotToColonMapping_ShouldResolveDottedColonAndMixedForms()
    {
        ConfigurationView view = ResolveWithMapping("[*]\nformat.indent.size = 4\n", ConfigurationKeyMapping.DotToColon);

        Assert.AreEqual("4", view["format:indent:size"]);
        Assert.AreEqual("4", view["format.indent.size"]);
        Assert.AreEqual("4", view["format.indent:size"]);
    }

    /// <summary>
    /// Verifies that under the colon mapping a value is reachable through both the dotted and colon lookup forms.
    /// </summary>
    [TestMethod]
    public void Lookup_WhenColonMapping_ShouldResolveDottedAndColonForms()
    {
        ConfigurationView view = ResolveWithMapping("[*]\nformat.indent.size = 4\n", ConfigurationKeyMapping.Colon);

        Assert.AreEqual("4", view["format:indent:size"]);
        Assert.AreEqual("4", view["format.indent.size"]);
    }

    /// <summary>
    /// Verifies that under the identity mapping — where the stored canonical key keeps its dotted form — a colon-form
    /// lookup still resolves to the same value. This is the case the previous one-way <c>'.'→':'</c> normalization
    /// could not reach.
    /// </summary>
    [TestMethod]
    public void Lookup_WhenIdentityMapping_ShouldResolveColonFormAgainstDottedStoredKey()
    {
        ConfigurationView view = ResolveWithMapping("[*]\nformat.indent.size = 4\n", ConfigurationKeyMapping.Identity);

        // Stored canonical key keeps the dotted form under identity mapping.
        Assert.Contains("format.indent.size", view.Keys);

        Assert.AreEqual("4", view["format.indent.size"]);
        Assert.AreEqual("4", view["format:indent:size"]);
        Assert.IsTrue(view.ContainsKey("format:indent:size"));
    }

    /// <summary>
    /// Verifies that a lookup key that cannot be parsed as a configuration key resolves to "absent" without throwing.
    /// </summary>
    [TestMethod]
    public void Lookup_WhenKeyHasEmptySegment_ShouldReturnNullWithoutThrowing()
    {
        ConfigurationView view = ResolveWithMapping("[*]\nformat.indent.size = 4\n", ConfigurationKeyMapping.DotToColon);

        Assert.IsNull(view["format..size"]);
        Assert.IsFalse(view.ContainsKey("format..size"));
    }
}
