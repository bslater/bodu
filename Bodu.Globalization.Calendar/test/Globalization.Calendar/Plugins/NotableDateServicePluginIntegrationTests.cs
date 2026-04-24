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
/// first-class contributor: its rule providers show up in <c>GetNotableDates</c> results, and its named calculators resolve
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
			plugins: new[] { plugin });

		// Plugin1's HarnessPlugin contributes "Harness Test Day" on 15 June with territory "ZZ".
		var results = service.GetNotableDates(2030, "ZZ");

		Assert.IsTrue(results.Any(r => r.Name == "Harness Test Day" && r.Date == new DateTime(2030, 6, 15)));
	}

	/// <summary>
	/// Verifies that a rule targeting a plugin-supplied calculator resolves via the composite calculator registry: the service
	/// registers the plugin's calculator and then a consumer rule whose <c>CalculatorKey</c> matches that registration
	/// resolves through to the expected date.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenRuleUsesPluginCalculator_ShouldResolveDateViaPluginRegistration()
	{
		var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());
		var plugin = loader.Load(Plugin1Path);

		// Consumer-authored rule that relies on the plugin's calculator registration ("harness.static"), which
		// Plugin1 hard-codes to return 15 June 2027.
		var consumerRule = new NotableDateRule
		{
			Name = "Test Plugin Calculator Day",
			Strategy = DateResolutionStrategy.Calculator,
			Category = NotableDateCategory.Observance,
			CalculatorKey = "harness.static",
		};

		var service = new NotableDateService(
			ruleProviders: new[] { new InMemoryRuleProvider(consumerRule) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			plugins: new[] { plugin });

		var results = service.GetNotableDates(2027);
		var resolved = results.SingleOrDefault(r => r.Name == "Test Plugin Calculator Day");

		Assert.IsNotNull(resolved);
		Assert.AreEqual(new DateTime(2027, 6, 15), resolved!.Date);
	}

	/// <summary>
	/// Verifies that when both the host and a plugin register a calculator under the same key,
	/// the host-supplied registration wins — exercising the composite-registry precedence path
	/// in <see cref="NotableDateService" />.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenHostAndPluginCollideOnCalculatorKey_ShouldPreferHostRegistration()
	{
		var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());
		var plugin = loader.Load(Plugin1Path);

		// Plugin1's calculator returns 2027-06-15 under key "harness.static". Host registers a
		// different calculator under the same key; host should win.
		var hostRegistry = new NotableDateCalculatorRegistry()
			.Register("harness.static", new HostOverrideCalculator(new DateTime(2099, 1, 1)));

		var consumerRule = new NotableDateRule
		{
			Name = "Composite Test Day",
			Strategy = DateResolutionStrategy.Calculator,
			Category = NotableDateCategory.Observance,
			CalculatorKey = "harness.static",
		};

		var service = new NotableDateService(
			ruleProviders: new[] { new InMemoryRuleProvider(consumerRule) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			calculatorRegistry: hostRegistry,
			plugins: new[] { plugin });

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
	public void CompositeCalculatorRegistry_ShouldReportContainsFromEitherLayer()
	{
		var loader = new ExternalPluginLoader(new AllowAllPluginTrustPolicy());
		var plugin = loader.Load(Plugin1Path);

		// Host provides "host.only"; plugin provides "harness.static". The composite should
		// Contains both.
		var hostRegistry = new NotableDateCalculatorRegistry()
			.Register("host.only", new HostOverrideCalculator(new DateTime(2050, 1, 1)));

		NotableDateRule hostOnlyRule = new NotableDateRule
		{
			Name = "Host Only",
			Strategy = DateResolutionStrategy.Calculator,
			Category = NotableDateCategory.Observance,
			CalculatorKey = "host.only",
		};
		NotableDateRule pluginOnlyRule = new NotableDateRule
		{
			Name = "Plugin Only",
			Strategy = DateResolutionStrategy.Calculator,
			Category = NotableDateCategory.Observance,
			CalculatorKey = "harness.static",
		};

		var service = new NotableDateService(
			ruleProviders: new[] { new InMemoryRuleProvider(hostOnlyRule, pluginOnlyRule) },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			calculatorRegistry: hostRegistry,
			plugins: new[] { plugin });

		IReadOnlyList<NotableDate> results = service.GetNotableDates(2050);

		Assert.IsTrue(results.Any(r => r.Name == "Host Only"));
		Assert.IsTrue(results.Any(r => r.Name == "Plugin Only"));
	}

	private sealed class InMemoryRuleProvider : INotableDateRuleProvider
	{
		private readonly NotableDateRule[] _rules;

		public InMemoryRuleProvider(params NotableDateRule[] rules) => _rules = rules;

		public IEnumerable<NotableDateRule> LoadRules() => _rules;
	}

	private sealed class HostOverrideCalculator : INotableDateCalculator
	{
		private readonly DateTime _date;

		public HostOverrideCalculator(DateTime date) => _date = date;

		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) => _date;
	}
}
