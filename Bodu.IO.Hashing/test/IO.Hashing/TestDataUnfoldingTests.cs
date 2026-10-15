// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TestDataUnfoldingTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.IO.Hashing;

/// <summary>
/// Verifies that every data-driven test in this assembly that asks MSTest to give each row its own test supplies rows
/// MSTest can serialize, so that it does not run them folded into one test instead.
/// </summary>
[TestClass]
public sealed class TestDataUnfoldingTests
{
    /// <summary>
    /// Verifies that no test method in this assembly asks to unfold rows MSTest cannot serialize: MSTest folds those
    /// silently, and then one row that times out cancels every row after it.
    /// </summary>
    [TestMethod]
    public void TestMethods_WhenTheyAskToUnfoldTheirRows_ShouldSupplyRowsMSTestCanSerialize()
    {
        IReadOnlyList<string> problems = UnfoldedTestData.FindRowsThatCannotUnfold(typeof(TestDataUnfoldingTests).Assembly);

        Assert.IsEmpty(problems, string.Join(Environment.NewLine, problems));
    }
}
