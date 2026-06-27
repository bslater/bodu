// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptionsTests.NumberHandling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="YamlSerializerOptions.NumberHandling" /> governs numeric coercion during deserialization.
/// </summary>
public partial class YamlSerializerOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="YamlNumberHandling.AllowFloatToInteger" /> truncates a fractional float into an integer
    /// target.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAllowFloatToInteger_ShouldTruncate()
    {
        var options = new YamlSerializerOptions { NumberHandling = YamlNumberHandling.AllowFloatToInteger };

        var value = YamlSerializer.Deserialize<int>("3.9\n", options);

        Assert.AreEqual(3, value);
    }
}
