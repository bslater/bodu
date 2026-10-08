// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

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
    /// Verifies that a <see cref="double" /> that needs seventeen significant digits, 0.1 + 0.2, is written as
    /// <c>0.30000000000000004</c> and reads back to the same value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenADoubleNeedsSeventeenDigits_ShouldRoundTripIt()
    {
        const double Value = 0.1 + 0.2;

        string text = DelimitedSerializer.Serialize(new List<DoubleRecord> { new() { X = Value } });
        List<DoubleRecord> restored = DelimitedSerializer.Deserialize<DoubleRecord>(text);

        Assert.AreEqual("X\r\n0.30000000000000004\r\n", text);
        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual(Value, restored[0].X);
    }

    /// <summary>
    /// Verifies that a <see cref="DateTime" /> with seven digits of fractional seconds and
    /// <see cref="DateTimeKind.Utc" /> reads back with the same ticks and the same kind.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenADateTimeHasFractionalSecondsAndUtcKind_ShouldRoundTripIt()
    {
        DateTime value = new DateTime(2021, 2, 6, 1, 2, 3, DateTimeKind.Utc).AddTicks(4567891);

        string text = DelimitedSerializer.Serialize(new List<TimestampRecord> { new() { At = value } });
        List<TimestampRecord> restored = DelimitedSerializer.Deserialize<TimestampRecord>(text);

        // The round-trip format shows every tick and the kind, which DateTime equality and its default text do not.
        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual(
            value.ToString("O", CultureInfo.InvariantCulture),
            restored[0].At.ToString("O", CultureInfo.InvariantCulture),
            $"The serializer wrote {text.ReplaceLineEndings(@"\r\n")}.");
    }

    /// <summary>
    /// Verifies that a <see cref="DateTime" /> with seven digits of fractional seconds reads back with the same ticks
    /// and the same kind, whichever <see cref="DateTimeKind" /> it has.
    /// </summary>
    /// <param name="kind">The value's kind.</param>
    [TestMethod]
    [DataRow(DateTimeKind.Utc)]
    [DataRow(DateTimeKind.Local)]
    [DataRow(DateTimeKind.Unspecified)]
    public void SerializeDeserialize_WhenADateTimeHasFractionalSeconds_ShouldRoundTripItsTicksAndKind(DateTimeKind kind)
    {
        DateTime value = new DateTime(2021, 2, 6, 1, 2, 3, kind).AddTicks(4567891);

        string text = DelimitedSerializer.Serialize(new List<IdValueRecord<DateTime>> { new() { Id = 1, Value = value } });
        List<IdValueRecord<DateTime>> restored = DelimitedSerializer.Deserialize<IdValueRecord<DateTime>>(text);

        // The round-trip format shows every tick and the kind, which DateTime equality and its default text do not.
        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual(
            value.ToString("O", CultureInfo.InvariantCulture),
            restored[0].Value.ToString("O", CultureInfo.InvariantCulture),
            $"The serializer wrote {text.ReplaceLineEndings(@"\r\n")}.");
    }

    /// <summary>
    /// Verifies that a <see cref="DateTimeOffset" /> with seven digits of fractional seconds reads back with the same
    /// ticks and the same offset, for a zero, a positive and a negative offset.
    /// </summary>
    /// <param name="offsetMinutes">The value's offset from UTC, in minutes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(600)]
    [DataRow(-330)]
    public void SerializeDeserialize_WhenADateTimeOffsetHasFractionalSeconds_ShouldRoundTripItsTicksAndOffset(int offsetMinutes)
    {
        DateTimeOffset value = new DateTimeOffset(2021, 2, 6, 1, 2, 3, TimeSpan.FromMinutes(offsetMinutes)).AddTicks(4567891);

        string text = DelimitedSerializer.Serialize(new List<IdValueRecord<DateTimeOffset>> { new() { Id = 1, Value = value } });
        List<IdValueRecord<DateTimeOffset>> restored = DelimitedSerializer.Deserialize<IdValueRecord<DateTimeOffset>>(text);

        // DateTimeOffset equality ignores the offset, so the round-trip text, which shows it, is compared instead.
        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual(
            value.ToString("O", CultureInfo.InvariantCulture),
            restored[0].Value.ToString("O", CultureInfo.InvariantCulture),
            $"The serializer wrote {text.ReplaceLineEndings(@"\r\n")}.");
    }

    /// <summary>
    /// Verifies that a <see cref="DateOnly" /> reads back as the same date.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenADateOnlyIsWritten_ShouldRoundTripIt()
    {
        var value = new DateOnly(2021, 2, 6);

        string text = DelimitedSerializer.Serialize(new List<IdValueRecord<DateOnly>> { new() { Id = 1, Value = value } });
        List<IdValueRecord<DateOnly>> restored = DelimitedSerializer.Deserialize<IdValueRecord<DateOnly>>(text);

        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual(value, restored[0].Value, $"The serializer wrote {text.ReplaceLineEndings(@"\r\n")}.");
    }

    /// <summary>
    /// Verifies that a <see cref="TimeOnly" /> with seven digits of fractional seconds reads back as the same time.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenATimeOnlyHasFractionalSeconds_ShouldRoundTripIt()
    {
        TimeOnly value = new TimeOnly(1, 2, 3).Add(TimeSpan.FromTicks(4567891));

        string text = DelimitedSerializer.Serialize(new List<IdValueRecord<TimeOnly>> { new() { Id = 1, Value = value } });
        List<IdValueRecord<TimeOnly>> restored = DelimitedSerializer.Deserialize<IdValueRecord<TimeOnly>>(text);

        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual(value, restored[0].Value, $"The serializer wrote {text.ReplaceLineEndings(@"\r\n")}.");
    }

    /// <summary>
    /// Verifies that a <see cref="TimeSpan" /> of several days with seven digits of fractional seconds reads back as
    /// the same duration, positive or negative.
    /// </summary>
    /// <param name="negative">Whether the duration is negative.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SerializeDeserialize_WhenATimeSpanHasDaysAndFractionalSeconds_ShouldRoundTripIt(bool negative)
    {
        TimeSpan value = new TimeSpan(3, 4, 5, 6).Add(TimeSpan.FromTicks(1234567));
        if (negative)
            value = value.Negate();

        string text = DelimitedSerializer.Serialize(new List<IdValueRecord<TimeSpan>> { new() { Id = 1, Value = value } });
        List<IdValueRecord<TimeSpan>> restored = DelimitedSerializer.Deserialize<IdValueRecord<TimeSpan>>(text);

        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual(value, restored[0].Value, $"The serializer wrote {text.ReplaceLineEndings(@"\r\n")}.");
    }

    /// <summary>
    /// Verifies that a nullable <see cref="char" /> reads back as it was written: a space and a tab as themselves, and
    /// <see langword="null" /> as <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenANullableCharIsWhiteSpaceOrNull_ShouldRoundTripIt()
    {
        var records = new List<IdValueRecord<char?>>
        {
            new() { Id = 1, Value = ' ' },
            new() { Id = 2, Value = '\t' },
            new() { Id = 3, Value = null },
        };

        string text = DelimitedSerializer.Serialize(records);
        List<IdValueRecord<char?>> restored = DelimitedSerializer.Deserialize<IdValueRecord<char?>>(text);

        string written = $"The serializer wrote {text.ReplaceLineEndings(@"\r\n").Replace("\t", @"\t", StringComparison.Ordinal)}.";
        Assert.AreEqual(3, restored.Count, written);
        Assert.AreEqual((char?)' ', restored[0].Value, written);
        Assert.AreEqual((char?)'\t', restored[1].Value, written);
        Assert.IsNull(restored[2].Value, written);
    }

    /// <summary>
    /// Verifies that a property hidden two levels down, behind a text and an <see cref="int" /> declaration, is written
    /// as one column, the most derived <see cref="decimal" /> property's, and reads back into it.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenAPropertyIsHiddenTwoLevelsDown_ShouldMapTheMostDerivedProperty()
    {
        string text = DelimitedSerializer.Serialize(new List<TwiceHidingRecord> { new() { Name = 1.5m } });
        List<TwiceHidingRecord> restored = DelimitedSerializer.Deserialize<TwiceHidingRecord>(text);

        Assert.AreEqual("Name\r\n1.5\r\n", text);
        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual(1.5m, restored[0].Name);
    }

    /// <summary>
    /// Verifies that a property that hides a base property of the same type with <see langword="new" /> is written as
    /// one column and reads back into the hiding property, leaving the hidden one unset.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenAPropertyHidesABasePropertyOfTheSameType_ShouldMapOneColumn()
    {
        string text = DelimitedSerializer.Serialize(new List<SameTypeHidingRecord> { new() { Name = "y" } });
        List<SameTypeHidingRecord> restored = DelimitedSerializer.Deserialize<SameTypeHidingRecord>(text);

        Assert.AreEqual("Name\r\ny\r\n", text);
        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual("y", restored[0].Name);
        Assert.IsNull(((TextNamedBase)restored[0]).Name);
    }

    /// <summary>
    /// Verifies that with <see cref="DelimitedSerializerOptions.IncludeFields" /> a field that hides a base field with
    /// <see langword="new" /> is written as one column, the hiding field's, and reads back into it.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenAFieldHidesABaseFieldWithNew_ShouldMapTheHidingField()
    {
        var options = new DelimitedSerializerOptions { IncludeFields = true };

        string text = DelimitedSerializer.Serialize(new List<FieldHidingRecord> { new() { Name = "f" } }, options);
        List<FieldHidingRecord> restored = DelimitedSerializer.Deserialize<FieldHidingRecord>(text, options);

        Assert.AreEqual("Name\r\nf\r\n", text);
        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual("f", restored[0].Name);
    }

    /// <summary>
    /// Verifies that 10,000 records, enough to pass the incremental writer's flush buffer several times, keep every
    /// value through a write to a stream and a read back, for <see cref="long" />, <see cref="DateTime" />,
    /// <see cref="double" /> and <see cref="Guid" /> values.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SerializeDeserialize_WhenManyRecordsPassTheWriterBuffer_ShouldKeepEveryValue()
    {
        await AssertManyRecordsRoundTripAsync(id => 1000L + id);

        // Whole minutes of unspecified kind; fractional seconds and every kind are the subject of the DateTime
        // round-trip tests above.
        await AssertManyRecordsRoundTripAsync(id => new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMinutes(id));
        await AssertManyRecordsRoundTripAsync(id => id / 7.0);
        await AssertManyRecordsRoundTripAsync(id => new Guid(id, (short)(id % 7), (short)(id % 11), 1, 2, 3, 4, 5, 6, 7, 8));
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

    /// <summary>
    /// Writes 10,000 records whose value column has a chosen type to a stream through the incremental
    /// <see cref="IAsyncEnumerable{T}" /> overload of <c>SerializeAsync</c>, reads them back, and asserts that every
    /// identifier and value survived.
    /// </summary>
    /// <typeparam name="TValue">The type of the value column.</typeparam>
    /// <param name="valueOf">Gives the value of the record with an identifier.</param>
    /// <returns>A task that completes when the records have been written, read back and compared.</returns>
    private static async Task AssertManyRecordsRoundTripAsync<TValue>(Func<int, TValue> valueOf)
    {
        const int Count = 10000;
        var records = new List<IdValueRecord<TValue>>(Count);
        for (int id = 0; id < Count; id++)
            records.Add(new IdValueRecord<TValue> { Id = id, Value = valueOf(id) });

        using var stream = new MemoryStream();
        await DelimitedSerializer.SerializeAsync(stream, ToAsyncEnumerable(records));
        stream.Position = 0;
        List<IdValueRecord<TValue>> restored = DelimitedSerializer.Deserialize<IdValueRecord<TValue>>(stream);

        Assert.AreEqual(Count, restored.Count, $"{typeof(TValue).Name}: the number of records differs.");
        for (int i = 0; i < Count; i++)
        {
            Assert.AreEqual(records[i].Id, restored[i].Id, $"{typeof(TValue).Name}: record {i} has another identifier.");
            Assert.AreEqual(records[i].Value, restored[i].Value, $"{typeof(TValue).Name}: record {i} has another value.");
        }
    }
}
