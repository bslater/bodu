// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationTests.Provider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Test.IO;
using Bodu.Text.Toml;

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class TomlConfigurationTests
{
    /// <summary>
    /// Verifies that the read-only TOML provider rejects mutation via <c>Set</c> with
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Provider_WhenSet_ShouldRejectMutation()
    {
        var provider = new TomlConfigurationProvider(new TomlConfigurationSource { Stream = new MemoryStream(Encoding.UTF8.GetBytes("a = 1")) });
        provider.Load();

        Assert.ThrowsExactly<NotSupportedException>(() => provider.Set("a", "2"));
    }

    /// <summary>
    /// Verifies that the TOML provider implements <see cref="IConfigurationProvider" />.
    /// </summary>
    [TestMethod]
    public void Provider_ShouldImplementConfigurationProvider()
    {
        var provider = new TomlConfigurationProvider(new TomlConfigurationSource());

        Assert.IsInstanceOfType<IConfigurationProvider>(provider);
    }
}
