// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyContextTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstPropertyContext.Contains" />.
/// </summary>
public partial class PstPropertyContextTests
{
    /// <summary>
    /// Verifies that containment reports presence without resolving the value's payload.
    /// </summary>
    [TestMethod]
    public void Contains_WhenPropertyIsPresent_ShouldReturnTrue()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsTrue(context.Contains(StringId));
            Assert.IsTrue(context.Contains(NullId));
        }
    }

    /// <summary>
    /// Verifies that an absent property reports no containment.
    /// </summary>
    [TestMethod]
    public void Contains_WhenPropertyIsAbsent_ShouldReturnFalse()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsFalse(context.Contains(0x7FFF));
        }
    }
}
