// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.Fields.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Verifies that public fields bind alongside properties.
/// </summary>
/// <remarks>
/// Field binding is opt-in through <c>IncludeFields</c> and had no coverage. The binder resolves fields through a
/// separate path from properties - a field has no accessor pair, so writability is decided by <c>readonly</c> rather
/// than by the presence of a setter - and the rest of the attribute and naming machinery has to apply to it just the
/// same, which is what these assert.
/// </remarks>
public partial class DotEnvSerializerTests
{
    /// <summary>
    /// Gets options that opt into field binding, which is off by default.
    /// </summary>
    /// <value>The field-binding options.</value>
    private static DotEnvSerializerOptions FieldOptions => new() { IncludeFields = true };

    /// <summary>
    /// A model whose members are fields rather than properties.
    /// </summary>
    private sealed class FieldModel
    {
        /// <summary>The host name.</summary>
        public string? Host;

        /// <summary>The port number.</summary>
        public int Port;

        /// <summary>A read-only field, which cannot be assigned during deserialization.</summary>
        public readonly string Fixed = "constant";

        /// <summary>A field renamed on the wire.</summary>
        [PropertyName("TIME_OUT")]
        public int Timeout;

        /// <summary>A field excluded from both directions.</summary>
        /// <remarks>
        /// Fully qualified because MSTest's own <c>Ignore</c> attribute is in scope through the project's implicit
        /// usings, and the two are otherwise ambiguous.
        /// </remarks>
        [Serialization.Ignore]
        public string? Secret;
    }

    /// <summary>
    /// Verifies that fields are written as entries, using their declared names.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenModelUsesFields_ShouldWriteThemAsEntries()
    {
        string text = DotEnvSerializer.Serialize(
            new FieldModel { Host = "localhost", Port = 8080 },
            FieldOptions);

        Assert.Contains("Host=localhost", text);
        Assert.Contains("Port=8080", text);
    }

    /// <summary>
    /// Verifies that fields are read back into the model, so a document written from fields round-trips.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenModelUsesFields_ShouldPopulateThem()
    {
        var original = new FieldModel { Host = "localhost", Port = 8080, Timeout = 30 };

        FieldModel? parsed = DotEnvSerializer.Deserialize<FieldModel>(
            DotEnvSerializer.Serialize(original, FieldOptions),
            FieldOptions);

        Assert.IsNotNull(parsed);
        Assert.AreEqual("localhost", parsed!.Host);
        Assert.AreEqual(8080, parsed.Port);
        Assert.AreEqual(30, parsed.Timeout);
    }

    /// <summary>
    /// Verifies that the attribute family applies to fields as it does to properties: a renamed field uses its wire
    /// name, and an ignored field appears in neither direction.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFieldsCarryAttributes_ShouldHonorThem()
    {
        string text = DotEnvSerializer.Serialize(
            new FieldModel { Host = "h", Timeout = 30, Secret = "hunter2" },
            FieldOptions);

        Assert.Contains("TIME_OUT=30", text);
        Assert.DoesNotContain("Timeout", text);
        Assert.DoesNotContain("hunter2", text);
        Assert.DoesNotContain("Secret", text);
    }

    /// <summary>
    /// Verifies that a read-only field is written but not assigned on the way back, since there is no way to set it.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenFieldIsReadOnly_ShouldNotAssignIt()
    {
        string text = DotEnvSerializer.Serialize(new FieldModel { Host = "h" }, FieldOptions);

        Assert.Contains("Fixed=constant", text);

        FieldModel? parsed = DotEnvSerializer.Deserialize<FieldModel>(
            text.Replace("Fixed=constant", "Fixed=changed", StringComparison.Ordinal),
            FieldOptions);

        Assert.AreEqual("constant", parsed!.Fixed);
    }

    /// <summary>
    /// Verifies that a naming policy applies to field names as it does to property names, so a model of fields is not
    /// silently exempt from the document's casing convention.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNamingPolicyIsSet_ShouldApplyItToFields()
    {
        string text = DotEnvSerializer.Serialize(
            new FieldModel { Host = "localhost" },
            new DotEnvSerializerOptions { IncludeFields = true, PropertyNamingPolicy = NamingPolicy.SnakeCaseUpper });

        Assert.Contains("HOST=localhost", text);
    }
}
