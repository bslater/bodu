// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExternalPluginLoaderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Globalization.Calendar.Plugins;

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
	/// and <see cref="INotableDateCalculatorPlugin" />, each returning the test fixtures declared by the plugin author.
	/// </summary>
	[TestMethod]
	public void Load_WhenPluginImplementsBothSplitInterfaces_ShouldExposeBothRulesAndCalculators()
	{
		var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());
		var plugin = loader.Load(Plugin1Path);

		var rulePlugin = plugin as INotableDateRulePlugin;
		Assert.IsNotNull(rulePlugin, "Test plugin should implement INotableDateRulePlugin.");
		var rules = rulePlugin!.GetRuleProviders().SelectMany(p => p.LoadRules()).ToList();
		Assert.AreEqual(1, rules.Count);
		Assert.AreEqual("Harness Test Day", rules[0].Name);

		var calculatorPlugin = plugin as INotableDateCalculatorPlugin;
		Assert.IsNotNull(calculatorPlugin, "Test plugin should implement INotableDateCalculatorPlugin.");
		var calculators = calculatorPlugin!.GetCalculators().ToList();
		Assert.AreEqual(1, calculators.Count);
		Assert.AreEqual("harness.static", calculators[0].Key);
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
	public void Constructor_WhenTrustPolicyIsNull_ShouldThrowArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => _ = new ExternalPluginLoader(null!));
	}
}
