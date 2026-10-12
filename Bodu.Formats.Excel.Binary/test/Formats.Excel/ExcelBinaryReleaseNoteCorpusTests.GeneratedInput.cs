// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.GeneratedInput.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents a generated input: a BIFF record stream, and how the Workbook surface stores it in a file.
    /// </summary>
    /// <param name="Stream">The record stream.</param>
    /// <param name="StreamName">The name of the compound-file stream that holds <paramref name="Stream" />.</param>
    private sealed record GeneratedInput(byte[] Stream, string StreamName)
    {
        /// <summary>
        /// Gets the extra streams stored beside the record stream.
        /// </summary>
        /// <value>The name and length of each, in the order the settings gave them; every byte of each is 0x5A.</value>
        public IReadOnlyList<(string Name, int Length)> Extras { get; init; } = [];

        /// <summary>
        /// Gets the container patches applied to the compound file after it is written.
        /// </summary>
        /// <value>The patches, in the order they apply.</value>
        public IReadOnlyList<string> ContainerPatches { get; init; } = [];

        /// <summary>
        /// Gets a value indicating whether the file is the bare record stream, with no compound file around it.
        /// </summary>
        /// <value><see langword="true" /> for an input with the <c>@container=none</c> setting.</value>
        public bool WithoutContainer { get; init; }

        /// <summary>
        /// Creates the file the Workbook surface opens.
        /// </summary>
        /// <returns>
        /// The record stream itself when <see cref="WithoutContainer" /> is set; otherwise a compound file that
        /// <see cref="CompoundFile.Create(Stream, bool, CompoundBuildOptions)" /> writes with the record stream and the
        /// extra streams in its root storage, with the container patches applied in order.
        /// </returns>
        /// <exception cref="FormatException">A container patch cannot be applied to the written file.</exception>
        public byte[] CreateFile()
        {
            if (WithoutContainer)
                return Stream;

            using var output = new MemoryStream();
            using (var file = CompoundFile.Create(output, leaveOpen: true))
            {
                file.RootStorage.CreateStream(StreamName, Stream);
                foreach ((string name, int length) in Extras)
                {
                    var filler = new byte[length];
                    Array.Fill(filler, (byte)0x5A);
                    file.RootStorage.CreateStream(name, filler);
                }

                file.Commit();
            }

            byte[] container = output.ToArray();
            foreach (string patch in ContainerPatches)
                container = ContainerPatcher.Apply(container, patch);

            return container;
        }
    }
}
