// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelNumberFormatTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the behavior of <see cref="ExcelNumberFormat" />, the number-format classifier.
/// </summary>
[TestClass]
public class ExcelNumberFormatTests
{
    /// <summary>
    /// Verifies that a built-in date or time format index is recognized as a date format without a code.
    /// </summary>
    /// <param name="formatIndex">The built-in number-format index.</param>
    [TestMethod]
    [DataRow((ushort)14)]
    [DataRow((ushort)18)]
    [DataRow((ushort)22)]
    [DataRow((ushort)45)]
    [DataRow((ushort)47)]
    public void IsDateFormat_WhenBuiltInDateIndex_ShouldReturnTrue(ushort formatIndex)
    {
        Assert.IsTrue(ExcelNumberFormat.IsDateFormat(formatIndex, null));
    }

    /// <summary>
    /// Verifies that a built-in numeric format index is not recognized as a date format.
    /// </summary>
    /// <param name="formatIndex">The built-in number-format index.</param>
    [TestMethod]
    [DataRow((ushort)0)]
    [DataRow((ushort)2)]
    [DataRow((ushort)9)]
    [DataRow((ushort)11)]
    public void IsDateFormat_WhenBuiltInNumericIndex_ShouldReturnFalse(ushort formatIndex)
    {
        Assert.IsFalse(ExcelNumberFormat.IsDateFormat(formatIndex, null));
    }

    /// <summary>
    /// Verifies that a custom format code containing date or time fields is recognized as a date format.
    /// </summary>
    /// <param name="formatCode">The custom format code.</param>
    [TestMethod]
    [DataRow("yyyy-mm-dd")]
    [DataRow("d/m/yyyy")]
    [DataRow("h:mm:ss")]
    [DataRow("[h]:mm:ss")]
    [DataRow("mmm dd, yyyy")]
    [DataRow("dddd")]
    public void IsDateFormat_WhenCustomCodeHasDateFields_ShouldReturnTrue(string formatCode)
    {
        Assert.IsTrue(ExcelNumberFormat.IsDateFormat(180, formatCode));
    }

    /// <summary>
    /// Verifies that a custom numeric, percentage, currency, or text code is not recognized as a date format.
    /// </summary>
    /// <param name="formatCode">The custom format code.</param>
    [TestMethod]
    [DataRow("0.00")]
    [DataRow("#,##0.00")]
    [DataRow("0.0%")]
    [DataRow("[Red]0.0")]
    [DataRow("0.00E+00")]
    [DataRow("@")]
    [DataRow("General")]
    public void IsDateFormat_WhenCustomCodeIsNotDate_ShouldReturnFalse(string formatCode)
    {
        Assert.IsFalse(ExcelNumberFormat.IsDateFormat(180, formatCode));
    }

    /// <summary>
    /// Verifies that date or time field characters enclosed in quoted literals do not classify a code as a date.
    /// </summary>
    [TestMethod]
    public void IsDateFormat_WhenDateLettersAreQuoted_ShouldReturnFalse()
    {
        Assert.IsFalse(ExcelNumberFormat.IsDateFormat(180, "0\" days\""));
    }

    /// <summary>
    /// Verifies that only the first format section determines whether a code is a date format.
    /// </summary>
    [TestMethod]
    public void IsDateFormat_WhenOnlyLaterSectionHasDateFields_ShouldReturnFalse()
    {
        Assert.IsFalse(ExcelNumberFormat.IsDateFormat(180, "0.00;yyyy"));
    }

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
