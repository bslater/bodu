// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoaderTests.LoadFrom.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class NotableDatePluginLoaderTests
{
    /// <summary>
    /// Verifies that a trusted, attributed assembly yields its plugin with the expected identity.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenTrustedAndAttributed_ActivatesThePlugin()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(TestAssembly, new AllowAllPluginTrustPolicy());

        Assert.IsInstanceOfType<TestPlugin>(plugin);
        Assert.AreEqual(
            ("Test Plugin", new Version(1, 2, 3)),
            (plugin.Name, plugin.Version));
    }

    /// <summary>
    /// Verifies that a rejecting trust policy prevents activation.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenTrustRejected_Throws()
    {
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(_ => PluginTrustResult.Rejected("blocked"));

        PluginNotTrustedException ex = Assert.ThrowsExactly<PluginNotTrustedException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(TestAssembly, policy);
        });

        Assert.AreEqual("blocked", ex.Reason);
    }

    /// <summary>
    /// Verifies that a composite policy rejects when any member rejects.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenCompositePolicyHasRejection_Throws()
    {
        IPluginTrustPolicy policy = new CompositePluginTrustPolicy(
            new AllowAllPluginTrustPolicy(),
            new DelegatingPluginTrustPolicy(_ => PluginTrustResult.Rejected("second policy")));

        PluginNotTrustedException ex = Assert.ThrowsExactly<PluginNotTrustedException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(TestAssembly, policy);
        });

        Assert.AreEqual("second policy", ex.Reason);
    }

    /// <summary>
    /// Verifies that an assembly without the plugin attribute is rejected.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenAssemblyHasNoAttribute_Throws()
    {
        Assert.ThrowsExactly<PluginMissingAttributeException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(typeof(string).Assembly, new AllowAllPluginTrustPolicy());
        });
    }

    /// <summary>
    /// Verifies that the loader populates the trust context with the assembly name and the strong-name public-key token
    /// (lowercase hexadecimal, or <see langword="null" /> when the assembly is not signed).
    /// </summary>
    [TestMethod]
    public void LoadFrom_ShouldPopulateTrustContextWithNameAndToken()
    {
        PluginTrustContext? captured = null;
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(context =>
        {
            captured = context;
            return PluginTrustResult.Trusted();
        });

        _ = NotableDatePluginLoader.LoadFrom(TestAssembly, policy);

        byte[]? tokenBytes = TestAssembly.GetName().GetPublicKeyToken();
        string? expectedToken = tokenBytes is null || tokenBytes.Length == 0
            ? null
            : Convert.ToHexString(tokenBytes).ToLowerInvariant();

        Assert.IsNotNull(captured);
        Assert.AreEqual(
            (TestAssembly.GetName().Name, (string?)expectedToken),
            ((string?)captured!.AssemblyName, captured.PublicKeyToken));
    }

    /// <summary>
    /// Verifies that the file-path <see cref="NotableDatePluginLoader.LoadFrom(string, IPluginTrustPolicy, Microsoft.Extensions.Logging.ILogger)" /> overload
    /// loads the assembly from disk and activates its plugin.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenGivenAssemblyPath_ShouldActivateThePlugin()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(TestAssembly.Location, new AllowAllPluginTrustPolicy());

        Assert.AreEqual("Test Plugin", plugin.Name);
    }
}
