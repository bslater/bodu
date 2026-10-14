// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonSchemaValidator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Validates a JSON document against a JSON Schema (draft 2020-12), for the keywords the companion schema of the
/// notable-date document uses.
/// </summary>
/// <remarks>
/// <para>
/// The keywords applied are <c>$ref</c> to a definition of the same schema (<c>#/$defs/…</c>), <c>type</c>,
/// <c>enum</c>, <c>const</c>, <c>pattern</c>, <c>minimum</c>, <c>maximum</c>, <c>properties</c>, <c>required</c>,
/// <c>additionalProperties</c>, <c>minProperties</c>, <c>maxProperties</c>, <c>items</c>, <c>minItems</c>,
/// <c>allOf</c>, <c>anyOf</c>, <c>oneOf</c>, <c>not</c>, and <c>if</c> / <c>then</c> / <c>else</c>. Every other
/// keyword is an annotation here, <c>format</c> among them, as draft 2020-12 makes it by default.
/// </para>
/// <para>
/// A keyword the schema uses that this validator does not apply would let a test pass vacuously, so
/// <see cref="Validate" /> throws <see cref="NotSupportedException" /> for a keyword outside the applied and annotation
/// sets.
/// </para>
/// </remarks>
internal static class JsonSchemaValidator
{
    /// <summary>The keywords that only annotate a schema, which validation ignores.</summary>
    private static readonly HashSet<string> s_annotations = new(StringComparer.Ordinal)
    {
        "$schema", "$id", "$defs", "$comment", "title", "description", "format", "default", "examples",
    };

    /// <summary>The keywords validation applies.</summary>
    private static readonly HashSet<string> s_assertions = new(StringComparer.Ordinal)
    {
        "$ref", "type", "enum", "const", "pattern", "minimum", "maximum", "properties", "required",
        "additionalProperties", "minProperties", "maxProperties", "items", "minItems", "allOf", "anyOf", "oneOf", "not",
        "if", "then", "else",
    };

    /// <summary>
    /// Validates a JSON document against a schema.
    /// </summary>
    /// <param name="schema">The schema's root object.</param>
    /// <param name="instance">The document, or <see langword="null" /> for the JSON literal <c>null</c>.</param>
    /// <returns>A description of each violation, with the location of the value; empty when the document is valid.</returns>
    /// <exception cref="NotSupportedException">The schema uses a keyword this validator neither applies nor ignores.</exception>
    public static IReadOnlyList<string> Validate(JsonObject schema, JsonNode? instance)
    {
        List<string> errors = new();
        Validate(schema, schema, instance, "#", errors);
        return errors;
    }

