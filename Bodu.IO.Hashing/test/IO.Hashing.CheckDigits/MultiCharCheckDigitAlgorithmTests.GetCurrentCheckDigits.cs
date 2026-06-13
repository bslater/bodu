// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitAlgorithmTests.GetCurrentCheckDigits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class MultiCharCheckDigitAlgorithmTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that the span and string overloads of <c>GetCurrentCheckDigits</c> agree.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (unused).</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The expected check code (unused in this cross-check).</param>
    [TestMethod]

    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(CheckDigitKatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(CheckDigitKatDisplayName))]
    public void GetCurrentCheckDigits_SpanAndStringOverloads_ShouldAgree(string name, string body, string expectedCheck)
    {
        _ = name;
        _ = expectedCheck;

        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append(body.AsSpan());

        Span<char> buffer = stackalloc char[algorithm.CheckLength];
        var written = algorithm.GetCurrentCheckDigits(buffer);

        Assert.AreEqual(algorithm.CheckLength, written);
        Assert.AreEqual(algorithm.GetCurrentCheckDigits(), new string(buffer));
    }
    /// <summary>
    /// Verifies that a freshly constructed algorithm reports the empty-body check code declared in the
    /// specification, when one is declared.
    /// </summary>
    [TestMethod]
    public void GetCurrentCheckDigits_WhenJustConstructed_ShouldReturnEmptyCheckDigits()
    {
        MultiCharCheckDigitAlgorithmSpecification spec = GetSpecification();
        if (spec.EmptyCheckDigits is not string expected) return;

        TAlgorithm algorithm = CreateAlgorithm();

        Assert.AreEqual(expected, algorithm.GetCurrentCheckDigits());
    }

    /// <summary>
    /// Verifies that reading the current check code twice in succession — with no intervening appends — yields
    /// the same value, proving the getter is non-destructive.
    /// </summary>
    [TestMethod]
    public void GetCurrentCheckDigits_WhenReadRepeatedly_ShouldReturnSameValue()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append("12345".AsSpan());

        var first = algorithm.GetCurrentCheckDigits();
        var second = algorithm.GetCurrentCheckDigits();
        var third = algorithm.GetCurrentCheckDigits();

        Assert.AreEqual(first, second);
        Assert.AreEqual(second, third);
    }

}
