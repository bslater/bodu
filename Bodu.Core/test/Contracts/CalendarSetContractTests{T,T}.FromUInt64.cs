// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.FromUInt64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that bit <c>n</c> selects the value at index <c>n</c> of the domain, and only that value.
    /// </summary>
    [TestMethod]
    public void FromUInt64_WhenOneBitIsSet_ShouldSelectTheValueAtThatIndex()
    {
        for (int bit = 0; bit < DomainSize; bit++)
        {
            int selected = bit;

            AssertSelects(i => i == selected, FromUInt64(1UL << bit), $"bit {bit}");
        }
    }

    /// <summary>
    /// Verifies that <c>FromUInt64</c> and <c>ToUInt64</c> round-trip any bits within the domain.
    /// </summary>
    [TestMethod]
    public void FromUInt64_WhenBitsAreInDomain_ShouldRoundTripThroughToUInt64()
    {
        var random = new Random(SampleSeed);
        ulong[] bits = [0, 1, DomainBits, DomainBits >> 1, .. Enumerable.Range(0, 64).Select(_ => (ulong)random.NextInt64() & DomainBits)];

        foreach (ulong value in bits)
            Assert.AreEqual(value, ToUInt64(FromUInt64(value)), $"bits {value:X}");
    }

    /// <summary>
    /// Verifies that <c>FromUInt64</c> throws <see cref="ArgumentOutOfRangeException" /> naming its parameter when a
    /// bit above the domain is set, since such a bit selects no value.
    /// </summary>
    [TestMethod]
    public void FromUInt64_WhenBitAboveDomainIsSet_ShouldThrowArgumentOutOfRangeException()
    {
        IEnumerable<ulong> invalid = Enumerable.Range(DomainSize, 64 - DomainSize)
            .Select(bit => 1UL << bit)
            .Append(ulong.MaxValue);

        foreach (ulong bits in invalid)
        {
            var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = FromUInt64(bits);
            }, $"bits {bits:X}");

            Assert.AreEqual("bits", ex.ParamName, $"bits {bits:X}");
        }
    }
}
