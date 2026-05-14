// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServicePluginIntegrationTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Extensions;
using Bodu.Globalization.Calendar.Plugins;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Verifies that a plugin loaded via <see cref="ExternalPluginLoader" /> flows through <see cref="NotableDateService" /> as a
/// first-class contributor: its rule providers show up in <c>GetNotableDates</c> results, and its named algorithms resolve
/// rules that target them by key.
/// </summary>
[TestClass]
public sealed class NotableDateServicePluginIntegrationTests
{
	private const string Plugin1AssemblyFileName = "Bodu.Globalization.Calendar.Plugin1.TestAssembly.dll";

	private static string Plugin1Path => Path.Combine(AppContext.BaseDirectory, Plugin1AssemblyFileName);

	/// <summary>
	/// Verifies that rules contributed by a loaded plugin appear in the service's per-year results, matched by the query
	/// territory declared on the plugin's rule.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenPluginContributesFixedRule_ShouldIncludeItInResults()
	{
		var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());
		var plugin = loader.Load(Plugin1Path);

		var service = new NotableDateService(
			ruleProviders: Array.Empty<INotableDateRuleProvider>(),
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			options: new NotableDateServiceOptions { Plugins = new[] { plugin } });

		// Plugin1's HarnessPlugin contributes "Harness Test Day" on 15 June with territory "ZZ".
		var results = service.GetNotableDates(2030, "ZZ");

		Assert.IsTrue(results.Any(r => r.Name == "Harness Test Day" && r.Date == new DateTime(2030, 6, 15)));
	}

	/// <summary>
	/// Verifies that a rule targeting a plugin-supplied algorithm resolves via the composite algorithm registry: the service
	/// registers the plugin's algorithm and then a consumer rule whose <c>AlgorithmKey</c> matches that registration
	/// resolves through to the expected date.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenRuleUsesPluginAlgorithm_ShouldResolveDateViaPluginRegistration()
	{
		var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());
		var plugin = loader.Load(Plugin1Path);

		// Consumer-authored rule that relies on the plugin's algorithm registration ("harness.static"), which
		// Plugin1 hard-codes to return 15 June 2027.
		var consumerRule = new NotableDateRule
		{
			Name = "Test Plugin Algorithm Day",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmKey = "harness.static",
		};

		var service = new NotableDateService(
			ruleProviders: new[] { new InMemoryRuleProvider(consumerRule) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			options: new NotableDateServiceOptions { Plugins = new[] { plugin } });

		var results = service.GetNotableDates(2027);
		var resolved = results.SingleOrDefault(r => r.Name == "Test Plugin Algorithm Day");

		Assert.IsNotNull(resolved);
		Assert.AreEqual(new DateTime(2027, 6, 15), resolved!.Date);
	}

	/// <summary>
	/// Verifies that when both the host and a plugin register a algorithm under the same key,
	/// the host-supplied registration wins — exercising the composite-registry precedence path
	/// in <see cref="NotableDateService" />.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenHostAndPluginCollideOnAlgorithmKey_ShouldPreferHostRegistration()
	{
		var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());
		var plugin = loader.Load(Plugin1Path);

		// Plugin1's algorithm returns 2027-06-15 under key "harness.static". Host registers a
		// different algorithm under the same key; host should win.
		var hostRegistry = new NotableDateAlgorithmRegistry()
			.Register("harness.static", new HostOverrideAlgorithm(new DateTime(2099, 1, 1)));

		var consumerRule = new NotableDateRule
		{
			Name = "Composite Test Day",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmKey = "harness.static",
		};

		var service = new NotableDateService(
			ruleProviders: new[] { new InMemoryRuleProvider(consumerRule) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			options: new NotableDateServiceOptions
			{
				AlgorithmRegistry = hostRegistry,
				Plugins = new[] { plugin },
			});

		var resolved = service.GetNotableDates(2099).SingleOrDefault(r => r.Name == "Composite Test Day");

		Assert.IsNotNull(resolved);
		Assert.AreEqual(new DateTime(2099, 1, 1), resolved!.Date);
	}

	/// <summary>
	/// Verifies that the composite registry's <c>Contains</c> path reports keys supplied by
	/// either layer as present — exercising the <c>_primary.Contains(key) || _fallback.Contains(key)</c>
	/// short-circuit.
	/// </summary>
	[TestMethod]
	public void CompositeAlgorithmRegistry_ShouldReportContainsFromEitherLayer()
	{
		var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());
		var plugin = loader.Load(Plugin1Path);

		// Host provides "host.only"; plugin provides "harness.static". The composite should
		// Contains both.
		var hostRegistry = new NotableDateAlgorithmRegistry()
			.Register("host.only", new HostOverrideAlgorithm(new DateTime(2050, 1, 1)));

		var hostOnlyRule = new NotableDateRule
		{
			Name = "Host Only",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmKey = "host.only",
		};
		var pluginOnlyRule = new NotableDateRule
		{
			Name = "Plugin Only",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmKey = "harness.static",
		};

		var service = new NotableDateService(
			ruleProviders: new[] { new InMemoryRuleProvider(hostOnlyRule, pluginOnlyRule) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			options: new NotableDateServiceOptions
			{
				AlgorithmRegistry = hostRegistry,
				Plugins = new[] { plugin },
			});

		IReadOnlyList<NotableDate> results = service.GetNotableDates(2050);

		Assert.IsTrue(results.Any(r => r.Name == "Host Only"));
		Assert.IsTrue(results.Any(r => r.Name == "Plugin Only"));
	}

	/// <summary>
	/// Verifies that when an algorithm rule's key is present in neither the host nor the plugin registry, the composite
	/// registry's <c>Contains</c> / <c>TryGet</c> short-circuits both layers and the service emits no occurrence for the
	/// rule — exercising the <see cref="NotableDateService.CompositeAlgorithmRegistry" /> double-miss branch.
	/// </summary>
	[TestMethod]
	public void CompositeAlgorithmRegistry_WhenKeyMissingFromBothLayers_ShouldEmitNoOccurrence()
	{
		var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());
		var plugin = loader.Load(Plugin1Path);

		var hostRegistry = new NotableDateAlgorithmRegistry()
			.Register("host.only", new HostOverrideAlgorithm(new DateTime(2050, 1, 1)));

		// The consumer rule targets an algorithm key that exists on neither layer.
		var orphan = new NotableDateRule
		{
			Name = "Orphan Algorithm Rule",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmKey = "missing.everywhere",
		};

		var service = new NotableDateService(
			ruleProviders: new[] { new InMemoryRuleProvider(orphan) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			options: new NotableDateServiceOptions
			{
				AlgorithmRegistry = hostRegistry,
				Plugins = new[] { plugin },
			});

		IReadOnlyList<NotableDate> results = service.GetNotableDates(2050);

		Assert.IsFalse(results.Any(r => r.Name == "Orphan Algorithm Rule"));
	}

	private sealed class InMemoryRuleProvider
		: INotableDateRuleProvider
	{
		private readonly NotableDateRule[] _rules;

		public InMemoryRuleProvider(params NotableDateRule[] rules) => _rules = rules;

		public IEnumerable<NotableDateRule> LoadRules() => _rules;
	}

	private sealed class HostOverrideAlgorithm
		: INotableDateAlgorithm
	{
		private readonly DateTime _date;

		public HostOverrideAlgorithm(DateTime date) => _date = date;

		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) => _date;
	}
}
