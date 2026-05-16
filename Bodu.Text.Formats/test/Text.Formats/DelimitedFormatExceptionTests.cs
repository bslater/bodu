// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedFormatExceptionTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Unit tests for <see cref="DelimitedFormatException" />.
/// </summary>
[TestClass]
public sealed class DelimitedFormatExceptionTests
{

    /// <summary>
    /// Verifies that the default constructor produces an instance whose
    /// <see cref="DelimitedFormatException.LineNumber" /> is <c>0</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_Default_ShouldHaveLineNumberZero()
    {
        DelimitedFormatException ex = new();

        Assert.AreEqual(0, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that the message constructor sets <see cref="Exception.Message" /> and leaves
    /// <see cref="DelimitedFormatException.LineNumber" /> at <c>0</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_Message_ShouldSetMessageAndLineNumberZero()
    {
        DelimitedFormatException ex = new("bad input");

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

        DelimitedFormatException ex = new("outer", inner);

        Assert.AreEqual("outer", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message-and-line-number constructor sets <see cref="Exception.Message" /> and
    /// <see cref="DelimitedFormatException.LineNumber" /> to the specified values.
    /// </summary>
    [TestMethod]
    public void Ctor_MessageAndLineNumber_ShouldSetBothProperties()
    {
        DelimitedFormatException ex = new("parse error", 5);

        Assert.AreEqual("parse error", ex.Message);
        Assert.AreEqual(5, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedFormatException" /> is derived from <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Type_ShouldDeriveFromFormatException()
    {
        DelimitedFormatException ex = new();

        Assert.IsInstanceOfType<FormatException>(ex);
    }

}
