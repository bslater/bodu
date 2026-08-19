// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyContextTests.GetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstPropertyContext.GetValue" />: the throwing retrieval counterpart.
/// </summary>
public partial class PstPropertyContextTests
{
    /// <summary>
    /// Verifies that a present property's value is returned.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenPropertyIsPresent_ShouldReturnValue()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.AreEqual("Sample1", context.GetValue(StringId).GetString());
        }
    }

    /// <summary>
    /// Verifies that an absent property throws <see cref="PstFileException" /> naming the property.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenPropertyIsAbsent_ShouldThrowPstFileException()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            var ex = Assert.ThrowsExactly<PstFileException>(() =>
            {
                _ = context.GetValue(0x7FFF);
            });

            Assert.IsTrue(ex.Message.Contains("0x7FFF", StringComparison.Ordinal));
        }
    }
}
