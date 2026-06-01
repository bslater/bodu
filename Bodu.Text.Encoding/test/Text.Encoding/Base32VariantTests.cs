// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32VariantTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for the <see cref="Base32Variant" /> enum.
/// </summary>
[TestClass]
public sealed class Base32VariantTests
{

    /// <summary>
    /// Verifies that <see cref="Base32Variant.Standard" /> is the default value (numerical zero).
    /// </summary>
    [TestMethod]
    public void Standard_ShouldBeDefaultValue() => Assert.AreEqual(default, Base32Variant.Standard);

    /// <summary>
    /// Verifies that the variants declare the documented numerical values so that persisted enum values stay stable.
    /// </summary>
    [TestMethod]
    public void Variants_ShouldDeclareStableNumericalValues()
    {
        Assert.AreEqual(0, (int)Base32Variant.Standard);
        Assert.AreEqual(1, (int)Base32Variant.HexExtended);
        Assert.AreEqual(2, (int)Base32Variant.Crockford);
        Assert.AreEqual(3, (int)Base32Variant.ZBase32);
    }

}
