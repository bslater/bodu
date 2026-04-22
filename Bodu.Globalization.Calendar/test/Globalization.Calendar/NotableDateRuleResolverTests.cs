using Bodu.Extensions;
using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Verifies the behaviour of <see cref="NotableDateRuleResolver" /> across every <see cref="DateResolutionStrategy" /> and edge case.
	/// </summary>
	[TestClass]
	public sealed class NotableDateRuleResolverTests
	{
		private static NotableDateRule FixedRule(string name, int month, int day, int? firstYear = null, int? lastYear = null, int? occurrenceYears = null) =>
			new()
			{
				Name = name,
				Strategy = DateResolutionStrategy.Fixed,
				Category = NotableDateCategory.Holiday,
				Month = month,
				Day = day,
				FirstYear = firstYear,
				LastYear = lastYear,
				OccurrenceYears = occurrenceYears,
			};

		private static NotableDateRule OffsetRule(string name, string anchor, int offset) =>
			new()
			{
				Name = name,
				Strategy = DateResolutionStrategy.OffsetFromAnchor,
				Category = NotableDateCategory.Observance,
				AnchorRuleName = anchor,
				OffsetDays = offset,
			};

		/// <summary>
		/// Verifies that a Fixed rule resolves to the calendar day specified by its month and day attributes.
		/// </summary>
		[TestMethod]
		public void ResolveAnchorDate_WhenFixedRule_ShouldReturnSpecifiedMonthAndDay()
		{
			var resolver = new NotableDateRuleResolver(new[] { FixedRule("New Year's Day", 1, 1) });

			var date = resolver.ResolveAnchorDate(FixedRule("New Year's Day", 1, 1), 2026);

			Assert.AreEqual(new DateTime(2026, 1, 1), date);
		}

		/// <summary>
		/// Verifies that a rule whose first year exceeds the requested year resolves to <see langword="null" />.
		/// </summary>
		[TestMethod]
		public void ResolveAnchorDate_WhenYearBeforeFirstYear_ShouldReturnNull()
		{
			var rule = FixedRule("Future Holiday", 1, 1, firstYear: 2030);
			var resolver = new NotableDateRuleResolver(new[] { rule });

			Assert.IsNull(resolver.ResolveAnchorDate(rule, 2025));
		}

		/// <summary>
		/// Verifies that a rule whose last year is below the requested year resolves to <see langword="null" />.
		/// </summary>
		[TestMethod]
		public void ResolveAnchorDate_WhenYearAfterLastYear_ShouldReturnNull()
		{
			var rule = FixedRule("Sunset Holiday", 1, 1, lastYear: 2020);
			var resolver = new NotableDateRuleResolver(new[] { rule });

			Assert.IsNull(resolver.ResolveAnchorDate(rule, 2025));
		}

		/// <summary>
		/// Verifies that <see cref="NotableDateRule.OccurrenceYears" /> is honoured: a quadrennial cadence anchored on 2024 produces a date
		/// in 2024 but not in 2025.
		/// </summary>
		[TestMethod]
		public void ResolveAnchorDate_WhenOccurrenceYearsSetAndYearOnCadence_ShouldReturnDate()
		{
			var rule = FixedRule("Olympics", 7, 1, firstYear: 2024, occurrenceYears: 4);
			var resolver = new NotableDateRuleResolver(new[] { rule });

			Assert.IsNotNull(resolver.ResolveAnchorDate(rule, 2024));
			Assert.IsNotNull(resolver.ResolveAnchorDate(rule, 2028));
		}

		/// <summary>
		/// Verifies that an off-cadence year resolves to <see langword="null" /> when <see cref="NotableDateRule.OccurrenceYears" /> is set.
		/// </summary>
		[TestMethod]
		public void ResolveAnchorDate_WhenOccurrenceYearsSetAndYearOffCadence_ShouldReturnNull()
		{
			var rule = FixedRule("Olympics", 7, 1, firstYear: 2024, occurrenceYears: 4);
			var resolver = new NotableDateRuleResolver(new[] { rule });

			Assert.IsNull(resolver.ResolveAnchorDate(rule, 2025));
			Assert.IsNull(resolver.ResolveAnchorDate(rule, 2027));
		}

		/// <summary>
		/// Verifies that an OffsetFromAnchor rule resolves through its referenced anchor.
		/// </summary>
		[TestMethod]
		public void ResolveAnchorDate_WhenOffsetFromAnchorRule_ShouldResolveThroughAnchor()
		{
			var anchor = FixedRule("Anchor", 4, 10);
			var offset = OffsetRule("Two Days Earlier", "Anchor", -2);
			var resolver = new NotableDateRuleResolver(new[] { anchor, offset });

			var date = resolver.ResolveAnchorDate(offset, 2025);

			Assert.AreEqual(new DateTime(2025, 4, 8), date);
		}

		/// <summary>
		/// Verifies that an OffsetFromAnchor rule throws when its anchor cannot be located.
		/// </summary>
		[TestMethod]
		public void ResolveAnchorDate_WhenAnchorMissing_ShouldThrowInvalidOperationException()
		{
			var offset = OffsetRule("Orphan", "Missing Anchor", -1);
			var resolver = new NotableDateRuleResolver(new[] { offset });

			Assert.ThrowsExactly<InvalidOperationException>(() => resolver.ResolveAnchorDate(offset, 2025));
		}

		/// <summary>
		/// Verifies that a circular OffsetFromAnchor chain is detected and reported as an
		/// <see cref="InvalidOperationException" />.
		/// </summary>
		[TestMethod]
		public void ResolveAnchorDate_WhenCircularChain_ShouldThrowInvalidOperationException()
		{
			var a = OffsetRule("A", "B", 1);
			var b = OffsetRule("B", "A", 1);
			var resolver = new NotableDateRuleResolver(new[] { a, b });

			Assert.ThrowsExactly<InvalidOperationException>(() => resolver.ResolveAnchorDate(a, 2025));
		}

		/// <summary>
		/// Verifies that <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> resolves to the requested ordinal weekday.
		/// </summary>
		[TestMethod]
		public void ResolveAnchorDate_WhenDayOfWeekInMonthRule_ShouldReturnNthWeekday()
		{
			var rule = new NotableDateRule
			{
				Name = "Second Sunday of May",
				Strategy = DateResolutionStrategy.DayOfWeekInMonth,
				Category = NotableDateCategory.Observance,
				Month = 5,
				DayOfWeek = DayOfWeek.Sunday,
				WeekOrdinal = WeekOfMonthOrdinal.Second,
			};

			var resolver = new NotableDateRuleResolver(new[] { rule });

			var date = resolver.ResolveAnchorDate(rule, 2025);

			Assert.AreEqual(new DateTime(2025, 5, 11), date);
		}

		/// <summary>
		/// Verifies that <see cref="DateResolutionStrategy.Calculator" /> rules look up their calculator via the supplied registry.
		/// </summary>
		[TestMethod]
		public void ResolveAnchorDate_WhenCalculatorRuleAndRegistered_ShouldUseRegistry()
		{
			var rule = new NotableDateRule
			{
				Name = "Static Calc",
				Strategy = DateResolutionStrategy.Calculator,
				Category = NotableDateCategory.Observance,
				CalculatorKey = "static",
			};

			var registry = new NotableDateCalculatorRegistry()
				.Register("static", new StaticCalculator(new DateTime(2025, 6, 15)));

			var resolver = new NotableDateRuleResolver(new[] { rule }, registry);

			var date = resolver.ResolveAnchorDate(rule, 2025);

			Assert.AreEqual(new DateTime(2025, 6, 15), date);
		}

		/// <summary>
		/// Verifies that a Calculator rule with no registered key and no fallback type resolves to <see langword="null" />.
		/// </summary>
		[TestMethod]
		public void ResolveAnchorDate_WhenCalculatorRuleAndNoCalculator_ShouldReturnNull()
		{
			var rule = new NotableDateRule
			{
				Name = "Missing Calc",
				Strategy = DateResolutionStrategy.Calculator,
				Category = NotableDateCategory.Observance,
				CalculatorKey = "missing",
			};

			var resolver = new NotableDateRuleResolver(new[] { rule });

			Assert.IsNull(resolver.ResolveAnchorDate(rule, 2025));
		}

		/// <summary>
		/// Verifies that <see cref="NotableDateRuleResolver.IsApplicable" /> returns <see langword="false" /> for off-cadence years and
		/// <see langword="true" /> for on-cadence years.
		/// </summary>
		[TestMethod]
		public void IsApplicable_WhenOccurrenceYearsAndYearOffCadence_ShouldReturnFalse()
		{
			var rule = FixedRule("Quadrennial", 1, 1, firstYear: 2000, occurrenceYears: 4);

			Assert.IsTrue(NotableDateRuleResolver.IsApplicable(rule, 2000));
			Assert.IsTrue(NotableDateRuleResolver.IsApplicable(rule, 2004));
			Assert.IsFalse(NotableDateRuleResolver.IsApplicable(rule, 2001));
		}

		private sealed class StaticCalculator : INotableDateCalculator
		{
			private readonly DateTime _value;

			public StaticCalculator(DateTime value) => _value = value;

			public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) => _value;
		}
	}
}
