// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationBindingTests.GetChildren.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class TextConfigurationBindingTests
{
    /// <summary>
    /// Verifies that <see cref="IConfiguration.GetSection(string)" /> exposes the nested keys via
    /// <see cref="IConfiguration.GetChildren" /> using the leaf segments as child keys.
    /// </summary>
    [TestMethod]
    public void GetChildren_WhenSectionHasNestedKeys_ShouldEnumerateLeafKeys()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(PocoSample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddTextConfigurationStream(stream)
            .Build();

        IConfigurationSection level = configuration.GetSection("logging:level");
        var childKeys = level.GetChildren().Select(c => c.Key).OrderBy(k => k).ToList();

        CollectionAssert.AreEquivalent(new[] { "console", "default" }, childKeys);
        Assert.AreEqual("Information", level["default"]);
        Assert.AreEqual("Warning", level["console"]);
    }
}
