using System.Linq;

namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Verifies that <see cref="XmlResourceNotableDateRuleProvider" /> correctly flattens the embedded country resources, applying
	/// cherry-pick directives, per-directive overrides, and <c>UseAll</c> wildcards.
	/// </summary>
	[TestClass]
	public sealed class XmlResourceNotableDateRuleProviderTests
	{
		private const string CommonResource = "Bodu.Globalization.Calendar.Resources.Common.xml";
		private const string ChristianResource = "Bodu.Globalization.Calendar.Resources.Christian.xml";
		private const string UsResource = "Bodu.Globalization.Calendar.Resources.US.xml";
		private const string GbResource = "Bodu.Globalization.Calendar.Resources.GB.xml";
		private const string FrResource = "Bodu.Globalization.Calendar.Resources.FR.xml";
		private const string AuResource = "Bodu.Globalization.Calendar.Resources.AU.xml";
		private const string DefaultResource = "Bodu.Globalization.Calendar.NotableDates.xml";

		/// <summary>
		/// Verifies that loading the standalone Common resource exposes its rules without errors.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenLoadingCommonResource_ShouldExposeUniversalRules()
		{
			var provider = new XmlResourceNotableDateRuleProvider(CommonResource);

			var rules = provider.LoadRules().ToList();

			Assert.IsTrue(rules.Any(r => r.Name == "New Year's Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Halloween"));
			Assert.IsTrue(rules.Any(r => r.Name == "International Workers' Day"));
		}

		/// <summary>
		/// Verifies that loading the US resource pulls in only the explicitly listed rules from Common and Christian — and does not
		/// include any rule that the US file did not opt in to (for example, Easter Monday or Whit Monday).
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenLoadingUsResource_ShouldOnlyIncludeCherryPickedRules()
		{
			var provider = new XmlResourceNotableDateRuleProvider(UsResource);

			var rules = provider.LoadRules().ToList();

			// Cherry-picked from Common
			Assert.IsTrue(rules.Any(r => r.Name == "New Year's Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Valentine's Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Halloween"));

			// Cherry-picked from Christian
			Assert.IsTrue(rules.Any(r => r.Name == "Easter Sunday"));
			Assert.IsTrue(rules.Any(r => r.Name == "Good Friday"));
			Assert.IsTrue(rules.Any(r => r.Name == "Christmas Day"));

			// Locally declared
			Assert.IsTrue(rules.Any(r => r.Name == "Independence Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Thanksgiving"));

			// Not opted in: should be absent.
			Assert.IsFalse(rules.Any(r => r.Name == "Easter Monday"));
			Assert.IsFalse(rules.Any(r => r.Name == "Whit Monday"));
			Assert.IsFalse(rules.Any(r => r.Name == "International Workers' Day"));
			Assert.IsFalse(rules.Any(r => r.Name == "All Saints' Day"));
		}

		/// <summary>
		/// Verifies that <c>Use</c> directives apply scalar overrides to the inherited rule. The US file marks Christmas Day non-working
		/// and tags the territory.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenUseDirectiveAppliesOverrides_ShouldRetagInheritedRule()
		{
			var provider = new XmlResourceNotableDateRuleProvider(UsResource);

			var christmasDay = provider.LoadRules().Single(r => r.Name == "Christmas Day" && r.TerritoryCode == "US");

			Assert.IsTrue(christmasDay.IsNonWorkingDay);
			Assert.AreEqual("US", christmasDay.TerritoryCode);
		}

		/// <summary>
		/// Verifies that loading the GB resource exposes Easter Monday (which the UK observes) because GB.xml explicitly cherry-picks it,
		/// independent of what other country files do.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenLoadingGbResource_ShouldIncludeEasterMonday()
		{
			var provider = new XmlResourceNotableDateRuleProvider(GbResource);

			var rules = provider.LoadRules().ToList();

			Assert.IsTrue(rules.Any(r => r.Name == "Easter Monday"));
			Assert.IsTrue(rules.Any(r => r.Name == "Boxing Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Burns Night"));
		}

		/// <summary>
		/// Verifies that locally declared rules in a country file override the inherited rule with the same (name, territory) key.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenLocalRuleOverridesInheritedRule_ShouldUseLocalRule()
		{
			var provider = new XmlResourceNotableDateRuleProvider(GbResource);

			// New Year's Day is declared locally in GB.xml with a Tag and territory; the inherited Common rule should be overridden.
			var newYears = provider.LoadRules().Single(r => r.Name == "New Year's Day");

			Assert.AreEqual("GB", newYears.TerritoryCode);
			Assert.IsTrue(newYears.Tags.Contains("BankHoliday"));
		}

		/// <summary>
		/// Verifies that loading the FR resource exposes the locally declared "Fête du Travail" while International Workers' Day is
		/// absent (because France did not cherry-pick it).
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenFrResource_ShouldExposeLocalNamesOnly()
		{
			var provider = new XmlResourceNotableDateRuleProvider(FrResource);

			var rules = provider.LoadRules().ToList();

			Assert.IsFalse(rules.Any(r => r.Name == "International Workers' Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Fête du Travail"));
			Assert.IsTrue(rules.Any(r => r.Name == "Bastille Day"));
		}

		/// <summary>
		/// Verifies that the default composite resource pulls in every rule from Common and Christian via the wildcard <c>UseAll</c>,
		/// but no country-specific rules.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenLoadingDefaultComposite_ShouldExposeUniversalRulesViaUseAll()
		{
			var provider = new XmlResourceNotableDateRuleProvider(DefaultResource);

			var rules = provider.LoadRules().ToList();

			Assert.IsTrue(rules.Any(r => r.Name == "New Year's Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Easter Sunday"));
			Assert.IsTrue(rules.Any(r => r.Name == "Christmas Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Halloween"));
			Assert.IsFalse(rules.Any(r => r.Name == "Independence Day"), "Country-specific rules should not appear in the default composite.");
			Assert.IsFalse(rules.Any(r => r.Name == "Bastille Day"));
		}

		/// <summary>
		/// Verifies that loading the AU resource pulls in the cherry-picked Common and Christian rules together with the locally declared
		/// national and state-specific rules, and excludes any rule the AU file did not opt in to.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenLoadingAuResource_ShouldIncludeCherryPickedAndLocalRules()
		{
			var provider = new XmlResourceNotableDateRuleProvider(AuResource);

			var rules = provider.LoadRules().ToList();

			// Cherry-picked from Common
			Assert.IsTrue(rules.Any(r => r.Name == "Valentine's Day" && r.TerritoryCode == "AU"));
			Assert.IsTrue(rules.Any(r => r.Name == "Halloween" && r.TerritoryCode == "AU"));
			Assert.IsTrue(rules.Any(r => r.Name == "Remembrance Day" && r.TerritoryCode == "AU"));

			// Cherry-picked from Christian
			Assert.IsTrue(rules.Any(r => r.Name == "Easter Sunday" && r.TerritoryCode == "AU"));
			Assert.IsTrue(rules.Any(r => r.Name == "Good Friday" && r.TerritoryCode == "AU"));
			Assert.IsTrue(rules.Any(r => r.Name == "Easter Saturday" && r.TerritoryCode == "AU"));
			Assert.IsTrue(rules.Any(r => r.Name == "Easter Monday" && r.TerritoryCode == "AU"));
			Assert.IsTrue(rules.Any(r => r.Name == "Christmas Day" && r.TerritoryCode == "AU"));

			// Locally declared national
			Assert.IsTrue(rules.Any(r => r.Name == "New Year's Day" && r.TerritoryCode == "AU"));
			Assert.IsTrue(rules.Any(r => r.Name == "Australia Day" && r.TerritoryCode == "AU"));
			Assert.IsTrue(rules.Any(r => r.Name == "Anzac Day" && r.TerritoryCode == "AU"));
			Assert.IsTrue(rules.Any(r => r.Name == "Boxing Day" && r.TerritoryCode == "AU"));

			// Locally declared subdivision-scoped (one example per state/territory)
			Assert.IsTrue(rules.Any(r => r.Name == "Bank Holiday" && r.TerritoryCode == "AU-NSW"));
			Assert.IsTrue(rules.Any(r => r.Name == "Melbourne Cup Day" && r.TerritoryCode == "AU-VIC"));
			Assert.IsTrue(rules.Any(r => r.Name == "Royal Queensland Show" && r.TerritoryCode == "AU-QLD"));
			Assert.IsTrue(rules.Any(r => r.Name == "Adelaide Cup Day" && r.TerritoryCode == "AU-SA"));
			Assert.IsTrue(rules.Any(r => r.Name == "Western Australia Day" && r.TerritoryCode == "AU-WA"));
			Assert.IsTrue(rules.Any(r => r.Name == "Eight Hours Day" && r.TerritoryCode == "AU-TAS"));
			Assert.IsTrue(rules.Any(r => r.Name == "Picnic Day" && r.TerritoryCode == "AU-NT"));
			Assert.IsTrue(rules.Any(r => r.Name == "Canberra Day" && r.TerritoryCode == "AU-ACT"));

			// Not opted in: should be absent
			Assert.IsFalse(rules.Any(r => r.Name == "International Workers' Day"));
			Assert.IsFalse(rules.Any(r => r.Name == "Whit Monday"));
			Assert.IsFalse(rules.Any(r => r.Name == "All Saints' Day"));
		}

		/// <summary>
		/// Verifies that the AU resource preserves multiple rules sharing the same canonical name (e.g. "Labour Day" and "King's
		/// Birthday") when they are scoped to different subdivisions, since the override pipeline keys by composite (name, territory).
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenLoadingAuResource_ShouldYieldDistinctLabourDayAndKingsBirthdayRulesPerSubdivision()
		{
			var provider = new XmlResourceNotableDateRuleProvider(AuResource);

			var rules = provider.LoadRules().ToList();

			var labourDayTerritories = rules
				.Where(r => r.Name == "Labour Day")
				.Select(r => r.TerritoryCode)
				.OrderBy(t => t, StringComparer.Ordinal)
				.ToList();

			CollectionAssert.AreEquivalent(
				new[] { "AU-ACT", "AU-NSW", "AU-QLD", "AU-SA", "AU-VIC", "AU-WA" },
				labourDayTerritories);

			var kingsBirthdayTerritories = rules
				.Where(r => r.Name == "King's Birthday")
				.Select(r => r.TerritoryCode)
				.OrderBy(t => t, StringComparer.Ordinal)
				.ToList();

			CollectionAssert.AreEquivalent(
				new[] { "AU-ACT", "AU-NSW", "AU-NT", "AU-QLD", "AU-SA", "AU-TAS", "AU-VIC", "AU-WA" },
				kingsBirthdayTerritories);
		}

		/// <summary>
		/// Verifies that Anzac Day in the AU resource is scoped to the country (territory <c>"AU"</c>) and is flagged as a non-working
		/// day, so that all subdivisions inherit it via the subdivision-aware containment match in <see cref="NotableDateService" />.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenLoadingAuResource_ShouldTagAnzacDayAsNationalNonWorkingDay()
		{
			var provider = new XmlResourceNotableDateRuleProvider(AuResource);

			var anzacDay = provider.LoadRules().Single(r => r.Name == "Anzac Day");

			Assert.AreEqual("AU", anzacDay.TerritoryCode);
			Assert.AreEqual(true, anzacDay.IsNonWorkingDay);
			Assert.IsTrue(anzacDay.Tags.Contains("NationalHoliday"));
		}

		/// <summary>
		/// Verifies that loading a non-existent resource throws a clear <see cref="FileNotFoundException" />.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenResourceMissing_ShouldThrowFileNotFoundException()
		{
			var provider = new XmlResourceNotableDateRuleProvider("Bodu.Globalization.Calendar.Resources.Imaginary.xml");

			Assert.ThrowsExactly<FileNotFoundException>(() => _ = provider.LoadRules().ToList());
		}

		/// <summary>
		/// Verifies that adding a new rule to a source resource does not cascade into a consumer that uses explicit cherry-pick. A new
		/// rule introduced into Common.xml would not show up in US.xml's flattened set unless US.xml explicitly added a Use directive
		/// for it. This test confirms the contract by verifying that the US set is a strict subset of Common's universal rules.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenSourceContainsRulesNotCherryPicked_ShouldNotInheritThem()
		{
			var common = new XmlResourceNotableDateRuleProvider(CommonResource).LoadRules().Select(r => r.Name).ToHashSet();
			var us = new XmlResourceNotableDateRuleProvider(UsResource).LoadRules().Select(r => r.Name).ToHashSet();

			// Pick a rule that exists in Common but is NOT cherry-picked by US.xml.
			Assert.IsTrue(common.Contains("International Workers' Day"));
			Assert.IsFalse(us.Contains("International Workers' Day"));
			Assert.IsTrue(common.Contains("Remembrance Day"));
			Assert.IsFalse(us.Contains("Remembrance Day"));
		}
	}
}
