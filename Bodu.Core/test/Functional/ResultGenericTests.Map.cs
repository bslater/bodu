// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.Map.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that Map projects the carried value into a new successful result.
    /// </summary>
    [TestMethod]
    public void Map_WhenSuccess_ShouldProjectValue()
    {
        var result = Result.Success("railway").Map(s => s.Length);

        Assert.AreEqual(Result.Success(7), result);
    }

    /// <summary>
    /// Verifies that Map propagates the original error without invoking the selector when the result is a failure.
    /// </summary>
    [TestMethod]
    public void Map_WhenFailure_ShouldPropagateErrorWithoutInvokingSelector()
    {
        var error = ResultError.FromMessage("boom", "Code");
        var invoked = false;

        var result = Result.Failure<string>(error).Map(s =>
        {
            invoked = true;
            return s.Length;
        });

        Assert.IsFalse(invoked);
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(error, result.Error);
    }

    /// <summary>
    /// Verifies that Map rejects a <see langword="null" /> selector.
    /// </summary>
    [TestMethod]
    public void Map_WhenSelectorIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).Map<string>(null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }
}
