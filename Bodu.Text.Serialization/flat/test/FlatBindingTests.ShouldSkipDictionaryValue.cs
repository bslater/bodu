// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatBindingTests.ShouldSkipDictionaryValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class FlatBindingTests
{
    /// <summary>
    /// Verifies that a null dictionary value is left out under the conditions that leave out null values, and that a
    /// value is never left out.
    /// </summary>
    [TestMethod]
    public void ShouldSkipDictionaryValue_WhenTheValueIsNull_ShouldFollowTheDefaultCondition()
    {
        Assert.IsTrue(FlatBinding.ShouldSkipDictionaryValue(null, IgnoreCondition.WhenWritingNull));
        Assert.IsTrue(FlatBinding.ShouldSkipDictionaryValue(null, IgnoreCondition.WhenWritingDefault));
        Assert.IsFalse(FlatBinding.ShouldSkipDictionaryValue(null, IgnoreCondition.Never));
        Assert.IsFalse(FlatBinding.ShouldSkipDictionaryValue(0, IgnoreCondition.WhenWritingDefault));
    }
}
