// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParser.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Parses authored JSON payloads into <see cref="NotableDateRule" /> instances. Companion to
/// <see cref="NotableDateRuleParser" /> for authors who prefer a JSON authoring format.
/// </summary>
/// <remarks>
/// <para>
/// The JSON vocabulary mirrors the XML schema (<c>NotableDates.xsd</c>) one-for-one: a top-level object
/// with optional <c>useFrom</c> and <c>notableDates</c> arrays; each rule selects exactly one of
/// <c>fixed</c>, <c>dayOfWeekInMonth</c>, <c>offsetFromAnchor</c>, or <c>algorithm</c> as its strategy.
/// </para>
/// <para>
/// Validation is performed in two phases: <see cref="JsonSerializer" /> handles structural validation
/// against the DTO shape, then a semantic pass enforces the same invariants the XML parser enforces —
/// "exactly one strategy" on a rule, unique adjustment keys within a rule, and recognised enum values.
/// </para>
/// </remarks>
public static class NotableDateRuleJsonParser
{
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = false,
	};

	/// <summary>
	/// Parses the supplied JSON string into rules.
	/// </summary>
	/// <param name="json">The JSON payload. Must not be <see langword="null" />, empty, or whitespace.</param>
	/// <returns>The parsed rules.</returns>
	/// <remarks>
	/// This convenience overload returns only the rules and discards any <c>useFrom</c> directives. To
	/// resolve a document graph including imports, call <see cref="ParseDocument(string)" /> instead and
	/// feed the result to a loader such as <see cref="JsonResourceNotableDateRuleProvider" />.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json" /> is <see langword="null" />, empty, or whitespace.</exception>
	/// <exception cref="JsonException">Thrown when <paramref name="json" /> is not well-formed JSON.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the document fails semantic validation.</exception>
	public static List<NotableDateRule> ParseJson(string json) =>
		ParseDocument(json).LocalRules.ToList();

	/// <summary>
	/// Parses the supplied JSON string into a <see cref="ParsedNotableDateDocument" />, exposing local rules
	/// together with any <c>useFrom</c> directives.
	/// </summary>
	/// <param name="json">The JSON payload. Must not be <see langword="null" />, empty, or whitespace.</param>
	/// <returns>The parsed document, including imports and rules.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json" /> is <see langword="null" />, empty, or whitespace.</exception>
	/// <exception cref="JsonException">Thrown when <paramref name="json" /> is not well-formed JSON.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the document fails semantic validation.</exception>
	public static ParsedNotableDateDocument ParseDocument(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
			throw new ArgumentNullException(nameof(json));

		var dto = JsonSerializer.Deserialize<NotableDatesDocumentDto>(json, SerializerOptions)
			?? throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_MissingRequiredAttribute, "root", "NotableDates"));

		var useGroups = (dto.UseFrom ?? Array.Empty<UseFromDto>())
			.Select(MapUseGroup)
			.ToImmutableArray();

		var localRules = (dto.NotableDates ?? Array.Empty<NotableDateDto>())
			.SelectMany(MapNotableDate)
			.ToImmutableArray();

		return new ParsedNotableDateDocument(useGroups, localRules);
	}

	// ----------------------------------------------------------------------------
	// DTO → record mapping
	// ----------------------------------------------------------------------------

	/// <summary>
	/// Maps a <see cref="UseFromDto" /> onto a <see cref="NotableDateRuleUseGroup" />.
	/// </summary>
	private static NotableDateRuleUseGroup MapUseGroup(UseFromDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Resource))
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_MissingRequiredAttribute, "resource", "useFrom"));

		var uses = (dto.Uses ?? Array.Empty<UseDto>())
			.Select(MapUseDirective)
			.ToImmutableArray();

		return new NotableDateRuleUseGroup(dto.Resource, dto.UseAll ?? false, uses);
	}

	/// <summary>
	/// Maps a <see cref="UseDto" /> onto a <see cref="NotableDateRuleUseDirective" />.
	/// </summary>
	private static NotableDateRuleUseDirective MapUseDirective(UseDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Name))
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_MissingRequiredAttribute, "name", "use"));

		var overrideBody = dto.Rule is null ? null : MapOverrideBody(dto.Rule);

		return new NotableDateRuleUseDirective(
			SourceRuleName: dto.Name,
			LocalName: dto.As,
			Category: ParseOptionalEnum<NotableDateCategory>(dto.Category, "category", "use"),
			TerritoryCode: dto.Territory,
			IsNonWorkingDay: dto.NonWorking,
			FirstYear: dto.FirstYear,
			LastYear: dto.LastYear,
			OccurrenceYears: dto.OccurrenceYears,
			DurationDays: dto.DurationDays,
			Priority: dto.Priority,
			Comment: dto.Comment,
			ClearTags: dto.ClearTags ?? false,
			ClearAdjustments: dto.ClearAdjustments ?? false,
			ClearInherited: dto.ClearInherited ?? false,
			OverrideBody: overrideBody);
	}

	/// <summary>
	/// Maps an <see cref="OverrideRuleDto" /> onto a <see cref="NotableDateRuleOverrideBody" />.
	/// </summary>
	private static NotableDateRuleOverrideBody MapOverrideBody(OverrideRuleDto dto)
	{
		var strategy = DetectOverrideStrategy(dto);

		var body = new NotableDateRuleOverrideBody
		{
			RuleName = dto.Name,
			Category = ParseOptionalEnum<NotableDateCategory>(dto.Category, "category", "rule"),
			TerritoryCode = dto.Territory,
			IsNonWorkingDay = dto.NonWorking,
			FirstYear = dto.FirstYear,
			LastYear = dto.LastYear,
			OccurrenceYears = dto.OccurrenceYears,
			DurationDays = dto.DurationDays,
			Priority = dto.Priority,
			Comment = dto.Comment,
			CalendarType = ParseOptionalType<SysGlobal.Calendar>(dto.CalendarType),
			Strategy = strategy,
			Tags = (dto.Tags ?? Array.Empty<string>())
				.Where(t => !string.IsNullOrWhiteSpace(t))
				.ToImmutableArray(),
			Adjustments = (dto.Adjustments ?? Array.Empty<AdjustmentDto>())
				.Select(MapAdjustment)
				.ToImmutableArray(),
		};

		if (strategy is not null)
			body = ApplyStrategySpecificsToBody(body, dto);

		EnsureUniqueAdjustmentKeys(body.Adjustments, "rule");
		return body;
	}

	/// <summary>
	/// Expands a <see cref="NotableDateDto" /> into one rule per <c>rules[]</c> entry.
	/// </summary>
	private static IEnumerable<NotableDateRule> MapNotableDate(NotableDateDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Name))
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_MissingRequiredAttribute, "name", "notableDate"));

		foreach (var ruleDto in dto.Rules ?? Array.Empty<RuleDto>())
		{
			yield return MapRule(dto.Name, ruleDto);
		}
	}

	/// <summary>
	/// Maps a single <see cref="RuleDto" /> onto a fully populated <see cref="NotableDateRule" />.
	/// </summary>
	private static NotableDateRule MapRule(string notableDateName, RuleDto dto)
	{
		var strategy = DetectRuleStrategy(dto, notableDateName);

		var adjustments = (dto.Adjustments ?? Array.Empty<AdjustmentDto>())
			.Select(MapAdjustment)
			.ToImmutableArray();

		EnsureUniqueAdjustmentKeys(adjustments, "rule");

		var rule = new NotableDateRule
		{
			Name = notableDateName,
			RuleName = dto.Name,
			Strategy = strategy,
			Category = ParseOptionalEnum<NotableDateCategory>(dto.Category, "category", "rule") ?? NotableDateCategory.None,
			FirstYear = dto.FirstYear,
			LastYear = dto.LastYear,
			OccurrenceYears = dto.OccurrenceYears,
			CalendarType = ParseOptionalType<SysGlobal.Calendar>(dto.CalendarType),
			TerritoryCode = dto.Territory,
			IsNonWorkingDay = dto.NonWorking,
			DurationDays = dto.DurationDays ?? 1,
			Priority = dto.Priority ?? 100,
			Tags = (dto.Tags ?? Array.Empty<string>())
				.Where(t => !string.IsNullOrWhiteSpace(t))
				.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase),
			Comment = dto.Comment,
			Adjustments = adjustments,
		};

		return ApplyStrategySpecifics(rule, dto);
	}

	/// <summary>
	/// Maps an <see cref="AdjustmentDto" /> onto an <see cref="ObservanceAdjustment" />.
	/// </summary>
	private static ObservanceAdjustment MapAdjustment(AdjustmentDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Key))
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_MissingRequiredAttribute, "key", "adjustment"));

		return new ObservanceAdjustment
		{
			Key = dto.Key,
			Trigger = ParseRequiredEnum<AdjustmentTrigger>(dto.When, "when", "adjustment"),
			Action = ParseRequiredEnum<AdjustmentAction>(dto.Action, "action", "adjustment"),
			DayOfWeek = ParseOptionalEnum<DayOfWeek>(dto.DayOfWeek, "dayOfWeek", "adjustment"),
			WeekOrdinal = ParseOptionalEnum<WeekOfMonthOrdinal>(dto.WeekOrdinal, "weekOrdinal", "adjustment"),
			IsNonWorkingDay = dto.NonWorking,
			OffsetDays = dto.Days ?? 0,
			TerritoryCode = dto.Territory,
			CalendarType = ParseOptionalType<SysGlobal.Calendar>(dto.CalendarType),
			EffectiveFromYear = dto.FromYear,
			EffectiveToYear = dto.ToYear,
			ComparisonDate = ParseOptionalMonthDay(dto.ComparisonMonth, dto.ComparisonDay),
			TargetRuleName = dto.Target,
			Priority = dto.Priority ?? 100,
			HandlerKey = dto.HandlerKey,
		};
	}

	// ----------------------------------------------------------------------------
	// Strategy detection and projection
	// ----------------------------------------------------------------------------

	/// <summary>
	/// Determines which strategy a <see cref="RuleDto" /> declared, enforcing exactly-one-of-N selection.
	/// </summary>
	private static DateResolutionStrategy DetectRuleStrategy(RuleDto dto, string notableDateName)
	{
		int count = 0;
		DateResolutionStrategy result = default;

		if (dto.Fixed is not null) { result = DateResolutionStrategy.Fixed; count++; }
		if (dto.DayOfWeekInMonth is not null) { result = DateResolutionStrategy.DayOfWeekInMonth; count++; }
		if (dto.OffsetFromAnchor is not null) { result = DateResolutionStrategy.OffsetFromAnchor; count++; }
		if (dto.Algorithm is not null) { result = DateResolutionStrategy.Algorithm; count++; }

		if (count == 0)
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_RuleMissingStrategy, notableDateName));

		if (count > 1)
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_UnknownStrategyElementOnRule, "<multiple>", notableDateName));

		return result;
	}

	/// <summary>
	/// Determines which strategy an <see cref="OverrideRuleDto" /> declared, allowing zero or one. Returns
	/// <see langword="null" /> when no strategy is specified (the override inherits the source's strategy).
	/// </summary>
	private static DateResolutionStrategy? DetectOverrideStrategy(OverrideRuleDto dto)
	{
		int count = 0;
		DateResolutionStrategy result = default;

		if (dto.Fixed is not null) { result = DateResolutionStrategy.Fixed; count++; }
		if (dto.DayOfWeekInMonth is not null) { result = DateResolutionStrategy.DayOfWeekInMonth; count++; }
		if (dto.OffsetFromAnchor is not null) { result = DateResolutionStrategy.OffsetFromAnchor; count++; }
		if (dto.Algorithm is not null) { result = DateResolutionStrategy.Algorithm; count++; }

		if (count == 0)
			return null;

		if (count > 1)
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_UnknownStrategyElementOnOverrideRule, "<multiple>"));

		return result;
	}

	/// <summary>
	/// Applies strategy-specific fields from <paramref name="dto" /> onto <paramref name="rule" />.
	/// </summary>
	private static NotableDateRule ApplyStrategySpecifics(NotableDateRule rule, RuleDto dto)
	{
		switch (rule.Strategy)
		{
			case DateResolutionStrategy.Fixed:
				{
					var f = dto.Fixed!;
					var (monthNum, monthAlias) = ParseMonthToken(RequireString(f.Month, "month", "fixed"));
					return rule with
					{
						Month = monthNum,
						CalendarMonthAlias = monthAlias,
						Day = RequireInt(f.Day, "day", "fixed"),
						SkipLeapMonth = f.SkipLeapMonth ?? false,
						SweepCalendarYears = f.SweepCalendarYears ?? false,
					};
				}

			case DateResolutionStrategy.DayOfWeekInMonth:
				{
					var d = dto.DayOfWeekInMonth!;
					return rule with
					{
						Month = ParseMonth(RequireString(d.Month, "month", "dayOfWeekInMonth")),
						DayOfWeek = ParseRequiredEnum<DayOfWeek>(d.DayOfWeek, "dayOfWeek", "dayOfWeekInMonth"),
						WeekOrdinal = ParseRequiredEnum<WeekOfMonthOrdinal>(d.WeekOrdinal, "weekOrdinal", "dayOfWeekInMonth"),
					};
				}

			case DateResolutionStrategy.OffsetFromAnchor:
				{
					var o = dto.OffsetFromAnchor!;
					return rule with
					{
						AnchorRuleName = RequireString(o.Name, "name", "offsetFromAnchor"),
						OffsetDays = RequireInt(o.Offset, "offset", "offsetFromAnchor"),
					};
				}

			case DateResolutionStrategy.Algorithm:
				{
					var a = dto.Algorithm!;
					return rule with
					{
						AlgorithmKey = a.Key,
						AlgorithmType = ParseOptionalType<INotableDateAlgorithm>(a.Type),
						AlgorithmMonth = a.Month,
						AlgorithmDay = a.Day,
					};
				}

			default:
				throw new NotSupportedException(
					string.Format(CultureInfo.InvariantCulture, CalendarStrings.NotSupportedException_UnsupportedStrategy, rule.Strategy));
		}
	}

	/// <summary>
	/// Applies strategy-specific fields from an override DTO onto <paramref name="body" />.
	/// </summary>
	private static NotableDateRuleOverrideBody ApplyStrategySpecificsToBody(NotableDateRuleOverrideBody body, OverrideRuleDto dto)
	{
		switch (body.Strategy)
		{
			case DateResolutionStrategy.Fixed:
				{
					var f = dto.Fixed!;
					var (monthNum, monthAlias) = ParseMonthToken(RequireString(f.Month, "month", "fixed"));
					return body with
					{
						Month = monthNum,
						CalendarMonthAlias = monthAlias,
						Day = RequireInt(f.Day, "day", "fixed"),
						SkipLeapMonth = f.SkipLeapMonth ?? false,
						SweepCalendarYears = f.SweepCalendarYears ?? false,
					};
				}

			case DateResolutionStrategy.DayOfWeekInMonth:
				{
					var d = dto.DayOfWeekInMonth!;
					return body with
					{
						Month = ParseMonth(RequireString(d.Month, "month", "dayOfWeekInMonth")),
						DayOfWeek = ParseRequiredEnum<DayOfWeek>(d.DayOfWeek, "dayOfWeek", "dayOfWeekInMonth"),
						WeekOrdinal = ParseRequiredEnum<WeekOfMonthOrdinal>(d.WeekOrdinal, "weekOrdinal", "dayOfWeekInMonth"),
					};
				}

			case DateResolutionStrategy.OffsetFromAnchor:
				{
					var o = dto.OffsetFromAnchor!;
					return body with
					{
						AnchorRuleName = RequireString(o.Name, "name", "offsetFromAnchor"),
						OffsetDays = RequireInt(o.Offset, "offset", "offsetFromAnchor"),
					};
				}

			case DateResolutionStrategy.Algorithm:
				{
					var a = dto.Algorithm!;
					return body with
					{
						AlgorithmKey = a.Key,
						AlgorithmType = ParseOptionalType<INotableDateAlgorithm>(a.Type),
						AlgorithmMonth = a.Month,
						AlgorithmDay = a.Day,
					};
				}

			default:
				return body;
		}
	}

	// ----------------------------------------------------------------------------
	// Helpers
	// ----------------------------------------------------------------------------

	/// <summary>
	/// Returns <paramref name="value" /> if non-empty, otherwise throws referring to the field by name.
	/// </summary>
	private static string RequireString(string? value, string fieldName, string contextName)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_MissingRequiredAttribute, fieldName, contextName));

		return value;
	}

	/// <summary>
	/// Returns <paramref name="value" /> if it has a value, otherwise throws referring to the field by name.
	/// </summary>
	private static int RequireInt(int? value, string fieldName, string contextName)
	{
		if (value is null)
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_MissingRequiredAttribute, fieldName, contextName));

		return value.Value;
	}

	/// <summary>
	/// Parses a required enum-valued field, throwing when missing or unrecognised.
	/// </summary>
	private static TEnum ParseRequiredEnum<TEnum>(string? raw, string fieldName, string contextName)
		where TEnum : struct, Enum
	{
		if (string.IsNullOrWhiteSpace(raw))
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_MissingRequiredAttribute, fieldName, contextName));

		if (!Enum.TryParse<TEnum>(raw, ignoreCase: true, out var result))
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_InvalidAttributeValue, fieldName, contextName));

		return result;
	}

	/// <summary>
	/// Parses an optional enum-valued field, returning <see langword="null" /> when absent.
	/// </summary>
	private static TEnum? ParseOptionalEnum<TEnum>(string? raw, string fieldName, string contextName)
		where TEnum : struct, Enum
	{
		if (string.IsNullOrWhiteSpace(raw))
			return null;

		if (!Enum.TryParse<TEnum>(raw, ignoreCase: true, out var result))
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_InvalidAttributeValue, fieldName, contextName));

		return result;
	}

	/// <summary>
	/// Resolves <paramref name="typeName" /> to a CLR type assignable to <typeparamref name="TBase" />.
	/// </summary>
	private static Type? ParseOptionalType<TBase>(string? typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName))
			return null;

		var type = Type.GetType(typeName, throwOnError: false);
		return type is not null && typeof(TBase).IsAssignableFrom(type) ? type : null;
	}

	/// <summary>
	/// Builds a synthetic comparison <see cref="DateTime" /> from a (month, day) pair, returning
	/// <see langword="null" /> when either is absent.
	/// </summary>
	private static DateTime? ParseOptionalMonthDay(string? month, int? day)
	{
		if (string.IsNullOrWhiteSpace(month) || day is null)
			return null;

		int monthValue = ParseMonth(month);
		return new DateTime(2000, monthValue, day.Value, 0, 0, 0, DateTimeKind.Unspecified);
	}

	/// <summary>
	/// Parses an English Gregorian month name or numeric month token (1–13).
	/// </summary>
	private static int ParseMonth(string monthName)
	{
		if (string.IsNullOrWhiteSpace(monthName))
			throw new ArgumentNullException(nameof(monthName));

		if (DateTime.TryParseExact(monthName, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
			return result.Month;

		if (int.TryParse(monthName, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric)
			&& numeric is >= 1 and <= 13)
			return numeric;

		throw new FormatException(
			string.Format(CultureInfo.InvariantCulture, CalendarStrings.FormatException_InvalidMonthValueGregorian, monthName));
	}

	/// <summary>
	/// Parses a Fixed-strategy month token, returning either a numeric month or a Hebrew calendar alias.
	/// </summary>
	private static (int? numericMonth, string? alias) ParseMonthToken(string token)
	{
		if (string.IsNullOrWhiteSpace(token))
			throw new ArgumentNullException(nameof(token));

		if (DateTime.TryParseExact(token, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
			return (result.Month, null);

		if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric)
			&& numeric is >= 1 and <= 13)
			return (numeric, null);

		int? simpleHebrew = token switch
		{
			"Tishri" => 1,
			"Heshvan" => 2,
			"Kislev" => 3,
			"Tevet" => 4,
			"Shevat" => 5,
			"AdarI" => 6,
			"AdarII" => 7,
			_ => (int?)null,
		};

		if (simpleHebrew is not null)
			return (simpleHebrew, null);

		if (token is "LastAdar" or "Nisan" or "Iyar" or "Sivan" or "Tammuz" or "Av" or "Elul")
			return (null, token);

		throw new FormatException(
			string.Format(CultureInfo.InvariantCulture, CalendarStrings.FormatException_InvalidMonthValueHebrew, token));
	}

	/// <summary>
	/// Enforces the per-rule uniqueness invariant on adjustment keys.
	/// </summary>
	private static void EnsureUniqueAdjustmentKeys(ImmutableArray<ObservanceAdjustment> adjustments, string contextName)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var adjustment in adjustments)
		{
			if (!seen.Add(adjustment.Key))
				throw new InvalidOperationException(
					string.Format(CultureInfo.InvariantCulture, CalendarStrings.InvalidOperationException_DuplicateAdjustmentKey, adjustment.Key, contextName));
		}
	}

	// ----------------------------------------------------------------------------
	// DTOs (private — bound to the JSON wire format only)
	// ----------------------------------------------------------------------------

	private sealed class NotableDatesDocumentDto
	{
		[JsonPropertyName("useFrom")] public UseFromDto[]? UseFrom { get; init; }
		[JsonPropertyName("notableDates")] public NotableDateDto[]? NotableDates { get; init; }
	}

	private sealed class UseFromDto
	{
		[JsonPropertyName("resource")] public string? Resource { get; init; }
		[JsonPropertyName("useAll")] public bool? UseAll { get; init; }
		[JsonPropertyName("uses")] public UseDto[]? Uses { get; init; }
	}

	private sealed class UseDto
	{
		[JsonPropertyName("name")] public string? Name { get; init; }
		[JsonPropertyName("as")] public string? As { get; init; }
		[JsonPropertyName("category")] public string? Category { get; init; }
		[JsonPropertyName("territory")] public string? Territory { get; init; }
		[JsonPropertyName("nonWorking")] public bool? NonWorking { get; init; }
		[JsonPropertyName("firstYear")] public int? FirstYear { get; init; }
		[JsonPropertyName("lastYear")] public int? LastYear { get; init; }
		[JsonPropertyName("occurrenceYears")] public int? OccurrenceYears { get; init; }
		[JsonPropertyName("durationDays")] public int? DurationDays { get; init; }
		[JsonPropertyName("priority")] public int? Priority { get; init; }
		[JsonPropertyName("comment")] public string? Comment { get; init; }
		[JsonPropertyName("clearTags")] public bool? ClearTags { get; init; }
		[JsonPropertyName("clearAdjustments")] public bool? ClearAdjustments { get; init; }
		[JsonPropertyName("clearInherited")] public bool? ClearInherited { get; init; }
		[JsonPropertyName("rule")] public OverrideRuleDto? Rule { get; init; }
	}

	private sealed class NotableDateDto
	{
		[JsonPropertyName("name")] public string? Name { get; init; }
		[JsonPropertyName("rules")] public RuleDto[]? Rules { get; init; }
	}

	private class RuleDto
	{
		[JsonPropertyName("name")] public string? Name { get; init; }
		[JsonPropertyName("category")] public string? Category { get; init; }
		[JsonPropertyName("firstYear")] public int? FirstYear { get; init; }
		[JsonPropertyName("lastYear")] public int? LastYear { get; init; }
		[JsonPropertyName("occurrenceYears")] public int? OccurrenceYears { get; init; }
		[JsonPropertyName("durationDays")] public int? DurationDays { get; init; }
		[JsonPropertyName("priority")] public int? Priority { get; init; }
		[JsonPropertyName("nonWorking")] public bool? NonWorking { get; init; }
		[JsonPropertyName("calendarType")] public string? CalendarType { get; init; }
		[JsonPropertyName("territory")] public string? Territory { get; init; }
		[JsonPropertyName("comment")] public string? Comment { get; init; }
		[JsonPropertyName("fixed")] public FixedDto? Fixed { get; init; }
		[JsonPropertyName("dayOfWeekInMonth")] public DayOfWeekInMonthDto? DayOfWeekInMonth { get; init; }
		[JsonPropertyName("offsetFromAnchor")] public OffsetFromAnchorDto? OffsetFromAnchor { get; init; }
		[JsonPropertyName("algorithm")] public AlgorithmDto? Algorithm { get; init; }
		[JsonPropertyName("tags")] public string[]? Tags { get; init; }
		[JsonPropertyName("adjustments")] public AdjustmentDto[]? Adjustments { get; init; }
	}

	private sealed class OverrideRuleDto : RuleDto
	{
	}

	private sealed class FixedDto
	{
		[JsonPropertyName("month")] public string? Month { get; init; }
		[JsonPropertyName("day")] public int? Day { get; init; }
		[JsonPropertyName("skipLeapMonth")] public bool? SkipLeapMonth { get; init; }
		[JsonPropertyName("sweepCalendarYears")] public bool? SweepCalendarYears { get; init; }
	}

	private sealed class DayOfWeekInMonthDto
	{
		[JsonPropertyName("month")] public string? Month { get; init; }
		[JsonPropertyName("dayOfWeek")] public string? DayOfWeek { get; init; }
		[JsonPropertyName("weekOrdinal")] public string? WeekOrdinal { get; init; }
	}

	private sealed class OffsetFromAnchorDto
	{
		[JsonPropertyName("name")] public string? Name { get; init; }
		[JsonPropertyName("offset")] public int? Offset { get; init; }
	}

	private sealed class AlgorithmDto
	{
		[JsonPropertyName("key")] public string? Key { get; init; }
		[JsonPropertyName("type")] public string? Type { get; init; }
		[JsonPropertyName("month")] public string? Month { get; init; }
		[JsonPropertyName("day")] public int? Day { get; init; }
	}

	private sealed class AdjustmentDto
	{
		[JsonPropertyName("key")] public string? Key { get; init; }
		[JsonPropertyName("when")] public string? When { get; init; }
		[JsonPropertyName("action")] public string? Action { get; init; }
		[JsonPropertyName("dayOfWeek")] public string? DayOfWeek { get; init; }
		[JsonPropertyName("weekOrdinal")] public string? WeekOrdinal { get; init; }
		[JsonPropertyName("days")] public int? Days { get; init; }
		[JsonPropertyName("priority")] public int? Priority { get; init; }
		[JsonPropertyName("nonWorking")] public bool? NonWorking { get; init; }
		[JsonPropertyName("territory")] public string? Territory { get; init; }
		[JsonPropertyName("calendarType")] public string? CalendarType { get; init; }
		[JsonPropertyName("fromYear")] public int? FromYear { get; init; }
		[JsonPropertyName("toYear")] public int? ToYear { get; init; }
		[JsonPropertyName("comparisonMonth")] public string? ComparisonMonth { get; init; }
		[JsonPropertyName("comparisonDay")] public int? ComparisonDay { get; init; }
		[JsonPropertyName("target")] public string? Target { get; init; }
		[JsonPropertyName("handlerKey")] public string? HandlerKey { get; init; }
	}
}
