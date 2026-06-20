// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationBindingTests.Bind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class TextConfigurationBindingTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationBinder.Get{T}(IConfiguration)" /> populates a POCO whose property
    /// names match the colon-delimited keys.
    /// </summary>
    [TestMethod]
    public void Bind_WhenSectionMapsToPoco_ShouldPopulateFields()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(PocoSample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddTextConfigurationStream(stream)
            .Build();

        LoggingOptions options = configuration.GetSection("logging").Get<LoggingOptions>() ?? new();

        Assert.AreEqual("Console", options.Provider);
        Assert.IsNotNull(options.Level);
        Assert.AreEqual("Information", options.Level!["default"]);
        Assert.AreEqual("Warning", options.Level!["console"]);
    }
}
