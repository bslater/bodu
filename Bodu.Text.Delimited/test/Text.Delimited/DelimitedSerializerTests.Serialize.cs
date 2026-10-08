// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;

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

    /// <summary>
    /// Verifies that with <see cref="DelimitedSerializerOptions.NoHeader" /> set, serializing records writes their value
    /// rows without the header row.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNoHeaderIsSet_ShouldNotWriteTheHeaderRow()
    {
        var options = new DelimitedSerializerOptions { NoHeader = true };

        string text = DelimitedSerializer.Serialize(new List<NameRecord> { new() { Name = "test" } }, options);

        Assert.AreEqual("test\r\n", text);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> property is written as an empty field that keeps its column.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAPropertyIsNull_ShouldWriteAnEmptyField()
    {
        string text = DelimitedSerializer.Serialize(new List<StringPairRecord> { new() { A = null, B = "x" } });

        Assert.AreEqual("A,B\r\n,x\r\n", text);
    }

    /// <summary>
    /// Verifies that a property that hides a base type's property of the same name with the <see langword="new" />
    /// modifier is written as a single column, the hiding property's.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAPropertyHidesABasePropertyWithNew_ShouldWriteOneColumn()
    {
        string text = DelimitedSerializer.Serialize(new List<HidingRecord> { new() { Name = "x" } });

        Assert.AreEqual("Name\r\nx\r\n", text);
    }

    /// <summary>
    /// Verifies that serializing records whose every property is ignored does not throw, and writes neither the ignored
    /// names nor their values.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenEveryPropertyIsIgnored_ShouldNotThrow()
    {
        string text = DelimitedSerializer.Serialize(new List<IgnoredRecord> { new() { Secret = "hidden", Comment = "unseen" } });

        Assert.DoesNotContain("Secret", text);
        Assert.DoesNotContain("hidden", text);
        Assert.DoesNotContain("Comment", text);
        Assert.DoesNotContain("unseen", text);
    }

    /// <summary>
    /// Verifies that a static property of the record type is not written as a column.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheRecordTypeHasAStaticProperty_ShouldNotWriteIt()
    {
        string text = DelimitedSerializer.Serialize(new List<StaticPropertyRecord> { new() { A = "x" } });

        Assert.AreEqual("A\r\nx\r\n", text);
    }

    /// <summary>
    /// Verifies that serializing an empty collection writes the header row alone, the delimited guide taking the header
    /// row from the record type.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheCollectionIsEmpty_ShouldWriteTheHeaderRow()
    {
        string text = DelimitedSerializer.Serialize(new List<Trade>());

        Assert.AreEqual("TradeId,Symbol,Price\r\n", text);
    }

    /// <summary>
    /// Verifies that serializing an empty array to a buffer writer writes the header row alone.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAnEmptyArrayIsWrittenToABufferWriter_ShouldWriteTheHeaderRow()
    {
        var buffer = new ArrayBufferWriter<byte>();

        DelimitedSerializer.Serialize(buffer, Array.Empty<Trade>());

        Assert.AreEqual("TradeId,Symbol,Price\r\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that with <see cref="DelimitedSerializerOptions.NoHeader" /> set, serializing an empty collection writes
    /// nothing at all.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheCollectionIsEmptyAndNoHeaderIsSet_ShouldWriteNothing()
    {
        string text = DelimitedSerializer.Serialize(new List<Trade>(), new DelimitedSerializerOptions { NoHeader = true });

        Assert.AreEqual(string.Empty, text);
    }

    /// <summary>
    /// Verifies that an empty collection of positional <see cref="string" /> array records, which have no header, writes
    /// nothing.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAnEmptyCollectionHoldsPositionalRecords_ShouldWriteNothing()
    {
        string text = DelimitedSerializer.Serialize(new List<string[]>());

        Assert.AreEqual(string.Empty, text);
    }

    /// <summary>
    /// Verifies that serializing an empty collection through a record factory writes the factory's header row alone,
    /// and nothing with <see cref="DelimitedSerializerOptions.NoHeader" /> set.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAFactoryIsGivenAnEmptyCollection_ShouldWriteTheHeaderRow()
    {
        string withHeader = DelimitedSerializer.Serialize(new List<Person>(), new PersonFactory());
        string withoutHeader = DelimitedSerializer.Serialize(new List<Person>(), new PersonFactory(), new DelimitedSerializerOptions { NoHeader = true });

        Assert.AreEqual("Name,Age\r\n", withHeader);
        Assert.AreEqual(string.Empty, withoutHeader);
    }

    /// <summary>
    /// Verifies that under a current culture whose decimal separator is a comma, a <see cref="double" /> is written with
    /// the invariant culture's decimal point and reads back to the same value.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheCurrentCultureUsesADecimalComma_ShouldWriteInvariantNumbers()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        string text;
        List<DoubleRecord> restored;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            text = DelimitedSerializer.Serialize(new List<DoubleRecord> { new() { X = 1.5 } });
            restored = DelimitedSerializer.Deserialize<DoubleRecord>(text);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        Assert.AreEqual("X\r\n1.5\r\n", text);
        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual(1.5, restored[0].X);
    }

    /// <summary>
    /// Verifies that <see cref="double.NaN" /> is written as <c>NaN</c> and reads back as NaN.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenADoubleIsNaN_ShouldWriteNaN()
    {
        string text = DelimitedSerializer.Serialize(new List<DoubleRecord> { new() { X = double.NaN } });
        List<DoubleRecord> restored = DelimitedSerializer.Deserialize<DoubleRecord>(text);

        Assert.AreEqual("X\r\nNaN\r\n", text);
        Assert.AreEqual(1, restored.Count);
        Assert.IsTrue(double.IsNaN(restored[0].X));
    }

    /// <summary>
    /// Verifies that <see cref="long.MinValue" /> and <see cref="long.MaxValue" /> are written with every digit and read
    /// back unchanged.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenALongPropertyHoldsItsExtremes_ShouldWriteEveryDigit()
    {
        var records = new List<FileSizeRecord> { new() { FileSize = long.MinValue }, new() { FileSize = long.MaxValue } };

        string text = DelimitedSerializer.Serialize(records);
        List<FileSizeRecord> restored = DelimitedSerializer.Deserialize<FileSizeRecord>(text);

        Assert.AreEqual("FileSize\r\n-9223372036854775808\r\n9223372036854775807\r\n", text);
        Assert.AreEqual(2, restored.Count);
        Assert.AreEqual(long.MinValue, restored[0].FileSize);
        Assert.AreEqual(long.MaxValue, restored[1].FileSize);
    }

    /// <summary>
    /// Verifies that a formatted value holding the delimiter is quoted: with <c>.</c> as the delimiter, 1.5 is written
    /// <c>"1.5"</c> and reads back as 1.5.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAFormattedValueHoldsTheDelimiter_ShouldQuoteIt()
    {
        var options = new DelimitedSerializerOptions { Delimiter = '.' };

        string text = DelimitedSerializer.Serialize(new List<DoubleRecord> { new() { X = 1.5 } }, options);
        List<DoubleRecord> restored = DelimitedSerializer.Deserialize<DoubleRecord>(text, options);

        Assert.AreEqual("X\r\n\"1.5\"\r\n", text);
        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual(1.5, restored[0].X);
    }

    /// <summary>
    /// Verifies that <see langword="null" /> nullable properties are written as empty fields after the values before
    /// them.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenANullablePropertyIsNull_ShouldWriteAnEmptyField()
    {
        string text = DelimitedSerializer.Serialize(new List<NullableColumnsRecord> { new() { Id = 1, Value = null, Name = null } });

        Assert.AreEqual("Id,Value,Name\r\n1,,\r\n", text);
    }

    /// <summary>
    /// Verifies that serializing with a delimiter equal to the quote character throws the writer's
    /// <see cref="ArgumentException" /> for <c>options</c>, naming both options, whether or not there are records.
    /// </summary>
    /// <param name="count">The number of records serialized.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(2)]
    public void Serialize_WhenTheDelimiterIsTheQuote_ShouldThrowArgumentException(int count)
    {
        var options = new DelimitedSerializerOptions { Delimiter = '\'', Quote = '\'' };
        List<Person> people = Enumerable.Range(0, count).Select(i => new Person { Name = "P" + i, Age = i }).ToList();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = DelimitedSerializer.Serialize(people, options);
        });

        Assert.AreEqual("options", ex.ParamName);
        Assert.Contains("Delimiter", ex.Message);
        Assert.Contains("Quote", ex.Message);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedSerializerOptions.CommentChar" /> reaches the writer: with <c>;</c> a record
    /// whose first value begins with <c>;</c> is quoted and one beginning with <c>#</c> is written bare.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheCommentCharIsSet_ShouldQuoteAFirstValueBeginningWithIt()
    {
        var people = new List<Person> { new() { Name = ";x", Age = 1 }, new() { Name = "#y", Age = 2 } };

        string text = DelimitedSerializer.Serialize(people, new DelimitedSerializerOptions { CommentChar = ';' });

        Assert.AreEqual("Name,Age\r\n\";x\",1\r\n#y,2\r\n", text);
    }

    /// <summary>
    /// Verifies that serializing with a comment character equal to the delimiter throws the writer's
    /// <see cref="ArgumentException" /> for <c>options</c>, naming both options.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheCommentCharIsTheDelimiter_ShouldThrowArgumentException()
    {
        var options = new DelimitedSerializerOptions { Delimiter = ';', CommentChar = ';' };
        var people = new List<Person> { new() { Name = "Ada", Age = 36 } };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = DelimitedSerializer.Serialize(people, options);
        });

        Assert.AreEqual("options", ex.ParamName);
        Assert.Contains("CommentChar", ex.Message);
        Assert.Contains("Delimiter", ex.Message);
    }

    /// <summary>
    /// Verifies that temporal values are written in their invariant round-trip forms: <c>O</c> for
    /// <see cref="DateTime" /> and <see cref="DateTimeOffset" />, ISO 8601 dates and times for <see cref="DateOnly" />
    /// and <see cref="TimeOnly" />, and the constant <c>c</c> form for <see cref="TimeSpan" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenARecordHoldsTemporalValues_ShouldWriteTheirRoundTripForms()
    {
        var record = new TemporalRecord
        {
            At = new DateTime(2021, 2, 6, 1, 2, 3, DateTimeKind.Utc).AddTicks(4567891),
            Moment = new DateTimeOffset(2021, 2, 6, 1, 2, 3, TimeSpan.FromHours(10)).AddTicks(4567891),
            Day = new DateOnly(2021, 2, 6),
            Time = new TimeOnly(1, 2, 3).Add(TimeSpan.FromTicks(4567891)),
            Span = new TimeSpan(1, 2, 3, 4, 567),
        };

        string text = DelimitedSerializer.Serialize(new List<TemporalRecord> { record });

        Assert.AreEqual(
            "At,Moment,Day,Time,Span\r\n"
            + "2021-02-06T01:02:03.4567891Z,2021-02-06T01:02:03.4567891+10:00,2021-02-06,01:02:03.4567891,1.02:03:04.5670000\r\n",
            text);
    }
}
