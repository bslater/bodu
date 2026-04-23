// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParser.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Parses authored XML payloads into <see cref="NotableDateRule" /> instances after schema validation.
/// </summary>
/// <remarks>
/// <para>
/// XML inputs are validated against the embedded <c>NotableDates.xsd</c> schema before parsing. JSON parsing is intentionally not yet
/// reinstated; the previous incomplete implementation has been preserved as a documented stub at the bottom of this file for future
/// completion.
/// </para>
/// <para>
/// This class replaces <c>NotableDateDefinitionParser</c>. The new schema vocabulary uses <c>Rule</c> as the per-definition element and
/// names the strategy child elements <c>Fixed</c>, <c>DayOfWeekInMonth</c>, <c>OffsetFromAnchor</c>, and <c>Calculator</c>.
/// </para>
/// </remarks>
public static class NotableDateRuleParser
{
	private static readonly XNamespace Namespace = "urn:bodu:globalization:calendar";
	private static readonly XmlSchemaSet SchemaSet = LoadSchema();

	/// <summary>
	/// Parses the supplied XML string into rules after validating against the embedded schema.
	/// </summary>
	/// <param name="xml">The XML payload. Must not be <see langword="null" /> or whitespace.</param>
	/// <returns>The parsed rules.</returns>
	/// <remarks>
	/// This convenience overload returns only the rules and discards any <c>Import</c> or <c>Suppress</c> directives. To resolve a
	/// document graph including imports, call <see cref="ParseDocument(string)" /> instead and feed the result to a loader such as
	/// <see cref="XmlResourceNotableDateRuleProvider" />.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="xml" /> is <see langword="null" />, empty, or whitespace.</exception>
	/// <exception cref="XmlSchemaValidationException">Thrown when the XML does not conform to the embedded schema.</exception>
	public static List<NotableDateRule> ParseXml(string xml)
	{
		return ParseDocument(xml).LocalRules.ToList();
	}

	/// <summary>
	/// Parses the supplied <see cref="XDocument" /> into rules after validating against the embedded schema.
	/// </summary>
	/// <param name="document">The XML document. Must not be <see langword="null" />.</param>
	/// <returns>The parsed rules.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="document" /> is <see langword="null" />.</exception>
	public static List<NotableDateRule> ParseXml(XDocument document)
	{
		return ParseDocument(document).LocalRules.ToList();
	}

	/// <summary>
	/// Parses the supplied XML string into a <see cref="ParsedNotableDateDocument" />, exposing local rules together with any
	/// <c>Import</c> and <c>Suppress</c> directives.
	/// </summary>
	/// <param name="xml">The XML payload. Must not be <see langword="null" /> or whitespace.</param>
	/// <returns>The parsed document, including imports, suppressions, and rules.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="xml" /> is <see langword="null" />, empty, or whitespace.</exception>
	/// <exception cref="XmlSchemaValidationException">Thrown when the XML does not conform to the embedded schema.</exception>
	public static ParsedNotableDateDocument ParseDocument(string xml)
	{
		if (string.IsNullOrWhiteSpace(xml))
			throw new ArgumentNullException(nameof(xml));

		using var stringReader = new StringReader(xml);
		using var xmlReader = XmlReader.Create(stringReader, CreateValidationSettings());
		var document = XDocument.Load(xmlReader);
		return ParseDocumentInternal(document);
	}

	/// <summary>
	/// Parses the supplied <see cref="XDocument" /> into a <see cref="ParsedNotableDateDocument" />.
	/// </summary>
	/// <param name="document">The XML document. Must not be <see langword="null" />.</param>
	/// <returns>The parsed document.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="document" /> is <see langword="null" />.</exception>
	public static ParsedNotableDateDocument ParseDocument(XDocument document)
	{
		if (document is null)
			throw new ArgumentNullException(nameof(document));

		ValidateDocument(document);
		return ParseDocumentInternal(document);
	}

	// ----------------------------------------------------------------------------
	// Per-element parsing
	// ----------------------------------------------------------------------------

