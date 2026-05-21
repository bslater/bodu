// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MixedFormatNotableDateRuleProviderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleResourceProviderBase" /> dispatches per-resource by file extension, so a JSON
/// document may <c>useFrom</c> an XML resource and vice versa within a single rule graph.
/// </summary>
[TestClass]
public sealed class MixedFormatNotableDateRuleProviderTests
{
    private const string MixedJsonRefXml = "Bodu.Globalization.Calendar.Fixtures.MixedJsonRefXml.json";
    private const string MixedXmlRefJson = "Bodu.Globalization.Calendar.Fixtures.MixedXmlRefJson.xml";

    /// <summary>
    /// Verifies that a JSON root document successfully cherry-picks rules from an XML source resource.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenJsonReferencesXml_ShouldFlattenAcrossFormats()
    {
        var provider = new JsonResourceNotableDateRuleProvider(
            MixedJsonRefXml,
            new ResourcePathResolver(),
            Assembly.GetExecutingAssembly());

        var rules = provider.LoadRules().ToList();

        Assert.IsTrue(rules.Any(r => r.Name == "Shared Alpha"), "Expected to inherit Shared Alpha from the XML source.");
        Assert.IsTrue(rules.Any(r => r.Name == "Shared Beta"), "Expected to inherit Shared Beta from the XML source.");
        Assert.IsTrue(rules.Any(r => r.Name == "Mixed Json Local"), "Expected the JSON-local rule to appear in the flattened output.");
    }

    /// <summary>
    /// Verifies that an XML root document successfully cherry-picks rules from a JSON source resource.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenXmlReferencesJson_ShouldFlattenAcrossFormats()
    {
        var provider = new XmlResourceNotableDateRuleProvider(
            MixedXmlRefJson,
            new ResourcePathResolver(),
            Assembly.GetExecutingAssembly());

        var rules = provider.LoadRules().ToList();

        Assert.IsTrue(rules.Any(r => r.Name == "Shared Alpha"), "Expected to inherit Shared Alpha from the JSON source.");
        Assert.IsTrue(rules.Any(r => r.Name == "Shared Beta"), "Expected to inherit Shared Beta from the JSON source.");
        Assert.IsTrue(rules.Any(r => r.Name == "Mixed Xml Local"), "Expected the XML-local rule to appear in the flattened output.");
    }
}