    /// <summary>
    /// Validates a value against a subschema, adding a description of each violation to a list.
    /// </summary>
    /// <param name="root">The schema's root object, which <c>$ref</c> resolves against.</param>
    /// <param name="schemaNode">The subschema: an object, or a Boolean schema.</param>
    /// <param name="instance">The value, or <see langword="null" /> for the JSON literal <c>null</c>.</param>
    /// <param name="path">The JSON pointer of the value, used in the descriptions.</param>
    /// <param name="errors">The list that receives the descriptions.</param>
    private static void Validate(JsonObject root, JsonNode? schemaNode, JsonNode? instance, string path, List<string> errors)
    {
        if (schemaNode is JsonValue boolean)
        {
            if (boolean.GetValueKind() == JsonValueKind.False)
                errors.Add(path + ": the schema allows no value here.");

            return;
        }

        JsonObject schema = schemaNode!.AsObject();
        foreach ((string keyword, _) in schema)
        {
            if (!s_assertions.Contains(keyword) && !s_annotations.Contains(keyword))
                throw new NotSupportedException("The JSON schema keyword '" + keyword + "' is not supported.");
        }

        if (schema["$ref"] is JsonValue reference)
            Validate(root, Resolve(root, reference.GetValue<string>()), instance, path, errors);

        if (schema["type"] is JsonNode type && !MatchesType(type, instance))
            errors.Add(path + ": " + Describe(instance) + " is not of type " + type.ToJsonString() + ".");

        if (schema["enum"] is JsonArray values && !values.Any(value => JsonNode.DeepEquals(value, instance)))
            errors.Add(path + ": " + Describe(instance) + " is not one of " + values.ToJsonString() + ".");

        if (schema.TryGetPropertyValue("const", out JsonNode? constant) && !JsonNode.DeepEquals(constant, instance))
            errors.Add(path + ": " + Describe(instance) + " is not " + (constant?.ToJsonString() ?? "null") + ".");

        switch (instance)
        {
            case JsonObject value:
                ValidateObject(root, schema, value, path, errors);
                break;

            case JsonArray value:
                ValidateArray(root, schema, value, path, errors);
                break;

            case JsonValue value when value.GetValueKind() == JsonValueKind.String:
                if (schema["pattern"] is JsonValue pattern && !Regex.IsMatch(value.GetValue<string>(), pattern.GetValue<string>(), RegexOptions.None, TimeSpan.FromSeconds(1)))
                    errors.Add(path + ": " + Describe(instance) + " does not match the pattern " + pattern.ToJsonString() + ".");

                break;

            case JsonValue value when value.GetValueKind() == JsonValueKind.Number:
                decimal number = value.GetValue<decimal>();
                if (schema["minimum"] is JsonValue minimum && number < minimum.GetValue<decimal>())
                    errors.Add(path + ": " + Describe(instance) + " is less than the minimum " + minimum.ToJsonString() + ".");

                if (schema["maximum"] is JsonValue maximum && number > maximum.GetValue<decimal>())
                    errors.Add(path + ": " + Describe(instance) + " is greater than the maximum " + maximum.ToJsonString() + ".");

                break;
        }

        if (schema["allOf"] is JsonArray allOf)
        {
            foreach (JsonNode? subschema in allOf)
                Validate(root, subschema, instance, path, errors);
        }

        if (schema["anyOf"] is JsonArray anyOf && !anyOf.Any(subschema => IsValid(root, subschema, instance, path)))
            errors.Add(path + ": " + Describe(instance) + " matches none of the anyOf schemas.");

        if (schema["oneOf"] is JsonArray oneOf)
        {
            int matches = oneOf.Count(subschema => IsValid(root, subschema, instance, path));
            if (matches != 1)
                errors.Add(path + ": " + Describe(instance) + " matches " + matches.ToString(CultureInfo.InvariantCulture) + " of the oneOf schemas rather than one.");
        }

        if (schema["not"] is JsonNode not && IsValid(root, not, instance, path))
            errors.Add(path + ": " + Describe(instance) + " matches the schema it must not match.");

        if (schema["if"] is JsonNode condition)
        {
            JsonNode? consequence = IsValid(root, condition, instance, path) ? schema["then"] : schema["else"];
            if (consequence is not null)
                Validate(root, consequence, instance, path, errors);
        }
    }

    /// <summary>
    /// Validates an object against the object keywords of a subschema.
    /// </summary>
    /// <param name="root">The schema's root object.</param>
    /// <param name="schema">The subschema.</param>
    /// <param name="instance">The object.</param>
    /// <param name="path">The JSON pointer of the object.</param>
    /// <param name="errors">The list that receives the descriptions.</param>
    private static void ValidateObject(JsonObject root, JsonObject schema, JsonObject instance, string path, List<string> errors)
    {
        if (schema["required"] is JsonArray required)
        {
            foreach (JsonNode? name in required)
            {
                if (!instance.ContainsKey(name!.GetValue<string>()))
                    errors.Add(path + ": the required property '" + name.GetValue<string>() + "' is missing.");
            }
        }

        if (schema["minProperties"] is JsonValue minProperties && instance.Count < minProperties.GetValue<int>())
            errors.Add(path + ": the object has fewer than " + minProperties.ToJsonString() + " properties.");

        if (schema["maxProperties"] is JsonValue maxProperties && instance.Count > maxProperties.GetValue<int>())
            errors.Add(path + ": the object has more than " + maxProperties.ToJsonString() + " properties.");

        JsonObject? properties = schema["properties"] as JsonObject;
        foreach ((string name, JsonNode? value) in instance)
        {
            string propertyPath = path + "/" + name;
            if (properties is not null && properties.TryGetPropertyValue(name, out JsonNode? propertySchema))
                Validate(root, propertySchema, value, propertyPath, errors);
            else if (schema["additionalProperties"] is JsonNode additional)
                Validate(root, additional, value, propertyPath, errors);
        }
    }

