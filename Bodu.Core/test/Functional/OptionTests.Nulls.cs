// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.Nulls.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies the strict/lenient split: the Some factory rejects <see langword="null" /> while the implicit
    /// conversion maps the same <see langword="null" /> to None.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenLiftedStrictlyVersusLeniently_ShouldThrowVersusProduceNone()
    {
        string? value = null;

        Option<string> lenient = value;
        Assert.IsTrue(lenient.IsNone);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Option<string>.Some(value!);
        });
    }

    /// <summary>
    /// Verifies that no combinator can produce an option carrying <see langword="null" />: a null projection maps to
    /// None, so a present value is always non-null.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenNullFlowsThroughCombinators_ShouldNeverProduceSomeNull()
    {
        var viaMap = Option<int>.Some(1).Map<string>(_ => null!);
        var viaCompanion = Option.FromNullable((int?)null);

        Assert.IsTrue(viaMap.IsNone);
        Assert.IsTrue(viaCompanion.IsNone);
    }
}
