// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyTagTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyTagTests
{
    /// <summary>
    /// Verifies that a representative tag formats in the canonical hexadecimal form.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSubjectUnicodeTag_ShouldReturnCanonicalHex()
    {
        Assert.AreEqual("0x0037001F", new MapiPropertyTag(0x0037, MapiPropertyType.Unicode).ToString());
    }

    /// <summary>
    /// Verifies that every base type, the multi-valued flag, and the identifier boundaries format to their pinned
    /// canonical hexadecimal text.
    /// </summary>
    /// <param name="kat">The known-answer row of raw tag value and expected text.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(
        nameof(ToStringKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ToString_WhenPinnedTagValue_ShouldReturnExpectedText(ValidKat<uint, string> kat)
    {
        Assert.AreEqual(kat.Expected, new MapiPropertyTag(kat.Input).ToString());
    }
}
