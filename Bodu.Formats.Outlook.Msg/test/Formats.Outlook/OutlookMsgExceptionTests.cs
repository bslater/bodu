// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMsgExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookMsgFormatException" />, the <c>.msg</c> format exception.
/// </summary>
[TestClass]
public class OutlookMsgExceptionTests
{
    /// <summary>
    /// Verifies that the exception derives from the shared Outlook base so the family is catchable together.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldDeriveFromOutlookFormatException()
    {
        var ex = new OutlookMsgFormatException();

        Assert.IsInstanceOfType<OutlookFormatException>(ex);
        Assert.IsInstanceOfType<FormatException>(ex);
    }

    /// <summary>
    /// Verifies that the message and inner-exception constructors preserve their arguments.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageAndInner_ShouldPreserveBoth()
    {
        var inner = new InvalidOperationException("cause");
        var ex = new OutlookMsgFormatException("boom", inner);

        Assert.AreEqual("boom", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }
}
