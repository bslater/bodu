// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaRequirementsTests.RequiresItems.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateSchemaRequirementsTests
{
    /// <summary>
    /// Verifies that a required property is held to hold an item exactly when the XSD requires a list there: a
    /// concept's <c>Rules</c> and a weekly recurrence's <c>Day</c> elements.
    /// </summary>
    [TestMethod]
    public void RequiresItems_ForEachRequiredProperty_ShouldMatchWhetherTheSchemaRequiresAList()
    {
        Dictionary<string, XElement> complexTypes = SchemaComplexTypes();

        foreach (NotableDateSchemaType type in Enum.GetValues<NotableDateSchemaType>())
        {
            foreach ((string property, bool isList) in SchemaRequiredProperties(complexTypes[type.ToString()]))
                Assert.AreEqual(isList, NotableDateSchemaRequirements.RequiresItems(type, property), type + " " + property);
        }
    }

    /// <summary>
    /// Verifies that a property a schema type does not require is not held to hold an item.
    /// </summary>
    /// <param name="type">The name of the schema type.</param>
    /// <param name="property">The JSON property.</param>
    [TestMethod]
    [DataRow("NotableDate", "tags")]
    [DataRow("Rule", "adjustments")]
    [DataRow("WeeklyRecurrence", "rules")]
    public void RequiresItems_WhenPropertyIsNotRequired_ShouldReturnFalse(string type, string property)
    {
        Assert.IsFalse(NotableDateSchemaRequirements.RequiresItems(Enum.Parse<NotableDateSchemaType>(type), property));
    }
}
