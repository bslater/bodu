// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.Nulls.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies the strict rule: the Success factory rejects <see langword="null" /> so a successful result can
    /// never carry a null value.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenSuccessCreatedWithNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success<string>(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> Map projection throws <see cref="ArgumentNullException" /> from the
    /// strict Success factory rather than producing a success carrying null — unlike
    /// <see cref="Option{T}.Map{TResult}(Func{T, TResult})" />, Result has no lenient lift.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenMapProjectsToNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).Map<string>(_ => null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