    /// <summary>
    /// Validates an array against the array keywords of a subschema.
    /// </summary>
    /// <param name="root">The schema's root object.</param>
    /// <param name="schema">The subschema.</param>
    /// <param name="instance">The array.</param>
    /// <param name="path">The JSON pointer of the array.</param>
    /// <param name="errors">The list that receives the descriptions.</param>
    private static void ValidateArray(JsonObject root, JsonObject schema, JsonArray instance, string path, List<string> errors)
    {
        if (schema["minItems"] is JsonValue minItems && instance.Count < minItems.GetValue<int>())
            errors.Add(path + ": the array has fewer than " + minItems.ToJsonString() + " items.");

        if (schema["items"] is JsonNode items)
        {
            for (int index = 0; index < instance.Count; index++)
                Validate(root, items, instance[index], path + "/" + index.ToString(CultureInfo.InvariantCulture), errors);
        }
    }

    /// <summary>
    /// Returns a value indicating whether a value is valid against a subschema.
    /// </summary>
    /// <param name="root">The schema's root object.</param>
    /// <param name="schema">The subschema.</param>
    /// <param name="instance">The value.</param>
    /// <param name="path">The JSON pointer of the value.</param>
    /// <returns><see langword="true" /> when the value is valid; otherwise, <see langword="false" />.</returns>
    private static bool IsValid(JsonObject root, JsonNode? schema, JsonNode? instance, string path)
    {
        List<string> errors = new();
        Validate(root, schema, instance, path, errors);
        return errors.Count == 0;
    }

    /// <summary>
    /// Returns a value indicating whether a value is of a JSON schema type, or of one of a list of them.
    /// </summary>
    /// <param name="type">The <c>type</c> keyword's value: a type name or an array of them.</param>
    /// <param name="instance">The value.</param>
    /// <returns><see langword="true" /> when the value is of the type or of one of them; otherwise, <see langword="false" />.</returns>
    private static bool MatchesType(JsonNode type, JsonNode? instance) =>
        type is JsonArray names
            ? names.Any(name => MatchesType(name!.GetValue<string>(), instance))
            : MatchesType(type.GetValue<string>(), instance);

    /// <summary>
    /// Returns a value indicating whether a value is of a JSON schema type.
    /// </summary>
    /// <param name="type">The type's name.</param>
    /// <param name="instance">The value.</param>
    /// <returns><see langword="true" /> when the value is of the type; otherwise, <see langword="false" />.</returns>
    private static bool MatchesType(string type, JsonNode? instance) =>
        (type, instance) switch
        {
            ("null", null) => true,
            ("object", JsonObject) => true,
            ("array", JsonArray) => true,
            ("string", JsonValue value) => value.GetValueKind() == JsonValueKind.String,
            ("boolean", JsonValue value) => value.GetValueKind() is JsonValueKind.True or JsonValueKind.False,
            ("number", JsonValue value) => value.GetValueKind() == JsonValueKind.Number,
            ("integer", JsonValue value) => value.GetValueKind() == JsonValueKind.Number && decimal.Truncate(value.GetValue<decimal>()) == value.GetValue<decimal>(),
            _ => false,
        };

    /// <summary>
    /// Resolves a reference to a definition of the schema.
    /// </summary>
    /// <param name="root">The schema's root object.</param>
    /// <param name="reference">The reference, of the form <c>#/$defs/name</c>.</param>
    /// <returns>The definition.</returns>
    /// <exception cref="NotSupportedException">The reference is of another form or names no definition.</exception>
    private static JsonNode Resolve(JsonObject root, string reference)
    {
        const string prefix = "#/$defs/";
        if (!reference.StartsWith(prefix, StringComparison.Ordinal) || root["$defs"]?[reference[prefix.Length..]] is not JsonNode definition)
            throw new NotSupportedException("The JSON schema reference '" + reference + "' is not supported.");

        return definition;
    }

    /// <summary>
    /// Describes a value for a violation's description: a scalar by its JSON text, a container by its kind.
    /// </summary>
    /// <param name="instance">The value.</param>
    /// <returns>The description.</returns>
    private static string Describe(JsonNode? instance) =>
        instance switch
        {
            null => "null",
            JsonObject => "the object",
            JsonArray => "the array",
            _ => instance.ToJsonString(),
        };
}
