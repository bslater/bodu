// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85VariantTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for the <see cref="Base85Variant" /> enum.
/// </summary>
[TestClass]
public sealed class Base85VariantTests
{

    /// <summary>
    /// Verifies that <see cref="Base85Variant.Ascii85" /> is the default value.
    /// </summary>
    [TestMethod]
    public void Ascii85_ShouldBeDefaultValue()
    {
        Assert.AreEqual(default, Base85Variant.Ascii85);
    }

    /// <summary>
    /// Verifies that the variants declare stable numerical values.
    /// </summary>
    [TestMethod]
    public void Variants_ShouldDeclareStableNumericalValues()
    {
        Assert.AreEqual(0, (int)Base85Variant.Ascii85);
        Assert.AreEqual(1, (int)Base85Variant.Z85);
    }

}
