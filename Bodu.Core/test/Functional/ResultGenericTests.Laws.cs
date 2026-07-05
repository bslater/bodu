// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.Laws.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies the functor identity law: mapping the identity function leaves the result unchanged.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(42)]
    [DataRow(-7)]
    public void Map_WhenSelectorIsIdentity_ShouldSatisfyFunctorIdentityLaw(int value)
    {
        var result = Result.Success(value);

        Assert.AreEqual(result, result.Map(x => x));
        Assert.AreEqual(Result.Failure<int>("boom"), Result.Failure<int>("boom").Map(x => x));
    }

    /// <summary>
    /// Verifies the functor composition law: mapping two functions in sequence equals mapping their composition.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(42)]
    [DataRow(-7)]
    public void Map_WhenSelectorsAreComposed_ShouldSatisfyFunctorCompositionLaw(int value)
    {
        static int F(int x) => x + 1;
        static int G(int x) => x * 2;
        var result = Result.Success(value);

        Assert.AreEqual(result.Map(F).Map(G), result.Map(x => G(F(x))));
    }

    /// <summary>
    /// Verifies the monad left-identity law: binding a function over a fresh Success equals applying the function.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(42)]
    [DataRow(-7)]
    public void Bind_WhenAppliedToFreshSuccess_ShouldSatisfyMonadLeftIdentityLaw(int value)
    {
        static Result<int> F(int x) => Result.Success(x + 1);

        Assert.AreEqual(F(value), Result.Success(value).Bind(F));
    }

    /// <summary>
    /// Verifies the monad right-identity law: binding the Success constructor leaves the result unchanged.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(42)]
    [DataRow(-7)]
    public void Bind_WhenSelectorIsSuccessConstructor_ShouldSatisfyMonadRightIdentityLaw(int value)
    {
        var result = Result.Success(value);

        Assert.AreEqual(result, result.Bind(Result.Success));
        Assert.AreEqual(Result.Failure<int>("boom"), Result.Failure<int>("boom").Bind(Result.Success));
    }

    /// <summary>
    /// Verifies the monad associativity law: nested binds equal sequential binds regardless of grouping.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(42)]
    [DataRow(-7)]
    public void Bind_WhenBindsAreRegrouped_ShouldSatisfyMonadAssociativityLaw(int value)
    {
        static Result<int> F(int x) => Result.Success(x + 1);
        static Result<int> G(int x) => x % 2 == 0 ? Result.Success(x * 2) : Result.Failure<int>("odd");
        var result = Result.Success(value);

        Assert.AreEqual(result.Bind(F).Bind(G), result.Bind(x => F(x).Bind(G)));
    }
}
