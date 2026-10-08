// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Delimited;

/// <summary>
/// Hosts the shared model types for the <see cref="DelimitedSerializer" /> test backbone.
/// </summary>
[TestClass]
public partial class DelimitedSerializerTests
{
    /// <summary>
    /// The options that bind the lower-case column names the release-note scenarios use (<c>a</c>, <c>b</c>, <c>c</c>)
    /// to the upper-case properties of the record types.
    /// </summary>
    private static readonly DelimitedSerializerOptions s_caseInsensitive = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// The values of the <see cref="CodeRecord.A" /> column.
    /// </summary>
    public enum Code
    {
        /// <summary>The first code.</summary>
        One,

        /// <summary>The second code.</summary>
        Two,

        /// <summary>The third code.</summary>
        Three,
    }

    /// <summary>
    /// The values of the <see cref="KindRecord.A" /> and <see cref="NullableKindRecord.A" /> columns, whose zero member
    /// an empty field must not silently produce.
    /// </summary>
    public enum Kind
    {
        /// <summary>No kind; the zero member.</summary>
        None,

        /// <summary>The first kind.</summary>
        One,
    }

    /// <summary>
    /// A record POCO of up to three text columns, <c>A</c>, <c>B</c> and <c>C</c>.
    /// </summary>
    public sealed class LetterRecord
    {
        /// <summary>Gets or sets the first column.</summary>
        /// <value>The first column's text.</value>
        public string? A { get; set; }

        /// <summary>Gets or sets the second column.</summary>
        /// <value>The second column's text.</value>
        public string? B { get; set; }

        /// <summary>Gets or sets the third column.</summary>
        /// <value>The third column's text.</value>
        public string? C { get; set; }
    }

    /// <summary>
    /// A record POCO that maps only column <c>A</c>, leaving any other column unmapped.
    /// </summary>
    public sealed class ColumnARecord
    {
        /// <summary>Gets or sets the mapped column.</summary>
        /// <value>The column's text.</value>
        public string? A { get; set; }
    }

    /// <summary>
    /// A record POCO with a single text column.
    /// </summary>
    public sealed class NameRecord
    {
        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string? Name { get; set; }
    }

    /// <summary>
    /// A record POCO of two nullable text columns, <c>A</c> and <c>B</c>.
    /// </summary>
    public sealed class StringPairRecord
    {
        /// <summary>Gets or sets the first column.</summary>
        /// <value>The first column's text, or <see langword="null" />.</value>
        public string? A { get; set; }

        /// <summary>Gets or sets the second column.</summary>
        /// <value>The second column's text, or <see langword="null" />.</value>
        public string? B { get; set; }
    }

    /// <summary>
    /// A record POCO with a <see cref="char" /> column.
    /// </summary>
    public sealed class CharRecord
    {
        /// <summary>Gets or sets the character.</summary>
        /// <value>The character.</value>
        public char C { get; set; }
    }

    /// <summary>
    /// A record POCO with a nullable <see cref="DateTime" /> column.
    /// </summary>
    public sealed class NullableDateTimeRecord
    {
        /// <summary>Gets or sets the date.</summary>
        /// <value>The date, or <see langword="null" />.</value>
        public DateTime? D { get; set; }
    }

    /// <summary>
    /// A record POCO with a <see cref="DateTime" /> column.
    /// </summary>
    public sealed class TimestampRecord
    {
        /// <summary>Gets or sets the instant.</summary>
        /// <value>The instant.</value>
        public DateTime At { get; set; }
    }

    /// <summary>
    /// A record POCO with a column of each temporal type the serializer formats.
    /// </summary>
    public sealed class TemporalRecord
    {
        /// <summary>Gets or sets the date and time.</summary>
        /// <value>The date and time.</value>
        public DateTime At { get; set; }

        /// <summary>Gets or sets the date and time with its offset.</summary>
        /// <value>The date and time with its offset.</value>
        public DateTimeOffset Moment { get; set; }

        /// <summary>Gets or sets the date.</summary>
        /// <value>The date.</value>
        public DateOnly Day { get; set; }

        /// <summary>Gets or sets the time of day.</summary>
        /// <value>The time of day.</value>
        public TimeOnly Time { get; set; }

