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
    /// Verifies that a <see cref="Uri" /> column binds through the common scalar set, matching the DotEnv and INI
    /// binders.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUriColumn_ShouldBindUri()
    {
        List<LinkRecord> records = DelimitedSerializer.Deserialize<LinkRecord>("Site\nhttps://example.com/docs\n");

        Assert.AreEqual(1, records.Count);
        Assert.AreEqual(new Uri("https://example.com/docs"), records[0].Site);
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

    /// <summary>
    /// Verifies that a <see cref="char" /> column holding a single space binds the space rather than trimming it away.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenACharColumnIsASpace_ShouldBindTheSpace()
    {
        List<CharRecord> records = DelimitedSerializer.Deserialize<CharRecord>("c\n\" \"\n", s_caseInsensitive);

        Assert.AreEqual(1, records.Count);
        Assert.AreEqual(' ', records[0].C);
    }

    /// <summary>
    /// Verifies that a nullable <see cref="DateTime" /> column holding only white space binds <see langword="null" />,
    /// as an empty field does.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenANullableDateTimeColumnIsWhiteSpace_ShouldBindNull()
    {
        List<NullableDateTimeRecord> records = DelimitedSerializer.Deserialize<NullableDateTimeRecord>("d\n\" \"\n", s_caseInsensitive);

        Assert.AreEqual(1, records.Count);
        Assert.IsNull(records[0].D);
    }

    /// <summary>
    /// Verifies that a column the record type does not map may hold bytes that are not valid UTF-8 without failing the
    /// binding of the mapped column.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAnUnmappedColumnHoldsInvalidUtf8_ShouldBindTheMappedColumns()
    {
        byte[] source = [.. "a,b\nok,"u8, 0xFF, (byte)'\n'];

        List<ColumnARecord> records = DelimitedSerializer.Deserialize<ColumnARecord>(source, s_caseInsensitive);

        Assert.AreEqual(1, records.Count);
        Assert.AreEqual("ok", records[0].A);
    }

    /// <summary>
    /// Verifies that a record with fewer fields than the header throws <see cref="DelimitedFormatException" />, since
    /// the serializer reads with the strict field-count policy the reader documents as its default.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenARecordIsShorterThanTheHeader_ShouldThrowDelimitedFormatException()
    {
        Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = DelimitedSerializer.Deserialize<LetterRecord>("a,b,c\n1,2\n", s_caseInsensitive);
        });
    }

    /// <summary>
    /// Verifies that a column holding the names of an enumeration's members binds those members.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAColumnHoldsAnEnumMemberName_ShouldBindTheEnum()
    {
        List<CodeRecord> records = DelimitedSerializer.Deserialize<CodeRecord>("a,b\nOne,1\nTwo,2\nThree,3\n", s_caseInsensitive);

        Assert.AreEqual(3, records.Count);
        Assert.AreEqual(Code.One, records[0].A);
        Assert.AreEqual(1, records[0].B);
        Assert.AreEqual(Code.Two, records[1].A);
        Assert.AreEqual(2, records[1].B);
        Assert.AreEqual(Code.Three, records[2].A);
        Assert.AreEqual(3, records[2].B);
    }

    /// <summary>
    /// Verifies that a <see cref="double" /> or <see cref="float" /> column holding <c>1,234,567.89</c>, with group
    /// separators and with surrounding spaces, parses as 1234567.89.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAFloatingPointColumnHoldsGroupSeparators_ShouldParseIt()
    {
        const string Text = "X\n\"1,234,567.89\"\n\" 1,234,567.89 \"\n";

        List<DoubleRecord> doubles = DelimitedSerializer.Deserialize<DoubleRecord>(Text);
        List<FloatRecord> floats = DelimitedSerializer.Deserialize<FloatRecord>(Text);

        Assert.AreEqual(2, doubles.Count);
        Assert.AreEqual(1234567.89, doubles[0].X);
        Assert.AreEqual(1234567.89, doubles[1].X);
        Assert.AreEqual(2, floats.Count);
        Assert.AreEqual(1234567.89f, floats[0].X);
        Assert.AreEqual(1234567.89f, floats[1].X);
    }

    /// <summary>
    /// Verifies that a <see cref="double" /> or <see cref="float" /> column holding <c>Infinity</c> and
    /// <c>-Infinity</c> binds positive and negative infinity.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAFloatingPointColumnHoldsInfinity_ShouldBindInfinities()
    {
        const string Text = "X\nInfinity\n-Infinity\n";

        List<DoubleRecord> doubles = DelimitedSerializer.Deserialize<DoubleRecord>(Text);
        List<FloatRecord> floats = DelimitedSerializer.Deserialize<FloatRecord>(Text);

        Assert.AreEqual(2, doubles.Count);
        Assert.AreEqual(double.PositiveInfinity, doubles[0].X);
        Assert.AreEqual(double.NegativeInfinity, doubles[1].X);
        Assert.AreEqual(2, floats.Count);
        Assert.AreEqual(float.PositiveInfinity, floats[0].X);
        Assert.AreEqual(float.NegativeInfinity, floats[1].X);
    }

    /// <summary>
    /// Verifies that a stream whose <see cref="Stream.Length" /> exceeds <see cref="int.MaxValue" /> is read to its
    /// end, both by <see cref="DelimitedSerializer.Deserialize{TRecord}(Stream, DelimitedSerializerOptions?)" /> and by
    /// <see cref="DelimitedSerializer.DeserializeAsyncEnumerableAsync{TRecord}(Stream, DelimitedSerializerOptions?, CancellationToken)" />.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task Deserialize_WhenTheStreamLengthExceedsIntMaxValue_ShouldRead()
    {
        byte[] content = "A;B\n1;2"u8.ToArray();
        var options = new DelimitedSerializerOptions { Delimiter = ';' };
        using var bufferedSource = new LongLengthStream(content);
        using var streamedSource = new LongLengthStream(content);

        List<LetterRecord> buffered = DelimitedSerializer.Deserialize<LetterRecord>(bufferedSource, options);
        List<LetterRecord> streamed = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<LetterRecord>(streamedSource, options));

        foreach (List<LetterRecord> records in new[] { buffered, streamed })
        {
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("1", records[0].A);
            Assert.AreEqual("2", records[0].B);
        }
    }

    /// <summary>
    /// Verifies that an empty field bound to a non-nullable enumeration throws
    /// <see cref="DelimitedSerializationException" /> rather than binding the zero member, while the same field bound to
    /// a nullable enumeration binds <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAnEmptyFieldBindsANonNullableEnum_ShouldThrowDelimitedSerializationException()
    {
        const string Text = "A,B\n,1\n";

        Assert.ThrowsExactly<DelimitedSerializationException>(() =>
        {
            _ = DelimitedSerializer.Deserialize<KindRecord>(Text);
        });

        List<NullableKindRecord> records = DelimitedSerializer.Deserialize<NullableKindRecord>(Text);
        Assert.AreEqual(1, records.Count);
        Assert.IsNull(records[0].A);
        Assert.AreEqual(1, records[0].B);
    }

    /// <summary>
    /// Verifies that an empty quoted field in a nullable <see cref="int" /> column binds <see langword="null" />, beside a
    /// quoted <c>1</c> that binds 1.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenANullableIntColumnIsAnEmptyQuotedField_ShouldBindNull()
    {
        List<NullableIntPairRecord> records = DelimitedSerializer.Deserialize<NullableIntPairRecord>("a,b\n\"1\",\"\"\n", s_caseInsensitive);

        Assert.AreEqual(1, records.Count);
        Assert.AreEqual(1, records[0].A);
        Assert.IsNull(records[0].B);
    }

    /// <summary>
    /// Verifies that an empty quoted field in a non-nullable <see cref="int" /> column throws
    /// <see cref="DelimitedSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAnIntColumnIsAnEmptyQuotedField_ShouldThrowDelimitedSerializationException()
    {
        Assert.ThrowsExactly<DelimitedSerializationException>(() =>
        {
            _ = DelimitedSerializer.Deserialize<IntPairRecord>("a,b\n\"1\",\"\"\n", s_caseInsensitive);
        });
    }

    /// <summary>
    /// Verifies that a nullable <see cref="int" /> column holding only white space binds <see langword="null" />, between
    /// records that bind 1 and 3.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenANullableIntColumnIsWhiteSpace_ShouldBindNull()
    {
        List<NameValueRecord> records = DelimitedSerializer.Deserialize<NameValueRecord>("Name,Value\nA,1\nB, \nC,3");

        Assert.AreEqual(3, records.Count);
        Assert.AreEqual(1, records[0].Value);
        Assert.IsNull(records[1].Value);
        Assert.AreEqual(3, records[2].Value);
    }

    /// <summary>
    /// Verifies that deserializing with a line feed as the delimiter throws the reader's
    /// <see cref="ArgumentException" /> for <c>options</c>, naming the <c>Delimiter</c> option, rather than reading
    /// every line as one record.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheDelimiterIsALineFeed_ShouldThrowArgumentException()
    {
        var options = new DelimitedSerializerOptions { Delimiter = '\n', NoHeader = true };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = DelimitedSerializer.Deserialize<string[]>("a,b\nc,d\n", options);
        });

        Assert.AreEqual("options", ex.ParamName);
        Assert.Contains("Delimiter", ex.Message);
    }

    /// <summary>
    /// Verifies that the serializer reads a line beginning with the comment character as a record, since it never skips
    /// comment lines: <c>#x,1</c> binds a person named <c>#x</c>.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenALineBeginsWithTheCommentChar_ShouldReadItAsARecord()
    {
        List<Person> people = DelimitedSerializer.Deserialize<Person>("Name,Age\n#x,1\n");

        Assert.AreEqual(1, people.Count);
        Assert.AreEqual("#x", people[0].Name);
        Assert.AreEqual(1, people[0].Age);
    }

    /// <summary>
    /// Verifies that deserializing with a comment character equal to the quote throws the reader's
    /// <see cref="ArgumentException" /> for <c>options</c>, naming both options.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheCommentCharIsTheQuote_ShouldThrowArgumentException()
    {
        var options = new DelimitedSerializerOptions { CommentChar = '"' };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = DelimitedSerializer.Deserialize<Person>("Name,Age\nAda,36\n", options);
        });

        Assert.AreEqual("options", ex.ParamName);
        Assert.Contains("CommentChar", ex.Message);
        Assert.Contains("Quote", ex.Message);
    }

    /// <summary>
    /// Verifies that a nullable <see cref="decimal" /> column holding only white space, spaces, a tab or both, binds
    /// <see langword="null" /> as an empty field does, while a value beside them still binds.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenANullableDecimalColumnIsWhiteSpace_ShouldBindNull()
    {
        List<IdValueRecord<decimal?>> records = DelimitedSerializer.Deserialize<IdValueRecord<decimal?>>("Id,Value\n1,   \n2,\t\n3,\" \t \"\n4,4.5\n");

        Assert.AreEqual(4, records.Count);
        Assert.IsNull(records[0].Value);
        Assert.IsNull(records[1].Value);
        Assert.IsNull(records[2].Value);
        Assert.AreEqual(4.5m, records[3].Value);
    }

    /// <summary>
    /// Verifies that a non-nullable <see cref="int" /> column holding only white space still throws
    /// <see cref="DelimitedSerializationException" />, since it has no value to bind.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenANonNullableIntColumnIsWhiteSpace_ShouldThrowDelimitedSerializationException()
    {
        Assert.ThrowsExactly<DelimitedSerializationException>(() =>
        {
            _ = DelimitedSerializer.Deserialize<IdValueRecord<int>>("Id,Value\n1,   \n");
        });
    }

    /// <summary>
    /// Verifies that a <see cref="string" /> column holding only white space keeps the white space as its value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAStringColumnIsWhiteSpace_ShouldKeepTheWhiteSpace()
    {
        List<IdValueRecord<string>> records = DelimitedSerializer.Deserialize<IdValueRecord<string>>("Id,Value\n1,   \n");

        Assert.AreEqual(1, records.Count);
        Assert.AreEqual("   ", records[0].Value);
    }

    /// <summary>
    /// Verifies that temporal columns written in the invariant general forms of earlier versions still parse: a
    /// <see cref="DateTime" />, a <see cref="DateTimeOffset" />, a <see cref="DateOnly" /> and a <see cref="TimeOnly" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTemporalColumnsHoldTheGeneralForms_ShouldParseThem()
    {
        List<TemporalRecord> records = DelimitedSerializer.Deserialize<TemporalRecord>(
            "At,Moment,Day,Time,Span\n02/06/2021 01:02:03,02/06/2021 01:02:03 +10:00,02/06/2021,01:02,1.02:03:04.5670000\n");

        Assert.AreEqual(1, records.Count);
        Assert.AreEqual(new DateTime(2021, 2, 6, 1, 2, 3, DateTimeKind.Unspecified), records[0].At);
        Assert.AreEqual(new DateTimeOffset(2021, 2, 6, 1, 2, 3, TimeSpan.FromHours(10)), records[0].Moment);
        Assert.AreEqual(new DateOnly(2021, 2, 6), records[0].Day);
        Assert.AreEqual(new TimeOnly(1, 2), records[0].Time);
        Assert.AreEqual(new TimeSpan(1, 2, 3, 4, 567), records[0].Span);
    }
}
