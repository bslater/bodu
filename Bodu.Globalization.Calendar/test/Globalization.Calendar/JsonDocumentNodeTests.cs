// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonDocumentNodeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the JSON-backed <see cref="IDocumentNode" /> adapter, focusing on non-scalar scalar reads and the
/// key-value-map projection.
/// </summary>
[TestClass]
public class JsonDocumentNodeTests
{
    private static JsonDocumentNode Node(string json) =>
        new(JsonDocument.Parse(json).RootElement, new List<NotableDateValidationDiagnostic>());

    /// <summary>
    /// Verifies that scalar reads coerce primitive tokens to text and return <see langword="null" /> for object,
    /// array, and null-valued properties.
    /// </summary>
    [TestMethod]
    public void Scalar_ShouldCoercePrimitivesAndRejectNonScalars()
    {
        JsonDocumentNode node = Node("""{"str":"hi","num":5,"yes":true,"no":false,"obj":{"a":1},"arr":[1,2],"nul":null}""");

        Assert.AreEqual("hi", node.Scalar("str"));
        Assert.AreEqual("5", node.Scalar("num"));
        Assert.AreEqual("true", node.Scalar("yes"));
        Assert.AreEqual("false", node.Scalar("no"));

        Assert.IsNull(node.Scalar("obj"));
        Assert.IsNull(node.Scalar("arr"));
        Assert.IsNull(node.Scalar("nul"));
        Assert.IsNull(node.Scalar("absent"));
    }

    /// <summary>
    /// Verifies that the key-value-map projection reads each property of a JSON object as a key/value pair, rendering
    /// non-string values as text.
    /// </summary>
    [TestMethod]
    public void KeyValueList_ShouldProjectObjectProperties()
    {
        JsonDocumentNode node = Node("""{"params":{"k1":"v1","k2":2}}""");

        IReadOnlyList<(string Key, string Value)> pairs = node.KeyValueList("Parameters", "Parameter", "name", "value", "params");

        Assert.HasCount(2, pairs);
        Assert.Contains(p => p is { Key: "k1", Value: "v1" }, pairs);
        Assert.Contains(p => p is { Key: "k2", Value: "2" }, pairs);
    }

    /// <summary>
    /// Verifies that the key-value-map projection yields nothing when the property is absent or not an object.
    /// </summary>
    [TestMethod]
    public void KeyValueList_WhenPropertyMissingOrNotObject_ShouldBeEmpty()
    {
        JsonDocumentNode node = Node("""{"params":[1,2]}""");

        Assert.IsEmpty(node.KeyValueList("Parameters", "Parameter", "name", "value", "params"));
        Assert.IsEmpty(node.KeyValueList("Parameters", "Parameter", "name", "value", "absent"));
    }

    /// <summary>
    /// Verifies that a value that breaks the pattern the schema enforces records one <c>BODU-CAL-SCHEMA</c> error
    /// naming the field, the value and the pattern.
    /// </summary>
    [TestMethod]
    public void CheckPattern_WhenValueBreaksPattern_ShouldRecordSchemaDiagnostic()
    {
        List<NotableDateValidationDiagnostic> diagnostics = new();

        Node("{}").CheckPattern("New-Year", NotableDateSchemaPattern.Identifier, "id", diagnostics);

        NotableDateValidationDiagnostic diagnostic = Assert.ContainsSingle(diagnostics);
        Assert.AreEqual("BODU-CAL-SCHEMA", diagnostic.Code);
        Assert.AreEqual(NotableDateValidationSeverity.Error, diagnostic.Severity);
        Assert.Contains("'id'", diagnostic.Message);
        Assert.Contains("'New-Year'", diagnostic.Message);
        Assert.Contains(NotableDateSchemaPatterns.Identifier, diagnostic.Message);
    }

    /// <summary>
    /// Verifies that a value that matches the pattern, or an absent value, records no diagnostic.
    /// </summary>
    /// <param name="value">The value, or <see langword="null" /> for an absent value.</param>
    [TestMethod]
    [DataRow("new-year")]
    [DataRow(null)]
    public void CheckPattern_WhenValueMatchesOrIsAbsent_ShouldRecordNothing(string? value)
    {
        List<NotableDateValidationDiagnostic> diagnostics = new();

        Node("{}").CheckPattern(value, NotableDateSchemaPattern.Identifier, "id", diagnostics);

        Assert.IsEmpty(diagnostics);
    }

    /// <summary>
    /// Verifies that a property the node's schema type requires that is absent, or holds the literal <c>null</c>,
    /// records one <c>BODU-CAL-SCHEMA</c> error naming the schema type and the property.
    /// </summary>
    /// <param name="json">The node's JSON object.</param>
    [TestMethod]
    [DataRow("""{ "notableDateRef": "x" }""")]
    [DataRow("""{ "notableDateRef": "x", "rule": null }""")]
    public void CheckRequired_WhenPropertyIsAbsentOrNull_ShouldRecordSchemaDiagnostic(string json)
    {
        List<NotableDateValidationDiagnostic> diagnostics = new();

        Node(json).CheckRequired(NotableDateSchemaType.AddRule, diagnostics);

        NotableDateValidationDiagnostic diagnostic = Assert.ContainsSingle(diagnostics);
        Assert.AreEqual("BODU-CAL-SCHEMA", diagnostic.Code);
        Assert.AreEqual(NotableDateValidationSeverity.Error, diagnostic.Severity);
        Assert.AreEqual("The 'AddRule' object has no 'rule' property, which the schema requires.", diagnostic.Message);
    }

