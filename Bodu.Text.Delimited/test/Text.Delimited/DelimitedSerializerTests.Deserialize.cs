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
}
