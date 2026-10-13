// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatBindingTests.ShouldSkipOnWrite.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class FlatBindingTests
{
    /// <summary>
    /// Verifies which values each ignore condition leaves out: always every value, null values under
    /// <see cref="IgnoreCondition.WhenWritingNull" />, null and value-type default values under
    /// <see cref="IgnoreCondition.WhenWritingDefault" />, and none under <see cref="IgnoreCondition.Never" />; a
    /// member's own condition is used before the default.
    /// </summary>
    [TestMethod]
    public void ShouldSkipOnWrite_WhenAConditionApplies_ShouldLeaveOutTheValuesItIgnores()
    {
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite("x", IgnoreCondition.Always, IgnoreCondition.Never));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(null, null, IgnoreCondition.WhenWritingNull));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(0, null, IgnoreCondition.WhenWritingNull));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(0, null, IgnoreCondition.WhenWritingDefault));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(false, null, IgnoreCondition.WhenWritingDefault));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(default(DateTime), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(1, null, IgnoreCondition.WhenWritingDefault));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(string.Empty, null, IgnoreCondition.WhenWritingDefault));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(null, null, IgnoreCondition.Never));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(null, IgnoreCondition.Never, IgnoreCondition.WhenWritingNull));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(0, IgnoreCondition.WhenWritingDefault, IgnoreCondition.Never));
    }
}
