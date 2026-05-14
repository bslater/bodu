// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExternalPluginLoaderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Verifies the end-to-end behaviour of <see cref="ExternalPluginLoader" /> against the two test-only plugin assemblies
/// <c>Bodu.Globalization.Calendar.Plugin1.TestAssembly</c> (happy path) and <c>Bodu.Globalization.Calendar.Plugin2.TestAssembly</c>
/// (missing attribute), which are copied to the test output directory by the test project's ProjectReference metadata.
/// </summary>
[TestClass]
public sealed class ExternalPluginLoaderTests
{
    private const string Plugin1AssemblyFileName = "Bodu.Globalization.Calendar.Plugin1.TestAssembly.dll";
    private const string Plugin2AssemblyFileName = "Bodu.Globalization.Calendar.Plugin2.TestAssembly.dll";

    private static string Plugin1Path => Path.Combine(AppContext.BaseDirectory, Plugin1AssemblyFileName);

    private static string Plugin2Path => Path.Combine(AppContext.BaseDirectory, Plugin2AssemblyFileName);

    /// <summary>
    /// Verifies that a trusted plugin assembly with a valid <see cref="NotableDatePluginAttribute" /> loads and activates into
    /// an <see cref="INotableDatePlugin" /> instance exposing the name and version declared by the plugin author.
    /// </summary>
    [TestMethod]
    public void Load_WhenAssemblyIsTrustedAndAttributeValid_ShouldReturnActivatedPlugin()
    {
        Assert.IsTrue(File.Exists(Plugin1Path), $"Test plugin not found at '{Plugin1Path}'. Ensure the ProjectReference is wired.");

        var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());

        var plugin = loader.Load(Plugin1Path);

