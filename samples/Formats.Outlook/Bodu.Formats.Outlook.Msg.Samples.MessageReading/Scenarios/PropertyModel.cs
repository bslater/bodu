// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyModel.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook;

namespace Bodu.Formats.Outlook.Msg.Samples.MessageReading.Scenarios;

/// <summary>
/// Demonstrates the layer beneath the conveniences: the tag-addressed <see cref="MapiPropertyCollection" /> that
/// <see cref="OutlookMessage.Subject" /> and its siblings are shorthand for.
/// </summary>
public static class PropertyModel
{
    /// <summary>
    /// Enumerates every decoded property on the message, then reads two of them by tag to show the shorthand and the
    /// raw lookup agreeing.
    /// </summary>
    /// <param name="path">The message file to read.</param>
    public static void Run(string path)
    {
        SampleConsole.Scenario(
            "The property model under the conveniences",
            what: "Lists every decoded property on the message with its tag, type, and value, then reads the subject "
                + "both ways - through the Subject shorthand and by its raw 0x0037 tag - and shows what a missing "
                + "tag does.",
            why: "The conveniences cover the properties nearly every consumer wants, but MAPI has thousands and a "
                + "mail archive will need ones no library can enumerate in advance. The collection is the escape "
                + "hatch: it is addressed by tag, so any property the file carries is reachable without waiting for "
                + "an API to expose it. Knowing the shorthand is a lookup - not a separate parse - is what makes it "
                + "safe to mix the two in one pass over a store.",
            expect: "Every property written by this sample appears in the listing with its type resolved (Unicode "
                + "for the strings). The two ways of reading the subject return the same string, and asking for a "
                + "tag the message does not carry reports absence through TryGetValue rather than throwing.");

        using var message = OutlookMessage.OpenRead(path);

        Console.WriteLine($"  Decoded properties ({message.Properties.Count}):");
        foreach (MapiProperty property in message.Properties.OrderBy(static p => p.Tag.Id))
        {
            // Long values are truncated for the transcript; the property itself carries the whole thing.
            var rendered = property.Value switch
            {
                string s => s.Length > 46 ? s[..43].Replace("\r\n", " ") + "..." : s.Replace("\r\n", " "),
                byte[] b => $"{b.Length} bytes",
                null => "(null)",
                var other => other.ToString(),
            };

            Console.WriteLine($"    0x{property.Tag.Id:X4} {property.Tag.Type,-10} {rendered}");
        }

        Console.WriteLine();

        // The shorthand and the tag lookup are two views of one decoded collection.
        var viaShorthand = message.Subject;
        var viaTag = message.Properties.TryGetValue(
            new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode), out MapiProperty? subject)
            ? subject!.Value as string
            : null;

        Console.WriteLine($"  message.Subject         : {viaShorthand}");
        Console.WriteLine($"  Properties[0x0037]      : {viaTag}");
        Console.WriteLine($"  same value              : {string.Equals(viaShorthand, viaTag, StringComparison.Ordinal)}   (the shorthand is a lookup into this collection, not a second parse)");

        // A tag the message does not carry is absence, not failure.
        var present = message.Properties.TryGetValue(
            new MapiPropertyTag(MapiPropertyIds.ClientSubmitTime, MapiPropertyType.SystemTime), out _);
        Console.WriteLine($"  0x0039 (submit time)    : present={present}   (this sample never wrote one - TryGetValue reports that rather than throwing)");
        Console.WriteLine();
    }
}
