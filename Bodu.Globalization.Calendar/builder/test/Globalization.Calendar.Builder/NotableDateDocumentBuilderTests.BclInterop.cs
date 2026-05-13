// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.BclInterop.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Bodu.Globalization.Calendar;

public partial class NotableDateDocumentBuilderTests
{
    // ============================================================================
    // BCL serialiser interop
    //
    // These tests confirm the contract for downstream consumers who author rules
    // via the fluent builder, project them onto schema-conformant DTOs of their
    // own, serialise via the BCL XmlSerializer / JsonSerializer, and feed the
    // resulting payload through NotableDateRuleParser / NotableDateRuleJsonParser.
    // The builder itself does not expose serialisation-attributed types — it
    // emits XLinq / JsonNode trees directly — so the DTOs live alongside the
    // tests and are populated from the rules returned by `Build()`.
    //
    // The document exercised here mixes all four resolution strategies plus the
    // optional scalars, tags, and adjustments that survive both XML and JSON
    // schema validation, so a green run proves the round-trip is viable for a
    // realistic policy document rather than a single trivial rule.
    // ============================================================================

    private const string BclInteropSchemaNamespace = "urn:bodu:globalization:calendar";

    /// <summary>
    /// Verifies that a multi-rule document authored via <see cref="NotableDateDocumentBuilder" /> — mixing
    /// every resolution strategy, optional scalars, tags, and adjustments — projects onto schema-conformant
    /// DTOs, serialises through the BCL <see cref="XmlSerializer" />, and re-parses by
    /// <see cref="NotableDateRuleParser" /> with every authored field preserved.
    /// </summary>
    [TestMethod]
    public void BclInterop_BuilderAuthoredComprehensiveDocument_XmlSerializerOutput_ShouldParseViaLibraryParser()
    {
        IReadOnlyList<NotableDateRule> built = BuildComprehensiveRules();
        BclXmlNotableDates dto = ProjectToBclXmlDto(built);

        string xml = SerialiseViaXmlSerializer(dto);
        List<NotableDateRule> parsed = NotableDateRuleParser.ParseXml(xml);

        AssertRuleListsEquivalent(built, parsed);
    }

    /// <summary>
    /// Verifies that a multi-rule document authored via <see cref="NotableDateDocumentBuilder" /> — mixing
    /// every resolution strategy, optional scalars, tags, and adjustments — projects onto schema-conformant
    /// DTOs, serialises through the BCL <see cref="JsonSerializer" />, and re-parses by
    /// <see cref="NotableDateRuleJsonParser" /> with every authored field preserved.
    /// </summary>
    [TestMethod]
    public void BclInterop_BuilderAuthoredComprehensiveDocument_JsonSerializerOutput_ShouldParseViaLibraryParser()
    {
        IReadOnlyList<NotableDateRule> built = BuildComprehensiveRules();
        BclJsonNotableDates dto = ProjectToBclJsonDto(built);

        string json = SerialiseViaJsonSerializer(dto);
        List<NotableDateRule> parsed = NotableDateRuleJsonParser.ParseJson(json);

        AssertRuleListsEquivalent(built, parsed);
    }

    /// <summary>
    /// Verifies that an embedded library resource — the canonical <c>default-minimal.xml</c> shipped with
    /// the calendar assembly — parses, projects onto schema-conformant DTOs, serialises through the BCL
    /// <see cref="XmlSerializer" />, and re-parses with every rule field intact. Proves the round-trip
    /// works for authored content that was not produced by the fluent builder.
    /// </summary>
    [TestMethod]
    public void BclInterop_ResourceXmlPayload_XmlSerializerOutput_ShouldReparseIdentically()
    {
        string sourceXml = LoadEmbeddedXmlResource("Bodu.Globalization.Calendar.Resources.default-minimal.xml");

        List<NotableDateRule> fromResource = NotableDateRuleParser.ParseXml(sourceXml);
        BclXmlNotableDates dto = ProjectToBclXmlDto(fromResource);
        string xmlFromBcl = SerialiseViaXmlSerializer(dto);
        List<NotableDateRule> fromBcl = NotableDateRuleParser.ParseXml(xmlFromBcl);

        AssertRuleListsEquivalent(fromResource, fromBcl);
    }

