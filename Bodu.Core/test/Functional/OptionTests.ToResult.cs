// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.ToResult.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that ToResult converts Some into a success carrying the contained value.
    /// </summary>
    [TestMethod]
    public void ToResult_WhenSome_ShouldProduceSuccessCarryingValue()
    {
        var result = Option<string>.Some("value").ToResult("missing");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("value", result.Value);
    }

    /// <summary>
    /// Verifies that ToResult converts None into a failure carrying the supplied error.
    /// </summary>
    [TestMethod]
    public void ToResult_WhenNone_ShouldProduceFailureCarryingGivenError()
    {
        var error = ResultError.FromMessage("missing", "Lookup.Miss");

        var result = Option<string>.None.ToResult(error);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(error, result.Error);
    }
}
