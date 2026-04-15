using System.Linq;

namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Verifies that <see cref="XmlResourceNotableDateRuleProvider" /> correctly flattens the embedded country resources, applying
	/// imports and suppressions under the project's override semantics.
	/// </summary>
	[TestClass]
	public sealed class XmlResourceNotableDateRuleProviderTests
	{
		private const string CommonResource = "Bodu.Globalization.Calendar.Resources.Common.xml";
		private const string ChristianResource = "Bodu.Globalization.Calendar.Resources.Christian.xml";
		private const string UsResource = "Bodu.Globalization.Calendar.Resources.US.xml";
		private const string GbResource = "Bodu.Globalization.Calendar.Resources.GB.xml";
		private const string FrResource = "Bodu.Globalization.Calendar.Resources.FR.xml";
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
		/// Verifies that loading the US resource imports the Common and Christian resources transitively.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenLoadingUsResource_ShouldIncludeImportedRules()
		{
			var provider = new XmlResourceNotableDateRuleProvider(UsResource);

			var rules = provider.LoadRules().ToList();

			// From Common
			Assert.IsTrue(rules.Any(r => r.Name == "New Year's Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Valentine's Day"));

			// From Christian
			Assert.IsTrue(rules.Any(r => r.Name == "Easter Sunday"));
			Assert.IsTrue(rules.Any(r => r.Name == "Christmas Day"));

			// US-specific
			Assert.IsTrue(rules.Any(r => r.Name == "Independence Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Thanksgiving"));
		}

		/// <summary>
		/// Verifies that suppressions declared in a country resource remove the named rules from the flattened set.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenUsResourceSuppressesRule_ShouldRemoveSuppressedRule()
		{
			var provider = new XmlResourceNotableDateRuleProvider(UsResource);

			var rules = provider.LoadRules().ToList();

			Assert.IsFalse(rules.Any(r => r.Name == "Easter Monday"), "US.xml should suppress Easter Monday from the Christian import.");
			Assert.IsFalse(rules.Any(r => r.Name == "International Workers' Day"), "US.xml should suppress International Workers' Day.");
		}

		/// <summary>
		/// Verifies that loading the GB resource preserves Easter Monday (which the UK observes) even though US.xml suppresses it
		/// independently.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenLoadingGbResource_ShouldRetainRulesNotSuppressedByGb()
		{
			var provider = new XmlResourceNotableDateRuleProvider(GbResource);

			var rules = provider.LoadRules().ToList();

			Assert.IsTrue(rules.Any(r => r.Name == "Easter Monday"), "GB.xml does not suppress Easter Monday.");
			Assert.IsTrue(rules.Any(r => r.Name == "Boxing Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Burns Night"));
		}

		/// <summary>
		/// Verifies that locally declared rules in a country file override the inherited rule with the same name.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenLocalRuleHasSameNameAsImportedRule_ShouldUseLocalRule()
		{
			var provider = new XmlResourceNotableDateRuleProvider(GbResource);

			// New Year's Day appears in Common.xml without a territory; GB.xml replaces it with a GB-scoped variant.
			var newYears = provider.LoadRules().Single(r => r.Name == "New Year's Day");

			Assert.AreEqual("GB", newYears.TerritoryCode);
		}

		/// <summary>
		/// Verifies that loading the FR resource exposes the locally renamed International Workers' Day variant ("Fête du Travail") and
		/// removes the original.
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenFrResourceRenamesAndSuppresses_ShouldExposeRenamedVariant()
		{
			var provider = new XmlResourceNotableDateRuleProvider(FrResource);

			var rules = provider.LoadRules().ToList();

			Assert.IsFalse(rules.Any(r => r.Name == "International Workers' Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Fête du Travail"));
			Assert.IsTrue(rules.Any(r => r.Name == "Bastille Day"));
		}

		/// <summary>
		/// Verifies that the default composite resource exposes only the universal Common and Christian sets and does not pull in any
		/// country-specific rules (which would otherwise cross-pollute suppressions between countries).
		/// </summary>
		[TestMethod]
		public void LoadRules_WhenLoadingDefaultComposite_ShouldExposeOnlyUniversalRules()
		{
			var provider = new XmlResourceNotableDateRuleProvider(DefaultResource);

			var rules = provider.LoadRules().ToList();

			Assert.IsTrue(rules.Any(r => r.Name == "New Year's Day"));
			Assert.IsTrue(rules.Any(r => r.Name == "Easter Sunday"));
			Assert.IsFalse(rules.Any(r => r.Name == "Independence Day"), "Country-specific rules should not appear in the default composite.");
			Assert.IsFalse(rules.Any(r => r.Name == "Bastille Day"));
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
	}
}
