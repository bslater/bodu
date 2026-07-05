// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that a success renders as <c>Success(value)</c>.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSuccess_ShouldRenderSuccessWithValue()
    {
        Assert.AreEqual("Success(42)", Result.Success(42).ToString());
    }

    /// <summary>
    /// Verifies that a failure renders as <c>Failure(error)</c> using the error's own formatting.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFailure_ShouldRenderFailureWithError()
    {
        Assert.AreEqual("Failure(boom)", Result.Failure<int>("boom").ToString());
        Assert.AreEqual("Failure(Code: boom)", Result.Failure<int>(ResultError.FromMessage("boom", "Code")).ToString());
    }

    /// <summary>
    /// Verifies that <c>default(Result&lt;T&gt;)</c> renders as a failure carrying the empty error.
    /// </summary>
    [TestMethod]
    public void ToString_WhenDefault_ShouldRenderFailureWithEmptyError()
    {
        Assert.AreEqual("Failure()", default(Result<int>).ToString());
    }
}
