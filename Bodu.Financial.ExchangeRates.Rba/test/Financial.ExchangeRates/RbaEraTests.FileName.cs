// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaEraTests.FileName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaEraTests
{
    /// <summary>
    /// Verifies that the file name is the label with a <c>.xls</c> extension.
    /// </summary>
    [TestMethod]
    public void FileName_ShouldAppendXlsExtension()
    {
        RbaEra era = new("2023-current", new DateOnly(2023, 1, 1), null);

        Assert.AreEqual("2023-current.xls", era.FileName);
    }
}
