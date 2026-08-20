// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyValueTests.GetBoolean.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstPropertyValue.GetBoolean" />.
/// </summary>
public partial class PstPropertyValueTests
{
    /// <summary>
    /// Verifies that a zero byte reads as <see langword="false" /> and any nonzero byte as <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WhenWireTypeIsBoolean_ShouldMapZeroAndNonzero()
    {
        Assert.IsFalse(Value(0x000B, [0]).GetBoolean());
        Assert.IsTrue(Value(0x000B, [1]).GetBoolean());
        Assert.IsTrue(Value(0x000B, [0xFF]).GetBoolean());
    }

    /// <summary>
    /// Verifies that reading a mismatched wire type throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WhenWireTypeMismatches_ShouldThrowInvalidOperationException()
    {
        PstPropertyValue value = Value(0x0003, [1, 0, 0, 0]);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = value.GetBoolean());
    }
}
