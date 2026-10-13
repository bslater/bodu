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
/// numbers and booleans to their string form) and lists to JSON arrays.
/// </summary>
internal sealed class JsonDocumentNode
    : IDocumentNode
{
    /// <summary>The wrapped element.</summary>
    private readonly JsonElement _element;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDocumentNode" /> class.
    /// </summary>
    /// <param name="element">The wrapped element.</param>
    public JsonDocumentNode(JsonElement element)
    {
        _element = element;
    }

    /// <inheritdoc />
    public string? Scalar(string name)
    {
        if (!TryGetProperty(name, out JsonElement value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt32(out int number) ? number.ToString(CultureInfo.InvariantCulture) : value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };
    }

    /// <inheritdoc />
    public IDocumentNode? Child(string name) =>
        TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Object
            ? new JsonDocumentNode(value)
            : null;

    /// <inheritdoc />
    public string Discriminator(string jsonProperty) =>
        Scalar(jsonProperty) ?? string.Empty;

    /// <inheritdoc />
    public IEnumerable<IDocumentNode> List(string? xmlWrapper, string? xmlItem, string jsonProperty)
    {
        if (!TryGetProperty(jsonProperty, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (JsonElement item in array.EnumerateArray())
            yield return new JsonDocumentNode(item);
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
        if (!TryGetProperty(jsonProperty, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
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
        if (!TryGetProperty(jsonProperty, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
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
                JsonDocumentNode reference = new(item);
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
        if (!TryGetProperty(jsonProperty, out JsonElement map) || map.ValueKind != JsonValueKind.Object)
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
        if (!TryGetProperty(jsonProperty, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
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
        if (!TryGetProperty(jsonProperty, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
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