        Assert.IsNotNull(plugin);
        Assert.AreEqual("Bodu.Test.Harness.Plugin1", plugin.Name);
        Assert.AreEqual(new Version(1, 0, 0), plugin.Version);
    }

    /// <summary>
    /// Verifies that the activated plugin participates in the split-concern contracts as an <see cref="INotableDateRulePlugin" />
    /// and <see cref="INotableDateAlgorithmPlugin" />, each returning the test fixtures declared by the plugin author.
    /// </summary>
    [TestMethod]
    public void Load_WhenPluginImplementsBothSplitInterfaces_ShouldExposeBothRulesAndAlgorithms()
    {
        var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());
        var plugin = loader.Load(Plugin1Path);

        var rulePlugin = plugin as INotableDateRulePlugin;
        Assert.IsNotNull(rulePlugin, "Test plugin should implement INotableDateRulePlugin.");
        var rules = rulePlugin!.GetRuleProviders().SelectMany(p => p.LoadRules()).ToList();
        Assert.AreEqual(1, rules.Count);
        Assert.AreEqual("Harness Test Day", rules[0].Name);

        var algorithmPlugin = plugin as INotableDateAlgorithmPlugin;
        Assert.IsNotNull(algorithmPlugin, "Test plugin should implement INotableDateAlgorithmPlugin.");
        var algorithms = algorithmPlugin!.GetAlgorithms().ToList();
        Assert.AreEqual(1, algorithms.Count);
        Assert.AreEqual("harness.static", algorithms[0].Key);
    }

    /// <summary>
    /// Verifies that a trust policy rejection surfaces as <see cref="PluginNotTrustedException" /> and propagates the policy's
    /// stated reason without loading the plugin assembly.
    /// </summary>
    [TestMethod]
    public void Load_WhenTrustPolicyRejects_ShouldThrowPluginNotTrustedExceptionWithReason()
    {
        var rejecting = new DelegatingPluginTrustPolicy(_ => new PluginTrustResult(false, "unit-test-rejection"));
        var loader = new ExternalPluginLoader(rejecting);

        var thrown = Assert.ThrowsExactly<PluginNotTrustedException>(() => _ = loader.Load(Plugin1Path));

        StringAssert.Contains(thrown.Message, "unit-test-rejection");
        Assert.AreEqual("unit-test-rejection", thrown.Reason);
    }

    /// <summary>
    /// Verifies that a trusted assembly without a <see cref="NotableDatePluginAttribute" /> surfaces as a
    /// <see cref="PluginMissingAttributeException" /> rather than an activation attempt.
    /// </summary>
    [TestMethod]
    public void Load_WhenAssemblyMissingPluginAttribute_ShouldThrowPluginMissingAttributeException()
    {
        Assert.IsTrue(File.Exists(Plugin2Path), $"Non-plugin test assembly not found at '{Plugin2Path}'.");

        var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());

        var thrown = Assert.ThrowsExactly<PluginMissingAttributeException>(() => _ = loader.Load(Plugin2Path));

        StringAssert.Contains(thrown.Message, "NotableDatePluginAttribute");
    }

    /// <summary>
    /// Verifies that supplying a path to a non-existent file throws <see cref="FileNotFoundException" /> rather than a generic
    /// or misleading error.
    /// </summary>
    [TestMethod]
    public void Load_WhenAssemblyDoesNotExist_ShouldThrowFileNotFoundException()
    {
        var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());

        Assert.ThrowsExactly<FileNotFoundException>(() => _ = loader.Load("/tmp/bodu-nonexistent-plugin.dll"));
    }

    /// <summary>
    /// Verifies that supplying a null, empty, or whitespace path throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Load_WhenPathIsEmpty_ShouldThrowArgumentException(string path)
    {
        var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());

        Assert.ThrowsExactly<ArgumentException>(() => _ = loader.Load(path));
    }

    /// <summary>
    /// Verifies that the constructor rejects a null trust policy rather than deferring the failure to the first
    /// <see cref="ExternalPluginLoader.Load" /> call.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTrustPolicyIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new ExternalPluginLoader(null!));
    }

    private const string Plugin3AssemblyFileName = "Bodu.Globalization.Calendar.Plugin3.TestAssembly.dll";
    private const string Plugin4AssemblyFileName = "Bodu.Globalization.Calendar.Plugin4.TestAssembly.dll";

    private static string Plugin3Path => Path.Combine(AppContext.BaseDirectory, Plugin3AssemblyFileName);

    private static string Plugin4Path => Path.Combine(AppContext.BaseDirectory, Plugin4AssemblyFileName);

    /// <summary>
    /// Verifies that a plugin whose entry-point type throws from its constructor surfaces as a
    /// <see cref="PluginActivationException" /> wrapping the original cause.
    /// </summary>
    [TestMethod]
    public void Load_WhenPluginConstructorThrows_ShouldThrowPluginActivationExceptionWrappingCause()
    {
        Assert.IsTrue(File.Exists(Plugin3Path), $"Plugin3 assembly not found at '{Plugin3Path}'.");
        var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());

        var ex = Assert.ThrowsExactly<PluginActivationException>(() => _ = loader.Load(Plugin3Path));

        Assert.AreEqual(Plugin3Path, ex.AssemblyPath);
        Assert.IsNotNull(ex.PluginType);
        Assert.IsNotNull(ex.InnerException);
        // Activator.CreateInstance wraps the constructor's exception in a TargetInvocationException;
        // walk the chain to confirm the original "intentional" marker reaches the caller.
        Exception? chain = ex.InnerException;
        bool found = false;
        while (chain is not null)
        {
            if (chain.Message.Contains("intentional", StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                break;
            }
            chain = chain.InnerException;
        }
        Assert.IsTrue(found, "Expected the original 'intentional' message somewhere in the inner-exception chain.");
    }

    /// <summary>
    /// Verifies that a plugin whose <see cref="NotableDatePluginAttribute" /> points at a type
    /// that does not implement <see cref="INotableDatePlugin" /> surfaces as a
    /// <see cref="PluginMissingAttributeException" /> with a reason mentioning the interface.
    /// </summary>
    [TestMethod]
    public void Load_WhenAttributeTypeDoesNotImplementInterface_ShouldThrowPluginMissingAttributeException()
    {
        Assert.IsTrue(File.Exists(Plugin4Path), $"Plugin4 assembly not found at '{Plugin4Path}'.");
        var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());

        var ex = Assert.ThrowsExactly<PluginMissingAttributeException>(() => _ = loader.Load(Plugin4Path));

        Assert.AreEqual(Plugin4Path, ex.AssemblyPath);
        Assert.IsTrue(ex.Reason.Contains("INotableDatePlugin", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that an unreadable assembly file — for instance a text file with a <c>.dll</c>
    /// extension — surfaces as a runtime exception raised during metadata inspection rather
    /// than being silently tolerated. Asserts only the presence of an exception because the
    /// exact type depends on where metadata parsing fails first.
    /// </summary>
    [TestMethod]
    public void Load_WhenAssemblyFileIsMalformed_ShouldSurfaceException()
    {
        string malformedPath = Path.Combine(Path.GetTempPath(), $"bodu-malformed-{Guid.NewGuid():N}.dll");
        try
        {
            File.WriteAllText(malformedPath, "this is not a valid portable-executable file");
            var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());

            Assert.ThrowsExactly<System.BadImageFormatException>(() => _ = loader.Load(malformedPath));
        }
        finally
        {
            if (File.Exists(malformedPath))
                File.Delete(malformedPath);
        }
    }

    /// <summary>
    /// Verifies that the private <c>ResolveFromHostOrAlongside</c> callback returns
    /// <see langword="null" /> for an assembly with an empty name, resolves a host-loaded
    /// assembly for a matching name, and returns <see langword="null" /> for an assembly
    /// name not loaded in the default context. The callback is an implementation detail
    /// invoked by <see cref="System.Runtime.Loader.AssemblyLoadContext" /> during plugin load
    /// and is not exercised by normal public-API plugin scenarios in this test harness.
    /// </summary>
    [TestMethod]
    public void ResolveFromHostOrAlongside_ShouldFavourHostAssemblyAndTolerateUnnamedRequests()
    {
        var resolve = typeof(ExternalPluginLoader).GetMethod(
            "ResolveFromHostOrAlongside",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(resolve, "ResolveFromHostOrAlongside was not found on ExternalPluginLoader.");

        var defaultContext = System.Runtime.Loader.AssemblyLoadContext.Default;

        var unnamed = new System.Reflection.AssemblyName { Name = string.Empty };
        var unnamedResult =
            (System.Reflection.Assembly?)resolve!.Invoke(null, new object?[] { defaultContext, unnamed });
        Assert.IsNull(unnamedResult);

        var calendarAssemblyName = typeof(NotableDateService).Assembly.GetName();
        var hostResult =
            (System.Reflection.Assembly?)resolve.Invoke(null, new object?[] { defaultContext, calendarAssemblyName });
        Assert.AreSame(typeof(NotableDateService).Assembly, hostResult);

        var unknownName = new System.Reflection.AssemblyName("Definitely.Not.Loaded.Assembly.Name");
        var unknownResult =
            (System.Reflection.Assembly?)resolve.Invoke(null, new object?[] { defaultContext, unknownName });
        Assert.IsNull(unknownResult);
    }
}