        /// <summary>Gets or sets the duration.</summary>
        /// <value>The duration.</value>
        public TimeSpan Span { get; set; }
    }

    /// <summary>
    /// A record POCO with a <see cref="double" /> column.
    /// </summary>
    public sealed class DoubleRecord
    {
        /// <summary>Gets or sets the value.</summary>
        /// <value>The value.</value>
        public double X { get; set; }
    }

    /// <summary>
    /// A record POCO with a <see cref="float" /> column.
    /// </summary>
    public sealed class FloatRecord
    {
        /// <summary>Gets or sets the value.</summary>
        /// <value>The value.</value>
        public float X { get; set; }
    }

    /// <summary>
    /// A record POCO with a <see cref="long" /> column.
    /// </summary>
    public sealed class FileSizeRecord
    {
        /// <summary>Gets or sets the file size.</summary>
        /// <value>The file size in bytes.</value>
        public long FileSize { get; set; }
    }

    /// <summary>
    /// A record POCO with a <see cref="Code" /> column and an <see cref="int" /> column.
    /// </summary>
    public sealed class CodeRecord
    {
        /// <summary>Gets or sets the code.</summary>
        /// <value>The code.</value>
        public Code A { get; set; }

        /// <summary>Gets or sets the number.</summary>
        /// <value>The number.</value>
        public int B { get; set; }
    }

    /// <summary>
    /// A record POCO with a non-nullable <see cref="Kind" /> column and an <see cref="int" /> column.
    /// </summary>
    public sealed class KindRecord
    {
        /// <summary>Gets or sets the kind.</summary>
        /// <value>The kind.</value>
        public Kind A { get; set; }

        /// <summary>Gets or sets the number.</summary>
        /// <value>The number.</value>
        public int B { get; set; }
    }

    /// <summary>
    /// A record POCO with a nullable <see cref="Kind" /> column and an <see cref="int" /> column.
    /// </summary>
    public sealed class NullableKindRecord
    {
        /// <summary>Gets or sets the kind.</summary>
        /// <value>The kind, or <see langword="null" />.</value>
        public Kind? A { get; set; }

        /// <summary>Gets or sets the number.</summary>
        /// <value>The number.</value>
        public int B { get; set; }
    }

    /// <summary>
    /// A record POCO of two non-nullable <see cref="int" /> columns, <c>A</c> and <c>B</c>.
    /// </summary>
    public sealed class IntPairRecord
    {
        /// <summary>Gets or sets the first number.</summary>
        /// <value>The first number.</value>
        public int A { get; set; }

        /// <summary>Gets or sets the second number.</summary>
        /// <value>The second number.</value>
        public int B { get; set; }
    }

    /// <summary>
    /// A record POCO of two nullable <see cref="int" /> columns, <c>A</c> and <c>B</c>.
    /// </summary>
    public sealed class NullableIntPairRecord
    {
        /// <summary>Gets or sets the first number.</summary>
        /// <value>The first number, or <see langword="null" />.</value>
        public int? A { get; set; }

        /// <summary>Gets or sets the second number.</summary>
        /// <value>The second number, or <see langword="null" />.</value>
        public int? B { get; set; }
    }

    /// <summary>
    /// A record POCO with a text column and a nullable <see cref="int" /> column.
    /// </summary>
    public sealed class NameValueRecord
    {
        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string? Name { get; set; }

        /// <summary>Gets or sets the value.</summary>
        /// <value>The value, or <see langword="null" />.</value>
        public int? Value { get; set; }
    }

    /// <summary>
    /// A record POCO with an <see cref="int" /> column followed by a nullable value column and a nullable text column.
    /// </summary>
    public sealed class NullableColumnsRecord
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        public int Id { get; set; }

        /// <summary>Gets or sets the value.</summary>
        /// <value>The value, or <see langword="null" />.</value>
        public int? Value { get; set; }

