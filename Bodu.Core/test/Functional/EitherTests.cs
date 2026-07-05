// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

[TestClass]
public sealed partial class EitherTests
{
    /// <summary>
    /// Verifies that Left and Right values each dispatch Match to their own branch, exercising the primary
    /// disjoint-union path.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Either_WhenMatchedOnEachSide_ShouldDispatchToMatchingBranch()
    {
        var left = Either<int, string>.Left(21);
        var right = Either<int, string>.Right("text");

        var fromLeft = left.Match(l => $"number {l * 2}", r => $"text {r}");
        var fromRight = right.Match(l => $"number {l * 2}", r => $"text {r}");

        Assert.AreEqual("number 42", fromLeft);
        Assert.AreEqual("text text", fromRight);
    }
}
