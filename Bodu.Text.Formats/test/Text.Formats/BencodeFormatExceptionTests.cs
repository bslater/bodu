// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeFormatExceptionTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Behavioural tests for <see cref="BencodeFormatException" />.
/// </summary>
[TestClass]
public sealed class BencodeFormatExceptionTests
{
    /// <summary>
    /// Verifies that the parameterless constructor creates an instance that derives from
    /// <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void ParameterlessConstructor_ShouldProduceFormatExceptionSubclass()
    {
        BencodeFormatException ex = new();

        Assert.IsInstanceOfType<FormatException>(ex);
    }

    /// <summary>
    /// Verifies that the message-only constructor preserves the supplied message.
    /// </summary>
    [TestMethod]
    public void MessageConstructor_ShouldPreserveMessage()
    {
        BencodeFormatException ex = new("custom message");

        Assert.AreEqual("custom message", ex.Message);
    }

    /// <summary>
    /// Verifies that the message+inner constructor preserves both the message and the inner exception.
    /// </summary>
    [TestMethod]
    public void MessageAndInnerConstructor_ShouldPreserveBoth()
    {
        InvalidOperationException inner = new("inner");

        BencodeFormatException ex = new("outer", inner);

        Assert.AreEqual("outer", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }
}
