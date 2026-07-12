// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Include.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="IncludeAttribute" /> forces a member to participate: a property with a non-public setter
/// is bound, and a public field is surfaced even when <see cref="YamlSerializerOptions.IncludeFields" /> is disabled.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that a property with a non-public setter marked <c>[Include]</c> is both written and read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenIncludeOnNonPublicSetter_ShouldRoundTrip()
    {
        var model = new IncludeSetterModel(5);

        string yaml = YamlSerializer.Serialize(model);
        Assert.AreEqual("Total: 5\n", yaml);

        IncludeSetterModel back = YamlSerializer.Deserialize<IncludeSetterModel>("Total: 9\n")!;
        Assert.AreEqual(9, back.Total);
    }

    /// <summary>
    /// Verifies that a public field marked <c>[Include]</c> is surfaced even though
    /// <see cref="YamlSerializerOptions.IncludeFields" /> is disabled.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenIncludeOnFieldAndFieldsDisabled_ShouldRoundTrip()
    {
        var model = new IncludeFieldModel { Retries = 3 };

        string yaml = YamlSerializer.Serialize(model);
        Assert.AreEqual("Retries: 3\n", yaml);

        IncludeFieldModel back = YamlSerializer.Deserialize<IncludeFieldModel>("Retries: 8\n")!;
        Assert.AreEqual(8, back.Retries);
    }

    /// <summary>A model whose property exposes only a non-public setter, opted in by <see cref="IncludeAttribute" />.</summary>
    private sealed class IncludeSetterModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeSetterModel" /> class.
        /// </summary>
        public IncludeSetterModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeSetterModel" /> class with a total.
        /// </summary>
        /// <param name="total">The total.</param>
        public IncludeSetterModel(int total)
        {
            Total = total;
        }

        /// <summary>
        /// Gets the total, assigned through a non-public setter.
        /// </summary>
        /// <value>The total.</value>
        [Include]
        public int Total { get; private set; }
    }

    /// <summary>A model whose public field is opted in by <see cref="IncludeAttribute" />.</summary>
    private sealed class IncludeFieldModel
    {
        /// <summary>The retry count, surfaced by <see cref="IncludeAttribute" /> despite fields being disabled.</summary>
        [Include]
        public int Retries;
    }
}
