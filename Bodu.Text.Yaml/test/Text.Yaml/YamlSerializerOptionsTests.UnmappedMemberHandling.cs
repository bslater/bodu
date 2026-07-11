// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptionsTests.UnmappedMemberHandling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="YamlSerializerOptions.UnmappedMemberHandling" /> governs how unmapped keys are handled
/// during deserialization.
/// </summary>
public partial class YamlSerializerOptionsTests
{
    /// <summary>
    /// Verifies that an unmapped key is ignored under the default <see cref="UnmappedMemberHandling.Skip" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyAndSkip_ShouldIgnore()
    {
        Point? value = YamlSerializer.Deserialize<Point>("X: 1\nextra: 2\n");

        Assert.IsNotNull(value);
        Assert.AreEqual(1, value!.X);
    }

    /// <summary>
    /// Verifies that an unmapped key throws under <see cref="UnmappedMemberHandling.Disallow" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyAndDisallow_ShouldThrow()
    {
        var options = new YamlSerializerOptions { UnmappedMemberHandling = UnmappedMemberHandling.Disallow };

        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<Point>("X: 1\nextra: 2\n", options);
        });
    }
}
