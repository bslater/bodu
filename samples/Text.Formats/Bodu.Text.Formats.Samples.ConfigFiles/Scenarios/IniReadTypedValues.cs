// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReadTypedValues.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Samples.Text.Formats.ConfigFiles.Scenarios;

/// <summary>
/// Demonstrates reading an INI file: the global (sectionless) area, named sections, typed value
/// access via <c>GetValue&lt;T&gt;</c>, and the comment model — comment lines are preserved on
/// the entry they precede. Note the parser deliberately treats <c>;</c> after a value as value
/// content (values may legitimately contain semicolons); inline comments are a write-side
/// feature via <c>IniEntry.InlineComment</c>.
/// </summary>
public static class IniReadTypedValues
{
    /// <summary>
    /// Parses <c>Data/app.ini</c> and reads global, section, typed, and comment data.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- INI: global section, named sections, typed values ---");

        var ini = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "app.ini"));
        var document = Ini.Parse(ini);

        // Keys before the first [section] live in the global section.
        Console.WriteLine($"global      : environment = {document.GlobalSection["environment"]}");
        Console.WriteLine($"sections    : {string.Join(", ", document.Sections.Select(s => s.Name))}");

        // Typed access - GetValue<T> parses with InvariantCulture, like the other formats.
        var server = document.GetSection("server")!;
        Console.WriteLine($"[server]    : {server["host"]}:{server.GetValue<int>("port")} (timeout {server.GetValue<int>("request_timeout")}s)");

        // TryGetValue for optional keys.
        var logging = document.GetSection("logging")!;
        var hasRetention = logging.TryGetValue<int>("retention_days", out _);
        Console.WriteLine($"[logging]   : level = {logging["level"]}, retention_days present = {hasRetention}");

        // Comments are first-class: a comment line attaches to the entry below it.
        var host = server.Entries.First(e => e.Key == "host");
        Console.WriteLine($"comments    : 'host' leading comment = '{host.LeadingComments[0].Text.Trim()}'");

        var path = logging.Entries.First(e => e.Key == "path");
        Console.WriteLine($"            : 'path' leading comment = '{path.LeadingComments[0].Text.Trim()}'");

        Console.WriteLine();
    }
}
