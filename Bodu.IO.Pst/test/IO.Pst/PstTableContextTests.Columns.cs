// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableContextTests.Columns.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstTableContext.Columns" />.
/// </summary>
public partial class PstTableContextTests
{
    /// <summary>
    /// Verifies that the columns surface each descriptor's property identifier, wire type, and cell width in stored
    /// order.
    /// </summary>
    [TestMethod]
    public void Columns_WhenTableDeclaresColumns_ShouldSurfaceDescriptorsInStoredOrder()
    {
        (PstFile file, PstTableContext context) = OpenSharedContext();
        using (file)
        {
            Assert.AreEqual(5, context.Columns.Count);

            PstTableColumn stringColumn = context.Columns[2];
            Assert.AreEqual(0x2222, stringColumn.PropertyId);
            Assert.AreEqual(0x001F, stringColumn.WireType);
            Assert.AreEqual(4, stringColumn.Width);
        }
    }
}
