// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultErrorTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

[TestClass]
public sealed partial class ResultErrorTests
{
    /// <summary>
    /// Verifies that FromMessage stores the message and code and carries no exception, exercising the primary
    /// construction path.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void FromMessage_WhenMessageAndCodeSupplied_ShouldStoreBoth()
    {
        var error = ResultError.FromMessage("The file was not found.", "IO.NotFound");

        Assert.AreEqual("The file was not found.", error.Message);
        Assert.AreEqual("IO.NotFound", error.Code);
        Assert.IsNull(error.Exception);
    }

    /// <summary>
    /// Verifies that FromMessage defaults the code to <see langword="null" /> when none is supplied.
    /// </summary>
    [TestMethod]
    public void FromMessage_WhenCodeOmitted_ShouldStoreNullCode()
    {
        var error = ResultError.FromMessage("boom");

        Assert.AreEqual("boom", error.Message);
        Assert.IsNull(error.Code);
    }

    /// <summary>
    /// Verifies that FromMessage rejects a <see langword="null" /> message with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void FromMessage_WhenMessageIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ResultError.FromMessage(null!);
        });

        Assert.AreEqual("message", ex.ParamName);
    }

    /// <summary>
    /// Verifies that FromException adopts the exception's message and retains the exception instance.
    /// </summary>
    [TestMethod]
    public void FromException_WhenExceptionSupplied_ShouldStoreMessageAndException()
    {
        var source = new InvalidOperationException("kaboom");

        var error = ResultError.FromException(source, "Op.Failed");

        Assert.AreEqual("kaboom", error.Message);
        Assert.AreEqual("Op.Failed", error.Code);
        Assert.AreSame(source, error.Exception);
    }

    /// <summary>
    /// Verifies that FromException rejects a <see langword="null" /> exception with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void FromException_WhenExceptionIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ResultError.FromException(null!);
        });

        Assert.AreEqual("exception", ex.ParamName);
    }

    /// <summary>
    /// Verifies the total-default contract: <c>default(ResultError)</c> exposes an empty message, no code, and no
    /// exception.
    /// </summary>
    [TestMethod]
    public void Message_WhenDefault_ShouldReturnEmptyString()
    {
        var error = default(ResultError);

        Assert.AreEqual(string.Empty, error.Message);
        Assert.IsNull(error.Code);
        Assert.IsNull(error.Exception);
    }
}
