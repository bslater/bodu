// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaRequirementsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the properties <see cref="NotableDateSchemaRequirements" /> holds for the JSON reader, which must be those the
/// notable-date XML schema requires of each complex type, as the schema declares them.
/// </summary>
[TestClass]
public sealed partial class NotableDateSchemaRequirementsTests
{
    /// <summary>The XML namespace of the XML Schema vocabulary.</summary>
    private static readonly XNamespace s_xs = "http://www.w3.org/2001/XMLSchema";

    /// <summary>
    /// The JSON property each required XSD element is read from, and whether it holds a list the schema requires to
    /// hold an item.
    /// </summary>
    private static readonly Dictionary<string, (string Property, bool IsList)> s_elementProperties = new(StringComparer.Ordinal)
    {
        ["Trigger"] = ("trigger", false),
        ["Action"] = ("action", false),
        ["Emission"] = ("emission", false),
        ["Rules"] = ("rules", true),
        ["UntilDate"] = ("untilDate", false),
        ["Day"] = ("days", true),
        ["Rule"] = ("rule", false),
    };

    /// <summary>
    /// The XSD complex types <see cref="NotableDateSchemaType" /> leaves out, each with the reason the JSON reader holds
    /// a JSON document to none of its requirements.
    /// </summary>
    private static readonly Dictionary<string, string> s_excludedTypes = new(StringComparer.Ordinal)
    {
        ["Metadata"] = "The loader does not read the metadata, and the schema requires none of it.",
        ["ResolutionPolicy"] = "The schema requires none of its attributes or elements.",
        ["CategoryPrecedence"] = "The schema requires none of its elements.",
        ["AdjustmentPolicies"] = "A JSON array stands for the wrapper, and an empty optional list reads as absent.",
        ["AdjustmentScope"] = "The schema requires none of its attributes or elements.",
        ["HandlerParameters"] = "A JSON object stands for the wrapper, and an empty optional map reads as absent.",
        ["HandlerParam"] = "A JSON property always carries its key and its value.",
        ["NotableDates"] = "The schema requires no concept.",
        ["Imports"] = "A JSON array stands for the wrapper, and an empty optional list reads as absent.",
        ["Tags"] = "A JSON array stands for the wrapper, and an empty optional list reads as absent.",
        ["Tag"] = "A JSON list holds a tag as a string, which has no properties.",
        ["Rules"] = "A concept's rules array stands for the wrapper, which NotableDate requires to hold a rule.",
        ["Applicability"] = "The schema requires none of its attributes or elements.",
        ["Strategy"] = "A rule's one strategy or recurrence kind is reported as BODU-CAL-RULE-SOURCE.",
        ["Recurrence"] = "A rule's one strategy or recurrence kind is reported as BODU-CAL-RULE-SOURCE.",
        ["UntilDate"] = "An end strategy's one kind is reported as BODU-CAL-DURATION-NOEND.",
        ["RuleAdjustments"] = "A JSON array stands for the wrapper, and an empty optional list reads as absent.",
        ["AdjustmentRef"] = "A JSON list holds a policy reference as a string, which has no properties.",
        ["Overrides"] = "A JSON array stands for the wrapper, and an empty optional list reads as absent.",
        ["TerritoryRef"] = "A JSON list holds a territory code as a string, which has no properties.",
        ["CalendarRef"] = "A JSON list holds a calendar as a string, which has no properties.",
        ["CategoryRef"] = "A JSON list holds a category as a string, which has no properties.",
        ["NotableDateScopeRef"] = "A JSON list holds a concept reference as a string, which has no properties.",
        ["WeekdayRef"] = "A JSON list holds a weekday as a string, which has no properties.",
        ["YearRef"] = "A JSON list holds a year as a number, which has no properties.",
    };

    /// <summary>
    /// Returns the named complex types the embedded notable-date XSD declares.
    /// </summary>
    /// <returns>The complex types, keyed by name.</returns>
    private static Dictionary<string, XElement> SchemaComplexTypes()
    {
        using Stream? stream = typeof(NotableDateSchemaRequirements).Assembly.GetManifestResourceStream("Bodu.Globalization.Calendar.NotableDates.xsd");
        Assert.IsNotNull(stream);

        XDocument schema = XDocument.Load(stream);
        return schema.Root!.Elements(s_xs + "complexType").ToDictionary(e => (string)e.Attribute("name")!, StringComparer.Ordinal);
    }

    /// <summary>
    /// Returns the JSON properties a complex type of the XSD requires: its attributes declared <c>use="required"</c> and
    /// the elements of its sequence it requires at least once, each with whether it holds a list.
    /// </summary>
    /// <param name="complexType">The complex type.</param>
    /// <returns>The required properties.</returns>
    private static List<(string Property, bool IsList)> SchemaRequiredProperties(XElement complexType)
    {
        List<(string Property, bool IsList)> properties = new();
        foreach (XElement attribute in complexType.Elements(s_xs + "attribute").Where(a => (string?)a.Attribute("use") == "required"))
            properties.Add(((string)attribute.Attribute("name")!, false));

        IEnumerable<XElement> elements = complexType.Element(s_xs + "sequence")?.Elements(s_xs + "element") ?? [];
        foreach (XElement element in elements.Where(e => (string?)e.Attribute("minOccurs") != "0"))
        {
            string name = (string)element.Attribute("name")!;
            Assert.IsTrue(s_elementProperties.TryGetValue(name, out (string Property, bool IsList) property), "No JSON property is mapped to the required element " + name + ".");
            properties.Add(property);
        }

        return properties;
    }
}
