// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.BclInterop.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
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
    // ============================================================================

    private const string BclInteropSchemaNamespace = "urn:bodu:globalization:calendar";

    /// <summary>
    /// Verifies that a Fixed rule authored via <see cref="NotableDateDocumentBuilder" />, projected onto a
    /// schema-conformant DTO, serialised by the BCL <see cref="XmlSerializer" />, and re-parsed by
    /// <see cref="NotableDateRuleParser" /> round-trips with every core field preserved.
    /// </summary>
    [TestMethod]
    public void BclInterop_BuilderAuthoredFixedRule_XmlSerializerOutput_ShouldParseViaLibraryParser()
    {
        IReadOnlyList<NotableDateRule> built = NotableDateDocumentBuilder.Create()
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .NonWorking()
                    .Fixed(12, 25)))
            .Build();

        BclXmlNotableDates dto = ProjectToBclXmlDto(built);
        string xml = SerialiseViaXmlSerializer(dto);

        List<NotableDateRule> parsed = NotableDateRuleParser.ParseXml(xml);

        Assert.AreEqual(1, parsed.Count);
        NotableDateRule rule = parsed[0];
        Assert.AreEqual(built[0].Name, rule.Name);
        Assert.AreEqual(built[0].Strategy, rule.Strategy);
        Assert.AreEqual(built[0].Category, rule.Category);
        Assert.AreEqual(built[0].IsNonWorkingDay, rule.IsNonWorkingDay);
        Assert.AreEqual(built[0].Month, rule.Month);
        Assert.AreEqual(built[0].Day, rule.Day);
    }

    /// <summary>
    /// Verifies that a Fixed rule authored via <see cref="NotableDateDocumentBuilder" />, projected onto a
    /// schema-conformant DTO, serialised by the BCL <see cref="JsonSerializer" />, and re-parsed by
    /// <see cref="NotableDateRuleJsonParser" /> round-trips with every core field preserved.
    /// </summary>
    [TestMethod]
    public void BclInterop_BuilderAuthoredFixedRule_JsonSerializerOutput_ShouldParseViaLibraryParser()
    {
        IReadOnlyList<NotableDateRule> built = NotableDateDocumentBuilder.Create()
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .NonWorking()
                    .Fixed(12, 25)))
            .Build();

        BclJsonNotableDates dto = ProjectToBclJsonDto(built);
        string json = SerialiseViaJsonSerializer(dto);

        List<NotableDateRule> parsed = NotableDateRuleJsonParser.ParseJson(json);

        Assert.AreEqual(1, parsed.Count);
        NotableDateRule rule = parsed[0];
        Assert.AreEqual(built[0].Name, rule.Name);
        Assert.AreEqual(built[0].Strategy, rule.Strategy);
        Assert.AreEqual(built[0].Category, rule.Category);
        Assert.AreEqual(built[0].IsNonWorkingDay, rule.IsNonWorkingDay);
        Assert.AreEqual(built[0].Month, rule.Month);
        Assert.AreEqual(built[0].Day, rule.Day);
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
    /// matches <see cref="NotableDateRule.Strategy" />.
    /// </summary>
    /// <param name="rule">The source rule.</param>
    /// <returns>The DTO representation.</returns>
    private static BclXmlRule ProjectRuleToBclXml(NotableDateRule rule) =>
        new()
        {
            Name = rule.RuleName ?? $"{rule.Name} ({rule.Strategy})",
            Category = rule.Category,
            NonWorking = rule.IsNonWorkingDay ?? false,
            NonWorkingSpecified = rule.IsNonWorkingDay.HasValue,
            Fixed = rule.Strategy == DateResolutionStrategy.Fixed && rule.Month.HasValue
                ? new BclXmlFixed
                {
                    Month = GregorianMonthName(rule.Month.Value),
                    Day = rule.Day!.Value,
                }
                : null,
        };

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
    /// matches <see cref="NotableDateRule.Strategy" />.
    /// </summary>
    /// <param name="rule">The source rule.</param>
    /// <returns>The DTO representation.</returns>
    private static BclJsonRule ProjectRuleToBclJson(NotableDateRule rule) =>
        new()
        {
            Name = rule.RuleName ?? $"{rule.Name} ({rule.Strategy})",
            Category = rule.Category,
            NonWorking = rule.IsNonWorkingDay,
            Fixed = rule.Strategy == DateResolutionStrategy.Fixed && rule.Month.HasValue
                ? new BclJsonFixed
                {
                    Month = GregorianMonthName(rule.Month.Value),
                    Day = rule.Day!.Value,
                }
                : null,
        };

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

        using XmlWriter writer = XmlWriter.Create(sb, settings);
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
    /// <c>monthOrNumber</c> simple type in the schema.
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
    /// A single <c>Rule</c> element. Only the Fixed strategy is modelled here; other strategies follow the
    /// same attribute / child-element pattern when this interop surface is extended.
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

        /// <summary>Gets or sets the Fixed strategy element.</summary>
        /// <returns>The <c>Fixed</c> child element, or <see langword="null" /> when another strategy applies.</returns>
        [XmlElement("Fixed", Namespace = BclInteropSchemaNamespace)]
        public BclXmlFixed? Fixed { get; set; }
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
    /// A single rule entry. Only the Fixed strategy is modelled here; other strategies follow the same
    /// property pattern when this interop surface is extended.
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

        /// <summary>Gets or sets the Fixed strategy object.</summary>
        /// <returns>The <c>fixed</c> nested object, or <see langword="null" /> when another strategy applies.</returns>
        [JsonPropertyName("fixed")]
        public BclJsonFixed? Fixed { get; set; }
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
}
