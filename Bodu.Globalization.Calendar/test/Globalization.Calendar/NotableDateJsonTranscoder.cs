// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateJsonTranscoder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Writes a notable-date XML document in the JSON form <see cref="NotableDateResourceLoader.LoadJson(string)" />
/// reads, member for member, so that a document can be held to the companion JSON schema and loaded in both forms.
/// </summary>
/// <remarks>
/// <para>
/// An attribute becomes a property of the same name, holding a JSON number when the XSD types it as an integer, a
/// JSON Boolean when it types it as a Boolean, and a string otherwise; an attribute the XSD supplies as a default is
/// left out, as the document left it out. A child element becomes the property the loader reads it from: an object
/// for a single element, an array of objects for a wrapper of elements (<c>Rules</c>, <c>NotableDates</c>, …), an
/// array of the item's value for a list of references or names (<c>Tags</c>, <c>Adjustments</c>, a scope's
/// <c>Territory</c> elements, …), and a map for the handler <c>Parameters</c>. An override becomes an object whose
/// <c>operation</c> names its element.
/// </para>
/// </remarks>
internal static class NotableDateJsonTranscoder
{
    /// <summary>The namespace of the notable-date document.</summary>
    private static readonly XNamespace s_namespace = "urn:bodu:globalization:calendar";

    /// <summary>
    /// The lists a parent element holds as repeated items, keyed by the parent's and the item's local names: the JSON
    /// property that holds them and the reader of each item.
    /// </summary>
    private static readonly Dictionary<(string Parent, string Item), (string Property, Func<XElement, JsonNode?> Read)> s_itemLists = new()
    {
        [("Scope", "Territory")] = ("territories", item => ItemText(item, "code")),
        [("Scope", "Calendar")] = ("calendars", item => ItemText(item, "name")),
        [("Scope", "Category")] = ("categories", item => ItemText(item, "value")),
        [("Scope", "NotableDate")] = ("notableDateRefs", item => ItemText(item, "ref")),
        [("Scope", "Rule")] = ("ruleRefs", ToObject),
        [("Scope", "OnlyYear")] = ("onlyYears", item => ToValue(item.Attribute("value")!)),
        [("Scope", "ExceptYear")] = ("exceptYears", item => ToValue(item.Attribute("value")!)),
        [("Applicability", "Territory")] = ("territories", item => ItemText(item, "code")),
        [("Applicability", "OnlyYear")] = ("onlyYears", item => ToValue(item.Attribute("value")!)),
        [("Applicability", "ExceptYear")] = ("exceptYears", item => ToValue(item.Attribute("value")!)),
        [("Trigger", "Weekday")] = ("weekdays", item => ItemText(item, "value")),
        [("Import", "Use")] = ("uses", ToObject),
        [("Weekly", "Day")] = ("days", item => ItemText(item, "dayOfWeek")),
        [("Metadata", "Source")] = ("sources", item => JsonValue.Create(item.Value)),
    };

    /// <summary>
    /// The wrapper elements whose children the loader reads as one list, keyed by the wrapper's local name: the JSON
    /// property that holds the list and the reader of each child.
    /// </summary>
    private static readonly Dictionary<string, (string Property, Func<XElement, JsonNode?> Read)> s_wrappers = new(StringComparer.Ordinal)
    {
        ["AdjustmentPolicies"] = ("adjustmentPolicies", ToObject),
        ["Imports"] = ("imports", ToObject),
        ["NotableDates"] = ("notableDates", ToObject),
        ["Rules"] = ("rules", ToObject),
        ["Overrides"] = ("overrides", ToOverride),
        ["Tags"] = ("tags", item => ItemText(item, "value")),
        ["Adjustments"] = ("adjustments", item => ItemText(item, "policyRef")),
        ["CategoryPrecedence"] = ("categoryPrecedence", item => ItemText(item, "value")),
    };

    /// <summary>
    /// Writes a notable-date XML document in the JSON form the loader reads.
    /// </summary>
    /// <param name="xml">The XML document, which must be valid against the notable-date XSD.</param>
    /// <returns>The JSON document.</returns>
    /// <exception cref="XmlSchemaValidationException">The document is not valid against the XSD.</exception>
    public static string Transcode(string xml)
    {
        XDocument document = XDocument.Parse(xml);
        document.Validate(Schemas(), static (_, args) => throw args.Exception, addSchemaInfo: true);

        return ToObject(document.Root!)!.ToJsonString();
    }

