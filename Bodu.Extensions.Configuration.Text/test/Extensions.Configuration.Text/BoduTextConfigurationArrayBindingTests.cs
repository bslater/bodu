// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextConfigurationArrayBindingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text.Tests;

/// <summary>
/// Verifies array-index key parity with <c>Microsoft.Extensions.Configuration.Json</c>: the Bodu source
/// format's default dotted-segment notation (<c>items.0 = a</c>) projects into the same colon-delimited
/// indexed keys (<c>items:0</c>) that JSON arrays produce, so <see cref="ConfigurationBinder" /> binds
/// collection types identically across both providers.
/// </summary>
[TestClass]
public class BoduTextConfigurationArrayBindingTests
{
    private const string ArraySample = """
items.0 = first
items.1 = second
items.2 = third
""";

    /// <summary>
    /// Verifies that dotted numeric segments project into colon-delimited indexed keys that
    /// <see cref="ConfigurationBinder" /> binds into a <see cref="List{T}" />.
    /// </summary>
    [TestMethod]
    public void ArrayBinding_WhenSourceUsesDottedIndexSegments_ShouldBindToListOfString()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(ArraySample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream)
            .Build();

        List<string>? items = configuration.GetSection("items").Get<List<string>>();

        Assert.IsNotNull(items);
        CollectionAssert.AreEqual(new[] { "first", "second", "third" }, items);
    }

    /// <summary>
    /// Verifies that the same dotted-index source binds into a primitive array via
    /// <see cref="ConfigurationBinder.Get{T}(IConfiguration)" />.
    /// </summary>
    [TestMethod]
    public void ArrayBinding_WhenSourceUsesDottedIndexSegments_ShouldBindToArrayOfString()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(ArraySample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream)
            .Build();

        string[]? items = configuration.GetSection("items").Get<string[]>();

        Assert.IsNotNull(items);
        CollectionAssert.AreEqual(new[] { "first", "second", "third" }, items);
    }

    /// <summary>
    /// Verifies that the resolved view exposes the indexed leaf keys (<c>0</c>, <c>1</c>, <c>2</c>) under
    /// the parent section, matching the shape JSON arrays produce through
    /// <c>Microsoft.Extensions.Configuration.Json</c>.
    /// </summary>
    [TestMethod]
    public void ArrayBinding_WhenSourceUsesDottedIndexSegments_ShouldExposeIndexedColonKeys()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(ArraySample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream)
            .Build();

        List<string> childKeys = configuration.GetSection("items")
            .GetChildren()
            .Select(c => c.Key)
            .OrderBy(k => k)
            .ToList();

        CollectionAssert.AreEqual(new[] { "0", "1", "2" }, childKeys);
        Assert.AreEqual("first", configuration["items:0"]);
        Assert.AreEqual("second", configuration["items:1"]);
        Assert.AreEqual("third", configuration["items:2"]);
    }
}
