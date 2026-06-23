// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelNumberFormatTests.GetBuiltInFormatCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelNumberFormatTests
{
    /// <summary>
    /// Verifies that a well-known built-in index resolves to its canonical format code.
    /// </summary>
    [TestMethod]
    public void GetBuiltInFormatCode_WhenKnownIndex_ShouldReturnCanonicalCode()
    {
        Assert.AreEqual("0.00", ExcelNumberFormat.GetBuiltInFormatCode(2));
        Assert.AreEqual("@", ExcelNumberFormat.GetBuiltInFormatCode(49));
    }

    /// <summary>
    /// Verifies that an unknown format index has no built-in code.
    /// </summary>
    [TestMethod]
    public void GetBuiltInFormatCode_WhenUnknownIndex_ShouldReturnNull()
    {
        Assert.IsNull(ExcelNumberFormat.GetBuiltInFormatCode(200));
    }
}
