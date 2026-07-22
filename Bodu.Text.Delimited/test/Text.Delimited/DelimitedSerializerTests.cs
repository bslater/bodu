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
