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
    /// naming the property and reads as absent; the JSON literal <c>null</c> reads as absent without a diagnostic.
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
                ReportValue(name, CalendarResourceStrings.Validation_InvalidScalarValue, value.GetRawText());
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
    /// diagnostic.
    /// </remarks>
    public IEnumerable<IDocumentNode> List(string? xmlWrapper, string? xmlItem, string jsonProperty)
    {
        if (!TryGetProperty(jsonProperty, JsonValueKind.Array, out JsonElement array))
            yield break;

        foreach (JsonElement item in array.EnumerateArray())
            yield return new JsonDocumentNode(item, _diagnostics);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ScalarList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty) =>
        ReadItemTexts(jsonProperty) ?? [];

    /// <inheritdoc />
    public IReadOnlyList<string>? OptionalScalarList(string? xmlWrapper, string xmlItem, string xmlAttribute, string jsonProperty) =>
        ReadItemTexts(jsonProperty);

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
            // An item is read as a scalar property is, so a number in quotes is accepted and any other item, a fraction
            // or a Boolean among them, is reported by its JSON text rather than dropped.
            string text = item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.GetRawText();
            if (NotableDateDocumentParseHelpers.ParseNullableInt(text, field, diagnostics) is int result)
                values.Add(result);
        }

        return values;
    }

    /// <inheritdoc />
    public IReadOnlyList<(string? Qualifier, string Name)> QualifiedReferenceList(string? xmlWrapper, string xmlItem, string qualifierName, string name, string jsonProperty, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
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
                references.Add((reference.Scalar(qualifierName), reference.Scalar(name) ?? string.Empty));
            }
            else
            {
                NotableDateDocumentParseHelpers.AddValueError(diagnostics, CalendarResourceStrings.Validation_InvalidReferenceItem, field, item.GetRawText());
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

    /// <summary>
    /// Reads the items of an array property as text, returning <see langword="null" /> when the property is absent: a
    /// string by its value, and any other item by its JSON text, so a name parse reports it rather than the list losing
    /// it.
    /// </summary>
    /// <param name="jsonProperty">The JSON array property name.</param>
    /// <returns>The item texts, or <see langword="null" /> when absent.</returns>
    private List<string>? ReadItemTexts(string jsonProperty)
    {
        if (!TryGetProperty(jsonProperty, JsonValueKind.Array, out JsonElement array))
            return null;

        List<string> values = new();
        foreach (JsonElement item in array.EnumerateArray())
            values.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.GetRawText());

        return values;
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
                NotableDateDocumentParseHelpers.AddValueError(diagnostics, CalendarResourceStrings.Validation_InvalidTextItem, field, item.GetRawText());
        }

        return values;
    }

    /// <summary>
    /// Returns the name of the kind of a JSON value, as the diagnostic for a property holding a value of the wrong kind
    /// names it: <c>object</c>, <c>array</c>, <c>string</c>, <c>number</c> or <c>Boolean</c>.
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
