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
/// <remarks>
/// A JSON property that a child accessor reads as an object, or a list accessor as an array, holding a value of another
/// kind reads as absent, and its node records a <c>BODU-CAL-VALUE</c> error naming it; the JSON literal <c>null</c>
/// reads as absent without a diagnostic.
/// </remarks>
internal interface IDocumentNode
{
    /// <summary>
    /// Reads a scalar field as a string: an XML attribute, or a JSON property coerced to its string form (a number to
    /// its invariant digits, a boolean to <c>true</c>/<c>false</c>, a string verbatim).
    /// </summary>
    /// <param name="name">The attribute or property name.</param>
    /// <returns>
    /// The scalar value, or <see langword="null" /> when absent or not a scalar. A JSON property holding an object or
    /// an array is not a scalar, and its node has recorded a <c>BODU-CAL-VALUE</c> error naming it.
    /// </returns>
    string? Scalar(string name);

    /// <summary>
    /// Returns a single child node: an XML child element matched by local name, or a JSON object property, both
    /// case-insensitively.
    /// </summary>
    /// <param name="name">The child element local name or JSON property name.</param>
    /// <returns>The child node, or <see langword="null" /> when absent or, in JSON, not an object.</returns>
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
    /// <returns>The item nodes; empty when absent or, in JSON, not an array.</returns>
    IEnumerable<IDocumentNode> List(string? xmlWrapper, string? xmlItem, string jsonProperty);

