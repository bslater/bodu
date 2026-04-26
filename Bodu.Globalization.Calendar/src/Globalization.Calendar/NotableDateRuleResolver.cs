// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleResolver.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Globalization;
using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves the anchor date of a <see cref="NotableDateRule" /> for a specified year, dispatching to the appropriate strategy and
/// honouring temporal bounds, recurrence cadences, anchor look-ups, and algorithm registry resolution.
/// </summary>
/// <remarks>
/// <para>
/// This class replaces the earlier <c>NotableDateResolver</c>. It preserves the same surface area (<see cref="ResolveAnchorDate" />)
/// while finishing several previously incomplete behaviours: <see cref="NotableDateRule.OccurrenceYears" /> is now honoured;
/// circular <c>OffsetFromAnchor</c> chains are detected at every depth; algorithms are looked up via
/// <see cref="INotableDateAlgorithmRegistry" /> with a CLR <see cref="Type" /> fallback for backward compatibility.
/// </para>
/// </remarks>
internal sealed class NotableDateRuleResolver
{
	private readonly IReadOnlyDictionary<string, NotableDateRule> _rulesByName;
	private readonly INotableDateAlgorithmRegistry? _algorithms;

	/// <summary>
	/// Initializes a new instance of the <see cref="NotableDateRuleResolver" /> class.
	/// </summary>
	/// <param name="rules">The rules available for resolution. Must not be <see langword="null" />.</param>
	/// <param name="algorithms">Optional algorithm registry consulted for <see cref="DateResolutionStrategy.Algorithm" /> rules.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rules" /> is <see langword="null" />.</exception>
	public NotableDateRuleResolver(IReadOnlyList<NotableDateRule> rules, INotableDateAlgorithmRegistry? algorithms = null)
	{
		if (rules is null) throw new ArgumentNullException(nameof(rules));

		// Build a name-keyed lookup once; rule names are unique within a service. Duplicates collapse silently with the last wins.
		var lookup = new Dictionary<string, NotableDateRule>(StringComparer.OrdinalIgnoreCase);
		foreach (var rule in rules)
		{
			if (!string.IsNullOrWhiteSpace(rule.Name))
				lookup[rule.Name] = rule;
		}

		_rulesByName = lookup;
		_algorithms = algorithms;
	}

