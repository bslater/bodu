// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginTrustTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Tests for the composite trust policy, the trust context record, and the trust-related exception properties.
/// </summary>
[TestClass]
public sealed class PluginTrustTests
{
    /// <summary>
    /// Verifies that a composite policy with no constituent policies trusts every context.
    /// </summary>
    [TestMethod]
    public void CompositePolicy_WhenNoConstituents_ShouldTrust()
    {
        PluginTrustContext context = new("plugin.asm", "/plugins/plugin.dll", FileHash: null);

        PluginTrustResult result = new CompositePluginTrustPolicy().Evaluate(context);

        Assert.IsTrue(result.IsTrusted);
    }

    /// <summary>
    /// Verifies that the trust context exposes its assembly path.
    /// </summary>
    [TestMethod]
    public void TrustContext_ShouldExposeAssemblyPath()
    {
        PluginTrustContext context = new("plugin.asm", "/plugins/plugin.dll", FileHash: null);

        Assert.AreEqual("/plugins/plugin.dll", context.AssemblyPath);
    }

    /// <summary>
    /// Verifies that <see cref="PluginMissingAttributeException.AssemblyName" /> exposes the supplied assembly name.
    /// </summary>
    [TestMethod]
    public void MissingAttributeException_ShouldExposeAssemblyName()
    {
        PluginMissingAttributeException ex = new("missing attribute", "plugin.asm");

        Assert.AreEqual("plugin.asm", ex.AssemblyName);
    }

    /// <summary>
    /// Verifies that <see cref="PluginNotTrustedException.AssemblyName" /> and <see cref="PluginNotTrustedException.Reason" />
    /// expose the supplied values.
    /// </summary>
    [TestMethod]
    public void NotTrustedException_ShouldExposeAssemblyNameAndReason()
    {
        PluginNotTrustedException ex = new("rejected", "plugin.asm", "untrusted hash");

        Assert.AreEqual(("plugin.asm", "untrusted hash"), (ex.AssemblyName, ex.Reason));
    }
}
