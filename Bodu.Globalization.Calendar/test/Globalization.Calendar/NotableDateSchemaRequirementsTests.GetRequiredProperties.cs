// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaRequirementsTests.GetRequiredProperties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateSchemaRequirementsTests
{
    /// <summary>
    /// Verifies that each schema type requires the JSON properties its XSD complex type requires, so the JSON reader and
    /// the schema cannot drift apart.
    /// </summary>
    [TestMethod]
    public void GetRequiredProperties_ForEachSchemaType_ShouldListWhatTheSchemaRequires()
    {
        Dictionary<string, XElement> complexTypes = SchemaComplexTypes();

        foreach (NotableDateSchemaType type in Enum.GetValues<NotableDateSchemaType>())
        {
            Assert.IsTrue(complexTypes.TryGetValue(type.ToString(), out XElement? complexType), "The XSD declares no complex type " + type + ".");

            string[] expected = [.. SchemaRequiredProperties(complexType).Select(p => p.Property).Order(StringComparer.Ordinal)];
            string[] actual = [.. NotableDateSchemaRequirements.GetRequiredProperties(type).Order(StringComparer.Ordinal)];

            CollectionAssert.AreEqual(expected, actual, type.ToString());
        }
    }

    /// <summary>
    /// Verifies that every complex type of the XSD is either a schema type or left out for a stated reason, so a type
    /// the schema gains is not silently left unchecked in JSON.
    /// </summary>
    [TestMethod]
    public void GetRequiredProperties_ForEachSchemaComplexType_ShouldHaveATypeOrAStatedExclusion()
    {
        HashSet<string> types = [.. Enum.GetNames<NotableDateSchemaType>()];

        foreach (string name in SchemaComplexTypes().Keys)
            Assert.IsTrue(types.Contains(name) ^ s_excludedTypes.ContainsKey(name), name);
    }

    /// <summary>
    /// Verifies that every complex type left out is one the XSD declares, so an exclusion does not outlive the type.
    /// </summary>
    [TestMethod]
    public void GetRequiredProperties_ForEachExcludedComplexType_ShouldBeOneTheSchemaDeclares()
    {
        Dictionary<string, XElement> complexTypes = SchemaComplexTypes();

        foreach (string name in s_excludedTypes.Keys)
            Assert.IsTrue(complexTypes.ContainsKey(name), name);
    }

    /// <summary>
    /// Verifies that a value that names no schema type throws <see cref="ArgumentOutOfRangeException" /> naming the
    /// <c>type</c> parameter.
    /// </summary>
    [TestMethod]
    public void GetRequiredProperties_WhenTypeIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = NotableDateSchemaRequirements.GetRequiredProperties((NotableDateSchemaType)999);
        });

        Assert.AreEqual("type", ex.ParamName);
    }
}
