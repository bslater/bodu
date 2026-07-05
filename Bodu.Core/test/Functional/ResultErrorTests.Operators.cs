// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultErrorTests.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultErrorTests
{
    /// <summary>
    /// Verifies that the implicit conversion from string produces the same error as FromMessage with no code.
    /// </summary>
    [TestMethod]
    public void ImplicitConversion_WhenMessageIsNotNull_ShouldMatchFromMessage()
    {
        ResultError error = "boom";

        Assert.AreEqual(ResultError.FromMessage("boom"), error);
        Assert.IsNull(error.Code);
    }

    /// <summary>
    /// Verifies that the implicit conversion rejects a <see langword="null" /> string with
    /// <see cref="ArgumentNullException" /> rather than producing a default error.
    /// </summary>
    [TestMethod]
    public void ImplicitConversion_WhenMessageIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ResultError _ = (string)null!;
        });

        Assert.AreEqual("message", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the equality operators agree with <see cref="ResultError.Equals(ResultError)" />.
    /// </summary>
    [TestMethod]
    public void EqualityOperators_WhenComparingErrors_ShouldMatchEqualsSemantics()
    {
        Assert.IsTrue(ResultError.FromMessage("boom") == ResultError.FromMessage("boom"));
        Assert.IsTrue(ResultError.FromMessage("boom") != ResultError.FromMessage("bang"));
        Assert.IsTrue(default(ResultError) == ResultError.FromMessage(string.Empty));
    }
}
