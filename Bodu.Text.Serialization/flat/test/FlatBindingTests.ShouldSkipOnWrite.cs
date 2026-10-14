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
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite("x", typeof(string), IgnoreCondition.Always, IgnoreCondition.Never));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(null, typeof(string), null, IgnoreCondition.WhenWritingNull));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(0, typeof(int), null, IgnoreCondition.WhenWritingNull));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(0, typeof(int), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(false, typeof(bool), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(default(DateTime), typeof(DateTime), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(1, typeof(int), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(string.Empty, typeof(string), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(null, typeof(string), null, IgnoreCondition.Never));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(null, typeof(string), IgnoreCondition.Never, IgnoreCondition.WhenWritingNull));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(0, typeof(int), IgnoreCondition.WhenWritingDefault, IgnoreCondition.Never));
    }

    /// <summary>
    /// Verifies that <see cref="IgnoreCondition.WhenWritingDefault" /> leaves out the default of the member's declared
    /// type, as <c>System.Text.Json</c> does: a member declared as a <see cref="Nullable{T}" /> or as
    /// <see cref="object" /> only when it is <see langword="null" />, so that <c>0</c>, <see langword="false" />, a
    /// default <see cref="DateTime" /> or a default structure that it holds is written.
    /// </summary>
    [TestMethod]
    public void ShouldSkipOnWrite_WhenTheDeclaredDefaultIsNull_ShouldLeaveOutOnlyNull()
    {
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(0, typeof(int?), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(false, typeof(bool?), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(default(DateTime), typeof(DateTime?), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(default(ValueSample), typeof(ValueSample?), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsFalse(FlatBinding.ShouldSkipOnWrite(0, typeof(object), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(null, typeof(int?), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(null, typeof(object), null, IgnoreCondition.WhenWritingDefault));
        Assert.IsTrue(FlatBinding.ShouldSkipOnWrite(default(ValueSample), typeof(ValueSample), null, IgnoreCondition.WhenWritingDefault));
    }
}
