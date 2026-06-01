// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvFormatExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Unit tests for <see cref="DotEnvFormatException" />.
/// </summary>
[TestClass]
public sealed class DotEnvFormatExceptionTests
{

    /// <summary>
    /// Verifies that the default constructor produces an instance whose <see cref="DotEnvFormatException.LineNumber" />
    /// is <c>0</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_Default_ShouldHaveLineNumberZero()
    {
        DotEnvFormatException ex = new();

        Assert.AreEqual(0, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that the message constructor sets <see cref="Exception.Message" /> and leaves
    /// <see cref="DotEnvFormatException.LineNumber" /> at <c>0</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_Message_ShouldSetMessageAndLineNumberZero()
    {
        DotEnvFormatException ex = new("bad input");

        Assert.AreEqual("bad input", ex.Message);
        Assert.AreEqual(0, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that the message-and-inner-exception constructor sets both <see cref="Exception.Message" /> and
    /// <see cref="Exception.InnerException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_MessageAndInnerException_ShouldSetBothProperties()
    {
        InvalidOperationException inner = new("inner");

        DotEnvFormatException ex = new("outer", inner);

        Assert.AreEqual("outer", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message-and-line-number constructor sets <see cref="Exception.Message" /> and
    /// <see cref="DotEnvFormatException.LineNumber" /> to the specified values.
    /// </summary>
    [TestMethod]
    public void Ctor_MessageAndLineNumber_ShouldSetBothProperties()
    {
        DotEnvFormatException ex = new("parse error", 7);

        Assert.AreEqual("parse error", ex.Message);
        Assert.AreEqual(7, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvFormatException" /> is derived from <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Type_ShouldDeriveFromFormatException()
    {
        DotEnvFormatException ex = new();

        Assert.IsInstanceOfType<FormatException>(ex);
    }

}