        /// <summary>Gets or sets the name.</summary>
        /// <value>The name, or <see langword="null" />.</value>
        public string? Name { get; set; }
    }

    /// <summary>
    /// The trade record of the delimited guide, whose column names the header row is taken from.
    /// </summary>
    public sealed class Trade
    {
        /// <summary>Gets or sets the trade identifier.</summary>
        /// <value>The trade identifier.</value>
        public int TradeId { get; set; }

        /// <summary>Gets or sets the traded symbol.</summary>
        /// <value>The symbol.</value>
        public string? Symbol { get; set; }

        /// <summary>Gets or sets the trade price.</summary>
        /// <value>The price.</value>
        public decimal Price { get; set; }
    }

    /// <summary>
    /// A base record type whose <see cref="int" /> <c>Name</c> column <see cref="HidingRecord" /> hides.
    /// </summary>
    public class NamedBase
    {
        /// <summary>Gets or sets the base name, which a derived type hides.</summary>
        /// <value>The base name.</value>
        public int Name { get; set; }
    }

    /// <summary>
    /// A record POCO whose text <c>Name</c> property hides the base type's <c>Name</c> with the <see langword="new" />
    /// modifier.
    /// </summary>
    public sealed class HidingRecord
        : NamedBase
    {
        /// <summary>Gets or sets the name, hiding the base type's.</summary>
        /// <value>The name.</value>
        public new string? Name { get; set; }
    }

    /// <summary>
    /// A record POCO whose every property is excluded with <see cref="Bodu.Text.Serialization.IgnoreAttribute" />.
    /// </summary>
    public sealed class IgnoredRecord
    {
        /// <summary>Gets or sets an ignored secret.</summary>
        /// <value>The secret.</value>
        [Bodu.Text.Serialization.Ignore]
        public string? Secret { get; set; }

        /// <summary>Gets or sets an ignored comment.</summary>
        /// <value>The comment.</value>
        [Bodu.Text.Serialization.Ignore]
        public string? Comment { get; set; }
    }

    /// <summary>
    /// A record POCO with one instance property and one static property, which is not a column.
    /// </summary>
    public sealed class StaticPropertyRecord
    {
        /// <summary>Gets the static value, shared by every record.</summary>
        /// <value>The text <c>static</c>.</value>
        public static string StaticValue => "static";

        /// <summary>Gets or sets the instance column.</summary>
        /// <value>The column's text.</value>
        public string? A { get; set; }
    }

    /// <summary>
    /// A record POCO with an <see cref="int" /> identifier and a value column of a chosen type.
    /// </summary>
    /// <typeparam name="TValue">The type of the value column.</typeparam>
    public sealed class IdValueRecord<TValue>
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        public int Id { get; set; }

        /// <summary>Gets or sets the value.</summary>
        /// <value>The value.</value>
        public TValue? Value { get; set; }
    }

    /// <summary>
    /// A simple record POCO with a string and a typed column.
    /// </summary>
    public sealed class Person
    {
        /// <summary>Gets or sets the person's name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the person's age.</summary>
        /// <value>The age.</value>
        public int Age { get; set; }
    }

    /// <summary>
    /// A record POCO with a <see cref="Uri" /> column, validating the common scalar set.
    /// </summary>
    public sealed class LinkRecord
    {
        /// <summary>Gets or sets the site address.</summary>
        /// <value>The site address.</value>
        public Uri? Site { get; set; }
    }

    /// <summary>
    /// A hand-written <see cref="IDelimitedRecordFactory{TRecord}" /> for <see cref="Person" />, standing in for the
    /// generated factory in the reflection-free serializer overload tests.
    /// </summary>
    private sealed class PersonFactory : IDelimitedRecordFactory<Person>
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Headers { get; } = new[] { "Name", "Age" };

        /// <inheritdoc />
        public string[] GetFields(Person record) =>
            [record.Name, record.Age.ToString(CultureInfo.InvariantCulture)];

        /// <inheritdoc />
        public Person Create(string[] fields, IReadOnlyList<string> headers)
        {
            IReadOnlyList<string> columns = headers.Count > 0 ? headers : Headers;
            var person = new Person();

            for (int i = 0; i < fields.Length && i < columns.Count; i++)
            {
                if (string.Equals(columns[i], "Name", StringComparison.Ordinal))
                    person.Name = fields[i];
                else if (string.Equals(columns[i], "Age", StringComparison.Ordinal))
                    person.Age = int.Parse(fields[i], CultureInfo.InvariantCulture);
            }

            return person;
        }
    }
}
