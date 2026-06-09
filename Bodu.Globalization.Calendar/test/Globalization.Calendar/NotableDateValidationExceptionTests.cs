// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the standard exception constructors on <see cref="NotableDateValidationException" /> initialize the
/// message, inner exception, and (empty) diagnostics collection.
/// </summary>
[TestClass]
public class NotableDateValidationExceptionTests
{
    /// <summary>
    /// Verifies that the parameterless constructor yields an empty diagnostics collection.
    /// </summary>
    [TestMethod]
    public void Constructor_Parameterless_ShouldHaveEmptyDiagnostics()
    {
        var exception = new NotableDateValidationException();

        Assert.IsNotNull(exception.Diagnostics);
        Assert.AreEqual(0, exception.Diagnostics.Count);
    }

    /// <summary>
    /// Verifies that the message constructor preserves the message and yields empty diagnostics.
    /// </summary>
    [TestMethod]
    public void Constructor_WithMessage_ShouldPreserveMessage()
    {
        var exception = new NotableDateValidationException("invalid document");

        Assert.AreEqual("invalid document", exception.Message);
        Assert.AreEqual(0, exception.Diagnostics.Count);
    }

    /// <summary>
    /// Verifies that the message-and-inner-exception constructor preserves both and yields empty diagnostics.
    /// </summary>
    [TestMethod]
    public void Constructor_WithMessageAndInner_ShouldPreserveBoth()
    {
        var inner = new InvalidOperationException("root cause");

        var exception = new NotableDateValidationException("invalid document", inner);

        Assert.AreEqual("invalid document", exception.Message);
        Assert.AreSame(inner, exception.InnerException);
        Assert.AreEqual(0, exception.Diagnostics.Count);
    }
}
