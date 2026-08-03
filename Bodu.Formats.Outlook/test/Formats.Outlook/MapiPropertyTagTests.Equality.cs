// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyTagTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyTagTests
{
    /// <summary>
    /// Verifies that tags with the same raw value are equal through every equality surface.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameRawValue_ShouldBeEqual()
    {
        var left = new MapiPropertyTag(0x0037, MapiPropertyType.Unicode);
        var right = new MapiPropertyTag(0x0037001Fu);

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(left.Equals((object)right));
        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that tags sharing an identifier but differing in type are unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameIdDifferentType_ShouldBeUnequal()
    {
        var unicode = new MapiPropertyTag(0x0037, MapiPropertyType.Unicode);
        var ansi = new MapiPropertyTag(0x0037, MapiPropertyType.String8);

        Assert.IsFalse(unicode.Equals(ansi));
        Assert.IsFalse(unicode == ansi);
        Assert.IsTrue(unicode != ansi);
    }

    /// <summary>
    /// Verifies that a tag is unequal to <see langword="null" /> and to values of other types.
    /// </summary>
    [TestMethod]
    public void Equals_WhenOtherObjectShape_ShouldBeUnequal()
    {
        var tag = new MapiPropertyTag(0x0037, MapiPropertyType.Unicode);

        Assert.IsFalse(tag.Equals(null));
        Assert.IsFalse(tag.Equals(0x0037001Fu));
    }
}