    /// <summary>
    /// Returns the schema set holding the notable-date XSD embedded in the loader's assembly.
    /// </summary>
    /// <returns>The schema set.</returns>
    private static XmlSchemaSet Schemas()
    {
        using Stream stream = typeof(NotableDateResourceLoader).Assembly.GetManifestResourceStream("Bodu.Globalization.Calendar.NotableDates.xsd")!;
        using XmlReader reader = XmlReader.Create(stream);

        XmlSchemaSet schemas = new();
        _ = schemas.Add(s_namespace.NamespaceName, reader);
        return schemas;
    }

    /// <summary>
    /// Writes an element as a JSON object: its attributes, then its children.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The JSON object.</returns>
    private static JsonNode? ToObject(XElement element)
    {
        JsonObject result = new();
        foreach (XAttribute attribute in element.Attributes())
        {
            if (!attribute.IsNamespaceDeclaration && attribute.Name.Namespace == XNamespace.None && attribute.GetSchemaInfo()?.IsDefault != true)
                result[attribute.Name.LocalName] = ToValue(attribute);
        }

        string parent = element.Name.LocalName;
        foreach (IGrouping<string, XElement> group in element.Elements().Where(child => child.Name.Namespace == s_namespace).GroupBy(child => child.Name.LocalName))
        {
            if (s_itemLists.TryGetValue((parent, group.Key), out (string Property, Func<XElement, JsonNode?> Read) list))
            {
                result[list.Property] = new JsonArray([.. group.Select(list.Read)]);
                continue;
            }

            foreach (XElement child in group)
            {
                if (s_wrappers.TryGetValue(group.Key, out (string Property, Func<XElement, JsonNode?> Read) wrapper))
                    result[wrapper.Property] = new JsonArray([.. child.Elements().Select(wrapper.Read)]);
                else if (group.Key == "Parameters")
                    result["parameters"] = Parameters(child);
                else if (parent == "Metadata")
                    result[CamelCase(group.Key)] = child.Value;
                else
                    result[CamelCase(group.Key)] = ToObject(child);
            }
        }

        return result;
    }

    /// <summary>
    /// Writes an override element as a JSON object whose <c>operation</c> names the element.
    /// </summary>
    /// <param name="element">The <c>AddRule</c>, <c>PatchRule</c> or <c>RemoveRule</c> element.</param>
    /// <returns>The JSON object.</returns>
    private static JsonNode? ToOverride(XElement element)
    {
        JsonObject result = ToObject(element)!.AsObject();
        result["operation"] = element.Name.LocalName;
        return result;
    }

    /// <summary>
    /// Writes a handler <c>Parameters</c> element as a JSON map of each parameter's key to its value.
    /// </summary>
    /// <param name="element">The <c>Parameters</c> element.</param>
    /// <returns>The JSON object.</returns>
    private static JsonObject Parameters(XElement element)
    {
        JsonObject result = new();
        foreach (XElement parameter in element.Elements(s_namespace + "Param"))
            result[(string)parameter.Attribute("key")!] = (string)parameter.Attribute("value")!;

        return result;
    }

    /// <summary>
    /// Returns the text of an item's attribute as a JSON string.
    /// </summary>
    /// <param name="item">The item element.</param>
    /// <param name="attribute">The attribute's name.</param>
    /// <returns>The JSON string.</returns>
    private static JsonNode? ItemText(XElement item, string attribute) =>
        JsonValue.Create((string)item.Attribute(attribute)!);

    /// <summary>
    /// Writes an attribute's value as the JSON value of its XSD type: a number for an integer, a Boolean for a Boolean,
    /// and a string otherwise.
    /// </summary>
    /// <param name="attribute">The attribute, validated against the XSD.</param>
    /// <returns>The JSON value.</returns>
    private static JsonNode? ToValue(XAttribute attribute)
    {
        string text = attribute.Value.Trim();
        return attribute.GetSchemaInfo()?.SchemaType?.TypeCode switch
        {
            XmlTypeCode.Int or XmlTypeCode.Integer or XmlTypeCode.PositiveInteger or XmlTypeCode.UnsignedShort =>
                JsonValue.Create(long.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)),
            XmlTypeCode.Boolean => JsonValue.Create(XmlConvert.ToBoolean(text)),
            _ => JsonValue.Create(attribute.Value),
        };
    }

    /// <summary>
    /// Returns an element's local name with its first letter in lowercase, the name of the JSON property that holds it.
    /// </summary>
    /// <param name="name">The element's local name.</param>
    /// <returns>The JSON property name.</returns>
    private static string CamelCase(string name) =>
        char.ToLowerInvariant(name[0]) + name[1..];
}
