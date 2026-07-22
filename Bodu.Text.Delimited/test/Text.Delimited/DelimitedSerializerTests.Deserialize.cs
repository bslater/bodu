// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.Deserialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Contains the <see cref="DelimitedSerializer.Deserialize{TRecord}(string, DelimitedSerializerOptions?)" /> backbone
/// tests.
/// </summary>
public partial class DelimitedSerializerTests
{
    /// <summary>
    /// Verifies that a CSV binds to a list of record POCOs, converting typed columns.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRecords_ShouldBindColumns()
    {
        List<Person> people = DelimitedSerializer.Deserialize<Person>("Name,Age\nAda,36\nGrace,45\n");

        Assert.AreEqual(2, people.Count);
        Assert.AreEqual("Ada", people[0].Name);
        Assert.AreEqual(36, people[0].Age);
        Assert.AreEqual("Grace", people[1].Name);
        Assert.AreEqual(45, people[1].Age);
    }

    /// <summary>
    /// Verifies that a headerless CSV binds to a list of positional <see cref="string" /> arrays.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringArray_ShouldReturnRawFields()
    {
        var options = new DelimitedSerializerOptions { NoHeader = true };

        List<string[]> rows = DelimitedSerializer.Deserialize<string[]>("1,2\n3,4\n", options);

        Assert.AreEqual(2, rows.Count);
        CollectionAssert.AreEqual(new[] { "1", "2" }, rows[0]);
        CollectionAssert.AreEqual(new[] { "3", "4" }, rows[1]);
    }

    /// <summary>
    /// Verifies that an unconvertible column value throws <see cref="DelimitedSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenValueNotConvertible_ShouldThrowDelimitedSerializationException()
    {
        Assert.ThrowsExactly<DelimitedSerializationException>(() =>
        {
            _ = DelimitedSerializer.Deserialize<Person>("Name,Age\nAda,notanumber\n");
        });
    }

    /// <summary>
    /// Verifies that column matching binds regardless of column order in the source.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenColumnsReordered_ShouldBindByName()
    {
        List<Person> people = DelimitedSerializer.Deserialize<Person>("Age,Name\n36,Ada\n");

        Assert.AreEqual("Ada", people[0].Name);
        Assert.AreEqual(36, people[0].Age);
    }

    /// <summary>
    /// Verifies that the reflection-free factory overload binds records through the factory, including reordered
    /// columns mapped by header name.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenFactory_ShouldBindRecords()
    {
        List<Person> people = DelimitedSerializer.Deserialize("Age,Name\n36,Ada\n45,Grace\n", new PersonFactory());

        Assert.AreEqual(2, people.Count);
        Assert.AreEqual("Ada", people[0].Name);
        Assert.AreEqual(36, people[0].Age);
        Assert.AreEqual("Grace", people[1].Name);
        Assert.AreEqual(45, people[1].Age);
    }

    /// <summary>
    /// Verifies that the factory overload binds a headerless document positionally in the factory's declared field
    /// order.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenFactoryWithoutHeader_ShouldBindPositionally()
    {
        var options = new DelimitedSerializerOptions { NoHeader = true };

        List<Person> people = DelimitedSerializer.Deserialize("Ada,36\n", new PersonFactory(), options);

        Assert.AreEqual(1, people.Count);
        Assert.AreEqual("Ada", people[0].Name);
        Assert.AreEqual(36, people[0].Age);
    }

    /// <summary>
    /// Verifies that the factory overload throws <see cref="ArgumentNullException" /> for a <see langword="null" />
    /// factory.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenFactoryNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DelimitedSerializer.Deserialize("Name,Age\nAda,36\n", (IDelimitedRecordFactory<Person>)null!);
        });
    }
}
