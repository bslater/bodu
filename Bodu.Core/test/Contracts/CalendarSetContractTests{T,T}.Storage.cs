// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.Storage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that the set holds its canonical bitmap in a <see cref="ulong" /> field named <c>_bits</c>, the storage
    /// every Bodu calendar value set shares whatever the size of its domain.
    /// </summary>
    [TestMethod]
    public void Storage_WhenInspected_ShouldHoldTheBitmapInAUInt64Field()
    {
        FieldInfo? bits = typeof(TSet).GetField("_bits", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(bits, $"{typeof(TSet).Name} has no _bits field.");
        Assert.AreEqual(typeof(ulong), bits.FieldType, $"{typeof(TSet).Name}._bits is not a ulong.");
    }

    /// <summary>
    /// Verifies that the set occupies eight bytes, so that copying it, as the recurrence and calendar searches do on
    /// every candidate, moves one machine word.
    /// </summary>
    [TestMethod]
    public void Storage_WhenMeasured_ShouldBeEightBytes()
    {
        Assert.AreEqual(8, Unsafe.SizeOf<TSet>());
    }

    /// <summary>
    /// Verifies that the domain holds at most 64 values, the most a 64-bit bitmap can represent one bit each.
    /// </summary>
    [TestMethod]
    public void Storage_WhenTheDomainIsCounted_ShouldHoldAtMostSixtyFourValues()
    {
        Assert.IsTrue(DomainSize is > 0 and <= 64, $"{typeof(TSet).Name} has {DomainSize} values.");
    }

    /// <summary>
    /// Verifies that every way of building a set leaves the bits outside the domain clear: the constructor,
    /// <c>FromUInt64</c>, <c>With</c>, <c>Without</c>, the binary operators, the complement, and parsing the default
    /// text.
    /// </summary>
    [TestMethod]
    public void Storage_WhenAnyPathBuildsASet_ShouldLeaveTheBitsOutsideTheDomainClear()
    {
        ulong outside = ~DomainBits;
        foreach ((TSet left, TSet right) in SamplePairs())
        {
            TElement[] values = ValuesOf(left);
            TSet[] built =
            [
                Create(values),
                FromUInt64(ToUInt64(left)),
                values.Aggregate(Empty, With),
                Domain.Except(values).Aggregate(All, Without),
                left & right,
                left | right,
                left ^ right,
                ~left,
                TSet.Parse(left.ToString()!, null),
            ];

            for (int i = 0; i < built.Length; i++)
                Assert.AreEqual(0UL, ToUInt64(built[i]) & outside, $"path {i} from {ToUInt64(left):X} and {ToUInt64(right):X}");
        }
    }

    /// <summary>
    /// Verifies that every way of reaching the same values gives the same bitmap and an equal set: the constructor,
    /// <c>FromUInt64</c>, <c>With</c> from the empty set, <c>Without</c> from the full set, the identities of the
    /// binary operators, a double complement, and parsing the default text.
    /// </summary>
    [TestMethod]
    public void Storage_WhenPathsReachTheSameValues_ShouldGiveTheSameBitmap()
    {
        foreach (TSet sample in SampleSets())
        {
            TElement[] values = ValuesOf(sample);
            ulong expected = ToUInt64(sample);
            TSet[] reached =
            [
                Create(values),
                FromUInt64(expected),
                values.Aggregate(Empty, With),
                Domain.Except(values).Aggregate(All, Without),
                sample | Empty,
                sample & All,
                sample ^ Empty,
                ~~sample,
                TSet.Parse(sample.ToString()!, null),
            ];

            for (int i = 0; i < reached.Length; i++)
            {
                Assert.AreEqual(expected, ToUInt64(reached[i]), $"path {i} to {expected:X}");
                Assert.IsTrue(reached[i] == sample, $"path {i} to {expected:X}");
            }
        }
    }

    /// <summary>
    /// Verifies that the set declares no implicit or explicit conversion, so that its bitmap crosses the public surface
    /// only through <c>FromUInt64</c> and <c>ToUInt64</c>.
    /// </summary>
    [TestMethod]
    public void Storage_WhenOperatorsAreListed_ShouldDeclareNoConversion()
    {
        string[] conversions = typeof(TSet)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name is "op_Implicit" or "op_Explicit")
            .Select(m => m.ToString()!)
            .ToArray();

        Assert.AreEqual(0, conversions.Length, string.Join("; ", conversions));
    }

    /// <summary>
    /// Returns the values of the domain a set selects, according to its <c>Contains</c>.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <returns>The selected values, in bit order.</returns>
    private TElement[] ValuesOf(TSet set) =>
        Domain.Where(value => Contains(set, value)).ToArray();
}