	private static ParsedNotableDateDocument ParseDocumentInternal(XDocument document)
	{
		var useGroups = document.Descendants(Namespace + "UseFrom")
			.Select(ParseUseGroup)
			.ToImmutableArray();

		var rules = document.Descendants(Namespace + "NotableDate")
			.SelectMany(ParseNotableDate)
			.ToImmutableArray();

		return new ParsedNotableDateDocument(useGroups, rules);
	}

	private static NotableDateRuleUseGroup ParseUseGroup(XElement useFromElement)
	{
		var resource = GetRequiredAttribute(useFromElement, "resource");
		bool useAll = useFromElement.Element(Namespace + "UseAll") is not null;

		var uses = useFromElement.Elements(Namespace + "Use")
			.Select(ParseUseDirective)
			.ToImmutableArray();

		return new NotableDateRuleUseGroup(resource, useAll, uses);
	}

	private static NotableDateRuleUseDirective ParseUseDirective(XElement useElement) =>
		new(
			SourceRuleName: GetRequiredAttribute(useElement, "name"),
			LocalName: GetOptionalAttribute(useElement, "as"),
			Category: ParseOptionalEnum<NotableDateCategory>(useElement, "category"),
			TerritoryCode: GetOptionalAttribute(useElement, "territory"),
			IsNonWorkingDay: ParseOptionalBool(useElement, "nonWorking"),
			FirstYear: ParseOptionalInt(useElement, "firstYear"),
			LastYear: ParseOptionalInt(useElement, "lastYear"),
			OccurrenceYears: ParseOptionalInt(useElement, "occurrenceYears"),
			DurationDays: ParseOptionalInt(useElement, "durationDays"),
			Priority: ParseOptionalInt(useElement, "priority"),
			Comment: GetOptionalAttribute(useElement, "comment"));

