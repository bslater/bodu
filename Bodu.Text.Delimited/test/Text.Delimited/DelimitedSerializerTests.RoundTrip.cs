// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Contains the serialize/deserialize round-trip and streaming tests for <see cref="DelimitedSerializer" />.
/// </summary>
public partial class DelimitedSerializerTests
{
    /// <summary>
    /// Verifies that a record list survives a serialize/deserialize round-trip.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenRecords_ShouldPreserveValues()
    {
        var original = new List<Person>
        {
            new() { Name = "Ada, the pioneer", Age = 36 },
            new() { Name = "Grace", Age = 45 },
        };

        string text = DelimitedSerializer.Serialize(original);
        List<Person> restored = DelimitedSerializer.Deserialize<Person>(text);

        Assert.AreEqual(original.Count, restored.Count);
        for (int i = 0; i < original.Count; i++)
        {
            Assert.AreEqual(original[i].Name, restored[i].Name);
            Assert.AreEqual(original[i].Age, restored[i].Age);
        }
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedSerializer.DeserializeAsyncEnumerableAsync{TRecord}(Stream, DelimitedSerializerOptions?, System.Threading.CancellationToken)" />
    /// yields each record from a stream.
    /// </summary>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenStream_ShouldYieldRecords()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("Name,Age\nAda,36\nGrace,45\n");
        using var stream = new MemoryStream(bytes);

        var names = new List<string>();
        await foreach (Person person in DelimitedSerializer.DeserializeAsyncEnumerableAsync<Person>(stream))
            names.Add(person.Name);

        CollectionAssert.AreEqual(new List<string> { "Ada", "Grace" }, names);
    }

    /// <summary>
    /// Verifies that the asynchronous-sequence serialize overload writes records read back identically.
    /// </summary>
    [TestMethod]
    public async Task SerializeAsync_WhenAsyncEnumerable_ShouldRoundTrip()
    {
        using var stream = new MemoryStream();
        await DelimitedSerializer.SerializeAsync(stream, GetPeopleAsync());

        stream.Position = 0;
        List<Person> restored = DelimitedSerializer.Deserialize<Person>(stream);

        Assert.AreEqual(2, restored.Count);
        Assert.AreEqual("Ada", restored[0].Name);
        Assert.AreEqual("Grace", restored[1].Name);
    }

    /// <summary>
    /// Yields a small asynchronous sequence of people for the streaming serialize test.
    /// </summary>
    /// <returns>An asynchronous sequence of people.</returns>
    private static async IAsyncEnumerable<Person> GetPeopleAsync()
    {
        yield return new Person { Name = "Ada", Age = 36 };
        await Task.Yield();
        yield return new Person { Name = "Grace", Age = 45 };
    }
}
