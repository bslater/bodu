// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationBindingTests.GetSection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class TextConfigurationBindingTests
{
    /// <summary>
    /// Verifies that <see cref="IConfiguration.GetSection(string)" /> returns an existing section whose
    /// <see cref="IConfigurationSection.Path" /> matches the requested path.
    /// </summary>
    [TestMethod]
    public void GetSection_WhenSectionExists_ShouldExposePathAndKey()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(PocoSample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddTextConfigurationStream(stream)
            .Build();

        IConfigurationSection section = configuration.GetSection("logging");

        Assert.AreEqual("logging", section.Path);
        Assert.AreEqual("logging", section.Key);
        Assert.AreEqual("Console", section["provider"]);
    }
}
