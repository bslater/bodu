// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetTests.CreateMask.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu;

public sealed partial class CalendarValueSetTests
{
    /// <summary>
    /// Verifies that the mask of a domain of every size from 0 to 64 sets exactly its low bits, the 64-value domain
    /// included, where a shift by 64 bits would wrap to a shift by none.
    /// </summary>
    [TestMethod]
    public void CreateMask_WhenCountIsFromZeroToSixtyFour_ShouldSetExactlyTheLowBits()
    {
        for (int count = 0; count <= 64; count++)
        {
            ulong mask = CalendarValueSet.CreateMask(count);
            ulong expected = 0;
            for (int bit = 0; bit < count; bit++)
                expected |= 1UL << bit;

            Assert.AreEqual(expected, mask, $"count {count}");
            Assert.AreEqual(count, BitOperations.PopCount(mask), $"count {count}");
        }
    }

    /// <summary>
    /// Verifies that the mask of a 64-value domain sets every bit, through both the size and the bounds overloads.
    /// </summary>
    [TestMethod]
    public void CreateMask_WhenCountIsSixtyFour_ShouldSetEveryBit()
    {
        Assert.AreEqual(ulong.MaxValue, CalendarValueSet.CreateMask(64));
        Assert.AreEqual(ulong.MaxValue, CalendarValueSet.DomainMask(0, 63));
        Assert.AreEqual(ulong.MaxValue, CalendarValueSet.DomainMask(1, 64));
    }
}
