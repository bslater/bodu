// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that the debugger proxy surfaces the dictionary entries in iteration order.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenPopulated_ShouldExposeEntriesInOrder()
    {
        var dictionary = CreatePopulated();
        var view = new SequencedDictionaryDebugView<string, int>(dictionary);

        CollectionAssert.AreEqual(
            new[]
            {
                new KeyValuePair<string, int>("a", 1),
                new KeyValuePair<string, int>("b", 2),
                new KeyValuePair<string, int>("c", 3),
            },
            view.Items);
    }

    /// <summary>
    /// Verifies that the debugger proxy throws when constructed with a <see langword="null" /> dictionary.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenDictionaryIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new SequencedDictionaryDebugView<string, int>(null!);
        });
    }
}
