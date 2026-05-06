// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilder.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

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
/// <item><description><see cref="ToProvider" /> — wraps the built rules in an <see cref="INotableDateRuleProvider" /> ready for use with <see cref="NotableDateService" />.</description></item>
/// </list>
/// <para>
/// The XML produced by <see cref="ToXDocument" /> and <see cref="ToXml" /> conforms to the <c>urn:bodu:globalization:calendar</c>
/// namespace and can be round-tripped through <see cref="NotableDateRuleParser.ParseXml(string)" />.
/// </para>
/// </remarks>
/// <example>
/// <para>Defining two notable dates, serialising to XML, and passing them into the service:</para>
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
/// // Persist as XML.
/// string xml = builder.ToXml();
///
/// // Or pass directly to the service.
/// NotableDateService service = new(new[] { builder.ToProvider() });
/// </code>
/// </example>
public sealed class NotableDateDocumentBuilder
{
    private static readonly XNamespace SchemaNamespace = XNamespace.Get("urn:bodu:globalization:calendar");

    private readonly List<(string Name, NotableDateBuilder Builder)> _dates = [];

    /// <summary>
    /// Initialises a new, empty <see cref="NotableDateDocumentBuilder" />.
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

        foreach ((string name, NotableDateBuilder builder) in _dates)
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

        using XmlWriter writer = XmlWriter.Create(sb, settings);
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

    private XDocument BuildDocument()
    {
        XElement root = new(SchemaNamespace + "NotableDates");

        foreach ((string name, NotableDateBuilder builder) in _dates)
            root.Add(builder.ToXElement(name, SchemaNamespace));

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
    }
}
