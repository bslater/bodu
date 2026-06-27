// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptionsTests.Freeze.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the freeze-on-use behavior of <see cref="YamlSerializerOptions" /> exposed through
/// <see cref="YamlSerializerOptions.IsReadOnly" /> and <see cref="YamlSerializerOptions.MakeReadOnly" />.
/// </summary>
public partial class YamlSerializerOptionsTests
{
    /// <summary>
    /// Verifies that an options instance becomes read-only after it is used and rejects further mutation.
    /// </summary>
    [TestMethod]
    public void IsReadOnly_WhenOptionsUsed_ShouldBecomeReadOnlyAndRejectMutation()
    {
        var options = new YamlSerializerOptions();

        _ = YamlSerializer.Serialize(1, options);

        Assert.IsTrue(options.IsReadOnly);
        Assert.ThrowsExactly<InvalidOperationException>(() => options.IncludeFields = true);
    }

    /// <summary>
    /// Verifies that <see cref="YamlSerializerOptions.MakeReadOnly" /> freezes the instance.
    /// </summary>
    [TestMethod]
    public void MakeReadOnly_ShouldFreezeOptions()
    {
        var options = new YamlSerializerOptions();
        options.MakeReadOnly();

        Assert.ThrowsExactly<InvalidOperationException>(() => options.SpecVersion = YamlSpecVersion.V1_1);
    }
}
