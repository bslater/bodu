// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.GetValueOrDefault.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that the parameterless overload returns the carried value for a success and the type default for a
    /// failure.
    /// </summary>
    [TestMethod]
    public void GetValueOrDefault_WhenParameterless_ShouldReturnValueOrTypeDefault()
    {
        Assert.AreEqual(5, Result.Success(5).GetValueOrDefault());
        Assert.AreEqual(0, Result.Failure<int>("boom").GetValueOrDefault());
        Assert.IsNull(Result.Failure<string>("boom").GetValueOrDefault());
    }

    /// <summary>
    /// Verifies that the fallback-value overload returns the carried value for a success and the fallback for a
    /// failure.
    /// </summary>
    [TestMethod]
    public void GetValueOrDefault_WhenFallbackValueSupplied_ShouldReturnValueOrFallback()
    {
        Assert.AreEqual(5, Result.Success(5).GetValueOrDefault(99));
        Assert.AreEqual(99, Result.Failure<int>("boom").GetValueOrDefault(99));
    }

    /// <summary>
    /// Verifies that the factory overload invokes the factory only for a failure.
    /// </summary>
    [TestMethod]
    public void GetValueOrDefault_WhenFactorySupplied_ShouldInvokeFactoryOnlyWhenFailure()
    {
        var invoked = false;

        var fromSuccess = Result.Success(5).GetValueOrDefault(() =>
        {
            invoked = true;
            return 99;
        });

        Assert.AreEqual(5, fromSuccess);
        Assert.IsFalse(invoked);
        Assert.AreEqual(99, Result.Failure<int>("boom").GetValueOrDefault(() => 99));
    }

    /// <summary>
    /// Verifies that the factory overload rejects a <see langword="null" /> factory.
    /// </summary>
    [TestMethod]
    public void GetValueOrDefault_WhenFactoryIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).GetValueOrDefault((Func<int>)null!);
        });

        Assert.AreEqual("defaultFactory", ex.ParamName);
    }
}
