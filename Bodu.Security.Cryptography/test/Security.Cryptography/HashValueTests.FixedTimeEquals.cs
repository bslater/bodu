// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashValueTests.FixedTimeEquals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class HashValueTests
{
    /// <summary>
    /// Verifies that <see cref="HashValue.FixedTimeEquals" /> returns <see langword="true" /> for identical bytes.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenBytesAreIdentical_ShouldReturnTrue()
    {
        var left = HashValue.FromBytes([0xAA, 0xBB, 0xCC]);
        var right = HashValue.FromBytes([0xAA, 0xBB, 0xCC]);

        Assert.IsTrue(left.FixedTimeEquals(right));
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.FixedTimeEquals" /> returns <see langword="false" /> when any byte differs.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenBytesDiffer_ShouldReturnFalse()
    {
        var left = HashValue.FromBytes([0xAA, 0xBB, 0xCC]);
        var right = HashValue.FromBytes([0xAA, 0xBB, 0xCD]);

        Assert.IsFalse(left.FixedTimeEquals(right));
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.FixedTimeEquals" /> returns <see langword="false" /> when the lengths differ.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenLengthsDiffer_ShouldReturnFalse()
    {
        var left = HashValue.FromBytes([0xAA, 0xBB]);
        var right = HashValue.FromBytes([0xAA, 0xBB, 0xCC]);

        Assert.IsFalse(left.FixedTimeEquals(right));
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.FixedTimeEquals" /> treats two empty values as equal.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenBothValuesAreEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(default(HashValue).FixedTimeEquals(HashValue.FromBytes([])));
    }
}
