// // ---------------------------------------------------------------------------------------------------------------
// // <copyright file="NotableDateDocumentBuilder.cs" company="PlaceholderCompany">
// //     Copyright (c) PlaceholderCompany. All rights reserved.
// // </copyright>
// // ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent interface for constructing a <c>NotableDates</c> document — a collection of named notable date entries —
/// that can be serialised to schema-valid XML or used directly as an <see cref="INotableDateRuleProvider" />.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="Create" /> to obtain a new builder, then call <see cref="AddDate(string, System.Action{NotableDateBuilder})" /> for
/// each notable date to define. When all entries are configured, call one of:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Build" /> — returns the rules as an <see cref="IReadOnlyList{T}" /> of <see cref="NotableDateRule" /> for direct in-process use.</description></item>
/// <item><description><see cref="ToXDocument" /> — serialises the rules to an <see cref="XDocument" /> matching the <c>NotableDates.xsd</c> schema.</description></item>
/// <item><description><see cref="ToXml" /> — serialises the rules to an XML string suitable for storage or transmission.</description></item>
/// <item><description><see cref="ToJsonNode" /> — serialises the rules to a <see cref="JsonObject" /> matching <c>NotableDates.schema.json</c>.</description></item>
/// <item><description><see cref="ToJson" /> — serialises the rules to an indented JSON string suitable for storage or transmission.</description></item>
/// <item><description><see cref="ToProvider" /> — wraps the built rules in an <see cref="INotableDateRuleProvider" /> ready for use with <see cref="NotableDateService" />.</description></item>
/// </list>
/// <para>
/// The XML produced by <see cref="ToXDocument" /> and <see cref="ToXml" /> conforms to the <c>urn:bodu:globalization:calendar</c>
/// namespace and can be round-tripped through <see cref="NotableDateRuleParser.ParseXml(string)" />. The JSON produced by
/// <see cref="ToJsonNode" /> and <see cref="ToJson" /> conforms to <c>NotableDates.schema.json</c> and can be round-tripped through
/// <see cref="NotableDateRuleJsonParser.ParseJson(string)" />.
/// </para>
/// </remarks>
/// <example>
/// <para>Defining two notable dates, persisting to XML or JSON, and passing them into the service:</para>
/// <code>
/// NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
///     .AddDate("Christmas Day", date => date
///         .AddRule(rule => rule
///             .Category(NotableDateCategory.Holiday)
///             .NonWorking()
///             .Fixed(12, 25)
///             .AddAdjustment("weekend-roll", adj => adj
///                 .When(AdjustmentTrigger.IfWeekend)
///                 .Action(AdjustmentAction.MoveToNextWeekday)
///                 .NonWorking())))
///     .AddDate("Easter Monday", date => date
///         .AddRule(rule => rule
///             .Category(NotableDateCategory.Holiday)
///             .NonWorking()
///             .OffsetFromAnchor("Easter Sunday", 1)));
///
/// // Persist as XML or JSON.
/// string xml = builder.ToXml();
/// string json = builder.ToJson();
///
/// // Or pass directly to the service.
/// NotableDateService service = new(new[] { builder.ToProvider() });
/// </code>
/// </example>
public sealed class NotableDateDocumentBuilder
{
    /// <summary>The XML namespace applied to the produced <c>NotableDates</c> document, matching <c>NotableDates.xsd</c>.</summary>
    private static readonly XNamespace s_schemaNamespace = XNamespace.Get("urn:bodu:globalization:calendar");

    /// <summary>The serializer options shared by <see cref="ToJson" /> for indented output.</summary>
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { WriteIndented = true };

    /// <summary>The notable date entries accumulated by <see cref="AddDate(string, System.Action{NotableDateBuilder})" />, each paired with its canonical name.</summary>
    private readonly List<(string Name, NotableDateBuilder Builder)> _dates = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateDocumentBuilder"/> class.
    /// </summary>
    private NotableDateDocumentBuilder() { }

    /// <summary>
    /// Creates a new, empty <see cref="NotableDateDocumentBuilder" />.
    /// </summary>
    /// <returns>A new builder instance.</returns>
    public static NotableDateDocumentBuilder Create() => new();

    /// <summary>
    /// Adds a named notable date entry to the document.
    /// </summary>
    /// <param name="name">The canonical name of the notable date (e.g. <c>"Christmas Day"</c>). Must not be <see langword="null" /> or whitespace.</param>
    /// <param name="configure">A callback that configures the <see cref="NotableDateBuilder" /> for this entry. Must not be <see langword="null" />.</param>
    /// <returns>This builder instance, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public NotableDateDocumentBuilder AddDate(string name, Action<NotableDateBuilder> configure)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(name);
        ThrowHelper.ThrowIfNull(configure);

        NotableDateBuilder dateBuilder = new();

        configure(dateBuilder);
        _dates.Add((name, dateBuilder));

