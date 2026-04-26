// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalJewishResourceTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the end-to-end wiring of the embedded <c>global-jewish.xml</c> resource: every rule resolves through the
/// generic <see cref="DateResolutionStrategy.Algorithm" /> CLR-type fallback path by activating
/// <see cref="Algorithms.HebrewNotableDateAlgorithm" /> with the authored Hebrew month and day, without requiring callers
/// to populate an <see cref="INotableDateAlgorithmRegistry" />.
/// </summary>
[TestClass]
public sealed class GlobalJewishResourceTests
{
	private const string JewishResourceName = "Bodu/Globalization/Calendar/Resources/global-jewish.xml";

	/// <summary>
	/// Builds a <see cref="NotableDateService" /> over the embedded <c>global-jewish.xml</c> resource without any algorithm
	/// registry, override providers, or plugins, exercising the bare CLR-type activation path the XML now relies on.
	/// </summary>
	/// <returns>The configured service.</returns>
	private static NotableDateService CreateBareService() =>
		new NotableDateService(
			ruleProviders: new[]
			{
				(INotableDateRuleProvider)new XmlResourceNotableDateRuleProvider(JewishResourceName, new ResourcePathResolver()),
			},
			weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

	/// <summary>
	/// Verifies that all seven Jewish observances declared in <c>global-jewish.xml</c> resolve into <see cref="NotableDate" />
	/// instances for a year that contains them, confirming each rule's <c>&lt;Algorithm type=… month=… day=… /&gt;</c> wiring.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenLoadingGlobalJewishWithoutRegistry_ShouldResolveAllSevenHolidays()
	{
		var service = CreateBareService();

		var resolvedNames = service.GetNotableDates(2024)
			.Select(r => r.Name)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		Assert.IsTrue(resolvedNames.Contains("Rosh Hashanah"));
		Assert.IsTrue(resolvedNames.Contains("Yom Kippur"));
		Assert.IsTrue(resolvedNames.Contains("Sukkot"));
		Assert.IsTrue(resolvedNames.Contains("Hanukkah"));
		Assert.IsTrue(resolvedNames.Contains("Purim"));
		Assert.IsTrue(resolvedNames.Contains("Passover"));
		Assert.IsTrue(resolvedNames.Contains("Shavuot"));
	}

	/// <summary>
	/// Verifies that each Jewish observance resolves to its known correct Gregorian date for representative years, confirming
	/// that the (HebrewMonth, int) constructor of <see cref="Algorithms.HebrewNotableDateAlgorithm" /> is being invoked with
	/// the month and day authored on the rule.
	/// </summary>
	[DataRow(2024, "Rosh Hashanah", 10, 3)]
	[DataRow(2024, "Yom Kippur", 10, 12)]
	[DataRow(2024, "Sukkot", 10, 17)]
	[DataRow(2024, "Hanukkah", 12, 26)]
	[DataRow(2024, "Purim", 3, 24)]
	[DataRow(2024, "Passover", 4, 23)]
	[DataRow(2023, "Purim", 3, 7)]
	[DataRow(2023, "Passover", 4, 6)]
	[TestMethod]
	public void GetNotableDates_WhenLoadingGlobalJewish_ShouldYieldKnownAnchorDates(int year, string holiday, int expectedMonth, int expectedDay)
	{
		var service = CreateBareService();

		NotableDate? resolved = service.GetNotableDates(year)
			.FirstOrDefault(r => string.Equals(r.Name, holiday, StringComparison.OrdinalIgnoreCase));

		Assert.IsNotNull(resolved);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), resolved!.Date);
	}

	/// <summary>
	/// Verifies that multi-day Jewish observances (Rosh Hashanah, Sukkot, Hanukkah, Passover) preserve their authored
	/// <see cref="NotableDateRule.DurationDays" /> across the resolution pipeline.
	/// </summary>
	[DataRow("Rosh Hashanah", 2)]
	[DataRow("Sukkot", 7)]
	[DataRow("Hanukkah", 8)]
	[DataRow("Passover", 7)]
	[DataRow("Yom Kippur", 1)]
	[DataRow("Purim", 1)]
	[DataRow("Shavuot", 1)]
	[TestMethod]
	public void GetNotableDates_WhenLoadingGlobalJewish_ShouldPreserveAuthoredDurationDays(string holiday, int expectedDuration)
	{
		var service = CreateBareService();

		NotableDate? resolved = service.GetNotableDates(2024)
			.FirstOrDefault(r => string.Equals(r.Name, holiday, StringComparison.OrdinalIgnoreCase));

		Assert.IsNotNull(resolved);
		Assert.AreEqual(expectedDuration, resolved!.DurationDays);
	}
}
