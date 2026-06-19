// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrongNamePluginTrustPolicyTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class StrongNamePluginTrustPolicyTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> allowlist throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTokensNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new StrongNamePluginTrustPolicy(null!);
        });
    }
}
