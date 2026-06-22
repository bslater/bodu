// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateKnownAnswerTests.KnownAnswerData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateKnownAnswerTests
{
    /// <summary>
    /// Verifies that the embedded data set loads and every row references a workbook in the RBA era catalogue.
    /// </summary>
    [TestMethod]
    public void KnownAnswerData_ShouldMapEveryRowToAKnownWorkbook()
    {
        var knownFiles = RbaEra.Default.Select(era => era.FileName).ToHashSet(StringComparer.Ordinal);

        Assert.IsNotEmpty(s_allRows);
        Assert.IsTrue(
            s_allRows.All(row => knownFiles.Contains(row.SourceFileName)),
            "Every known-answer row must reference a workbook in the RBA era catalogue.");
    }
}
