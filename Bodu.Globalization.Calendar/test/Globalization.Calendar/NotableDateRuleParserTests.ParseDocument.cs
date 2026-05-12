// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.ParseDocument.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Linq;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

public partial class NotableDateRuleParserTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateRuleParser.ParseDocument(string)" /> exposes UseFrom groups in declaration order, each
    /// pointing at the named source resource.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenUseFromPresent_ShouldExposeGroupsInOrder()
    {
        var doc = NotableDateRuleParser.ParseDocument(CherryPickXml);

        Assert.AreEqual(2, doc.UseGroups.Length);
        Assert.AreEqual("Bodu.Globalization.Calendar.Resources.Common.xml", doc.UseGroups[0].SourceResource);
        Assert.AreEqual("Bodu.Globalization.Calendar.Resources.Christian.xml", doc.UseGroups[1].SourceResource);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleParser.ParseDocument(string)" /> exposes each Use directive's source name and any
    /// scalar overrides (territory, non-working, priority, rename via 'as').
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenUseDirectivesPresent_ShouldExposeScalarOverrides()
    {
        var doc = NotableDateRuleParser.ParseDocument(CherryPickXml);

        var common = doc.UseGroups[0];
        Assert.AreEqual(2, common.Uses.Length);
        Assert.IsFalse(common.UseAll);
        Assert.AreEqual("New Year's Day", common.Uses[0].SourceRuleName);
        Assert.AreEqual("US", common.Uses[0].TerritoryCode);
        Assert.AreEqual("Halloween", common.Uses[1].SourceRuleName);
        Assert.IsNull(common.Uses[1].TerritoryCode);

        var christian = doc.UseGroups[1];
        Assert.AreEqual(1, christian.Uses.Length);
        Assert.AreEqual("Christmas Day", christian.Uses[0].SourceRuleName);
        Assert.AreEqual("Christmas", christian.Uses[0].LocalName);
        Assert.AreEqual("US", christian.Uses[0].TerritoryCode);
        Assert.IsTrue(christian.Uses[0].IsNonWorkingDay);
        Assert.AreEqual(5, christian.Uses[0].Priority);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleParser.ParseDocument(string)" /> recognises the wildcard <c>UseAll</c> directive.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenUseAllPresent_ShouldFlagGroupAsWildcard()
    {
        var doc = NotableDateRuleParser.ParseDocument(UseAllXml);

        Assert.AreEqual(1, doc.UseGroups.Length);
        Assert.IsTrue(doc.UseGroups[0].UseAll);
        Assert.AreEqual(0, doc.UseGroups[0].Uses.Length);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleParser.ParseDocument(string)" /> still parses local rules alongside cherry-pick
    /// directives.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenLocalRulesPresent_ShouldExposeLocalRules()
    {
        var doc = NotableDateRuleParser.ParseDocument(CherryPickXml);

        Assert.AreEqual(1, doc.LocalRules.Length);
        Assert.AreEqual("Independence Day", doc.LocalRules[0].Name);
    }


    /// <summary>
    /// Verifies that the <see cref="NotableDateRuleParser.ParseDocument(System.Xml.Linq.XDocument)" />
    /// overload returns a parsed document equivalent to the string overload.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenCalledWithXDocument_ShouldReturnSameDocument()
    {
        var document = XDocument.Parse(FixedRuleXml);

        var parsed = NotableDateRuleParser.ParseDocument(document);

        Assert.AreEqual(1, parsed.LocalRules.Length);
        Assert.AreEqual("Fixed Rule Test", parsed.LocalRules[0].Name);
    }

    /// <summary>
    /// Verifies that the <see cref="NotableDateRuleParser.ParseDocument(System.Xml.Linq.XDocument)" />
    /// overload rejects a null document via <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXDocumentIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateRuleParser.ParseDocument((XDocument)null!);
        });

        Assert.AreEqual("document", ex.ParamName);
    }
}