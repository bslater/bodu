// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlgorithmDateStrategyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AlgorithmDateStrategyTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> key throws <see cref="ArgumentNullException" /> naming the <c>key</c> parameter.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new AlgorithmDateStrategy(null!);
        });

        Assert.AreEqual("key", ex.ParamName);
    }
}
