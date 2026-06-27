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
        var dict = YamlSerializer.Deserialize<Dictionary<string, int>>("a: 1\nb: 2\nc: 3\n")!;
        Assert.AreEqual(3, dict.Count);
        Assert.AreEqual(2, dict["b"]);
    }

    /// <summary>Verifies that the loosely-typed object binding produces nested dictionaries and lists.</summary>
    [TestMethod]
    public void Deserialize_WhenObject_ShouldBindDynamic()
    {
        var result = YamlSerializer.Deserialize<object>("name: x\nitems:\n  - 1\n  - 2\n");
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
        var person = YamlSerializer.Deserialize<Person>("name: Eve\nAGE: 30\nactive: true\n", options)!;
        Assert.AreEqual("Eve", person.Name);
        Assert.AreEqual(30, person.Age);
        Assert.IsTrue(person.Active);
    }
}