        return this;
    }

    /// <summary>
    /// Builds all <see cref="NotableDateRule" /> instances from the current builder state.
    /// </summary>
    /// <returns>
    /// A read-only list of all rules, preserving the order in which notable dates and their rules were added.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any notable date entry contains no rules, or when a rule has no resolution strategy selected.
    /// </exception>
    public IReadOnlyList<NotableDateRule> Build()
    {
        List<NotableDateRule> rules = [];

        foreach ((var name, NotableDateBuilder builder) in _dates)
            rules.AddRange(builder.Build(name));

        return rules;
    }

    /// <summary>
    /// Serialises the builder state to an <see cref="XDocument" /> conforming to the <c>NotableDates.xsd</c> schema.
    /// </summary>
    /// <returns>
    /// An <see cref="XDocument" /> with a <c>&lt;NotableDates&gt;</c> root element in the
    /// <c>urn:bodu:globalization:calendar</c> namespace.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any notable date entry contains no rules, or when a rule has no resolution strategy selected.
    /// </exception>
    public XDocument ToXDocument() => BuildDocument();

    /// <summary>
    /// Serialises the builder state to a schema-valid XML string.
    /// </summary>
    /// <returns>
    /// An indented XML string whose root element is <c>&lt;NotableDates&gt;</c> in the
    /// <c>urn:bodu:globalization:calendar</c> namespace.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any notable date entry contains no rules, or when a rule has no resolution strategy selected.
    /// </exception>
    public string ToXml()
    {
        XDocument document = BuildDocument();
        StringBuilder sb = new();
        XmlWriterSettings settings = new()
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false,
        };

        using var writer = XmlWriter.Create(sb, settings);
        document.WriteTo(writer);
        writer.Flush();
        return sb.ToString();
    }

    /// <summary>
    /// Wraps the built rules in an <see cref="INotableDateRuleProvider" /> suitable for use with <see cref="NotableDateService" />.
    /// </summary>
    /// <returns>An <see cref="InlineNotableDateRuleProvider" /> backed by the built rules.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any notable date entry contains no rules, or when a rule has no resolution strategy selected.
    /// </exception>
    public INotableDateRuleProvider ToProvider() =>
        new InlineNotableDateRuleProvider(Build());

    /// <summary>
    /// Serialises the builder state to a <see cref="JsonObject" /> conforming to <c>NotableDates.schema.json</c>.
    /// </summary>
    /// <returns>
    /// A <see cref="JsonObject" /> with a <c>notableDates</c> array containing one entry per accumulated notable date,
    /// suitable for parsing via <see cref="NotableDateRuleJsonParser.ParseJson(string)" /> after stringification.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any notable date entry contains no rules, or when a rule has no resolution strategy selected.
    /// </exception>
    public JsonObject ToJsonNode() => BuildJsonDocument();

    /// <summary>
    /// Serialises the builder state to an indented schema-valid JSON string.
    /// </summary>
    /// <returns>
    /// An indented JSON string whose root object contains a <c>notableDates</c> array conforming to
    /// <c>NotableDates.schema.json</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any notable date entry contains no rules, or when a rule has no resolution strategy selected.
    /// </exception>
    public string ToJson() => BuildJsonDocument().ToJsonString(s_jsonSerializerOptions);

    /// <summary>
    /// Creates an independent copy of this document builder, deep-cloning every notable date entry so that
    /// subsequent mutations of either builder do not bleed across the boundary.
    /// </summary>
    /// <returns>A new <see cref="NotableDateDocumentBuilder" /> with the same notable date entries as this instance.</returns>
    /// <remarks>
    /// <para>
    /// Intended for the template-factory pattern: configure a baseline document once, clone it for each variant,
    /// and tweak only the entries that differ between variants.
    /// </para>
    /// </remarks>
    public NotableDateDocumentBuilder Clone()
    {
        NotableDateDocumentBuilder copy = new();
        foreach ((var name, NotableDateBuilder builder) in _dates)
            copy._dates.Add((name, builder.Clone()));
        return copy;
    }

    /// <summary>
    /// Builds the underlying <see cref="XDocument" /> shared by <see cref="ToXDocument" /> and <see cref="ToXml" />.
    /// </summary>
    /// <returns>
    /// An <see cref="XDocument" /> whose root is a <c>&lt;NotableDates&gt;</c> element in the
    /// <c>urn:bodu:globalization:calendar</c> namespace, containing one child per accumulated notable date entry in the order they were added.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any notable date entry contains no rules, or when a rule has no resolution strategy selected.
    /// </exception>
    private XDocument BuildDocument()
    {
        XElement root = new(s_schemaNamespace + "NotableDates");

        foreach ((var name, NotableDateBuilder builder) in _dates)
            root.Add(builder.ToXElement(name, s_schemaNamespace));

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
    }

    /// <summary>
    /// Builds the underlying <see cref="JsonObject" /> shared by <see cref="ToJsonNode" /> and <see cref="ToJson" />.
    /// </summary>
    /// <returns>
    /// A <see cref="JsonObject" /> whose <c>notableDates</c> property is a <see cref="JsonArray" /> of one child per
    /// accumulated notable date entry, in the order they were added.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any notable date entry contains no rules, or when a rule has no resolution strategy selected.
    /// </exception>
    private JsonObject BuildJsonDocument()
    {
        JsonArray notableDates = [];

        foreach ((var name, NotableDateBuilder builder) in _dates)
            notableDates.Add(builder.ToJsonNode(name));

        return new JsonObject
        {
            ["notableDates"] = notableDates,
        };
    }
}