    /// <summary>
    /// Verifies that every property the node's schema type requires that is absent records its own
    /// <c>BODU-CAL-SCHEMA</c> error, in the order the type lists them.
    /// </summary>
    [TestMethod]
    public void CheckRequired_WhenSeveralPropertiesAreAbsent_ShouldRecordOneDiagnosticEach()
    {
        List<NotableDateValidationDiagnostic> diagnostics = new();

        Node("""{ "category": "Observance" }""").CheckRequired(NotableDateSchemaType.NotableDate, diagnostics);

        CollectionAssert.AreEqual(
            new[]
            {
                "The 'NotableDate' object has no 'id' property, which the schema requires.",
                "The 'NotableDate' object has no 'displayName' property, which the schema requires.",
                "The 'NotableDate' object has no 'rules' property, which the schema requires.",
            },
            diagnostics.Select(d => d.Message).ToArray());
    }

    /// <summary>
    /// Verifies that a required list that holds no item records one <c>BODU-CAL-SCHEMA</c> error naming the schema type
    /// and the list.
    /// </summary>
    [TestMethod]
    public void CheckRequired_WhenRequiredListIsEmpty_ShouldRecordSchemaDiagnostic()
    {
        List<NotableDateValidationDiagnostic> diagnostics = new();

        Node("""{ "days": [ ] }""").CheckRequired(NotableDateSchemaType.WeeklyRecurrence, diagnostics);

        NotableDateValidationDiagnostic diagnostic = Assert.ContainsSingle(diagnostics);
        Assert.AreEqual("BODU-CAL-SCHEMA", diagnostic.Code);
        Assert.AreEqual("The 'WeeklyRecurrence' object's 'days' property holds an empty list, which the schema does not allow.", diagnostic.Message);
    }

    /// <summary>
    /// Verifies that a required property that is present, under another case or holding a value of another kind among
    /// them, records no diagnostic.
    /// </summary>
    /// <param name="json">The node's JSON object.</param>
    [TestMethod]
    [DataRow("""{ "notableDateRef": "x", "rule": {} }""")]
    [DataRow("""{ "NOTABLEDATEREF": "x", "RULE": {} }""")]
    [DataRow("""{ "notableDateRef": 5, "rule": 5 }""")]
    public void CheckRequired_WhenPropertyIsPresent_ShouldRecordNothing(string json)
    {
        List<NotableDateValidationDiagnostic> diagnostics = new();

        Node(json).CheckRequired(NotableDateSchemaType.AddRule, diagnostics);

        Assert.IsEmpty(diagnostics);
    }

    /// <summary>
    /// Verifies that a value outside a schema range records one <c>BODU-CAL-SCHEMA</c> error naming the field, the value
    /// and the bound it passes.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="expected">The expected message.</param>
    [TestMethod]
    [DataRow(0, "The 'day' value '0' is less than 1, the minimum the schema allows.")]
    [DataRow(32, "The 'day' value '32' is greater than 31, the maximum the schema allows.")]
    public void CheckRange_WhenValueIsOutsideRange_ShouldRecordSchemaDiagnostic(int value, string expected)
    {
        List<NotableDateValidationDiagnostic> diagnostics = new();

        Node("{}").CheckRange(value, NotableDateSchemaRange.DayValue, "day", diagnostics);

        NotableDateValidationDiagnostic diagnostic = Assert.ContainsSingle(diagnostics);
        Assert.AreEqual("BODU-CAL-SCHEMA", diagnostic.Code);
        Assert.AreEqual(expected, diagnostic.Message);
    }

    /// <summary>
    /// Verifies that a value inside a schema range, its bounds included, or an absent value records no diagnostic.
    /// </summary>
    /// <param name="value">The value, or <see langword="null" /> for an absent value.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(31)]
    [DataRow(null)]
    public void CheckRange_WhenValueIsInsideRangeOrAbsent_ShouldRecordNothing(int? value)
    {
        List<NotableDateValidationDiagnostic> diagnostics = new();

        Node("{}").CheckRange(value, NotableDateSchemaRange.DayValue, "day", diagnostics);

        Assert.IsEmpty(diagnostics);
    }

    /// <summary>
    /// Verifies that a list of objects yields only its object items and records one <c>BODU-CAL-VALUE</c> error, naming
    /// the property and the first other item's kind, for the items it leaves out.
    /// </summary>
    [TestMethod]
    public void List_WhenItemsAreNotObjects_ShouldYieldObjectsAndRecordOneValueDiagnostic()
    {
        List<NotableDateValidationDiagnostic> diagnostics = new();
        JsonDocumentNode node = new(JsonDocument.Parse("""{ "rules": [ 5, { "id": "r" }, "s", { "id": "t" } ] }""").RootElement, diagnostics);

        List<string?> ids = node.List("Rules", "Rule", "rules").Select(rule => rule.Scalar("id")).ToList();

        CollectionAssert.AreEqual(new[] { "r", "t" }, ids);
        NotableDateValidationDiagnostic diagnostic = Assert.ContainsSingle(diagnostics);
        Assert.AreEqual("BODU-CAL-VALUE", diagnostic.Code);
        Assert.AreEqual("The 'rules' property holds a JSON number item, which is not an object.", diagnostic.Message);
    }
}
