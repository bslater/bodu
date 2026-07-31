// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyTagTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyTagTests
{
    /// <summary>
    /// Verifies that the identifier-and-type constructor composes the raw value with the identifier in the high word
    /// and the type in the low word.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIdAndType_ShouldComposeRawValue()
    {
        var tag = new MapiPropertyTag(0x0037, MapiPropertyType.Unicode);

        Assert.AreEqual(0x0037001Fu, tag.Value);
        Assert.AreEqual((ushort)0x0037, tag.Id);
        Assert.AreEqual(MapiPropertyType.Unicode, tag.Type);
    }

    /// <summary>
    /// Verifies that the raw-value constructor preserves the value verbatim and decomposes it into identifier and
    /// type.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRawValue_ShouldDecomposeIdAndType()
    {
        var tag = new MapiPropertyTag(0x3701000Du);

        Assert.AreEqual(0x3701000Du, tag.Value);
        Assert.AreEqual((ushort)0x3701, tag.Id);
        Assert.AreEqual(MapiPropertyType.Object, tag.Type);
    }

    /// <summary>
    /// Verifies that a raw value carrying the multi-valued flag reports the base type with the flag stripped.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRawValueCarriesMultiValuedFlag_ShouldReportBaseType()
    {
        var tag = new MapiPropertyTag(0x0037101Fu);

        Assert.AreEqual(MapiPropertyType.Unicode, tag.Type);
        Assert.IsTrue(tag.IsMultiValued);
        Assert.AreEqual(0x0037101Fu, tag.Value);
    }
}
