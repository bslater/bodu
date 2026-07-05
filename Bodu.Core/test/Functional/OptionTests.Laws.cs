// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.Laws.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies the functor identity law: mapping the identity function leaves the option unchanged.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(42)]
    [DataRow(-7)]
    public void Map_WhenSelectorIsIdentity_ShouldSatisfyFunctorIdentityLaw(int value)
    {
        var option = Option<int>.Some(value);

        Assert.AreEqual(option, option.Map(x => x));
        Assert.AreEqual(Option<int>.None, Option<int>.None.Map(x => x));
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
        var option = Option<int>.Some(value);

        Assert.AreEqual(option.Map(F).Map(G), option.Map(x => G(F(x))));
    }

    /// <summary>
    /// Verifies the monad left-identity law: binding a function over a fresh Some equals applying the function.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(42)]
    [DataRow(-7)]
    public void Bind_WhenAppliedToFreshSome_ShouldSatisfyMonadLeftIdentityLaw(int value)
    {
        static Option<int> F(int x) => Option<int>.Some(x + 1);

        Assert.AreEqual(F(value), Option<int>.Some(value).Bind(F));
    }

    /// <summary>
    /// Verifies the monad right-identity law: binding the Some constructor leaves the option unchanged.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(42)]
    [DataRow(-7)]
    public void Bind_WhenSelectorIsSomeConstructor_ShouldSatisfyMonadRightIdentityLaw(int value)
    {
        var option = Option<int>.Some(value);

        Assert.AreEqual(option, option.Bind(Option<int>.Some));
        Assert.AreEqual(Option<int>.None, Option<int>.None.Bind(Option<int>.Some));
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
        static Option<int> F(int x) => Option<int>.Some(x + 1);
        static Option<int> G(int x) => x % 2 == 0 ? Option<int>.Some(x * 2) : Option<int>.None;
        var option = Option<int>.Some(value);

        Assert.AreEqual(option.Bind(F).Bind(G), option.Bind(x => F(x).Bind(G)));
    }
}
