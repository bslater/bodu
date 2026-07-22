// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Contains the <see cref="DelimitedSerializer.Serialize{T}(T, DelimitedSerializerOptions?)" /> backbone tests.
/// </summary>
public partial class DelimitedSerializerTests
{
    /// <summary>
    /// Verifies that a list of record POCOs serializes to a header row followed by one value row per record.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRecordList_ShouldWriteHeaderAndRows()
    {
        var people = new List<Person>
        {
            new() { Name = "Ada", Age = 36 },
            new() { Name = "Grace", Age = 45 },
        };

        string text = DelimitedSerializer.Serialize(people);

        Assert.AreEqual("Name,Age\r\nAda,36\r\nGrace,45\r\n", text);
    }

    /// <summary>
    /// Verifies that a list of positional <see cref="string" /> arrays serializes headerless.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStringArrays_ShouldWritePositionalRows()
    {
        var rows = new List<string[]> { new[] { "Ada", "36" }, new[] { "Grace", "45" } };
        var options = new DelimitedSerializerOptions { NoHeader = true };

        string text = DelimitedSerializer.Serialize(rows, options);

        Assert.AreEqual("Ada,36\r\nGrace,45\r\n", text);
    }

    /// <summary>
    /// Verifies that a non-collection root throws <see cref="DelimitedSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNonCollectionRoot_ShouldThrowDelimitedSerializationException()
    {
        Assert.ThrowsExactly<DelimitedSerializationException>(() =>
        {
            _ = DelimitedSerializer.Serialize(new Person { Name = "Ada", Age = 36 });
        });
    }

    /// <summary>
    /// Verifies that the Web defaults emit <c>snake_case</c> column names.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenWebDefaults_ShouldUseSnakeCaseHeaders()
    {
        var options = new DelimitedSerializerOptions(DelimitedSerializerDefaults.Web);
        var people = new List<Person> { new() { Name = "Ada", Age = 36 } };

        string text = DelimitedSerializer.Serialize(people, options);

        Assert.IsTrue(text.StartsWith("name,age\r\n", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the reflection-free factory overload produces byte-identical output to the reflection binder,
    /// including the header row and quoting.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFactory_ShouldMatchReflectionOutput()
    {
        var people = new List<Person>
        {
            new() { Name = "Ada, the \"pioneer\"", Age = 36 },
            new() { Name = "Grace", Age = 45 },
        };

        string reflection = DelimitedSerializer.Serialize(people);
        string viaFactory = DelimitedSerializer.Serialize(people, new PersonFactory());

        Assert.AreEqual(reflection, viaFactory);
    }

    /// <summary>
    /// Verifies that the factory overload suppresses the header row in headerless mode.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFactoryWithNoHeader_ShouldWritePositionalRows()
    {
        var people = new List<Person> { new() { Name = "Ada", Age = 36 } };
        var options = new DelimitedSerializerOptions { NoHeader = true };

        string text = DelimitedSerializer.Serialize(people, new PersonFactory(), options);

        Assert.AreEqual("Ada,36\r\n", text);
    }

    /// <summary>
    /// Verifies that the factory overload throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> record sequence or factory.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFactoryArgumentsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DelimitedSerializer.Serialize((IEnumerable<Person>)null!, new PersonFactory());
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DelimitedSerializer.Serialize(new List<Person>(), (IDelimitedRecordFactory<Person>)null!);
        });
    }
}
