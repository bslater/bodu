// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializer.StreamingFieldLists.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public static partial class DelimitedSerializer
{
    /// <summary>
    /// Holds the field lists that the writers of the records of one asynchronous serialization share, so that the
    /// records, each encoded by a writer of its own, do not each allocate them.
    /// </summary>
    private sealed class StreamingFieldLists
    {
        /// <summary>
        /// Gets the list the header names are kept in.
        /// </summary>
        public List<string> Headers { get; } = [];

        /// <summary>
        /// Gets the list a record's field names are collected in.
        /// </summary>
        public List<string> Names { get; } = [];

        /// <summary>
        /// Gets the list a record's field values are collected in.
        /// </summary>
        public List<string> Values { get; } = [];
    }
}