    /// <summary>
    /// Verifies that an embedded library resource — the canonical <c>default-minimal.xml</c> shipped with
    /// the calendar assembly — parses, projects onto schema-conformant DTOs, serialises through the BCL
    /// <see cref="JsonSerializer" />, and parses back through <see cref="NotableDateRuleJsonParser" />
    /// with every rule field intact. Demonstrates that the same authored content survives the cross-format
    /// XML → JSON round-trip.
    /// </summary>
    [TestMethod]
    public void BclInterop_ResourceXmlPayload_JsonSerializerOutput_ShouldReparseIdentically()
    {
        string sourceXml = LoadEmbeddedXmlResource("Bodu.Globalization.Calendar.Resources.default-minimal.xml");

        List<NotableDateRule> fromResource = NotableDateRuleParser.ParseXml(sourceXml);
        BclJsonNotableDates dto = ProjectToBclJsonDto(fromResource);
        string jsonFromBcl = SerialiseViaJsonSerializer(dto);
        List<NotableDateRule> fromBcl = NotableDateRuleJsonParser.ParseJson(jsonFromBcl);

        AssertRuleListsEquivalent(fromResource, fromBcl);
    }

    /// <summary>
    /// Loads an embedded XML resource from the main calendar assembly's manifest.
    /// </summary>
    /// <param name="manifestResourceName">The fully qualified manifest resource name (e.g. <c>Bodu.Globalization.Calendar.Resources.default-minimal.xml</c>).</param>
    /// <returns>The UTF-8 decoded resource content.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the resource is not found in the assembly manifest.</exception>
    private static string LoadEmbeddedXmlResource(string manifestResourceName)
    {
        Assembly assembly = typeof(NotableDateRuleParser).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(manifestResourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {manifestResourceName}.");
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    // ============================================================================
    // Fixture
    // ============================================================================

    /// <summary>
    /// Builds the comprehensive set of rules exercised by both interop tests: one entry per resolution
    /// strategy, with the optional scalars, tags, and adjustments that the schemas accept.
    /// </summary>
    /// <returns>The rules produced by <see cref="NotableDateDocumentBuilder.Build" />.</returns>
    private static IReadOnlyList<NotableDateRule> BuildComprehensiveRules() =>
        NotableDateDocumentBuilder.Create()
            .AddDate("Australia Day", date => date
                .AddRule(rule => rule
                    .RuleName("Australia Day (Fixed)")
                    .Category(NotableDateCategory.Holiday)
                    .Territory("AU")
                    .NonWorking()
                    .FirstYear(1994)
                    .Priority(10)
                    .AddTag("Federal")
                    .Fixed(1, 26)
                    .AddAdjustment("weekend-roll", adj => adj
                        .When(AdjustmentTrigger.IfWeekend)
                        .Action(AdjustmentAction.MoveToNextWeekday))))
            .AddDate("Easter Sunday", date => date
                .AddRule(rule => rule
                    .RuleName("Easter Sunday (Algorithm)")
                    .Category(NotableDateCategory.Religious)
                    .AddTag("Christian")
                    .Algorithm(key: "easter-gregorian")))
            .AddDate("Easter Monday", date => date
                .AddRule(rule => rule
                    .RuleName("Easter Monday (OffsetFromAnchor)")
                    .Category(NotableDateCategory.Holiday)
                    .NonWorking()
                    .OffsetFromAnchor("Easter Sunday", 1)))
            .AddDate("Thanksgiving", date => date
                .AddRule(rule => rule
                    .RuleName("Thanksgiving (DayOfWeekInMonth)")
                    .Category(NotableDateCategory.Holiday)
                    .Territory("US")
                    .DayOfWeekInMonth(11, DayOfWeek.Thursday, WeekOfMonthOrdinal.Fourth)))
            .Build();

    /// <summary>
    /// Asserts that two lists of rules are equivalent on every field that survives the round-trip through
    /// schema-conformant serialisation.
    /// </summary>
    /// <param name="expected">The reference rule list (typically the builder output or the resource parse).</param>
    /// <param name="actual">The rule list produced by the library parser after the BCL serialisation pass.</param>
    private static void AssertRuleListsEquivalent(IReadOnlyList<NotableDateRule> expected, IReadOnlyList<NotableDateRule> actual)
    {
        Assert.AreEqual(expected.Count, actual.Count, "Rule count mismatch.");
        for (int i = 0; i < expected.Count; i++)
        {
            NotableDateRule e = expected[i];
            NotableDateRule a = actual[i];

            Assert.AreEqual(e.Name, a.Name, $"Name mismatch at index {i}.");
            Assert.AreEqual(e.RuleName, a.RuleName, $"RuleName mismatch at index {i}.");
            Assert.AreEqual(e.Strategy, a.Strategy, $"Strategy mismatch at index {i}.");
            Assert.AreEqual(e.Category, a.Category, $"Category mismatch at index {i}.");
            Assert.AreEqual(e.IsNonWorkingDay, a.IsNonWorkingDay, $"IsNonWorkingDay mismatch at index {i}.");
            Assert.AreEqual(e.FirstYear, a.FirstYear, $"FirstYear mismatch at index {i}.");
            Assert.AreEqual(e.Priority, a.Priority, $"Priority mismatch at index {i}.");
            Assert.AreEqual(e.TerritoryCode, a.TerritoryCode, $"TerritoryCode mismatch at index {i}.");
            Assert.AreEqual(e.Month, a.Month, $"Month mismatch at index {i}.");
            Assert.AreEqual(e.Day, a.Day, $"Day mismatch at index {i}.");
            Assert.AreEqual(e.DayOfWeek, a.DayOfWeek, $"DayOfWeek mismatch at index {i}.");
            Assert.AreEqual(e.WeekOrdinal, a.WeekOrdinal, $"WeekOrdinal mismatch at index {i}.");
            Assert.AreEqual(e.AnchorRuleName, a.AnchorRuleName, $"AnchorRuleName mismatch at index {i}.");
            Assert.AreEqual(e.OffsetDays, a.OffsetDays, $"OffsetDays mismatch at index {i}.");
            Assert.AreEqual(e.AlgorithmKey, a.AlgorithmKey, $"AlgorithmKey mismatch at index {i}.");

            // Tags are an order-independent set.
            CollectionAssert.AreEquivalent(e.Tags.ToList(), a.Tags.ToList(), $"Tags mismatch at index {i}.");

            Assert.AreEqual(e.Adjustments.Length, a.Adjustments.Length, $"Adjustment count mismatch at index {i}.");
            for (int j = 0; j < e.Adjustments.Length; j++)
            {
                ObservanceAdjustment ea = e.Adjustments[j];
                ObservanceAdjustment aa = a.Adjustments[j];
                Assert.AreEqual(ea.Key, aa.Key, $"Adjustment key mismatch at rule {i}, adjustment {j}.");
                Assert.AreEqual(ea.Trigger, aa.Trigger, $"Adjustment trigger mismatch at rule {i}, adjustment {j}.");
                Assert.AreEqual(ea.Action, aa.Action, $"Adjustment action mismatch at rule {i}, adjustment {j}.");
            }
        }
    }

    // ============================================================================
    // Projection helpers (NotableDateRule → BCL DTOs)
    // ============================================================================

    /// <summary>
    /// Projects the built rules onto a <see cref="BclXmlNotableDates" /> tree, grouping by canonical
    /// notable-date name to mirror the NotableDate / Rule hierarchy expected by <c>NotableDates.xsd</c>.
    /// </summary>
    /// <param name="rules">The rules produced by <see cref="NotableDateDocumentBuilder.Build" />.</param>
    /// <returns>A DTO graph ready for <see cref="XmlSerializer" />.</returns>
    private static BclXmlNotableDates ProjectToBclXmlDto(IReadOnlyList<NotableDateRule> rules) =>
        new()
        {
            NotableDates = rules
                .GroupBy(r => r.Name)
                .Select(g => new BclXmlNotableDate
                {
                    Name = g.Key,
                    Rules = g.Select(ProjectRuleToBclXml).ToList(),
                })
                .ToList(),
        };

    /// <summary>
    /// Projects a single rule onto a <see cref="BclXmlRule" />, populating the strategy element that
    /// matches <see cref="NotableDateRule.Strategy" /> plus any authored tags and adjustments.
    /// </summary>
    /// <param name="rule">The source rule.</param>
    /// <returns>The DTO representation.</returns>
    private static BclXmlRule ProjectRuleToBclXml(NotableDateRule rule)
    {
        BclXmlRule dto = new()
        {
            Name = rule.RuleName ?? $"{rule.Name} ({rule.Strategy})",
            Category = rule.Category,
            NonWorking = rule.IsNonWorkingDay ?? false,
            NonWorkingSpecified = rule.IsNonWorkingDay.HasValue,
            FirstYear = (ushort)(rule.FirstYear ?? 0),
            FirstYearSpecified = rule.FirstYear.HasValue,
            Priority = rule.Priority,
            PrioritySpecified = rule.Priority != 100,
            Territory = rule.TerritoryCode,
            Tags = rule.Tags.ToList(),
            Adjustments = rule.Adjustments
                .Select(a => new BclXmlAdjustment
                {
                    Key = a.Key,
                    When = a.Trigger,
                    Action = a.Action,
                })
                .ToList(),
        };

        switch (rule.Strategy)
        {
            case DateResolutionStrategy.Fixed:
                dto.Fixed = new BclXmlFixed
                {
                    Month = GregorianMonthName(rule.Month!.Value),
                    Day = rule.Day!.Value,
                };
                break;
            case DateResolutionStrategy.DayOfWeekInMonth:
                dto.DayOfWeekInMonth = new BclXmlDayOfWeekInMonth
                {
                    Month = GregorianMonthName(rule.Month!.Value),
                    DayOfWeek = rule.DayOfWeek!.Value,
                    WeekOrdinal = rule.WeekOrdinal!.Value,
                };
                break;
            case DateResolutionStrategy.OffsetFromAnchor:
                dto.OffsetFromAnchor = new BclXmlOffsetFromAnchor
                {
                    Name = rule.AnchorRuleName!,
                    Offset = rule.OffsetDays!.Value,
                };
                break;
            case DateResolutionStrategy.Algorithm:
                dto.Algorithm = new BclXmlAlgorithm
                {
                    Key = rule.AlgorithmKey,
                };
                break;
            default:
                throw new NotSupportedException($"Unsupported strategy: {rule.Strategy}");
        }

        return dto;
    }

    /// <summary>
    /// Projects the built rules onto a <see cref="BclJsonNotableDates" /> tree, grouping by canonical
    /// notable-date name to mirror the structure expected by <c>NotableDates.schema.json</c>.
    /// </summary>
    /// <param name="rules">The rules produced by <see cref="NotableDateDocumentBuilder.Build" />.</param>
    /// <returns>A DTO graph ready for <see cref="JsonSerializer" />.</returns>
    private static BclJsonNotableDates ProjectToBclJsonDto(IReadOnlyList<NotableDateRule> rules) =>
        new()
        {
            NotableDates = rules
                .GroupBy(r => r.Name)
                .Select(g => new BclJsonNotableDate
                {
                    Name = g.Key,
                    Rules = g.Select(ProjectRuleToBclJson).ToList(),
                })
                .ToList(),
        };

    /// <summary>
    /// Projects a single rule onto a <see cref="BclJsonRule" />, populating the strategy property that
    /// matches <see cref="NotableDateRule.Strategy" /> plus any authored tags and adjustments.
    /// </summary>
    /// <param name="rule">The source rule.</param>
    /// <returns>The DTO representation.</returns>
    private static BclJsonRule ProjectRuleToBclJson(NotableDateRule rule)
    {
        BclJsonRule dto = new()
        {
            Name = rule.RuleName ?? $"{rule.Name} ({rule.Strategy})",
            Category = rule.Category,
            NonWorking = rule.IsNonWorkingDay,
            FirstYear = rule.FirstYear,
            Priority = rule.Priority != 100 ? rule.Priority : null,
            Territory = rule.TerritoryCode,
            Tags = rule.Tags.Count > 0 ? rule.Tags.ToList() : null,
            Adjustments = rule.Adjustments.Length > 0
                ? rule.Adjustments
                    .Select(a => new BclJsonAdjustment { Key = a.Key, When = a.Trigger, Action = a.Action })
                    .ToList()
                : null,
        };

        switch (rule.Strategy)
        {
            case DateResolutionStrategy.Fixed:
                dto.Fixed = new BclJsonFixed
                {
                    Month = GregorianMonthName(rule.Month!.Value),
                    Day = rule.Day!.Value,
                };
                break;
            case DateResolutionStrategy.DayOfWeekInMonth:
                dto.DayOfWeekInMonth = new BclJsonDayOfWeekInMonth
                {
                    Month = GregorianMonthName(rule.Month!.Value),
                    DayOfWeek = rule.DayOfWeek!.Value,
                    WeekOrdinal = rule.WeekOrdinal!.Value,
                };
                break;
            case DateResolutionStrategy.OffsetFromAnchor:
                dto.OffsetFromAnchor = new BclJsonOffsetFromAnchor
                {
                    Name = rule.AnchorRuleName!,
                    Offset = rule.OffsetDays!.Value,
                };
                break;
            case DateResolutionStrategy.Algorithm:
                dto.Algorithm = new BclJsonAlgorithm
                {
                    Key = rule.AlgorithmKey,
                };
                break;
            default:
                throw new NotSupportedException($"Unsupported strategy: {rule.Strategy}");
        }

        return dto;
    }

    /// <summary>
    /// Serialises the DTO graph via <see cref="XmlSerializer" /> with the schema namespace as the default,
    /// producing indented UTF-8 XML with an XML declaration.
    /// </summary>
    /// <param name="dto">The root DTO to serialise.</param>
    /// <returns>The XML payload.</returns>
    private static string SerialiseViaXmlSerializer(BclXmlNotableDates dto)
    {
        StringBuilder sb = new();
        XmlWriterSettings settings = new()
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false,
        };

        // Declare the schema namespace as the default (empty prefix) to suppress
        // xmlns:xsi / xmlns:xsd from the XmlSerializer output and produce a payload
        // shaped like NotableDateDocumentBuilder.ToXml.
        XmlSerializerNamespaces namespaces = new();
        namespaces.Add(string.Empty, BclInteropSchemaNamespace);

        using var writer = XmlWriter.Create(sb, settings);
        new XmlSerializer(typeof(BclXmlNotableDates)).Serialize(writer, dto, namespaces);
        return sb.ToString();
    }

    /// <summary>
    /// Serialises the DTO graph via <see cref="JsonSerializer" /> with indented output, string-encoded
    /// enums, and null property suppression so the result conforms to <c>NotableDates.schema.json</c>.
    /// </summary>
    /// <param name="dto">The root DTO to serialise.</param>
    /// <returns>The JSON payload.</returns>
    private static string SerialiseViaJsonSerializer(BclJsonNotableDates dto)
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() },
        };
        return JsonSerializer.Serialize(dto, options);
    }

    /// <summary>
    /// Converts a Gregorian month number (1–12) to its full English name as required by the
    /// <c>monthOrNumber</c> and <c>monthName</c> simple types in the schema.
    /// </summary>
    /// <param name="month">The month number.</param>
    /// <returns>The full English month name (e.g. <c>"December"</c>).</returns>
    private static string GregorianMonthName(int month) =>
        new DateTime(2000, month, 1).ToString("MMMM", CultureInfo.InvariantCulture);

    // ============================================================================
    // BCL XML DTOs — schema-conformant projection of NotableDates.xsd.
    // ============================================================================

    /// <summary>
    /// The root <c>NotableDates</c> element of the schema, holding zero or more <c>NotableDate</c> children.
    /// </summary>
    [XmlRoot("NotableDates", Namespace = BclInteropSchemaNamespace)]
    public sealed class BclXmlNotableDates
    {
        /// <summary>Gets or sets the notable date entries.</summary>
        /// <returns>The child <c>NotableDate</c> elements; never <see langword="null" />.</returns>
        [XmlElement("NotableDate", Namespace = BclInteropSchemaNamespace)]
        public List<BclXmlNotableDate> NotableDates { get; set; } = new();
    }

    /// <summary>
    /// A single <c>NotableDate</c> element, carrying the canonical name and one or more authoring rules.
    /// </summary>
    public sealed class BclXmlNotableDate
    {
        /// <summary>Gets or sets the canonical notable-date title.</summary>
        /// <returns>The notable date name; the schema requires a non-empty value.</returns>
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the resolution rules for this notable date.</summary>
        /// <returns>The child <c>Rule</c> elements; the schema requires at least one.</returns>
        [XmlElement("Rule", Namespace = BclInteropSchemaNamespace)]
        public List<BclXmlRule> Rules { get; set; } = new();
    }

    /// <summary>
    /// A single <c>Rule</c> element. The strategy choice is modelled as four mutually exclusive properties
    /// (<see cref="Fixed" />, <see cref="DayOfWeekInMonth" />, <see cref="OffsetFromAnchor" />,
    /// <see cref="Algorithm" />); exactly one is set per projected rule, matching the schema's
    /// <c>&lt;xs:choice&gt;</c>.
    /// </summary>
    public sealed class BclXmlRule
    {
        /// <summary>Gets or sets the rule-level identifier.</summary>
        /// <returns>The <c>name</c> attribute value; the schema requires a non-empty value.</returns>
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the primary category for the produced notable date.</summary>
        /// <returns>One of the defined <see cref="NotableDateCategory" /> values; required by the schema.</returns>
        [XmlAttribute("category")]
        public NotableDateCategory Category { get; set; }

        /// <summary>Gets or sets whether the produced notable date is non-working.</summary>
        /// <returns>The optional <c>nonWorking</c> attribute value; honoured only when <see cref="NonWorkingSpecified" /> is <see langword="true" />.</returns>
        [XmlAttribute("nonWorking")]
        public bool NonWorking { get; set; }

        /// <summary>Gets or sets a value indicating whether <see cref="NonWorking" /> should be emitted.</summary>
        /// <returns><see langword="true" /> to include the attribute; otherwise <see langword="false" />.</returns>
        /// <remarks>
        /// <para>
        /// XmlSerializer's <c>FieldSpecified</c> pattern: a sibling <c>bool</c> property whose name is
        /// <c>{Field}Specified</c> controls whether the optional value-type attribute is written.
        /// </para>
        /// </remarks>
        [XmlIgnore]
        public bool NonWorkingSpecified { get; set; }

        /// <summary>Gets or sets the inclusive first applicable year.</summary>
        /// <returns>The <c>firstYear</c> attribute value; honoured only when <see cref="FirstYearSpecified" /> is <see langword="true" />.</returns>
        [XmlAttribute("firstYear")]
        public ushort FirstYear { get; set; }

        /// <summary>Gets or sets a value indicating whether <see cref="FirstYear" /> should be emitted.</summary>
        /// <returns><see langword="true" /> to include the attribute; otherwise <see langword="false" />.</returns>
        [XmlIgnore]
        public bool FirstYearSpecified { get; set; }

        /// <summary>Gets or sets the tie-break priority.</summary>
        /// <returns>The <c>priority</c> attribute value; honoured only when <see cref="PrioritySpecified" /> is <see langword="true" />.</returns>
        [XmlAttribute("priority")]
        public int Priority { get; set; }

        /// <summary>Gets or sets a value indicating whether <see cref="Priority" /> should be emitted.</summary>
        /// <returns><see langword="true" /> to include the attribute; otherwise <see langword="false" />.</returns>
        [XmlIgnore]
        public bool PrioritySpecified { get; set; }

        /// <summary>Gets or sets the territory scope.</summary>
        /// <returns>The <c>territory</c> attribute value, or <see langword="null" /> when the rule applies globally.</returns>
        [XmlAttribute("territory")]
        public string? Territory { get; set; }

        // Strategy choice — at most one of the following four is populated per rule.

        /// <summary>Gets or sets the Fixed strategy element.</summary>
        /// <returns>The <c>Fixed</c> child element, or <see langword="null" /> when another strategy applies.</returns>
        [XmlElement("Fixed", Namespace = BclInteropSchemaNamespace)]
        public BclXmlFixed? Fixed { get; set; }

        /// <summary>Gets or sets the DayOfWeekInMonth strategy element.</summary>
        /// <returns>The <c>DayOfWeekInMonth</c> child element, or <see langword="null" /> when another strategy applies.</returns>
        [XmlElement("DayOfWeekInMonth", Namespace = BclInteropSchemaNamespace)]
        public BclXmlDayOfWeekInMonth? DayOfWeekInMonth { get; set; }

        /// <summary>Gets or sets the OffsetFromAnchor strategy element.</summary>
        /// <returns>The <c>OffsetFromAnchor</c> child element, or <see langword="null" /> when another strategy applies.</returns>
        [XmlElement("OffsetFromAnchor", Namespace = BclInteropSchemaNamespace)]
        public BclXmlOffsetFromAnchor? OffsetFromAnchor { get; set; }

        /// <summary>Gets or sets the Algorithm strategy element.</summary>
        /// <returns>The <c>Algorithm</c> child element, or <see langword="null" /> when another strategy applies.</returns>
        [XmlElement("Algorithm", Namespace = BclInteropSchemaNamespace)]
        public BclXmlAlgorithm? Algorithm { get; set; }

        /// <summary>Gets or sets the tag values.</summary>
        /// <returns>The repeated <c>Tag</c> elements; empty when no tags are authored.</returns>
        [XmlElement("Tag", Namespace = BclInteropSchemaNamespace)]
        public List<string> Tags { get; set; } = new();

        /// <summary>Gets or sets the adjustments.</summary>
        /// <returns>The repeated <c>Adjustment</c> elements; empty when no adjustments are authored.</returns>
        [XmlElement("Adjustment", Namespace = BclInteropSchemaNamespace)]
        public List<BclXmlAdjustment> Adjustments { get; set; } = new();
    }

    /// <summary>
    /// The <c>Fixed</c> strategy child element, carrying the month token and day-of-month.
    /// </summary>
    public sealed class BclXmlFixed
    {
        /// <summary>Gets or sets the month token (English month name or 1–13 numeric).</summary>
        /// <returns>The <c>month</c> attribute value; required by the schema.</returns>
        [XmlAttribute("month")]
        public string Month { get; set; } = string.Empty;

        /// <summary>Gets or sets the day of month.</summary>
        /// <returns>The <c>day</c> attribute value in the range 1–31; required by the schema.</returns>
        [XmlAttribute("day")]
        public int Day { get; set; }
    }

    /// <summary>
    /// The <c>DayOfWeekInMonth</c> strategy child element, locking onto the <em>n</em>th occurrence of a
    /// weekday within a Gregorian month.
    /// </summary>
    public sealed class BclXmlDayOfWeekInMonth
    {
        /// <summary>Gets or sets the Gregorian month name.</summary>
        /// <returns>The <c>month</c> attribute value; required by the schema.</returns>
        [XmlAttribute("month")]
        public string Month { get; set; } = string.Empty;

        /// <summary>Gets or sets the weekday.</summary>
        /// <returns>The <c>dayOfWeek</c> attribute value; required by the schema.</returns>
        [XmlAttribute("dayOfWeek")]
        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>Gets or sets the ordinal occurrence within the month.</summary>
        /// <returns>The <c>weekOrdinal</c> attribute value; required by the schema.</returns>
        [XmlAttribute("weekOrdinal")]
        public WeekOfMonthOrdinal WeekOrdinal { get; set; }
    }

    /// <summary>
    /// The <c>OffsetFromAnchor</c> strategy child element, resolving relative to another named rule.
    /// </summary>
    public sealed class BclXmlOffsetFromAnchor
    {
        /// <summary>Gets or sets the anchor rule name.</summary>
        /// <returns>The <c>name</c> attribute value; required by the schema.</returns>
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the day offset relative to the anchor date.</summary>
        /// <returns>The <c>offset</c> attribute value; required by the schema.</returns>
        [XmlAttribute("offset")]
        public int Offset { get; set; }
    }

    /// <summary>
    /// The <c>Algorithm</c> strategy child element, delegating resolution to a registered algorithm.
    /// </summary>
    public sealed class BclXmlAlgorithm
    {
        /// <summary>Gets or sets the algorithm registry key.</summary>
        /// <returns>The <c>key</c> attribute value, or <see langword="null" /> when the algorithm is selected by type.</returns>
        [XmlAttribute("key")]
        public string? Key { get; set; }
    }

    /// <summary>
    /// An <c>Adjustment</c> child element, expressing a conditional date shift attached to the parent rule.
    /// </summary>
    public sealed class BclXmlAdjustment
    {
        /// <summary>Gets or sets the adjustment key.</summary>
        /// <returns>The <c>key</c> attribute value; required by the schema.</returns>
        [XmlAttribute("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>Gets or sets the activation condition.</summary>
        /// <returns>The <c>when</c> attribute value; required by the schema.</returns>
        [XmlAttribute("when")]
        public AdjustmentTrigger When { get; set; }

        /// <summary>Gets or sets the modification applied when the adjustment activates.</summary>
        /// <returns>The <c>action</c> attribute value; required by the schema.</returns>
        [XmlAttribute("action")]
        public AdjustmentAction Action { get; set; }
    }

    // ============================================================================
    // BCL JSON DTOs — schema-conformant projection of NotableDates.schema.json.
    // ============================================================================

    /// <summary>
    /// The root JSON object of the schema, holding a <c>notableDates</c> array.
    /// </summary>
    public sealed class BclJsonNotableDates
    {
        /// <summary>Gets or sets the notable date entries.</summary>
        /// <returns>The <c>notableDates</c> array; never <see langword="null" />.</returns>
        [JsonPropertyName("notableDates")]
        public List<BclJsonNotableDate> NotableDates { get; set; } = new();
    }

    /// <summary>
    /// A single <c>notableDate</c> entry, carrying the canonical name and one or more authoring rules.
    /// </summary>
    public sealed class BclJsonNotableDate
    {
        /// <summary>Gets or sets the canonical notable-date title.</summary>
        /// <returns>The notable date name; the schema requires a non-empty value.</returns>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the resolution rules for this notable date.</summary>
        /// <returns>The <c>rules</c> array; the schema requires at least one entry.</returns>
        [JsonPropertyName("rules")]
        public List<BclJsonRule> Rules { get; set; } = new();
    }

    /// <summary>
    /// A single rule entry. Exactly one of the strategy properties (<see cref="Fixed" />,
    /// <see cref="DayOfWeekInMonth" />, <see cref="OffsetFromAnchor" />, <see cref="Algorithm" />) is set
    /// per projected rule, matching the schema's <c>exactlyOneStrategy</c> constraint.
    /// </summary>
    public sealed class BclJsonRule
    {
        /// <summary>Gets or sets the rule-level identifier.</summary>
        /// <returns>The <c>name</c> property value; the schema requires a non-empty value.</returns>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the primary category for the produced notable date.</summary>
        /// <returns>One of the defined <see cref="NotableDateCategory" /> values; required by the schema.</returns>
        [JsonPropertyName("category")]
        public NotableDateCategory Category { get; set; }

        /// <summary>Gets or sets whether the produced notable date is non-working.</summary>
        /// <returns>The <c>nonWorking</c> property value, or <see langword="null" /> to omit the property.</returns>
        [JsonPropertyName("nonWorking")]
        public bool? NonWorking { get; set; }

        /// <summary>Gets or sets the inclusive first applicable year.</summary>
        /// <returns>The <c>firstYear</c> property value, or <see langword="null" /> to omit the property.</returns>
        [JsonPropertyName("firstYear")]
        public int? FirstYear { get; set; }

        /// <summary>Gets or sets the tie-break priority.</summary>
        /// <returns>The <c>priority</c> property value, or <see langword="null" /> to omit the property.</returns>
        [JsonPropertyName("priority")]
        public int? Priority { get; set; }

        /// <summary>Gets or sets the territory scope.</summary>
        /// <returns>The <c>territory</c> property value, or <see langword="null" /> when the rule applies globally.</returns>
        [JsonPropertyName("territory")]
        public string? Territory { get; set; }

        /// <summary>Gets or sets the Fixed strategy object.</summary>
        /// <returns>The <c>fixed</c> nested object, or <see langword="null" /> when another strategy applies.</returns>
        [JsonPropertyName("fixed")]
        public BclJsonFixed? Fixed { get; set; }

        /// <summary>Gets or sets the DayOfWeekInMonth strategy object.</summary>
        /// <returns>The <c>dayOfWeekInMonth</c> nested object, or <see langword="null" /> when another strategy applies.</returns>
        [JsonPropertyName("dayOfWeekInMonth")]
        public BclJsonDayOfWeekInMonth? DayOfWeekInMonth { get; set; }

        /// <summary>Gets or sets the OffsetFromAnchor strategy object.</summary>
        /// <returns>The <c>offsetFromAnchor</c> nested object, or <see langword="null" /> when another strategy applies.</returns>
        [JsonPropertyName("offsetFromAnchor")]
        public BclJsonOffsetFromAnchor? OffsetFromAnchor { get; set; }

        /// <summary>Gets or sets the Algorithm strategy object.</summary>
        /// <returns>The <c>algorithm</c> nested object, or <see langword="null" /> when another strategy applies.</returns>
        [JsonPropertyName("algorithm")]
        public BclJsonAlgorithm? Algorithm { get; set; }

        /// <summary>Gets or sets the tag values.</summary>
        /// <returns>The <c>tags</c> array, or <see langword="null" /> to omit the property.</returns>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        /// <summary>Gets or sets the adjustments.</summary>
        /// <returns>The <c>adjustments</c> array, or <see langword="null" /> to omit the property.</returns>
        [JsonPropertyName("adjustments")]
        public List<BclJsonAdjustment>? Adjustments { get; set; }
    }

    /// <summary>
    /// The <c>fixed</c> strategy nested object, carrying the month token and day-of-month.
    /// </summary>
    public sealed class BclJsonFixed
    {
        /// <summary>Gets or sets the month token (English month name or 1–13 numeric).</summary>
        /// <returns>The <c>month</c> property value; required by the schema.</returns>
        [JsonPropertyName("month")]
        public string Month { get; set; } = string.Empty;

        /// <summary>Gets or sets the day of month.</summary>
        /// <returns>The <c>day</c> property value in the range 1–31; required by the schema.</returns>
        [JsonPropertyName("day")]
        public int Day { get; set; }
    }

    /// <summary>
    /// The <c>dayOfWeekInMonth</c> strategy nested object.
    /// </summary>
    public sealed class BclJsonDayOfWeekInMonth
    {
        /// <summary>Gets or sets the Gregorian month name.</summary>
        /// <returns>The <c>month</c> property value; required by the schema.</returns>
        [JsonPropertyName("month")]
        public string Month { get; set; } = string.Empty;

        /// <summary>Gets or sets the weekday.</summary>
        /// <returns>The <c>dayOfWeek</c> property value; required by the schema.</returns>
        [JsonPropertyName("dayOfWeek")]
        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>Gets or sets the ordinal occurrence within the month.</summary>
        /// <returns>The <c>weekOrdinal</c> property value; required by the schema.</returns>
        [JsonPropertyName("weekOrdinal")]
        public WeekOfMonthOrdinal WeekOrdinal { get; set; }
    }

    /// <summary>
    /// The <c>offsetFromAnchor</c> strategy nested object.
    /// </summary>
    public sealed class BclJsonOffsetFromAnchor
    {
        /// <summary>Gets or sets the anchor rule name.</summary>
        /// <returns>The <c>name</c> property value; required by the schema.</returns>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the day offset relative to the anchor date.</summary>
        /// <returns>The <c>offset</c> property value; required by the schema.</returns>
        [JsonPropertyName("offset")]
        public int Offset { get; set; }
    }

    /// <summary>
    /// The <c>algorithm</c> strategy nested object.
    /// </summary>
    public sealed class BclJsonAlgorithm
    {
        /// <summary>Gets or sets the algorithm registry key.</summary>
        /// <returns>The <c>key</c> property value, or <see langword="null" /> when the algorithm is selected by type.</returns>
        [JsonPropertyName("key")]
        public string? Key { get; set; }
    }

    /// <summary>
    /// An <c>adjustment</c> object, expressing a conditional date shift attached to the parent rule.
    /// </summary>
    public sealed class BclJsonAdjustment
    {
        /// <summary>Gets or sets the adjustment key.</summary>
        /// <returns>The <c>key</c> property value; required by the schema.</returns>
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>Gets or sets the activation condition.</summary>
        /// <returns>The <c>when</c> property value; required by the schema.</returns>
        [JsonPropertyName("when")]
        public AdjustmentTrigger When { get; set; }

        /// <summary>Gets or sets the modification applied when the adjustment activates.</summary>
        /// <returns>The <c>action</c> property value; required by the schema.</returns>
        [JsonPropertyName("action")]
        public AdjustmentAction Action { get; set; }
    }
}
