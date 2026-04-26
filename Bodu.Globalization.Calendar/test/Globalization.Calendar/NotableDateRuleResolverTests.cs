// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleResolverTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

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
	/// Verifies that <see cref="DateResolutionStrategy.Algorithm" /> rules look up their algorithm via the supplied registry.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmRuleAndRegistered_ShouldUseRegistry()
	{
		var rule = new NotableDateRule
		{
			Name = "Static Calc",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmKey = "static",
		};

		var registry = new NotableDateAlgorithmRegistry()
			.Register("static", new StaticAlgorithm(new DateTime(2025, 6, 15)));

		var resolver = new NotableDateRuleResolver(new[] { rule }, registry);

		var date = resolver.ResolveAnchorDate(rule, 2025);

		Assert.AreEqual(new DateTime(2025, 6, 15), date);
	}

	/// <summary>
	/// Verifies that a Algorithm rule with no registered key and no fallback type resolves to <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmRuleAndNoAlgorithm_ShouldReturnNull()
	{
		var rule = new NotableDateRule
		{
			Name = "Missing Calc",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmKey = "missing",
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

	// -----------------------------------------------------------------------------------------
	// Argument validation + constructor paths
	// -----------------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that the constructor throws <see cref="ArgumentNullException" /> when
	/// <paramref name="rules" /> is <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void Constructor_WhenRulesIsNull_ShouldThrowArgumentNullException()
	{
		var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			_ = new NotableDateRuleResolver(null!);
		});

		Assert.AreEqual("rules", ex.ParamName);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateRuleResolver.ResolveAnchorDate" /> throws
	/// <see cref="ArgumentNullException" /> when the rule argument is <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenRuleIsNull_ShouldThrowArgumentNullException()
	{
		var resolver = new NotableDateRuleResolver(Array.Empty<NotableDateRule>());

		var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			_ = resolver.ResolveAnchorDate(null!, 2025);
		});

		Assert.AreEqual("rule", ex.ParamName);
	}

	/// <summary>
	/// Verifies that a rule with an unrecognised <see cref="DateResolutionStrategy" /> throws
	/// <see cref="NotSupportedException" /> naming the rule.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenStrategyIsUnsupported_ShouldThrowNotSupportedException()
	{
		var rule = new NotableDateRule
		{
			Name = "Alien",
			Strategy = (DateResolutionStrategy)999,
			Category = NotableDateCategory.Observance,
		};
		var resolver = new NotableDateRuleResolver(new[] { rule });

		var ex = Assert.ThrowsExactly<NotSupportedException>(() =>
		{
			_ = resolver.ResolveAnchorDate(rule, 2025);
		});

		Assert.IsTrue(ex.Message.Contains("Alien", StringComparison.Ordinal));
	}

	// -----------------------------------------------------------------------------------------
	// Strategy-specific edge cases
	// -----------------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that a Fixed rule with missing <see cref="NotableDateRule.Month" /> or
	/// <see cref="NotableDateRule.Day" /> resolves to <see langword="null" /> rather than
	/// throwing.
	/// </summary>
	[DataRow(null!, null!)]
	[DataRow(5, null!)]
	[DataRow(null!, 15)]
	[TestMethod]
	public void ResolveAnchorDate_WhenFixedRuleMissingMonthOrDay_ShouldReturnNull(int? month, int? day)
	{
		var rule = new NotableDateRule
		{
			Name = "Incomplete",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = month,
			Day = day,
		};
		var resolver = new NotableDateRuleResolver(new[] { rule });

		Assert.IsNull(resolver.ResolveAnchorDate(rule, 2025));
	}

	/// <summary>
	/// Verifies that a DayOfWeekInMonth rule with any of Month/DayOfWeek/WeekOrdinal missing
	/// resolves to <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenDayOfWeekInMonthRuleMissingRequiredFields_ShouldReturnNull()
	{
		var rule = new NotableDateRule
		{
			Name = "Incomplete DOW",
			Strategy = DateResolutionStrategy.DayOfWeekInMonth,
			Category = NotableDateCategory.Observance,
			Month = 5,
			DayOfWeek = null,
			WeekOrdinal = null,
		};
		var resolver = new NotableDateRuleResolver(new[] { rule });

		Assert.IsNull(resolver.ResolveAnchorDate(rule, 2025));
	}

	/// <summary>
	/// Verifies that an OffsetFromAnchor rule with a missing/whitespace anchor name short
	/// -circuits to <see langword="null" /> rather than throwing a lookup exception.
	/// </summary>
	[DataRow(null!)]
	[DataRow("")]
	[DataRow("   ")]
	[TestMethod]
	public void ResolveAnchorDate_WhenOffsetFromAnchorHasNoAnchorName_ShouldReturnNull(string? anchor)
	{
		var rule = new NotableDateRule
		{
			Name = "Orphan",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Observance,
			AnchorRuleName = anchor,
			OffsetDays = 1,
		};
		var resolver = new NotableDateRuleResolver(new[] { rule });

		Assert.IsNull(resolver.ResolveAnchorDate(rule, 2025));
	}

	/// <summary>
	/// Verifies that an OffsetFromAnchor rule with no <see cref="NotableDateRule.OffsetDays" />
	/// returns <see langword="null" /> rather than defaulting to zero.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenOffsetFromAnchorHasNoOffsetDays_ShouldReturnNull()
	{
		var anchor = FixedRule("Anchor", 4, 10);
		var offset = new NotableDateRule
		{
			Name = "Derived",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Observance,
			AnchorRuleName = "Anchor",
			OffsetDays = null,
		};
		var resolver = new NotableDateRuleResolver(new[] { anchor, offset });

		Assert.IsNull(resolver.ResolveAnchorDate(offset, 2025));
	}

	/// <summary>
	/// Verifies that an OffsetFromAnchor rule returns <see langword="null" /> when the anchor
	/// rule itself does not apply to the requested year.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenOffsetFromAnchorAnchorNotApplicable_ShouldReturnNull()
	{
		var anchor = FixedRule("Anchor", 4, 10, firstYear: 2030);
		var offset = OffsetRule("Derived", "Anchor", 1);
		var resolver = new NotableDateRuleResolver(new[] { anchor, offset });

		Assert.IsNull(resolver.ResolveAnchorDate(offset, 2025));
	}

	/// <summary>
	/// Verifies that a Algorithm rule uses the CLR <see cref="NotableDateRule.AlgorithmType" />
	/// fallback when no algorithm registry is configured or the registry does not contain the
	/// requested key.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmRuleFallsBackToType_ShouldInstantiateAndInvoke()
	{
		var rule = new NotableDateRule
		{
			Name = "Legacy Calc",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmType = typeof(FixedJuneAlgorithm),
		};
		var resolver = new NotableDateRuleResolver(new[] { rule });

		DateTime? result = resolver.ResolveAnchorDate(rule, 2025);

		Assert.AreEqual(new DateTime(2025, 6, 1), result);
	}

	/// <summary>
	/// Verifies that a Algorithm rule with a registry that has the key wins over the CLR
	/// <see cref="NotableDateRule.AlgorithmType" /> fallback.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmRuleHasBothKeyAndType_ShouldPreferRegistry()
	{
		var rule = new NotableDateRule
		{
			Name = "Dual",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmKey = "key",
			AlgorithmType = typeof(FixedJuneAlgorithm),
		};
		var registry = new NotableDateAlgorithmRegistry()
			.Register("key", new StaticAlgorithm(new DateTime(2025, 3, 15)));
		var resolver = new NotableDateRuleResolver(new[] { rule }, registry);

		DateTime? result = resolver.ResolveAnchorDate(rule, 2025);

		Assert.AreEqual(new DateTime(2025, 3, 15), result);
	}

	/// <summary>
	/// Verifies that a Algorithm rule whose CLR type does not implement
	/// <see cref="INotableDateAlgorithm" /> gracefully returns <see langword="null" /> rather
	/// than throwing.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmTypeIsNotAlgorithm_ShouldReturnNull()
	{
		var rule = new NotableDateRule
		{
			Name = "Wrong Type",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmType = typeof(NotAAlgorithm),
		};
		var resolver = new NotableDateRuleResolver(new[] { rule });

		Assert.IsNull(resolver.ResolveAnchorDate(rule, 2025));
	}

	/// <summary>
	/// Verifies that anchor rules whose <see cref="NotableDateRule.Name" /> is null, empty, or
	/// whitespace are skipped during the name-index build: a later offset rule referencing a
	/// legitimate anchor name cannot resolve because the would-be anchor was not indexed.
	/// </summary>
	[DataRow(null!)]
	[DataRow("")]
	[DataRow("   ")]
	[TestMethod]
	public void Constructor_WhenAnchorNameIsNullOrWhitespace_ShouldSkipFromNameIndex(string? anchorName)
	{
		var unindexedAnchor = new NotableDateRule
		{
			Name = anchorName ?? string.Empty,
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 4,
			Day = 10,
		};
		var offset = OffsetRule("Derived", "RealAnchor", 1);
		var resolver = new NotableDateRuleResolver(new[] { unindexedAnchor, offset });

		Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = resolver.ResolveAnchorDate(offset, 2025);
		});
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateRuleResolver.IsApplicable" /> enforces both the
	/// first/last year window and the cadence test derived from
	/// <see cref="NotableDateRule.OccurrenceYears" />. Each row states the expected outcome
	/// directly to make the truth table self-auditing.
	/// </summary>
	[DataRow(2025, null, null, null, true)]    // no bounds, no cadence → always
	[DataRow(2025, 2020, 2030, null, true)]    // in-window, no cadence
	[DataRow(2019, 2020, 2030, null, false)]   // below firstYear
	[DataRow(2031, 2020, 2030, null, false)]   // above lastYear
	[DataRow(2024, 2024, null, 4, true)]       // on cadence (Olympic origin)
	[DataRow(2025, 2024, null, 4, false)]      // off cadence
	[DataRow(2028, 2024, null, 4, true)]       // on cadence + 1 cycle
	[DataRow(2020, null, null, 4, true)]       // cadence origin 0 ⇒ 2020 % 4 == 0
	[DataRow(2021, null, null, 4, false)]      // cadence origin 0 ⇒ 2021 % 4 != 0
	[DataRow(2025, null, null, 1, true)]       // cadence 1 ⇒ always
	[DataRow(2025, null, null, 0, true)]       // cadence 0 is ignored (not > 0)
	[TestMethod]
	public void IsApplicable_TruthTable(int year, int? first, int? last, int? cadence, bool expected)
	{
		var rule = new NotableDateRule
		{
			Name = "r",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			FirstYear = first,
			LastYear = last,
			OccurrenceYears = cadence,
		};

		Assert.AreEqual(expected, NotableDateRuleResolver.IsApplicable(rule, year));
	}

	/// <summary>
	/// Verifies that an Algorithm rule whose <see cref="NotableDateRule.AlgorithmType" /> exposes a two-argument
	/// constructor of the form <c>(EnumMonth, int)</c> is activated using <see cref="NotableDateRule.AlgorithmMonth" />
	/// and <see cref="NotableDateRule.AlgorithmDay" />, with the month token parsed against the constructor's enum type.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmTypeHasEnumIntCtor_ShouldActivateWithAuthoredMonthAndDay()
	{
		var rule = new NotableDateRule
		{
			Name = "Enum-Month Algorithm",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmType = typeof(EnumMonthAlgorithm),
			AlgorithmMonth = "March",
			AlgorithmDay = 17,
		};
		var resolver = new NotableDateRuleResolver(new[] { rule });

		DateTime? result = resolver.ResolveAnchorDate(rule, 2025);

		Assert.AreEqual(new DateTime(2025, 3, 17), result);
	}

	/// <summary>
	/// Verifies that the enum-month parse is case-insensitive — a lowercase month token still resolves to the matching
	/// enum value during constructor selection.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmMonthIsCaseInsensitive_ShouldStillActivate()
	{
		var rule = new NotableDateRule
		{
			Name = "Lowercase Month",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmType = typeof(EnumMonthAlgorithm),
			AlgorithmMonth = "july",
			AlgorithmDay = 4,
		};
		var resolver = new NotableDateRuleResolver(new[] { rule });

		DateTime? result = resolver.ResolveAnchorDate(rule, 2025);

		Assert.AreEqual(new DateTime(2025, 7, 4), result);
	}

	/// <summary>
	/// Verifies that an Algorithm rule whose <see cref="NotableDateRule.AlgorithmType" /> exposes an
	/// <c>(int, int)</c> constructor is activated when <see cref="NotableDateRule.AlgorithmMonth" /> parses as an integer.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmTypeHasIntIntCtor_ShouldActivateWithNumericMonthToken()
	{
		var rule = new NotableDateRule
		{
			Name = "Int-Month Algorithm",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmType = typeof(IntMonthAlgorithm),
			AlgorithmMonth = "11",
			AlgorithmDay = 5,
		};
		var resolver = new NotableDateRuleResolver(new[] { rule });

		DateTime? result = resolver.ResolveAnchorDate(rule, 2025);

		Assert.AreEqual(new DateTime(2025, 11, 5), result);
	}

	/// <summary>
	/// Verifies that an Algorithm rule whose <see cref="NotableDateRule.AlgorithmType" /> exposes a <c>(string, int)</c>
	/// constructor receives the raw <see cref="NotableDateRule.AlgorithmMonth" /> token without coercion.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmTypeHasStringIntCtor_ShouldActivateWithRawMonthToken()
	{
		var rule = new NotableDateRule
		{
			Name = "String-Month Algorithm",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmType = typeof(StringMonthAlgorithm),
			AlgorithmMonth = "Custom",
			AlgorithmDay = 9,
		};
		var resolver = new NotableDateRuleResolver(new[] { rule });

		DateTime? result = resolver.ResolveAnchorDate(rule, 2025);

		Assert.AreEqual(new DateTime(2025, 9, 9), result);
		Assert.AreEqual("Custom", StringMonthAlgorithm.LastMonthToken);
	}

	/// <summary>
	/// Verifies that supplying a month token that does not parse to the constructor's enum type causes the resolver to
	/// return <see langword="null" /> rather than activating the algorithm or throwing.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmMonthDoesNotMatchEnum_ShouldReturnNull()
	{
		var rule = new NotableDateRule
		{
			Name = "Unparseable Month",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmType = typeof(EnumMonthAlgorithm),
			AlgorithmMonth = "Smarch",
			AlgorithmDay = 1,
		};
		var resolver = new NotableDateRuleResolver(new[] { rule });

		Assert.IsNull(resolver.ResolveAnchorDate(rule, 2025));
	}

	/// <summary>
	/// Verifies that when the rule supplies <see cref="NotableDateRule.AlgorithmMonth" /> and
	/// <see cref="NotableDateRule.AlgorithmDay" /> but the algorithm type only exposes a parameterless constructor, the
	/// resolver falls through to that constructor rather than failing.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmTypeOnlyHasParameterlessCtor_ShouldFallThroughToParameterless()
	{
		var rule = new NotableDateRule
		{
			Name = "Parameterless",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Observance,
			AlgorithmType = typeof(FixedJuneAlgorithm),
			AlgorithmMonth = "Tishri",
			AlgorithmDay = 1,
		};
		var resolver = new NotableDateRuleResolver(new[] { rule });

		DateTime? result = resolver.ResolveAnchorDate(rule, 2025);

		Assert.AreEqual(new DateTime(2025, 6, 1), result);
	}

	private sealed class StaticAlgorithm : INotableDateAlgorithm
	{
		private readonly DateTime _value;

		public StaticAlgorithm(DateTime value) => _value = value;

		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) => _value;
	}

	/// <summary>
	/// Algorithm-type fallback double. Activator creates one via its parameterless ctor.
	/// </summary>
	public sealed class FixedJuneAlgorithm : INotableDateAlgorithm
	{
		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) =>
			new DateTime(year, 6, 1);
	}

	/// <summary>
	/// Intentionally not an <see cref="INotableDateAlgorithm" /> — used to test the
	/// resolver's defensive fallback.
	/// </summary>
	public sealed class NotAAlgorithm
	{
	}

	/// <summary>
	/// Algorithm-type fallback double exposing a <c>(MonthOfYear, int)</c> constructor; used to verify the resolver's
	/// generic enum-month constructor selection logic.
	/// </summary>
	public sealed class EnumMonthAlgorithm : INotableDateAlgorithm
	{
		private readonly MonthOfYear _month;
		private readonly int _day;

		public EnumMonthAlgorithm(MonthOfYear month, int day)
		{
			_month = month;
			_day = day;
		}

		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) =>
			new DateTime(year, (int)_month, _day);
	}

	/// <summary>
	/// Algorithm-type fallback double exposing an <c>(int, int)</c> constructor; used to verify integer-coerced month
	/// constructor selection.
	/// </summary>
	public sealed class IntMonthAlgorithm : INotableDateAlgorithm
	{
		private readonly int _month;
		private readonly int _day;

		public IntMonthAlgorithm(int month, int day)
		{
			_month = month;
			_day = day;
		}

		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) =>
			new DateTime(year, _month, _day);
	}

	/// <summary>
	/// Algorithm-type fallback double exposing a <c>(string, int)</c> constructor; used to verify pass-through of the
	/// raw month token. Captures the token in <see cref="LastMonthToken" /> so tests can assert no coercion occurred.
	/// </summary>
	public sealed class StringMonthAlgorithm : INotableDateAlgorithm
	{
		private readonly int _day;

		public StringMonthAlgorithm(string month, int day)
		{
			LastMonthToken = month;
			_day = day;
		}

		public static string? LastMonthToken { get; private set; }

		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) =>
			new DateTime(year, 9, _day);
	}

	/// <summary>
	/// Months supplied as the first parameter of <see cref="EnumMonthAlgorithm" />. Defined inside the test so the
	/// production assembly does not pick up an unrelated public enum.
	/// </summary>
	public enum MonthOfYear
	{
		January = 1,
		February = 2,
		March = 3,
		April = 4,
		May = 5,
		June = 6,
		July = 7,
		August = 8,
		September = 9,
		October = 10,
		November = 11,
		December = 12,
	}
}
