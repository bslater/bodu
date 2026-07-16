// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Deserialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies <see cref="YamlSerializer.Deserialize{TValue}(string, YamlSerializerOptions)" />: dictionary binding,
/// loosely-typed object binding, and case-insensitive property matching.
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
}
