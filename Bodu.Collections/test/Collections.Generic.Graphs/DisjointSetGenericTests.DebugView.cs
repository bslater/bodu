// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DisjointSetGenericTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Graphs;

public sealed partial class DisjointSetGenericTests
{
    /// <summary>
    /// Verifies that <see cref="DisjointSetDebugView{T}.Items" /> groups the members by the set they belong to
    /// after a union.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldGroupMembersByRepresentative()
    {
        var sut = new DisjointSet<string>();
        sut.Add("a");
        sut.Add("b");
        sut.Add("c");
        sut.Union("a", "c");

        var view = new DisjointSetDebugView<string>(sut);

        Assert.AreEqual(sut.SetCount, view.Items.Length);
        Assert.AreEqual(3, view.Items.Sum(group => group.Length));
        Assert.ContainsSingle(view.Items.Where(group => group.Length == 2));
    }

    /// <summary>
    /// Verifies that <see cref="DisjointSetDebugView{T}.Items" /> reports one singleton group per member when
    /// nothing has been unioned.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_WhenNoUnionPerformed_ShouldReportSingletonGroups()
    {
        var sut = new DisjointSet<string>();
        sut.Add("a");
        sut.Add("b");

        var view = new DisjointSetDebugView<string>(sut);

        Assert.AreEqual(2, view.Items.Length);
        Assert.IsTrue(view.Items.All(group => group.Length == 1));
    }

    /// <summary>
    /// Verifies that constructing <see cref="DisjointSetDebugView{T}" /> with a <see langword="null" /> set
    /// throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenSetIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DisjointSetDebugView<string>(null!);
        });

        Assert.AreEqual("set", ex.ParamName);
    }
}
