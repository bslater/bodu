// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericCheckDigitAlgorithmTests{T,T}.Append.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class AlphanumericCheckDigitAlgorithmTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that <c>Append</c> rejects characters that fall outside the declared input alphabet.
    /// </summary>
    [TestMethod]
    public void Append_WhenCharacterIsOutsideInputAlphabet_ShouldThrowExactly()
    {
        AlphanumericCheckDigitAlgorithmSpecification spec = GetSpecification();
        char invalid = spec.InputAlphabet == CheckDigitInputAlphabet.DecimalDigits ? 'A' : '!';

        TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.Append(new[] { '1', invalid, '2' }.AsSpan());
        });
    }
    /// <summary>
    /// Verifies that appending a body in a single call produces the check character recorded for that
    /// known-answer vector.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append.</param>
    /// <param name="expectedCheck">The check character the algorithm is expected to emit.</param>
    [TestMethod]

    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(CheckDigitKatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(CheckDigitKatDisplayName))]
    public void Append_WhenKnownAnswerIsAppendedInFull_ShouldProduceExpectedCheckDigit(string name, string body, char expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        algorithm.Append(body.AsSpan());

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that appending a body one character at a time produces the same check character as a single
    /// span-based call.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append.</param>
    /// <param name="expectedCheck">The check character the algorithm is expected to emit.</param>
    [TestMethod]

    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(CheckDigitKatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(CheckDigitKatDisplayName))]
    public void Append_WhenKnownAnswerIsAppendedOneCharAtATime_ShouldProduceExpectedCheckDigit(string name, string body, char expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        foreach (char ch in body)
            algorithm.Append(ch);

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that appending a body in two chunks produces the same check character as appending it in one
    /// call.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append.</param>
    /// <param name="expectedCheck">The check character the algorithm is expected to emit.</param>
    [TestMethod]

    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(CheckDigitKatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(CheckDigitKatDisplayName))]
    public void Append_WhenKnownAnswerIsSplitAcrossTwoChunks_ShouldProduceExpectedCheckDigit(string name, string body, char expectedCheck)
    {
        _ = name;
        if (body.Length < 2) return;

        TAlgorithm algorithm = CreateAlgorithm();
        int split = body.Length / 2;

        algorithm.Append(body.AsSpan(0, split));
        algorithm.Append(body.AsSpan(split));

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that appending an empty span leaves the running check character unchanged.
    /// </summary>
    [TestMethod]
    public void Append_WhenSpanIsEmpty_ShouldLeaveCheckDigitUnchanged()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append("123".AsSpan());
        char initial = algorithm.GetCurrentCheckDigit();

        algorithm.Append([]);

        Assert.AreEqual(initial, algorithm.GetCurrentCheckDigit());
    }

}
