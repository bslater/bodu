// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyContextTests.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstPropertyContext.Count" />.
/// </summary>
public partial class PstPropertyContextTests
{
    /// <summary>
    /// Verifies that the count reflects every record the context carries.
    /// </summary>
    [TestMethod]
    public void Count_WhenContextCarriesProperties_ShouldReportRecordCount()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.AreEqual(9, context.Count);
        }
    }

    /// <summary>
    /// Verifies that an empty property context — one whose tree has no records — reports a count of zero.
    /// </summary>
    [TestMethod]
    public void Count_WhenContextIsEmpty_ShouldReportZero()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();
        _ = ltp.AddPropertyContext();
        ltp.AddHeapNode(builder, NodeId);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);

        Assert.AreEqual(0, file.GetNode(new PstNodeId(NodeId)).ReadPropertyContext().Count);
    }
}
