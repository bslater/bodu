// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationMultiSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Verifies that multiple Bodu Text Configuration sources layer in the order they are added, matching the
/// last-writer-wins precedence applied by <c>Microsoft.Extensions.Configuration</c> for every provider.
/// </summary>
[TestClass]
public class TextConfigurationMultiSourceTests
{
    /// <summary>
    /// Verifies that when two sources are added, the second source overrides the first for keys present in
    /// both.
    /// </summary>
    [TestMethod]
    public void MultipleTextSources_ShouldLayerByOrderAdded()
    {
        const string baseSource = """
service.name = First
service.port = 8080
""";

        const string overrideSource = """
service.name = Second
""";

        using MemoryStream first = new(Encoding.UTF8.GetBytes(baseSource));
        using MemoryStream second = new(Encoding.UTF8.GetBytes(overrideSource));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(first)
            .AddConfiguration(second)
            .Build();

        Assert.AreEqual("Second", configuration["service:name"]);
        Assert.AreEqual("8080", configuration["service:port"]);
    }
}
