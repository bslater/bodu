// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyTagTests.IsMultiValued.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyTagTests
{
    /// <summary>
    /// Verifies that the multi-valued flag in the type word is reported by <see cref="MapiPropertyTag.IsMultiValued" />.
    /// </summary>
    /// <param name="testName">The scenario label.</param>
    /// <param name="value">The raw tag value.</param>
    /// <param name="expected">The expected flag state.</param>
    [TestMethod]
    [DataRow("SingleValuedUnicode", 0x0037001Fu, false)]
    [DataRow("MultiValuedUnicode", 0x0037101Fu, true)]
    [DataRow("SingleValuedBinary", 0x37010102u, false)]
    [DataRow("MultiValuedBinary", 0x37011102u, true)]
    public void IsMultiValued_WhenTypeWordCarriesFlag_ShouldReflectFlag(string testName, uint value, bool expected)
    {
        _ = testName;

        Assert.AreEqual(expected, new MapiPropertyTag(value).IsMultiValued);
    }
}
