using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves the anchor date of a <see cref="NotableDateRule" /> for a specified year, dispatching to the appropriate strategy and
/// honouring temporal bounds, recurrence cadences, anchor look-ups, and calculator registry resolution.
/// </summary>
/// <remarks>
/// <para>
/// This class replaces the earlier <c>NotableDateResolver</c>. It preserves the same surface area (<see cref="ResolveAnchorDate" />)
/// while finishing several previously incomplete behaviours: <see cref="NotableDateRule.OccurrenceYears" /> is now honoured;
/// circular <c>OffsetFromAnchor</c> chains are detected at every depth; calculators are looked up via
/// <see cref="INotableDateCalculatorRegistry" /> with a CLR <see cref="Type" /> fallback for backward compatibility.
/// </para>
/// </remarks>
internal sealed class NotableDateRuleResolver
{
	private readonly IReadOnlyDictionary<string, NotableDateRule> _rulesByName;
	private readonly INotableDateCalculatorRegistry? _calculators;

	/// <summary>
	/// Initializes a new instance of the <see cref="NotableDateRuleResolver" /> class.
	/// </summary>
	/// <param name="rules">The rules available for resolution. Must not be <see langword="null" />.</param>
	/// <param name="calculators">Optional calculator registry consulted for <see cref="DateResolutionStrategy.Calculator" /> rules.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rules" /> is <see langword="null" />.</exception>
	public NotableDateRuleResolver(IReadOnlyList<NotableDateRule> rules, INotableDateCalculatorRegistry? calculators = null)
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
		_calculators = calculators;
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
						return new DateTime(year, m1, d1, 0, 0, 0, DateTimeKind.Unspecified);
					return null;

				case DateResolutionStrategy.DayOfWeekInMonth:
					if (rule.Month is { } m2 && rule.WeekOrdinal is { } ord && rule.DayOfWeek is { } dow)
						return DateTimeExtensions.GetNthDayOfWeekInMonth(year, m2, dow, ord);
					return null;

				case DateResolutionStrategy.OffsetFromAnchor:
					return ResolveOffsetAnchor(rule, year, resolving);

				case DateResolutionStrategy.Calculator:
					return ResolveCalculator(rule, year);

				default:
					throw new NotSupportedException($"Unsupported date resolution strategy '{rule.Strategy}' on rule '{rule.Name}'.");
			}
		}
		finally
		{
			resolving.Remove(rule.Name);
		}
	}

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

	private DateTime? ResolveCalculator(NotableDateRule rule, int year)
	{
		// Prefer registry lookup (DI-friendly, decoupled from CLR type names).
		if (!string.IsNullOrWhiteSpace(rule.CalculatorKey)
			&& _calculators is not null
			&& _calculators.TryGet(rule.CalculatorKey!, out var calculator))
		{
			return calculator.GetDate(year);
		}

		// Fallback: legacy CLR type instantiation, for compatibility with rules authored before the registry existed.
		if (rule.CalculatorType is not null)
		{
			if (Activator.CreateInstance(rule.CalculatorType) is INotableDateCalculator legacyCalculator)
				return legacyCalculator.GetDate(year);
		}

		return null;
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
