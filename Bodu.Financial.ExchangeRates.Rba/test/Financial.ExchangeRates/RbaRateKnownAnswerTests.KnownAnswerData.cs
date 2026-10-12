// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateKnownAnswerTests.KnownAnswerData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateKnownAnswerTests
{
    /// <summary>
    /// Verifies that the known answers cover every workbook of the RBA era catalogue and reference no other.
    /// </summary>
    [TestMethod]
    public void KnownAnswerData_ShouldMapEveryRowToAKnownWorkbook()
    {
        string[] knownFiles = [.. RbaEraWorkbook.Default.Select(era => era.FileName)];

        Assert.IsNotEmpty(s_allRows);
        CollectionAssert.AreEquivalent(knownFiles, s_allRows.Select(row => row.SourceFileName).Distinct().ToArray());
    }
}
