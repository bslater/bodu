// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedValueKindTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Behavioural tests for the <see cref="BencodedValueKind" /> enum. The enum's integer values are part of the
/// public contract, so this class pins them.
/// </summary>
[TestClass]
public sealed class BencodedValueKindTests
{

    /// <summary>
    /// Verifies that <see cref="BencodedValueKind" /> defines exactly four members covering the four Bencode
    /// productions.
    /// </summary>
    [TestMethod]
    public void BencodedValueKind_ShouldDefineExactlyFourMembers()
    {
        Assert.AreEqual(4, Enum.GetNames(typeof(BencodedValueKind)).Length);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedValueKind" /> defines the expected member names.
    /// </summary>
    [TestMethod]
    public void BencodedValueKind_ShouldDefineExpectedMemberNames()
    {
        CollectionAssert.AreEquivalent(
            new[] { "Integer", "String", "List", "Dictionary" },
            Enum.GetNames(typeof(BencodedValueKind)));
    }

    /// <summary>
    /// Verifies that the integer value of each <see cref="BencodedValueKind" /> member is stable, so callers can
    /// rely on the numeric encoding (e.g. for persistence or diagnostics).
    /// </summary>
    [TestMethod]
    public void BencodedValueKind_ShouldDefineExpectedNumericValues()
    {
        Assert.AreEqual(0, (int)BencodedValueKind.Integer);
        Assert.AreEqual(1, (int)BencodedValueKind.String);
        Assert.AreEqual(2, (int)BencodedValueKind.List);
        Assert.AreEqual(3, (int)BencodedValueKind.Dictionary);
    }

}
