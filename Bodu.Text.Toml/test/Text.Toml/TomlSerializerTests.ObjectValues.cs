// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.ObjectValues.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Document;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the <see cref="object" />-typed member support of <see cref="TomlSerializer" />: on write the runtime type
/// selects the converter, a bare <see cref="object" /> emits an empty inline table, and a <see langword="null" />
/// member is omitted; on read the value surfaces as a <see cref="TomlElement" />, mirroring the
/// <see cref="System.Text.Json.JsonElement" /> behavior of <see cref="System.Text.Json.JsonSerializer" />.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that an <see cref="object" />-typed member serializes through its runtime type's converter across the
    /// representative scalar shapes.
    /// </summary>
    /// <param name="value">The boxed value under test.</param>
    /// <param name="expected">The expected canonical value text, excluding the <c>Value = </c> prefix.</param>
    [TestMethod]
    [DataRow("x", "\"x\"", DisplayName = "boxed string")]
    [DataRow(5, "5", DisplayName = "boxed int")]
    [DataRow(1.5, "1.5", DisplayName = "boxed double")]
    [DataRow(true, "true", DisplayName = "boxed bool")]
    public void Serialize_WhenObjectMemberHoldsScalar_ShouldDispatchToRuntimeType(object value, string expected)
    {
        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = value });

        Assert.AreEqual($"Value = {expected}\n", text);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member holding a boxed array serializes as a TOML array through the
    /// runtime type's converter.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberHoldsArray_ShouldWriteArray()
    {
        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = new[] { 1, 2, 3 } });

        Assert.AreEqual("Value = [1, 2, 3]\n", text);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member holding a dictionary serializes as a TOML table through the
    /// runtime type's converter.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberHoldsDictionary_ShouldWriteTable()
    {
        var value = new Dictionary<string, int> { ["A"] = 1 };

        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = value });

        Assert.AreEqual("[Value]\nA = 1\n", text);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member holding a plain object graph serializes as a TOML table
    /// through the runtime type's converter.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberHoldsPoco_ShouldWriteTable()
    {
        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = new ValueModel<int> { Value = 7 } });

        Assert.AreEqual("[Value]\nValue = 7\n", text);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member holding a bare <see cref="object" /> serializes as an empty
    /// table — the TOML analogue of the empty JSON object — which the writer canonically emits as an empty header
    /// section.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberHoldsBareObject_ShouldWriteEmptyTable()
    {
        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = new object() });

        Assert.AreEqual("[Value]\n", text);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member whose value is <see langword="null" /> is omitted from the
    /// output, because TOML has no null form.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberNull_ShouldOmitMember()
    {
        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = null! });

        Assert.AreEqual(string.Empty, text);
    }

    /// <summary>
    /// Verifies that deserializing into an <see cref="object" />-typed member yields a <see cref="TomlElement" />
    /// carrying the matching <see cref="TomlValueKind" /> for each TOML value kind.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the value.</param>
    /// <param name="kind">The expected value kind of the surfaced element.</param>
    [TestMethod]
    [DataRow("Value = \"x\"\n", TomlValueKind.String, DisplayName = "string")]
    [DataRow("Value = 5\n", TomlValueKind.Integer, DisplayName = "integer")]
    [DataRow("Value = 1.5\n", TomlValueKind.Float, DisplayName = "float")]
    [DataRow("Value = true\n", TomlValueKind.Boolean, DisplayName = "boolean")]
    [DataRow("Value = [1, 2]\n", TomlValueKind.Array, DisplayName = "array")]
    [DataRow("Value = { A = 1 }\n", TomlValueKind.Table, DisplayName = "table")]
    public void Deserialize_WhenObjectMember_ShouldSurfaceTomlElement(string toml, TomlValueKind kind)
    {
        object actual = TomlSerializer.Deserialize<ValueModel<object>>(toml).Value;

        Assert.IsInstanceOfType<TomlElement>(actual);
        Assert.AreEqual(kind, ((TomlElement)actual).ValueKind);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member round-trips: the element read back re-serializes to the text
    /// its source value produced.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenObjectMember_ShouldRoundTripThroughElement()
    {
        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = new[] { 1, 2, 3 } });
        object element = TomlSerializer.Deserialize<ValueModel<object>>(text).Value;
        string again = TomlSerializer.Serialize(new ValueModel<object> { Value = element });

        Assert.AreEqual(text, again);
    }

    /// <summary>
    /// Verifies that deserializing a whole document into <see cref="object" /> yields a table-kind
    /// <see cref="TomlElement" /> exposing the document's properties.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenObjectRoot_ShouldSurfaceTableElement()
    {
        object actual = TomlSerializer.Deserialize<object>("A = 1\n");

        Assert.IsInstanceOfType<TomlElement>(actual);

        var element = (TomlElement)actual;
        Assert.AreEqual(TomlValueKind.Table, element.ValueKind);
        Assert.AreEqual(1L, element.GetProperty("A").GetInt64());
    }

    /// <summary>
    /// Verifies that serializing a boxed table-shaped value typed as <see cref="object" /> at the document root
    /// dispatches to the runtime type and emits the table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectRootHoldsTableShape_ShouldWriteTable()
    {
        object value = new Dictionary<string, int> { ["A"] = 1 };

        Assert.AreEqual("A = 1\n", TomlSerializer.Serialize(value));
    }

    /// <summary>
    /// Verifies that serializing a boxed scalar typed as <see cref="object" /> at the document root throws
    /// <see cref="InvalidOperationException" /> from the writer's root state machine, because a TOML document root must
    /// be a table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectRootHoldsScalar_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = TomlSerializer.Serialize<object>(5);
        });
    }
}