	/// <summary>
	/// Resolves the anchor date for the supplied rule and year.
	/// </summary>
	/// <param name="rule">The rule to resolve. Must not be <see langword="null" />.</param>
	/// <param name="year">The target year.</param>
	/// <returns>The resolved anchor date, or <see langword="null" /> if the rule does not apply to the supplied year.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rule" /> is <see langword="null" />.</exception>
	/// <exception cref="InvalidOperationException">Thrown when a circular <c>OffsetFromAnchor</c> chain is detected or a referenced anchor cannot be found.</exception>
	public DateTime? ResolveAnchorDate(NotableDateRule rule, int year)
	{
		if (rule is null) throw new ArgumentNullException(nameof(rule));

		return ResolveInternal(rule, year, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
	}

    /// <summary>
    /// Resolves <paramref name="rule" /> for <paramref name="year" />, tracking active names
    /// in <paramref name="resolving" /> to detect cycles among offset-anchored rules.
    /// </summary>
    /// <param name="rule">The rule to resolve.</param>
    /// <param name="year">The civil year.</param>
    /// <param name="resolving">The set of rule names currently being resolved up the call stack.</param>
    /// <returns>The resolved date, or <see langword="null" /> if the rule does not apply.</returns>
    /// <exception cref="InvalidOperationException">A cycle was detected among offset-anchored rules.</exception>
	private DateTime? ResolveInternal(NotableDateRule rule, int year, HashSet<string> resolving)
	{
		if (!IsApplicable(rule, year))
			return null;

		if (!resolving.Add(rule.Name))
		{
			var chain = string.Join(" -> ", resolving.Concat(new[] { rule.Name }));
			throw new InvalidOperationException($"Circular dependency detected while resolving notable date rule '{rule.Name}': {chain}.");
		}

		try
		{
			switch (rule.Strategy)
			{
				case DateResolutionStrategy.Fixed:
					if (rule.Month is { } m1 && rule.Day is { } d1)
					{
						if (rule.CalendarType is { } calType
							&& Activator.CreateInstance(calType) is System.Globalization.Calendar cal)
						{
							try
							{
								DateTime converted = cal.ToDateTime(year, m1, d1, 0, 0, 0, 0);
								return DateTime.SpecifyKind(converted.Date, DateTimeKind.Unspecified);
							}
							catch (ArgumentOutOfRangeException)
							{
								// Year is outside the calendar's supported range; treat as no occurrence.
								return null;
							}
						}

						return new DateTime(year, m1, d1, 0, 0, 0, DateTimeKind.Unspecified);
					}

					return null;

				case DateResolutionStrategy.DayOfWeekInMonth:
					if (rule.Month is { } m2 && rule.WeekOrdinal is { } ord && rule.DayOfWeek is { } dow)
						return DateTimeExtensions.GetNthDayOfWeekInMonth(year, m2, dow, ord);
					return null;

				case DateResolutionStrategy.OffsetFromAnchor:
					return ResolveOffsetAnchor(rule, year, resolving);

				case DateResolutionStrategy.Algorithm:
					return ResolveAlgorithm(rule, year);

				default:
					throw new NotSupportedException($"Unsupported date resolution strategy '{rule.Strategy}' on rule '{rule.Name}'.");
			}
		}
		finally
		{
			resolving.Remove(rule.Name);
		}
	}

    /// <summary>
    /// Resolves the anchor date of a rule whose strategy is offset-based, looking up the
    /// anchor rule by name and applying the configured day offset.
    /// </summary>
    /// <param name="rule">The offset-based rule whose anchor must be resolved.</param>
    /// <param name="year">The civil year.</param>
    /// <param name="resolving">The set of rule names currently being resolved up the call stack.</param>
    /// <returns>The resolved anchor date, or <see langword="null" /> if the anchor rule does not
    /// apply in the given year.</returns>
	private DateTime? ResolveOffsetAnchor(NotableDateRule rule, int year, HashSet<string> resolving)
	{
		if (string.IsNullOrWhiteSpace(rule.AnchorRuleName))
			return null;

		if (!_rulesByName.TryGetValue(rule.AnchorRuleName!, out var anchorRule))
			throw new InvalidOperationException($"Anchor rule '{rule.AnchorRuleName}' referenced by '{rule.Name}' was not found.");

		var anchorDate = ResolveInternal(anchorRule, year, resolving);
		if (anchorDate is null || rule.OffsetDays is not { } offset)
			return null;

		return anchorDate.Value.AddDays(offset);
	}

    /// <summary>
    /// Resolves a rule whose strategy is a registered <see cref="INotableDateAlgorithm" />,
    /// delegating to the configured algorithm registry.
    /// </summary>
    /// <param name="rule">The rule bound to a algorithm strategy.</param>
    /// <param name="year">The civil year.</param>
    /// <returns>The calculated date, or <see langword="null" /> if the configured algorithm
    /// returns no date for the year.</returns>
	private DateTime? ResolveAlgorithm(NotableDateRule rule, int year)
	{
		// Prefer registry lookup (DI-friendly, decoupled from CLR type names).
		if (!string.IsNullOrWhiteSpace(rule.AlgorithmKey)
			&& _algorithms is not null
			&& _algorithms.TryGet(rule.AlgorithmKey!, out var algorithm))
		{
			return algorithm.GetDate(year);
		}

		// Fallback: legacy CLR type instantiation, for compatibility with rules authored before the registry existed.
		if (rule.AlgorithmType is not null)
		{
			INotableDateAlgorithm? legacyAlgorithm = TryCreateAlgorithm(rule);
			if (legacyAlgorithm is not null)
				return legacyAlgorithm.GetDate(year);
		}

		return null;
	}

    /// <summary>
    /// Instantiates the rule's <see cref="NotableDateRule.AlgorithmType" /> by selecting the constructor that matches the
    /// authored arguments: a two-parameter <c>(month, day)</c> constructor when <see cref="NotableDateRule.AlgorithmMonth" />
    /// and <see cref="NotableDateRule.AlgorithmDay" /> are both supplied, otherwise the public parameterless constructor.
    /// </summary>
    /// <param name="rule">The rule whose algorithm is being constructed.</param>
    /// <returns>The constructed algorithm, or <see langword="null" /> when no compatible constructor exists or activation fails.</returns>
    private static INotableDateAlgorithm? TryCreateAlgorithm(NotableDateRule rule)
	{
		Type type = rule.AlgorithmType!;

		if (rule.AlgorithmMonth is { } monthToken && rule.AlgorithmDay is { } day)
		{
			foreach (var ctor in type.GetConstructors())
			{
				var parameters = ctor.GetParameters();
				if (parameters.Length != 2) continue;
				if (parameters[1].ParameterType != typeof(int)) continue;

				if (!TryCoerceMonthArgument(monthToken, parameters[0].ParameterType, out object? monthValue))
					continue;

				try
				{
					return ctor.Invoke(new[] { monthValue, (object)day }) as INotableDateAlgorithm;
				}
				catch (TargetInvocationException)
				{
					return null;
				}
				catch (ArgumentException)
				{
					return null;
				}
			}

			// No (month, int) constructor matched; fall through to the parameterless attempt below so a misauthored
			// rule still has a chance to surface via the legacy path rather than silently producing nothing.
		}

		try
		{
			return Activator.CreateInstance(type) as INotableDateAlgorithm;
		}
		catch (MissingMethodException)
		{
			return null;
		}
		catch (TargetInvocationException)
		{
			return null;
		}
	}

    /// <summary>
    /// Parses <paramref name="token" /> into a value compatible with <paramref name="targetType" /> for use as the first
    /// argument of an algorithm constructor. Supports enum, <see cref="int" />, and <see cref="string" /> parameter types.
    /// </summary>
    /// <param name="token">The authored month token (typically the calendar's month name).</param>
    /// <param name="targetType">The target parameter type declared by the candidate constructor.</param>
    /// <param name="value">The coerced value when the method returns <see langword="true" />; otherwise <see langword="null" />.</param>
    /// <returns><see langword="true" /> when <paramref name="token" /> was successfully coerced; otherwise <see langword="false" />.</returns>
    private static bool TryCoerceMonthArgument(string token, Type targetType, out object? value)
	{
		if (targetType.IsEnum)
		{
			if (Enum.TryParse(targetType, token, ignoreCase: true, out var parsed) && parsed is not null && Enum.IsDefined(targetType, parsed))
			{
				value = parsed;
				return true;
			}

			value = null;
			return false;
		}

		if (targetType == typeof(int))
		{
			if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
			{
				value = parsed;
				return true;
			}

			value = null;
			return false;
		}

		if (targetType == typeof(string))
		{
			value = token;
			return true;
		}

		value = null;
		return false;
	}

	/// <summary>
	/// Determines whether the supplied rule applies to the supplied year, after consulting <see cref="NotableDateRule.FirstYear" />,
	/// <see cref="NotableDateRule.LastYear" />, and <see cref="NotableDateRule.OccurrenceYears" />.
	/// </summary>
	/// <param name="rule">The rule under test.</param>
	/// <param name="year">The target year.</param>
	/// <returns><see langword="true" /> if the rule applies; otherwise <see langword="false" />.</returns>
	public static bool IsApplicable(NotableDateRule rule, int year)
	{
		if (rule.FirstYear is { } first && year < first) return false;
		if (rule.LastYear is { } last && year > last) return false;

		if (rule.OccurrenceYears is { } interval && interval > 0)
		{
			int origin = rule.FirstYear ?? 0;
			if (((year - origin) % interval) != 0)
				return false;
		}

		return true;
	}
}
