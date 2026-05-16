// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocTagPolicyTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

[TestClass]
public sealed class XmlDocTagPolicyTests
{
    /// <summary>
    /// Verifies that <see cref="XmlDocTagPolicy.Default" /> reports the documented unset overrides.
    /// </summary>
    [TestMethod]
    public void Default_ShouldExposeAutoLayoutAndNullOverrides()
    {
        XmlDocTagPolicy policy = XmlDocTagPolicy.Default;

        Assert.AreEqual(XmlDocTagLayout.Auto, policy.Layout);
        Assert.IsNull(policy.MaxSingleLineLength);
        Assert.IsNull(policy.AllowLineBreakInside);
        Assert.IsNull(policy.SelfClosingTrailingSpace);
    }

    /// <summary>
    /// Verifies that constructor-supplied properties round-trip through the public getters.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldRoundTripProperties()
    {
        XmlDocTagPolicy policy = new XmlDocTagPolicy(
            XmlDocTagLayout.InlineAtomic,
            maxSingleLineLength: 80,
            allowLineBreakInside: false,
            selfClosingTrailingSpace: true);

        Assert.AreEqual(XmlDocTagLayout.InlineAtomic, policy.Layout);
        Assert.AreEqual(80, policy.MaxSingleLineLength);
        Assert.AreEqual(false, policy.AllowLineBreakInside);
        Assert.AreEqual(true, policy.SelfClosingTrailingSpace);
    }
}
