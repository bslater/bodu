// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OffsetFromRuleStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class OffsetFromRuleStrategyTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> concept reference throws <see cref="ArgumentNullException" /> naming the
    /// <c>notableDateRef</c> parameter.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNotableDateRefIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new OffsetFromRuleStrategy(null!, null, 1);
        });

        Assert.AreEqual("notableDateRef", ex.ParamName);
    }
}
