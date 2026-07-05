// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonGenericTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class AhoCorasickAutomatonGenericTests
{
    /// <summary>
    /// Verifies that <see cref="AhoCorasickAutomatonDebugView{TValue}.Items" /> exposes every pattern with its value.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldExposeEveryPatternValuePair()
    {
        var sut = AhoCorasickAutomaton<int>.Build(
        [
            new KeyValuePair<string, int>("he", 1),
            new KeyValuePair<string, int>("she", 2),
        ]);

        var view = new AhoCorasickAutomatonDebugView<int>(sut);

        CollectionAssert.AreEqual(
            new[]
            {
                new KeyValuePair<string, int>("he", 1),
                new KeyValuePair<string, int>("she", 2),
            },
            view.Items);
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
            _ = new AhoCorasickAutomatonDebugView<int>(null!);
        });

        Assert.AreEqual("automaton", ex.ParamName);
    }
}
