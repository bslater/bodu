// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCodeTests.Conversions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class TerritoryCodeTests
{
    /// <summary>
    /// Verifies that the explicit string conversion parses and the implicit conversion yields the canonical form.
    /// </summary>
    [TestMethod]
    public void Conversions_ShouldRoundTripThroughCanonicalForm()
    {
        var code = (TerritoryCode)"au-nsw";
        string canonical = code;

        Assert.AreEqual("AU-NSW", canonical);
    }
}
