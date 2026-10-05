// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetContractTests{T}.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;

namespace Bodu.Contracts;

public abstract partial class CalendarValueSetContractTests<TSet>
{
    /// <summary>
    /// Verifies that <c>ToString</c> writes the canonical text of each canonical row's set.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSetHasCanonicalRow_ShouldWriteCanonicalText()
    {
        foreach (ValidKat<string, TSet> kat in CanonicalCases())
            Assert.AreEqual(kat.Input, kat.Expected.ToString(), kat.Name);
    }

    /// <summary>
    /// Verifies that <c>ToString</c> writes the empty string for the empty set.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSetIsEmpty_ShouldReturnEmptyString()
    {
        Assert.AreEqual(string.Empty, Empty.ToString());
    }

    /// <summary>
    /// Verifies that <c>ToString</c> writes a set of one value as that value alone.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSetSelectsOneValue_ShouldWriteThatValue()
    {
        foreach (int value in Domain)
            Assert.AreEqual(value.ToString(CultureInfo.InvariantCulture), Create(value).ToString());
    }

    /// <summary>
    /// Verifies that <c>ToString</c> writes two consecutive values as a range rather than a list.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTwoValuesAreConsecutive_ShouldWriteRange()
    {
        string expected = string.Create(CultureInfo.InvariantCulture, $"{Minimum}-{Minimum + 1}");

        Assert.AreEqual(expected, Create(Minimum, Minimum + 1).ToString());
    }

    /// <summary>
    /// Verifies that <c>ToString</c> writes each sample set as a plain reading of the format does, with the values
    /// read from the set's bits.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSetSelectsValues_ShouldMatchReferenceFormat()
    {
        foreach (TSet set in SampleSets())
        {
            string expected = FormatReference(ValuesOf(ToUInt64(set)));

            Assert.AreEqual(expected, set.ToString(), $"set {ToUInt64(set):X}");
        }
    }

    /// <summary>
    /// Verifies that <c>ToString</c> writes the same text whatever the current culture.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCurrentCultureChanges_ShouldWriteSameText()
    {
        TSet set = SampleSets().Last();
        string invariant = FormatReference(ValuesOf(ToUInt64(set)));
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            foreach (string name in new[] { "ar-SA", "fa-IR", "fr-FR", "hi-IN" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);

                Assert.AreEqual(invariant, set.ToString(), name);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
