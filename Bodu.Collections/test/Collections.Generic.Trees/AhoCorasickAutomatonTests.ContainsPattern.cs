// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonTests.ContainsPattern.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class AhoCorasickAutomatonTests
{
    /// <summary>
    /// Verifies that every built pattern is reported as contained.
    /// </summary>
    [TestMethod]
    public void ContainsPattern_WhenPatternWasBuilt_ShouldReturnTrue()
    {
        var sut = CreateClassic();

        Assert.IsTrue(sut.ContainsPattern("he"));
        Assert.IsTrue(sut.ContainsPattern("she"));
        Assert.IsTrue(sut.ContainsPattern("his"));
        Assert.IsTrue(sut.ContainsPattern("hers"));
    }

    /// <summary>
    /// Verifies that an absent pattern — including a strict prefix of a built pattern — is not reported as contained.
    /// </summary>
    [TestMethod]
    public void ContainsPattern_WhenPatternAbsent_ShouldReturnFalse()
    {
        var sut = AhoCorasickAutomaton.Build(["hers"]);

        Assert.IsFalse(sut.ContainsPattern("her"));
        Assert.IsFalse(sut.ContainsPattern("herself"));
        Assert.IsFalse(sut.ContainsPattern("xyz"));
    }

    /// <summary>
    /// Verifies that the empty string is never contained, because empty patterns are rejected at build time.
    /// </summary>
    [TestMethod]
    public void ContainsPattern_WhenPatternIsEmpty_ShouldReturnFalse()
    {
        var sut = CreateClassic();

        Assert.IsFalse(sut.ContainsPattern(string.Empty));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> pattern throws <see cref="ArgumentNullException" /> naming
    /// <c>pattern</c>.
    /// </summary>
    [TestMethod]
    public void ContainsPattern_WhenPatternIsNull_ShouldThrowForPattern()
    {
        var sut = CreateClassic();

        Assert.AreEqual(
            "pattern",
            Assert.ThrowsExactly<ArgumentNullException>(() => sut.ContainsPattern(null!)).ParamName);
    }
}
