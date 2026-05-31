// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BaseFormatStylesTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for the <see cref="BaseFormatStyles" /> flag enum.
/// </summary>
[TestClass]
public sealed class BaseFormatStylesTests
{

    /// <summary>
    /// Verifies that the documented flag values are powers of two so combinations preserve bit identity.
    /// </summary>
    [TestMethod]
    public void Flags_ShouldUseDistinctPowersOfTwo()
    {
        Assert.AreEqual(1, (int)BaseFormatStyles.AllowPrefix);
        Assert.AreEqual(2, (int)BaseFormatStyles.IgnoreWhitespace);
    }
    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.None" /> equals zero so the enum participates correctly in default
    /// values and bitwise unions.
    /// </summary>
    [TestMethod]
    public void None_ShouldEqualZero()
    {
        Assert.AreEqual(0, (int)BaseFormatStyles.None);
    }

    /// <summary>
    /// Verifies that the enum carries the <see cref="FlagsAttribute" />.
    /// </summary>
    [TestMethod]
    public void Type_ShouldDeclareFlagsAttribute()
    {
        Assert.IsTrue(typeof(BaseFormatStyles).IsDefined(typeof(FlagsAttribute), inherit: false));
    }

}
