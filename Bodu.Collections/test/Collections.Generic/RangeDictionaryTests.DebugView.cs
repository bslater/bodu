// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="RangeDictionaryDebugView{TKey, TValue}.Items" /> exposes every stored range entry.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldExposeEveryEntry()
    {
        var sut = new RangeDictionary<int, string>();
        sut.Add(0, 10, "a");
        sut.Add(10, 20, "b");

        var view = new RangeDictionaryDebugView<int, string>(sut);

        CollectionAssert.AreEqual(sut.ToArray(), view.Items);
        Assert.AreEqual(2, view.Items.Length);
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionaryDebugView{TKey, TValue}.Items" /> is empty for an empty dictionary.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_WhenDictionaryIsEmpty_ShouldBeEmpty()
    {
        var view = new RangeDictionaryDebugView<int, string>(new RangeDictionary<int, string>());

        Assert.AreEqual(0, view.Items.Length);
    }

    /// <summary>
    /// Verifies that the debug view constructor throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> dictionary.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenDictionaryIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new RangeDictionaryDebugView<int, string>(null!);
        });

        Assert.AreEqual("rangeDictionary", ex.ParamName);
    }
}
