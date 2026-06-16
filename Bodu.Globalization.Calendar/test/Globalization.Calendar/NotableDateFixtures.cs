// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Loads embedded notable-date document fixtures by file name for the v2 test catalogue.
/// </summary>
internal static class NotableDateFixtures
{
    /// <summary>
    /// Reads the raw XML text of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>strategies.xml</c>.</param>
    /// <returns>The fixture content.</returns>
    /// <exception cref="InvalidOperationException">The fixture is not embedded.</exception>
    public static string ReadText(string fileName)
    {
        string resourceName = "Bodu.Globalization.Calendar.Fixtures." + fileName;
        using Stream stream = typeof(NotableDateFixtures).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{resourceName}'.");
        using StreamReader reader = new(stream);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Loads and validates an embedded fixture into a <see cref="NotableDateResource" />.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>strategies.xml</c>.</param>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    public static NotableDateResource Load(string fileName) =>
        NotableDateResourceLoader.Load(ReadText(fileName));

    /// <summary>
    /// Loads an embedded fixture and returns a resolver over it.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>strategies.xml</c>.</param>
    /// <returns>A resolver for the loaded fixture.</returns>
    public static NotableDateService Resolver(string fileName) =>
        new(Load(fileName));
}
