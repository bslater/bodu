// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64VariantTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for the <see cref="Base64Variant" /> enum.
/// </summary>
[TestClass]
public sealed class Base64VariantTests
{

    /// <summary>
    /// Verifies that <see cref="Base64Variant.Standard" /> is the default value (numerical zero).
    /// </summary>
    [TestMethod]
    public void Standard_ShouldBeDefaultValue()
    {
        Assert.AreEqual(default, Base64Variant.Standard);
    }

    /// <summary>
    /// Verifies that the variants declare the documented numerical values so persisted enum values stay stable.
    /// </summary>
    [TestMethod]
    public void Variants_ShouldDeclareStableNumericalValues()
    {
        Assert.AreEqual(0, (int)Base64Variant.Standard);
        Assert.AreEqual(1, (int)Base64Variant.UrlSafe);
        Assert.AreEqual(2, (int)Base64Variant.Mime);
    }

}
