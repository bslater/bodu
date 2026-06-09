// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolveOptionsTests.PresetsUndefined.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationResolveOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationResolveOptions.For(ConfigurationProfile)" /> rejects an undefined profile
    /// value.
    /// </summary>
    [TestMethod]
    public void For_WhenProfileIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = ConfigurationResolveOptions.For((ConfigurationProfile)42);
        });
    }
}
