// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfoTests.MetadataFields.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CurrencyInfoTests
{
    /// <summary>
    /// Verifies that the extended display-metadata members default to empty when not supplied, so the positional
    /// constructor remains usable unchanged.
    /// </summary>
    [TestMethod]
    public void MetadataFields_WhenNotSpecified_ShouldDefaultToEmpty()
    {
        CurrencyInfo info = new("USD", 2, 0m, false, null, null);

        Assert.AreEqual("", info.Symbol);
        Assert.AreEqual("", info.InternationalSymbol);
        Assert.AreEqual("", info.NativeName);
        Assert.AreEqual(0, info.RegionCodes.Count);
        Assert.AreEqual(0, info.AlternativeSymbols.Count);
    }

    /// <summary>
    /// Verifies that the extended display-metadata members can be supplied through a <c>with</c> expression and are
    /// preserved on the resulting record.
    /// </summary>
    [TestMethod]
    public void MetadataFields_WhenSetViaWithExpression_ShouldPreserveValues()
    {
        var info = new CurrencyInfo("USD", 2, 0m, false, null, null, "United States Dollar", 840)
        {
            Symbol = "$",
            InternationalSymbol = "US$",
            NativeName = "US Dollar",
            RegionCodes = ["US", "EC"],
            AlternativeSymbols = ["US$"],
        };

        Assert.AreEqual("$", info.Symbol);
        Assert.AreEqual("US$", info.InternationalSymbol);
        Assert.AreEqual("US Dollar", info.NativeName);
        CollectionAssert.AreEqual(new[] { "US", "EC" }, info.RegionCodes.ToArray());
        CollectionAssert.AreEqual(new[] { "US$" }, info.AlternativeSymbols.ToArray());

        // Positional fields are unaffected by the extended members.
        Assert.AreEqual("USD", info.IsoCode);
        Assert.AreEqual(840, info.NumericCode);
    }
}
