// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58VariantTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for the <see cref="Base58Variant" /> enum.
/// </summary>
[TestClass]
public sealed class Base58VariantTests
{

    /// <summary>
    /// Verifies that <see cref="Base58Variant.BitcoinFlickr" /> is the default value.
    /// </summary>
    [TestMethod]
    public void BitcoinFlickr_ShouldBeDefaultValue() => Assert.AreEqual(default, Base58Variant.BitcoinFlickr);

    /// <summary>
    /// Verifies that the variants declare the documented numerical values.
    /// </summary>
    [TestMethod]
    public void Variants_ShouldDeclareStableNumericalValues()
    {
        Assert.AreEqual(0, (int)Base58Variant.BitcoinFlickr);
        Assert.AreEqual(1, (int)Base58Variant.Ripple);
    }

}
