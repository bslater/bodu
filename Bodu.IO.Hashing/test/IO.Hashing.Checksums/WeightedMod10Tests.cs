// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeightedMod10Tests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains direct unit tests for the internal <see cref="WeightedMod10" /> helpers. Public consumers
/// (<see cref="AbaRoutingNumber" />, ISBN-13, GTIN) cover most paths indirectly, but the non-digit short-circuit
/// of <see cref="WeightedMod10.IsValidAba" /> requires direct invocation through
/// <see cref="System.Runtime.CompilerServices.InternalsVisibleToAttribute" />.
/// </summary>
[TestClass]
public sealed class WeightedMod10Tests
{

    /// <summary>
    /// Verifies that <see cref="WeightedMod10.IsValidAba" /> returns <see langword="false" /> as soon as a
    /// non-digit character is encountered, without throwing.
    /// </summary>
    [TestMethod]
    public void IsValidAba_WhenSequenceContainsNonDigit_ShouldReturnFalse() => Assert.IsFalse(WeightedMod10.IsValidAba("01100A015".AsSpan()));

    /// <summary>
    /// Verifies that <see cref="WeightedMod10.IsValidIsbn13" /> returns <see langword="false" /> when the sequence
    /// contains a non-digit, exercising the inline short-circuit on the validation loop.
    /// </summary>
    [TestMethod]
    public void IsValidIsbn13_WhenSequenceContainsNonDigit_ShouldReturnFalse() => Assert.IsFalse(WeightedMod10.IsValidIsbn13("978030640615X".AsSpan()));

}
