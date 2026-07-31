// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeIdTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstNodeIdTests
{
    /// <summary>
    /// Verifies that equality follows the raw value across the operators, <see cref="PstNodeId.Equals(PstNodeId)" />,
    /// and the boxed overload.
    /// </summary>
    [TestMethod]
    public void Equality_WhenSameValue_ShouldBeEqual()
    {
        var left = new PstNodeId(0x122);
        var right = new PstNodeId(PstNodeType.NormalFolder, 9);

        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(left.Equals((object)right));
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that distinct values compare unequal, and that a foreign type never equals an identifier.
    /// </summary>
    [TestMethod]
    public void Equality_WhenDifferentValue_ShouldBeUnequal()
    {
        var left = new PstNodeId(0x21);
        var right = new PstNodeId(0x61);

        Assert.IsFalse(left == right);
        Assert.IsTrue(left != right);
        Assert.IsFalse(left.Equals(right));
        Assert.IsFalse(left.Equals("0x21"));
        Assert.IsFalse(left.Equals(null));
    }
}
