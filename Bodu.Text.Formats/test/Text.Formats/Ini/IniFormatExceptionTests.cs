// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniFormatExceptionTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats.Ini;

/// <summary>
/// Behavioural tests for <see cref="IniFormatException" />.
/// </summary>
[TestClass]
public sealed class IniFormatExceptionTests
{

    /// <summary>
    /// Verifies that the parameterless constructor creates an instance that derives from
    /// <see cref="FormatException" /> and that <see cref="IniFormatException.LineNumber" /> defaults to zero.
    /// </summary>
    [TestMethod]
    public void ParameterlessConstructor_ShouldProduceFormatExceptionSubclassWithZeroLineNumber()
    {
        IniFormatException ex = new();

        Assert.IsInstanceOfType<FormatException>(ex);
        Assert.AreEqual(0, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that the message-only constructor preserves the supplied message and leaves
    /// <see cref="IniFormatException.LineNumber" /> at zero.
    /// </summary>
    [TestMethod]
    public void MessageConstructor_ShouldPreserveMessageAndDefaultLineNumber()
    {
        IniFormatException ex = new("custom message");

        Assert.AreEqual("custom message", ex.Message);
        Assert.AreEqual(0, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that the message+inner constructor preserves both the message and the inner exception, and that
    /// <see cref="IniFormatException.LineNumber" /> defaults to zero.
    /// </summary>
    [TestMethod]
    public void MessageAndInnerConstructor_ShouldPreserveBothAndDefaultLineNumber()
    {
        InvalidOperationException inner = new("inner");

        IniFormatException ex = new("outer", inner);

        Assert.AreEqual("outer", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
        Assert.AreEqual(0, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that the message+lineNumber constructor preserves both the message and the supplied line number.
    /// </summary>
    [TestMethod]
    public void MessageAndLineNumberConstructor_ShouldPreserveMessageAndLineNumber()
    {
        IniFormatException ex = new("parse error", 42);

        Assert.AreEqual("parse error", ex.Message);
        Assert.AreEqual(42, ex.LineNumber);
    }

}
