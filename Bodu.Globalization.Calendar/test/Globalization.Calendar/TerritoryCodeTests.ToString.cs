// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCodeTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class TerritoryCodeTests
{
    /// <summary>
    /// Verifies that the default value renders as the empty string and is empty.
    /// </summary>
    [TestMethod]
    public void ToString_WhenDefault_ShouldBeEmpty()
    {
        TerritoryCode code = default;

        Assert.AreEqual((string.Empty, true), (code.ToString(), code.IsEmpty));
    }
}