	private static IEnumerable<NotableDateRule> ParseNotableDate(XElement notableDateElement)
	{
		var name = GetRequiredAttribute(notableDateElement, "name");

		foreach (var ruleElement in notableDateElement.Elements(Namespace + "Rule"))
		{
			var strategyElement = ruleElement.Elements()
				.FirstOrDefault(e => IsStrategyElement(e.Name.LocalName))
				?? throw new InvalidOperationException($"Rule '{name}' is missing a strategy child element.");

			var strategy = strategyElement.Name.LocalName switch
			{
				"Fixed" => DateResolutionStrategy.Fixed,
				"DayOfWeekInMonth" => DateResolutionStrategy.DayOfWeekInMonth,
				"Calculator" => DateResolutionStrategy.Calculator,
				"OffsetFromAnchor" => DateResolutionStrategy.OffsetFromAnchor,
				_ => throw new InvalidOperationException($"Unknown strategy element '{strategyElement.Name.LocalName}' on rule '{name}'.")
			};

			var rule = new NotableDateRule
			{
				Name = GetOptionalAttribute(ruleElement, "name") ?? name,
				Strategy = strategy,
				Category = ParseOptionalEnum<NotableDateCategory>(ruleElement, "category") ?? NotableDateCategory.None,
				FirstYear = ParseOptionalInt(ruleElement, "firstYear"),
				LastYear = ParseOptionalInt(ruleElement, "lastYear"),
				OccurrenceYears = ParseOptionalInt(ruleElement, "occurrenceYears"),
				CalendarType = ParseOptionalType<SysGlobal.Calendar>(ruleElement, "calendarType"),
				TerritoryCode = GetOptionalAttribute(ruleElement, "territory"),
				IsNonWorkingDay = ParseOptionalBool(ruleElement, "nonWorking"),
				DurationDays = ParseOptionalInt(ruleElement, "durationDays") ?? 1,
				Priority = ParseOptionalInt(ruleElement, "priority") ?? 100,
				Tags = ruleElement.Elements(Namespace + "Tag")
					.Select(t => t.Value)
					.Where(t => !string.IsNullOrWhiteSpace(t))
					.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase),
				Comment = GetOptionalAttribute(ruleElement, "comment"),
				Adjustments = ruleElement.Elements(Namespace + "Adjustment")
					.Select(ParseAdjustment)
					.ToImmutableArray(),
			};

			yield return ApplyStrategySpecifics(rule, strategyElement);
		}
	}

	private static bool IsStrategyElement(string localName) =>
		localName is "Fixed" or "DayOfWeekInMonth" or "Calculator" or "OffsetFromAnchor";

	private static NotableDateRule ApplyStrategySpecifics(NotableDateRule rule, XElement strategyElement) =>
		rule.Strategy switch
		{
			DateResolutionStrategy.Fixed => rule with
			{
				Month = ParseMonth(GetRequiredAttribute(strategyElement, "month")),
				Day = int.Parse(GetRequiredAttribute(strategyElement, "day"), CultureInfo.InvariantCulture)
			},
			DateResolutionStrategy.DayOfWeekInMonth => rule with
			{
				Month = ParseMonth(GetRequiredAttribute(strategyElement, "month")),
				DayOfWeek = ParseRequiredEnum<DayOfWeek>(strategyElement, "dayOfWeek"),
				WeekOrdinal = ParseRequiredEnum<WeekOfMonthOrdinal>(strategyElement, "weekOrdinal"),
			},
			DateResolutionStrategy.OffsetFromAnchor => rule with
			{
				AnchorRuleName = GetRequiredAttribute(strategyElement, "name"),
				OffsetDays = int.Parse(GetRequiredAttribute(strategyElement, "offset"), CultureInfo.InvariantCulture),
			},
			DateResolutionStrategy.Calculator => rule with
			{
				CalculatorKey = GetOptionalAttribute(strategyElement, "key"),
				CalculatorType = ParseOptionalType<INotableDateCalculator>(strategyElement, "type"),
			},
			_ => throw new NotSupportedException($"Unsupported strategy: {rule.Strategy}.")
		};

	private static ObservanceAdjustment ParseAdjustment(XElement element) =>
		new()
		{
			Trigger = ParseRequiredEnum<AdjustmentTrigger>(element, "when"),
			Action = ParseRequiredEnum<AdjustmentAction>(element, "action"),
			DayOfWeek = ParseOptionalEnum<DayOfWeek>(element, "dayOfWeek"),
			WeekOrdinal = ParseOptionalEnum<WeekOfMonthOrdinal>(element, "weekOrdinal"),
			IsNonWorkingDay = ParseOptionalBool(element, "nonWorking"),
			OffsetDays = ParseOptionalInt(element, "days") ?? 0,
			TerritoryCode = GetOptionalAttribute(element, "territory"),
			CalendarType = ParseOptionalType<SysGlobal.Calendar>(element, "calendarType"),
			EffectiveFromYear = ParseOptionalInt(element, "fromYear"),
			EffectiveToYear = ParseOptionalInt(element, "toYear"),
			ComparisonDate = ParseOptionalMonthDay(element, "comparisonMonth", "comparisonDay"),
			TargetRuleName = GetOptionalAttribute(element, "target"),
			Priority = ParseOptionalInt(element, "priority") ?? 100,
			HandlerKey = GetOptionalAttribute(element, "handlerKey"),
		};

	// ----------------------------------------------------------------------------
	// Attribute helpers
	// ----------------------------------------------------------------------------

	private static string GetRequiredAttribute(XElement element, string attributeName) =>
		element.Attribute(attributeName)?.Value
			?? throw new InvalidOperationException($"Missing required attribute '{attributeName}' on element '{element.Name.LocalName}'.");

	private static string? GetOptionalAttribute(XElement element, string attributeName) =>
		element.Attribute(attributeName)?.Value;

	private static TEnum ParseRequiredEnum<TEnum>(XElement element, string attributeName) where TEnum : struct, Enum =>
		Enum.TryParse<TEnum>(GetRequiredAttribute(element, attributeName), ignoreCase: true, out var result)
			? result
			: throw new InvalidOperationException($"Invalid value for attribute '{attributeName}' on element '{element.Name.LocalName}'.");

	private static TEnum? ParseOptionalEnum<TEnum>(XElement element, string attributeName) where TEnum : struct, Enum
	{
		var raw = GetOptionalAttribute(element, attributeName);
		return raw is not null && Enum.TryParse<TEnum>(raw, ignoreCase: true, out var result) ? result : null;
	}

	private static int? ParseOptionalInt(XElement element, string attributeName)
	{
		var raw = GetOptionalAttribute(element, attributeName);
		return raw is not null && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : (int?)null;
	}

	private static bool? ParseOptionalBool(XElement element, string attributeName)
	{
		var raw = GetOptionalAttribute(element, attributeName);
		return raw is not null && bool.TryParse(raw, out var result) ? result : (bool?)null;
	}

	private static DateTime? ParseOptionalMonthDay(XElement element, string monthAttr, string dayAttr)
	{
		var month = GetOptionalAttribute(element, monthAttr);
		var day = GetOptionalAttribute(element, dayAttr);
		if (month is null || day is null) return null;

		int monthValue = ParseMonth(month);
		if (!int.TryParse(day, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dayValue)) return null;

		// Year is irrelevant for comparison-date authoring; the adjuster reprojects onto the resolved year.
		return new DateTime(2000, monthValue, dayValue, 0, 0, 0, DateTimeKind.Unspecified);
	}

	private static Type? ParseOptionalType<TBase>(XElement element, string attributeName)
	{
		var typeName = GetOptionalAttribute(element, attributeName);
		if (string.IsNullOrWhiteSpace(typeName)) return null;

		var type = Type.GetType(typeName, throwOnError: false);
		return type is not null && typeof(TBase).IsAssignableFrom(type) ? type : null;
	}

	private static int ParseMonth(string monthName)
	{
		ThrowHelper.ThrowIfNullOrEmpty(monthName);

		if (DateTime.TryParseExact(monthName, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
			return result.Month;

		throw new FormatException($"Invalid month name '{monthName}'. Expected a full English month name (e.g. 'January').");
	}

	// ----------------------------------------------------------------------------
	// Schema validation
	// ----------------------------------------------------------------------------

	private static XmlSchemaSet LoadSchema()
	{
		var assembly = Assembly.GetExecutingAssembly();
		const string schemaResourceName = "Bodu.Globalization.Calendar.NotableDates.xsd";

		using var stream = assembly.GetManifestResourceStream(schemaResourceName)
			?? throw new FileNotFoundException($"Embedded schema resource '{schemaResourceName}' not found in assembly '{assembly.FullName}'.");

		var schemaSet = new XmlSchemaSet();
		schemaSet.Add(null, XmlReader.Create(stream));
		return schemaSet;
	}

	private static XmlReaderSettings CreateValidationSettings()
	{
		var settings = new XmlReaderSettings
		{
			ValidationType = ValidationType.Schema,
			Schemas = SchemaSet,
			ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings,
		};
		settings.ValidationEventHandler += HandleValidationEvent;
		return settings;
	}

	private static void ValidateDocument(XDocument document)
	{
		using var reader = document.CreateReader();
		using var validatingReader = XmlReader.Create(reader, CreateValidationSettings());
		while (validatingReader.Read()) { }
	}

	private static void HandleValidationEvent(object? sender, ValidationEventArgs e)
	{
		if (e.Severity == XmlSeverityType.Error)
			throw new XmlSchemaValidationException($"Schema validation error: {e.Message}", e.Exception);
	}

	// ----------------------------------------------------------------------------
	// JSON parser placeholder (preserved for future implementation)
	// ----------------------------------------------------------------------------
	//
	// The previous incomplete JSON parser has been intentionally kept out of the
	// public surface until it can be reinstated against the new NotableDateRule
	// vocabulary. When implementing, mirror ParseXml: validate against a JSON
	// schema (or strict deserialiser), then map each item through ApplyStrategySpecifics.
}
