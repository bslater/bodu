using Bodu.Extensions;
using System.Linq;

namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Verifies the end-to-end behaviour of the embedded <c>AU.xml</c> rule catalogue across the <see cref="NotableDateService" />,
	/// including national-vs-subdivision scoping, weekend substitute handling, the <c>firstYear</c> bound on Reconciliation Day, and
	/// caller-side delineation of country and state/territory entries via <see cref="NotableDate.TerritoryCode" />.
	/// </summary>
	[TestClass]
	public sealed class AustralianNotableDatesTests
	{
		private const string AuResource = "Bodu.Globalization.Calendar.Resources.AU.xml";

		private static readonly string[] ExpectedTerritories =
		{
			"AU",
			"AU-ACT",
			"AU-NSW",
			"AU-NT",
			"AU-QLD",
			"AU-SA",
			"AU-TAS",
			"AU-VIC",
			"AU-WA",
		};

		private static NotableDateService BuildAuService() =>
			new(
				new[] { (INotableDateRuleProvider)new XmlResourceNotableDateRuleProvider(AuResource, new ResourcePathResolver()) },
				CalendarWeekendDefinition.SaturdaySunday);

		/// <summary>
		/// Verifies that querying the country scope returns the four national fixed-date holidays at their unadjusted positions for
		/// year 2026, when none of them collide with a weekend.
		/// </summary>
		[TestMethod]
		public void GetNotableDates_WhenQueryingAu_ShouldIncludeNationalRules_ForYear2026()
		{
			var service = BuildAuService();

			var results = service.GetNotableDates(2026, "AU");

			Assert.IsTrue(results.Any(d => d.Name == "New Year's Day" && d.Date == new DateTime(2026, 1, 1)));
			Assert.IsTrue(results.Any(d => d.Name == "Australia Day" && d.Date == new DateTime(2026, 1, 26)));
			Assert.IsTrue(results.Any(d => d.Name == "Anzac Day" && d.Date == new DateTime(2026, 4, 25)));
			Assert.IsTrue(results.Any(d => d.Name == "Christmas Day" && d.Date == new DateTime(2026, 12, 25)));
		}

		/// <summary>
		/// Verifies that when Australia Day falls on a Sunday (26 January 2020), the service emits both the original date and a substitute
		/// non-working Monday observance, with the adjusted occurrence carrying an <see cref="NotableDate.AdjustmentReason" />.
		/// </summary>
		[TestMethod]
		public void GetNotableDates_WhenAustraliaDayFallsOnSunday_ShouldEmitMondaySubstitute()
		{
			var service = BuildAuService();

			var occurrences = service.GetNotableDates(2020, "AU")
				.Where(d => d.Name == "Australia Day")
				.OrderBy(d => d.Date)
				.ToList();

			Assert.AreEqual(2, occurrences.Count);
			Assert.AreEqual(new DateTime(2020, 1, 26), occurrences[0].Date);
			Assert.IsFalse(occurrences[0].WasAdjusted);
			Assert.AreEqual(new DateTime(2020, 1, 27), occurrences[1].Date);
			Assert.IsTrue(occurrences[1].WasAdjusted);
			Assert.AreEqual(DayOfWeek.Monday, occurrences[1].Date.DayOfWeek);
		}

		/// <summary>
		/// Verifies that querying the Victorian subdivision returns the Victorian Labour Day on the second Monday of March without
		/// returning the New South Wales Labour Day, demonstrating that subdivision-scoped rules survive the composite-key flatten.
		/// </summary>
		[TestMethod]
		public void GetNotableDates_WhenQueryingAuVic_ShouldIncludeVictoriaLabourDay_AndExcludeNswLabourDay()
		{
			var service = BuildAuService();

			var results = service.GetNotableDates(2026, "AU-VIC");

			var labourDay = results.SingleOrDefault(d => d.Name == "Labour Day");
			Assert.IsNotNull(labourDay);
			Assert.AreEqual("AU-VIC", labourDay!.TerritoryCode);
			Assert.AreEqual(new DateTime(2026, 3, 9), labourDay.Date);
		}

		/// <summary>
		/// Verifies the mirror case: querying the New South Wales subdivision returns the NSW Labour Day on the first Monday of October
		/// and not the Victorian variant.
		/// </summary>
		[TestMethod]
		public void GetNotableDates_WhenQueryingAuNsw_ShouldIncludeNswLabourDay_AndExcludeVictoriaLabourDay()
		{
			var service = BuildAuService();

			var results = service.GetNotableDates(2026, "AU-NSW");

			var labourDay = results.SingleOrDefault(d => d.Name == "Labour Day");
			Assert.IsNotNull(labourDay);
			Assert.AreEqual("AU-NSW", labourDay!.TerritoryCode);
			Assert.AreEqual(new DateTime(2026, 10, 5), labourDay.Date);
		}

		/// <summary>
		/// Verifies that every notable date returned for the AU country scope carries a non-null <see cref="NotableDate.TerritoryCode" />,
		/// and that the set of distinct territory codes spans the country plus all eight state and territory subdivisions. This is the
		/// contract that lets callers delineate national entries from subdivision-specific ones in the produced list.
		/// </summary>
		[TestMethod]
		public void GetNotableDates_WhenQueryingAu_ShouldPreserveTerritoryCodeOnEveryEntry_ForDelineation()
		{
			var service = BuildAuService();

			var results = service.GetNotableDates(2026, "AU");

			Assert.IsTrue(results.Count > 0);
			Assert.IsTrue(results.All(d => !string.IsNullOrEmpty(d.TerritoryCode)),
				"Every Australian notable date must carry its originating TerritoryCode so callers can delineate national from subdivision entries.");

			var distinctTerritories = results
				.Select(d => d.TerritoryCode!)
				.Distinct()
				.OrderBy(t => t, StringComparer.Ordinal)
				.ToArray();

			CollectionAssert.AreEquivalent(ExpectedTerritories, distinctTerritories);
		}

		/// <summary>
		/// Verifies that the Queensland King's Birthday resolves to the first Monday of October for years from 2016 onwards, reflecting
		/// the statutory change that moved the holiday out of June.
		/// </summary>
		[TestMethod]
		public void GetNotableDates_WhenQueryingAuQld_ShouldResolveKingsBirthdayToOctober_ForYear2026()
		{
			var service = BuildAuService();

			var results = service.GetNotableDates(2026, "AU-QLD");

			var kingsBirthday = results.SingleOrDefault(d => d.Name == "King's Birthday");
			Assert.IsNotNull(kingsBirthday);
			Assert.AreEqual(new DateTime(2026, 10, 5), kingsBirthday!.Date);
		}

		/// <summary>
		/// Verifies that Reconciliation Day, introduced in the ACT in 2018, is suppressed for any year before 2018 by the rule's
		/// <c>firstYear</c> bound.
		/// </summary>
		[TestMethod]
		public void GetNotableDates_WhenQueryingAuAct_ForYear2017_ShouldNotIncludeReconciliationDay()
		{
			var service = BuildAuService();

			var results = service.GetNotableDates(2017, "AU-ACT");

			Assert.IsFalse(results.Any(d => d.Name == "Reconciliation Day"));
		}

		/// <summary>
		/// Verifies that Reconciliation Day appears for the ACT from 2018 onwards and resolves to a Monday in late May.
		/// </summary>
		[TestMethod]
		public void GetNotableDates_WhenQueryingAuAct_ForYear2018_ShouldIncludeReconciliationDay()
		{
			var service = BuildAuService();

			var results = service.GetNotableDates(2018, "AU-ACT");

			var reconciliation = results.SingleOrDefault(d => d.Name == "Reconciliation Day");
			Assert.IsNotNull(reconciliation);
			Assert.AreEqual(new DateTime(2018, 5, 28), reconciliation!.Date);
			Assert.AreEqual(DayOfWeek.Monday, reconciliation.Date.DayOfWeek);
		}

		/// <summary>
		/// Verifies that Melbourne Cup Day for Victoria resolves to the first Tuesday of November (3 November in 2026), equivalent to the
		/// statutory definition "the Tuesday in the week in which the first Tuesday in November occurs".
		/// </summary>
		[TestMethod]
		public void GetNotableDates_WhenMelbourneCupDay_ForYear2026_ShouldResolveToFirstTuesdayOfNovember()
		{
			var service = BuildAuService();

			var results = service.GetNotableDates(2026, "AU-VIC");

			var melbourneCup = results.SingleOrDefault(d => d.Name == "Melbourne Cup Day");
			Assert.IsNotNull(melbourneCup);
			Assert.AreEqual(new DateTime(2026, 11, 3), melbourneCup!.Date);
			Assert.AreEqual(DayOfWeek.Tuesday, melbourneCup.Date.DayOfWeek);
		}

		/// <summary>
		/// Verifies that querying NSW for 2026 reports the substitute Monday after Boxing Day as a non-working day, since 26 December
		/// 2026 falls on a Saturday and the national Boxing Day rule rolls weekend occurrences forward.
		/// </summary>
		[TestMethod]
		public void IsNonWorkingDay_WhenBoxingDayOnSaturday_ShouldReturnTrueForSubstituteMonday_ForAuNsw()
		{
			var service = BuildAuService();

			// 26 December 2026 is a Saturday; the next non-weekend day is Monday 28 December.
			Assert.IsTrue(service.IsNonWorkingDay(new DateTime(2026, 12, 28), "AU-NSW"));
		}

		/// <summary>
		/// Verifies that querying any Australian subdivision returns the country-level Anzac Day on 25 April, demonstrating that the
		/// territory containment match expands the parent country's rules into every subdivision.
		/// </summary>
		[TestMethod]
		[DataRow("AU-NSW")]
		[DataRow("AU-VIC")]
		[DataRow("AU-QLD")]
		[DataRow("AU-SA")]
		[DataRow("AU-WA")]
		[DataRow("AU-TAS")]
		[DataRow("AU-NT")]
		[DataRow("AU-ACT")]
		public void GetNotableDates_WhenQueryingAnySubdivision_ShouldIncludeNationalAnzacDay_ForYear2026(string subdivision)
		{
			var service = BuildAuService();

			var results = service.GetNotableDates(2026, subdivision);

			// Anzac Day can fall on a weekend (25 April 2026 is a Saturday); WA and NT emit a substitute Monday as well,
			// so filter to the original observance entry rather than asserting a single occurrence per subdivision.
			var anzacDay = results.SingleOrDefault(d => d.Name == "Anzac Day" && !d.WasAdjusted);
			Assert.IsNotNull(anzacDay, $"Anzac Day should be visible to subdivision {subdivision}.");
			Assert.AreEqual("AU", anzacDay!.TerritoryCode);
			Assert.AreEqual(new DateTime(2026, 4, 25), anzacDay.Date);
		}

	/// <summary>
	/// Verifies that Western Australia observes a substitute Monday when Anzac Day falls on a Saturday (25 April 2020), emitting
	/// both the original Saturday observance and the adjusted Monday substitute scoped to <c>AU-WA</c>.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenAnzacDayOnSaturday_ShouldEmitMondaySubstitute_ForAuWa()
	{
		var service = BuildAuService();

		var occurrences = service.GetNotableDates(2020, "AU-WA")
			.Where(d => d.Name == "Anzac Day")
			.OrderBy(d => d.Date)
			.ToList();

		Assert.AreEqual(2, occurrences.Count);
		Assert.AreEqual(new DateTime(2020, 4, 25), occurrences[0].Date);
		Assert.IsFalse(occurrences[0].WasAdjusted);
		Assert.AreEqual(new DateTime(2020, 4, 27), occurrences[1].Date);
		Assert.IsTrue(occurrences[1].WasAdjusted);
		Assert.AreEqual(DayOfWeek.Monday, occurrences[1].Date.DayOfWeek);
		Assert.AreEqual("AU-WA", occurrences[1].TerritoryCode,
			"The substitute Monday must be tagged with the narrower AU-WA scope so consumers know it's not a country-wide shift.");
	}

	/// <summary>
	/// Verifies that New South Wales does NOT observe a substitute Monday when Anzac Day falls on a Saturday (25 April 2020):
	/// only the original observance is emitted, reflecting the per-state divergence in Anzac Day weekend treatment.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenAnzacDayOnSaturday_ShouldNotEmitSubstitute_ForAuNsw()
	{
		var service = BuildAuService();

		var occurrences = service.GetNotableDates(2020, "AU-NSW")
			.Where(d => d.Name == "Anzac Day")
			.ToList();

		Assert.AreEqual(1, occurrences.Count);
		Assert.AreEqual(new DateTime(2020, 4, 25), occurrences[0].Date);
		Assert.IsFalse(occurrences[0].WasAdjusted);
	}

	/// <summary>
	/// Verifies that the Northern Territory observes a substitute Monday when Anzac Day falls on a Sunday (25 April 2021).
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenAnzacDayOnSunday_ShouldEmitMondaySubstitute_ForAuNt()
	{
		var service = BuildAuService();

		var occurrences = service.GetNotableDates(2021, "AU-NT")
			.Where(d => d.Name == "Anzac Day")
			.OrderBy(d => d.Date)
			.ToList();

		Assert.AreEqual(2, occurrences.Count);
		Assert.AreEqual(new DateTime(2021, 4, 25), occurrences[0].Date);
		Assert.IsFalse(occurrences[0].WasAdjusted);
		Assert.AreEqual(new DateTime(2021, 4, 26), occurrences[1].Date);
		Assert.IsTrue(occurrences[1].WasAdjusted);
		Assert.AreEqual("AU-NT", occurrences[1].TerritoryCode);
	}
}
}
