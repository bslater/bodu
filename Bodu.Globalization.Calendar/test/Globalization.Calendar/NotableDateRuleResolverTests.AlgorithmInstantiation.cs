// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleResolverTests.AlgorithmInstantiation.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the legacy <see cref="NotableDateRule.AlgorithmType" /> instantiation path of
/// <see cref="NotableDateRuleResolver" />: parameterless constructors, two-parameter <c>(month, day)</c>
/// constructors with enum/int/string month tokens, and failure modes when no compatible constructor exists.
/// </summary>
[TestClass]
public sealed class NotableDateRuleResolverAlgorithmInstantiationTests
{
	/// <summary>
	/// Verifies that an algorithm rule with no registered key and no <see cref="NotableDateRule.AlgorithmType" /> resolves
	/// to <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmRuleHasNeitherKeyNorType_ShouldReturnNull()
	{
		NotableDateRule rule = new()
		{
			Name = "Missing Algorithm",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Religious,
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.IsNull(date);
	}

	/// <summary>
	/// Verifies that an algorithm rule whose <see cref="NotableDateRule.AlgorithmType" /> exposes a public
	/// parameterless constructor instantiates and invokes the algorithm.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmTypeHasParameterlessCtor_ShouldReturnAlgorithmResult()
	{
		NotableDateRule rule = new()
		{
			Name = "Fixed July Fourth",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Holiday,
			AlgorithmType = typeof(JulyFourthAlgorithm),
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.AreEqual(new DateTime(2026, 7, 4), date);
	}

	/// <summary>
	/// Verifies that an algorithm rule whose <see cref="NotableDateRule.AlgorithmType" /> exposes only a
	/// two-parameter constructor falls through to the parameterless attempt and returns <see langword="null" />
	/// when no parameterless overload exists.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmTypeHasNoCompatibleCtor_ShouldReturnNull()
	{
		NotableDateRule rule = new()
		{
			Name = "Bad Algorithm",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Religious,
			AlgorithmType = typeof(RequiresTwoArgsAlgorithm),
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.IsNull(date);
	}

	/// <summary>
	/// Verifies that the resolver invokes the two-parameter <c>(month-enum, day)</c> constructor when both
	/// <see cref="NotableDateRule.AlgorithmMonth" /> and <see cref="NotableDateRule.AlgorithmDay" /> are supplied.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmTypeHasEnumDayConstructorAndArgumentsProvided_ShouldReturnArgumentDrivenDate()
	{
		NotableDateRule rule = new()
		{
			Name = "Configurable Algorithm",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Holiday,
			AlgorithmType = typeof(MonthEnumDayAlgorithm),
			AlgorithmMonth = "March",
			AlgorithmDay = 17,
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.AreEqual(new DateTime(2026, 3, 17), date);
	}

	/// <summary>
	/// Verifies that the resolver invokes the two-parameter <c>(int-month, int-day)</c> constructor when
	/// <see cref="NotableDateRule.AlgorithmMonth" /> is a numeric token.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmTypeHasIntDayConstructorAndArgumentsProvided_ShouldReturnArgumentDrivenDate()
	{
		NotableDateRule rule = new()
		{
			Name = "Numeric Algorithm",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Holiday,
			AlgorithmType = typeof(IntMonthIntDayAlgorithm),
			AlgorithmMonth = "11",
			AlgorithmDay = 5,
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.AreEqual(new DateTime(2026, 11, 5), date);
	}

	/// <summary>
	/// Verifies that an algorithm rule that supplies <see cref="NotableDateRule.AlgorithmMonth" /> but no matching
	/// constructor falls back to the parameterless constructor and produces its default date.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmArgumentsDoNotMatchCtor_ShouldFallbackToParameterless()
	{
		NotableDateRule rule = new()
		{
			Name = "Fallback Algorithm",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Holiday,
			AlgorithmType = typeof(JulyFourthAlgorithm),
			AlgorithmMonth = "Smarchadar",
			AlgorithmDay = 1,
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.AreEqual(new DateTime(2026, 7, 4), date);
	}

	/// <summary>
	/// Verifies that the resolver invokes the two-parameter <c>(string-month, int-day)</c> constructor when the
	/// candidate constructor's first parameter type is <see cref="string" />. The token is forwarded verbatim.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmTypeHasStringDayCtor_ShouldForwardMonthTokenVerbatim()
	{
		NotableDateRule rule = new()
		{
			Name = "String Algorithm",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Religious,
			AlgorithmType = typeof(StringMonthIntDayAlgorithm),
			AlgorithmMonth = "Special",
			AlgorithmDay = 7,
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.AreEqual(new DateTime(2026, 1, 7), date);
		Assert.AreEqual("Special", StringMonthIntDayAlgorithm.LastMonthToken);
	}

	/// <summary>
	/// Verifies that an algorithm constructor throwing during activation surfaces as <see langword="null" /> from
	/// <c>TryCreateAlgorithm</c> — the <see cref="System.Reflection.TargetInvocationException" /> wrapping branch.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAlgorithmConstructorThrows_ShouldReturnNull()
	{
		NotableDateRule rule = new()
		{
			Name = "Throwing Algorithm",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Religious,
			AlgorithmType = typeof(ThrowingTwoArgAlgorithm),
			AlgorithmMonth = "5",
			AlgorithmDay = 1,
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.IsNull(date);
	}

	private sealed class JulyFourthAlgorithm
		: INotableDateAlgorithm
	{
		public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null) =>
			new(year, 7, 4);
	}

	private sealed class StringMonthIntDayAlgorithm
		: INotableDateAlgorithm
	{
		public static string? LastMonthToken { get; private set; }

		private readonly int _day;

		public StringMonthIntDayAlgorithm(string month, int day)
		{
			LastMonthToken = month;
			_day = day;
		}

		public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null) =>
			new(year, 1, _day);
	}

	private sealed class ThrowingTwoArgAlgorithm
		: INotableDateAlgorithm
	{
		public ThrowingTwoArgAlgorithm(int month, int day)
		{
			_ = month;
			_ = day;
			throw new InvalidOperationException("Constructor deliberately throws.");
		}

		public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null) =>
			new(year, 1, 1);
	}

	private sealed class MonthEnumDayAlgorithm
		: INotableDateAlgorithm
	{
		private readonly int _month;
		private readonly int _day;

		public MonthEnumDayAlgorithm(MonthsOfYear month, int day)
		{
			_month = (int)month;
			_day = day;
		}

		public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null) =>
			new(year, _month, _day);
	}

	private sealed class IntMonthIntDayAlgorithm
		: INotableDateAlgorithm
	{
		private readonly int _month;
		private readonly int _day;

		public IntMonthIntDayAlgorithm(int month, int day)
		{
			_month = month;
			_day = day;
		}

		public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null) =>
			new(year, _month, _day);
	}

	private sealed class RequiresTwoArgsAlgorithm
		: INotableDateAlgorithm
	{
		public RequiresTwoArgsAlgorithm(int month, int day)
		{
			_ = month;
			_ = day;
		}

		public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null) =>
			new(year, 1, 1);
	}

	private enum MonthsOfYear
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
