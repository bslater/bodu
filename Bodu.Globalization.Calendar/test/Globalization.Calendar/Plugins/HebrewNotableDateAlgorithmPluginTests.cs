// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HebrewNotableDateAlgorithmPluginTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Verifies the wiring of <see cref="HebrewNotableDateAlgorithmPlugin" />: that it advertises the seven Jewish-calendar
/// algorithm keys consumed by the embedded <c>global-jewish.xml</c> resource and that those algorithms resolve correctly
/// when composed into a <see cref="NotableDateService" />.
/// </summary>
[TestClass]
public sealed class HebrewNotableDateAlgorithmPluginTests
{
	private const string JewishResourceName = "Bodu/Globalization/Calendar/Resources/global-jewish.xml";

	private static readonly string[] s_expectedKeys =
	{
		"rosh-hashanah",
		"yom-kippur",
		"sukkot",
		"hanukkah",
		"purim",
		"passover",
		"shavuot",
	};

	/// <summary>
	/// Verifies that the plugin advertises the stable name and version expected by the host.
	/// </summary>
	[TestMethod]
	public void Metadata_ShouldExposeStableNameAndVersion()
	{
		var sut = new HebrewNotableDateAlgorithmPlugin();

		Assert.AreEqual("Bodu.Globalization.Calendar.Hebrew", sut.Name);
		Assert.AreEqual(new Version(1, 0, 0), sut.Version);
	}

	/// <summary>
	/// Verifies that <see cref="HebrewNotableDateAlgorithmPlugin.GetAlgorithms" /> contributes exactly the seven keys
	/// referenced by the embedded <c>global-jewish.xml</c> resource and that no key is registered more than once.
	/// </summary>
	[TestMethod]
	public void GetAlgorithms_ShouldContributeAllJewishHolidayKeysWithoutDuplicates()
	{
		var sut = new HebrewNotableDateAlgorithmPlugin();

		var registrations = sut.GetAlgorithms().ToList();
		var keys = registrations.Select(p => p.Key).ToList();

		CollectionAssert.AreEquivalent(s_expectedKeys, keys);
		Assert.AreEqual(s_expectedKeys.Length, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
		Assert.IsTrue(registrations.All(p => p.Value is HebrewNotableDateAlgorithm));
	}

	/// <summary>
	/// Verifies that each contributed algorithm is bound to the correct Hebrew month and day by resolving each one for
	/// the year 2024 against the values produced directly by an equivalently configured
	/// <see cref="HebrewNotableDateAlgorithm" />.
	/// </summary>
	[DataRow("rosh-hashanah", HebrewMonth.Tishri, 1)]
	[DataRow("yom-kippur", HebrewMonth.Tishri, 10)]
	[DataRow("sukkot", HebrewMonth.Tishri, 15)]
	[DataRow("hanukkah", HebrewMonth.Kislev, 25)]
	[DataRow("purim", HebrewMonth.LastAdar, 14)]
	[DataRow("passover", HebrewMonth.Nisan, 15)]
	[DataRow("shavuot", HebrewMonth.Sivan, 6)]
	[TestMethod]
	public void GetAlgorithms_WhenResolvedForYear_ShouldMatchDirectAlgorithmDate(string key, HebrewMonth month, int day)
	{
		var sut = new HebrewNotableDateAlgorithmPlugin();

		var registration = sut.GetAlgorithms().Single(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
		var expected = new HebrewNotableDateAlgorithm(month, day).GetDate(2024);
		var actual = registration.Value.GetDate(2024);

		Assert.IsNotNull(expected);
		Assert.IsNotNull(actual);
		Assert.AreEqual(expected!.Value, actual!.Value);
	}

	/// <summary>
	/// Verifies the end-to-end wiring: when the plugin is supplied to <see cref="NotableDateService" /> together with the
	/// embedded <c>global-jewish.xml</c> rule resource, every Jewish holiday named by that resource is resolved into a
	/// <see cref="NotableDate" /> for the requested year.
	/// </summary>
	[TestMethod]
	public void NotableDateService_WhenLoadingGlobalJewishWithPlugin_ShouldResolveAllSevenHolidays()
	{
		var provider = new XmlResourceNotableDateRuleProvider(JewishResourceName, new ResourcePathResolver());

		var service = new NotableDateService(
			ruleProviders: new[] { (INotableDateRuleProvider)provider },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			plugins: new[] { (INotableDatePlugin)new HebrewNotableDateAlgorithmPlugin() });

		var results = service.GetNotableDates(2024);
		var resolvedNames = results.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

		Assert.IsTrue(resolvedNames.Contains("Rosh Hashanah"));
		Assert.IsTrue(resolvedNames.Contains("Yom Kippur"));
		Assert.IsTrue(resolvedNames.Contains("Sukkot"));
		Assert.IsTrue(resolvedNames.Contains("Hanukkah"));
		Assert.IsTrue(resolvedNames.Contains("Purim"));
		Assert.IsTrue(resolvedNames.Contains("Passover"));
		Assert.IsTrue(resolvedNames.Contains("Shavuot"));
	}

	/// <summary>
	/// Verifies that a known anchor date — Rosh Hashanah 2024 falling on 3 October — flows from the plugin through the
	/// service unchanged, confirming that the algorithm keyed under <c>rosh-hashanah</c> is the one being invoked.
	/// </summary>
	[TestMethod]
	public void NotableDateService_WhenResolvingRoshHashanah2024_ShouldReturnThirdOfOctober()
	{
		var provider = new XmlResourceNotableDateRuleProvider(JewishResourceName, new ResourcePathResolver());

		var service = new NotableDateService(
			ruleProviders: new[] { (INotableDateRuleProvider)provider },
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
			plugins: new[] { (INotableDatePlugin)new HebrewNotableDateAlgorithmPlugin() });

		NotableDate? roshHashanah = service.GetNotableDates(2024)
			.FirstOrDefault(r => string.Equals(r.Name, "Rosh Hashanah", StringComparison.OrdinalIgnoreCase));

		Assert.IsNotNull(roshHashanah);
		Assert.AreEqual(new DateTime(2024, 10, 3), roshHashanah!.Date);
	}
}
