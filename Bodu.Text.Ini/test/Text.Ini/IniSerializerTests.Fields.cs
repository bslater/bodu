// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.Fields.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Ini;

/// <summary>
/// Verifies that public fields bind alongside properties.
/// </summary>
/// <remarks>
/// Field binding is opt-in through <c>IncludeFields</c> and had no coverage. The binder resolves fields through a
/// separate path from properties - a field has no accessor pair, so writability is decided by <c>readonly</c> rather
/// than by the presence of a setter - and the rest of the attribute and naming machinery has to apply to it just the
/// same, which is what these assert.
/// </remarks>
public partial class IniSerializerTests
{
    /// <summary>
    /// Gets options that opt into field binding, which is off by default.
    /// </summary>
    /// <value>The field-binding options.</value>
    private static IniSerializerOptions FieldOptions => new() { IncludeFields = true };

    /// <summary>
    /// A section model whose members are fields rather than properties.
    /// </summary>
    private sealed class FieldSection
    {
        /// <summary>The host name.</summary>
        public string? Host;

        /// <summary>The port number.</summary>
        public int Port;

        /// <summary>A read-only field, which cannot be assigned during deserialization.</summary>
        public readonly string Fixed = "constant";

        /// <summary>A field renamed on the wire.</summary>
        [PropertyName("time_out")]
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
    /// A root model whose section member is itself a field.
    /// </summary>
    private sealed class FieldRoot
    {
        /// <summary>The application name, written as a global key.</summary>
        public string? Name;

        /// <summary>The database section.</summary>
        public FieldSection? Database;
    }

    /// <summary>
    /// Verifies that fields are written as entries, using their declared names.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenModelUsesFields_ShouldWriteThemAsEntries()
    {
        string ini = IniSerializer.Serialize(
            new FieldRoot
            {
                Name = "app",
                Database = new FieldSection { Host = "localhost", Port = 8080, Timeout = 30 },
            },
            FieldOptions);

        Assert.Contains("Name=app", ini);
        Assert.Contains("[Database]", ini);
        Assert.Contains("Host=localhost", ini);
        Assert.Contains("Port=8080", ini);
    }

    /// <summary>
    /// Verifies that fields are read back into the model, so a document written from fields round-trips.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenModelUsesFields_ShouldPopulateThem()
    {
        var original = new FieldRoot
        {
            Name = "app",
            Database = new FieldSection { Host = "localhost", Port = 8080, Timeout = 30 },
        };

        FieldRoot? parsed = IniSerializer.Deserialize<FieldRoot>(IniSerializer.Serialize(original, FieldOptions), FieldOptions);

        Assert.IsNotNull(parsed);
        Assert.AreEqual("app", parsed!.Name);
        Assert.IsNotNull(parsed.Database);
        Assert.AreEqual("localhost", parsed.Database!.Host);
        Assert.AreEqual(8080, parsed.Database.Port);
        Assert.AreEqual(30, parsed.Database.Timeout);
    }

    /// <summary>
    /// Verifies that the attribute family applies to fields as it does to properties: a renamed field uses its wire
    /// name in both directions, and an ignored field appears in neither.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFieldsCarryAttributes_ShouldHonorThem()
    {
        string ini = IniSerializer.Serialize(
            new FieldRoot { Database = new FieldSection { Host = "h", Timeout = 30, Secret = "hunter2" } },
            FieldOptions);

        Assert.Contains("time_out=30", ini);
        Assert.DoesNotContain("Timeout", ini);
        Assert.DoesNotContain("hunter2", ini);
        Assert.DoesNotContain("Secret", ini);
    }

    /// <summary>
    /// Verifies that a read-only field is written but not assigned on the way back, since there is no way to set it -
    /// the value has to survive as the one the constructor established.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenFieldIsReadOnly_ShouldNotAssignIt()
    {
        string ini = IniSerializer.Serialize(new FieldRoot { Database = new FieldSection { Host = "h" } }, FieldOptions);

        Assert.Contains("Fixed=constant", ini);

        FieldRoot? parsed = IniSerializer.Deserialize<FieldRoot>(
            ini.Replace("Fixed=constant", "Fixed=changed", StringComparison.Ordinal),
            FieldOptions);

        Assert.AreEqual("constant", parsed!.Database!.Fixed);
    }

    /// <summary>
    /// Verifies that a naming policy applies to field names as it does to property names, so a model of fields is not
    /// silently exempt from the document's casing convention.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNamingPolicyIsSet_ShouldApplyItToFields()
    {
        string ini = IniSerializer.Serialize(
            new FieldRoot { Database = new FieldSection { Host = "localhost" } },
            new IniSerializerOptions { IncludeFields = true, PropertyNamingPolicy = NamingPolicy.SnakeCaseLower });

        Assert.Contains("host=localhost", ini);
    }
}
