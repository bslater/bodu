// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the construction contract and base-type relationships of the <c>Bodu.Formats.Excel</c> exception types.
/// </summary>
[TestClass]
public class ExcelBinaryExceptionTests
{
    /// <summary>
    /// Verifies that the message and inner-exception constructors preserve their arguments for
    /// <see cref="ExcelBinaryFormatException" />, which derives from <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void ExcelBinaryFormatException_WhenConstructed_ShouldPreserveMessageAndInner()
    {
        Exception inner = new InvalidOperationException("cause");

        var withMessage = new ExcelBinaryFormatException("bad record");
        var withInner = new ExcelBinaryFormatException("bad record", inner);

        Assert.IsInstanceOfType<FormatException>(withMessage);
        Assert.AreEqual("bad record", withMessage.Message);
        Assert.AreSame(inner, withInner.InnerException);
    }

    /// <summary>
    /// Verifies that the message and inner-exception constructors preserve their arguments for
    /// <see cref="ExcelBinaryUnsupportedException" />, which derives from <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void ExcelBinaryUnsupportedException_WhenConstructed_ShouldPreserveMessageAndInner()
    {
        Exception inner = new InvalidOperationException("cause");

        var withMessage = new ExcelBinaryUnsupportedException("not biff8");
        var withInner = new ExcelBinaryUnsupportedException("not biff8", inner);

        Assert.IsInstanceOfType<NotSupportedException>(withMessage);
        Assert.AreEqual("not biff8", withMessage.Message);
        Assert.AreSame(inner, withInner.InnerException);
    }

    /// <summary>
    /// Verifies that the message and inner-exception constructors preserve their arguments for
    /// <see cref="ExcelBinaryEncryptedWorkbookException" />, which derives from <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void ExcelBinaryEncryptedWorkbookException_WhenConstructed_ShouldPreserveMessageAndInner()
    {
        Exception inner = new InvalidOperationException("cause");

        var withMessage = new ExcelBinaryEncryptedWorkbookException("encrypted");
        var withInner = new ExcelBinaryEncryptedWorkbookException("encrypted", inner);

        Assert.IsInstanceOfType<NotSupportedException>(withMessage);
        Assert.AreEqual("encrypted", withMessage.Message);
        Assert.AreSame(inner, withInner.InnerException);
    }

    /// <summary>
    /// Verifies that the message and inner-exception constructors preserve their arguments for
    /// <see cref="ExcelBinaryWorkbookStreamNotFoundException" />, which derives from <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void ExcelBinaryWorkbookStreamNotFoundException_WhenConstructed_ShouldPreserveMessageAndInner()
    {
        Exception inner = new InvalidOperationException("cause");

        var withMessage = new ExcelBinaryWorkbookStreamNotFoundException("missing");
        var withInner = new ExcelBinaryWorkbookStreamNotFoundException("missing", inner);

        Assert.IsInstanceOfType<KeyNotFoundException>(withMessage);
        Assert.AreEqual("missing", withMessage.Message);
        Assert.AreSame(inner, withInner.InnerException);
    }
}