    /// <summary>
    /// Reads a list of enumeration names, returning an empty list when absent.
    /// </summary>
    /// <param name="xmlWrapper">
    /// The XML wrapper element, or <see langword="null" /> to read this node's children.
    /// </param>
    /// <param name="xmlItem">The XML item element local name.</param>
    /// <param name="xmlAttribute">The XML attribute on each item carrying the value.</param>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <param name="field">The name of the field the list belongs to, used in the diagnostics.</param>
    /// <param name="diagnostics">
    /// The collection that receives the diagnostic for each JSON item that is an object or an array.
    /// </param>
    /// <returns>The scalar values; empty when absent.</returns>
    /// <remarks>
    /// A JSON item that is a number, a Boolean or <c>null</c> is returned as its JSON text, so the name parse the
    /// caller applies reports it rather than the list silently losing it. An item that is an object or an array is left
    /// out after a <c>BODU-CAL-VALUE</c> error naming its JSON kind is recorded for it.
    /// </remarks>
    IReadOnlyList<string> ScalarList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics);

    /// <summary>
    /// Reads an optional list of enumeration names, returning <see langword="null" /> when the wrapper element or JSON
    /// property is absent so callers can distinguish "unspecified" from "empty".
    /// </summary>
    /// <param name="xmlWrapper">
    /// The XML wrapper element, or <see langword="null" /> to read this node's children.
    /// </param>
    /// <param name="xmlItem">The XML item element local name.</param>
    /// <param name="xmlAttribute">The XML attribute on each item carrying the value.</param>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <param name="field">The name of the field the list belongs to, used in the diagnostics.</param>
    /// <param name="diagnostics">
    /// The collection that receives the diagnostic for each JSON item that is an object or an array.
    /// </param>
    /// <returns>The scalar values, or <see langword="null" /> when absent.</returns>
    /// <remarks>
    /// A JSON item that is not a string is returned as its JSON text, or left out after a <c>BODU-CAL-VALUE</c> error
    /// is recorded for it, as <see cref="ScalarList" /> treats it.
    /// </remarks>
    IReadOnlyList<string>? OptionalScalarList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics);

    /// <summary>
    /// Reads a list of text values (territory codes, references, or tags), returning an empty list when absent and
    /// recording an error diagnostic for each JSON item that is not a string.
    /// </summary>
    /// <param name="xmlWrapper">
    /// The XML wrapper element, or <see langword="null" /> to read this node's children.
    /// </param>
    /// <param name="xmlItem">The XML item element local name.</param>
    /// <param name="xmlAttribute">The XML attribute on each item carrying the value.</param>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <param name="field">The name of the field the list belongs to, used in the diagnostics.</param>
    /// <param name="diagnostics">
    /// The collection that receives the diagnostic for each item that is not a string.
    /// </param>
    /// <returns>The text values; empty when absent.</returns>
    /// <remarks>
    /// An item that is not a string is left out of the list after a <c>BODU-CAL-VALUE</c> error naming its JSON kind is
    /// recorded for it, so the shortened list never reaches a loaded resource.
    /// </remarks>
    IReadOnlyList<string> TextList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics);

    /// <summary>
    /// Reads an optional list of text values, returning <see langword="null" /> when the wrapper element or JSON
    /// property is absent so callers can distinguish "unspecified" from "empty", and recording an error diagnostic for
    /// each JSON item that is not a string.
    /// </summary>
    /// <param name="xmlWrapper">
    /// The XML wrapper element, or <see langword="null" /> to read this node's children.
    /// </param>
    /// <param name="xmlItem">The XML item element local name.</param>
    /// <param name="xmlAttribute">The XML attribute on each item carrying the value.</param>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <param name="field">The name of the field the list belongs to, used in the diagnostics.</param>
    /// <param name="diagnostics">
    /// The collection that receives the diagnostic for each item that is not a string.
    /// </param>
    /// <returns>The text values, or <see langword="null" /> when absent.</returns>
    /// <remarks>
    /// An item that is not a string is left out after a <c>BODU-CAL-VALUE</c> error is recorded for it, as
    /// <see cref="TextList" /> leaves it out.
    /// </remarks>
    IReadOnlyList<string>? OptionalTextList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics);

    /// <summary>
    /// Reads a list of integer scalars, returning an empty list when absent and recording an error diagnostic for each
    /// item that is not a 32-bit integer.
    /// </summary>
    /// <param name="xmlWrapper">
    /// The XML wrapper element, or <see langword="null" /> to read this node's children.
    /// </param>
    /// <param name="xmlItem">The XML item element local name.</param>
    /// <param name="xmlAttribute">The XML attribute on each item carrying the value.</param>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <param name="field">The name of the field the list belongs to, used in the diagnostics.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for each malformed item.</param>
    /// <returns>The integer values; empty when absent.</returns>
    /// <remarks>
    /// An item that is not an integer is left out of the list after a <c>BODU-CAL-VALUE</c> error is recorded for it,
    /// so the shortened list never reaches a loaded resource. A JSON item is read as a scalar is: a number by its
    /// digits, a string by its text, and any other scalar by its JSON text; an object or an array is reported by its
    /// JSON kind.
    /// </remarks>
    IReadOnlyList<int> IntList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics);

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

    /// <summary>
    /// Checks a text value read from this node against a pattern the XML schema enforces, recording an error diagnostic
    /// when a JSON value does not match it; an XML value is left to the schema, which has checked it.
    /// </summary>
    /// <param name="value">The value, or <see langword="null" /> when absent.</param>
    /// <param name="pattern">The pattern the schema enforces on the value.</param>
    /// <param name="field">The name of the JSON property the value was read from, used in the diagnostic.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic.</param>
    void CheckPattern(string? value, NotableDateSchemaPattern pattern, string field, ICollection<NotableDateValidationDiagnostic> diagnostics);

    /// <summary>
    /// Checks that this node holds a property the XML schema requires of it, recording an error diagnostic when a JSON
    /// node lacks it; an XML node is left to the schema, which has checked it.
    /// </summary>
    /// <param name="schemaType">The name of the XSD complex type this node stands for, used in the diagnostic.</param>
    /// <param name="property">The name of the JSON property the schema requires.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic.</param>
    /// <remarks>
    /// The JSON literal <c>null</c> counts as absent. A property that holds a value of the wrong kind is present, and
    /// its node has reported it with <c>BODU-CAL-VALUE</c> when it was read.
    /// </remarks>
    void CheckRequired(string schemaType, string property, ICollection<NotableDateValidationDiagnostic> diagnostics);

    /// <summary>
    /// Reads a list of references that may be qualified: XML item elements carrying a qualifier attribute and a name
    /// attribute, or a JSON array whose items are objects carrying the same two properties, or strings carrying the
    /// name alone.
    /// </summary>
    /// <param name="xmlWrapper">
    /// The XML wrapper element, or <see langword="null" /> to read this node's children.
    /// </param>
    /// <param name="xmlItem">The XML item element local name.</param>
    /// <param name="qualifierName">The XML attribute and JSON property carrying each reference's qualifier.</param>
    /// <param name="name">The XML attribute and JSON property carrying each reference's name.</param>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <param name="field">The name of the field the list belongs to, used in the diagnostics.</param>
    /// <param name="diagnostics">
    /// The collection that receives the diagnostic for each JSON item that is neither a string nor an object.
    /// </param>
    /// <returns>
    /// The references, each with a <see langword="null" /> qualifier when it carries none; empty when absent.
    /// </returns>
    /// <remarks>
    /// A JSON item that is neither a string nor an object is left out after a <c>BODU-CAL-VALUE</c> error naming its
    /// JSON kind is recorded for it, so the shortened list never reaches a loaded resource.
    /// </remarks>
    IReadOnlyList<(string? Qualifier, string Name)> QualifiedReferenceList(string? xmlWrapper, string xmlItem, string qualifierName, string name, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics);
}
