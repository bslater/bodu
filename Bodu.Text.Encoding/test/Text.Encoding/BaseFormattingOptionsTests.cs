// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BaseFormattingOptionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for the <see cref="BaseFormattingOptions" /> flag enum.
/// </summary>
[TestClass]
public sealed class BaseFormattingOptionsTests
{

    /// <summary>
    /// Verifies that the documented flag values are distinct powers of two.
    /// </summary>
    [TestMethod]
    public void Flags_ShouldUseDistinctPowersOfTwo()
    {
        Assert.AreEqual(1, (int)BaseFormattingOptions.UpperCase);
        Assert.AreEqual(2, (int)BaseFormattingOptions.InsertLineBreaks);
        Assert.AreEqual(4, (int)BaseFormattingOptions.IncludePrefix);
        Assert.AreEqual(8, (int)BaseFormattingOptions.InsertSpacing);
    }
    /// <summary>
    /// Verifies that <see cref="BaseFormattingOptions.None" /> equals zero so the enum participates correctly in
    /// default values and bitwise unions.
    /// </summary>
    [TestMethod]
    public void None_ShouldEqualZero()
    {
        Assert.AreEqual(0, (int)BaseFormattingOptions.None);
    }

    /// <summary>
    /// Verifies that the enum carries the <see cref="FlagsAttribute" />.
    /// </summary>
    [TestMethod]
    public void Type_ShouldDeclareFlagsAttribute()
    {
        Assert.IsTrue(typeof(BaseFormattingOptions).IsDefined(typeof(FlagsAttribute), inherit: false));
    }

}
