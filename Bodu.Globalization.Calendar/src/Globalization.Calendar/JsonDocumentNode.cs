// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonDocumentNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Adapts a <see cref="JsonElement" /> to <see cref="IDocumentNode" />, binding scalars to object properties (coercing
/// numbers and booleans to their string form, and reporting an object or an array), child nodes to objects and lists to
/// JSON arrays (reporting a value of another kind).
/// </summary>
/// <remarks>
/// A value or an item of a kind its field does not read is reported by the name of its JSON kind rather than its text,
/// which the document builder, holding parsed nodes, could not reproduce byte for byte.
/// </remarks>
internal sealed class JsonDocumentNode
    : IDocumentNode
{
    /// <summary>The wrapped element.</summary>
    private readonly JsonElement _element;

    /// <summary>The collection that receives the diagnostic for a property holding a value of the wrong kind.</summary>
    private readonly ICollection<NotableDateValidationDiagnostic> _diagnostics;

    /// <summary>The names of the properties already reported as holding a value of the wrong kind, so a property read twice is reported once; <see langword="null" /> until the first report.</summary>
    private HashSet<string>? _reportedProperties;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDocumentNode" /> class.
    /// </summary>
    /// <param name="element">The wrapped element.</param>
    /// <param name="diagnostics">
    /// The collection that receives the diagnostic for a property holding a value of the wrong kind, shared with every
    /// node read from this one.
    /// </param>
    public JsonDocumentNode(JsonElement element, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        _element = element;
        _diagnostics = diagnostics;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A property holding an object or an array, which no scalar field accepts, records a <c>BODU-CAL-VALUE</c> error
    /// naming the property and the kind of value it holds, and reads as absent; the JSON literal <c>null</c> reads as
    /// absent without a diagnostic.
    /// </remarks>
    public string? Scalar(string name)
    {
        if (!TryGetProperty(name, out JsonElement value))
            return null;

        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                return value.GetString();

            case JsonValueKind.Number:
                return value.TryGetInt32(out int number) ? number.ToString(CultureInfo.InvariantCulture) : value.GetRawText();

            case JsonValueKind.True:
                return "true";

            case JsonValueKind.False:
                return "false";

            default:
                ReportValue(name, CalendarResourceStrings.Validation_InvalidScalarValue, KindName(value.ValueKind));
                return null;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// A property holding a value that is not an object records a <c>BODU-CAL-VALUE</c> error naming the property and
    /// the kind of value it holds, and reads as absent; the JSON literal <c>null</c> reads as absent without a
    /// diagnostic.
    /// </remarks>
    public IDocumentNode? Child(string name) =>
        TryGetProperty(name, JsonValueKind.Object, out JsonElement value)
            ? new JsonDocumentNode(value, _diagnostics)
            : null;

    /// <inheritdoc />
    public string Discriminator(string jsonProperty) =>
        Scalar(jsonProperty) ?? string.Empty;

    /// <inheritdoc />
    /// <remarks>
    /// A property holding a value that is not an array records a <c>BODU-CAL-VALUE</c> error naming the property and
    /// the kind of value it holds, and reads as an empty list, as does the JSON literal <c>null</c>, without a
    /// diagnostic. An item that is not an object is left out, and the first such item records a <c>BODU-CAL-VALUE</c>
    /// error naming the property and the item's kind.
    /// </remarks>
    public IEnumerable<IDocumentNode> List(string? xmlWrapper, string? xmlItem, string jsonProperty)
    {
        if (!TryGetProperty(jsonProperty, JsonValueKind.Array, out JsonElement array))
            yield break;

        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
                yield return new JsonDocumentNode(item, _diagnostics);
            else
                ReportValue(jsonProperty, CalendarResourceStrings.Validation_InvalidObjectItem, KindName(item.ValueKind));
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ScalarList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics) =>
        ReadItemTexts(jsonProperty, field, diagnostics) ?? [];

    /// <inheritdoc />
    public IReadOnlyList<string>? OptionalScalarList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics) =>
        ReadItemTexts(jsonProperty, field, diagnostics);

    /// <inheritdoc />
    public IReadOnlyList<string> TextList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics) =>
        ReadStrings(jsonProperty, field, diagnostics) ?? [];

    /// <inheritdoc />
    public IReadOnlyList<string>? OptionalTextList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics) =>
        ReadStrings(jsonProperty, field, diagnostics);

    /// <inheritdoc />
    public IReadOnlyList<int> IntList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (!TryGetProperty(jsonProperty, JsonValueKind.Array, out JsonElement array))
            return [];

        List<int> values = new();
        foreach (JsonElement item in array.EnumerateArray())
        {
            // An item is read as a scalar property is, so a number in quotes is accepted and any other scalar, a fraction
            // or a Boolean among them, is reported by its JSON text rather than dropped.
            if (ScalarItemText(item, field, diagnostics) is string text && NotableDateDocumentParseHelpers.ParseNullableInt(text, field, diagnostics) is int result)
                values.Add(result);
        }

        return values;
    }

    /// <inheritdoc />
    public IReadOnlyList<(string? Qualifier, string Name)> QualifiedReferenceList(string? xmlWrapper, string xmlItem, string qualifierName, string name, string jsonProperty, NotableDateSchemaType itemType, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (!TryGetProperty(jsonProperty, JsonValueKind.Array, out JsonElement array))
            return [];

        List<(string? Qualifier, string Name)> references = new();
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                references.Add((null, item.GetString() ?? string.Empty));
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                JsonDocumentNode reference = new(item, _diagnostics);
                reference.CheckRequired(itemType, diagnostics);

                // An object without its name has just been reported as missing a required property; it is left out
                // rather than read with an empty name that the reader would then report again against its pattern.
                if (reference.Scalar(name) is string referenceName)
                    references.Add((reference.Scalar(qualifierName), referenceName));
            }
            else
            {
                NotableDateDocumentParseHelpers.AddValueError(diagnostics, CalendarResourceStrings.Validation_InvalidReferenceItem, field, KindName(item.ValueKind));
            }
        }

        return references;
    }

    /// <inheritdoc />
    public IReadOnlyList<(string Key, string Value)> KeyValueList(string xmlWrapper, string xmlItem, string xmlKeyAttribute, string xmlValueAttribute, string jsonProperty)
    {
        if (!TryGetProperty(jsonProperty, JsonValueKind.Object, out JsonElement map))
            return [];

        List<(string Key, string Value)> pairs = new();
        foreach (JsonProperty property in map.EnumerateObject())
        {
            string value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : property.Value.ToString();
            pairs.Add((property.Name, value));
        }

        return pairs;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A value that does not match the pattern records a <c>BODU-CAL-SCHEMA</c> error naming the property, the value
    /// and the pattern, as the schema reports the same value in an XML document.
    /// </remarks>
    public void CheckPattern(string? value, NotableDateSchemaPattern pattern, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (value is not null && NotableDateSchemaPatterns.Check(value, pattern, field) is NotableDateValidationDiagnostic diagnostic)
            diagnostics.Add(diagnostic);
    }

    /// <inheritdoc />
    /// <remarks>
    /// An absent property records a <c>BODU-CAL-SCHEMA</c> error naming the schema type and the property, and an empty
    /// required list one naming the list, as the schema reports the missing attribute or element in an XML document.
    /// </remarks>
    public void CheckRequired(NotableDateSchemaType type, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        foreach (string property in NotableDateSchemaRequirements.GetRequiredProperties(type))
        {
            if (!TryGetProperty(property, out JsonElement value))
                diagnostics.Add(NotableDateSchemaRequirements.MissingProperty(type, property));
            else if (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0 && NotableDateSchemaRequirements.RequiresItems(type, property))
                diagnostics.Add(NotableDateSchemaRequirements.EmptyList(type, property));
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// A value outside the range records a <c>BODU-CAL-SCHEMA</c> error naming the property, the value and the bound it
    /// passes, as the schema reports the same value in an XML document.
    /// </remarks>
    public void CheckRange(int? value, NotableDateSchemaRange range, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (value is int number && NotableDateSchemaRanges.Check(number, range, field) is NotableDateValidationDiagnostic diagnostic)
            diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Reads the items of an array property as text, returning <see langword="null" /> when the property is absent: a
    /// string by its value, and any other scalar by its JSON text, so a name parse reports it rather than the list
    /// losing it; an object or an array is left out after a <c>BODU-CAL-VALUE</c> error is recorded for it.
    /// </summary>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <param name="field">The name of the field the array belongs to, used in the diagnostics.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for each object or array item.</param>
    /// <returns>The item texts, or <see langword="null" /> when absent.</returns>
    private List<string>? ReadItemTexts(string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (!TryGetProperty(jsonProperty, JsonValueKind.Array, out JsonElement array))
            return null;

        List<string> values = new();
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (ScalarItemText(item, field, diagnostics) is string text)
                values.Add(text);
        }

        return values;
    }

    /// <summary>
    /// Returns the text of an item of a list of scalars, as a scalar property is read: a string by its value, and a
    /// number, a Boolean or <c>null</c> by its JSON text; an object or an array, which no scalar is, records a
    /// <c>BODU-CAL-VALUE</c> error naming the list and the item's kind.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="field">The name of the field the list belongs to, used in the diagnostic.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic for an object or an array.</param>
    /// <returns>The item's text, or <see langword="null" /> when it is an object or an array.</returns>
    private static string? ScalarItemText(JsonElement item, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        switch (item.ValueKind)
        {
            case JsonValueKind.String:
                return item.GetString() ?? string.Empty;

            case JsonValueKind.Object or JsonValueKind.Array:
                NotableDateDocumentParseHelpers.AddValueError(diagnostics, CalendarResourceStrings.Validation_InvalidScalarItem, field, KindName(item.ValueKind));
                return null;

            default:
                return item.GetRawText();
        }
    }

    /// <summary>
    /// Reads the string items of an array property, returning <see langword="null" /> when the property is absent and
    /// recording a <c>BODU-CAL-VALUE</c> error for each item that is not a string.
    /// </summary>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <param name="field">The name of the field the array belongs to, used in the diagnostics.</param>
    /// <param name="diagnostics">
    /// The collection that receives the diagnostic for each item that is not a string.
    /// </param>
    /// <returns>The string values, or <see langword="null" /> when absent.</returns>
    private List<string>? ReadStrings(string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (!TryGetProperty(jsonProperty, JsonValueKind.Array, out JsonElement array))
            return null;

        List<string> values = new();
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
                values.Add(item.GetString() ?? string.Empty);
            else
                NotableDateDocumentParseHelpers.AddValueError(diagnostics, CalendarResourceStrings.Validation_InvalidTextItem, field, KindName(item.ValueKind));
        }

        return values;
    }

    /// <summary>
    /// Returns the name of the kind of a JSON value, as the diagnostic for a property or a list item holding a value of
    /// the wrong kind names it: <c>object</c>, <c>array</c>, <c>string</c>, <c>number</c>, <c>Boolean</c> or
    /// <c>null</c>.
    /// </summary>
    /// <param name="kind">The kind of the value.</param>
    /// <returns>The kind's name.</returns>
    internal static string KindName(JsonValueKind kind) =>
        kind switch
        {
            JsonValueKind.Object => "object",
            JsonValueKind.Array => "array",
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "Boolean",
            _ => "null",
        };

    /// <summary>
    /// Looks up an object property that must hold a value of the given kind, as
    /// <see cref="TryGetProperty(string, out JsonElement)" /> does, recording a <c>BODU-CAL-VALUE</c> error for a
    /// property that holds a value of another kind.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="kind">
    /// The kind the value must be: <see cref="JsonValueKind.Object" /> or <see cref="JsonValueKind.Array" />.
    /// </param>
    /// <param name="value">
    /// When this method returns, the property value, or the default when absent or of another kind.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a non-null property of the kind was found; otherwise <see langword="false" />.
    /// </returns>
    private bool TryGetProperty(string name, JsonValueKind kind, out JsonElement value)
    {
        if (!TryGetProperty(name, out value))
            return false;

        if (value.ValueKind == kind)
            return true;

        string format = kind == JsonValueKind.Object
            ? CalendarResourceStrings.Validation_InvalidObjectValue
            : CalendarResourceStrings.Validation_InvalidListValue;
        ReportValue(name, format, KindName(value.ValueKind));

        value = default;
        return false;
    }

    /// <summary>
    /// Records the <c>BODU-CAL-VALUE</c> error for a property holding a value of the wrong kind, once per property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="format">
    /// The resource format string, taking the property name and a description of the value.
    /// </param>
    /// <param name="value">The description of the value.</param>
    private void ReportValue(string name, string format, string value)
    {
        if ((_reportedProperties ??= new(StringComparer.OrdinalIgnoreCase)).Add(name))
            NotableDateDocumentParseHelpers.AddValueError(_diagnostics, format, name, value);
    }

    /// <summary>
    /// Looks up an object property case-insensitively, treating an explicit JSON null as absent.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">When this method returns, the property value, or the default when absent.</param>
    /// <returns>
    /// <see langword="true" /> when a non-null property was found; otherwise <see langword="false" />.
    /// </returns>
    private bool TryGetProperty(string name, out JsonElement value)
    {
        if (_element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in _element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind != JsonValueKind.Null)
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
