// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocumentNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Adapts an <see cref="XElement" /> to <see cref="IDocumentNode" />, binding scalars to attributes and lists to child
/// elements within the notable-date document namespace.
/// </summary>
internal sealed class XmlDocumentNode
    : IDocumentNode
{
    /// <summary>The document namespace every element is qualified with.</summary>
    private readonly XNamespace _ns;

    /// <summary>The wrapped element.</summary>
    private readonly XElement _element;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocumentNode" /> class.
    /// </summary>
    /// <param name="element">The wrapped element.</param>
    /// <param name="ns">The document namespace every element is qualified with.</param>
    public XmlDocumentNode(XElement element, XNamespace ns)
    {
        _element = element;
        _ns = ns;
    }

    /// <inheritdoc />
    public string? Scalar(string name) =>
        (string?)_element.Attribute(name);

    /// <inheritdoc />
    public IDocumentNode? Child(string name)
    {
        XElement? child = _element.Elements()
            .FirstOrDefault(e => string.Equals(e.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));

        return child is null ? null : new XmlDocumentNode(child, _ns);
    }

    /// <inheritdoc />
    public string Discriminator(string jsonProperty) =>
        _element.Name.LocalName;

    /// <inheritdoc />
    public IEnumerable<IDocumentNode> List(string? xmlWrapper, string? xmlItem, string jsonProperty)
    {
        XElement? container = xmlWrapper is null ? _element : _element.Element(_ns + xmlWrapper);
        if (container is null)
            yield break;

        IEnumerable<XElement> items = xmlItem is null ? container.Elements() : container.Elements(_ns + xmlItem);
        foreach (XElement item in items)
            yield return new XmlDocumentNode(item, _ns);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ScalarList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics) =>
        ReadScalarList(xmlWrapper, xmlItem, xmlAttribute) ?? [];

    /// <inheritdoc />
    public IReadOnlyList<string>? OptionalScalarList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics) =>
        ReadScalarList(xmlWrapper, xmlItem, xmlAttribute);

    /// <inheritdoc />
    public IReadOnlyList<string> TextList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics) =>
        ReadScalarList(xmlWrapper, xmlItem, xmlAttribute) ?? [];

    /// <inheritdoc />
    public IReadOnlyList<string>? OptionalTextList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics) =>
        ReadScalarList(xmlWrapper, xmlItem, xmlAttribute);

    /// <inheritdoc />
    public IReadOnlyList<int> IntList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        XElement? container = xmlWrapper is null ? _element : _element.Element(_ns + xmlWrapper);
        if (container is null)
            return [];

        List<int> values = new();
        foreach (XElement item in container.Elements(_ns + xmlItem))
        {
            if (NotableDateDocumentParseHelpers.ParseNullableInt((string?)item.Attribute(xmlAttribute), field, diagnostics) is int result)
                values.Add(result);
        }

        return values;
    }

    /// <inheritdoc />
    public IReadOnlyList<(string? Qualifier, string Name)> QualifiedReferenceList(string? xmlWrapper, string xmlItem, string qualifierName, string name, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        XElement? container = xmlWrapper is null ? _element : _element.Element(_ns + xmlWrapper);
        if (container is null)
            return [];

        List<(string? Qualifier, string Name)> references = new();
        foreach (XElement item in container.Elements(_ns + xmlItem))
            references.Add(((string?)item.Attribute(qualifierName), (string?)item.Attribute(name) ?? string.Empty));

        return references;
    }

    /// <inheritdoc />
    public void CheckPattern(string? value, NotableDateSchemaPattern pattern, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        // The schema validation that precedes reading has already reported a value that breaks its pattern.
    }

    /// <inheritdoc />
    public void CheckRequired(string schemaType, string property, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        // The schema validation that precedes reading has already reported a missing required attribute or element.
    }

    /// <inheritdoc />
    public IReadOnlyList<(string Key, string Value)> KeyValueList(string xmlWrapper, string xmlItem, string xmlKeyAttribute, string xmlValueAttribute, string jsonProperty)
    {
        XElement? container = _element.Element(_ns + xmlWrapper);
        if (container is null)
            return [];

        List<(string Key, string Value)> pairs = new();
        foreach (XElement item in container.Elements(_ns + xmlItem))
            pairs.Add(((string?)item.Attribute(xmlKeyAttribute) ?? string.Empty, (string?)item.Attribute(xmlValueAttribute) ?? string.Empty));

        return pairs;
    }

    /// <summary>
    /// Reads the values of an attribute across the item elements, returning <see langword="null" /> when the wrapper
    /// element is absent.
    /// </summary>
    /// <param name="xmlWrapper">
    /// The XML wrapper element, or <see langword="null" /> to read this node's children.
    /// </param>
    /// <param name="xmlItem">The XML item element local name.</param>
    /// <param name="xmlAttribute">The XML attribute carrying the value.</param>
    /// <returns>The values, or <see langword="null" /> when the wrapper is absent.</returns>
    private List<string>? ReadScalarList(string? xmlWrapper, string xmlItem, string xmlAttribute)
    {
        XElement? container = xmlWrapper is null ? _element : _element.Element(_ns + xmlWrapper);
        if (container is null)
            return null;

        List<string> values = new();
        foreach (XElement item in container.Elements(_ns + xmlItem))
            values.Add((string?)item.Attribute(xmlAttribute) ?? string.Empty);

        return values;
    }
}
