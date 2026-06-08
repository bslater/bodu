// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IDocumentNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Abstracts a node of a parsed notable-date document so that a single reader can shape both the XML and the JSON
/// projections of the schema. Each accessor takes the format-specific binding names it needs (the XML wrapper element,
/// item element, and attribute, plus the JSON property), keeping the XML-attribute versus JSON-array vocabulary
/// divergence in one place rather than duplicated across two parsers.
/// </summary>
internal interface IDocumentNode
{
    /// <summary>
    /// Reads a scalar field as a string: an XML attribute, or a JSON property coerced to its string form (a number to
    /// its invariant digits, a boolean to <c>true</c>/<c>false</c>, a string verbatim).
    /// </summary>
    /// <param name="name">The attribute or property name.</param>
    /// <returns>The scalar value, or <see langword="null" /> when absent or not a scalar.</returns>
    string? Scalar(string name);

    /// <summary>
    /// Returns a single child node: an XML child element matched by local name, or a JSON object property, both
    /// case-insensitively.
    /// </summary>
    /// <param name="name">The child element local name or JSON property name.</param>
    /// <returns>The child node, or <see langword="null" /> when absent.</returns>
    IDocumentNode? Child(string name);

    /// <summary>
    /// Returns the discriminator that selects a heterogeneous item's kind: an XML element's local name, or the value of
    /// the supplied JSON discriminator property.
    /// </summary>
    /// <param name="jsonProperty">The JSON property carrying the discriminator value.</param>
    /// <returns>The discriminator, or the empty string when absent.</returns>
    string Discriminator(string jsonProperty);

    /// <summary>
    /// Enumerates the object items of a list: the XML item elements within an optional wrapper element (every element
    /// child when <paramref name="xmlItem" /> is <see langword="null" />), or the JSON array's elements.
    /// </summary>
    /// <param name="xmlWrapper">
    /// The XML wrapper element, or <see langword="null" /> to read this node's children.
    /// </param>
    /// <param name="xmlItem">
    /// The XML item element local name, or <see langword="null" /> for every element child.
    /// </param>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <returns>The item nodes; empty when absent.</returns>
    IEnumerable<IDocumentNode> List(string? xmlWrapper, string? xmlItem, string jsonProperty);

    /// <summary>
    /// Reads a list of scalar strings, returning an empty list when absent.
    /// </summary>
    /// <param name="xmlWrapper">
    /// The XML wrapper element, or <see langword="null" /> to read this node's children.
    /// </param>
    /// <param name="xmlItem">The XML item element local name.</param>
    /// <param name="xmlAttribute">The XML attribute on each item carrying the value.</param>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <returns>The scalar values; empty when absent.</returns>
    IReadOnlyList<string> ScalarList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty);

    /// <summary>
    /// Reads an optional list of scalar strings, returning <see langword="null" /> when the wrapper element or JSON
    /// property is absent so callers can distinguish "unspecified" from "empty".
    /// </summary>
    /// <param name="xmlWrapper">
    /// The XML wrapper element, or <see langword="null" /> to read this node's children.
    /// </param>
    /// <param name="xmlItem">The XML item element local name.</param>
    /// <param name="xmlAttribute">The XML attribute on each item carrying the value.</param>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <returns>The scalar values, or <see langword="null" /> when absent.</returns>
    IReadOnlyList<string>? OptionalScalarList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty);

    /// <summary>
    /// Reads a list of integer scalars, returning an empty list when absent.
    /// </summary>
    /// <param name="xmlWrapper">
    /// The XML wrapper element, or <see langword="null" /> to read this node's children.
    /// </param>
    /// <param name="xmlItem">The XML item element local name.</param>
    /// <param name="xmlAttribute">The XML attribute on each item carrying the value.</param>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <returns>The integer values; empty when absent.</returns>
    IReadOnlyList<int> IntList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty);

    /// <summary>
    /// Reads a key/value map: XML item elements carrying a key attribute and a value attribute within a wrapper, or a
    /// JSON object's properties.
    /// </summary>
    /// <param name="xmlWrapper">The XML wrapper element.</param>
    /// <param name="xmlItem">The XML item element local name.</param>
    /// <param name="xmlKeyAttribute">The XML attribute carrying each entry's key.</param>
    /// <param name="xmlValueAttribute">The XML attribute carrying each entry's value.</param>
    /// <param name="jsonProperty">The JSON object property name.</param>
    /// <returns>The key/value pairs; empty when absent.</returns>
    IReadOnlyList<(string Key, string Value)> KeyValueList(string xmlWrapper, string xmlItem, string xmlKeyAttribute, string xmlValueAttribute, string jsonProperty);
}
