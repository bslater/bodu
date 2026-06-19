// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileHashPluginTrustPolicyTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class FileHashPluginTrustPolicyTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> allowlist throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMapIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new FileHashPluginTrustPolicy(null!);
        });
    }
}
