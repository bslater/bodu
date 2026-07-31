// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookFormatExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookFormatException" />, the Outlook format exception base.
/// </summary>
[TestClass]
public class OutlookFormatExceptionTests
{
    /// <summary>
    /// Verifies that the exception derives from <see cref="FormatException" /> so consumers can catch the family
    /// through the BCL base.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldDeriveFromFormatException()
    {
        var ex = new OutlookFormatException();

        Assert.IsInstanceOfType<FormatException>(ex);
    }

    /// <summary>
    /// Verifies that the message constructor preserves the message.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessage_ShouldPreserveMessage()
    {
        var ex = new OutlookFormatException("boom");

        Assert.AreEqual("boom", ex.Message);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message-and-inner constructor preserves both.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageAndInner_ShouldPreserveBoth()
    {
        var inner = new InvalidOperationException("cause");
        var ex = new OutlookFormatException("boom", inner);

        Assert.AreEqual("boom", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }
}
