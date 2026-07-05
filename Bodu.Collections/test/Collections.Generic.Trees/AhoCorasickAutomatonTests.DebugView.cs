// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class AhoCorasickAutomatonTests
{
    /// <summary>
    /// Verifies that <see cref="AhoCorasickAutomatonDebugView.Items" /> exposes every distinct pattern.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldExposeEveryPattern()
    {
        var sut = CreateClassic();

        var view = new AhoCorasickAutomatonDebugView(sut);

        CollectionAssert.AreEqual(new[] { "he", "she", "his", "hers" }, view.Items);
    }

    /// <summary>
    /// Verifies that the debug view constructor throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> automaton.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenAutomatonIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new AhoCorasickAutomatonDebugView(null!);
        });

        Assert.AreEqual("automaton", ex.ParamName);
    }
}
