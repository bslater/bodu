// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetContractTests{T}.TryParse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Contracts;

public abstract partial class CalendarValueSetContractTests<TSet>
{
    /// <summary>
    /// Verifies that <c>TryParse</c> answers <see langword="true" /> with the expected set for every valid row,
    /// canonical or not.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenTextIsValid_ShouldReturnTrueAndExpectedSet()
    {
        foreach (ValidKat<string, TSet> kat in CanonicalCases().Concat(LenientCases()))
        {
            bool parsed = TryParse(kat.Input, out TSet result);

            Assert.IsTrue(parsed, kat.Name);
            Assert.AreEqual(kat.Expected, result, kat.Name);
        }
    }

    /// <summary>
    /// Verifies that <c>TryParse</c> answers <see langword="false" />, without throwing, with the empty set for text
    /// that is not a list of values and ranges of the domain.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenTextIsMalformed_ShouldReturnFalseAndEmptySet()
    {
        foreach (InvalidKat<string> kat in MalformedCases())
        {
            bool parsed = TryParse(kat.Input, out TSet result);

            Assert.IsFalse(parsed, kat.Name);
            Assert.AreEqual(Empty, result, kat.Name);
        }
    }

    /// <summary>
    /// Verifies that <c>TryParse</c> reads exactly one <c>0</c> or <c>1</c> per value of the domain as the binary form,
    /// even where the text also reads as a list, and zeros and ones of any other length, or with whitespace, as a list.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenTextIsZerosAndOnes_ShouldReadTheBinaryFormOnlyAtTheDomainWidth()
    {
        foreach (ValidKat<string, TSet> kat in DetectionCases())
        {
            bool parsed = TryParse(kat.Input, out TSet result);

            Assert.IsTrue(parsed, kat.Name);
            Assert.AreEqual(kat.Expected, result, kat.Name);
        }
    }

    /// <summary>
    /// Verifies that <c>TryParse</c> answers <see langword="false" /> with the empty set for text that is neither the
    /// binary form nor a list, even when the text has the binary form's length.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenTextIsNeitherBinaryNorAList_ShouldReturnFalseAndEmptySet()
    {
        foreach (InvalidKat<string> kat in DetectionFailureCases())
        {
            bool parsed = TryParse(kat.Input, out TSet result);

            Assert.IsFalse(parsed, kat.Name);
            Assert.AreEqual(Empty, result, kat.Name);
        }
    }

    /// <summary>
    /// Verifies that <c>TryParse</c> answers <see langword="false" /> with the empty set for <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenTextIsNull_ShouldReturnFalseAndEmptySet()
    {
        bool parsed = TryParse(null, out TSet result);

        Assert.IsFalse(parsed);
        Assert.AreEqual(Empty, result);
    }
}
