// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Deserialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies <see cref="YamlSerializer.Deserialize{TValue}(string, YamlSerializerOptions)" />: dictionary binding,
/// loosely-typed object binding, case-insensitive property matching, the kind of a bound timestamp, and the binding
/// failure a root value that cannot convert to the target type reports.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>Verifies that a primitive dictionary deserializes from YAML.</summary>
    [TestMethod]
    public void Deserialize_WhenDictionary_ShouldBind()
    {
        Dictionary<string, int> dict = YamlSerializer.Deserialize<Dictionary<string, int>>("a: 1\nb: 2\nc: 3\n")!;
        Assert.AreEqual(3, dict.Count);
        Assert.AreEqual(2, dict["b"]);
    }

    /// <summary>Verifies that the loosely-typed object binding produces nested dictionaries and lists.</summary>
    [TestMethod]
    public void Deserialize_WhenObject_ShouldBindDynamic()
    {
        object? result = YamlSerializer.Deserialize<object>("name: x\nitems:\n  - 1\n  - 2\n");
        var map = (Dictionary<string, object?>)result!;
        Assert.AreEqual("x", map["name"]);
        var items = (List<object?>)map["items"]!;
        Assert.AreEqual(1L, items[0]);
    }

    /// <summary>Verifies that case-insensitive property matching binds differently-cased keys.</summary>
    [TestMethod]
    public void Deserialize_WhenCaseInsensitive_ShouldBind()
    {
        var options = new YamlSerializerOptions { PropertyNameCaseInsensitive = true };
        Person person = YamlSerializer.Deserialize<Person>("name: Eve\nAGE: 30\nactive: true\n", options)!;
        Assert.AreEqual("Eve", person.Name);
        Assert.AreEqual(30, person.Age);
        Assert.IsTrue(person.Active);
    }

    /// <summary>
    /// Verifies that the <see cref="Stream" /> overload reads a stream of UTF-8 YAML bytes to its end and binds the
    /// value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStreamSource_ShouldReturnValue()
    {
        using var source = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Name: x\nAge: 7\nActive: true\n"));

        Person person = YamlSerializer.Deserialize<Person>(source)!;

        Assert.AreEqual("x", person.Name);
        Assert.AreEqual(7, person.Age);
        Assert.IsTrue(person.Active);
    }

    /// <summary>
    /// Verifies that the <see cref="Stream" /> overload throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> <c>source</c> when the stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStreamSourceIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = YamlSerializer.Deserialize<Person>((Stream)null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a key whose property a derived type hides with <see langword="new" /> and a property of another
    /// type binds the derived property and leaves the hidden one at its initial value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPropertyIsHiddenByAnotherType_ShouldBindTheDerivedProperty()
    {
        HiddenPropertyDerivedModel model = YamlSerializer.Deserialize<HiddenPropertyDerivedModel>("Value: text\n")!;

        Assert.AreEqual(("text", 1), (model.Value, ((HiddenPropertyBaseModel)model).Value));
    }

    /// <summary>
    /// Verifies that a missing required member is reported with the dotted path to it, as other binding failures are:
    /// its name at the root, and its path through nested mappings and sequences below it.
    /// </summary>
    /// <param name="yaml">The document, which leaves out one required member.</param>
    /// <param name="path">The expected path to the missing member.</param>
    [TestMethod]
    [DataRow("other: 1\n", "Id", DisplayName = "at the root")]
    [DataRow("Id: 1\nInner: {}\n", "Inner.Name", DisplayName = "in a nested mapping")]
    [DataRow("Id: 1\nMiddle:\n  Inner: {}\n", "Middle.Inner.Name", DisplayName = "two mappings down")]
    [DataRow("Id: 1\nItems:\n  - Name: a\n  - {}\n", "Items[1].Name", DisplayName = "in a sequence")]
    public void Deserialize_WhenRequiredMemberIsMissing_ShouldReportItsPath(string yaml, string path)
    {
        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<RequiredPathRootModel>(yaml);
        });

        Assert.AreEqual(path, ex.Path);
    }

    /// <summary>
    /// Verifies that a timestamp with a <c>Z</c> suffix binds to a <see cref="DateTime" /> whose kind is
    /// <see cref="DateTimeKind.Utc" /> and whose value is the UTC time the text names.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDateTimeHasZSuffix_ShouldReturnUtcKind()
    {
        var options = new YamlSerializerOptions { PropertyNamingPolicy = NamingPolicy.CamelCase };

        BirthRecord record = YamlSerializer.Deserialize<BirthRecord>("momentOfBirth: 1983-04-21T20:21:03.0041599Z\n", options)!;

        Assert.AreEqual(DateTimeKind.Utc, record.MomentOfBirth.Kind);
        Assert.AreEqual(new DateTime(1983, 4, 21, 20, 21, 3, DateTimeKind.Utc).AddTicks(41_599), record.MomentOfBirth);
    }

    /// <summary>
    /// Verifies that a root scalar that cannot convert to the target type throws
    /// <see cref="YamlSerializationException" />, as the same value one level down does, at the root value, line 1,
    /// column 1, offset 0, with the root's path, which no key names and so is <see langword="null" />, and the
    /// converter's <see cref="FormatException" /> as its inner exception.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRootScalarCannotConvert_ShouldThrowYamlSerializationExceptionAtRoot()
    {
        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<int>("x");
        });

        Assert.AreEqual(1, ex.LineNumber, "LineNumber");
        Assert.AreEqual(1, ex.ColumnNumber, "ColumnNumber");
        Assert.AreEqual(0, ex.Offset, "Offset");
        Assert.IsNull(ex.Path, "Path");
        Assert.IsNotNull(ex.InnerException);
        Assert.AreEqual(typeof(FormatException), ex.InnerException.GetType());
    }

    /// <summary>
    /// Verifies that a root scalar that a built-in converter cannot parse as the target type throws
    /// <see cref="YamlSerializationException" /> at the root value, wrapping the parse failure, for each type whose
    /// converter parses the scalar's text.
    /// </summary>
    /// <param name="type">The target type.</param>
    /// <param name="yaml">A document whose root scalar does not parse as the target type.</param>
    [TestMethod]
    [DataRow(typeof(int), "x", DisplayName = "int")]
    [DataRow(typeof(decimal), "x", DisplayName = "decimal")]
    [DataRow(typeof(double), "x", DisplayName = "double")]
    [DataRow(typeof(float), "x", DisplayName = "float")]
    [DataRow(typeof(Guid), "x", DisplayName = "Guid")]
    [DataRow(typeof(DateTime), "x", DisplayName = "DateTime")]
    [DataRow(typeof(DateTimeOffset), "x", DisplayName = "DateTimeOffset")]
    [DataRow(typeof(TimeSpan), "x", DisplayName = "TimeSpan")]
    public void Deserialize_WhenRootScalarCannotParse_ForBuiltInType_ShouldWrapParseFailure(Type type, string yaml)
    {
        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize(Encoding.UTF8.GetBytes(yaml), type);
        });

        Assert.AreEqual(0, ex.Offset, "Offset");
        Assert.IsInstanceOfType<FormatException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a <see cref="YamlSerializationException" /> that a root value's converter throws itself passes
    /// through as thrown, taking the root value's position, rather than being wrapped in another.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRootConverterThrowsYamlSerializationException_ShouldNotWrapIt()
    {
        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<long>("99999999999999999999");
        });

        Assert.AreEqual(0, ex.Offset, "Offset");
        Assert.IsInstanceOfType<OverflowException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a document that is not valid YAML throws <see cref="YamlFormatException" />, unwrapped, whatever
    /// the root's target type.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDocumentIsMalformed_ShouldThrowYamlFormatException()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            _ = YamlSerializer.Deserialize<int>("[1, 2");
        });
    }

    /// <summary>A model holding a timestamp.</summary>
    private sealed class BirthRecord
    {
        /// <summary>Gets or sets the moment of birth.</summary>
        /// <value>The moment of birth.</value>
        public DateTime MomentOfBirth { get; set; }
    }
}
